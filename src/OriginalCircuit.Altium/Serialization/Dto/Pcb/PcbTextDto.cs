using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Pcb;

/// <summary>
/// Data Transfer Object for PCB text records.
/// Represents text or string objects placed on the PCB.
/// </summary>
[AltiumRecord("Text")]
internal sealed partial record PcbTextDto
{
    /// <summary>
    /// X coordinate of the text anchor point.
    /// </summary>
    [AltiumParameter("X")]
    [AltiumCoord]
    public int LocationX { get; init; }

    /// <summary>
    /// Y coordinate of the text anchor point.
    /// </summary>
    [AltiumParameter("Y")]
    [AltiumCoord]
    public int LocationY { get; init; }

    /// <summary>
    /// The text content to display.
    /// </summary>
    [AltiumParameter("TEXT")]
    public string? Text { get; init; }

    /// <summary>
    /// Height of the text characters.
    /// </summary>
    [AltiumParameter("HEIGHT")]
    [AltiumCoord]
    public int Height { get; init; }

    /// <summary>
    /// Width of the text bounding box.
    /// </summary>
    [AltiumParameter("WIDTH")]
    [AltiumCoord]
    public int Width { get; init; }

    /// <summary>
    /// Rotation angle in degrees.
    /// </summary>
    [AltiumParameter("ROTATION")]
    public double Rotation { get; init; }

    /// <summary>
    /// Layer the text is on.
    /// </summary>
    [AltiumParameter("LAYER")]
    public int Layer { get; init; }

    /// <summary>
    /// Whether the text is mirrored.
    /// </summary>
    [AltiumParameter("MIRRORED")]
    public bool IsMirrored { get; init; }

    /// <summary>
    /// Text kind (0=Stroke, 1=TrueType, 2=BarCode).
    /// </summary>
    [AltiumParameter("TEXTKIND")]
    public int TextKind { get; init; }

    /// <summary>
    /// Stroke font type (0=Default, 1=SansSerif, 3=Serif).
    /// </summary>
    [AltiumParameter("STROKEFONT")]
    public int StrokeFont { get; init; }

    /// <summary>
    /// Width of stroke lines for stroke font.
    /// </summary>
    [AltiumParameter("STROKEWIDTH")]
    [AltiumCoord]
    public int StrokeWidth { get; init; }

    /// <summary>
    /// TrueType font name.
    /// </summary>
    [AltiumParameter("FONTNAME")]
    public string? FontName { get; init; }

    /// <summary>
    /// Whether the font is bold.
    /// </summary>
    [AltiumParameter("BOLD")]
    public bool IsBold { get; init; }

    /// <summary>
    /// Whether the font is italic.
    /// </summary>
    [AltiumParameter("ITALIC")]
    public bool IsItalic { get; init; }

    /// <summary>
    /// Whether the text is inverted (text is knockout).
    /// </summary>
    [AltiumParameter("INVERTED")]
    public bool IsInverted { get; init; }

    /// <summary>
    /// Border width for inverted text.
    /// </summary>
    [AltiumParameter("INVERTEDBORDER")]
    [AltiumCoord]
    public int InvertedBorder { get; init; }

    /// <summary>
    /// Whether inverted text uses a rectangle background.
    /// </summary>
    [AltiumParameter("INVERTEDRECT")]
    public bool InvertedRect { get; init; }

    /// <summary>
    /// Width of inverted rectangle.
    /// </summary>
    [AltiumParameter("INVERTEDRECTWIDTH")]
    [AltiumCoord]
    public int InvertedRectWidth { get; init; }

    /// <summary>
    /// Height of inverted rectangle.
    /// </summary>
    [AltiumParameter("INVERTEDRECTHEIGHT")]
    [AltiumCoord]
    public int InvertedRectHeight { get; init; }

    /// <summary>
    /// Justification of inverted rect text (1-9, where 5 is center).
    /// </summary>
    [AltiumParameter("INVERTEDRECTJUSTIFICATION")]
    public int InvertedRectJustification { get; init; }

    /// <summary>
    /// Text offset within inverted rectangle.
    /// </summary>
    [AltiumParameter("INVERTEDRECTTEXTOFFSET")]
    [AltiumCoord]
    public int InvertedRectTextOffset { get; init; }

    /// <summary>
    /// Left margin for barcode text.
    /// </summary>
    [AltiumParameter("BARCODELRMARGIN")]
    [AltiumCoord]
    public int BarcodeLRMargin { get; init; }

    /// <summary>
    /// Top/bottom margin for barcode text.
    /// </summary>
    [AltiumParameter("BARCODETBMARGIN")]
    [AltiumCoord]
    public int BarcodeTBMargin { get; init; }

    /// <summary>
    /// Component index if this text belongs to a component.
    /// </summary>
    [AltiumParameter("COMPONENT")]
    public int ComponentIndex { get; init; }

    /// <summary>
    /// Primitive flags.
    /// </summary>
    [AltiumParameter("FLAGS")]
    public int Flags { get; init; }

    /// <summary>
    /// Unique identifier for this primitive.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }

    /// <summary>
    /// Whether text is hidden.
    /// </summary>
    [AltiumParameter("HIDDEN")]
    public bool IsHidden { get; init; }

    /// <summary>
    /// Designator type (Comment, Designator, etc.).
    /// </summary>
    [AltiumParameter("DESIGNATORTYPE")]
    public int DesignatorType { get; init; }
}
