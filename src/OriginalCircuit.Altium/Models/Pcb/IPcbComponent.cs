using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a PCB footprint/component.
/// </summary>
public interface IPcbComponent : IComponent
{
    Coord Height { get; set; }

    IReadOnlyList<IPcbPad> Pads { get; }
    IReadOnlyList<IPcbTrack> Tracks { get; }
    IReadOnlyList<IPcbVia> Vias { get; }
    IReadOnlyList<IPcbArc> Arcs { get; }
    IReadOnlyList<IPcbText> Texts { get; }
    IReadOnlyList<IPcbFill> Fills { get; }
    IReadOnlyList<IPcbRegion> Regions { get; }
    IReadOnlyList<IPcbComponentBody> ComponentBodies { get; }
}

/// <summary>
/// Represents a PCB pad primitive used for component soldering and through-hole or surface-mount connections.
/// </summary>
public interface IPcbPad : IPrimitive
{
    string? Designator { get; set; }
    CoordPoint Location { get; set; }
    double Rotation { get; }
    PadShape ShapeTop { get; }
    PadShape ShapeBottom { get; }
    CoordPoint SizeTop { get; }
    CoordPoint SizeBottom { get; }
    Coord HoleSize { get; }
    PadHoleType HoleType { get; }
    double HoleRotation { get; }
    int CornerRadiusPercentage { get; }
    Coord SolderMaskExpansion { get; }
    bool IsPlated { get; }
    int Layer { get; }
}

/// <summary>
/// Represents a PCB track (trace) primitive that forms a copper connection between two points.
/// </summary>
public interface IPcbTrack : IPrimitive
{
    CoordPoint Start { get; set; }
    CoordPoint End { get; set; }
    Coord Width { get; set; }
    int Layer { get; }
}

/// <summary>
/// Represents a PCB via primitive that provides a copper connection between layers.
/// </summary>
public interface IPcbVia : IPrimitive
{
    CoordPoint Location { get; set; }
    Coord Diameter { get; set; }
    Coord HoleSize { get; set; }
    int StartLayer { get; }
    int EndLayer { get; }
    int Layer { get; }
}

/// <summary>
/// Represents a PCB arc primitive defined by a center point, radius, and angular range.
/// </summary>
public interface IPcbArc : IPrimitive
{
    CoordPoint Center { get; set; }
    Coord Radius { get; set; }
    double StartAngle { get; set; }
    double EndAngle { get; set; }
    Coord Width { get; }
    int Layer { get; }
}

/// <summary>
/// Represents a PCB text string primitive placed on a copper or silkscreen layer.
/// </summary>
public interface IPcbText : IPrimitive
{
    string Text { get; set; }
    CoordPoint Location { get; set; }
    Coord Height { get; }
    Coord StrokeWidth { get; }
    double Rotation { get; }
    int Layer { get; }
    bool IsMirrored { get; }
    string? FontName { get; }
    PcbTextKind TextKind { get; }
    bool FontBold { get; }
    bool FontItalic { get; }
}

/// <summary>
/// Represents a PCB fill primitive that defines a solid rectangular copper region.
/// </summary>
public interface IPcbFill : IPrimitive
{
    CoordPoint Corner1 { get; }
    CoordPoint Corner2 { get; }
    int Layer { get; }
    double Rotation { get; }
}

/// <summary>
/// Represents a PCB region primitive defined by an arbitrary closed polygon outline.
/// </summary>
public interface IPcbRegion : IPrimitive
{
    IReadOnlyList<CoordPoint> Outline { get; }
    int Layer { get; }
}

/// <summary>
/// Represents a PCB component body primitive that defines the physical 3D outline of a component.
/// </summary>
public interface IPcbComponentBody : IPrimitive
{
    IReadOnlyList<CoordPoint> Outline { get; }
    int Layer { get; }
}
