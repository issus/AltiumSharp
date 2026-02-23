using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Extensions;

namespace OriginalCircuit.Altium.Tests;

public class PcbBuilderTests
{
    [Fact]
    public void PadBuilder_CreatesValidPad()
    {
        var pad = PcbPad.Create("1")
            .At(0.Mils(), 0.Mils())
            .Size(60.Mils(), 60.Mils())
            .Shape(PadShape.Round)
            .ThroughHole(30.Mils())
            .Build();

        Assert.Equal("1", pad.Designator);
        Assert.Equal(60.0, pad.SizeTop.X.ToMils(), precision: 3);
        Assert.Equal(30.0, pad.HoleSize.ToMils(), precision: 3);
        Assert.Equal(PadShape.Round, pad.ShapeTop);
        Assert.True(pad.IsPlated);
    }

    [Fact]
    public void PadBuilder_SmdPad_HasNoHole()
    {
        var pad = PcbPad.Create("A1")
            .At(0.5.Mm(), 0.Mm())
            .Size(0.6.Mm(), 0.8.Mm())
            .Shape(PadShape.RoundedRectangle)
            .Smd()
            .Build();

        Assert.Equal("A1", pad.Designator);
        Assert.Equal(Coord.Zero, pad.HoleSize);
        Assert.Equal(1, pad.Layer); // Top layer
    }

    [Fact]
    public void TrackBuilder_CreatesValidTrack()
    {
        var track = PcbTrack.Create()
            .From(0.Mils(), 0.Mils())
            .To(100.Mils(), 100.Mils())
            .Width(10.Mils())
            .OnLayer(1)
            .Net("VCC")
            .Build();

        Assert.Equal(0.0, track.Start.X.ToMils(), precision: 3);
        Assert.Equal(100.0, track.End.X.ToMils(), precision: 3);
        Assert.Equal(10.0, track.Width.ToMils(), precision: 3);
        Assert.Equal("VCC", track.Net);
    }

    [Fact]
    public void ViaBuilder_ThroughHole_SpansAllLayers()
    {
        var via = PcbVia.Create()
            .At(50.Mils(), 50.Mils())
            .Diameter(20.Mils())
            .HoleSize(10.Mils())
            .ThroughHole()
            .Net("GND")
            .Build();

        Assert.Equal(1, via.StartLayer);
        Assert.Equal(32, via.EndLayer);
        Assert.Equal("GND", via.Net);
    }

    [Fact]
    public void ViaBuilder_BlindVia_HasCorrectLayers()
    {
        var via = PcbVia.Create()
            .At(0.Mm(), 0.Mm())
            .Diameter(0.5.Mm())
            .HoleSize(0.25.Mm())
            .Blind(1, 2)
            .Tented()
            .Build();

        Assert.Equal(1, via.StartLayer);
        Assert.Equal(2, via.EndLayer);
        Assert.True(via.IsTented);
    }

    [Fact]
    public void ArcBuilder_FullCircle_HasCorrectAngles()
    {
        var arc = PcbArc.Create()
            .Center(0.Mils(), 0.Mils())
            .Radius(50.Mils())
            .FullCircle()
            .Width(10.Mils())
            .Build();

        Assert.Equal(0, arc.StartAngle);
        Assert.Equal(360, arc.EndAngle);
        Assert.Equal(360, arc.SweepAngle);
    }

    [Fact]
    public void TextBuilder_CreatesValidText()
    {
        var text = PcbText.Create(".Designator")
            .At(0.Mm(), 2.Mm())
            .Height(1.Mm())
            .StrokeWidth(0.15.Mm())
            .Rotation(90)
            .Justify(TextJustification.MiddleCenter)
            .Build();

        Assert.Equal(".Designator", text.Text);
        Assert.Equal(1.0, text.Height.ToMm(), precision: 3);
        Assert.Equal(90, text.Rotation);
        Assert.Equal(TextJustification.MiddleCenter, text.Justification);
    }

    [Fact]
    public void ComponentBuilder_FluentApi_CreatesCompleteComponent()
    {
        var resistor = PcbComponent.Create("R0402")
            .WithDescription("0402 Resistor Footprint")
            .WithHeight(0.35.Mm())
            .AddPad(pad => pad
                .At(-0.5.Mm(), 0.Mm())
                .Size(0.5.Mm(), 0.6.Mm())
                .Shape(PadShape.RoundedRectangle)
                .Smd())
            .AddPad(pad => pad
                .At(0.5.Mm(), 0.Mm())
                .Size(0.5.Mm(), 0.6.Mm())
                .Shape(PadShape.RoundedRectangle)
                .Smd())
            .AddText(".Designator", text => text
                .At(0.Mm(), 0.8.Mm())
                .Height(0.6.Mm()))
            .Build();

        Assert.Equal("R0402", resistor.Name);
        Assert.Equal("0402 Resistor Footprint", resistor.Description);
        Assert.Equal(2, resistor.Pads.Count);
        Assert.Single(resistor.Texts);
    }

    [Fact]
    public void ComponentBuilder_ImplicitConversion_Works()
    {
        PcbComponent component = PcbComponent.Create("TEST")
            .WithDescription("Test Component");

        Assert.Equal("TEST", component.Name);
    }

    [Fact]
    public void PcbComponent_Bounds_CalculatedFromPrimitives()
    {
        var component = PcbComponent.Create("TEST")
            .AddPad(pad => pad
                .At(0.Mils(), 0.Mils())
                .Size(50.Mils(), 50.Mils()))
            .AddPad(pad => pad
                .At(100.Mils(), 0.Mils())
                .Size(50.Mils(), 50.Mils()))
            .Build();

        var bounds = component.Bounds;

        // Should span from -25 to 125 mils in X (accounting for pad sizes)
        Assert.True(bounds.Width.ToMils() > 100);
    }
}
