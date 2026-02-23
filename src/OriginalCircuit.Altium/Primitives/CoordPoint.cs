using System.Runtime.CompilerServices;

namespace OriginalCircuit.Altium.Primitives;

/// <summary>
/// Represents a 2D point in internal coordinate units.
/// </summary>
public readonly struct CoordPoint : IEquatable<CoordPoint>
{
    /// <summary>
    /// The origin point (0, 0).
    /// </summary>
    public static readonly CoordPoint Zero = new(Coord.Zero, Coord.Zero);

    /// <summary>
    /// The X (horizontal) coordinate.
    /// </summary>
    public Coord X { get; }

    /// <summary>
    /// The Y (vertical) coordinate.
    /// </summary>
    public Coord Y { get; }

    /// <summary>
    /// Creates a new point from X and Y coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
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

    /// <summary>
    /// Adds two points component-wise.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CoordPoint operator +(CoordPoint a, CoordPoint b) => new(a.X + b.X, a.Y + b.Y);

    /// <summary>
    /// Subtracts one point from another component-wise.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CoordPoint operator -(CoordPoint a, CoordPoint b) => new(a.X - b.X, a.Y - b.Y);

    /// <summary>
    /// Negates both components of a point.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CoordPoint operator -(CoordPoint a) => new(-a.X, -a.Y);

    /// <summary>
    /// Multiplies both components of a point by a scalar value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CoordPoint operator *(CoordPoint a, double scalar) => new(a.X * scalar, a.Y * scalar);

    /// <summary>
    /// Multiplies a scalar value by both components of a point.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CoordPoint operator *(double scalar, CoordPoint a) => new(a.X * scalar, a.Y * scalar);

    /// <inheritdoc />
    public bool Equals(CoordPoint other) => X == other.X && Y == other.Y;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is CoordPoint other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y);

    /// <summary>
    /// Returns <see langword="true"/> if two points have equal coordinates.
    /// </summary>
    public static bool operator ==(CoordPoint left, CoordPoint right) => left.Equals(right);

    /// <summary>
    /// Returns <see langword="true"/> if two points have different coordinates.
    /// </summary>
    public static bool operator !=(CoordPoint left, CoordPoint right) => !left.Equals(right);

    /// <summary>
    /// Returns a string representation in the form "(X, Y)".
    /// </summary>
    public override string ToString() => $"({X}, {Y})";
}
