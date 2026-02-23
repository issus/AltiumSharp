using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Serialization.Readers;
using OriginalCircuit.Altium.Serialization.Writers;

namespace OriginalCircuit.Altium.Tests.RoundTrip;

/// <summary>
/// Tests for verifying PCB library round-trip serialization (write → read → compare).
/// </summary>
public sealed class PcbLibRoundTripTests
{
    private static PcbLibrary RoundTrip(PcbLibrary original)
    {
        using var ms = new MemoryStream();
        new PcbLibWriter().Write(original, ms);
        ms.Position = 0;
        return (PcbLibrary)new PcbLibReader().Read(ms);
    }

    [Fact]
    public void Pad_PreservesAllProperties()
    {
        var original = new PcbLibrary();
        var component = PcbComponent.Create("TestPad")
            .AddPad(p => p
                .At(Coord.FromMils(100), Coord.FromMils(200))
                .Size(Coord.FromMils(60), Coord.FromMils(80))
                .HoleSize(Coord.FromMils(30))
                .WithDesignator("A1")
                .Rotation(45)
                .Layer(1))
            .Build();
        original.Add(component);

        var readBack = RoundTrip(original);
        var pad = (PcbPad)readBack.Components.First().Pads.First();

        Assert.Equal(100, pad.Location.X.ToMils(), 1);
        Assert.Equal(200, pad.Location.Y.ToMils(), 1);
        Assert.Equal(60, pad.SizeTop.X.ToMils(), 1);
        Assert.Equal(80, pad.SizeTop.Y.ToMils(), 1);
        Assert.Equal(30, pad.HoleSize.ToMils(), 1);
        Assert.Equal("A1", pad.Designator);
        Assert.Equal(45, pad.Rotation, 1);
        Assert.Equal(1, pad.Layer);
    }

    [Fact]
    public void Track_PreservesAllProperties()
    {
        var original = new PcbLibrary();
        var component = PcbComponent.Create("TestTrack")
            .AddTrack(t => t
                .From(Coord.FromMils(0), Coord.FromMils(0))
                .To(Coord.FromMils(500), Coord.FromMils(300))
                .Width(Coord.FromMils(10))
                .OnLayer(1))
            .Build();
        original.Add(component);

        var readBack = RoundTrip(original);
        var track = (PcbTrack)readBack.Components.First().Tracks.First();

        Assert.Equal(0, track.Start.X.ToMils(), 1);
        Assert.Equal(0, track.Start.Y.ToMils(), 1);
        Assert.Equal(500, track.End.X.ToMils(), 1);
        Assert.Equal(300, track.End.Y.ToMils(), 1);
        Assert.Equal(10, track.Width.ToMils(), 1);
        Assert.Equal(1, track.Layer);
    }

    [Fact]
    public void Via_PreservesAllProperties()
    {
        var original = new PcbLibrary();
        var component = PcbComponent.Create("TestVia")
            .AddVia(v => v
                .At(Coord.FromMils(250), Coord.FromMils(150))
                .Diameter(Coord.FromMils(40))
                .HoleSize(Coord.FromMils(20))
                .Layers(1, 32))
            .Build();
        original.Add(component);

        var readBack = RoundTrip(original);
        var via = (PcbVia)readBack.Components.First().Vias.First();

        Assert.Equal(250, via.Location.X.ToMils(), 1);
        Assert.Equal(150, via.Location.Y.ToMils(), 1);
        Assert.Equal(40, via.Diameter.ToMils(), 1);
        Assert.Equal(20, via.HoleSize.ToMils(), 1);
        Assert.Equal(1, via.StartLayer);
        Assert.Equal(32, via.EndLayer);
    }

    [Fact]
    public void Arc_PreservesAllProperties()
    {
        var original = new PcbLibrary();
        var component = PcbComponent.Create("TestArc")
            .AddArc(a => a
                .At(Coord.FromMils(100), Coord.FromMils(100))
                .Radius(Coord.FromMils(50))
                .Angles(45, 270)
                .Width(Coord.FromMils(10))
                .OnLayer(1))
            .Build();
        original.Add(component);

        var readBack = RoundTrip(original);
        var arc = (PcbArc)readBack.Components.First().Arcs.First();

        Assert.Equal(100, arc.Center.X.ToMils(), 1);
        Assert.Equal(100, arc.Center.Y.ToMils(), 1);
        Assert.Equal(50, arc.Radius.ToMils(), 1);
        Assert.Equal(45, arc.StartAngle, 1);
        Assert.Equal(270, arc.EndAngle, 1);
        Assert.Equal(10, arc.Width.ToMils(), 1);
        Assert.Equal(1, arc.Layer);
    }

    [Fact]
    public void Text_PreservesBasicProperties()
    {
        var original = new PcbLibrary();
        var component = PcbComponent.Create("TestText")
            .AddText(".Designator", t => t
                .At(Coord.FromMils(0), Coord.FromMils(0))
                .Height(Coord.FromMils(60))
                .StrokeWidth(Coord.FromMils(6))
                .OnLayer(22)
                .Rotation(0))
            .Build();
        original.Add(component);

        var readBack = RoundTrip(original);
        var text = (PcbText)readBack.Components.First().Texts.First();

        Assert.Equal(0, text.Location.X.ToMils(), 1);
        Assert.Equal(0, text.Location.Y.ToMils(), 1);
        Assert.Equal(".Designator", text.Text);
        Assert.Equal(60, text.Height.ToMils(), 1);
        Assert.Equal(6, text.StrokeWidth.ToMils(), 1);
        Assert.Equal(22, text.Layer);
    }

    [Fact]
    public void Text_PreservesExtendedProperties()
    {
        var original = new PcbLibrary();
        var component = PcbComponent.Create("TestTextExt")
            .AddText("TrueType Text", t => t
                .At(Coord.FromMils(50), Coord.FromMils(100))
                .Height(Coord.FromMils(40))
                .StrokeWidth(Coord.FromMils(0))
                .OnLayer(22)
                .Rotation(90))
            .Build();

        // Set extended properties after build
        var textPrim = (PcbText)component.Texts.First();
        textPrim.TextKind = PcbTextKind.TrueType;
        textPrim.FontBold = true;
        textPrim.FontItalic = true;
        textPrim.FontName = "Times New Roman";
        textPrim.IsInverted = true;
        textPrim.InvertedBorder = Coord.FromMils(5);
        textPrim.IsMirrored = true;

        original.Add(component);

        var readBack = RoundTrip(original);
        var text = (PcbText)readBack.Components.First().Texts.First();

        Assert.Equal(50, text.Location.X.ToMils(), 1);
        Assert.Equal(100, text.Location.Y.ToMils(), 1);
        Assert.Equal("TrueType Text", text.Text);
        Assert.Equal(40, text.Height.ToMils(), 1);
        Assert.Equal(90, text.Rotation, 1);
        Assert.True(text.IsMirrored);
        Assert.Equal(PcbTextKind.TrueType, text.TextKind);
        Assert.True(text.FontBold);
        Assert.True(text.FontItalic);
        Assert.Equal("Times New Roman", text.FontName);
        Assert.True(text.IsInverted);
        Assert.Equal(5, text.InvertedBorder.ToMils(), 1);
    }

    [Fact]
    public void Fill_PreservesAllProperties()
    {
        var original = new PcbLibrary();
        var component = PcbComponent.Create("TestFill")
            .AddFill(f => f
                .From(Coord.FromMils(-50), Coord.FromMils(-25))
                .To(Coord.FromMils(50), Coord.FromMils(25))
                .Rotation(30)
                .OnLayer(1))
            .Build();
        original.Add(component);

        var readBack = RoundTrip(original);
        var fill = (PcbFill)((PcbComponent)readBack.Components.First()).Fills.First();

        Assert.Equal(-50, fill.Corner1.X.ToMils(), 1);
        Assert.Equal(-25, fill.Corner1.Y.ToMils(), 1);
        Assert.Equal(50, fill.Corner2.X.ToMils(), 1);
        Assert.Equal(25, fill.Corner2.Y.ToMils(), 1);
        Assert.Equal(30, fill.Rotation, 1);
        Assert.Equal(1, fill.Layer);
    }

    [Fact]
    public void MultipleComponents_PreservesAll()
    {
        var original = new PcbLibrary();

        for (var i = 1; i <= 3; i++)
        {
            var component = PcbComponent.Create($"Component{i}")
                .WithDescription($"Description {i}")
                .AddPad(p => p
                    .At(Coord.FromMils(i * 10), Coord.FromMils(i * 10))
                    .Size(Coord.FromMils(50), Coord.FromMils(50))
                    .WithDesignator("1"))
                .Build();
            original.Add(component);
        }

        var readBack = RoundTrip(original);

        Assert.Equal(3, readBack.Components.Count);
        var names = readBack.Components.Select(c => c.Name).OrderBy(n => n).ToList();
        Assert.Equal(new[] { "Component1", "Component2", "Component3" }, names);
    }

    [SkippableFact]
    public void RealFiles_PreservesComponentAndPrimitiveCounts()
    {
        var testDataPath = GetTestDataPath();
        if (!Directory.Exists(testDataPath)) { Skip.If(true, "Test data not available"); return; }

        foreach (var filePath in Directory.GetFiles(testDataPath, "*.PcbLib"))
        {
            var originalLib = (PcbLibrary)new PcbLibReader().Read(File.OpenRead(filePath));

            using var ms = new MemoryStream();
            new PcbLibWriter().Write(originalLib, ms);

            ms.Position = 0;
            var roundTripped = (PcbLibrary)new PcbLibReader().Read(ms);

            Assert.Equal(originalLib.Components.Count, roundTripped.Components.Count);

            for (int i = 0; i < originalLib.Components.Count; i++)
            {
                var oc = (PcbComponent)originalLib.Components[i];
                var rc = (PcbComponent)roundTripped.Components[i];
                Assert.Equal(oc.Name, rc.Name);
                Assert.Equal(oc.Pads.Count, rc.Pads.Count);
                Assert.Equal(oc.Tracks.Count, rc.Tracks.Count);
                Assert.Equal(oc.Arcs.Count, rc.Arcs.Count);
                Assert.Equal(oc.Vias.Count, rc.Vias.Count);
                Assert.Equal(oc.Texts.Count, rc.Texts.Count);
                Assert.Equal(oc.Fills.Count, rc.Fills.Count);
                Assert.Equal(oc.Regions.Count, rc.Regions.Count);
            }
        }
    }

    [Fact]
    public void ComponentWithAllPrimitiveTypes_PreservesCounts()
    {
        var original = new PcbLibrary();
        var component = PcbComponent.Create("AllTypes")
            .WithDescription("Has every primitive type")
            .WithHeight(Coord.FromMils(100))
            .AddPad(p => p.At(Coord.FromMils(0), Coord.FromMils(0)).Size(Coord.FromMils(60), Coord.FromMils(60)).HoleSize(Coord.FromMils(30)).WithDesignator("1"))
            .AddPad(p => p.At(Coord.FromMils(100), Coord.FromMils(0)).Size(Coord.FromMils(60), Coord.FromMils(60)).HoleSize(Coord.FromMils(30)).WithDesignator("2"))
            .AddTrack(t => t.From(Coord.FromMils(0), Coord.FromMils(-50)).To(Coord.FromMils(100), Coord.FromMils(-50)).Width(Coord.FromMils(10)))
            .AddVia(v => v.At(Coord.FromMils(50), Coord.FromMils(50)).Diameter(Coord.FromMils(40)).HoleSize(Coord.FromMils(20)))
            .AddArc(a => a.At(Coord.FromMils(0), Coord.FromMils(0)).Radius(Coord.FromMils(30)).Angles(0, 360).Width(Coord.FromMils(10)))
            .AddText(".Designator", t => t.At(Coord.FromMils(0), Coord.FromMils(100)).Height(Coord.FromMils(60)).StrokeWidth(Coord.FromMils(6)).OnLayer(22))
            .AddFill(f => f.From(Coord.FromMils(-25), Coord.FromMils(-25)).To(Coord.FromMils(25), Coord.FromMils(25)).OnLayer(1))
            .Build();
        original.Add(component);

        var readBack = RoundTrip(original);
        var comp = (PcbComponent)readBack.Components.First();

        Assert.Equal("AllTypes", comp.Name);
        Assert.Equal("Has every primitive type", comp.Description);
        Assert.Equal(2, comp.Pads.Count);
        Assert.Single(comp.Tracks);
        Assert.Single(comp.Vias);
        Assert.Single(comp.Arcs);
        Assert.Single(comp.Texts);
        Assert.Single(comp.Fills);
    }

    private static string GetTestDataPath()
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "TestData");
    }
}
