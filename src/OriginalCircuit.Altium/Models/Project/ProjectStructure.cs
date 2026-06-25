using System.Globalization;
using System.Text;

namespace OriginalCircuit.Altium.Models.Project;

/// <summary>
/// One pipe-delimited record from a <c>.PrjPcbStructure</c> file. The fields are kept in order
/// (so the file round-trips exactly) with typed accessors for the common keys.
/// </summary>
public sealed class ProjectStructureRecord
{
    /// <summary>Creates a record from its ordered fields.</summary>
    public ProjectStructureRecord(List<KeyValuePair<string, string>> fields)
    {
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
    }

    /// <summary>All <c>key=value</c> fields of the record, in file order, values stored exactly as written.</summary>
    public List<KeyValuePair<string, string>> Fields { get; }

    /// <summary>The record kind: the value of the leading <c>Record</c> field (e.g. <c>"TopLevelDocument"</c> or <c>"SheetSymbol"</c>).</summary>
    public string? Record => Field("Record");

    /// <summary>The document this record references (a sheet symbol's child sheet, or the top document).</summary>
    public string? FileName => Field("FileName");

    /// <summary>For a sheet symbol, the parent document that contains it.</summary>
    public string? SourceDocument => Field("SourceDocument");

    /// <summary>For a sheet symbol, its designator (instance name, e.g. <c>"U_Power"</c>).</summary>
    public string? Designator => Field("Designator");

    /// <summary>The assigned sheet number, when present.</summary>
    public int? SheetNumber =>
        int.TryParse(Field("SheetNumber"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    /// <summary>Returns the first field with the given key (case-insensitive), or <c>null</c>.</summary>
    public string? Field(string key)
    {
        foreach (var f in Fields)
            if (string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase))
                return f.Value;
        return null;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Record}: {Designator ?? "(top)"} -> {FileName}";
}

/// <summary>
/// A node in a project's logical sheet hierarchy (built from the flat
/// <see cref="ProjectStructure.Records"/>). Each node is one sheet <em>instance</em>: the same
/// document may appear at several nodes if it is instantiated by more than one sheet symbol.
/// </summary>
public sealed class ProjectSheetNode
{
    internal ProjectSheetNode(string fileName, string? designator, int? sheetNumber)
    {
        FileName = fileName;
        Designator = designator;
        SheetNumber = sheetNumber;
    }

    /// <summary>The document this node represents (e.g. <c>"Power.SchDoc"</c>).</summary>
    public string FileName { get; }

    /// <summary>The instance designator of the sheet symbol that created this node, or <c>null</c> for the root.</summary>
    public string? Designator { get; }

    /// <summary>The sheet number assigned to this instance, when known.</summary>
    public int? SheetNumber { get; }

    /// <summary>The child sheets instantiated within this sheet.</summary>
    public List<ProjectSheetNode> Children { get; } = new();

    /// <summary><c>true</c> when this node was pruned because it would re-enter a document already on its ancestor chain.</summary>
    public bool IsCycle { get; internal set; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{(Designator is null ? "" : Designator + " -> ")}{FileName}" + (Children.Count > 0 ? $" ({Children.Count} children)" : "");
}

/// <summary>
/// The parsed contents of a <c>.PrjPcbStructure</c> file: the project's compiled logical document
/// structure (which schematic sheet symbols instantiate which child sheets). The raw
/// <see cref="Records"/> preserve the file byte-for-byte; <see cref="BuildTree"/> turns them into a
/// navigable hierarchy.
/// </summary>
public sealed class ProjectStructure
{
    /// <summary>The records exactly as they appear in the file, in order.</summary>
    public List<ProjectStructureRecord> Records { get; } = new();

    /// <summary>The top-level document file name (the <c>Record=TopLevelDocument</c> entry), or <c>null</c>.</summary>
    public string? TopLevelDocument
    {
        get
        {
            foreach (var r in Records)
                if (string.Equals(r.Record, "TopLevelDocument", StringComparison.OrdinalIgnoreCase))
                    return r.FileName;
            return null;
        }
    }

    /// <summary>
    /// Builds the logical sheet hierarchy from <see cref="Records"/>. The root is the top-level
    /// document; each sheet's children are the sheet-symbol records whose <c>SourceDocument</c>
    /// equals that sheet's document. Re-entrant references (a document appearing on its own ancestor
    /// chain) are pruned and flagged via <see cref="ProjectSheetNode.IsCycle"/>.
    /// </summary>
    /// <returns>The root node, or <c>null</c> when there is no top-level document record.</returns>
    public ProjectSheetNode? BuildTree()
    {
        // Group sheet-symbol records by their parent document.
        var childrenByParent = new Dictionary<string, List<ProjectStructureRecord>>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in Records)
        {
            if (!string.Equals(r.Record, "SheetSymbol", StringComparison.OrdinalIgnoreCase))
                continue;
            var parent = r.SourceDocument;
            if (string.IsNullOrEmpty(parent))
                continue;
            if (!childrenByParent.TryGetValue(parent, out var list))
                childrenByParent[parent] = list = new List<ProjectStructureRecord>();
            list.Add(r);
        }

        var topDoc = TopLevelDocument;
        if (string.IsNullOrEmpty(topDoc))
            return null;

        var root = new ProjectSheetNode(topDoc, null, TopSheetNumber());
        AddChildren(root, childrenByParent, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { topDoc });
        return root;
    }

    private int? TopSheetNumber()
    {
        foreach (var r in Records)
            if (string.Equals(r.Record, "TopLevelDocument", StringComparison.OrdinalIgnoreCase))
                return r.SheetNumber;
        return null;
    }

    private static void AddChildren(
        ProjectSheetNode node,
        Dictionary<string, List<ProjectStructureRecord>> childrenByParent,
        HashSet<string> ancestors)
    {
        if (!childrenByParent.TryGetValue(node.FileName, out var children))
            return;

        foreach (var rec in children)
        {
            var childDoc = rec.FileName;
            if (string.IsNullOrEmpty(childDoc))
                continue;

            var child = new ProjectSheetNode(childDoc, rec.Designator, rec.SheetNumber);
            node.Children.Add(child);

            if (ancestors.Contains(childDoc))
            {
                child.IsCycle = true; // re-entrant reference — stop here to avoid infinite recursion
                continue;
            }

            ancestors.Add(childDoc);
            AddChildren(child, childrenByParent, ancestors);
            ancestors.Remove(childDoc);
        }
    }

    /// <summary>
    /// Parses the text of a <c>.PrjPcbStructure</c> file into a <see cref="ProjectStructure"/>.
    /// Blank lines are ignored; each non-blank line is one pipe-delimited record.
    /// </summary>
    public static ProjectStructure Parse(string text)
    {
        var structure = new ProjectStructure();
        if (string.IsNullOrEmpty(text))
            return structure;

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Length == 0)
                continue;

            var fields = new List<KeyValuePair<string, string>>();
            foreach (var segment in line.Split('|'))
            {
                var eq = segment.IndexOf('=');
                fields.Add(eq < 0
                    ? new KeyValuePair<string, string>(segment, string.Empty)
                    : new KeyValuePair<string, string>(segment[..eq], segment[(eq + 1)..]));
            }
            structure.Records.Add(new ProjectStructureRecord(fields));
        }

        return structure;
    }

    /// <summary>
    /// Serialises this structure back to <c>.PrjPcbStructure</c> text. Each record is emitted as
    /// <c>key=value</c> fields joined by <c>|</c>, one record per line terminated by
    /// <paramref name="newLine"/> (Altium uses CRLF, with no BOM and no trailing blank line).
    /// </summary>
    public string Serialize(string newLine = "\r\n")
    {
        var sb = new StringBuilder();
        foreach (var record in Records)
        {
            for (var i = 0; i < record.Fields.Count; i++)
            {
                if (i > 0)
                    sb.Append('|');
                sb.Append(record.Fields[i].Key).Append('=').Append(record.Fields[i].Value);
            }
            sb.Append(newLine);
        }
        return sb.ToString();
    }
}
