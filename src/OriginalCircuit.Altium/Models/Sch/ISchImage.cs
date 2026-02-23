using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchImage : IPrimitive
{
    CoordPoint Corner1 { get; }
    CoordPoint Corner2 { get; }
    byte[]? ImageData { get; }
    int BorderColor { get; }
    bool ShowBorder { get; }
    int LineWidth { get; }
}
