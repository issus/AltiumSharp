using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>Helpers for resolving the idiosyncratic schematic identity fields used by connectivity.</summary>
internal static class SchDesignators
{
    /// <summary>
    /// Resolves a placed component's reference designator. Altium stores it as a child parameter named
    /// <c>"Designator"</c> (not a first-class field); falls back to the designator prefix when absent.
    /// </summary>
    public static string? GetDesignator(SchComponent component)
    {
        foreach (var p in component.Parameters)
        {
            if (string.Equals(p.Name, "Designator", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrEmpty(p.Value) ? null : p.Value;
        }
        return null;
    }

    /// <summary>
    /// Computes a pin's absolute electrical tip from its root <see cref="SchPin.Location"/>, length and
    /// orientation. Verified empirically: the stored Location is the body root and the connecting tip is
    /// <c>Location ± Length</c> along the orientation axis.
    /// </summary>
    public static CoordPoint PinTip(SchPin pin) => pin.Orientation switch
    {
        PinOrientation.Right => new CoordPoint(pin.Location.X + pin.Length, pin.Location.Y),
        PinOrientation.Left => new CoordPoint(pin.Location.X - pin.Length, pin.Location.Y),
        PinOrientation.Up => new CoordPoint(pin.Location.X, pin.Location.Y + pin.Length),
        PinOrientation.Down => new CoordPoint(pin.Location.X, pin.Location.Y - pin.Length),
        _ => pin.Location,
    };
}
