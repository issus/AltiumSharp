using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Serialization.Readers;
using OriginalCircuit.Altium.Serialization.Writers;

namespace OriginalCircuit.Altium.Tests.RoundTrip;

/// <summary>
/// Tests for the PcbDoc programmatic-edit API: primitive removal (incl. fills/polygons and the bulk
/// <see cref="PcbDocument.RemoveAll{T}"/>), the read-derived <c>IsFreePrimitive</c> ownership signal,
/// and whole-footprint <see cref="PcbComponent.MoveTo"/>/<see cref="PcbComponent.TranslateBy"/>.
/// </summary>
public sealed class PcbDocEditApiTests
{
    // --- 1. Primitive removal -------------------------------------------------------------------

    [Fact]
    public void RemoveFillAndPolygon_AreReachableOnConcreteType_AndRemove()
    {
        var doc = new PcbDocument();
        var fill = new PcbFill { Corner1 = new CoordPoint(Coord.Zero, Coord.Zero), Corner2 = new CoordPoint(Coord.OneMm, Coord.OneMm) };
        var polygon = new PcbPolygon { Name = "GND" };
        doc.AddFill(fill);
        doc.AddPolygon(polygon);

        // Compiles directly on PcbDocument (no IPcbDocument cast required).
        Assert.True(doc.RemoveFill(fill));
        Assert.True(doc.RemovePolygon(polygon));

        Assert.Empty(doc.Fills);
        Assert.Empty(doc.Polygons);

        // Removing again returns false (nothing left to remove).
        Assert.False(doc.RemoveFill(fill));
        Assert.False(doc.RemovePolygon(polygon));
    }

    [Fact]
    public void RemoveFill_RoundTrips_RemovedPrimitiveGoneRestUnchanged()
    {
        var doc = new PcbDocument();
        var keep = new PcbFill { Layer = 1, Corner1 = CoordPoint.Zero, Corner2 = new CoordPoint(Coord.OneMm, Coord.OneMm) };
        var drop = new PcbFill { Layer = 2, Corner1 = CoordPoint.Zero, Corner2 = new CoordPoint(Coord.OneMm, Coord.OneMm) };
        doc.AddFill(keep);
        doc.AddFill(drop);
        doc.AddTrack(new PcbTrack { Start = CoordPoint.Zero, End = new CoordPoint(Coord.OneMm, Coord.Zero), Width = Coord.OneMil, Layer = 1 });

        Assert.True(doc.RemoveFill(drop));

        using var ms = new MemoryStream();
        new PcbDocWriter().Write(doc, ms);
        ms.Position = 0;
        var rt = new PcbDocReader().Read(ms);

        Assert.Single(rt.Fills);                       // only the removed fill is gone
        Assert.Equal(1, ((PcbFill)rt.Fills[0]).Layer); // the surviving fill is the one we kept
        Assert.Single(rt.Tracks);                      // nothing else changed
    }

    [Fact]
    public void RemoveMethods_ArePublicOnConcreteType()
    {
        var doc = new PcbDocument();
        var track = new PcbTrack { Start = CoordPoint.Zero, End = new CoordPoint(Coord.OneMm, Coord.Zero), Width = Coord.OneMil };
        var via = new PcbVia { Location = CoordPoint.Zero, Diameter = Coord.OneMm };
        var arc = new PcbArc { Center = CoordPoint.Zero, Radius = Coord.OneMm };
        var text = PcbText.Create("X").Build();
        var region = PcbRegion.Create().AddPoint(Coord.Zero, Coord.Zero).Build();
        var body = PcbComponentBody.Create().Build();

        doc.AddTrack(track);
        doc.AddVia(via);
        doc.AddArc(arc);
        doc.AddText(text);
        doc.AddRegion(region);
        doc.AddComponentBody(body);

        // All resolve as public instance methods on PcbDocument.
        Assert.True(doc.RemoveTrack(track));
        Assert.True(doc.RemoveVia(via));
        Assert.True(doc.RemoveArc(arc));
        Assert.True(doc.RemoveText(text));
        Assert.True(doc.RemoveRegion(region));
        Assert.True(doc.RemoveComponentBody(body));

        Assert.Empty(doc.Tracks);
        Assert.Empty(doc.Vias);
        Assert.Empty(doc.Arcs);
        Assert.Empty(doc.Texts);
        Assert.Empty(doc.Regions);
        Assert.Empty(doc.ComponentBodies);
    }

    [Fact]
    public void RemoveMethods_StillSatisfyInterface()
    {
        var doc = new PcbDocument();
        var track = new PcbTrack { Start = CoordPoint.Zero, End = new CoordPoint(Coord.OneMm, Coord.Zero) };
        doc.AddTrack(track);

        // Promoting the explicit-interface Remove* to public keeps existing IPcbDocument callers working.
        IPcbDocument iface = doc;
        Assert.True(iface.RemoveTrack(track));
        Assert.Empty(doc.Tracks);
    }

    [Fact]
    public void RemoveAll_RemovesMatchingPrimitives_AndReturnsCount()
    {
        var doc = new PcbDocument();
        for (var i = 0; i < 5; i++)
            doc.AddTrack(new PcbTrack { Layer = i < 3 ? 1 : 2, Start = CoordPoint.Zero, End = new CoordPoint(Coord.OneMm, Coord.Zero) });

        var removed = doc.RemoveAll<PcbTrack>(t => t.Layer == 1);

        Assert.Equal(3, removed);
        Assert.Equal(2, doc.Tracks.Count);
        Assert.All(doc.Tracks, t => Assert.Equal(2, t.Layer));
    }

    [Fact]
    public void RemoveAll_UnsupportedType_Throws()
        => Assert.Throws<NotSupportedException>(() => new PcbDocument().RemoveAll<PcbNet>(_ => true));

    // --- 2. IsFreePrimitive ---------------------------------------------------------------------

    [Fact]
    public void IsFreePrimitive_TracksComponentIndex()
    {
        var free = new PcbTrack();                         // ComponentIndex defaults to -1
        var owned = new PcbTrack { ComponentIndex = 7 };
        Assert.True(free.IsFreePrimitive);
        Assert.False(owned.IsFreePrimitive);

        var freePoly = new PcbPolygon();
        Assert.True(freePoly.IsFreePrimitive);             // board-level pour, ComponentIndex == -1
        Assert.False(new PcbPolygon { ComponentIndex = 0 }.IsFreePrimitive);
    }

    [SkippableFact]
    public void IsFreePrimitive_AgreesWithOwnership_AfterLoad()
    {
        var path = Path.Combine(GetTestDataPath(), "SPI Isolator.PcbDoc");
        if (!File.Exists(path)) { Skip.If(true, "Test data not available"); return; }

        var doc = new PcbDocReader().Read(File.OpenRead(path));
        Assert.NotEmpty(doc.Components);

        // Every primitive's free flag must equal its actual footprint ownership (ComponentIndex < 0).
        Assert.All(doc.Pads.Cast<PcbPad>(), p => Assert.Equal(p.ComponentIndex < 0, p.IsFreePrimitive));
        Assert.All(doc.Tracks.Cast<PcbTrack>(), t => Assert.Equal(t.ComponentIndex < 0, t.IsFreePrimitive));
        Assert.All(doc.Arcs.Cast<PcbArc>(), a => Assert.Equal(a.ComponentIndex < 0, a.IsFreePrimitive));

        // Regression guard: before the fix IsFreePrimitive was stuck at its default false. A synced board
        // has both component-owned pads (not free) and free routing, so both states must be observed.
        Assert.Contains(doc.Pads.Cast<PcbPad>(), p => !p.IsFreePrimitive);   // component-owned pads exist
        Assert.Contains(doc.Tracks.Cast<PcbTrack>(), t => t.IsFreePrimitive); // free copper exists
    }

    // --- 3. Component move ----------------------------------------------------------------------

    [Fact]
    public void TranslateBy_MovesFromScratchComponentAndChildren()
    {
        var comp = PcbComponent.Create("R1").Build();
        comp.X = Coord.FromMils(100);
        comp.Y = Coord.FromMils(100);
        var pad = PcbPad.Create("1").At(Coord.FromMils(100), Coord.FromMils(100)).Build();
        var silk = new PcbText { Location = new CoordPoint(Coord.FromMils(90), Coord.FromMils(120)) };
        comp.AddPad(pad);
        comp.AddText(silk);

        comp.TranslateBy(Coord.FromMils(50), Coord.FromMils(-25));

        Assert.Equal(Coord.FromMils(150).ToRaw(), comp.X.ToRaw());
        Assert.Equal(Coord.FromMils(75).ToRaw(), comp.Y.ToRaw());
        Assert.Equal(Coord.FromMils(150).ToRaw(), pad.Location.X.ToRaw());
        Assert.Equal(Coord.FromMils(75).ToRaw(), pad.Location.Y.ToRaw());
        Assert.Equal(Coord.FromMils(140).ToRaw(), silk.Location.X.ToRaw());
        Assert.Equal(Coord.FromMils(95).ToRaw(), silk.Location.Y.ToRaw());
    }

    [SkippableFact]
    public void MoveComponent_RelocatesWholeFootprint_AndRoundTrips()
    {
        var path = Path.Combine(GetTestDataPath(), "SPI Isolator.PcbDoc");
        if (!File.Exists(path)) { Skip.If(true, "Test data not available"); return; }

        var doc = new PcbDocReader().Read(File.OpenRead(path));
        Assert.NotEmpty(doc.Components);

        // Pick the component that owns the most document-level primitives so silk, copper, courtyard
        // regions and 3D bodies are all exercised (a PcbDoc keeps everything but pads in the flat lists).
        var ci = Enumerable.Range(0, doc.Components.Count)
            .OrderByDescending(i => OwnedAnchors(doc, i).Count)
            .First();
        var comp = (PcbComponent)doc.Components[ci];

        var anchorsBefore = OwnedAnchors(doc, ci);
        Assert.NotEmpty(anchorsBefore);
        // Guarantee the core fix is under test: the component owns document-level primitives beyond the
        // pads in its own child collection (these live in the flat lists keyed by ComponentIndex and
        // would be left behind by a child-collection-only move).
        Assert.True(anchorsBefore.Count > comp.Pads.Count,
            "Expected the chosen component to own non-pad document-level primitives (silk/copper/body).");
        var compBefore = new CoordPoint(comp.X, comp.Y);
        var netsBefore = OwnedPadNets(doc, ci);

        var dx = Coord.FromMils(500);
        var dy = Coord.FromMils(250);
        comp.MoveTo(new CoordPoint(comp.X + dx, comp.Y + dy));

        using var ms = new MemoryStream();
        new PcbDocWriter().Write(doc, ms);
        ms.Position = 0;
        var rt = new PcbDocReader().Read(ms);

        // Counts unchanged: nothing dropped or duplicated by the move.
        Assert.Equal(doc.Pads.Count, rt.Pads.Count);
        Assert.Equal(doc.Tracks.Count, rt.Tracks.Count);
        Assert.Equal(doc.Components.Count, rt.Components.Count);

        var rtComp = (PcbComponent)rt.Components[ci];
        Assert.Equal((compBefore.X + dx).ToRaw(), rtComp.X.ToRaw());
        Assert.Equal((compBefore.Y + dy).ToRaw(), rtComp.Y.ToRaw());

        // Every owned primitive moved by exactly (dx, dy) and survived the round-trip.
        var anchorsAfter = OwnedAnchors(rt, ci);
        Assert.Equal(anchorsBefore.Count, anchorsAfter.Count);
        for (var i = 0; i < anchorsBefore.Count; i++)
        {
            Assert.Equal((anchorsBefore[i].X + dx).ToRaw(), anchorsAfter[i].X.ToRaw());
            Assert.Equal((anchorsBefore[i].Y + dy).ToRaw(), anchorsAfter[i].Y.ToRaw());
        }

        // Net assignments are preserved by the move (only geometry changes).
        Assert.Equal(netsBefore, OwnedPadNets(rt, ci));
    }

    private static List<ushort> OwnedPadNets(PcbDocument d, int componentIndex)
        => d.Pads.Cast<PcbPad>().Where(p => p.ComponentIndex == componentIndex).Select(p => p.NetIndex).ToList();

    // --- 4. Component.Children (owned-primitive view, incl. 3D bodies) --------------------------

    [Fact]
    public void Children_Surfaces3DBodies_TypedAndShapeBased_AndExcludesFreeOnes()
    {
        var doc = new PcbDocument();
        var comp = PcbComponent.Create("U1").Build();
        doc.AddComponent(comp); // index 0, wires up OwnerDocument

        var ownedPad = PcbPad.Create("1").At(Coord.Zero, Coord.Zero).Build();
        ownedPad.ComponentIndex = 0;
        doc.AddPad(ownedPad);

        var typedBody = PcbComponentBody.Create().Build();
        typedBody.ComponentIndex = 0;
        doc.AddComponentBody(typedBody);

        var shapeBody = new PcbShapeBasedRegion { ComponentIndex = 0 };
        shapeBody.Outline.Add(new PcbExtendedVertex { X = 0, Y = 0 });
        shapeBody.Outline.Add(new PcbExtendedVertex { X = 1000, Y = 1000 });
        doc.ShapeBasedComponentBodies.Add(shapeBody);

        // A free body (not owned by any component) must not appear in the component's children.
        var freeBody = PcbComponentBody.Create().Build(); // ComponentIndex defaults to -1
        doc.AddComponentBody(freeBody);

        var children = comp.Children.ToList();

        // 3D bodies are reachable — both the typed ComponentBodies6 form and the shape-based form.
        Assert.Contains(typedBody, children);
        Assert.Contains(shapeBody, children);
        Assert.Single(children.OfType<PcbComponentBody>());      // only the owned typed body
        Assert.Single(children.OfType<PcbShapeBasedRegion>());   // the shape-based 3D body
        Assert.DoesNotContain(freeBody, children);               // free body excluded
        Assert.Contains(ownedPad, children);

        // The shape-based body is a first-class IPrimitive now (non-empty bounds from its outline).
        Assert.NotEqual(CoordRect.Empty, shapeBody.Bounds);
    }

    [SkippableFact]
    public void Children_IsCompleteAndDeduped_OnRealBoard()
    {
        var path = Path.Combine(GetTestDataPath(), "SPI Isolator.PcbDoc");
        if (!File.Exists(path)) { Skip.If(true, "Test data not available"); return; }

        var doc = new PcbDocReader().Read(File.OpenRead(path));
        var ci = Enumerable.Range(0, doc.Components.Count)
            .OrderByDescending(i => OwnedAnchors(doc, i).Count)
            .First();
        var comp = (PcbComponent)doc.Components[ci];

        var children = comp.Children.ToList();

        // Pads appear exactly once even though they live in both comp.Pads and document.Pads (shared refs).
        Assert.Equal(comp.Pads.Count, children.OfType<PcbPad>().Count());
        Assert.Equal(children.Count, children.Distinct().Count());

        // The view reaches document-level silk/copper/body the component owns by ComponentIndex — not just
        // the pads in its own child collection.
        Assert.True(children.Count > comp.Pads.Count);
        Assert.All(children, c => Assert.NotNull(c));
    }

    /// <summary>
    /// A deterministic, order-stable list of one representative anchor point per primitive owned by the
    /// component at <paramref name="componentIndex"/>. Storages are written and read back in list order,
    /// so the same enumeration before the move and after the round-trip aligns 1:1.
    /// </summary>
    private static List<CoordPoint> OwnedAnchors(PcbDocument d, int componentIndex)
    {
        var list = new List<CoordPoint>();
        foreach (var x in d.Pads) if (((PcbPad)x).ComponentIndex == componentIndex) list.Add(((PcbPad)x).Location);
        foreach (var x in d.Vias) if (((PcbVia)x).ComponentIndex == componentIndex) list.Add(((PcbVia)x).Location);
        foreach (var x in d.Tracks) if (((PcbTrack)x).ComponentIndex == componentIndex) list.Add(((PcbTrack)x).Start);
        foreach (var x in d.Arcs) if (((PcbArc)x).ComponentIndex == componentIndex) list.Add(((PcbArc)x).Center);
        foreach (var x in d.Texts) if (((PcbText)x).ComponentIndex == componentIndex) list.Add(((PcbText)x).Location);
        foreach (var x in d.Fills) if (((PcbFill)x).ComponentIndex == componentIndex) list.Add(((PcbFill)x).Corner1);
        foreach (var x in d.Regions)
        {
            var r = (PcbRegion)x;
            if (r.ComponentIndex == componentIndex && r.Outline.Count > 0) list.Add(r.Outline[0]);
        }
        foreach (var x in d.ComponentBodies)
        {
            var b = (PcbComponentBody)x;
            if (b.ComponentIndex == componentIndex)
                list.Add(b.Outline.Count > 0 ? b.Outline[0] : b.Model2DLocation);
        }
        foreach (var r in d.ShapeBasedRegions)
            if (r.ComponentIndex == componentIndex && r.Outline.Count > 0)
                list.Add(new CoordPoint(Coord.FromRaw(r.Outline[0].X), Coord.FromRaw(r.Outline[0].Y)));
        foreach (var r in d.ShapeBasedComponentBodies)
            if (r.ComponentIndex == componentIndex && r.Outline.Count > 0)
                list.Add(new CoordPoint(Coord.FromRaw(r.Outline[0].X), Coord.FromRaw(r.Outline[0].Y)));
        return list;
    }

    private static string GetTestDataPath()
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "TestData");
    }
}
