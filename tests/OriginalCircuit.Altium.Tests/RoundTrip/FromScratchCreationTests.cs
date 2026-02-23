using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Serialization.Readers;
using OriginalCircuit.Altium.Serialization.Writers;

namespace OriginalCircuit.Altium.Tests.RoundTrip;

/// <summary>
/// Verifies that each file type can be created from scratch without reading
/// an existing file, saved, then loaded back with correct property values.
/// </summary>
public sealed class FromScratchCreationTests
{
    #region PcbLib

    [Fact]
    public void PcbLib_FromScratch_RoundTrips()
    {
        var library = new PcbLibrary();

        var component = PcbComponent.Create("QFP48")
            .WithDescription("48-pin QFP footprint")
            .WithHeight(Coord.FromMils(50))
            .AddPad(p => p
                .At(Coord.FromMils(0), Coord.FromMils(0))
                .Size(Coord.FromMils(60), Coord.FromMils(25))
                .HoleSize(Coord.FromMils(0))
                .WithDesignator("1")
                .Layer(1))
            .AddTrack(t => t
                .From(Coord.FromMils(-500), Coord.FromMils(-500))
                .To(Coord.FromMils(500), Coord.FromMils(-500))
                .Width(Coord.FromMils(10))
                .Layer(21))
            .AddArc(a => a
                .At(Coord.FromMils(0), Coord.FromMils(0))
                .Radius(Coord.FromMils(50))
                .Angles(0, 360)
                .Width(Coord.FromMils(10))
                .Layer(21))
            .AddText(".Designator", t => t
                .At(Coord.FromMils(0), Coord.FromMils(300))
                .Height(Coord.FromMils(60))
                .Layer(21))
            .Build();

        component.AddFill(new PcbFill
        {
            Corner1 = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(-100)),
            Corner2 = new CoordPoint(Coord.FromMils(100), Coord.FromMils(100)),
            Layer = 1
        });

        library.Add(component);

        // Round-trip
        using var ms = new MemoryStream();
        new PcbLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (PcbLibrary)new PcbLibReader().Read(ms);

        Assert.Equal(1, readBack.Count);
        var comp = readBack.Components.First();
        Assert.Equal("QFP48", comp.Name);
        Assert.Equal("48-pin QFP footprint", comp.Description);
        Assert.Equal(1, comp.Pads.Count);
        Assert.Equal(1, comp.Tracks.Count);
        Assert.Equal(1, comp.Arcs.Count);
        Assert.Equal(1, comp.Texts.Count);
        Assert.Equal(1, comp.Fills.Count);

        var pad = (PcbPad)comp.Pads[0];
        Assert.Equal("1", pad.Designator);
        Assert.Equal(60, pad.SizeTop.X.ToMils(), 1);
    }

    [Fact]
    public void PcbLib_FromScratch_MultipleComponents()
    {
        var library = new PcbLibrary();

        for (var i = 1; i <= 3; i++)
        {
            var comp = PcbComponent.Create($"COMP{i}")
                .WithDescription($"Component {i}")
                .AddPad(p => p
                    .At(Coord.FromMils(0), Coord.FromMils(0))
                    .Size(Coord.FromMils(50), Coord.FromMils(50))
                    .WithDesignator("1")
                    .Layer(74))
                .Build();
            library.Add(comp);
        }

        using var ms = new MemoryStream();
        new PcbLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (PcbLibrary)new PcbLibReader().Read(ms);

        Assert.Equal(3, readBack.Count);
        Assert.Equal("COMP1", readBack.Components[0].Name);
        Assert.Equal("COMP2", readBack.Components[1].Name);
        Assert.Equal("COMP3", readBack.Components[2].Name);
    }

    [Fact]
    public void PcbLib_FromScratch_EmptyLibrary()
    {
        var library = new PcbLibrary();

        // An empty library with no components
        using var ms = new MemoryStream();
        new PcbLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (PcbLibrary)new PcbLibReader().Read(ms);

        Assert.Equal(0, readBack.Count);
    }

    #endregion

    #region SchLib

    [Fact]
    public void SchLib_FromScratch_RoundTrips()
    {
        var library = new SchLibrary();

        var component = new SchComponent
        {
            Name = "RES_0805",
            Description = "0805 Resistor",
            PartCount = 1
        };

        component.AddPin(SchPin.Create("1")
            .WithName("A")
            .At(Coord.FromMils(-200), Coord.FromMils(0))
            .Length(Coord.FromMils(200))
            .Orient(PinOrientation.Right)
            .Electrical(PinElectricalType.Passive)
            .Build());

        component.AddPin(SchPin.Create("2")
            .WithName("B")
            .At(Coord.FromMils(200), Coord.FromMils(0))
            .Length(Coord.FromMils(200))
            .Orient(PinOrientation.Left)
            .Electrical(PinElectricalType.Passive)
            .Build());

        component.AddRectangle(new SchRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(-50)),
            Corner2 = new CoordPoint(Coord.FromMils(100), Coord.FromMils(50)),
            IsFilled = true,
            FillColor = 0xFFFF00,
            Color = 128
        });

        component.AddLine(new SchLine
        {
            Start = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(100), Coord.FromMils(0)),
            Color = 0
        });

        library.Add(component);

        // Round-trip
        using var ms = new MemoryStream();
        new SchLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (SchLibrary)new SchLibReader().Read(ms);

        Assert.Equal(1, readBack.Count);
        var comp = readBack.Components.First();
        Assert.Equal("RES_0805", comp.Name);
        Assert.Equal("0805 Resistor", comp.Description);
        Assert.Equal(2, comp.Pins.Count);
        Assert.Equal(1, comp.Rectangles.Count);
        Assert.Equal(1, comp.Lines.Count);

        var pin1 = (SchPin)comp.Pins[0];
        Assert.Equal("1", pin1.Designator);
        Assert.Equal("A", pin1.Name);
        Assert.Equal(PinElectricalType.Passive, pin1.ElectricalType);
    }

    [Fact]
    public void SchLib_FromScratch_ComplexComponent()
    {
        var library = new SchLibrary();

        var component = new SchComponent
        {
            Name = "OPAMP",
            Description = "Operational Amplifier",
            PartCount = 1
        };

        // Triangle body using polyline
        var polyline = SchPolyline.Create()
            .LineWidth(1)
            .Color(128)
            .From(Coord.FromMils(-100), Coord.FromMils(-150))
            .To(Coord.FromMils(-100), Coord.FromMils(150))
            .To(Coord.FromMils(200), Coord.FromMils(0))
            .To(Coord.FromMils(-100), Coord.FromMils(-150))
            .Build();
        component.AddPolyline(polyline);

        // Pins
        component.AddPin(SchPin.Create("3")
            .WithName("+")
            .At(Coord.FromMils(-300), Coord.FromMils(50))
            .Length(Coord.FromMils(200))
            .Orient(PinOrientation.Right)
            .Build());

        component.AddPin(SchPin.Create("2")
            .WithName("-")
            .At(Coord.FromMils(-300), Coord.FromMils(-50))
            .Length(Coord.FromMils(200))
            .Orient(PinOrientation.Right)
            .Build());

        component.AddPin(SchPin.Create("1")
            .WithName("OUT")
            .At(Coord.FromMils(400), Coord.FromMils(0))
            .Length(Coord.FromMils(200))
            .Orient(PinOrientation.Left)
            .Build());

        // Label
        component.AddLabel(new SchLabel
        {
            Text = "OPA",
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(200)),
            FontId = 1,
            Color = 0
        });

        library.Add(component);

        using var ms = new MemoryStream();
        new SchLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (SchLibrary)new SchLibReader().Read(ms);

        var comp = readBack.Components.First();
        Assert.Equal("OPAMP", comp.Name);
        Assert.Equal(3, comp.Pins.Count);
        Assert.Equal(1, comp.Polylines.Count);
        Assert.Equal(1, comp.Labels.Count);
    }

    #endregion

    #region SchDoc

    [Fact]
    public void SchDoc_FromScratch_RoundTrips()
    {
        var doc = new SchDocument();

        var comp = new SchComponent
        {
            Name = "R1",
            Description = "Resistor",
            PartCount = 1,
            Location = new CoordPoint(Coord.FromMils(500), Coord.FromMils(500))
        };

        comp.AddPin(SchPin.Create("1")
            .WithName("A")
            .At(Coord.FromMils(-200), Coord.FromMils(0))
            .Length(Coord.FromMils(200))
            .Orient(PinOrientation.Right)
            .Build());

        doc.AddComponent(comp);

        // Wire
        var wire = new SchWire();
        wire.AddVertex(new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)));
        wire.AddVertex(new CoordPoint(Coord.FromMils(300), Coord.FromMils(0)));
        wire.Color = 128;
        doc.AddPrimitive(wire);

        // Net label
        doc.AddPrimitive(new SchNetLabel
        {
            Text = "VCC",
            Location = new CoordPoint(Coord.FromMils(300), Coord.FromMils(0)),
            Color = 128
        });

        // Junction
        doc.AddPrimitive(new SchJunction
        {
            Location = new CoordPoint(Coord.FromMils(300), Coord.FromMils(0)),
            Color = 128
        });

        // Power object
        doc.AddPrimitive(new SchPowerObject
        {
            Text = "GND",
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(-200)),
            Style = PowerPortStyle.Bar,
            Color = 128
        });

        // Round-trip
        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.Components);
        Assert.Single(readBack.Wires);
        Assert.Single(readBack.NetLabels);
        Assert.Single(readBack.Junctions);
        Assert.Single(readBack.PowerObjects);

        Assert.Equal("R1", readBack.Components.First().Name);
        Assert.Equal("VCC", readBack.NetLabels.First().Text);
        Assert.Equal("GND", ((SchPowerObject)readBack.PowerObjects.First()).Text);
    }

    [Fact]
    public void SchDoc_FromScratch_PreservesHeaderParameters()
    {
        var doc = new SchDocument();
        doc.HeaderParameters = new Dictionary<string, string>
        {
            ["HEADER"] = "Protel for Windows - Schematic Capture Binary File Version 5.0",
            ["WEIGHT"] = "0",
            ["SHEETSTYLE"] = "4",
            ["SYSTEMFONT"] = "1",
            ["FONTNAME1"] = "Times New Roman",
            ["SIZE1"] = "10"
        };

        doc.AddPrimitive(new SchWire());

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.NotNull(readBack.HeaderParameters);
        Assert.Equal("4", readBack.HeaderParameters["SHEETSTYLE"]);
        Assert.Equal("Times New Roman", readBack.HeaderParameters["FONTNAME1"]);
    }

    [Fact]
    public void SchDoc_FromScratch_WithSheetSymbolsAndPorts()
    {
        var doc = new SchDocument();

        var sheetSymbol = new SchSheetSymbol
        {
            Location = new CoordPoint(Coord.FromMils(100), Coord.FromMils(100)),
            XSize = Coord.FromMils(400),
            YSize = Coord.FromMils(300),
            FileName = "SubSheet.SchDoc",
            SheetName = "SubSheet",
            Color = 128,
            AreaColor = 0xFFFF00
        };

        var entry = new SchSheetEntry
        {
            Name = "CLK",
            IoType = 1,
            Side = 0,
            DistanceFromTop = Coord.FromMils(50),
            Color = 128
        };
        sheetSymbol.AddEntry(entry);

        doc.AddPrimitive(sheetSymbol);

        var port = new SchPort
        {
            Name = "DATA_IN",
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(500)),
            IoType = 1,
            Style = 3,
            Width = Coord.FromMils(200),
            Height = Coord.FromMils(30),
            Color = 128
        };
        doc.AddPrimitive(port);

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.SheetSymbols);
        Assert.Equal("SubSheet.SchDoc", readBack.SheetSymbols.First().FileName);
        Assert.Single(readBack.SheetSymbols.First().Entries);
        Assert.Equal("CLK", readBack.SheetSymbols.First().Entries.First().Name);

        Assert.Single(readBack.Ports);
        Assert.Equal("DATA_IN", readBack.Ports.First().Name);
    }

    #endregion

    #region PcbDoc

    [Fact]
    public void PcbDoc_FromScratch_RoundTrips()
    {
        var doc = new PcbDocument();

        doc.AddTrack(new PcbTrack
        {
            Start = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(1000), Coord.FromMils(0)),
            Width = Coord.FromMils(10),
            Layer = 1
        });

        doc.AddPad(new PcbPad
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            SizeTop = new CoordPoint(Coord.FromMils(60), Coord.FromMils(60)),
            HoleSize = Coord.FromMils(30),
            Layer = 74,
            Designator = "1"
        });

        doc.AddVia(new PcbVia
        {
            Location = new CoordPoint(Coord.FromMils(500), Coord.FromMils(0)),
            Diameter = Coord.FromMils(40),
            HoleSize = Coord.FromMils(20)
        });

        doc.AddArc(new PcbArc
        {
            Center = new CoordPoint(Coord.FromMils(500), Coord.FromMils(500)),
            Radius = Coord.FromMils(200),
            StartAngle = 0,
            EndAngle = 360,
            Width = Coord.FromMils(10),
            Layer = 21
        });

        doc.AddComponent(new PcbComponent
        {
            Name = "U1",
            Description = "IC Package"
        });

        // Round-trip
        using var ms = new MemoryStream();
        new PcbDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new PcbDocReader().Read(ms);

        Assert.Single(readBack.Tracks);
        Assert.Single(readBack.Pads);
        Assert.Single(readBack.Vias);
        Assert.Single(readBack.Arcs);
        Assert.Single(readBack.Components);
        Assert.Equal("U1", readBack.Components.First().Name);
    }

    [Fact]
    public async Task PcbDoc_FromScratch_SaveAsync_Works()
    {
        var doc = new PcbDocument();
        doc.AddTrack(new PcbTrack
        {
            Start = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(100), Coord.FromMils(0)),
            Width = Coord.FromMils(10),
            Layer = 1
        });

        using var ms = new MemoryStream();
        await doc.SaveAsync(ms);

        ms.Position = 0;
        var readBack = new PcbDocReader().Read(ms);

        Assert.Single(readBack.Tracks);
    }

    [Fact]
    public void PcbDoc_FromScratch_WithNetsAndPolygons()
    {
        var doc = new PcbDocument();

        doc.AddNet(new PcbNet { Name = "GND" });
        doc.AddNet(new PcbNet { Name = "VCC" });

        var polygon = new PcbPolygon
        {
            Layer = 1,
            Net = "GND",
            Name = "CopperPour1",
            PolygonType = 1
        };
        polygon.AddVertex(new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)));
        polygon.AddVertex(new CoordPoint(Coord.FromMils(1000), Coord.FromMils(0)));
        polygon.AddVertex(new CoordPoint(Coord.FromMils(1000), Coord.FromMils(1000)));
        polygon.AddVertex(new CoordPoint(Coord.FromMils(0), Coord.FromMils(1000)));
        doc.AddPolygon(polygon);

        using var ms = new MemoryStream();
        new PcbDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new PcbDocReader().Read(ms);

        Assert.Equal(2, readBack.Nets.Count);
        Assert.Equal("GND", readBack.Nets[0].Name);
        Assert.Equal("VCC", readBack.Nets[1].Name);

        Assert.Single(readBack.Polygons);
        Assert.Equal("GND", readBack.Polygons[0].Net);
        Assert.Equal("CopperPour1", readBack.Polygons[0].Name);
        Assert.Equal(4, readBack.Polygons[0].Vertices.Count);
    }

    [Fact]
    public void PcbDoc_FromScratch_WithBoardParameters()
    {
        var doc = new PcbDocument();
        doc.BoardParameters = new Dictionary<string, string>
        {
            ["LAYER1NAME"] = "TopLayer",
            ["LAYER32NAME"] = "BottomLayer",
            ["BOARDTHICKNESS"] = "1600000"
        };

        doc.AddTrack(new PcbTrack
        {
            Start = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(100), Coord.FromMils(0)),
            Width = Coord.FromMils(10),
            Layer = 1
        });

        using var ms = new MemoryStream();
        new PcbDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new PcbDocReader().Read(ms);

        Assert.NotNull(readBack.BoardParameters);
        Assert.Equal("TopLayer", readBack.BoardParameters["LAYER1NAME"]);
        Assert.Equal("BottomLayer", readBack.BoardParameters["LAYER32NAME"]);
    }

    [Fact]
    public void PcbDoc_FromScratch_WithEmbeddedBoard()
    {
        var doc = new PcbDocument();
        doc.AddEmbeddedBoard(new PcbEmbeddedBoard
        {
            DocumentPath = @"C:\Designs\SubBoard.PcbDoc",
            Layer = 1,
            Rotation = 90.0,
            ColCount = 2,
            RowCount = 3,
            ColSpacing = Coord.FromMils(500),
            RowSpacing = Coord.FromMils(400),
            Scale = 1.0,
            IsViewport = true,
            ViewportTitle = "Panel View",
        });

        using var ms = new MemoryStream();
        new PcbDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new PcbDocReader().Read(ms);

        Assert.Single(readBack.EmbeddedBoards);
        var eb = readBack.EmbeddedBoards[0];
        Assert.Equal(@"C:\Designs\SubBoard.PcbDoc", eb.DocumentPath);
        Assert.Equal(1, eb.Layer);
        Assert.Equal(90.0, eb.Rotation);
        Assert.Equal(2, eb.ColCount);
        Assert.Equal(3, eb.RowCount);
        Assert.True(eb.IsViewport);
        Assert.Equal("Panel View", eb.ViewportTitle);
        // Coord values round-trip through "mil" format: verify within tolerance
        Assert.InRange(eb.ColSpacing.ToMils(), 499.9, 500.1);
        Assert.InRange(eb.RowSpacing.ToMils(), 399.9, 400.1);
    }

    [Fact]
    public void PcbLib_FromScratch_With3DModel()
    {
        var library = new PcbLibrary();

        var component = PcbComponent.Create("SOT23")
            .WithDescription("SOT-23 package")
            .AddPad(p => p
                .At(Coord.FromMils(0), Coord.FromMils(0))
                .Size(Coord.FromMils(40), Coord.FromMils(30))
                .WithDesignator("1")
                .Layer(74))
            .Build();

        library.Models.Add(new PcbModel
        {
            Id = "3D-MODEL-001",
            Name = "SOT23.step",
            IsEmbedded = true,
            ModelSource = "FromFile",
            RotationX = 0,
            RotationY = 0,
            RotationZ = 90.0,
            Dz = 50,
            StepData = "ISO-10303-21;\nHEADER;\nFILE_DESCRIPTION(('SOT23'),'2;1');\nENDSEC;\nDATA;\nENDSEC;\nEND-ISO-10303-21;"
        });

        library.Add(component);

        using var ms = new MemoryStream();
        new PcbLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (PcbLibrary)new PcbLibReader().Read(ms);

        Assert.Single(readBack.Components);
        Assert.Single(readBack.Models);
        Assert.Equal("3D-MODEL-001", readBack.Models[0].Id);
        Assert.Equal("SOT23.step", readBack.Models[0].Name);
        Assert.Contains("SOT23", readBack.Models[0].StepData);
    }

    #endregion

    #region SchLib Additional Primitives

    [Fact]
    public void SchLib_FromScratch_WithEllipse()
    {
        var library = new SchLibrary();
        var comp = new SchComponent { Name = "ELLIPSE_TEST", PartCount = 1 };
        comp.AddEllipse(new SchEllipse
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            RadiusX = Coord.FromMils(100),
            RadiusY = Coord.FromMils(50),
            Color = 0xFF0000,
            IsFilled = true,
            FillColor = 0x00FF00
        });
        library.Add(comp);

        using var ms = new MemoryStream();
        new SchLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (SchLibrary)new SchLibReader().Read(ms);

        Assert.Single(readBack.Components.First().Ellipses);
        var e = (SchEllipse)readBack.Components.First().Ellipses[0];
        Assert.Equal(100, e.RadiusX.ToMils(), 1);
        Assert.Equal(50, e.RadiusY.ToMils(), 1);
    }

    [Fact]
    public void SchLib_FromScratch_WithPie()
    {
        var library = new SchLibrary();
        var comp = new SchComponent { Name = "PIE_TEST", PartCount = 1 };
        comp.AddPie(new SchPie
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Radius = Coord.FromMils(100),
            StartAngle = 0,
            EndAngle = 90,
            Color = 128,
            IsFilled = true,
            FillColor = 0xFFFF00
        });
        library.Add(comp);

        using var ms = new MemoryStream();
        new SchLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (SchLibrary)new SchLibReader().Read(ms);

        Assert.Single(readBack.Components.First().Pies);
        var p = (SchPie)readBack.Components.First().Pies[0];
        Assert.Equal(100, p.Radius.ToMils(), 1);
        Assert.Equal(0, p.StartAngle, 1);
        Assert.Equal(90, p.EndAngle, 1);
    }

    [Fact]
    public void SchLib_FromScratch_WithTextFrame()
    {
        var library = new SchLibrary();
        var comp = new SchComponent { Name = "TF_TEST", PartCount = 1 };
        comp.AddTextFrame(new SchTextFrame
        {
            Corner1 = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Corner2 = new CoordPoint(Coord.FromMils(200), Coord.FromMils(100)),
            Text = "Hello World",
            FontId = 1,
            ShowBorder = true,
            IsFilled = true,
            FillColor = 0xFFFFFF
        });
        library.Add(comp);

        using var ms = new MemoryStream();
        new SchLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (SchLibrary)new SchLibReader().Read(ms);

        Assert.Single(readBack.Components.First().TextFrames);
        var tf = (SchTextFrame)readBack.Components.First().TextFrames[0];
        Assert.Equal("Hello World", tf.Text);
        Assert.True(tf.ShowBorder);
    }

    [Fact]
    public void SchLib_FromScratch_WithRoundedRectangle()
    {
        var library = new SchLibrary();
        var comp = new SchComponent { Name = "RR_TEST", PartCount = 1 };
        comp.AddRoundedRectangle(new SchRoundedRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(-50)),
            Corner2 = new CoordPoint(Coord.FromMils(100), Coord.FromMils(50)),
            CornerRadiusX = Coord.FromMils(20),
            CornerRadiusY = Coord.FromMils(20),
            Color = 128,
            IsFilled = true,
            FillColor = 0xC0C0C0
        });
        library.Add(comp);

        using var ms = new MemoryStream();
        new SchLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (SchLibrary)new SchLibReader().Read(ms);

        Assert.Single(readBack.Components.First().RoundedRectangles);
        var rr = (SchRoundedRectangle)readBack.Components.First().RoundedRectangles[0];
        Assert.Equal(20, rr.CornerRadiusX.ToMils(), 1);
    }

    [Fact]
    public void SchLib_FromScratch_WithPolygon()
    {
        var library = new SchLibrary();
        var comp = new SchComponent { Name = "POLY_TEST", PartCount = 1 };
        var polygon = SchPolygon.Create()
            .AddVertex(Coord.FromMils(0), Coord.FromMils(0))
            .AddVertex(Coord.FromMils(100), Coord.FromMils(0))
            .AddVertex(Coord.FromMils(50), Coord.FromMils(100))
            .Color(128)
            .Filled(true)
            .FillColor(0x00FF00)
            .Build();
        comp.AddPolygon(polygon);
        library.Add(comp);

        using var ms = new MemoryStream();
        new SchLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (SchLibrary)new SchLibReader().Read(ms);

        Assert.Single(readBack.Components.First().Polygons);
        Assert.Equal(3, readBack.Components.First().Polygons[0].Vertices.Count);
    }

    [Fact]
    public void SchLib_FromScratch_WithEllipticalArc()
    {
        var library = new SchLibrary();
        var comp = new SchComponent { Name = "EA_TEST", PartCount = 1 };
        comp.AddEllipticalArc(new SchEllipticalArc
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            PrimaryRadius = Coord.FromMils(100),
            SecondaryRadius = Coord.FromMils(50),
            StartAngle = 0,
            EndAngle = 180,
            Color = 128
        });
        library.Add(comp);

        using var ms = new MemoryStream();
        new SchLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (SchLibrary)new SchLibReader().Read(ms);

        Assert.Single(readBack.Components.First().EllipticalArcs);
        var ea = (SchEllipticalArc)readBack.Components.First().EllipticalArcs[0];
        Assert.Equal(100, ea.PrimaryRadius.ToMils(), 1);
        Assert.Equal(50, ea.SecondaryRadius.ToMils(), 1);
    }

    [Fact]
    public void SchLib_FromScratch_WithBezier()
    {
        var library = new SchLibrary();
        var comp = new SchComponent { Name = "BEZ_TEST", PartCount = 1 };
        var bezier = SchBezier.Create()
            .AddPoint(Coord.FromMils(0), Coord.FromMils(0))
            .AddPoint(Coord.FromMils(50), Coord.FromMils(100))
            .AddPoint(Coord.FromMils(100), Coord.FromMils(100))
            .AddPoint(Coord.FromMils(150), Coord.FromMils(0))
            .Color(128)
            .Build();
        comp.AddBezier(bezier);
        library.Add(comp);

        using var ms = new MemoryStream();
        new SchLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (SchLibrary)new SchLibReader().Read(ms);

        Assert.Single(readBack.Components.First().Beziers);
        Assert.Equal(4, readBack.Components.First().Beziers[0].ControlPoints.Count);
    }

    [Fact]
    public void SchLib_FromScratch_WithImage()
    {
        var library = new SchLibrary();
        var comp = new SchComponent { Name = "IMG_TEST", PartCount = 1 };
        comp.AddImage(new SchImage
        {
            Corner1 = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Corner2 = new CoordPoint(Coord.FromMils(200), Coord.FromMils(200)),
            EmbedImage = true,
            Filename = "test.bmp",
            KeepAspect = true,
            ImageData = new byte[] { 0x42, 0x4D, 0x00, 0x00, 0x00, 0x00 } // Minimal BMP header stub
        });
        library.Add(comp);

        using var ms = new MemoryStream();
        new SchLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (SchLibrary)new SchLibReader().Read(ms);

        Assert.Single(readBack.Components.First().Images);
        var img = (SchImage)readBack.Components.First().Images[0];
        Assert.Equal("test.bmp", img.Filename);
        Assert.True(img.EmbedImage);
        Assert.True(img.KeepAspect);
    }

    #endregion

    #region SchDoc Additional Primitives

    [Fact]
    public void SchDoc_FromScratch_WithEllipse()
    {
        var doc = new SchDocument();
        doc.AddPrimitive(new SchEllipse
        {
            Center = new CoordPoint(Coord.FromMils(500), Coord.FromMils(500)),
            RadiusX = Coord.FromMils(100),
            RadiusY = Coord.FromMils(60),
            Color = 128
        });

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.Ellipses);
        Assert.Equal(100, ((SchEllipse)readBack.Ellipses[0]).RadiusX.ToMils(), 1);
    }

    [Fact]
    public void SchDoc_FromScratch_WithPie()
    {
        var doc = new SchDocument();
        doc.AddPrimitive(new SchPie
        {
            Center = new CoordPoint(Coord.FromMils(500), Coord.FromMils(500)),
            Radius = Coord.FromMils(80),
            StartAngle = 45,
            EndAngle = 135,
            Color = 128
        });

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.Pies);
        Assert.Equal(80, ((SchPie)readBack.Pies[0]).Radius.ToMils(), 1);
    }

    [Fact]
    public void SchDoc_FromScratch_WithTextFrame()
    {
        var doc = new SchDocument();
        doc.AddPrimitive(new SchTextFrame
        {
            Corner1 = new CoordPoint(Coord.FromMils(100), Coord.FromMils(100)),
            Corner2 = new CoordPoint(Coord.FromMils(400), Coord.FromMils(300)),
            Text = "Design Notes",
            FontId = 1,
            ShowBorder = true
        });

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.TextFrames);
        Assert.Equal("Design Notes", ((SchTextFrame)readBack.TextFrames[0]).Text);
    }

    [Fact]
    public void SchDoc_FromScratch_WithRoundedRectangle()
    {
        var doc = new SchDocument();
        doc.AddPrimitive(new SchRoundedRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Corner2 = new CoordPoint(Coord.FromMils(300), Coord.FromMils(200)),
            CornerRadiusX = Coord.FromMils(20),
            CornerRadiusY = Coord.FromMils(20),
            Color = 128
        });

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.RoundedRectangles);
        Assert.Equal(20, ((SchRoundedRectangle)readBack.RoundedRectangles[0]).CornerRadiusX.ToMils(), 1);
    }

    [Fact]
    public void SchDoc_FromScratch_WithPolygon()
    {
        var doc = new SchDocument();
        var polygon = SchPolygon.Create()
            .AddVertex(Coord.FromMils(0), Coord.FromMils(0))
            .AddVertex(Coord.FromMils(200), Coord.FromMils(0))
            .AddVertex(Coord.FromMils(100), Coord.FromMils(200))
            .Color(128)
            .Build();
        doc.AddPrimitive(polygon);

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.Polygons);
        Assert.Equal(3, readBack.Polygons[0].Vertices.Count);
    }

    [Fact]
    public void SchDoc_FromScratch_WithEllipticalArc()
    {
        var doc = new SchDocument();
        doc.AddPrimitive(new SchEllipticalArc
        {
            Center = new CoordPoint(Coord.FromMils(500), Coord.FromMils(500)),
            PrimaryRadius = Coord.FromMils(100),
            SecondaryRadius = Coord.FromMils(60),
            StartAngle = 0,
            EndAngle = 270,
            Color = 128
        });

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.EllipticalArcs);
        Assert.Equal(100, ((SchEllipticalArc)readBack.EllipticalArcs[0]).PrimaryRadius.ToMils(), 1);
    }

    [Fact]
    public void SchDoc_FromScratch_WithBezier()
    {
        var doc = new SchDocument();
        var bezier = SchBezier.Create()
            .AddPoint(Coord.FromMils(0), Coord.FromMils(0))
            .AddPoint(Coord.FromMils(50), Coord.FromMils(100))
            .AddPoint(Coord.FromMils(150), Coord.FromMils(100))
            .AddPoint(Coord.FromMils(200), Coord.FromMils(0))
            .Color(128)
            .Build();
        doc.AddPrimitive(bezier);

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.Beziers);
        Assert.Equal(4, readBack.Beziers[0].ControlPoints.Count);
    }

    [Fact]
    public void SchDoc_FromScratch_WithBlanket()
    {
        var doc = new SchDocument();
        var blanket = new SchBlanket();
        blanket.AddVertex(new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)));
        blanket.AddVertex(new CoordPoint(Coord.FromMils(1000), Coord.FromMils(0)));
        blanket.AddVertex(new CoordPoint(Coord.FromMils(1000), Coord.FromMils(1000)));
        blanket.AddVertex(new CoordPoint(Coord.FromMils(0), Coord.FromMils(1000)));
        blanket.Color = 128;
        doc.AddPrimitive(blanket);

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.Blankets);
        Assert.Equal(4, readBack.Blankets[0].Vertices.Count);
    }

    [Fact]
    public void SchDoc_FromScratch_WithBus()
    {
        var doc = new SchDocument();
        var bus = new SchBus();
        bus.AddVertex(new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)));
        bus.AddVertex(new CoordPoint(Coord.FromMils(500), Coord.FromMils(0)));
        bus.Color = 128;
        doc.AddPrimitive(bus);

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.Buses);
        Assert.Equal(2, readBack.Buses[0].Vertices.Count);
    }

    [Fact]
    public void SchDoc_FromScratch_WithBusEntry()
    {
        var doc = new SchDocument();
        doc.AddPrimitive(new SchBusEntry
        {
            Location = new CoordPoint(Coord.FromMils(500), Coord.FromMils(500)),
            Corner = new CoordPoint(Coord.FromMils(550), Coord.FromMils(450)),
            Color = 128
        });

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.BusEntries);
        Assert.Equal(500, ((SchBusEntry)readBack.BusEntries[0]).Location.X.ToMils(), 1);
    }

    [Fact]
    public void SchDoc_FromScratch_WithNoErc()
    {
        var doc = new SchDocument();
        doc.AddPrimitive(new SchNoErc
        {
            Location = new CoordPoint(Coord.FromMils(300), Coord.FromMils(300)),
            IsActive = true,
            Color = 128
        });

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.NoErcs);
        Assert.True(((SchNoErc)readBack.NoErcs[0]).IsActive);
    }

    [Fact]
    public void SchDoc_FromScratch_WithParameterSet()
    {
        var doc = new SchDocument();
        var ps = new SchParameterSet
        {
            Location = new CoordPoint(Coord.FromMils(100), Coord.FromMils(100)),
            Name = "TestParamSet",
            Color = 128
        };
        ps.AddParameter(new SchParameter
        {
            Name = "Tolerance",
            Value = "5%",
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            FontId = 1,
            Color = 128,
            IsVisible = true
        });
        doc.AddPrimitive(ps);

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.ParameterSets);
        Assert.Equal("TestParamSet", ((SchParameterSet)readBack.ParameterSets[0]).Name);
    }

    [Fact]
    public void SchDoc_FromScratch_WithImage()
    {
        var doc = new SchDocument();
        doc.AddPrimitive(new SchImage
        {
            Corner1 = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Corner2 = new CoordPoint(Coord.FromMils(200), Coord.FromMils(200)),
            EmbedImage = true,
            Filename = "logo.bmp",
            KeepAspect = true,
            ImageData = new byte[] { 0x42, 0x4D, 0x00, 0x00, 0x00, 0x00 }
        });

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.Images);
        Assert.Equal("logo.bmp", ((SchImage)readBack.Images[0]).Filename);
    }

    [Fact]
    public void SchDoc_FromScratch_WithParameter()
    {
        var doc = new SchDocument();
        doc.AddPrimitive(new SchParameter
        {
            Name = "Author",
            Value = "Engineer",
            Location = new CoordPoint(Coord.FromMils(100), Coord.FromMils(100)),
            FontId = 1,
            Color = 128,
            IsVisible = true
        });

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.Parameters);
        Assert.Equal("Author", ((SchParameter)readBack.Parameters[0]).Name);
        Assert.Equal("Engineer", ((SchParameter)readBack.Parameters[0]).Value);
    }

    [Fact]
    public void SchDoc_FromScratch_WithSymbol()
    {
        var doc = new SchDocument();
        doc.AddPrimitive(new SchSymbol
        {
            Location = new CoordPoint(Coord.FromMils(500), Coord.FromMils(500)),
            SymbolType = 1,
            Color = 128
        });

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.Symbols);
        Assert.Equal(1, ((SchSymbol)readBack.Symbols[0]).SymbolType);
    }

    #endregion

    #region Implementation/MapDefiner

    [Fact]
    public void SchLib_Implementation_RoundTrips()
    {
        var library = new SchLibrary();
        var component = new SchComponent { Name = "TestComponent", Description = "Test" };

        var impl = new SchImplementation
        {
            Description = "Footprint model",
            ModelName = "QFP48",
            ModelType = "PCBLIB",
            IsCurrent = true,
            DataFileKinds = { "PCBLib" }
        };

        var md = new SchMapDefiner
        {
            DesignatorInterface = "1",
            DesignatorImplementations = { "1" },
            IsTrivial = true
        };
        impl.AddMapDefiner(md);
        component.AddImplementation(impl);
        library.Add(component);

        using var ms = new MemoryStream();
        new SchLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (SchLibrary)new SchLibReader().Read(ms);

        Assert.Equal(1, readBack.Components.Count);
        var rtComp = (SchComponent)readBack.Components[0];
        Assert.Equal(1, rtComp.Implementations.Count);

        var rtImpl = rtComp.Implementations[0];
        Assert.Equal("Footprint model", rtImpl.Description);
        Assert.Equal("QFP48", rtImpl.ModelName);
        Assert.Equal("PCBLIB", rtImpl.ModelType);
        Assert.True(rtImpl.IsCurrent);
        Assert.Equal(1, rtImpl.DataFileKinds.Count);
        Assert.Equal("PCBLib", rtImpl.DataFileKinds[0]);

        Assert.Equal(1, rtImpl.MapDefiners.Count);
        var rtMd = rtImpl.MapDefiners[0];
        Assert.Equal("1", rtMd.DesignatorInterface);
        Assert.Equal(1, rtMd.DesignatorImplementations.Count);
        Assert.Equal("1", rtMd.DesignatorImplementations[0]);
        Assert.True(rtMd.IsTrivial);
    }

    [Fact]
    public void SchDoc_Implementation_RoundTrips()
    {
        var doc = new SchDocument();
        var component = new SchComponent { Name = "U1", Description = "MCU" };

        var impl = new SchImplementation
        {
            Description = "PCB footprint",
            ModelName = "LQFP64",
            ModelType = "PCBLIB",
            IsCurrent = true,
            DataFileKinds = { "PCBLib", "Step" }
        };

        var md1 = new SchMapDefiner
        {
            DesignatorInterface = "A1",
            DesignatorImplementations = { "1", "2" },
            IsTrivial = false
        };
        var md2 = new SchMapDefiner
        {
            DesignatorInterface = "B1",
            DesignatorImplementations = { "3" },
            IsTrivial = true
        };
        impl.AddMapDefiner(md1);
        impl.AddMapDefiner(md2);
        component.AddImplementation(impl);
        doc.AddComponent(component);

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Equal(1, readBack.Components.Count);
        var rtComp = (SchComponent)readBack.Components[0];
        Assert.Equal(1, rtComp.Implementations.Count);

        var rtImpl = rtComp.Implementations[0];
        Assert.Equal("PCB footprint", rtImpl.Description);
        Assert.Equal("LQFP64", rtImpl.ModelName);
        Assert.Equal("PCBLIB", rtImpl.ModelType);
        Assert.True(rtImpl.IsCurrent);
        Assert.Equal(2, rtImpl.DataFileKinds.Count);
        Assert.Equal("PCBLib", rtImpl.DataFileKinds[0]);
        Assert.Equal("Step", rtImpl.DataFileKinds[1]);

        Assert.Equal(2, rtImpl.MapDefiners.Count);
        Assert.Equal("A1", rtImpl.MapDefiners[0].DesignatorInterface);
        Assert.Equal(2, rtImpl.MapDefiners[0].DesignatorImplementations.Count);
        Assert.False(rtImpl.MapDefiners[0].IsTrivial);
        Assert.Equal("B1", rtImpl.MapDefiners[1].DesignatorInterface);
        Assert.True(rtImpl.MapDefiners[1].IsTrivial);
    }

    [Fact]
    public void SchLib_MultipleImplementations_RoundTrips()
    {
        var library = new SchLibrary();
        var component = new SchComponent { Name = "R1", Description = "Resistor" };

        var pcbImpl = new SchImplementation
        {
            ModelName = "0402",
            ModelType = "PCBLIB",
            IsCurrent = true
        };
        var simImpl = new SchImplementation
        {
            ModelName = "RES_IDEAL",
            ModelType = "SIM",
            IsCurrent = false,
            Description = "Simulation model"
        };
        component.AddImplementation(pcbImpl);
        component.AddImplementation(simImpl);
        library.Add(component);

        using var ms = new MemoryStream();
        new SchLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (SchLibrary)new SchLibReader().Read(ms);

        var rtComp = (SchComponent)readBack.Components[0];
        Assert.Equal(2, rtComp.Implementations.Count);
        Assert.Equal("0402", rtComp.Implementations[0].ModelName);
        Assert.Equal("PCBLIB", rtComp.Implementations[0].ModelType);
        Assert.True(rtComp.Implementations[0].IsCurrent);
        Assert.Equal("RES_IDEAL", rtComp.Implementations[1].ModelName);
        Assert.Equal("SIM", rtComp.Implementations[1].ModelType);
        Assert.False(rtComp.Implementations[1].IsCurrent);
        Assert.Equal("Simulation model", rtComp.Implementations[1].Description);
    }

    #endregion

    #region PcbDoc Advanced Features

    [Fact]
    public void PcbDoc_Rules_RoundTrip()
    {
        var document = new PcbDocument();
        var rule = new PcbRule
        {
            Name = "Clearance_1",
            RuleKind = "Clearance",
            Comment = "Default clearance",
            Enabled = true,
            Priority = 1,
            UniqueId = "ABCD1234",
            Scope1Expression = "All",
            Scope2Expression = "All",
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["NAME"] = "Clearance_1",
                ["RULEKIND"] = "Clearance",
                ["COMMENT"] = "Default clearance",
                ["ENABLED"] = "TRUE",
                ["PRIORITY"] = "1",
                ["UNIQUEID"] = "ABCD1234",
                ["SCOPE1EXPRESSION"] = "All",
                ["SCOPE2EXPRESSION"] = "All",
                ["GAP"] = "10mil"
            }
        };
        document.AddRule(rule);

        using var ms = new MemoryStream();
        new PcbDocWriter().Write(document, ms);
        ms.Position = 0;
        var readBack = new PcbDocReader().Read(ms);

        Assert.Equal(1, readBack.Rules.Count);
        Assert.Equal("Clearance_1", readBack.Rules[0].Name);
        Assert.Equal("Clearance", readBack.Rules[0].RuleKind);
        Assert.Equal("Default clearance", readBack.Rules[0].Comment);
        Assert.True(readBack.Rules[0].Enabled);
        Assert.Equal(1, readBack.Rules[0].Priority);
        Assert.Equal("10mil", readBack.Rules[0].Parameters["GAP"]);
    }

    [Fact]
    public void PcbDoc_Classes_RoundTrip()
    {
        var document = new PcbDocument();
        var objectClass = new PcbObjectClass
        {
            Name = "PowerNets",
            Kind = "Net",
            SuperClass = "",
            Enabled = true,
            UniqueId = "EFGH5678",
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["NAME"] = "PowerNets",
                ["KIND"] = "Net",
                ["SUPERCLASS"] = "",
                ["ENABLED"] = "TRUE",
                ["UNIQUEID"] = "EFGH5678",
                ["MEMBER0"] = "VCC",
                ["MEMBER1"] = "GND",
                ["MEMBER2"] = "VDD"
            }
        };
        objectClass.Members.Add("VCC");
        objectClass.Members.Add("GND");
        objectClass.Members.Add("VDD");
        document.AddClass(objectClass);

        using var ms = new MemoryStream();
        new PcbDocWriter().Write(document, ms);
        ms.Position = 0;
        var readBack = new PcbDocReader().Read(ms);

        Assert.Equal(1, readBack.Classes.Count);
        Assert.Equal("PowerNets", readBack.Classes[0].Name);
        Assert.Equal("Net", readBack.Classes[0].Kind);
        Assert.Equal(3, readBack.Classes[0].Members.Count);
        Assert.Equal("VCC", readBack.Classes[0].Members[0]);
        Assert.Equal("GND", readBack.Classes[0].Members[1]);
        Assert.Equal("VDD", readBack.Classes[0].Members[2]);
    }

    [Fact]
    public void PcbDoc_DifferentialPairs_RoundTrip()
    {
        var document = new PcbDocument();
        var pair = new PcbDifferentialPair
        {
            Name = "USB_DP",
            PositiveNetName = "USB_D+",
            NegativeNetName = "USB_D-",
            UniqueId = "IJKL9012",
            Enabled = true,
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["NAME"] = "USB_DP",
                ["POSITIVENETNAME"] = "USB_D+",
                ["NEGATIVENETNAME"] = "USB_D-",
                ["UNIQUEID"] = "IJKL9012",
                ["ENABLED"] = "TRUE"
            }
        };
        document.AddDifferentialPair(pair);

        using var ms = new MemoryStream();
        new PcbDocWriter().Write(document, ms);
        ms.Position = 0;
        var readBack = new PcbDocReader().Read(ms);

        Assert.Equal(1, readBack.DifferentialPairs.Count);
        Assert.Equal("USB_DP", readBack.DifferentialPairs[0].Name);
        Assert.Equal("USB_D+", readBack.DifferentialPairs[0].PositiveNetName);
        Assert.Equal("USB_D-", readBack.DifferentialPairs[0].NegativeNetName);
    }

    [Fact]
    public void PcbDoc_Rooms_RoundTrip()
    {
        var document = new PcbDocument();
        var room = new PcbRoom
        {
            Name = "PowerSection",
            UniqueId = "MNOP3456",
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["NAME"] = "PowerSection",
                ["UNIQUEID"] = "MNOP3456"
            }
        };
        document.AddRoom(room);

        using var ms = new MemoryStream();
        new PcbDocWriter().Write(document, ms);
        ms.Position = 0;
        var readBack = new PcbDocReader().Read(ms);

        Assert.Equal(1, readBack.Rooms.Count);
        Assert.Equal("PowerSection", readBack.Rooms[0].Name);
        Assert.Equal("MNOP3456", readBack.Rooms[0].UniqueId);
    }

    #endregion

    #region ToParameters Sync Tests

    [Fact]
    public void PcbDoc_RuleToParameters_SyncsTypedProperties()
    {
        // Create a rule from parameters
        var rule = new PcbRule
        {
            Name = "OriginalName",
            RuleKind = "Clearance",
            Enabled = true,
            Priority = 1,
            UniqueId = "ABC123"
        };
        rule.Parameters["NAME"] = "OriginalName";
        rule.Parameters["RULEKIND"] = "Clearance";
        rule.Parameters["GAP"] = "100000"; // rule-specific param

        // Modify typed properties
        rule.Name = "ModifiedName";
        rule.Priority = 5;
        rule.Enabled = false;
        rule.Scope1Expression = "InNet('VCC')";

        // Sync and verify
        var result = rule.ToParameters();
        Assert.Equal("ModifiedName", result["NAME"]);
        Assert.Equal("5", result["PRIORITY"]);
        Assert.Equal("FALSE", result["ENABLED"]);
        Assert.Equal("InNet('VCC')", result["SCOPE1EXPRESSION"]);
        Assert.Equal("100000", result["GAP"]); // rule-specific param preserved
    }

    [Fact]
    public void PcbDoc_RuleToParameters_RoundTrip()
    {
        var doc = new PcbDocument();
        var rule = new PcbRule
        {
            Name = "TestClearance",
            RuleKind = "Clearance",
            Enabled = true,
            Priority = 2,
            Comment = "Test rule",
            UniqueId = "RULE001"
        };
        doc.AddRule(rule);

        // Modify typed property after creation
        rule.Name = "ModifiedClearance";

        // Write and read back
        using var ms = new MemoryStream();
        new PcbDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new PcbDocReader().Read(ms);

        Assert.Equal(1, readBack.Rules.Count);
        Assert.Equal("ModifiedClearance", readBack.Rules[0].Name);
    }

    #endregion
}
