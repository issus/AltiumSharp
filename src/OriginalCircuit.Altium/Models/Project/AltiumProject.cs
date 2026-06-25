using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Altium.Serialization.Writers;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Models.Sch;

namespace OriginalCircuit.Altium.Models.Project;

/// <summary>
/// An Altium PCB project (<c>.PrjPcb</c>): the list of documents that make up a board design
/// together with its configurations, variants, parameters and output jobs, and (when available)
/// the compiled logical sheet hierarchy from the sibling <c>.PrjPcbStructure</c> file.
/// </summary>
/// <remarks>
/// <para>
/// The project file is a flat INI-style text document. <see cref="Sections"/> is the raw,
/// ordered representation that the writer emits verbatim, which is what makes a project round-trip
/// byte-for-byte. The typed collections (<see cref="Documents"/>, <see cref="Configurations"/>,
/// <see cref="Variants"/>, …) are lightweight views computed over those sections, so reading them is
/// always consistent with <see cref="Sections"/> and editing through a view writes straight back.
/// </para>
/// <para>
/// Use <see cref="OriginalCircuit.Altium.AltiumLibrary.OpenProjectAsync(string, System.Threading.CancellationToken)"/>
/// to load a project, then the <c>Open…Async</c> helpers here to open its individual documents with
/// the existing readers.
/// </para>
/// </remarks>
public sealed class AltiumProject
{
    /// <summary>
    /// The absolute path of the <c>.PrjPcb</c> file, when the project was loaded from or saved to
    /// disk. Used to resolve relative document paths and to locate the sibling structure file.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Every section of the project file, in order. This is the source of truth for serialization;
    /// the typed collections are views over it.
    /// </summary>
    public List<ProjectSection> Sections { get; } = new();

    /// <summary>
    /// The compiled logical document hierarchy from the sibling <c>.PrjPcbStructure</c> file, or
    /// <c>null</c> when that file was absent or not loaded.
    /// </summary>
    public ProjectStructure? Structure { get; set; }

    /// <summary>Non-fatal issues encountered while reading the project.</summary>
    public IReadOnlyList<AltiumDiagnostic> Diagnostics { get; internal set; } = Array.Empty<AltiumDiagnostic>();

    /// <summary>
    /// Whether the file begins with a UTF-8 byte-order mark. Altium writes one; preserved for
    /// byte-exact round-tripping and defaulted to <c>true</c> for new projects.
    /// </summary>
    public bool HasByteOrderMark { get; set; } = true;

    /// <summary>The line terminator used when serializing. Altium uses CRLF.</summary>
    public string NewLine { get; set; } = "\r\n";

    /// <summary>The project name (the file name without extension), or <c>null</c> when <see cref="FilePath"/> is unset.</summary>
    public string? Name =>
        string.IsNullOrEmpty(FilePath) ? null : Path.GetFileNameWithoutExtension(FilePath);

    /// <summary>The directory containing the project file, or <c>null</c> when <see cref="FilePath"/> is unset.</summary>
    public string? ProjectDirectory =>
        string.IsNullOrEmpty(FilePath) ? null : Path.GetDirectoryName(Path.GetFullPath(FilePath));

    // ---- Typed section views -------------------------------------------------------------------

    /// <summary>The <c>[Design]</c> settings. The section is created on first access if absent.</summary>
    public ProjectDesignSettings Design => new(GetOrCreateSection("Design"));

    /// <summary>The source documents of the project (<c>[DocumentN]</c> sections), in file order.</summary>
    public IReadOnlyList<ProjectDocument> Documents =>
        IndexedSections("Document").Select(s => new ProjectDocument(s)).ToList();

    /// <summary>The generated/output documents tracked by the project (<c>[GeneratedDocumentN]</c> sections).</summary>
    public IReadOnlyList<ProjectGeneratedDocument> GeneratedDocuments =>
        IndexedSections("GeneratedDocument").Select(s => new ProjectGeneratedDocument(s)).ToList();

    /// <summary>The project configurations (<c>[ConfigurationN]</c> sections).</summary>
    public IReadOnlyList<ProjectConfiguration> Configurations =>
        IndexedSections("Configuration").Select(s => new ProjectConfiguration(s)).ToList();

    /// <summary>The design variants (<c>[ProjectVariantN]</c> sections).</summary>
    public IReadOnlyList<ProjectVariant> Variants =>
        IndexedSections("ProjectVariant").Select(s => new ProjectVariant(s)).ToList();

    /// <summary>The project-level parameters (<c>[ParameterN]</c> sections).</summary>
    public IReadOnlyList<ProjectParameter> Parameters =>
        IndexedSections("Parameter").Select(s => new ProjectParameter(s)).ToList();

    /// <summary>The output-job groups (<c>[OutputGroupN]</c> sections).</summary>
    public IReadOnlyList<ProjectOutputGroup> OutputGroups =>
        IndexedSections("OutputGroup").Select(s => new ProjectOutputGroup(s)).ToList();

    // ---- Document filtering --------------------------------------------------------------------

    /// <summary>The schematic source documents (<c>.SchDoc</c>) in the project.</summary>
    public IEnumerable<ProjectDocument> SchematicDocuments => DocumentsOfKind(ProjectDocumentKind.Schematic);

    /// <summary>The PCB layout documents (<c>.PcbDoc</c>) in the project.</summary>
    public IEnumerable<ProjectDocument> PcbDocuments => DocumentsOfKind(ProjectDocumentKind.Pcb);

    /// <summary>The documents of the project that match the given kind.</summary>
    public IEnumerable<ProjectDocument> DocumentsOfKind(ProjectDocumentKind kind) =>
        Documents.Where(d => d.Kind == kind);

    // ---- Section helpers -----------------------------------------------------------------------

    /// <summary>Returns the first section with the given name (case-insensitive), or <c>null</c>.</summary>
    public ProjectSection? GetSection(string name)
    {
        foreach (var section in Sections)
            if (string.Equals(section.Name, name, StringComparison.OrdinalIgnoreCase))
                return section;
        return null;
    }

    /// <summary>Returns the section with the given name, creating and appending it when absent.</summary>
    public ProjectSection GetOrCreateSection(string name)
    {
        var existing = GetSection(name);
        if (existing != null)
            return existing;
        var section = new ProjectSection(name);
        Sections.Add(section);
        return section;
    }

    // Returns sections whose name is exactly the prefix followed by one or more digits
    // (e.g. "Document1", "Document12"), in their existing file order.
    private IEnumerable<ProjectSection> IndexedSections(string prefix)
    {
        foreach (var section in Sections)
        {
            if (section.Name.Length <= prefix.Length)
                continue;
            if (!section.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;
            var suffix = section.Name.AsSpan(prefix.Length);
            var allDigits = true;
            foreach (var c in suffix)
            {
                if (!char.IsDigit(c)) { allDigits = false; break; }
            }
            if (allDigits)
                yield return section;
        }
    }

    /// <summary>
    /// Appends a new <c>[DocumentN]</c> section referencing <paramref name="documentPath"/> (relative
    /// to the project directory) with Altium's default per-document settings, and returns it.
    /// </summary>
    public ProjectDocument AddDocument(string documentPath)
    {
        if (string.IsNullOrWhiteSpace(documentPath))
            throw new ArgumentException("Document path must be provided.", nameof(documentPath));

        var index = NextIndex("Document");
        var section = new ProjectSection($"Document{index}");
        section.Add("DocumentPath", documentPath);
        section.Add("AnnotationEnabled", "1");
        section.Add("AnnotateStartValue", "1");
        section.Add("AnnotationIndexControlEnabled", "0");
        section.Add("AnnotateSuffix", "");
        section.Add("AnnotateScope", "All");
        section.Add("AnnotateOrder", "-1");
        section.Add("DoLibraryUpdate", "1");
        section.Add("DoDatabaseUpdate", "1");
        section.Add("ClassGenCCAutoEnabled", "1");
        section.Add("ClassGenCCAutoRoomEnabled", "0");
        section.Add("ClassGenNCAutoScope", "None");
        section.Add("DItemRevisionGUID", "");
        section.Add("GenerateClassCluster", "0");
        section.Add("DocumentUniqueId", "");

        // Insert after the last existing Document section (or after [Preferences]/[Design]) to keep
        // documents grouped, matching how Altium lays the file out.
        InsertSectionAfterLast(section, "Document", fallbackAfter: new[] { "Preferences", "Design" });
        return new ProjectDocument(section);
    }

    private int NextIndex(string prefix)
    {
        var max = 0;
        foreach (var section in IndexedSections(prefix))
        {
            var suffix = section.Name.Substring(prefix.Length);
            if (int.TryParse(suffix, out var n) && n > max)
                max = n;
        }
        return max + 1;
    }

    private void InsertSectionAfterLast(ProjectSection section, string prefix, string[] fallbackAfter)
    {
        var insertAt = -1;
        for (var i = 0; i < Sections.Count; i++)
            if (Sections[i].Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                insertAt = i;

        if (insertAt < 0)
        {
            foreach (var name in fallbackAfter)
            {
                for (var i = 0; i < Sections.Count; i++)
                    if (string.Equals(Sections[i].Name, name, StringComparison.OrdinalIgnoreCase))
                        insertAt = Math.Max(insertAt, i);
                if (insertAt >= 0)
                    break;
            }
        }

        if (insertAt < 0)
            Sections.Add(section);
        else
            Sections.Insert(insertAt + 1, section);
    }

    // ---- Path resolution & document loading ----------------------------------------------------

    /// <summary>
    /// Resolves a document's stored path to an absolute path using <see cref="ProjectDirectory"/>.
    /// Returns <c>null</c> when the path is unset or the project directory is unknown.
    /// </summary>
    public string? ResolveDocumentPath(ProjectDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return ResolveDocumentPath(document.DocumentPath);
    }

    /// <summary>
    /// Resolves a stored relative document path to an absolute path using <see cref="ProjectDirectory"/>.
    /// Returns <c>null</c> when the path is empty or the project directory is unknown.
    /// </summary>
    public string? ResolveDocumentPath(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return null;
        if (Path.IsPathRooted(relativePath))
            return Path.GetFullPath(relativePath);
        var dir = ProjectDirectory;
        if (string.IsNullOrEmpty(dir))
            return null;
        return Path.GetFullPath(Path.Combine(dir, relativePath));
    }

    /// <summary>Opens a schematic document of the project with <see cref="OriginalCircuit.Altium.AltiumLibrary"/>.</summary>
    /// <exception cref="InvalidOperationException">The document is not a schematic or its path cannot be resolved.</exception>
    /// <exception cref="FileNotFoundException">The resolved file does not exist.</exception>
    public ValueTask<ISchDocument> OpenSchematicAsync(ProjectDocument document, CancellationToken cancellationToken = default)
    {
        var path = RequireResolvedPath(document, ProjectDocumentKind.Schematic);
        return AltiumLibrary.OpenSchDocAsync(path, cancellationToken);
    }

    /// <summary>Opens a PCB layout document of the project with <see cref="OriginalCircuit.Altium.AltiumLibrary"/>.</summary>
    /// <exception cref="InvalidOperationException">The document is not a PCB layout or its path cannot be resolved.</exception>
    /// <exception cref="FileNotFoundException">The resolved file does not exist.</exception>
    public ValueTask<IPcbDocument> OpenPcbAsync(ProjectDocument document, CancellationToken cancellationToken = default)
    {
        var path = RequireResolvedPath(document, ProjectDocumentKind.Pcb);
        return AltiumLibrary.OpenPcbDocAsync(path, cancellationToken);
    }

    /// <summary>Opens a schematic library document of the project with <see cref="OriginalCircuit.Altium.AltiumLibrary"/>.</summary>
    public ValueTask<ISchLibrary> OpenSchematicLibraryAsync(ProjectDocument document, CancellationToken cancellationToken = default)
    {
        var path = RequireResolvedPath(document, ProjectDocumentKind.SchematicLibrary);
        return AltiumLibrary.OpenSchLibAsync(path, cancellationToken);
    }

    /// <summary>Opens a PCB footprint library document of the project with <see cref="OriginalCircuit.Altium.AltiumLibrary"/>.</summary>
    public ValueTask<IPcbLibrary> OpenPcbLibraryAsync(ProjectDocument document, CancellationToken cancellationToken = default)
    {
        var path = RequireResolvedPath(document, ProjectDocumentKind.PcbLibrary);
        return AltiumLibrary.OpenPcbLibAsync(path, cancellationToken);
    }

    private string RequireResolvedPath(ProjectDocument document, ProjectDocumentKind expectedKind)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Kind != expectedKind)
            throw new InvalidOperationException(
                $"Document '{document.DocumentPath}' is a {document.Kind}, not a {expectedKind}.");
        var path = ResolveDocumentPath(document)
            ?? throw new InvalidOperationException(
                $"Cannot resolve a path for '{document.DocumentPath}'. Set {nameof(FilePath)} on the project first.");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Project document not found: {path}", path);
        return path;
    }

    // ---- Saving --------------------------------------------------------------------------------

    /// <summary>
    /// Saves the project to <see cref="FilePath"/>. When <see cref="Structure"/> is set, the sibling
    /// <c>.PrjPcbStructure</c> file is written alongside it.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="FilePath"/> is not set.</exception>
    public ValueTask SaveAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(FilePath))
            throw new InvalidOperationException($"{nameof(FilePath)} is not set; use SaveAsync(path) instead.");
        return SaveAsync(FilePath, cancellationToken);
    }

    /// <summary>
    /// Saves the project to <paramref name="path"/> and updates <see cref="FilePath"/>. When
    /// <see cref="Structure"/> is set, the sibling <c>.PrjPcbStructure</c> file is written too.
    /// </summary>
    public async ValueTask SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        await new PrjPcbWriter().WriteAsync(this, path, cancellationToken).ConfigureAwait(false);
        FilePath = Path.GetFullPath(path);
    }
}
