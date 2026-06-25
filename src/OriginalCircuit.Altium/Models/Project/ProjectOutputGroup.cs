namespace OriginalCircuit.Altium.Models.Project;

/// <summary>A single output within a <see cref="ProjectOutputGroup"/> (one indexed <c>OutputTypeN</c> set).</summary>
public sealed class ProjectOutput
{
    internal ProjectOutput(string? type, string? name, string? documentPath, string? variantName, bool isDefault, string? pageOptions)
    {
        Type = type;
        Name = name;
        DocumentPath = documentPath;
        VariantName = variantName;
        IsDefault = isDefault;
        PageOptions = pageOptions;
    }

    /// <summary>The output type identifier (e.g. <c>"Gerber"</c>, <c>"BOM_PartType"</c>).</summary>
    public string? Type { get; }

    /// <summary>The human-readable output name (e.g. <c>"Gerber Files"</c>).</summary>
    public string? Name { get; }

    /// <summary>The document the output is bound to, when applicable; usually blank.</summary>
    public string? DocumentPath { get; }

    /// <summary>The variant the output targets (e.g. <c>"[No Variations]"</c>).</summary>
    public string? VariantName { get; }

    /// <summary>Whether the output is enabled by default.</summary>
    public bool IsDefault { get; }

    /// <summary>The raw page-options string, when present.</summary>
    public string? PageOptions { get; }

    /// <inheritdoc/>
    public override string ToString() => $"{Type}: {Name}";
}

/// <summary>
/// A group of outputs, as described by an <c>[OutputGroupN]</c> section (e.g. "Fabrication
/// Outputs", "Assembly Outputs"). A typed view over the underlying <see cref="ProjectSection"/>;
/// <see cref="Outputs"/> is parsed from the section's indexed <c>OutputTypeN</c>/<c>OutputNameN</c> keys.
/// </summary>
public sealed class ProjectOutputGroup
{
    /// <summary>Wraps an existing <c>[OutputGroupN]</c> section.</summary>
    public ProjectOutputGroup(ProjectSection section)
    {
        Section = section ?? throw new ArgumentNullException(nameof(section));
    }

    /// <summary>The raw section backing this output group.</summary>
    public ProjectSection Section { get; }

    /// <summary>The group name (e.g. <c>"Fabrication Outputs"</c>).</summary>
    public string? Name
    {
        get => Section.Get("Name");
        set => Section.Set("Name", value);
    }

    /// <summary>The group description.</summary>
    public string? Description => Section.Get("Description");

    /// <summary>The individual outputs in this group, in index order.</summary>
    public IReadOnlyList<ProjectOutput> Outputs
    {
        get
        {
            var list = new List<ProjectOutput>();
            for (var i = 1; ; i++)
            {
                if (!Section.Contains($"OutputType{i}"))
                    break;
                list.Add(new ProjectOutput(
                    Section.Get($"OutputType{i}"),
                    Section.Get($"OutputName{i}"),
                    Section.Get($"OutputDocumentPath{i}"),
                    Section.Get($"OutputVariantName{i}"),
                    Section.GetBool($"OutputDefault{i}"),
                    Section.Get($"PageOptions{i}")));
            }
            return list;
        }
    }

    /// <inheritdoc/>
    public override string ToString() => $"OutputGroup: {Name} ({Outputs.Count} outputs)";
}
