using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Serialization.Readers;
using OriginalCircuit.Altium.Serialization.Writers;

namespace OriginalCircuit.Altium.Tests.RoundTrip;

public sealed class PcbDocRoundTripTests
{
    [Fact]
    public void WriteThenRead_PreservesTracks()
    {
        var original = new PcbDocument();
        original.AddTrack(new PcbTrack
        {
            Start = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(1000), Coord.FromMils(0)),
            Width = Coord.FromMils(10),
            Layer = 1
        });
        original.AddTrack(new PcbTrack
        {
            Start = new CoordPoint(Coord.FromMils(1000), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(1000), Coord.FromMils(500)),
            Width = Coord.FromMils(10),
            Layer = 1
        });

        using var ms = new MemoryStream();
        new PcbDocWriter().Write(original, ms);

        ms.Position = 0;
        var readBack = new PcbDocReader().Read(ms);

        Assert.Equal(2, readBack.Tracks.Count);
    }

    [Fact]
    public void WriteThenRead_PreservesPads()
    {
        var original = new PcbDocument();
        original.AddPad(new PcbPad
        {
            Location = new CoordPoint(Coord.FromMils(100), Coord.FromMils(100)),
            SizeTop = new CoordPoint(Coord.FromMils(60), Coord.FromMils(60)),
            HoleSize = Coord.FromMils(30),
            Layer = 1,
            Designator = "1"
        });

        using var ms = new MemoryStream();
        new PcbDocWriter().Write(original, ms);

        ms.Position = 0;
        var readBack = new PcbDocReader().Read(ms);

        Assert.Single(readBack.Pads);
    }

    [Fact]
    public void WriteThenRead_PreservesArcs()
    {
        var original = new PcbDocument();
        original.AddArc(new PcbArc
        {
            Center = new CoordPoint(Coord.FromMils(500), Coord.FromMils(500)),
            Radius = Coord.FromMils(200),
            StartAngle = 0,
            EndAngle = 360,
            Width = Coord.FromMils(10),
            Layer = 1
        });

        using var ms = new MemoryStream();
        new PcbDocWriter().Write(original, ms);

        ms.Position = 0;
        var readBack = new PcbDocReader().Read(ms);

        Assert.Single(readBack.Arcs);
    }

    [Fact]
    public void WriteThenRead_PreservesMultiplePrimitiveTypes()
    {
        var original = new PcbDocument();

        original.AddTrack(new PcbTrack
        {
            Start = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(500), Coord.FromMils(0)),
            Width = Coord.FromMils(10),
            Layer = 1
        });

        original.AddPad(new PcbPad
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            SizeTop = new CoordPoint(Coord.FromMils(60), Coord.FromMils(60)),
            HoleSize = Coord.FromMils(30),
            Layer = 1,
            Designator = "1"
        });

        original.AddVia(new PcbVia
        {
            Location = new CoordPoint(Coord.FromMils(250), Coord.FromMils(250)),
            Diameter = Coord.FromMils(50),
            HoleSize = Coord.FromMils(25)
        });

        using var ms = new MemoryStream();
        new PcbDocWriter().Write(original, ms);

        ms.Position = 0;
        var readBack = new PcbDocReader().Read(ms);

        Assert.Single(readBack.Tracks);
        Assert.Single(readBack.Pads);
        Assert.Single(readBack.Vias);
    }

    [SkippableFact]
    public void WriteThenRead_RealFiles_PreservesPrimitiveCounts()
    {
        var testDataPath = GetTestDataPath();
        if (!Directory.Exists(testDataPath)) { Skip.If(true, "Test data not available"); return; }

        foreach (var filePath in Directory.GetFiles(testDataPath, "*.PcbDoc"))
        {
            var originalDoc = new PcbDocReader().Read(File.OpenRead(filePath));

            using var ms = new MemoryStream();
            new PcbDocWriter().Write(originalDoc, ms);

            ms.Position = 0;
            var roundTripped = new PcbDocReader().Read(ms);

            Assert.Equal(originalDoc.Tracks.Count, roundTripped.Tracks.Count);
            Assert.Equal(originalDoc.Pads.Count, roundTripped.Pads.Count);
            Assert.Equal(originalDoc.Vias.Count, roundTripped.Vias.Count);
            Assert.Equal(originalDoc.Arcs.Count, roundTripped.Arcs.Count);
            Assert.Equal(originalDoc.Fills.Count, roundTripped.Fills.Count);
            Assert.Equal(originalDoc.Texts.Count, roundTripped.Texts.Count);
            Assert.Equal(originalDoc.Regions.Count, roundTripped.Regions.Count);
            Assert.Equal(originalDoc.ComponentBodies.Count, roundTripped.ComponentBodies.Count);
            Assert.Equal(originalDoc.Polygons.Count, roundTripped.Polygons.Count);
            Assert.Equal(originalDoc.EmbeddedBoards.Count, roundTripped.EmbeddedBoards.Count);
        }
    }

    [SkippableFact]
    public void WriteThenRead_RealFiles_PreservesTrackProperties()
    {
        var testDataPath = GetTestDataPath();
        var filePath = Path.Combine(testDataPath, "SPI Isolator.PcbDoc");
        if (!File.Exists(filePath)) { Skip.If(true, "Test data not available"); return; }

        var original = new PcbDocReader().Read(File.OpenRead(filePath));

        using var ms = new MemoryStream();
        new PcbDocWriter().Write(original, ms);
        ms.Position = 0;
        var rt = new PcbDocReader().Read(ms);

        // Verify first few tracks preserve key properties
        for (int i = 0; i < Math.Min(5, original.Tracks.Count); i++)
        {
            Assert.Equal(original.Tracks[i].Start.X.ToRaw(), rt.Tracks[i].Start.X.ToRaw());
            Assert.Equal(original.Tracks[i].Start.Y.ToRaw(), rt.Tracks[i].Start.Y.ToRaw());
            Assert.Equal(original.Tracks[i].End.X.ToRaw(), rt.Tracks[i].End.X.ToRaw());
            Assert.Equal(original.Tracks[i].End.Y.ToRaw(), rt.Tracks[i].End.Y.ToRaw());
            Assert.Equal(original.Tracks[i].Width.ToRaw(), rt.Tracks[i].Width.ToRaw());
        }
    }

    [SkippableFact]
    public void WriteThenRead_PanelFile_PreservesEmbeddedBoardProperties()
    {
        var testDataPath = GetTestDataPath();
        var filePath = Path.Combine(testDataPath, "Power Adapter Panel.PcbDoc");
        if (!File.Exists(filePath)) { Skip.If(true, "Test data not available"); return; }

        var original = new PcbDocReader().Read(File.OpenRead(filePath));
        Assert.True(original.EmbeddedBoards.Count > 0);

        using var ms = new MemoryStream();
        new PcbDocWriter().Write(original, ms);
        ms.Position = 0;
        var rt = new PcbDocReader().Read(ms);

        Assert.Equal(original.EmbeddedBoards.Count, rt.EmbeddedBoards.Count);
        var origEb = original.EmbeddedBoards[0];
        var rtEb = rt.EmbeddedBoards[0];

        Assert.Equal(origEb.DocumentPath, rtEb.DocumentPath);
        Assert.Equal(origEb.Layer, rtEb.Layer);
        Assert.Equal(origEb.Rotation, rtEb.Rotation, 0.01);
        Assert.Equal(origEb.MirrorFlag, rtEb.MirrorFlag);
        Assert.Equal(origEb.OriginMode, rtEb.OriginMode);
        Assert.Equal(origEb.ColCount, rtEb.ColCount);
        Assert.Equal(origEb.RowCount, rtEb.RowCount);
        Assert.Equal(origEb.ColSpacing.ToMils(), rtEb.ColSpacing.ToMils(), 0.1);
        Assert.Equal(origEb.RowSpacing.ToMils(), rtEb.RowSpacing.ToMils(), 0.1);
        Assert.Equal(origEb.X1Location.ToMils(), rtEb.X1Location.ToMils(), 0.1);
        Assert.Equal(origEb.Y1Location.ToMils(), rtEb.Y1Location.ToMils(), 0.1);
        Assert.Equal(origEb.X2Location.ToMils(), rtEb.X2Location.ToMils(), 0.1);
        Assert.Equal(origEb.Y2Location.ToMils(), rtEb.Y2Location.ToMils(), 0.1);
        Assert.Equal(origEb.IsViewport, rtEb.IsViewport);
        Assert.Equal(origEb.ViewportVisible, rtEb.ViewportVisible);
        Assert.Equal(origEb.UserRouted, rtEb.UserRouted);
        Assert.Equal(origEb.IsKeepout, rtEb.IsKeepout);
    }

    private static string GetTestDataPath()
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "TestData");
    }
}
