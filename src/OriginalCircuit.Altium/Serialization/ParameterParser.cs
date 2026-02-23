using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace OriginalCircuit.Altium.Serialization;

/// <summary>
/// Zero-allocation, single-pass parser for Altium parameter strings.
/// </summary>
/// <remarks>
/// Parameters are in the format: |KEY1=VALUE1|KEY2=VALUE2|...
/// This is a ref struct to enable stack-only semantics and prevent heap allocation.
/// </remarks>
internal ref struct ParameterParser
{
    private const char EntrySeparator = '|';
    private const char NestedEntrySeparator = '`';
    private const char KeyValueSeparator = '=';
    private const string Utf8Prefix = "%UTF8%";

    private ReadOnlySpan<char> _remaining;
    private readonly int _level;

    /// <summary>
    /// Gets the current parameter name.
    /// </summary>
    public ReadOnlySpan<char> CurrentName { get; private set; }

    /// <summary>
    /// Gets the current parameter value.
    /// </summary>
    public ReadOnlySpan<char> CurrentValue { get; private set; }

    /// <summary>
    /// Gets whether the current parameter has the UTF-8 prefix.
    /// </summary>
    public bool CurrentIsUtf8 { get; private set; }

    /// <summary>
    /// Creates a new parser for the given parameter data.
    /// </summary>
    /// <param name="data">The parameter string to parse.</param>
    /// <param name="level">Nesting level (0 for top-level, 1 for nested).</param>
    public ParameterParser(ReadOnlySpan<char> data, int level = 0)
    {
        _remaining = data;
        _level = level;
        CurrentName = default;
        CurrentValue = default;
        CurrentIsUtf8 = false;
    }

    /// <summary>
    /// Gets the entry separator for the current nesting level.
    /// </summary>
    private char Separator => _level == 0 ? EntrySeparator : NestedEntrySeparator;

    /// <summary>
    /// Moves to the next parameter.
    /// </summary>
    /// <returns>True if another parameter exists, false if parsing is complete.</returns>
    public bool MoveNext()
    {
        while (!_remaining.IsEmpty)
        {
            // Skip leading separators
            if (_remaining[0] == Separator)
            {
                _remaining = _remaining.Slice(1);
                continue;
            }

            // Find next separator or end
            var separatorIndex = _remaining.IndexOf(Separator);
            var entry = separatorIndex >= 0
                ? _remaining.Slice(0, separatorIndex)
                : _remaining;

            // Advance past this entry
            _remaining = separatorIndex >= 0
                ? _remaining.Slice(separatorIndex + 1)
                : default;

            // Trim trailing whitespace (especially \r\n)
            entry = entry.TrimEnd();

            if (entry.IsEmpty)
                continue;

            // Split into key=value
            var equalsIndex = entry.IndexOf(KeyValueSeparator);
            if (equalsIndex < 0)
            {
                // Value-only entry (rare, but handle gracefully)
                CurrentName = default;
                CurrentValue = entry;
                CurrentIsUtf8 = false;
                return true;
            }

            CurrentName = entry.Slice(0, equalsIndex);
            CurrentValue = entry.Slice(equalsIndex + 1);

            // Check for UTF-8 prefix
            if (CurrentName.StartsWith(Utf8Prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                CurrentName = CurrentName.Slice(Utf8Prefix.Length);
                CurrentIsUtf8 = true;
            }
            else
            {
                CurrentIsUtf8 = false;
            }

            return true;
        }

        CurrentName = default;
        CurrentValue = default;
        CurrentIsUtf8 = false;
        return false;
    }

    /// <summary>
    /// Gets the enumerator for foreach support.
    /// </summary>
    public ParameterParser GetEnumerator() => this;

    /// <summary>
    /// Gets the current entry (for foreach support).
    /// </summary>
    public readonly ParameterEntry Current => new(CurrentName, CurrentValue, CurrentIsUtf8);
}

/// <summary>
/// Represents a single parameter entry during parsing.
/// </summary>
internal readonly ref struct ParameterEntry
{
    /// <summary>
    /// The parameter name/key.
    /// </summary>
    public ReadOnlySpan<char> Name { get; }

    /// <summary>
    /// The parameter value.
    /// </summary>
    public ReadOnlySpan<char> Value { get; }

    /// <summary>
    /// Whether this parameter uses UTF-8 encoding.
    /// </summary>
    public bool IsUtf8 { get; }

    internal ParameterEntry(ReadOnlySpan<char> name, ReadOnlySpan<char> value, bool isUtf8)
    {
        Name = name;
        Value = value;
        IsUtf8 = isUtf8;
    }

    /// <summary>
    /// Gets the value as a string.
    /// </summary>
    public string GetString() => Value.ToString();

    /// <summary>
    /// Tries to get the value as an integer.
    /// </summary>
    public bool TryGetInt32(out int value) =>
        int.TryParse(Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    /// <summary>
    /// Gets the value as an integer, or a default value if parsing fails.
    /// </summary>
    public int GetInt32OrDefault(int defaultValue = 0) =>
        TryGetInt32(out var value) ? value : defaultValue;

    /// <summary>
    /// Tries to get the value as a double.
    /// </summary>
    public bool TryGetDouble(out double value) =>
        double.TryParse(Value, NumberStyles.Any, CultureInfo.InvariantCulture, out value);

    /// <summary>
    /// Gets the value as a double, or a default value if parsing fails.
    /// </summary>
    public double GetDoubleOrDefault(double defaultValue = 0.0) =>
        TryGetDouble(out var value) ? value : defaultValue;

    /// <summary>
    /// Gets the value as a boolean.
    /// </summary>
    /// <remarks>
    /// Recognizes: T, TRUE, F, FALSE (case-insensitive).
    /// Empty/null is treated as false.
    /// </remarks>
    public bool GetBool()
    {
        if (Value.IsEmpty)
            return false;

        if (Value.Length == 1)
        {
            return Value[0] == 'T' || Value[0] == 't';
        }

        return Value.Equals("TRUE".AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if this entry matches a given parameter name (case-insensitive).
    /// </summary>
    public bool NameEquals(ReadOnlySpan<char> name) =>
        Name.Equals(name, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Extension methods for working with parameters.
/// </summary>
internal static class ParameterParserExtensions
{
    /// <summary>
    /// Creates a parser for the given string.
    /// </summary>
    public static ParameterParser ParseParameters(this string data, int level = 0) =>
        new(data.AsSpan(), level);

    /// <summary>
    /// Creates a parser for the given span.
    /// </summary>
    public static ParameterParser ParseParameters(this ReadOnlySpan<char> data, int level = 0) =>
        new(data, level);

    /// <summary>
    /// Tries to find a parameter by name and returns its value.
    /// </summary>
    /// <remarks>
    /// This iterates through all parameters, so for repeated lookups consider
    /// using a dictionary-based approach or caching results.
    /// </remarks>
    public static bool TryGetParameter(this string data, string name, out string? value)
    {
        var parser = new ParameterParser(data.AsSpan());
        while (parser.MoveNext())
        {
            if (parser.CurrentName.Equals(name.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                value = parser.CurrentValue.ToString();
                return true;
            }
        }

        value = null;
        return false;
    }
}
