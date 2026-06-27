using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Connectivity;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Models.Project;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Eda.Enums;
using Xunit;

namespace OriginalCircuit.Altium.Tests.Connectivity;

/// <summary>
/// Integration tests for the project-wide hierarchical netlist solver. Each test writes a couple of
/// tiny schematic documents to a temp directory, wires up a project, and asserts cross-sheet merging.
/// </summary>
public class ProjectNetlistTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "conn_" + Guid.NewGuid().ToString("N"));

    public ProjectNetlistTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    // ---- builders ----

    private static SchComponent Comp(string designator, string pin, int x, int y)
    {
        var c = new SchComponent { Name = designator, PartCount = 1, CurrentPartId = 1 };
        c.AddParameter(new SchParameter { Name = "Designator", Value = designator });
        c.AddPin(SchPin.Create(pin).At(Coord.FromMils(x), Coord.FromMils(y))
            .Length(Coord.FromMils(0)).Orient(PinOrientation.Right).Build());
        return c;
    }

    private static SchWire Wire(int x1, int y1, int x2, int y2)
    {
        var w = new SchWire();
        w.AddVertex(new CoordPoint(Coord.FromMils(x1), Coord.FromMils(y1)));
        w.AddVertex(new CoordPoint(Coord.FromMils(x2), Coord.FromMils(y2)));
        return w;
    }

    private async Task WriteDoc(string name, Action<SchDocument> build)
    {
        var doc = new SchDocument();
        build(doc);
        await doc.SaveAsync(Path.Combine(_dir, name));
    }

    private async Task<AltiumProject> Project(params string[] schDocNames)
    {
        var project = AltiumLibrary.CreateProject();
        project.FilePath = Path.Combine(_dir, "test.PrjPcb");
        foreach (var n in schDocNames)
            project.AddDocument(n);
        await Task.CompletedTask;
        return project;
    }

    // ---- tests ----

    [Fact]
    public async Task Port_And_SheetEntry_Merge_Across_Boundary()
    {
        // Parent: pin P1.1 -- wire -- sheet entry "NETX" (right edge of a sheet symbol -> child.SchDoc).
        await WriteDoc("parent.SchDoc", doc =>
        {
            var sym = new SchSheetSymbol
            {
                Location = new CoordPoint(Coord.FromMils(1000), Coord.FromMils(2000)),
                XSize = Coord.FromMils(500),
                YSize = Coord.FromMils(400),
                FileName = "child.SchDoc",
                SheetName = "U_CHILD",
            };
            sym.AddEntry(new SchSheetEntry { Side = 1, DistanceFromTop = Coord.FromMils(100), Name = "NETX" });
            doc.AddPrimitive(sym);
            // Entry connection point = (right=1500, top-100 = 1900).
            doc.AddPrimitive(Wire(1500, 1900, 2000, 1900));
            doc.AddComponent(Comp("P1", "1", 2000, 1900));
        });

        // Child: pin C1.1 -- wire -- port "NETX".
        await WriteDoc("child.SchDoc", doc =>
        {
            doc.AddPrimitive(new SchPort
            {
                Name = "NETX",
                Location = new CoordPoint(Coord.FromMils(500), Coord.FromMils(500)),
                Width = Coord.FromMils(200),
                ConnectedEnd = 1, // left end = Location
            });
            doc.AddPrimitive(Wire(500, 500, 1000, 500));
            doc.AddComponent(Comp("C1", "1", 1000, 500));
        });

        var project = await Project("parent.SchDoc", "child.SchDoc");
        var pnl = await ProjectNetlistBuilder.BuildAsync(project);

        var pNet = pnl.NetForPin("P1", "1");
        var cNet = pnl.NetForPin("C1", "1");
        Assert.NotNull(pNet);
        Assert.NotNull(cNet);
        Assert.Equal(pNet, cNet); // merged across the port <-> sheet-entry boundary
    }

    [Fact]
    public async Task Power_Net_Unifies_Globally_Across_Sheets()
    {
        await WriteDoc("parent.SchDoc", doc =>
        {
            var sym = new SchSheetSymbol
            {
                Location = new CoordPoint(Coord.FromMils(1000), Coord.FromMils(2000)),
                XSize = Coord.FromMils(500),
                YSize = Coord.FromMils(400),
                FileName = "child.SchDoc",
                SheetName = "U_CHILD",
            };
            doc.AddPrimitive(sym);
            doc.AddPrimitive(Wire(3000, 3000, 3200, 3000));
            doc.AddComponent(Comp("P1", "1", 3000, 3000));
            doc.AddPrimitive(new SchPowerObject { Text = "GND", Location = new CoordPoint(Coord.FromMils(3200), Coord.FromMils(3000)) });
        });

        await WriteDoc("child.SchDoc", doc =>
        {
            doc.AddPrimitive(Wire(500, 500, 700, 500));
            doc.AddComponent(Comp("C1", "1", 500, 500));
            doc.AddPrimitive(new SchPowerObject { Text = "GND", Location = new CoordPoint(Coord.FromMils(700), Coord.FromMils(500)) });
        });

        var project = await Project("parent.SchDoc", "child.SchDoc");
        var pnl = await ProjectNetlistBuilder.BuildAsync(project);

        var pNet = pnl.NetForPin("P1", "1");
        Assert.NotNull(pNet);
        Assert.Equal("GND", pNet!.Name);
        Assert.Equal(pNet, pnl.NetForPin("C1", "1"));
    }

    [Fact]
    public async Task NetLabel_Scope_Flat_Merges_But_Hierarchical_Does_Not()
    {
        // Two sheets (parent references child), each with a wire+pin labelled "SIG", not otherwise joined.
        await WriteDoc("parent.SchDoc", doc =>
        {
            var sym = new SchSheetSymbol
            {
                Location = new CoordPoint(Coord.FromMils(1000), Coord.FromMils(2000)),
                XSize = Coord.FromMils(500),
                YSize = Coord.FromMils(400),
                FileName = "child.SchDoc",
                SheetName = "U_CHILD",
            };
            doc.AddPrimitive(sym);
            doc.AddPrimitive(Wire(3000, 3000, 3200, 3000));
            doc.AddComponent(Comp("P1", "1", 3000, 3000));
            doc.AddPrimitive(new SchNetLabel { Text = "SIG", Location = new CoordPoint(Coord.FromMils(3000), Coord.FromMils(3000)) });
        });

        await WriteDoc("child.SchDoc", doc =>
        {
            doc.AddPrimitive(Wire(500, 500, 700, 500));
            doc.AddComponent(Comp("C1", "1", 500, 500));
            doc.AddPrimitive(new SchNetLabel { Text = "SIG", Location = new CoordPoint(Coord.FromMils(500), Coord.FromMils(500)) });
        });

        var project = await Project("parent.SchDoc", "child.SchDoc");

        var flat = await ProjectNetlistBuilder.BuildAsync(project,
            new NetlistOptions { Scope = NetIdentifierScope.Flat, ScopeIsExplicit = true });
        Assert.Equal(flat.NetForPin("P1", "1"), flat.NetForPin("C1", "1")); // global labels merge

        var hier = await ProjectNetlistBuilder.BuildAsync(project,
            new NetlistOptions { Scope = NetIdentifierScope.Hierarchical, ScopeIsExplicit = true });
        Assert.NotNull(hier.NetForPin("P1", "1"));
        Assert.NotEqual(hier.NetForPin("P1", "1"), hier.NetForPin("C1", "1")); // sheet-local labels stay apart
    }
}
