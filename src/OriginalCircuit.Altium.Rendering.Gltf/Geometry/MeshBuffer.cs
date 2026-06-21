using System.Numerics;

namespace OriginalCircuit.Altium.Rendering.Gltf.Geometry;

/// <summary>
/// Accumulates triangle geometry (positions, per-vertex normals, indices) for one glTF mesh, in
/// board space: X/Y are Altium board millimetres and Z is the layer height. Provides the building
/// blocks the renderer needs — flat filled polygons, double-sided sheets, extruded prisms and
/// cylinder walls — all honouring polygon holes.
/// </summary>
internal sealed class MeshBuffer
{
    public List<Vector3> Positions { get; } = [];
    public List<Vector3> Normals { get; } = [];
    public List<int> Indices { get; } = [];

    public bool IsEmpty => Indices.Count == 0;
    public int TriangleCount => Indices.Count / 3;

    private int AddVertex(Vector3 p, Vector3 n)
    {
        Positions.Add(p);
        Normals.Add(n);
        return Positions.Count - 1;
    }

    /// <summary>Adds a single triangle, computing a flat face normal from its winding.</summary>
    public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 n = Vector3.Cross(b - a, c - a);
        n = n.LengthSquared() > 1e-20f ? Vector3.Normalize(n) : new Vector3(0, 0, 1);
        AddTriangle(a, b, c, n);
    }

    private void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 n)
    {
        Indices.Add(AddVertex(a, n));
        Indices.Add(AddVertex(b, n));
        Indices.Add(AddVertex(c, n));
    }

    /// <summary>Appends an already-triangulated mesh (e.g. a tessellated component body).</summary>
    public void Append(IReadOnlyList<Vector3> positions, IReadOnlyList<Vector3> normals, IReadOnlyList<int> indices)
    {
        int baseIndex = Positions.Count;
        for (int i = 0; i < positions.Count; i++)
        {
            Positions.Add(positions[i]);
            Normals.Add(i < normals.Count ? normals[i] : new Vector3(0, 0, 1));
        }
        foreach (int idx in indices) Indices.Add(baseIndex + idx);
    }

    /// <summary>
    /// Fills a (possibly holed) polygon as a flat horizontal cap at height <paramref name="z"/>.
    /// When <paramref name="faceUp"/> the cap faces +Z; otherwise −Z (winding is flipped to match).
    /// </summary>
    public void AddFlatPolygon(IReadOnlyList<Vec2> outer, IReadOnlyList<IReadOnlyList<Vec2>>? holes, double z, bool faceUp)
    {
        if (outer.Count < 3) return;
        var tris = Triangulator.Triangulate(outer, holes);
        if (tris.Count == 0) return;

        var verts = FlattenRings(outer, holes);
        var normal = new Vector3(0, 0, faceUp ? 1f : -1f);
        float zf = (float)z;
        for (int t = 0; t < tris.Count; t += 3)
        {
            Vec2 a = verts[tris[t]], b = verts[tris[t + 1]], c = verts[tris[t + 2]];
            var va = new Vector3((float)a.X, (float)a.Y, zf);
            var vb = new Vector3((float)b.X, (float)b.Y, zf);
            var vc = new Vector3((float)c.X, (float)c.Y, zf);
            // Earcut emits CCW (viewed from +Z); reverse for a downward-facing cap.
            if (faceUp) AddTriangle(va, vb, vc, normal);
            else AddTriangle(va, vc, vb, normal);
        }
    }

    /// <summary>Adds a polygon as a zero-thickness double-sided sheet at height <paramref name="z"/>.</summary>
    public void AddSheet(IReadOnlyList<Vec2> outer, IReadOnlyList<IReadOnlyList<Vec2>>? holes, double z)
    {
        AddFlatPolygon(outer, holes, z, faceUp: true);
        AddFlatPolygon(outer, holes, z, faceUp: false);
    }

    /// <summary>
    /// Extrudes a (possibly holed) polygon into a closed solid prism between <paramref name="z0"/>
    /// and <paramref name="z1"/>: top cap, bottom cap and vertical side walls with outward normals.
    /// </summary>
    public void AddPrism(IReadOnlyList<Vec2> outer, IReadOnlyList<IReadOnlyList<Vec2>>? holes, double z0, double z1)
    {
        if (outer.Count < 3) return;
        double zt = Math.Max(z0, z1), zb = Math.Min(z0, z1);

        // Caps share the renderer's winding convention; orient rings so walls get outward normals
        // (outer CCW = interior on the left; holes CW = solid on the left).
        var outerCcw = EnsureWinding(outer, ccw: true);
        var holesCw = holes?.Select(h => EnsureWinding(h, ccw: false)).Where(h => h.Count >= 3).ToList();

        AddFlatPolygon(outerCcw, holesCw, zt, faceUp: true);
        AddFlatPolygon(outerCcw, holesCw, zb, faceUp: false);

        AddWalls(outerCcw, zb, zt);
        if (holesCw is not null)
            foreach (var h in holesCw) AddWalls(h, zb, zt);
    }

    /// <summary>Adds a vertical cylindrical wall (an open tube) — e.g. a plated drill barrel.</summary>
    public void AddCylinderWall(Vec2 center, double radius, double z0, double z1, int segments)
    {
        if (radius <= 0 || segments < 3) return;
        var ring = Shapes.Circle(center, radius, segments);
        AddWalls(EnsureWinding(ring, ccw: true), Math.Min(z0, z1), Math.Max(z0, z1), outward: true);
    }

    // Emits the side walls for one closed ring. The ring must already wind so that the solid is on
    // the left of each edge; the outward horizontal normal is then the edge direction rotated −90°.
    private void AddWalls(IReadOnlyList<Vec2> ring, double zb, double zt, bool outward = true)
    {
        int n = ring.Count;
        for (int i = 0; i < n; i++)
        {
            Vec2 a = ring[i], b = ring[(i + 1) % n];
            Vec2 dir = (b - a);
            if (dir.Length < 1e-9) continue;
            Vec2 nrm2 = new Vec2(dir.Y, -dir.X).Normalized();
            if (!outward) nrm2 *= -1;
            var nrm = new Vector3((float)nrm2.X, (float)nrm2.Y, 0f);

            var a0 = new Vector3((float)a.X, (float)a.Y, (float)zb);
            var b0 = new Vector3((float)b.X, (float)b.Y, (float)zb);
            var b1 = new Vector3((float)b.X, (float)b.Y, (float)zt);
            var a1 = new Vector3((float)a.X, (float)a.Y, (float)zt);

            AddTriangle(a0, b0, b1, nrm);
            AddTriangle(a0, b1, a1, nrm);
        }
    }

    private static List<Vec2> FlattenRings(IReadOnlyList<Vec2> outer, IReadOnlyList<IReadOnlyList<Vec2>>? holes)
    {
        var verts = new List<Vec2>(outer);
        if (holes is not null)
            foreach (var h in holes)
                if (h.Count >= 3) verts.AddRange(h);
        return verts;
    }

    private static List<Vec2> EnsureWinding(IReadOnlyList<Vec2> ring, bool ccw)
    {
        var list = ring as List<Vec2> ?? [.. ring];
        // Shoelace: positive area => counter-clockwise in standard (X right, Y up) orientation.
        double area2 = 0;
        for (int i = 0, j = list.Count - 1; i < list.Count; j = i++)
            area2 += (list[j].X * list[i].Y) - (list[i].X * list[j].Y);
        bool isCcw = area2 > 0;
        if (isCcw == ccw) return [.. list];
        var rev = new List<Vec2>(list);
        rev.Reverse();
        return rev;
    }
}
