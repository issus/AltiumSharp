using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Connectivity;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Project;
using Xunit;
using Xunit.Abstractions;

namespace OriginalCircuit.Altium.Tests.Connectivity;

/// <summary>
/// The acceptance gate and permanent regression guard for the schematic connectivity solver: it builds
/// the project netlist and checks, pad by pad, that the schematic net partition matches the synced
/// PcbDoc net partition by IDENTITY (object/index, not name strings).
///
/// For a multi-channel design the same component designator appears on several channels, so a PCB pad is
/// mapped to its exact channel instance using the component's <c>SourceUniqueId</c> chain (which equals
/// the schematic sheet-symbol <c>UniqueId</c> chain) before looking up its net.
/// </summary>
public class PerPadPartitionOracleTests
{
    private readonly ITestOutputHelper _output;
    public PerPadPartitionOracleTests(ITestOutputHelper output) => _output = output;

    private static string RepoTestDataPath()
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "TestData");
    }

    [SkippableTheory]
    [InlineData("SPI")]
    [InlineData("Coherent")]
    public async Task ProjectNetlist_PerPad_Partition_Matches_PcbDoc(string which)
    {
        ProjectNetlist pnl;
        PcbDocument pcb;

        if (which == "SPI")
        {
            // Flat single-sheet board, in-repo. No project file exists, so synthesise one.
            var dir = RepoTestDataPath();
            var sch = Path.Combine(dir, "SPI Isolator.SchDoc");
            var pcbPath = Path.Combine(dir, "SPI Isolator.PcbDoc");
            Skip.IfNot(File.Exists(sch) && File.Exists(pcbPath), "SPI Isolator test data not available.");

            var project = AltiumLibrary.CreateProject();
            project.FilePath = Path.Combine(dir, "_oracle.PrjPcb");
            project.AddDocument("SPI Isolator.SchDoc");
            pnl = await ProjectNetlistBuilder.BuildAsync(project);
            pcb = (PcbDocument)await AltiumLibrary.OpenPcbDocAsync(pcbPath);
        }
        else
        {
            // Complex hierarchical multi-channel project (network share); skip when unavailable.
            var prj = @"Z:\Original Circuit\Coherent-Digitiser-8CH\Coherent-Digitiser-8CH.PrjPcb";
            Skip.IfNot(File.Exists(prj), "Coherent-Digitiser-8CH project not available.");
            var project = await AltiumLibrary.OpenProjectAsync(prj);
            pnl = await ProjectNetlistBuilder.BuildAsync(project);
            var pd = project.PcbDocuments.FirstOrDefault();
            var pcbPath = pd is null ? null : project.ResolveDocumentPath(pd);
            Skip.If(pcbPath is null || !File.Exists(pcbPath), "Coherent PcbDoc not found.");
            pcb = (PcbDocument)await AltiumLibrary.OpenPcbDocAsync(pcbPath!);
        }

        var r = ComputePartition(pnl, pcb);
        _output.WriteLine(r.ToString());

        if (which == "SPI")
        {
            // Flat board must be a perfect 1:1 partition; the only allowed misses are connector
            // mounting pads (J*.M) which are absent from the schematic.
            Assert.True(r.Misses.All(m => m.Pad == "M"),
                "Unexpected pads missing from the schematic: " + string.Join(", ", r.Misses.Where(m => m.Pad != "M").Take(10)));
            Assert.Empty(r.OverMerges);
            Assert.Empty(r.UnderSplits);
            Assert.Equal(r.PresentPads, r.CleanPads); // every present pad is a clean 1:1 mapping
        }
        else
        {
            // Channel disambiguation must leave no pad unmapped and never under-split a PCB net.
            Assert.Empty(r.Misses);
            Assert.Empty(r.UnderSplits);

            // Every multi-channel SIGNAL net must be distinct per channel — i.e. no signal net
            // over-merges. The only tolerated over-merges are a few power rails where the design reuses
            // one power-net name for electrically-distinct supplies, which Altium separates only by room.
            var allowedPowerRails = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "5V", "-5V", "1V8", "ADC_3V3", "3V3", "VDD_CORE" };
            var signalOverMerges = r.OverMerges.Where(o => !allowedPowerRails.Contains(o.SchNet)).ToList();
            Assert.True(signalOverMerges.Count == 0,
                "Signal nets over-merged across channels (should be distinct per channel):\n" +
                string.Join("\n", signalOverMerges.Take(15).Select(o => $"  {o.SchNet} -> {string.Join(" | ", o.PcbNets)}")));

            // Regression guard: the clean per-pad partition must stay high (channel-aware solve).
            Assert.True(r.CleanPads >= r.PresentPads * 0.88,
                $"Clean per-pad partition regressed: {r.CleanPads}/{r.PresentPads}.");
        }
    }

    // ---- partition computation (by identity) ----------------------------------------------------

    private sealed record OverMerge(string SchNet, IReadOnlyList<string> PcbNets);
    private sealed record Miss(string Refdes, string Pad);

    private sealed class PartitionResult
    {
        public int PresentPads, CleanPads, MissCount;
        public List<Miss> Misses = new();
        public List<OverMerge> OverMerges = new();
        public List<string> UnderSplits = new();

        public override string ToString() =>
            $"present={PresentPads} clean={CleanPads} ({(PresentPads == 0 ? 0 : 100.0 * CleanPads / PresentPads):0.0}%) " +
            $"misses={MissCount} over-merge={OverMerges.Count} under-split={UnderSplits.Count}\n" +
            string.Join("\n", OverMerges.Take(10).Select(o => $"  OVER {o.SchNet} -> {string.Join(" | ", o.PcbNets)}")) +
            (UnderSplits.Count > 0 ? "\n" + string.Join("\n", UnderSplits.Take(10).Select(u => "  SPLIT " + u)) : "");
    }

    private static string[] ChannelPath(string? uid)
    {
        if (string.IsNullOrEmpty(uid))
            return Array.Empty<string>();
        var segs = uid.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        return segs.Length <= 1 ? Array.Empty<string>() : segs[..^1]; // drop the component's own UniqueId
    }

    private static PartitionResult ComputePartition(ProjectNetlist pnl, PcbDocument pcb)
    {
        // PCB designators that appear more than once are channel components needing disambiguation.
        var desCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in pcb.Components)
        {
            var d = ((PcbComponent)c).SourceDesignator ?? ((PcbComponent)c).Name;
            if (!string.IsNullOrEmpty(d))
                desCount[d!] = desCount.GetValueOrDefault(d!) + 1;
        }

        var r = new PartitionResult();
        var schToPcb = new Dictionary<SchematicNet, HashSet<string>>();
        var pcbToSch = new Dictionary<string, HashSet<SchematicNet>>(StringComparer.OrdinalIgnoreCase);
        var pairs = new List<(SchematicNet Sn, string PcbNet)>();

        foreach (var ip in pcb.Pads)
        {
            var pad = (PcbPad)ip;
            if (pad.ComponentIndex < 0 || pad.ComponentIndex >= pcb.Components.Count)
                continue;
            var comp = (PcbComponent)pcb.Components[pad.ComponentIndex];
            var refdes = !string.IsNullOrWhiteSpace(comp.SourceDesignator) ? comp.SourceDesignator! : comp.Name;
            if (string.IsNullOrEmpty(refdes) || string.IsNullOrEmpty(pad.Designator))
                continue;
            if (pad.NetIndex == 0xFFFF || pad.NetIndex >= pcb.Nets.Count)
                continue;
            var pcbNet = pcb.Nets[pad.NetIndex].Name;

            SchematicNet? sn;
            if (desCount.GetValueOrDefault(refdes) > 1)
            {
                var inst = pnl.FindInstanceByUidPath(ChannelPath(comp.SourceUniqueId));
                sn = inst is null ? null : pnl.NetForPin(inst.Id, refdes, pad.Designator!);
            }
            else
            {
                sn = pnl.NetForPin(refdes, pad.Designator!);
            }

            if (sn is null)
            {
                r.MissCount++;
                if (r.Misses.Count < 60)
                    r.Misses.Add(new Miss(refdes, pad.Designator!));
                continue;
            }

            pairs.Add((sn, pcbNet));
            (schToPcb.TryGetValue(sn, out var a) ? a : schToPcb[sn] = new()).Add(pcbNet);
            (pcbToSch.TryGetValue(pcbNet, out var b) ? b : pcbToSch[pcbNet] = new()).Add(sn);
        }

        r.PresentPads = pairs.Count;
        r.CleanPads = pairs.Count(t => schToPcb[t.Sn].Count == 1 && pcbToSch[t.PcbNet].Count == 1);
        foreach (var kv in schToPcb.Where(kv => kv.Value.Count > 1))
            r.OverMerges.Add(new OverMerge(kv.Key.Name, kv.Value.ToList()));
        foreach (var kv in pcbToSch.Where(kv => kv.Value.Count > 1))
            r.UnderSplits.Add($"{kv.Key} -> {string.Join(" | ", kv.Value.Select(s => s.Name).Take(5))}");
        return r;
    }
}
