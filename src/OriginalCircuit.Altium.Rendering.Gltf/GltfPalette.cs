using OriginalCircuit.Mech.GLTF;

namespace OriginalCircuit.Altium.Rendering.Gltf;

/// <summary>
/// The PBR material palette for a rendered board. Colours are authored in sRGB (how a PCB actually
/// looks) and converted to the linear RGB that glTF <c>baseColorFactor</c> expects.
/// </summary>
internal static class GltfPalette
{
    private static double SrgbToLinear(double c)
        => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    private static double[] Rgba(double r, double g, double b, double a = 1.0)
        => [SrgbToLinear(r), SrgbToLinear(g), SrgbToLinear(b), a];

    /// <summary>FR-4 laminate — an olive/tan dielectric, fully rough and non-metallic.</summary>
    public static MaterialSpec Substrate { get; } = new()
    {
        Name = "FR4",
        BaseColorFactor = Rgba(0.62, 0.52, 0.30),
        MetallicFactor = 0.0,
        RoughnessFactor = 0.85,
    };

    /// <summary>Solder mask — translucent green so copper traces read through it.</summary>
    public static MaterialSpec SolderMask { get; } = new()
    {
        Name = "SolderMask",
        BaseColorFactor = Rgba(0.05, 0.32, 0.14, 0.78),
        MetallicFactor = 0.0,
        RoughnessFactor = 0.5,
        AlphaMode = "BLEND",
        DoubleSided = true,
    };

    /// <summary>Silkscreen — matte white print.</summary>
    public static MaterialSpec Silkscreen { get; } = new()
    {
        Name = "Silkscreen",
        BaseColorFactor = Rgba(0.93, 0.93, 0.90),
        MetallicFactor = 0.0,
        RoughnessFactor = 0.85,
        DoubleSided = true,
    };

    /// <summary>Solder paste stencil — dull metallic grey.</summary>
    public static MaterialSpec Paste { get; } = new()
    {
        Name = "Paste",
        BaseColorFactor = Rgba(0.66, 0.66, 0.69),
        MetallicFactor = 0.6,
        RoughnessFactor = 0.5,
        DoubleSided = true,
    };

    /// <summary>A neutral dark-grey plastic material for component bodies that carry no STEP colour.</summary>
    public static MaterialSpec Component() => new()
    {
        Name = "Component",
        BaseColorFactor = Rgba(0.18, 0.18, 0.20),
        MetallicFactor = 0.1,
        RoughnessFactor = 0.6,
        DoubleSided = true,
    };

    /// <summary>The copper material for the requested surface finish.</summary>
    public static MaterialSpec Copper(GltfCopperFinish finish, bool doubleSided)
    {
        var (color, rough) = finish switch
        {
            GltfCopperFinish.Enig => (Rgba(0.92, 0.73, 0.36), 0.35),
            GltfCopperFinish.Hasl => (Rgba(0.78, 0.80, 0.83), 0.40),
            _ => (Rgba(0.80, 0.48, 0.25), 0.45), // bare copper
        };
        return new MaterialSpec
        {
            Name = $"Copper.{finish}",
            BaseColorFactor = color,
            MetallicFactor = 1.0,
            RoughnessFactor = rough,
            DoubleSided = doubleSided,
        };
    }
}
