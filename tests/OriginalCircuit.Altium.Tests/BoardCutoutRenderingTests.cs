using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Rendering;
using OriginalCircuit.Altium.Rendering.Svg;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Eda.Rendering;
using Xunit;

namespace OriginalCircuit.Altium.Tests;

/// <summary>
/// Tests that board cut-outs (regions flagged <c>ISBOARDCUTOUT</c>, which live on a mechanical layer and
/// are otherwise drawn by no layer pass) are rendered as holes through the board in the photorealistic
/// view. Regression for the "board cut-outs are not displayed" report.
/// </summary>
public sealed class BoardCutoutRenderingTests
{
    private static readonly XNamespace SvgNs = "http://www.w3.org/2000/svg";

    private static PcbDocument BoardWithOutline() => new()
    {
        BoardParameters = new System.Collections.Generic.Dictionary<string, string>
        {
            ["KIND0"] = "0", ["VX0"] = "0mil",      ["VY0"] = "0mil",
            ["KIND1"] = "0", ["VX1"] = "1574.8mil", ["VY1"] = "0mil",      // 40 mm
            ["KIND2"] = "0", ["VX2"] = "1574.8mil", ["VY2"] = "1181.1mil", // 40 x 30 mm
            ["KIND3"] = "0", ["VX3"] = "0mil",      ["VY3"] = "1181.1mil",
            ["KIND4"] = "0", ["VX4"] = "0mil",      ["VY4"] = "0mil",
        },
    };

    // A square board cut-out on a mechanical layer (56), flagged ISBOARDCUTOUT.
    private static PcbRegion BoardCutout()
    {
        var region = PcbRegion.Create().OnLayer(56)
            .AddPoint(Coord.FromMm(15), Coord.FromMm(10))
            .AddPoint(Coord.FromMm(25), Coord.FromMm(10))
            .AddPoint(Coord.FromMm(25), Coord.FromMm(20))
            .AddPoint(Coord.FromMm(15), Coord.FromMm(20))
            .Build();
        region.IsBoardCutout = true;
        return region;
    }

    private static async Task<XDocument> RenderRealisticSvg(PcbDocument board)
    {
        var renderer = new SvgRenderer();
        using var ms = new MemoryStream();
        await renderer.RenderRealisticAsync(board, ms, new RenderOptions { Width = 400, Height = 300 });
        ms.Position = 0;
        return XDocument.Load(ms);
    }

    [Fact]
    public async Task BoardCutout_RendersAsHoleGroup()
    {
        var board = BoardWithOutline();
        board.AddRegion(BoardCutout());

        var doc = await RenderRealisticSvg(board);

        var cutouts = doc.Descendants(SvgNs + "g")
            .FirstOrDefault(g => (string?)g.Attribute("id") == "cutouts");
        Assert.NotNull(cutouts);
        Assert.Contains(cutouts!.Descendants(),
            e => e.Name == SvgNs + "polygon" || e.Name == SvgNs + "path");
    }

    [Fact]
    public async Task NoBoardCutout_EmitsNoCutoutsGroup()
    {
        // A board with no cut-outs and no milling-layer geometry must not emit a "cutouts" group.
        var doc = await RenderRealisticSvg(BoardWithOutline());

        Assert.DoesNotContain(doc.Descendants(SvgNs + "g"),
            g => (string?)g.Attribute("id") == "cutouts");
    }
}
