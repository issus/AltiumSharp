using System.Numerics;
using System.Text.Json.Nodes;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Mech.GLTF;
using OriginalCircuit.Mech.STEP.Geometry;
using OriginalCircuit.Mech.STEP.Schema;
using OriginalCircuit.Mech.STEP.Tessellation;
using OriginalCircuit.Mech.STEP.Topology;

namespace OriginalCircuit.Altium.Rendering.Gltf;

/// <summary>
/// Places each component's embedded 3D STEP body onto the board. The model is parsed and tessellated
/// once per unique model id (cached), then each placement is transformed into board space — model
/// canonical orientation, the body's 3D rotation, the footprint 2D rotation, the board XY location
/// and the standoff height — and emitted as its own toggleable node under a single "Components" node.
/// </summary>
internal sealed class GltfComponentPlacer(
    PcbDocument doc,
    GltfRenderSettings settings,
    PcbStackup stack,
    GltfBuilder builder,
    double centerXMm,
    double centerYMm)
{
    // Canonical mesh (STEP scene transform + model orientation/Dz applied), cached per model id.
    private readonly Dictionary<string, CanonicalMesh?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private int _material = -1;

    private sealed record CanonicalMesh(List<Vector3> Positions, List<Vector3> Normals, List<int> Indices);

    public int? Build()
    {
        var models = new Dictionary<string, PcbModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in doc.Models)
            if (!string.IsNullOrEmpty(m.Id)) models[m.Id] = m;
        if (models.Count == 0) return null;

        double boardTopZ = stack.ForLayer(37)?.Z1Mm ?? stack.TotalThicknessMm;
        var children = new List<int>();

        foreach (var body in doc.ComponentBodies.OfType<PcbComponentBody>())
        {
            if (string.IsNullOrEmpty(body.ModelId) || !models.TryGetValue(body.ModelId, out var model)) continue;
            if (string.IsNullOrWhiteSpace(model.StepData)) continue;

            CanonicalMesh? canonical = GetCanonical(body.ModelId, model);
            if (canonical is null || canonical.Indices.Count == 0) continue;

            int node = EmitBody(body, model, canonical, boardTopZ);
            if (node >= 0) children.Add(node);
        }

        if (children.Count == 0) return null;
        if (_material < 0) return null;
        return builder.AddNode(name: "Components", children: children);
    }

    private int EmitBody(PcbComponentBody body, PcbModel model, CanonicalMesh canonical, double boardTopZ)
    {
        // Footprint + 3D placement rotations (degrees), applied after the model's canonical pose.
        double rx = body.Model3DRotX, ry = body.Model3DRotY, rz = body.Model3DRotZ + body.Model2DRotation;
        double tx = body.Model2DLocation.X.ToMm() - centerXMm;
        double ty = body.Model2DLocation.Y.ToMm() - centerYMm;
        double tz = boardTopZ + body.StandoffHeight.ToMm() + body.Model3DDz.ToMm();

        var positions = new List<Vector3>(canonical.Positions.Count);
        var normals = new List<Vector3>(canonical.Normals.Count);
        foreach (var p in canonical.Positions)
        {
            var (x, y, z) = Rotate(p.X, p.Y, p.Z, rx, ry, rz);
            positions.Add(new Vector3((float)(x + tx), (float)(y + ty), (float)(z + tz)));
        }
        foreach (var n in canonical.Normals)
        {
            var (x, y, z) = Rotate(n.X, n.Y, n.Z, rx, ry, rz);
            var v = new Vector3((float)x, (float)y, (float)z);
            normals.Add(v.LengthSquared() > 1e-12f ? Vector3.Normalize(v) : new Vector3(0, 0, 1));
        }

        string name = ComponentName(body);
        int mesh = builder.AddMesh(positions, normals, canonical.Indices,
            [new MeshPartSpec(0, canonical.Indices.Count, _material)], name);

        var extras = new JsonObject { ["role"] = "component", ["designator"] = name };
        if (!string.IsNullOrEmpty(body.ModelName)) extras["model"] = body.ModelName;
        return builder.AddNode(mesh: mesh, name: name, extras: extras);
    }

    // Tessellates the model once and bakes its canonical orientation (PcbModel rotation + Dz).
    private CanonicalMesh? GetCanonical(string modelId, PcbModel model)
    {
        if (_cache.TryGetValue(modelId, out var cached)) return cached;

        CanonicalMesh? result = null;
        try
        {
            var stepModel = StepModel.Parse(model.StepData);
            var options = new TessellationOptions { ChordTolerance = settings.ComponentChordToleranceMm };
            var tess = new Tessellator(stepModel, options);

            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var indices = new List<int>();

            double mrx = model.RotationX, mry = model.RotationY, mrz = model.RotationZ;
            double mdz = Coord.FromRaw(model.Dz).ToMm();

            foreach (var (transform, tri) in CollectMeshes(tess, stepModel))
            {
                int baseIndex = positions.Count;
                for (int i = 0; i < tri.Positions.Count; i++)
                {
                    Vec3 wp = transform.TransformPoint(tri.Positions[i]);
                    var (x, y, z) = Rotate(wp.X, wp.Y, wp.Z, mrx, mry, mrz);
                    positions.Add(new Vector3((float)x, (float)y, (float)(z + mdz)));

                    Vec3 wn = i < tri.Normals.Count ? transform.TransformDirection(tri.Normals[i]) : new Vec3(0, 0, 1);
                    var (nx, ny, nz) = Rotate(wn.X, wn.Y, wn.Z, mrx, mry, mrz);
                    var nv = new Vector3((float)nx, (float)ny, (float)nz);
                    normals.Add(nv.LengthSquared() > 1e-12f ? Vector3.Normalize(nv) : new Vector3(0, 0, 1));
                }
                foreach (int idx in tri.Indices) indices.Add(baseIndex + idx);
            }

            if (indices.Count > 0)
            {
                result = new CanonicalMesh(positions, normals, indices);
                if (_material < 0) _material = builder.AddMaterial(GltfPalette.Component());
            }
        }
        catch
        {
            // A model that cannot be parsed/tessellated is skipped rather than failing the render.
            result = null;
        }

        _cache[modelId] = result;
        return result;
    }

    private static List<(Matrix4 Transform, TriangleMesh Mesh)> CollectMeshes(Tessellator tess, StepModel model)
    {
        var found = new List<(Matrix4, TriangleMesh)>();
        foreach (var item in Flatten(tess.TessellateScene(), Matrix4.Identity)) found.Add(item);
        if (found.Count > 0) return found;

        // Fallback: a model with no assembly occurrences — tessellate its representations directly.
        foreach (var rep in model.OfType<Representation>())
        {
            var mesh = tess.TessellateRepresentation(rep);
            if (mesh is not null) found.Add((Matrix4.Identity, mesh));
        }
        return found;
    }

    private static IEnumerable<(Matrix4 Transform, TriangleMesh Mesh)> Flatten(SceneNode node, Matrix4 parent)
    {
        Matrix4 world = parent * node.Transform;
        if (node.Mesh is { } mesh) yield return (world, mesh);
        foreach (var child in node.Children)
            foreach (var item in Flatten(child, world))
                yield return item;
    }

    private string ComponentName(PcbComponentBody body)
    {
        if (body.ComponentIndex >= 0 && body.ComponentIndex < doc.Components.Count)
        {
            string? designator = doc.Components[body.ComponentIndex].Name;
            if (!string.IsNullOrEmpty(designator)) return designator;
        }
        if (!string.IsNullOrEmpty(body.Name)) return body.Name!;
        return body.ModelName ?? "Component";
    }

    private static (double X, double Y, double Z) Rotate(double x, double y, double z, double degX, double degY, double degZ)
    {
        if (degX != 0) { double a = degX * Math.PI / 180.0, c = Math.Cos(a), s = Math.Sin(a); (y, z) = ((y * c) - (z * s), (y * s) + (z * c)); }
        if (degY != 0) { double a = degY * Math.PI / 180.0, c = Math.Cos(a), s = Math.Sin(a); (x, z) = ((x * c) + (z * s), (-x * s) + (z * c)); }
        if (degZ != 0) { double a = degZ * Math.PI / 180.0, c = Math.Cos(a), s = Math.Sin(a); (x, y) = ((x * c) - (y * s), (x * s) + (y * c)); }
        return (x, y, z);
    }
}
