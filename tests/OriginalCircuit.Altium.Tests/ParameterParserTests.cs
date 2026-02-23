using OriginalCircuit.Altium.Serialization;

namespace OriginalCircuit.Altium.Tests;

public class ParameterParserTests
{
    [Fact]
    public void Parse_SimpleKeyValue_ExtractsCorrectly()
    {
        var parser = new ParameterParser("|NAME=Test|VALUE=123|");

        Assert.True(parser.MoveNext());
        Assert.True(parser.CurrentName.SequenceEqual("NAME".AsSpan()));
        Assert.True(parser.CurrentValue.SequenceEqual("Test".AsSpan()));

        Assert.True(parser.MoveNext());
        Assert.True(parser.CurrentName.SequenceEqual("VALUE".AsSpan()));
        Assert.True(parser.CurrentValue.SequenceEqual("123".AsSpan()));

        Assert.False(parser.MoveNext());
    }

    [Fact]
    public void Parse_NoLeadingSeparator_WorksCorrectly()
    {
        var parser = new ParameterParser("NAME=Test|VALUE=123");

        Assert.True(parser.MoveNext());
        Assert.True(parser.CurrentName.SequenceEqual("NAME".AsSpan()));

        Assert.True(parser.MoveNext());
        Assert.True(parser.CurrentName.SequenceEqual("VALUE".AsSpan()));

        Assert.False(parser.MoveNext());
    }

    [Fact]
    public void Parse_EmptyEntries_AreSkipped()
    {
        var parser = new ParameterParser("||NAME=Test|||VALUE=123||");

        Assert.True(parser.MoveNext());
        Assert.True(parser.CurrentName.SequenceEqual("NAME".AsSpan()));

        Assert.True(parser.MoveNext());
        Assert.True(parser.CurrentName.SequenceEqual("VALUE".AsSpan()));

        Assert.False(parser.MoveNext());
    }

    [Fact]
    public void Parse_Utf8Prefix_IsRecognized()
    {
        var parser = new ParameterParser("|%UTF8%DESCRIPTION=Test Description|");

        Assert.True(parser.MoveNext());
        Assert.True(parser.CurrentIsUtf8);
        Assert.True(parser.CurrentName.SequenceEqual("DESCRIPTION".AsSpan()));
        Assert.True(parser.CurrentValue.SequenceEqual("Test Description".AsSpan()));
    }

    [Fact]
    public void Parse_TrailingWhitespace_IsTrimmed()
    {
        var parser = new ParameterParser("|NAME=Test\r\n|");

        Assert.True(parser.MoveNext());
        Assert.True(parser.CurrentValue.SequenceEqual("Test".AsSpan()));
    }

    [Fact]
    public void ParameterEntry_GetInt32_ParsesCorrectly()
    {
        var parser = new ParameterParser("|VALUE=42|");

        Assert.True(parser.MoveNext());
        var entry = parser.Current;
        Assert.True(entry.TryGetInt32(out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void ParameterEntry_GetDouble_ParsesCorrectly()
    {
        var parser = new ParameterParser("|VALUE=3.14159|");

        Assert.True(parser.MoveNext());
        var entry = parser.Current;
        Assert.True(entry.TryGetDouble(out var value));
        Assert.Equal(3.14159, value, precision: 5);
    }

    [Fact]
    public void ParameterEntry_GetBool_RecognizesTrueValues()
    {
        var testCases = new[] { "T", "t", "TRUE", "true", "True" };

        foreach (var testValue in testCases)
        {
            var parser = new ParameterParser($"|FLAG={testValue}|");
            Assert.True(parser.MoveNext());
            Assert.True(parser.Current.GetBool(), $"Expected '{testValue}' to be true");
        }
    }

    [Fact]
    public void ParameterEntry_GetBool_RecognizesFalseValues()
    {
        var testCases = new[] { "F", "f", "FALSE", "false", "" };

        foreach (var testValue in testCases)
        {
            var parser = new ParameterParser($"|FLAG={testValue}|");
            Assert.True(parser.MoveNext());
            Assert.False(parser.Current.GetBool(), $"Expected '{testValue}' to be false");
        }
    }

    [Fact]
    public void ParameterEntry_NameEquals_IsCaseInsensitive()
    {
        var parser = new ParameterParser("|MYPARAMETER=value|");

        Assert.True(parser.MoveNext());
        Assert.True(parser.Current.NameEquals("myparameter".AsSpan()));
        Assert.True(parser.Current.NameEquals("MYPARAMETER".AsSpan()));
        Assert.True(parser.Current.NameEquals("MyParameter".AsSpan()));
    }

    [Fact]
    public void Foreach_EnumeratesAllEntries()
    {
        var data = "|A=1|B=2|C=3|";
        var entries = new List<(string Name, string Value)>();

        foreach (var entry in data.ParseParameters())
        {
            entries.Add((entry.Name.ToString(), entry.Value.ToString()));
        }

        Assert.Equal(3, entries.Count);
        Assert.Equal(("A", "1"), entries[0]);
        Assert.Equal(("B", "2"), entries[1]);
        Assert.Equal(("C", "3"), entries[2]);
    }

    [Fact]
    public void TryGetParameter_FindsExistingParameter()
    {
        var data = "|NAME=TestComponent|HEIGHT=100|WIDTH=50|";

        Assert.True(data.TryGetParameter("HEIGHT", out var height));
        Assert.Equal("100", height);

        Assert.True(data.TryGetParameter("name", out var name)); // case insensitive
        Assert.Equal("TestComponent", name);
    }

    [Fact]
    public void TryGetParameter_ReturnsFalseForMissing()
    {
        var data = "|NAME=TestComponent|";

        Assert.False(data.TryGetParameter("NOTEXIST", out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Parse_NestedLevel_UsesBacktickSeparator()
    {
        var parser = new ParameterParser("`A=1`B=2`", level: 1);

        Assert.True(parser.MoveNext());
        Assert.True(parser.CurrentName.SequenceEqual("A".AsSpan()));

        Assert.True(parser.MoveNext());
        Assert.True(parser.CurrentName.SequenceEqual("B".AsSpan()));

        Assert.False(parser.MoveNext());
    }

    [Fact]
    public void Parse_EmptyValue_ReturnsEmpty()
    {
        var parser = new ParameterParser("|NAME=|");

        Assert.True(parser.MoveNext());
        Assert.True(parser.CurrentName.SequenceEqual("NAME".AsSpan()));
        Assert.Equal(string.Empty, parser.CurrentValue.ToString());
    }

    [Fact]
    public void Parse_ValueWithEquals_PreservesAfterFirstEquals()
    {
        var parser = new ParameterParser("|EXPR=A=B|");

        Assert.True(parser.MoveNext());
        Assert.True(parser.CurrentName.SequenceEqual("EXPR".AsSpan()));
        Assert.Equal("A=B", parser.CurrentValue.ToString());
    }

    [Fact]
    public void Parse_EmptyInput_YieldsNoEntries()
    {
        var parser = new ParameterParser("");
        Assert.False(parser.MoveNext());
    }

    [Fact]
    public void Parse_OnlyPipes_YieldsNoEntries()
    {
        var parser = new ParameterParser("||||");
        Assert.False(parser.MoveNext());
    }

    [Fact]
    public void Parse_DuplicateKeys_LastValueUsed()
    {
        // When consumed via TryGetParameter (dictionary), last value wins
        var data = "|KEY=first|KEY=second|";
        Assert.True(data.TryGetParameter("KEY", out var value));
        // TryGetParameter scans linearly and returns first match
        Assert.Equal("first", value);
    }

    [Fact]
    public void Parse_RealWorldPcbData_ParsesCorrectly()
    {
        // Simulated real Altium parameter string
        var data = "|RECORD=Component|PATTERN=RESISTOR_0402|HEIGHT=50000|DESCRIPTION=SMD Resistor 0402|";
        var parser = new ParameterParser(data);

        // RECORD
        Assert.True(parser.MoveNext());
        Assert.True(parser.Current.NameEquals("RECORD".AsSpan()));
        Assert.Equal("Component", parser.Current.GetString());

        // PATTERN
        Assert.True(parser.MoveNext());
        Assert.True(parser.Current.NameEquals("PATTERN".AsSpan()));
        Assert.Equal("RESISTOR_0402", parser.Current.GetString());

        // HEIGHT
        Assert.True(parser.MoveNext());
        Assert.True(parser.Current.NameEquals("HEIGHT".AsSpan()));
        Assert.Equal(50000, parser.Current.GetInt32OrDefault());

        // DESCRIPTION
        Assert.True(parser.MoveNext());
        Assert.True(parser.Current.NameEquals("DESCRIPTION".AsSpan()));
        Assert.Equal("SMD Resistor 0402", parser.Current.GetString());

        Assert.False(parser.MoveNext());
    }
}
