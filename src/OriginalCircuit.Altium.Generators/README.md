# OriginalCircuit.Altium.Generators

A Roslyn source generator used internally by `OriginalCircuit.Altium`. It is not published as a standalone NuGet package.

## Purpose

Altium Designer files store component and primitive metadata as pipe-delimited key=value strings (e.g., `DESIGNATOR=U1|VALUE=100nF|FOOTPRINT=C0402`). The source generator automates the conversion between these parameter strings and strongly typed C# record types.

## How It Works

Declare a `sealed partial record` and annotate each property with `[AltiumParameter("KEY")]`:

```csharp
[AltiumParameterRecord]
sealed partial record SchComponentDto
{
    [AltiumParameter("DESIGNATOR")]
    public string Designator { get; init; } = string.Empty;

    [AltiumParameter("VALUE")]
    public string Value { get; init; } = string.Empty;

    [AltiumParameter("LOCATION.X")]
    public Coord LocationX { get; init; }

    [AltiumParameter("LOCATION.Y")]
    public Coord LocationY { get; init; }
}
```

The generator emits two methods on the partial record:

- `static SchComponentDto FromParameters(ParameterCollection parameters)` — constructs the record from a parsed parameter collection
- `ParameterCollection ToParameters()` — serializes the record back to a parameter collection

## Supported Property Types

| C# Type | Altium Parameter Format |
|---------|------------------------|
| `string` | Raw string value |
| `int`, `long` | Integer string |
| `double` | Floating-point string |
| `bool` | `T` / `F` |
| `Coord` | Integer with optional `_FRAC` companion key |
| `Color` | Packed integer |
| Enums | Integer or string depending on attribute options |

## Internal Use Only

This package is consumed as a project reference within the solution. It is not intended for use by external consumers of `OriginalCircuit.Altium`.
