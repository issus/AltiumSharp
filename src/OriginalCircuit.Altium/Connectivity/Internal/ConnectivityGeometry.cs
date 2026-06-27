using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>
/// Exact integer geometry helpers for connectivity: point/segment coincidence and point-in-polygon.
/// All work is done in raw <see cref="Coord"/> units (<see cref="Coord.UnitsPerMil"/> = 10000/mil) so
/// on-grid Altium coordinates compare exactly; a non-zero tolerance switches to distance tests.
/// </summary>
internal static class ConnectivityGeometry
{
    public static long RawX(CoordPoint p) => p.X.ToRaw();
    public static long RawY(CoordPoint p) => p.Y.ToRaw();

    /// <summary>Whether two points coincide within <paramref name="tolRaw"/> raw units (0 = exact).</summary>
    public static bool PointsCoincide(CoordPoint a, CoordPoint b, long tolRaw)
    {
        var dx = RawX(a) - RawX(b);
        var dy = RawY(a) - RawY(b);
        if (tolRaw <= 0)
            return dx == 0 && dy == 0;
        return dx * dx + dy * dy <= tolRaw * tolRaw;
    }

    /// <summary>
    /// Whether point <paramref name="p"/> lies on segment <paramref name="a"/>–<paramref name="b"/>
    /// (endpoints included) within <paramref name="tolRaw"/>. With <paramref name="tolRaw"/> = 0 this is
    /// an exact integer test (cross product zero and within the segment's parameter range).
    /// </summary>
    public static bool PointOnSegment(CoordPoint p, CoordPoint a, CoordPoint b, long tolRaw)
    {
        long ax = RawX(a), ay = RawY(a), bx = RawX(b), by = RawY(b), px = RawX(p), py = RawY(p);
        long dx = bx - ax, dy = by - ay;

        if (dx == 0 && dy == 0)
            return PointsCoincide(p, a, tolRaw);

        if (tolRaw <= 0)
        {
            // Collinear: cross product is exactly zero.
            var cross = dx * (py - ay) - dy * (px - ax);
            if (cross != 0)
                return false;
            // Within the segment range: projection parameter in [0, len^2].
            var dot = (px - ax) * dx + (py - ay) * dy;
            if (dot < 0)
                return false;
            var len2 = dx * dx + dy * dy;
            return dot <= len2;
        }

        return DistanceToSegmentSquared(px, py, ax, ay, bx, by) <= (double)tolRaw * tolRaw;
    }

    /// <summary>
    /// As <see cref="PointOnSegment"/> but excludes the segment's endpoints — i.e. the point lies on the
    /// <em>interior</em> of the segment. Used to distinguish a T-connection from a shared endpoint.
    /// </summary>
    public static bool PointOnSegmentInterior(CoordPoint p, CoordPoint a, CoordPoint b, long tolRaw)
    {
        if (PointsCoincide(p, a, tolRaw) || PointsCoincide(p, b, tolRaw))
            return false;
        return PointOnSegment(p, a, b, tolRaw);
    }

    private static double DistanceToSegmentSquared(long px, long py, long ax, long ay, long bx, long by)
    {
        double dx = bx - ax, dy = by - ay;
        double len2 = dx * dx + dy * dy;
        if (len2 == 0)
        {
            double ex = px - ax, ey = py - ay;
            return ex * ex + ey * ey;
        }
        double t = ((px - ax) * dx + (py - ay) * dy) / len2;
        if (t < 0) t = 0; else if (t > 1) t = 1;
        double cx = ax + t * dx, cy = ay + t * dy;
        double rx = px - cx, ry = py - cy;
        return rx * rx + ry * ry;
    }

    /// <summary>
    /// Whether two collinear segments overlap over a non-zero length (not merely touch at a point).
    /// Returns <see langword="false"/> for non-collinear segments.
    /// </summary>
    public static bool SegmentsCollinearOverlap(CoordPoint a1, CoordPoint a2, CoordPoint b1, CoordPoint b2, long tolRaw)
    {
        long a1x = RawX(a1), a1y = RawY(a1), a2x = RawX(a2), a2y = RawY(a2);
        long b1x = RawX(b1), b1y = RawY(b1), b2x = RawX(b2), b2y = RawY(b2);
        long dx = a2x - a1x, dy = a2y - a1y;
        if (dx == 0 && dy == 0)
            return false;

        // Both endpoints of B must be collinear with A.
        var cross1 = dx * (b1y - a1y) - dy * (b1x - a1x);
        var cross2 = dx * (b2y - a1y) - dy * (b2x - a1x);
        if (tolRaw <= 0)
        {
            if (cross1 != 0 || cross2 != 0)
                return false;
        }
        else
        {
            double len = Math.Sqrt((double)dx * dx + (double)dy * dy);
            if (Math.Abs(cross1) / len > tolRaw || Math.Abs(cross2) / len > tolRaw)
                return false;
        }

        // Project onto A's direction and test interval overlap with positive length.
        double len2 = (double)dx * dx + (double)dy * dy;
        double ta1 = 0, ta2 = 1;
        double tb1 = ((b1x - a1x) * (double)dx + (b1y - a1y) * (double)dy) / len2;
        double tb2 = ((b2x - a1x) * (double)dx + (b2y - a1y) * (double)dy) / len2;
        double bLo = Math.Min(tb1, tb2), bHi = Math.Max(tb1, tb2);
        double lo = Math.Max(ta1, bLo), hi = Math.Min(ta2, bHi);
        return hi - lo > 1e-9; // overlap of more than a point
    }

    /// <summary>
    /// Whether <paramref name="p"/> is inside the polygon defined by <paramref name="vertices"/>
    /// (ray-casting, even-odd rule). Points on the boundary count as inside.
    /// </summary>
    public static bool PointInPolygon(CoordPoint p, IReadOnlyList<CoordPoint> vertices)
    {
        var n = vertices.Count;
        if (n < 3)
            return false;

        double px = RawX(p), py = RawY(p);
        var inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            double xi = RawX(vertices[i]), yi = RawY(vertices[i]);
            double xj = RawX(vertices[j]), yj = RawY(vertices[j]);

            // Boundary check.
            if (PointOnSegment(p, vertices[j], vertices[i], 0))
                return true;

            var intersect = ((yi > py) != (yj > py)) &&
                            (px < (xj - xi) * (py - yi) / (yj - yi) + xi);
            if (intersect)
                inside = !inside;
        }
        return inside;
    }
}
