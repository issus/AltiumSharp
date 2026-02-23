using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OriginalCircuit.AltiumSharp.Export.Serialization;

/// <summary>
/// JSON converter for System.Drawing.Color.
/// </summary>
public sealed class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? hex = reader.GetString();
            if (hex != null && hex.StartsWith('#'))
            {
                return ColorTranslator.FromHtml(hex);
            }
        }

        throw new JsonException("Expected hex color string");
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("hex", $"#{value.R:X2}{value.G:X2}{value.B:X2}");
        writer.WriteNumber("r", value.R);
        writer.WriteNumber("g", value.G);
        writer.WriteNumber("b", value.B);
        if (value.A != 255)
        {
            writer.WriteNumber("a", value.A);
        }
        if (value.IsNamedColor)
        {
            writer.WriteString("name", value.Name);
        }
        writer.WriteEndObject();
    }
}

/// <summary>
/// Options provider for AltiumSharp JSON serialization.
/// </summary>
public static class AltiumJsonOptions
{
    /// <summary>
    /// Get configured JsonSerializerOptions for AltiumSharp export.
    /// </summary>
    public static JsonSerializerOptions CreateOptions(bool indented = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new ColorJsonConverter()
            }
        };

        return options;
    }
}
