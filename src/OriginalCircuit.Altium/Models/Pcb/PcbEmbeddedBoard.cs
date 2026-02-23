using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents an embedded board object in a PCB design.
/// </summary>
public sealed class PcbEmbeddedBoard
{
    /// <summary>
    /// Path to the embedded board document.
    /// </summary>
    public string? DocumentPath { get; set; }

    /// <summary>
    /// Layer this embedded board is on.
    /// </summary>
    public int Layer { get; set; }

    /// <summary>
    /// Rotation angle in degrees.
    /// </summary>
    public double Rotation { get; set; }

    /// <summary>
    /// Whether the board is mirrored.
    /// </summary>
    public bool MirrorFlag { get; set; }

    /// <summary>
    /// Origin mode.
    /// </summary>
    public int OriginMode { get; set; }

    /// <summary>
    /// Scale factor.
    /// </summary>
    public double Scale { get; set; }

    /// <summary>
    /// Number of columns.
    /// </summary>
    public int ColCount { get; set; }

    /// <summary>
    /// Column spacing.
    /// </summary>
    public Coord ColSpacing { get; set; }

    /// <summary>
    /// Number of rows.
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// Row spacing.
    /// </summary>
    public Coord RowSpacing { get; set; }

    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string? UniqueId { get; set; }

    /// <summary>
    /// Whether this embedded board is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether this is a keepout.
    /// </summary>
    public bool IsKeepout { get; set; }

    /// <summary>
    /// Whether this is an electrical primitive.
    /// </summary>
    public bool IsElectricalPrim { get; set; }

    /// <summary>
    /// Whether this is a pre-route.
    /// </summary>
    public bool IsPreRoute { get; set; }

    /// <summary>
    /// Whether this has a teardrop.
    /// </summary>
    public bool TearDrop { get; set; }

    /// <summary>
    /// Whether this is part of a polygon outline.
    /// </summary>
    public bool PolygonOutline { get; set; }

    /// <summary>
    /// Whether user routed.
    /// </summary>
    public bool UserRouted { get; set; }

    /// <summary>
    /// Union index for grouped primitives.
    /// </summary>
    public int UnionIndex { get; set; }

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
    /// Whether this is a viewport.
    /// </summary>
    public bool IsViewport { get; set; }

    /// <summary>
    /// Viewport title.
    /// </summary>
    public string? ViewportTitle { get; set; }

    /// <summary>
    /// Whether the viewport is visible.
    /// </summary>
    public bool ViewportVisible { get; set; }

    /// <summary>
    /// Title font color.
    /// </summary>
    public int TitleFontColor { get; set; }

    /// <summary>
    /// Title font name.
    /// </summary>
    public string? TitleFontName { get; set; }

    /// <summary>
    /// Title font size.
    /// </summary>
    public int TitleFontSize { get; set; }

    /// <summary>
    /// Title object type.
    /// </summary>
    public int TitleObject { get; set; }

    /// <summary>
    /// Whether to transmit board shape.
    /// </summary>
    public bool TransmitBoardShape { get; set; }

    /// <summary>
    /// Whether to transmit dimensions.
    /// </summary>
    public bool TransmitDimensions { get; set; }

    /// <summary>
    /// Whether to transmit drill table.
    /// </summary>
    public bool TransmitDrillTable { get; set; }

    /// <summary>
    /// Whether to transmit top layers enabled.
    /// </summary>
    public bool TransmitLayersEnabledTop { get; set; }

    /// <summary>
    /// Whether to transmit layer stack table.
    /// </summary>
    public bool TransmitLayerStackTable { get; set; }

    /// <summary>
    /// Number of transmit parameters.
    /// </summary>
    public int TransmitParametersCount { get; set; }

    /// <summary>
    /// Whether this embedded board allows global editing.
    /// </summary>
    public bool AllowGlobalEdit { get; set; }

    /// <summary>
    /// Whether this embedded board is moveable.
    /// </summary>
    public bool Moveable { get; set; }

    /// <summary>
    /// Paste mask expansion override.
    /// </summary>
    public Coord PasteMaskExpansion { get; set; }

    /// <summary>
    /// Whether this embedded board is hidden from view.
    /// </summary>
    public bool IsHidden { get; set; }

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
}
