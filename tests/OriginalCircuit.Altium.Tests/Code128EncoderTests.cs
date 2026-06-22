using OriginalCircuit.Altium.Barcodes;

namespace OriginalCircuit.Altium.Tests;

/// <summary>
/// Tests for the Code 128 (Code Set B) barcode encoder. The reference module patterns below were produced by
/// this encoder and confirmed to decode back to the original text by the independent <c>ZXing.Net</c> reader
/// (PossibleFormats = CODE_128, PureBarcode), so a byte-for-byte match here validates the whole pipeline:
/// the Start B symbol, the per-character values, the modulo-103 symbol check, and the Stop + terminating bar.
/// </summary>
public sealed class Code128EncoderTests
{
    // '#' = bar (dark), '.' = space (light). Each is Start B + data + check + Stop(13 modules).
    private const string RefA = "##.#..#....#.#...##...#...#.##...##...###.#.##";
    private const string RefCode128 = "##.#..#....#...#...##.#...###.##.#.##...#...#...##.#...#..###..##.##..###..#.###.#..##..###..#..##.##...###.#.##";
    private const string RefPanel = "##.#..#....##.###.###.#.#...##...#.#..##....##.###.###.#.#...##...##...#.###.##.###...#.#.#..##....##...#...#.##.###.#...#...###.##.#...##.###.#.#...##...##.###...#.#...###.##.##...#.###.###.#.##...##...###.#.##";

    private static string Encode(string text)
    {
        Assert.True(Code128Encoder.TryEncode(text, out var modules));
        Assert.NotNull(modules);
        return string.Concat(modules!.Select(b => b ? '#' : '.'));
    }

    [Theory]
    [InlineData("A", 46)]
    [InlineData("CODE128", 112)]
    [InlineData("UA_UART_ISOLATOR", 211)]
    public void Encodes_to_expected_module_count(string text, int expected)
        => Assert.Equal(expected, Encode(text).Length);

    [Fact]
    public void Encodes_single_character() => Assert.Equal(RefA, Encode("A"));

    [Fact]
    public void Encodes_mixed_alphanumeric() => Assert.Equal(RefCode128, Encode("CODE128"));

    [Fact]
    public void Encodes_panel_id_payload() => Assert.Equal(RefPanel, Encode("UA_UART_ISOLATOR"));

    [Fact]
    public void Every_symbol_begins_with_a_bar_and_ends_with_a_bar()
    {
        Assert.True(Code128Encoder.TryEncode("Hello, World!", out var modules));
        Assert.True(modules![0]);                 // a Code 128 symbol always starts with a bar...
        Assert.True(modules[^1]);                 // ...and the Stop pattern ends with the terminating bar.
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Rejects_empty_input(string? text) => Assert.False(Code128Encoder.TryEncode(text, out _));

    [Fact]
    public void Rejects_characters_outside_printable_ascii()
        => Assert.False(Code128Encoder.TryEncode("café", out _)); // 'é' (U+00E9) is not in Code Set B
}
