namespace OriginalCircuit.Altium.Serialization.Writers;

/// <summary>
/// Shared utility methods for Altium file writers.
/// </summary>
internal static class WriterUtilities
{
    /// <summary>
    /// Converts a component name to a compound file storage key by truncating to 31 chars
    /// and replacing '/' with '_'.
    /// </summary>
    internal static string GetSectionKeyFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "_";

        var maxLength = Math.Min(name.Length, 31);
        return name.Substring(0, maxLength).Replace('/', '_');
    }
}
