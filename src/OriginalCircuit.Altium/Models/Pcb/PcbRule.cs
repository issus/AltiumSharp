namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a PCB design rule from the Rules6 storage.
/// Rules define constraints like clearance, width, routing, etc.
/// </summary>
public sealed class PcbRule
{
    /// <summary>
    /// Rule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Rule kind identifier (e.g., "Clearance", "Width", "RoutingTopology").
    /// </summary>
    public string RuleKind { get; set; } = string.Empty;

    /// <summary>
    /// Whether this rule is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Rule priority (lower number = higher priority).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Comment/description for this rule.
    /// </summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier for this rule.
    /// </summary>
    public string UniqueId { get; set; } = string.Empty;

    /// <summary>
    /// First scope expression (query filter).
    /// </summary>
    public string Scope1Expression { get; set; } = string.Empty;

    /// <summary>
    /// Second scope expression (query filter).
    /// </summary>
    public string Scope2Expression { get; set; } = string.Empty;

    /// <summary>
    /// All parameters for this rule, including rule-kind-specific keys
    /// (e.g., GAP for clearance, MINWIDTH/MAXWIDTH for width rules).
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Synchronizes typed properties back into the Parameters dictionary and returns it.
    /// </summary>
    public Dictionary<string, string> ToParameters()
    {
        Parameters["NAME"] = Name;
        Parameters["RULEKIND"] = RuleKind;
        Parameters["ENABLED"] = Enabled ? "TRUE" : "FALSE";
        Parameters["PRIORITY"] = Priority.ToString();
        if (!string.IsNullOrEmpty(Comment)) Parameters["COMMENT"] = Comment;
        if (!string.IsNullOrEmpty(UniqueId)) Parameters["UNIQUEID"] = UniqueId;
        if (!string.IsNullOrEmpty(Scope1Expression)) Parameters["SCOPE1EXPRESSION"] = Scope1Expression;
        if (!string.IsNullOrEmpty(Scope2Expression)) Parameters["SCOPE2EXPRESSION"] = Scope2Expression;
        return Parameters;
    }
}
