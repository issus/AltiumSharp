using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Extensions;

/// <summary>
/// Extension methods for fluent coordinate creation.
/// </summary>
public static class CoordExtensions
{
    /// <summary>
    /// Creates a coordinate from a value in millimeters.
    /// </summary>
    /// <example>
    /// var width = 1.27.Mm();
    /// </example>
    public static Coord Mm(this double value) => Coord.FromMm(value);

    /// <summary>
    /// Creates a coordinate from a value in millimeters.
    /// </summary>
    public static Coord Mm(this int value) => Coord.FromMm(value);

    /// <summary>
    /// Creates a coordinate from a value in mils (1/1000 inch).
    /// </summary>
    /// <example>
    /// var spacing = 50.Mils();
    /// </example>
    public static Coord Mils(this double value) => Coord.FromMils(value);

    /// <summary>
    /// Creates a coordinate from a value in mils (1/1000 inch).
    /// </summary>
    public static Coord Mils(this int value) => Coord.FromMils(value);

    /// <summary>
    /// Creates a coordinate from a value in inches.
    /// </summary>
    /// <example>
    /// var offset = 0.1.Inches();
    /// </example>
    public static Coord Inches(this double value) => Coord.FromInches(value);

    /// <summary>
    /// Creates a coordinate from a value in inches.
    /// </summary>
    public static Coord Inches(this int value) => Coord.FromInches(value);
}
