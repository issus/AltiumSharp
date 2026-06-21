using System.Globalization;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// The role of a layer in the physical board stack-up.
/// </summary>
public enum PcbStackupLayerKind
{
    /// <summary>Silkscreen / component overlay (a surface print, no physical thickness modelled).</summary>
    Overlay,

    /// <summary>Solder mask / solder resist coat.</summary>
    SolderMask,

    /// <summary>Solder paste stencil layer (a surface print, no physical thickness modelled).</summary>
    Paste,

    /// <summary>A copper signal or plane layer.</summary>
    Copper,

    /// <summary>A dielectric core or prepreg between copper layers.</summary>
    Dielectric,
}

/// <summary>
/// One layer in a <see cref="PcbStackup"/>, with its physical thickness and absolute Z position
/// (millimetres, measured from the bottom face of the board at Z=0).
/// </summary>
public sealed class PcbStackupLayer
{
    /// <summary>Layer name as it appears in the stack manager (e.g. "Top Layer", "Dielectric 1").</summary>
    public required string Name { get; init; }

    /// <summary>The layer's role in the stack.</summary>
    public PcbStackupLayerKind Kind { get; init; }

    /// <summary>Physical thickness in millimetres. Zero for surface prints (overlay, paste).</summary>
    public double ThicknessMm { get; init; }

    /// <summary>Dielectric / copper material name (e.g. "FR-4", "PP-006", "Solder Resist"), if known.</summary>
    public string? Material { get; init; }

    /// <summary>Relative permittivity (Dk) for dielectric layers, or 0 when not specified.</summary>
    public double DielectricConstant { get; init; }

    /// <summary>The packed Altium stack layer id (the raw <c>LAYERID</c> field), or 0 when unknown.</summary>
    public long LayerId { get; init; }

    /// <summary>
    /// The classic small-integer layer id used by primitives (1=Top, 32=Bottom, 2..31=mid copper,
    /// 33/34=overlay, 35/36=paste, 37/38=solder mask), or <see langword="null"/> for dielectric
    /// cores which carry no primitives.
    /// </summary>
    public int? Layer { get; internal set; }

    /// <summary>Absolute Z of the bottom face of this layer, in millimetres (board bottom = 0).</summary>
    public double Z0Mm { get; internal set; }

    /// <summary>Absolute Z of the top face of this layer, in millimetres.</summary>
    public double Z1Mm { get; internal set; }

    /// <summary>The mid-plane Z of this layer, in millimetres.</summary>
    public double CenterZMm => 0.5 * (Z0Mm + Z1Mm);

    /// <summary>True when this is a copper layer.</summary>
    public bool IsCopper => Kind == PcbStackupLayerKind.Copper;
}

/// <summary>
/// The physical board stack-up: an ordered (top-to-bottom) list of copper, dielectric, solder-mask
/// and surface-print layers with true thicknesses and absolute Z positions. Parsed from the modern
/// Altium <c>V9_STACK_LAYER{N}_*</c> Board6 parameters, or synthesised as a sensible default when a
/// file carries no usable stack data.
/// </summary>
public sealed class PcbStackup
{
    /// <summary>Layers ordered from the top of the board to the bottom.</summary>
    public IReadOnlyList<PcbStackupLayer> Layers { get; }

    /// <summary>Total physical board thickness in millimetres (sum of all layer thicknesses).</summary>
    public double TotalThicknessMm { get; }

    /// <summary>True when this stack was synthesised from defaults rather than read from the file.</summary>
    public bool IsFallback { get; }

    private const double OneOzCopperMm = 1.4 * MilToMm;   // 1.4 mil ≈ 35 µm (1 oz)
    private const double DefaultMaskMm = 0.4 * MilToMm;    // 0.4 mil typical solder-mask coat
    private const double MilToMm = 0.0254;

    private PcbStackup(List<PcbStackupLayer> layers, bool isFallback)
    {
        // Assign small-integer layer ids to copper entries by stack position: the top-most copper is
        // the Top Layer (1), the bottom-most is the Bottom Layer (32), and any layers in between are
        // Mid-Layer 1..30 (2..31) in top-to-bottom order. (Verified against real boards.)
        var copper = layers.Where(l => l.IsCopper).ToList();
        for (int i = 0; i < copper.Count; i++)
        {
            if (i == 0) copper[i].Layer = 1;
            else if (i == copper.Count - 1) copper[i].Layer = 32;
            else copper[i].Layer = i + 1; // Mid-Layer i -> id i+1
        }
        if (copper.Count == 1) copper[0].Layer = 1;

        // Compute absolute Z from the top down so the bottom face lands on Z=0.
        TotalThicknessMm = layers.Sum(l => l.ThicknessMm);
        double cursor = TotalThicknessMm;
        foreach (var l in layers)
        {
            l.Z1Mm = cursor;
            l.Z0Mm = cursor - l.ThicknessMm;
            cursor = l.Z0Mm;
        }

        Layers = layers;
        IsFallback = isFallback;
    }

    /// <summary>All copper layers, top to bottom.</summary>
    public IEnumerable<PcbStackupLayer> CopperLayers => Layers.Where(l => l.IsCopper);

    /// <summary>The stack entry whose primitive layer id matches <paramref name="layer"/>, or null.</summary>
    public PcbStackupLayer? ForLayer(int layer) => Layers.FirstOrDefault(l => l.Layer == layer);

    /// <summary>
    /// Parses the physical stack from a Board6 parameter dictionary, or returns <see langword="null"/>
    /// when no modern <c>V9_STACK_LAYER</c> data is present.
    /// </summary>
    public static PcbStackup? FromBoardParameters(IReadOnlyDictionary<string, string>? bp)
    {
        if (bp is null) return null;

        var raw = new List<PcbStackupLayer>();
        for (int n = 1; ; n++)
        {
            var prefix = $"V9_STACK_LAYER{n}_";
            if (!bp.TryGetValue(prefix + "NAME", out var name))
            {
                if (n == 1) return null;     // no V9 stack at all
                break;                        // reached the end of a contiguous stack
            }

            string? Field(string s) => bp.TryGetValue(prefix + s, out var v) ? v : null;
            var copthick = Field("COPTHICK");
            var dielType = Field("DIELTYPE");
            var dielHeight = Field("DIELHEIGHT");

            PcbStackupLayerKind kind;
            double thickness;
            if (copthick is not null)
            {
                kind = PcbStackupLayerKind.Copper;
                thickness = ParseLengthMm(copthick);
            }
            else if (name.Contains("Overlay", StringComparison.OrdinalIgnoreCase))
            {
                kind = PcbStackupLayerKind.Overlay;
                thickness = 0;
            }
            else if (name.Contains("Paste", StringComparison.OrdinalIgnoreCase))
            {
                kind = PcbStackupLayerKind.Paste;
                thickness = 0;
            }
            else if (dielType == "3" || name.Contains("Solder", StringComparison.OrdinalIgnoreCase))
            {
                kind = PcbStackupLayerKind.SolderMask;
                thickness = ParseLengthMm(dielHeight);
            }
            else
            {
                kind = PcbStackupLayerKind.Dielectric;
                thickness = ParseLengthMm(dielHeight);
            }

            raw.Add(new PcbStackupLayer
            {
                Name = name,
                Kind = kind,
                ThicknessMm = thickness,
                Material = Field("DIELMATERIAL"),
                DielectricConstant = ParseDouble(Field("DIELCONST")),
                LayerId = long.TryParse(Field("LAYERID"), out var lid) ? lid : 0,
                Layer = MapNamedLayer(name, kind),
            });
        }

        if (raw.Count == 0) return null;
        return new PcbStackup(raw, isFallback: false);
    }

    /// <summary>
    /// Builds a sensible default stack-up of <paramref name="copperLayers"/> copper layers totalling
    /// <paramref name="totalThicknessMm"/> (FR-4 dielectric, 1 oz copper, thin solder mask both sides).
    /// </summary>
    public static PcbStackup CreateDefault(double totalThicknessMm = 1.6, int copperLayers = 2)
    {
        int nCu = Math.Max(1, copperLayers);
        int nDiel = Math.Max(1, nCu - 1);
        double dielTotal = Math.Max(0.1, totalThicknessMm - nCu * OneOzCopperMm - 2 * DefaultMaskMm);
        double dielEach = dielTotal / nDiel;

        var layers = new List<PcbStackupLayer>
        {
            New("Top Overlay", PcbStackupLayerKind.Overlay, 0),
            New("Top Solder", PcbStackupLayerKind.SolderMask, DefaultMaskMm, "Solder Resist"),
            New("Top Layer", PcbStackupLayerKind.Copper, OneOzCopperMm),
        };
        for (int k = 1; k <= nDiel; k++)
        {
            layers.Add(New($"Dielectric {k}", PcbStackupLayerKind.Dielectric, dielEach, "FR-4"));
            if (k < nDiel)
                layers.Add(New($"Mid-Layer {k}", PcbStackupLayerKind.Copper, OneOzCopperMm));
        }
        if (nCu >= 2) layers.Add(New("Bottom Layer", PcbStackupLayerKind.Copper, OneOzCopperMm));
        layers.Add(New("Bottom Solder", PcbStackupLayerKind.SolderMask, DefaultMaskMm, "Solder Resist"));
        layers.Add(New("Bottom Overlay", PcbStackupLayerKind.Overlay, 0));

        return new PcbStackup(layers, isFallback: true);

        static PcbStackupLayer New(string name, PcbStackupLayerKind kind, double t, string? mat = null)
            => new() { Name = name, Kind = kind, ThicknessMm = t, Material = mat, Layer = MapNamedLayer(name, kind) };
    }

    // Maps the well-known surface layers by name to their classic small-integer ids. Copper ids are
    // assigned positionally in the constructor, so copper returns null here.
    private static int? MapNamedLayer(string name, PcbStackupLayerKind kind) => kind switch
    {
        PcbStackupLayerKind.Overlay => name.Contains("Bottom", StringComparison.OrdinalIgnoreCase) ? 34 : 33,
        PcbStackupLayerKind.Paste => name.Contains("Bottom", StringComparison.OrdinalIgnoreCase) ? 36 : 35,
        PcbStackupLayerKind.SolderMask => name.Contains("Bottom", StringComparison.OrdinalIgnoreCase) ? 38 : 37,
        _ => null,
    };

    // Parses an Altium length value such as "1.4mil", "0.035mm" or a bare number (assumed mils) to mm.
    private static double ParseLengthMm(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        s = s.Trim();
        double scale = MilToMm;
        if (s.EndsWith("mil", StringComparison.OrdinalIgnoreCase)) s = s[..^3];
        else if (s.EndsWith("mm", StringComparison.OrdinalIgnoreCase)) { s = s[..^2]; scale = 1.0; }
        return double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v * scale : 0;
    }

    private static double ParseDouble(string? s)
        => double.TryParse(s?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;
}
