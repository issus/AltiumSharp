using System.Collections.Generic;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using Xunit;

namespace OriginalCircuit.Altium.Tests;

/// <summary>
/// Tests for <see cref="PcbDocument.GetFramingBounds"/>, the bounds a renderer auto-zooms to. It must
/// frame the physical board and ignore content lying entirely off-board (off-sheet notes, title blocks,
/// auto-placed hidden designators/comments), so a board carrying such clutter still fills the view rather
/// than zooming far out. Regression for the "auto-zoom doesn't work" report on a board with off-board
/// silkscreen text.
/// </summary>
public sealed class FramingBoundsTests
{
    // A 40 x 30 mm rectangular board outline (mils in the Board6 KIND/VX/VY keys, closing vertex repeated).
    private static PcbDocument BoardWithOutline() => new()
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

    [Fact]
    public void GetFramingBounds_ExcludesContentEntirelyOffBoard()
    {
        var board = BoardWithOutline();

        // On-board copper.
        board.AddTrack(PcbTrack.Create().From(Coord.FromMm(5), Coord.FromMm(5))
            .To(Coord.FromMm(35), Coord.FromMm(25)).Width(Coord.FromMm(0.3)).Layer(1).Build());

        // A silk note placed far off the board (the kind of off-sheet clutter that wrecks auto-zoom).
        board.AddText(new PcbText
        {
            Text = "FAR",
            Location = new CoordPoint(Coord.FromMm(200), Coord.FromMm(200)),
            Height = Coord.FromMm(2),
            Layer = 33,
        });

        // Bounds (full extent) is pulled out to the off-board note...
        Assert.True(board.Bounds.Max.X.ToMm() > 150, "Bounds should include the off-board note");

        // ...but the framing bounds stays on the board (a little under the 40 mm width is fine; nowhere
        // near the 200 mm note).
        var framing = board.GetFramingBounds();
        Assert.True(framing.Max.X.ToMm() < 60, $"framing X={framing.Max.X.ToMm():F1} mm should exclude the 200 mm note");
        Assert.True(framing.Max.Y.ToMm() < 60, $"framing Y={framing.Max.Y.ToMm():F1} mm should exclude the 200 mm note");

        // The board itself is still fully framed.
        Assert.True(framing.Max.X.ToMm() >= 39.9, "framing should cover the 40 mm board width");
        Assert.True(framing.Max.Y.ToMm() >= 29.9, "framing should cover the 30 mm board height");
    }

    [Fact]
    public void GetFramingBounds_KeepsEdgeOverhangThatTouchesBoard()
    {
        var board = BoardWithOutline();

        // A track that starts on the board and overhangs the right edge to 70 mm: it touches the board,
        // so it is legitimate edge content and must stay in frame.
        board.AddTrack(PcbTrack.Create().From(Coord.FromMm(30), Coord.FromMm(20))
            .To(Coord.FromMm(70), Coord.FromMm(20)).Width(Coord.FromMm(0.3)).Layer(33).Build());

        var framing = board.GetFramingBounds();
        Assert.True(framing.Max.X.ToMm() >= 69.9, $"framing X={framing.Max.X.ToMm():F1} mm should include the edge overhang");
    }

    [Fact]
    public void GetFramingBounds_NoOutline_FallsBackToBounds()
    {
        var board = new PcbDocument();
        board.AddTrack(PcbTrack.Create().From(Coord.FromMm(5), Coord.FromMm(5))
            .To(Coord.FromMm(35), Coord.FromMm(5)).Width(Coord.FromMm(0.2)).Layer(33).Build());

        Assert.Equal(board.Bounds, board.GetFramingBounds());
    }
}
