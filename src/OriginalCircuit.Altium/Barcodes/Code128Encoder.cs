using System;
using System.Collections.Generic;

namespace OriginalCircuit.Altium.Barcodes;

/// <summary>
/// Encodes a string as a Code 128 (1-D) barcode and returns the bar/space module pattern: a flat array of
/// equal-width modules where <c>true</c> is a bar (dark) and <c>false</c> is a space (light). The pattern
/// includes the Start B symbol, the modulo-103 symbol check, the Stop symbol and its terminating bar; the
/// caller applies the bar width (X-dimension) and quiet zone when laying it into a box.
/// </summary>
/// <remarks>
/// Code Set B is used throughout — it covers all printable ASCII (32..126), which is what Altium stores for a
/// silkscreen ID barcode — so any single-byte text round-trips to the same decoded value regardless of the
/// code-set optimisation a generator might otherwise choose.
/// </remarks>
public static class Code128Encoder
{
    // Element-width patterns for symbol values 0..106 (bar,space,bar,space,bar,space). Each sums to 11
    // modules; the Stop (106) is 7 elements summing to 13 (it carries the terminating bar). This is the
    // canonical Code 128 symbology table.
    private static readonly string[] Patterns =
    {
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", // 0-7
        "132212", "221213", "221312", "231212", "112232", "122132", "122231", "113222", // 8-15
        "123122", "123221", "223211", "221132", "221231", "213212", "223112", "312131", // 16-23
        "311222", "321122", "321221", "312212", "322112", "322211", "212123", "212321", // 24-31
        "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313", // 32-39
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", // 40-47
        "313121", "211331", "231131", "213113", "213311", "213131", "311123", "311321", // 48-55
        "331121", "312113", "312311", "332111", "314111", "221411", "431111", "111224", // 56-63
        "111422", "121124", "121421", "141122", "141221", "112214", "112412", "122114", // 64-71
        "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111", // 72-79
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", // 80-87
        "421211", "212141", "214121", "412121", "111143", "111341", "131141", "114113", // 88-95
        "114311", "411113", "411311", "113141", "114131", "311141", "411131", "211412", // 96-103
        "211214", "211232", "2331112",                                                  // 104-106 (StartB, StartC, Stop)
    };

    private const int StartB = 104;
    private const int Stop = 106;

    /// <summary>
    /// Encodes <paramref name="text"/> as a Code 128 B module pattern. Returns false (with a null pattern) if
    /// the text is empty or contains a character outside printable ASCII (32..126).
    /// </summary>
    public static bool TryEncode(string? text, out bool[]? modules)
    {
        modules = null;
        if (string.IsNullOrEmpty(text)) return false;
        foreach (char ch in text!)
            if (ch < 32 || ch > 126) return false;

        // Symbol values: Start B, each character (value = ASCII − 32), the modulo-103 check, Stop.
        long sum = StartB;
        var values = new List<int>(text.Length + 3) { StartB };
        for (int i = 0; i < text.Length; i++)
        {
            int v = text[i] - 32;
            values.Add(v);
            sum += (long)v * (i + 1); // weight is the 1-based position of the data symbol
        }
        values.Add((int)(sum % 103));
        values.Add(Stop);

        var bits = new List<bool>(values.Count * 11 + 2);
        foreach (int v in values)
        {
            string pattern = Patterns[v];
            bool bar = true; // every symbol pattern begins with a bar
            foreach (char w in pattern)
            {
                int width = w - '0';
                for (int k = 0; k < width; k++) bits.Add(bar);
                bar = !bar;
            }
        }

        modules = bits.ToArray();
        return true;
    }
}
