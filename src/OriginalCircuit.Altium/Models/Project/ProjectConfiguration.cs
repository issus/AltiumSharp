namespace OriginalCircuit.Altium.Models.Project;

/// <summary>
/// A project configuration, as described by a <c>[ConfigurationN]</c> section. Configurations
/// bind a design variant and a set of output jobs together for a particular release.
/// A typed view over the underlying <see cref="ProjectSection"/>.
/// </summary>
public sealed class ProjectConfiguration
{
    /// <summary>Wraps an existing <c>[ConfigurationN]</c> section.</summary>
    public ProjectConfiguration(ProjectSection section)
    {
        Section = section ?? throw new ArgumentNullException(nameof(section));
    }

    /// <summary>The raw section backing this configuration.</summary>
    public ProjectSection Section { get; }

    /// <summary>The configuration name (e.g. <c>"Sources"</c>).</summary>
    public string? Name
    {
        get => Section.Get("Name");
        set => Section.Set("Name", value);
    }

    /// <summary>The design variant this configuration uses (e.g. <c>"[No Variations]"</c>).</summary>
    public string? Variant => Section.Get("Variant");

    /// <summary>The configuration type (e.g. <c>"Source"</c>).</summary>
    public string? ConfigurationType => Section.Get("ConfigurationType");

    /// <summary>The content-type GUID describing the configuration's purpose.</summary>
    public string? ContentTypeGuid => Section.Get("ContentTypeGUID");

    /// <inheritdoc/>
    public override string ToString() => $"Configuration: {Name}";
}
