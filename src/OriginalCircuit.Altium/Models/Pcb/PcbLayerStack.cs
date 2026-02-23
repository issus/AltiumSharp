namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a single layer entry in the PCB layer stack.
/// </summary>
public sealed class PcbLayerEntry
{
    /// <summary>
    /// Layer index (1-based).
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Layer name (e.g., "Top Layer", "Bottom Layer", "Mid-Layer 1").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Index of the previous layer in the stack.
    /// </summary>
    public int PreviousIndex { get; set; }

    /// <summary>
    /// Index of the next layer in the stack.
    /// </summary>
    public int NextIndex { get; set; }

    /// <summary>
    /// Whether copper is present on this layer.
    /// </summary>
    public bool CopperEnabled { get; set; }

    /// <summary>
    /// Dielectric material name.
    /// </summary>
    public string DielectricMaterial { get; set; } = string.Empty;

    /// <summary>
    /// Layer color as packed integer.
    /// </summary>
    public int Color { get; set; }
}

/// <summary>
/// Represents the PCB layer stack parsed from Board6 parameters.
/// Provides convenient access to layer ordering and properties.
/// </summary>
public sealed class PcbLayerStack
{
    /// <summary>
    /// Ordered list of layer entries from top to bottom.
    /// </summary>
    public IReadOnlyList<PcbLayerEntry> Layers { get; }

    private PcbLayerStack(List<PcbLayerEntry> layers)
    {
        Layers = layers;
    }

    /// <summary>
    /// Parses a layer stack from Board6 parameters.
    /// Returns null if no layer stack data is present.
    /// </summary>
    public static PcbLayerStack? FromBoardParameters(Dictionary<string, string>? parameters)
    {
        if (parameters == null)
            return null;

        var entries = new Dictionary<int, PcbLayerEntry>();

        // Scan for V7_LAYER{N}NAME pattern
        for (var i = 1; i <= 100; i++)
        {
            if (!parameters.TryGetValue($"V7_LAYER{i}NAME", out var name))
                continue;

            var entry = new PcbLayerEntry { Index = i, Name = name };

            if (parameters.TryGetValue($"V7_LAYER{i}PREV", out var prev) && int.TryParse(prev, out var prevIdx))
                entry.PreviousIndex = prevIdx;
            if (parameters.TryGetValue($"V7_LAYER{i}NEXT", out var next) && int.TryParse(next, out var nextIdx))
                entry.NextIndex = nextIdx;
            if (parameters.TryGetValue($"V7_LAYER{i}COPTHICK", out var cop))
                entry.CopperEnabled = cop != "0";
            if (parameters.TryGetValue($"V7_LAYER{i}DIELTYPE", out var diel))
                entry.DielectricMaterial = diel;
            if (parameters.TryGetValue($"V7_LAYER{i}COLOR", out var color) && int.TryParse(color, out var c))
                entry.Color = c;

            entries[i] = entry;
        }

        if (entries.Count == 0)
            return null;

        // Build ordered list by following PREV/NEXT chain
        // Find the first layer (no valid previous)
        var ordered = new List<PcbLayerEntry>();
        var first = entries.Values.FirstOrDefault(e => e.PreviousIndex == 0 || !entries.ContainsKey(e.PreviousIndex));
        if (first != null)
        {
            var current = first;
            var visited = new HashSet<int>();
            while (current != null && visited.Add(current.Index))
            {
                ordered.Add(current);
                entries.TryGetValue(current.NextIndex, out current);
            }
        }

        // If chain traversal missed some entries, add them at the end
        if (ordered.Count < entries.Count)
        {
            foreach (var entry in entries.Values.OrderBy(e => e.Index))
            {
                if (!ordered.Contains(entry))
                    ordered.Add(entry);
            }
        }

        return new PcbLayerStack(ordered);
    }
}
