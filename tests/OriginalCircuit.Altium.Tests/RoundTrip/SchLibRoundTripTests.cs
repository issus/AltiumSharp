using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Serialization.Readers;
using OriginalCircuit.Altium.Serialization.Writers;

namespace OriginalCircuit.Altium.Tests.RoundTrip;

/// <summary>
/// Tests for verifying SchLib round-trip serialization (write → read → compare).
/// </summary>
public sealed class SchLibRoundTripTests
{
    private static SchLibrary RoundTrip(SchLibrary original)
    {
        using var ms = new MemoryStream();
        new SchLibWriter().Write(original, ms);
        ms.Position = 0;
        return (SchLibrary)new SchLibReader().Read(ms);
    }

    [Fact]
    public void Pin_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };

        var pin = SchPin.Create("1")
            .WithName("INPUT")
            .At(Coord.FromMils(200), Coord.FromMils(-100))
            .Length(Coord.FromMils(300))
            .Orient(PinOrientation.Left)
            .Electrical(PinElectricalType.Input)
            .Build();
        pin.ShowName = false;
        pin.ShowDesignator = true;
        pin.Description = "Test pin";
        pin.SymbolInnerEdge = 2;
        pin.SymbolOuterEdge = 3;
        pin.SymbolInside = 1;
        pin.SymbolOutside = 2;
        component.AddPin(pin);
        original.Add(component);

        var readBack = RoundTrip(original);
        var readPin = (SchPin)readBack.Components.First().Pins.First();

        Assert.Equal("1", readPin.Designator);
        Assert.Equal("INPUT", readPin.Name);
        Assert.Equal(200, readPin.Location.X.ToMils(), 1);
        Assert.Equal(-100, readPin.Location.Y.ToMils(), 1);
        Assert.Equal(300, readPin.Length.ToMils(), 1);
        Assert.Equal(PinOrientation.Left, readPin.Orientation);
        Assert.Equal(PinElectricalType.Input, readPin.ElectricalType);
        Assert.False(readPin.ShowName);
        Assert.True(readPin.ShowDesignator);
        Assert.Equal("Test pin", readPin.Description);
        Assert.Equal(2, readPin.SymbolInnerEdge);
        Assert.Equal(3, readPin.SymbolOuterEdge);
        Assert.Equal(1, readPin.SymbolInside);
        Assert.Equal(2, readPin.SymbolOutside);
    }

    [Fact]
    public void Line_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };

        component.AddLine(new SchLine
        {
            Start = new CoordPoint(Coord.FromMils(-100), Coord.FromMils(50)),
            End = new CoordPoint(Coord.FromMils(200), Coord.FromMils(-75)),
            Width = Coord.FromMils(2), // Medium
            Color = 255, // Red
            UniqueId = "ABCD1234"
        });
        original.Add(component);

        var readBack = RoundTrip(original);
        var line = (SchLine)readBack.Components.First().Lines.First();

        Assert.Equal(-100, line.Start.X.ToMils(), 1);
        Assert.Equal(50, line.Start.Y.ToMils(), 1);
        Assert.Equal(200, line.End.X.ToMils(), 1);
        Assert.Equal(-75, line.End.Y.ToMils(), 1);
        Assert.Equal(2, line.Width.ToMils(), 1); // Medium width
        Assert.Equal(255, line.Color);
        Assert.Equal("ABCD1234", line.UniqueId);
    }

    [Fact]
    public void Rectangle_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };

        component.AddRectangle(new SchRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-50)),
            Corner2 = new CoordPoint(Coord.FromMils(150), Coord.FromMils(100)),
            LineWidth = Coord.FromMils(4), // Large
            IsFilled = true,
            Color = 128,
            FillColor = 16777215,
            UniqueId = "RECT0001"
        });
        original.Add(component);

        var readBack = RoundTrip(original);
        var rect = (SchRectangle)readBack.Components.First().Rectangles.First();

        Assert.Equal(-50, rect.Corner1.X.ToMils(), 1);
        Assert.Equal(-50, rect.Corner1.Y.ToMils(), 1);
        Assert.Equal(150, rect.Corner2.X.ToMils(), 1);
        Assert.Equal(100, rect.Corner2.Y.ToMils(), 1);
        Assert.Equal(4, rect.LineWidth.ToMils(), 1); // Large width
        Assert.True(rect.IsFilled);
        Assert.Equal(128, rect.Color);
        Assert.Equal(16777215, rect.FillColor);
        Assert.Equal("RECT0001", rect.UniqueId);
    }

    [Fact]
    public void Label_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };

        component.AddLabel(new SchLabel
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(100)),
            Text = "Hello World",
            FontId = 2,
            Color = 32768, // Green
            Rotation = 90,
            Justification = SchTextJustification.BottomRight,
            IsMirrored = true,
            IsHidden = false,
            UniqueId = "LBL00001"
        });
        original.Add(component);

        var readBack = RoundTrip(original);
        var label = (SchLabel)readBack.Components.First().Labels.First();

        Assert.Equal(0, label.Location.X.ToMils(), 1);
        Assert.Equal(100, label.Location.Y.ToMils(), 1);
        Assert.Equal("Hello World", label.Text);
        Assert.Equal(2, label.FontId);
        Assert.Equal(32768, label.Color);
        Assert.Equal(90, label.Rotation, 1);
        Assert.Equal(SchTextJustification.BottomRight, label.Justification);
        Assert.True(label.IsMirrored);
        Assert.False(label.IsHidden);
        Assert.Equal("LBL00001", label.UniqueId);
    }

    [Fact]
    public void Arc_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };

        component.AddArc(new SchArc
        {
            Center = new CoordPoint(Coord.FromMils(50), Coord.FromMils(50)),
            Radius = Coord.FromMils(100),
            StartAngle = 45,
            EndAngle = 270,
            LineWidth = 2, // Large
            Color = 16711680,
            UniqueId = "ARC00001"
        });
        original.Add(component);

        var readBack = RoundTrip(original);
        var arc = (SchArc)readBack.Components.First().Arcs.First();

        Assert.Equal(50, arc.Center.X.ToMils(), 1);
        Assert.Equal(50, arc.Center.Y.ToMils(), 1);
        Assert.Equal(100, arc.Radius.ToMils(), 1);
        Assert.Equal(45, arc.StartAngle, 1);
        Assert.Equal(270, arc.EndAngle, 1);
        Assert.Equal(2, arc.LineWidth);
        Assert.Equal(16711680, arc.Color);
        Assert.Equal("ARC00001", arc.UniqueId);
    }

    [Fact]
    public void Polygon_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };

        var polygon = SchPolygon.Create()
            .AddVertex(Coord.FromMils(0), Coord.FromMils(0))
            .AddVertex(Coord.FromMils(100), Coord.FromMils(0))
            .AddVertex(Coord.FromMils(50), Coord.FromMils(100))
            .LineWidth(1)
            .Color(255)
            .FillColor(65535)
            .Filled(true)
            .Transparent(false)
            .Build();
        polygon.UniqueId = "POLY0001";
        component.AddPolygon(polygon);
        original.Add(component);

        var readBack = RoundTrip(original);
        var poly = (SchPolygon)readBack.Components.First().Polygons.First();

        Assert.Equal(3, poly.Vertices.Count);
        Assert.Equal(0, poly.Vertices[0].X.ToMils(), 1);
        Assert.Equal(100, poly.Vertices[1].X.ToMils(), 1);
        Assert.Equal(50, poly.Vertices[2].X.ToMils(), 1);
        Assert.Equal(1, poly.LineWidth);
        Assert.Equal(255, poly.Color);
        Assert.Equal(65535, poly.FillColor);
        Assert.True(poly.IsFilled);
        Assert.False(poly.IsTransparent);
        Assert.Equal("POLY0001", poly.UniqueId);
    }

    [Fact]
    public void Polyline_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };

        var polyline = SchPolyline.Create()
            .AddVertex(Coord.FromMils(0), Coord.FromMils(0))
            .AddVertex(Coord.FromMils(100), Coord.FromMils(50))
            .LineWidth(2)
            .Color(128)
            .Style(SchLineStyle.Dashed)
            .Build();
        polyline.UniqueId = "PLIN0001";
        component.AddPolyline(polyline);
        original.Add(component);

        var readBack = RoundTrip(original);
        var pline = (SchPolyline)readBack.Components.First().Polylines.First();

        Assert.Equal(2, pline.Vertices.Count);
        Assert.Equal(0, pline.Vertices[0].X.ToMils(), 1);
        Assert.Equal(100, pline.Vertices[1].X.ToMils(), 1);
        Assert.Equal(2, pline.LineWidth);
        Assert.Equal(128, pline.Color);
        Assert.Equal(SchLineStyle.Dashed, pline.LineStyle);
        Assert.Equal("PLIN0001", pline.UniqueId);
    }

    [Fact]
    public void Bezier_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };

        var bezier = SchBezier.Create()
            .AddPoint(Coord.FromMils(0), Coord.FromMils(0))
            .AddPoint(Coord.FromMils(30), Coord.FromMils(80))
            .AddPoint(Coord.FromMils(70), Coord.FromMils(80))
            .AddPoint(Coord.FromMils(100), Coord.FromMils(0))
            .LineWidth(1)
            .Color(192)
            .Build();
        bezier.UniqueId = "BEZ00001";
        component.AddBezier(bezier);
        original.Add(component);

        var readBack = RoundTrip(original);
        var bez = (SchBezier)readBack.Components.First().Beziers.First();

        Assert.Equal(4, bez.ControlPoints.Count);
        Assert.Equal(0, bez.ControlPoints[0].X.ToMils(), 1);
        Assert.Equal(100, bez.ControlPoints[3].X.ToMils(), 1);
        Assert.Equal(1, bez.LineWidth);
        Assert.Equal(192, bez.Color);
        Assert.Equal("BEZ00001", bez.UniqueId);
    }

    [Fact]
    public void Ellipse_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };

        component.AddEllipse(new SchEllipse
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            RadiusX = Coord.FromMils(80),
            RadiusY = Coord.FromMils(50),
            LineWidth = 1,
            Color = 255,
            FillColor = 65280,
            IsFilled = true,
            IsTransparent = true,
            UniqueId = "ELL00001"
        });
        original.Add(component);

        var readBack = RoundTrip(original);
        var ellipse = (SchEllipse)((SchComponent)readBack.Components.First()).Ellipses.First();

        Assert.Equal(0, ellipse.Center.X.ToMils(), 1);
        Assert.Equal(80, ellipse.RadiusX.ToMils(), 1);
        Assert.Equal(50, ellipse.RadiusY.ToMils(), 1);
        Assert.Equal(1, ellipse.LineWidth);
        Assert.Equal(255, ellipse.Color);
        Assert.Equal(65280, ellipse.FillColor);
        Assert.True(ellipse.IsFilled);
        Assert.True(ellipse.IsTransparent);
        Assert.Equal("ELL00001", ellipse.UniqueId);
    }

    [Fact]
    public void RoundedRectangle_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };

        component.AddRoundedRectangle(new SchRoundedRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(-50), Coord.FromMils(-30)),
            Corner2 = new CoordPoint(Coord.FromMils(50), Coord.FromMils(30)),
            CornerRadiusX = Coord.FromMils(10),
            CornerRadiusY = Coord.FromMils(10),
            LineWidth = 2,
            Color = 128,
            FillColor = 12632256,
            IsFilled = true,
            IsTransparent = false,
            UniqueId = "RREC0001"
        });
        original.Add(component);

        var readBack = RoundTrip(original);
        var rrect = (SchRoundedRectangle)((SchComponent)readBack.Components.First()).RoundedRectangles.First();

        Assert.Equal(-50, rrect.Corner1.X.ToMils(), 1);
        Assert.Equal(50, rrect.Corner2.X.ToMils(), 1);
        Assert.Equal(10, rrect.CornerRadiusX.ToMils(), 1);
        Assert.Equal(10, rrect.CornerRadiusY.ToMils(), 1);
        Assert.Equal(2, rrect.LineWidth);
        Assert.Equal(128, rrect.Color);
        Assert.Equal(12632256, rrect.FillColor);
        Assert.True(rrect.IsFilled);
        Assert.False(rrect.IsTransparent);
        Assert.Equal("RREC0001", rrect.UniqueId);
    }

    [Fact]
    public void Pie_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };

        component.AddPie(new SchPie
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Radius = Coord.FromMils(75),
            StartAngle = 30,
            EndAngle = 120,
            LineWidth = 1,
            Color = 255,
            FillColor = 16711680,
            IsFilled = true,
            IsTransparent = false,
            UniqueId = "PIE00001"
        });
        original.Add(component);

        var readBack = RoundTrip(original);
        var pie = (SchPie)((SchComponent)readBack.Components.First()).Pies.First();

        Assert.Equal(0, pie.Center.X.ToMils(), 1);
        Assert.Equal(75, pie.Radius.ToMils(), 1);
        Assert.Equal(30, pie.StartAngle, 1);
        Assert.Equal(120, pie.EndAngle, 1);
        Assert.Equal(1, pie.LineWidth);
        Assert.Equal(255, pie.Color);
        Assert.Equal(16711680, pie.FillColor);
        Assert.True(pie.IsFilled);
        Assert.False(pie.IsTransparent);
        Assert.Equal("PIE00001", pie.UniqueId);
    }

    [Fact]
    public void EllipticalArc_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };

        component.AddEllipticalArc(new SchEllipticalArc
        {
            Center = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            PrimaryRadius = Coord.FromMils(100),
            SecondaryRadius = Coord.FromMils(60),
            StartAngle = 0,
            EndAngle = 180,
            LineWidth = Coord.FromMils(2),
            Color = 32768,
            UniqueId = "EARC0001"
        });
        original.Add(component);

        var readBack = RoundTrip(original);
        var earc = (SchEllipticalArc)((SchComponent)readBack.Components.First()).EllipticalArcs.First();

        Assert.Equal(0, earc.Center.X.ToMils(), 1);
        Assert.Equal(100, earc.PrimaryRadius.ToMils(), 1);
        Assert.Equal(60, earc.SecondaryRadius.ToMils(), 1);
        Assert.Equal(0, earc.StartAngle, 1);
        Assert.Equal(180, earc.EndAngle, 1);
        Assert.Equal(2, earc.LineWidth.ToMils(), 1);
        Assert.Equal(32768, earc.Color);
        Assert.Equal("EARC0001", earc.UniqueId);
    }

    [Fact]
    public void Parameter_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };

        component.AddParameter(new SchParameter
        {
            Location = new CoordPoint(Coord.FromMils(0), Coord.FromMils(-200)),
            Name = "Value",
            Value = "10k",
            FontId = 1,
            Color = 0,
            ParamType = 1,
            Orientation = 0,
            Justification = SchTextJustification.BottomLeft,
            ShowName = false,
            IsMirrored = false,
            IsVisible = true,
            IsReadOnly = true,
            UniqueId = "PARM0001"
        });
        original.Add(component);

        var readBack = RoundTrip(original);
        var param = (SchParameter)readBack.Components.First().Parameters.First();

        Assert.Equal(0, param.Location.X.ToMils(), 1);
        Assert.Equal(-200, param.Location.Y.ToMils(), 1);
        Assert.Equal("Value", param.Name);
        Assert.Equal("10k", param.Value);
        Assert.Equal(1, param.FontId);
        Assert.Equal(0, param.Color);
        Assert.Equal(1, param.ParamType);
        Assert.Equal(0, param.Orientation);
        Assert.Equal(SchTextJustification.BottomLeft, param.Justification);
        Assert.False(param.ShowName);
        Assert.False(param.IsMirrored);
        Assert.True(param.IsVisible);
        Assert.True(param.IsReadOnly);
        Assert.Equal("PARM0001", param.UniqueId);
    }

    [Fact]
    public void NetLabel_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };
        component.AddNetLabel(new SchNetLabel
        {
            Location = new CoordPoint(Coord.FromMils(100), Coord.FromMils(200)),
            Text = "VCC",
            Color = 255,
            FontId = 2,
            Orientation = 1,
            Justification = SchTextJustification.MiddleCenter,
            IsMirrored = true,
            UniqueId = "NL000001"
        });
        original.Add(component);

        var readBack = RoundTrip(original);
        var netLabel = (SchNetLabel)readBack.Components.First().NetLabels.First();

        Assert.Equal(100, netLabel.Location.X.ToMils(), 1);
        Assert.Equal(200, netLabel.Location.Y.ToMils(), 1);
        Assert.Equal("VCC", netLabel.Text);
        Assert.Equal(255, netLabel.Color);
        Assert.Equal(2, netLabel.FontId);
        Assert.Equal(1, netLabel.Orientation);
        Assert.Equal(SchTextJustification.MiddleCenter, netLabel.Justification);
        Assert.True(netLabel.IsMirrored);
        Assert.Equal("NL000001", netLabel.UniqueId);
    }

    [Fact]
    public void Junction_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };
        component.AddJunction(new SchJunction
        {
            Location = new CoordPoint(Coord.FromMils(300), Coord.FromMils(400)),
            Color = 128,
            Locked = true,
            UniqueId = "JN000001"
        });
        original.Add(component);

        var readBack = RoundTrip(original);
        var junction = (SchJunction)readBack.Components.First().Junctions.First();

        Assert.Equal(300, junction.Location.X.ToMils(), 1);
        Assert.Equal(400, junction.Location.Y.ToMils(), 1);
        Assert.Equal(128, junction.Color);
        Assert.True(junction.Locked);
        Assert.Equal("JN000001", junction.UniqueId);
    }

    [Fact]
    public void TextFrame_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };
        component.AddTextFrame(new SchTextFrame
        {
            Corner1 = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Corner2 = new CoordPoint(Coord.FromMils(500), Coord.FromMils(300)),
            Text = "Hello World",
            TextColor = 255,
            FillColor = 16777215,
            FontId = 1,
            Orientation = 0,
            Alignment = SchTextJustification.MiddleCenter,
            IsFilled = true,
            ShowBorder = true,
            WordWrap = true,
            ClipToRect = false,
            UniqueId = "TF000001"
        });
        original.Add(component);

        var readBack = RoundTrip(original);
        var tf = (SchTextFrame)((SchComponent)readBack.Components.First()).TextFrames.First();

        Assert.Equal(0, tf.Corner1.X.ToMils(), 1);
        Assert.Equal(0, tf.Corner1.Y.ToMils(), 1);
        Assert.Equal(500, tf.Corner2.X.ToMils(), 1);
        Assert.Equal(300, tf.Corner2.Y.ToMils(), 1);
        Assert.Equal("Hello World", tf.Text);
        Assert.Equal(255, tf.TextColor);
        Assert.Equal(16777215, tf.FillColor);
        Assert.Equal(1, tf.FontId);
        Assert.Equal(SchTextJustification.MiddleCenter, tf.Alignment);
        Assert.True(tf.IsFilled);
        Assert.True(tf.ShowBorder);
        Assert.True(tf.WordWrap);
        Assert.False(tf.ClipToRect);
        Assert.Equal("TF000001", tf.UniqueId);
    }

    [Fact]
    public void Image_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };
        component.AddImage(new SchImage
        {
            Corner1 = new CoordPoint(Coord.FromMils(10), Coord.FromMils(20)),
            Corner2 = new CoordPoint(Coord.FromMils(200), Coord.FromMils(150)),
            BorderColor = 0,
            LineWidth = 1,
            KeepAspect = true,
            EmbedImage = false,
            Filename = "test.bmp",
            UniqueId = "IM000001"
        });
        original.Add(component);

        var readBack = RoundTrip(original);
        var img = (SchImage)readBack.Components.First().Images.First();

        Assert.Equal(10, img.Corner1.X.ToMils(), 1);
        Assert.Equal(20, img.Corner1.Y.ToMils(), 1);
        Assert.Equal(200, img.Corner2.X.ToMils(), 1);
        Assert.Equal(150, img.Corner2.Y.ToMils(), 1);
        Assert.Equal(0, img.BorderColor);
        Assert.Equal(1, img.LineWidth);
        Assert.True(img.KeepAspect);
        Assert.False(img.EmbedImage);
        Assert.Equal("test.bmp", img.Filename);
        Assert.Equal("IM000001", img.UniqueId);
    }

    [Fact]
    public void Symbol_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };
        component.AddSymbol(new SchSymbol
        {
            Location = new CoordPoint(Coord.FromMils(50), Coord.FromMils(75)),
            Color = 128,
            SymbolType = 1,
            IsMirrored = true,
            Orientation = 2,
            LineWidth = 1,
            ScaleFactor = 2,
            UniqueId = "SY000001"
        });
        original.Add(component);

        var readBack = RoundTrip(original);
        var sym = (SchSymbol)((SchComponent)readBack.Components.First()).Symbols.First();

        Assert.Equal(50, sym.Location.X.ToMils(), 1);
        Assert.Equal(75, sym.Location.Y.ToMils(), 1);
        Assert.Equal(128, sym.Color);
        Assert.Equal(1, sym.SymbolType);
        Assert.True(sym.IsMirrored);
        Assert.Equal(2, sym.Orientation);
        Assert.Equal(1, sym.LineWidth);
        Assert.Equal(2, sym.ScaleFactor);
        Assert.Equal("SY000001", sym.UniqueId);
    }

    [Fact]
    public void PowerObject_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent { Name = "TEST", PartCount = 1 };
        component.AddPowerObject(new SchPowerObject
        {
            Location = new CoordPoint(Coord.FromMils(100), Coord.FromMils(100)),
            Color = 128,
            Text = "GND",
            Style = PowerPortStyle.PowerGround,
            Rotation = 90,
            ShowNetName = true,
            IsCrossSheetConnector = false,
            FontId = 1,
            UniqueId = "PO000001"
        });
        original.Add(component);

        var readBack = RoundTrip(original);
        var po = (SchPowerObject)((SchComponent)readBack.Components.First()).PowerObjects.First();

        Assert.Equal(100, po.Location.X.ToMils(), 1);
        Assert.Equal(100, po.Location.Y.ToMils(), 1);
        Assert.Equal(128, po.Color);
        Assert.Equal("GND", po.Text);
        Assert.Equal(PowerPortStyle.PowerGround, po.Style);
        Assert.Equal(90, po.Rotation, 1);
        Assert.True(po.ShowNetName);
        Assert.False(po.IsCrossSheetConnector);
        Assert.Equal(1, po.FontId);
        Assert.Equal("PO000001", po.UniqueId);
    }

    [Fact]
    public void Component_PreservesAllProperties()
    {
        var original = new SchLibrary();
        var component = new SchComponent
        {
            Name = "RESISTOR",
            Description = "Standard resistor",
            DesignatorPrefix = "R?",
            PartCount = 2
        };
        // DesignatorPrefix is derived from the Designator parameter text during read,
        // so we must add a Designator parameter for round-trip (matching Altium behavior)
        component.AddParameter(new SchParameter
        {
            Name = "Designator",
            Value = "R?",
            FontId = 1,
            UniqueId = "DES00001"
        });
        original.Add(component);

        var readBack = RoundTrip(original);
        var comp = (SchComponent)readBack.Components.First();

        Assert.Equal("RESISTOR", comp.Name);
        Assert.Equal("Standard resistor", comp.Description);
        Assert.Equal("R?", comp.DesignatorPrefix);
        Assert.Equal(2, comp.PartCount);
    }

    [Fact]
    public void MultipleComponents_PreservesAll()
    {
        var original = new SchLibrary();

        for (var i = 1; i <= 3; i++)
        {
            var component = new SchComponent
            {
                Name = $"COMP{i}",
                Description = $"Component {i}",
                PartCount = 1
            };
            original.Add(component);
        }

        var readBack = RoundTrip(original);

        Assert.Equal(3, readBack.Components.Count);
        var names = readBack.Components.Select(c => c.Name).OrderBy(n => n).ToList();
        Assert.Equal(new[] { "COMP1", "COMP2", "COMP3" }, names);
    }

    [Fact]
    public void RealFiles_PreservesComponentAndPrimitiveCounts()
    {
        var testDataPath = GetTestDataPath();
        var examplesPath = GetExamplesPath();

        foreach (var dir in new[] { testDataPath, examplesPath })
        {
            if (!Directory.Exists(dir)) continue;

            foreach (var filePath in Directory.GetFiles(dir, "*.SchLib"))
            {
                var originalLib = (SchLibrary)new SchLibReader().Read(File.OpenRead(filePath));

                using var ms = new MemoryStream();
                new SchLibWriter().Write(originalLib, ms);

                ms.Position = 0;
                var roundTripped = (SchLibrary)new SchLibReader().Read(ms);

                Assert.Equal(originalLib.Components.Count, roundTripped.Components.Count);

                for (int i = 0; i < originalLib.Components.Count; i++)
                {
                    var oc = originalLib.Components[i];
                    var rc = roundTripped.Components[i];
                    Assert.Equal(oc.Name, rc.Name);
                    Assert.Equal(oc.Pins.Count, rc.Pins.Count);
                    Assert.Equal(oc.Lines.Count, rc.Lines.Count);
                    Assert.Equal(oc.Rectangles.Count, rc.Rectangles.Count);
                    Assert.Equal(oc.Labels.Count, rc.Labels.Count);
                    Assert.Equal(oc.Arcs.Count, rc.Arcs.Count);
                    Assert.Equal(oc.Polygons.Count, rc.Polygons.Count);
                    Assert.Equal(oc.Polylines.Count, rc.Polylines.Count);
                    Assert.Equal(oc.Parameters.Count, rc.Parameters.Count);
                }
            }
        }
    }

    private static string GetTestDataPath()
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "TestData", "Generated", "Individual", "SchLib");
    }

    private static string GetExamplesPath()
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "Examples");
    }
}
