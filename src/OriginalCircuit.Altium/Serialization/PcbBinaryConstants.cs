namespace OriginalCircuit.Altium.Serialization;

/// <summary>
/// Constants for the PCB binary file format shared between readers and writers.
/// </summary>
internal static class PcbBinaryConstants
{
    /// <summary>Bit 2: Unlocked flag (inverted — 0 means locked).</summary>
    internal const ushort FlagUnlocked = 0x04;

    /// <summary>Bit 5: Tenting top.</summary>
    internal const ushort FlagTentingTop = 0x20;

    /// <summary>Bit 6: Tenting bottom.</summary>
    internal const ushort FlagTentingBottom = 0x40;

    /// <summary>Bit 9: Keepout region.</summary>
    internal const ushort FlagKeepout = 0x200;

    /// <summary>
    /// Decodes primitive flags into individual boolean properties.
    /// </summary>
    internal static void DecodeFlags(ushort flags, out bool isLocked, out bool isTentingTop,
        out bool isTentingBottom, out bool isKeepout)
    {
        isLocked = (flags & FlagUnlocked) == 0;
        isTentingTop = (flags & FlagTentingTop) != 0;
        isTentingBottom = (flags & FlagTentingBottom) != 0;
        isKeepout = (flags & FlagKeepout) != 0;
    }
}
