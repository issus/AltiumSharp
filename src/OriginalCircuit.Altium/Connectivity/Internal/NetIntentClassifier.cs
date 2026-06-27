using System.Globalization;
using System.Text.RegularExpressions;

namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>
/// Classifies a raw directive (parameter <c>Name</c>/<c>Value</c>) into a typed <see cref="NetIntent"/>,
/// parsing common unit suffixes best-effort while always preserving the raw name and value.
/// </summary>
internal static partial class NetIntentClassifier
{
    [GeneratedRegex(@"(?<num>[-+]?\d*\.?\d+)\s*(?<unit>[a-zA-ZµΩ]*)", RegexOptions.CultureInvariant)]
    private static partial Regex NumberRegex();

    public static NetIntent Classify(string name, string value, NetIntentSource source, object? primitive)
    {
        // Normalise the directive name for matching: lower-case, strip spaces/underscores/hyphens so
        // "Net Class", "net_class" and "NetClass" all match. The raw name is preserved on the intent.
        var n = new string(name.ToLowerInvariant().Where(c => c is not (' ' or '_' or '-')).ToArray());
        value ??= string.Empty;

        if (Contains(n, "impedance"))
            return new NetIntent(NetIntentKind.Impedance, name, value, source, primitive) { Ohms = ParseOhms(value) };

        if (Contains(n, "frequency") || n == "freq" || Contains(n, "highspeed"))
            return new NetIntent(NetIntentKind.Frequency, name, value, source, primitive) { Hz = ParseHz(value) };

        if (Contains(n, "voltage") || n == "netvoltage")
            return new NetIntent(NetIntentKind.Voltage, name, value, source, primitive) { Volts = ParseScalar(value, "v") };

        if (Contains(n, "differentialpair") || Contains(n, "diffpair") || Contains(n, "differential"))
            return new NetIntent(NetIntentKind.DiffPair, name, value, source, primitive) { Pair = ParsePair(value) };

        if (Contains(n, "netclass") || Contains(n, "classname") || n == "class")
            return new NetIntent(NetIntentKind.NetClass, name, value, source, primitive)
            { NetClass = string.IsNullOrWhiteSpace(value) ? null : value };

        if (Contains(n, "matchlength") || Contains(n, "matchedlength") || Contains(n, "lengthmatch")
            || Contains(n, "length") || Contains(n, "delay"))
            return new NetIntent(NetIntentKind.LengthMatch, name, value, source, primitive) { LengthMm = ParseLengthMm(value) };

        if (Contains(n, "rule") || Contains(n, "directive"))
            return new NetIntent(NetIntentKind.PcbRule, name, value, source, primitive);

        return new NetIntent(NetIntentKind.Other, name, value, source, primitive);
    }

    private static bool Contains(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.Ordinal);

    private static (double Num, string Unit)? ParseNumberUnit(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var m = NumberRegex().Match(value);
        if (!m.Success)
            return null;
        if (!double.TryParse(m.Groups["num"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
            return null;
        return (num, m.Groups["unit"].Value.ToLowerInvariant());
    }

    private static double? ParseOhms(string value)
    {
        var p = ParseNumberUnit(value);
        if (p is null) return null;
        var (num, unit) = p.Value;
        return unit switch
        {
            "kohm" or "kω" or "k" => num * 1_000,
            "mohm" or "mω" => num * 1_000_000,
            _ => num, // ohm, Ω, or bare
        };
    }

    private static double? ParseHz(string value)
    {
        var p = ParseNumberUnit(value);
        if (p is null) return null;
        var (num, unit) = p.Value;
        return unit switch
        {
            "ghz" => num * 1e9,
            "mhz" => num * 1e6,
            "khz" => num * 1e3,
            _ => num,
        };
    }

    private static double? ParseScalar(string value, string baseUnit)
    {
        var p = ParseNumberUnit(value);
        if (p is null) return null;
        var (num, unit) = p.Value;
        if (unit.StartsWith('m') && unit != baseUnit) return num / 1000.0; // mV
        if (unit.StartsWith('k')) return num * 1000.0;
        return num;
    }

    private static double? ParseLengthMm(string value)
    {
        var p = ParseNumberUnit(value);
        if (p is null) return null;
        var (num, unit) = p.Value;
        return unit switch
        {
            "mil" or "mils" => num * 0.0254,
            "in" or "inch" => num * 25.4,
            "um" or "µm" => num / 1000.0,
            _ => num, // mm or bare
        };
    }

    private static (string, string)? ParsePair(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var parts = value.Split(new[] { ',', ';', '/', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? (parts[0], parts[1]) : null;
    }
}
