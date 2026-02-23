# AltiumSharp Test Data Generator

Altium Designer script project for generating comprehensive test files for AltiumSharp validation and reverse engineering.

## Generated Files

The script generates standalone library files with matching JSON exports:

| File | Description |
|------|-------------|
| `TestPcbLib.PcbLib` | Combined PCB footprint library with all primitive types |
| `TestPcbLib.json` | JSON export of all PCB library data |
| `TestSchLib.SchLib` | Combined schematic symbol library with all primitive types |
| `TestSchLib.json` | JSON export of all schematic library data |
| `Individual/PCB/*.PcbLib` | Individual PCB library files (one footprint per file) |
| `Individual/PCB/*.json` | JSON exports for individual PCB libraries |
| `Individual/SchLib/*.SchLib` | Individual schematic library files (one symbol per file) |
| `Individual/SchLib/*.json` | JSON exports for individual schematic libraries |

## PCB Library Contents

Footprints covering all PCB primitive types:

### Pad Types
- **PAD_TH_ROUND** - Through-hole round pad
- **PAD_TH_RECTANGULAR** - Through-hole rectangular pad
- **PAD_TH_OCTAGONAL** - Through-hole octagonal pad
- **PAD_SMD_RECTANGULAR** - SMD rectangular pad
- **PAD_SMD_ROUNDED** - SMD rounded rectangular pad
- **PAD_ROTATED** - Pad with 45-degree rotation

### Basic Primitives
- **TRACKS_MULTILAYER** - Tracks on Top, Bottom, and Overlay layers
- **ARCS_TEST** - Arcs with various angles (90, 180, 360 degrees)
- **FILLS_TEST** - Fill regions with rotation
- **REGIONS_TEST** - Region objects
- **COMPLEX_FOOTPRINT** - Multiple primitives combined

### Text Objects
- **TEXT_TEST** - Text objects (normal, rotated, mirrored)
- **TEXT_TRUETYPE** - TrueType font text (Arial, Times New Roman, Courier)
- **TEXT_INVERTED** - Inverted (knockout) text
- **TEXT_BARCODE** - Barcode text (Code39)

### Polygons
- **POLYGON_SOLID** - Solid copper pour polygon
- **POLYGON_HATCHED** - Hatched copper pour polygon
- **REGION_CUTOUT** - Cutout region for polygon pours

### Advanced Objects
- **BODY_3D** - 3D extruded component body
- **BODY_3D_STEP** - 3D body loaded from STEP file
- **KEEPOUT_REGION** - Keepout region and track
- **DIMENSION_LINEAR** - Linear dimension object

## Schematic Library Contents

Symbols covering various component and primitive types:

### Component Symbols
- **RESISTOR** - Basic 2-pin passive with rectangle body
- **CAPACITOR** - Capacitor with parallel plates
- **OPAMP** - 5-pin operational amplifier with triangle body
- **CONNECTOR_4** - 4-pin connector
- **LED** - LED with anode/cathode and light arrows
- **TRANSISTOR_NPN** - NPN transistor with B/C/E pins

### Shape Test Symbols
- **CIRCLE_FILLED** - Filled circle (ellipse with equal radii)
- **CIRCLE_OUTLINE** - Outline circle
- **ELLIPSE_TEST** - Ellipse with different X/Y radii
- **ROUNDRECT_TEST** - Rounded rectangle
- **ARC_FULL** - Full 360-degree arc
- **POLYLINE_TEST** - Open polyline with 4 vertices (zigzag pattern)
- **POLYGON_TEST** - Closed polygon with 4 vertices (diamond shape)

### Text Objects
- **TEXTFRAME_TEST** - Text frame with border

## Scripts

The project includes two script files:

| Script | Description |
|--------|-------------|
| `TestDataGenerator.pas` | Generates test files with known primitives for AltiumSharp validation |
| `FileToJsonConverter.pas` | Converts existing Altium files to JSON format |

## Usage

### TestDataGenerator - Generating Test Files

1. Open the project: `File > Open Project > TestDataGenerator.PrjPcb`
2. Open the script: Double-click `TestDataGenerator.pas`
3. Run a procedure: `DXP > Run Script` (or `File > Run Script`)
4. Select one of:
   - `RunGenerateAll` - Generate all test files (combined + individual)
   - `RunGeneratePcbLib` - Generate only PCB library (combined + individual)
   - `RunGenerateSchLib` - Generate only Schematic library (combined + individual)

### FileToJsonConverter - Converting Existing Files

1. Open the project: `File > Open Project > TestDataGenerator.PrjPcb`
2. Open the file you want to convert (PcbLib, SchLib, SchDoc, or PcbDoc)
3. Run a procedure: `DXP > Run Script`
4. Select `FileToJsonConverter.pas` and one of:
   - `RunExportCurrentPcbLib` - Export currently open PCB library to JSON
   - `RunExportCurrentSchLib` - Export currently open Schematic library to JSON
   - `RunExportCurrentSchDoc` - Export currently open schematic document to JSON
   - `RunExportCurrentPcbDoc` - Export currently open PCB document to JSON

JSON files are saved alongside the source file with `.json` extension.

### Command Line (Automated)

```batch
REM Generate all test files
"C:\Program Files\Altium\AD25\X2.EXE" -RScriptingSystem:RunScript(ProjectName="D:\src\AltiumSharp\TestDataGenerator\TestDataGenerator.PrjPcb"^|ProcName="RunGenerateAll")
```

Note: Adjust the Altium Designer path for your installation.

## Output Directory

Generated files are saved to:
```
D:\src\AltiumSharp\TestData\Generated\
    TestPcbLib.PcbLib
    TestPcbLib.json
    TestSchLib.SchLib
    TestSchLib.json
    Individual/
        PCB/
            PAD_TH_ROUND.PcbLib
            PAD_TH_ROUND.json
            PAD_SMD_ROUNDED.PcbLib
            PAD_SMD_ROUNDED.json
            TEXT_INVERTED.PcbLib
            TEXT_INVERTED.json
            ...
        SchLib/
            CIRCLE_FILLED.SchLib
            CIRCLE_FILLED.json
            ELLIPSE_TEST.SchLib
            ELLIPSE_TEST.json
            POLYLINE_TEST.SchLib
            POLYLINE_TEST.json
            ...
```

## JSON Export Format

### PCB Library JSON Structure

```json
{
  "metadata": {
    "exportType": "PcbLib",
    "fileName": "TestPcbLib.PcbLib",
    "exportedBy": "AltiumSharp TestDataGenerator",
    "version": "1.0"
  },
  "footprints": [
    {
      "name": "PAD_TH_ROUND",
      "description": "Through-hole round pad test",
      "height": {...},
      "primitives": [
        {
          "objectType": "Pad",
          "name": "1",
          "x": {"internal": 0, "mils": 0, "mm": 0},
          "y": {"internal": 0, "mils": 0, "mm": 0},
          "topXSize": {"internal": 600000, "mils": 60, "mm": 1.524},
          "holeSize": {"internal": 300000, "mils": 30, "mm": 0.762},
          "topShape": 1,
          "rotation": 0,
          "layer": 74,
          "plated": true
        }
      ]
    }
  ]
}
```

### Schematic Library JSON Structure

```json
{
  "metadata": {
    "exportType": "SchLib",
    "fileName": "TestSchLib.SchLib",
    "exportedBy": "AltiumSharp TestDataGenerator",
    "version": "1.0"
  },
  "symbols": [
    {
      "objectType": "Component",
      "designator": "R?",
      "comment": "Resistor",
      "libReference": "RESISTOR",
      "description": "Basic 2-pin resistor symbol",
      "primitives": [
        {
          "objectType": "Pin",
          "name": "P1",
          "designator": "1",
          "x": -400,
          "y": 0,
          "orientation": 2,
          "electrical": 7
        },
        {
          "objectType": "Ellipse",
          "x": 0,
          "y": 0,
          "radius": 200,
          "secondaryRadius": 200,
          "isSolid": true
        }
      ]
    }
  ]
}
```

## Supported Object Types

### PCB Object Types
- Pad, Track, Arc, Fill, Text, Region
- Polygon (solid/hatched)
- ComponentBody (3D)
- Dimension

### Schematic Object Types
- Pin, Line, Rectangle, Arc, Polygon, Polyline, Label
- Ellipse (for circles and ellipses)
- RoundRectangle
- TextFrame

## Coordinate Systems

### PCB Coordinates
- Internal units: 10,000 per mil
- All coordinates exported as: `{"internal": N, "mils": N/10000, "mm": N/393701}`

### Schematic Coordinates
- Internal units: 10 per mil (DXP units)
- Exported as raw integer values

## Extending the Generator

To add new test primitives:

1. Add creation procedure (e.g., `CreatePcbPad`, `CreateSchPin`)
2. Add JSON export procedure (e.g., `ExportPcbPadToJson`)
3. Call from generator procedure (`GeneratePcbLibTestFootprints`, etc.)
4. Update the iterator filter in export procedure if needed
5. For individual file generation, add a `CreateFootprint_*` procedure and call from `GenerateIndividualPcbLibFiles`

## STEP Files

The generator includes a `BODY_3D_STEP` footprint that loads a 3D model from a STEP file. STEP files are stored in:
```
D:\src\AltiumSharp\TestDataGenerator\step\
```

Available STEP files:
- `PSEMI QFN-24 4x4.step` - QFN-24 package (used by default)
- `RES AXIAL TH D1.6 L3.6 R0.01 T5.step` - Axial resistor
- `AMD FCBGA-676 FFVB676.step` - BGA package
- `STM UFQFPN-48 7X7 A0B9.step` - QFP package

## Integration with AltiumSharp

Use the generated files and JSON exports for:

1. **Unit Testing**: Compare AltiumSharp parsed values against JSON ground truth
2. **Reverse Engineering**: Discover how Altium stores different primitive types
3. **Format Discovery**: Compare JSON exports from different Altium versions
4. **Validation**: Verify round-trip read/write operations preserve data
5. **Individual Test Cases**: Use single-footprint files for targeted unit tests
