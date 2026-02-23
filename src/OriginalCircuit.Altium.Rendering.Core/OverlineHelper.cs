namespace OriginalCircuit.Altium.Rendering;

/// <summary>
/// Parses Altium's overline escape sequences in pin names and text.
/// A backslash (\) toggles overline mode: characters between pairs of backslashes
/// are rendered with an overline.
/// </summary>
public static class OverlineHelper
{
    /// <summary>
    /// Represents a segment of text with an overline flag.
    /// </summary>
    public readonly record struct TextSegment(string Text, bool HasOverline);

    /// <summary>
    /// Parses a string containing backslash overline markers into segments.
    /// Each backslash toggles overline state. The backslashes themselves are not
    /// included in the output text.
    /// </summary>
    public static List<TextSegment> Parse(string? text)
    {
        var segments = new List<TextSegment>();
        if (string.IsNullOrEmpty(text)) return segments;

        bool overline = false;
        int start = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\')
            {
                // Emit segment before the backslash
                if (i > start)
                {
                    segments.Add(new TextSegment(text[start..i], overline));
                }
                overline = !overline;
                start = i + 1;
            }
        }

        // Emit remaining text
        if (start < text.Length)
        {
            segments.Add(new TextSegment(text[start..], overline));
        }

        return segments;
    }

    /// <summary>
    /// Returns the display text (backslashes removed) from a string with overline markers.
    /// </summary>
    public static string GetDisplayText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Replace("\\", "");
    }
}
