using OriginalCircuit.Altium.Connectivity;
using OriginalCircuit.Altium.Connectivity.Internal;
using Xunit;

namespace OriginalCircuit.Altium.Tests.Connectivity;

/// <summary>
/// Unit tests for best-effort directive value parsing in <see cref="NetIntentClassifier"/> — the unit
/// suffixes, scientific notation, and the length-vs-delay distinction that the integration tests don't
/// exercise.
/// </summary>
public class NetIntentClassifierTests
{
    private static NetIntent Classify(string name, string value) =>
        NetIntentClassifier.Classify(name, value, NetIntentSource.ParameterSet, null);

    [Theory]
    [InlineData("Impedance", "50ohm", 50.0)]
    [InlineData("Impedance", "100", 100.0)]
    [InlineData("Impedance", "4.7kohm", 4700.0)]
    [InlineData("Impedance", "4.7k", 4700.0)]
    public void Parses_Impedance_Ohms(string n, string v, double ohms)
    {
        var i = Classify(n, v);
        Assert.Equal(NetIntentKind.Impedance, i.Kind);
        Assert.Equal(ohms, i.Ohms);
    }

    [Theory]
    [InlineData("Frequency", "100MHz", 100e6)]
    [InlineData("Frequency", "2.5GHz", 2.5e9)]
    [InlineData("Frequency", "50e6", 50e6)]   // scientific notation, bare hertz
    public void Parses_Frequency_Hz(string n, string v, double hz)
    {
        var i = Classify(n, v);
        Assert.Equal(NetIntentKind.Frequency, i.Kind);
        Assert.Equal(hz, i.Hz);
    }

    [Theory]
    [InlineData("Voltage", "3.3V", 3.3)]
    [InlineData("Voltage", "250mV", 0.25)]
    [InlineData("Voltage", "12kV", 12000.0)]
    [InlineData("Voltage", "5", 5.0)]    // bare value
    [InlineData("Voltage", "5m", 5.0)]    // a stray 'm' is NOT silently treated as milli
    public void Parses_Voltage_Volts(string n, string v, double volts)
    {
        var i = Classify(n, v);
        Assert.Equal(NetIntentKind.Voltage, i.Kind);
        Assert.Equal(volts, i.Volts);
    }

    [Theory]
    [InlineData("MatchedLength", "5mm", 5.0)]
    [InlineData("MatchedLength", "100mil", 2.54)]
    [InlineData("Length", "1in", 25.4)]
    public void Parses_Length_AsMillimetres(string n, string v, double mm)
    {
        var i = Classify(n, v);
        Assert.Equal(NetIntentKind.LengthMatch, i.Kind);
        Assert.NotNull(i.LengthMm);
        Assert.Equal(mm, i.LengthMm!.Value, 3);
        Assert.Null(i.DelaySeconds);
    }

    [Theory]
    [InlineData("Delay", "250ps", 250e-12)]
    [InlineData("Delay", "1.5ns", 1.5e-9)]
    [InlineData("MatchedLength", "2ns", 2e-9)]   // a time value on a length-match directive is a delay
    public void Parses_Delay_AsSeconds_NotLength(string n, string v, double seconds)
    {
        var i = Classify(n, v);
        Assert.Equal(NetIntentKind.LengthMatch, i.Kind);
        Assert.NotNull(i.DelaySeconds);
        Assert.Equal(seconds * 1e9, i.DelaySeconds!.Value * 1e9, 6); // compare in ns to avoid ULP noise
        Assert.Null(i.LengthMm);   // a delay must NOT be stored as millimetres
    }

    [Fact]
    public void Scientific_Notation_Is_Parsed()
    {
        Assert.Equal(50.0, Classify("Impedance", "5e1ohm").Ohms);
        Assert.Equal(0.05, Classify("Voltage", "50e-3V").Volts!.Value, 6);
    }

    [Theory]
    [InlineData("DiffPair", "USB_DP,USB_DM", "USB_DP", "USB_DM")]
    [InlineData("DifferentialPair", "A / B", "A", "B")]
    public void Parses_DiffPair(string n, string v, string p, string q)
    {
        var i = Classify(n, v);
        Assert.Equal(NetIntentKind.DiffPair, i.Kind);
        Assert.Equal((p, q), i.Pair);
    }

    [Fact]
    public void Preserves_Raw_When_Unparseable()
    {
        var i = Classify("Impedance", "");
        Assert.Equal(NetIntentKind.Impedance, i.Kind);
        Assert.Null(i.Ohms);
        Assert.Equal("", i.RawValue);
        Assert.Equal("Impedance", i.RawName);
    }
}
