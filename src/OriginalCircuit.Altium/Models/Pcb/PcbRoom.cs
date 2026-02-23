namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a PCB room from the Rooms6 storage.
/// Rooms define physical placement regions for components.
/// </summary>
public sealed class PcbRoom
{
    /// <summary>
    /// Room name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string UniqueId { get; set; } = string.Empty;

    /// <summary>
    /// All parameters for this room.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Synchronizes typed properties back into the Parameters dictionary and returns it.
    /// </summary>
    public Dictionary<string, string> ToParameters()
    {
        Parameters["NAME"] = Name;
        if (!string.IsNullOrEmpty(UniqueId)) Parameters["UNIQUEID"] = UniqueId;
        return Parameters;
    }
}
