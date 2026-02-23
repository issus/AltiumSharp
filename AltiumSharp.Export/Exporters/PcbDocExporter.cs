using System.Reflection;
using System.Security.Cryptography;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Serialization.Readers;
using OriginalCircuit.AltiumSharp.Export.Models;
using V2Coord = OriginalCircuit.Altium.Primitives.Coord;
using V2CoordPoint = OriginalCircuit.Altium.Primitives.CoordPoint;

namespace OriginalCircuit.AltiumSharp.Export.Exporters;

/// <summary>
/// Exports PCB document files (.PcbDoc) to JSON format using the v2 reader.
/// </summary>
public sealed class PcbDocExporter
{
    private readonly ExportOptions _options;
    private readonly List<string> _warnings = [];
    private readonly List<string> _errors = [];

    public IReadOnlyList<string> Warnings => _warnings;
    public IReadOnlyList<string> Errors => _errors;

    public PcbDocExporter(ExportOptions? options = null)
    {
        _options = options ?? new ExportOptions();
    }

    public ExportResult Export(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("File not found", filePath);

        var metadata = CreateMetadata(fileInfo);
        var result = new ExportResult { Metadata = metadata };

        if (_options.IncludeRawMcdfStructure)
        {
            using var mcdfExporter = new McdfExporter(_options);
            var mcdfResult = mcdfExporter.Export(filePath);
            result.RawMcdf = mcdfResult.RawMcdf;
            _warnings.AddRange(mcdfExporter.Warnings);
        }

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
        PcbDocument document;

        try
        {
            using var stream = File.OpenRead(filePath);
            document = new PcbDocReader().Read(stream);
        }
        catch (Exception ex)
        {
            _errors.Add($"Failed to parse PCB document: {ex.Message}");
            return new ParsedModel { FileType = "PcbDoc" };
        }

        return new ParsedModel
        {
            FileType = "PcbDoc",
            PcbDoc = ConvertDocument(document)
        };
    }

    private ParsedPcbDoc ConvertDocument(PcbDocument document)
    {
        return new ParsedPcbDoc
        {
            Components = document.Components.Select(ConvertComponent).ToList(),
            Primitives = new ParsedPcbDocPrimitives
            {
                PadCount = document.Pads.Count,
                ViaCount = document.Vias.Count,
                TrackCount = document.Tracks.Count,
                ArcCount = document.Arcs.Count,
                TextCount = document.Texts.Count,
                FillCount = document.Fills.Count,
                RegionCount = document.Regions.Count,
                ComponentBodyCount = document.ComponentBodies.Count,
                Pads = document.Pads.Select(p => ConvertPrimitive("Pad", p)).ToList(),
                Vias = document.Vias.Select(v => ConvertPrimitive("Via", v)).ToList(),
                Tracks = document.Tracks.Select(t => ConvertPrimitive("Track", t)).ToList(),
                Arcs = document.Arcs.Select(a => ConvertPrimitive("Arc", a)).ToList(),
                Texts = document.Texts.Select(t => ConvertPrimitive("Text", t)).ToList(),
                Fills = document.Fills.Select(f => ConvertPrimitive("Fill", f)).ToList(),
                Regions = document.Regions.Select(r => ConvertPrimitive("Region", r)).ToList(),
                ComponentBodies = document.ComponentBodies.Select(b => ConvertPrimitive("ComponentBody", b)).ToList()
            }
        };
    }

    private ParsedPcbComponent ConvertComponent(IPcbComponent component)
    {
        return new ParsedPcbComponent
        {
            Pattern = component.Name,
            Description = component.Description,
            Height = CoordValue.FromCoord(component.Height.ToRaw()),
            Primitives = []
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
            _ when value.GetType().IsPrimitive => value,
            _ => value.ToString()
        };
    }
}
