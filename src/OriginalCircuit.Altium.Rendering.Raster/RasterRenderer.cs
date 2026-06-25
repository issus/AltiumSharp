using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Eda.Rendering;
using OriginalCircuit.Eda.Rendering.Raster;
using SkiaSharp;

namespace OriginalCircuit.Altium.Rendering.Raster;

/// <summary>
/// Renders components and whole documents to raster images (PNG or JPEG) using SkiaSharp.
/// </summary>
/// <remarks>
/// The output format is taken from <see cref="RenderOptions.Format"/> (PNG by default); the
/// file-path overloads infer it from the extension (.png / .jpg / .jpeg). The image is encoded
/// into memory and written to the output stream asynchronously, so the renderer works with
/// streams that disallow synchronous I/O (e.g. an ASP.NET response body).
/// </remarks>
public sealed class RasterRenderer : IRenderer, IPcbLibRenderer
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
        var bytes = RenderToBytes(options, PrepareComponent(component));
        await output.WriteAsync(bytes, cancellationToken);
    }

    /// <summary>Renders a whole PCB document (board) to a raster image.</summary>
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
        var bytes = RenderToBytes(options, PrepareDocument(document, settings));
        await output.WriteAsync(bytes, cancellationToken);
    }

    /// <summary>Renders a whole schematic document (sheet) to a raster image.</summary>
    public async ValueTask RenderAsync(
        SchDocument document,
        Stream output,
        RenderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(output);

        options ??= new RenderOptions();
        var bytes = RenderToBytes(options, PrepareSheet(document));
        await output.WriteAsync(bytes, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask RenderAsync(
        IComponent component,
        string path,
        RenderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options = ApplyPathFormat(options, path);
        var bytes = RenderToBytes(options, PrepareComponent(component));
        await WriteFileAsync(path, bytes, cancellationToken);
    }

    /// <summary>Renders a whole PCB document (board) to a raster file (format inferred from the extension).</summary>
    /// <param name="settings">Optional view-side and layer-visibility settings; null renders a top view with every layer.</param>
    public async ValueTask RenderAsync(
        PcbDocument document,
        string path,
        RenderOptions? options = null,
        PcbRenderSettings? settings = null,
        CancellationToken cancellationToken = default)
    {
        options = ApplyPathFormat(options, path);
        var bytes = RenderToBytes(options, PrepareDocument(document, settings));
        await WriteFileAsync(path, bytes, cancellationToken);
    }

    /// <summary>Renders a whole schematic document (sheet) to a raster file (format inferred from the extension).</summary>
    public async ValueTask RenderAsync(
        SchDocument document,
        string path,
        RenderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options = ApplyPathFormat(options, path);
        var bytes = RenderToBytes(options, PrepareSheet(document));
        await WriteFileAsync(path, bytes, cancellationToken);
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

    /// <summary>
    /// Renders a whole PCB document (board) as a photorealistic 2D image (a fab-house / gerber-viewer
    /// look) rather than the Altium-editor view. Configure the solder-mask, copper, silkscreen, finish
    /// and substrate colours via <paramref name="style"/>; null uses the default green-mask / ENIG / white
    /// preset (<see cref="PcbRealisticStyle.GreenEnig"/>).
    /// </summary>
    /// <param name="style">Appearance (colours, viewed side, finish, supersampling); null uses the default preset.</param>
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
        var bytes = RenderRealisticToBytes(options, style ?? new PcbRealisticStyle(), document);
        await output.WriteAsync(bytes, cancellationToken);
    }

    /// <summary>
    /// Renders a whole PCB document (board) as a photorealistic 2D image to a file (format inferred from
    /// the extension: .png / .jpg / .jpeg).
    /// </summary>
    /// <param name="style">Appearance (colours, viewed side, finish, supersampling); null uses the default preset.</param>
    public async ValueTask RenderRealisticAsync(
        PcbDocument document,
        string path,
        RenderOptions? options = null,
        PcbRealisticStyle? style = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        options = ApplyPathFormat(options, path);
        var bytes = RenderRealisticToBytes(options, style ?? new PcbRealisticStyle(), document);
        await WriteFileAsync(path, bytes, cancellationToken);
    }

    // Renders the photorealistic board, optionally supersampled (drawn at style.Supersample× the output
    // size then downscaled) for smoother edges, and returns the encoded image bytes.
    private static byte[] RenderRealisticToBytes(RenderOptions options, PcbRealisticStyle style, PcbDocument document)
    {
        int ss = Math.Clamp(style.Supersample, 1, 4);

        // Crop the output to the board's bounding box: the image takes the board's aspect ratio so the
        // board fills it with no surrounding letterbox.
        var bounds = document.GetFramingBounds();
        var (outW, outH, margin) = PcbRealisticRenderer.FitOutput(
            bounds, options.Width, options.Height, style.CropToBoardBounds && options.AutoZoom);
        int rw = outW * ss;
        int rh = outH * ss;

        using var bitmap = new SKBitmap(rw, rh);
        using (var canvas = new SKCanvas(bitmap))
        using (var context = new SkiaRenderContext(canvas))
        {
            context.Clear(ColorHelper.EdaColorToArgb(options.BackgroundColor));

            // Set Scale*ss so a fixed-scale (AutoZoom off) render still fills the same area after
            // downscaling; AutoZoom overwrites Scale anyway when on.
            var transform = new CoordTransform
            {
                ScreenWidth = rw,
                ScreenHeight = rh,
                Scale = options.Scale * ss,
            };
            if (options.AutoZoom)
                transform.AutoZoom(bounds, margin);

            new PcbRealisticRenderer(transform, style, ColorHelper.EdaColorToArgb(options.BackgroundColor))
                .Render(document, context);
        }

        SKBitmap finalBitmap = bitmap;
        SKBitmap? downscaled = null;
        if (ss > 1)
        {
            // Box-filter the supersampled buffer down to the target size. A paint-less DrawBitmap samples
            // nearest-neighbor (no smoothing), so draw through an explicit linear SKSamplingOptions.
            downscaled = new SKBitmap(outW, outH);
            using (var sc = new SKCanvas(downscaled))
            using (var image = SKImage.FromBitmap(bitmap))
            {
                sc.DrawImage(image, new SKRect(0, 0, outW, outH),
                    new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
            }
            finalBitmap = downscaled;
        }

        try
        {
            using var image = SKImage.FromBitmap(finalBitmap);
            var (skFormat, quality) = options.Format == RasterImageFormat.Jpeg
                ? (SKEncodedImageFormat.Jpeg, Math.Clamp(options.Quality, 1, 100))
                : (SKEncodedImageFormat.Png, 100);
            using var data = image.Encode(skFormat, quality);
            return data.ToArray();
        }
        finally
        {
            downscaled?.Dispose();
        }
    }

    // ── internals ───────────────────────────────────────────────────────────

    // What to draw and how to frame it — built once per input type and shared by the stream and
    // file overloads (the only difference between them is where the encoded bytes go).
    private readonly record struct Scene(CoordRect Bounds, double Margin, Action<CoordTransform, IRenderContext> Draw);

    private static Scene PrepareComponent(IComponent component) =>
        // Schematic symbols need more breathing room so outward pin-name text isn't clipped.
        new(component.Bounds, component is SchComponent ? 0.82 : 0.9, (transform, context) =>
        {
            if (component is PcbComponent pcb)
            {
                new PcbComponentRenderer(transform).Render(pcb, context);
            }
            else if (component is SchComponent sch)
            {
                var renderer = new SchComponentRenderer(transform);
                renderer.SetFonts(sch.Fonts);
                // A multi-part library symbol stores every part's pins; show one part (like Altium).
                renderer.PartFilter = sch.CurrentPartId > 0 ? sch.CurrentPartId : 1;
                renderer.Render(sch, context);
            }
        });

    private static Scene PrepareDocument(PcbDocument document, PcbRenderSettings? settings) =>
        // Frame to the physical board (outline ∪ content), not just the primitives, so the board
        // edge isn't cropped and an outline-only board still fills the canvas.
        new(document.GetFramingBounds(), 0.95,
            (transform, context) => CreatePcbRenderer(transform, settings).Render(document, context));

    private static Scene PrepareSheet(SchDocument document) =>
        // Frame to the sheet page (like Altium's "fit sheet") rather than the content bounds: schematic
        // component bounds over-estimate text extents, which would otherwise zoom the page out.
        new(document.SheetInfo.SheetRect, 0.96, (transform, context) =>
        {
            var renderer = new SchComponentRenderer(transform);
            renderer.SetFonts(document.Fonts);
            renderer.Render(document, context);
        });

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

    // Renders to an in-memory bitmap and returns the encoded image bytes. Encoding is CPU-bound
    // and synchronous; the caller writes the result to the destination stream asynchronously.
    private static byte[] RenderToBytes(RenderOptions options, Scene scene)
    {
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
            transform.AutoZoom(scene.Bounds, scene.Margin);

        scene.Draw(transform, context);

        using var image = SKImage.FromBitmap(bitmap);
        var (skFormat, quality) = options.Format == RasterImageFormat.Jpeg
            ? (SKEncodedImageFormat.Jpeg, Math.Clamp(options.Quality, 1, 100))
            : (SKEncodedImageFormat.Png, 100);
        using var data = image.Encode(skFormat, quality);
        return data.ToArray();
    }

    // For file output, pick the format from the extension (.jpg/.jpeg/.png) unless already chosen.
    private static RenderOptions ApplyPathFormat(RenderOptions? options, string path)
    {
        options ??= new RenderOptions();
        var ext = Path.GetExtension(path);
        if (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            return options with { Format = RasterImageFormat.Jpeg };
        if (ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
            return options with { Format = RasterImageFormat.Png };
        return options;
    }

    private static async ValueTask WriteFileAsync(string path, byte[] bytes, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await stream.WriteAsync(bytes, cancellationToken);
    }
}
