using System.Text;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Mech.GLTF;

namespace OriginalCircuit.Altium.Rendering.Gltf;

/// <summary>
/// Renders an Altium <see cref="PcbDocument"/> to a glTF 2.0 3D model — the full layer stack (copper,
/// laminate, solder mask, silkscreen, drills) plus placed component bodies, as named, individually
/// toggleable nodes. Output can be binary <c>.glb</c> or JSON <c>.gltf</c>.
/// </summary>
public sealed class GltfRenderer
{
    /// <summary>
    /// Builds the in-memory glTF document for <paramref name="document"/> without writing it, for
    /// callers that want to post-process the scene or serialise it themselves.
    /// </summary>
    public GltfDocument BuildDocument(PcbDocument document, GltfRenderSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new GltfSceneBuilder(document, settings ?? new GltfRenderSettings()).Build();
    }

    /// <summary>
    /// Renders <paramref name="document"/> to a file. The container format is taken from
    /// <see cref="GltfRenderSettings.Format"/>, defaulting to one inferred from the path extension
    /// (<c>.glb</c> ⇒ binary, otherwise JSON with an embedded buffer).
    /// </summary>
    public async ValueTask RenderAsync(PcbDocument document, string path, GltfRenderSettings? settings = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrEmpty(path);
        settings ??= new GltfRenderSettings();

        GltfDocument gltf = BuildDocument(document, settings);
        cancellationToken.ThrowIfCancellationRequested();

        switch (ResolveFormat(settings.Format, path))
        {
            case GltfOutputFormat.GltfExternal:
                // Writes the .gltf and its sibling .bin (synchronous in the writer).
                GltfWriter.WriteGltfExternalFile(gltf, path, indented: true);
                break;
            case GltfOutputFormat.GltfEmbedded:
                await File.WriteAllTextAsync(path, GltfWriter.WriteGltfEmbedded(gltf, indented: true), cancellationToken)
                    .ConfigureAwait(false);
                break;
            default:
                await File.WriteAllBytesAsync(path, GltfWriter.WriteGlb(gltf), cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    /// <summary>
    /// Renders <paramref name="document"/> to a stream. <see cref="GltfOutputFormat.Auto"/> means GLB;
    /// the external-buffer format is not available for a stream and falls back to an embedded
    /// <c>.gltf</c>.
    /// </summary>
    public async ValueTask RenderAsync(PcbDocument document, Stream stream, GltfRenderSettings? settings = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(stream);
        settings ??= new GltfRenderSettings();

        GltfDocument gltf = BuildDocument(document, settings);
        cancellationToken.ThrowIfCancellationRequested();

        GltfOutputFormat format = settings.Format == GltfOutputFormat.Auto ? GltfOutputFormat.Glb : settings.Format;
        if (format is GltfOutputFormat.GltfEmbedded or GltfOutputFormat.GltfExternal)
        {
            byte[] json = Encoding.UTF8.GetBytes(GltfWriter.WriteGltfEmbedded(gltf, indented: true));
            await stream.WriteAsync(json, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await stream.WriteAsync(GltfWriter.WriteGlb(gltf), cancellationToken).ConfigureAwait(false);
        }
    }

    private static GltfOutputFormat ResolveFormat(GltfOutputFormat requested, string path)
    {
        if (requested != GltfOutputFormat.Auto) return requested;
        string ext = Path.GetExtension(path);
        return ext.Equals(".gltf", StringComparison.OrdinalIgnoreCase)
            ? GltfOutputFormat.GltfEmbedded
            : GltfOutputFormat.Glb;
    }
}
