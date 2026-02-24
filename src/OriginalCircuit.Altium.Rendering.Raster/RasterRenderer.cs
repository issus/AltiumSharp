using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Rendering;
using OriginalCircuit.Eda.Rendering.Raster;
using SkiaSharp;

namespace OriginalCircuit.Altium.Rendering.Raster;

/// <summary>
/// Renders components to raster images (PNG) using SkiaSharp.
/// </summary>
public sealed class RasterRenderer : IRenderer, IPcbLibRenderer
{
    /// <inheritdoc />
    public ValueTask RenderAsync(
        IComponent component,
        Stream output,
        RenderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(output);

        options ??= new RenderOptions();

        using var bitmap = new SKBitmap(options.Width, options.Height);
        using var canvas = new SKCanvas(bitmap);
        using var context = new SkiaRenderContext(canvas);

        context.Clear(ColorHelper.EdaColorToArgb(options.BackgroundColor));

        var transform = new CoordTransform
        {
            ScreenWidth = options.Width,
            ScreenHeight = options.Height,
            Scale = options.Scale,
        };

        if (options.AutoZoom)
        {
            transform.AutoZoom(component.Bounds);
        }

        if (component is PcbComponent pcb)
        {
            var renderer = new PcbComponentRenderer(transform);
            renderer.Render(pcb, context);
        }
        else if (component is SchComponent sch)
        {
            var renderer = new SchComponentRenderer(transform);
            renderer.Render(sch, context);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(output);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask RenderAsync(
        IComponent component,
        string path,
        RenderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await RenderAsync(component, stream, options, cancellationToken);
    }

    /// <inheritdoc />
    ValueTask IPcbLibRenderer.RenderAsync(
        IPcbComponent component,
        Stream output,
        RenderOptions? options,
        CancellationToken cancellationToken)
    {
        return RenderAsync(component, output, options, cancellationToken);
    }
}
