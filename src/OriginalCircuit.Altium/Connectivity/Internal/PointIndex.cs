using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>
/// A grid-bucketed index of connection points keyed by snapped cell, supporting coincidence queries
/// and pairwise coincidence unioning. With <c>tolRaw == 0</c> the cell size is 1 raw unit, so points
/// share a cell only when identical (exact integer coincidence).
/// </summary>
internal sealed class PointIndex
{
    private readonly long _cell;
    private readonly long _tolRaw;
    private readonly Dictionary<(long, long), List<(CoordPoint Pt, int Elem)>> _map = new();

    public PointIndex(long tolRaw)
    {
        _tolRaw = tolRaw;
        _cell = tolRaw > 0 ? tolRaw : 1;
    }

    private (long, long) CellOf(CoordPoint p) =>
        (FloorDiv(p.X.ToRaw(), _cell), FloorDiv(p.Y.ToRaw(), _cell));

    private static long FloorDiv(long a, long b)
    {
        var q = a / b;
        if ((a % b != 0) && ((a < 0) != (b < 0)))
            q--;
        return q;
    }

    public void Add(CoordPoint p, int elem)
    {
        var key = CellOf(p);
        if (!_map.TryGetValue(key, out var list))
            _map[key] = list = new List<(CoordPoint, int)>();
        list.Add((p, elem));
    }

    /// <summary>Returns the distinct element ids whose indexed points coincide with <paramref name="p"/>.</summary>
    public IEnumerable<int> Query(CoordPoint p)
    {
        var (cx, cy) = CellOf(p);
        var seen = new HashSet<int>();
        for (var dx = -1; dx <= 1; dx++)
        for (var dy = -1; dy <= 1; dy++)
        {
            if (!_map.TryGetValue((cx + dx, cy + dy), out var list))
                continue;
            foreach (var (pt, elem) in list)
                if (ConnectivityGeometry.PointsCoincide(p, pt, _tolRaw) && seen.Add(elem))
                    yield return elem;
        }
    }

    /// <summary>Unions every pair of distinct elements whose indexed points coincide.</summary>
    public void UnionCoincident(UnionFind uf)
    {
        // Exact-coincidence fast path (the default): with cell size 1 a cell holds only identical
        // points, so unioning each cell's members directly is O(n) — the per-point neighbourhood scan
        // below is O(n^2) on a cell shared by many points (a dense net hub).
        if (_tolRaw <= 0)
        {
            foreach (var list in _map.Values)
                for (var i = 1; i < list.Count; i++)
                    uf.Union(list[0].Elem, list[i].Elem);
            return;
        }

        foreach (var list in _map.Values)
        {
            foreach (var (pt, elem) in list)
            {
                foreach (var other in Query(pt))
                {
                    if (other != elem)
                        uf.Union(elem, other);
                }
            }
        }
    }
}
