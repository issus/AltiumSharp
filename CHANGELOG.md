# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0-alpha.1] - Unreleased

### Added

- Complete rewrite of the library with a focus on correctness, performance, and maintainability.
- Fully async read and write API (`ReadAsync` / `WriteAsync`) with `CancellationToken` support on all readers and writers.
- Roslyn source generator (`OriginalCircuit.Altium.Generators`) that generates `FromParameters` / `ToParameters` methods from `[AltiumParameter]` attributes, eliminating reflection-based serialization at runtime.
- Support for reading and writing PCB documents (`.PcbDoc`), including nets, rules, classes, differential pairs, rooms, and advanced storage sections.
- Cross-platform rendering via two new packages:
  - `OriginalCircuit.Altium.Rendering.Raster` — PNG/JPG output using SkiaSharp
  - `OriginalCircuit.Altium.Rendering.Svg` — vector SVG output using .NET XML APIs
- 3D rendering via `OriginalCircuit.Altium.Rendering.Gltf`: exports a `PcbDocument` to a glTF 2.0 model (`.glb`/`.gltf`) — the full physical layer stack (laminate at true thickness, copper layers, solder mask, silkscreen and plated drills) plus the embedded component 3D STEP bodies, placed from their footprint/3D transforms. Every board feature and component is a named, individually toggleable node. Driven by `GltfRenderer` / `GltfRenderSettings`; built on a native earcut triangulator and the `OriginalCircuit.Mech.STEP` / `OriginalCircuit.Mech.GLTF` libraries (vendored as submodules). Adds a `PcbStackup` model surfaced via `PcbDocument.GetStackup()` that exposes per-layer thicknesses and absolute Z positions.
- Shared rendering abstractions in `OriginalCircuit.Altium.Rendering.Core` (`IRenderContext`, `IRenderer`, `CoordTransform`, `LayerColors`, visitor-pattern component renderers).
- Structured diagnostics system: readers collect non-fatal warnings and errors as `AltiumDiagnostic` records (with `DiagnosticSeverity` of Info, Warning, or Error) on the returned model object instead of throwing.
- Exception hierarchy: `AltiumFileException` base, `AltiumCorruptFileException` (includes stream name), `AltiumUnsupportedFeatureException` (includes record type).
- Property coverage test infrastructure and auto-generated `COVERAGE.md` report.
- Fluent builder API for constructing components and primitives programmatically.
- `PcbBinaryConstants` class with named flag constants and a `DecodeFlags` helper.
- `WriterUtilities` shared between `PcbLibWriter` and `SchLibWriter`.
- 2-D barcode rendering: a PCB `Text` whose `BarCodeType` is `PcbBarCodeKind.DataMatrix` or `PcbBarCodeKind.QrCode` is now encoded on the fly and drawn as its module pattern in both the Altium-style and photorealistic renderers, instead of falling back to the source text. Data Matrix uses ISO/IEC 16022 ECC200 (ASCII encodation, Reed-Solomon, Annex F placement); QR Code uses ISO/IEC 18004 (numeric/alphanumeric/byte modes, Reed-Solomon, automatic version selection and penalty-based data masking, error-correction level M to match Altium). On the solder-mask layer the symbol is rendered as mask openings (honoring `BarCodeInverted`, the box size and quiet-zone margins), so an inverted symbol over a copper fill reads as a gold field with green data modules. New public `DataMatrixEncoder` / `QrCodeEncoder` APIs (with `DataMatrixSymbol` / `QrCodeSymbol`) in `OriginalCircuit.Altium.Barcodes`, and a `PcbBarCodeKind` enum (`Code39`, `Code128`, `QrCode`, `DataMatrix`).

### Changed

- New top-level namespace: `OriginalCircuit.Altium` (previously `OriginalCircuit.AltiumSharp`).
- New NuGet package ID: `OriginalCircuit.Altium` (previously `OriginalCircuit.AltiumSharp`).
- Interface-driven API: data models implement `IContainer`, `IComponent`, and related interfaces rather than inheriting from concrete base classes where possible.
- Target framework updated to `net10.0`.
- All four readers and writers are stateless and thread-safe; they can be instantiated once and reused across calls.
- `ParameterCollection` is now a value-type-friendly immutable record supporting efficient serialization round-trips.
- `Coord` conversions use `checked()` arithmetic and throw `OverflowException` on values that exceed the internal fixed-point range.

### Fixed

- Renderers now write to the output stream asynchronously (the image/SVG is encoded in memory, then written with `WriteAsync`), so `RenderAsync` works with streams that disallow synchronous I/O such as an ASP.NET Core response body.
- Raster rendering now produces JPEG as well as PNG: set `RenderOptions.Format` and `Quality`, or render to a `.jpg`/`.jpeg`/`.png` path (the format is inferred from the extension). Previously only PNG was produced despite the documented JPG support.
- `SchLibWriter` now preserves each pin's `OwnerPartId`, so multi-part component symbols round-trip with their pins correctly assigned to their parts (previously every pin was written as part 1).

### Removed

- `System.Drawing.Common` dependency removed from the core library; rendering is now handled exclusively by the optional rendering packages.
- Synchronous-only read and write API removed; all I/O is async.
- Windows-specific rendering code (GDI+) removed; replaced by cross-platform SkiaSharp and SVG backends.
