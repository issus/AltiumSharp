using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Serialization.Readers;
using OriginalCircuit.Altium.Serialization.Writers;

namespace OriginalCircuit.Altium.Tests.PropertyCoverage;

/// <summary>
/// Round-trip property coverage tests for PcbDoc parameter block types.
/// Each test creates objects with all properties set to non-default values,
/// writes to PcbDoc, reads back, and verifies every property round-trips.
/// </summary>
public sealed class PcbDocPropertyCoverageTests
{
    private static PcbDocument RoundTrip(PcbDocument original)
    {
        using var ms = new MemoryStream();
        new PcbDocWriter().Write(original, ms);
        ms.Position = 0;
        return new PcbDocReader().Read(ms);
    }

    [Fact]
    public void Polygon_PreservesAllProperties()
    {
        var doc = new PcbDocument();
        var polygon = new PcbPolygon
        {
            Layer = 1,
            Net = "GND",
            Name = "TestPoly",
            UniqueId = "POLY001",
            PolygonType = 1,
            PolyHatchStyle = 2,
            PourOver = 1,
            RemoveIslandsByArea = true,
            IslandAreaThreshold = 1000,
            RemoveDead = true,
            RemoveNarrowNecks = true,
            UseOctagons = true,
            AvoidObstacles = true,
            AvoidObsticles = true,
            Grid = Coord.FromMils(10),
            TrackSize = Coord.FromMils(8),
            MinTrack = Coord.FromMils(5),
            NeckWidthThreshold = Coord.FromMils(3),
            ArcApproximation = Coord.FromMils(1),
            BorderWidth = Coord.FromMils(2),
            SolderMaskExpansion = Coord.FromMils(4),
            PasteMaskExpansion = Coord.FromMils(3),
            ReliefAirGap = Coord.FromMils(10),
            ReliefConductorWidth = Coord.FromMils(8),
            ReliefEntries = 4,
            PowerPlaneClearance = Coord.FromMils(20),
            PowerPlaneConnectStyle = 1,
            PowerPlaneReliefExpansion = Coord.FromMils(15),
            PourIndex = 2,
            AreaSize = 123456789,
            PrimitiveLock = true,
            IsHidden = true,
            PourOverSameNetPolygons = true,
            Enabled = false,
            IsKeepout = true,
            PolygonOutline = true,
            Poured = true,
            AutoGenerateName = true,
            ClipAcuteCorners = true,
            DrawDeadCopper = true,
            DrawRemovedIslands = true,
            DrawRemovedNecks = true,
            ExpandOutline = true,
            IgnoreViolations = true,
            MitreCorners = true,
            ObeyPolygonCutout = true,
            OptimalVoidRotation = true,
            AllowGlobalEdit = true,
            Moveable = true,
            ArcPourMode = true
        };
        polygon.AddVertex(new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)));
        polygon.AddVertex(new CoordPoint(Coord.FromMils(1000), Coord.FromMils(0)));
        polygon.AddVertex(new CoordPoint(Coord.FromMils(1000), Coord.FromMils(1000)));
        polygon.AddVertex(new CoordPoint(Coord.FromMils(0), Coord.FromMils(1000)));
        doc.AddPolygon(polygon);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.Polygons);
        var p = readBack.Polygons[0];

        // Basic identity
        Assert.Equal(1, p.Layer);
        Assert.Equal("GND", p.Net);
        Assert.Equal("TestPoly", p.Name);
        Assert.Equal("POLY001", p.UniqueId);
        Assert.Equal(1, p.PolygonType);

        // Hatch/pour settings (written with DTO keys)
        Assert.Equal(2, p.PolyHatchStyle);
        Assert.Equal(1, p.PourOver);

        // Boolean flags
        Assert.True(p.RemoveIslandsByArea);
        Assert.Equal(1000, p.IslandAreaThreshold);
        Assert.True(p.RemoveDead);
        Assert.True(p.RemoveNarrowNecks);
        Assert.True(p.UseOctagons);
        Assert.True(p.AvoidObstacles);

        // Coord properties
        Assert.Equal(10, p.Grid.ToMils(), 0.1);
        Assert.Equal(8, p.TrackSize.ToMils(), 0.1);
        Assert.Equal(5, p.MinTrack.ToMils(), 0.1);
        Assert.Equal(3, p.NeckWidthThreshold.ToMils(), 0.1);
        Assert.Equal(1, p.ArcApproximation.ToMils(), 0.1);
        Assert.Equal(2, p.BorderWidth.ToMils(), 0.1);
        Assert.Equal(4, p.SolderMaskExpansion.ToMils(), 0.1);
        Assert.Equal(3, p.PasteMaskExpansion.ToMils(), 0.1);
        Assert.Equal(10, p.ReliefAirGap.ToMils(), 0.1);
        Assert.Equal(8, p.ReliefConductorWidth.ToMils(), 0.1);
        Assert.Equal(20, p.PowerPlaneClearance.ToMils(), 0.1);
        Assert.Equal(15, p.PowerPlaneReliefExpansion.ToMils(), 0.1);

        // Integer properties
        Assert.Equal(4, p.ReliefEntries);
        Assert.Equal(1, p.PowerPlaneConnectStyle);
        Assert.Equal(2, p.PourIndex);
        Assert.Equal(123456789, p.AreaSize);

        // More boolean flags
        Assert.True(p.PrimitiveLock);
        Assert.True(p.IsHidden);
        Assert.True(p.PourOverSameNetPolygons);
        Assert.False(p.Enabled);
        Assert.True(p.IsKeepout);
        Assert.True(p.PolygonOutline);
        Assert.True(p.Poured);
        Assert.True(p.AutoGenerateName);
        Assert.True(p.ClipAcuteCorners);
        Assert.True(p.DrawDeadCopper);
        Assert.True(p.DrawRemovedIslands);
        Assert.True(p.DrawRemovedNecks);
        Assert.True(p.ExpandOutline);
        Assert.True(p.IgnoreViolations);
        Assert.True(p.MitreCorners);
        Assert.True(p.ObeyPolygonCutout);
        Assert.True(p.OptimalVoidRotation);
        Assert.True(p.AllowGlobalEdit);
        Assert.True(p.Moveable);
        Assert.True(p.ArcPourMode);

        // Vertices
        Assert.Equal(4, p.Vertices.Count);
        Assert.Equal(0, p.Vertices[0].X.ToMils(), 0.1);
        Assert.Equal(1000, p.Vertices[1].X.ToMils(), 0.1);
        Assert.Equal(1000, p.Vertices[2].Y.ToMils(), 0.1);
    }

    [Fact]
    public void Polygon_AdditionalParameters_RoundTrip()
    {
        var doc = new PcbDocument();
        var polygon = new PcbPolygon
        {
            Layer = 1,
            Name = "WithExtras",
            AdditionalParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CUSTOMKEY1"] = "CustomValue1",
                ["CUSTOMKEY2"] = "42"
            }
        };
        polygon.AddVertex(new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)));
        polygon.AddVertex(new CoordPoint(Coord.FromMils(100), Coord.FromMils(0)));
        polygon.AddVertex(new CoordPoint(Coord.FromMils(100), Coord.FromMils(100)));
        doc.AddPolygon(polygon);

        var readBack = RoundTrip(doc);
        var p = readBack.Polygons[0];

        Assert.NotNull(p.AdditionalParameters);
        Assert.Equal("CustomValue1", p.AdditionalParameters["CUSTOMKEY1"]);
        Assert.Equal("42", p.AdditionalParameters["CUSTOMKEY2"]);
    }

    [Fact]
    public void Component_PreservesAllProperties()
    {
        var doc = new PcbDocument();
        var comp = new PcbComponent
        {
            Name = "SOIC-8",
            Description = "8-pin SOIC",
            Height = Coord.FromMils(50),
            Comment = "U1",
            X = Coord.FromMils(1000),
            Y = Coord.FromMils(2000),
            Rotation = 45.0,
            Layer = 1,
            CommentOn = true,
            CommentAutoPosition = 2,
            NameOn = true,
            NameAutoPosition = 3,
            LockStrings = true,
            ComponentKind = 1,
            Enabled = false,
            FlippedOnLayer = true,
            GroupNum = 5,
            IsBGA = true,
            SourceDesignator = "U1",
            SourceLibReference = "SOIC8",
            SourceComponentLibrary = "TestLib.PcbLib",
            SourceDescription = "Source desc",
            SourceFootprintLibrary = "FpLib.PcbLib",
            SourceUniqueId = "SRC001",
            SourceHierarchicalPath = "/Root/Sub",
            SourceCompDesignItemID = "CDID001",
            ItemGUID = "IGUID001",
            ItemRevisionGUID = "IRGUID001",
            VaultGUID = "VGUID001",
            UniqueId = "COMP001",
            ModelHash = "HASH001",
            PackageSpecificHash = "PKHASH001",
            DefaultPCB3DModel = "Model3D"
        };
        doc.AddComponent(comp);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.Components);
        var c = (PcbComponent)readBack.Components[0];

        Assert.Equal("SOIC-8", c.Name);
        Assert.Equal("8-pin SOIC", c.Description);
        Assert.Equal(50, c.Height.ToMils(), 0.1);
        Assert.Equal("U1", c.Comment);
        Assert.Equal(1000, c.X.ToMils(), 0.1);
        Assert.Equal(2000, c.Y.ToMils(), 0.1);
        Assert.Equal(45.0, c.Rotation, 0.01);
        Assert.Equal(1, c.Layer);
        Assert.True(c.CommentOn);
        Assert.Equal(2, c.CommentAutoPosition);
        Assert.True(c.NameOn);
        Assert.Equal(3, c.NameAutoPosition);
        Assert.True(c.LockStrings);
        Assert.Equal(1, c.ComponentKind);
        Assert.False(c.Enabled);
        Assert.True(c.FlippedOnLayer);
        Assert.Equal(5, c.GroupNum);
        Assert.True(c.IsBGA);
        Assert.Equal("U1", c.SourceDesignator);
        Assert.Equal("SOIC8", c.SourceLibReference);
        Assert.Equal("TestLib.PcbLib", c.SourceComponentLibrary);
        Assert.Equal("Source desc", c.SourceDescription);
        Assert.Equal("FpLib.PcbLib", c.SourceFootprintLibrary);
        Assert.Equal("SRC001", c.SourceUniqueId);
        Assert.Equal("/Root/Sub", c.SourceHierarchicalPath);
        Assert.Equal("CDID001", c.SourceCompDesignItemID);
        Assert.Equal("IGUID001", c.ItemGUID);
        Assert.Equal("IRGUID001", c.ItemRevisionGUID);
        Assert.Equal("VGUID001", c.VaultGUID);
        Assert.Equal("COMP001", c.UniqueId);
        Assert.Equal("HASH001", c.ModelHash);
        Assert.Equal("PKHASH001", c.PackageSpecificHash);
        Assert.Equal("Model3D", c.DefaultPCB3DModel);
    }

    [Fact]
    public void Component_AdditionalParameters_RoundTrip()
    {
        var doc = new PcbDocument();
        var comp = new PcbComponent
        {
            Name = "WithExtras",
            AdditionalParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["FUTUREKEY"] = "FutureValue",
                ["EXTRAPROP"] = "123"
            }
        };
        doc.AddComponent(comp);

        var readBack = RoundTrip(doc);
        var c = (PcbComponent)readBack.Components[0];

        Assert.NotNull(c.AdditionalParameters);
        Assert.Equal("FutureValue", c.AdditionalParameters["FUTUREKEY"]);
        Assert.Equal("123", c.AdditionalParameters["EXTRAPROP"]);
    }

    [Fact]
    public void Net_PreservesName()
    {
        var doc = new PcbDocument();
        doc.AddNet(new PcbNet { Name = "VCC" });
        doc.AddNet(new PcbNet { Name = "GND" });
        doc.AddNet(new PcbNet { Name = "DATA[0..7]" });

        var readBack = RoundTrip(doc);
        Assert.Equal(3, readBack.Nets.Count);
        Assert.Equal("VCC", readBack.Nets[0].Name);
        Assert.Equal("GND", readBack.Nets[1].Name);
        Assert.Equal("DATA[0..7]", readBack.Nets[2].Name);
    }

    [Fact]
    public void BoardParameters_RoundTrip()
    {
        var doc = new PcbDocument();
        doc.BoardParameters = new Dictionary<string, string>
        {
            ["BOARDTHICKNESS"] = "62000",
            ["LAYERSTACKSTYLE"] = "0",
            ["SHOWTOPDIELECTRIC"] = "TRUE",
            ["SHOWBOTTOMDIELECTRIC"] = "TRUE",
            ["CUSTOMKEY"] = "CustomValue"
        };

        var readBack = RoundTrip(doc);
        Assert.NotNull(readBack.BoardParameters);
        Assert.Equal("62000", readBack.BoardParameters["BOARDTHICKNESS"]);
        Assert.Equal("0", readBack.BoardParameters["LAYERSTACKSTYLE"]);
        Assert.Equal("TRUE", readBack.BoardParameters["SHOWTOPDIELECTRIC"]);
        Assert.Equal("CustomValue", readBack.BoardParameters["CUSTOMKEY"]);
    }

    [Fact]
    public void EmbeddedBoard_PreservesAllProperties()
    {
        // EmbeddedBoards6 uses parameter-block format with "mil" coord values.
        // Only properties present in the actual file format are tested here.
        var doc = new PcbDocument();
        var board = new PcbEmbeddedBoard
        {
            DocumentPath = @"C:\Designs\SubBoard.PcbDoc",
            Layer = 1,
            Rotation = 45.5,
            MirrorFlag = true,
            OriginMode = 1,
            Scale = 2.5,
            ColCount = 3,
            ColSpacing = Coord.FromMils(500),
            RowCount = 4,
            RowSpacing = Coord.FromMils(300),
            IsKeepout = true,
            PolygonOutline = true,
            UserRouted = true,
            UnionIndex = 5,
            IsViewport = true,
            ViewportTitle = "Main Viewport",
            ViewportVisible = true,
            TitleFontColor = 0xFF0000,
            TitleFontName = "Arial",
            TitleFontSize = 12,
            X1Location = Coord.FromMils(100),
            Y1Location = Coord.FromMils(200),
            X2Location = Coord.FromMils(5000),
            Y2Location = Coord.FromMils(4000)
        };
        doc.AddEmbeddedBoard(board);

        var readBack = RoundTrip(doc);
        Assert.Single(readBack.EmbeddedBoards);
        var eb = readBack.EmbeddedBoards[0];

        Assert.Equal(@"C:\Designs\SubBoard.PcbDoc", eb.DocumentPath);
        Assert.Equal(1, eb.Layer);
        Assert.Equal(45.5, eb.Rotation, 0.01);
        Assert.True(eb.MirrorFlag);
        Assert.Equal(1, eb.OriginMode);
        Assert.Equal(2.5, eb.Scale, 0.01);
        Assert.Equal(3, eb.ColCount);
        Assert.Equal(500, eb.ColSpacing.ToMils(), 0.1);
        Assert.Equal(4, eb.RowCount);
        Assert.Equal(300, eb.RowSpacing.ToMils(), 0.1);
        Assert.True(eb.IsKeepout);
        Assert.True(eb.PolygonOutline);
        Assert.True(eb.UserRouted);
        Assert.Equal(5, eb.UnionIndex);
        Assert.True(eb.IsViewport);
        Assert.Equal("Main Viewport", eb.ViewportTitle);
        Assert.True(eb.ViewportVisible);
        Assert.Equal(0xFF0000, eb.TitleFontColor);
        Assert.Equal("Arial", eb.TitleFontName);
        Assert.Equal(12, eb.TitleFontSize);
        Assert.Equal(100, eb.X1Location.ToMils(), 0.1);
        Assert.Equal(200, eb.Y1Location.ToMils(), 0.1);
        Assert.Equal(5000, eb.X2Location.ToMils(), 0.1);
        Assert.Equal(4000, eb.Y2Location.ToMils(), 0.1);
    }

    [Fact]
    public void AdditionalStreams_RoundTrip()
    {
        var doc = new PcbDocument();
        var testData1 = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var testData2 = new byte[] { 0xAA, 0xBB, 0xCC };
        doc.AdditionalStreams = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["CustomStorage/Data"] = testData1,
            ["CustomStorage/Header"] = testData2
        };

        // Also add at least one known element so the file is valid
        doc.AddTrack(PcbTrack.Create()
            .From(Coord.FromMils(0), Coord.FromMils(0))
            .To(Coord.FromMils(100), Coord.FromMils(0))
            .Width(Coord.FromMils(10))
            .Build());

        var readBack = RoundTrip(doc);
        Assert.NotNull(readBack.AdditionalStreams);
        Assert.True(readBack.AdditionalStreams.ContainsKey("CustomStorage/Data"));
        Assert.Equal(testData1, readBack.AdditionalStreams["CustomStorage/Data"]);
        Assert.True(readBack.AdditionalStreams.ContainsKey("CustomStorage/Header"));
        Assert.Equal(testData2, readBack.AdditionalStreams["CustomStorage/Header"]);
    }
}
