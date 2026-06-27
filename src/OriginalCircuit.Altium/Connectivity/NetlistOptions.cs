using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Connectivity;

/// <summary>
/// Altium's "Net Identifier Scope" project option. It decides whether net labels are sheet-local
/// or global and how ports / sheet entries propagate connectivity across the hierarchy.
/// </summary>
/// <remarks>
/// The setting lives in the <c>[Design]</c> section of the <c>.PrjPcb</c> file. Modern projects
/// encode it with the <c>HierarchyMode</c> / <c>NameNetsHierarchically</c> flags; older projects use a
/// spread of boolean flags (<c>AllowPortNetNames</c>, <c>AllowSheetEntryNetNames</c>, …). See
/// <see cref="OriginalCircuit.Altium.Connectivity.NetIdentifierScopeReader"/>.
/// </remarks>
public enum NetIdentifierScope
{
    /// <summary>
    /// Altium picks the behaviour automatically: if the design has sheet entries / ports the scope is
    /// hierarchical; otherwise net labels and ports behave globally. This is the Altium default.
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Flat: net labels are global across every sheet; ports are global too. Sheet symbols / sheet
    /// entries are ignored for connectivity. Suits single-tier flat multi-sheet designs.
    /// </summary>
    Flat = 1,

    /// <summary>
    /// Hierarchical: net labels are local to their sheet; connectivity crosses sheet boundaries only
    /// through matching ports ↔ sheet entries. Power nets remain global by name.
    /// </summary>
    Hierarchical = 2,

    /// <summary>
    /// Global: net labels AND ports are global across the whole project, regardless of hierarchy.
    /// </summary>
    Global = 3,
}

/// <summary>
/// Tunables for the schematic connectivity solver (<see cref="SchematicNetlistBuilder"/> and
/// <see cref="ProjectNetlistBuilder"/>).
/// </summary>
public sealed class NetlistOptions
{
    /// <summary>
    /// The net-identifier scope governing cross-sheet naming and propagation. Defaults to
    /// <see cref="NetIdentifierScope.Automatic"/>. <see cref="ProjectNetlistBuilder"/> overrides this
    /// from the project's <c>[Design]</c> settings unless <see cref="ScopeIsExplicit"/> is set.
    /// </summary>
    public NetIdentifierScope Scope { get; set; } = NetIdentifierScope.Automatic;

    /// <summary>
    /// When <see langword="true"/>, <see cref="Scope"/> is honoured verbatim and the value read from the
    /// project file is ignored. Defaults to <see langword="false"/> (read from the project).
    /// </summary>
    public bool ScopeIsExplicit { get; set; }

    /// <summary>
    /// Coincidence tolerance. Two connection points are considered coincident when they are within this
    /// distance. <see cref="Coord.Zero"/> (the default) means exact integer equality, which matches
    /// real on-grid Altium files; raise it only to absorb off-grid imports.
    /// </summary>
    public Coord Tolerance { get; set; } = Coord.Zero;

    /// <summary>
    /// When <see langword="true"/> (the default), ranged bus labels (e.g. <c>D[0..7]</c>) are expanded
    /// into their member nets and bus entries are mapped to the corresponding members.
    /// </summary>
    public bool ExpandBuses { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/> (the default), harness connectors / signal harnesses are resolved
    /// so the nets feeding a harness entry are bundled under the harness.
    /// </summary>
    public bool ResolveHarnesses { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/> (the default), directives (parameter sets / blankets) and per-net
    /// parameters are bound to the nets they apply to and surfaced as <see cref="NetIntent"/> objects.
    /// </summary>
    public bool ExtractIntents { get; set; } = true;

    /// <summary>A default options instance (all defaults).</summary>
    public static NetlistOptions Default { get; } = new();
}
