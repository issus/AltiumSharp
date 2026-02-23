using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium;

/// <summary>
/// Converts between Altium packed BGR color integers and <see cref="EdaColor"/> values.
/// Altium stores colors as 0x00BBGGRR (blue in high byte, red in low byte).
/// </summary>
internal static class AltiumColorHelper
{
    /// <summary>
    /// Converts an Altium BGR color integer to an <see cref="EdaColor"/>.
    /// </summary>
    public static EdaColor BgrToEdaColor(int bgrColor)
    {
        var r = (byte)(bgrColor & 0xFF);
        var g = (byte)((bgrColor >> 8) & 0xFF);
        var b = (byte)((bgrColor >> 16) & 0xFF);
        return EdaColor.FromRgb(r, g, b);
    }

    /// <summary>
    /// Converts an <see cref="EdaColor"/> to an Altium BGR color integer.
    /// </summary>
    public static int EdaColorToBgr(EdaColor color) =>
        color.R | (color.G << 8) | (color.B << 16);
}
