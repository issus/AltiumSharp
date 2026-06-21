using System.Numerics;
using OriginalCircuit.Altium.Rendering.Gltf.Geometry;

namespace OriginalCircuit.Altium.Tests;

/// <summary>
/// Tests for the glTF rendering geometry toolkit: the polygon-with-holes triangulator, the shape
/// generators, and the mesh buffer. Triangulation is validated by area conservation — a correct
/// tiling has total triangle area equal to the polygon's area.
/// </summary>
public class GltfGeometryTests
{
    private static double PolygonArea(IReadOnlyList<Vec2> ring)
    {
        double a = 0;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            a += (ring[j].X * ring[i].Y) - (ring[i].X * ring[j].Y);
        return Math.Abs(a) / 2.0;
    }

    private static double TriangulatedArea(IReadOnlyList<Vec2> outer, IReadOnlyList<IReadOnlyList<Vec2>>? holes = null)
    {
        var tris = Triangulator.Triangulate(outer, holes);
        var verts = new List<Vec2>(outer);
        if (holes is not null)
            foreach (var h in holes)
                if (h.Count >= 3) verts.AddRange(h);

        double area = 0;
        for (int t = 0; t < tris.Count; t += 3)
        {
            Vec2 a = verts[tris[t]], b = verts[tris[t + 1]], c = verts[tris[t + 2]];
            area += Math.Abs(((b.X - a.X) * (c.Y - a.Y)) - ((c.X - a.X) * (b.Y - a.Y))) / 2.0;
        }
        return area;
    }

    [Fact]
    public void Triangulate_Square_ProducesTwoTrianglesCoveringArea()
    {
        var square = new List<Vec2> { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
        var tris = Triangulator.Triangulate(square);

        Assert.Equal(6, tris.Count); // two triangles
        Assert.Equal(100.0, TriangulatedArea(square), 6);
    }

    [Fact]
    public void Triangulate_SquareWithHole_ConservesArea()
    {
        var outer = new List<Vec2> { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
        var hole = new List<Vec2> { new(3, 3), new(3, 7), new(7, 7), new(7, 3) }; // opposite winding
        var holes = new List<IReadOnlyList<Vec2>> { hole };

        Assert.Equal(100.0 - 16.0, TriangulatedArea(outer, holes), 4);
    }

    [Fact]
    public void Triangulate_ConcaveLShape_ConservesArea()
    {
        // An L-shape: a 10x10 square with a 6x6 bite taken out of the top-right corner. Area = 64.
        var l = new List<Vec2>
        {
            new(0, 0), new(10, 0), new(10, 4), new(4, 4), new(4, 10), new(0, 10),
        };
        Assert.Equal(64.0, TriangulatedArea(l), 4);
    }

    [Fact]
    public void Triangulate_CircleApproximation_TilesPolygonAndApproximatesArea()
    {
        var circle = Shapes.Circle(new Vec2(5, 5), 4, segments: 128);

        // The triangulator must tile the polygon exactly (its area equals the ring's shoelace area)...
        Assert.Equal(PolygonArea(circle), TriangulatedArea(circle), 6);
        // ...and a 128-gon is a good circle approximation (within 0.1% of π r²).
        Assert.InRange(PolygonArea(circle), Math.PI * 16.0 * 0.999, Math.PI * 16.0);
    }

    [Fact]
    public void Capsule_AreaMatchesRectanglePlusCircle()
    {
        var cap = Shapes.Capsule(new Vec2(0, 0), new Vec2(10, 0), width: 2, capSegments: 64);
        // stadium = rectangle (length 10 × width 2) + a full circle of radius 1.
        double expected = (10.0 * 2.0) + (Math.PI * 1.0);
        Assert.Equal(expected, TriangulatedArea(cap), 1);
    }

    [Fact]
    public void MeshBuffer_AddPrism_HasCapsWallsAndUnitNormals()
    {
        var square = new List<Vec2> { new(0, 0), new(4, 0), new(4, 4), new(0, 4) };
        var mesh = new MeshBuffer();
        mesh.AddPrism(square, holes: null, z0: 0.0, z1: 1.0);

        Assert.False(mesh.IsEmpty);
        // 2 caps × 2 tris + 4 walls × 2 tris = 12 triangles.
        Assert.Equal(12, mesh.TriangleCount);

        float minZ = mesh.Positions.Min(p => p.Z), maxZ = mesh.Positions.Max(p => p.Z);
        Assert.Equal(0f, minZ, 4);
        Assert.Equal(1f, maxZ, 4);

        foreach (var n in mesh.Normals)
            Assert.Equal(1.0, n.Length(), 3);
    }

    [Fact]
    public void MeshBuffer_AddFlatPolygon_FaceUpNormalsPointUp()
    {
        var square = new List<Vec2> { new(0, 0), new(4, 0), new(4, 4), new(0, 4) };
        var mesh = new MeshBuffer();
        mesh.AddFlatPolygon(square, holes: null, z: 0.5, faceUp: true);

        Assert.All(mesh.Positions, p => Assert.Equal(0.5f, p.Z, 4));
        Assert.All(mesh.Normals, n => Assert.Equal(new Vector3(0, 0, 1), n));
    }
}
