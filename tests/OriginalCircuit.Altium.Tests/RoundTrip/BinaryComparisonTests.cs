using OpenMcdf;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Serialization.Readers;
using OriginalCircuit.Altium.Serialization.Writers;
using Xunit;
using Xunit.Abstractions;

namespace OriginalCircuit.Altium.Tests.RoundTrip;

/// <summary>
/// Binary comparison tests that verify write fidelity by comparing
/// the MCDF stream contents after a read → write → re-read cycle.
/// These tests identify byte-level differences in serialization.
/// </summary>
public sealed class BinaryComparisonTests
{
    private readonly ITestOutputHelper _output;

    public BinaryComparisonTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SchLib_RoundTrip_PreservesComponentCounts()
    {
        foreach (var filePath in GetSchLibFiles())
        {
            var fileName = Path.GetFileName(filePath);

            SchLibrary original;
            using (var stream = File.OpenRead(filePath))
            {
                original = new SchLibReader().Read(stream);
            }

            using var ms = new MemoryStream();
            new SchLibWriter().Write(original, ms);
            ms.Position = 0;

            var roundTripped = new SchLibReader().Read(ms);

            Assert.Equal(original.Components.Count, roundTripped.Components.Count);

            for (int i = 0; i < original.Components.Count; i++)
            {
                var origComp = original.Components[i];
                var rtComp = roundTripped.Components[i];

                Assert.Equal(origComp.Name, rtComp.Name);
                Assert.Equal(origComp.Pins.Count, rtComp.Pins.Count);
                Assert.Equal(origComp.Lines.Count, rtComp.Lines.Count);
                Assert.Equal(origComp.Rectangles.Count, rtComp.Rectangles.Count);
                Assert.Equal(origComp.Labels.Count, rtComp.Labels.Count);
                Assert.Equal(origComp.Arcs.Count, rtComp.Arcs.Count);
                Assert.Equal(origComp.Polygons.Count, rtComp.Polygons.Count);
                Assert.Equal(origComp.Polylines.Count, rtComp.Polylines.Count);
                Assert.Equal(origComp.Beziers.Count, rtComp.Beziers.Count);
                Assert.Equal(origComp.Ellipses.Count, rtComp.Ellipses.Count);
                Assert.Equal(origComp.RoundedRectangles.Count, rtComp.RoundedRectangles.Count);
                Assert.Equal(origComp.Pies.Count, rtComp.Pies.Count);
                Assert.Equal(origComp.NetLabels.Count, rtComp.NetLabels.Count);
                Assert.Equal(origComp.Junctions.Count, rtComp.Junctions.Count);
                Assert.Equal(origComp.Parameters.Count, rtComp.Parameters.Count);
                Assert.Equal(origComp.TextFrames.Count, rtComp.TextFrames.Count);
                Assert.Equal(origComp.Images.Count, rtComp.Images.Count);
                Assert.Equal(origComp.Symbols.Count, rtComp.Symbols.Count);
                Assert.Equal(origComp.EllipticalArcs.Count, rtComp.EllipticalArcs.Count);
                Assert.Equal(origComp.PowerObjects.Count, rtComp.PowerObjects.Count);
            }

            _output.WriteLine($"PASS: {fileName} - {original.Components.Count} components preserved");
        }
    }

    [Fact]
    public void SchLib_RoundTrip_BinaryStreamSizeComparison()
    {
        foreach (var filePath in GetSchLibFiles())
        {
            var fileName = Path.GetFileName(filePath);
            var originalBytes = File.ReadAllBytes(filePath);

            SchLibrary lib;
            using (var stream = new MemoryStream(originalBytes))
            {
                lib = new SchLibReader().Read(stream);
            }

            using var ms = new MemoryStream();
            new SchLibWriter().Write(lib, ms);
            var roundTrippedBytes = ms.ToArray();

            var ratio = originalBytes.Length > 0
                ? (double)roundTrippedBytes.Length / originalBytes.Length * 100
                : 0;

            _output.WriteLine($"{fileName}: original={originalBytes.Length}, roundTripped={roundTrippedBytes.Length}, ratio={ratio:F1}%");
        }
    }

    [Fact]
    public void PcbLib_RoundTrip_PreservesComponentCounts()
    {
        foreach (var filePath in GetPcbLibFiles())
        {
            var fileName = Path.GetFileName(filePath);

            PcbLibrary original;
            using (var stream = File.OpenRead(filePath))
            {
                original = new PcbLibReader().Read(stream);
            }

            using var ms = new MemoryStream();
            new PcbLibWriter().Write(original, ms);
            ms.Position = 0;

            var roundTripped = new PcbLibReader().Read(ms);

            Assert.Equal(original.Components.Count, roundTripped.Components.Count);

            for (int i = 0; i < original.Components.Count; i++)
            {
                var origComp = original.Components[i];
                var rtComp = roundTripped.Components[i];

                Assert.Equal(origComp.Name, rtComp.Name);
                Assert.Equal(origComp.Pads.Count, rtComp.Pads.Count);
                Assert.Equal(origComp.Tracks.Count, rtComp.Tracks.Count);
                Assert.Equal(origComp.Vias.Count, rtComp.Vias.Count);
                Assert.Equal(origComp.Arcs.Count, rtComp.Arcs.Count);
                Assert.Equal(origComp.Texts.Count, rtComp.Texts.Count);
                Assert.Equal(origComp.Fills.Count, rtComp.Fills.Count);
                Assert.Equal(origComp.Regions.Count, rtComp.Regions.Count);
                Assert.Equal(origComp.ComponentBodies.Count, rtComp.ComponentBodies.Count);
            }

            _output.WriteLine($"PASS: {fileName} - {original.Components.Count} components preserved");
        }
    }

    [Fact]
    public void PcbLib_RoundTrip_BinaryStreamSizeComparison()
    {
        foreach (var filePath in GetPcbLibFiles())
        {
            var fileName = Path.GetFileName(filePath);
            var originalBytes = File.ReadAllBytes(filePath);

            PcbLibrary lib;
            using (var stream = new MemoryStream(originalBytes))
            {
                lib = new PcbLibReader().Read(stream);
            }

            using var ms = new MemoryStream();
            new PcbLibWriter().Write(lib, ms);
            var roundTrippedBytes = ms.ToArray();

            var ratio = originalBytes.Length > 0
                ? (double)roundTrippedBytes.Length / originalBytes.Length * 100
                : 0;

            _output.WriteLine($"{fileName}: original={originalBytes.Length}, roundTripped={roundTrippedBytes.Length}, ratio={ratio:F1}%");
        }
    }

    [Fact]
    public void SchLib_RoundTrip_PinPropertiesPreserved()
    {
        foreach (var filePath in GetSchLibFiles())
        {
            SchLibrary original;
            using (var stream = File.OpenRead(filePath))
            {
                original = new SchLibReader().Read(stream);
            }

            using var ms = new MemoryStream();
            new SchLibWriter().Write(original, ms);
            ms.Position = 0;

            var roundTripped = new SchLibReader().Read(ms);

            for (int c = 0; c < original.Components.Count; c++)
            {
                var origComp = original.Components[c];
                var rtComp = roundTripped.Components[c];

                for (int p = 0; p < origComp.Pins.Count && p < rtComp.Pins.Count; p++)
                {
                    var origPin = (SchPin)origComp.Pins[p];
                    var rtPin = (SchPin)rtComp.Pins[p];

                    Assert.Equal(origPin.Name, rtPin.Name);
                    Assert.Equal(origPin.Designator, rtPin.Designator);
                    Assert.Equal(origPin.ElectricalType, rtPin.ElectricalType);
                    Assert.Equal(origPin.Location.X.ToRaw(), rtPin.Location.X.ToRaw());
                    Assert.Equal(origPin.Location.Y.ToRaw(), rtPin.Location.Y.ToRaw());
                    Assert.Equal(origPin.Length.ToRaw(), rtPin.Length.ToRaw());
                    Assert.Equal(origPin.Orientation, rtPin.Orientation);
                }
            }

            _output.WriteLine($"PASS: {Path.GetFileName(filePath)} - pin properties verified");
        }
    }

    [Fact]
    public void SchLib_RoundTrip_RectanglePropertiesPreserved()
    {
        foreach (var filePath in GetSchLibFiles())
        {
            SchLibrary original;
            using (var stream = File.OpenRead(filePath))
            {
                original = new SchLibReader().Read(stream);
            }

            using var ms = new MemoryStream();
            new SchLibWriter().Write(original, ms);
            ms.Position = 0;

            var roundTripped = new SchLibReader().Read(ms);

            for (int c = 0; c < original.Components.Count; c++)
            {
                var origComp = original.Components[c];
                var rtComp = roundTripped.Components[c];

                for (int r = 0; r < origComp.Rectangles.Count && r < rtComp.Rectangles.Count; r++)
                {
                    var origRect = (SchRectangle)origComp.Rectangles[r];
                    var rtRect = (SchRectangle)rtComp.Rectangles[r];

                    Assert.Equal(origRect.Corner1.X.ToRaw(), rtRect.Corner1.X.ToRaw());
                    Assert.Equal(origRect.Corner1.Y.ToRaw(), rtRect.Corner1.Y.ToRaw());
                    Assert.Equal(origRect.Corner2.X.ToRaw(), rtRect.Corner2.X.ToRaw());
                    Assert.Equal(origRect.Corner2.Y.ToRaw(), rtRect.Corner2.Y.ToRaw());
                    Assert.Equal(origRect.IsFilled, rtRect.IsFilled);
                    Assert.Equal(origRect.Color, rtRect.Color);
                    Assert.Equal(origRect.FillColor, rtRect.FillColor);
                    Assert.Equal(origRect.LineWidth.ToRaw(), rtRect.LineWidth.ToRaw());
                }
            }

            _output.WriteLine($"PASS: {Path.GetFileName(filePath)} - rectangle properties verified");
        }
    }

    [Fact]
    public void PcbLib_RoundTrip_PadPropertiesPreserved()
    {
        foreach (var filePath in GetPcbLibFiles())
        {
            PcbLibrary original;
            using (var stream = File.OpenRead(filePath))
            {
                original = new PcbLibReader().Read(stream);
            }

            using var ms = new MemoryStream();
            new PcbLibWriter().Write(original, ms);
            ms.Position = 0;

            var roundTripped = new PcbLibReader().Read(ms);

            for (int c = 0; c < original.Components.Count; c++)
            {
                var origComp = original.Components[c];
                var rtComp = roundTripped.Components[c];

                for (int p = 0; p < origComp.Pads.Count && p < rtComp.Pads.Count; p++)
                {
                    var origPad = (PcbPad)origComp.Pads[p];
                    var rtPad = (PcbPad)rtComp.Pads[p];

                    Assert.Equal(origPad.Designator, rtPad.Designator);
                    Assert.Equal(origPad.Location.X.ToRaw(), rtPad.Location.X.ToRaw());
                    Assert.Equal(origPad.Location.Y.ToRaw(), rtPad.Location.Y.ToRaw());
                    Assert.Equal(origPad.SizeTop.X.ToRaw(), rtPad.SizeTop.X.ToRaw());
                    Assert.Equal(origPad.SizeTop.Y.ToRaw(), rtPad.SizeTop.Y.ToRaw());
                    Assert.Equal(origPad.HoleSize.ToRaw(), rtPad.HoleSize.ToRaw());
                    Assert.Equal(origPad.ShapeTop, rtPad.ShapeTop);
                    Assert.Equal(origPad.Layer, rtPad.Layer);
                    Assert.Equal(origPad.IsPlated, rtPad.IsPlated);
                }
            }

            _output.WriteLine($"PASS: {Path.GetFileName(filePath)} - pad properties verified");
        }
    }

    [Fact]
    public void PcbLib_RoundTrip_TrackPropertiesPreserved()
    {
        foreach (var filePath in GetPcbLibFiles())
        {
            PcbLibrary original;
            using (var stream = File.OpenRead(filePath))
            {
                original = new PcbLibReader().Read(stream);
            }

            using var ms = new MemoryStream();
            new PcbLibWriter().Write(original, ms);
            ms.Position = 0;

            var roundTripped = new PcbLibReader().Read(ms);

            for (int c = 0; c < original.Components.Count; c++)
            {
                var origComp = original.Components[c];
                var rtComp = roundTripped.Components[c];

                for (int t = 0; t < origComp.Tracks.Count && t < rtComp.Tracks.Count; t++)
                {
                    var origTrack = (PcbTrack)origComp.Tracks[t];
                    var rtTrack = (PcbTrack)rtComp.Tracks[t];

                    Assert.Equal(origTrack.Start.X.ToRaw(), rtTrack.Start.X.ToRaw());
                    Assert.Equal(origTrack.Start.Y.ToRaw(), rtTrack.Start.Y.ToRaw());
                    Assert.Equal(origTrack.End.X.ToRaw(), rtTrack.End.X.ToRaw());
                    Assert.Equal(origTrack.End.Y.ToRaw(), rtTrack.End.Y.ToRaw());
                    Assert.Equal(origTrack.Width.ToRaw(), rtTrack.Width.ToRaw());
                    Assert.Equal(origTrack.Layer, rtTrack.Layer);
                }
            }

            _output.WriteLine($"PASS: {Path.GetFileName(filePath)} - track properties verified");
        }
    }

    [Fact]
    public void SchDoc_RoundTrip_PreservesComponentCounts()
    {
        foreach (var filePath in GetSchDocFiles())
        {
            var fileName = Path.GetFileName(filePath);

            SchDocument original;
            using (var stream = File.OpenRead(filePath))
            {
                original = new SchDocReader().Read(stream);
            }

            using var ms = new MemoryStream();
            new SchDocWriter().Write(original, ms);
            ms.Position = 0;

            var roundTripped = new SchDocReader().Read(ms);

            Assert.Equal(original.Components.Count, roundTripped.Components.Count);
            _output.WriteLine($"PASS: {fileName} - {original.Components.Count} components, " +
                $"{original.Wires.Count} wires, {original.NetLabels.Count} net labels, " +
                $"{original.Junctions.Count} junctions, {original.PowerObjects.Count} power objects");
        }
    }

    [Fact]
    public void PcbDoc_RoundTrip_PreservesPrimitiveCounts()
    {
        foreach (var filePath in GetPcbDocFiles())
        {
            var fileName = Path.GetFileName(filePath);

            PcbDocument original;
            using (var stream = File.OpenRead(filePath))
            {
                original = new PcbDocReader().Read(stream);
            }

            using var ms = new MemoryStream();
            new PcbDocWriter().Write(original, ms);
            ms.Position = 0;

            var roundTripped = new PcbDocReader().Read(ms);

            Assert.Equal(original.Tracks.Count, roundTripped.Tracks.Count);
            Assert.Equal(original.Pads.Count, roundTripped.Pads.Count);
            Assert.Equal(original.Vias.Count, roundTripped.Vias.Count);
            Assert.Equal(original.Arcs.Count, roundTripped.Arcs.Count);
            Assert.Equal(original.Texts.Count, roundTripped.Texts.Count);
            Assert.Equal(original.Fills.Count, roundTripped.Fills.Count);
            Assert.Equal(original.Regions.Count, roundTripped.Regions.Count);
            Assert.Equal(original.ComponentBodies.Count, roundTripped.ComponentBodies.Count);
            Assert.Equal(original.Components.Count, roundTripped.Components.Count);

            _output.WriteLine($"PASS: {fileName} - tracks={original.Tracks.Count}, pads={original.Pads.Count}, " +
                $"vias={original.Vias.Count}, arcs={original.Arcs.Count}, texts={original.Texts.Count}, " +
                $"fills={original.Fills.Count}, regions={original.Regions.Count}, " +
                $"bodies={original.ComponentBodies.Count}, components={original.Components.Count}");
        }
    }

    [Fact]
    public void AllFiles_LoadWithoutExceptions()
    {
        var errors = new List<string>();

        foreach (var file in GetSchLibFiles())
        {
            try
            {
                using var stream = File.OpenRead(file);
                var lib = new SchLibReader().Read(stream);
                _output.WriteLine($"OK SchLib: {Path.GetFileName(file)} - {lib.Components.Count} components");
            }
            catch (Exception ex)
            {
                errors.Add($"SchLib {Path.GetFileName(file)}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        foreach (var file in GetPcbLibFiles())
        {
            try
            {
                using var stream = File.OpenRead(file);
                var lib = new PcbLibReader().Read(stream);
                _output.WriteLine($"OK PcbLib: {Path.GetFileName(file)} - {lib.Components.Count} components");
            }
            catch (Exception ex)
            {
                errors.Add($"PcbLib {Path.GetFileName(file)}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        foreach (var file in GetSchDocFiles())
        {
            try
            {
                using var stream = File.OpenRead(file);
                var doc = new SchDocReader().Read(stream);
                _output.WriteLine($"OK SchDoc: {Path.GetFileName(file)} - {doc.Components.Count} components");
            }
            catch (Exception ex)
            {
                errors.Add($"SchDoc {Path.GetFileName(file)}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        foreach (var file in GetPcbDocFiles())
        {
            try
            {
                using var stream = File.OpenRead(file);
                var doc = new PcbDocReader().Read(stream);
                _output.WriteLine($"OK PcbDoc: {Path.GetFileName(file)} - tracks={doc.Tracks.Count}, pads={doc.Pads.Count}");
            }
            catch (Exception ex)
            {
                errors.Add($"PcbDoc {Path.GetFileName(file)}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            _output.WriteLine($"\n{errors.Count} FAILURES:");
            foreach (var error in errors)
                _output.WriteLine($"  FAIL: {error}");
            Assert.Fail($"{errors.Count} files failed to load:\n" + string.Join("\n", errors));
        }
    }

    [Fact]
    public void AllFiles_RoundTripWithoutExceptions()
    {
        var errors = new List<string>();

        foreach (var file in GetSchLibFiles())
        {
            try
            {
                using var stream = File.OpenRead(file);
                var lib = new SchLibReader().Read(stream);
                using var ms = new MemoryStream();
                new SchLibWriter().Write(lib, ms);
                ms.Position = 0;
                var rt = new SchLibReader().Read(ms);
                Assert.Equal(lib.Components.Count, rt.Components.Count);
            }
            catch (Exception ex)
            {
                errors.Add($"SchLib {Path.GetFileName(file)}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        foreach (var file in GetPcbLibFiles())
        {
            try
            {
                using var stream = File.OpenRead(file);
                var lib = new PcbLibReader().Read(stream);
                using var ms = new MemoryStream();
                new PcbLibWriter().Write(lib, ms);
                ms.Position = 0;
                var rt = new PcbLibReader().Read(ms);
                Assert.Equal(lib.Components.Count, rt.Components.Count);
            }
            catch (Exception ex)
            {
                errors.Add($"PcbLib {Path.GetFileName(file)}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        foreach (var file in GetSchDocFiles())
        {
            try
            {
                using var stream = File.OpenRead(file);
                var doc = new SchDocReader().Read(stream);
                using var ms = new MemoryStream();
                new SchDocWriter().Write(doc, ms);
                ms.Position = 0;
                var rt = new SchDocReader().Read(ms);
                Assert.Equal(doc.Components.Count, rt.Components.Count);
            }
            catch (Exception ex)
            {
                errors.Add($"SchDoc {Path.GetFileName(file)}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        foreach (var file in GetPcbDocFiles())
        {
            try
            {
                using var stream = File.OpenRead(file);
                var doc = new PcbDocReader().Read(stream);
                using var ms = new MemoryStream();
                new PcbDocWriter().Write(doc, ms);
                ms.Position = 0;
                var rt = new PcbDocReader().Read(ms);
                Assert.Equal(doc.Tracks.Count, rt.Tracks.Count);
                Assert.Equal(doc.Pads.Count, rt.Pads.Count);
            }
            catch (Exception ex)
            {
                errors.Add($"PcbDoc {Path.GetFileName(file)}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            _output.WriteLine($"\n{errors.Count} FAILURES:");
            foreach (var error in errors)
                _output.WriteLine($"  FAIL: {error}");
            Assert.Fail($"{errors.Count} files failed round-trip:\n" + string.Join("\n", errors));
        }
    }

    [Fact]
    public void PcbLib_RoundTrip_StreamLevelComparison()
    {
        // Use a single simple test file to compare stream-by-stream
        var testFile = GetPcbLibFiles().FirstOrDefault(f => Path.GetFileName(f).Equals("PAD_TH_ROUND.PcbLib", StringComparison.OrdinalIgnoreCase));
        if (testFile == null)
        {
            testFile = GetPcbLibFiles().First();
        }

        var fileName = Path.GetFileName(testFile);
        _output.WriteLine($"=== Stream-level comparison for {fileName} ===\n");

        // Open original file with OpenMcdf
        using var originalCf = new OpenMcdf.CompoundFile(testFile);
        var originalStreams = new Dictionary<string, byte[]>();
        EnumerateStreams(originalCf.RootStorage, "", originalStreams);

        // Round-trip
        PcbLibrary lib;
        using (var stream = File.OpenRead(testFile))
        {
            lib = new PcbLibReader().Read(stream);
        }

        using var ms = new MemoryStream();
        new PcbLibWriter().Write(lib, ms);
        ms.Position = 0;

        using var rtCf = new OpenMcdf.CompoundFile(ms);
        var rtStreams = new Dictionary<string, byte[]>();
        EnumerateStreams(rtCf.RootStorage, "", rtStreams);

        // Compare
        _output.WriteLine("ORIGINAL streams:");
        foreach (var kvp in originalStreams.OrderBy(k => k.Key))
            _output.WriteLine($"  {kvp.Key}: {kvp.Value.Length} bytes");

        _output.WriteLine("\nROUND-TRIPPED streams:");
        foreach (var kvp in rtStreams.OrderBy(k => k.Key))
            _output.WriteLine($"  {kvp.Key}: {kvp.Value.Length} bytes");

        _output.WriteLine("\nMISSING in round-trip:");
        foreach (var kvp in originalStreams.OrderBy(k => k.Key))
        {
            if (!rtStreams.ContainsKey(kvp.Key))
                _output.WriteLine($"  MISSING: {kvp.Key} ({kvp.Value.Length} bytes)");
        }

        _output.WriteLine("\nSIZE DIFFERENCES:");
        foreach (var kvp in originalStreams.OrderBy(k => k.Key))
        {
            if (rtStreams.TryGetValue(kvp.Key, out var rtData))
            {
                if (kvp.Value.Length != rtData.Length)
                {
                    _output.WriteLine($"  {kvp.Key}: original={kvp.Value.Length}, rt={rtData.Length}, diff={rtData.Length - kvp.Value.Length}");
                }
                else if (!kvp.Value.SequenceEqual(rtData))
                {
                    // Same size but different content
                    var firstDiff = -1;
                    for (int i = 0; i < kvp.Value.Length; i++)
                    {
                        if (kvp.Value[i] != rtData[i]) { firstDiff = i; break; }
                    }
                    _output.WriteLine($"  {kvp.Key}: same size ({kvp.Value.Length}) but content differs at byte {firstDiff}");
                }
                else
                {
                    _output.WriteLine($"  {kvp.Key}: IDENTICAL ({kvp.Value.Length} bytes)");
                }
            }
        }

        // For the Data stream specifically, show hex dump of first differences
        var compName = lib.Components.First().Name;
        var sectionKey = compName.Length > 31 ? compName.Substring(0, 31).Replace('/', '_') : compName.Replace('/', '_');
        var dataKey = $"{sectionKey}/Data";

        if (originalStreams.TryGetValue(dataKey, out var origData) && rtStreams.TryGetValue(dataKey, out var rtDataStream))
        {
            _output.WriteLine($"\n=== Data stream comparison ({dataKey}) ===");
            _output.WriteLine($"Original: {origData.Length} bytes, RT: {rtDataStream.Length} bytes");

            var maxLen = Math.Min(origData.Length, rtDataStream.Length);
            var diffCount = 0;
            for (int i = 0; i < maxLen && diffCount < 20; i++)
            {
                if (origData[i] != rtDataStream[i])
                {
                    _output.WriteLine($"  Byte {i:X4} (dec {i}): orig=0x{origData[i]:X2} rt=0x{rtDataStream[i]:X2}");
                    diffCount++;
                }
            }
            if (origData.Length != rtDataStream.Length)
            {
                _output.WriteLine($"  Length difference: {origData.Length - rtDataStream.Length} bytes (original longer)");
            }
        }
    }

    [Fact]
    public void SchLib_RoundTrip_StreamLevelComparison()
    {
        // Use a file that's at 100% to verify it's truly identical
        var testFile = GetSchLibFiles().FirstOrDefault(f => Path.GetFileName(f).Equals("RESISTOR.SchLib", StringComparison.OrdinalIgnoreCase));
        if (testFile == null) testFile = GetSchLibFiles().First();

        var fileName = Path.GetFileName(testFile);
        _output.WriteLine($"=== Stream-level comparison for {fileName} ===\n");

        using var originalCf = new OpenMcdf.CompoundFile(testFile);
        var originalStreams = new Dictionary<string, byte[]>();
        EnumerateStreams(originalCf.RootStorage, "", originalStreams);

        SchLibrary lib;
        using (var stream = File.OpenRead(testFile))
        {
            lib = new SchLibReader().Read(stream);
        }

        using var ms = new MemoryStream();
        new SchLibWriter().Write(lib, ms);
        ms.Position = 0;

        using var rtCf = new OpenMcdf.CompoundFile(ms);
        var rtStreams = new Dictionary<string, byte[]>();
        EnumerateStreams(rtCf.RootStorage, "", rtStreams);

        _output.WriteLine("ORIGINAL streams:");
        foreach (var kvp in originalStreams.OrderBy(k => k.Key))
            _output.WriteLine($"  {kvp.Key}: {kvp.Value.Length} bytes");

        _output.WriteLine("\nROUND-TRIPPED streams:");
        foreach (var kvp in rtStreams.OrderBy(k => k.Key))
            _output.WriteLine($"  {kvp.Key}: {kvp.Value.Length} bytes");

        _output.WriteLine("\nMISSING in round-trip:");
        foreach (var kvp in originalStreams.OrderBy(k => k.Key))
        {
            if (!rtStreams.ContainsKey(kvp.Key))
                _output.WriteLine($"  MISSING: {kvp.Key} ({kvp.Value.Length} bytes)");
        }

        _output.WriteLine("\nSTREAM COMPARISON:");
        foreach (var kvp in originalStreams.OrderBy(k => k.Key))
        {
            if (rtStreams.TryGetValue(kvp.Key, out var rtData))
            {
                if (kvp.Value.Length != rtData.Length)
                {
                    _output.WriteLine($"  {kvp.Key}: original={kvp.Value.Length}, rt={rtData.Length}, diff={rtData.Length - kvp.Value.Length}");
                }
                else if (!kvp.Value.SequenceEqual(rtData))
                {
                    var firstDiff = -1;
                    for (int i = 0; i < kvp.Value.Length; i++)
                    {
                        if (kvp.Value[i] != rtData[i]) { firstDiff = i; break; }
                    }
                    _output.WriteLine($"  {kvp.Key}: same size ({kvp.Value.Length}) but differs at byte {firstDiff}");
                }
                else
                {
                    _output.WriteLine($"  {kvp.Key}: IDENTICAL ({kvp.Value.Length} bytes)");
                }
            }
        }

        // Show hex comparison for Data streams that differ
        foreach (var kvp in originalStreams.Where(k => k.Key.EndsWith("/Data") || k.Key == "FileHeader"))
        {
            if (rtStreams.TryGetValue(kvp.Key, out var rtData) && !kvp.Value.SequenceEqual(rtData))
            {
                _output.WriteLine($"\n=== Hex diff: {kvp.Key} ===");
                _output.WriteLine($"Original: {kvp.Value.Length} bytes, RT: {rtData.Length} bytes");
                var maxLen = Math.Min(kvp.Value.Length, rtData.Length);
                var diffCount = 0;
                for (int i = 0; i < maxLen && diffCount < 30; i++)
                {
                    if (kvp.Value[i] != rtData[i])
                    {
                        _output.WriteLine($"  Byte {i:X4}: orig=0x{kvp.Value[i]:X2} rt=0x{rtData[i]:X2}");
                        diffCount++;
                    }
                }
            }
        }
    }

    [Fact]
    public void Diagnostic_DumpRawParameterStrings()
    {
        // Dump raw parameter strings from original SchLib files to see exact key casing for all record types
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var encoding = System.Text.Encoding.GetEncoding(1252);

        // Files covering all record types
        var schFiles = new[]
        {
            "RESISTOR.SchLib",          // RECORD=1,2,14,34
            "ARC_FULL.SchLib",          // RECORD=12 (Arc)
            "POLYGON_TEST.SchLib",      // RECORD=7 (Polygon)
            "POLYLINE_TEST.SchLib",     // RECORD=6 (Polyline)
            "ELLIPSE_TEST.SchLib",      // RECORD=8 (Ellipse)
            "PIE_TEST.SchLib",          // RECORD=9 (Pie)
            "ROUNDRECT_TEST.SchLib",    // RECORD=10 (RoundedRectangle)
            "LABEL_JUSTIFY_TEST.SchLib",// RECORD=4 (Label)
            "TEXTFRAME_TEST.SchLib",    // RECORD=28 (TextFrame)
            "VCC_SYMBOL.SchLib",        // RECORD=17 (PowerObject)
            "LINESTYLE_TEST.SchLib",    // RECORD=13 (Line)
            "ELLIPSE_ARC_TEST.SchLib",  // RECORD=11 (EllipticalArc)
            "PIN_PROPS_TEST.SchLib",    // RECORD=41 (Parameter)
            "POLYLINE_ARROW_TEST.SchLib",// RECORD=6 with shapes
            "POLYGON_COLORS_TEST.SchLib",// RECORD=7 with colors
        };

        foreach (var schFileName in schFiles)
        {
            var schFile = GetSchLibFiles().FirstOrDefault(f =>
                Path.GetFileName(f).Equals(schFileName, StringComparison.OrdinalIgnoreCase));
            if (schFile == null) continue;

            _output.WriteLine($"\n=== {schFileName} ===");
            using var cf = new OpenMcdf.CompoundFile(schFile);

            // Find component storage
            var componentName = Path.GetFileNameWithoutExtension(schFileName);
            OpenMcdf.CFStorage? compStorage = null;
            try { compStorage = cf.RootStorage.GetStorage(componentName); }
            catch
            {
                // Try to find any storage that's not FileHeader or Storage
                cf.RootStorage.VisitEntries(entry =>
                {
                    if (entry is OpenMcdf.CFStorage s && entry.Name != "Storage")
                        compStorage ??= s;
                }, false);
            }

            if (compStorage == null) continue;

            var data = compStorage.GetStream("Data").GetData();
            var offset = 0;
            var seenRecords = new HashSet<string>();
            while (offset + 4 <= data.Length)
            {
                var sizeHeader = BitConverter.ToInt32(data, offset);
                var flags = (byte)((sizeHeader >> 24) & 0xFF);
                var dataSize = sizeHeader & 0x00FFFFFF;
                offset += 4;

                if (dataSize <= 0 || offset + dataSize > data.Length) break;

                if (flags == 0x01)
                {
                    _output.WriteLine($"  BINARY PIN ({dataSize} bytes)");
                    offset += dataSize;
                }
                else
                {
                    var str = encoding.GetString(data, offset, dataSize).TrimEnd('\0');
                    // Extract RECORD type
                    var recordType = "?";
                    var recordMatch = System.Text.RegularExpressions.Regex.Match(str, @"\|RECORD=(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (recordMatch.Success) recordType = recordMatch.Groups[1].Value;

                    if (!seenRecords.Contains(recordType))
                    {
                        seenRecords.Add(recordType);
                        _output.WriteLine($"  RECORD={recordType}: {str}");
                    }
                    offset += dataSize;
                }
            }
        }

        // PcbLib: dump key structure info
        var pcbFile = GetPcbLibFiles().FirstOrDefault(f => Path.GetFileName(f).Equals("PAD_TH_ROUND.PcbLib", StringComparison.OrdinalIgnoreCase));
        if (pcbFile != null)
        {
            using var cf = new OpenMcdf.CompoundFile(pcbFile);

            _output.WriteLine("\n=== PcbLib Library/Data header (first 300 chars) ===");
            var libData = cf.RootStorage.GetStorage("Library").GetStream("Data").GetData();
            DumpParameterBlock(libData, 0, encoding, 300);

            _output.WriteLine("\n=== PcbLib PAD_TH_ROUND/Parameters ===");
            var compParams = cf.RootStorage.GetStorage("PAD_TH_ROUND").GetStream("Parameters").GetData();
            DumpParameterBlock(compParams, 0, encoding);

            _output.WriteLine("\n=== PcbLib UniqueID ===");
            try
            {
                var uidData = cf.RootStorage.GetStorage("PAD_TH_ROUND")
                    .GetStorage("UniqueIDPrimitiveInformation").GetStream("Data").GetData();
                DumpParameterBlock(uidData, 0, encoding);
            }
            catch { }
        }
    }

    [Theory]
    [InlineData("RESISTOR.SchLib")]
    [InlineData("BATTERY.SchLib")]
    [InlineData("FUSE.SchLib")]
    [InlineData("NAND_GATE.SchLib")]
    [InlineData("RELAY_COIL.SchLib")]
    public void Diagnostic_SchLib_ParameterStringComparison(string fileName = "RESISTOR.SchLib")
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var encoding = System.Text.Encoding.GetEncoding(1252);

        var testFile = GetSchLibFiles().FirstOrDefault(f =>
            Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase));
        if (testFile == null) { _output.WriteLine($"{fileName} not found"); return; }

        // Read original
        using var originalCf = new OpenMcdf.CompoundFile(testFile);

        // Round-trip
        SchLibrary lib;
        using (var stream = File.OpenRead(testFile)) { lib = new SchLibReader().Read(stream); }
        using var ms = new MemoryStream();
        new SchLibWriter().Write(lib, ms);
        ms.Position = 0;
        using var rtCf = new OpenMcdf.CompoundFile(ms);

        // Find first component name
        var compName = lib.Components.FirstOrDefault()?.Name ?? Path.GetFileNameWithoutExtension(fileName);

        // Compare FileHeader
        _output.WriteLine("=== FileHeader ===");
        var origFh = originalCf.RootStorage.GetStream("FileHeader").GetData();
        var rtFh = rtCf.RootStorage.GetStream("FileHeader").GetData();
        if (origFh.SequenceEqual(rtFh))
            _output.WriteLine($"IDENTICAL ({origFh.Length} bytes)");
        else
            _output.WriteLine($"ORIG ({origFh.Length}b) vs RT ({rtFh.Length}b)");

        // Compare Storage
        _output.WriteLine("\n=== Storage ===");
        var origSt = originalCf.RootStorage.GetStream("Storage").GetData();
        var rtSt = rtCf.RootStorage.GetStream("Storage").GetData();
        if (origSt.SequenceEqual(rtSt))
            _output.WriteLine($"IDENTICAL ({origSt.Length} bytes)");
        else
            _output.WriteLine($"ORIG ({origSt.Length}b) vs RT ({rtSt.Length}b)");

        // Compare Data records
        _output.WriteLine($"\n=== {compName}/Data ===");
        byte[] origData, rtData;
        try
        {
            origData = originalCf.RootStorage.GetStorage(compName).GetStream("Data").GetData();
        }
        catch
        {
            _output.WriteLine($"Original {compName}/Data not found");
            return;
        }
        try
        {
            rtData = rtCf.RootStorage.GetStorage(compName).GetStream("Data").GetData();
        }
        catch
        {
            _output.WriteLine($"RT {compName}/Data not found");
            return;
        }

        _output.WriteLine($"Original: {origData.Length} bytes, RT: {rtData.Length} bytes\n");

        var origRecords = DecodeAllRecords(origData, encoding);
        var rtRecords = DecodeAllRecords(rtData, encoding);

        var maxRecords = Math.Max(origRecords.Count, rtRecords.Count);
        for (int i = 0; i < maxRecords; i++)
        {
            var origStr = i < origRecords.Count ? origRecords[i] : "(missing)";
            var rtStr = i < rtRecords.Count ? rtRecords[i] : "(missing)";

            if (origStr == rtStr)
            {
                _output.WriteLine($"Record {i}: IDENTICAL ({origStr.Length} chars)");
            }
            else
            {
                _output.WriteLine($"Record {i} ORIG: {origStr}");
                _output.WriteLine($"Record {i} RT:   {rtStr}");
            }
        }
    }

    [Fact]
    public void Diagnostic_PinBinaryDump()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        var testFile = GetSchLibFiles().FirstOrDefault(f =>
            Path.GetFileName(f).Equals("RESISTOR.SchLib", StringComparison.OrdinalIgnoreCase));
        if (testFile == null) { _output.WriteLine("RESISTOR.SchLib not found"); return; }

        // Dump original pin bytes
        using var cf = new OpenMcdf.CompoundFile(testFile);
        var data = cf.RootStorage.GetStorage("RESISTOR").GetStream("Data").GetData();
        var offset = 0;
        var recordIndex = 0;
        while (offset + 4 <= data.Length)
        {
            var sizeHeader = BitConverter.ToInt32(data, offset);
            var flags = (byte)((sizeHeader >> 24) & 0xFF);
            var dataSize = sizeHeader & 0x00FFFFFF;
            offset += 4;
            if (dataSize <= 0 || offset + dataSize > data.Length) break;

            if (flags == 0x01)
            {
                _output.WriteLine($"ORIG Record {recordIndex} [BINARY PIN {dataSize} bytes]:");
                var hexStr = BitConverter.ToString(data, offset, dataSize).Replace("-", " ");
                _output.WriteLine($"  {hexStr}");
            }
            offset += dataSize;
            recordIndex++;
        }

        // Round-trip and dump
        SchLibrary lib;
        using (var stream = File.OpenRead(testFile)) { lib = new SchLibReader().Read(stream); }
        using var ms = new MemoryStream();
        new SchLibWriter().Write(lib, ms);
        ms.Position = 0;
        var rtData = new byte[ms.Length];
        ms.Read(rtData, 0, rtData.Length);

        // Parse the OLE compound file from the round-tripped data
        ms.Position = 0;
        using var rtCf = new OpenMcdf.CompoundFile(ms);
        var rtCompData = rtCf.RootStorage.GetStorage("RESISTOR").GetStream("Data").GetData();
        offset = 0;
        recordIndex = 0;
        while (offset + 4 <= rtCompData.Length)
        {
            var sizeHeader = BitConverter.ToInt32(rtCompData, offset);
            var flags = (byte)((sizeHeader >> 24) & 0xFF);
            var dataSize = sizeHeader & 0x00FFFFFF;
            offset += 4;
            if (dataSize <= 0 || offset + dataSize > rtCompData.Length) break;

            if (flags == 0x01)
            {
                _output.WriteLine($"RT   Record {recordIndex} [BINARY PIN {dataSize} bytes]:");
                var hexStr = BitConverter.ToString(rtCompData, offset, dataSize).Replace("-", " ");
                _output.WriteLine($"  {hexStr}");
            }
            offset += dataSize;
            recordIndex++;
        }
    }

    private string DecodeParamBlock(byte[] data, int offset, System.Text.Encoding encoding)
    {
        if (offset + 4 > data.Length) return "(empty)";
        var sizeHeader = BitConverter.ToInt32(data, offset);
        var size = sizeHeader & 0x00FFFFFF;
        if (size <= 0 || offset + 4 + size > data.Length) return $"(invalid size={size})";
        return encoding.GetString(data, offset + 4, size).TrimEnd('\0');
    }

    private List<string> DecodeAllRecords(byte[] data, System.Text.Encoding encoding)
    {
        var records = new List<string>();
        var offset = 0;
        while (offset + 4 <= data.Length)
        {
            var sizeHeader = BitConverter.ToInt32(data, offset);
            var flags = (byte)((sizeHeader >> 24) & 0xFF);
            var dataSize = sizeHeader & 0x00FFFFFF;
            offset += 4;
            if (dataSize <= 0 || offset + dataSize > data.Length) break;

            if (flags == 0x01)
            {
                records.Add($"[BINARY PIN {dataSize} bytes]");
            }
            else
            {
                records.Add(encoding.GetString(data, offset, dataSize).TrimEnd('\0'));
            }
            offset += dataSize;
        }
        return records;
    }

    private void DumpParameterBlock(byte[] data, int startOffset, System.Text.Encoding encoding, int maxChars = 2000)
    {
        if (startOffset + 4 > data.Length) return;
        var size = BitConverter.ToInt32(data, startOffset) & 0x00FFFFFF;
        if (size <= 0 || startOffset + 4 + size > data.Length)
        {
            _output.WriteLine($"  Block size: {size} (invalid or empty)");
            return;
        }
        var str = encoding.GetString(data, startOffset + 4, Math.Min(size, maxChars)).TrimEnd('\0');
        _output.WriteLine($"  Block size: {size}");
        _output.WriteLine($"  Content: {str}");
    }

    private static void EnumerateStreams(OpenMcdf.CFStorage storage, string path, Dictionary<string, byte[]> result)
    {
        storage.VisitEntries(entry =>
        {
            var fullPath = string.IsNullOrEmpty(path) ? entry.Name : $"{path}/{entry.Name}";
            if (entry is OpenMcdf.CFStream stream)
            {
                result[fullPath] = stream.GetData();
            }
            else if (entry is OpenMcdf.CFStorage subStorage)
            {
                EnumerateStreams(subStorage, fullPath, result);
            }
        }, false);
    }

    private static IEnumerable<string> GetSchLibFiles()
    {
        var root = GetDataPath("TestData");
        if (!Directory.Exists(root)) yield break;
        foreach (var file in Directory.GetFiles(root, "*.SchLib", SearchOption.AllDirectories))
            yield return file;
    }

    private static IEnumerable<string> GetPcbLibFiles()
    {
        var root = GetDataPath("TestData");
        if (!Directory.Exists(root)) yield break;
        foreach (var file in Directory.GetFiles(root, "*.PcbLib", SearchOption.AllDirectories))
            yield return file;
        // Also include .PCBLIB (case-insensitive on Windows, but explicit for safety)
        foreach (var file in Directory.GetFiles(root, "*.PCBLIB", SearchOption.AllDirectories))
            yield return file;
    }

    private static IEnumerable<string> GetSchDocFiles()
    {
        var root = GetDataPath("TestData");
        if (!Directory.Exists(root)) yield break;
        foreach (var file in Directory.GetFiles(root, "*.SchDoc", SearchOption.AllDirectories))
            yield return file;
    }

    private static IEnumerable<string> GetPcbDocFiles()
    {
        var root = GetDataPath("TestData");
        if (!Directory.Exists(root)) yield break;
        foreach (var file in Directory.GetFiles(root, "*.PcbDoc", SearchOption.AllDirectories))
            yield return file;
    }

    [Fact]
    public void PcbLib_RoundTrip_StreamLevelDiagnostic()
    {
        foreach (var filePath in GetPcbLibFiles())
        {
            var fileName = Path.GetFileName(filePath);
            var originalBytes = File.ReadAllBytes(filePath);

            PcbLibrary lib;
            using (var stream = new MemoryStream(originalBytes))
            {
                lib = new PcbLibReader().Read(stream);
            }

            using var ms = new MemoryStream();
            new PcbLibWriter().Write(lib, ms);
            var roundTrippedBytes = ms.ToArray();

            var ratio = originalBytes.Length > 0
                ? (double)roundTrippedBytes.Length / originalBytes.Length * 100
                : 100.0;

            // Only diagnose files that are not 100%
            if (Math.Abs(ratio - 100.0) < 0.05)
                continue;

            _output.WriteLine($"\n=== {fileName}: {ratio:F1}% (orig={originalBytes.Length}, rt={roundTrippedBytes.Length}) ===");

            // Compare streams using OpenMcdf
            using var origCf = new CompoundFile(new MemoryStream(originalBytes));
            using var rtCf = new CompoundFile(new MemoryStream(roundTrippedBytes));

            CompareStorage(origCf.RootStorage, rtCf.RootStorage, "");
        }
    }

    private void CompareStorage(CFStorage orig, CFStorage rt, string path)
    {
        var origStreams = new Dictionary<string, byte[]>();
        var origStorages = new Dictionary<string, CFStorage>();
        var rtStreams = new Dictionary<string, byte[]>();
        var rtStorages = new Dictionary<string, CFStorage>();

        orig.VisitEntries(item =>
        {
            if (item is CFStream s) origStreams[s.Name] = s.GetData();
            else if (item is CFStorage st) origStorages[st.Name] = st;
        }, false);

        rt.VisitEntries(item =>
        {
            if (item is CFStream s) rtStreams[s.Name] = s.GetData();
            else if (item is CFStorage st) rtStorages[st.Name] = st;
        }, false);

        foreach (var name in origStreams.Keys.Union(rtStreams.Keys).OrderBy(n => n))
        {
            var fullPath = string.IsNullOrEmpty(path) ? name : $"{path}/{name}";
            var inOrig = origStreams.TryGetValue(name, out var origData);
            var inRt = rtStreams.TryGetValue(name, out var rtData);

            if (inOrig && !inRt)
                _output.WriteLine($"  MISSING in RT: {fullPath} ({origData!.Length} bytes)");
            else if (!inOrig && inRt)
                _output.WriteLine($"  EXTRA in RT: {fullPath} ({rtData!.Length} bytes)");
            else if (inOrig && inRt && origData!.Length != rtData!.Length)
                _output.WriteLine($"  SIZE DIFF: {fullPath} orig={origData.Length} rt={rtData.Length} diff={rtData.Length - origData.Length}");
        }

        foreach (var name in origStorages.Keys.Union(rtStorages.Keys).OrderBy(n => n))
        {
            var fullPath = string.IsNullOrEmpty(path) ? name : $"{path}/{name}";
            var inOrig = origStorages.TryGetValue(name, out var origSt);
            var inRt = rtStorages.TryGetValue(name, out var rtSt);

            if (inOrig && !inRt)
                _output.WriteLine($"  MISSING storage in RT: {fullPath}");
            else if (!inOrig && inRt)
                _output.WriteLine($"  EXTRA storage in RT: {fullPath}");
            else if (inOrig && inRt)
                CompareStorage(origSt!, rtSt!, fullPath);
        }
    }

    [Fact]
    public void PcbLib_DataStream_PrimitiveSizeDiagnostic()
    {
        var targetFiles = new[] { "AD LFCSP-24 4X4MM CP-24-8", "PSEMI QFN-12 3x3x0.5" };

        foreach (var filePath in GetPcbLibFiles())
        {
            var fileName = Path.GetFileName(filePath);

            // Read original compound file
            var originalBytes = File.ReadAllBytes(filePath);
            using var origCf = new CompoundFile(new MemoryStream(originalBytes));

            // Find component storages
            origCf.RootStorage.VisitEntries(item =>
            {
                if (item is not CFStorage storage) return;
                if (storage.Name == "Library") return;

                // Check if this matches one of our target components
                var match = false;
                foreach (var t in targetFiles)
                    if (storage.Name.Contains(t, StringComparison.OrdinalIgnoreCase))
                        match = true;
                if (!match) return;

                // Read the Data stream
                CFStream? dataStream = null;
                storage.VisitEntries(e =>
                {
                    if (e is CFStream s && s.Name == "Data")
                        dataStream = s;
                }, false);

                if (dataStream == null) return;

                var data = dataStream.GetData();
                _output.WriteLine($"\n=== {fileName} / {storage.Name}/Data ({data.Length} bytes) ===");

                using var ms = new MemoryStream(data);
                using var br = new BinaryReader(ms);

                // Read pattern name (StringBlock: 4-byte len + string)
                var patternLen = br.ReadInt32();
                var pattern = System.Text.Encoding.ASCII.GetString(br.ReadBytes(patternLen));
                _output.WriteLine($"  Pattern: {pattern} ({4 + patternLen} bytes)");

                var primIndex = 0;
                while (ms.Position < ms.Length)
                {
                    var startPos = ms.Position;
                    var objectId = br.ReadByte();
                    var objectName = objectId switch
                    {
                        1 => "Arc", 2 => "Pad", 3 => "Via", 4 => "Track",
                        5 => "Text", 6 => "Fill", 11 => "Region", 12 => "Body3D",
                        _ => $"Unknown({objectId})"
                    };

                    if (objectId == 2) // Pad - complex multi-block
                    {
                        var desLen = br.ReadInt32();
                        br.ReadBytes(desLen); // designator
                        var blk1Size = br.ReadInt32() & 0x00FFFFFF;
                        br.ReadBytes(blk1Size); // unknown block 1
                        var netLen = br.ReadInt32();
                        br.ReadBytes(netLen); // net string
                        var blk2Size = br.ReadInt32() & 0x00FFFFFF;
                        br.ReadBytes(blk2Size); // unknown block 2
                        var mainSize = br.ReadInt32() & 0x00FFFFFF;
                        br.ReadBytes(mainSize); // main block
                        var shapeSize = br.ReadInt32() & 0x00FFFFFF;
                        br.ReadBytes(shapeSize); // shape block

                        var totalSize = ms.Position - startPos;
                        _output.WriteLine($"  [{primIndex}] {objectName}: {totalSize} bytes (des={desLen}, blk1={blk1Size}, net={netLen}, blk2={blk2Size}, main={mainSize}, shape={shapeSize})");
                    }
                    else if (objectId == 5) // Text - has extra string block
                    {
                        var blockSize = br.ReadInt32() & 0x00FFFFFF;
                        br.ReadBytes(blockSize);
                        var txtLen = br.ReadInt32();
                        br.ReadBytes(txtLen);
                        var totalSize = ms.Position - startPos;
                        _output.WriteLine($"  [{primIndex}] {objectName}: {totalSize} bytes (block={blockSize}, text={txtLen})");
                    }
                    else // Simple block
                    {
                        var blockSize = br.ReadInt32() & 0x00FFFFFF;
                        br.ReadBytes(blockSize);
                        var totalSize = ms.Position - startPos;
                        _output.WriteLine($"  [{primIndex}] {objectName}: {totalSize} bytes (block={blockSize})");
                    }

                    primIndex++;
                }
            }, false);
        }
    }

    private static string GetDataPath(params string[] parts)
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }
}
