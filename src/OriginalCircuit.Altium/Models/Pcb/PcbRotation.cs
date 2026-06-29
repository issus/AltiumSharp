using System;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Shared helpers for rotating PCB primitive geometry counter-clockwise about a pivot. Coord-typed
/// anchors rotate via <c>CoordPoint.RotateAround</c>; raw double vertices (region <c>OutlineExact</c>,
/// hole contours, shape-based vertices) use <see cref="RotateRaw"/> with pre-computed cos/sin.
/// </summary>
internal static class PcbRotation
{
    /// <summary>Normalizes an angle (degrees) to the half-open range [0, 360).</summary>
    public static double Normalize360(double degrees)
    {
        var a = degrees % 360.0;
        return a < 0 ? a + 360.0 : a;
    }

    /// <summary>The cosine and sine of an angle given in degrees.</summary>
    public static (double Cos, double Sin) CosSin(double degrees)
    {
        var rad = degrees * Math.PI / 180.0;
        return (Math.Cos(rad), Math.Sin(rad));
    }

    /// <summary>
    /// Rotates a raw point (<paramref name="x"/>, <paramref name="y"/>) counter-clockwise about the raw
    /// pivot (<paramref name="cx"/>, <paramref name="cy"/>) using pre-computed
    /// <paramref name="cos"/>/<paramref name="sin"/>.
    /// </summary>
    public static (double X, double Y) RotateRaw(double x, double y, double cx, double cy, double cos, double sin)
    {
        var dx = x - cx;
        var dy = y - cy;
        return (cx + dx * cos - dy * sin, cy + dx * sin + dy * cos);
    }
}
