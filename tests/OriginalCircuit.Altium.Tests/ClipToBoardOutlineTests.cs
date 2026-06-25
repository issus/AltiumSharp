using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Rendering;
using OriginalCircuit.Altium.Rendering.Gltf;
using OriginalCircuit.Altium.Rendering.Raster;
using OriginalCircuit.Altium.Rendering.Svg;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Eda.Rendering;
using SkiaSharp;

namespace OriginalCircuit.Altium.Tests;

/// <summary>
/// Tests for the opt-in <c>ClipToBoardOutline</c> option that trims everything outside the physical
/// board outline, across the three render engines: the standard 2D editor view
/// (<see cref="PcbComponentRenderer"/> via the raster/SVG backends), the photorealistic 2D view
/// (<see cref="PcbRealisticRenderer"/>) and the glTF 3D export (<see cref="GltfRenderer"/>).
/// </summary>
/// <remarks>
/// The board is 40 x 30 mm. Silkscreen at 5..35 mm sits inside; a silk track running to 70 mm and a
/// "OUTSIDE" note near 50 mm overhang the 40 mm right edge. The tests prove the overhang is removed AND
/// the inside silk survives, not merely that the output changed.
/// </remarks>
public sealed class ClipToBoardOutlineTests
{
    private static readonly XNamespace SvgNs = "http://www.w3.org/2000/svg";

    private const double HalfWidthMm = 20.0;    // board-centred right edge (glTF local space)

    private static PcbDocument BuildBoardWithOverhang()
    {
        var board = new PcbDocument
        {
            BoardParameters = new Dictionary<string, string>
            {
                ["KIND0"] = "0", ["VX0"] = "0mil",      ["VY0"] = "0mil",
                ["KIND1"] = "0", ["VX1"] = "1574.8mil", ["VY1"] = "0mil",      // 40 mm
                ["KIND2"] = "0", ["VX2"] = "1574.8mil", ["VY2"] = "1181.1mil", // 40 x 30 mm
                ["KIND3"] = "0", ["VX3"] = "0mil",      ["VY3"] = "1181.1mil",
                ["KIND4"] = "0", ["VX4"] = "0mil",      ["VY4"] = "0mil",
            },
        };

        // Copper on the board (so a copper layer/feature exists).
        board.AddPad(PcbPad.Create("1").At(Coord.FromMm(8), Coord.FromMm(8))
            .Size(Coord.FromMm(2), Coord.FromMm(1.2)).Smd(1).Build());
        board.AddTrack(PcbTrack.Create().From(Coord.FromMm(8), Coord.FromMm(8))
            .To(Coord.FromMm(20), Coord.FromMm(15)).Width(Coord.FromMm(0.4)).Layer(1).Build());

        // Silkscreen INSIDE the board (5..35 mm).
        board.AddTrack(PcbTrack.Create().From(Coord.FromMm(5), Coord.FromMm(15))
            .To(Coord.FromMm(35), Coord.FromMm(15)).Width(Coord.FromMm(0.25)).Layer(33).Build());

        // Silkscreen that overhangs the right edge: starts inside, runs well past 40 mm.
        board.AddTrack(PcbTrack.Create().From(Coord.FromMm(30), Coord.FromMm(20))
            .To(Coord.FromMm(70), Coord.FromMm(20)).Width(Coord.FromMm(0.3)).Layer(33).Build());

        // A silk note placed entirely outside the board.
        board.AddText(new PcbText
        {
            Text = "OUTSIDE",
            Location = new CoordPoint(Coord.FromMm(50), Coord.FromMm(25)),
            Height = Coord.FromMm(2),
            Layer = 33,
        });

        return board;
    }

    private static PcbDocument BuildBoardNoOutline()
    {
        // No Board6 KIND/VX/VY params -> GetBoardOutline() is empty -> clipping is a no-op.
        var board = new PcbDocument();
        board.AddTrack(PcbTrack.Create().From(Coord.FromMm(5), Coord.FromMm(5))
            .To(Coord.FromMm(35), Coord.FromMm(5)).Width(Coord.FromMm(0.2)).Layer(33).Build());
        board.AddPad(PcbPad.Create("1").At(Coord.FromMm(8), Coord.FromMm(8))
            .Size(Coord.FromMm(2), Coord.FromMm(1.2)).Smd(1).Build());
        return board;
    }

    // ── Defaults ────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ClipToBoardOutline_DefaultsToFalse_OnAllThreeSettings()
    {
        Assert.False(new PcbRenderSettings().ClipToBoardOutline);
        Assert.False(new PcbRealisticStyle().ClipToBoardOutline);
        Assert.False(PcbRealisticStyle.GreenEnig.ClipToBoardOutline);
        Assert.False(PcbRealisticStyle.GreenEnig.For(PcbViewSide.Bottom).ClipToBoardOutline); // survives For()/clone
        Assert.False(new GltfRenderSettings().ClipToBoardOutline);
    }

    // ── Standard 2D editor view (PcbComponentRenderer) ───────────────────────────────────────────

    [Fact]
    public async Task Standard_Svg_ClipContourIsBoardOutline_AndExcludesOverhang()
    {
        var board = BuildBoardWithOverhang();
        var renderer = new SvgRenderer();

        var off = await RenderStandardSvg(renderer, board, new PcbRenderSettings { ClipToBoardOutline = false });
        var on = await RenderStandardSvg(renderer, board, new PcbRenderSettings { ClipToBoardOutline = true });

        // Disabled: the standard renderer uses a clip path ONLY for this feature, so none is emitted.
        Assert.Empty(off.Descendants(SvgNs + "clipPath"));

        // Enabled: the clip contour must be the board outline itself. The renderer draws the board
        // substrate as a black polygon of the same outline, so the clip's bounds must match it.
        var clipBox = ClipPathBBox(on);
        Assert.NotNull(clipBox);
        var boardFill = on.Descendants(SvgNs + "polygon")
            .First(p => (string?)p.Attribute("fill") == "rgb(0,0,0)");
        var boardBox = NumbersBBox(boardFill.Attribute("points")?.Value);
        Assert.NotNull(boardBox);
        AssertBoxesMatch(boardBox!.Value, clipBox!.Value, tol: 1.0);

        // The overhanging silk genuinely extends PAST the clip region, so the clip will trim it: the
        // right-most drawn coordinate in the unclipped render lies well beyond the clip's right edge.
        Assert.True(MaxDrawnX(off) > clipBox.Value.maxX + 5,
            "overhanging silk should extend beyond the board-outline clip");
    }

    [Fact]
    public async Task Standard_Raster_RemovesOverhangSilk_KeepsInsideSilk()
    {
        var board = BuildBoardWithOverhang();
        var renderer = new RasterRenderer();

        // Top-overlay silk renders yellow (0xFFFFFF00); count yellow pixels with and without the clip.
        int off = CountSilkYellow(await RenderStandardPng(renderer, board, new PcbRenderSettings { ClipToBoardOutline = false }));
        int on = CountSilkYellow(await RenderStandardPng(renderer, board, new PcbRenderSettings { ClipToBoardOutline = true }));

        Assert.True(on > 0, "the inside silk must survive the clip");
        Assert.True(on < off, "the overhanging silk pixels must be removed by the clip");
    }

    [Fact]
    public async Task Standard_Svg_NoOutline_ClipIsNoOp()
    {
        var board = BuildBoardNoOutline();
        var renderer = new SvgRenderer();

        // A board with no outline can't be clipped; it must still render and add no clip path.
        var doc = await RenderStandardSvg(renderer, board, new PcbRenderSettings { ClipToBoardOutline = true });
        Assert.Equal("svg", doc.Root!.Name.LocalName);
        Assert.Empty(doc.Descendants(SvgNs + "clipPath"));
    }

    [Fact]
    public async Task Standard_Svg_BottomView_AppliesClip()
    {
        // The clip must also work in a flipped bottom view (it is set in the post-flip frame).
        var board = BuildBoardWithOverhang();
        var renderer = new SvgRenderer();
        var doc = await RenderStandardSvg(renderer, board,
            new PcbRenderSettings { ClipToBoardOutline = true, ViewSide = PcbViewSide.Bottom });
        Assert.NotEmpty(doc.Descendants(SvgNs + "clipPath"));
    }

    // ── Photorealistic 2D view (PcbRealisticRenderer) ────────────────────────────────────────────

    [Fact]
    public async Task Realistic_Svg_OutlineClipMatchesSubstrate_OnlyWhenEnabled()
    {
        var board = BuildBoardWithOverhang();
        var renderer = new SvgRenderer();

        var off = await RenderRealisticSvg(renderer, board, new PcbRealisticStyle { ClipToBoardOutline = false });
        var on = await RenderRealisticSvg(renderer, board, new PcbRealisticStyle { ClipToBoardOutline = true });

        // The realistic renderer already uses a clip for the mask reveal (the union of pad openings — a
        // small bbox). The outline clip instead wraps the WHOLE stack with the board-outline contour, so a
        // clipPath whose bounds match the substrate polygon appears ONLY when the option is enabled.
        var substrateBox = SubstratePolygonBBox(on);
        Assert.NotNull(substrateBox);

        Assert.True(HasClipPathMatching(on, substrateBox!.Value, tol: 1.0),
            "an outline-sized clip path should wrap the stack when enabled");
        Assert.False(HasClipPathMatching(off, substrateBox!.Value, tol: 1.0),
            "no outline-sized clip path should exist when disabled");
        Assert.True(SubstrateHasClippedAncestor(on));
        Assert.False(SubstrateHasClippedAncestor(off));
    }

    [Fact]
    public async Task Realistic_Raster_RemovesOverhangSilk_KeepsInsideSilk()
    {
        var board = BuildBoardWithOverhang();
        var renderer = new RasterRenderer();

        // Black page background + white silk: count near-white (silk) pixels with and without the clip.
        // Disable cropping so both renders share the same framing; only the clip differs.
        var bg = new RenderOptions { Width = 400, Height = 300, BackgroundColor = EdaColor.FromRgb(0, 0, 0) };
        var styleOff = new PcbRealisticStyle { ClipToBoardOutline = false, CropToBoardBounds = false };
        var styleOn = new PcbRealisticStyle { ClipToBoardOutline = true, CropToBoardBounds = false };

        int off = CountNearWhite(await RenderRealisticPng(renderer, board, bg, styleOff));
        int on = CountNearWhite(await RenderRealisticPng(renderer, board, bg, styleOn));

        Assert.True(on > 0, "the inside silk must survive the clip");
        Assert.True(on < off, "the overhanging silk pixels must be removed by the clip");
    }

    [Fact]
    public async Task Realistic_Svg_NoOutline_ClipIsNoOp()
    {
        var board = BuildBoardNoOutline();
        var renderer = new SvgRenderer();
        var doc = await RenderRealisticSvg(renderer, board, new PcbRealisticStyle { ClipToBoardOutline = true });
        Assert.Equal("svg", doc.Root!.Name.LocalName);
        Assert.False(SubstrateHasClippedAncestor(doc));
    }

    // ── glTF 3D export (GltfSceneBuilder) ────────────────────────────────────────────────────────

    [Fact]
    public async Task Gltf_Silkscreen_OverhangTrimmed_InsidePreserved()
    {
        var board = BuildBoardWithOverhang();

        var (offMin, offMax) = await SilkscreenLocalXExtent(board, clip: false);
        var (onMin, onMax) = await SilkscreenLocalXExtent(board, clip: true);

        // Unclipped, the overhang reaches well past the +20 mm right edge (centred local space).
        Assert.True(offMax > HalfWidthMm + 2, $"unclipped silk should overhang the board (was {offMax:0.0} mm)");

        // Clipped, the silk is trimmed to the board's right edge...
        Assert.True(onMax <= HalfWidthMm + 0.5, $"clipped silk should stay within the board (was {onMax:0.0} mm)");

        // ...but the interior silk (the 5..35 mm track => left end ~ -15 mm centred) is preserved, not
        // clipped away: the clipped min X stays near the unclipped min X rather than collapsing.
        Assert.True(onMin <= -10, $"interior silk should be preserved (clipped min X was {onMin:0.0} mm)");
        Assert.True(Math.Abs(onMin - offMin) < 1.0, "clipping must not move the interior silk's left edge");
    }

    [Fact]
    public async Task Gltf_Clip_DoesNotRemoveSubstrateOrShrinkBelowBoard()
    {
        // The substrate IS the outline, so clipping must not shrink the overall model below the board:
        // the substrate node's extent stays at the board half-width whether or not clipping is on.
        var board = BuildBoardWithOverhang();
        double substrateMaxX = (await NodeLocalXExtent(board, "Substrate", clip: true)).max;
        Assert.True(Math.Abs(substrateMaxX - HalfWidthMm) < 0.5,
            $"substrate should still span the board ({substrateMaxX:0.0} mm vs {HalfWidthMm} mm)");
    }

    [Fact]
    public async Task Gltf_NoOutline_ClipIsNoOp()
    {
        // No outline -> nothing to clip to -> the model still builds with its silkscreen intact.
        var board = BuildBoardNoOutline();
        var json = await RenderGltfJson(board, clip: true);
        Assert.Contains("Silkscreen.", json);
    }

    // ── Render helpers ──────────────────────────────────────────────────────────────────────────

    private static async Task<XDocument> RenderStandardSvg(SvgRenderer renderer, PcbDocument board, PcbRenderSettings settings)
    {
        using var ms = new MemoryStream();
        await renderer.RenderAsync(board, ms, new RenderOptions { Width = 400, Height = 300 }, settings);
        ms.Position = 0;
        return XDocument.Load(ms);
    }

    private static async Task<byte[]> RenderStandardPng(RasterRenderer renderer, PcbDocument board, PcbRenderSettings settings)
    {
        using var ms = new MemoryStream();
        await renderer.RenderAsync(board, ms, new RenderOptions { Width = 400, Height = 300 }, settings);
        return ms.ToArray();
    }

    private static async Task<XDocument> RenderRealisticSvg(SvgRenderer renderer, PcbDocument board, PcbRealisticStyle style)
    {
        using var ms = new MemoryStream();
        await renderer.RenderRealisticAsync(board, ms, new RenderOptions { Width = 400, Height = 300 }, style);
        ms.Position = 0;
        return XDocument.Load(ms);
    }

    private static async Task<byte[]> RenderRealisticPng(RasterRenderer renderer, PcbDocument board, RenderOptions options, PcbRealisticStyle style)
    {
        using var ms = new MemoryStream();
        await renderer.RenderRealisticAsync(board, ms, options, style);
        return ms.ToArray();
    }

    private static async Task<string> RenderGltfJson(PcbDocument board, bool clip)
    {
        using var ms = new MemoryStream();
        await new GltfRenderer().RenderAsync(board, ms,
            new GltfRenderSettings { ClipToBoardOutline = clip, IncludeComponents = false });
        return ExtractGlbJson(ms.ToArray());
    }

    // ── Pixel helpers ───────────────────────────────────────────────────────────────────────────

    private static int CountSilkYellow(byte[] png)
    {
        AssertIsPng(png);
        using var ms = new MemoryStream(png);
        using var bmp = SKBitmap.Decode(ms);
        int count = 0;
        for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
            {
                var p = bmp.GetPixel(x, y);
                if (p.Red > 200 && p.Green > 200 && p.Blue < 80) count++; // top-overlay yellow
            }
        return count;
    }

    private static int CountNearWhite(byte[] png)
    {
        AssertIsPng(png);
        using var ms = new MemoryStream(png);
        using var bmp = SKBitmap.Decode(ms);
        int count = 0;
        for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
            {
                var p = bmp.GetPixel(x, y);
                if (p.Red > 225 && p.Green > 225 && p.Blue > 225) count++; // white silkscreen ink
            }
        return count;
    }

    // ── SVG geometry helpers ──────────────────────────────────────────────────────────────────────

    // True when the "substrate" group has an ancestor <g> carrying a clip-path (the outline clip wrapping
    // the whole stack), as opposed to the mask-reveal clip which sits BELOW the soldermask group.
    private static bool SubstrateHasClippedAncestor(XDocument doc)
    {
        var substrate = doc.Descendants(SvgNs + "g").FirstOrDefault(g => (string?)g.Attribute("id") == "substrate");
        return substrate is not null && substrate.Ancestors(SvgNs + "g").Any(a => a.Attribute("clip-path") != null);
    }

    private static (double minX, double minY, double maxX, double maxY)? ClipPathBBox(XDocument doc)
    {
        var d = doc.Descendants(SvgNs + "clipPath").Elements(SvgNs + "path")
            .Select(p => (string?)p.Attribute("d")).FirstOrDefault(d => d is not null);
        return NumbersBBox(d);
    }

    private static (double minX, double minY, double maxX, double maxY)? SubstratePolygonBBox(XDocument doc)
    {
        var poly = doc.Descendants(SvgNs + "g").First(g => (string?)g.Attribute("id") == "substrate")
            .Descendants(SvgNs + "polygon").FirstOrDefault();
        return NumbersBBox(poly?.Attribute("points")?.Value);
    }

    private static bool HasClipPathMatching(XDocument doc, (double minX, double minY, double maxX, double maxY) box, double tol)
        => doc.Descendants(SvgNs + "clipPath").Elements(SvgNs + "path")
            .Select(p => NumbersBBox((string?)p.Attribute("d")))
            .Any(b => b is not null && BoxesMatch(b.Value, box, tol));

    // Largest X over every drawn <line>/<polyline>/<polygon> coordinate — used to show the unclipped
    // overhang extends beyond the clip region.
    private static double MaxDrawnX(XDocument doc)
    {
        double max = double.MinValue;
        foreach (var line in doc.Descendants(SvgNs + "line"))
        {
            max = Math.Max(max, ParseD(line.Attribute("x1")));
            max = Math.Max(max, ParseD(line.Attribute("x2")));
        }
        foreach (var poly in doc.Descendants(SvgNs + "polyline").Concat(doc.Descendants(SvgNs + "polygon")))
        {
            var b = NumbersBBox(poly.Attribute("points")?.Value);
            if (b is not null) max = Math.Max(max, b.Value.maxX);
        }
        return max;
    }

    private static double ParseD(XAttribute? a) =>
        a is null ? double.MinValue : double.Parse(a.Value, CultureInfo.InvariantCulture);

    // Bounding box of every number in an SVG path "d" / polygon "points" string (treated as x,y pairs).
    private static (double minX, double minY, double maxX, double maxY)? NumbersBBox(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        var nums = Regex.Matches(s, @"-?\d+(?:\.\d+)?")
            .Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture)).ToList();
        if (nums.Count < 2) return null;
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        for (int i = 0; i + 1 < nums.Count; i += 2)
        {
            double x = nums[i], y = nums[i + 1];
            if (x < minX) minX = x; if (x > maxX) maxX = x;
            if (y < minY) minY = y; if (y > maxY) maxY = y;
        }
        return (minX, minY, maxX, maxY);
    }

    private static bool BoxesMatch((double minX, double minY, double maxX, double maxY) a,
        (double minX, double minY, double maxX, double maxY) b, double tol)
        => Math.Abs(a.minX - b.minX) <= tol && Math.Abs(a.minY - b.minY) <= tol
        && Math.Abs(a.maxX - b.maxX) <= tol && Math.Abs(a.maxY - b.maxY) <= tol;

    private static void AssertBoxesMatch((double minX, double minY, double maxX, double maxY) a,
        (double minX, double minY, double maxX, double maxY) b, double tol)
        => Assert.True(BoxesMatch(a, b, tol), $"expected boxes to match within {tol}: {a} vs {b}");

    // ── glTF inspection ───────────────────────────────────────────────────────────────────────────

    private static async Task<(double min, double max)> SilkscreenLocalXExtent(PcbDocument board, bool clip)
        => await NodeLocalXExtent(board, "Silkscreen.Top", clip);

    // Renders the board to GLB and returns the (min, max) local X (board-centred mm) of the named feature
    // node's POSITION accessor — read straight from the accessor's required min/max, no buffer decoding.
    private static async Task<(double min, double max)> NodeLocalXExtent(PcbDocument board, string nodeName, bool clip)
    {
        var json = await RenderGltfJson(board, clip);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        int meshIndex = -1;
        foreach (var node in root.GetProperty("nodes").EnumerateArray())
            if (node.TryGetProperty("name", out var n) && n.GetString() == nodeName &&
                node.TryGetProperty("mesh", out var m))
            {
                meshIndex = m.GetInt32();
                break;
            }
        Assert.True(meshIndex >= 0, $"expected a '{nodeName}' node with a mesh");

        var primitive = root.GetProperty("meshes")[meshIndex].GetProperty("primitives")[0];
        int posAccessor = primitive.GetProperty("attributes").GetProperty("POSITION").GetInt32();
        var accessor = root.GetProperty("accessors")[posAccessor];
        return (accessor.GetProperty("min")[0].GetDouble(), accessor.GetProperty("max")[0].GetDouble());
    }

    // GLB layout: 12-byte header, then chunk-0 (JSON) with its 4-byte length at offset 12 and data at 20.
    private static string ExtractGlbJson(byte[] glb)
    {
        int jsonLength = BitConverter.ToInt32(glb, 12);
        return Encoding.UTF8.GetString(glb, 20, jsonLength);
    }

    private static void AssertIsPng(byte[] bytes)
    {
        Assert.True(bytes.Length > 8, "PNG output should be non-empty");
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);
    }
}
