using System.Reflection;
using System.Security.Cryptography;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.AltiumSharp.Export.Models;
using V2Coord = OriginalCircuit.Altium.Primitives.Coord;
using V2CoordPoint = OriginalCircuit.Altium.Primitives.CoordPoint;
using V2SchLibReader = OriginalCircuit.Altium.Serialization.Readers.SchLibReader;

namespace OriginalCircuit.AltiumSharp.Export.Exporters;

/// <summary>
/// Exports schematic library files (.SchLib) to JSON format using the v2 reader.
/// </summary>
public sealed class SchLibExporter
{
    private readonly ExportOptions _options;
    private readonly List<string> _warnings = [];
    private readonly List<string> _errors = [];

    public SchLibExporter(ExportOptions? options = null)
    {
        _options = options ?? new ExportOptions();
    }

    /// <summary>
    /// Export a schematic library file.
    /// </summary>
    public ExportResult Export(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        var metadata = CreateMetadata(fileInfo);
        var result = new ExportResult { Metadata = metadata };

        // Export raw MCDF structure
        if (_options.IncludeRawMcdfStructure)
        {
            using var mcdfExporter = new McdfExporter(_options);
            var mcdfResult = mcdfExporter.Export(filePath);
            result.RawMcdf = mcdfResult.RawMcdf;
            _warnings.AddRange(mcdfExporter.Warnings);
        }

        // Export parsed model
        if (_options.IncludeParsedModel)
        {
            result.ParsedModel = ExportParsedModel(filePath);
        }

        metadata.Warnings.AddRange(_warnings);
        metadata.Errors.AddRange(_errors);

        return result;
    }

    private ExportMetadata CreateMetadata(FileInfo fileInfo)
    {
        using var stream = fileInfo.OpenRead();
        var hash = SHA256.HashData(stream);

        return new ExportMetadata
        {
            SourceFileName = fileInfo.Name,
            SourceFileSize = fileInfo.Length,
            SourceFileHash = $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}"
        };
    }

    private ParsedModel ExportParsedModel(string filePath)
    {
        SchLibrary schLib;

        try
        {
            using var stream = File.OpenRead(filePath);
            schLib = new V2SchLibReader().Read(stream);
        }
        catch (Exception ex)
        {
            _errors.Add($"Failed to parse schematic library: {ex.Message}");
            return new ParsedModel { FileType = "SchLib" };
        }

        return new ParsedModel
        {
            FileType = "SchLib",
            SchLib = ConvertSchLib(schLib)
        };
    }

    private ParsedSchLib ConvertSchLib(SchLibrary schLib)
    {
        return new ParsedSchLib
        {
            Components = schLib.Components.Select(ConvertComponent).ToList()
        };
    }

    private ParsedSchComponent ConvertComponent(ISchComponent component)
    {
        var primitives = new List<ParsedPrimitive>();

        foreach (var pin in component.Pins) primitives.Add(ConvertPrimitive("Pin", pin));
        foreach (var line in component.Lines) primitives.Add(ConvertPrimitive("Line", line));
        foreach (var rect in component.Rectangles) primitives.Add(ConvertPrimitive("Rectangle", rect));
        foreach (var label in component.Labels) primitives.Add(ConvertPrimitive("Label", label));
        foreach (var wire in component.Wires) primitives.Add(ConvertPrimitive("Wire", wire));
        foreach (var polyline in component.Polylines) primitives.Add(ConvertPrimitive("Polyline", polyline));
        foreach (var polygon in component.Polygons) primitives.Add(ConvertPrimitive("Polygon", polygon));
        foreach (var arc in component.Arcs) primitives.Add(ConvertPrimitive("Arc", arc));
        foreach (var bezier in component.Beziers) primitives.Add(ConvertPrimitive("Bezier", bezier));
        foreach (var ellipse in component.Ellipses) primitives.Add(ConvertPrimitive("Ellipse", ellipse));
        foreach (var roundedRect in component.RoundedRectangles) primitives.Add(ConvertPrimitive("RoundedRectangle", roundedRect));
        foreach (var pie in component.Pies) primitives.Add(ConvertPrimitive("Pie", pie));
        foreach (var netLabel in component.NetLabels) primitives.Add(ConvertPrimitive("NetLabel", netLabel));
        foreach (var junction in component.Junctions) primitives.Add(ConvertPrimitive("Junction", junction));
        foreach (var param in component.Parameters) primitives.Add(ConvertPrimitive("Parameter", param));
        foreach (var textFrame in component.TextFrames) primitives.Add(ConvertPrimitive("TextFrame", textFrame));
        foreach (var image in component.Images) primitives.Add(ConvertPrimitive("Image", image));
        foreach (var symbol in component.Symbols) primitives.Add(ConvertPrimitive("Symbol", symbol));
        foreach (var ellipticalArc in component.EllipticalArcs) primitives.Add(ConvertPrimitive("EllipticalArc", ellipticalArc));
        foreach (var powerObj in component.PowerObjects) primitives.Add(ConvertPrimitive("PowerObject", powerObj));

        return new ParsedSchComponent
        {
            Name = component.Name,
            Description = component.Description,
            PartCount = component.PartCount,
            Primitives = primitives
        };
    }

    private ParsedPrimitive ConvertPrimitive(string objectType, object primitive)
    {
        return new ParsedPrimitive
        {
            ObjectType = objectType,
            Properties = ExtractProperties(primitive)
        };
    }

    private Dictionary<string, object?> ExtractProperties(object obj)
    {
        var result = new Dictionary<string, object?>();
        var type = obj.GetType();

        var skipProps = new HashSet<string> { "Bounds" };

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead) continue;
            if (skipProps.Contains(prop.Name)) continue;

            try
            {
                var value = prop.GetValue(obj);
                if (value != null)
                {
                    result[prop.Name] = ConvertValue(value);
                }
            }
            catch
            {
                // Skip properties that throw
            }
        }

        return result;
    }

    private object? ConvertValue(object? value)
    {
        if (value == null) return null;

        return value switch
        {
            V2Coord coord => CoordValue.FromCoord(coord.ToRaw()),
            V2CoordPoint point => new CoordPointValue
            {
                X = CoordValue.FromCoord(point.X.ToRaw()),
                Y = CoordValue.FromCoord(point.Y.ToRaw())
            },
            Enum e => new { name = e.ToString(), value = Convert.ToInt32(e) },
            string s => s,
            bool b => b,
            int i => i,
            long l => l,
            double d => d,
            float f => f,
            byte b => b,
            IEnumerable<V2CoordPoint> points => points.Select(p => new CoordPointValue
            {
                X = CoordValue.FromCoord(p.X.ToRaw()),
                Y = CoordValue.FromCoord(p.Y.ToRaw())
            }).ToList(),
            IEnumerable<byte> bytes => Convert.ToBase64String(bytes.ToArray()),
            _ when value.GetType().IsPrimitive => value,
            _ => value.ToString()
        };
    }
}
