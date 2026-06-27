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

    // ---- buses ----------------------------------------------------------------------------------

    private static SchBus Bus(params (int x, int y)[] pts)
    {
        var b = new SchBus();
        foreach (var (x, y) in pts)
            b.AddVertex(new CoordPoint(Coord.FromMils(x), Coord.FromMils(y)));
        return b;
    }

    private static SchBusEntry BusEntry(int x1, int y1, int x2, int y2) =>
        new()
        {
            Location = new CoordPoint(Coord.FromMils(x1), Coord.FromMils(y1)),
            Corner = new CoordPoint(Coord.FromMils(x2), Coord.FromMils(y2)),
        };

    [Fact]
    public void Bus_Members_Form_By_Label_And_Do_Not_Short_Through_Bus()
    {
        // A vertical bus carrying D0/D1, each member appearing twice (joined by its label), with bus
        // entries touching the bus. The bus must NOT short D0 to D1.
        var prims = new List<object>
        {
            Bus((0, -100), (0, 700)),
            new SchNetLabel { Text = "D[0..7]", Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(300)) },

            // D0 instance 1
            Wire((100, 0), (300, 0)), Label("D0", 100, 0), BusEntry(100, 0, 0, 0), Comp("A", ("1", 300, 0)),
            // D0 instance 2
            Wire((100, 500), (300, 500)), Label("D0", 100, 500), BusEntry(100, 500, 0, 500), Comp("B", ("1", 300, 500)),
            // D1 instance 1
            Wire((100, 100), (300, 100)), Label("D1", 100, 100), BusEntry(100, 100, 0, 100), Comp("A", ("2", 300, 100)),
            // D1 instance 2
            Wire((100, 600), (300, 600)), Label("D1", 100, 600), BusEntry(100, 600, 0, 600), Comp("B", ("2", 300, 600)),
        };

        var nl = Solve(prims.ToArray());

        var d0 = nl.NetForPin("A", "1");
        var d1 = nl.NetForPin("A", "2");
        Assert.NotNull(d0);
        Assert.NotNull(d1);
        Assert.Equal("D0", d0!.Name);
        Assert.Equal("D1", d1!.Name);
        Assert.NotEqual(d0, d1); // not shorted through the bus
        Assert.Equal(d0, nl.NetForPin("B", "1")); // D0 joins its two instances by label
        Assert.Equal(d1, nl.NetForPin("B", "2"));
        Assert.DoesNotContain(nl.Nets, n => n.Name.Contains('[')); // ranged label is not a net name
    }

    // ---- harness bundles ------------------------------------------------------------------------

    private static (SchHarnessConnector Conn, SchSignalHarness Sh, SchPort Port, SchWire Wire) HarnessBreakout(
        int connX, int connTop, string bundle, string member, int memberPinX, int memberPinY)
    {
        // Connector anchored top-left; bundle point on the left edge; one member entry on the right edge.
        var conn = new SchHarnessConnector
        {
            Location = new CoordPoint(Coord.FromMils(connX), Coord.FromMils(connTop)),
            XSize = Coord.FromMils(500),
            YSize = Coord.FromMils(600),
            PrimaryConnectionPosition = Coord.FromMils(300),
        };
        conn.Entries.Add(new SchHarnessEntry { Text = member, Side = 1, DistanceFromTop = Coord.FromMils(100) });

        // Bundle point = (connX, connTop-300). Port left end wired to it via a signal harness.
        var sh = new SchSignalHarness();
        sh.Vertices.Add(new CoordPoint(Coord.FromMils(connX - 500), Coord.FromMils(connTop - 300)));
        sh.Vertices.Add(new CoordPoint(Coord.FromMils(connX), Coord.FromMils(connTop - 300)));

        var port = new SchPort
        {
            Name = bundle,
            HarnessType = bundle,
            Location = new CoordPoint(Coord.FromMils(connX - 500), Coord.FromMils(connTop - 300)),
            Width = Coord.FromMils(200),
            ConnectedEnd = 1, // left end = Location coincides with the signal harness end
        };

        // Member entry at (connX+500, connTop-100) -> wire -> member pin.
        var wire = Wire((connX + 500, connTop - 100), (memberPinX, memberPinY));
        return (conn, sh, port, wire);
    }

    [Fact]
    public void Harness_Members_Reconnect_By_Qualified_Bundle_Name()
    {
        // Two connectors breaking out the same bundle "BUS" member "SIG": the members reconnect.
        var (c1, sh1, p1, w1) = HarnessBreakout(2000, 3000, "BUS", "SIG", 2700, 2900);
        var a = Comp("A", ("1", 2700, 2900));
        var (c2, sh2, p2, w2) = HarnessBreakout(6000, 3000, "BUS", "SIG", 6700, 2900);
        var b = Comp("B", ("1", 6700, 2900));

        var nl = Solve(c1, sh1, p1, w1, a, c2, sh2, p2, w2, b);

        var na = nl.NetForPin("A", "1");
        var nb = nl.NetForPin("B", "1");
        Assert.NotNull(na);
        Assert.NotNull(nb);
        Assert.Equal(na, nb); // BUS.SIG reconnected across the two breakouts
    }

    // ---- net intents (directives) ---------------------------------------------------------------

    [Fact]
    public void ParameterSet_Binds_NetClass_And_Impedance_To_Coincident_Net()
    {
        var w = Wire((0, 0), (100, 0));
        var a = Comp("U1", ("1", 0, 0));
        var b = Comp("U2", ("1", 100, 0));

        var ps = new SchParameterSet { Location = new CoordPoint(Coord.FromMils(50), Coord.FromMils(0)) };
        ps.AddParameter(new SchParameter { Name = "ClassName", Value = "RGMII" });
        ps.AddParameter(new SchParameter { Name = "Impedance", Value = "50ohm" });

        var nl = Solve(w, a, b, ps);
        var net = nl.NetForPin("U1", "1");
        Assert.NotNull(net);

        var cls = net!.Intents.FirstOrDefault(i => i.Kind == NetIntentKind.NetClass);
        Assert.NotNull(cls);
        Assert.Equal("RGMII", cls!.NetClass);

        var imp = net.Intents.FirstOrDefault(i => i.Kind == NetIntentKind.Impedance);
        Assert.NotNull(imp);
        Assert.Equal(50.0, imp!.Ohms);
    }

    [Fact]
    public void Blanket_Binds_Directive_To_All_Enclosed_Nets()
    {
        // Two separate nets both inside the blanket polygon both receive the directive.
        var w1 = Wire((100, 100), (300, 100));
        var a = Comp("U1", ("1", 100, 100));
        var a2 = Comp("U1b", ("1", 300, 100));
        var w2 = Wire((100, 300), (300, 300));
        var b = Comp("U2", ("1", 100, 300));
        var b2 = Comp("U2b", ("1", 300, 300));

        var blanket = new SchBlanket();
        foreach (var (x, y) in new[] { (0, 0), (400, 0), (400, 400), (0, 400) })
            blanket.AddVertex(new CoordPoint(Coord.FromMils(x), Coord.FromMils(y)));
        blanket.AddParameter(new SchParameter { Name = "ClassName", Value = "DDR" });

        var nl = Solve(w1, a, a2, w2, b, b2, blanket);

        var n1 = nl.NetForPin("U1", "1");
        var n2 = nl.NetForPin("U2", "1");
        Assert.NotNull(n1);
        Assert.NotNull(n2);
        Assert.Contains(n1!.Intents, i => i.Kind == NetIntentKind.NetClass && i.NetClass == "DDR");
        Assert.Contains(n2!.Intents, i => i.Kind == NetIntentKind.NetClass && i.NetClass == "DDR");
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
