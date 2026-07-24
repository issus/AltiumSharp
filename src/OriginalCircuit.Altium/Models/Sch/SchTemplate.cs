namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic sheet-template reference (record type 39). Identifies the .SchDot
/// template applied to the sheet (border, title block, default fonts).
/// </summary>
public sealed class SchTemplate
{
    /// <summary>
    /// Path to the .SchDot template file applied to the sheet.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the template is locked from selection.
    /// </summary>
    public bool IsNotAccessible { get; set; }

    /// <summary>
    /// Owner part ID (sheet-level records use -1).
    /// </summary>
    public int OwnerPartId { get; set; } = -1;

    /// <summary>
    /// Owner index linking this record to its parent (sheet-level records use -1).
    /// </summary>
    public int OwnerIndex { get; set; } = -1;

    /// <summary>
    /// Index of this record within the sheet.
    /// </summary>
    public int IndexInSheet { get; set; }

    private readonly List<object> _ownedPrimitives = new();

    /// <summary>
    /// Graphical primitives that belong to this sheet template — the border lines, title-block text
    /// and logo images drawn by the applied <c>.SchDot</c>. Altium stores them as records whose
    /// <c>OWNERINDEX</c> points at this template record (type 39). They are kept here, separate from the
    /// document's own top-level primitives, so they are not rendered or netlisted as regular schematic
    /// content (which would double-draw the sheet frame/title block) while still round-tripping on write.
    /// </summary>
    public IReadOnlyList<object> OwnedPrimitives => _ownedPrimitives;

    /// <summary>
    /// Attaches a primitive owned by this template (its <c>OWNERINDEX</c> points at the template record).
    /// </summary>
    public void AddPrimitive(object primitive) => _ownedPrimitives.Add(primitive);
}
