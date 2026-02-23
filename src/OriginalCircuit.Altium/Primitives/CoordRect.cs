using System.Runtime.CompilerServices;

namespace OriginalCircuit.Altium.Primitives;

/// <summary>
/// Represents an axis-aligned bounding rectangle in internal coordinate units.
/// </summary>
public readonly struct CoordRect : IEquatable<CoordRect>
{
    public static readonly CoordRect Empty = new(CoordPoint.Zero, CoordPoint.Zero);

    /// <summary>
    /// Bottom-left corner (minimum X, minimum Y).
    /// </summary>
    public CoordPoint Min { get; }

    /// <summary>
    /// Top-right corner (maximum X, maximum Y).
    /// </summary>
    public CoordPoint Max { get; }

    /// <summary>
    /// Creates a rectangle from two corner points (will be normalized).
    /// </summary>
    public CoordRect(CoordPoint p1, CoordPoint p2)
    {
        Min = new CoordPoint(Coord.Min(p1.X, p2.X), Coord.Min(p1.Y, p2.Y));
        Max = new CoordPoint(Coord.Max(p1.X, p2.X), Coord.Max(p1.Y, p2.Y));
    }

    /// <summary>
    /// Creates a rectangle from explicit coordinates.
    /// </summary>
    public CoordRect(Coord minX, Coord minY, Coord maxX, Coord maxY)
        : this(new CoordPoint(minX, minY), new CoordPoint(maxX, maxY))
    {
    }

    /// <summary>
    /// Creates a rectangle from center point and size.
    /// </summary>
    public static CoordRect FromCenterAndSize(CoordPoint center, Coord width, Coord height)
    {
        var halfWidth = width / 2;
        var halfHeight = height / 2;
        return new CoordRect(
            new CoordPoint(center.X - halfWidth, center.Y - halfHeight),
            new CoordPoint(center.X + halfWidth, center.Y + halfHeight));
    }

    /// <summary>
    /// Creates a rectangle from center point and size (alias for FromCenterAndSize).
    /// </summary>
    public static CoordRect FromCenter(CoordPoint center, Coord width, Coord height) =>
        FromCenterAndSize(center, width, height);

    /// <summary>
    /// Width of the rectangle.
    /// </summary>
    public Coord Width => Max.X - Min.X;

    /// <summary>
    /// Height of the rectangle.
    /// </summary>
    public Coord Height => Max.Y - Min.Y;

    /// <summary>
    /// Center point of the rectangle.
    /// </summary>
    public CoordPoint Center => new(
        Min.X + Width / 2,
        Min.Y + Height / 2);

    /// <summary>
    /// Returns true if this rectangle has zero area.
    /// </summary>
    public bool IsEmpty => Width.ToRaw() == 0 && Height.ToRaw() == 0;

    /// <summary>
    /// Returns true if the given point is inside this rectangle.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(CoordPoint point) =>
        point.X >= Min.X && point.X <= Max.X &&
        point.Y >= Min.Y && point.Y <= Max.Y;

    /// <summary>
    /// Returns true if this rectangle intersects with another.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(CoordRect other) =>
        Min.X <= other.Max.X && Max.X >= other.Min.X &&
        Min.Y <= other.Max.Y && Max.Y >= other.Min.Y;

    /// <summary>
    /// Returns a rectangle expanded by the specified amount on all sides.
    /// </summary>
    public CoordRect Inflate(Coord amount) => new(
        new CoordPoint(Min.X - amount, Min.Y - amount),
        new CoordPoint(Max.X + amount, Max.Y + amount));

    /// <summary>
    /// Returns the union of this rectangle with another.
    /// </summary>
    public CoordRect Union(CoordRect other)
    {
        if (IsEmpty) return other;
        if (other.IsEmpty) return this;

        return new CoordRect(
            new CoordPoint(Coord.Min(Min.X, other.Min.X), Coord.Min(Min.Y, other.Min.Y)),
            new CoordPoint(Coord.Max(Max.X, other.Max.X), Coord.Max(Max.Y, other.Max.Y)));
    }

    /// <summary>
    /// Returns the union of multiple rectangles.
    /// </summary>
    public static CoordRect Union(IEnumerable<CoordRect> rects)
    {
        var result = Empty;
        foreach (var rect in rects)
        {
            result = result.Union(rect);
        }
        return result;
    }

    /// <summary>
    /// Returns the intersection of this rectangle with another, or Empty if they don't intersect.
    /// </summary>
    public CoordRect Intersect(CoordRect other)
    {
        if (!Intersects(other)) return Empty;

        return new CoordRect(
            new CoordPoint(Coord.Max(Min.X, other.Min.X), Coord.Max(Min.Y, other.Min.Y)),
            new CoordPoint(Coord.Min(Max.X, other.Max.X), Coord.Min(Max.Y, other.Max.Y)));
    }

    // Equality
    public bool Equals(CoordRect other) => Min == other.Min && Max == other.Max;
    public override bool Equals(object? obj) => obj is CoordRect other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Min, Max);

    public static bool operator ==(CoordRect left, CoordRect right) => left.Equals(right);
    public static bool operator !=(CoordRect left, CoordRect right) => !left.Equals(right);

    public override string ToString() => $"[{Min} - {Max}]";
}
