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
- Shared rendering abstractions in `OriginalCircuit.Altium.Rendering.Core` (`IRenderContext`, `IRenderer`, `CoordTransform`, `LayerColors`, visitor-pattern component renderers).
- Structured diagnostics system: readers collect non-fatal warnings and errors as `AltiumDiagnostic` records (with `DiagnosticSeverity` of Info, Warning, or Error) on the returned model object instead of throwing.
- Exception hierarchy: `AltiumFileException` base, `AltiumCorruptFileException` (includes stream name), `AltiumUnsupportedFeatureException` (includes record type).
- Property coverage test infrastructure and auto-generated `COVERAGE.md` report.
- Fluent builder API for constructing components and primitives programmatically.
- `PcbBinaryConstants` class with named flag constants and a `DecodeFlags` helper.
- `WriterUtilities` shared between `PcbLibWriter` and `SchLibWriter`.

### Changed

- New top-level namespace: `OriginalCircuit.Altium` (previously `OriginalCircuit.AltiumSharp`).
- New NuGet package ID: `OriginalCircuit.Altium` (previously `OriginalCircuit.AltiumSharp`).
- Interface-driven API: data models implement `IContainer`, `IComponent`, and related interfaces rather than inheriting from concrete base classes where possible.
- Target framework updated to `net10.0`.
- All four readers and writers are stateless and thread-safe; they can be instantiated once and reused across calls.
- `ParameterCollection` is now a value-type-friendly immutable record supporting efficient serialization round-trips.
- `Coord` conversions use `checked()` arithmetic and throw `OverflowException` on values that exceed the internal fixed-point range.

### Removed

- `System.Drawing.Common` dependency removed from the core library; rendering is now handled exclusively by the optional rendering packages.
- Synchronous-only read and write API removed; all I/O is async.
- Windows-specific rendering code (GDI+) removed; replaced by cross-platform SkiaSharp and SVG backends.
