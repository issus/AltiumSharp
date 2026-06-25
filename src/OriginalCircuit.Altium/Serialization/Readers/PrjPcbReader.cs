using System.Text;
using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Altium.Models.Project;

namespace OriginalCircuit.Altium.Serialization.Readers;

/// <summary>
/// Reads Altium project (<c>.PrjPcb</c>) files. Unlike the other Altium formats, a project file is a
/// plain UTF-8 INI-style text document (not an OLE compound file), so this reader operates on text.
/// </summary>
/// <remarks>
/// Reading from a path also loads the sibling <c>.PrjPcbStructure</c> file (the compiled logical
/// sheet hierarchy) when it exists. This reader is not thread-safe; create one instance per use.
/// </remarks>
public sealed class PrjPcbReader
{
    private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };

    private List<AltiumDiagnostic> _diagnostics = new();

    /// <summary>
    /// Reads a project from the given <c>.PrjPcb</c> path, also loading the sibling
    /// <c>.PrjPcbStructure</c> file when present.
    /// </summary>
    public async ValueTask<AltiumProject> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var project = ReadBytes(bytes);
        project.FilePath = Path.GetFullPath(path);

        var structurePath = Path.ChangeExtension(path, ".PrjPcbStructure");
        if (File.Exists(structurePath))
        {
            try
            {
                var structureBytes = await File.ReadAllBytesAsync(structurePath, cancellationToken).ConfigureAwait(false);
                project.Structure = ProjectStructure.Parse(DecodeText(structureBytes, out _));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning,
                    $"Failed to read project structure file: {ex.Message}", Path.GetFileName(structurePath)));
                project.Diagnostics = _diagnostics;
            }
        }

        return project;
    }

    /// <summary>Reads a project from a stream containing <c>.PrjPcb</c> text. The structure file is not loaded.</summary>
    public AltiumProject Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ReadBytes(ms.ToArray());
    }

    /// <summary>Parses already-decoded <c>.PrjPcb</c> text. A leading UTF-8/UTF-16 BOM character is honoured.</summary>
    public AltiumProject Read(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var hasBom = text.Length > 0 && text[0] == '﻿';
        if (hasBom)
            text = text[1..];
        var project = ParseText(text);
        project.HasByteOrderMark = hasBom;
        return project;
    }

    private AltiumProject ReadBytes(byte[] bytes)
    {
        var text = DecodeText(bytes, out var hasBom);
        var project = ParseText(text);
        project.HasByteOrderMark = hasBom;
        return project;
    }

    private static string DecodeText(byte[] bytes, out bool hasBom)
    {
        hasBom = bytes.Length >= 3 && bytes[0] == Utf8Bom[0] && bytes[1] == Utf8Bom[1] && bytes[2] == Utf8Bom[2];
        var start = hasBom ? 3 : 0;
        return Encoding.UTF8.GetString(bytes, start, bytes.Length - start);
    }

    private AltiumProject ParseText(string text)
    {
        _diagnostics = new List<AltiumDiagnostic>();
        var project = new AltiumProject
        {
            // Altium uses CRLF; honour whatever the file actually used so it round-trips.
            NewLine = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n",
        };

        ProjectSection? current = null;
        foreach (var rawLine in text.Split('\n'))
        {
            // Lines were split on '\n'; drop the matching '\r' so both CRLF and LF parse identically.
            var line = rawLine.Length > 0 && rawLine[^1] == '\r' ? rawLine[..^1] : rawLine;

            if (line.Length == 0)
                continue; // blank line — the separator emitted after each section; not data

            if (line[0] == '[' && line[^1] == ']')
            {
                current = new ProjectSection(line[1..^1]);
                project.Sections.Add(current);
                continue;
            }

            if (current == null)
            {
                // Content before the first section header is unexpected; keep it so nothing is lost.
                current = new ProjectSection(string.Empty);
                project.Sections.Add(current);
                _diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning,
                    "Project file has content before the first section header."));
            }

            var eq = line.IndexOf('=');
            if (eq < 0)
                current.Entries.Add(new KeyValuePair<string, string>(line, string.Empty));
            else
                current.Entries.Add(new KeyValuePair<string, string>(line[..eq], line[(eq + 1)..]));
        }

        project.Diagnostics = _diagnostics;
        return project;
    }
}
