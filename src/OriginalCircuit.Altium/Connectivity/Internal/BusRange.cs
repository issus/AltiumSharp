using System.Globalization;
using System.Text.RegularExpressions;

namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>
/// Parses and expands Altium ranged bus / net-label names such as <c>D[0..7]</c>, <c>D[7..0]</c> or
/// <c>ADDR[0..15]A</c> into their ordered member net names (<c>D0..D7</c> etc.).
/// </summary>
internal static partial class BusRange
{
    [GeneratedRegex(@"^(?<prefix>.*?)\[\s*(?<from>\d+)\s*\.\.\s*(?<to>\d+)\s*\](?<suffix>.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex RangeRegex();

    /// <summary>
    /// Attempts to parse a ranged bus name. On success <paramref name="members"/> holds the expanded
    /// member net names in declaration order (descending when <c>from &gt; to</c>).
    /// </summary>
    public static bool TryExpand(string? name, out IReadOnlyList<string> members)
    {
        members = Array.Empty<string>();
        if (string.IsNullOrEmpty(name))
            return false;

        var m = RangeRegex().Match(name);
        if (!m.Success)
            return false;

        if (!int.TryParse(m.Groups["from"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var from) ||
            !int.TryParse(m.Groups["to"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var to))
            return false;

        // Guard against absurd ranges.
        if (Math.Abs((long)from - to) > 100_000)
            return false;

        var prefix = m.Groups["prefix"].Value;
        var suffix = m.Groups["suffix"].Value;

        var list = new List<string>();
        if (from <= to)
            for (var i = from; i <= to; i++)
                list.Add($"{prefix}{i}{suffix}");
        else
            for (var i = from; i >= to; i--)
                list.Add($"{prefix}{i}{suffix}");

        members = list;
        return true;
    }

    /// <summary>Whether the name is a ranged bus name (<c>X[a..b]</c>).</summary>
    public static bool IsRanged(string? name) => !string.IsNullOrEmpty(name) && RangeRegex().IsMatch(name);
}
