namespace OriginalCircuit.Altium.Models.Project;

/// <summary>
/// A generated/output document tracked by the project, as described by a
/// <c>[GeneratedDocumentN]</c> section (e.g. a netlist or simulation data file produced
/// during compilation). A typed view over the underlying <see cref="ProjectSection"/>.
/// </summary>
public sealed class ProjectGeneratedDocument
{
    /// <summary>Wraps an existing <c>[GeneratedDocumentN]</c> section.</summary>
    public ProjectGeneratedDocument(ProjectSection section)
    {
        Section = section ?? throw new ArgumentNullException(nameof(section));
    }

    /// <summary>The raw section backing this generated document.</summary>
    public ProjectSection Section { get; }

    /// <summary>The generated document's path, relative to the project directory.</summary>
    public string? DocumentPath
    {
        get => Section.Get("DocumentPath");
        set => Section.Set("DocumentPath", value);
    }

    /// <summary>The document category derived from <see cref="DocumentPath"/>'s extension.</summary>
    public ProjectDocumentKind Kind => ProjectDocumentKinds.FromPath(DocumentPath);

    /// <inheritdoc/>
    public override string ToString() => $"Generated: {DocumentPath}";
}
