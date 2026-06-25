namespace OriginalCircuit.Altium.Models.Project;

/// <summary>
/// A project-level parameter, as described by a <c>[ParameterN]</c> section
/// (a name/value pair available to all documents in the project, e.g. <c>Revision</c>).
/// A typed view over the underlying <see cref="ProjectSection"/>.
/// </summary>
public sealed class ProjectParameter
{
    /// <summary>Wraps an existing <c>[ParameterN]</c> section.</summary>
    public ProjectParameter(ProjectSection section)
    {
        Section = section ?? throw new ArgumentNullException(nameof(section));
    }

    /// <summary>The raw section backing this parameter.</summary>
    public ProjectSection Section { get; }

    /// <summary>The parameter name.</summary>
    public string? Name
    {
        get => Section.Get("Name");
        set => Section.Set("Name", value);
    }

    /// <summary>The parameter value.</summary>
    public string? Value
    {
        get => Section.Get("Value");
        set => Section.Set("Value", value);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Name}={Value}";
}
