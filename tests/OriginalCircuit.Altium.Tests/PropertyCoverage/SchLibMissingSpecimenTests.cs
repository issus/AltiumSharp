using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Serialization.Readers;
using OriginalCircuit.Altium.Serialization.Writers;

namespace OriginalCircuit.Altium.Tests.PropertyCoverage;

/// <summary>
/// Round-trip tests for the 8 SchLib types that previously had 0 test specimens.
/// Each test creates a SchLib component containing the target primitive type,
/// writes to SchLib, reads back, and verifies all properties round-trip.
/// </summary>
public sealed class SchLibMissingSpecimenTests
{
    private static SchLibrary RoundTrip(SchLibrary original)
    {
        using var ms = new MemoryStream();
        new SchLibWriter().Write(original, ms);
        ms.Position = 0;
        return (SchLibrary)new SchLibReader().Read(ms);
    }

    private static SchComponent CreateTestComponent(string name = "TestComp")
    {
        return new SchComponent { Name = name, Description = "Test" };
    }

    [Fact]
    public void Wire_InSchLib_PreservesAllProperties()
    {
        var lib = new SchLibrary();
        var comp = CreateTestComponent();
        var wire = new SchWire
        {
            Color = 0xFF0000,
            LineWidth = 2,
            LineStyle = SchLineStyle.Dashed,
            AreaColor = 0x00FF00,
            IsSolid = true,
            IsTransparent = true,
            AutoWire = true,
            UnderlineColor = 0x0000FF,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "WIRE001"
        };
        wire.AddVertex(new CoordPoint(Coord.FromMils(100), Coord.FromMils(200)));
        wire.AddVertex(new CoordPoint(Coord.FromMils(300), Coord.FromMils(400)));
        comp.AddWire(wire);
        lib.Add(comp);

        var readBack = RoundTrip(lib);
        var readComp = (SchComponent)readBack.Components[0];
        Assert.Single(readComp.Wires);
        var w = (SchWire)readComp.Wires[0];

        Assert.Equal(2, w.Vertices.Count);
        Assert.Equal(100, w.Vertices[0].X.ToMils(), 1);
        Assert.Equal(200, w.Vertices[0].Y.ToMils(), 1);
        Assert.Equal(300, w.Vertices[1].X.ToMils(), 1);
        Assert.Equal(400, w.Vertices[1].Y.ToMils(), 1);
        Assert.Equal(0xFF0000, w.Color);
        Assert.Equal(2, w.LineWidth);
        Assert.Equal(SchLineStyle.Dashed, w.LineStyle);
        Assert.Equal(0x00FF00, w.AreaColor);
        Assert.True(w.IsSolid);
        Assert.True(w.IsTransparent);
        Assert.True(w.AutoWire);
        Assert.Equal(0x0000FF, w.UnderlineColor);
        Assert.True(w.GraphicallyLocked);
        Assert.True(w.Disabled);
        Assert.True(w.Dimmed);
        Assert.Equal("WIRE001", w.UniqueId);
    }

    [Fact]
    public void Parameter_InSchLib_PreservesAllProperties()
    {
        var lib = new SchLibrary();
        var comp = CreateTestComponent();
        var param = new SchParameter
        {
            Location = new CoordPoint(Coord.FromMils(100), Coord.FromMils(200)),
            Color = 0x0000FF,
            Value = "ParamValue",
            Name = "ParamName",
            FontId = 2,
            Orientation = 1,
            Justification = SchTextJustification.MiddleCenter,
            IsVisible = false,
            IsMirrored = true,
            IsReadOnly = true,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "PARAM001"
        };
        comp.AddParameter(param);
        lib.Add(comp);

        var readBack = RoundTrip(lib);
        var readComp = (SchComponent)readBack.Components[0];
        Assert.Single(readComp.Parameters);
        var p = (SchParameter)readComp.Parameters[0];

        Assert.Equal(100, p.Location.X.ToMils(), 1);
        Assert.Equal(200, p.Location.Y.ToMils(), 1);
        Assert.Equal(0x0000FF, p.Color);
        Assert.Equal("ParamValue", p.Value);
        Assert.Equal("ParamName", p.Name);
        Assert.Equal(2, p.FontId);
        Assert.Equal(1, p.Orientation);
        Assert.Equal(SchTextJustification.MiddleCenter, p.Justification);
        Assert.False(p.IsVisible);
        Assert.True(p.IsMirrored);
        Assert.True(p.IsReadOnly);
        Assert.True(p.GraphicallyLocked);
        Assert.True(p.Disabled);
        Assert.True(p.Dimmed);
        Assert.Equal("PARAM001", p.UniqueId);
    }

    [Fact]
    public void NetLabel_InSchLib_PreservesAllProperties()
    {
        var lib = new SchLibrary();
        var comp = CreateTestComponent();
        var netLabel = new SchNetLabel
        {
            Location = new CoordPoint(Coord.FromMils(100), Coord.FromMils(200)),
            Color = 0x0000FF,
            Text = "TestNet",
            FontId = 2,
            Orientation = 1,
            Justification = SchTextJustification.MiddleCenter,
            IsMirrored = true,
            AreaColor = 0xFFFF00,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "NL001"
        };
        comp.AddNetLabel(netLabel);
        lib.Add(comp);

        var readBack = RoundTrip(lib);
        var readComp = (SchComponent)readBack.Components[0];
        Assert.Single(readComp.NetLabels);
        var nl = (SchNetLabel)readComp.NetLabels[0];

        Assert.Equal(100, nl.Location.X.ToMils(), 1);
        Assert.Equal(200, nl.Location.Y.ToMils(), 1);
        Assert.Equal(0x0000FF, nl.Color);
        Assert.Equal("TestNet", nl.Text);
        Assert.Equal(2, nl.FontId);
        Assert.Equal(1, nl.Orientation);
        Assert.Equal(SchTextJustification.MiddleCenter, nl.Justification);
        Assert.True(nl.IsMirrored);
        Assert.Equal(0xFFFF00, nl.AreaColor);
        Assert.True(nl.GraphicallyLocked);
        Assert.True(nl.Disabled);
        Assert.True(nl.Dimmed);
        Assert.Equal("NL001", nl.UniqueId);
    }

    [Fact]
    public void Bezier_InSchLib_PreservesAllProperties()
    {
        var lib = new SchLibrary();
        var comp = CreateTestComponent();
        var bezier = new SchBezier
        {
            Color = 0xFF0000,
            LineWidth = 2,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "BEZ001"
        };
        bezier.AddControlPoint(new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)));
        bezier.AddControlPoint(new CoordPoint(Coord.FromMils(100), Coord.FromMils(200)));
        bezier.AddControlPoint(new CoordPoint(Coord.FromMils(200), Coord.FromMils(200)));
        bezier.AddControlPoint(new CoordPoint(Coord.FromMils(300), Coord.FromMils(0)));
        comp.AddBezier(bezier);
        lib.Add(comp);

        var readBack = RoundTrip(lib);
        var readComp = (SchComponent)readBack.Components[0];
        Assert.Single(readComp.Beziers);
        var b = (SchBezier)readComp.Beziers[0];

        Assert.Equal(4, b.ControlPoints.Count);
        Assert.Equal(0, b.ControlPoints[0].X.ToMils(), 1);
        Assert.Equal(0, b.ControlPoints[0].Y.ToMils(), 1);
        Assert.Equal(100, b.ControlPoints[1].X.ToMils(), 1);
        Assert.Equal(200, b.ControlPoints[1].Y.ToMils(), 1);
        Assert.Equal(300, b.ControlPoints[3].X.ToMils(), 1);
        Assert.Equal(0, b.ControlPoints[3].Y.ToMils(), 1);
        Assert.Equal(0xFF0000, b.Color);
        Assert.Equal(2, b.LineWidth);
        Assert.True(b.GraphicallyLocked);
        Assert.True(b.Disabled);
        Assert.True(b.Dimmed);
        Assert.Equal("BEZ001", b.UniqueId);
    }

    [Fact]
    public void Image_InSchLib_PreservesAllProperties()
    {
        var lib = new SchLibrary();
        var comp = CreateTestComponent();
        var image = new SchImage
        {
            Corner1 = new CoordPoint(Coord.FromMils(100), Coord.FromMils(200)),
            Corner2 = new CoordPoint(Coord.FromMils(400), Coord.FromMils(500)),
            BorderColor = 0xFF0000,
            LineWidth = 2,
            KeepAspect = false,
            EmbedImage = false,
            Filename = "test.bmp",
            AreaColor = 0x00FF00,
            IsSolid = true,
            LineStyle = 1,
            IsTransparent = true,
            ShowBorder = true,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "IMG001"
        };
        comp.AddImage(image);
        lib.Add(comp);

        var readBack = RoundTrip(lib);
        var readComp = (SchComponent)readBack.Components[0];
        Assert.Single(readComp.Images);
        var img = (SchImage)readComp.Images[0];

        Assert.Equal(100, img.Corner1.X.ToMils(), 1);
        Assert.Equal(200, img.Corner1.Y.ToMils(), 1);
        Assert.Equal(400, img.Corner2.X.ToMils(), 1);
        Assert.Equal(500, img.Corner2.Y.ToMils(), 1);
        Assert.Equal(0xFF0000, img.BorderColor);
        Assert.Equal(2, img.LineWidth);
        Assert.False(img.KeepAspect);
        Assert.False(img.EmbedImage);
        Assert.Equal("test.bmp", img.Filename);
        Assert.Equal(0x00FF00, img.AreaColor);
        Assert.True(img.IsSolid);
        Assert.Equal(1, img.LineStyle);
        Assert.True(img.IsTransparent);
        Assert.True(img.ShowBorder);
        Assert.True(img.GraphicallyLocked);
        Assert.True(img.Disabled);
        Assert.True(img.Dimmed);
        Assert.Equal("IMG001", img.UniqueId);
    }

    [Fact]
    public void Image_EmbeddedData_InSchLib_RoundTrips()
    {
        var lib = new SchLibrary();
        var comp = CreateTestComponent();
        var imageData = new byte[] { 0x42, 0x4D, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        var image = new SchImage
        {
            Corner1 = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Corner2 = new CoordPoint(Coord.FromMils(100), Coord.FromMils(100)),
            EmbedImage = true,
            ImageData = imageData,
            UniqueId = "EIMG001"
        };
        comp.AddImage(image);
        lib.Add(comp);

        var readBack = RoundTrip(lib);
        var readComp = (SchComponent)readBack.Components[0];
        Assert.Single(readComp.Images);
        var img = (SchImage)readComp.Images[0];

        Assert.True(img.EmbedImage);
        Assert.NotNull(img.ImageData);
        Assert.Equal(imageData, img.ImageData);
    }

    [Fact]
    public void Symbol_InSchLib_PreservesAllProperties()
    {
        var lib = new SchLibrary();
        var comp = CreateTestComponent();
        var symbol = new SchSymbol
        {
            Location = new CoordPoint(Coord.FromMils(200), Coord.FromMils(300)),
            Color = 0xFF00FF,
            SymbolType = 2,
            IsMirrored = true,
            Orientation = 1,
            LineWidth = 3,
            ScaleFactor = 2,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "SYM001"
        };
        comp.AddSymbol(symbol);
        lib.Add(comp);

        var readBack = RoundTrip(lib);
        var readComp = (SchComponent)readBack.Components[0];
        Assert.Single(readComp.Symbols);
        var s = (SchSymbol)readComp.Symbols[0];

        Assert.Equal(200, s.Location.X.ToMils(), 1);
        Assert.Equal(300, s.Location.Y.ToMils(), 1);
        Assert.Equal(0xFF00FF, s.Color);
        Assert.Equal(2, s.SymbolType);
        Assert.True(s.IsMirrored);
        Assert.Equal(1, s.Orientation);
        Assert.Equal(3, s.LineWidth);
        Assert.Equal(2, s.ScaleFactor);
        Assert.True(s.GraphicallyLocked);
        Assert.True(s.Disabled);
        Assert.True(s.Dimmed);
        Assert.Equal("SYM001", s.UniqueId);
    }

    [Fact]
    public void Junction_InSchLib_PreservesAllProperties()
    {
        var lib = new SchLibrary();
        var comp = CreateTestComponent();
        var junction = new SchJunction
        {
            Location = new CoordPoint(Coord.FromMils(150), Coord.FromMils(250)),
            Color = 0x00FF00,
            Locked = true,
            Size = Coord.FromMils(5),
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "JUNC001"
        };
        comp.AddJunction(junction);
        lib.Add(comp);

        var readBack = RoundTrip(lib);
        var readComp = (SchComponent)readBack.Components[0];
        Assert.Single(readComp.Junctions);
        var j = (SchJunction)readComp.Junctions[0];

        Assert.Equal(150, j.Location.X.ToMils(), 1);
        Assert.Equal(250, j.Location.Y.ToMils(), 1);
        Assert.Equal(0x00FF00, j.Color);
        Assert.True(j.Locked);
        Assert.True(j.GraphicallyLocked);
        Assert.True(j.Disabled);
        Assert.True(j.Dimmed);
        Assert.Equal("JUNC001", j.UniqueId);
    }

    [Fact]
    public void PowerObject_InSchLib_PreservesAllProperties()
    {
        var lib = new SchLibrary();
        var comp = CreateTestComponent();
        var power = new SchPowerObject
        {
            Location = new CoordPoint(Coord.FromMils(300), Coord.FromMils(400)),
            Color = 0xFF00FF,
            Text = "VCC",
            Style = PowerPortStyle.Arrow,
            Rotation = 90,
            ShowNetName = true,
            IsCrossSheetConnector = true,
            FontId = 3,
            AreaColor = 0x808080,
            IsCustomStyle = true,
            IsMirrored = true,
            Justification = 1,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "PWR001"
        };
        comp.AddPowerObject(power);
        lib.Add(comp);

        var readBack = RoundTrip(lib);
        var readComp = (SchComponent)readBack.Components[0];
        Assert.Single(readComp.PowerObjects);
        var p = (SchPowerObject)readComp.PowerObjects[0];

        Assert.Equal(300, p.Location.X.ToMils(), 1);
        Assert.Equal(400, p.Location.Y.ToMils(), 1);
        Assert.Equal(0xFF00FF, p.Color);
        Assert.Equal("VCC", p.Text);
        Assert.Equal(PowerPortStyle.Arrow, p.Style);
        Assert.Equal(90, p.Rotation, 0.1);
        Assert.True(p.ShowNetName);
        Assert.True(p.IsCrossSheetConnector);
        Assert.Equal(3, p.FontId);
        Assert.Equal(0x808080, p.AreaColor);
        Assert.True(p.IsCustomStyle);
        Assert.True(p.IsMirrored);
        Assert.Equal(1, p.Justification);
        Assert.True(p.GraphicallyLocked);
        Assert.True(p.Disabled);
        Assert.True(p.Dimmed);
        Assert.Equal("PWR001", p.UniqueId);
    }

    [Fact]
    public void Ellipse_InSchLib_PreservesAllProperties()
    {
        var lib = new SchLibrary();
        var comp = CreateTestComponent();
        var ellipse = new SchEllipse
        {
            Center = new CoordPoint(Coord.FromMils(300), Coord.FromMils(400)),
            RadiusX = Coord.FromMils(150),
            RadiusY = Coord.FromMils(100),
            LineWidth = 2,
            Color = 0xFF0000,
            FillColor = 0x00FF00,
            IsFilled = true,
            IsTransparent = true,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "ELL001"
        };
        comp.AddEllipse(ellipse);
        lib.Add(comp);

        var readBack = RoundTrip(lib);
        var readComp = (SchComponent)readBack.Components[0];
        Assert.Single(readComp.Ellipses);
        var e = (SchEllipse)readComp.Ellipses[0];

        Assert.Equal(300, e.Center.X.ToMils(), 1);
        Assert.Equal(400, e.Center.Y.ToMils(), 1);
        Assert.Equal(150, e.RadiusX.ToMils(), 1);
        Assert.Equal(100, e.RadiusY.ToMils(), 1);
        Assert.Equal(2, e.LineWidth);
        Assert.Equal(0xFF0000, e.Color);
        Assert.Equal(0x00FF00, e.FillColor);
        Assert.True(e.IsFilled);
        Assert.True(e.IsTransparent);
        Assert.True(e.GraphicallyLocked);
        Assert.True(e.Disabled);
        Assert.True(e.Dimmed);
        Assert.Equal("ELL001", e.UniqueId);
    }

    [Fact]
    public void EllipticalArc_InSchLib_PreservesAllProperties()
    {
        var lib = new SchLibrary();
        var comp = CreateTestComponent();
        var arc = new SchEllipticalArc
        {
            Center = new CoordPoint(Coord.FromMils(200), Coord.FromMils(300)),
            PrimaryRadius = Coord.FromMils(120),
            SecondaryRadius = Coord.FromMils(80),
            StartAngle = 30.0,
            EndAngle = 270.0,
            LineWidth = Coord.FromMils(2),
            Color = 0x0000FF,
            AreaColor = 0xFF00FF,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "EA001"
        };
        comp.AddEllipticalArc(arc);
        lib.Add(comp);

        var readBack = RoundTrip(lib);
        var readComp = (SchComponent)readBack.Components[0];
        Assert.Single(readComp.EllipticalArcs);
        var ea = (SchEllipticalArc)readComp.EllipticalArcs[0];

        Assert.Equal(200, ea.Center.X.ToMils(), 1);
        Assert.Equal(300, ea.Center.Y.ToMils(), 1);
        Assert.Equal(120, ea.PrimaryRadius.ToMils(), 1);
        Assert.Equal(80, ea.SecondaryRadius.ToMils(), 1);
        Assert.Equal(30.0, ea.StartAngle, 0.1);
        Assert.Equal(270.0, ea.EndAngle, 0.1);
        Assert.Equal(0x0000FF, ea.Color);
        Assert.Equal(0xFF00FF, ea.AreaColor);
        Assert.True(ea.GraphicallyLocked);
        Assert.True(ea.Disabled);
        Assert.True(ea.Dimmed);
        Assert.Equal("EA001", ea.UniqueId);
    }

    [Fact]
    public void Pie_InSchLib_PreservesAllProperties()
    {
        var lib = new SchLibrary();
        var comp = CreateTestComponent();
        var pie = new SchPie
        {
            Center = new CoordPoint(Coord.FromMils(400), Coord.FromMils(500)),
            Radius = Coord.FromMils(200),
            StartAngle = 0.0,
            EndAngle = 90.0,
            LineWidth = 2,
            Color = 0xFF0000,
            FillColor = 0x00FF00,
            IsFilled = true,
            IsTransparent = true,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "PIE001"
        };
        comp.AddPie(pie);
        lib.Add(comp);

        var readBack = RoundTrip(lib);
        var readComp = (SchComponent)readBack.Components[0];
        Assert.Single(readComp.Pies);
        var p = (SchPie)readComp.Pies[0];

        Assert.Equal(400, p.Center.X.ToMils(), 1);
        Assert.Equal(500, p.Center.Y.ToMils(), 1);
        Assert.Equal(200, p.Radius.ToMils(), 1);
        Assert.Equal(0.0, p.StartAngle, 0.1);
        Assert.Equal(90.0, p.EndAngle, 0.1);
        Assert.Equal(2, p.LineWidth);
        Assert.Equal(0xFF0000, p.Color);
        Assert.Equal(0x00FF00, p.FillColor);
        Assert.True(p.IsFilled);
        Assert.True(p.IsTransparent);
        Assert.True(p.GraphicallyLocked);
        Assert.True(p.Disabled);
        Assert.True(p.Dimmed);
        Assert.Equal("PIE001", p.UniqueId);
    }

    [Fact]
    public void Polygon_InSchLib_PreservesAllProperties()
    {
        var lib = new SchLibrary();
        var comp = CreateTestComponent();
        var polygon = SchPolygon.Create()
            .LineWidth(2)
            .Color(0xFF0000)
            .FillColor(0x00FF00)
            .Filled(true)
            .Transparent(true)
            .AddVertex(Coord.FromMils(0), Coord.FromMils(0))
            .AddVertex(Coord.FromMils(300), Coord.FromMils(0))
            .AddVertex(Coord.FromMils(300), Coord.FromMils(200))
            .AddVertex(Coord.FromMils(0), Coord.FromMils(200))
            .Build();
        polygon.GraphicallyLocked = true;
        polygon.Disabled = true;
        polygon.Dimmed = true;
        polygon.UniqueId = "POLY001";
        comp.AddPolygon(polygon);
        lib.Add(comp);

        var readBack = RoundTrip(lib);
        var readComp = (SchComponent)readBack.Components[0];
        Assert.Single(readComp.Polygons);
        var pg = (SchPolygon)readComp.Polygons[0];

        Assert.Equal(4, pg.Vertices.Count);
        Assert.Equal(0, pg.Vertices[0].X.ToMils(), 1);
        Assert.Equal(300, pg.Vertices[1].X.ToMils(), 1);
        Assert.Equal(200, pg.Vertices[2].Y.ToMils(), 1);
        Assert.Equal(2, pg.LineWidth);
        Assert.Equal(0xFF0000, pg.Color);
        Assert.Equal(0x00FF00, pg.FillColor);
        Assert.True(pg.IsFilled);
        Assert.True(pg.IsTransparent);
        Assert.True(pg.GraphicallyLocked);
        Assert.True(pg.Disabled);
        Assert.True(pg.Dimmed);
        Assert.Equal("POLY001", pg.UniqueId);
    }

    [Fact]
    public void RoundedRectangle_InSchLib_PreservesAllProperties()
    {
        var lib = new SchLibrary();
        var comp = CreateTestComponent();
        var rr = new SchRoundedRectangle
        {
            Corner1 = new CoordPoint(Coord.FromMils(100), Coord.FromMils(200)),
            Corner2 = new CoordPoint(Coord.FromMils(400), Coord.FromMils(500)),
            CornerRadiusX = Coord.FromMils(20),
            CornerRadiusY = Coord.FromMils(30),
            LineWidth = 2,
            LineStyle = 1,
            Color = 0xFF0000,
            FillColor = 0x00FF00,
            IsFilled = true,
            IsTransparent = true,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "RR001"
        };
        comp.AddRoundedRectangle(rr);
        lib.Add(comp);

        var readBack = RoundTrip(lib);
        var readComp = (SchComponent)readBack.Components[0];
        Assert.Single(readComp.RoundedRectangles);
        var r = (SchRoundedRectangle)readComp.RoundedRectangles[0];

        Assert.Equal(100, r.Corner1.X.ToMils(), 1);
        Assert.Equal(200, r.Corner1.Y.ToMils(), 1);
        Assert.Equal(400, r.Corner2.X.ToMils(), 1);
        Assert.Equal(500, r.Corner2.Y.ToMils(), 1);
        Assert.Equal(20, r.CornerRadiusX.ToMils(), 1);
        Assert.Equal(30, r.CornerRadiusY.ToMils(), 1);
        Assert.Equal(2, r.LineWidth);
        Assert.Equal(1, r.LineStyle);
        Assert.Equal(0xFF0000, r.Color);
        Assert.Equal(0x00FF00, r.FillColor);
        Assert.True(r.IsFilled);
        Assert.True(r.IsTransparent);
        Assert.True(r.GraphicallyLocked);
        Assert.True(r.Disabled);
        Assert.True(r.Dimmed);
        Assert.Equal("RR001", r.UniqueId);
    }

    [Fact]
    public void TextFrame_InSchLib_PreservesAllProperties()
    {
        var lib = new SchLibrary();
        var comp = CreateTestComponent();
        var tf = new SchTextFrame
        {
            Corner1 = new CoordPoint(Coord.FromMils(100), Coord.FromMils(200)),
            Corner2 = new CoordPoint(Coord.FromMils(500), Coord.FromMils(400)),
            Text = "Hello World",
            Orientation = 1,
            Alignment = SchTextJustification.MiddleCenter,
            FontId = 2,
            TextColor = 0x0000FF,
            BorderColor = 0xFF0000,
            FillColor = 0x00FF00,
            ShowBorder = true,
            IsFilled = true,
            WordWrap = true,
            ClipToRect = true,
            LineWidth = 2,
            LineStyle = 1,
            TextMargin = 5,
            IsTransparent = true,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "TF001"
        };
        comp.AddTextFrame(tf);
        lib.Add(comp);

        var readBack = RoundTrip(lib);
        var readComp = (SchComponent)readBack.Components[0];
        Assert.Single(readComp.TextFrames);
        var t = (SchTextFrame)readComp.TextFrames[0];

        Assert.Equal(100, t.Corner1.X.ToMils(), 1);
        Assert.Equal(200, t.Corner1.Y.ToMils(), 1);
        Assert.Equal(500, t.Corner2.X.ToMils(), 1);
        Assert.Equal(400, t.Corner2.Y.ToMils(), 1);
        Assert.Equal("Hello World", t.Text);
        Assert.Equal(1, t.Orientation);
        Assert.Equal(SchTextJustification.MiddleCenter, t.Alignment);
        Assert.Equal(2, t.FontId);
        Assert.Equal(0x0000FF, t.TextColor);
        Assert.Equal(0xFF0000, t.BorderColor);
        Assert.Equal(0x00FF00, t.FillColor);
        Assert.True(t.ShowBorder);
        Assert.True(t.IsFilled);
        Assert.True(t.WordWrap);
        Assert.True(t.ClipToRect);
        Assert.Equal(2, t.LineWidth);
        Assert.Equal(1, t.LineStyle);
        Assert.Equal(5, t.TextMargin);
        Assert.True(t.IsTransparent);
        Assert.True(t.GraphicallyLocked);
        Assert.True(t.Disabled);
        Assert.True(t.Dimmed);
        Assert.Equal("TF001", t.UniqueId);
    }

    [Fact]
    public void Parameter_InSchLib_PreservesExtendedProperties()
    {
        var lib = new SchLibrary();
        var comp = CreateTestComponent();
        var param = new SchParameter
        {
            Location = new CoordPoint(Coord.FromMils(100), Coord.FromMils(200)),
            Color = 0x0000FF,
            Value = "ExtValue",
            Name = "ExtParam",
            FontId = 2,
            Orientation = 1,
            Justification = SchTextJustification.MiddleCenter,
            IsVisible = false,
            IsMirrored = true,
            IsReadOnly = true,
            HideName = true,
            ParamType = 1,
            ShowName = true,
            AreaColor = 0x808080,
            AutoPosition = 2,
            IsConfigurable = true,
            IsRule = true,
            IsSystemParameter = true,
            TextHorzAnchor = 1,
            TextVertAnchor = 2,
            Description = "Test description",
            AllowDatabaseSynchronize = true,
            AllowLibrarySynchronize = true,
            NameIsReadOnly = true,
            PhysicalDesignator = "R1",
            ValueIsReadOnly = true,
            VariantOption = "TestVariant",
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "PARAM_EXT"
        };
        comp.AddParameter(param);
        lib.Add(comp);

        var readBack = RoundTrip(lib);
        var readComp = (SchComponent)readBack.Components[0];
        Assert.Single(readComp.Parameters);
        var p = (SchParameter)readComp.Parameters[0];

        Assert.Equal(100, p.Location.X.ToMils(), 1);
        Assert.Equal(200, p.Location.Y.ToMils(), 1);
        Assert.Equal(0x0000FF, p.Color);
        Assert.Equal("ExtValue", p.Value);
        Assert.Equal("ExtParam", p.Name);
        Assert.Equal(2, p.FontId);
        Assert.Equal(1, p.Orientation);
        Assert.Equal(SchTextJustification.MiddleCenter, p.Justification);
        Assert.False(p.IsVisible);
        Assert.True(p.IsMirrored);
        Assert.True(p.IsReadOnly);
        Assert.True(p.HideName);
        Assert.Equal(1, p.ParamType);
        Assert.True(p.ShowName);
        Assert.Equal(0x808080, p.AreaColor);
        Assert.Equal(2, p.AutoPosition);
        Assert.True(p.IsConfigurable);
        Assert.True(p.IsRule);
        Assert.True(p.IsSystemParameter);
        Assert.Equal(1, p.TextHorzAnchor);
        Assert.Equal(2, p.TextVertAnchor);
        Assert.Equal("Test description", p.Description);
        Assert.True(p.AllowDatabaseSynchronize);
        Assert.True(p.AllowLibrarySynchronize);
        Assert.True(p.NameIsReadOnly);
        Assert.Equal("R1", p.PhysicalDesignator);
        Assert.True(p.ValueIsReadOnly);
        Assert.Equal("TestVariant", p.VariantOption);
        Assert.True(p.GraphicallyLocked);
        Assert.True(p.Disabled);
        Assert.True(p.Dimmed);
        Assert.Equal("PARAM_EXT", p.UniqueId);
    }
}
