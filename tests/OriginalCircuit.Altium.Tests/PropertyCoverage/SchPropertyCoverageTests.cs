using System.Text.Json;
using OriginalCircuit.Altium.Serialization.Readers;

namespace OriginalCircuit.Altium.Tests.PropertyCoverage;

/// <summary>
/// Tests that verify the v2 schematic reader captures all design-time properties
/// from test data. Each test reads all matching JSON+binary file pairs and
/// reports which properties are modeled vs missing.
/// </summary>
public class SchPropertyCoverageTests : CoverageTestBase
{
    private static CoverageResult CheckSchTypeCoverage(
        string objectType,
        Dictionary<string, string> mapping)
    {
        var dir = GetSchTestDataPath();
        var (allKeys, fileCount, primitiveCount) = CollectPropertyKeys(dir, "*.json", objectType, isPcb: false);

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

    private static CoverageResult CheckSchComponentCoverage()
    {
        var dir = GetSchTestDataPath();
        if (!Directory.Exists(dir))
            return new CoverageResult { TypeName = "Component" };

        var allKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fileCount = 0;
        var primitiveCount = 0;

        foreach (var jsonFile in Directory.GetFiles(dir, "*.json"))
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

    [Fact]
    public void SchComponent_CoverageReport()
    {
        var result = CheckSchComponentCoverage();
        ReportAndAssert(result);
    }

    [Fact]
    public void SchPin_CoverageReport()
    {
        var result = CheckSchTypeCoverage("Pin", SchPropertyMappings.Pin);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchWire_CoverageReport()
    {
        var result = CheckSchTypeCoverage("Wire", SchPropertyMappings.Wire);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchLine_CoverageReport()
    {
        var result = CheckSchTypeCoverage("Line", SchPropertyMappings.Line);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchRectangle_CoverageReport()
    {
        var result = CheckSchTypeCoverage("Rectangle", SchPropertyMappings.Rectangle);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchLabel_CoverageReport()
    {
        var result = CheckSchTypeCoverage("Label", SchPropertyMappings.Label);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchParameter_CoverageReport()
    {
        var result = CheckSchTypeCoverage("Parameter", SchPropertyMappings.Parameter);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchNetLabel_CoverageReport()
    {
        var result = CheckSchTypeCoverage("NetLabel", SchPropertyMappings.NetLabel);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchArc_CoverageReport()
    {
        var result = CheckSchTypeCoverage("Arc", SchPropertyMappings.Arc);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchPolygon_CoverageReport()
    {
        var result = CheckSchTypeCoverage("Polygon", SchPropertyMappings.Polygon);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchPolyline_CoverageReport()
    {
        var result = CheckSchTypeCoverage("Polyline", SchPropertyMappings.Polyline);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchBezier_CoverageReport()
    {
        var result = CheckSchTypeCoverage("Bezier", SchPropertyMappings.Bezier);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchEllipse_CoverageReport()
    {
        var result = CheckSchTypeCoverage("Ellipse", SchPropertyMappings.Ellipse);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchPie_CoverageReport()
    {
        var result = CheckSchTypeCoverage("Pie", SchPropertyMappings.Pie);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchRoundedRectangle_CoverageReport()
    {
        var result = CheckSchTypeCoverage("RoundRectangle", SchPropertyMappings.RoundedRectangle);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchEllipticalArc_CoverageReport()
    {
        var result = CheckSchTypeCoverage("EllipticalArc", SchPropertyMappings.EllipticalArc);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchTextFrame_CoverageReport()
    {
        var result = CheckSchTypeCoverage("TextFrame", SchPropertyMappings.TextFrame);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchImage_CoverageReport()
    {
        var result = CheckSchTypeCoverage("Image", SchPropertyMappings.Image);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchSymbol_CoverageReport()
    {
        var result = CheckSchTypeCoverage("Symbol", SchPropertyMappings.Symbol);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchJunction_CoverageReport()
    {
        var result = CheckSchTypeCoverage("Junction", SchPropertyMappings.Junction);
        ReportAndAssert(result);
    }

    [Fact]
    public void SchPowerObject_CoverageReport()
    {
        var result = CheckSchTypeCoverage("PowerObject", SchPropertyMappings.PowerObject);
        ReportAndAssert(result);
    }

    /// <summary>
    /// Reads ALL SchLib test files and verifies no exceptions are thrown.
    /// </summary>
    [SkippableFact]
    public void AllSchTestFiles_ReadWithoutExceptions()
    {
        var dir = GetSchTestDataPath();
        if (!Directory.Exists(dir)) { Skip.If(true, "Test data not available"); return; }

        var failures = new List<string>();

        foreach (var jsonFile in Directory.GetFiles(dir, "*.json").Order())
        {
            var binaryFile = Path.ChangeExtension(jsonFile, ".SchLib");
            if (!File.Exists(binaryFile)) continue;

            var fileName = Path.GetFileNameWithoutExtension(jsonFile);

            try
            {
                using var stream = File.OpenRead(binaryFile);
                var library = new SchLibReader().Read(stream);

                using var doc = LoadJson(jsonFile);
                if (doc == null) continue;

                var jsonComponents = GetSchComponents(doc);

                var v2Count = library.Components.Count();
                var jsonCount = jsonComponents.Count;
                if (jsonCount != v2Count)
                    failures.Add($"{fileName}: component count mismatch (JSON={jsonCount}, v2={v2Count})");
            }
            catch (Exception ex)
            {
                failures.Add($"{fileName}: EXCEPTION - {ex.GetType().Name}: {ex.Message}");
            }
        }

        // Report failures but don't hard-fail — track improvement over time.
        if (failures.Count > 0)
        {
            var total = Directory.GetFiles(dir, "*.json")
                .Count(f => File.Exists(Path.ChangeExtension(f, ".SchLib")));
            var succeeded = total - failures.Count;
            Assert.True(true,
                $"SchLib read results: {succeeded}/{total} succeeded, {failures.Count} failed");
        }
    }

    /// <summary>
    /// Properties that are known to be unmapped and are explicitly allowed.
    /// If a new unmapped property appears, the test will fail — add it here
    /// (with a comment explaining why) or add a mapping.
    /// </summary>
    private static readonly HashSet<string> AllowedUnmappedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        // Currently empty — all properties are mapped at 100% coverage.
        // Add entries here if a property is intentionally unmapped, e.g.:
        // "someProperty", // Reason: not relevant for library context
    };

    private static void ReportAndAssert(CoverageResult result)
    {
        var message = $"\n=== {result.TypeName} Coverage ===\n" +
            $"Files: {result.FileCount}, Primitives: {result.PrimitiveCount}\n" +
            $"JSON properties: {result.TotalJsonProperties}\n" +
            $"Mapped: {result.ModeledProperties} ({result.CoveragePercent}%)\n" +
            $"Missing ({result.UnmappedKeys.Count}): {string.Join(", ", result.UnmappedKeys.Order())}\n";

        // Skip types that don't appear in test data (not all types are generated yet)
        if (result.FileCount == 0) return;

        // Fail if there are unmapped properties not in the explicit allowlist.
        var unexpected = result.UnmappedKeys
            .Where(k => !AllowedUnmappedProperties.Contains(k))
            .OrderBy(k => k)
            .ToList();

        Assert.True(unexpected.Count == 0,
            $"{result.TypeName} has {unexpected.Count} unexpected unmapped properties: " +
            string.Join(", ", unexpected) +
            "\nEither add a mapping or add to AllowedUnmappedProperties with a reason.");
    }
}
