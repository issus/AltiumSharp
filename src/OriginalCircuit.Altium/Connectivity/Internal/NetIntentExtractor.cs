namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>
/// Extracts design directives (parameter sets, blankets, per-net properties) and binds them to the nets
/// they apply to as <see cref="NetIntent"/> objects.
/// </summary>
internal static class NetIntentExtractor
{
    public static void Extract(
        IReadOnlyList<SheetGraph> sheets,
        UnionFind uf,
        Dictionary<int, SchematicNet> rootToNet,
        NetlistOptions options)
    {
        DirectiveBinder.Bind(sheets, uf, rootToNet);
    }
}
