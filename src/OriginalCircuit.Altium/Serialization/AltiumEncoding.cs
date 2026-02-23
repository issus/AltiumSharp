using System.Text;

namespace OriginalCircuit.Altium.Serialization;

/// <summary>
/// Provides encoding support for Altium file formats.
/// </summary>
internal static class AltiumEncoding
{
    private static bool _initialized;
    private static Encoding? _windows1252;

    /// <summary>
    /// Gets the Windows-1252 encoding used by Altium for ASCII strings.
    /// </summary>
    public static Encoding Windows1252
    {
        get
        {
            EnsureInitialized();
            return _windows1252!;
        }
    }

    /// <summary>
    /// Ensures encoding providers are registered.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (_initialized)
            return;

        // Register the code pages encoding provider for Windows-1252 support
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _windows1252 = Encoding.GetEncoding(1252);
        _initialized = true;
    }
}
