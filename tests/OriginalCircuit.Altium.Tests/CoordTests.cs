using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Extensions;

namespace OriginalCircuit.Altium.Tests;

public class CoordTests
{
    [Fact]
    public void FromMils_CreatesCorrectValue()
    {
        var coord = Coord.FromMils(100);
        Assert.Equal(100.0, coord.ToMils(), precision: 5);
    }

    [Fact]
    public void FromMm_CreatesCorrectValue()
    {
        var coord = Coord.FromMm(2.54);
        Assert.Equal(100.0, coord.ToMils(), precision: 3);
    }

    [Fact]
    public void Extension_Mm_CreatesCorrectValue()
    {
        var coord = 1.27.Mm();
        Assert.Equal(50.0, coord.ToMils(), precision: 3);
    }

    [Fact]
    public void Extension_Mils_CreatesCorrectValue()
    {
        var coord = 50.Mils();
        Assert.Equal(50.0, coord.ToMils(), precision: 5);
    }

    [Fact]
    public void Parse_WithMilSuffix_ParsesCorrectly()
    {
        Assert.True(Coord.TryParse("100mil", out var coord));
        Assert.Equal(100.0, coord.ToMils(), precision: 5);
    }

    [Fact]
    public void Parse_WithMmSuffix_ParsesCorrectly()
    {
        Assert.True(Coord.TryParse("2.54mm", out var coord));
        Assert.Equal(100.0, coord.ToMils(), precision: 3);
    }

    [Fact]
    public void Arithmetic_Addition_Works()
    {
        var a = 100.Mils();
        var b = 50.Mils();
        var sum = a + b;
        Assert.Equal(150.0, sum.ToMils(), precision: 5);
    }

    [Fact]
    public void Comparison_LessThan_Works()
    {
        var a = 100.Mils();
        var b = 200.Mils();
        Assert.True(a < b);
        Assert.False(b < a);
    }

    [Fact]
    public void FromMils_Overflow_ThrowsOverflowException()
    {
        Assert.Throws<OverflowException>(() => Coord.FromMils(1e15));
    }

    [Fact]
    public void FromMm_Overflow_ThrowsOverflowException()
    {
        Assert.Throws<OverflowException>(() => Coord.FromMm(1e15));
    }

    [Fact]
    public void FromInches_Overflow_ThrowsOverflowException()
    {
        Assert.Throws<OverflowException>(() => Coord.FromInches(1e15));
    }

    [Fact]
    public void FromInches_CreatesCorrectValue()
    {
        var coord = Coord.FromInches(1.0);
        Assert.Equal(1000.0, coord.ToMils(), precision: 5);
        Assert.Equal(1.0, coord.ToInches(), precision: 5);
    }

    [Fact]
    public void FromRaw_ToRaw_RoundTrips()
    {
        var coord = Coord.FromRaw(123456);
        Assert.Equal(123456, coord.ToRaw());
    }

    [Fact]
    public void Zero_HasZeroRawValue()
    {
        Assert.Equal(0, Coord.Zero.ToRaw());
        Assert.Equal(0.0, Coord.Zero.ToMils(), precision: 5);
    }

    [Fact]
    public void OneMil_HasCorrectValue()
    {
        Assert.Equal(1.0, Coord.OneMil.ToMils(), precision: 5);
        Assert.Equal(Coord.UnitsPerMil, Coord.OneMil.ToRaw());
    }

    [Fact]
    public void OneInch_HasCorrectValue()
    {
        Assert.Equal(1000.0, Coord.OneInch.ToMils(), precision: 5);
        Assert.Equal(Coord.UnitsPerInch, Coord.OneInch.ToRaw());
    }

    [Fact]
    public void OneMm_HasCorrectValue()
    {
        Assert.Equal(1.0, Coord.OneMm.ToMm(), precision: 3);
    }

    [Fact]
    public void Subtraction_Works()
    {
        var a = Coord.FromMils(200);
        var b = Coord.FromMils(50);
        Assert.Equal(150.0, (a - b).ToMils(), precision: 5);
    }

    [Fact]
    public void Negation_Works()
    {
        var a = Coord.FromMils(100);
        Assert.Equal(-100.0, (-a).ToMils(), precision: 5);
    }

    [Fact]
    public void Multiplication_ByScalar_Works()
    {
        var a = Coord.FromMils(50);
        Assert.Equal(150.0, (a * 3.0).ToMils(), precision: 5);
        Assert.Equal(150.0, (3.0 * a).ToMils(), precision: 5);
    }

    [Fact]
    public void Division_ByScalar_Works()
    {
        var a = Coord.FromMils(300);
        Assert.Equal(100.0, (a / 3.0).ToMils(), precision: 5);
    }

    [Fact]
    public void Division_ByCoord_ReturnsRatio()
    {
        var a = Coord.FromMils(300);
        var b = Coord.FromMils(100);
        Assert.Equal(3.0, a / b, precision: 5);
    }

    [Fact]
    public void NegativeValues_Work()
    {
        var coord = Coord.FromMils(-50);
        Assert.Equal(-50.0, coord.ToMils(), precision: 5);
        Assert.True(coord < Coord.Zero);
    }

    [Fact]
    public void CompareTo_OrdersCorrectly()
    {
        var small = Coord.FromMils(10);
        var large = Coord.FromMils(100);
        Assert.True(small.CompareTo(large) < 0);
        Assert.True(large.CompareTo(small) > 0);
        Assert.Equal(0, small.CompareTo(small));
    }

    [Fact]
    public void Equals_WorksCorrectly()
    {
        var a = Coord.FromMils(100);
        var b = Coord.FromMils(100);
        var c = Coord.FromMils(200);

        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals((object)42));
    }

    [Fact]
    public void GetHashCode_EqualCoords_SameHash()
    {
        var a = Coord.FromMils(100);
        var b = Coord.FromMils(100);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Abs_ReturnsAbsoluteValue()
    {
        var neg = Coord.FromMils(-50);
        var pos = Coord.FromMils(50);
        Assert.Equal(pos, Coord.Abs(neg));
        Assert.Equal(pos, Coord.Abs(pos));
    }

    [Fact]
    public void Min_ReturnsSmaller()
    {
        var a = Coord.FromMils(10);
        var b = Coord.FromMils(100);
        Assert.Equal(a, Coord.Min(a, b));
        Assert.Equal(a, Coord.Min(b, a));
    }

    [Fact]
    public void Max_ReturnsLarger()
    {
        var a = Coord.FromMils(10);
        var b = Coord.FromMils(100);
        Assert.Equal(b, Coord.Max(a, b));
        Assert.Equal(b, Coord.Max(b, a));
    }

    [Fact]
    public void TryParse_InchSuffix_ParsesCorrectly()
    {
        Assert.True(Coord.TryParse("1in", out var coord));
        Assert.Equal(1000.0, coord.ToMils(), precision: 5);
    }

    [Fact]
    public void TryParse_NoSuffix_DefaultsToMils()
    {
        Assert.True(Coord.TryParse("50", out var coord));
        Assert.Equal(50.0, coord.ToMils(), precision: 5);
    }

    [Fact]
    public void TryParse_InvalidInput_ReturnsFalse()
    {
        Assert.False(Coord.TryParse("abc", out _));
        Assert.False(Coord.TryParse("", out _));
    }

    [Fact]
    public void Parse_InvalidInput_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => Coord.Parse("not_a_number"));
    }

    [Fact]
    public void ToString_MilFormat_FormatsCorrectly()
    {
        var coord = Coord.FromMils(100);
        Assert.Equal("100mil", coord.ToString());
        Assert.Equal("100mil", coord.ToString("mil", null));
    }

    [Fact]
    public void ToString_MmFormat_FormatsCorrectly()
    {
        var coord = Coord.FromMm(2.54);
        Assert.Contains("mm", coord.ToString("mm", null));
    }

    [Fact]
    public void ToString_RawFormat_ReturnsRawInteger()
    {
        var coord = Coord.FromMils(100);
        Assert.Equal("1000000", coord.ToString("raw", null));
    }

    [Fact]
    public void ToString_UnknownFormat_ThrowsFormatException()
    {
        var coord = Coord.FromMils(100);
        Assert.Throws<FormatException>(() => coord.ToString("xyz", null));
    }

    [Fact]
    public void ComparisonOperators_Work()
    {
        var a = Coord.FromMils(10);
        var b = Coord.FromMils(20);

        Assert.True(a == Coord.FromMils(10));
        Assert.True(a != b);
        Assert.True(a <= b);
        Assert.True(a <= Coord.FromMils(10));
        Assert.True(b >= a);
        Assert.True(b >= Coord.FromMils(20));
        Assert.True(b > a);
    }

    [Fact]
    public void Multiplication_Overflow_ThrowsOverflowException()
    {
        var large = Coord.FromRaw(int.MaxValue);
        Assert.Throws<OverflowException>(() => large * 2.0);
    }
}
