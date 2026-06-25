using System.Globalization;

namespace OriginalCircuit.Altium.Models.Project;

/// <summary>How a component is varied in a particular project variant.</summary>
public enum VariationKind
{
    /// <summary>The component is fitted as in the base design.</summary>
    Fitted = 0,

    /// <summary>The component is not fitted (depopulated) in this variant.</summary>
    NotFitted = 1,

    /// <summary>The component is replaced by an alternate part in this variant.</summary>
    Alternate = 2,
}

/// <summary>
/// A single component variation within a <see cref="ProjectVariant"/> (one <c>VariationN</c>
/// entry). Parsed read-only from the variant section; the raw pipe-delimited fields are kept
/// in <see cref="Fields"/> so alternate-part metadata (the <c>AltLibLink_*</c> keys) is available.
/// </summary>
public sealed class ProjectVariation
{
    internal ProjectVariation(IReadOnlyList<KeyValuePair<string, string>> fields)
    {
        Fields = fields;
    }

    /// <summary>The component designator this variation applies to (e.g. <c>"J3"</c>).</summary>
    public string Designator => Field("Designator") ?? string.Empty;

    /// <summary>The component's unique-id path within the design (e.g. <c>"\CWVLSYAN"</c>).</summary>
    public string? UniqueId => Field("UniqueId");

    /// <summary>The raw variation kind value.</summary>
    public int KindValue => int.TryParse(Field("Kind"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

    /// <summary>The variation kind.</summary>
    public VariationKind Kind => Enum.IsDefined(typeof(VariationKind), KindValue) ? (VariationKind)KindValue : VariationKind.Fitted;

    /// <summary>For an alternate-part variation, the replacement description; empty otherwise.</summary>
    public string? AlternatePart => Field("AlternatePart");

    /// <summary>All pipe-delimited fields of the variation entry, in order.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Fields { get; }

    /// <summary>Returns the first field with the given key, or <c>null</c>.</summary>
    public string? Field(string key)
    {
        foreach (var f in Fields)
            if (string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase))
                return f.Value;
        return null;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Designator} ({Kind})";
}

/// <summary>
/// A parameter override applied to a component in a variant (a <c>ParamVariationN</c> entry
/// paired with its <c>ParamDesignatorN</c>).
/// </summary>
public sealed class ProjectParameterVariation
{
    internal ProjectParameterVariation(string designator, IReadOnlyList<KeyValuePair<string, string>> fields)
    {
        Designator = designator;
        Fields = fields;
    }

    /// <summary>The component designator the parameter override applies to.</summary>
    public string Designator { get; }

    /// <summary>The name of the overridden parameter.</summary>
    public string ParameterName => Field("ParameterName") ?? string.Empty;

    /// <summary>The parameter's value in this variant.</summary>
    public string VariantValue => Field("VariantValue") ?? string.Empty;

    /// <summary>All pipe-delimited fields of the entry, in order.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Fields { get; }

    /// <summary>Returns the first field with the given key, or <c>null</c>.</summary>
    public string? Field(string key)
    {
        foreach (var f in Fields)
            if (string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase))
                return f.Value;
        return null;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Designator}.{ParameterName}={VariantValue}";
}

/// <summary>
/// A design variant, as described by a <c>[ProjectVariantN]</c> section. A variant captures
/// how the assembled board differs from the base design (depopulated parts, alternate parts and
/// parameter overrides). A typed view over the underlying <see cref="ProjectSection"/>;
/// <see cref="Variations"/> and <see cref="ParameterVariations"/> are parsed from the section.
/// </summary>
public sealed class ProjectVariant
{
    /// <summary>Wraps an existing <c>[ProjectVariantN]</c> section.</summary>
    public ProjectVariant(ProjectSection section)
    {
        Section = section ?? throw new ArgumentNullException(nameof(section));
    }

    /// <summary>The raw section backing this variant.</summary>
    public ProjectSection Section { get; }

    /// <summary>The human-readable variant name (e.g. <c>"MALE"</c>).</summary>
    public string? Description
    {
        get => Section.Get("Description");
        set => Section.Set("Description", value);
    }

    /// <summary>The variant's persistent unique id.</summary>
    public string? UniqueId
    {
        get => Section.Get("UniqueId");
        set => Section.Set("UniqueId", value);
    }

    /// <summary>Whether this variant is allowed to be fabricated.</summary>
    public bool AllowFabrication => Section.GetBool("AllowFabrication");

    /// <summary>The component variations (depopulated/alternate parts) defined by this variant.</summary>
    public IReadOnlyList<ProjectVariation> Variations
    {
        get
        {
            var list = new List<ProjectVariation>();
            var count = Section.GetInt("VariationCount");
            for (var i = 1; i <= count; i++)
            {
                var raw = Section.Get($"Variation{i}");
                if (raw is null)
                    continue;
                list.Add(new ProjectVariation(ParseFields(raw)));
            }
            return list;
        }
    }

    /// <summary>The per-component parameter overrides defined by this variant.</summary>
    public IReadOnlyList<ProjectParameterVariation> ParameterVariations
    {
        get
        {
            var list = new List<ProjectParameterVariation>();
            var count = Section.GetInt("ParamVariationCount");
            for (var i = 1; i <= count; i++)
            {
                var raw = Section.Get($"ParamVariation{i}");
                if (raw is null)
                    continue;
                var designator = Section.Get($"ParamDesignator{i}") ?? string.Empty;
                list.Add(new ProjectParameterVariation(designator, ParseFields(raw)));
            }
            return list;
        }
    }

    // Splits a "Key=Value|Key=Value|..." entry value into ordered fields (split each segment on the
    // first '=' only, since values may themselves contain '=' — e.g. AlternatePart==Value).
    private static List<KeyValuePair<string, string>> ParseFields(string raw)
    {
        var fields = new List<KeyValuePair<string, string>>();
        foreach (var segment in raw.Split('|'))
        {
            var eq = segment.IndexOf('=');
            fields.Add(eq < 0
                ? new KeyValuePair<string, string>(segment, string.Empty)
                : new KeyValuePair<string, string>(segment[..eq], segment[(eq + 1)..]));
        }
        return fields;
    }

    /// <inheritdoc/>
    public override string ToString() => $"Variant: {Description}";
}
