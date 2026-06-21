namespace OriginalCircuit.Altium.Rendering.Gltf.Geometry;

/// <summary>
/// A 2D point in board space, in millimetres. Board geometry is authored in the XY plane (Altium X/Y)
/// with Z carrying the layer height; a single root node transform later maps this Z-up millimetre
/// space into glTF's Y-up metre convention.
/// </summary>
public readonly record struct Vec2(double X, double Y)
{
    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 a, double s) => new(a.X * s, a.Y * s);

    /// <summary>Euclidean length.</summary>
    public double Length => Math.Sqrt((X * X) + (Y * Y));

    /// <summary>The 2D cross product (z component) of (this) × (other).</summary>
    public double Cross(Vec2 b) => (X * b.Y) - (Y * b.X);

    /// <summary>Unit vector, or (0,0) for a zero-length vector.</summary>
    public Vec2 Normalized()
    {
        double len = Length;
        return len > 1e-12 ? new Vec2(X / len, Y / len) : new Vec2(0, 0);
    }

    /// <summary>Left-hand perpendicular (rotate +90°).</summary>
    public Vec2 PerpLeft() => new(-Y, X);
}
