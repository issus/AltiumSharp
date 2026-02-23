namespace OriginalCircuit.Altium.Rendering;

/// <summary>
/// Describes a font entry from a schematic document's font table.
/// </summary>
public sealed record SchFontInfo(string FontName, double Size, bool Bold, bool Italic);
