using System;
using System.Linq;
using System.Drawing;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchComponent : SchGraphicalObject, IComponent
    {
        public string Name => LibReference;

        public string Description => ComponentDescription;

        public override CoordRect CalculateBounds() =>
            CoordRect.Union(GetPrimitivesOfType<Primitive>(false)
                .Select(p => p.CalculateBounds()));

        public string UniqueId { get; internal set; }
        public int CurrentPartId { get; internal set; }
        public string LibReference { get; internal set; }
        public string ComponentDescription { get; internal set; }
        public int PartCount { get; internal set; }
        public int DisplayModeCount { get; internal set; }
        public int DisplayMode { get; internal set; }
        public string LibraryPath { get; internal set; }
        public string SourceLibraryName { get; internal set; }
        public string SheetPartFilename { get; internal set; }
        public string TargetFilename { get; internal set; }
        public bool DesignatorLocked { get; internal set; }
        public bool PartIdLocked { get; internal set; }
        public string DesignItemId { get; internal set; }
        public int ComponentKind { get; internal set; }
        public string AliasList { get; internal set; }
        public int AllPinCount { get; internal set; }

        public SchComponent()
        {
            Record = 1;
            DisplayModeCount = 1;
            IndexInSheet = -1;
            OwnerPartId = -1;
            CurrentPartId = 1;
            LibraryPath = "*";
            SourceLibraryName = "*";
            SheetPartFilename = "*";
            TargetFilename = "*";
            UniqueId = Utils.GenerateUniqueId();
            AreaColor = ColorTranslator.FromWin32(11599871);
            Color = ColorTranslator.FromWin32(128);
            PartIdLocked = true;
        }

        public void Add(SchPrimitive primitive)
        {
            Primitives.Add(primitive);
        }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            UniqueId = p["UNIQUEID"].AsStringOrDefault();
            CurrentPartId = p["CURRENTPARTID"].AsIntOrDefault();
            LibReference = p["LIBREFERENCE"].AsStringOrDefault();
            ComponentDescription = p["COMPONENTDESCRIPTION"].AsStringOrDefault();
            PartCount = p["PARTCOUNT"].AsIntOrDefault() - 1; // for some reason this number is one more than the actual number of parts
            DisplayModeCount = p["DISPLAYMODECOUNT"].AsIntOrDefault();
            DisplayMode = p["DISPLAYMODE"].AsIntOrDefault();
            LibraryPath = p["LIBRARYPATH"].AsStringOrDefault("*");
            SourceLibraryName = p["SOURCELIBRARYNAME"].AsStringOrDefault("*");
            SheetPartFilename = p["SHEETPARTFILENAME"].AsStringOrDefault("*");
            TargetFilename = p["TARGETFILENAME"].AsStringOrDefault("*");
            DesignatorLocked = p["DESIGNATORLOCKED"].AsBool();
            PartIdLocked = p["PARTIDLOCKED"].AsBool();
            DesignItemId = p["DESIGNITEMID"].AsStringOrDefault();
            ComponentKind = p["COMPONENTKIND"].AsIntOrDefault();
            AliasList = p["ALIASLIST"].AsStringOrDefault();
            AllPinCount = p["ALLPINCOUNT"].AsIntOrDefault();
        }
        
        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            p.Add("RECORD", Record);
            p.Add("LIBREFERENCE", LibReference);
            p.Add("COMPONENTDESCRIPTION", ComponentDescription);
            p.Add("PARTCOUNT", PartCount + 1);
            p.Add("DISPLAYMODECOUNT", DisplayModeCount);
            p.Add("INDEXINSHEET", IndexInSheet);
            p.Add("OWNERPARTID", OwnerPartId);
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
            p.Add("CURRENTPARTID", CurrentPartId);
            p.Add("LIBRARYPATH", LibraryPath);
            p.Add("SOURCELIBRARYNAME", SourceLibraryName);
            p.Add("SHEETPARTFILENAME", SheetPartFilename);
            p.Add("TARGETFILENAME", TargetFilename);
            p.Add("UNIQUEID", UniqueId);
            p.Add("AREACOLOR", AreaColor);
            p.Add("COLOR", Color);
            p.Add("DISPLAYMODE", DisplayMode);
            p.Add("DESIGNATORLOCKED", DesignatorLocked);
            p.Add("PARTIDLOCKED", PartIdLocked, false);
            p.Add("ALIASLIST", AliasList);
            p.Add("DESIGNITEMID", DesignItemId);
            p.Add("COMPONENTKIND", ComponentKind);
            p.Add("ALLPINCOUNT", AllPinCount);

            base.ExportToParameters(p); // call base last so the parameter order is overriden
        }
    }
}
