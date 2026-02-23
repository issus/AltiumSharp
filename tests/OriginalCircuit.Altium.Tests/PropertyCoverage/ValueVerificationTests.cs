using System.Text.Json;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Serialization.Readers;
using Xunit;
using Xunit.Abstractions;

namespace OriginalCircuit.Altium.Tests.PropertyCoverage;

/// <summary>
/// Verifies that property VALUES read from binary files match the expected values
/// in JSON test data. This catches data loss that structural coverage tests miss.
/// </summary>
public sealed class ValueVerificationTests : CoverageTestBase
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Altium PcbLib stores primitive coordinates relative to component origin (0,0).
    /// The Altium API reports absolute coordinates where the board origin is at 500M,500M internal units.
    /// This offset is subtracted from JSON values when comparing position coordinates.
    /// </summary>
    private const int PcbLibOriginOffset = 500_000_000;

    public ValueVerificationTests(ITestOutputHelper output) => _output = output;

    [SkippableFact]
    public void SchLib_ComponentValues_MatchJson()
    {
        var errors = new List<string>();
        var dir = GetSchTestDataPath();
        if (!Directory.Exists(dir)) { Skip.If(true, "Test data not available"); return; }

        foreach (var jsonFile in Directory.GetFiles(dir, "*.json"))
        {
            var binaryFile = Path.ChangeExtension(jsonFile, ".SchLib");
            if (!File.Exists(binaryFile)) continue;

            using var doc = LoadJson(jsonFile);
            if (doc == null) continue;

            var jsonComponents = GetSchComponents(doc);
            if (jsonComponents.Count == 0) continue;

            SchLibrary lib;
            using (var stream = File.OpenRead(binaryFile))
                lib = new SchLibReader().Read(stream);

            var fileName = Path.GetFileName(jsonFile);

            if (lib.Components.Count != jsonComponents.Count)
            {
                errors.Add($"{fileName}: component count mismatch: JSON={jsonComponents.Count}, reader={lib.Components.Count}");
                continue;
            }

            for (var i = 0; i < jsonComponents.Count; i++)
            {
                var jc = jsonComponents[i];
                var rc = (SchComponent)lib.Components[i];

                CompareOptional(errors, fileName, "Component", rc.Name, "libReference", jc, rc.Name);
                CompareOptional(errors, fileName, "Component", rc.Name, "description", jc, rc.Description);
                CompareOptional(errors, fileName, "Component", rc.Name, "designator", jc, rc.DesignatorPrefix);
                CompareOptional(errors, fileName, "Component", rc.Name, "comment", jc, rc.Comment);
                CompareInt(errors, fileName, "Component", rc.Name, "partCount", jc, rc.PartCount);
                CompareOptional(errors, fileName, "Component", rc.Name, "uniqueId", jc, rc.UniqueId);
            }
        }

        ReportErrors(errors);
    }

    [SkippableFact]
    public void SchLib_PinValues_MatchJson()
    {
        var errors = new List<string>();
        var dir = GetSchTestDataPath();
        if (!Directory.Exists(dir)) { Skip.If(true, "Test data not available"); return; }

        foreach (var jsonFile in Directory.GetFiles(dir, "*.json"))
        {
            var binaryFile = Path.ChangeExtension(jsonFile, ".SchLib");
            if (!File.Exists(binaryFile)) continue;

            using var doc = LoadJson(jsonFile);
            if (doc == null) continue;

            SchLibrary lib;
            using (var stream = File.OpenRead(binaryFile))
                lib = new SchLibReader().Read(stream);

            var fileName = Path.GetFileName(jsonFile);

            // For each component, compare its pins
            var jsonSymbols = doc.RootElement.GetProperty("symbols").EnumerateArray().ToList();
            for (var ci = 0; ci < lib.Components.Count && ci < jsonSymbols.Count; ci++)
            {
                var comp = (SchComponent)lib.Components[ci];
                var jSymbol = jsonSymbols[ci];
                if (!jSymbol.TryGetProperty("primitives", out var jPrims)) continue;

                var jsonPins = jPrims.EnumerateArray()
                    .Where(p => p.GetProperty("objectType").GetString() == "Pin")
                    .ToList();

                if (comp.Pins.Count != jsonPins.Count)
                {
                    errors.Add($"{fileName}/{comp.Name}: pin count mismatch: JSON={jsonPins.Count}, reader={comp.Pins.Count}");
                    continue;
                }

                for (var pi = 0; pi < jsonPins.Count; pi++)
                {
                    var jp = jsonPins[pi];
                    var rp = (SchPin)comp.Pins[pi];
                    var ctx = $"{fileName}/{comp.Name}/Pin[{pi}]";

                    CompareOptional(errors, ctx, "Pin", rp.Designator ?? "", "name", jp, rp.Name);
                    CompareOptional(errors, ctx, "Pin", rp.Designator ?? "", "designator", jp, rp.Designator);
                    CompareInt(errors, ctx, "Pin", rp.Designator ?? "", "electrical", jp, (int)rp.ElectricalType);
                    CompareInt(errors, ctx, "Pin", rp.Designator ?? "", "pinLength", jp, rp.Length.ToRaw());
                    CompareInt(errors, ctx, "Pin", rp.Designator ?? "", "x", jp, rp.Location.X.ToRaw());
                    CompareInt(errors, ctx, "Pin", rp.Designator ?? "", "y", jp, rp.Location.Y.ToRaw());
                    CompareInt(errors, ctx, "Pin", rp.Designator ?? "", "orientation", jp, (int)rp.Orientation);
                    CompareBool(errors, ctx, "Pin", rp.Designator ?? "", "showName", jp, rp.ShowName);
                    CompareBool(errors, ctx, "Pin", rp.Designator ?? "", "showDesignator", jp, rp.ShowDesignator);
                    CompareInt(errors, ctx, "Pin", rp.Designator ?? "", "symbol_Inner", jp, rp.SymbolInside);
                    CompareInt(errors, ctx, "Pin", rp.Designator ?? "", "symbol_InnerEdge", jp, rp.SymbolInnerEdge);
                    CompareInt(errors, ctx, "Pin", rp.Designator ?? "", "symbol_Outer", jp, rp.SymbolOutside);
                    CompareInt(errors, ctx, "Pin", rp.Designator ?? "", "symbol_OuterEdge", jp, rp.SymbolOuterEdge);
                    CompareInt(errors, ctx, "Pin", rp.Designator ?? "", "symbol_LineWidth", jp, rp.SymbolLineWidth);
                }
            }
        }

        ReportErrors(errors);
    }

    [SkippableFact]
    public void SchLib_RectangleValues_MatchJson()
    {
        var errors = new List<string>();
        var dir = GetSchTestDataPath();
        if (!Directory.Exists(dir)) { Skip.If(true, "Test data not available"); return; }

        foreach (var jsonFile in Directory.GetFiles(dir, "*.json"))
        {
            var binaryFile = Path.ChangeExtension(jsonFile, ".SchLib");
            if (!File.Exists(binaryFile)) continue;

            using var doc = LoadJson(jsonFile);
            if (doc == null) continue;

            SchLibrary lib;
            using (var stream = File.OpenRead(binaryFile))
                lib = new SchLibReader().Read(stream);

            var fileName = Path.GetFileName(jsonFile);
            var jsonSymbols = doc.RootElement.GetProperty("symbols").EnumerateArray().ToList();

            for (var ci = 0; ci < lib.Components.Count && ci < jsonSymbols.Count; ci++)
            {
                var comp = (SchComponent)lib.Components[ci];
                var jSymbol = jsonSymbols[ci];
                if (!jSymbol.TryGetProperty("primitives", out var jPrims)) continue;

                var jsonRects = jPrims.EnumerateArray()
                    .Where(p => p.GetProperty("objectType").GetString() == "Rectangle")
                    .ToList();

                if (comp.Rectangles.Count != jsonRects.Count) continue;

                for (var ri = 0; ri < jsonRects.Count; ri++)
                {
                    var jr = jsonRects[ri];
                    var rr = (SchRectangle)comp.Rectangles[ri];
                    var ctx = $"{fileName}/{comp.Name}/Rect[{ri}]";

                    CompareInt(errors, ctx, "Rectangle", "", "x1", jr, rr.Corner1.X.ToRaw());
                    CompareInt(errors, ctx, "Rectangle", "", "y1", jr, rr.Corner1.Y.ToRaw());
                    CompareInt(errors, ctx, "Rectangle", "", "x2", jr, rr.Corner2.X.ToRaw());
                    CompareInt(errors, ctx, "Rectangle", "", "y2", jr, rr.Corner2.Y.ToRaw());
                    CompareInt(errors, ctx, "Rectangle", "", "color", jr, rr.Color);
                    CompareInt(errors, ctx, "Rectangle", "", "areaColor", jr, rr.FillColor);
                    CompareBool(errors, ctx, "Rectangle", "", "isSolid", jr, rr.IsFilled);
                }
            }
        }

        ReportErrors(errors);
    }

    [SkippableFact]
    public void SchLib_LineValues_MatchJson()
    {
        var errors = new List<string>();
        var dir = GetSchTestDataPath();
        if (!Directory.Exists(dir)) { Skip.If(true, "Test data not available"); return; }

        foreach (var jsonFile in Directory.GetFiles(dir, "*.json"))
        {
            var binaryFile = Path.ChangeExtension(jsonFile, ".SchLib");
            if (!File.Exists(binaryFile)) continue;

            using var doc = LoadJson(jsonFile);
            if (doc == null) continue;

            SchLibrary lib;
            using (var stream = File.OpenRead(binaryFile))
                lib = new SchLibReader().Read(stream);

            var fileName = Path.GetFileName(jsonFile);
            var jsonSymbols = doc.RootElement.GetProperty("symbols").EnumerateArray().ToList();

            for (var ci = 0; ci < lib.Components.Count && ci < jsonSymbols.Count; ci++)
            {
                var comp = (SchComponent)lib.Components[ci];
                var jSymbol = jsonSymbols[ci];
                if (!jSymbol.TryGetProperty("primitives", out var jPrims)) continue;

                var jsonLines = jPrims.EnumerateArray()
                    .Where(p => p.GetProperty("objectType").GetString() == "Line")
                    .ToList();

                if (comp.Lines.Count != jsonLines.Count) continue;

                for (var li = 0; li < jsonLines.Count; li++)
                {
                    var jl = jsonLines[li];
                    var rl = (SchLine)comp.Lines[li];
                    var ctx = $"{fileName}/{comp.Name}/Line[{li}]";

                    CompareInt(errors, ctx, "Line", "", "x1", jl, rl.Start.X.ToRaw());
                    CompareInt(errors, ctx, "Line", "", "y1", jl, rl.Start.Y.ToRaw());
                    CompareInt(errors, ctx, "Line", "", "x2", jl, rl.End.X.ToRaw());
                    CompareInt(errors, ctx, "Line", "", "y2", jl, rl.End.Y.ToRaw());
                    CompareInt(errors, ctx, "Line", "", "color", jl, rl.Color);
                }
            }
        }

        ReportErrors(errors);
    }

    [SkippableFact]
    public void SchLib_ArcValues_MatchJson()
    {
        var errors = new List<string>();
        var dir = GetSchTestDataPath();
        if (!Directory.Exists(dir)) { Skip.If(true, "Test data not available"); return; }

        foreach (var jsonFile in Directory.GetFiles(dir, "*.json"))
        {
            var binaryFile = Path.ChangeExtension(jsonFile, ".SchLib");
            if (!File.Exists(binaryFile)) continue;

            using var doc = LoadJson(jsonFile);
            if (doc == null) continue;

            SchLibrary lib;
            using (var stream = File.OpenRead(binaryFile))
                lib = new SchLibReader().Read(stream);

            var fileName = Path.GetFileName(jsonFile);
            var jsonSymbols = doc.RootElement.GetProperty("symbols").EnumerateArray().ToList();

            for (var ci = 0; ci < lib.Components.Count && ci < jsonSymbols.Count; ci++)
            {
                var comp = (SchComponent)lib.Components[ci];
                var jSymbol = jsonSymbols[ci];
                if (!jSymbol.TryGetProperty("primitives", out var jPrims)) continue;

                var jsonArcs = jPrims.EnumerateArray()
                    .Where(p => p.GetProperty("objectType").GetString() == "Arc")
                    .ToList();

                if (comp.Arcs.Count != jsonArcs.Count) continue;

                for (var ai = 0; ai < jsonArcs.Count; ai++)
                {
                    var ja = jsonArcs[ai];
                    var ra = (SchArc)comp.Arcs[ai];
                    var ctx = $"{fileName}/{comp.Name}/Arc[{ai}]";

                    CompareInt(errors, ctx, "Arc", "", "x", ja, ra.Center.X.ToRaw());
                    CompareInt(errors, ctx, "Arc", "", "y", ja, ra.Center.Y.ToRaw());
                    CompareInt(errors, ctx, "Arc", "", "radius", ja, ra.Radius.ToRaw());
                    CompareDouble(errors, ctx, "Arc", "", "startAngle", ja, ra.StartAngle);
                    CompareDouble(errors, ctx, "Arc", "", "endAngle", ja, ra.EndAngle);
                    CompareInt(errors, ctx, "Arc", "", "color", ja, ra.Color);
                }
            }
        }

        ReportErrors(errors);
    }

    [SkippableFact]
    public void SchLib_PolygonValues_MatchJson()
    {
        var errors = new List<string>();
        var dir = GetSchTestDataPath();
        if (!Directory.Exists(dir)) { Skip.If(true, "Test data not available"); return; }

        foreach (var jsonFile in Directory.GetFiles(dir, "*.json"))
        {
            var binaryFile = Path.ChangeExtension(jsonFile, ".SchLib");
            if (!File.Exists(binaryFile)) continue;

            using var doc = LoadJson(jsonFile);
            if (doc == null) continue;

            SchLibrary lib;
            using (var stream = File.OpenRead(binaryFile))
                lib = new SchLibReader().Read(stream);

            var fileName = Path.GetFileName(jsonFile);
            var jsonSymbols = doc.RootElement.GetProperty("symbols").EnumerateArray().ToList();

            for (var ci = 0; ci < lib.Components.Count && ci < jsonSymbols.Count; ci++)
            {
                var comp = (SchComponent)lib.Components[ci];
                var jSymbol = jsonSymbols[ci];
                if (!jSymbol.TryGetProperty("primitives", out var jPrims)) continue;

                var jsonPolys = jPrims.EnumerateArray()
                    .Where(p => p.GetProperty("objectType").GetString() == "Polygon")
                    .ToList();

                if (comp.Polygons.Count != jsonPolys.Count) continue;

                for (var pi = 0; pi < jsonPolys.Count; pi++)
                {
                    var jp = jsonPolys[pi];
                    var rp = (SchPolygon)comp.Polygons[pi];
                    var ctx = $"{fileName}/{comp.Name}/Polygon[{pi}]";

                    CompareInt(errors, ctx, "Polygon", "", "color", jp, rp.Color);
                    CompareInt(errors, ctx, "Polygon", "", "areaColor", jp, rp.FillColor);
                    CompareBool(errors, ctx, "Polygon", "", "isSolid", jp, rp.IsFilled);

                    // Compare vertex count
                    if (jp.TryGetProperty("vertices", out var vertices))
                    {
                        var jvCount = vertices.GetArrayLength();
                        if (jvCount != rp.Vertices.Count)
                            errors.Add($"{ctx}: vertex count mismatch: JSON={jvCount}, reader={rp.Vertices.Count}");
                    }
                }
            }
        }

        ReportErrors(errors);
    }

    [SkippableFact]
    public void PcbLib_PadValues_MatchJson()
    {
        var errors = new List<string>();
        var dir = GetPcbTestDataPath();
        if (!Directory.Exists(dir)) { Skip.If(true, "Test data not available"); return; }

        foreach (var jsonFile in Directory.GetFiles(dir, "PAD_*.json"))
        {
            var binaryFile = Path.ChangeExtension(jsonFile, ".PcbLib");
            if (!File.Exists(binaryFile)) continue;

            using var doc = LoadJson(jsonFile);
            if (doc == null) continue;

            PcbLibrary lib;
            using (var stream = File.OpenRead(binaryFile))
                lib = new PcbLibReader().Read(stream);

            var fileName = Path.GetFileName(jsonFile);
            var jsonFootprints = doc.RootElement.GetProperty("footprints").EnumerateArray().ToList();

            for (var fi = 0; fi < lib.Components.Count && fi < jsonFootprints.Count; fi++)
            {
                var comp = (PcbComponent)lib.Components[fi];
                var jFp = jsonFootprints[fi];
                var jsonPads = jFp.GetProperty("primitives").EnumerateArray()
                    .Where(p => p.GetProperty("objectType").GetString() == "Pad")
                    .ToList();

                if (comp.Pads.Count != jsonPads.Count) continue;

                for (var pi = 0; pi < jsonPads.Count; pi++)
                {
                    var jp = jsonPads[pi];
                    var rp = (PcbPad)comp.Pads[pi];
                    var ctx = $"{fileName}/{comp.Name}/Pad[{pi}]";

                    CompareOptional(errors, ctx, "Pad", rp.Designator ?? "", "name", jp, rp.Designator);
                    CompareCoord(errors, ctx, "Pad", "x", jp, rp.Location.X.ToRaw(), PcbLibOriginOffset);
                    CompareCoord(errors, ctx, "Pad", "y", jp, rp.Location.Y.ToRaw(), PcbLibOriginOffset);
                    CompareCoord(errors, ctx, "Pad", "topXSize", jp, rp.SizeTop.X.ToRaw());
                    CompareCoord(errors, ctx, "Pad", "topYSize", jp, rp.SizeTop.Y.ToRaw());
                    CompareCoord(errors, ctx, "Pad", "midXSize", jp, rp.SizeMiddle.X.ToRaw());
                    CompareCoord(errors, ctx, "Pad", "midYSize", jp, rp.SizeMiddle.Y.ToRaw());
                    CompareCoord(errors, ctx, "Pad", "botXSize", jp, rp.SizeBottom.X.ToRaw());
                    CompareCoord(errors, ctx, "Pad", "botYSize", jp, rp.SizeBottom.Y.ToRaw());
                    CompareCoord(errors, ctx, "Pad", "holeSize", jp, rp.HoleSize.ToRaw());
                    CompareInt(errors, ctx, "Pad", rp.Designator ?? "", "topShape", jp, (int)rp.ShapeTop);
                    CompareInt(errors, ctx, "Pad", rp.Designator ?? "", "midShape", jp, (int)rp.ShapeMiddle);
                    CompareInt(errors, ctx, "Pad", rp.Designator ?? "", "botShape", jp, (int)rp.ShapeBottom);
                    CompareDouble(errors, ctx, "Pad", rp.Designator ?? "", "rotation", jp, rp.Rotation);
                    CompareBool(errors, ctx, "Pad", rp.Designator ?? "", "plated", jp, rp.IsPlated);
                    CompareBool(errors, ctx, "Pad", rp.Designator ?? "", "enabled", jp, rp.Enabled);
                }
            }
        }

        ReportErrors(errors);
    }

    [SkippableFact]
    public void PcbLib_TrackValues_MatchJson()
    {
        var errors = new List<string>();
        var dir = GetPcbTestDataPath();
        if (!Directory.Exists(dir)) { Skip.If(true, "Test data not available"); return; }

        foreach (var jsonFile in Directory.GetFiles(dir, "TRACK*.json"))
        {
            var binaryFile = Path.ChangeExtension(jsonFile, ".PcbLib");
            if (!File.Exists(binaryFile)) continue;

            using var doc = LoadJson(jsonFile);
            if (doc == null) continue;

            PcbLibrary lib;
            using (var stream = File.OpenRead(binaryFile))
                lib = new PcbLibReader().Read(stream);

            var fileName = Path.GetFileName(jsonFile);
            var jsonFootprints = doc.RootElement.GetProperty("footprints").EnumerateArray().ToList();

            for (var fi = 0; fi < lib.Components.Count && fi < jsonFootprints.Count; fi++)
            {
                var comp = (PcbComponent)lib.Components[fi];
                var jFp = jsonFootprints[fi];
                var jsonTracks = jFp.GetProperty("primitives").EnumerateArray()
                    .Where(p => p.GetProperty("objectType").GetString() == "Track")
                    .ToList();

                if (comp.Tracks.Count != jsonTracks.Count) continue;

                for (var ti = 0; ti < jsonTracks.Count; ti++)
                {
                    var jt = jsonTracks[ti];
                    var rt = (PcbTrack)comp.Tracks[ti];
                    var ctx = $"{fileName}/{comp.Name}/Track[{ti}]";

                    CompareCoord(errors, ctx, "Track", "x1", jt, rt.Start.X.ToRaw(), PcbLibOriginOffset);
                    CompareCoord(errors, ctx, "Track", "y1", jt, rt.Start.Y.ToRaw(), PcbLibOriginOffset);
                    CompareCoord(errors, ctx, "Track", "x2", jt, rt.End.X.ToRaw(), PcbLibOriginOffset);
                    CompareCoord(errors, ctx, "Track", "y2", jt, rt.End.Y.ToRaw(), PcbLibOriginOffset);
                    CompareCoord(errors, ctx, "Track", "width", jt, rt.Width.ToRaw());
                    CompareInt(errors, ctx, "Track", "", "layer", jt, rt.Layer);
                }
            }
        }

        ReportErrors(errors);
    }

    [SkippableFact]
    public void PcbLib_ViaValues_MatchJson()
    {
        var errors = new List<string>();
        var dir = GetPcbTestDataPath();
        if (!Directory.Exists(dir)) { Skip.If(true, "Test data not available"); return; }

        foreach (var jsonFile in Directory.GetFiles(dir, "VIA_*.json"))
        {
            var binaryFile = Path.ChangeExtension(jsonFile, ".PcbLib");
            if (!File.Exists(binaryFile)) continue;

            using var doc = LoadJson(jsonFile);
            if (doc == null) continue;

            PcbLibrary lib;
            using (var stream = File.OpenRead(binaryFile))
                lib = new PcbLibReader().Read(stream);

            var fileName = Path.GetFileName(jsonFile);
            var jsonFootprints = doc.RootElement.GetProperty("footprints").EnumerateArray().ToList();

            for (var fi = 0; fi < lib.Components.Count && fi < jsonFootprints.Count; fi++)
            {
                var comp = (PcbComponent)lib.Components[fi];
                var jFp = jsonFootprints[fi];
                var jsonVias = jFp.GetProperty("primitives").EnumerateArray()
                    .Where(p => p.GetProperty("objectType").GetString() == "Via")
                    .ToList();

                if (comp.Vias.Count != jsonVias.Count) continue;

                for (var vi = 0; vi < jsonVias.Count; vi++)
                {
                    var jv = jsonVias[vi];
                    var rv = (PcbVia)comp.Vias[vi];
                    var ctx = $"{fileName}/{comp.Name}/Via[{vi}]";

                    CompareCoord(errors, ctx, "Via", "x", jv, rv.Location.X.ToRaw(), PcbLibOriginOffset);
                    CompareCoord(errors, ctx, "Via", "y", jv, rv.Location.Y.ToRaw(), PcbLibOriginOffset);
                    CompareCoord(errors, ctx, "Via", "diameter", jv, rv.Diameter.ToRaw());
                    CompareCoord(errors, ctx, "Via", "holeSize", jv, rv.HoleSize.ToRaw());
                    CompareInt(errors, ctx, "Via", "", "startLayer", jv, rv.StartLayer);
                    CompareInt(errors, ctx, "Via", "", "endLayer", jv, rv.EndLayer);
                }
            }
        }

        ReportErrors(errors);
    }

    [SkippableFact]
    public void PcbLib_ArcValues_MatchJson()
    {
        var errors = new List<string>();
        var dir = GetPcbTestDataPath();
        if (!Directory.Exists(dir)) { Skip.If(true, "Test data not available"); return; }

        foreach (var jsonFile in Directory.GetFiles(dir, "ARC*.json"))
        {
            var binaryFile = Path.ChangeExtension(jsonFile, ".PcbLib");
            if (!File.Exists(binaryFile)) continue;

            using var doc = LoadJson(jsonFile);
            if (doc == null) continue;

            PcbLibrary lib;
            using (var stream = File.OpenRead(binaryFile))
                lib = new PcbLibReader().Read(stream);

            var fileName = Path.GetFileName(jsonFile);
            var jsonFootprints = doc.RootElement.GetProperty("footprints").EnumerateArray().ToList();

            for (var fi = 0; fi < lib.Components.Count && fi < jsonFootprints.Count; fi++)
            {
                var comp = (PcbComponent)lib.Components[fi];
                var jFp = jsonFootprints[fi];
                var jsonArcs = jFp.GetProperty("primitives").EnumerateArray()
                    .Where(p => p.GetProperty("objectType").GetString() == "Arc")
                    .ToList();

                if (comp.Arcs.Count != jsonArcs.Count) continue;

                for (var ai = 0; ai < jsonArcs.Count; ai++)
                {
                    var ja = jsonArcs[ai];
                    var ra = (PcbArc)comp.Arcs[ai];
                    var ctx = $"{fileName}/{comp.Name}/Arc[{ai}]";

                    CompareCoord(errors, ctx, "Arc", "x", ja, ra.Center.X.ToRaw(), PcbLibOriginOffset);
                    CompareCoord(errors, ctx, "Arc", "y", ja, ra.Center.Y.ToRaw(), PcbLibOriginOffset);
                    CompareCoord(errors, ctx, "Arc", "radius", ja, ra.Radius.ToRaw());
                    CompareCoord(errors, ctx, "Arc", "width", ja, ra.Width.ToRaw());
                    CompareDouble(errors, ctx, "Arc", "", "startAngle", ja, ra.StartAngle);
                    CompareDouble(errors, ctx, "Arc", "", "endAngle", ja, ra.EndAngle);
                    CompareInt(errors, ctx, "Arc", "", "layer", ja, ra.Layer);
                }
            }
        }

        ReportErrors(errors);
    }

    [SkippableFact]
    public void PcbLib_TextValues_MatchJson()
    {
        var errors = new List<string>();
        var dir = GetPcbTestDataPath();
        if (!Directory.Exists(dir)) { Skip.If(true, "Test data not available"); return; }

        foreach (var jsonFile in Directory.GetFiles(dir, "TEXT_*.json"))
        {
            var binaryFile = Path.ChangeExtension(jsonFile, ".PcbLib");
            if (!File.Exists(binaryFile)) continue;

            using var doc = LoadJson(jsonFile);
            if (doc == null) continue;

            PcbLibrary lib;
            using (var stream = File.OpenRead(binaryFile))
                lib = new PcbLibReader().Read(stream);

            var fileName = Path.GetFileName(jsonFile);
            var jsonFootprints = doc.RootElement.GetProperty("footprints").EnumerateArray().ToList();

            for (var fi = 0; fi < lib.Components.Count && fi < jsonFootprints.Count; fi++)
            {
                var comp = (PcbComponent)lib.Components[fi];
                var jFp = jsonFootprints[fi];
                var jsonTexts = jFp.GetProperty("primitives").EnumerateArray()
                    .Where(p => p.GetProperty("objectType").GetString() == "Text")
                    .ToList();

                if (comp.Texts.Count != jsonTexts.Count) continue;

                for (var ti = 0; ti < jsonTexts.Count; ti++)
                {
                    var jt = jsonTexts[ti];
                    var rt = (PcbText)comp.Texts[ti];
                    var ctx = $"{fileName}/{comp.Name}/Text[{ti}]";

                    CompareOptional(errors, ctx, "Text", rt.Text, "text", jt, rt.Text);
                    CompareCoord(errors, ctx, "Text", "x", jt, rt.Location.X.ToRaw(), PcbLibOriginOffset);
                    CompareCoord(errors, ctx, "Text", "y", jt, rt.Location.Y.ToRaw(), PcbLibOriginOffset);
                    CompareCoord(errors, ctx, "Text", "height", jt, rt.Height.ToRaw());
                    CompareDouble(errors, ctx, "Text", rt.Text, "rotation", jt, rt.Rotation);
                    CompareInt(errors, ctx, "Text", rt.Text, "layer", jt, rt.Layer);
                    CompareBool(errors, ctx, "Text", rt.Text, "isMirrored", jt, rt.IsMirrored);
                    CompareBool(errors, ctx, "Text", rt.Text, "isTrueType", jt, rt.IsTrueType);
                    CompareBool(errors, ctx, "Text", rt.Text, "isInverted", jt, rt.IsInverted);
                }
            }
        }

        ReportErrors(errors);
    }

    [SkippableFact]
    public void PcbLib_RegionValues_MatchJson()
    {
        var errors = new List<string>();
        var dir = GetPcbTestDataPath();
        if (!Directory.Exists(dir)) { Skip.If(true, "Test data not available"); return; }

        foreach (var jsonFile in Directory.GetFiles(dir, "REGION*.json"))
        {
            var binaryFile = Path.ChangeExtension(jsonFile, ".PcbLib");
            if (!File.Exists(binaryFile)) continue;

            using var doc = LoadJson(jsonFile);
            if (doc == null) continue;

            PcbLibrary lib;
            using (var stream = File.OpenRead(binaryFile))
                lib = new PcbLibReader().Read(stream);

            var fileName = Path.GetFileName(jsonFile);
            var jsonFootprints = doc.RootElement.GetProperty("footprints").EnumerateArray().ToList();

            for (var fi = 0; fi < lib.Components.Count && fi < jsonFootprints.Count; fi++)
            {
                var comp = (PcbComponent)lib.Components[fi];
                var jFp = jsonFootprints[fi];
                var jsonRegions = jFp.GetProperty("primitives").EnumerateArray()
                    .Where(p => p.GetProperty("objectType").GetString() == "Region")
                    .ToList();

                if (comp.Regions.Count != jsonRegions.Count) continue;

                for (var ri = 0; ri < jsonRegions.Count; ri++)
                {
                    var jr = jsonRegions[ri];
                    var rr = (PcbRegion)comp.Regions[ri];
                    var ctx = $"{fileName}/{comp.Name}/Region[{ri}]";

                    CompareInt(errors, ctx, "Region", "", "layer", jr, rr.Layer);
                    CompareInt(errors, ctx, "Region", "", "kind", jr, rr.Kind);
                    CompareBool(errors, ctx, "Region", "", "isKeepout", jr, rr.IsKeepout);

                    // Compare vertex count
                    if (jr.TryGetProperty("vertices", out var vertices))
                    {
                        var jvCount = vertices.GetArrayLength();
                        if (jvCount != rr.Outline.Count)
                            errors.Add($"{ctx}: vertex count mismatch: JSON={jvCount}, reader={rr.Outline.Count}");
                    }
                }
            }
        }

        ReportErrors(errors);
    }

    #region Comparison helpers

    private static void CompareOptional(List<string> errors, string ctx, string type, string id, string prop, JsonElement json, string? actual)
    {
        if (!json.TryGetProperty(prop, out var jval)) return;
        var expected = jval.GetString();
        if (expected != actual)
            errors.Add($"{ctx}.{prop}: expected=\"{expected}\", actual=\"{actual}\"");
    }

    private static void CompareInt(List<string> errors, string ctx, string type, string id, string prop, JsonElement json, int actual)
    {
        if (!json.TryGetProperty(prop, out var jval)) return;
        if (jval.ValueKind != JsonValueKind.Number) return;
        var expected = jval.GetInt32();
        if (expected != actual)
            errors.Add($"{ctx}.{prop}: expected={expected}, actual={actual}");
    }

    private static void CompareDouble(List<string> errors, string ctx, string type, string id, string prop, JsonElement json, double actual)
    {
        if (!json.TryGetProperty(prop, out var jval)) return;
        if (jval.ValueKind != JsonValueKind.Number) return;
        var expected = jval.GetDouble();
        if (Math.Abs(expected - actual) > 0.01)
            errors.Add($"{ctx}.{prop}: expected={expected}, actual={actual}");
    }

    private static void CompareBool(List<string> errors, string ctx, string type, string id, string prop, JsonElement json, bool actual)
    {
        if (!json.TryGetProperty(prop, out var jval)) return;
        if (jval.ValueKind != JsonValueKind.True && jval.ValueKind != JsonValueKind.False) return;
        var expected = jval.GetBoolean();
        if (expected != actual)
            errors.Add($"{ctx}.{prop}: expected={expected}, actual={actual}");
    }

    /// <summary>
    /// Compare a PCB coordinate value. PCB JSON stores coords as {internal, mils, mm} objects.
    /// </summary>
    private static void CompareCoord(List<string> errors, string ctx, string type, string prop, JsonElement json, int actualRaw, int offset = 0)
    {
        if (!json.TryGetProperty(prop, out var jval)) return;
        if (jval.ValueKind == JsonValueKind.Object && jval.TryGetProperty("internal", out var internalVal))
        {
            var expected = internalVal.GetInt32() - offset;
            if (expected != actualRaw)
                errors.Add($"{ctx}.{prop}: expected={expected + offset}, actual={actualRaw + offset}");
        }
        else if (jval.ValueKind == JsonValueKind.Number)
        {
            var expected = jval.GetInt32() - offset;
            if (expected != actualRaw)
                errors.Add($"{ctx}.{prop}: expected={expected + offset}, actual={actualRaw + offset}");
        }
    }

    private void ReportErrors(List<string> errors)
    {
        if (errors.Count == 0)
        {
            _output.WriteLine("All values match.");
            return;
        }

        _output.WriteLine($"{errors.Count} value mismatches:");
        foreach (var error in errors.Take(50))
            _output.WriteLine($"  {error}");
        if (errors.Count > 50)
            _output.WriteLine($"  ... and {errors.Count - 50} more");

        Assert.Fail($"{errors.Count} property value mismatches found. First 10:\n" +
            string.Join("\n", errors.Take(10)));
    }

    #endregion
}
