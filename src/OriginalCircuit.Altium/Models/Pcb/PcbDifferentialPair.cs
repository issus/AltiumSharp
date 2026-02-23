namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a PCB differential pair from the DifferentialPairs6 storage.
/// Links a positive and negative net as a differential pair.
/// </summary>
public sealed class PcbDifferentialPair
{
    /// <summary>
    /// Differential pair name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Positive net name.
    /// </summary>
    public string PositiveNetName { get; set; } = string.Empty;

    /// <summary>
    /// Negative net name.
    /// </summary>
    public string NegativeNetName { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string UniqueId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this differential pair is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// All parameters for this differential pair.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Synchronizes typed properties back into the Parameters dictionary and returns it.
    /// </summary>
    public Dictionary<string, string> ToParameters()
    {
        Parameters["NAME"] = Name;
        if (!string.IsNullOrEmpty(PositiveNetName)) Parameters["POSITIVENETNAME"] = PositiveNetName;
        if (!string.IsNullOrEmpty(NegativeNetName)) Parameters["NEGATIVENETNAME"] = NegativeNetName;
        if (!string.IsNullOrEmpty(UniqueId)) Parameters["UNIQUEID"] = UniqueId;
        Parameters["ENABLED"] = Enabled ? "TRUE" : "FALSE";
        return Parameters;
    }
}
