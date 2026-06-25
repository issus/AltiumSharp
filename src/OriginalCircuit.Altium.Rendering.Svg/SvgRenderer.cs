using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Eda.Rendering;
using OriginalCircuit.Eda.Rendering.Svg;

namespace OriginalCircuit.Altium.Rendering.Svg;

/// <summary>
/// Renders components and whole documents to SVG vector graphics.
/// </summary>
/// <remarks>
/// The SVG is built in memory and written to the output stream asynchronously, so the renderer
/// works with streams that disallow synchronous I/O (e.g. an ASP.NET response body).
/// </remarks>
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
        // Schematic symbols need more breathing room so outward pin-name text isn't clipped.
        var margin = component is SchComponent ? 0.82 : 0.9;
        var bytes = RenderToBytes(options, component.Bounds, margin, (transform, ctx) =>
        {
            if (component is PcbComponent pcbComponent)
            {
                new PcbComponentRenderer(transform).Render(pcbComponent, ctx);
            }
            else if (component is SchComponent schComponent)
            {
                var renderer = new SchComponentRenderer(transform);
                renderer.SetFonts(schComponent.Fonts);
                // A multi-part library symbol stores every part's pins; show one part (like Altium).
                renderer.PartFilter = schComponent.CurrentPartId > 0 ? schComponent.CurrentPartId : 1;
                renderer.Render(schComponent, ctx);
            }
        });
        await output.WriteAsync(bytes, cancellationToken);
    }

    /// <summary>Renders a whole PCB document (board) to SVG.</summary>
    /// <param name="settings">Optional view-side and layer-visibility settings; null renders a top view with every layer.</param>
    public async ValueTask RenderAsync(
        PcbDocument document,
        Stream output,
        RenderOptions? options = null,
        PcbRenderSettings? settings = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(output);

        options ??= new RenderOptions();
        // Frame to the physical board (outline ∪ content), not just the primitives, so the board
        // edge isn't cropped and an outline-only board still fills the canvas.
        var bytes = RenderToBytes(options, document.GetFramingBounds(), 0.95,
            (transform, ctx) => CreatePcbRenderer(transform, settings).Render(document, ctx));
        await output.WriteAsync(bytes, cancellationToken);
    }

    /// <summary>
    /// Renders a whole PCB document (board) as a photorealistic 2D SVG (a fab-house / gerber-viewer look)
    /// rather than the Altium-editor view. Configure the solder-mask, copper, silkscreen, finish and
    /// substrate colours via <paramref name="style"/>; null uses the default green-mask / ENIG / white
    /// preset. The supersample knob is ignored for vector output.
    /// </summary>
    /// <param name="style">Appearance (colours, viewed side, finish); null uses the default preset.</param>
    public async ValueTask RenderRealisticAsync(
        PcbDocument document,
        Stream output,
        RenderOptions? options = null,
        PcbRealisticStyle? style = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(output);

        options ??= new RenderOptions();
        var s = style ?? new PcbRealisticStyle();

        // Crop the output to the board's bounding box: the SVG viewport takes the board's aspect ratio so
        // the board fills it with no surrounding letterbox.
        var bounds = document.GetFramingBounds();
        var (outW, outH, margin) = PcbRealisticRenderer.FitOutput(
            bounds, options.Width, options.Height, s.CropToBoardBounds && options.AutoZoom);

        var bgArgb = ColorHelper.EdaColorToArgb(options.BackgroundColor);
        var ctx = new SvgRenderContext(outW, outH);
        ctx.Clear(bgArgb);
        var transform = new CoordTransform { ScreenWidth = outW, ScreenHeight = outH, Scale = options.Scale };
        if (options.AutoZoom)
            transform.AutoZoom(bounds, margin);
        new PcbRealisticRenderer(transform, s, bgArgb).Render(document, ctx);

        using var buffer = new MemoryStream();
        ctx.WriteTo(buffer);
        await output.WriteAsync(buffer.ToArray(), cancellationToken);
    }

    /// <summary>Renders a whole PCB document (board) as a photorealistic 2D SVG to a file.</summary>
    /// <param name="style">Appearance (colours, viewed side, finish); null uses the default preset.</param>
    public async ValueTask RenderRealisticAsync(
        PcbDocument document,
        string path,
        RenderOptions? options = null,
        PcbRealisticStyle? style = null,
        CancellationToken cancellationToken = default)
    {
        // Validate before opening the file so a null document doesn't leave an empty file behind.
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(path);
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await RenderRealisticAsync(document, stream, options, style, cancellationToken);
    }

    private static PcbComponentRenderer CreatePcbRenderer(CoordTransform transform, PcbRenderSettings? settings)
    {
        var renderer = new PcbComponentRenderer(transform);
        if (settings is not null)
        {
            renderer.ViewSide = settings.ViewSide;
            renderer.LayerFilter = settings.IsLayerAllowed;
            renderer.ClipToBoardOutline = settings.ClipToBoardOutline;
        }
        return renderer;
    }

    /// <summary>Renders a whole schematic document (sheet) to SVG.</summary>
    public async ValueTask RenderAsync(
        SchDocument document,
        Stream output,
        RenderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(output);

        options ??= new RenderOptions();
        // Frame to the sheet page (like Altium's "fit sheet") rather than the content bounds: schematic
        // component bounds over-estimate text extents, which would otherwise zoom the page out.
        var bytes = RenderToBytes(options, document.SheetInfo.SheetRect, 0.96, (transform, ctx) =>
        {
            var renderer = new SchComponentRenderer(transform);
            renderer.SetFonts(document.Fonts);
            renderer.Render(document, ctx);
        });
        await output.WriteAsync(bytes, cancellationToken);
    }

    // Builds the SVG document in memory and returns its bytes; the caller writes them to the
    // destination stream asynchronously.
    private static byte[] RenderToBytes(RenderOptions options, CoordRect bounds, double margin,
        Action<CoordTransform, IRenderContext> draw)
    {
        var ctx = new SvgRenderContext(options.Width, options.Height);
        ctx.Clear(ColorHelper.EdaColorToArgb(options.BackgroundColor));

        var transform = new CoordTransform
        {
            ScreenWidth = options.Width,
            ScreenHeight = options.Height,
            Scale = options.Scale
        };
        if (options.AutoZoom)
            transform.AutoZoom(bounds, margin);

        draw(transform, ctx);

        using var buffer = new MemoryStream();
        ctx.WriteTo(buffer);   // produces the exact SVG document (XML declaration + svg element)
        return buffer.ToArray();
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

    /// <summary>Renders a whole PCB document (board) to an SVG file.</summary>
    /// <param name="settings">Optional view-side and layer-visibility settings; null renders a top view with every layer.</param>
    public async ValueTask RenderAsync(
        PcbDocument document,
        string path,
        RenderOptions? options = null,
        PcbRenderSettings? settings = null,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await RenderAsync(document, stream, options, settings, cancellationToken);
    }

    /// <summary>Renders a whole schematic document (sheet) to an SVG file.</summary>
    public async ValueTask RenderAsync(
        SchDocument document,
        string path,
        RenderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await RenderAsync(document, stream, options, cancellationToken);
    }
}
