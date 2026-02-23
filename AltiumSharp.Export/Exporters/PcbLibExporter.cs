using System.Reflection;
using System.Security.Cryptography;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.AltiumSharp.Export.Models;
using V2Coord = OriginalCircuit.Altium.Primitives.Coord;
using V2CoordPoint = OriginalCircuit.Altium.Primitives.CoordPoint;
using V2PcbLibReader = OriginalCircuit.Altium.Serialization.Readers.PcbLibReader;

namespace OriginalCircuit.AltiumSharp.Export.Exporters;

/// <summary>
/// Exports PCB library files (.PcbLib) to JSON format using the v2 reader.
/// </summary>
public sealed class PcbLibExporter
{
    private readonly ExportOptions _options;
    private readonly List<string> _warnings = [];
    private readonly List<string> _errors = [];

    public PcbLibExporter(ExportOptions? options = null)
    {
        _options = options ?? new ExportOptions();
    }

    /// <summary>
    /// Export a PCB library file.
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
        PcbLibrary pcbLib;

        try
        {
            using var stream = File.OpenRead(filePath);
            pcbLib = (PcbLibrary)new V2PcbLibReader().Read(stream);
        }
        catch (Exception ex)
        {
            _errors.Add($"Failed to parse PCB library: {ex.Message}");
            return new ParsedModel { FileType = "PcbLib" };
        }

        return new ParsedModel
        {
            FileType = "PcbLib",
            PcbLib = ConvertPcbLib(pcbLib)
        };
    }

    private ParsedPcbLib ConvertPcbLib(PcbLibrary pcbLib)
    {
        return new ParsedPcbLib
        {
            Components = pcbLib.Components.Select(ConvertComponent).ToList()
        };
    }

    private ParsedPcbComponent ConvertComponent(IPcbComponent component)
    {
        var primitives = new List<ParsedPrimitive>();

        foreach (var pad in component.Pads) primitives.Add(ConvertPrimitive("Pad", pad));
        foreach (var track in component.Tracks) primitives.Add(ConvertPrimitive("Track", track));
        foreach (var via in component.Vias) primitives.Add(ConvertPrimitive("Via", via));
        foreach (var arc in component.Arcs) primitives.Add(ConvertPrimitive("Arc", arc));
        foreach (var text in component.Texts) primitives.Add(ConvertPrimitive("Text", text));
        foreach (var fill in component.Fills) primitives.Add(ConvertPrimitive("Fill", fill));
        foreach (var region in component.Regions) primitives.Add(ConvertPrimitive("Region", region));
        foreach (var body in component.ComponentBodies) primitives.Add(ConvertPrimitive("ComponentBody", body));

        return new ParsedPcbComponent
        {
            Pattern = component.Name,
            Description = component.Description,
            Height = CoordValue.FromCoord(component.Height.ToRaw()),
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
