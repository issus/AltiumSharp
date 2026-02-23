using OriginalCircuit.Altium.Models;

namespace OriginalCircuit.Altium.Rendering.Gltf;

/// <summary>
/// Renders PCB components to glTF 3D format.
/// </summary>
public sealed class GltfRenderer : IRenderer
{
    /// <summary>
    /// Options specific to glTF rendering.
    /// </summary>
    public GltfOptions GltfOptions { get; set; } = new();

    /// <inheritdoc />
    public async ValueTask RenderAsync(
        IComponent component,
        Stream output,
        RenderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        throw new NotImplementedException(
            "glTF 3D rendering is planned for a future release using SharpGLTF. " +
            "This will support PCB 3D visualization with copper layers and STEP models.");
    }

    /// <inheritdoc />
    public async ValueTask RenderAsync(
        IComponent component,
        string path,
        RenderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await RenderAsync(component, stream, options, cancellationToken);
    }
}

/// <summary>
/// Options specific to glTF/3D rendering.
/// </summary>
public sealed class GltfOptions
{
    /// <summary>
    /// Whether to include STEP 3D models from component bodies.
    /// </summary>
    public bool IncludeStepModels { get; set; } = true;

    /// <summary>
    /// Whether to generate separate meshes for each layer.
    /// </summary>
    public bool SeparateLayers { get; set; } = true;

    /// <summary>
    /// Copper thickness in mm.
    /// </summary>
    public double CopperThickness { get; set; } = 0.035;

    /// <summary>
    /// Board thickness in mm.
    /// </summary>
    public double BoardThickness { get; set; } = 1.6;
}
