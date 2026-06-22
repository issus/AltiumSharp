namespace OriginalCircuit.Altium.Rendering.Gltf.Geometry;

/// <summary>
/// Generators for the 2D contours of common PCB primitives (circles, rectangles, rounded tracks,
/// arc bands), in board-space millimetres. Returned rings are simple (non-self-intersecting) so the
/// <see cref="Triangulator"/> can fill them; winding is normalised downstream where it matters.
/// </summary>
internal static class Shapes
{
    /// <summary>Number of segments to approximate a full circle of <paramref name="radiusMm"/> within a chord tolerance.</summary>
    public static int SegmentCount(double radiusMm, double chordToleranceMm, int min = 16, int max = 512)
    {
        if (radiusMm <= 0) return min;
        double t = Math.Clamp(chordToleranceMm, 1e-4, radiusMm);
        double step = 2.0 * Math.Acos(Math.Max(0.0, 1.0 - (t / radiusMm)));
        if (step <= 1e-9) return max;
        return Math.Clamp((int)Math.Ceiling(2.0 * Math.PI / step), min, max);
    }

    /// <summary>A closed circle polygon.</summary>
    public static List<Vec2> Circle(Vec2 center, double radius, int segments)
    {
        segments = Math.Max(3, segments);
        var pts = new List<Vec2>(segments);
        for (int i = 0; i < segments; i++)
        {
            double a = 2.0 * Math.PI * i / segments;
            pts.Add(new Vec2(center.X + (radius * Math.Cos(a)), center.Y + (radius * Math.Sin(a))));
        }
        return pts;
    }

    /// <summary>An axis-aligned rectangle rotated by <paramref name="rotationDeg"/> about its centre.</summary>
    public static List<Vec2> Rectangle(Vec2 center, double width, double height, double rotationDeg)
    {
        double hw = width / 2.0, hh = height / 2.0;
        double rad = rotationDeg * Math.PI / 180.0;
        double cos = Math.Cos(rad), sin = Math.Sin(rad);
        Vec2 R(double x, double y) => new(center.X + (x * cos) - (y * sin), center.Y + (x * sin) + (y * cos));
        return [R(-hw, -hh), R(hw, -hh), R(hw, hh), R(-hw, hh)];
    }

    /// <summary>An octagonal pad: a rectangle with its four corners chamfered at 45°.</summary>
    public static List<Vec2> Octagon(Vec2 center, double width, double height, double rotationDeg)
    {
        double hw = width / 2.0, hh = height / 2.0;
        double c = Math.Min(hw, hh) * 0.4142; // chamfer ≈ (√2−1)·half-extent, an octagon-ish corner cut
        var local = new (double X, double Y)[]
        {
            (-hw + c, -hh), (hw - c, -hh), (hw, -hh + c), (hw, hh - c),
            (hw - c, hh), (-hw + c, hh), (-hw, hh - c), (-hw, -hh + c),
        };
        return Place(local, center, rotationDeg);
    }

    /// <summary>A rounded rectangle whose corner radius is <paramref name="cornerPercent"/>% of the half min-side.</summary>
    public static List<Vec2> RoundedRectangle(Vec2 center, double width, double height, double rotationDeg, double cornerPercent, int segmentsPerCorner)
    {
        double hw = width / 2.0, hh = height / 2.0;
        double r = Math.Min(hw, hh) * Math.Clamp(cornerPercent, 0, 100) / 100.0;
        if (r <= 1e-6) return Rectangle(center, width, height, rotationDeg);

        int seg = Math.Max(1, segmentsPerCorner);
        var local = new List<(double X, double Y)>((seg + 1) * 4);
        // Four corner arcs, counter-clockwise from the bottom-right corner.
        AddCorner(local, hw - r, -(hh - r), -Math.PI / 2, 0, r, seg);
        AddCorner(local, hw - r, hh - r, 0, Math.PI / 2, r, seg);
        AddCorner(local, -(hw - r), hh - r, Math.PI / 2, Math.PI, r, seg);
        AddCorner(local, -(hw - r), -(hh - r), Math.PI, 3 * Math.PI / 2, r, seg);
        return Place(local, center, rotationDeg);
    }

    private static void AddCorner(List<(double X, double Y)> pts, double cx, double cy, double a0, double a1, double r, int seg)
    {
        for (int i = 0; i <= seg; i++)
        {
            double a = a0 + ((a1 - a0) * i / seg);
            pts.Add((cx + (r * Math.Cos(a)), cy + (r * Math.Sin(a))));
        }
    }

    private static List<Vec2> Place(IReadOnlyList<(double X, double Y)> local, Vec2 center, double rotationDeg)
    {
        double rad = rotationDeg * Math.PI / 180.0;
        double cos = Math.Cos(rad), sin = Math.Sin(rad);
        var pts = new List<Vec2>(local.Count);
        foreach (var (x, y) in local)
            pts.Add(new Vec2(center.X + (x * cos) - (y * sin), center.Y + (x * sin) + (y * cos)));
        return pts;
    }

    /// <summary>
    /// A rounded rectangle / stadium between two points (an Altium track or oval pad): a rectangle of
    /// the given <paramref name="width"/> capped by semicircles of radius width/2 at each end.
    /// </summary>
    public static List<Vec2> Capsule(Vec2 a, Vec2 b, double width, int capSegments)
    {
        double r = width / 2.0;
        Vec2 d = b - a;
        double len = d.Length;
        if (len < 1e-9) return Circle(a, r, Math.Max(8, capSegments * 2));

        double baseAngle = Math.Atan2(d.Y, d.X);
        int caps = Math.Max(3, capSegments);

        var pts = new List<Vec2>((caps + 1) * 2);
        AddArc(pts, b, r, baseAngle - (Math.PI / 2), baseAngle + (Math.PI / 2), caps); // forward cap
        AddArc(pts, a, r, baseAngle + (Math.PI / 2), baseAngle + (3 * Math.PI / 2), caps); // backward cap
        return pts;
    }

    /// <summary>
    /// An arc "band" of the given <paramref name="width"/> centred on a circle of radius
    /// <paramref name="radius"/>, swept counter-clockwise from <paramref name="startDeg"/> to
    /// <paramref name="endDeg"/> (an Altium PCB arc). Returns a closed annular-sector ring.
    /// </summary>
    public static List<Vec2> ArcBand(Vec2 center, double radius, double startDeg, double endDeg, double width, int segments)
    {
        double inner = Math.Max(0.0, radius - (width / 2.0));
        double outer = radius + (width / 2.0);

        double a0 = startDeg * Math.PI / 180.0;
        double a1 = endDeg * Math.PI / 180.0;
        if (a1 <= a0) a1 += 2.0 * Math.PI; // sweep CCW
        int segs = Math.Max(2, segments);

        var pts = new List<Vec2>((segs + 1) * 2);
        for (int i = 0; i <= segs; i++)
        {
            double a = a0 + ((a1 - a0) * i / segs);
            pts.Add(new Vec2(center.X + (outer * Math.Cos(a)), center.Y + (outer * Math.Sin(a))));
        }
        if (inner <= 1e-9)
        {
            pts.Add(center); // degenerate to a pie wedge
        }
        else
        {
            for (int i = segs; i >= 0; i--)
            {
                double a = a0 + ((a1 - a0) * i / segs);
                pts.Add(new Vec2(center.X + (inner * Math.Cos(a)), center.Y + (inner * Math.Sin(a))));
            }
        }
        return pts;
    }

    private static void AddArc(List<Vec2> pts, Vec2 center, double r, double fromAngle, double toAngle, int segments)
    {
        for (int i = 0; i <= segments; i++)
        {
            double a = fromAngle + ((toAngle - fromAngle) * i / segments);
            pts.Add(new Vec2(center.X + (r * Math.Cos(a)), center.Y + (r * Math.Sin(a))));
        }
    }
}
