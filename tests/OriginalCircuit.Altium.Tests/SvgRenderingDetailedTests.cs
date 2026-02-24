using System.Xml.Linq;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Rendering;
using OriginalCircuit.Altium.Rendering.Svg;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Eda.Rendering;

namespace OriginalCircuit.Altium.Tests;

/// <summary>
/// Detailed SVG rendering tests that verify exact element counts and attributes
/// for every primitive type. These tests ensure that the SVG output contains
/// the correct number of elements and that no shapes are silently dropped.
/// </summary>
public sealed class SvgRenderingDetailedTests
{
    private static readonly XNamespace Ns = "http://www.w3.org/2000/svg";

    // ── Helpers ──────────────────────────────────────────────────────

    private static async Task<XDocument> RenderSchToSvg(SchComponent component, int size = 256)
    {
        var renderer = new SvgRenderer();
        using var ms = new MemoryStream();
        await renderer.RenderAsync(component, ms, new RenderOptions { Width = size, Height = size });
        ms.Position = 0;
        return XDocument.Load(ms);
    }

    private static async Task<XDocument> RenderPcbToSvg(PcbComponent component, int size = 256)
    {
        var renderer = new SvgRenderer();
        using var ms = new MemoryStream();
        await renderer.RenderAsync(component, ms, new RenderOptions { Width = size, Height = size });
        ms.Position = 0;
        return XDocument.Load(ms);
    }

    /// <summary>
    /// Count elements excluding the background rect (first child of root from Clear()).
    /// </summary>
    private static int CountElements(XDocument doc, string localName)
    {
        return doc.Descendants(Ns + localName).Count();
    }

    // ── SchLine ──────────────────────────────────────────────────────

    [Fact]
    public async Task SchLine_ProducesExactlyOneLine()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddLine(new SchLine
        {
            Start = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(100), Coord.FromMils(0)),
            Color = 0x000000FF,
            Width = Coord.FromMils(10)
        });

        var doc = await RenderSchToSvg(c);

        // Exactly 1 line element (the schematic line)
        Assert.Equal(1, CountElements(doc, "line"));
    }

    [Fact]
    public async Task SchLine_HasCorrectAttributes()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddLine(new SchLine
        {
            Start = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(100), Coord.FromMils(0)),
            Color = 0x000000FF,
            Width = Coord.FromMils(10)
        });

        var doc = await RenderSchToSvg(c);
        var line = doc.Descendants(Ns + "line").First();

        Assert.NotNull(line.Attribute("x1"));
        Assert.NotNull(line.Attribute("y1"));
        Assert.NotNull(line.Attribute("x2"));
        Assert.NotNull(line.Attribute("y2"));
        Assert.NotNull(line.Attribute("stroke"));
        Assert.NotNull(line.Attribute("stroke-width"));
        Assert.NotEqual(line.Attribute("x1")!.Value, line.Attribute("x2")!.Value);
    }

    [Fact]
    public async Task SchLine_TwoLines_ProducesExactlyTwoLines()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddLine(new SchLine
        {
            Start = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(100), Coord.FromMils(0)),
            Color = 0x000000FF,
            Width = Coord.FromMils(10)
        });
        c.AddLine(new SchLine
        {
            Start = new CoordPoint(Coord.FromMils(0), Coord.FromMils(-100)),
            End = new CoordPoint(Coord.FromMils(0), Coord.FromMils(100)),
            Color = 0x000000FF,
            Width = Coord.FromMils(10)
        });

        var doc = await RenderSchToSvg(c);
        Assert.Equal(2, CountElements(doc, "line"));
    }

    // ── SchRectangle ─────────────────────────────────────────────────

    [Fact]
    public async Task SchRectangle_Unfilled_ProducesExactlyTwoRects()
    {
        // 1 background rect from Clear() + 1 border rect
        var c = new SchComponent { Name = "Test" };
        c.AddRectangle(new SchRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-100)),
            Corner2 = new CoordPoint(Coord.FromMils(50), Coord.FromMils(100)),
            Color = 0x00FF0000,
            IsFilled = false
        });

        var doc = await RenderSchToSvg(c);
        var rects = doc.Descendants(Ns + "rect").ToList();

        // 1 background + 1 border = 2 rects
        Assert.Equal(2, rects.Count);
    }

    [Fact]
    public async Task SchRectangle_Filled_ProducesExactlyThreeRects()
    {
        // 1 background rect + 1 fill rect + 1 border rect = 3
        var c = new SchComponent { Name = "Test" };
        c.AddRectangle(new SchRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-100)),
            Corner2 = new CoordPoint(Coord.FromMils(50), Coord.FromMils(100)),
            Color = 0x00FF0000,
            FillColor = 0x0000FFFF,
            IsFilled = true
        });

        var doc = await RenderSchToSvg(c);
        var rects = doc.Descendants(Ns + "rect").ToList();

        // 1 background + 1 fill + 1 border = 3 rects
        Assert.Equal(3, rects.Count);

        // At least one rect should have fill="none" (the border rect)
        Assert.Contains(rects, r => r.Attribute("fill")?.Value == "none");
        // At least one rect (non-background) should have a non-none fill
        Assert.True(rects.Count(r => r.Attribute("fill")?.Value != "none") >= 2,
            "Expected at least 2 filled rects (background + shape fill)");
    }

    [Fact]
    public async Task SchRectangle_FilledTransparent_ProducesExactlyTwoRects()
    {
        // IsFilled=true but IsTransparent=true should skip the fill rect
        var c = new SchComponent { Name = "Test" };
        c.AddRectangle(new SchRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-100)),
            Corner2 = new CoordPoint(Coord.FromMils(50), Coord.FromMils(100)),
            Color = 0x00FF0000,
            FillColor = 0x0000FFFF,
            IsFilled = true,
            IsTransparent = true
        });

        var doc = await RenderSchToSvg(c);
        var rects = doc.Descendants(Ns + "rect").ToList();

        // 1 background + 1 border only (fill skipped because transparent) = 2
        Assert.Equal(2, rects.Count);
    }

    [Fact]
    public async Task SchRectangle_TwoRectangles_ProducesCorrectCount()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddRectangle(new SchRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-100)),
            Corner2 = new CoordPoint(Coord.FromMils(50), Coord.FromMils(100)),
            Color = 0x00FF0000,
            IsFilled = false
        });
        c.AddRectangle(new SchRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-25), Coord.FromMils(-50)),
            Corner2 = new CoordPoint(Coord.FromMils(25), Coord.FromMils(50)),
            Color = 0x0000FF00,
            IsFilled = false
        });

        var doc = await RenderSchToSvg(c);
        // 1 background + 2 border rects = 3
        Assert.Equal(3, CountElements(doc, "rect"));
    }

    // ── SchEllipse ───────────────────────────────────────────────────

    [Fact]
    public async Task SchEllipse_Unfilled_ProducesExactlyOneEllipse()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddEllipse(new SchEllipse
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            RadiusX = Coord.FromMils(50),
            RadiusY = Coord.FromMils(30),
            Color = 0x0000FF00,
            IsFilled = false
        });

        var doc = await RenderSchToSvg(c);
        Assert.Equal(1, CountElements(doc, "ellipse"));
    }

    [Fact]
    public async Task SchEllipse_Filled_ProducesExactlyTwoEllipses()
    {
        // 1 filled ellipse + 1 border ellipse
        var c = new SchComponent { Name = "Test" };
        c.AddEllipse(new SchEllipse
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            RadiusX = Coord.FromMils(50),
            RadiusY = Coord.FromMils(30),
            Color = 0x0000FF00,
            FillColor = 0x00FFFF00,
            IsFilled = true
        });

        var doc = await RenderSchToSvg(c);
        var ellipses = doc.Descendants(Ns + "ellipse").ToList();
        Assert.Equal(2, ellipses.Count);

        // One should have fill="none" (border), one should have a color fill
        Assert.Contains(ellipses, e => e.Attribute("fill")?.Value == "none");
        Assert.Contains(ellipses, e => e.Attribute("fill")?.Value != "none");
    }

    [Fact]
    public async Task SchEllipse_HasCorrectAttributes()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddEllipse(new SchEllipse
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            RadiusX = Coord.FromMils(50),
            RadiusY = Coord.FromMils(30),
            Color = 0x0000FF00
        });

        var doc = await RenderSchToSvg(c);
        var el = doc.Descendants(Ns + "ellipse").First();

        Assert.NotNull(el.Attribute("cx"));
        Assert.NotNull(el.Attribute("cy"));
        Assert.NotNull(el.Attribute("rx"));
        Assert.NotNull(el.Attribute("ry"));
        Assert.NotNull(el.Attribute("stroke"));
        Assert.NotEqual("0", el.Attribute("rx")!.Value);
        Assert.NotEqual("0", el.Attribute("ry")!.Value);
    }

    // ── SchArc ───────────────────────────────────────────────────────

    [Fact]
    public async Task SchArc_FullCircle_ProducesEllipseNotPath()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddArc(new SchArc
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Radius = Coord.FromMils(50),
            StartAngle = 0,
            EndAngle = 360,
            Color = 0x000000FF
        });

        var doc = await RenderSchToSvg(c);
        Assert.Equal(1, CountElements(doc, "ellipse"));
        Assert.Equal(0, CountElements(doc, "path"));
    }

    [Fact]
    public async Task SchArc_Partial_ProducesPathNotEllipse()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddArc(new SchArc
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Radius = Coord.FromMils(50),
            StartAngle = 0,
            EndAngle = 90,
            Color = 0x000000FF
        });

        var doc = await RenderSchToSvg(c);

        // Should produce a path with arc command, not an ellipse
        var paths = doc.Descendants(Ns + "path").ToList();
        Assert.Equal(1, paths.Count);
        Assert.Contains("A", paths[0].Attribute("d")!.Value);

        // No ellipse (only background-unrelated ellipses)
        Assert.Equal(0, CountElements(doc, "ellipse"));
    }

    [Fact]
    public async Task SchArc_Semicircle_ProducesExactlyOnePath()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddArc(new SchArc
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Radius = Coord.FromMils(50),
            StartAngle = 0,
            EndAngle = 180,
            Color = 0x000000FF
        });

        var doc = await RenderSchToSvg(c);
        Assert.Equal(1, CountElements(doc, "path"));
    }

    // ── SchPin ───────────────────────────────────────────────────────

    [Fact]
    public async Task SchPin_WithNameAndDesignator_ProducesLineAndTwoTexts()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddPin(new SchPin
        {
            Location = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(0)),
            Length = Coord.FromMils(50),
            Orientation = PinOrientation.Right,
            Designator = "1",
            Name = "INPUT",
            ShowName = true,
            ShowDesignator = true
        });

        var doc = await RenderSchToSvg(c);

        // 1 line for pin body
        Assert.Equal(1, CountElements(doc, "line"));

        // 2 text elements: name + designator
        var texts = doc.Descendants(Ns + "text").ToList();
        Assert.Equal(2, texts.Count);

        var allText = string.Join(" ", texts.Select(t => t.Value));
        Assert.Contains("INPUT", allText);
        Assert.Contains("1", allText);
    }

    [Fact]
    public async Task SchPin_HiddenNameOnly_ProducesLineAndOneText()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddPin(new SchPin
        {
            Location = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(0)),
            Length = Coord.FromMils(50),
            Orientation = PinOrientation.Right,
            Designator = "1",
            Name = "INPUT",
            ShowName = false,   // hidden
            ShowDesignator = true
        });

        var doc = await RenderSchToSvg(c);

        Assert.Equal(1, CountElements(doc, "line"));
        // Only designator text, name hidden
        Assert.Equal(1, CountElements(doc, "text"));
    }

    [Fact]
    public async Task SchPin_Hidden_ProducesNoElements()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddPin(new SchPin
        {
            Location = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(0)),
            Length = Coord.FromMils(50),
            Orientation = PinOrientation.Right,
            Designator = "1",
            Name = "INPUT",
            IsHidden = true
        });

        var doc = await RenderSchToSvg(c);

        // Hidden pin should produce nothing
        Assert.Equal(0, CountElements(doc, "line"));
        Assert.Equal(0, CountElements(doc, "text"));
    }

    [Fact]
    public async Task SchPin_InputElectrical_ProducesArrowPolygons()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddPin(new SchPin
        {
            Location = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(0)),
            Length = Coord.FromMils(50),
            Orientation = PinOrientation.Right,
            Designator = "1",
            Name = "A",
            ShowName = true,
            ShowDesignator = true,
            ElectricalType = PinElectricalType.Input
        });

        var doc = await RenderSchToSvg(c);

        // Input pin should have arrow polygons (white fill + outline)
        var polygons = doc.Descendants(Ns + "polygon").ToList();
        Assert.Equal(2, polygons.Count); // 1 FillPolygon + 1 DrawPolygon
    }

    [Fact]
    public async Task SchPin_TwoPins_ProducesTwoLinesAndFourTexts()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddPin(new SchPin
        {
            Location = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(50)),
            Length = Coord.FromMils(50),
            Orientation = PinOrientation.Right,
            Designator = "1",
            Name = "A",
            ShowName = true,
            ShowDesignator = true
        });
        c.AddPin(new SchPin
        {
            Location = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(-50)),
            Length = Coord.FromMils(50),
            Orientation = PinOrientation.Right,
            Designator = "2",
            Name = "B",
            ShowName = true,
            ShowDesignator = true
        });

        var doc = await RenderSchToSvg(c);

        Assert.Equal(2, CountElements(doc, "line"));
        Assert.Equal(4, CountElements(doc, "text"));
    }

    // ── SchLabel ─────────────────────────────────────────────────────

    [Fact]
    public async Task SchLabel_ProducesExactlyOneText()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddLabel(new SchLabel
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Text = "Hello World",
            Color = 0x00000000
        });

        var doc = await RenderSchToSvg(c);
        var texts = doc.Descendants(Ns + "text").ToList();

        Assert.Equal(1, texts.Count);
        Assert.Equal("Hello World", texts[0].Value);
    }

    [Fact]
    public async Task SchLabel_Hidden_ProducesNoText()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddLabel(new SchLabel
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Text = "Hidden Label",
            Color = 0x00000000,
            IsHidden = true
        });

        var doc = await RenderSchToSvg(c);
        Assert.Equal(0, CountElements(doc, "text"));
    }

    [Fact]
    public async Task SchLabel_EmptyText_ProducesNoText()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddLabel(new SchLabel
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Text = "",
            Color = 0x00000000
        });

        var doc = await RenderSchToSvg(c);
        Assert.Equal(0, CountElements(doc, "text"));
    }

    [Fact]
    public async Task SchLabel_WithRotation_ProducesTextInGroup()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddLabel(new SchLabel
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Text = "Rotated",
            Color = 0x00000000,
            Rotation = 90
        });

        var doc = await RenderSchToSvg(c);

        Assert.Equal(1, CountElements(doc, "text"));

        // When rotated, text should be inside a <g> with transform
        var groups = doc.Descendants(Ns + "g").Where(g => g.Attribute("transform") != null).ToList();
        Assert.True(groups.Count >= 1, "Rotated label should be in a transformed group");
    }

    // ── SchWire ──────────────────────────────────────────────────────

    [Fact]
    public async Task SchWire_ProducesExactlyOnePolyline()
    {
        var c = new SchComponent { Name = "Test" };
        var wire = SchWire.Create()
            .From(Coord.FromMils(-100), Coord.FromMils(0))
            .To(Coord.FromMils(0), Coord.FromMils(100))
            .To(Coord.FromMils(100), Coord.FromMils(0))
            .Build();
        c.AddWire(wire);

        var doc = await RenderSchToSvg(c);

        Assert.Equal(1, CountElements(doc, "polyline"));

        var polyline = doc.Descendants(Ns + "polyline").First();
        Assert.NotNull(polyline.Attribute("points"));
        Assert.NotNull(polyline.Attribute("stroke"));
        Assert.Equal("none", polyline.Attribute("fill")?.Value);
    }

    [Fact]
    public async Task SchWire_TwoWires_ProducesTwoPolylines()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddWire(SchWire.Create()
            .From(Coord.FromMils(-100), Coord.FromMils(0))
            .To(Coord.FromMils(100), Coord.FromMils(0))
            .Build());
        c.AddWire(SchWire.Create()
            .From(Coord.FromMils(0), Coord.FromMils(-100))
            .To(Coord.FromMils(0), Coord.FromMils(100))
            .Build());

        var doc = await RenderSchToSvg(c);
        Assert.Equal(2, CountElements(doc, "polyline"));
    }

    // ── SchPolyline ──────────────────────────────────────────────────

    [Fact]
    public async Task SchPolyline_ProducesExactlyOnePolyline()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddPolyline(SchPolyline.Create()
            .From(Coord.FromMils(-100), Coord.FromMils(-50))
            .To(Coord.FromMils(0), Coord.FromMils(50))
            .To(Coord.FromMils(100), Coord.FromMils(-50))
            .Build());

        var doc = await RenderSchToSvg(c);

        Assert.Equal(1, CountElements(doc, "polyline"));
        var pl = doc.Descendants(Ns + "polyline").First();
        Assert.NotNull(pl.Attribute("points"));
        // Points string should contain 3 coordinate pairs
        var points = pl.Attribute("points")!.Value;
        Assert.Equal(3, points.Split(' ').Length);
    }

    // ── SchPolygon ───────────────────────────────────────────────────

    [Fact]
    public async Task SchPolygon_Unfilled_ProducesExactlyOnePolygon()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddPolygon(SchPolygon.Create()
            .AddVertex(Coord.FromMils(-50), Coord.FromMils(-50))
            .AddVertex(Coord.FromMils(50), Coord.FromMils(-50))
            .AddVertex(Coord.FromMils(0), Coord.FromMils(50))
            .Build());

        var doc = await RenderSchToSvg(c);

        // Unfilled polygon: 1 DrawPolygon (border only)
        var polygons = doc.Descendants(Ns + "polygon").ToList();
        Assert.Equal(1, polygons.Count);
        Assert.Equal("none", polygons[0].Attribute("fill")?.Value);
    }

    [Fact]
    public async Task SchPolygon_Filled_ProducesExactlyTwoPolygons()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddPolygon(SchPolygon.Create()
            .AddVertex(Coord.FromMils(-50), Coord.FromMils(-50))
            .AddVertex(Coord.FromMils(50), Coord.FromMils(-50))
            .AddVertex(Coord.FromMils(0), Coord.FromMils(50))
            .FillColor(0x0000FFFF)
            .Filled()
            .Build());

        var doc = await RenderSchToSvg(c);

        // Filled polygon: 1 FillPolygon + 1 DrawPolygon = 2
        var polygons = doc.Descendants(Ns + "polygon").ToList();
        Assert.Equal(2, polygons.Count);

        Assert.Contains(polygons, p => p.Attribute("fill")?.Value == "none");
        Assert.Contains(polygons, p => p.Attribute("fill")?.Value != "none");
    }

    // ── SchBezier ────────────────────────────────────────────────────

    [Fact]
    public async Task SchBezier_SingleSegment_ProducesExactlyOnePath()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddBezier(SchBezier.Create()
            .AddPoint(Coord.FromMils(0), Coord.FromMils(0))
            .AddPoint(Coord.FromMils(50), Coord.FromMils(100))
            .AddPoint(Coord.FromMils(100), Coord.FromMils(100))
            .AddPoint(Coord.FromMils(150), Coord.FromMils(0))
            .Build());

        var doc = await RenderSchToSvg(c);

        var paths = doc.Descendants(Ns + "path").ToList();
        Assert.Equal(1, paths.Count);
        Assert.Contains("C", paths[0].Attribute("d")!.Value); // Cubic bezier command
    }

    [Fact]
    public async Task SchBezier_TooFewPoints_ProducesNoPath()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddBezier(SchBezier.Create()
            .AddPoint(Coord.FromMils(0), Coord.FromMils(0))
            .AddPoint(Coord.FromMils(50), Coord.FromMils(100))
            .Build());

        var doc = await RenderSchToSvg(c);
        Assert.Equal(0, CountElements(doc, "path"));
    }

    // ── SchRoundedRectangle ──────────────────────────────────────────

    [Fact]
    public async Task SchRoundedRectangle_Unfilled_ProducesRectWithRxRy()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddRoundedRectangle(new SchRoundedRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-100)),
            Corner2 = new CoordPoint(Coord.FromMils(50), Coord.FromMils(100)),
            CornerRadiusX = Coord.FromMils(10),
            CornerRadiusY = Coord.FromMils(10),
            Color = 0x00FF0000,
            IsFilled = false
        });

        var doc = await RenderSchToSvg(c);

        // 1 background + 1 rounded border = 2 rects
        var rects = doc.Descendants(Ns + "rect").ToList();
        Assert.Equal(2, rects.Count);

        // The non-background rect should have rx and ry
        var borderRect = rects.First(r => r.Attribute("fill")?.Value == "none");
        Assert.NotNull(borderRect.Attribute("rx"));
        Assert.NotNull(borderRect.Attribute("ry"));
    }

    [Fact]
    public async Task SchRoundedRectangle_Filled_ProducesThreeRects()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddRoundedRectangle(new SchRoundedRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-100)),
            Corner2 = new CoordPoint(Coord.FromMils(50), Coord.FromMils(100)),
            CornerRadiusX = Coord.FromMils(10),
            CornerRadiusY = Coord.FromMils(10),
            Color = 0x00FF0000,
            FillColor = 0x0000FFFF,
            IsFilled = true
        });

        var doc = await RenderSchToSvg(c);

        // 1 background + 1 fill (rounded) + 1 border (rounded) = 3
        Assert.Equal(3, CountElements(doc, "rect"));
    }

    // ── SchPie ───────────────────────────────────────────────────────

    [Fact]
    public async Task SchPie_Unfilled_ProducesExactlyOnePath()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddPie(new SchPie
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Radius = Coord.FromMils(50),
            StartAngle = 0,
            EndAngle = 90,
            Color = 0x000000FF,
            IsFilled = false
        });

        var doc = await RenderSchToSvg(c);

        // DrawPie produces 1 path
        var paths = doc.Descendants(Ns + "path").ToList();
        Assert.Equal(1, paths.Count);
        // Pie path should contain L (line to), A (arc), and Z (close)
        var d = paths[0].Attribute("d")!.Value;
        Assert.Contains("L", d);
        Assert.Contains("A", d);
        Assert.Contains("Z", d);
    }

    [Fact]
    public async Task SchPie_Filled_ProducesExactlyTwoPaths()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddPie(new SchPie
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Radius = Coord.FromMils(50),
            StartAngle = 0,
            EndAngle = 90,
            Color = 0x000000FF,
            FillColor = 0x00FF0000,
            IsFilled = true
        });

        var doc = await RenderSchToSvg(c);

        // FillPie + DrawPie = 2 paths
        Assert.Equal(2, CountElements(doc, "path"));
    }

    // ── SchEllipticalArc ─────────────────────────────────────────────

    [Fact]
    public async Task SchEllipticalArc_FullCircle_ProducesEllipse()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddEllipticalArc(new SchEllipticalArc
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            PrimaryRadius = Coord.FromMils(80),
            SecondaryRadius = Coord.FromMils(40),
            StartAngle = 0,
            EndAngle = 360,
            Color = 0x000000FF
        });

        var doc = await RenderSchToSvg(c);
        Assert.Equal(1, CountElements(doc, "ellipse"));
        Assert.Equal(0, CountElements(doc, "path"));
    }

    [Fact]
    public async Task SchEllipticalArc_Partial_ProducesPath()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddEllipticalArc(new SchEllipticalArc
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            PrimaryRadius = Coord.FromMils(80),
            SecondaryRadius = Coord.FromMils(40),
            StartAngle = 0,
            EndAngle = 180,
            Color = 0x000000FF
        });

        var doc = await RenderSchToSvg(c);
        Assert.Equal(0, CountElements(doc, "ellipse"));
        Assert.Equal(1, CountElements(doc, "path"));
    }

    // ── SchJunction ──────────────────────────────────────────────────

    [Fact]
    public async Task SchJunction_ProducesExactlyOneFilledEllipse()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddJunction(new SchJunction
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Size = Coord.FromMils(10),
            Color = 0x00008000
        });

        var doc = await RenderSchToSvg(c);

        var ellipses = doc.Descendants(Ns + "ellipse").ToList();
        Assert.Equal(1, ellipses.Count);
        // Junction is filled (not stroke-only)
        Assert.NotEqual("none", ellipses[0].Attribute("fill")?.Value);
    }

    // ── SchNetLabel ──────────────────────────────────────────────────

    [Fact]
    public async Task SchNetLabel_ProducesExactlyOneText()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddNetLabel(new SchNetLabel
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Text = "VCC",
            Color = 0x000000FF
        });

        var doc = await RenderSchToSvg(c);

        var texts = doc.Descendants(Ns + "text").ToList();
        Assert.Equal(1, texts.Count);
        Assert.Equal("VCC", texts[0].Value);
    }

    [Fact]
    public async Task SchNetLabel_EmptyText_ProducesNoText()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddNetLabel(new SchNetLabel
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Text = "",
            Color = 0x000000FF
        });

        var doc = await RenderSchToSvg(c);
        Assert.Equal(0, CountElements(doc, "text"));
    }

    // ── SchParameter ─────────────────────────────────────────────────

    [Fact]
    public async Task SchParameter_Visible_ProducesExactlyOneText()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddParameter(new SchParameter
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Name = "Value",
            Value = "10k",
            IsVisible = true,
            Color = 0x00000000
        });

        var doc = await RenderSchToSvg(c);

        var texts = doc.Descendants(Ns + "text").ToList();
        Assert.Equal(1, texts.Count);
        Assert.Contains("10k", texts[0].Value);
    }

    [Fact]
    public async Task SchParameter_Hidden_ProducesNoText()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddParameter(new SchParameter
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Name = "Value",
            Value = "10k",
            IsVisible = false,
            Color = 0x00000000
        });

        var doc = await RenderSchToSvg(c);
        Assert.Equal(0, CountElements(doc, "text"));
    }

    // ── SchTextFrame ─────────────────────────────────────────────────

    [Fact]
    public async Task SchTextFrame_WithBorderAndFill_ProducesRectsAndText()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddTextFrame(new SchTextFrame
        {
            Corner1 = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(-50)),
            Corner2 = new CoordPoint(Coord.FromMils(100), Coord.FromMils(50)),
            Text = "Frame Text",
            ShowBorder = true,
            IsFilled = true,
            FillColor = 0x00FFFFFF,
            BorderColor = 0x00000000,
            TextColor = 0x00000000
        });

        var doc = await RenderSchToSvg(c);

        // 1 background + 1 fill + 1 border = 3 rects
        Assert.Equal(3, CountElements(doc, "rect"));

        // 1 text for the content
        Assert.Equal(1, CountElements(doc, "text"));
        var text = doc.Descendants(Ns + "text").First();
        Assert.Equal("Frame Text", text.Value);
    }

    [Fact]
    public async Task SchTextFrame_NoBorderNoFill_ProducesOnlyBackgroundAndText()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddTextFrame(new SchTextFrame
        {
            Corner1 = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(-50)),
            Corner2 = new CoordPoint(Coord.FromMils(100), Coord.FromMils(50)),
            Text = "Simple Text",
            ShowBorder = false,
            IsFilled = false,
            TextColor = 0x00000000
        });

        var doc = await RenderSchToSvg(c);

        // 1 background rect only
        Assert.Equal(1, CountElements(doc, "rect"));
        Assert.Equal(1, CountElements(doc, "text"));
    }

    // ── SchImage ─────────────────────────────────────────────────────

    [Fact]
    public async Task SchImage_NoData_ProducesPlaceholderCross()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddImage(new SchImage
        {
            Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-50)),
            Corner2 = new CoordPoint(Coord.FromMils(50), Coord.FromMils(50)),
            ImageData = null
        });

        var doc = await RenderSchToSvg(c);

        // Placeholder: 1 DrawRectangle (frame) + 2 DrawLine (X cross)
        // Plus 1 background rect = 2 rects total
        Assert.Equal(2, CountElements(doc, "rect"));
        Assert.Equal(2, CountElements(doc, "line"));
    }

    [Fact]
    public async Task SchImage_WithData_ProducesImageElement()
    {
        var c = new SchComponent { Name = "Test" };
        // Minimal 1x1 PNG
        var pngData = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG sig
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
            0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41,
            0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
            0x00, 0x00, 0x02, 0x00, 0x01, 0xE2, 0x21, 0xBC,
            0x33, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E,
            0x44, 0xAE, 0x42, 0x60, 0x82
        };
        c.AddImage(new SchImage
        {
            Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-50)),
            Corner2 = new CoordPoint(Coord.FromMils(50), Coord.FromMils(50)),
            ImageData = pngData
        });

        var doc = await RenderSchToSvg(c);

        var images = doc.Descendants(Ns + "image").ToList();
        Assert.Equal(1, images.Count);
        Assert.Contains("data:image/png;base64,", images[0].Attribute("href")!.Value);
    }

    // ── SchPowerObject ───────────────────────────────────────────────

    [Fact]
    public async Task SchPowerObject_Bar_ProducesPinAndSymbolLines()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddPowerObject(new SchPowerObject
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Text = "VCC",
            Style = PowerPortStyle.Bar,
            ShowNetName = true,
            Color = 0x00000000
        });

        var doc = await RenderSchToSvg(c);

        // Pin line + bar line = 2 lines
        Assert.Equal(2, CountElements(doc, "line"));
        // Net name text
        Assert.Equal(1, CountElements(doc, "text"));
    }

    [Fact]
    public async Task SchPowerObject_PowerGround_ProducesThreeBarLines()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddPowerObject(new SchPowerObject
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Text = "GND",
            Style = PowerPortStyle.PowerGround,
            ShowNetName = false,
            Color = 0x00000000
        });

        var doc = await RenderSchToSvg(c);

        // Pin line (1) + 3 ground bars = 4 lines
        Assert.Equal(4, CountElements(doc, "line"));
    }

    [Fact]
    public async Task SchPowerObject_Circle_ProducesEllipseAndLine()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddPowerObject(new SchPowerObject
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Text = "VDD",
            Style = PowerPortStyle.Circle,
            ShowNetName = false,
            Color = 0x00000000
        });

        var doc = await RenderSchToSvg(c);

        // 1 pin line
        Assert.Equal(1, CountElements(doc, "line"));
        // 1 circle (DrawEllipse for circle symbol)
        Assert.Equal(1, CountElements(doc, "ellipse"));
    }

    // ── PCB Primitives ───────────────────────────────────────────────

    [Fact]
    public async Task PcbTrack_ProducesExactlyOneLine()
    {
        var component = PcbComponent.Create("TestFP")
            .AddTrack(t => t
                .From(Coord.FromMils(-100), Coord.FromMils(0))
                .To(Coord.FromMils(100), Coord.FromMils(0))
                .Width(Coord.FromMils(10))
                .Layer(1))
            .Build();

        var doc = await RenderPcbToSvg(component);
        Assert.Equal(1, CountElements(doc, "line"));
    }

    [Fact]
    public async Task PcbTrack_TwoTracks_ProducesTwoLines()
    {
        var component = PcbComponent.Create("TestFP")
            .AddTrack(t => t
                .From(Coord.FromMils(-100), Coord.FromMils(0))
                .To(Coord.FromMils(100), Coord.FromMils(0))
                .Width(Coord.FromMils(10))
                .Layer(1))
            .AddTrack(t => t
                .From(Coord.FromMils(0), Coord.FromMils(-100))
                .To(Coord.FromMils(0), Coord.FromMils(100))
                .Width(Coord.FromMils(10))
                .Layer(1))
            .Build();

        var doc = await RenderPcbToSvg(component);
        Assert.Equal(2, CountElements(doc, "line"));
    }

    [Fact]
    public async Task PcbArc_FullCircle_ProducesEllipse()
    {
        var component = PcbComponent.Create("TestFP")
            .AddArc(a => a
                .At(Coord.FromMils(0), Coord.FromMils(0))
                .Radius(Coord.FromMils(50))
                .FullCircle()
                .Width(Coord.FromMils(5))
                .Layer(21))
            .Build();

        var doc = await RenderPcbToSvg(component);
        Assert.True(CountElements(doc, "ellipse") >= 1, "Full circle arc should produce ellipse");
    }

    [Fact]
    public async Task PcbArc_Partial_ProducesPath()
    {
        var component = PcbComponent.Create("TestFP")
            .AddArc(a => a
                .At(Coord.FromMils(0), Coord.FromMils(0))
                .Radius(Coord.FromMils(50))
                .Angles(0, 90)
                .Width(Coord.FromMils(5))
                .Layer(21))
            .Build();

        var doc = await RenderPcbToSvg(component);
        Assert.True(CountElements(doc, "path") >= 1, "Partial arc should produce path");
    }

    [Fact]
    public async Task PcbPad_ProducesMultipleShapes()
    {
        var component = PcbComponent.Create("TestFP")
            .AddPad(p => p
                .At(Coord.FromMils(0), Coord.FromMils(0))
                .Size(Coord.FromMils(60), Coord.FromMils(60))
                .HoleSize(Coord.FromMils(30))
                .WithDesignator("1")
                .Layer(1))
            .Build();

        var doc = await RenderPcbToSvg(component);

        // A pad with hole produces:
        // 4 pad shapes (bottom solder mask, top solder mask, bottom copper, top copper)
        // + 1 hole (FillEllipse) + possibly 1 designator text
        // Total shapes should be > 4
        var totalShapes = CountElements(doc, "rect")
            + CountElements(doc, "ellipse")
            + CountElements(doc, "polygon");

        // Subtract 1 for background rect
        Assert.True(totalShapes - 1 >= 4,
            $"Pad should produce at least 4 shapes (layers + hole), found {totalShapes - 1} (excluding background)");
    }

    [Fact]
    public async Task PcbVia_ProducesEllipseElements()
    {
        var component = PcbComponent.Create("TestFP")
            .AddVia(v => v
                .At(Coord.FromMils(0), Coord.FromMils(0))
                .Diameter(Coord.FromMils(40))
                .HoleSize(Coord.FromMils(20))
                .ThroughHole())
            .Build();

        var doc = await RenderPcbToSvg(component);

        // Via: split pie halves (paths) or single ellipse + hole ellipse
        var totalShapes = CountElements(doc, "ellipse")
            + CountElements(doc, "path");
        Assert.True(totalShapes >= 2,
            $"Via should produce at least 2 shapes (outer + hole), found {totalShapes}");
    }

    [Fact]
    public async Task PcbFill_ProducesFilledRect()
    {
        var component = PcbComponent.Create("TestFP")
            .AddFill(f => f
                .From(Coord.FromMils(-50), Coord.FromMils(-25))
                .To(Coord.FromMils(50), Coord.FromMils(25))
                .OnLayer(1))
            .Build();

        var doc = await RenderPcbToSvg(component);

        // 1 background + 1 fill rect = 2
        Assert.Equal(2, CountElements(doc, "rect"));
    }

    [Fact]
    public async Task PcbRegion_ProducesFilledPolygon()
    {
        var component = PcbComponent.Create("TestFP")
            .AddRegion(r => r
                .AddPoint(Coord.FromMils(-50), Coord.FromMils(-50))
                .AddPoint(Coord.FromMils(50), Coord.FromMils(-50))
                .AddPoint(Coord.FromMils(50), Coord.FromMils(50))
                .AddPoint(Coord.FromMils(-50), Coord.FromMils(50))
                .OnLayer(1))
            .Build();

        var doc = await RenderPcbToSvg(component);

        var polygons = doc.Descendants(Ns + "polygon").ToList();
        Assert.Equal(1, polygons.Count);
        Assert.NotEqual("none", polygons[0].Attribute("fill")?.Value);
    }

    [Fact]
    public async Task PcbText_ProducesTextElement()
    {
        var component = PcbComponent.Create("TestFP")
            .AddText("REF**", t => t
                .At(Coord.FromMils(0), Coord.FromMils(0))
                .Height(Coord.FromMils(40))
                .Layer(21))
            .Build();

        var doc = await RenderPcbToSvg(component);

        var texts = doc.Descendants(Ns + "text").ToList();
        Assert.True(texts.Count >= 1,
            $"PCB text should produce at least 1 <text> element, found {texts.Count}");
        if (texts.Count > 0)
            Assert.Contains("REF**", string.Join(" ", texts.Select(t => t.Value)));
    }

    [Fact]
    public async Task PcbComponentBody_ProducesTwoPolygons()
    {
        var component = PcbComponent.Create("TestFP")
            .AddComponentBody(b => b
                .AddPoint(Coord.FromMils(-50), Coord.FromMils(-50))
                .AddPoint(Coord.FromMils(50), Coord.FromMils(-50))
                .AddPoint(Coord.FromMils(50), Coord.FromMils(50))
                .AddPoint(Coord.FromMils(-50), Coord.FromMils(50))
                .OnLayer("TOP"))
            .Build();

        var doc = await RenderPcbToSvg(component);

        // ComponentBody: FillPolygon + DrawPolygon = 2 polygons
        var polygons = doc.Descendants(Ns + "polygon").ToList();
        Assert.Equal(2, polygons.Count);

        Assert.Contains(polygons, p => p.Attribute("fill")?.Value == "none");
        Assert.Contains(polygons, p => p.Attribute("fill")?.Value != "none");
    }

    // ── Composite Tests ──────────────────────────────────────────────

    [Fact]
    public async Task Composite_ResistorSymbol_ProducesCorrectElements()
    {
        // Build a realistic resistor symbol: rectangle body + 2 pins
        var c = new SchComponent { Name = "R" };

        c.AddRectangle(new SchRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-30), Coord.FromMils(-80)),
            Corner2 = new CoordPoint(Coord.FromMils(30), Coord.FromMils(80)),
            Color = 0x00FF0000,
            FillColor = 0x0000FFFF,
            IsFilled = true
        });

        c.AddPin(new SchPin
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(120)),
            Length = Coord.FromMils(40),
            Orientation = PinOrientation.Down,
            Designator = "1",
            Name = "~",
            ShowName = false,
            ShowDesignator = true
        });

        c.AddPin(new SchPin
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(-120)),
            Length = Coord.FromMils(40),
            Orientation = PinOrientation.Up,
            Designator = "2",
            Name = "~",
            ShowName = false,
            ShowDesignator = true
        });

        var doc = await RenderSchToSvg(c);

        // Rects: 1 bg + 1 fill + 1 border = 3
        Assert.Equal(3, CountElements(doc, "rect"));
        // Lines: 2 pin bodies
        Assert.Equal(2, CountElements(doc, "line"));
        // Texts: 2 designators (names hidden)
        Assert.Equal(2, CountElements(doc, "text"));
    }

    [Fact]
    public async Task Composite_OpAmpSymbol_ProducesCorrectElements()
    {
        var c = new SchComponent { Name = "OPAMP" };

        // Triangle body (polyline with 4 points, closed)
        c.AddPolyline(SchPolyline.Create()
            .From(Coord.FromMils(-50), Coord.FromMils(-80))
            .To(Coord.FromMils(-50), Coord.FromMils(80))
            .To(Coord.FromMils(100), Coord.FromMils(0))
            .To(Coord.FromMils(-50), Coord.FromMils(-80))
            .Build());

        // 3 pins
        c.AddPin(new SchPin
        {
            Location = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(40)),
            Length = Coord.FromMils(50),
            Orientation = PinOrientation.Right,
            Designator = "3",
            Name = "+",
            ShowName = true,
            ShowDesignator = true
        });
        c.AddPin(new SchPin
        {
            Location = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(-40)),
            Length = Coord.FromMils(50),
            Orientation = PinOrientation.Right,
            Designator = "2",
            Name = "-",
            ShowName = true,
            ShowDesignator = true
        });
        c.AddPin(new SchPin
        {
            Location = new CoordPoint(Coord.FromMils(150), Coord.FromMils(0)),
            Length = Coord.FromMils(50),
            Orientation = PinOrientation.Left,
            Designator = "1",
            Name = "OUT",
            ShowName = true,
            ShowDesignator = true
        });

        var doc = await RenderSchToSvg(c);

        // 1 polyline for the triangle body
        Assert.Equal(1, CountElements(doc, "polyline"));
        // 3 pin lines
        Assert.Equal(3, CountElements(doc, "line"));
        // 3 pins * 2 (name + designator) = 6 texts
        Assert.Equal(6, CountElements(doc, "text"));
    }

    [Fact]
    public async Task Composite_PcbFootprint_ProducesAllPrimitives()
    {
        // Build a realistic QFP-like footprint
        var component = PcbComponent.Create("QFP")
            .WithDescription("Test QFP")
            // 2 pads
            .AddPad(p => p
                .At(Coord.FromMils(-100), Coord.FromMils(-50))
                .Size(Coord.FromMils(60), Coord.FromMils(25))
                .HoleSize(Coord.FromMils(0))
                .WithDesignator("1")
                .Layer(1))
            .AddPad(p => p
                .At(Coord.FromMils(-100), Coord.FromMils(50))
                .Size(Coord.FromMils(60), Coord.FromMils(25))
                .HoleSize(Coord.FromMils(0))
                .WithDesignator("2")
                .Layer(1))
            // 4 silkscreen tracks
            .AddTrack(t => t
                .From(Coord.FromMils(-75), Coord.FromMils(-75))
                .To(Coord.FromMils(75), Coord.FromMils(-75))
                .Width(Coord.FromMils(5))
                .Layer(21))
            .AddTrack(t => t
                .From(Coord.FromMils(75), Coord.FromMils(-75))
                .To(Coord.FromMils(75), Coord.FromMils(75))
                .Width(Coord.FromMils(5))
                .Layer(21))
            .AddTrack(t => t
                .From(Coord.FromMils(75), Coord.FromMils(75))
                .To(Coord.FromMils(-75), Coord.FromMils(75))
                .Width(Coord.FromMils(5))
                .Layer(21))
            .AddTrack(t => t
                .From(Coord.FromMils(-75), Coord.FromMils(75))
                .To(Coord.FromMils(-75), Coord.FromMils(-75))
                .Width(Coord.FromMils(5))
                .Layer(21))
            .Build();

        var doc = await RenderPcbToSvg(component);

        // 4 silkscreen tracks = 4 lines
        Assert.Equal(4, CountElements(doc, "line"));

        // Each pad creates multiple shapes; we should have shapes for 2 pads
        var totalPadShapes = CountElements(doc, "rect") - 1 // subtract background
            + CountElements(doc, "ellipse")
            + CountElements(doc, "polygon");
        Assert.True(totalPadShapes >= 4,
            $"2 pads should produce at least 4 pad shapes, found {totalPadShapes}");
    }

    // ── Empty / Edge Cases ───────────────────────────────────────────

    [Fact]
    public async Task EmptyComponent_ProducesOnlyBackgroundRect()
    {
        var c = new SchComponent { Name = "Empty" };
        var doc = await RenderSchToSvg(c);

        Assert.Equal(1, CountElements(doc, "rect")); // background only
        Assert.Equal(0, CountElements(doc, "line"));
        Assert.Equal(0, CountElements(doc, "ellipse"));
        Assert.Equal(0, CountElements(doc, "polygon"));
        Assert.Equal(0, CountElements(doc, "polyline"));
        Assert.Equal(0, CountElements(doc, "path"));
        Assert.Equal(0, CountElements(doc, "text"));
    }

    [Fact]
    public async Task EmptyPcbComponent_ProducesOnlyBackgroundRect()
    {
        var component = PcbComponent.Create("Empty").Build();
        var doc = await RenderPcbToSvg(component);

        Assert.Equal(1, CountElements(doc, "rect")); // background only
        Assert.Equal(0, CountElements(doc, "line"));
        Assert.Equal(0, CountElements(doc, "ellipse"));
        Assert.Equal(0, CountElements(doc, "polygon"));
        Assert.Equal(0, CountElements(doc, "path"));
    }

    // ── SVG Structure Validation ─────────────────────────────────────

    [Fact]
    public async Task SvgOutput_HasCorrectNamespace()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddLine(new SchLine
        {
            Start = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(100), Coord.FromMils(0)),
            Color = 0x000000FF,
            Width = Coord.FromMils(10)
        });

        var doc = await RenderSchToSvg(c);

        Assert.Equal("svg", doc.Root!.Name.LocalName);
        Assert.Equal("http://www.w3.org/2000/svg", doc.Root.Name.NamespaceName);
    }

    [Fact]
    public async Task SvgOutput_HasWidthHeightViewBox()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddRectangle(new SchRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-50)),
            Corner2 = new CoordPoint(Coord.FromMils(50), Coord.FromMils(50)),
            Color = 0x00FF0000
        });

        var doc = await RenderSchToSvg(c, 512);

        Assert.Equal("512", doc.Root!.Attribute("width")?.Value);
        Assert.Equal("512", doc.Root.Attribute("height")?.Value);
        Assert.Contains("512", doc.Root.Attribute("viewBox")?.Value ?? "");
    }

    // ── Line Style Tests ─────────────────────────────────────────────

    [Fact]
    public async Task SchLine_DashedStyle_HasDashArray()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddLine(new SchLine
        {
            Start = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(100), Coord.FromMils(0)),
            Color = 0x000000FF,
            Width = Coord.FromMils(10),
            LineStyle = 1 // Dashed
        });

        var doc = await RenderSchToSvg(c);
        var line = doc.Descendants(Ns + "line").First();

        Assert.NotNull(line.Attribute("stroke-dasharray"));
        Assert.Equal("8,4", line.Attribute("stroke-dasharray")!.Value);
    }

    [Fact]
    public async Task SchLine_DottedStyle_HasDashArray()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddLine(new SchLine
        {
            Start = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(100), Coord.FromMils(0)),
            Color = 0x000000FF,
            Width = Coord.FromMils(10),
            LineStyle = 2 // Dotted
        });

        var doc = await RenderSchToSvg(c);
        var line = doc.Descendants(Ns + "line").First();

        Assert.NotNull(line.Attribute("stroke-dasharray"));
        Assert.Equal("2,4", line.Attribute("stroke-dasharray")!.Value);
    }

    [Fact]
    public async Task SchLine_SolidStyle_NoDashArray()
    {
        var c = new SchComponent { Name = "Test" };
        c.AddLine(new SchLine
        {
            Start = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(100), Coord.FromMils(0)),
            Color = 0x000000FF,
            Width = Coord.FromMils(10),
            LineStyle = 0 // Solid
        });

        var doc = await RenderSchToSvg(c);
        var line = doc.Descendants(Ns + "line").First();

        Assert.Null(line.Attribute("stroke-dasharray"));
    }

    // ── Color Validation ─────────────────────────────────────────────

    [Fact]
    public async Task SchLine_Color_IsCorrectRgb()
    {
        var c = new SchComponent { Name = "Test" };
        // BGR 0x000000FF = R:255 G:0 B:0 → ARGB 0xFFFF0000 → svg rgb(255,0,0)
        c.AddLine(new SchLine
        {
            Start = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(100), Coord.FromMils(0)),
            Color = 0x000000FF, // Red in BGR
            Width = Coord.FromMils(10)
        });

        var doc = await RenderSchToSvg(c);
        var line = doc.Descendants(Ns + "line").First();

        Assert.Equal("rgb(255,0,0)", line.Attribute("stroke")!.Value);
    }
}
