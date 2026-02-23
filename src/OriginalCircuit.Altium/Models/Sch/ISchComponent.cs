using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic symbol/component.
/// </summary>
public interface ISchComponent : IComponent
{
    string? Comment { get; set; }
    string? DesignatorPrefix { get; set; }
    int PartCount { get; }

    IReadOnlyList<ISchPin> Pins { get; }
    IReadOnlyList<ISchLine> Lines { get; }
    IReadOnlyList<ISchRectangle> Rectangles { get; }
    IReadOnlyList<ISchLabel> Labels { get; }
    IReadOnlyList<ISchWire> Wires { get; }
    IReadOnlyList<ISchPolyline> Polylines { get; }
    IReadOnlyList<ISchPolygon> Polygons { get; }
    IReadOnlyList<ISchArc> Arcs { get; }
    IReadOnlyList<ISchBezier> Beziers { get; }
    IReadOnlyList<ISchEllipse> Ellipses { get; }
    IReadOnlyList<ISchRoundedRectangle> RoundedRectangles { get; }
    IReadOnlyList<ISchPie> Pies { get; }
    IReadOnlyList<ISchNetLabel> NetLabels { get; }
    IReadOnlyList<ISchJunction> Junctions { get; }
    IReadOnlyList<ISchParameter> Parameters { get; }
    IReadOnlyList<ISchTextFrame> TextFrames { get; }
    IReadOnlyList<ISchImage> Images { get; }
    IReadOnlyList<ISchSymbol> Symbols { get; }
    IReadOnlyList<ISchEllipticalArc> EllipticalArcs { get; }
    IReadOnlyList<ISchPowerObject> PowerObjects { get; }
    IReadOnlyList<ISchImplementation> Implementations { get; }
}
