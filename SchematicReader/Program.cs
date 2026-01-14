using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using OriginalCircuit.AltiumSharp;
using OriginalCircuit.AltiumSharp.Records;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: SchematicReader <file.SchDoc>");
            Console.Error.WriteLine("Reads an Altium SchDoc file and outputs JSON to stdout.");
            Environment.Exit(1);
        }

        string testFile = args[0];

        try
        {
            using var reader = new SchDocReader();
            var schDoc = reader.Read(testFile);

            var output = new SchematicDump
            {
                FileName = Path.GetFileName(testFile),

                Components = schDoc.Items.OfType<SchComponent>().Select(c => new ComponentDump
                {
                    LibReference = c.LibReference,
                    SourceLibraryName = c.SourceLibraryName,
                    LibraryPath = c.LibraryPath,
                    Designator = c.Designator?.Text,
                    X = (int)(c.Location.X.ToMils()),
                    Y = (int)(c.Location.Y.ToMils()),
                    PrimitivesCount = c.GetAllPrimitives().Count(),
                    PrimitiveTypes = c.GetAllPrimitives().Select(p => p.GetType().Name).Distinct().ToList(),
                    Pins = c.GetPrimitivesOfType<SchPin>().Select(p => {
                        var corner = p.GetCorner();
                        return new PinDump
                        {
                            Name = p.Name,
                            Designator = p.Designator,
                            X = (int)(p.Location.X.ToMils()),
                            Y = (int)(p.Location.Y.ToMils()),
                            ConnX = (int)(corner.X.ToMils()),
                            ConnY = (int)(corner.Y.ToMils()),
                            Length = (int)(p.PinLength.ToMils()),
                            Electrical = p.Electrical.ToString()
                        };
                    }).ToList()
                }).ToList(),

                Wires = schDoc.Items.OfType<SchWire>().Select(w => new WireDump
                {
                    Points = w.Vertices.Select(v => new PointDump
                    {
                        X = (int)(v.X.ToMils()),
                        Y = (int)(v.Y.ToMils())
                    }).ToList()
                }).ToList(),

                NetLabels = schDoc.Items.OfType<SchNetLabel>().Select(n => new NetLabelDump
                {
                    Text = n.Text,
                    X = (int)(n.Location.X.ToMils()),
                    Y = (int)(n.Location.Y.ToMils())
                }).ToList(),

                PowerPorts = schDoc.Items.OfType<SchPowerObject>().Select(p => new PowerPortDump
                {
                    Text = p.Text,
                    Style = p.Style.ToString(),
                    X = (int)(p.Location.X.ToMils()),
                    Y = (int)(p.Location.Y.ToMils())
                }).ToList(),

                Stats = new StatsDump
                {
                    TotalComponents = schDoc.Items.OfType<SchComponent>().Count(),
                    TotalWires = schDoc.Items.OfType<SchWire>().Count(),
                    TotalPins = schDoc.Items.OfType<SchComponent>()
                        .SelectMany(c => c.GetPrimitivesOfType<SchPin>()).Count(),
                    TotalNetLabels = schDoc.Items.OfType<SchNetLabel>().Count(),
                    TotalPowerPorts = schDoc.Items.OfType<SchPowerObject>().Count()
                }
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            Console.WriteLine(JsonSerializer.Serialize(output, options));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}

// Data classes for JSON serialization
class SchematicDump
{
    public string FileName { get; set; }
    public StatsDump Stats { get; set; }
    public List<ComponentDump> Components { get; set; }
    public List<WireDump> Wires { get; set; }
    public List<NetLabelDump> NetLabels { get; set; }
    public List<PowerPortDump> PowerPorts { get; set; }
}

class StatsDump
{
    public int TotalComponents { get; set; }
    public int TotalWires { get; set; }
    public int TotalPins { get; set; }
    public int TotalNetLabels { get; set; }
    public int TotalPowerPorts { get; set; }
}

class ComponentDump
{
    public string LibReference { get; set; }
    public string SourceLibraryName { get; set; }
    public string LibraryPath { get; set; }
    public string Designator { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int PrimitivesCount { get; set; }
    public List<string> PrimitiveTypes { get; set; }
    public List<PinDump> Pins { get; set; }
}

class PinDump
{
    public string Name { get; set; }
    public string Designator { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int ConnX { get; set; }
    public int ConnY { get; set; }
    public int Length { get; set; }
    public string Electrical { get; set; }
}

class WireDump
{
    public List<PointDump> Points { get; set; }
}

class PointDump
{
    public int X { get; set; }
    public int Y { get; set; }
}

class NetLabelDump
{
    public string Text { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

class PowerPortDump
{
    public string Text { get; set; }
    public string Style { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}
