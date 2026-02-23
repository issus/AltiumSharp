namespace OriginalCircuit.Altium;

/// <summary>
/// Base exception for all Altium file processing errors.
/// </summary>
public class AltiumFileException : Exception
{
    /// <summary>
    /// Path of the file being processed, if available.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="AltiumFileException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="filePath">Path of the file being processed, if available.</param>
    /// <param name="innerException">The inner exception, if any.</param>
    public AltiumFileException(string message, string? filePath = null, Exception? innerException = null)
        : base(message, innerException)
    {
        FilePath = filePath;
    }
}

/// <summary>
/// Thrown when an Altium file is structurally corrupt (missing streams, invalid headers, etc.).
/// </summary>
public class AltiumCorruptFileException : AltiumFileException
{
    /// <summary>
    /// Name of the OLE stream where corruption was detected.
    /// </summary>
    public string? StreamName { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="AltiumCorruptFileException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="streamName">Name of the OLE stream where corruption was detected.</param>
    /// <param name="filePath">Path of the file being processed, if available.</param>
    /// <param name="innerException">The inner exception, if any.</param>
    public AltiumCorruptFileException(string message, string? streamName = null, string? filePath = null, Exception? innerException = null)
        : base(message, filePath, innerException)
    {
        StreamName = streamName;
    }
}

/// <summary>
/// Thrown when an Altium file contains a record type or feature that is not supported.
/// </summary>
public class AltiumUnsupportedFeatureException : AltiumFileException
{
    /// <summary>
    /// The unsupported record type number, if applicable.
    /// </summary>
    public int? RecordType { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="AltiumUnsupportedFeatureException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="recordType">The unsupported record type number, if applicable.</param>
    /// <param name="filePath">Path of the file being processed, if available.</param>
    /// <param name="innerException">The inner exception, if any.</param>
    public AltiumUnsupportedFeatureException(string message, int? recordType = null, string? filePath = null, Exception? innerException = null)
        : base(message, filePath, innerException)
    {
        RecordType = recordType;
    }
}
