namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>
/// Compares strings with embedded numbers naturally (so <c>"U2"</c> sorts before <c>"U10"</c>), giving
/// stable, human-friendly ordering for auto-net naming and designator sorts.
/// </summary>
internal sealed class NaturalComparer : IComparer<string>
{
    public static readonly NaturalComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int ix = 0, iy = 0;
        while (ix < x.Length && iy < y.Length)
        {
            var cx = x[ix];
            var cy = y[iy];
            if (char.IsDigit(cx) && char.IsDigit(cy))
            {
                var sx = ix;
                var sy = iy;
                while (ix < x.Length && char.IsDigit(x[ix])) ix++;
                while (iy < y.Length && char.IsDigit(y[iy])) iy++;
                var nx = x.AsSpan(sx, ix - sx).TrimStart('0');
                var ny = y.AsSpan(sy, iy - sy).TrimStart('0');
                if (nx.Length != ny.Length)
                    return nx.Length - ny.Length;
                var cmp = nx.SequenceCompareTo(ny);
                if (cmp != 0)
                    return cmp;
            }
            else
            {
                var cmp = char.ToUpperInvariant(cx).CompareTo(char.ToUpperInvariant(cy));
                if (cmp != 0)
                    return cmp;
                ix++;
                iy++;
            }
        }
        return (x.Length - ix) - (y.Length - iy);
    }
}
