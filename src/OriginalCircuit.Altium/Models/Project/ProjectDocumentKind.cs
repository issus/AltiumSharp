namespace OriginalCircuit.Altium.Models.Project;

/// <summary>
/// The category of a document referenced by a project, derived from its file extension.
/// </summary>
public enum ProjectDocumentKind
{
    /// <summary>The extension was not recognised.</summary>
    Unknown = 0,

    /// <summary>A schematic sheet (<c>.SchDoc</c>).</summary>
    Schematic,

    /// <summary>A PCB layout (<c>.PcbDoc</c>).</summary>
    Pcb,

    /// <summary>A schematic symbol library (<c>.SchLib</c>).</summary>
    SchematicLibrary,

    /// <summary>A PCB footprint library (<c>.PcbLib</c>).</summary>
    PcbLibrary,

    /// <summary>An integrated library source or output (<c>.LibPkg</c>/<c>.IntLib</c>).</summary>
    IntegratedLibrary,

    /// <summary>A bill-of-materials document (<c>.BomDoc</c>).</summary>
    BillOfMaterials,

    /// <summary>An output job configuration (<c>.OutJob</c>).</summary>
    OutputJob,

    /// <summary>A harness definition (<c>.Harness</c>).</summary>
    Harness,

    /// <summary>A Draftsman drawing (<c>.PcbDwf</c>/<c>.SchDwf</c>).</summary>
    Draftsman,

    /// <summary>A mixed-signal simulation configuration (<c>.SimCfg</c>).</summary>
    Simulation,

    /// <summary>A recognised project document whose kind has no dedicated category above.</summary>
    Other,
}

/// <summary>Maps document file extensions to <see cref="ProjectDocumentKind"/>.</summary>
public static class ProjectDocumentKinds
{
    /// <summary>
    /// Classifies a document path or file name by its extension. The leading dot is optional
    /// and the comparison is case-insensitive.
    /// </summary>
    public static ProjectDocumentKind FromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return ProjectDocumentKind.Unknown;

        var ext = System.IO.Path.GetExtension(path);
        if (string.IsNullOrEmpty(ext))
            ext = path; // allow a bare extension to be passed in

        return ext.TrimStart('.').ToLowerInvariant() switch
        {
            "schdoc" => ProjectDocumentKind.Schematic,
            "pcbdoc" => ProjectDocumentKind.Pcb,
            "schlib" => ProjectDocumentKind.SchematicLibrary,
            "pcblib" => ProjectDocumentKind.PcbLibrary,
            "libpkg" or "intlib" => ProjectDocumentKind.IntegratedLibrary,
            "bomdoc" => ProjectDocumentKind.BillOfMaterials,
            "outjob" => ProjectDocumentKind.OutputJob,
            "harness" => ProjectDocumentKind.Harness,
            "pcbdwf" or "schdwf" or "dwf" => ProjectDocumentKind.Draftsman,
            "simcfg" => ProjectDocumentKind.Simulation,
            "" => ProjectDocumentKind.Unknown,
            _ => ProjectDocumentKind.Other,
        };
    }
}
