using System.Xml.Linq;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Rendering;
using OriginalCircuit.Altium.Rendering.Raster;
using OriginalCircuit.Altium.Rendering.Svg;

namespace OriginalCircuit.Altium.Tests;

/// <summary>
/// Smoke tests for the rendering system.
/// Verifies that rendering produces non-empty output without exceptions.
/// </summary>
public sealed class RenderingTests
{
    [Fact]
    public void CoordTransform_AutoZoom_CentersOnBounds()
    {
        var transform = new CoordTransform
        {
            ScreenWidth = 800,
            ScreenHeight = 600
        };

        var bounds = new CoordRect(
            Coord.FromMils(0), Coord.FromMils(0),
            Coord.FromMils(1000), Coord.FromMils(800));

        transform.AutoZoom(bounds);

        Assert.True(transform.Scale > 0);
        Assert.True(transform.CenterX > 0);
        Assert.True(transform.CenterY > 0);
    }

    [Fact]
    public void CoordTransform_WorldToScreen_ReturnsScreenCoordinates()
    {
        var transform = new CoordTransform
        {
            ScreenWidth = 800,
            ScreenHeight = 600,
            Scale = 0.01,
            CenterX = 0,
            CenterY = 0
        };

        var (sx, sy) = transform.WorldToScreen(Coord.FromMils(0), Coord.FromMils(0));
        Assert.Equal(400, sx, 0.1);
        Assert.Equal(300, sy, 0.1);
    }

    [Fact]
    public void ColorHelper_BgrToArgb_ConvertsCorrectly()
    {
        // BGR: Blue=0xFF, Green=0x00, Red=0x00 -> ARGB: 0xFF0000FF
        Assert.Equal(0xFF0000FFu, ColorHelper.BgrToArgb(0x00FF0000));

        // BGR: Blue=0x00, Green=0x00, Red=0xFF -> ARGB: 0xFFFF0000
        Assert.Equal(0xFFFF0000u, ColorHelper.BgrToArgb(0x000000FF));

        // BGR: Blue=0x00, Green=0xFF, Red=0x00 -> ARGB: 0xFF00FF00
        Assert.Equal(0xFF00FF00u, ColorHelper.BgrToArgb(0x0000FF00));
    }

    [Fact]
    public async Task RasterRenderer_PcbComponent_ProducesNonEmptyPng()
    {
        var component = PcbComponent.Create("TestFP")
            .WithDescription("Test footprint")
            .AddPad(p => p
                .At(Coord.FromMils(0), Coord.FromMils(0))
                .Size(Coord.FromMils(60), Coord.FromMils(25))
                .HoleSize(Coord.FromMils(0))
                .WithDesignator("1")
                .Layer(1))
            .AddTrack(t => t
                .From(Coord.FromMils(-100), Coord.FromMils(0))
                .To(Coord.FromMils(100), Coord.FromMils(0))
                .Width(Coord.FromMils(10))
                .Layer(1))
            .Build();

        var renderer = new RasterRenderer();
        using var ms = new MemoryStream();
        await renderer.RenderAsync(component, ms, new RenderOptions { Width = 256, Height = 256 });

        Assert.True(ms.Length > 0, "PNG output should be non-empty");
        ms.Position = 0;
        // Check PNG magic bytes
        var header = new byte[4];
        ms.Read(header, 0, 4);
        Assert.Equal(0x89, header[0]); // PNG signature
        Assert.Equal((byte)'P', header[1]);
        Assert.Equal((byte)'N', header[2]);
        Assert.Equal((byte)'G', header[3]);
    }

    [Fact]
    public async Task SvgRenderer_PcbComponent_ProducesValidSvg()
    {
        var component = PcbComponent.Create("TestFP")
            .WithDescription("Test footprint")
            .AddPad(p => p
                .At(Coord.FromMils(0), Coord.FromMils(0))
                .Size(Coord.FromMils(60), Coord.FromMils(25))
                .HoleSize(Coord.FromMils(0))
                .WithDesignator("1")
                .Layer(1))
            .Build();

        var renderer = new SvgRenderer();
        using var ms = new MemoryStream();
        await renderer.RenderAsync(component, ms, new RenderOptions { Width = 256, Height = 256 });

        Assert.True(ms.Length > 0, "SVG output should be non-empty");
        ms.Position = 0;
        var svg = new StreamReader(ms).ReadToEnd();
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }

    [Fact]
    public async Task RasterRenderer_SchComponent_ProducesNonEmptyPng()
    {
        var component = new SchComponent { Name = "R1" };
        component.AddRectangle(new SchRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-100)),
            Corner2 = new CoordPoint(Coord.FromMils(50), Coord.FromMils(100)),
            Color = 0x00FF0000, // Blue in BGR
            FillColor = 0x0000FFFF, // Yellow in BGR
            IsFilled = true
        });
        component.AddPin(new SchPin
        {
            Location = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(0)),
            Length = Coord.FromMils(30),
            Orientation = PinOrientation.Left,
            Designator = "1"
        });

        var renderer = new RasterRenderer();
        using var ms = new MemoryStream();
        await renderer.RenderAsync(component, ms, new RenderOptions { Width = 256, Height = 256 });

        Assert.True(ms.Length > 0, "PNG output should be non-empty");
    }

    [Fact]
    public async Task SvgRenderer_SchComponent_ProducesValidSvg()
    {
        var component = new SchComponent { Name = "R1" };
        component.AddRectangle(new SchRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-100)),
            Corner2 = new CoordPoint(Coord.FromMils(50), Coord.FromMils(100)),
            Color = 0x00FF0000,
            FillColor = 0x0000FFFF,
            IsFilled = true
        });

        var renderer = new SvgRenderer();
        using var ms = new MemoryStream();
        await renderer.RenderAsync(component, ms, new RenderOptions { Width = 256, Height = 256 });

        Assert.True(ms.Length > 0, "SVG output should be non-empty");
        ms.Position = 0;
        var svg = new StreamReader(ms).ReadToEnd();
        Assert.Contains("<svg", svg);
        Assert.Contains("<rect", svg); // Should contain a rectangle element
    }

    [Fact]
    public void LayerColors_ReturnsColorsForKnownLayers()
    {
        Assert.NotEqual(0u, LayerColors.GetColor(1));  // Top Layer
        Assert.NotEqual(0u, LayerColors.GetColor(32)); // Bottom Layer
        Assert.NotEqual(0u, LayerColors.GetColor(57)); // Multi Layer
    }

    [Fact]
    public void LayerColors_DrawPriority_TopAboveBottom()
    {
        Assert.True(LayerColors.GetDrawPriority(1) > LayerColors.GetDrawPriority(32),
            "Top layer should have higher draw priority (drawn later) than bottom layer");
    }

    // ── SVG Validation Tests ────────────────────────────────────────

    private static readonly XNamespace SvgNs = "http://www.w3.org/2000/svg";

    /// <summary>
    /// Helper: renders a SchComponent to SVG and parses as XDocument.
    /// </summary>
    private static async Task<XDocument> RenderSchComponentToSvgDoc(SchComponent component)
    {
        var renderer = new SvgRenderer();
        using var ms = new MemoryStream();
        await renderer.RenderAsync(component, ms, new RenderOptions { Width = 256, Height = 256 });
        ms.Position = 0;
        return XDocument.Load(ms);
    }

    /// <summary>
    /// Helper: renders a PcbComponent to SVG and parses as XDocument.
    /// </summary>
    private static async Task<XDocument> RenderPcbComponentToSvgDoc(PcbComponent component)
    {
        var renderer = new SvgRenderer();
        using var ms = new MemoryStream();
        await renderer.RenderAsync(component, ms, new RenderOptions { Width = 256, Height = 256 });
        ms.Position = 0;
        return XDocument.Load(ms);
    }

    [Fact]
    public async Task SvgValidation_RootElement_IsSvgWithDimensions()
    {
        var component = new SchComponent { Name = "Test" };
        component.AddRectangle(new SchRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-50)),
            Corner2 = new CoordPoint(Coord.FromMils(50), Coord.FromMils(50)),
            Color = 0x00FF0000,
            IsFilled = true,
            FillColor = 0x0000FFFF
        });

        var doc = await RenderSchComponentToSvgDoc(component);

        Assert.NotNull(doc.Root);
        Assert.Equal("svg", doc.Root!.Name.LocalName);
        Assert.Equal("http://www.w3.org/2000/svg", doc.Root.Name.NamespaceName);
        Assert.NotNull(doc.Root.Attribute("width"));
        Assert.NotNull(doc.Root.Attribute("height"));
        Assert.NotNull(doc.Root.Attribute("viewBox"));
    }

    [Fact]
    public async Task SvgValidation_SchRectangle_ProducesRectElements()
    {
        var component = new SchComponent { Name = "R1" };
        component.AddRectangle(new SchRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-100)),
            Corner2 = new CoordPoint(Coord.FromMils(50), Coord.FromMils(100)),
            Color = 0x00FF0000,
            FillColor = 0x0000FFFF,
            IsFilled = true
        });

        var doc = await RenderSchComponentToSvgDoc(component);
        var rects = doc.Descendants(SvgNs + "rect").ToList();

        // At least 2 rects: background clear rect + fill rect + border rect (fill and border are separate)
        Assert.True(rects.Count >= 2,
            $"Expected at least 2 <rect> elements (background + filled rectangle), found {rects.Count}");

        // Verify at least one rect has a fill attribute (the filled rectangle)
        Assert.Contains(rects, r => r.Attribute("fill") != null);
    }

    [Fact]
    public async Task SvgValidation_SchLine_ProducesLineElement()
    {
        var component = new SchComponent { Name = "L1" };
        component.AddLine(new SchLine
        {
            Start = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(100), Coord.FromMils(0)),
            Color = 0x000000FF,
            Width = Coord.FromMils(10)
        });

        var doc = await RenderSchComponentToSvgDoc(component);
        var lines = doc.Descendants(SvgNs + "line").ToList();

        Assert.True(lines.Count >= 1,
            $"Expected at least 1 <line> element for the schematic line, found {lines.Count}");

        // Verify the line has required SVG attributes
        var line = lines.First();
        Assert.NotNull(line.Attribute("x1"));
        Assert.NotNull(line.Attribute("y1"));
        Assert.NotNull(line.Attribute("x2"));
        Assert.NotNull(line.Attribute("y2"));
        Assert.NotNull(line.Attribute("stroke"));
    }

    [Fact]
    public async Task SvgValidation_SchPin_ProducesLineAndTextElements()
    {
        var component = new SchComponent { Name = "P1" };
        component.AddPin(new SchPin
        {
            Location = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(0)),
            Length = Coord.FromMils(50),
            Orientation = PinOrientation.Right,
            Designator = "1",
            Name = "INPUT",
            ShowName = true,
            ShowDesignator = true
        });

        var doc = await RenderSchComponentToSvgDoc(component);

        // Pin renders as a line (pin body)
        var lines = doc.Descendants(SvgNs + "line").ToList();
        Assert.True(lines.Count >= 1,
            $"Expected at least 1 <line> for pin body, found {lines.Count}");

        // Pin name and designator render as text elements
        var texts = doc.Descendants(SvgNs + "text").ToList();
        Assert.True(texts.Count >= 2,
            $"Expected at least 2 <text> elements (pin name + designator), found {texts.Count}");

        // Verify text content includes pin name and designator
        var allText = string.Join(" ", texts.Select(t => t.Value));
        Assert.Contains("INPUT", allText);
        Assert.Contains("1", allText);
    }

    [Fact]
    public async Task SvgValidation_SchEllipse_ProducesEllipseElement()
    {
        var component = new SchComponent { Name = "E1" };
        component.AddEllipse(new SchEllipse
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            RadiusX = Coord.FromMils(50),
            RadiusY = Coord.FromMils(30),
            Color = 0x0000FF00,
            IsFilled = false
        });

        var doc = await RenderSchComponentToSvgDoc(component);
        var ellipses = doc.Descendants(SvgNs + "ellipse").ToList();

        Assert.True(ellipses.Count >= 1,
            $"Expected at least 1 <ellipse> element, found {ellipses.Count}");

        var ellipse = ellipses.First();
        Assert.NotNull(ellipse.Attribute("cx"));
        Assert.NotNull(ellipse.Attribute("cy"));
        Assert.NotNull(ellipse.Attribute("rx"));
        Assert.NotNull(ellipse.Attribute("ry"));
        Assert.NotNull(ellipse.Attribute("stroke"));
    }

    [Fact]
    public async Task SvgValidation_SchArc_FullCircle_ProducesEllipseElement()
    {
        var component = new SchComponent { Name = "A1" };
        component.AddArc(new SchArc
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Radius = Coord.FromMils(50),
            StartAngle = 0,
            EndAngle = 360,
            Color = 0x000000FF
        });

        var doc = await RenderSchComponentToSvgDoc(component);

        // A full-circle arc renders as an <ellipse> (see SchComponentRenderer.RenderArc)
        var ellipses = doc.Descendants(SvgNs + "ellipse").ToList();
        Assert.True(ellipses.Count >= 1,
            $"Expected at least 1 <ellipse> for full-circle arc, found {ellipses.Count}");
    }

    [Fact]
    public async Task SvgValidation_SchArc_Partial_ProducesPathElement()
    {
        var component = new SchComponent { Name = "A2" };
        component.AddArc(new SchArc
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Radius = Coord.FromMils(50),
            StartAngle = 0,
            EndAngle = 90,
            Color = 0x000000FF
        });

        var doc = await RenderSchComponentToSvgDoc(component);

        // A partial arc renders as a <path> with an arc command
        var paths = doc.Descendants(SvgNs + "path").ToList();
        Assert.True(paths.Count >= 1,
            $"Expected at least 1 <path> for partial arc, found {paths.Count}");

        // The path should contain an arc command (A)
        var pathD = paths.First().Attribute("d")?.Value ?? "";
        Assert.Contains("A", pathD);
    }

    [Fact]
    public async Task SvgValidation_SchLabel_ProducesTextElement()
    {
        var component = new SchComponent { Name = "LBL" };
        component.AddLabel(new SchLabel
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Text = "Hello World",
            Color = 0x00000000
        });

        var doc = await RenderSchComponentToSvgDoc(component);
        var texts = doc.Descendants(SvgNs + "text").ToList();

        Assert.True(texts.Count >= 1,
            $"Expected at least 1 <text> element for label, found {texts.Count}");

        var allText = string.Join(" ", texts.Select(t => t.Value));
        Assert.Contains("Hello World", allText);
    }

    [Fact]
    public async Task SvgValidation_MultiplePrimitives_AllPresent()
    {
        var component = new SchComponent { Name = "Multi" };

        // Add a rectangle
        component.AddRectangle(new SchRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-100)),
            Corner2 = new CoordPoint(Coord.FromMils(50), Coord.FromMils(100)),
            Color = 0x00FF0000,
            FillColor = 0x0000FFFF,
            IsFilled = true
        });

        // Add a line
        component.AddLine(new SchLine
        {
            Start = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(100), Coord.FromMils(0)),
            Color = 0x000000FF,
            Width = Coord.FromMils(10)
        });

        // Add a pin
        component.AddPin(new SchPin
        {
            Location = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(0)),
            Length = Coord.FromMils(30),
            Orientation = PinOrientation.Left,
            Designator = "1",
            Name = "A",
            ShowName = true,
            ShowDesignator = true
        });

        // Add an ellipse
        component.AddEllipse(new SchEllipse
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            RadiusX = Coord.FromMils(20),
            RadiusY = Coord.FromMils(20),
            Color = 0x0000FF00
        });

        var doc = await RenderSchComponentToSvgDoc(component);

        // Verify presence of each expected element type
        Assert.True(doc.Descendants(SvgNs + "rect").Any(), "Expected <rect> elements");
        Assert.True(doc.Descendants(SvgNs + "line").Any(), "Expected <line> elements");
        Assert.True(doc.Descendants(SvgNs + "text").Any(), "Expected <text> elements");
        Assert.True(doc.Descendants(SvgNs + "ellipse").Any(), "Expected <ellipse> elements");
    }

    [Fact]
    public async Task SvgValidation_PcbComponent_ProducesExpectedElements()
    {
        var component = PcbComponent.Create("TestFP")
            .WithDescription("Test footprint")
            .AddPad(p => p
                .At(Coord.FromMils(0), Coord.FromMils(0))
                .Size(Coord.FromMils(60), Coord.FromMils(25))
                .HoleSize(Coord.FromMils(0))
                .WithDesignator("1")
                .Layer(1))
            .AddTrack(t => t
                .From(Coord.FromMils(-100), Coord.FromMils(0))
                .To(Coord.FromMils(100), Coord.FromMils(0))
                .Width(Coord.FromMils(10))
                .Layer(1))
            .Build();

        var doc = await RenderPcbComponentToSvgDoc(component);

        Assert.NotNull(doc.Root);
        Assert.Equal("svg", doc.Root!.Name.LocalName);

        // PCB tracks render as lines; pads render as filled shapes (rect or ellipse)
        var totalElements = doc.Descendants(SvgNs + "line").Count()
            + doc.Descendants(SvgNs + "rect").Count()
            + doc.Descendants(SvgNs + "ellipse").Count()
            + doc.Descendants(SvgNs + "polygon").Count();

        Assert.True(totalElements >= 2,
            $"Expected at least 2 shape elements for pad + track, found {totalElements}");
    }

    [Fact]
    public async Task SvgValidation_EmptyComponent_ProducesValidSvgWithBackground()
    {
        var component = new SchComponent { Name = "Empty" };

        var doc = await RenderSchComponentToSvgDoc(component);

        Assert.NotNull(doc.Root);
        Assert.Equal("svg", doc.Root!.Name.LocalName);

        // Even with no primitives, we expect the background rect from Clear()
        var rects = doc.Descendants(SvgNs + "rect").ToList();
        Assert.True(rects.Count >= 1,
            "Expected at least 1 <rect> for the background, even with no primitives");
    }
}
