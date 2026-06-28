using OriginalCircuit.Altium.Connectivity.Internal;
using Xunit;

namespace OriginalCircuit.Altium.Tests.Connectivity;

/// <summary>Unit tests for ranged bus / net-label expansion (<see cref="BusRange"/>).</summary>
public class BusRangeTests
{
    [Theory]
    [InlineData("D[0..3]", new[] { "D0", "D1", "D2", "D3" })]
    [InlineData("D[3..0]", new[] { "D3", "D2", "D1", "D0" })]            // reversed range, declaration order
    [InlineData("ADDR[0..2]A", new[] { "ADDR0A", "ADDR1A", "ADDR2A" })] // prefix + suffix
    [InlineData("X[5..5]", new[] { "X5" })]                             // single member
    [InlineData("D[ 0 .. 2 ]", new[] { "D0", "D1", "D2" })]             // whitespace tolerant
    public void Expands_Members(string name, string[] expected)
    {
        Assert.True(BusRange.TryExpand(name, out var members));
        Assert.Equal(expected, members);
        Assert.True(BusRange.IsRanged(name));
    }

    [Theory]
    [InlineData("D0")]
    [InlineData("NET_FOO")]
    [InlineData("")]
    public void Non_Ranged_Names_Do_Not_Expand(string? name)
    {
        Assert.False(BusRange.TryExpand(name, out var members));
        Assert.Empty(members);
        Assert.False(BusRange.IsRanged(name));
    }
}
