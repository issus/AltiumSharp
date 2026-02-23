namespace OriginalCircuit.Altium.Rendering;

/// <summary>
/// Default color mapping for PCB layers, matching Altium's default layer colors.
/// Layer IDs: 1=Top, 2-31=Mid1-30, 32=Bottom, 33=TopOverlay, 34=BottomOverlay,
/// 35=TopPaste, 36=BottomPaste, 37=TopSolder, 38=BottomSolder, 39-54=InternalPlane1-16,
/// 55=DrillGuide, 56=KeepOut, 57-72=Mechanical1-16, 73=DrillDrawing, 74=MultiLayer,
/// 81=PadHole, 82=ViaHole.
/// </summary>
public static class LayerColors
{
    // Colors are stored as ARGB (0xAARRGGBB).
    // V1 source uses Win32 BGR format (0x00BBGGRR) via ColorTranslator.FromWin32.
    // Conversion: R = bgr & 0xFF, G = (bgr >> 8) & 0xFF, B = (bgr >> 16) & 0xFF → 0xFF_RR_GG_BB
    private static readonly Dictionary<int, uint> DefaultColors = new()
    {
        // Signal layers
        [1]  = 0xFFFF0000, // Top Layer (BGR 0x0000FF → Red)
        [2]  = 0xFFBC8E00, // Mid-Layer 1 (BGR 0x008EBC)
        [3]  = 0xFF70DBFA, // Mid-Layer 2 (BGR 0xFADB70)
        [4]  = 0xFF00CC66, // Mid-Layer 3 (BGR 0x66CC00)
        [5]  = 0xFF9966FF, // Mid-Layer 4 (BGR 0xFF6699)
        [6]  = 0xFF00FFFF, // Mid-Layer 5 (BGR 0xFFFF00)
        [7]  = 0xFF800080, // Mid-Layer 6 (BGR 0x800080)
        [8]  = 0xFFFF00FF, // Mid-Layer 7 (BGR 0xFF00FF)
        [9]  = 0xFF808000, // Mid-Layer 8 (BGR 0x008080)
        [10] = 0xFFFFFF00, // Mid-Layer 9 (BGR 0x00FFFF)
        [11] = 0xFF808080, // Mid-Layer 10 (BGR 0x808080)
        [12] = 0xFFFFFFFF, // Mid-Layer 11 (BGR 0xFFFFFF)
        [13] = 0xFF800080, // Mid-Layer 12 (BGR 0x800080)
        [14] = 0xFF008080, // Mid-Layer 13 (BGR 0x808000)
        [15] = 0xFFC0C0C0, // Mid-Layer 14 (BGR 0xC0C0C0)
        [16] = 0xFF800000, // Mid-Layer 15 (BGR 0x000080)
        [17] = 0xFF008000, // Mid-Layer 16 (BGR 0x008000)
        [18] = 0xFF00FF00, // Mid-Layer 17 (BGR 0x00FF00)
        [19] = 0xFF000080, // Mid-Layer 18 (BGR 0x800000)
        [20] = 0xFF00FFFF, // Mid-Layer 19 (BGR 0xFFFF00)
        [21] = 0xFF800080, // Mid-Layer 20 (BGR 0x800080)
        [22] = 0xFFFF00FF, // Mid-Layer 21 (BGR 0xFF00FF)
        [23] = 0xFF808000, // Mid-Layer 22 (BGR 0x008080)
        [24] = 0xFFFFFF00, // Mid-Layer 23 (BGR 0x00FFFF)
        [25] = 0xFF808080, // Mid-Layer 24 (BGR 0x808080)
        [26] = 0xFFFFFFFF, // Mid-Layer 25 (BGR 0xFFFFFF)
        [27] = 0xFF800080, // Mid-Layer 26 (BGR 0x800080)
        [28] = 0xFF008080, // Mid-Layer 27 (BGR 0x808000)
        [29] = 0xFFC0C0C0, // Mid-Layer 28 (BGR 0xC0C0C0)
        [30] = 0xFF800000, // Mid-Layer 29 (BGR 0x000080)
        [31] = 0xFF008000, // Mid-Layer 30 (BGR 0x008000)
        [32] = 0xFF0000FF, // Bottom Layer (BGR 0xFF0000 → Blue)

        // Overlay (silkscreen)
        [33] = 0xFFFFFF00, // Top Overlay (BGR 0x00FFFF → Yellow)
        [34] = 0xFF808000, // Bottom Overlay (BGR 0x008080 → Teal)

        // Paste
        [35] = 0xFF808080, // Top Paste (BGR 0x808080 → Gray)
        [36] = 0xFF800000, // Bottom Paste (BGR 0x000080 → Dark Red... actually Navy)

        // Solder mask
        [37] = 0xFF800080, // Top Solder (BGR 0x800080 → Purple)
        [38] = 0xFFFF00FF, // Bottom Solder (BGR 0xFF00FF → Magenta)

        // Internal planes
        [39] = 0xFF008000, // Internal Plane 1 (BGR 0x008000 → Green)
        [40] = 0xFF800000, // Internal Plane 2 (BGR 0x000080)
        [41] = 0xFF800080, // Internal Plane 3 (BGR 0x800080)
        [42] = 0xFF008080, // Internal Plane 4 (BGR 0x808000)
        [43] = 0xFF008000, // Internal Plane 5 (BGR 0x008000)
        [44] = 0xFF800000, // Internal Plane 6 (BGR 0x000080)
        [45] = 0xFF800080, // Internal Plane 7 (BGR 0x800080)
        [46] = 0xFF008080, // Internal Plane 8 (BGR 0x808000)
        [47] = 0xFF008000, // Internal Plane 9 (BGR 0x008000)
        [48] = 0xFF800000, // Internal Plane 10 (BGR 0x000080)
        [49] = 0xFF800080, // Internal Plane 11 (BGR 0x800080)
        [50] = 0xFF008080, // Internal Plane 12 (BGR 0x808000)
        [51] = 0xFF008000, // Internal Plane 13 (BGR 0x008000)
        [52] = 0xFF800000, // Internal Plane 14 (BGR 0x000080)
        [53] = 0xFF800080, // Internal Plane 15 (BGR 0x800080)
        [54] = 0xFF008080, // Internal Plane 16 (BGR 0x808000)

        // Utility layers
        [55] = 0xFF800000, // Drill Guide (BGR 0x000080)
        [56] = 0xFFFF00FF, // Keep-Out Layer (BGR 0xFF00FF)

        // Mechanical layers
        [57] = 0xFFFF00FF, // Mechanical 1 (BGR 0xFF00FF)
        [58] = 0xFF800080, // Mechanical 2 (BGR 0x800080)
        [59] = 0xFF008000, // Mechanical 3 (BGR 0x008000)
        [60] = 0xFF808000, // Mechanical 4 (BGR 0x008080)
        [61] = 0xFFFF00FF, // Mechanical 5 (BGR 0xFF00FF)
        [62] = 0xFF800080, // Mechanical 6 (BGR 0x800080)
        [63] = 0xFF008000, // Mechanical 7 (BGR 0x008000)
        [64] = 0xFF808000, // Mechanical 8 (BGR 0x008080)
        [65] = 0xFFFF00FF, // Mechanical 9 (BGR 0xFF00FF)
        [66] = 0xFF800080, // Mechanical 10 (BGR 0x800080)
        [67] = 0xFF008000, // Mechanical 11 (BGR 0x008000)
        [68] = 0xFF808000, // Mechanical 12 (BGR 0x008080)
        [69] = 0xFFFF00FF, // Mechanical 13 (BGR 0xFF00FF)
        [70] = 0xFF800080, // Mechanical 14 (BGR 0x800080)
        [71] = 0xFF008000, // Mechanical 15 (BGR 0x008000)
        [72] = 0xFF000000, // Mechanical 16 (BGR 0x000000 → Black)

        // Other layers
        [73] = 0xFFFF002A, // Drill Drawing (BGR 0x2A00FF)
        [74] = 0xFFC0C0C0, // Multi-Layer (BGR 0xC0C0C0 → Silver)

        // Special layers
        [81] = 0xFF009190, // Pad Holes (BGR 0x909100)
        [82] = 0xFF816200, // Via Holes (BGR 0x006281)
    };

    /// <summary>
    /// Gets the default ARGB color for a PCB layer.
    /// </summary>
    public static uint GetColor(int layerId)
    {
        return DefaultColors.TryGetValue(layerId, out var color) ? color : 0xFF808080;
    }

    /// <summary>
    /// Gets the draw priority for a PCB layer (lower = drawn first / below).
    /// Draw priorities match V1's LayerMetadata ordering.
    /// </summary>
    public static int GetDrawPriority(int layerId)
    {
        return layerId switch
        {
            32 => 0,                                      // Bottom Layer
            38 => 1,                                      // Bottom Solder
            36 => 2,                                      // Bottom Paste
            34 => 3,                                      // Bottom Overlay
            _ when layerId >= 2 && layerId <= 31 => 6,    // Mid layers
            1 => 6,                                       // Top Layer (same priority as mid)
            37 => 9,                                      // Top Solder
            35 => 7,                                      // Top Paste
            33 => 2,                                      // Top Overlay
            74 => 1,                                      // Multi Layer
            _ when layerId >= 39 && layerId <= 54 => 11,  // Internal planes
            55 => 12,                                     // Drill Guide
            56 => 13,                                     // Keep-Out
            _ when layerId >= 57 && layerId <= 72 => 14,  // Mechanical
            73 => 15,                                     // Drill Drawing
            81 => 1,                                      // Pad Holes
            82 => 1,                                      // Via Holes
            _ => 50                                       // Other/unknown
        };
    }
}
