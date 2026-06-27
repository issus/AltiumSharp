using System.Globalization;
using OriginalCircuit.Altium.Models.Project;

namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>
/// Reads Altium's net-identifier-scope setting from a project's <c>[Design]</c> section. Modern projects
/// encode it with the <c>NameNetsHierarchically</c> / <c>HierarchyMode</c> flags; some carry a single
/// <c>NetIdentifierScope</c> integer. Defaults to <see cref="NetIdentifierScope.Automatic"/>.
/// </summary>
internal static class NetIdentifierScopeReader
{
    public static NetIdentifierScope Read(AltiumProject project)
    {
        var design = project.GetSection("Design");
        if (design is null)
            return NetIdentifierScope.Automatic;

        // Single explicit integer key, when present.
        var raw = design.Get("NetIdentifierScope");
        if (!string.IsNullOrEmpty(raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
        {
            return n switch
            {
                1 => NetIdentifierScope.Flat,
                2 or 3 => NetIdentifierScope.Hierarchical,
                4 => NetIdentifierScope.Global,
                _ => NetIdentifierScope.Automatic,
            };
        }

        // Flag form: NameNetsHierarchically drives whether net labels are sheet-local.
        var nameHier = design.Get("NameNetsHierarchically");
        if (nameHier == "1")
            return NetIdentifierScope.Hierarchical;
        if (nameHier == "0")
            return NetIdentifierScope.Flat;

        return NetIdentifierScope.Automatic;
    }

    /// <summary>
    /// Resolves <see cref="NetIdentifierScope.Automatic"/> to a concrete scope: hierarchical when the
    /// design uses sheet symbols, otherwise flat.
    /// </summary>
    public static NetIdentifierScope Resolve(NetIdentifierScope scope, bool hasSheetSymbols) =>
        scope != NetIdentifierScope.Automatic
            ? scope
            : hasSheetSymbols ? NetIdentifierScope.Hierarchical : NetIdentifierScope.Flat;
}
