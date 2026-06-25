namespace OriginalCircuit.Altium.Models.Project;

/// <summary>
/// The project's <c>[Design]</c> section: design-wide settings such as the project version,
/// hierarchy mode, default configuration and managed-project identifiers. A typed view over the
/// underlying <see cref="ProjectSection"/> exposing the commonly used keys; the full set of keys
/// remains available through <see cref="Section"/>.
/// </summary>
public sealed class ProjectDesignSettings
{
    /// <summary>Wraps an existing <c>[Design]</c> section.</summary>
    public ProjectDesignSettings(ProjectSection section)
    {
        Section = section ?? throw new ArgumentNullException(nameof(section));
    }

    /// <summary>The raw <c>[Design]</c> section. Use this to read or write keys not surfaced as typed properties.</summary>
    public ProjectSection Section { get; }

    /// <summary>The project file-format version (e.g. <c>"1.0"</c>).</summary>
    public string? Version
    {
        get => Section.Get("Version");
        set => Section.Set("Version", value);
    }

    /// <summary>The hierarchy mode flag.</summary>
    public int HierarchyMode => Section.GetInt("HierarchyMode");

    /// <summary>The name of the configuration selected by default (e.g. <c>"Sources"</c>).</summary>
    public string? DefaultConfiguration
    {
        get => Section.Get("DefaultConfiguration");
        set => Section.Set("DefaultConfiguration", value);
    }

    /// <summary>The managed-project GUID, when the project is managed by a vault/Workspace.</summary>
    public Guid? ManagedProjectGuid => Section.GetGuid("ManagedProjectGUID");

    /// <summary>The vault GUID the project is associated with, when managed.</summary>
    public Guid? VaultGuid => Section.GetGuid("VaultGUID");

    /// <summary>The release vault name, when managed.</summary>
    public string? ReleaseVaultName => Section.Get("ReleaseVaultName");

    /// <summary>The configured output path, when overridden from the default.</summary>
    public string? OutputPath
    {
        get => Section.Get("OutputPath");
        set => Section.Set("OutputPath", value);
    }

    /// <summary>The configured project-logs folder path, when overridden from the default.</summary>
    public string? LogFolderPath => Section.Get("LogFolderPath");

    /// <inheritdoc/>
    public override string ToString() => $"[Design] Version={Version}";
}
