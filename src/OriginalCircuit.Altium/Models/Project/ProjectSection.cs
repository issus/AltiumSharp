using System.Globalization;

namespace OriginalCircuit.Altium.Models.Project;

/// <summary>
/// A single <c>[Name]</c> section of a <c>.PrjPcb</c> project file, holding its
/// <c>key=value</c> entries in their original order.
/// </summary>
/// <remarks>
/// A project file is a flat INI-style document. <see cref="ProjectSection"/> is the
/// raw, byte-faithful representation of one section: the writer emits these sections
/// verbatim, so preserving entry order (and any unknown keys) here is what makes a
/// project round-trip exactly. The strongly-typed wrappers
/// (<see cref="ProjectDocument"/>, <see cref="ProjectConfiguration"/>, etc.) are thin
/// views over a <see cref="ProjectSection"/>; reading or writing a typed property goes
/// straight through to <see cref="Entries"/>.
/// </remarks>
public sealed class ProjectSection
{
    /// <summary>Creates an empty section with the given name (e.g. <c>"Design"</c> or <c>"Document1"</c>).</summary>
    public ProjectSection(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>The section name as written between the brackets, without the brackets themselves.</summary>
    public string Name { get; set; }

    /// <summary>
    /// The <c>key=value</c> entries of this section, in file order. Duplicate keys are
    /// permitted and preserved; values are stored exactly as written (including any
    /// leading/trailing spaces) so the file round-trips byte-for-byte.
    /// </summary>
    public List<KeyValuePair<string, string>> Entries { get; } = new();

    /// <summary>Returns the value of the first entry with the given key, or <c>null</c> when absent. Case-insensitive.</summary>
    public string? Get(string key)
    {
        foreach (var entry in Entries)
            if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
                return entry.Value;
        return null;
    }

    /// <summary>Returns the value of the first matching entry, or <paramref name="defaultValue"/> when absent.</summary>
    public string GetString(string key, string defaultValue = "") => Get(key) ?? defaultValue;

    /// <summary>Parses the value of the given key as an integer, returning <paramref name="defaultValue"/> when absent or unparseable.</summary>
    public int GetInt(string key, int defaultValue = 0) =>
        int.TryParse(Get(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : defaultValue;

    /// <summary>
    /// Parses the value of the given key as a boolean. Altium writes project booleans as
    /// <c>1</c>/<c>0</c>; <c>true</c>/<c>false</c> are also accepted for robustness.
    /// </summary>
    public bool GetBool(string key, bool defaultValue = false)
    {
        var value = Get(key);
        if (string.IsNullOrEmpty(value))
            return defaultValue;
        if (value == "1") return true;
        if (value == "0") return false;
        return bool.TryParse(value, out var b) ? b : defaultValue;
    }

    /// <summary>Parses the value of the given key as a GUID, or <c>null</c> when absent/blank/invalid.</summary>
    public Guid? GetGuid(string key) =>
        Guid.TryParse(Get(key), out var g) ? g : null;

    /// <summary><c>true</c> when the section contains at least one entry with the given key.</summary>
    public bool Contains(string key)
    {
        foreach (var entry in Entries)
            if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    /// <summary>
    /// Sets the value of <paramref name="key"/>: updates the first existing entry in place
    /// (preserving its position), or appends a new entry when the key is absent.
    /// </summary>
    public void Set(string key, string? value)
    {
        value ??= string.Empty;
        for (var i = 0; i < Entries.Count; i++)
        {
            if (string.Equals(Entries[i].Key, key, StringComparison.OrdinalIgnoreCase))
            {
                Entries[i] = new KeyValuePair<string, string>(Entries[i].Key, value);
                return;
            }
        }
        Entries.Add(new KeyValuePair<string, string>(key, value));
    }

    /// <summary>Sets an integer value (see <see cref="Set(string, string)"/>).</summary>
    public void SetInt(string key, int value) => Set(key, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>Sets a boolean value using Altium's <c>1</c>/<c>0</c> convention.</summary>
    public void SetBool(string key, bool value) => Set(key, value ? "1" : "0");

    /// <summary>Appends an entry without checking for an existing key (use for ordered/indexed arrays).</summary>
    public void Add(string key, string? value) =>
        Entries.Add(new KeyValuePair<string, string>(key, value ?? string.Empty));

    /// <summary>Removes every entry with the given key. Returns the number removed.</summary>
    public int Remove(string key) =>
        Entries.RemoveAll(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc/>
    public override string ToString() => $"[{Name}] ({Entries.Count} entries)";
}
