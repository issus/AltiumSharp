using System.Reflection;
using System.Security.Cryptography;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.AltiumSharp.Export.Models;
using V2Coord = OriginalCircuit.Altium.Primitives.Coord;
using V2CoordPoint = OriginalCircuit.Altium.Primitives.CoordPoint;
using V2SchDocReader = OriginalCircuit.Altium.Serialization.Readers.SchDocReader;

namespace OriginalCircuit.AltiumSharp.Export.Exporters;

/// <summary>
/// Exports schematic document files (.SchDoc) to JSON format using the v2 reader.
/// </summary>
public sealed class SchDocExporter
{
    private readonly ExportOptions _options;
    private readonly List<string> _warnings = [];
    private readonly List<string> _errors = [];

    public SchDocExporter(ExportOptions? options = null)
    {
        _options = options ?? new ExportOptions();
    }

    /// <summary>
    /// Export a schematic document file.
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
        SchDocument schDoc;

        try
        {
            using var stream = File.OpenRead(filePath);
            schDoc = new V2SchDocReader().Read(stream);
        }
        catch (Exception ex)
        {
            _errors.Add($"Failed to parse schematic document: {ex.Message}");
            return new ParsedModel { FileType = "SchDoc" };
        }

        return new ParsedModel
        {
            FileType = "SchDoc",
            SchDoc = ConvertSchDoc(schDoc)
        };
    }

    private ParsedSchDoc ConvertSchDoc(SchDocument schDoc)
    {
        var primitives = new List<ParsedPrimitive>();

        // Export components and their children
        foreach (var component in schDoc.Components)
        {
            var compPrimitive = new ParsedPrimitive
            {
                ObjectType = "Component",
                Properties = new Dictionary<string, object?>
                {
                    ["Name"] = component.Name,
                    ["Description"] = component.Description,
                    ["PartCount"] = component.PartCount,
                    ["DesignatorPrefix"] = component.DesignatorPrefix
                },
                Children = GetComponentChildren(component)
            };
            primitives.Add(compPrimitive);
        }

        // Export document-level primitives
        foreach (var wire in schDoc.Wires) primitives.Add(ConvertPrimitive("Wire", wire));
        foreach (var netLabel in schDoc.NetLabels) primitives.Add(ConvertPrimitive("NetLabel", netLabel));
        foreach (var junction in schDoc.Junctions) primitives.Add(ConvertPrimitive("Junction", junction));
        foreach (var powerObj in schDoc.PowerObjects) primitives.Add(ConvertPrimitive("PowerObject", powerObj));
        foreach (var label in schDoc.Labels) primitives.Add(ConvertPrimitive("Label", label));
        foreach (var param in schDoc.Parameters) primitives.Add(ConvertPrimitive("Parameter", param));
        foreach (var line in schDoc.Lines) primitives.Add(ConvertPrimitive("Line", line));
        foreach (var rect in schDoc.Rectangles) primitives.Add(ConvertPrimitive("Rectangle", rect));
        foreach (var polygon in schDoc.Polygons) primitives.Add(ConvertPrimitive("Polygon", polygon));
        foreach (var polyline in schDoc.Polylines) primitives.Add(ConvertPrimitive("Polyline", polyline));
        foreach (var arc in schDoc.Arcs) primitives.Add(ConvertPrimitive("Arc", arc));
        foreach (var bezier in schDoc.Beziers) primitives.Add(ConvertPrimitive("Bezier", bezier));
        foreach (var ellipse in schDoc.Ellipses) primitives.Add(ConvertPrimitive("Ellipse", ellipse));
        foreach (var roundedRect in schDoc.RoundedRectangles) primitives.Add(ConvertPrimitive("RoundedRectangle", roundedRect));
        foreach (var pie in schDoc.Pies) primitives.Add(ConvertPrimitive("Pie", pie));
        foreach (var textFrame in schDoc.TextFrames) primitives.Add(ConvertPrimitive("TextFrame", textFrame));
        foreach (var image in schDoc.Images) primitives.Add(ConvertPrimitive("Image", image));
        foreach (var symbol in schDoc.Symbols) primitives.Add(ConvertPrimitive("Symbol", symbol));
        foreach (var ellipticalArc in schDoc.EllipticalArcs) primitives.Add(ConvertPrimitive("EllipticalArc", ellipticalArc));

        return new ParsedSchDoc
        {
            Primitives = primitives
        };
    }

    private List<ParsedPrimitive> GetComponentChildren(ISchComponent component)
    {
        var children = new List<ParsedPrimitive>();

        foreach (var pin in component.Pins) children.Add(ConvertPrimitive("Pin", pin));
        foreach (var line in component.Lines) children.Add(ConvertPrimitive("Line", line));
        foreach (var rect in component.Rectangles) children.Add(ConvertPrimitive("Rectangle", rect));
        foreach (var label in component.Labels) children.Add(ConvertPrimitive("Label", label));
        foreach (var wire in component.Wires) children.Add(ConvertPrimitive("Wire", wire));
        foreach (var polyline in component.Polylines) children.Add(ConvertPrimitive("Polyline", polyline));
        foreach (var polygon in component.Polygons) children.Add(ConvertPrimitive("Polygon", polygon));
        foreach (var arc in component.Arcs) children.Add(ConvertPrimitive("Arc", arc));
        foreach (var bezier in component.Beziers) children.Add(ConvertPrimitive("Bezier", bezier));
        foreach (var ellipse in component.Ellipses) children.Add(ConvertPrimitive("Ellipse", ellipse));
        foreach (var roundedRect in component.RoundedRectangles) children.Add(ConvertPrimitive("RoundedRectangle", roundedRect));
        foreach (var pie in component.Pies) children.Add(ConvertPrimitive("Pie", pie));
        foreach (var netLabel in component.NetLabels) children.Add(ConvertPrimitive("NetLabel", netLabel));
        foreach (var junction in component.Junctions) children.Add(ConvertPrimitive("Junction", junction));
        foreach (var param in component.Parameters) children.Add(ConvertPrimitive("Parameter", param));
        foreach (var textFrame in component.TextFrames) children.Add(ConvertPrimitive("TextFrame", textFrame));
        foreach (var image in component.Images) children.Add(ConvertPrimitive("Image", image));
        foreach (var symbol in component.Symbols) children.Add(ConvertPrimitive("Symbol", symbol));
        foreach (var ellipticalArc in component.EllipticalArcs) children.Add(ConvertPrimitive("EllipticalArc", ellipticalArc));
        foreach (var powerObj in component.PowerObjects) children.Add(ConvertPrimitive("PowerObject", powerObj));

        return children;
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
