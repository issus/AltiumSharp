namespace OriginalCircuit.Altium.Models.Project;

/// <summary>
/// A source document that belongs to a project, as described by a <c>[DocumentN]</c>
/// section of a <c>.PrjPcb</c> file. This is a strongly-typed view over the underlying
/// <see cref="ProjectSection"/>; writing a property updates that section in place.
/// </summary>
public sealed class ProjectDocument
{
    /// <summary>Wraps an existing <c>[DocumentN]</c> section.</summary>
    public ProjectDocument(ProjectSection section)
    {
        Section = section ?? throw new ArgumentNullException(nameof(section));
    }

    /// <summary>The raw section backing this document. Edits here and via the typed properties are equivalent.</summary>
    public ProjectSection Section { get; }

    /// <summary>
    /// The document's path as stored in the project, relative to the project directory
    /// (e.g. <c>"SPI Isolator.SchDoc"</c> or <c>"sheets/Power.SchDoc"</c>).
    /// Use <see cref="AltiumProject.ResolveDocumentPath(ProjectDocument)"/> for an absolute path.
    /// </summary>
    public string? DocumentPath
    {
        get => Section.Get("DocumentPath");
        set => Section.Set("DocumentPath", value);
    }

    /// <summary>The document category derived from <see cref="DocumentPath"/>'s extension.</summary>
    public ProjectDocumentKind Kind => ProjectDocumentKinds.FromPath(DocumentPath);

    /// <summary>The file name component of <see cref="DocumentPath"/>, or <c>null</c> when the path is unset.</summary>
    public string? FileName =>
        string.IsNullOrEmpty(DocumentPath) ? null : System.IO.Path.GetFileName(DocumentPath);

    /// <summary>The persistent unique id Altium assigns to the document within the project (<c>DocumentUniqueId</c>).</summary>
    public string? DocumentUniqueId
    {
        get => Section.Get("DocumentUniqueId");
        set => Section.Set("DocumentUniqueId", value);
    }

    /// <summary>Whether designator annotation is enabled for this document.</summary>
    public bool AnnotationEnabled
    {
        get => Section.GetBool("AnnotationEnabled", true);
        set => Section.SetBool("AnnotationEnabled", value);
    }

    /// <summary>
    /// The compile/annotation order of the document. The top schematic is typically <c>0</c>;
    /// non-schematic documents are commonly <c>-1</c>.
    /// </summary>
    public int AnnotateOrder => Section.GetInt("AnnotateOrder", -1);

    /// <summary>The annotation scope (e.g. <c>"All"</c>).</summary>
    public string? AnnotateScope => Section.Get("AnnotateScope");

    /// <inheritdoc/>
    public override string ToString() => $"{Kind}: {DocumentPath}";
}
