using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Mech.GLTF;

namespace OriginalCircuit.Altium.Rendering.Gltf;

/// <summary>
/// Places each component's embedded 3D STEP body onto the board: tessellates the model, applies the
/// placement transform (position, rotation, board side, standoff) and emits one toggleable node per
/// component grouped under a single "Components" node.
/// </summary>
internal sealed class GltfComponentPlacer(
    PcbDocument doc,
    GltfRenderSettings settings,
    PcbStackup stack,
    GltfBuilder builder,
    double centerXMm,
    double centerYMm)
{
    /// <summary>
    /// Builds the component group and returns its root node index, or <see langword="null"/> when no
    /// components were placed.
    /// </summary>
    public int? Build()
    {
        // Implemented in the component-placement task.
        _ = (doc, settings, stack, builder, centerXMm, centerYMm);
        return null;
    }
}
