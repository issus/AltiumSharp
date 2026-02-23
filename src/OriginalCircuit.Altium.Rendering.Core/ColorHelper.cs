namespace OriginalCircuit.Altium.Rendering;

/// <summary>
/// Converts Altium packed BGR color integers to standard ARGB format.
/// </summary>
public static class ColorHelper
{
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

    public static uint FromRgb(byte r, byte g, byte b) => 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b;

    // Common colors
    public const uint Black = 0xFF000000;
    public const uint White = 0xFFFFFFFF;
    public const uint Red = 0xFFFF0000;
    public const uint Green = 0xFF00FF00;
    public const uint Blue = 0xFF0000FF;
    public const uint Yellow = 0xFFFFFF00;
    public const uint DarkGreen = 0xFF006400;
    public const uint DarkBlue = 0xFF00008B;
    public const uint Navy = 0xFF000080;
    public const uint Gray = 0xFF808080;
}
