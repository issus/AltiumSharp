using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Models.Sch;

namespace OriginalCircuit.Altium.Rendering;

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
    void Visit(ISchNoConnect noErc, TContext context);
    void Visit(ISchBusEntry busEntry, TContext context);
    void Visit(ISchBus bus, TContext context);
    void Visit(ISchPort port, TContext context);
    void Visit(ISchSheet sheetSymbol, TContext context);
    void Visit(ISchSheetPin sheetEntry, TContext context);
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
