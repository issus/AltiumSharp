using System.Text;

namespace OriginalCircuit.Altium.Tests.PropertyCoverage;

/// <summary>
/// Generates the COVERAGE.md report summarizing property coverage across all types.
/// Run this test to regenerate the report.
/// </summary>
public class CoverageReportTests : CoverageTestBase
{
    [Fact]
    public void GenerateCoverageReport()
    {
        var pcbDir = GetPcbTestDataPath();
        var schDir = GetSchTestDataPath();

        var sb = new StringBuilder();
        sb.AppendLine("# Property Coverage Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine();

        // PCB types
        sb.AppendLine("## PCB Primitive Types");
        sb.AppendLine();
        sb.AppendLine("| Type | Files | Primitives | JSON Props | Mapped | Missing | Coverage |");
        sb.AppendLine("|------|-------|------------|-----------|--------|---------|----------|");

        var pcbTypes = new (string objectType, Dictionary<string, string> mapping, string prefix)[]
        {
            ("Pad", PcbPropertyMappings.Pad, "PAD_"),
            ("Via", PcbPropertyMappings.Via, "VIA_"),
            ("Track", PcbPropertyMappings.Track, "TRACK_"),
            ("Arc", PcbPropertyMappings.Arc, "ARC_"),
            ("Text", PcbPropertyMappings.Text, "TEXT_"),
            ("Fill", PcbPropertyMappings.Fill, "FILL"),
            ("Region", PcbPropertyMappings.Region, "*REGION*"),
            ("ComponentBody", PcbPropertyMappings.ComponentBody, "*BODY*3D*"),
        };

        var allResults = new List<CoverageResult>();

        foreach (var (objectType, mapping, prefix) in pcbTypes)
        {
            var result = CheckCoverage(pcbDir, $"{prefix}*.json", objectType, mapping, isPcb: true);
            allResults.Add(result);
            sb.AppendLine($"| {result.TypeName} | {result.FileCount} | {result.PrimitiveCount} | " +
                $"{result.TotalJsonProperties} | {result.ModeledProperties} | {result.MissingProperties} | " +
                $"{result.CoveragePercent}% |");
        }

        sb.AppendLine();

        // Schematic types
        sb.AppendLine("## Schematic Primitive Types");
        sb.AppendLine();
        sb.AppendLine("| Type | Files | Primitives | JSON Props | Mapped | Missing | Coverage |");
        sb.AppendLine("|------|-------|------------|-----------|--------|---------|----------|");

        var schPrimitiveTypes = new (string objectType, Dictionary<string, string> mapping)[]
        {
            ("Pin", SchPropertyMappings.Pin),
            ("Wire", SchPropertyMappings.Wire),
            ("Line", SchPropertyMappings.Line),
            ("Rectangle", SchPropertyMappings.Rectangle),
            ("Label", SchPropertyMappings.Label),
            ("Parameter", SchPropertyMappings.Parameter),
            ("NetLabel", SchPropertyMappings.NetLabel),
            ("Arc", SchPropertyMappings.Arc),
            ("Polygon", SchPropertyMappings.Polygon),
            ("Polyline", SchPropertyMappings.Polyline),
            ("Bezier", SchPropertyMappings.Bezier),
            ("Ellipse", SchPropertyMappings.Ellipse),
            ("Pie", SchPropertyMappings.Pie),
            ("RoundRectangle", SchPropertyMappings.RoundedRectangle),
            ("EllipticalArc", SchPropertyMappings.EllipticalArc),
            ("TextFrame", SchPropertyMappings.TextFrame),
            ("Image", SchPropertyMappings.Image),
            ("Symbol", SchPropertyMappings.Symbol),
            ("Junction", SchPropertyMappings.Junction),
            ("PowerObject", SchPropertyMappings.PowerObject),
        };

        foreach (var (objectType, mapping) in schPrimitiveTypes)
        {
            var result = CheckCoverage(schDir, "*.json", objectType, mapping, isPcb: false);
            allResults.Add(result);
            sb.AppendLine($"| {result.TypeName} | {result.FileCount} | {result.PrimitiveCount} | " +
                $"{result.TotalJsonProperties} | {result.ModeledProperties} | {result.MissingProperties} | " +
                $"{result.CoveragePercent}% |");
        }

        // Component coverage (special — not a primitive)
        var compResult = CheckComponentCoverage(schDir);
        allResults.Add(compResult);
        sb.AppendLine($"| **Component** | {compResult.FileCount} | {compResult.PrimitiveCount} | " +
            $"{compResult.TotalJsonProperties} | {compResult.ModeledProperties} | {compResult.MissingProperties} | " +
            $"{compResult.CoveragePercent}% |");

        sb.AppendLine();

        // Summary
        var totalJson = allResults.Sum(r => r.TotalJsonProperties);
        var totalMapped = allResults.Sum(r => r.ModeledProperties);
        var overallCoverage = totalJson == 0 ? 100 : Math.Round(100.0 * totalMapped / totalJson, 1);
        sb.AppendLine($"**Overall: {totalMapped}/{totalJson} properties mapped ({overallCoverage}%)**");
        sb.AppendLine();

        // Detailed missing properties per type
        sb.AppendLine("## Missing Properties by Type");
        sb.AppendLine();

        foreach (var result in allResults.Where(r => r.UnmappedKeys.Count > 0))
        {
            sb.AppendLine($"### {result.TypeName} ({result.UnmappedKeys.Count} missing)");
            sb.AppendLine();
            sb.AppendLine("```");
            foreach (var key in result.UnmappedKeys.Order())
                sb.AppendLine(key);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // Write the report
        var reportPath = Path.Combine(GetTestDataPath(), "..", "tests", "COVERAGE.md");
        reportPath = Path.GetFullPath(reportPath);
        File.WriteAllText(reportPath, sb.ToString());

        // Also output to test console
        Assert.True(true, sb.ToString());
    }

    private static CoverageResult CheckCoverage(
        string directory, string filePattern, string objectType,
        Dictionary<string, string> mapping, bool isPcb)
    {
        var (allKeys, fileCount, primitiveCount) = CollectPropertyKeys(directory, filePattern, objectType, isPcb);

        var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unmapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in allKeys)
        {
            if (mapping.ContainsKey(key))
                mapped.Add(key);
            else
                unmapped.Add(key);
        }

        return new CoverageResult
        {
            TypeName = objectType,
            TotalJsonProperties = allKeys.Count,
            ModeledProperties = mapped.Count,
            AllJsonKeys = allKeys,
            MappedKeys = mapped,
            UnmappedKeys = unmapped,
            FileCount = fileCount,
            PrimitiveCount = primitiveCount
        };
    }

    private static CoverageResult CheckComponentCoverage(string directory)
    {
        if (!Directory.Exists(directory))
            return new CoverageResult { TypeName = "Component" };

        var allKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fileCount = 0;
        var primitiveCount = 0;

        foreach (var jsonFile in Directory.GetFiles(directory, "*.json"))
        {
            using var doc = LoadJson(jsonFile);
            if (doc == null) continue;

            var components = GetSchComponents(doc);

            if (components.Count > 0) fileCount++;
            primitiveCount += components.Count;

            foreach (var comp in components)
            {
                var keys = GetDesignTimePropertyKeys(comp);
                allKeys.UnionWith(keys);
            }
        }

        var mapping = SchPropertyMappings.Component;
        var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unmapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in allKeys)
        {
            if (mapping.ContainsKey(key))
                mapped.Add(key);
            else
                unmapped.Add(key);
        }

        return new CoverageResult
        {
            TypeName = "Component",
            TotalJsonProperties = allKeys.Count,
            ModeledProperties = mapped.Count,
            AllJsonKeys = allKeys,
            MappedKeys = mapped,
            UnmappedKeys = unmapped,
            FileCount = fileCount,
            PrimitiveCount = primitiveCount
        };
    }
}
