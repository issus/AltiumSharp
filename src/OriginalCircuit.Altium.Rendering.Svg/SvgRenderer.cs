using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Rendering;
using OriginalCircuit.Eda.Rendering.Svg;

namespace OriginalCircuit.Altium.Rendering.Svg;

/// <summary>
/// Renders components to SVG vector graphics.
/// </summary>
public sealed class SvgRenderer : IRenderer
{
    /// <inheritdoc />
    public async ValueTask RenderAsync(
        IComponent component,
        Stream output,
        RenderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(output);

        options ??= new RenderOptions();

        var width = (double)options.Width;
        var height = (double)options.Height;

        var ctx = new SvgRenderContext(width, height);
        ctx.Clear(ColorHelper.EdaColorToArgb(options.BackgroundColor));

        var transform = new CoordTransform
        {
            ScreenWidth = width,
            ScreenHeight = height,
            Scale = options.Scale
        };

        if (options.AutoZoom)
        {
            transform.AutoZoom(component.Bounds);
        }

        if (component is PcbComponent pcbComponent)
        {
            var renderer = new PcbComponentRenderer(transform);
            renderer.Render(pcbComponent, ctx);
        }
        else if (component is SchComponent schComponent)
        {
            var renderer = new SchComponentRenderer(transform);
            renderer.Render(schComponent, ctx);
        }

        ctx.WriteTo(output);
        await Task.CompletedTask;
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
