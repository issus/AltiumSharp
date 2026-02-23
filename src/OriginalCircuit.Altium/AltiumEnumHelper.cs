using EdaEnums = OriginalCircuit.Eda.Enums;
using AltiumSchEnums = OriginalCircuit.Altium.Models.Sch;
using AltiumPcbEnums = OriginalCircuit.Altium.Models.Pcb;

namespace OriginalCircuit.Altium;

/// <summary>
/// Maps between Altium-specific enum values and shared <see cref="OriginalCircuit.Eda.Enums"/> values.
/// Altium assigns different integer values to some enum members.
/// </summary>
internal static class AltiumEnumHelper
{
    /// <summary>
    /// Converts Altium <see cref="AltiumSchEnums.PinElectricalType"/> to shared <see cref="EdaEnums.PinElectricalType"/>.
    /// Altium order: Input=0, InputOutput=1, Output=2, OpenCollector=3, Passive=4, HiZ=5, OpenEmitter=6, Power=7
    /// Shared order: Input=0, Output=1, Bidirectional=2, Passive=3, TriState=4, PowerIn=5, PowerOut=6, OpenCollector=7, OpenEmitter=8, Unspecified=9, NoConnect=10, Free=11
    /// </summary>
    public static EdaEnums.PinElectricalType ToEdaPinElectricalType(AltiumSchEnums.PinElectricalType altiumType) =>
        altiumType switch
        {
            AltiumSchEnums.PinElectricalType.Input => EdaEnums.PinElectricalType.Input,
            AltiumSchEnums.PinElectricalType.InputOutput => EdaEnums.PinElectricalType.Bidirectional,
            AltiumSchEnums.PinElectricalType.Output => EdaEnums.PinElectricalType.Output,
            AltiumSchEnums.PinElectricalType.OpenCollector => EdaEnums.PinElectricalType.OpenCollector,
            AltiumSchEnums.PinElectricalType.Passive => EdaEnums.PinElectricalType.Passive,
            AltiumSchEnums.PinElectricalType.HiZ => EdaEnums.PinElectricalType.TriState,
            AltiumSchEnums.PinElectricalType.OpenEmitter => EdaEnums.PinElectricalType.OpenEmitter,
            AltiumSchEnums.PinElectricalType.Power => EdaEnums.PinElectricalType.PowerIn,
            _ => EdaEnums.PinElectricalType.Unspecified
        };

    /// <summary>
    /// Converts shared <see cref="EdaEnums.PinElectricalType"/> back to Altium <see cref="AltiumSchEnums.PinElectricalType"/>.
    /// </summary>
    public static AltiumSchEnums.PinElectricalType ToAltiumPinElectricalType(EdaEnums.PinElectricalType edaType) =>
        edaType switch
        {
            EdaEnums.PinElectricalType.Input => AltiumSchEnums.PinElectricalType.Input,
            EdaEnums.PinElectricalType.Output => AltiumSchEnums.PinElectricalType.Output,
            EdaEnums.PinElectricalType.Bidirectional => AltiumSchEnums.PinElectricalType.InputOutput,
            EdaEnums.PinElectricalType.Passive => AltiumSchEnums.PinElectricalType.Passive,
            EdaEnums.PinElectricalType.TriState => AltiumSchEnums.PinElectricalType.HiZ,
            EdaEnums.PinElectricalType.PowerIn => AltiumSchEnums.PinElectricalType.Power,
            EdaEnums.PinElectricalType.PowerOut => AltiumSchEnums.PinElectricalType.Power,
            EdaEnums.PinElectricalType.OpenCollector => AltiumSchEnums.PinElectricalType.OpenCollector,
            EdaEnums.PinElectricalType.OpenEmitter => AltiumSchEnums.PinElectricalType.OpenEmitter,
            _ => AltiumSchEnums.PinElectricalType.Input
        };

    /// <summary>
    /// Converts Altium <see cref="AltiumPcbEnums.PadShape"/> to shared <see cref="EdaEnums.PadShape"/>.
    /// Altium: Round=1, Rectangular=2, Octagonal=3, RoundedRectangle=9
    /// Shared: Circle=0, Rect=1, Oval=2, RoundRect=3, Trapezoid=4, Custom=5
    /// </summary>
    public static EdaEnums.PadShape ToEdaPadShape(AltiumPcbEnums.PadShape altiumShape) =>
        altiumShape switch
        {
            AltiumPcbEnums.PadShape.Round => EdaEnums.PadShape.Circle,
            AltiumPcbEnums.PadShape.Rectangular => EdaEnums.PadShape.Rect,
            AltiumPcbEnums.PadShape.Octagonal => EdaEnums.PadShape.Custom, // no direct equivalent
            AltiumPcbEnums.PadShape.RoundedRectangle => EdaEnums.PadShape.RoundRect,
            _ => EdaEnums.PadShape.Custom
        };

    /// <summary>
    /// Converts shared <see cref="EdaEnums.PadShape"/> back to Altium <see cref="AltiumPcbEnums.PadShape"/>.
    /// </summary>
    public static AltiumPcbEnums.PadShape ToAltiumPadShape(EdaEnums.PadShape edaShape) =>
        edaShape switch
        {
            EdaEnums.PadShape.Circle => AltiumPcbEnums.PadShape.Round,
            EdaEnums.PadShape.Rect => AltiumPcbEnums.PadShape.Rectangular,
            EdaEnums.PadShape.Oval => AltiumPcbEnums.PadShape.Round, // closest match
            EdaEnums.PadShape.RoundRect => AltiumPcbEnums.PadShape.RoundedRectangle,
            _ => AltiumPcbEnums.PadShape.Rectangular
        };

    /// <summary>
    /// Converts Altium <see cref="AltiumPcbEnums.PadHoleType"/> to shared <see cref="EdaEnums.PadHoleType"/>.
    /// Altium: Round=0, Square=1, Slot=2
    /// Shared: Round=0, Slot=1
    /// </summary>
    public static EdaEnums.PadHoleType ToEdaPadHoleType(AltiumPcbEnums.PadHoleType altiumType) =>
        altiumType switch
        {
            AltiumPcbEnums.PadHoleType.Round => EdaEnums.PadHoleType.Round,
            AltiumPcbEnums.PadHoleType.Square => EdaEnums.PadHoleType.Round, // closest match
            AltiumPcbEnums.PadHoleType.Slot => EdaEnums.PadHoleType.Slot,
            _ => EdaEnums.PadHoleType.Round
        };

    /// <summary>
    /// Converts Altium line style int to shared <see cref="EdaEnums.LineStyle"/>.
    /// Both use same integer values (0=Solid, 1=Dash, 2=Dot, 3=DashDot, 4=DashDotDot).
    /// </summary>
    public static EdaEnums.LineStyle ToEdaLineStyle(int altiumLineStyle) =>
        altiumLineStyle switch
        {
            0 => EdaEnums.LineStyle.Solid,
            1 => EdaEnums.LineStyle.Dash,
            2 => EdaEnums.LineStyle.Dot,
            3 => EdaEnums.LineStyle.DashDot,
            4 => EdaEnums.LineStyle.DashDotDot,
            _ => EdaEnums.LineStyle.Solid
        };

    /// <summary>
    /// Converts <see cref="EdaEnums.SchLineStyle"/> to shared <see cref="EdaEnums.LineStyle"/>.
    /// </summary>
    public static EdaEnums.LineStyle SchLineStyleToEdaLineStyle(EdaEnums.SchLineStyle style) =>
        style switch
        {
            EdaEnums.SchLineStyle.Solid => EdaEnums.LineStyle.Solid,
            EdaEnums.SchLineStyle.Dashed => EdaEnums.LineStyle.Dash,
            EdaEnums.SchLineStyle.Dotted => EdaEnums.LineStyle.Dot,
            _ => EdaEnums.LineStyle.Solid
        };
}
