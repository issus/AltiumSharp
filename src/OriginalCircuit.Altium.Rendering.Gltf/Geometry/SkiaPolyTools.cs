using SkiaSharp;

namespace OriginalCircuit.Altium.Rendering.Gltf.Geometry;

/// <summary>
/// Robust 2D polygon boolean operations for the renderer, backed by SkiaSharp's path algebra. Used
/// to build the solder mask as "board outline minus the union of openings" (an inverse layer): the
/// openings — non-tented pad/via expansions plus the negative geometry drawn on the solder-mask layer
/// — can overlap arbitrarily, which an even-odd hole fill or a naive ear-clip cannot handle, so the
/// union/difference is computed with <see cref="SKPath.Op"/> and the result re-grouped into
/// outer/hole contours for triangulation.
/// </summary>
internal static class SkiaPolyTools
{
    // mm are scaled up before going into Skia's float path math so sub-millimetre features keep
    // precision through the boolean ops, then scaled back on the way out.
    private const float Scale = 1000f;
    // Grid (in scaled units) that input vertices are snapped to before the boolean — 4 = 4µm. Coarse enough
    // to coalesce float drift between coincident edges, fine enough to leave real geometry unmoved.
    private const double Grid = 4.0;

    /// <summary>
    /// Subtracts the union of <paramref name="holes"/> from <paramref name="outer"/> and returns the
    /// result as outer/hole contour groups (millimetres). Returns the outer unchanged when there are
    /// no holes. With <paramref name="normalizeWinding"/> (the default), every input contour is forced to a
    /// common orientation so the holes UNION cleanly; pass false to preserve input winding when the holes
    /// intentionally encode their own holes by opposite winding (an inverted-text glyph: outer + counter).
    /// </summary>
    public static List<(List<Vec2> Outer, List<List<Vec2>> Holes)> Difference(
        IReadOnlyList<Vec2> outer, IReadOnlyList<IReadOnlyList<Vec2>> holes, bool normalizeWinding = true)
    {
        if (outer.Count < 3) return [];
        if (holes.Count == 0) return [(new List<Vec2>(outer), [])];

        using var a = FromPolygons([outer], normalizeWinding);
        using var b = FromPolygons(holes, normalizeWinding);
        using var result = a.Op(b, SKPathOp.Difference);
        return ToGroups(result);
    }

    /// <summary>
    /// Intersects <paramref name="subject"/> with <paramref name="clip"/> and returns the result as
    /// outer/hole contour groups (millimetres). Used to clip a surface feature to the board outline.
    /// </summary>
    public static List<(List<Vec2> Outer, List<List<Vec2>> Holes)> Intersect(
        IReadOnlyList<Vec2> subject, IReadOnlyList<Vec2> clip)
    {
        if (subject.Count < 3 || clip.Count < 3) return [];
        using var a = FromPolygons([subject], normalizeWinding: true);
        using var b = FromPolygons([clip], normalizeWinding: true);
        using var result = a.Op(b, SKPathOp.Intersect);
        return ToGroups(result);
    }

    private static SKPath FromPolygons(IReadOnlyList<IReadOnlyList<Vec2>> polys, bool normalizeWinding)
    {
        // Non-zero winding so overlapping contours union rather than cancel. When normalizeWinding is set,
        // every contour is forced to CCW first: the shape generators (and a CW board outline that meets CCW
        // drill circles) don't share an orientation, and under the Winding rule an opposite-wound hole that
        // sits inside another nets winding 0 — which would leave it UNSUBTRACTED (e.g. solder mask covering
        // every drill). Callers that DELIBERATELY rely on that cancellation (an inverted-text glyph: outer
        // CCW + counter CW, so the counter survives the knockout) pass normalizeWinding=false.
        var path = new SKPath { FillType = SKPathFillType.Winding };
        // Snap to a fine grid so that edges meant to coincide (e.g. the shared channel between two stacked
        // array boards, whose rout strokes are placed by independent transforms) land on exactly the same
        // coordinates. Sub-micron float drift there makes Skia emit slivers and inconsistent unions, which
        // on a panel shows up as whole boards being wrongly merged into / dropped from the cut region.
        static float Snap(double v) => (float)(Math.Round(v * Scale / Grid) * Grid);
        foreach (var poly in polys)
        {
            if (poly.Count < 3) continue;
            bool reverse = normalizeWinding && SignedArea(poly) < 0; // force CCW
            path.MoveTo(Snap(poly[0].X), Snap(poly[0].Y));
            if (!reverse)
                for (int i = 1; i < poly.Count; i++)
                    path.LineTo(Snap(poly[i].X), Snap(poly[i].Y));
            else
                for (int i = poly.Count - 1; i >= 1; i--)
                    path.LineTo(Snap(poly[i].X), Snap(poly[i].Y));
            path.Close();
        }
        return path;
    }

    // Flattens a (line-only, post-boolean) path into contours, then groups them into outer/hole sets
    // by even-odd containment depth.
    private static List<(List<Vec2> Outer, List<List<Vec2>> Holes)> ToGroups(SKPath path)
    {
        var contours = new List<List<Vec2>>();
        List<Vec2>? cur = null;
        using (var it = path.CreateRawIterator())
        {
            var pts = new SKPoint[4];
            SKPathVerb verb;
            while ((verb = it.Next(pts)) != SKPathVerb.Done)
            {
                switch (verb)
                {
                    case SKPathVerb.Move:
                        Finish(contours, cur);
                        cur = [new Vec2(pts[0].X / Scale, pts[0].Y / Scale)];
                        break;
                    case SKPathVerb.Line:
                        cur?.Add(new Vec2(pts[1].X / Scale, pts[1].Y / Scale));
                        break;
                    case SKPathVerb.Close:
                        Finish(contours, cur);
                        cur = null;
                        break;
                }
            }
        }
        Finish(contours, cur);

        var groups = new List<(List<Vec2>, List<List<Vec2>>)>();
        int n = contours.Count;
        if (n == 0) return groups;

        var area = new double[n];
        var depth = new int[n];
        // Containment is tested with a point STRICTLY INSIDE each contour, not a vertex: after a boolean
        // the result's contours can share vertices (e.g. a milled channel meeting a tab), and a vertex on
        // another contour's edge makes point-in-polygon ambiguous — which would mis-assign a hole to a far
        // outer and make the triangulator bridge across the board (a spanning triangle).
        var rep = new Vec2[n];
        for (int i = 0; i < n; i++) rep[i] = InteriorPoint(contours[i]);
        for (int i = 0; i < n; i++) area[i] = Math.Abs(SignedArea(contours[i]));
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                if (i != j && area[j] > area[i] && Contains(contours[j], rep[i]))
                    depth[i]++;

        var index = new Dictionary<int, (List<Vec2> Outer, List<List<Vec2>> Holes)>();
        for (int i = 0; i < n; i++)
            if (depth[i] % 2 == 0)
                index[i] = (contours[i], []);

        for (int i = 0; i < n; i++)
        {
            if (depth[i] % 2 == 0) continue;
            int parent = -1;
            double best = double.MaxValue;
            for (int j = 0; j < n; j++)
                if (j != i && depth[j] % 2 == 0 && area[j] > area[i] && area[j] < best && Contains(contours[j], rep[i]))
                { best = area[j]; parent = j; }
            if (parent >= 0) index[parent].Holes.Add(contours[i]);
        }

        groups.AddRange(index.Values);
        return groups;
    }

    private static void Finish(List<List<Vec2>> contours, List<Vec2>? c)
    {
        if (c is null) return;
        // The boolean can emit sub-micron sliver edges (near-duplicate / near-collinear vertices); the
        // ear-clip's exact-equality cleanup misses them, and on the complex merged panel they make it
        // fail (dropping a board's worth of triangles). Drop near-duplicate and near-collinear points.
        const double eps = 1e-4; // mm
        var clean = new List<Vec2>(c.Count);
        foreach (var p in c)
            if (clean.Count == 0 || Math.Abs(p.X - clean[^1].X) > eps || Math.Abs(p.Y - clean[^1].Y) > eps)
                clean.Add(p);
        if (clean.Count >= 2 && Math.Abs(clean[0].X - clean[^1].X) <= eps && Math.Abs(clean[0].Y - clean[^1].Y) <= eps)
            clean.RemoveAt(clean.Count - 1);

        // Remove vertices that are (near-)collinear with their neighbours — they form zero-width spikes.
        bool changed = true;
        while (changed && clean.Count > 3)
        {
            changed = false;
            for (int i = 0; i < clean.Count && clean.Count > 3; i++)
            {
                Vec2 a = clean[(i - 1 + clean.Count) % clean.Count], b = clean[i], d = clean[(i + 1) % clean.Count];
                double cross = ((b.X - a.X) * (d.Y - a.Y)) - ((b.Y - a.Y) * (d.X - a.X));
                if (Math.Abs(cross) < 1e-7) { clean.RemoveAt(i); changed = true; i--; }
            }
        }
        if (clean.Count >= 3) contours.Add(clean);
    }

    // A point strictly inside a simple polygon: step into the interior from its lowest vertex (whose
    // interior is always above it) along the edge bisector. Robust for the containment grouping.
    private static Vec2 InteriorPoint(List<Vec2> poly)
    {
        int n = poly.Count;
        int b = 0;
        for (int i = 1; i < n; i++)
            if (poly[i].Y < poly[b].Y || (poly[i].Y == poly[b].Y && poly[i].X < poly[b].X)) b = i;

        Vec2 cur = poly[b], prev = poly[((b - 1) % n + n) % n], next = poly[(b + 1) % n];
        Vec2 e1 = prev - cur, e2 = next - cur;
        double l1 = e1.Length, l2 = e2.Length;
        if (l1 < 1e-12 || l2 < 1e-12) return cur;

        Vec2 dir = (e1 * (1.0 / l1)) + (e2 * (1.0 / l2));
        double dl = dir.Length;
        dir = dl < 1e-9 ? new Vec2(-e1.Y / l1, e1.X / l1) : dir * (1.0 / dl);

        double step = Math.Min(l1, l2) * 0.05;
        var p = new Vec2(cur.X + (dir.X * step), cur.Y + (dir.Y * step));
        if (!Contains(poly, p)) p = new Vec2(cur.X - (dir.X * step), cur.Y - (dir.Y * step));
        return Contains(poly, p) ? p : cur;
    }

    private static double SignedArea(IReadOnlyList<Vec2> ring)
    {
        double a = 0;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            a += (ring[j].X * ring[i].Y) - (ring[i].X * ring[j].Y);
        return a / 2.0;
    }

    private static bool Contains(List<Vec2> poly, Vec2 p)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            if (((poly[i].Y > p.Y) != (poly[j].Y > p.Y)) &&
                (p.X < ((poly[j].X - poly[i].X) * (p.Y - poly[i].Y) / (poly[j].Y - poly[i].Y)) + poly[i].X))
                inside = !inside;
        return inside;
    }
}
