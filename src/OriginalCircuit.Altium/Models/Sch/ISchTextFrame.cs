using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

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
    SchTextJustification Alignment { get; }
}
