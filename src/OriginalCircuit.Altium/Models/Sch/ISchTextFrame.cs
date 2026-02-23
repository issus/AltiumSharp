using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic text frame primitive that displays text within a bordered rectangular area.
/// This is an Altium-specific primitive with no shared equivalent.
/// </summary>
public interface ISchTextFrame : IPrimitive
{
    CoordPoint Corner1 { get; }
    CoordPoint Corner2 { get; }
    string Text { get; }
    int BorderColor { get; }
    int FillColor { get; }
    int TextColor { get; }
    int FontId { get; }
    bool ShowBorder { get; }
    bool IsFilled { get; }
    bool WordWrap { get; }
    bool ClipToRect { get; }
    int LineWidth { get; }
    TextJustification Alignment { get; }
}
