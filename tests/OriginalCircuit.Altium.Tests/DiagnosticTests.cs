using OriginalCircuit.Altium.Diagnostics;

namespace OriginalCircuit.Altium.Tests;

public class DiagnosticTests
{
    [Fact]
    public void AltiumFileException_IsException()
    {
        var ex = new AltiumFileException("test");
        Assert.IsAssignableFrom<Exception>(ex);
        Assert.Equal("test", ex.Message);
    }

    [Fact]
    public void AltiumCorruptFileException_IsAltiumFileException()
    {
        var ex = new AltiumCorruptFileException("corrupt", streamName: "TestStream");
        Assert.IsAssignableFrom<AltiumFileException>(ex);
        Assert.Equal("TestStream", ex.StreamName);
        Assert.Contains("corrupt", ex.Message);
    }

    [Fact]
    public void AltiumCorruptFileException_WithInnerException()
    {
        var inner = new InvalidDataException("bad data");
        var ex = new AltiumCorruptFileException("corrupt", streamName: "Stream1", innerException: inner);
        Assert.Same(inner, ex.InnerException);
        Assert.Equal("Stream1", ex.StreamName);
    }

    [Fact]
    public void AltiumUnsupportedFeatureException_IsAltiumFileException()
    {
        var ex = new AltiumUnsupportedFeatureException("unsupported feature", 42);
        Assert.IsAssignableFrom<AltiumFileException>(ex);
        Assert.Equal(42, ex.RecordType);
    }

    [Fact]
    public void AltiumDiagnostic_Record_Properties()
    {
        var diag = new AltiumDiagnostic(
            DiagnosticSeverity.Warning,
            "Test warning",
            "Data",
            5);

        Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
        Assert.Equal("Test warning", diag.Message);
        Assert.Equal("Data", diag.StreamName);
        Assert.Equal(5, diag.RecordIndex);
    }

    [Fact]
    public void AltiumDiagnostic_OptionalFields_DefaultToNull()
    {
        var diag = new AltiumDiagnostic(DiagnosticSeverity.Info, "info");
        Assert.Null(diag.StreamName);
        Assert.Null(diag.RecordIndex);
    }

    [Fact]
    public void AltiumDiagnostic_Equality_Works()
    {
        var a = new AltiumDiagnostic(DiagnosticSeverity.Error, "error", "Stream1", 1);
        var b = new AltiumDiagnostic(DiagnosticSeverity.Error, "error", "Stream1", 1);
        Assert.Equal(a, b);
    }

    [Fact]
    public void DiagnosticSeverity_HasExpectedValues()
    {
        Assert.Equal(0, (int)DiagnosticSeverity.Info);
        Assert.Equal(1, (int)DiagnosticSeverity.Warning);
        Assert.Equal(2, (int)DiagnosticSeverity.Error);
    }
}
