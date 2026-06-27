using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>
/// A coarse grid index of conductor segments, used to find which wires/buses pass through a query
/// point (for T-connections, junctions and net-label binding) without scanning every segment.
/// </summary>
internal sealed class SegmentIndex
{
    // 1000-mil cells: coarse enough to keep the bucket map small, fine enough to prune effectively.
    private const long Cell = 1000 * Coord.UnitsPerMil;

    private readonly Dictionary<(long, long), List<int>> _map = new();
    private readonly List<(CoordPoint A, CoordPoint B, int Elem)> _segments = new();
    private readonly long _tolRaw;

    public SegmentIndex(long tolRaw) => _tolRaw = tolRaw;

    public void Add(CoordPoint a, CoordPoint b, int elem)
    {
        var segId = _segments.Count;
        _segments.Add((a, b, elem));

        var pad = _tolRaw;
        long minX = Math.Min(a.X.ToRaw(), b.X.ToRaw()) - pad;
        long maxX = Math.Max(a.X.ToRaw(), b.X.ToRaw()) + pad;
        long minY = Math.Min(a.Y.ToRaw(), b.Y.ToRaw()) - pad;
        long maxY = Math.Max(a.Y.ToRaw(), b.Y.ToRaw()) + pad;

        long cx0 = FloorDiv(minX, Cell), cx1 = FloorDiv(maxX, Cell);
        long cy0 = FloorDiv(minY, Cell), cy1 = FloorDiv(maxY, Cell);

        // Guard against pathological spans.
        if ((cx1 - cx0) > 4096 || (cy1 - cy0) > 4096)
        {
            // Fall back to a single sentinel bucket so the segment is still queryable everywhere.
            AddTo((long.MinValue, long.MinValue), segId);
            return;
        }

        for (var cx = cx0; cx <= cx1; cx++)
        for (var cy = cy0; cy <= cy1; cy++)
            AddTo((cx, cy), segId);
    }

    private void AddTo((long, long) key, int segId)
    {
        if (!_map.TryGetValue(key, out var list))
            _map[key] = list = new List<int>();
        list.Add(segId);
    }

    private static long FloorDiv(long a, long b)
    {
        var q = a / b;
        if ((a % b != 0) && ((a < 0) != (b < 0)))
            q--;
        return q;
    }

    /// <summary>Returns segment record ids whose bucket could contain <paramref name="p"/>.</summary>
    private IEnumerable<int> CandidateSegs(CoordPoint p)
    {
        var seen = new HashSet<int>();
        var cx = FloorDiv(p.X.ToRaw(), Cell);
        var cy = FloorDiv(p.Y.ToRaw(), Cell);
        for (var dx = -1; dx <= 1; dx++)
        for (var dy = -1; dy <= 1; dy++)
        {
            if (_map.TryGetValue((cx + dx, cy + dy), out var list))
                foreach (var s in list)
                    if (seen.Add(s))
                        yield return s;
        }
        if (_map.TryGetValue((long.MinValue, long.MinValue), out var sentinel))
            foreach (var s in sentinel)
                if (seen.Add(s))
                    yield return s;
    }

    /// <summary>
    /// Returns distinct element ids of conductors whose segment passes through <paramref name="p"/>
    /// (endpoints included). When <paramref name="interiorOnly"/> is set, only counts a hit when the
    /// point is on a segment's interior (not at its endpoints) — the T-connection test.
    /// </summary>
    public IEnumerable<int> ElementsAt(CoordPoint p, bool interiorOnly)
    {
        var seen = new HashSet<int>();
        foreach (var s in CandidateSegs(p))
        {
            var (a, b, elem) = _segments[s];
            var hit = interiorOnly
                ? ConnectivityGeometry.PointOnSegmentInterior(p, a, b, _tolRaw)
                : ConnectivityGeometry.PointOnSegment(p, a, b, _tolRaw);
            if (hit && seen.Add(elem))
                yield return elem;
        }
    }
}
