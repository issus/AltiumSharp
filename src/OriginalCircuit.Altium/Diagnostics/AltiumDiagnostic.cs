namespace OriginalCircuit.Altium.Diagnostics;

/// <summary>
/// Severity levels for diagnostics emitted during file processing.
/// </summary>
public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// A diagnostic message emitted during Altium file reading/writing.
/// </summary>
/// <param name="Severity">The severity level of this diagnostic.</param>
/// <param name="Message">Human-readable description of the issue.</param>
/// <param name="StreamName">The OLE stream name where the issue occurred, if applicable.</param>
/// <param name="RecordIndex">The record index where the issue occurred, if applicable.</param>
public sealed record AltiumDiagnostic(
    DiagnosticSeverity Severity,
    string Message,
    string? StreamName = null,
    int? RecordIndex = null);
