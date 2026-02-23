namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a 3D model embedded in a PCB library.
/// Each model contains STEP data and metadata linking it to component bodies.
/// </summary>
public sealed class PcbModel
{
    /// <summary>
    /// Unique identifier (GUID) for this model. Referenced by <see cref="PcbComponentBody.ModelId"/>.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Original filename of the STEP model (e.g., "PSEMI QFN-24 4x4.step").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether the model data is embedded in the library file.
    /// </summary>
    public bool IsEmbedded { get; set; } = true;

    /// <summary>
    /// Model source type (typically "Undefined" for embedded models).
    /// </summary>
    public string ModelSource { get; set; } = "Undefined";

    /// <summary>
    /// X-axis rotation in degrees.
    /// </summary>
    public double RotationX { get; set; }

    /// <summary>
    /// Y-axis rotation in degrees.
    /// </summary>
    public double RotationY { get; set; }

    /// <summary>
    /// Z-axis rotation in degrees.
    /// </summary>
    public double RotationZ { get; set; }

    /// <summary>
    /// Z-axis offset.
    /// </summary>
    public int Dz { get; set; }

    /// <summary>
    /// Checksum value computed by Altium's 3D engine.
    /// The algorithm is proprietary; this value is preserved through round-trips
    /// and set to 0 for newly created models.
    /// </summary>
    public int Checksum { get; set; }

    /// <summary>
    /// The STEP model text data (ISO-10303-21 format).
    /// Stored compressed (zlib) in the file; decompressed here for direct access.
    /// </summary>
    public string StepData { get; set; } = string.Empty;
}
