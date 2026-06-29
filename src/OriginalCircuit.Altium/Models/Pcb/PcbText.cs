using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// The three built-in (vector) stroke fonts that Altium ships in the default font table.
/// These are the well-known low font-table IDs stored in a PCB text record's font field
/// (see <see cref="PcbText.FontId"/>); the underlying value is a font-table index, so values
/// outside this set are possible (custom / TrueType fonts) and are represented directly by
/// <see cref="PcbText.FontId"/>. Verified against Altium AD25 (a default stroke text uses
/// <see cref="Default"/> = 1) and the altium_monkey reference (1=Default, 2=Sans Serif, 3=Serif).
/// </summary>
public enum PcbStrokeFont
{
    /// <summary>Altium's default stroke font (font-table id 1).</summary>
    Default = 1,
    /// <summary>The built-in sans-serif stroke font (font-table id 2).</summary>
    SansSerif = 2,
    /// <summary>The built-in serif stroke font (font-table id 3).</summary>
    Serif = 3
}

/// <summary>
/// The barcode symbology of a PCB text whose <see cref="PcbText.TextKind"/> is
/// <see cref="OriginalCircuit.Eda.Enums.PcbTextKind.BarCode"/>. Values match Altium's
/// <c>TBarcodeKind</c> ordinals exactly (verified against the AD25 <c>Advpcb.dll</c> RTTI:
/// <c>eBarcode39=0, eBarCode128=1, eBarCode_QrCode=2, eBarCode_DataMatrix=3</c>). The underlying
/// storage is the raw byte in <see cref="PcbText.BarCodeKind"/>, so out-of-range values round-trip
/// via that field even though they have no named member here.
/// </summary>
public enum PcbBarCodeKind
{
    /// <summary>Code 39 (1-D, alphanumeric).</summary>
    Code39 = 0,
    /// <summary>Code 128 (1-D, full ASCII).</summary>
    Code128 = 1,
    /// <summary>QR Code (2-D).</summary>
    QrCode = 2,
    /// <summary>Data Matrix / ECC200 (2-D).</summary>
    DataMatrix = 3
}

/// <summary>
/// Anchor / justification of PCB text inside its frame (the "inverted rectangle" / text-box
/// justification byte). Values match Altium's <c>TTextAutoposition</c> encoding exactly (column-major,
/// 1-based, with <see cref="Manual"/> = 0). NOTE: this is a different ordering from the schematic
/// <c>TextJustification</c> (which is 0-based, row-major from bottom).
/// </summary>
public enum PcbTextJustification
{
    /// <summary>No automatic justification (0).</summary>
    Manual = 0,
    LeftTop = 1,
    LeftCenter = 2,
    LeftBottom = 3,
    CenterTop = 4,
    CenterCenter = 5,
    CenterBottom = 6,
    RightTop = 7,
    RightCenter = 8,
    RightBottom = 9
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

    /// <summary>Net index into the board's net list (0xFFFF = no net).</summary>
    public ushort NetIndex { get; set; } = 0xFFFF;

    /// <summary>Component index into the board's component list (-1 = not part of a component).</summary>
    public int ComponentIndex { get; set; } = -1;

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
    /// Convenience view of <see cref="FontId"/> as one of the three built-in stroke fonts.
    /// Reads/writes <see cref="FontId"/> directly, so a font-table index outside 1..3 round-trips
    /// faithfully via <see cref="FontId"/> even though it has no named <see cref="PcbStrokeFont"/> value.
    /// </summary>
    public PcbStrokeFont StrokeFont
    {
        get => (PcbStrokeFont)FontId;
        set => FontId = (int)value;
    }

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
    /// Justification / anchor of the text within its frame (the "inverted rectangle"). Uses the
    /// Altium PCB encoding (<see cref="PcbTextJustification"/>), which differs from the schematic
    /// <c>TextJustification</c> ordering.
    /// </summary>
    public PcbTextJustification InvertedRectJustification { get; set; }

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
    /// Altium font-table index for this text (the record's <c>fontID</c> field, stored as the
    /// 16-bit value at binary offset 25). For stroke text this selects the vector font; ids
    /// 1/2/3 are the built-in Default/Sans-Serif/Serif stroke fonts (see <see cref="PcbStrokeFont"/>
    /// and <see cref="StrokeFont"/>). Defaults to 1 (Altium's default), matching how Altium writes
    /// a freshly created stroke text.
    /// </summary>
    public int FontId { get; set; } = 1;

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
    /// Barcode symbology as the raw Altium <c>TBarcodeKind</c> ordinal (0=Code39, 1=Code128, 2=QR,
    /// 3=Data Matrix). Stored as an <see cref="int"/> for byte-exact round-tripping of any value; use
    /// <see cref="BarCodeType"/> for the typed view.
    /// </summary>
    public int BarCodeKind { get; set; }

    /// <summary>
    /// Typed view of <see cref="BarCodeKind"/> as a <see cref="PcbBarCodeKind"/>. Reads/writes the raw
    /// <see cref="BarCodeKind"/> ordinal directly, so unknown values round-trip faithfully.
    /// </summary>
    public PcbBarCodeKind BarCodeType
    {
        get => (PcbBarCodeKind)BarCodeKind;
        set => BarCodeKind = (int)value;
    }

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
    /// Whether this is a free primitive (not owned by a component). Derived from
    /// <see cref="ComponentIndex"/> (&lt; 0 means free), which is the authoritative ownership signal.
    /// </summary>
    public bool IsFreePrimitive => ComponentIndex < 0;

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
    /// Override for the SubRecord-1 base font-type byte (offset 43). The writer normally derives this
    /// from <see cref="TextKind"/>/<see cref="IsTrueType"/>; this captures the exact byte only when the
    /// source disagrees with the derived value (barcodes can carry a base font that the model flattens).
    /// Null otherwise (derive). Keeps round-trip byte-exact without breaking from-scratch authoring.
    /// </summary>
    internal byte? BaseFontType { get; set; }

    /// <summary>
    /// Override for the SubRecord-1 authoritative text-kind byte (offset 160). Normally derived from
    /// <see cref="TextKind"/>; captures the exact byte only when the source disagrees. Null otherwise.
    /// </summary>
    internal byte? TextKindByte { get; set; }

    /// <summary>
    /// Exact 64-byte primary font-name field (offsets 46-109), captured only when its bytes after the
    /// name's null terminator are non-zero. That trailing "dirt" is <em>non-deterministic stale heap
    /// memory</em> Altium leaves from a reused buffer — observed to contain leftover previous font names
    /// (e.g. "…New Roman"), heap pointers, even fragments of another record's parameter block — so it is
    /// irreducible: it cannot be reconstructed from the model. It affects ~half of real-world texts.
    /// Null when the field is clean, in which case the writer emits the modeled <see cref="FontName"/>
    /// zero-padded (deterministic). From-scratch texts always write clean; this capture only lets a
    /// loaded file round-trip byte-for-byte.
    /// </summary>
    internal byte[]? FontFieldRaw { get; set; }

    /// <summary>
    /// Exact 64-byte barcode font-name field (offsets 161-224), captured only when its trailing padding is
    /// non-zero (same irreducible stale-heap dirt as <see cref="FontFieldRaw"/>). Null otherwise (writer
    /// emits the modeled <see cref="BarCodeFontName"/> zero-padded).
    /// </summary>
    internal byte[]? BarCodeFontFieldRaw { get; set; }

    /// <summary>
    /// The raw 16-bit primitive flags word as read from the source, so unmodelled flag bits round-trip
    /// verbatim. Null for text built from scratch. See PcbBinaryConstants.MergeFlags.
    /// </summary>
    internal ushort? RawFlags { get; set; }

    /// <summary>
    /// Whether the text is rendered with a frame (text box).
    /// </summary>
    public bool IsFrame { get; set; }

    /// <summary>
    /// Text border spacing mode: false = margin, true = offset.
    /// </summary>
    public bool IsOffsetBorder { get; set; }

    /// <summary>
    /// Whether the text-box justification value is valid.
    /// </summary>
    public bool IsJustificationValid { get; set; }

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
            // Approximate text extent based on height and text length.
            var estimatedWidth = Height * Text.Length * 0.6; // rough estimate
            if (Rotation == 0 && !Mirrored)
                return new CoordRect(Location.X, Location.Y, Location.X + estimatedWidth, Location.Y + Height);

            // Axis-aligned bounding box of the rotated (and possibly mirrored) text box anchored at
            // Location. Without this, rotated silkscreen designators (commonly at 90/270 deg) report a
            // too-small extent and get clipped by AutoZoom framing.
            var rad = Rotation * System.Math.PI / 180.0;
            var cos = System.Math.Cos(rad);
            var sin = System.Math.Sin(rad);
            var w = (Mirrored ? -estimatedWidth.ToRaw() : estimatedWidth.ToRaw());
            var h = Height.ToRaw();
            int minX = 0, minY = 0, maxX = 0, maxY = 0;
            foreach (var (cx, cy) in new[] { (0, 0), (w, 0), (w, h), (0, h) })
            {
                var rx = (int)System.Math.Round(cx * cos - cy * sin);
                var ry = (int)System.Math.Round(cx * sin + cy * cos);
                minX = System.Math.Min(minX, rx); maxX = System.Math.Max(maxX, rx);
                minY = System.Math.Min(minY, ry); maxY = System.Math.Max(maxY, ry);
            }
            return new CoordRect(
                Location.X + Coord.FromRaw(minX), Location.Y + Coord.FromRaw(minY),
                Location.X + Coord.FromRaw(maxX), Location.Y + Coord.FromRaw(maxY));
        }
    }

    /// <summary>
    /// Moves this text by the given offset, shifting its <see cref="Location"/> and snap point.
    /// </summary>
    /// <param name="dx">The X offset.</param>
    /// <param name="dy">The Y offset.</param>
    public void Translate(Coord dx, Coord dy)
    {
        Location = Location.Offset(dx, dy);
        SnapPointX += dx;
        SnapPointY += dy;
    }

    /// <summary>
    /// Rotates this text counter-clockwise by <paramref name="degrees"/> about <paramref name="pivot"/>,
    /// moving its <see cref="Location"/> and snap point and spinning its own <see cref="Rotation"/>.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees (counter-clockwise).</param>
    /// <param name="pivot">The point to rotate about.</param>
    public void Rotate(double degrees, CoordPoint pivot)
    {
        Location = Location.RotateAround(pivot, degrees);
        var snap = new CoordPoint(SnapPointX, SnapPointY).RotateAround(pivot, degrees);
        SnapPointX = snap.X;
        SnapPointY = snap.Y;
        Rotation = PcbRotation.Normalize360(Rotation + degrees);
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
