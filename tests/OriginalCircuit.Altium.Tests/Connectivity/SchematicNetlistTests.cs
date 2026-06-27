using OriginalCircuit.Altium.Connectivity;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Eda.Enums;
using Xunit;

namespace OriginalCircuit.Altium.Tests.Connectivity;

/// <summary>
/// Unit tests for the single-sheet schematic connectivity solver, built from tiny hand-authored
/// schematic documents that exercise each implicit-connectivity rule in isolation.
/// </summary>
public class SchematicNetlistTests
{
    // ---- builders -------------------------------------------------------------------------------

    private static SchComponent Comp(string designator, params (string pin, int x, int y)[] pins)
    {
        var c = new SchComponent { Name = designator, PartCount = 1 };
        c.AddParameter(new SchParameter { Name = "Designator", Value = designator });
        foreach (var (pin, x, y) in pins)
        {
            // Length 0 so the electrical tip == Location == the coordinate we give.
            c.AddPin(SchPin.Create(pin)
                .At(Coord.FromMils(x), Coord.FromMils(y))
                .Length(Coord.FromMils(0))
                .Orient(PinOrientation.Right)
                .Build());
        }
        return c;
    }

    private static SchWire Wire(params (int x, int y)[] pts)
    {
        var w = new SchWire();
        foreach (var (x, y) in pts)
            w.AddVertex(new CoordPoint(Coord.FromMils(x), Coord.FromMils(y)));
        return w;
    }

    private static SchNetLabel Label(string text, int x, int y) =>
        new() { Text = text, Location = new CoordPoint(Coord.FromMils(x), Coord.FromMils(y)) };

    private static SchPowerObject Power(string text, int x, int y) =>
        new() { Text = text, Location = new CoordPoint(Coord.FromMils(x), Coord.FromMils(y)) };

    private static SchJunction Junction(int x, int y) =>
        new() { Location = new CoordPoint(Coord.FromMils(x), Coord.FromMils(y)) };

    private static SchematicNetlist Solve(params object[] primitives)
    {
        var doc = new SchDocument();
        foreach (var p in primitives)
        {
            if (p is SchComponent c) doc.AddComponent(c);
            else doc.AddPrimitive(p);
        }
        return SchematicNetlistBuilder.Build(doc);
    }

    // ---- pin tip geometry -----------------------------------------------------------------------

    [Theory]
    [InlineData(PinOrientation.Right, 100, 0)]
    [InlineData(PinOrientation.Left, -100, 0)]
    [InlineData(PinOrientation.Up, 0, 100)]
    [InlineData(PinOrientation.Down, 0, -100)]
    public void PinTip_Is_Location_Plus_Length_Along_Orientation(PinOrientation orient, int tipXmils, int tipYmils)
    {
        var comp = new SchComponent { Name = "U1", PartCount = 1 };
        comp.AddParameter(new SchParameter { Name = "Designator", Value = "U1" });
        comp.AddPin(SchPin.Create("1").At(Coord.FromMils(0), Coord.FromMils(0))
            .Length(Coord.FromMils(100)).Orient(orient).Build());

        // A wire whose endpoint sits exactly at the expected tip.
        var wire = Wire((tipXmils, tipYmils), (tipXmils + 50, tipYmils + 50));
        var other = Comp("U2", ("1", tipXmils + 50, tipYmils + 50));

        var nl = Solve(comp, wire, other);
        var net = nl.NetForPin("U1", "1");
        Assert.NotNull(net);
        Assert.Equal(net, nl.NetForPin("U2", "1")); // connected through the wire at the computed tip
    }

    // ---- T-junction / crossover / manual junction -----------------------------------------------

    [Fact]
    public void TJunction_WireEndpoint_On_Interior_Connects()
    {
        // Horizontal wire; a vertical wire's endpoint lands on its interior (a T).
        var h = Wire((0, 0), (100, 0));
        var v = Wire((50, 0), (50, 50));
        var a = Comp("U1", ("1", 0, 0));
        var b = Comp("U2", ("1", 50, 50));

        var nl = Solve(h, v, a, b);
        Assert.Equal(nl.NetForPin("U1", "1"), nl.NetForPin("U2", "1"));
    }

    [Fact]
    public void Crossover_Without_Junction_Does_Not_Connect()
    {
        // Two wires cross at (50,0), interior to both, vertex of neither: NOT connected.
        var h = Wire((0, 0), (100, 0));
        var v = Wire((50, -50), (50, 50));
        var a = Comp("U1", ("1", 0, 0));
        var b = Comp("U2", ("1", 50, 50));

        var nl = Solve(h, v, a, b);
        Assert.NotNull(nl.NetForPin("U1", "1"));
        Assert.NotNull(nl.NetForPin("U2", "1"));
        Assert.NotEqual(nl.NetForPin("U1", "1"), nl.NetForPin("U2", "1"));
    }

    [Fact]
    public void Crossover_With_Junction_Connects()
    {
        var h = Wire((0, 0), (100, 0));
        var v = Wire((50, -50), (50, 50));
        var j = Junction(50, 0);
        var a = Comp("U1", ("1", 0, 0));
        var b = Comp("U2", ("1", 50, 50));

        var nl = Solve(h, v, j, a, b);
        Assert.Equal(nl.NetForPin("U1", "1"), nl.NetForPin("U2", "1"));
    }

    // ---- named identifiers ----------------------------------------------------------------------

    [Fact]
    public void SameName_NetLabels_Unify_Across_Disjoint_Wires()
    {
        var w1 = Wire((0, 0), (100, 0));
        var w2 = Wire((0, 200), (100, 200));
        var a = Comp("U1", ("1", 100, 0));
        var b = Comp("U2", ("1", 100, 200));

        var nl = Solve(w1, w2, a, b, Label("DATA", 0, 0), Label("DATA", 0, 200));

        var net = nl.NetForPin("U1", "1");
        Assert.NotNull(net);
        Assert.Equal(net, nl.NetForPin("U2", "1"));
        Assert.Equal("DATA", net!.Name);
        Assert.True(net.IsNamedExplicitly);
    }

    [Fact]
    public void Power_Objects_Unify_Globally_By_Name()
    {
        var w1 = Wire((0, 0), (100, 0));
        var w2 = Wire((0, 200), (100, 200));
        var a = Comp("U1", ("1", 100, 0));
        var b = Comp("U2", ("1", 100, 200));

        var nl = Solve(w1, w2, a, b, Power("GND", 0, 0), Power("GND", 0, 200));

        var net = nl.NetForPin("U1", "1");
        Assert.NotNull(net);
        Assert.Equal(net, nl.NetForPin("U2", "1"));
        Assert.Equal("GND", net!.Name);
        Assert.Equal(NetScope.Power, net.Scope);
    }

    [Fact]
    public void NetLabel_Beats_Power_For_Naming()
    {
        // A single net carrying both a net label and a power object: the label name wins.
        var w = Wire((0, 0), (100, 0));
        var a = Comp("U1", ("1", 0, 0));
        var b = Comp("U2", ("1", 100, 0));
        var nl = Solve(w, a, b, Label("MYNET", 0, 0), Power("GND", 100, 0));

        var net = nl.NetForPin("U1", "1");
        Assert.NotNull(net);
        Assert.Equal("MYNET", net!.Name);
    }

    // ---- hidden power pins ----------------------------------------------------------------------

    [Fact]
    public void Hidden_Pin_With_HiddenNetName_Joins_That_Net()
    {
        // U1.7 is a hidden VCC pin; a power object "VCC" is wired to U2.1. They should be the same net.
        var u1 = new SchComponent { Name = "U1", PartCount = 1 };
        u1.AddParameter(new SchParameter { Name = "Designator", Value = "U1" });
        u1.AddPin(SchPin.Create("7").At(Coord.FromMils(1000), Coord.FromMils(1000))
            .Length(Coord.FromMils(0)).Orient(PinOrientation.Right).Build());
        ((SchPin)u1.Pins[0]).IsHidden = true;
        ((SchPin)u1.Pins[0]).HiddenNetName = "VCC";

        var w = Wire((0, 0), (100, 0));
        var u2 = Comp("U2", ("1", 100, 0));
        var nl = Solve(u1, u2, w, Power("VCC", 0, 0));

        var net = nl.NetForPin("U2", "1");
        Assert.NotNull(net);
        Assert.Equal("VCC", net!.Name);
        Assert.Equal(net, nl.NetForPin("U1", "7"));
    }

    // ---- unconnected pins -----------------------------------------------------------------------

    [Fact]
    public void Floating_Pin_Is_Reported_Unconnected()
    {
        var a = Comp("U1", ("1", 0, 0));
        var b = Comp("U2", ("1", 100, 0));
        var w = Wire((0, 0), (50, 0)); // touches U1.1 only
        var nl = Solve(a, b, w);

        Assert.NotNull(nl.NetForPin("U1", "1"));
        Assert.Null(nl.NetForPin("U2", "1"));
        Assert.Contains(nl.UnconnectedPins, p => p.ComponentDesignator == "U2" && p.PinDesignator == "1");
    }

    // ---- auto naming ----------------------------------------------------------------------------

    [Fact]
    public void Unnamed_Net_Gets_Altium_Style_AutoName()
    {
        var w = Wire((0, 0), (100, 0));
        var a = Comp("R5", ("2", 0, 0));
        var b = Comp("U1", ("3", 100, 0));
        var nl = Solve(w, a, b);

        var net = nl.NetForPin("R5", "2");
        Assert.NotNull(net);
        // Lowest pin key by natural order is R5.2 < U1.3 (R < U).
        Assert.Equal("NetR5_2", net!.Name);
        Assert.False(net.IsNamedExplicitly);
        Assert.Equal(NetScope.Auto, net.Scope);
    }

    // ---- multi-part component -------------------------------------------------------------------

    [Fact]
    public void MultiPart_Component_Pins_Share_Designator_Connect_Independently()
    {
        // One package U1 with two units (different OwnerPartId); each pin nets independently.
        var u1 = new SchComponent { Name = "DUAL", PartCount = 2 };
        u1.AddParameter(new SchParameter { Name = "Designator", Value = "U1" });
        u1.AddPin(SchPin.Create("1").At(Coord.FromMils(0), Coord.FromMils(0))
            .Length(Coord.FromMils(0)).Orient(PinOrientation.Right).Build());
        u1.AddPin(SchPin.Create("8").At(Coord.FromMils(500), Coord.FromMils(0))
            .Length(Coord.FromMils(0)).Orient(PinOrientation.Right).Build());
        ((SchPin)u1.Pins[0]).OwnerPartId = 1;
        ((SchPin)u1.Pins[1]).OwnerPartId = 2;

        var w = Wire((0, 0), (200, 0));
        var u2 = Comp("U2", ("1", 200, 0));
        var nl = Solve(u1, u2, w);

        // Both pins resolve under designator U1.
        Assert.NotNull(nl.NetForPin("U1", "1"));
        Assert.Equal(nl.NetForPin("U1", "1"), nl.NetForPin("U2", "1"));
        // U1.8 (other unit) is on its own net, not joined to U1.1.
        Assert.NotEqual(nl.NetForPin("U1", "1"), nl.NetForPin("U1", "8"));
    }
}
