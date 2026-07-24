namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// How a pad or via connects to an internal power/ground plane. Mirrors Altium's
/// <c>TPlaneConnectStyle</c> (<c>eReliefConnectToPlane</c>, <c>eDirectConnectToPlane</c>,
/// <c>eNoConnect</c>) and is the stored value behind the pad/via "Plane Connection"
/// setting shown in the Altium properties panel.
/// </summary>
/// <remarks>
/// <para>
/// The underlying byte is verified against Altium's own scripting API: in
/// <c>PCBObjectInspector.pas</c> the value of <c>IPCB_Pad2.PowerPlaneConnectStyle</c>
/// indexes the array <c>['eReliefConnectToPlane', 'eDirectConnectToPlane', 'eNoConnect']</c>,
/// so <c>0</c> is a thermal-relief connection, <c>1</c> is a solid/direct connection and
/// <c>2</c> is no connection. The thermal-relief geometry (conductor width, entries, air
/// gap and expansion) only applies when the style is <see cref="Relief"/>.
/// </para>
/// <para>
/// This is the pad/via's <i>stored/configured</i> style — the value shown in the footprint
/// pad properties and the one the relief geometry belongs to. It is not necessarily the
/// context-dependent <i>effective</i> style that Altium's scripting API computes for a placed
/// board: in a footprint library (no power planes) the API reports every primitive as
/// <c>eNoConnect</c> regardless of this stored value.
/// </para>
/// </remarks>
public enum PlaneConnectStyle
{
    /// <summary>Thermal-relief connection (spokes). Altium <c>eReliefConnectToPlane</c>.</summary>
    Relief = 0,

    /// <summary>Solid/direct connection. Altium <c>eDirectConnectToPlane</c>.</summary>
    Direct = 1,

    /// <summary>No connection to the plane. Altium <c>eNoConnect</c>.</summary>
    NoConnect = 2
}
