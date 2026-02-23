using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Rendering;

/// <summary>
/// Converts Altium packed BGR color integers to standard ARGB format.
/// </summary>
public static class ColorHelper
{
    /// <summary>
    /// Converts an EdaColor (R, G, B, A) to ARGB uint (0xAARRGGBB).
    /// </summary>
    public static uint EdaColorToArgb(EdaColor color)
    {
        return ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
    }

    /// <summary>
    /// Returns true if the EdaColor is non-zero (not default black).
    /// </summary>
    public static bool IsNonZero(EdaColor color) => color.R != 0 || color.G != 0 || color.B != 0;

    /// <summary>
    /// Converts Altium BGR color (0x00BBGGRR) to ARGB (0xFFRRGGBB).
    /// </summary>
    public static uint BgrToArgb(int bgrColor)
    {
        var r = (uint)(bgrColor & 0xFF);
        var g = (uint)((bgrColor >> 8) & 0xFF);
        var b = (uint)((bgrColor >> 16) & 0xFF);
        return 0xFF000000 | (r << 16) | (g << 8) | b;
    }

    /// <summary>
    /// Creates a fully opaque ARGB color from individual red, green, and blue components.
    /// </summary>
    public static uint FromRgb(byte r, byte g, byte b) => 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b;

    /// <summary>Black (0xFF000000).</summary>
    public const uint Black = 0xFF000000;
    /// <summary>White (0xFFFFFFFF).</summary>
    public const uint White = 0xFFFFFFFF;
    /// <summary>Red (0xFFFF0000).</summary>
    public const uint Red = 0xFFFF0000;
    /// <summary>Green (0xFF00FF00).</summary>
    public const uint Green = 0xFF00FF00;
    /// <summary>Blue (0xFF0000FF).</summary>
    public const uint Blue = 0xFF0000FF;
    /// <summary>Yellow (0xFFFFFF00).</summary>
    public const uint Yellow = 0xFFFFFF00;
    /// <summary>Dark green (0xFF006400).</summary>
    public const uint DarkGreen = 0xFF006400;
    /// <summary>Dark blue (0xFF00008B).</summary>
    public const uint DarkBlue = 0xFF00008B;
    /// <summary>Navy (0xFF000080).</summary>
    public const uint Navy = 0xFF000080;
    /// <summary>Gray (0xFF808080).</summary>
    public const uint Gray = 0xFF808080;
}
