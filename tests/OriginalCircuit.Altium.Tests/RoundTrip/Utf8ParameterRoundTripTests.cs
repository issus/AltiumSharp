using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Serialization.Readers;
using OriginalCircuit.Altium.Serialization.Writers;

namespace OriginalCircuit.Altium.Tests.RoundTrip;

/// <summary>
/// Regression tests for %UTF8%-prefixed parameter values. The surrounding parameter block is
/// Windows-1252; a UTF-8 value is exposed on <see cref="SchParameter.Value"/> as decoded Unicode
/// (not mojibake), and non-Latin-1 text survives a modify-then-write instead of becoming '?'.
/// </summary>
public sealed class Utf8ParameterRoundTripTests
{
    [Theory]
    [InlineData("café")]      // combining accent
    [InlineData("Résistance")]  // Latin-1 representable (e-acute)
    [InlineData("Ω µ — π")] // Omega/pi are NOT Windows-1252 representable
    [InlineData("日本語テスト")] // CJK, 3-byte UTF-8
    [InlineData("Δοκιμή")]   // Greek
    public void SchLib_NonAsciiParameterValue_RoundTrips(string text)
    {
        var library = new SchLibrary();
        var component = new SchComponent { Name = "C1", PartCount = 1 };
        component.AddParameter(new SchParameter { Name = "Manufacturer", Value = text });
        library.Add(component);

        using var ms = new MemoryStream();
        new SchLibWriter().Write(library, ms);
        ms.Position = 0;
        var readBack = (SchLibrary)new SchLibReader().Read(ms);

        var param = readBack.Components.First().Parameters.First(p => p.Name == "Manufacturer");
        Assert.Equal(text, param.Value);
    }

    [Fact]
    public void SchDoc_Utf8FlaggedParameter_RoundTripsValueAndFlag()
    {
        const string text = "Résistance Ω"; // includes a non-1252 char
        var doc = new SchDocument();
        var comp = new SchComponent { Name = "R1", PartCount = 1 };
        comp.AddParameter(new SchParameter { Name = "Manufacturer", Value = text, TextIsUtf8 = true });
        doc.AddComponent(comp);

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        var param = (SchParameter)readBack.Components.First().Parameters.First(p => p.Name == "Manufacturer");
        Assert.Equal(text, param.Value);
        Assert.True(param.TextIsUtf8);
    }

    /// <summary>
    /// Altium writes non-ASCII values twice: a %UTF8% variant first, then a plain-ANSI twin
    /// (e.g. <c>|%UTF8%ComponentDescription=KON 4.7ÂµF|ComponentDescription=KON 4.7µF|</c>).
    /// Both collapse onto one key when the prefix is stripped; the UTF-8 variant must win and be
    /// decoded, not returned as mojibake (bd-23s).
    /// </summary>
    [Fact]
    public void ParameterCollection_Utf8AndAnsiDuplicate_ReturnsDecodedValue()
    {
        var collection = OriginalCircuit.Altium.Primitives.ParameterCollection.Parse(
            "|RECORD=1|%UTF8%ComponentDescription=KON 4.7ÂµF|ComponentDescription=KON 4.7µF|PartCount=2");

        Assert.Equal("KON 4.7µF", collection["ComponentDescription"].AsStringOrDefault());
    }

    [Theory]
    [InlineData("KON 4.7µF ±10%")] // Windows-1252 representable → plain ComponentDescription key
    [InlineData("Shunt 5mΩ")]      // Ω is not 1252-representable → %UTF8%ComponentDescription key
    public void SchDoc_NonAsciiComponentDescription_RoundTrips(string description)
    {
        var doc = new SchDocument();
        doc.AddComponent(new SchComponent { Name = "R1", PartCount = 1, Description = description });

        using var ms = new MemoryStream();
        new SchDocWriter().Write(doc, ms);
        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Equal(description, readBack.Components.First().Description);
    }
}
