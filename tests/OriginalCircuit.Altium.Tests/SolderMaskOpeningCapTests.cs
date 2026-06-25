using System.Collections.Generic;
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
/// A track on a solder-mask layer is stroked into a mask-opening contour; it must get ROUND end caps so the
/// exposed copper reads like Altium's rounded track ends, not a flat-ended rectangle. Regression for the
/// #40 follow-up ("solder mask tracks rendered as rectangles with flat ends").
/// </summary>
public sealed class SolderMaskOpeningCapTests
{
    private static readonly XNamespace SvgNs = "http://www.w3.org/2000/svg";

    [Fact]
    public async Task SolderMaskLayerTrack_OpeningIsRoundCapped()
    {
        var board = new PcbDocument
        {
            BoardParameters = new Dictionary<string, string>
            {
                ["KIND0"] = "0", ["VX0"] = "0mil",      ["VY0"] = "0mil",
                ["KIND1"] = "0", ["VX1"] = "1574.8mil", ["VY1"] = "0mil",
                ["KIND2"] = "0", ["VX2"] = "1574.8mil", ["VY2"] = "1181.1mil",
                ["KIND3"] = "0", ["VX3"] = "0mil",      ["VY3"] = "1181.1mil",
                ["KIND4"] = "0", ["VX4"] = "0mil",      ["VY4"] = "0mil",
            },
        };
        // A single mask-opening track on the top solder-mask layer (37), and no pads — so the openings clip
        // contains only this one stroke.
        board.AddTrack(PcbTrack.Create().From(Coord.FromMm(10), Coord.FromMm(15))
            .To(Coord.FromMm(30), Coord.FromMm(15)).Width(Coord.FromMm(2)).Layer(37).Build());

        var renderer = new SvgRenderer();
        using var ms = new MemoryStream();
        await renderer.RenderRealisticAsync(board, ms, new RenderOptions { Width = 400, Height = 300 });
        ms.Position = 0;
        var doc = XDocument.Load(ms);

        // The openings live in a <clipPath>; our single track is one subpath (one 'M'). Count its vertices
        // (M + L commands): a square stroke has 4 corners, a round-capped stroke has many (an arc per end).
        var singleSubpath = doc.Descendants(SvgNs + "clipPath")
            .SelectMany(cp => cp.Descendants(SvgNs + "path"))
            .Select(p => p.Attribute("d")?.Value ?? "")
            .Where(d => d.Count(ch => ch == 'M') == 1)
            .OrderByDescending(d => d.Count(ch => ch is 'M' or 'L'))
            .FirstOrDefault();

        Assert.NotNull(singleSubpath);
        int vertices = singleSubpath!.Count(ch => ch is 'M' or 'L');
        Assert.True(vertices > 10, $"mask opening should be round-capped, not a 4-corner rectangle (had {vertices} vertices)");
    }
}
