using SkiaSharp;

namespace OriginalCircuit.Altium.Rendering.Gltf.Geometry;

/// <summary>
/// Tessellates TrueType/OpenType text into filled 2D contours so silkscreen text that uses a named
/// system font (e.g. Arial, Trebuchet MS) renders with its real glyph shapes rather than the Altium
/// stroke font. Glyph outlines come from SkiaSharp; Bézier segments are flattened and the resulting
/// contours are grouped into outer/hole sets (the bowls of letters like O, A, e) for triangulation.
/// </summary>
internal static class GltfTrueTypeText
{
    private const float Em = 256f;     // glyph extraction size; results are scaled to the text height
    private const int CurveSegments = 8;

    /// <summary>A filled glyph region: an outer contour with zero or more holes, in board millimetres.</summary>
    public readonly record struct Glyph(List<Vec2> Outer, List<List<Vec2>> Holes);

    /// <summary>
    /// Lays out one line of text into glyph fill regions (baseline at y=0, left edge at x=0, +y up),
    /// scaled so the font em equals <paramref name="heightMm"/>. <paramref name="advanceMm"/> receives
    /// the line's advance width.
    /// </summary>
    public static List<Glyph> Layout(string line, string? fontFamily, bool bold, bool italic, double heightMm, out double advanceMm)
    {
        advanceMm = 0;
        var result = new List<Glyph>();
        if (string.IsNullOrEmpty(line) || heightMm <= 0) return result;

        var typeface = SKTypeface.FromFamilyName(
            string.IsNullOrWhiteSpace(fontFamily) ? "Arial" : fontFamily,
            bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright) ?? SKTypeface.Default;

        using var font = new SKFont(typeface, Em);
        double scale = heightMm / Em;
        advanceMm = font.MeasureText(line) * scale;

        using var path = font.GetTextPath(line, new SKPoint(0, 0));
        if (path is null || path.IsEmpty) return result;

        var contours = Flatten(path, scale);
        if (contours.Count == 0) return result;
        GroupContours(contours, result);
        return result;
    }

    // Flattens an SKPath into closed contours of points (mm, baseline y=0, +y up — Skia's y-down is negated).
    private static List<List<Vec2>> Flatten(SKPath path, double scale)
    {
        var contours = new List<List<Vec2>>();
        List<Vec2>? cur = null;
        Vec2 Map(SKPoint p) => new(p.X * scale, -p.Y * scale);

        using var it = path.CreateRawIterator();
        var pts = new SKPoint[4];
        SKPathVerb verb;
        while ((verb = it.Next(pts)) != SKPathVerb.Done)
        {
            switch (verb)
            {
                case SKPathVerb.Move:
                    Finish(contours, cur);
                    cur = [Map(pts[0])];
                    break;
                case SKPathVerb.Line:
                    cur?.Add(Map(pts[1]));
                    break;
                case SKPathVerb.Quad:
                case SKPathVerb.Conic:
                    Bezier(cur, Map(pts[0]), Map(pts[1]), Map(pts[2]));
                    break;
                case SKPathVerb.Cubic:
                    Bezier(cur, Map(pts[0]), Map(pts[1]), Map(pts[2]), Map(pts[3]));
                    break;
                case SKPathVerb.Close:
                    Finish(contours, cur);
                    cur = null;
                    break;
            }
        }
        Finish(contours, cur);
        return contours;
    }

    private static void Finish(List<List<Vec2>> contours, List<Vec2>? c)
    {
        if (c is { Count: >= 3 }) contours.Add(c);
    }

    private static void Bezier(List<Vec2>? c, Vec2 p0, Vec2 p1, Vec2 p2)
    {
        if (c is null) return;
        for (int i = 1; i <= CurveSegments; i++)
        {
            double t = i / (double)CurveSegments, u = 1 - t;
            c.Add(new Vec2((u * u * p0.X) + (2 * u * t * p1.X) + (t * t * p2.X),
                           (u * u * p0.Y) + (2 * u * t * p1.Y) + (t * t * p2.Y)));
        }
    }

    private static void Bezier(List<Vec2>? c, Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3)
    {
        if (c is null) return;
        for (int i = 1; i <= CurveSegments; i++)
        {
            double t = i / (double)CurveSegments, u = 1 - t;
            double b0 = u * u * u, b1 = 3 * u * u * t, b2 = 3 * u * t * t, b3 = t * t * t;
            c.Add(new Vec2((b0 * p0.X) + (b1 * p1.X) + (b2 * p2.X) + (b3 * p3.X),
                           (b0 * p0.Y) + (b1 * p1.Y) + (b2 * p2.Y) + (b3 * p3.Y)));
        }
    }

    // Groups contours into outer/hole sets: a contour contained by an odd number of others is a hole of
    // the smallest containing (even-level) contour. Text rarely nests beyond one level (letter bowls).
    private static void GroupContours(List<List<Vec2>> contours, List<Glyph> result)
    {
        int n = contours.Count;
        var area = new double[n];
        var depth = new int[n];
        for (int i = 0; i < n; i++) area[i] = Math.Abs(SignedArea(contours[i]));

        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                if (i != j && area[j] > area[i] && Contains(contours[j], contours[i][0]))
                    depth[i]++;

        // Outer contours are at even depth; each odd-depth contour is a hole of the smallest even-depth
        // contour that contains it.
        var outers = new Dictionary<int, Glyph>();
        for (int i = 0; i < n; i++)
            if (depth[i] % 2 == 0)
                outers[i] = new Glyph(contours[i], []);

        for (int i = 0; i < n; i++)
        {
            if (depth[i] % 2 == 0) continue;
            int parent = -1;
            double best = double.MaxValue;
            for (int j = 0; j < n; j++)
                if (j != i && depth[j] % 2 == 0 && area[j] > area[i] && area[j] < best && Contains(contours[j], contours[i][0]))
                { best = area[j]; parent = j; }
            if (parent >= 0 && outers.TryGetValue(parent, out var g)) g.Holes.Add(contours[i]);
        }

        result.AddRange(outers.Values);
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
