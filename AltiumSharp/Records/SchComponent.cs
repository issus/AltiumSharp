using System;
using System.Drawing;
using System.Linq;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchComponent : SchPrimitive, IComponent
    {
        public string Name => LibReference;

        public string Description => ComponentDescription;

        public override CoordRect CalculateBounds() =>
            CoordRect.Union(GetPrimitivesOfType<Primitive>(false)
                .Select(p => p.CalculateBounds()));

        public string LibReference { get; internal set; }
        public string ComponentDescription { get; internal set; }
        public int PartCount { get; internal set; }
        public int DisplayModeCount { get; internal set; }
        public int DisplayMode { get; internal set; }
        public CoordPoint Location { get; internal set; }
        public string LibraryPath { get; internal set; }
        public string SourceLibraryName { get; internal set; }
        public string SheetPartFilename { get; internal set; }
        public string TargetFilename { get; internal set; }
        public Color AreaColor { get; internal set; }
        public Color Color { get; internal set; }
        public bool PartIdLocked { get; internal set; }
        public string AliasList { get; internal set; }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            LibReference = p["LIBREFERENCE"].AsStringOrDefault();
            ComponentDescription = p["COMPONENTDESCRIPTION"].AsStringOrDefault();
            PartCount = p["PARTCOUNT"].AsIntOrDefault() - 1; // for some reason this number is one more than the actual number of parts
            DisplayModeCount = p["DISPLAYMODECOUNT"].AsIntOrDefault();
            DisplayMode = p["DISPLAYMODE"].AsIntOrDefault();
            Location = new CoordPoint(
                Utils.DxpFracToCoord(p["LOCATION.X"].AsIntOrDefault(), p["LOCATION.X_FRAC"].AsIntOrDefault()),
                Utils.DxpFracToCoord(p["LOCATION.Y"].AsIntOrDefault(), p["LOCATION.Y_FRAC"].AsIntOrDefault()));
            LibraryPath = p["LIBRARYPATH"].AsStringOrDefault();
            SourceLibraryName = p["SOURCELIBRARYNAME"].AsStringOrDefault();
            SheetPartFilename = p["SHEETPARTFILENAME"].AsStringOrDefault();
            TargetFilename = p["TARGETFILENAME"].AsStringOrDefault();
            AreaColor = p["AREACOLOR"].AsColorOrDefault();
            Color = p["COLOR"].AsColorOrDefault();
            PartIdLocked = p["PARTIDLOCKED"].AsBool();
            AliasList = p["ALIASLIST"].AsStringOrDefault();
        }
        
        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("LIBREFERENCE", LibReference);
            p.Add("COMPONENTDESCRIPTION", ComponentDescription);
            p.Add("PARTCOUNT", PartCount + 1);
            p.Add("DISPLAYMODECOUNT", DisplayModeCount);
            p.Add("DISPLAYMODE", DisplayMode);
            {
                var (n, f) = Utils.CoordToDxpFrac(Location.X);
                if (n != 0 || f != 0) p.Add("LOCATION.X", n);
                if (f != 0) p.Add("LOCATION.X" + "_FRAC", f);
            }
            {
                var (n, f) = Utils.CoordToDxpFrac(Location.Y);
                if (n != 0 || f != 0) p.Add("LOCATION.Y", n);
                if (f != 0) p.Add("LOCATION.Y" + "_FRAC", f);
            }
            p.Add("LIBRARYPATH", LibraryPath);
            p.Add("SOURCELIBRARYNAME", SourceLibraryName);
            p.Add("SHEETPARTFILENAME", SheetPartFilename);
            p.Add("TARGETFILENAME", TargetFilename);
            p.Add("AREACOLOR", AreaColor);
            p.Add("COLOR", Color);
            p.Add("PARTIDLOCKED", PartIdLocked);
            p.Add("ALIASLIST", AliasList);
        }
    }
}
