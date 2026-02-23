using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Pcb;

/// <summary>
/// Data Transfer Object for PCB pad records.
/// Represents a pad with support for through-hole and SMD configurations.
/// </summary>
[AltiumRecord("Pad")]
internal sealed partial record PcbPadDto
{
    /// <summary>
    /// Pad designator (pin number or name).
    /// </summary>
    [AltiumParameter("NAME")]
    public string? Designator { get; init; }

    /// <summary>
    /// X coordinate of the pad center.
    /// </summary>
    [AltiumParameter("X")]
    [AltiumCoord]
    public int LocationX { get; init; }

    /// <summary>
    /// Y coordinate of the pad center.
    /// </summary>
    [AltiumParameter("Y")]
    [AltiumCoord]
    public int LocationY { get; init; }

    /// <summary>
    /// X size of the pad on top layer.
    /// </summary>
    [AltiumParameter("XSIZE")]
    [AltiumCoord]
    public int SizeTopX { get; init; }

    /// <summary>
    /// Y size of the pad on top layer.
    /// </summary>
    [AltiumParameter("YSIZE")]
    [AltiumCoord]
    public int SizeTopY { get; init; }

    /// <summary>
    /// X size of the pad on middle layers.
    /// </summary>
    [AltiumParameter("XMIDSIZE")]
    [AltiumCoord]
    public int SizeMiddleX { get; init; }

    /// <summary>
    /// Y size of the pad on middle layers.
    /// </summary>
    [AltiumParameter("YMIDSIZE")]
    [AltiumCoord]
    public int SizeMiddleY { get; init; }

    /// <summary>
    /// X size of the pad on bottom layer.
    /// </summary>
    [AltiumParameter("XBOTSIZE")]
    [AltiumCoord]
    public int SizeBottomX { get; init; }

    /// <summary>
    /// Y size of the pad on bottom layer.
    /// </summary>
    [AltiumParameter("YBOTSIZE")]
    [AltiumCoord]
    public int SizeBottomY { get; init; }

    /// <summary>
    /// Hole size diameter for through-hole pads.
    /// </summary>
    [AltiumParameter("HOLESIZE")]
    [AltiumCoord]
    public int HoleSize { get; init; }

    /// <summary>
    /// Pad shape on top layer (1=Round, 2=Rectangular, 3=Octagonal, 9=RoundedRectangle).
    /// </summary>
    [AltiumParameter("SHAPE")]
    public int ShapeTop { get; init; }

    /// <summary>
    /// Pad shape on middle layers.
    /// </summary>
    [AltiumParameter("MIDSHAPE")]
    public int ShapeMiddle { get; init; }

    /// <summary>
    /// Pad shape on bottom layer.
    /// </summary>
    [AltiumParameter("BOTSHAPE")]
    public int ShapeBottom { get; init; }

    /// <summary>
    /// Hole shape (0=Round, 1=Square, 2=Slot).
    /// </summary>
    [AltiumParameter("HOLESHAPE")]
    public int HoleShape { get; init; }

    /// <summary>
    /// Layer the pad is on.
    /// </summary>
    [AltiumParameter("LAYER")]
    public int Layer { get; init; }

    /// <summary>
    /// Rotation angle in degrees.
    /// </summary>
    [AltiumParameter("ROTATION")]
    public double Rotation { get; init; }

    /// <summary>
    /// Hole rotation angle in degrees.
    /// </summary>
    [AltiumParameter("HOLEROTATION")]
    public double HoleRotation { get; init; }

    /// <summary>
    /// Whether the pad hole is plated.
    /// </summary>
    [AltiumParameter("PLATED")]
    public bool IsPlated { get; init; }

    /// <summary>
    /// Net name the pad is connected to.
    /// </summary>
    [AltiumParameter("NET")]
    public string? Net { get; init; }

    /// <summary>
    /// Jumper ID for test point identification.
    /// </summary>
    [AltiumParameter("JUMPERID")]
    public int JumperId { get; init; }

    /// <summary>
    /// X offset from hole center for top layer.
    /// </summary>
    [AltiumParameter("XOFFSET")]
    [AltiumCoord]
    public int OffsetFromHoleCenterX { get; init; }

    /// <summary>
    /// Y offset from hole center for top layer.
    /// </summary>
    [AltiumParameter("YOFFSET")]
    [AltiumCoord]
    public int OffsetFromHoleCenterY { get; init; }

    /// <summary>
    /// Slot length for slot-shaped holes.
    /// </summary>
    [AltiumParameter("HOLESLOTLENGTH")]
    [AltiumCoord]
    public int HoleSlotLength { get; init; }

    /// <summary>
    /// Corner radius percentage for rounded rectangle pads (0-100).
    /// </summary>
    [AltiumParameter("CORNERRADIUS")]
    public int CornerRadiusTop { get; init; }

    /// <summary>
    /// Corner radius percentage for middle layers.
    /// </summary>
    [AltiumParameter("MIDCORNERRADIUS")]
    public int CornerRadiusMiddle { get; init; }

    /// <summary>
    /// Corner radius percentage for bottom layer.
    /// </summary>
    [AltiumParameter("BOTCORNERRADIUS")]
    public int CornerRadiusBottom { get; init; }

    /// <summary>
    /// Stack mode (0=Simple, 1=TopMiddleBottom, 2=FullStack).
    /// </summary>
    [AltiumParameter("STACKMODE")]
    public int StackMode { get; init; }

    /// <summary>
    /// Whether paste mask expansion is manually specified.
    /// </summary>
    [AltiumParameter("PASTEMASKEXPANSIONMODE")]
    public int PasteMaskExpansionMode { get; init; }

    /// <summary>
    /// Paste mask expansion value.
    /// </summary>
    [AltiumParameter("PASTEMASKEXPANSION")]
    [AltiumCoord]
    public int PasteMaskExpansion { get; init; }

    /// <summary>
    /// Whether solder mask expansion is manually specified.
    /// </summary>
    [AltiumParameter("SOLDERMASKEXPANSIONMODE")]
    public int SolderMaskExpansionMode { get; init; }

    /// <summary>
    /// Solder mask expansion value.
    /// </summary>
    [AltiumParameter("SOLDERMASKEXPANSION")]
    [AltiumCoord]
    public int SolderMaskExpansion { get; init; }

    /// <summary>
    /// Primitive flags (tenting, keepout, etc.).
    /// </summary>
    [AltiumParameter("FLAGS")]
    public int Flags { get; init; }

    /// <summary>
    /// Unique identifier for this primitive.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }

    /// <summary>
    /// User-defined routing style for the pad.
    /// </summary>
    [AltiumParameter("USERROUTED")]
    public bool UserRouted { get; init; }
}
