using OriginalCircuit.Altium.Models.Sch;
using Xunit;

namespace OriginalCircuit.Altium.Tests.Connectivity;

/// <summary>Unit tests for parsing the Altium <c>Repeat(...)</c> multi-channel directive.</summary>
public class RepeatInfoTests
{
    [Theory]
    [InlineData("Repeat(CH,1,4)", "CH", 1, 4, 4)]
    [InlineData("Repeat(ADC, 1, 8)", "ADC", 1, 8, 8)]
    [InlineData("repeat(Bank,0,3)", "Bank", 0, 3, 4)]
    [InlineData("Repeat(X, 5, 5)", "X", 5, 5, 1)]
    [InlineData("Repeat(Rev,4,1)", "Rev", 1, 4, 4)] // tolerate reversed bounds
    public void Parse_Recognises_Repeat(string text, string name, int first, int last, int count)
    {
        var r = RepeatInfo.Parse(text);
        Assert.True(r.IsRepeated);
        Assert.Equal(name, r.ChannelName);
        Assert.Equal(first, r.FirstInstance);
        Assert.Equal(last, r.LastInstance);
        Assert.Equal(count, r.InstanceCount);
    }

    [Theory]
    [InlineData("U_Analogue")]
    [InlineData("U1")]
    [InlineData("")]
    [InlineData(null)]
    public void Parse_Plain_Designator_Is_Not_Repeated(string? text)
    {
        var r = RepeatInfo.Parse(text);
        Assert.False(r.IsRepeated);
        Assert.Equal(1, r.InstanceCount);
    }
}
