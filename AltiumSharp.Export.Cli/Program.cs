using System.CommandLine;
using System.Text.Json;
using OriginalCircuit.AltiumSharp.Export.Analysis;
using OriginalCircuit.AltiumSharp.Export.Comparison;
using OriginalCircuit.AltiumSharp.Export.Exporters;
using OriginalCircuit.AltiumSharp.Export.Models;

namespace OriginalCircuit.AltiumSharp.Export.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Altium file export and comparison tool for reverse engineering")
        {
            CreateExportCommand(),
            CreateInspectCommand(),
            CreateCompareCommand(),
            CreateBatchExportCommand(),
            CreateSchemaCommand(),
            CreateAnalyzeCommand(),
            CreateExploreCommand(),
            CreateBinaryDiffCommand(),
            CreateCorrelateCommand(),
            CreateTemplateCommand(),
            CreateRefsCommand(),
            CreateValidateCommand(),
            CreateDecompressCommand(),
            CreateXRefCommand(),
            CreateLearnCommand()
        };

        return await rootCommand.InvokeAsync(args);
    }

    private static Command CreateExportCommand()
    {
        var inputArg = new Argument<FileInfo>("input", "Input Altium file (.PcbLib, .SchLib, .SchDoc, .PcbDoc)");
        var outputOption = new Option<FileInfo?>(
            aliases: ["-o", "--output"],
            description: "Output JSON file path (default: input file with .json extension)");
        var noRawDataOption = new Option<bool>(
            "--no-raw-data",
            "Exclude raw binary data from output");
        var noRawMcdfOption = new Option<bool>(
            "--no-raw-mcdf",
            "Exclude raw MCDF structure from output");
        var noParsedOption = new Option<bool>(
            "--no-parsed",
            "Exclude parsed model from output");
        var compactOption = new Option<bool>(
            "--compact",
            "Output compact JSON (no indentation)");

        var command = new Command("export", "Export an Altium file to JSON")
        {
            inputArg,
            outputOption,
            noRawDataOption,
            noRawMcdfOption,
            noParsedOption,
            compactOption
        };

        command.SetHandler(async (input, output, noRawData, noRawMcdf, noParsed, compact) =>
        {
            await ExportFile(input, output, noRawData, noRawMcdf, noParsed, compact);
        }, inputArg, outputOption, noRawDataOption, noRawMcdfOption, noParsedOption, compactOption);

        return command;
    }

    private static Command CreateInspectCommand()
    {
        var inputArg = new Argument<FileInfo>("input", "Input Altium file");

        var command = new Command("inspect", "Quick inspection of an Altium file structure")
        {
            inputArg
        };

        command.SetHandler(input =>
        {
            InspectFile(input);
        }, inputArg);

        return command;
    }

    private static Command CreateCompareCommand()
    {
        var file1Arg = new Argument<FileInfo>("file1", "First JSON export file (old version)");
        var file2Arg = new Argument<FileInfo>("file2", "Second JSON export file (new version)");
        var outputOption = new Option<FileInfo?>(
            aliases: ["-o", "--output"],
            description: "Output diff JSON file");
        var summaryOption = new Option<bool>(
            "--summary",
            "Only show summary, not full diff");
        var rawMcdfOnlyOption = new Option<bool>(
            "--raw-mcdf-only",
            "Only compare raw MCDF section");
        var parsedOnlyOption = new Option<bool>(
            "--parsed-only",
            "Only compare parsed model section");
        var newParamsOption = new Option<bool>(
            "--find-new-params",
            "Focus on finding new parameters (for version discovery)");

        var command = new Command("compare", "Compare two JSON exports to find differences")
        {
            file1Arg,
            file2Arg,
            outputOption,
            summaryOption,
            rawMcdfOnlyOption,
            parsedOnlyOption,
            newParamsOption
        };

        command.SetHandler(async (file1, file2, output, summary, rawMcdfOnly, parsedOnly, newParams) =>
        {
            await CompareFiles(file1, file2, output, summary, rawMcdfOnly, parsedOnly, newParams);
        }, file1Arg, file2Arg, outputOption, summaryOption, rawMcdfOnlyOption, parsedOnlyOption, newParamsOption);

        return command;
    }

    private static Command CreateBatchExportCommand()
    {
        var inputArg = new Argument<string>("pattern", "File pattern to match (e.g., '*.PcbLib' or 'libs/**/*.SchLib')");
        var outputDirOption = new Option<DirectoryInfo?>(
            aliases: ["-d", "--output-dir"],
            description: "Output directory for JSON files (default: same as input files)");
        var noRawDataOption = new Option<bool>(
            "--no-raw-data",
            "Exclude raw binary data from output");
        var noRawMcdfOption = new Option<bool>(
            "--no-raw-mcdf",
            "Exclude raw MCDF structure from output");
        var noParsedOption = new Option<bool>(
            "--no-parsed",
            "Exclude parsed model from output");
        var compactOption = new Option<bool>(
            "--compact",
            "Output compact JSON (no indentation)");
        var recursiveOption = new Option<bool>(
            aliases: ["-r", "--recursive"],
            description: "Search subdirectories recursively");

        var command = new Command("batch", "Export multiple Altium files matching a pattern")
        {
            inputArg,
            outputDirOption,
            noRawDataOption,
            noRawMcdfOption,
            noParsedOption,
            compactOption,
            recursiveOption
        };

        command.SetHandler(async (pattern, outputDir, noRawData, noRawMcdf, noParsed, compact, recursive) =>
        {
            await BatchExport(pattern, outputDir, noRawData, noRawMcdf, noParsed, compact, recursive);
        }, inputArg, outputDirOption, noRawDataOption, noRawMcdfOption, noParsedOption, compactOption, recursiveOption);

        return command;
    }

    private static async Task ExportFile(
        FileInfo input,
        FileInfo? output,
        bool noRawData,
        bool noRawMcdf,
        bool noParsed,
        bool compact)
    {
        if (!input.Exists)
        {
            Console.Error.WriteLine($"Error: File not found: {input.FullName}");
            return;
        }

        var options = new ExportOptions
        {
            IncludeRawData = !noRawData,
            IncludeRawMcdfStructure = !noRawMcdf,
            IncludeParsedModel = !noParsed,
            PrettyPrint = !compact
        };

        Console.WriteLine($"Exporting: {input.Name}");

        var result = ExportByExtension(input.FullName, options);

        // Determine output path
        var outputPath = output?.FullName ?? Path.ChangeExtension(input.FullName, ".json");

        // Write result
        await result.SaveToFileAsync(outputPath, !compact);

        Console.WriteLine($"Exported to: {outputPath}");
        PrintExportSummary(result);
    }

    private static ExportResult ExportByExtension(string filePath, ExportOptions options)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        return ext switch
        {
            ".pcblib" => new PcbLibExporter(options).Export(filePath),
            ".schlib" => new SchLibExporter(options).Export(filePath),
            ".schdoc" => new SchDocExporter(options).Export(filePath),
            ".pcbdoc" => new PcbDocExporter(options).Export(filePath),
            _ => new McdfExporter(options).Export(filePath)
        };
    }

    private static void PrintExportSummary(ExportResult result)
    {
        if (result.Metadata.Warnings.Count > 0)
        {
            Console.WriteLine($"\nWarnings ({result.Metadata.Warnings.Count}):");
            foreach (var warning in result.Metadata.Warnings.Take(5))
            {
                Console.WriteLine($"  - {warning}");
            }
            if (result.Metadata.Warnings.Count > 5)
            {
                Console.WriteLine($"  ... and {result.Metadata.Warnings.Count - 5} more");
            }
        }

        if (result.Metadata.Errors.Count > 0)
        {
            Console.WriteLine($"\nErrors ({result.Metadata.Errors.Count}):");
            foreach (var error in result.Metadata.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }

        // Print summary stats
        if (result.ParsedModel != null)
        {
            Console.WriteLine("\nParsed content:");
            if (result.ParsedModel.PcbLib != null)
            {
                Console.WriteLine($"  Components: {result.ParsedModel.PcbLib.Components.Count}");
                var totalPrims = result.ParsedModel.PcbLib.Components.Sum(c => c.Primitives.Count);
                Console.WriteLine($"  Total primitives: {totalPrims}");
            }
            else if (result.ParsedModel.SchLib != null)
            {
                Console.WriteLine($"  Components: {result.ParsedModel.SchLib.Components.Count}");
                var totalPrims = result.ParsedModel.SchLib.Components.Sum(c => c.Primitives.Count);
                Console.WriteLine($"  Total primitives: {totalPrims}");
            }
            else if (result.ParsedModel.SchDoc != null)
            {
                Console.WriteLine($"  Primitives: {result.ParsedModel.SchDoc.Primitives.Count}");
            }
            else if (result.ParsedModel.PcbDoc != null)
            {
                var prims = result.ParsedModel.PcbDoc.Primitives;
                Console.WriteLine($"  Components: {result.ParsedModel.PcbDoc.Components.Count}");
                Console.WriteLine($"  Pads: {prims.PadCount}, Vias: {prims.ViaCount}, Tracks: {prims.TrackCount}");
                Console.WriteLine($"  Arcs: {prims.ArcCount}, Texts: {prims.TextCount}, Fills: {prims.FillCount}");
                Console.WriteLine($"  Regions: {prims.RegionCount}, Bodies: {prims.ComponentBodyCount}");
            }
        }
    }

    private static void InspectFile(FileInfo input)
    {
        if (!input.Exists)
        {
            Console.Error.WriteLine($"Error: File not found: {input.FullName}");
            return;
        }

        var options = new ExportOptions
        {
            IncludeRawData = false,
            IncludeRawMcdfStructure = true,
            IncludeParsedModel = false
        };

        Console.WriteLine($"Inspecting: {input.Name}");
        Console.WriteLine($"Size: {input.Length:N0} bytes");
        Console.WriteLine($"Type: {input.Extension.ToUpperInvariant()}");
        Console.WriteLine();

        using var exporter = new McdfExporter(options);
        var result = exporter.Export(input.FullName);

        if (result.RawMcdf != null)
        {
            PrintStorageTree(result.RawMcdf.RootStorage, 0);
        }
    }

    private static void PrintStorageTree(McdfStorage storage, int indent)
    {
        var prefix = new string(' ', indent * 2);

        if (indent > 0)
        {
            Console.WriteLine($"{prefix}[{storage.Name}]");
        }

        foreach (var stream in storage.Streams)
        {
            var contentType = stream.Content.InterpretedAs;
            var paramCount = stream.Content.Parameters?.Count ?? 0;
            var info = contentType switch
            {
                "Parameters" => $"({paramCount} params)",
                "Binary" => $"({stream.Size} bytes)",
                "Empty" => "(empty)",
                "Text" => $"({stream.Size} chars)",
                _ => $"({contentType})"
            };

            Console.WriteLine($"{prefix}  {stream.Name}: {info}");

            // Show first few parameters
            if (stream.Content.Parameters != null && stream.Content.Parameters.Count > 0)
            {
                var shown = 0;
                foreach (var (key, value) in stream.Content.Parameters.Take(3))
                {
                    var displayValue = value.Length > 50 ? value[..47] + "..." : value;
                    Console.WriteLine($"{prefix}    {key} = {displayValue}");
                    shown++;
                }
                if (stream.Content.Parameters.Count > shown)
                {
                    Console.WriteLine($"{prefix}    ... and {stream.Content.Parameters.Count - shown} more");
                }
            }
        }

        foreach (var childStorage in storage.Storages)
        {
            PrintStorageTree(childStorage, indent + 1);
        }
    }

    private static async Task CompareFiles(
        FileInfo file1,
        FileInfo file2,
        FileInfo? output,
        bool summaryOnly,
        bool rawMcdfOnly,
        bool parsedOnly,
        bool findNewParams)
    {
        if (!file1.Exists)
        {
            Console.Error.WriteLine($"Error: File not found: {file1.FullName}");
            return;
        }
        if (!file2.Exists)
        {
            Console.Error.WriteLine($"Error: File not found: {file2.FullName}");
            return;
        }

        Console.WriteLine($"Comparing:");
        Console.WriteLine($"  Old: {file1.Name}");
        Console.WriteLine($"  New: {file2.Name}");
        Console.WriteLine();

        var options = new DiffOptions
        {
            RawMcdfOnly = rawMcdfOnly,
            ParsedModelOnly = parsedOnly,
            FindNewParametersOnly = findNewParams
        };

        var differ = new JsonDiffer(options);
        var result = differ.Compare(file1.FullName, file2.FullName);

        // Print summary
        Console.WriteLine("Summary:");
        Console.WriteLine($"  Total differences: {result.Summary.TotalDifferences}");
        Console.WriteLine($"  Added fields: {result.Summary.AddedFields}");
        Console.WriteLine($"  Removed fields: {result.Summary.RemovedFields}");
        Console.WriteLine($"  Changed values: {result.Summary.ChangedValues}");

        if (result.Summary.BySection.Count > 0)
        {
            Console.WriteLine("\n  By section:");
            foreach (var (section, count) in result.Summary.BySection)
            {
                Console.WriteLine($"    {section}: {count}");
            }
        }

        // Print new parameters found
        if (result.NewParameters.Count > 0)
        {
            Console.WriteLine($"\nNew parameters discovered ({result.NewParameters.Count}):");
            foreach (var param in result.NewParameters.Take(10))
            {
                Console.WriteLine($"  {param.Name} = {(param.Value.Length > 30 ? param.Value[..27] + "..." : param.Value)}");
                if (param.LikelyPurpose != null)
                {
                    Console.WriteLine($"    Likely: {param.LikelyPurpose}");
                }
            }
            if (result.NewParameters.Count > 10)
            {
                Console.WriteLine($"  ... and {result.NewParameters.Count - 10} more");
            }
        }

        // Print differences (unless summary only)
        if (!summaryOnly && result.Differences.Count > 0)
        {
            Console.WriteLine($"\nDifferences ({result.Differences.Count}):");
            foreach (var diff in result.Differences.Take(20))
            {
                var typeStr = diff.Type switch
                {
                    DifferenceType.Added => "+",
                    DifferenceType.Removed => "-",
                    DifferenceType.ValueChanged => "~",
                    DifferenceType.TypeChanged => "!",
                    DifferenceType.ArrayItemAdded => "[+]",
                    DifferenceType.ArrayItemRemoved => "[-]",
                    DifferenceType.ArrayItemChanged => "[~]",
                    _ => "?"
                };

                Console.WriteLine($"  {typeStr} {diff.Path}");
                if (diff.OldValue != null)
                    Console.WriteLine($"      Old: {diff.OldValue}");
                if (diff.NewValue != null)
                    Console.WriteLine($"      New: {diff.NewValue}");
            }
            if (result.Differences.Count > 20)
            {
                Console.WriteLine($"  ... and {result.Differences.Count - 20} more");
            }
        }

        // Save to file if requested
        if (output != null)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(result, jsonOptions);
            await File.WriteAllTextAsync(output.FullName, json);
            Console.WriteLine($"\nDiff saved to: {output.FullName}");
        }
    }

    private static async Task BatchExport(
        string pattern,
        DirectoryInfo? outputDir,
        bool noRawData,
        bool noRawMcdf,
        bool noParsed,
        bool compact,
        bool recursive)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var directory = Path.GetDirectoryName(pattern);
        var filePattern = Path.GetFileName(pattern);

        if (string.IsNullOrEmpty(directory))
        {
            directory = ".";
        }

        if (!Directory.Exists(directory))
        {
            Console.Error.WriteLine($"Error: Directory not found: {directory}");
            return;
        }

        var files = Directory.GetFiles(directory, filePattern, searchOption);

        if (files.Length == 0)
        {
            Console.WriteLine($"No files found matching: {pattern}");
            return;
        }

        Console.WriteLine($"Found {files.Length} files to export");
        Console.WriteLine();

        var options = new ExportOptions
        {
            IncludeRawData = !noRawData,
            IncludeRawMcdfStructure = !noRawMcdf,
            IncludeParsedModel = !noParsed,
            PrettyPrint = !compact
        };

        int success = 0;
        int failed = 0;

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            Console.Write($"  {fileName}...");

            try
            {
                var result = ExportByExtension(file, options);

                // Determine output path
                string outputPath;
                if (outputDir != null)
                {
                    if (!outputDir.Exists)
                    {
                        outputDir.Create();
                    }
                    outputPath = Path.Combine(outputDir.FullName, Path.ChangeExtension(fileName, ".json"));
                }
                else
                {
                    outputPath = Path.ChangeExtension(file, ".json");
                }

                await result.SaveToFileAsync(outputPath, !compact);

                Console.WriteLine(" OK");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($" FAILED: {ex.Message}");
                failed++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Completed: {success} succeeded, {failed} failed");
    }

    private static Command CreateSchemaCommand()
    {
        var inputArg = new Argument<string[]>("files", "JSON export files to analyze")
        {
            Arity = ArgumentArity.OneOrMore
        };
        var outputOption = new Option<FileInfo?>(
            aliases: ["-o", "--output"],
            description: "Output schema file (JSON or Markdown based on extension)");
        var formatOption = new Option<string>(
            aliases: ["-f", "--format"],
            description: "Output format (json, markdown)",
            getDefaultValue: () => "json");

        var command = new Command("schema", "Build a schema from multiple JSON exports")
        {
            inputArg,
            outputOption,
            formatOption
        };

        command.SetHandler(async (files, output, format) =>
        {
            await BuildSchema(files, output, format);
        }, inputArg, outputOption, formatOption);

        return command;
    }

    private static Command CreateAnalyzeCommand()
    {
        var inputArg = new Argument<FileInfo>("input", "Input Altium file to analyze");
        var streamOption = new Option<string?>(
            aliases: ["-s", "--stream"],
            description: "Specific stream path to analyze (e.g., /FileHeader)");
        var unknownOption = new Option<bool>(
            "--unknown",
            "Focus on unknown/undocumented fields");
        var binaryOption = new Option<bool>(
            "--binary",
            "Perform binary field analysis");
        var outputOption = new Option<FileInfo?>(
            aliases: ["-o", "--output"],
            description: "Output analysis to file (Markdown format)");

        var command = new Command("analyze", "Deep analysis of Altium file structure")
        {
            inputArg,
            streamOption,
            unknownOption,
            binaryOption,
            outputOption
        };

        command.SetHandler(async (input, stream, unknown, binary, output) =>
        {
            await AnalyzeFile(input, stream, unknown, binary, output);
        }, inputArg, streamOption, unknownOption, binaryOption, outputOption);

        return command;
    }

    private static Command CreateExploreCommand()
    {
        var inputArg = new Argument<FileInfo>("input", "Input Altium file to explore");

        var command = new Command("explore", "Interactive exploration of Altium file structure")
        {
            inputArg
        };

        command.SetHandler(input =>
        {
            RunExplorer(input);
        }, inputArg);

        return command;
    }

    private static async Task BuildSchema(string[] files, FileInfo? output, string format)
    {
        var builder = new SchemaBuilder();
        var docGen = new DocumentationGenerator();

        Console.WriteLine($"Building schema from {files.Length} files...");
        Console.WriteLine();

        foreach (var file in files)
        {
            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"Warning: File not found: {file}");
                continue;
            }

            Console.WriteLine($"  Processing: {Path.GetFileName(file)}");

            try
            {
                builder.AddExport(file);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"    Error: {ex.Message}");
            }
        }

        var schema = builder.Build();

        Console.WriteLine();
        Console.WriteLine("Schema Summary:");
        Console.WriteLine($"  Files analyzed: {schema.FileCount}");
        Console.WriteLine($"  Storages found: {schema.Storages.Count}");
        Console.WriteLine($"  Streams found: {schema.Streams.Count}");
        Console.WriteLine($"  Record types: {schema.RecordTypes.Count}");

        if (output != null)
        {
            var ext = output.Extension.ToLowerInvariant();
            var useMarkdown = format.Equals("markdown", StringComparison.OrdinalIgnoreCase) ||
                              format.Equals("md", StringComparison.OrdinalIgnoreCase) ||
                              ext == ".md";

            if (useMarkdown)
            {
                var markdown = docGen.GenerateMarkdown(schema);
                await File.WriteAllTextAsync(output.FullName, markdown);
                Console.WriteLine($"\nMarkdown documentation saved to: {output.FullName}");
            }
            else
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(schema, jsonOptions);
                await File.WriteAllTextAsync(output.FullName, json);
                Console.WriteLine($"\nJSON schema saved to: {output.FullName}");
            }
        }
    }

    private static async Task AnalyzeFile(
        FileInfo input,
        string? streamPath,
        bool focusUnknown,
        bool binaryAnalysis,
        FileInfo? output)
    {
        if (!input.Exists)
        {
            Console.Error.WriteLine($"Error: File not found: {input.FullName}");
            return;
        }

        Console.WriteLine($"Analyzing: {input.Name}");
        Console.WriteLine();

        using var explorer = new InteractiveExplorer();
        var loadResult = explorer.Load(input.FullName);

        if (!loadResult.Success)
        {
            Console.Error.WriteLine($"Error: {loadResult.Message}");
            return;
        }

        var unknownTracker = new UnknownFieldTracker();
        var binaryAnalyzer = new BinaryFieldAnalyzer();
        var docGen = new DocumentationGenerator();
        var reports = new List<UnknownFieldReport>();
        var binaryResults = new List<(string Path, BinaryAnalysisResult Analysis)>();

        if (!string.IsNullOrEmpty(streamPath))
        {
            // Analyze specific stream
            var readResult = explorer.Read(streamPath, analyze: binaryAnalysis);
            if (readResult.Success && readResult.StreamInfo != null)
            {
                ProcessStreamAnalysis(readResult.StreamInfo, unknownTracker, binaryAnalyzer,
                    focusUnknown, binaryAnalysis, reports, binaryResults, input.FullName);
            }
            else
            {
                Console.Error.WriteLine($"Error: {readResult.Message}");
            }
        }
        else
        {
            // Analyze all streams recursively
            AnalyzeAllStreams(explorer, "/", unknownTracker, binaryAnalyzer,
                focusUnknown, binaryAnalysis, reports, binaryResults, input.FullName);
        }

        // Print results
        if (focusUnknown && reports.Count > 0)
        {
            var summary = unknownTracker.GetSummary(reports);

            Console.WriteLine("Unknown Fields Summary:");
            Console.WriteLine($"  Reports analyzed: {summary.TotalReportsAnalyzed}");
            Console.WriteLine($"  Total parameters: {summary.TotalParametersAnalyzed}");
            Console.WriteLine($"  Unknown fields: {summary.TotalUnknownFields}");
            Console.WriteLine();

            if (summary.UnknownFields.Count > 0)
            {
                Console.WriteLine("Top unknown fields:");
                foreach (var field in summary.UnknownFields.Take(15))
                {
                    Console.WriteLine($"  {field.Name} ({field.InferredType}) - {field.Occurrences}x");
                    if (field.PossiblePurpose != null)
                    {
                        Console.WriteLine($"    Possible: {field.PossiblePurpose}");
                    }
                }
            }

            if (output != null)
            {
                var markdown = docGen.GenerateUnknownFieldsDoc(summary);
                await File.WriteAllTextAsync(output.FullName, markdown);
                Console.WriteLine($"\nReport saved to: {output.FullName}");
            }
        }

        if (binaryAnalysis && binaryResults.Count > 0)
        {
            Console.WriteLine("\nBinary Analysis Results:");
            foreach (var (path, analysis) in binaryResults)
            {
                Console.WriteLine($"\n  {path}:");
                Console.WriteLine($"    Total bytes: {analysis.TotalBytes}");
                if (analysis.Blocks?.Count > 0)
                    Console.WriteLine($"    Blocks found: {analysis.Blocks.Count}");
                if (analysis.Fields?.Count > 0)
                    Console.WriteLine($"    Fields detected: {analysis.Fields.Count}");
                if (analysis.Strings?.Count > 0)
                    Console.WriteLine($"    Strings found: {analysis.Strings.Count}");
                if (analysis.CoordinatePairs?.Count > 0)
                    Console.WriteLine($"    Coord pairs: {analysis.CoordinatePairs.Count}");
            }
        }
    }

    private static void ProcessStreamAnalysis(
        StreamInfo streamInfo,
        UnknownFieldTracker unknownTracker,
        BinaryFieldAnalyzer binaryAnalyzer,
        bool focusUnknown,
        bool binaryAnalysis,
        List<UnknownFieldReport> reports,
        List<(string, BinaryAnalysisResult)> binaryResults,
        string fileName)
    {
        var isPcb = fileName.EndsWith(".PcbLib", StringComparison.OrdinalIgnoreCase) ||
                    fileName.EndsWith(".PcbDoc", StringComparison.OrdinalIgnoreCase);

        if (focusUnknown && streamInfo.Parameters != null)
        {
            var report = unknownTracker.AnalyzeParameters(streamInfo.Parameters, streamInfo.Path, isPcb);
            reports.Add(report);
        }

        if (binaryAnalysis && streamInfo.RawData != null)
        {
            var analysis = binaryAnalyzer.Analyze(streamInfo.RawData, streamInfo.Path);
            binaryResults.Add((streamInfo.Path, analysis));
        }
    }

    private static void AnalyzeAllStreams(
        InteractiveExplorer explorer,
        string currentPath,
        UnknownFieldTracker unknownTracker,
        BinaryFieldAnalyzer binaryAnalyzer,
        bool focusUnknown,
        bool binaryAnalysis,
        List<UnknownFieldReport> reports,
        List<(string, BinaryAnalysisResult)> binaryResults,
        string fileName)
    {
        var listResult = explorer.Navigate(currentPath);
        if (!listResult.Success || listResult.AvailableItems == null) return;

        foreach (var item in listResult.AvailableItems)
        {
            if (item.Type == ItemType.Parent) continue;

            var itemPath = currentPath == "/" ? "/" + item.Name : currentPath + "/" + item.Name;

            if (item.Type == ItemType.Storage)
            {
                AnalyzeAllStreams(explorer, itemPath, unknownTracker, binaryAnalyzer,
                    focusUnknown, binaryAnalysis, reports, binaryResults, fileName);
            }
            else if (item.Type == ItemType.Stream)
            {
                var readResult = explorer.Read(itemPath, analyze: binaryAnalysis);
                if (readResult.Success && readResult.StreamInfo != null)
                {
                    ProcessStreamAnalysis(readResult.StreamInfo, unknownTracker, binaryAnalyzer,
                        focusUnknown, binaryAnalysis, reports, binaryResults, fileName);
                }
            }
        }

        // Navigate back
        explorer.NavigateUp();
    }

    private static void RunExplorer(FileInfo input)
    {
        if (!input.Exists)
        {
            Console.Error.WriteLine($"Error: File not found: {input.FullName}");
            return;
        }

        using var explorer = new InteractiveExplorer();
        var result = explorer.Load(input.FullName);

        if (!result.Success)
        {
            Console.Error.WriteLine($"Error: {result.Message}");
            return;
        }

        Console.WriteLine($"Loaded: {input.Name}");
        Console.WriteLine("Type 'help' for available commands, 'quit' to exit.");
        Console.WriteLine();

        PrintExplorerItems(result);

        while (true)
        {
            Console.Write($"{explorer.CurrentPath}> ");
            var line = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Trim().Split(' ', 2);
            var cmd = parts[0].ToLowerInvariant();
            var arg = parts.Length > 1 ? parts[1] : null;

            switch (cmd)
            {
                case "quit":
                case "exit":
                case "q":
                    return;

                case "help":
                case "?":
                    PrintExplorerHelp();
                    break;

                case "ls":
                case "list":
                case "dir":
                    result = explorer.List();
                    PrintExplorerItems(result);
                    break;

                case "cd":
                    if (arg != null)
                    {
                        result = explorer.Navigate(arg);
                        if (!result.Success)
                            Console.WriteLine($"Error: {result.Message}");
                        else
                            PrintExplorerItems(result);
                    }
                    else
                    {
                        Console.WriteLine("Usage: cd <path>");
                    }
                    break;

                case "..":
                    result = explorer.NavigateUp();
                    PrintExplorerItems(result);
                    break;

                case "back":
                    result = explorer.Back();
                    if (!result.Success)
                        Console.WriteLine(result.Message);
                    else
                        PrintExplorerItems(result);
                    break;

                case "read":
                case "cat":
                    if (arg != null)
                    {
                        result = explorer.Read(arg);
                        PrintStreamInfo(result);
                    }
                    else
                    {
                        Console.WriteLine("Usage: read <stream>");
                    }
                    break;

                case "hex":
                case "hexdump":
                    if (arg != null)
                    {
                        var hexParts = arg.Split(' ', 2);
                        var stream = hexParts[0];
                        var len = hexParts.Length > 1 && int.TryParse(hexParts[1], out var l) ? l : 256;
                        result = explorer.HexDump(stream, 0, len);
                        Console.WriteLine(result.Message);
                    }
                    else
                    {
                        Console.WriteLine("Usage: hex <stream> [length]");
                    }
                    break;

                case "analyze":
                    if (arg != null)
                    {
                        result = explorer.Read(arg, analyze: true);
                        PrintBinaryAnalysis(result);
                    }
                    else
                    {
                        Console.WriteLine("Usage: analyze <stream>");
                    }
                    break;

                case "unknown":
                    if (arg != null)
                    {
                        result = explorer.GetUnknownFields(arg);
                        PrintUnknownFields(result);
                    }
                    else
                    {
                        Console.WriteLine("Usage: unknown <stream>");
                    }
                    break;

                case "search":
                case "find":
                    if (arg != null)
                    {
                        result = explorer.Search(arg);
                        PrintSearchResults(result);
                    }
                    else
                    {
                        Console.WriteLine("Usage: search <pattern>");
                    }
                    break;

                case "stats":
                case "info":
                    result = explorer.GetStats();
                    PrintFileStats(result);
                    break;

                default:
                    // Try to navigate to the path
                    result = explorer.Navigate(cmd);
                    if (result.Success)
                    {
                        if (result.StreamInfo != null)
                            PrintStreamInfo(result);
                        else
                            PrintExplorerItems(result);
                    }
                    else
                    {
                        Console.WriteLine($"Unknown command: {cmd}. Type 'help' for commands.");
                    }
                    break;
            }
        }
    }

    private static void PrintExplorerHelp()
    {
        Console.WriteLine("Commands:");
        Console.WriteLine("  ls, list, dir        - List contents of current path");
        Console.WriteLine("  cd <path>            - Navigate to storage or view stream");
        Console.WriteLine("  ..                   - Go up one level");
        Console.WriteLine("  back                 - Go back in navigation history");
        Console.WriteLine("  read <stream>        - Read and parse stream content");
        Console.WriteLine("  hex <stream> [len]   - Show hex dump of stream");
        Console.WriteLine("  analyze <stream>     - Binary field analysis of stream");
        Console.WriteLine("  unknown <stream>     - Show unknown/undocumented fields");
        Console.WriteLine("  search <pattern>     - Search for pattern in all streams");
        Console.WriteLine("  stats, info          - Show file statistics");
        Console.WriteLine("  quit, exit, q        - Exit explorer");
        Console.WriteLine();
    }

    private static void PrintExplorerItems(ExplorerResult result)
    {
        if (result.AvailableItems == null || result.AvailableItems.Count == 0)
        {
            Console.WriteLine("(empty)");
            return;
        }

        foreach (var item in result.AvailableItems)
        {
            switch (item.Type)
            {
                case ItemType.Parent:
                    Console.WriteLine("  [..]");
                    break;
                case ItemType.Storage:
                    Console.WriteLine($"  [{item.Name}]/");
                    break;
                case ItemType.Stream:
                    Console.WriteLine($"  {item.Name} ({item.Size} bytes)");
                    break;
            }
        }
    }

    private static void PrintStreamInfo(ExplorerResult result)
    {
        if (!result.Success)
        {
            Console.WriteLine($"Error: {result.Message}");
            return;
        }

        var info = result.StreamInfo;
        if (info == null) return;

        Console.WriteLine($"Stream: {info.Name}");
        Console.WriteLine($"Size: {info.Size} bytes");
        Console.WriteLine($"Type: {info.ContentType}");
        Console.WriteLine();

        if (info.Parameters != null && info.Parameters.Count > 0)
        {
            Console.WriteLine("Parameters:");
            foreach (var (key, value) in info.Parameters.Take(20))
            {
                var displayValue = value.Length > 60 ? value[..57] + "..." : value;
                Console.WriteLine($"  {key} = {displayValue}");
            }
            if (info.Parameters.Count > 20)
            {
                Console.WriteLine($"  ... and {info.Parameters.Count - 20} more");
            }
        }
        else if (info.TextContent != null)
        {
            Console.WriteLine("Content:");
            var lines = info.TextContent.Split('\n').Take(20);
            foreach (var line in lines)
            {
                Console.WriteLine($"  {line}");
            }
        }
    }

    private static void PrintBinaryAnalysis(ExplorerResult result)
    {
        if (!result.Success)
        {
            Console.WriteLine($"Error: {result.Message}");
            return;
        }

        var analysis = result.StreamInfo?.BinaryAnalysis;
        if (analysis == null)
        {
            Console.WriteLine("No binary analysis available");
            return;
        }

        Console.WriteLine($"Binary Analysis: {analysis.TotalBytes} bytes");

        if (analysis.Blocks?.Count > 0)
        {
            Console.WriteLine($"\nBlocks ({analysis.Blocks.Count}):");
            foreach (var block in analysis.Blocks.Take(10))
            {
                Console.WriteLine($"  @0x{block.Offset:X4}: {block.ContentSize} bytes, {block.BlockType}");
            }
        }

        if (analysis.Fields?.Count > 0)
        {
            Console.WriteLine($"\nDetected Fields ({analysis.Fields.Count}):");
            foreach (var field in analysis.Fields.Take(10))
            {
                Console.WriteLine($"  @0x{field.Offset:X4}: {field.Type} = {field.InterpretedValue}");
            }
        }

        if (analysis.Strings?.Count > 0)
        {
            Console.WriteLine($"\nStrings ({analysis.Strings.Count}):");
            foreach (var str in analysis.Strings.Take(10))
            {
                var display = str.Value.Length > 40 ? str.Value[..37] + "..." : str.Value;
                Console.WriteLine($"  @0x{str.Offset:X4}: \"{display}\"");
            }
        }
    }

    private static void PrintUnknownFields(ExplorerResult result)
    {
        if (!result.Success)
        {
            Console.WriteLine($"Error: {result.Message}");
            return;
        }

        Console.WriteLine(result.Message);

        var report = result.UnknownFieldReport;
        if (report == null) return;

        if (report.UnknownParameters.Count > 0)
        {
            Console.WriteLine("\nUnknown parameters:");
            foreach (var param in report.UnknownParameters.Take(15))
            {
                var display = param.Value.Length > 40 ? param.Value[..37] + "..." : param.Value;
                Console.WriteLine($"  {param.Name} ({param.ValueType}) = {display}");
                if (param.PossiblePurpose != null)
                {
                    Console.WriteLine($"    -> {param.PossiblePurpose}");
                }
            }
            if (report.UnknownParameters.Count > 15)
            {
                Console.WriteLine($"  ... and {report.UnknownParameters.Count - 15} more");
            }
        }
    }

    private static void PrintSearchResults(ExplorerResult result)
    {
        Console.WriteLine(result.Message);

        if (result.SearchResults == null || result.SearchResults.Count == 0)
            return;

        Console.WriteLine();
        foreach (var match in result.SearchResults.Take(20))
        {
            Console.WriteLine($"  {match.Path} ({match.Type})");
            if (match.Type.Contains("content"))
            {
                Console.WriteLine($"    ...{match.Match}...");
            }
        }
        if (result.SearchResults.Count > 20)
        {
            Console.WriteLine($"  ... and {result.SearchResults.Count - 20} more");
        }
    }

    private static void PrintFileStats(ExplorerResult result)
    {
        if (!result.Success || result.Stats == null)
        {
            Console.WriteLine($"Error: {result.Message}");
            return;
        }

        var stats = result.Stats;
        Console.WriteLine("File Statistics:");
        Console.WriteLine($"  File name: {stats.FileName}");
        Console.WriteLine($"  File size: {stats.FileSize:N0} bytes");
        Console.WriteLine($"  Storages: {stats.StorageCount}");
        Console.WriteLine($"  Streams: {stats.StreamCount}");
        Console.WriteLine($"  Total stream data: {stats.TotalStreamSize:N0} bytes");
    }

    // ============== New Analysis Commands ==============

    private static Command CreateBinaryDiffCommand()
    {
        var file1Arg = new Argument<FileInfo>("file1", "First Altium file (original)");
        var file2Arg = new Argument<FileInfo>("file2", "Second Altium file (modified)");
        var outputOption = new Option<FileInfo?>(
            aliases: ["-o", "--output"],
            description: "Output diff result to JSON file");
        var verboseOption = new Option<bool>(
            aliases: ["-v", "--verbose"],
            description: "Show detailed byte-level changes");

        var command = new Command("bindiff", "Binary-level comparison between two Altium files")
        {
            file1Arg,
            file2Arg,
            outputOption,
            verboseOption
        };

        command.SetHandler(async (file1, file2, output, verbose) =>
        {
            await RunBinaryDiff(file1, file2, output, verbose);
        }, file1Arg, file2Arg, outputOption, verboseOption);

        return command;
    }

    private static async Task RunBinaryDiff(FileInfo file1, FileInfo file2, FileInfo? output, bool verbose)
    {
        if (!file1.Exists || !file2.Exists)
        {
            Console.Error.WriteLine("Error: One or both files not found");
            return;
        }

        Console.WriteLine($"Comparing binary differences:");
        Console.WriteLine($"  Original: {file1.Name}");
        Console.WriteLine($"  Modified: {file2.Name}");
        Console.WriteLine();

        using var diffTool = new BinaryDiffTool();
        var result = diffTool.Compare(file1.FullName, file2.FullName);

        Console.WriteLine("Summary:");
        Console.WriteLine($"  File 1 size: {result.File1Size:N0} bytes");
        Console.WriteLine($"  File 2 size: {result.File2Size:N0} bytes");
        Console.WriteLine($"  Structural changes: {result.StructuralChanges.Count}");
        Console.WriteLine($"  Streams changed: {result.TotalStreamsChanged}");
        Console.WriteLine($"  Bytes changed: {result.TotalBytesChanged:N0}");

        if (result.StructuralChanges.Count > 0)
        {
            Console.WriteLine("\nStructural Changes:");
            foreach (var change in result.StructuralChanges.Take(10))
            {
                var symbol = change.Type switch
                {
                    StructuralChangeType.Added => "+",
                    StructuralChangeType.Removed => "-",
                    StructuralChangeType.TypeChanged => "~",
                    _ => "?"
                };
                Console.WriteLine($"  {symbol} {change.Path}: {change.Description}");
            }
        }

        if (verbose && result.StreamDiffs.Count > 0)
        {
            Console.WriteLine("\nStream Differences:");
            foreach (var diff in result.StreamDiffs.Take(10))
            {
                Console.WriteLine($"\n  {diff.Path}:");
                Console.WriteLine($"    Size: {diff.Size1} -> {diff.Size2} ({diff.SizeDelta:+0;-0;0} bytes)");
                Console.WriteLine($"    Changes: {diff.Changes.Count}");

                foreach (var change in diff.Changes.Take(5))
                {
                    var interp = change.Interpretation != null
                        ? $" ({change.Interpretation.Type}: {change.Interpretation.OldValue} -> {change.Interpretation.NewValue})"
                        : "";
                    Console.WriteLine($"      @0x{change.Offset:X4}: {change.Length} bytes{interp}");
                }
                if (diff.Changes.Count > 5)
                {
                    Console.WriteLine($"      ... and {diff.Changes.Count - 5} more changes");
                }
            }
        }

        if (output != null)
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await File.WriteAllTextAsync(output.FullName, json);
            Console.WriteLine($"\nDiff saved to: {output.FullName}");
        }
    }

    private static Command CreateCorrelateCommand()
    {
        var beforeArg = new Argument<FileInfo>("before", "File before the change");
        var afterArg = new Argument<FileInfo>("after", "File after the change");
        var actionOption = new Option<string>(
            aliases: ["-a", "--action"],
            description: "Description of the action performed (e.g., 'moved pad 10mil right')");
        var kbOption = new Option<FileInfo?>(
            aliases: ["-k", "--knowledge-base"],
            description: "Knowledge base file to load/save correlations");

        var command = new Command("correlate", "Correlate file changes with specific actions for pattern learning")
        {
            beforeArg,
            afterArg,
            actionOption,
            kbOption
        };

        command.SetHandler(async (before, after, action, kb) =>
        {
            await RunCorrelate(before, after, action, kb);
        }, beforeArg, afterArg, actionOption, kbOption);

        return command;
    }

    private static async Task RunCorrelate(FileInfo before, FileInfo after, string? action, FileInfo? kbFile)
    {
        if (!before.Exists || !after.Exists)
        {
            Console.Error.WriteLine("Error: One or both files not found");
            return;
        }

        if (string.IsNullOrEmpty(action))
        {
            Console.Error.WriteLine("Error: Action description is required (-a)");
            return;
        }

        var correlator = new ChangeCorrelationTool();

        if (kbFile?.Exists == true)
        {
            correlator.LoadKnowledgeBase(kbFile.FullName);
            Console.WriteLine($"Loaded knowledge base: {kbFile.Name}");
        }

        Console.WriteLine($"Recording change: {action}");
        var correlation = correlator.RecordChange(before.FullName, after.FullName, action);

        Console.WriteLine($"\nCorrelation recorded:");
        Console.WriteLine($"  Category: {correlation.Category}");
        Console.WriteLine($"  Affected streams: {correlation.AffectedStreams.Count}");
        Console.WriteLine($"  Total bytes changed: {correlation.TotalBytesChanged}");

        if (correlation.AffectedStreams.Count > 0)
        {
            Console.WriteLine("\n  Streams affected:");
            foreach (var stream in correlation.AffectedStreams.Take(5))
            {
                Console.WriteLine($"    {stream.Path}: {stream.BytesChanged} bytes");
            }
        }

        if (kbFile != null)
        {
            await correlator.SaveKnowledgeBaseAsync(kbFile.FullName);
            Console.WriteLine($"\nKnowledge base saved to: {kbFile.FullName}");

            var summary = correlator.GetSummary();
            Console.WriteLine($"Total recorded changes: {summary.TotalChanges}");
        }
    }

    private static Command CreateTemplateCommand()
    {
        var inputArg = new Argument<FileInfo>("input", "Input Altium file");
        var streamOption = new Option<string>(
            aliases: ["-s", "--stream"],
            description: "Stream path to apply template to");
        var templateOption = new Option<string?>(
            aliases: ["-t", "--template"],
            description: "Template name (e.g., SizePrefixedBlock, PcbPadHeader) or file path");
        var listOption = new Option<bool>(
            "--list",
            description: "List available built-in templates");

        var command = new Command("template", "Apply binary templates to parse stream data")
        {
            inputArg,
            streamOption,
            templateOption,
            listOption
        };

        command.SetHandler((input, stream, template, list) =>
        {
            RunTemplate(input, stream, template, list);
        }, inputArg, streamOption, templateOption, listOption);

        return command;
    }

    private static void RunTemplate(FileInfo input, string? streamPath, string? templateName, bool list)
    {
        var templateEngine = new BinaryTemplateEngine();

        // Register some built-in templates
        RegisterBuiltInTemplates(templateEngine);

        if (list)
        {
            Console.WriteLine("Built-in templates:");
            Console.WriteLine("  SizePrefixedBlock: Common Altium block format with 4-byte size prefix");
            Console.WriteLine("  PcbPadHeader: PCB pad record header structure");
            Console.WriteLine("  CoordPair: X,Y coordinate pair");
            return;
        }

        if (!input.Exists)
        {
            Console.Error.WriteLine($"Error: File not found: {input.FullName}");
            return;
        }

        if (string.IsNullOrEmpty(streamPath) || string.IsNullOrEmpty(templateName))
        {
            Console.Error.WriteLine("Error: Both --stream and --template are required");
            return;
        }

        using var explorer = new InteractiveExplorer();
        var loadResult = explorer.Load(input.FullName);

        if (!loadResult.Success)
        {
            Console.Error.WriteLine($"Error: {loadResult.Message}");
            return;
        }

        var readResult = explorer.Read(streamPath);
        if (!readResult.Success || readResult.StreamInfo?.RawData == null)
        {
            Console.Error.WriteLine($"Error: Could not read stream: {streamPath}");
            return;
        }

        Console.WriteLine($"Applying template '{templateName}' to {streamPath}");
        Console.WriteLine($"Data size: {readResult.StreamInfo.RawData.Length} bytes");
        Console.WriteLine();

        var parsed = templateEngine.Apply(templateName, readResult.StreamInfo.RawData);

        if (!parsed.Success)
        {
            Console.Error.WriteLine($"Error: {parsed.Error}");
            return;
        }

        Console.WriteLine($"Parsed fields ({parsed.Fields.Count}):");
        foreach (var field in parsed.Fields)
        {
            Console.WriteLine($"  @0x{field.StartOffset:X4} {field.FieldName}: {field.InterpretedValue ?? field.RawValue?.ToString()} ({field.DataType})");
        }

        var unparsed = readResult.StreamInfo.RawData.Length - parsed.TotalSize;
        if (unparsed > 0)
        {
            Console.WriteLine($"\nUnparsed bytes: {unparsed}");
        }
    }

    private static void RegisterBuiltInTemplates(BinaryTemplateEngine engine)
    {
        // Size-prefixed block
        engine.RegisterTemplate(new BinaryTemplate
        {
            Name = "SizePrefixedBlock",
            Description = "Common Altium block format with 4-byte size prefix",
            Fields =
            [
                new TemplateField { Name = "Size", Type = FieldDataType.Int32 },
                new TemplateField { Name = "Data", Type = FieldDataType.Bytes, SizeReference = "Size", SizeMask = 0x00FFFFFF }
            ]
        });

        // Coordinate pair
        engine.RegisterTemplate(new BinaryTemplate
        {
            Name = "CoordPair",
            Description = "X,Y coordinate pair",
            Fields =
            [
                new TemplateField { Name = "X", Type = FieldDataType.Coord },
                new TemplateField { Name = "Y", Type = FieldDataType.Coord }
            ]
        });

        // PCB Pad header
        engine.RegisterTemplate(new BinaryTemplate
        {
            Name = "PcbPadHeader",
            Description = "PCB pad record header structure",
            Fields =
            [
                new TemplateField { Name = "Size", Type = FieldDataType.Int32, SizeMask = 0x00FFFFFF },
                new TemplateField { Name = "Layer", Type = FieldDataType.Byte },
                new TemplateField { Name = "Flags", Type = FieldDataType.UInt16 },
                new TemplateField { Name = "X", Type = FieldDataType.Coord },
                new TemplateField { Name = "Y", Type = FieldDataType.Coord },
                new TemplateField { Name = "TopXSize", Type = FieldDataType.Coord },
                new TemplateField { Name = "TopYSize", Type = FieldDataType.Coord }
            ]
        });
    }

    private static Command CreateRefsCommand()
    {
        var inputArg = new Argument<FileInfo>("input", "Input Altium file to analyze");
        var outputOption = new Option<FileInfo?>(
            aliases: ["-o", "--output"],
            description: "Output reference graph to JSON file");
        var graphOption = new Option<int?>(
            aliases: ["-g", "--graph"],
            description: "Show object graph starting from record index");

        var command = new Command("refs", "Track references and indices between records in Altium files")
        {
            inputArg,
            outputOption,
            graphOption
        };

        command.SetHandler(async (input, output, graphIndex) =>
        {
            await RunRefs(input, output, graphIndex);
        }, inputArg, outputOption, graphOption);

        return command;
    }

    private static async Task RunRefs(FileInfo input, FileInfo? output, int? graphIndex)
    {
        if (!input.Exists)
        {
            Console.Error.WriteLine($"Error: File not found: {input.FullName}");
            return;
        }

        Console.WriteLine($"Analyzing references in: {input.Name}");
        Console.WriteLine();

        using var tracker = new ReferenceTracker();
        var result = tracker.Analyze(input.FullName);

        Console.WriteLine("Reference Analysis:");
        Console.WriteLine($"  Total records: {result.TotalRecords}");
        Console.WriteLine($"  Total references: {result.TotalReferences}");
        Console.WriteLine($"  Root records: {result.RootRecords.Count}");
        Console.WriteLine($"  Orphan records: {result.OrphanRecords.Count}");
        Console.WriteLine($"  Hierarchy depth: {result.HierarchyDepth}");

        if (result.CircularReferences.Count > 0)
        {
            Console.WriteLine($"  Circular references: {result.CircularReferences.Count}");
        }

        if (result.RecordsByStream.Count > 0)
        {
            Console.WriteLine("\nRecords by stream:");
            foreach (var (stream, count) in result.RecordsByStream.OrderByDescending(kv => kv.Value).Take(10))
            {
                Console.WriteLine($"  {stream}: {count}");
            }
        }

        if (graphIndex.HasValue)
        {
            var graph = tracker.GetObjectGraph(graphIndex.Value);
            Console.WriteLine($"\nObject graph for record {graphIndex.Value}:");
            PrintObjectGraph(graph, 0);
        }

        if (output != null)
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await File.WriteAllTextAsync(output.FullName, json);
            Console.WriteLine($"\nReference analysis saved to: {output.FullName}");
        }
    }

    private static void PrintObjectGraph(ObjectGraph graph, int depth)
    {
        var indent = new string(' ', depth * 2);
        var typeStr = graph.RecordType ?? "Unknown";
        Console.WriteLine($"{indent}[{graph.RecordIndex}] {typeStr}");

        if (graph.IsCyclic)
        {
            Console.WriteLine($"{indent}  (cyclic reference)");
            return;
        }

        foreach (var child in graph.Children.Take(5))
        {
            PrintObjectGraph(child, depth + 1);
        }
        if (graph.Children.Count > 5)
        {
            Console.WriteLine($"{indent}  ... and {graph.Children.Count - 5} more children");
        }
    }

    private static Command CreateValidateCommand()
    {
        var inputArg = new Argument<FileInfo>("input", "Input Altium library file (.PcbLib or .SchLib)");
        var outputOption = new Option<FileInfo?>(
            aliases: ["-o", "--output"],
            description: "Output validation report to JSON file");
        var lossOption = new Option<bool>(
            "--loss",
            description: "Analyze data loss during parsing");

        var command = new Command("validate", "Validate round-trip parsing completeness")
        {
            inputArg,
            outputOption,
            lossOption
        };

        command.SetHandler(async (input, output, loss) =>
        {
            await RunValidate(input, output, loss);
        }, inputArg, outputOption, lossOption);

        return command;
    }

    private static async Task RunValidate(FileInfo input, FileInfo? output, bool analyzeLoss)
    {
        if (!input.Exists)
        {
            Console.Error.WriteLine($"Error: File not found: {input.FullName}");
            return;
        }

        var validator = new RoundTripValidator();
        var ext = input.Extension.ToLowerInvariant();

        Console.WriteLine($"Validating: {input.Name}");
        Console.WriteLine();

        if (analyzeLoss)
        {
            var lossAnalysis = validator.AnalyzeLoss(input.FullName);

            Console.WriteLine("Loss Analysis:");
            Console.WriteLine($"  Total records: {lossAnalysis.TotalRecords}");
            Console.WriteLine($"  Partially parsed: {lossAnalysis.PartiallyParsedRecords.Count}");

            if (lossAnalysis.PartiallyParsedRecords.Count > 0)
            {
                Console.WriteLine("\nRecords with potential data loss:");
                foreach (var record in lossAnalysis.PartiallyParsedRecords.Take(10))
                {
                    Console.WriteLine($"  {record.Context}:");
                    Console.WriteLine($"    Raw: {record.RawSize} bytes, Parsed: {record.ParsedSize} bytes");
                    Console.WriteLine($"    Coverage: {record.CoverageRatio:P0}, Missing: {record.MissingBytes} bytes");
                }
            }

            if (output != null)
            {
                var json = JsonSerializer.Serialize(lossAnalysis, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await File.WriteAllTextAsync(output.FullName, json);
                Console.WriteLine($"\nLoss analysis saved to: {output.FullName}");
            }
        }
        else
        {
            ValidationResult? result = ext switch
            {
                ".pcblib" => validator.ValidatePcbLib(input.FullName),
                ".schlib" => validator.ValidateSchLib(input.FullName),
                _ => null
            };

            if (result == null)
            {
                Console.Error.WriteLine($"Error: Unsupported file type for validation: {ext}");
                return;
            }

            Console.WriteLine("Validation Result:");
            Console.WriteLine($"  Success: {result.Success}");
            Console.WriteLine($"  Total components: {result.TotalComponents}");
            Console.WriteLine($"  Fully matched: {result.FullyMatchedComponents}");

            if (result.Error != null)
            {
                Console.WriteLine($"  Error: {result.Error}");
            }

            if (output != null)
            {
                var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await File.WriteAllTextAsync(output.FullName, json);
                Console.WriteLine($"\nValidation result saved to: {output.FullName}");
            }
        }
    }

    private static Command CreateDecompressCommand()
    {
        var inputArg = new Argument<FileInfo>("input", "Input Altium file to scan for compression");
        var scanOption = new Option<bool>(
            "--scan",
            description: "Scan entire file for compressed sections");
        var outputOption = new Option<FileInfo?>(
            aliases: ["-o", "--output"],
            description: "Output scan results to JSON file");

        var command = new Command("decompress", "Detect and analyze compressed data in Altium files")
        {
            inputArg,
            scanOption,
            outputOption
        };

        command.SetHandler(async (input, scan, output) =>
        {
            await RunDecompress(input, scan, output);
        }, inputArg, scanOption, outputOption);

        return command;
    }

    private static async Task RunDecompress(FileInfo input, bool scan, FileInfo? output)
    {
        if (!input.Exists)
        {
            Console.Error.WriteLine($"Error: File not found: {input.FullName}");
            return;
        }

        var handler = new CompressionHandler();

        Console.WriteLine($"Scanning for compression: {input.Name}");
        Console.WriteLine();

        var scanResult = handler.ScanFile(input.FullName);

        Console.WriteLine("Compression Scan:");
        Console.WriteLine($"  Compressed sections: {scanResult.Sections.Count}");
        Console.WriteLine($"  Total compressed: {scanResult.TotalCompressedBytes:N0} bytes");
        Console.WriteLine($"  Total decompressed: {scanResult.TotalDecompressedBytes:N0} bytes");

        if (scanResult.Sections.Count > 0)
        {
            Console.WriteLine("\nCompressed sections:");
            foreach (var section in scanResult.Sections.Take(15))
            {
                var ratio = section.CompressionRatio > 0 ? $"{section.CompressionRatio:P0}" : "N/A";
                var status = section.IsValid ? "OK" : "Invalid";
                Console.WriteLine($"  {section.Path}:");
                Console.WriteLine($"    Type: {section.CompressionType}, Size: {section.CompressedSize} -> {section.DecompressedSize}");
                Console.WriteLine($"    Ratio: {ratio}, Status: {status}");
            }
            if (scanResult.Sections.Count > 15)
            {
                Console.WriteLine($"  ... and {scanResult.Sections.Count - 15} more");
            }
        }

        if (output != null)
        {
            var json = JsonSerializer.Serialize(scanResult, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await File.WriteAllTextAsync(output.FullName, json);
            Console.WriteLine($"\nScan results saved to: {output.FullName}");
        }
    }

    private static Command CreateXRefCommand()
    {
        var inputArg = new Argument<FileInfo>("input", "Input Altium file to search");
        var valueOption = new Option<string?>(
            aliases: ["-v", "--value"],
            description: "Value to search for (number, coordinate, color hex, or string)");
        var coordOption = new Option<double?>(
            "--coord",
            description: "Search for coordinate value in mils");
        var colorOption = new Option<string?>(
            "--color",
            description: "Search for color value (hex like #FF0000)");
        var outputOption = new Option<FileInfo?>(
            aliases: ["-o", "--output"],
            description: "Output search results to JSON file");

        var command = new Command("xref", "Cross-reference search for values across entire file")
        {
            inputArg,
            valueOption,
            coordOption,
            colorOption,
            outputOption
        };

        command.SetHandler(async (input, value, coord, color, output) =>
        {
            await RunXRef(input, value, coord, color, output);
        }, inputArg, valueOption, coordOption, colorOption, outputOption);

        return command;
    }

    private static async Task RunXRef(FileInfo input, string? value, double? coord, string? color, FileInfo? output)
    {
        if (!input.Exists)
        {
            Console.Error.WriteLine($"Error: File not found: {input.FullName}");
            return;
        }

        if (value == null && coord == null && color == null)
        {
            Console.Error.WriteLine("Error: Specify a search value with -v, --coord, or --color");
            return;
        }

        using var searcher = new CrossReferenceSearch();

        Console.WriteLine($"Searching in: {input.Name}");
        Console.WriteLine();

        CrossReferenceResult? result = null;

        if (coord.HasValue)
        {
            Console.WriteLine($"Searching for coordinate: {coord.Value}mil");
            result = searcher.SearchCoordinate(input.FullName, coord.Value);
        }
        else if (!string.IsNullOrEmpty(color))
        {
            // Parse color from hex (e.g., #FF0000)
            var colorStr = color.TrimStart('#');
            if (colorStr.Length == 6)
            {
                var r = Convert.ToInt32(colorStr[..2], 16);
                var g = Convert.ToInt32(colorStr[2..4], 16);
                var b = Convert.ToInt32(colorStr[4..6], 16);
                Console.WriteLine($"Searching for color: RGB({r}, {g}, {b})");
                result = searcher.SearchColor(input.FullName, r, g, b);
            }
            else
            {
                Console.Error.WriteLine("Error: Invalid color format. Use #RRGGBB format.");
                return;
            }
        }
        else if (!string.IsNullOrEmpty(value))
        {
            if (int.TryParse(value, out var intVal))
            {
                Console.WriteLine($"Searching for integer: {intVal}");
                result = searcher.SearchInt32(input.FullName, intVal);
            }
            else
            {
                Console.WriteLine($"Searching for string: \"{value}\"");
                result = searcher.SearchString(input.FullName, value);
            }
        }

        if (result != null)
        {
            Console.WriteLine($"\nFound {result.Matches.Count} matches:");

            if (result.InterpretedAs.Count > 0)
            {
                Console.WriteLine("Interpreted as:");
                foreach (var interp in result.InterpretedAs)
                {
                    Console.WriteLine($"  {interp}");
                }
                Console.WriteLine();
            }

            foreach (var match in result.Matches.Take(20))
            {
                Console.WriteLine($"  {match.StreamPath}:");
                Console.WriteLine($"    @0x{match.Offset:X4} ({match.ValueType})");
                if (match.FieldContext != null)
                {
                    Console.WriteLine($"    Context: {match.FieldContext}");
                }
            }
            if (result.Matches.Count > 20)
            {
                Console.WriteLine($"  ... and {result.Matches.Count - 20} more");
            }

            if (output != null)
            {
                var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await File.WriteAllTextAsync(output.FullName, json);
                Console.WriteLine($"\nSearch results saved to: {output.FullName}");
            }
        }
    }

    private static Command CreateLearnCommand()
    {
        var filesArg = new Argument<string[]>("files", "Altium files to learn patterns from")
        {
            Arity = ArgumentArity.OneOrMore
        };
        var outputOption = new Option<FileInfo?>(
            aliases: ["-o", "--output"],
            description: "Output learned patterns to JSON file");
        var streamOption = new Option<string?>(
            aliases: ["-s", "--stream"],
            description: "Focus learning on specific stream path");

        var command = new Command("learn", "Learn binary patterns from multiple Altium files")
        {
            filesArg,
            outputOption,
            streamOption
        };

        command.SetHandler(async (files, output, stream) =>
        {
            await RunLearn(files, output, stream);
        }, filesArg, outputOption, streamOption);

        return command;
    }

    private static async Task RunLearn(string[] files, FileInfo? output, string? streamPath)
    {
        var existingFiles = files.Where(File.Exists).ToList();

        if (existingFiles.Count == 0)
        {
            Console.Error.WriteLine("Error: No valid files found");
            return;
        }

        Console.WriteLine($"Learning patterns from {existingFiles.Count} files...");
        Console.WriteLine();

        var learner = new PatternLearner();

        foreach (var file in existingFiles)
        {
            Console.WriteLine($"  Analyzing: {Path.GetFileName(file)}");
            learner.AddFile(file);
        }

        var result = learner.Analyze();

        Console.WriteLine("\nLearned Patterns:");
        Console.WriteLine($"  Streams analyzed: {result.TotalStreamsAnalyzed}");
        Console.WriteLine($"  Total samples: {result.TotalSamples}");
        Console.WriteLine($"  Stream analyses: {result.StreamAnalyses.Count}");

        // Filter by stream path if specified
        var analyses = result.StreamAnalyses;
        if (!string.IsNullOrEmpty(streamPath))
        {
            analyses = analyses.Where(a => a.StreamPath.Contains(streamPath, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (analyses.Count > 0)
        {
            Console.WriteLine("\nAnalyzed streams:");
            foreach (var analysis in analyses.Take(10))
            {
                Console.WriteLine($"  {analysis.StreamPath}:");
                Console.WriteLine($"    Samples: {analysis.SampleCount}");
                Console.WriteLine($"    Confidence: {analysis.OverallConfidence:P0}");
                Console.WriteLine($"    Fields: {analysis.IdentifiedFields.Count}");

                foreach (var field in analysis.IdentifiedFields.Take(5))
                {
                    Console.WriteLine($"      @0x{field.Offset:X2}: {field.Type} ({field.Size} bytes) - {field.Confidence:P0}");
                    if (field.SampleValues.Count > 0)
                    {
                        Console.WriteLine($"        Samples: {string.Join(", ", field.SampleValues.Take(3))}");
                    }
                }
                if (analysis.IdentifiedFields.Count > 5)
                {
                    Console.WriteLine($"      ... and {analysis.IdentifiedFields.Count - 5} more fields");
                }
            }
        }

        if (result.CommonPatterns.Count > 0)
        {
            Console.WriteLine("\nCommon patterns across streams:");
            foreach (var pattern in result.CommonPatterns.Take(5))
            {
                Console.WriteLine($"  {pattern.Name}: {pattern.Description} ({pattern.Occurrences}x)");
            }
        }

        if (result.SuggestedTemplates.Count > 0)
        {
            Console.WriteLine("\nSuggested templates:");
            foreach (var template in result.SuggestedTemplates.Take(5))
            {
                Console.WriteLine($"  {template.Name}: {template.Fields.Count} fields (from {template.BasedOnStream})");
            }
        }

        if (output != null)
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await File.WriteAllTextAsync(output.FullName, json);
            Console.WriteLine($"\nLearned patterns saved to: {output.FullName}");
        }
    }
}
