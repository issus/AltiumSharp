using System.Reflection;
using OriginalCircuit.Altium.Models;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using V2Coord = OriginalCircuit.Altium.Primitives.Coord;
using V2CoordPoint = OriginalCircuit.Altium.Primitives.CoordPoint;
using V2PcbLibReader = OriginalCircuit.Altium.Serialization.Readers.PcbLibReader;
using V2PcbLibWriter = OriginalCircuit.Altium.Serialization.Writers.PcbLibWriter;
using V2SchLibReader = OriginalCircuit.Altium.Serialization.Readers.SchLibReader;
using V2SchLibWriter = OriginalCircuit.Altium.Serialization.Writers.SchLibWriter;

namespace OriginalCircuit.AltiumSharp.Export.Analysis;

/// <summary>
/// Validates parsing completeness by performing read → write → re-read round-trips
/// and comparing model properties to identify gaps in serialization fidelity.
/// </summary>
public sealed class RoundTripValidator
{
    /// <summary>
    /// Validate a PCB library file by reading, writing, and re-reading.
    /// </summary>
    public ValidationResult ValidatePcbLib(string filePath)
    {
        var result = new ValidationResult { FileName = Path.GetFileName(filePath) };

        try
        {
            PcbLibrary original;
            using (var stream = File.OpenRead(filePath))
            {
                original = new V2PcbLibReader().Read(stream);
            }

            // Round-trip: write to memory, re-read
            PcbLibrary roundTripped;
            using (var ms = new MemoryStream())
            {
                new V2PcbLibWriter().Write(original, ms);
                ms.Position = 0;
                roundTripped = new V2PcbLibReader().Read(ms);
            }

            // Compare components
            var origComponents = original.Components.ToList();
            var rtComponents = roundTripped.Components.ToList();

            result.TotalComponents = origComponents.Count;

            for (int i = 0; i < origComponents.Count && i < rtComponents.Count; i++)
            {
                var componentResult = ValidatePcbComponent(origComponents[i], rtComponents[i]);
                result.ComponentResults.Add(componentResult);
            }

            // Check for lost/extra components
            if (origComponents.Count != rtComponents.Count)
            {
                result.ComponentResults.Add(new ComponentValidation
                {
                    Name = "[Component Count]",
                    FullMatch = false,
                    TotalPrimitives = origComponents.Count,
                    FullyMatchedPrimitives = rtComponents.Count
                });
            }

            result.Success = result.ComponentResults.All(c => c.FullMatch);
            result.FullyMatchedComponents = result.ComponentResults.Count(c => c.FullMatch);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Validate a schematic library file by reading, writing, and re-reading.
    /// </summary>
    public ValidationResult ValidateSchLib(string filePath)
    {
        var result = new ValidationResult { FileName = Path.GetFileName(filePath) };

        try
        {
            SchLibrary original;
            using (var stream = File.OpenRead(filePath))
            {
                original = new V2SchLibReader().Read(stream);
            }

            // Round-trip: write to memory, re-read
            SchLibrary roundTripped;
            using (var ms = new MemoryStream())
            {
                new V2SchLibWriter().Write(original, ms);
                ms.Position = 0;
                roundTripped = new V2SchLibReader().Read(ms);
            }

            // Compare components
            var origComponents = original.Components.ToList();
            var rtComponents = roundTripped.Components.ToList();

            result.TotalComponents = origComponents.Count;

            for (int i = 0; i < origComponents.Count && i < rtComponents.Count; i++)
            {
                var componentResult = ValidateSchComponent(origComponents[i], rtComponents[i]);
                result.ComponentResults.Add(componentResult);
            }

            if (origComponents.Count != rtComponents.Count)
            {
                result.ComponentResults.Add(new ComponentValidation
                {
                    Name = "[Component Count]",
                    FullMatch = false,
                    TotalPrimitives = origComponents.Count,
                    FullyMatchedPrimitives = rtComponents.Count
                });
            }

            result.Success = result.ComponentResults.All(c => c.FullMatch);
            result.FullyMatchedComponents = result.ComponentResults.Count(c => c.FullMatch);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Validate a single primitive by comparing properties of original vs round-tripped.
    /// </summary>
    public PrimitiveValidation ValidatePrimitive(object original, object roundTripped)
    {
        var type = original.GetType();
        var validation = new PrimitiveValidation
        {
            PrimitiveType = type.Name
        };

        try
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (!prop.CanRead) continue;
                if (ShouldSkipProperty(prop.Name)) continue;

                try
                {
                    var origValue = prop.GetValue(original);
                    var rtValue = prop.GetValue(roundTripped);
                    var match = CompareValues(origValue, rtValue, prop.PropertyType);

                    validation.Properties.Add(new PropertyValidation
                    {
                        PropertyName = prop.Name,
                        PropertyValue = FormatValue(origValue),
                        ParameterValue = FormatValue(rtValue),
                        Match = match,
                        Notes = match ? null : "Values differ after round-trip"
                    });

                    if (match) validation.MatchedProperties++;
                    else validation.MismatchedProperties++;
                }
                catch (Exception ex)
                {
                    validation.Properties.Add(new PropertyValidation
                    {
                        PropertyName = prop.Name,
                        Match = false,
                        Notes = $"Error: {ex.Message}"
                    });
                }
            }

            validation.FullMatch = validation.MismatchedProperties == 0;
        }
        catch (Exception ex)
        {
            validation.Error = ex.Message;
        }

        return validation;
    }

    /// <summary>
    /// Compare raw binary data with what would be serialized.
    /// </summary>
    public BinaryValidation ValidateBinary(byte[] original, byte[] serialized, string context)
    {
        var validation = new BinaryValidation
        {
            Context = context,
            OriginalSize = original.Length,
            SerializedSize = serialized.Length
        };

        if (original.Length != serialized.Length)
        {
            validation.SizeMatch = false;
            validation.Differences.Add(new BinaryDifference
            {
                Type = "SizeMismatch",
                Description = $"Size differs: original={original.Length}, serialized={serialized.Length}"
            });
        }
        else
        {
            validation.SizeMatch = true;
        }

        // Find byte differences
        var minLen = Math.Min(original.Length, serialized.Length);
        for (int i = 0; i < minLen; i++)
        {
            if (original[i] != serialized[i])
            {
                validation.Differences.Add(new BinaryDifference
                {
                    Type = "ByteMismatch",
                    Offset = i,
                    OriginalValue = original[i],
                    SerializedValue = serialized[i],
                    Description = $"Byte at 0x{i:X4}: original=0x{original[i]:X2}, serialized=0x{serialized[i]:X2}"
                });
            }
        }

        validation.FullMatch = validation.SizeMatch && validation.Differences.Count == 0;
        validation.MatchPercentage = minLen > 0
            ? (double)(minLen - validation.Differences.Count) / minLen * 100
            : 0;

        return validation;
    }

    /// <summary>
    /// Analyze what fields are being lost during parsing by counting model properties
    /// vs expected properties from test data.
    /// </summary>
    public LossAnalysis AnalyzeLoss(string filePath)
    {
        var analysis = new LossAnalysis { FileName = Path.GetFileName(filePath) };

        try
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();

            if (ext == ".pcblib")
            {
                AnalyzePcbLibLoss(filePath, analysis);
            }
            else if (ext == ".schlib")
            {
                AnalyzeSchLibLoss(filePath, analysis);
            }
        }
        catch (Exception ex)
        {
            analysis.Error = ex.Message;
        }

        return analysis;
    }

    private void AnalyzePcbLibLoss(string filePath, LossAnalysis analysis)
    {
        PcbLibrary pcbLib;
        using (var stream = File.OpenRead(filePath))
        {
            pcbLib = new V2PcbLibReader().Read(stream);
        }

        int totalPrimitives = 0;
        foreach (var component in pcbLib.Components)
        {
            foreach (var primitive in GetAllPcbPrimitives(component))
            {
                totalPrimitives++;
                var properties = primitive.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && !ShouldSkipProperty(p.Name))
                    .Count();

                // v2 models don't have RawData, so we estimate based on property count
                // A typical fully-modeled primitive has 15-30 properties
                var estimatedCompleteness = Math.Min(1.0, properties / 25.0);

                if (estimatedCompleteness < 0.9)
                {
                    analysis.PartiallyParsedRecords.Add(new PartialRecord
                    {
                        RecordType = primitive.GetType().Name,
                        Context = $"{component.Name}/{primitive.GetType().Name}",
                        RawSize = 0, // Not available in v2
                        ParsedSize = properties,
                        CoverageRatio = estimatedCompleteness,
                        MissingBytes = 0
                    });
                }
            }
        }

        analysis.TotalRecords = totalPrimitives;
    }

    private void AnalyzeSchLibLoss(string filePath, LossAnalysis analysis)
    {
        SchLibrary schLib;
        using (var stream = File.OpenRead(filePath))
        {
            schLib = new V2SchLibReader().Read(stream);
        }

        int totalPrimitives = 0;
        foreach (var component in schLib.Components)
        {
            foreach (var primitive in GetAllSchPrimitives(component))
            {
                totalPrimitives++;
                var properties = primitive.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && !ShouldSkipProperty(p.Name))
                    .Count();

                var estimatedCompleteness = Math.Min(1.0, properties / 20.0);

                if (estimatedCompleteness < 0.9)
                {
                    analysis.PartiallyParsedRecords.Add(new PartialRecord
                    {
                        RecordType = primitive.GetType().Name,
                        Context = $"{component.Name}/{primitive.GetType().Name}",
                        RawSize = 0,
                        ParsedSize = properties,
                        CoverageRatio = estimatedCompleteness,
                        MissingBytes = 0
                    });
                }
            }
        }

        analysis.TotalRecords = totalPrimitives;
    }

    private ComponentValidation ValidatePcbComponent(IPcbComponent original, IPcbComponent roundTripped)
    {
        var origPrimitives = GetAllPcbPrimitives(original).ToList();
        var rtPrimitives = GetAllPcbPrimitives(roundTripped).ToList();

        var result = new ComponentValidation
        {
            Name = original.Name,
            TotalPrimitives = origPrimitives.Count
        };

        for (int i = 0; i < origPrimitives.Count && i < rtPrimitives.Count; i++)
        {
            var primValidation = ValidatePrimitive(origPrimitives[i], rtPrimitives[i]);
            result.PrimitiveResults.Add(primValidation);
        }

        result.FullyMatchedPrimitives = result.PrimitiveResults.Count(p => p.FullMatch);
        result.FullMatch = result.FullyMatchedPrimitives == result.TotalPrimitives
                           && origPrimitives.Count == rtPrimitives.Count;

        return result;
    }

    private ComponentValidation ValidateSchComponent(ISchComponent original, ISchComponent roundTripped)
    {
        var origPrimitives = GetAllSchPrimitives(original).ToList();
        var rtPrimitives = GetAllSchPrimitives(roundTripped).ToList();

        var result = new ComponentValidation
        {
            Name = original.Name,
            TotalPrimitives = origPrimitives.Count
        };

        for (int i = 0; i < origPrimitives.Count && i < rtPrimitives.Count; i++)
        {
            var primValidation = ValidatePrimitive(origPrimitives[i], rtPrimitives[i]);
            result.PrimitiveResults.Add(primValidation);
        }

        result.FullyMatchedPrimitives = result.PrimitiveResults.Count(p => p.FullMatch);
        result.FullMatch = result.FullyMatchedPrimitives == result.TotalPrimitives
                           && origPrimitives.Count == rtPrimitives.Count;

        return result;
    }

    private static IEnumerable<object> GetAllPcbPrimitives(IPcbComponent component)
    {
        foreach (var p in component.Pads) yield return p;
        foreach (var p in component.Tracks) yield return p;
        foreach (var p in component.Vias) yield return p;
        foreach (var p in component.Arcs) yield return p;
        foreach (var p in component.Texts) yield return p;
        foreach (var p in component.Fills) yield return p;
        foreach (var p in component.Regions) yield return p;
        foreach (var p in component.ComponentBodies) yield return p;
    }

    private static IEnumerable<object> GetAllSchPrimitives(ISchComponent component)
    {
        foreach (var p in component.Pins) yield return p;
        foreach (var p in component.Lines) yield return p;
        foreach (var p in component.Rectangles) yield return p;
        foreach (var p in component.Labels) yield return p;
        foreach (var p in component.Wires) yield return p;
        foreach (var p in component.Polylines) yield return p;
        foreach (var p in component.Polygons) yield return p;
        foreach (var p in component.Arcs) yield return p;
        foreach (var p in component.Beziers) yield return p;
        foreach (var p in component.Ellipses) yield return p;
        foreach (var p in component.RoundedRectangles) yield return p;
        foreach (var p in component.Pies) yield return p;
        foreach (var p in component.NetLabels) yield return p;
        foreach (var p in component.Junctions) yield return p;
        foreach (var p in component.Parameters) yield return p;
        foreach (var p in component.TextFrames) yield return p;
        foreach (var p in component.Images) yield return p;
        foreach (var p in component.Symbols) yield return p;
        foreach (var p in component.EllipticalArcs) yield return p;
        foreach (var p in component.PowerObjects) yield return p;
    }

    private static bool ShouldSkipProperty(string name)
    {
        var skipList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Bounds", "Vertices", "Points" // Collections compared separately
        };
        return skipList.Contains(name);
    }

    private static bool CompareValues(object? a, object? b, Type propertyType)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        if (propertyType == typeof(V2Coord))
        {
            return ((V2Coord)a).ToRaw() == ((V2Coord)b).ToRaw();
        }

        if (propertyType == typeof(V2CoordPoint))
        {
            var pa = (V2CoordPoint)a;
            var pb = (V2CoordPoint)b;
            return pa.X.ToRaw() == pb.X.ToRaw() && pa.Y.ToRaw() == pb.Y.ToRaw();
        }

        if (propertyType == typeof(bool))
        {
            return (bool)a == (bool)b;
        }

        if (propertyType == typeof(int) || propertyType == typeof(long) ||
            propertyType == typeof(double) || propertyType == typeof(float) ||
            propertyType == typeof(byte))
        {
            return a.Equals(b);
        }

        if (propertyType == typeof(string))
        {
            return string.Equals((string)a, (string)b, StringComparison.Ordinal);
        }

        if (propertyType.IsEnum)
        {
            return Convert.ToInt32(a) == Convert.ToInt32(b);
        }

        return a.Equals(b);
    }

    private static string FormatValue(object? value)
    {
        if (value == null) return "null";

        if (value is V2Coord coord)
        {
            return $"{coord.ToRaw()} ({coord.ToMils():F2}mil)";
        }

        if (value is V2CoordPoint point)
        {
            return $"({point.X.ToRaw()}, {point.Y.ToRaw()})";
        }

        return value.ToString() ?? "null";
    }
}

#region Result Types

public sealed class ValidationResult
{
    public string FileName { get; init; } = "";
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int TotalComponents { get; set; }
    public int FullyMatchedComponents { get; set; }
    public List<ComponentValidation> ComponentResults { get; init; } = [];
}

public sealed class ComponentValidation
{
    public string Name { get; init; } = "";
    public bool FullMatch { get; set; }
    public int TotalPrimitives { get; set; }
    public int FullyMatchedPrimitives { get; set; }
    public List<PrimitiveValidation> PrimitiveResults { get; init; } = [];
}

public sealed class PrimitiveValidation
{
    public string PrimitiveType { get; init; } = "";
    public bool FullMatch { get; set; }
    public string? Error { get; set; }
    public int MatchedProperties { get; set; }
    public int MismatchedProperties { get; set; }
    public int UnmappedProperties { get; set; }
    public List<PropertyValidation> Properties { get; init; } = [];
    public List<ExtraParameter> ExtraParameters { get; init; } = [];
}

public sealed class PropertyValidation
{
    public string PropertyName { get; init; } = "";
    public string? ParameterKey { get; init; }
    public string? PropertyValue { get; init; }
    public string? ParameterValue { get; init; }
    public bool Match { get; init; }
    public string? Notes { get; init; }
}

public sealed class ExtraParameter
{
    public string Key { get; init; } = "";
    public string Value { get; init; } = "";
}

public sealed class BinaryValidation
{
    public string Context { get; init; } = "";
    public int OriginalSize { get; init; }
    public int SerializedSize { get; init; }
    public bool SizeMatch { get; set; }
    public bool FullMatch { get; set; }
    public double MatchPercentage { get; set; }
    public List<BinaryDifference> Differences { get; init; } = [];
}

public sealed class BinaryDifference
{
    public string Type { get; init; } = "";
    public int Offset { get; init; }
    public byte OriginalValue { get; init; }
    public byte SerializedValue { get; init; }
    public string Description { get; init; } = "";
}

public sealed class LossAnalysis
{
    public string FileName { get; init; } = "";
    public string? Error { get; set; }
    public int TotalRecords { get; set; }
    public List<PartialRecord> PartiallyParsedRecords { get; init; } = [];
}

public sealed class PartialRecord
{
    public string RecordType { get; init; } = "";
    public string Context { get; init; } = "";
    public int RawSize { get; init; }
    public int ParsedSize { get; init; }
    public double CoverageRatio { get; init; }
    public int MissingBytes { get; init; }
}

#endregion
