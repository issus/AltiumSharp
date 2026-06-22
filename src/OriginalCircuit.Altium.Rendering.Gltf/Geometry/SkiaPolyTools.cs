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

    /// <summary>
    /// Subtracts the union of <paramref name="holes"/> from <paramref name="outer"/> and returns the
    /// result as outer/hole contour groups (millimetres). Returns the outer unchanged when there are
    /// no holes.
    /// </summary>
    public static List<(List<Vec2> Outer, List<List<Vec2>> Holes)> Difference(
        IReadOnlyList<Vec2> outer, IReadOnlyList<IReadOnlyList<Vec2>> holes)
    {
        if (outer.Count < 3) return [];
        if (holes.Count == 0) return [(new List<Vec2>(outer), [])];

        using var a = FromPolygons([outer]);
        using var b = FromPolygons(holes);
        using var result = a.Op(b, SKPathOp.Difference);
        return ToGroups(result);
    }

    private static SKPath FromPolygons(IReadOnlyList<IReadOnlyList<Vec2>> polys)
    {
        // Non-zero winding so coincident/overlapping same-orientation contours union rather than cancel.
        var path = new SKPath { FillType = SKPathFillType.Winding };
        foreach (var poly in polys)
        {
            if (poly.Count < 3) continue;
            path.MoveTo((float)(poly[0].X * Scale), (float)(poly[0].Y * Scale));
            for (int i = 1; i < poly.Count; i++)
                path.LineTo((float)(poly[i].X * Scale), (float)(poly[i].Y * Scale));
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
        for (int i = 0; i < n; i++) area[i] = Math.Abs(SignedArea(contours[i]));
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                if (i != j && area[j] > area[i] && Contains(contours[j], contours[i][0]))
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
                if (j != i && depth[j] % 2 == 0 && area[j] > area[i] && area[j] < best && Contains(contours[j], contours[i][0]))
                { best = area[j]; parent = j; }
            if (parent >= 0) index[parent].Holes.Add(contours[i]);
        }

        groups.AddRange(index.Values);
        return groups;
    }

    private static void Finish(List<List<Vec2>> contours, List<Vec2>? c)
    {
        if (c is { Count: >= 3 }) contours.Add(c);
    }

    private static double SignedArea(List<Vec2> ring)
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
