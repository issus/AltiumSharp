using OriginalCircuit.Altium.Models;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Rendering;

/// <summary>
/// Base interface for component renderers.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Renders a component to the specified output.
    /// </summary>
    ValueTask RenderAsync(
        IComponent component,
        Stream output,
        RenderOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a component to a file.
    /// </summary>
    ValueTask RenderAsync(
        IComponent component,
        string path,
        RenderOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for rendering.
/// </summary>
public class RenderOptions
{
    /// <summary>
    /// Output width in pixels (for raster) or units (for vector).
    /// </summary>
    public int Width { get; set; } = 1024;

    /// <summary>
    /// Output height in pixels (for raster) or units (for vector).
    /// </summary>
    public int Height { get; set; } = 768;

    /// <summary>
    /// Background color as ARGB.
    /// </summary>
    public uint BackgroundColor { get; set; } = 0xFFFFFFFF; // White

    /// <summary>
    /// Whether to automatically zoom to fit the component.
    /// </summary>
    public bool AutoZoom { get; set; } = true;

    /// <summary>
    /// Scale factor (1.0 = 100%).
    /// </summary>
    public double Scale { get; set; } = 1.0;
}

/// <summary>
/// Renderer specialized for schematic components.
/// </summary>
public interface ISchLibRenderer : IRenderer
{
    /// <summary>
    /// Renders a schematic component.
    /// </summary>
    ValueTask RenderAsync(
        ISchComponent component,
        Stream output,
        RenderOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Renderer specialized for PCB components.
/// </summary>
public interface IPcbLibRenderer : IRenderer
{
    /// <summary>
    /// Renders a PCB footprint component.
    /// </summary>
    ValueTask RenderAsync(
        IPcbComponent component,
        Stream output,
        RenderOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Visitor pattern for rendering primitives.
/// </summary>
/// <typeparam name="TContext">The rendering context type (e.g., Graphics, SVG builder).</typeparam>
public interface IPrimitiveVisitor<in TContext>
{
    /// <summary>
    /// Visits and renders a primitive to the context.
    /// </summary>
    void Visit(IPrimitive primitive, TContext context);
}

/// <summary>
/// Visitor for schematic primitives.
/// </summary>
/// <typeparam name="TContext">The rendering context type.</typeparam>
public interface ISchPrimitiveVisitor<in TContext> : IPrimitiveVisitor<TContext>
{
    void Visit(ISchPin pin, TContext context);
    void Visit(ISchLine line, TContext context);
    void Visit(ISchLabel label, TContext context);
    void Visit(ISchRectangle rectangle, TContext context);
    void Visit(ISchWire wire, TContext context);
    void Visit(ISchPolygon polygon, TContext context);
    void Visit(ISchPolyline polyline, TContext context);
    void Visit(ISchArc arc, TContext context);
    void Visit(ISchBezier bezier, TContext context);
    void Visit(ISchEllipse ellipse, TContext context);
    void Visit(ISchRoundedRectangle roundedRectangle, TContext context);
    void Visit(ISchPie pie, TContext context);
    void Visit(ISchNetLabel netLabel, TContext context);
    void Visit(ISchJunction junction, TContext context);
    void Visit(ISchParameter parameter, TContext context);
    void Visit(ISchTextFrame textFrame, TContext context);
    void Visit(ISchImage image, TContext context);
    void Visit(ISchEllipticalArc ellipticalArc, TContext context);
    void Visit(ISchPowerObject powerObject, TContext context);
    void Visit(ISchNoErc noErc, TContext context);
    void Visit(ISchBusEntry busEntry, TContext context);
    void Visit(ISchBus bus, TContext context);
    void Visit(ISchPort port, TContext context);
    void Visit(ISchSheetSymbol sheetSymbol, TContext context);
    void Visit(ISchSheetEntry sheetEntry, TContext context);
}

/// <summary>
/// Visitor for PCB primitives.
/// </summary>
/// <typeparam name="TContext">The rendering context type.</typeparam>
public interface IPcbPrimitiveVisitor<in TContext> : IPrimitiveVisitor<TContext>
{
    void Visit(IPcbPad pad, TContext context);
    void Visit(IPcbTrack track, TContext context);
    void Visit(IPcbVia via, TContext context);
    void Visit(IPcbArc arc, TContext context);
    void Visit(IPcbText text, TContext context);
    void Visit(IPcbFill fill, TContext context);
    void Visit(IPcbRegion region, TContext context);
    void Visit(IPcbComponentBody body, TContext context);
}
