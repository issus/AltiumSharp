using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Extensions;

namespace OriginalCircuit.Altium.Tests;

public class CoordGeometryTests
{
    // --- CoordPoint Tests ---

    [Fact]
    public void CoordPoint_Zero_HasZeroComponents()
    {
        Assert.Equal(Coord.Zero, CoordPoint.Zero.X);
        Assert.Equal(Coord.Zero, CoordPoint.Zero.Y);
    }

    [Fact]
    public void CoordPoint_Constructor_SetsComponents()
    {
        var p = new CoordPoint(100.Mils(), 200.Mils());
        Assert.Equal(100.0, p.X.ToMils(), precision: 5);
        Assert.Equal(200.0, p.Y.ToMils(), precision: 5);
    }

    [Fact]
    public void CoordPoint_Deconstruct_Works()
    {
        var p = new CoordPoint(10.Mils(), 20.Mils());
        var (x, y) = p;
        Assert.Equal(p.X, x);
        Assert.Equal(p.Y, y);
    }

    [Fact]
    public void CoordPoint_Addition_Works()
    {
        var a = new CoordPoint(10.Mils(), 20.Mils());
        var b = new CoordPoint(30.Mils(), 40.Mils());
        var sum = a + b;
        Assert.Equal(40.0, sum.X.ToMils(), precision: 5);
        Assert.Equal(60.0, sum.Y.ToMils(), precision: 5);
    }

    [Fact]
    public void CoordPoint_Subtraction_Works()
    {
        var a = new CoordPoint(100.Mils(), 200.Mils());
        var b = new CoordPoint(30.Mils(), 50.Mils());
        var diff = a - b;
        Assert.Equal(70.0, diff.X.ToMils(), precision: 5);
        Assert.Equal(150.0, diff.Y.ToMils(), precision: 5);
    }

    [Fact]
    public void CoordPoint_Negation_Works()
    {
        var p = new CoordPoint(10.Mils(), 20.Mils());
        var neg = -p;
        Assert.Equal(-10.0, neg.X.ToMils(), precision: 5);
        Assert.Equal(-20.0, neg.Y.ToMils(), precision: 5);
    }

    [Fact]
    public void CoordPoint_ScalarMultiplication_Works()
    {
        var p = new CoordPoint(10.Mils(), 20.Mils());
        var scaled = p * 3.0;
        Assert.Equal(30.0, scaled.X.ToMils(), precision: 5);
        Assert.Equal(60.0, scaled.Y.ToMils(), precision: 5);

        var scaled2 = 2.0 * p;
        Assert.Equal(20.0, scaled2.X.ToMils(), precision: 5);
    }

    [Fact]
    public void CoordPoint_Offset_ReturnsNewPoint()
    {
        var p = new CoordPoint(10.Mils(), 20.Mils());
        var offset = p.Offset(5.Mils(), 10.Mils());
        Assert.Equal(15.0, offset.X.ToMils(), precision: 5);
        Assert.Equal(30.0, offset.Y.ToMils(), precision: 5);
    }

    [Fact]
    public void CoordPoint_DistanceTo_CalculatesCorrectly()
    {
        var a = new CoordPoint(0.Mils(), 0.Mils());
        var b = new CoordPoint(300.Mils(), 400.Mils());
        Assert.Equal(500.0, a.DistanceTo(b), precision: 3);
    }

    [Fact]
    public void CoordPoint_Rotate_90Degrees()
    {
        var p = new CoordPoint(100.Mils(), 0.Mils());
        var rotated = p.Rotate(90);
        Assert.Equal(0.0, rotated.X.ToMils(), precision: 3);
        Assert.Equal(100.0, rotated.Y.ToMils(), precision: 3);
    }

    [Fact]
    public void CoordPoint_Equality_Works()
    {
        var a = new CoordPoint(10.Mils(), 20.Mils());
        var b = new CoordPoint(10.Mils(), 20.Mils());
        var c = new CoordPoint(10.Mils(), 30.Mils());

        Assert.True(a == b);
        Assert.False(a == c);
        Assert.True(a != c);
        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // --- CoordRect Tests ---

    [Fact]
    public void CoordRect_Empty_IsEmpty()
    {
        Assert.True(CoordRect.Empty.IsEmpty);
        Assert.Equal(Coord.Zero, CoordRect.Empty.Width);
        Assert.Equal(Coord.Zero, CoordRect.Empty.Height);
    }

    [Fact]
    public void CoordRect_Constructor_Normalizes()
    {
        // Pass corners in reverse order — should normalize
        var rect = new CoordRect(
            new CoordPoint(100.Mils(), 200.Mils()),
            new CoordPoint(0.Mils(), 0.Mils()));

        Assert.Equal(0.0, rect.Min.X.ToMils(), precision: 5);
        Assert.Equal(0.0, rect.Min.Y.ToMils(), precision: 5);
        Assert.Equal(100.0, rect.Max.X.ToMils(), precision: 5);
        Assert.Equal(200.0, rect.Max.Y.ToMils(), precision: 5);
    }

    [Fact]
    public void CoordRect_WidthHeight_Correct()
    {
        var rect = new CoordRect(0.Mils(), 0.Mils(), 100.Mils(), 200.Mils());
        Assert.Equal(100.0, rect.Width.ToMils(), precision: 5);
        Assert.Equal(200.0, rect.Height.ToMils(), precision: 5);
    }

    [Fact]
    public void CoordRect_Center_Correct()
    {
        var rect = new CoordRect(0.Mils(), 0.Mils(), 100.Mils(), 200.Mils());
        Assert.Equal(50.0, rect.Center.X.ToMils(), precision: 5);
        Assert.Equal(100.0, rect.Center.Y.ToMils(), precision: 5);
    }

    [Fact]
    public void CoordRect_FromCenter_CreatesCorrectRect()
    {
        var center = new CoordPoint(50.Mils(), 50.Mils());
        var rect = CoordRect.FromCenter(center, 100.Mils(), 60.Mils());

        Assert.Equal(0.0, rect.Min.X.ToMils(), precision: 5);
        Assert.Equal(20.0, rect.Min.Y.ToMils(), precision: 5);
        Assert.Equal(100.0, rect.Max.X.ToMils(), precision: 5);
        Assert.Equal(80.0, rect.Max.Y.ToMils(), precision: 5);
    }

    [Fact]
    public void CoordRect_Contains_Point()
    {
        var rect = new CoordRect(0.Mils(), 0.Mils(), 100.Mils(), 100.Mils());
        Assert.True(rect.Contains(new CoordPoint(50.Mils(), 50.Mils())));
        Assert.True(rect.Contains(new CoordPoint(0.Mils(), 0.Mils()))); // edge
        Assert.False(rect.Contains(new CoordPoint(150.Mils(), 50.Mils())));
    }

    [Fact]
    public void CoordRect_Intersects_Overlapping()
    {
        var a = new CoordRect(0.Mils(), 0.Mils(), 100.Mils(), 100.Mils());
        var b = new CoordRect(50.Mils(), 50.Mils(), 150.Mils(), 150.Mils());
        Assert.True(a.Intersects(b));
        Assert.True(b.Intersects(a));
    }

    [Fact]
    public void CoordRect_Intersects_NonOverlapping_ReturnsFalse()
    {
        var a = new CoordRect(0.Mils(), 0.Mils(), 10.Mils(), 10.Mils());
        var b = new CoordRect(100.Mils(), 100.Mils(), 200.Mils(), 200.Mils());
        Assert.False(a.Intersects(b));
    }

    [Fact]
    public void CoordRect_Union_ExpandsBounds()
    {
        var a = new CoordRect(0.Mils(), 0.Mils(), 50.Mils(), 50.Mils());
        var b = new CoordRect(25.Mils(), 25.Mils(), 100.Mils(), 100.Mils());
        var union = a.Union(b);

        Assert.Equal(0.0, union.Min.X.ToMils(), precision: 5);
        Assert.Equal(0.0, union.Min.Y.ToMils(), precision: 5);
        Assert.Equal(100.0, union.Max.X.ToMils(), precision: 5);
        Assert.Equal(100.0, union.Max.Y.ToMils(), precision: 5);
    }

    [Fact]
    public void CoordRect_Union_WithEmpty_ReturnsOther()
    {
        var rect = new CoordRect(10.Mils(), 10.Mils(), 100.Mils(), 100.Mils());
        Assert.Equal(rect, CoordRect.Empty.Union(rect));
        Assert.Equal(rect, rect.Union(CoordRect.Empty));
    }

    [Fact]
    public void CoordRect_StaticUnion_Multiple()
    {
        var rects = new[]
        {
            new CoordRect(0.Mils(), 0.Mils(), 10.Mils(), 10.Mils()),
            new CoordRect(50.Mils(), 50.Mils(), 60.Mils(), 60.Mils()),
            new CoordRect(90.Mils(), 90.Mils(), 100.Mils(), 100.Mils()),
        };
        var union = CoordRect.Union(rects);
        Assert.Equal(0.0, union.Min.X.ToMils(), precision: 5);
        Assert.Equal(100.0, union.Max.X.ToMils(), precision: 5);
    }

    [Fact]
    public void CoordRect_Intersect_ReturnsOverlapRegion()
    {
        var a = new CoordRect(0.Mils(), 0.Mils(), 100.Mils(), 100.Mils());
        var b = new CoordRect(50.Mils(), 50.Mils(), 150.Mils(), 150.Mils());
        var intersection = a.Intersect(b);

        Assert.Equal(50.0, intersection.Min.X.ToMils(), precision: 5);
        Assert.Equal(50.0, intersection.Min.Y.ToMils(), precision: 5);
        Assert.Equal(100.0, intersection.Max.X.ToMils(), precision: 5);
        Assert.Equal(100.0, intersection.Max.Y.ToMils(), precision: 5);
    }

    [Fact]
    public void CoordRect_Intersect_NoOverlap_ReturnsEmpty()
    {
        var a = new CoordRect(0.Mils(), 0.Mils(), 10.Mils(), 10.Mils());
        var b = new CoordRect(100.Mils(), 100.Mils(), 200.Mils(), 200.Mils());
        var intersection = a.Intersect(b);
        Assert.Equal(CoordRect.Empty, intersection);
    }

    [Fact]
    public void CoordRect_Inflate_ExpandsAllSides()
    {
        var rect = new CoordRect(10.Mils(), 10.Mils(), 90.Mils(), 90.Mils());
        var inflated = rect.Inflate(10.Mils());
        Assert.Equal(0.0, inflated.Min.X.ToMils(), precision: 5);
        Assert.Equal(100.0, inflated.Max.X.ToMils(), precision: 5);
    }

    [Fact]
    public void CoordRect_Equality_Works()
    {
        var a = new CoordRect(0.Mils(), 0.Mils(), 100.Mils(), 100.Mils());
        var b = new CoordRect(0.Mils(), 0.Mils(), 100.Mils(), 100.Mils());
        var c = new CoordRect(0.Mils(), 0.Mils(), 50.Mils(), 50.Mils());

        Assert.True(a == b);
        Assert.True(a != c);
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
