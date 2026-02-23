namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a PCB object class from the Classes6 storage.
/// Classes group nets, components, or other objects together.
/// </summary>
public sealed class PcbObjectClass
{
    /// <summary>
    /// Class name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Super class identifier.
    /// </summary>
    public string SuperClass { get; set; } = string.Empty;

    /// <summary>
    /// Sub class identifier.
    /// </summary>
    public string SubClass { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier for this class.
    /// </summary>
    public string UniqueId { get; set; } = string.Empty;

    /// <summary>
    /// Kind of member objects (e.g., "Net", "Component", "Pad").
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Whether this class is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Member names in this class (indexed as MEMBER0, MEMBER1, ...).
    /// </summary>
    public List<string> Members { get; } = new();

    /// <summary>
    /// All parameters for this class.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Synchronizes typed properties back into the Parameters dictionary and returns it.
    /// </summary>
    public Dictionary<string, string> ToParameters()
    {
        Parameters["NAME"] = Name;
        if (!string.IsNullOrEmpty(SuperClass)) Parameters["SUPERCLASS"] = SuperClass;
        if (!string.IsNullOrEmpty(SubClass)) Parameters["SUBCLASS"] = SubClass;
        if (!string.IsNullOrEmpty(UniqueId)) Parameters["UNIQUEID"] = UniqueId;
        if (!string.IsNullOrEmpty(Kind)) Parameters["KIND"] = Kind;
        Parameters["ENABLED"] = Enabled ? "TRUE" : "FALSE";
        for (int i = 0; i < Members.Count; i++)
            Parameters[$"MEMBER{i}"] = Members[i];
        Parameters["MEMBERCOUNT"] = Members.Count.ToString();
        return Parameters;
    }
}
