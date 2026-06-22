using System;
using System.Collections.Generic;
using OriginalCircuit.Altium.Barcodes;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Rendering;

/// <summary>
/// Turns a PCB <see cref="PcbText"/> that is a 2-D barcode (Data Matrix or QR Code) into world-space geometry
/// the renderers can draw. Altium stores only the barcode's source text — never the module pattern — so the
/// symbol is (re-)encoded on demand (<see cref="DataMatrixEncoder"/> / <see cref="QrCodeEncoder"/>) and laid
/// out at the text's location/size.
/// </summary>
/// <remarks>
/// Sizing follows Altium's barcode "Size Mode": the square symbol fits inside a box whose side comes from the
/// text-box width/height fields (where Altium stores a 2-D barcode's full size — e.g. 7.5&#160;mm on the Coherent
/// Digitiser), inset on all sides by the X/Y margin (the quiet zone). The box's bottom-left corner is anchored
/// at <see cref="PcbText.Location"/> and extends right (+X) / up (+Y) before the text rotation and mirror are
/// applied — matching the copper backing fill that shares that origin.
///
/// The renderable region depends on <see cref="PcbText.BarCodeInverted"/>:
/// <list type="bullet">
/// <item>Not inverted: the foreground is the dark data modules (dark-on-light).</item>
/// <item>Inverted: the foreground is the whole box <em>minus</em> the dark modules — the quiet-zone frame plus
/// the light modules — so the symbol reads light-on-dark. On the solder-mask layer that foreground is a mask
/// opening revealing the copper/finish (a gold field with the data modules left masked / green).</item>
/// </list>
///
/// QR Code symbols use error-correction level M (Altium's fixed level) and the standard penalty-based data
/// mask, which reproduces Altium's symbol.
/// </remarks>
internal static class PcbBarcodeGeometry
{
    /// <summary>The laid-out geometry of a 2-D barcode in world coordinates.</summary>
    public sealed class Layout
    {
        /// <summary>The quads to render as the barcode's foreground (dark modules, or — when inverted — the
        /// field around the dark modules). On the solder-mask layer these are mask openings.</summary>
        public required IReadOnlyList<CoordPoint[]> Foreground { get; init; }

        /// <summary>Whether the symbol is inverted (foreground is the field, modules are the holes).</summary>
        public required bool Inverted { get; init; }
    }

    /// <summary>
    /// Builds the world-space geometry for a 2-D barcode text, or returns null if <paramref name="text"/> is not
    /// a renderable Data Matrix or QR Code barcode.
    /// </summary>
    public static Layout? TryBuild(PcbText text)
    {
        if (text.TextKind != PcbTextKind.BarCode) return null;

        var payload = !string.IsNullOrEmpty(text.ConvertedString) ? text.ConvertedString! : text.Text;
        if (string.IsNullOrEmpty(payload)) return null;

        return text.BarCodeType switch
        {
            PcbBarCodeKind.DataMatrix or PcbBarCodeKind.QrCode => Build2D(text, payload!),
            PcbBarCodeKind.Code128 or PcbBarCodeKind.Code39 => Build1D(text, payload!),
            _ => null,
        };
    }

    // The 2-D symbologies (Data Matrix, QR): a square grid of modules fitted into the box.
    private static Layout? Build2D(PcbText text, string payload)
    {
        bool[,]? grid = text.BarCodeType switch
        {
            PcbBarCodeKind.DataMatrix => DataMatrixEncoder.TryEncode(payload, out var dm) ? dm!.ToArray() : null,
            PcbBarCodeKind.QrCode => QrCodeEncoder.TryEncode(payload, QrErrorCorrection.Medium, out var qr) ? qr!.ToArray() : null,
            _ => null,
        };
        if (grid is null) return null;

        int rows = grid.GetLength(0), nCols = grid.GetLength(1);
        int n = rows; // both symbologies are square

        // Box (full barcode extent) and quiet-zone margins, in raw world units.
        double boxW = BoxExtent(text.InvertedRectWidth, text.BarCodeFullWidth, n, text.BarCodeMinWidth, text.Height);
        double boxH = BoxExtent(text.InvertedRectHeight, text.BarCodeFullHeight, n, text.BarCodeMinWidth, text.Height);
        if (boxW <= 0 || boxH <= 0) return null;
        double marginX = Math.Max(0, text.BarCodeXMargin.ToRaw());
        double marginY = Math.Max(0, text.BarCodeYMargin.ToRaw());

        // The square module field fits inside the box minus margins; centre it when the box is not square.
        double availW = boxW - 2 * marginX, availH = boxH - 2 * marginY;
        double fieldSide = Math.Min(availW, availH);
        if (fieldSide <= 0) { fieldSide = Math.Min(boxW, boxH); marginX = marginY = 0; }
        double module = fieldSide / n;
        double fieldX = marginX + (availW - fieldSide) / 2.0; // local bottom-left of the module field
        double fieldY = marginY + (availH - fieldSide) / 2.0;

        // Local frame: X right, Y up, origin at the box bottom-left (the text Location), pre-rotation.
        double ox = text.Location.X.ToRaw();
        double oy = text.Location.Y.ToRaw();
        double rad = text.Rotation * Math.PI / 180.0;
        double cos = Math.Cos(rad), sin = Math.Sin(rad);
        int mirror = text.IsMirrored ? -1 : 1;

        CoordPoint ToWorld(double lx, double ly)
        {
            double mx = mirror * lx;
            return new CoordPoint(
                Coord.FromRaw((int)Math.Round(ox + (mx * cos - ly * sin))),
                Coord.FromRaw((int)Math.Round(oy + (mx * sin + ly * cos))));
        }

        CoordPoint[] Rect(double x0, double y0, double x1, double y1)
            => new[] { ToWorld(x0, y0), ToWorld(x1, y0), ToWorld(x1, y1), ToWorld(x0, y1) };

        // Cell rectangle for module [row, col]: row 0 is the top of the symbol (world Y up -> highest Y).
        CoordPoint[] Cell(int row, int col)
        {
            double cx = fieldX + col * module;
            double cy = fieldY + (n - 1 - row) * module;
            return Rect(cx, cy, cx + module, cy + module);
        }

        bool inverted = text.BarCodeInverted;
        var foreground = new List<CoordPoint[]>();

        if (!inverted)
        {
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < nCols; c++)
                    if (grid[r, c]) foreground.Add(Cell(r, c));
        }
        else
        {
            // Inverted: fill the whole box except the dark modules — the quiet-zone frame plus light modules.
            double fr = fieldX + fieldSide, ft = fieldY + fieldSide;
            foreground.Add(Rect(0, 0, fieldX, boxH));          // left margin strip
            foreground.Add(Rect(fr, 0, boxW, boxH));           // right margin strip
            foreground.Add(Rect(fieldX, 0, fr, fieldY));       // bottom margin strip
            foreground.Add(Rect(fieldX, ft, fr, boxH));        // top margin strip
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < nCols; c++)
                    if (!grid[r, c]) foreground.Add(Cell(r, c));
        }

        return new Layout { Foreground = foreground, Inverted = inverted };
    }

    // The 1-D symbologies (Code 128, Code 39): a row of full-height vertical bars fitted into the box.
    private static Layout? Build1D(PcbText text, string payload)
    {
        // Code 128 is implemented; Code 39 falls through to null (rendered as nothing) until needed.
        bool[]? bars = text.BarCodeType == PcbBarCodeKind.Code128 && Code128Encoder.TryEncode(payload, out var c128)
            ? c128
            : null;
        if (bars is null || bars.Length == 0) return null;
        int n = bars.Length;

        double boxW = BoxExtent(text.InvertedRectWidth, text.BarCodeFullWidth, n, text.BarCodeMinWidth, text.Height);
        double boxH = BoxExtent(text.InvertedRectHeight, text.BarCodeFullHeight, n, text.BarCodeMinWidth, text.Height);
        if (boxW <= 0 || boxH <= 0) return null;
        double marginX = Math.Max(0, text.BarCodeXMargin.ToRaw());
        double marginY = Math.Max(0, text.BarCodeYMargin.ToRaw());

        // The bar field fits inside the box minus the quiet-zone margins; bars span its full height.
        double fieldW = boxW - 2 * marginX, fieldH = boxH - 2 * marginY;
        if (fieldW <= 0 || fieldH <= 0) { fieldW = boxW; fieldH = boxH; marginX = marginY = 0; }
        double module = fieldW / n;
        double fieldX = marginX, fieldY = marginY;
        double fieldTop = fieldY + fieldH, fieldRight = fieldX + fieldW;

        // Local frame: X right, Y up, origin at the box bottom-left (the text Location), pre-rotation.
        double ox = text.Location.X.ToRaw();
        double oy = text.Location.Y.ToRaw();
        double rad = text.Rotation * Math.PI / 180.0;
        double cos = Math.Cos(rad), sin = Math.Sin(rad);
        int mirror = text.IsMirrored ? -1 : 1;

        CoordPoint ToWorld(double lx, double ly)
        {
            double mx = mirror * lx;
            return new CoordPoint(
                Coord.FromRaw((int)Math.Round(ox + (mx * cos - ly * sin))),
                Coord.FromRaw((int)Math.Round(oy + (mx * sin + ly * cos))));
        }

        CoordPoint[] Rect(double x0, double y0, double x1, double y1)
            => new[] { ToWorld(x0, y0), ToWorld(x1, y0), ToWorld(x1, y1), ToWorld(x0, y1) };

        // A bar (foreground) is `true`; merge each maximal run into one quad. The complement (spaces) plus the
        // quiet-zone frame is what an INVERTED barcode prints — dark bars then read out of a light field.
        bool inverted = text.BarCodeInverted;
        var foreground = new List<CoordPoint[]>();

        if (!inverted)
        {
            for (int i = 0; i < n;)
            {
                if (!bars[i]) { i++; continue; }
                int j = i; while (j < n && bars[j]) j++;
                foreground.Add(Rect(fieldX + i * module, fieldY, fieldX + j * module, fieldTop));
                i = j;
            }
        }
        else
        {
            foreground.Add(Rect(0, 0, boxW, fieldY));            // bottom margin strip (full width)
            foreground.Add(Rect(0, fieldTop, boxW, boxH));       // top margin strip (full width)
            foreground.Add(Rect(0, fieldY, fieldX, fieldTop));   // left margin strip
            foreground.Add(Rect(fieldRight, fieldY, boxW, fieldTop)); // right margin strip
            for (int i = 0; i < n;)
            {
                if (bars[i]) { i++; continue; }
                int j = i; while (j < n && !bars[j]) j++;
                foreground.Add(Rect(fieldX + i * module, fieldY, fieldX + j * module, fieldTop));
                i = j;
            }
        }

        return new Layout { Foreground = foreground, Inverted = inverted };
    }

    // Box side in raw world units: Altium stores a 2-D barcode's full size in the text-box (inverted-rect)
    // width/height; fall back to the barcode full-width field, then to N modules of the minimum width plus a
    // little quiet zone, then to the text height (sensible defaults for from-scratch barcodes).
    private static double BoxExtent(Coord textBox, Coord barcodeFull, int modules, Coord minModule, Coord height)
    {
        if (textBox > Coord.Zero) return textBox.ToRaw();
        if (barcodeFull > Coord.Zero) return barcodeFull.ToRaw();
        if (minModule > Coord.Zero) return (double)minModule.ToRaw() * (modules + 2);
        if (height > Coord.Zero) return height.ToRaw();
        return 0;
    }
}
