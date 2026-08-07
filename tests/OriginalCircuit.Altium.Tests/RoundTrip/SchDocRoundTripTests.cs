using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Serialization.Readers;
using OriginalCircuit.Altium.Serialization.Writers;

namespace OriginalCircuit.Altium.Tests.RoundTrip;

public sealed class SchDocRoundTripTests
{
    [Fact]
    public void WriteThenRead_PreservesComponents()
    {
        var original = new SchDocument();
        var comp = new SchComponent
        {
            Name = "R1",
            Description = "Resistor",
            PartCount = 1
        };
        original.AddComponent(comp);

        var readBack = RoundTrip(original);

        Assert.Single(readBack.Components);
        Assert.Equal("R1", readBack.Components.First().Name);
    }

    [Fact]
    public void WriteThenRead_PreservesComponentsWithPins()
    {
        var original = new SchDocument();
        var comp = new SchComponent
        {
            Name = "R1",
            PartCount = 1
        };

        var pin = SchPin.Create("1")
            .WithName("A")
            .At(Coord.FromMils(0), Coord.FromMils(0))
            .Length(Coord.FromMils(200))
            .Orient(PinOrientation.Right)
            .Build();
        comp.AddPin(pin);
        original.AddComponent(comp);

        var readBack = RoundTrip(original);

        Assert.Single(readBack.Components);
        Assert.Single(readBack.Components.First().Pins);
    }

    [Fact]
    public void WriteThenRead_PreservesMultipleComponents()
    {
        var original = new SchDocument();

        for (var i = 1; i <= 3; i++)
        {
            var comp = new SchComponent
            {
                Name = $"U{i}",
                PartCount = 1
            };
            original.AddComponent(comp);
        }

        var readBack = RoundTrip(original);

        Assert.Equal(3, readBack.Components.Count);
    }

    [Fact]
    public void WriteThenRead_PreservesDocumentWires()
    {
        var doc = new SchDocument();
        var wire = new SchWire();
        wire.AddVertex(new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)));
        wire.AddVertex(new CoordPoint(Coord.FromMils(100), Coord.FromMils(200)));
        wire.Color = 255;
        doc.AddPrimitive(wire);

        var readBack = RoundTrip(doc);

        Assert.Single(readBack.Wires);
        var w = (SchWire)readBack.Wires[0];
        Assert.Equal(2, w.Vertices.Count);
        Assert.Equal(0, w.Vertices[0].X.ToMils(), 1);
        Assert.Equal(200, w.Vertices[1].Y.ToMils(), 1);
        Assert.Equal(255, w.Color);
    }

    [Fact]
    public void WriteThenRead_PreservesDocumentLabels()
    {
        var doc = new SchDocument();
        var label = new SchLabel
        {
            Text = "TestLabel",
            Location = new CoordPoint(Coord.FromMils(50), Coord.FromMils(75)),
            Color = 128,
            FontId = 1
        };
        doc.AddPrimitive(label);

        var readBack = RoundTrip(doc);

        Assert.Single(readBack.Labels);
        var l = (SchLabel)readBack.Labels[0];
        Assert.Equal("TestLabel", l.Text);
        Assert.Equal(50, l.Location.X.ToMils(), 1);
        Assert.Equal(75, l.Location.Y.ToMils(), 1);
    }

    [Fact]
    public void WriteThenRead_PreservesDocumentNetLabels()
    {
        var doc = new SchDocument();
        var netLabel = new SchNetLabel
        {
            Text = "VCC",
            Location = new CoordPoint(Coord.FromMils(100), Coord.FromMils(200)),
            Color = 128,
            FontId = 1
        };
        doc.AddPrimitive(netLabel);

        var readBack = RoundTrip(doc);

        Assert.Single(readBack.NetLabels);
        var nl = (SchNetLabel)readBack.NetLabels[0];
        Assert.Equal("VCC", nl.Text);
        Assert.Equal(100, nl.Location.X.ToMils(), 1);
        Assert.Equal(200, nl.Location.Y.ToMils(), 1);
    }

    [Fact]
    public void WriteThenRead_PreservesDocumentJunctions()
    {
        var doc = new SchDocument();
        var junction = new SchJunction
        {
            Location = new CoordPoint(Coord.FromMils(300), Coord.FromMils(400)),
            Color = 255
        };
        doc.AddPrimitive(junction);

        var readBack = RoundTrip(doc);

        Assert.Single(readBack.Junctions);
        var j = (SchJunction)readBack.Junctions[0];
        Assert.Equal(300, j.Location.X.ToMils(), 1);
        Assert.Equal(400, j.Location.Y.ToMils(), 1);
    }

    [Fact]
    public void WriteThenRead_PreservesDocumentLines()
    {
        var doc = new SchDocument();
        var line = new SchLine
        {
            Start = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(100), Coord.FromMils(100)),
            Color = 200,
            Width = Coord.FromMils(1)
        };
        doc.AddPrimitive(line);

        var readBack = RoundTrip(doc);

        Assert.Single(readBack.Lines);
        var l = readBack.Lines[0];
        Assert.Equal(0, l.Start.X.ToMils(), 1);
        Assert.Equal(100, l.End.X.ToMils(), 1);
    }

    [Fact]
    public void WriteThenRead_PreservesDocumentRectangles()
    {
        var doc = new SchDocument();
        var rect = new SchRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(10), Coord.FromMils(20)),
            Corner2 = new CoordPoint(Coord.FromMils(110), Coord.FromMils(120)),
            LineWidth = Coord.FromMils(1),
            Color = 100,
            FillColor = 200,
            IsFilled = true
        };
        doc.AddPrimitive(rect);

        var readBack = RoundTrip(doc);

        Assert.Single(readBack.Rectangles);
        var r = readBack.Rectangles[0];
        Assert.Equal(10, r.Corner1.X.ToMils(), 1);
        Assert.Equal(120, r.Corner2.Y.ToMils(), 1);
        Assert.True(r.IsFilled);
    }

    [Fact]
    public void WriteThenRead_PreservesDocumentParameters()
    {
        var doc = new SchDocument();
        var param = new SchParameter
        {
            Name = "Value",
            Value = "10k",
            Location = new CoordPoint(Coord.FromMils(50), Coord.FromMils(60)),
            FontId = 1
        };
        doc.AddPrimitive(param);

        var readBack = RoundTrip(doc);

        Assert.Single(readBack.Parameters);
        var p = (SchParameter)readBack.Parameters[0];
        Assert.Equal("Value", p.Name);
        Assert.Equal("10k", p.Value);
    }

    [Fact]
    public void WriteThenRead_PreservesDocumentArcs()
    {
        var doc = new SchDocument();
        var arc = new SchArc
        {
            Center = new CoordPoint(Coord.FromMils(100), Coord.FromMils(100)),
            Radius = Coord.FromMils(50),
            StartAngle = 0,
            EndAngle = 180,
            LineWidth = 1,
            Color = 128
        };
        doc.AddPrimitive(arc);

        var readBack = RoundTrip(doc);

        Assert.Single(readBack.Arcs);
        var a = readBack.Arcs[0];
        Assert.Equal(100, a.Center.X.ToMils(), 1);
        Assert.Equal(50, a.Radius.ToMils(), 1);
        Assert.Equal(0, a.StartAngle, 1);
        Assert.Equal(180, a.EndAngle, 1);
    }

    [Fact]
    public void WriteThenRead_PreservesDocumentPolygons()
    {
        var doc = new SchDocument();
        var polygon = new SchPolygon
        {
            Color = 100,
            FillColor = 200,
            IsFilled = true,
            LineWidth = 1
        };
        polygon.AddVertex(new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)));
        polygon.AddVertex(new CoordPoint(Coord.FromMils(100), Coord.FromMils(0)));
        polygon.AddVertex(new CoordPoint(Coord.FromMils(50), Coord.FromMils(100)));
        doc.AddPrimitive(polygon);

        var readBack = RoundTrip(doc);

        Assert.Single(readBack.Polygons);
        var p = readBack.Polygons[0];
        Assert.Equal(3, p.Vertices.Count);
        Assert.True(p.IsFilled);
    }

    [Fact]
    public void WriteThenRead_PreservesDocumentTextFrames()
    {
        var doc = new SchDocument();
        var textFrame = new SchTextFrame
        {
            Corner1 = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Corner2 = new CoordPoint(Coord.FromMils(200), Coord.FromMils(100)),
            Text = "Hello World",
            TextColor = 255,
            FillColor = 16777215,
            FontId = 1,
            ShowBorder = true,
            IsFilled = true
        };
        doc.AddPrimitive(textFrame);

        var readBack = RoundTrip(doc);

        Assert.Single(readBack.TextFrames);
        var tf = readBack.TextFrames[0];
        Assert.Equal("Hello World", tf.Text);
        Assert.Equal(255, tf.TextColor);
        Assert.Equal(200, tf.Corner2.X.ToMils(), 1);
    }

    [Fact]
    public void WriteThenRead_PreservesDocumentPowerObjects()
    {
        var doc = new SchDocument();
        var power = new SchPowerObject
        {
            Text = "VCC",
            Location = new CoordPoint(Coord.FromMils(100), Coord.FromMils(200)),
            Style = PowerPortStyle.Bar,
            Color = 128,
            ShowNetName = true,
            FontId = 1
        };
        doc.AddPrimitive(power);

        var readBack = RoundTrip(doc);

        Assert.Single(readBack.PowerObjects);
        var p = (SchPowerObject)readBack.PowerObjects[0];
        Assert.Equal("VCC", p.Text);
        Assert.Equal(PowerPortStyle.Bar, p.Style);
    }

    [Fact]
    public void WriteThenRead_PreservesComponentWithAllChildTypes()
    {
        var doc = new SchDocument();
        var comp = new SchComponent { Name = "U1", PartCount = 1 };

        comp.AddPin(SchPin.Create("1").WithName("A").At(Coord.FromMils(0), Coord.FromMils(0)).Length(Coord.FromMils(200)).Build());
        comp.AddLine(new SchLine { Start = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)), End = new CoordPoint(Coord.FromMils(100), Coord.FromMils(0)), Color = 128, Width = Coord.FromMils(1) });
        comp.AddRectangle(new SchRectangle { Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-50)), Corner2 = new CoordPoint(Coord.FromMils(50), Coord.FromMils(50)), Color = 128, LineWidth = Coord.FromMils(1) });
        comp.AddLabel(new SchLabel { Text = "Label", Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)), FontId = 1 });
        comp.AddArc(new SchArc { Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)), Radius = Coord.FromMils(25), StartAngle = 0, EndAngle = 360, Color = 128, LineWidth = 1 });

        doc.AddComponent(comp);

        var readBack = RoundTrip(doc);

        Assert.Single(readBack.Components);
        var c = readBack.Components[0];
        Assert.Single(c.Pins);
        Assert.Single(c.Lines);
        Assert.Single(c.Rectangles);
        Assert.Single(c.Labels);
        Assert.Single(c.Arcs);
    }

    [Fact]
    public void WriteThenRead_PreservesMixedDocumentAndComponentPrimitives()
    {
        var doc = new SchDocument();

        // Add a component with a pin
        var comp = new SchComponent { Name = "R1", PartCount = 1 };
        comp.AddPin(SchPin.Create("1").WithName("A").At(Coord.FromMils(0), Coord.FromMils(0)).Length(Coord.FromMils(200)).Build());
        doc.AddComponent(comp);

        // Add document-level wire
        var wire = new SchWire();
        wire.AddVertex(new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)));
        wire.AddVertex(new CoordPoint(Coord.FromMils(500), Coord.FromMils(0)));
        doc.AddPrimitive(wire);

        // Add document-level net label
        var netLabel = new SchNetLabel { Text = "NET1", Location = new CoordPoint(Coord.FromMils(250), Coord.FromMils(50)), FontId = 1 };
        doc.AddPrimitive(netLabel);

        var readBack = RoundTrip(doc);

        Assert.Single(readBack.Components);
        Assert.Single(readBack.Components[0].Pins);
        Assert.Single(readBack.Wires);
        Assert.Single(readBack.NetLabels);
    }

    [SkippableFact]
    public void WriteThenRead_RealFiles_PreservesAllPrimitiveCounts()
    {
        var testDataPath = GetTestDataPath();
        if (!Directory.Exists(testDataPath)) { Skip.If(true, "Test data not available"); return; }

        foreach (var filePath in Directory.GetFiles(testDataPath, "*.SchDoc"))
        {
            var originalDoc = (SchDocument)new SchDocReader().Read(File.OpenRead(filePath));

            using var ms = new MemoryStream();
            new SchDocWriter().Write(originalDoc, ms);

            ms.Position = 0;
            var rt = (SchDocument)new SchDocReader().Read(ms);

            var fileName = Path.GetFileName(filePath);
            Assert.True(originalDoc.Components.Count == rt.Components.Count, $"{fileName}: Components {originalDoc.Components.Count} != {rt.Components.Count}");
            Assert.True(originalDoc.Wires.Count == rt.Wires.Count, $"{fileName}: Wires {originalDoc.Wires.Count} != {rt.Wires.Count}");
            Assert.True(originalDoc.NetLabels.Count == rt.NetLabels.Count, $"{fileName}: NetLabels {originalDoc.NetLabels.Count} != {rt.NetLabels.Count}");
            Assert.True(originalDoc.Junctions.Count == rt.Junctions.Count, $"{fileName}: Junctions {originalDoc.Junctions.Count} != {rt.Junctions.Count}");
            Assert.True(originalDoc.PowerObjects.Count == rt.PowerObjects.Count, $"{fileName}: PowerObjects {originalDoc.PowerObjects.Count} != {rt.PowerObjects.Count}");
            Assert.True(originalDoc.Labels.Count == rt.Labels.Count, $"{fileName}: Labels {originalDoc.Labels.Count} != {rt.Labels.Count}");
            Assert.True(originalDoc.Parameters.Count == rt.Parameters.Count, $"{fileName}: Parameters {originalDoc.Parameters.Count} != {rt.Parameters.Count}");
            Assert.True(originalDoc.Lines.Count == rt.Lines.Count, $"{fileName}: Lines {originalDoc.Lines.Count} != {rt.Lines.Count}");
            Assert.True(originalDoc.Rectangles.Count == rt.Rectangles.Count, $"{fileName}: Rectangles {originalDoc.Rectangles.Count} != {rt.Rectangles.Count}");
            Assert.True(originalDoc.Polygons.Count == rt.Polygons.Count, $"{fileName}: Polygons {originalDoc.Polygons.Count} != {rt.Polygons.Count}");
            Assert.True(originalDoc.Polylines.Count == rt.Polylines.Count, $"{fileName}: Polylines {originalDoc.Polylines.Count} != {rt.Polylines.Count}");
            Assert.True(originalDoc.Arcs.Count == rt.Arcs.Count, $"{fileName}: Arcs {originalDoc.Arcs.Count} != {rt.Arcs.Count}");
            Assert.True(originalDoc.Beziers.Count == rt.Beziers.Count, $"{fileName}: Beziers {originalDoc.Beziers.Count} != {rt.Beziers.Count}");
            Assert.True(originalDoc.Ellipses.Count == rt.Ellipses.Count, $"{fileName}: Ellipses {originalDoc.Ellipses.Count} != {rt.Ellipses.Count}");
            Assert.True(originalDoc.RoundedRectangles.Count == rt.RoundedRectangles.Count, $"{fileName}: RoundedRectangles {originalDoc.RoundedRectangles.Count} != {rt.RoundedRectangles.Count}");
            Assert.True(originalDoc.Pies.Count == rt.Pies.Count, $"{fileName}: Pies {originalDoc.Pies.Count} != {rt.Pies.Count}");
            Assert.True(originalDoc.TextFrames.Count == rt.TextFrames.Count, $"{fileName}: TextFrames {originalDoc.TextFrames.Count} != {rt.TextFrames.Count}");
            Assert.True(originalDoc.Images.Count == rt.Images.Count, $"{fileName}: Images {originalDoc.Images.Count} != {rt.Images.Count}");
            Assert.True(originalDoc.Symbols.Count == rt.Symbols.Count, $"{fileName}: Symbols {originalDoc.Symbols.Count} != {rt.Symbols.Count}");
            Assert.True(originalDoc.EllipticalArcs.Count == rt.EllipticalArcs.Count, $"{fileName}: EllipticalArcs {originalDoc.EllipticalArcs.Count} != {rt.EllipticalArcs.Count}");
            Assert.True(originalDoc.NoErcs.Count == rt.NoErcs.Count, $"{fileName}: NoErcs {originalDoc.NoErcs.Count} != {rt.NoErcs.Count}");
            Assert.True(originalDoc.BusEntries.Count == rt.BusEntries.Count, $"{fileName}: BusEntries {originalDoc.BusEntries.Count} != {rt.BusEntries.Count}");
            Assert.True(originalDoc.Buses.Count == rt.Buses.Count, $"{fileName}: Buses {originalDoc.Buses.Count} != {rt.Buses.Count}");
            Assert.True(originalDoc.Ports.Count == rt.Ports.Count, $"{fileName}: Ports {originalDoc.Ports.Count} != {rt.Ports.Count}");
            Assert.True(originalDoc.SheetSymbols.Count == rt.SheetSymbols.Count, $"{fileName}: SheetSymbols {originalDoc.SheetSymbols.Count} != {rt.SheetSymbols.Count}");
            Assert.True(originalDoc.SheetEntries.Count == rt.SheetEntries.Count, $"{fileName}: SheetEntries {originalDoc.SheetEntries.Count} != {rt.SheetEntries.Count}");
            Assert.True(originalDoc.Blankets.Count == rt.Blankets.Count, $"{fileName}: Blankets {originalDoc.Blankets.Count} != {rt.Blankets.Count}");
            Assert.True(originalDoc.ParameterSets.Count == rt.ParameterSets.Count, $"{fileName}: ParameterSets {originalDoc.ParameterSets.Count} != {rt.ParameterSets.Count}");
        }
    }

    [SkippableFact]
    public void WriteThenRead_RealFiles_PreservesWireProperties()
    {
        var testDataPath = GetTestDataPath();
        var filePath = Path.Combine(testDataPath, "DAC.SchDoc");
        if (!File.Exists(filePath)) { Skip.If(true, "Test data not available"); return; }

        var original = (SchDocument)new SchDocReader().Read(File.OpenRead(filePath));

        using var ms = new MemoryStream();
        new SchDocWriter().Write(original, ms);
        ms.Position = 0;
        var rt = (SchDocument)new SchDocReader().Read(ms);

        for (int i = 0; i < Math.Min(5, original.Wires.Count); i++)
        {
            var ow = (SchWire)original.Wires[i];
            var rw = (SchWire)rt.Wires[i];
            Assert.Equal(ow.Vertices.Count, rw.Vertices.Count);
            for (int v = 0; v < ow.Vertices.Count; v++)
            {
                Assert.Equal(ow.Vertices[v].X.ToRaw(), rw.Vertices[v].X.ToRaw());
                Assert.Equal(ow.Vertices[v].Y.ToRaw(), rw.Vertices[v].Y.ToRaw());
            }
            Assert.Equal(ow.Color, rw.Color);
        }
    }

    [SkippableFact]
    public void ReadRealFile_ParsesSheetTemplateRecord()
    {
        var testDataPath = GetTestDataPath();
        var filePath = Path.Combine(testDataPath, "USB Power.SchDoc");
        if (!File.Exists(filePath)) { Skip.If(true, "Test data not available"); return; }

        var doc = (SchDocument)new SchDocReader().Read(File.OpenRead(filePath));

        // USB Power.SchDoc references the A4.SchDot sheet template (record type 39).
        var template = Assert.Single(doc.Templates);
        Assert.Contains("A4.SchDot", template.FileName);

        // The template owns its border/title-block graphics (labels, polylines and a logo image) via
        // OWNERINDEX. They must be attached to the template, NOT leaked into the document's own top-level
        // collections — otherwise the renderer draws the template frame a second time over the schematic.
        Assert.NotEmpty(template.OwnedPrimitives);
        Assert.Contains(template.OwnedPrimitives, p => p is SchLabel);
        Assert.Contains(template.OwnedPrimitives, p => p is SchPolyline);
        // None of the template-owned primitives may appear as document-level primitives.
        Assert.DoesNotContain(doc.Labels, l => template.OwnedPrimitives.Contains(l));
        Assert.DoesNotContain(doc.Polylines, l => template.OwnedPrimitives.Contains(l));
        Assert.DoesNotContain(doc.Images, i => template.OwnedPrimitives.Contains(i));

        // The template and all its owned graphics survive a round-trip through the model.
        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var rt = (SchDocument)new SchDocReader().Read(ms);
        var rtTemplate = Assert.Single(rt.Templates);
        Assert.Equal(template.FileName, rtTemplate.FileName);
        Assert.Equal(template.OwnedPrimitives.Count, rtTemplate.OwnedPrimitives.Count);
    }

    [Fact]
    public void FromScratch_Template_RoundTrips()
    {
        var doc = new SchDocument();
        doc.AddPrimitive(new SchTemplate
        {
            FileName = @"C:\Templates\A4.SchDot",
            IsNotAccessible = true,
        });

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var rt = (SchDocument)new SchDocReader().Read(ms);

        var template = Assert.Single(rt.Templates);
        Assert.Equal(@"C:\Templates\A4.SchDot", template.FileName);
        Assert.True(template.IsNotAccessible);
    }

    [Fact]
    public void FromScratch_Template_WithOwnedPrimitives_RoundTrips()
    {
        var doc = new SchDocument();
        var template = new SchTemplate { FileName = @"C:\Templates\A4.SchDot" };
        template.AddPrimitive(new SchLabel
        {
            Text = "Title Block",
            Location = new CoordPoint(Coord.FromMils(6750), Coord.FromMils(450)),
            FontId = 1,
        });
        var frame = SchPolyline.Create().From(Coord.FromMils(6700), Coord.FromMils(600)).To(Coord.FromMils(11300), Coord.FromMils(600)).Build();
        template.AddPrimitive(frame);
        doc.AddPrimitive(template);

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var rt = (SchDocument)new SchDocReader().Read(ms);

        // The template's owned graphics round-trip attached to the template...
        var rtTemplate = Assert.Single(rt.Templates);
        Assert.Equal(2, rtTemplate.OwnedPrimitives.Count);
        var rtLabel = Assert.Single(rtTemplate.OwnedPrimitives.OfType<SchLabel>());
        Assert.Equal("Title Block", rtLabel.Text);
        Assert.Single(rtTemplate.OwnedPrimitives.OfType<SchPolyline>());

        // ...and are NOT surfaced as document-level primitives (which would double-draw the frame).
        Assert.Empty(rt.Labels);
        Assert.Empty(rt.Polylines);
    }

    [Fact]
    public void FromScratch_Note_RoundTrips()
    {
        var doc = new SchDocument();
        doc.AddPrimitive(new SchNote
        {
            Text = "Design note: rev B",
            Author = "Mark",
            Corner1 = new CoordPoint(Coord.FromMils(100), Coord.FromMils(200)),
            Corner2 = new CoordPoint(Coord.FromMils(400), Coord.FromMils(300)),
            Color = 255,
        });

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var rt = (SchDocument)new SchDocReader().Read(ms);

        var note = Assert.Single(rt.Notes);
        Assert.Equal("Design note: rev B", note.Text);
        Assert.Equal("Mark", note.Author);
        Assert.Equal(Coord.FromMils(100).ToRaw(), note.Corner1.X.ToRaw());
        Assert.Equal(Coord.FromMils(400).ToRaw(), note.Corner2.X.ToRaw());
    }

    [Fact]
    public void FromScratch_Hyperlink_RoundTrips()
    {
        var doc = new SchDocument();
        doc.AddPrimitive(new SchHyperlink
        {
            Text = "Datasheet",
            Url = "https://example.com/datasheet.pdf",
            Location = new CoordPoint(Coord.FromMils(500), Coord.FromMils(600)),
            Color = 128,
        });

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var rt = (SchDocument)new SchDocReader().Read(ms);

        var hyperlink = Assert.Single(rt.Hyperlinks);
        Assert.Equal("Datasheet", hyperlink.Text);
        Assert.Equal("https://example.com/datasheet.pdf", hyperlink.Url);
        Assert.Equal(Coord.FromMils(500).ToRaw(), hyperlink.Location.X.ToRaw());
    }

    [Fact]
    public void FromScratch_CompileMask_RoundTrips()
    {
        var doc = new SchDocument();
        doc.AddPrimitive(new SchCompileMask
        {
            Corner1 = new CoordPoint(Coord.FromMils(100), Coord.FromMils(100)),
            Corner2 = new CoordPoint(Coord.FromMils(500), Coord.FromMils(400)),
            AreaColor = 0xC0C0C0,
        });

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var rt = (SchDocument)new SchDocReader().Read(ms);

        var mask = Assert.Single(rt.CompileMasks);
        Assert.Equal(Coord.FromMils(100).ToRaw(), mask.Corner1.X.ToRaw());
        Assert.Equal(Coord.FromMils(500).ToRaw(), mask.Corner2.X.ToRaw());
        Assert.Equal(0xC0C0C0, mask.AreaColor);
    }

    [Fact]
    public void FromScratch_Harness_RoundTrips()
    {
        var doc = new SchDocument();
        doc.AddPrimitive(new SchHarnessConnector
        {
            Corner1 = new CoordPoint(Coord.FromMils(100), Coord.FromMils(100)),
            Corner2 = new CoordPoint(Coord.FromMils(300), Coord.FromMils(400)),
            Color = 128,
            AreaColor = 0xFFFFFF,
        });
        doc.AddPrimitive(new SchHarnessEntry
        {
            Text = "GND",
            Side = 1,
            DistanceFromTop = Coord.FromMils(50),
            TextColor = 255,
        });
        doc.AddPrimitive(new SchHarnessType
        {
            Text = "PowerHarness",
            Location = new CoordPoint(Coord.FromMils(110), Coord.FromMils(390)),
            Color = 64,
        });

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var rt = (SchDocument)new SchDocReader().Read(ms);

        var connector = Assert.Single(rt.HarnessConnectors);
        Assert.Equal(Coord.FromMils(300).ToRaw(), connector.Corner2.X.ToRaw());

        var entry = Assert.Single(rt.HarnessEntries);
        Assert.Equal("GND", entry.Text);
        Assert.Equal(1, entry.Side);
        Assert.Equal(Coord.FromMils(50).ToRaw(), entry.DistanceFromTop.ToRaw());

        var harnessType = Assert.Single(rt.HarnessTypes);
        Assert.Equal("PowerHarness", harnessType.Text);
        Assert.Equal(Coord.FromMils(110).ToRaw(), harnessType.Location.X.ToRaw());
    }

    [Fact]
    public void FromScratch_SignalHarness_RoundTrips()
    {
        var doc = new SchDocument();
        var harness = new SchSignalHarness { Color = 200, LineWidth = 1 };
        harness.Vertices.Add(new CoordPoint(Coord.FromMils(100), Coord.FromMils(100)));
        harness.Vertices.Add(new CoordPoint(Coord.FromMils(300), Coord.FromMils(100)));
        harness.Vertices.Add(new CoordPoint(Coord.FromMils(300), Coord.FromMils(400)));
        doc.AddPrimitive(harness);

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var rt = (SchDocument)new SchDocReader().Read(ms);

        var rtHarness = Assert.Single(rt.SignalHarnesses);
        Assert.Equal(3, rtHarness.Vertices.Count);
        Assert.Equal(Coord.FromMils(300).ToRaw(), rtHarness.Vertices[2].X.ToRaw());
        Assert.Equal(Coord.FromMils(400).ToRaw(), rtHarness.Vertices[2].Y.ToRaw());
        Assert.Equal(200, rtHarness.Color);
    }

    [Fact]
    public void WriteThenRead_ComponentComment_RoundTrips()
    {
        var doc = new SchDocument();
        var comp = SchComponent.Create("R1").WithComment("10k").Build();
        doc.AddComponent(comp);

        var readBack = RoundTrip(doc);

        Assert.Equal("10k", ((SchComponent)readBack.Components[0]).Comment);
    }

    [SkippableFact]
    public void WriteThenRead_RealFile_MutatedComment_Persists()
    {
        // Regression test for a bug where SchDocWriter's byte-faithful replay path (taken whenever the
        // primitive count is unchanged) echoed the original captured bytes verbatim, silently dropping
        // an edit to SchComponent.Comment because the edit doesn't add or remove a primitive.
        var testDataPath = GetTestDataPath();
        var filePath = Path.Combine(testDataPath, "DAC.SchDoc");
        if (!File.Exists(filePath)) { Skip.If(true, "Test data not available"); return; }

        var original = (SchDocument)new SchDocReader().Read(File.OpenRead(filePath));
        var comp = (SchComponent)original.Components[0];
        var before = comp.Comment;
        comp.Comment = "MUTATED_COMMENT_VALUE";

        using var ms = new MemoryStream();
        new SchDocWriter().Write(original, ms);
        ms.Position = 0;
        var rt = (SchDocument)new SchDocReader().Read(ms);

        Assert.NotEqual("MUTATED_COMMENT_VALUE", before);
        Assert.Equal("MUTATED_COMMENT_VALUE", ((SchComponent)rt.Components[0]).Comment);
    }

    [SkippableFact]
    public void WriteThenRead_RealFiles_PreservesComponentProperties()
    {
        var testDataPath = GetTestDataPath();
        var filePath = Path.Combine(testDataPath, "DAC.SchDoc");
        if (!File.Exists(filePath)) { Skip.If(true, "Test data not available"); return; }

        var original = (SchDocument)new SchDocReader().Read(File.OpenRead(filePath));

        using var ms = new MemoryStream();
        new SchDocWriter().Write(original, ms);
        ms.Position = 0;
        var rt = (SchDocument)new SchDocReader().Read(ms);

        for (int i = 0; i < original.Components.Count; i++)
        {
            Assert.Equal(original.Components[i].Name, rt.Components[i].Name);
            Assert.Equal(original.Components[i].Pins.Count, rt.Components[i].Pins.Count);
        }
    }

    private static SchDocument RoundTrip(SchDocument original)
    {
        using var ms = new MemoryStream();
        new SchDocWriter().Write(original, ms);
        ms.Position = 0;
        return new SchDocReader().Read(ms);
    }

    private static string GetTestDataPath()
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "TestData");
    }
}
