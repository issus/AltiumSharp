using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Serialization.Readers;
using OriginalCircuit.Altium.Serialization.Writers;

namespace OriginalCircuit.Altium.Tests.PropertyCoverage;

/// <summary>
/// Round-trip property coverage tests for SchDoc-specific primitive types.
/// Each test creates a primitive with all properties set to non-default values,
/// writes to SchDoc, reads back, and verifies every property round-trips.
/// </summary>
public sealed class SchDocPropertyCoverageTests
{
    private static SchDocument RoundTrip(SchDocument original)
    {
        using var ms = new MemoryStream();
        new SchDocWriter().Write(original, ms);
        ms.Position = 0;
        return new SchDocReader().Read(ms);
    }

    [Fact]
    public void Wire_PreservesAllProperties()
    {
        var doc = new SchDocument();
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
        wire.AddVertex(new CoordPoint(Coord.FromMils(500), Coord.FromMils(600)));
        doc.AddPrimitive(wire);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.Wires);
        var w = (SchWire)readBack.Wires[0];

        Assert.Equal(3, w.Vertices.Count);
        Assert.Equal(100, w.Vertices[0].X.ToMils(), 1);
        Assert.Equal(200, w.Vertices[0].Y.ToMils(), 1);
        Assert.Equal(500, w.Vertices[2].X.ToMils(), 1);
        Assert.Equal(600, w.Vertices[2].Y.ToMils(), 1);
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
    public void Junction_PreservesAllProperties()
    {
        var doc = new SchDocument();
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
        doc.AddPrimitive(junction);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.Junctions);
        var j = (SchJunction)readBack.Junctions[0];

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
    public void NetLabel_PreservesAllProperties()
    {
        var doc = new SchDocument();
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
        doc.AddPrimitive(netLabel);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.NetLabels);
        var nl = (SchNetLabel)readBack.NetLabels[0];

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
    public void PowerObject_PreservesAllProperties()
    {
        var doc = new SchDocument();
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
        doc.AddPrimitive(power);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.PowerObjects);
        var p = (SchPowerObject)readBack.PowerObjects[0];

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
    public void Port_PreservesAllProperties()
    {
        var doc = new SchDocument();
        var port = new SchPort
        {
            Location = new CoordPoint(Coord.FromMils(100), Coord.FromMils(200)),
            Name = "DataPort",
            IoType = 1,
            Style = 2,
            Alignment = 1,
            Width = Coord.FromMils(50),
            Height = Coord.FromMils(20),
            BorderWidth = 2,
            AutoSize = true,
            ConnectedEnd = 1,
            CrossReference = "Page2",
            ShowNetName = true,
            HarnessType = "TestHarness",
            HarnessColor = 0xAABBCC,
            IsCustomStyle = true,
            FontId = 2,
            Color = 0xFF0000,
            AreaColor = 0x00FF00,
            TextColor = 0x0000FF,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "PORT001"
        };
        doc.AddPrimitive(port);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.Ports);
        var pt = readBack.Ports[0];

        Assert.Equal(100, pt.Location.X.ToMils(), 1);
        Assert.Equal(200, pt.Location.Y.ToMils(), 1);
        Assert.Equal("DataPort", pt.Name);
        Assert.Equal(1, pt.IoType);
        Assert.Equal(2, pt.Style);
        Assert.Equal(1, pt.Alignment);
        Assert.Equal(50, pt.Width.ToMils(), 1);
        Assert.Equal(20, pt.Height.ToMils(), 1);
        Assert.Equal(2, pt.BorderWidth);
        Assert.True(pt.AutoSize);
        Assert.Equal(1, pt.ConnectedEnd);
        Assert.Equal("Page2", pt.CrossReference);
        Assert.True(pt.ShowNetName);
        Assert.Equal("TestHarness", pt.HarnessType);
        Assert.Equal(0xAABBCC, pt.HarnessColor);
        Assert.True(pt.IsCustomStyle);
        Assert.Equal(2, pt.FontId);
        Assert.Equal(0xFF0000, pt.Color);
        Assert.Equal(0x00FF00, pt.AreaColor);
        Assert.Equal(0x0000FF, pt.TextColor);
        Assert.True(pt.GraphicallyLocked);
        Assert.True(pt.Disabled);
        Assert.True(pt.Dimmed);
        Assert.Equal("PORT001", pt.UniqueId);
    }

    [Fact]
    public void Bus_PreservesAllProperties()
    {
        var doc = new SchDocument();
        var bus = new SchBus
        {
            Color = 0x0000FF,
            LineWidth = 3,
            LineStyle = 1,
            AreaColor = 0xFF0000,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "BUS001"
        };
        bus.AddVertex(new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)));
        bus.AddVertex(new CoordPoint(Coord.FromMils(1000), Coord.FromMils(0)));
        bus.AddVertex(new CoordPoint(Coord.FromMils(1000), Coord.FromMils(500)));
        doc.AddPrimitive(bus);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.Buses);
        var b = (SchBus)readBack.Buses[0];

        Assert.Equal(3, b.Vertices.Count);
        Assert.Equal(0, b.Vertices[0].X.ToMils(), 1);
        Assert.Equal(1000, b.Vertices[1].X.ToMils(), 1);
        Assert.Equal(500, b.Vertices[2].Y.ToMils(), 1);
        Assert.Equal(0x0000FF, b.Color);
        Assert.Equal(3, b.LineWidth);
        Assert.Equal(1, b.LineStyle);
        Assert.Equal(0xFF0000, b.AreaColor);
        Assert.True(b.GraphicallyLocked);
        Assert.True(b.Disabled);
        Assert.True(b.Dimmed);
        Assert.Equal("BUS001", b.UniqueId);
    }

    [Fact]
    public void BusEntry_PreservesAllProperties()
    {
        var doc = new SchDocument();
        var entry = new SchBusEntry
        {
            Location = new CoordPoint(Coord.FromMils(100), Coord.FromMils(200)),
            Corner = new CoordPoint(Coord.FromMils(110), Coord.FromMils(210)),
            LineWidth = 2,
            Color = 0x00FF00,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "BE001"
        };
        doc.AddPrimitive(entry);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.BusEntries);
        var be = readBack.BusEntries[0];

        Assert.Equal(100, be.Location.X.ToMils(), 1);
        Assert.Equal(200, be.Location.Y.ToMils(), 1);
        Assert.Equal(110, be.Corner.X.ToMils(), 1);
        Assert.Equal(210, be.Corner.Y.ToMils(), 1);
        Assert.Equal(2, be.LineWidth);
        Assert.Equal(0x00FF00, be.Color);
        Assert.True(be.GraphicallyLocked);
        Assert.True(be.Disabled);
        Assert.True(be.Dimmed);
        Assert.Equal("BE001", be.UniqueId);
    }

    [Fact]
    public void NoErc_PreservesAllProperties()
    {
        var doc = new SchDocument();
        var noErc = new SchNoErc
        {
            Location = new CoordPoint(Coord.FromMils(500), Coord.FromMils(600)),
            Orientation = 2,
            Color = 0xFF0000,
            IsActive = true,
            Symbol = 1,
            AreaColor = 0x00FF00,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "NOERC001"
        };
        doc.AddPrimitive(noErc);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.NoErcs);
        var ne = readBack.NoErcs[0];

        Assert.Equal(500, ne.Location.X.ToMils(), 1);
        Assert.Equal(600, ne.Location.Y.ToMils(), 1);
        Assert.Equal(2, ne.Orientation);
        Assert.Equal(0xFF0000, ne.Color);
        Assert.True(ne.IsActive);
        Assert.Equal(1, ne.Symbol);
        Assert.Equal(0x00FF00, ne.AreaColor);
        Assert.True(ne.GraphicallyLocked);
        Assert.True(ne.Disabled);
        Assert.True(ne.Dimmed);
        Assert.Equal("NOERC001", ne.UniqueId);
    }

    [Fact]
    public void SheetSymbol_PreservesAllProperties()
    {
        var doc = new SchDocument();
        var sheet = new SchSheetSymbol
        {
            Location = new CoordPoint(Coord.FromMils(100), Coord.FromMils(200)),
            XSize = Coord.FromMils(500),
            YSize = Coord.FromMils(300),
            IsMirrored = true,
            FileName = "SubSheet.SchDoc",
            SheetName = "SubSheet",
            LineWidth = 2,
            Color = 0xFF0000,
            AreaColor = 0x00FF00,
            IsSolid = true,
            ShowHiddenFields = true,
            SymbolType = 1,
            DesignItemId = "DID001",
            ItemGuid = "GUID001",
            LibIdentifierKind = 1,
            LibraryIdentifier = "LIB001",
            RevisionGuid = "REVGUID001",
            SourceLibraryName = "TestLib",
            VaultGuid = "VGUID001",
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "SS001"
        };

        // Add an entry
        var entry = new SchSheetEntry
        {
            Side = 1,
            DistanceFromTop = Coord.FromMils(50),
            Name = "EntryPin",
            IoType = 1,
            Style = 2,
            ArrowKind = 1,
            HarnessType = "TestHarness",
            HarnessColor = 0xAABBCC,
            FontId = 2,
            Color = 0x0000FF,
            AreaColor = 0xFF00FF,
            TextColor = 0x00FFFF,
            TextStyle = 1,
            UniqueId = "SE001"
        };
        sheet.AddEntry(entry);
        doc.AddPrimitive(sheet);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.SheetSymbols);
        var ss = readBack.SheetSymbols[0];

        Assert.Equal(100, ss.Location.X.ToMils(), 1);
        Assert.Equal(200, ss.Location.Y.ToMils(), 1);
        Assert.Equal(500, ss.XSize.ToMils(), 1);
        Assert.Equal(300, ss.YSize.ToMils(), 1);
        Assert.True(ss.IsMirrored);
        Assert.Equal("SubSheet.SchDoc", ss.FileName);
        Assert.Equal("SubSheet", ss.SheetName);
        Assert.Equal(2, ss.LineWidth);
        Assert.Equal(0xFF0000, ss.Color);
        Assert.Equal(0x00FF00, ss.AreaColor);
        Assert.True(ss.IsSolid);
        Assert.True(ss.ShowHiddenFields);
        Assert.Equal(1, ss.SymbolType);
        Assert.Equal("DID001", ss.DesignItemId);
        Assert.Equal("GUID001", ss.ItemGuid);
        Assert.Equal(1, ss.LibIdentifierKind);
        Assert.Equal("LIB001", ss.LibraryIdentifier);
        Assert.Equal("REVGUID001", ss.RevisionGuid);
        Assert.Equal("TestLib", ss.SourceLibraryName);
        Assert.Equal("VGUID001", ss.VaultGuid);
        Assert.True(ss.GraphicallyLocked);
        Assert.True(ss.Disabled);
        Assert.True(ss.Dimmed);
        Assert.Equal("SS001", ss.UniqueId);

        // Verify entry
        Assert.Single(ss.Entries);
        var se = ss.Entries[0];
        Assert.Equal(1, se.Side);
        Assert.Equal(50, se.DistanceFromTop.ToMils(), 1);
        Assert.Equal("EntryPin", se.Name);
        Assert.Equal(1, se.IoType);
        Assert.Equal(2, se.Style);
        Assert.Equal(1, se.ArrowKind);
        Assert.Equal("TestHarness", se.HarnessType);
        Assert.Equal(0xAABBCC, se.HarnessColor);
        Assert.Equal(2, se.FontId);
        Assert.Equal(0x0000FF, se.Color);
        Assert.Equal(0xFF00FF, se.AreaColor);
        Assert.Equal(0x00FFFF, se.TextColor);
        Assert.Equal(1, se.TextStyle);
        Assert.Equal("SE001", se.UniqueId);
    }

    [Fact]
    public void ParameterSet_PreservesAllProperties()
    {
        var doc = new SchDocument();
        var paramSet = new SchParameterSet
        {
            Location = new CoordPoint(Coord.FromMils(200), Coord.FromMils(300)),
            Orientation = 1,
            Style = 2,
            Color = 0xFF0000,
            AreaColor = 0x00FF00,
            Name = "TestParamSet",
            ShowHiddenFields = true,
            BorderWidth = 2,
            IsSolid = true,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "PS001"
        };
        var param = new SchParameter
        {
            Name = "Param1",
            Value = "Value1",
            Location = new CoordPoint(Coord.FromMils(210), Coord.FromMils(310)),
            Color = 0x0000FF,
            FontId = 2,
            UniqueId = "P001"
        };
        paramSet.AddParameter(param);
        doc.AddPrimitive(paramSet);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.ParameterSets);
        var ps = readBack.ParameterSets[0];

        Assert.Equal(200, ps.Location.X.ToMils(), 1);
        Assert.Equal(300, ps.Location.Y.ToMils(), 1);
        Assert.Equal(1, ps.Orientation);
        Assert.Equal(2, ps.Style);
        Assert.Equal(0xFF0000, ps.Color);
        Assert.Equal(0x00FF00, ps.AreaColor);
        Assert.Equal("TestParamSet", ps.Name);
        Assert.True(ps.ShowHiddenFields);
        Assert.Equal(2, ps.BorderWidth);
        Assert.True(ps.IsSolid);
        Assert.True(ps.GraphicallyLocked);
        Assert.True(ps.Disabled);
        Assert.True(ps.Dimmed);
        Assert.Equal("PS001", ps.UniqueId);

        // Verify child parameter
        Assert.Single(ps.Parameters);
        Assert.Equal("Param1", ps.Parameters[0].Name);
        Assert.Equal("Value1", ps.Parameters[0].Value);
    }

    [Fact]
    public void Blanket_PreservesAllProperties()
    {
        var doc = new SchDocument();
        var blanket = new SchBlanket
        {
            Color = 0xFF0000,
            AreaColor = 0x00FF00,
            LineWidth = 2,
            LineStyle = 1,
            IsSolid = true,
            IsTransparent = true,
            IsCollapsed = true,
            GraphicallyLocked = true,
            Disabled = true,
            Dimmed = true,
            UniqueId = "BLK001"
        };
        blanket.AddVertex(new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)));
        blanket.AddVertex(new CoordPoint(Coord.FromMils(500), Coord.FromMils(0)));
        blanket.AddVertex(new CoordPoint(Coord.FromMils(500), Coord.FromMils(300)));
        blanket.AddVertex(new CoordPoint(Coord.FromMils(0), Coord.FromMils(300)));

        var param = new SchParameter
        {
            Name = "BlanketParam",
            Value = "BlkValue",
            UniqueId = "BP001"
        };
        blanket.AddParameter(param);
        doc.AddPrimitive(blanket);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.Blankets);
        var bl = readBack.Blankets[0];

        Assert.Equal(4, bl.Vertices.Count);
        Assert.Equal(0, bl.Vertices[0].X.ToMils(), 1);
        Assert.Equal(500, bl.Vertices[1].X.ToMils(), 1);
        Assert.Equal(300, bl.Vertices[2].Y.ToMils(), 1);
        Assert.Equal(0xFF0000, bl.Color);
        Assert.Equal(0x00FF00, bl.AreaColor);
        Assert.Equal(2, bl.LineWidth);
        Assert.Equal(1, bl.LineStyle);
        Assert.True(bl.IsSolid);
        Assert.True(bl.IsTransparent);
        Assert.True(bl.IsCollapsed);
        Assert.True(bl.GraphicallyLocked);
        Assert.True(bl.Disabled);
        Assert.True(bl.Dimmed);
        Assert.Equal("BLK001", bl.UniqueId);

        // Verify child parameter
        Assert.Single(bl.Parameters);
        Assert.Equal("BlanketParam", bl.Parameters[0].Name);
    }

    [Fact]
    public void Image_PreservesAllProperties()
    {
        var doc = new SchDocument();
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
        doc.AddPrimitive(image);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.Images);
        var img = (SchImage)readBack.Images[0];

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
    public void Image_EmbeddedData_RoundTrips()
    {
        var doc = new SchDocument();
        var imageData = new byte[] { 0x42, 0x4D, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        var image = new SchImage
        {
            Corner1 = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            Corner2 = new CoordPoint(Coord.FromMils(100), Coord.FromMils(100)),
            EmbedImage = true,
            ImageData = imageData,
            UniqueId = "EIMG001"
        };
        doc.AddPrimitive(image);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.Images);
        var img = (SchImage)readBack.Images[0];

        Assert.True(img.EmbedImage);
        Assert.NotNull(img.ImageData);
        Assert.Equal(imageData, img.ImageData);
    }

    [Fact]
    public void Symbol_PreservesAllProperties()
    {
        var doc = new SchDocument();
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
        doc.AddPrimitive(symbol);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.Symbols);
        var s = readBack.Symbols[0];

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
    public void Ellipse_PreservesAllProperties()
    {
        var doc = new SchDocument();
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
        doc.AddPrimitive(ellipse);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.Ellipses);
        var e = (SchEllipse)readBack.Ellipses[0];

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
    public void EllipticalArc_PreservesAllProperties()
    {
        var doc = new SchDocument();
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
        doc.AddPrimitive(arc);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.EllipticalArcs);
        var ea = (SchEllipticalArc)readBack.EllipticalArcs[0];

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
    public void Pie_PreservesAllProperties()
    {
        var doc = new SchDocument();
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
        doc.AddPrimitive(pie);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.Pies);
        var p = (SchPie)readBack.Pies[0];

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
    public void Polygon_PreservesAllProperties()
    {
        var doc = new SchDocument();
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
        doc.AddPrimitive(polygon);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.Polygons);
        var pg = (SchPolygon)readBack.Polygons[0];

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
    public void RoundedRectangle_PreservesAllProperties()
    {
        var doc = new SchDocument();
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
        doc.AddPrimitive(rr);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.RoundedRectangles);
        var r = (SchRoundedRectangle)readBack.RoundedRectangles[0];

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
    public void TextFrame_PreservesAllProperties()
    {
        var doc = new SchDocument();
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
        doc.AddPrimitive(tf);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.TextFrames);
        var t = (SchTextFrame)readBack.TextFrames[0];

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
}
