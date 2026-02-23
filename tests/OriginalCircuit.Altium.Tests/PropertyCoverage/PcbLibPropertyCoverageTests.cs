using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Serialization.Readers;
using OriginalCircuit.Altium.Serialization.Writers;

namespace OriginalCircuit.Altium.Tests.PropertyCoverage;

/// <summary>
/// Round-trip property coverage tests for PcbLib types.
/// </summary>
public sealed class PcbLibPropertyCoverageTests
{
    private static PcbLibrary RoundTrip(PcbLibrary original)
    {
        using var ms = new MemoryStream();
        new PcbLibWriter().Write(original, ms);
        ms.Position = 0;
        return (PcbLibrary)new PcbLibReader().Read(ms);
    }

    [Fact]
    public void PcbModel_PreservesAllProperties()
    {
        var library = new PcbLibrary();

        var component = PcbComponent.Create("WithModel")
            .WithDescription("Component with 3D model")
            .AddPad(p => p
                .At(Coord.FromMils(0), Coord.FromMils(0))
                .Size(Coord.FromMils(60), Coord.FromMils(60))
                .WithDesignator("1")
                .Layer(74))
            .Build();

        var model = new PcbModel
        {
            Id = "MODEL-GUID-001",
            Name = "TestModel.step",
            IsEmbedded = true,
            ModelSource = "FromFile",
            RotationX = 15.5,
            RotationY = 30.0,
            RotationZ = 45.5,
            Dz = 100,
            Checksum = 0,
            StepData = "ISO-10303-21;\nHEADER;\nFILE_DESCRIPTION(('Test'),'2;1');\nFILE_NAME('test.step','2024-01-01');\nFILE_SCHEMA(('AUTOMOTIVE_DESIGN'));\nENDSEC;\nDATA;\n#1=PRODUCT('test','test','',(#2));\n#2=PRODUCT_CONTEXT('',#3,'');\n#3=APPLICATION_CONTEXT('automotive_design');\nENDSEC;\nEND-ISO-10303-21;"
        };
        library.Models.Add(model);
        library.Add(component);

        var readBack = RoundTrip(library);
        Assert.Single(readBack.Models);
        var m = readBack.Models[0];

        Assert.Equal("MODEL-GUID-001", m.Id);
        Assert.Equal("TestModel.step", m.Name);
        Assert.True(m.IsEmbedded);
        Assert.Equal("FromFile", m.ModelSource);
        Assert.Equal(15.5, m.RotationX, 0.01);
        Assert.Equal(30.0, m.RotationY, 0.01);
        Assert.Equal(45.5, m.RotationZ, 0.01);
        Assert.Equal(100, m.Dz);
        Assert.Contains("ISO-10303-21", m.StepData);
        Assert.Contains("AUTOMOTIVE_DESIGN", m.StepData);
    }

    [Fact]
    public void PcbModel_MultipleModels_RoundTrip()
    {
        var library = new PcbLibrary();

        var component = PcbComponent.Create("MultiModel")
            .WithDescription("Component with multiple 3D models")
            .AddPad(p => p
                .At(Coord.FromMils(0), Coord.FromMils(0))
                .Size(Coord.FromMils(60), Coord.FromMils(60))
                .WithDesignator("1")
                .Layer(74))
            .Build();

        var model1 = new PcbModel
        {
            Id = "MODEL-001",
            Name = "Body.step",
            IsEmbedded = true,
            ModelSource = "FromFile",
            RotationX = 0,
            RotationY = 0,
            RotationZ = 0,
            Dz = 0,
            StepData = "ISO-10303-21;\nHEADER;\nFILE_DESCRIPTION(('Body'),'2;1');\nENDSEC;\nDATA;\nENDSEC;\nEND-ISO-10303-21;"
        };

        var model2 = new PcbModel
        {
            Id = "MODEL-002",
            Name = "Leads.step",
            IsEmbedded = true,
            ModelSource = "FromFile",
            RotationX = 90.0,
            RotationY = 0,
            RotationZ = 0,
            Dz = 50,
            StepData = "ISO-10303-21;\nHEADER;\nFILE_DESCRIPTION(('Leads'),'2;1');\nENDSEC;\nDATA;\nENDSEC;\nEND-ISO-10303-21;"
        };

        library.Models.Add(model1);
        library.Models.Add(model2);
        library.Add(component);

        var readBack = RoundTrip(library);
        Assert.Equal(2, readBack.Models.Count);

        Assert.Equal("MODEL-001", readBack.Models[0].Id);
        Assert.Equal("Body.step", readBack.Models[0].Name);
        Assert.Contains("Body", readBack.Models[0].StepData);

        Assert.Equal("MODEL-002", readBack.Models[1].Id);
        Assert.Equal("Leads.step", readBack.Models[1].Name);
        Assert.Equal(90.0, readBack.Models[1].RotationX, 0.01);
        Assert.Equal(50, readBack.Models[1].Dz);
        Assert.Contains("Leads", readBack.Models[1].StepData);
    }
}
