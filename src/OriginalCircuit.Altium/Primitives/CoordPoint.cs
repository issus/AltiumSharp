using System.Runtime.CompilerServices;

namespace OriginalCircuit.Altium.Primitives;

/// <summary>
/// Represents a 2D point in internal coordinate units.
/// </summary>
public readonly struct CoordPoint : IEquatable<CoordPoint>
{
    public static readonly CoordPoint Zero = new(Coord.Zero, Coord.Zero);

    public Coord X { get; }
    public Coord Y { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CoordPoint(Coord x, Coord y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Deconstructs into X and Y components.
    /// </summary>
    public void Deconstruct(out Coord x, out Coord y)
    {
        x = X;
        y = Y;
    }

    /// <summary>
    /// Returns a new point offset by the specified amounts.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CoordPoint Offset(Coord dx, Coord dy) => new(X + dx, Y + dy);

    /// <summary>
    /// Returns the distance to another point.
    /// </summary>
    public double DistanceTo(CoordPoint other)
    {
        var dx = (X - other.X).ToRaw();
        var dy = (Y - other.Y).ToRaw();
        return Math.Sqrt(dx * (double)dx + dy * (double)dy) / Coord.UnitsPerMil;
    }

    /// <summary>
    /// Returns a new point rotated around the origin by the specified angle in degrees.
    /// </summary>
    public CoordPoint Rotate(double angleDegrees)
    {
        var radians = angleDegrees * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var x = X.ToRaw();
        var y = Y.ToRaw();
        return new CoordPoint(
            Coord.FromRaw((int)(x * cos - y * sin)),
            Coord.FromRaw((int)(x * sin + y * cos)));
    }

    /// <summary>
    /// Returns a new point rotated around a center point by the specified angle in degrees.
    /// </summary>
    public CoordPoint RotateAround(CoordPoint center, double angleDegrees)
    {
        var translated = new CoordPoint(X - center.X, Y - center.Y);
        var rotated = translated.Rotate(angleDegrees);
        return new CoordPoint(rotated.X + center.X, rotated.Y + center.Y);
    }

    // Arithmetic operators
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CoordPoint operator +(CoordPoint a, CoordPoint b) => new(a.X + b.X, a.Y + b.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CoordPoint operator -(CoordPoint a, CoordPoint b) => new(a.X - b.X, a.Y - b.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CoordPoint operator -(CoordPoint a) => new(-a.X, -a.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CoordPoint operator *(CoordPoint a, double scalar) => new(a.X * scalar, a.Y * scalar);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CoordPoint operator *(double scalar, CoordPoint a) => new(a.X * scalar, a.Y * scalar);

    // Equality
    public bool Equals(CoordPoint other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is CoordPoint other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(CoordPoint left, CoordPoint right) => left.Equals(right);
    public static bool operator !=(CoordPoint left, CoordPoint right) => !left.Equals(right);

    public override string ToString() => $"({X}, {Y})";
}
