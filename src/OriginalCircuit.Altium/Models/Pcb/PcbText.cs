using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Text justification options.
/// </summary>
public enum TextJustification
{
    BottomLeft = 0,
    BottomCenter = 1,
    BottomRight = 2,
    MiddleLeft = 3,
    MiddleCenter = 4,
    MiddleRight = 5,
    TopLeft = 6,
    TopCenter = 7,
    TopRight = 8
}

/// <summary>
/// Text kind (stroke, TrueType, or barcode).
/// </summary>
public enum PcbTextKind
{
    Stroke = 0,
    TrueType = 1,
    BarCode = 2
}

/// <summary>
/// Stroke font type.
/// </summary>
public enum PcbStrokeFont
{
    Default = 0,
    SansSerif = 1,
    Serif = 3
}

/// <summary>
/// Represents PCB text.
/// </summary>
public sealed class PcbText : IPcbText
{
    /// <inheritdoc />
    public string Text { get; set; } = string.Empty;

    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <summary>
    /// Height of the text.
    /// </summary>
    public Coord Height { get; set; }

    /// <summary>
    /// Width of the text stroke.
    /// </summary>
    public Coord StrokeWidth { get; set; }

    /// <summary>
    /// Rotation angle in degrees.
    /// </summary>
    public double Rotation { get; set; }

    /// <summary>
    /// Layer this text is on.
    /// </summary>
    public int Layer { get; set; } = 1;

    /// <summary>
    /// Whether the text is mirrored.
    /// </summary>
    public bool IsMirrored { get; set; }

    /// <summary>
    /// Text justification.
    /// </summary>
    public TextJustification Justification { get; set; } = TextJustification.BottomLeft;

    /// <summary>
    /// Whether this is a TrueType font (vs stroke font).
    /// </summary>
    public bool IsTrueType { get; set; }

    /// <summary>
    /// Font name for TrueType fonts.
    /// </summary>
    public string? FontName { get; set; }

    /// <summary>
    /// Text kind (stroke, TrueType, or barcode).
    /// </summary>
    public PcbTextKind TextKind { get; set; }

    /// <summary>
    /// Stroke font type.
    /// </summary>
    public PcbStrokeFont StrokeFont { get; set; }

    /// <summary>
    /// Whether the font is bold.
    /// </summary>
    public bool FontBold { get; set; }

    /// <summary>
    /// Whether the font is italic.
    /// </summary>
    public bool FontItalic { get; set; }

    /// <summary>
    /// Whether the text is inverted (white on dark background).
    /// </summary>
    public bool IsInverted { get; set; }

    /// <summary>
    /// Whether the inverted rectangle is enabled.
    /// </summary>
    public bool UseInvertedRectangle { get; set; }

    /// <summary>
    /// Border width for inverted text.
    /// </summary>
    public Coord InvertedBorder { get; set; }

    /// <summary>
    /// Width of the inverted rectangle.
    /// </summary>
    public Coord InvertedRectWidth { get; set; }

    /// <summary>
    /// Height of the inverted rectangle.
    /// </summary>
    public Coord InvertedRectHeight { get; set; }

    /// <summary>
    /// Justification within the inverted rectangle.
    /// </summary>
    public TextJustification InvertedRectJustification { get; set; }

    /// <summary>
    /// Text offset within the inverted rectangle.
    /// </summary>
    public Coord InvertedRectTextOffset { get; set; }

    /// <summary>
    /// Barcode left/right margin.
    /// </summary>
    public Coord BarcodeLRMargin { get; set; }

    /// <summary>
    /// Barcode top/bottom margin.
    /// </summary>
    public Coord BarcodeTBMargin { get; set; }

    /// <summary>
    /// Font ID (stroke font index).
    /// </summary>
    public int FontId { get; set; }

    /// <summary>
    /// Whether to use TrueType fonts.
    /// </summary>
    public bool UseTTFonts { get; set; }

    /// <summary>
    /// Whether multiline text is enabled.
    /// </summary>
    public bool MultiLine { get; set; }

    /// <summary>
    /// Whether word wrap is enabled.
    /// </summary>
    public bool WordWrap { get; set; }

    /// <summary>
    /// Whether the text is mirrored (separate from IsMirrored).
    /// </summary>
    public bool MirrorFlag { get; set; }

    /// <summary>
    /// Whether this text is locked from editing.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Whether this text is a keepout region.
    /// </summary>
    public bool IsKeepout { get; set; }

    /// <summary>
    /// Text size (for TrueType fonts).
    /// </summary>
    public Coord Size { get; set; }

    /// <summary>
    /// Text width (overall).
    /// </summary>
    public Coord Width { get; set; }

    /// <summary>
    /// Multiline text height.
    /// </summary>
    public Coord MultilineTextHeight { get; set; }

    /// <summary>
    /// Multiline text width.
    /// </summary>
    public Coord MultilineTextWidth { get; set; }

    /// <summary>
    /// Whether multiline text resizing is enabled.
    /// </summary>
    public bool MultilineTextResizeEnabled { get; set; }

    /// <summary>
    /// TrueType text height.
    /// </summary>
    public Coord TtfTextHeight { get; set; }

    /// <summary>
    /// TrueType text width.
    /// </summary>
    public Coord TtfTextWidth { get; set; }

    /// <summary>
    /// Barcode kind (Code39, Code128, QR, etc.).
    /// </summary>
    public int BarCodeKind { get; set; }

    /// <summary>
    /// Barcode bit pattern.
    /// </summary>
    public string? BarCodeBitPattern { get; set; }

    /// <summary>
    /// Barcode full height.
    /// </summary>
    public Coord BarCodeFullHeight { get; set; }

    /// <summary>
    /// Barcode full width.
    /// </summary>
    public Coord BarCodeFullWidth { get; set; }

    /// <summary>
    /// Barcode minimum width.
    /// </summary>
    public Coord BarCodeMinWidth { get; set; }

    /// <summary>
    /// Whether to show text below barcode.
    /// </summary>
    public bool BarCodeShowText { get; set; }

    /// <summary>
    /// Snap point X.
    /// </summary>
    public Coord SnapPointX { get; set; }

    /// <summary>
    /// Snap point Y.
    /// </summary>
    public Coord SnapPointY { get; set; }

    /// <summary>
    /// Bounding box X1 location.
    /// </summary>
    public Coord X1Location { get; set; }

    /// <summary>
    /// Bounding box Y1 location.
    /// </summary>
    public Coord Y1Location { get; set; }

    /// <summary>
    /// Bounding box X2 location.
    /// </summary>
    public Coord X2Location { get; set; }

    /// <summary>
    /// Bounding box Y2 location.
    /// </summary>
    public Coord Y2Location { get; set; }

    /// <summary>
    /// Internal wide string index for Unicode text.
    /// </summary>
    internal int WideStringIndex { get; set; } = -1;

    /// <summary>
    /// Unique identifier for this text.
    /// </summary>
    public string? UniqueId { get; set; }

    /// <summary>
    /// Whether this text is enabled (active in the design).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether this text is hidden from view.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// The underlying (pre-conversion) string content.
    /// For special strings like ".Designator", this stores the raw template.
    /// </summary>
    public string? UnderlyingString { get; set; }

    /// <summary>
    /// The converted (post-processing) string content.
    /// Result after special string conversion has been applied.
    /// </summary>
    public string? ConvertedString { get; set; }

    /// <summary>
    /// Whether user routed this text.
    /// </summary>
    public bool UserRouted { get; set; }

    /// <summary>
    /// Union index for grouped primitives.
    /// </summary>
    public int UnionIndex { get; set; }

    /// <summary>
    /// Whether this is a free primitive.
    /// </summary>
    public bool IsFreePrimitive { get; set; }

    /// <summary>
    /// Whether this is an electrical primitive.
    /// </summary>
    public bool IsElectricalPrim { get; set; }

    /// <summary>
    /// Whether this is a pre-route.
    /// </summary>
    public bool IsPreRoute { get; set; }

    /// <summary>
    /// Whether this text has a teardrop.
    /// </summary>
    public bool TearDrop { get; set; }

    /// <summary>
    /// Whether this text is part of a polygon outline.
    /// </summary>
    public bool PolygonOutline { get; set; }

    /// <summary>
    /// Whether tenting is applied.
    /// </summary>
    public bool IsTenting { get; set; }

    /// <summary>
    /// Whether top side is tented.
    /// </summary>
    public bool IsTentingTop { get; set; }

    /// <summary>
    /// Whether bottom side is tented.
    /// </summary>
    public bool IsTentingBottom { get; set; }

    /// <summary>
    /// Whether this is a top-side test point.
    /// </summary>
    public bool IsTestpointTop { get; set; }

    /// <summary>
    /// Whether this is a bottom-side test point.
    /// </summary>
    public bool IsTestpointBottom { get; set; }

    /// <summary>
    /// Whether this is a top assembly test point.
    /// </summary>
    public bool IsAssyTestpointTop { get; set; }

    /// <summary>
    /// Whether this is a bottom assembly test point.
    /// </summary>
    public bool IsAssyTestpointBottom { get; set; }

    /// <summary>
    /// Power plane clearance.
    /// </summary>
    public Coord PowerPlaneClearance { get; set; }

    /// <summary>
    /// Power plane connection style.
    /// </summary>
    public int PowerPlaneConnectStyle { get; set; }

    /// <summary>
    /// Power plane relief expansion.
    /// </summary>
    public Coord PowerPlaneReliefExpansion { get; set; }

    /// <summary>
    /// Thermal relief air gap.
    /// </summary>
    public Coord ReliefAirGap { get; set; }

    /// <summary>
    /// Thermal relief conductor width.
    /// </summary>
    public Coord ReliefConductorWidth { get; set; }

    /// <summary>
    /// Number of thermal relief entries.
    /// </summary>
    public int ReliefEntries { get; set; }

    /// <summary>
    /// Solder mask expansion.
    /// </summary>
    public Coord SolderMaskExpansion { get; set; }

    /// <summary>
    /// Paste mask expansion override.
    /// </summary>
    public Coord PasteMaskExpansion { get; set; }

    /// <summary>
    /// Whether this text allows global editing.
    /// </summary>
    public bool AllowGlobalEdit { get; set; }

    /// <summary>
    /// Whether this text is moveable.
    /// </summary>
    public bool Moveable { get; set; }

    /// <summary>
    /// Whether this text is redundant.
    /// </summary>
    public bool IsRedundant { get; set; }

    /// <summary>
    /// Whether the font is bold.
    /// </summary>
    public bool Bold { get; set; }

    /// <summary>
    /// Whether the font is italic.
    /// </summary>
    public bool Italic { get; set; }

    /// <summary>
    /// Whether the text is mirrored (separate from MirrorFlag).
    /// </summary>
    public bool Mirrored { get; set; }

    /// <summary>
    /// Whether this text is a comment field.
    /// </summary>
    public bool IsComment { get; set; }

    /// <summary>
    /// Whether this text is a designator field.
    /// </summary>
    public bool IsDesignator { get; set; }

    /// <summary>
    /// Whether the barcode is inverted.
    /// </summary>
    public bool BarCodeInverted { get; set; }

    /// <summary>
    /// Barcode render mode.
    /// </summary>
    public int BarCodeRenderMode { get; set; }

    /// <summary>
    /// Barcode font name.
    /// </summary>
    public string? BarCodeFontName { get; set; }

    /// <summary>
    /// Barcode X margin.
    /// </summary>
    public Coord BarCodeXMargin { get; set; }

    /// <summary>
    /// Barcode Y margin.
    /// </summary>
    public Coord BarCodeYMargin { get; set; }

    /// <summary>
    /// Whether advance snapping is enabled.
    /// </summary>
    public bool AdvanceSnapping { get; set; }

    /// <summary>
    /// Border space type.
    /// </summary>
    public int BorderSpaceType { get; set; }

    /// <summary>
    /// Whether multiline rectangle size can be edited.
    /// </summary>
    public bool CanEditMultilineRectSize { get; set; }

    /// <summary>
    /// Character set.
    /// </summary>
    public int CharSet { get; set; }

    /// <summary>
    /// Whether special string conversion is disabled.
    /// </summary>
    public bool DisableSpecialStringConversion { get; set; }

    /// <summary>
    /// Whether the text is inverted.
    /// </summary>
    public bool Inverted { get; set; }

    /// <summary>
    /// Inverted TrueType text border size.
    /// </summary>
    public Coord InvertedTTTextBorder { get; set; }

    /// <summary>
    /// Inverted rectangle height.
    /// </summary>
    public Coord InvRectHeight { get; set; }

    /// <summary>
    /// Inverted rectangle width.
    /// </summary>
    public Coord InvRectWidth { get; set; }

    /// <summary>
    /// Multiline text auto position.
    /// </summary>
    public int MultilineTextAutoPosition { get; set; }

    /// <summary>
    /// TrueType inverted text justify.
    /// </summary>
    public int TtfInvertedTextJustify { get; set; }

    /// <summary>
    /// TrueType offset from inverted rectangle.
    /// </summary>
    public Coord TtfOffsetFromInvertedRect { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            // Approximate bounds based on height and text length
            var estimatedWidth = Height * Text.Length * 0.6; // rough estimate
            return new CoordRect(Location.X, Location.Y, Location.X + estimatedWidth, Location.Y + Height);
        }
    }

    /// <summary>
    /// Creates a fluent builder for new text.
    /// </summary>
    public static TextBuilder Create(string text) => new(text);
}

/// <summary>
/// Fluent builder for creating PCB text.
/// </summary>
public sealed class TextBuilder
{
    private readonly PcbText _text = new();

    internal TextBuilder(string text)
    {
        _text.Text = text;
    }

    /// <summary>
    /// Sets the text location.
    /// </summary>
    public TextBuilder At(Coord x, Coord y)
    {
        _text.Location = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the text location.
    /// </summary>
    public TextBuilder At(CoordPoint location)
    {
        _text.Location = location;
        return this;
    }

    /// <summary>
    /// Sets the text height.
    /// </summary>
    public TextBuilder Height(Coord height)
    {
        _text.Height = height;
        return this;
    }

    /// <summary>
    /// Sets the stroke width.
    /// </summary>
    public TextBuilder StrokeWidth(Coord width)
    {
        _text.StrokeWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the rotation angle.
    /// </summary>
    public TextBuilder Rotation(double degrees)
    {
        _text.Rotation = degrees;
        return this;
    }

    /// <summary>
    /// Sets the layer.
    /// </summary>
    public TextBuilder OnLayer(int layer)
    {
        _text.Layer = layer;
        return this;
    }

    /// <summary>
    /// Sets the layer (alias for OnLayer).
    /// </summary>
    public TextBuilder Layer(int layer)
    {
        _text.Layer = layer;
        return this;
    }

    /// <summary>
    /// Sets the text as mirrored.
    /// </summary>
    public TextBuilder Mirrored(bool mirrored = true)
    {
        _text.IsMirrored = mirrored;
        return this;
    }

    /// <summary>
    /// Sets the justification.
    /// </summary>
    public TextBuilder Justify(TextJustification justification)
    {
        _text.Justification = justification;
        return this;
    }

    /// <summary>
    /// Configures as TrueType font.
    /// </summary>
    public TextBuilder TrueType(string fontName)
    {
        _text.IsTrueType = true;
        _text.FontName = fontName;
        return this;
    }

    /// <summary>
    /// Builds the text.
    /// </summary>
    public PcbText Build() => _text;

    /// <summary>
    /// Implicit conversion to PcbText.
    /// </summary>
    public static implicit operator PcbText(TextBuilder builder) => builder.Build();
}
