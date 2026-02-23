# OriginalCircuit.Altium.Rendering.Core

Shared rendering abstractions for the OriginalCircuit.Altium rendering packages. This package is consumed by `OriginalCircuit.Altium.Rendering.Raster` and `OriginalCircuit.Altium.Rendering.Svg`; most users should install one of those packages rather than this one directly.

## Key Abstractions

### IRenderContext

Represents the drawing surface. Concrete implementations are provided by each rendering backend:

- `SkiaRenderContext` (Raster package) — draws to a SkiaSharp canvas
- `SvgRenderContext` (Svg package) — accumulates SVG elements

### IRenderer

Accepts a data model and an `IRenderContext` and produces output. Implementations include `PcbComponentRenderer` and `SchComponentRenderer`.

### CoordTransform

Converts between Altium's internal fixed-point coordinate system and the output coordinate space (pixels or SVG units). Handles scaling, translation, and Y-axis flipping (Altium uses Y-up; raster images use Y-down).

```csharp
var transform = new CoordTransform(
    scale: 10.0,           // output units per mil
    originX: bounds.Left,
    originY: bounds.Bottom,
    flipY: true
);
```

### Visitor Pattern

Renderers use a visitor pattern over the primitive hierarchy. Each renderer implements `Visit` overloads for the primitive types it handles (`PcbPad`, `PcbTrack`, `SchPin`, `SchWire`, etc.). Unknown primitive types are silently skipped.

### Color Utilities

`ColorHelper` provides conversions between Altium's packed integer color format and the color types used by each backend. `LayerColors` provides the default color mapping for PCB layers.

## Extending the Rendering System

To implement a custom rendering backend:

1. Implement `IRenderContext` to wrap your drawing API.
2. Subclass `PcbComponentRenderer` or `SchComponentRenderer`, overriding the `Visit` methods for each primitive type you want to support.
3. Use `CoordTransform` to map coordinates.

## License

MIT
