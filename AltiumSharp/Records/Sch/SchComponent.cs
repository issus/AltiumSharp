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
        public int CurrentPartId { get; internal set; } = -1;
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
        }
        
        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            p.Add("RECORD", Record);
            p.Add("LIBREFERENCE", LibReference);
            p.Add("PARTCOUNT", PartCount + 1);
            p.Add("DISPLAYMODECOUNT", DisplayModeCount);
            p.Add("INDEXINSHEET", IndexInSheet);
            p.Add("OWNERPARTID", OwnerPartId);
            p.Add("CURRENTPARTID", CurrentPartId);
            p.Add("LIBRARYPATH", LibraryPath);
            p.Add("SOURCELIBRARYNAME", SourceLibraryName);
            p.Add("SHEETPARTFILENAME", SheetPartFilename);
            p.Add("TARGETFILENAME", TargetFilename);
            p.Add("UNIQUEID", UniqueId);
            p.Add("AREACOLOR", AreaColor);
            p.Add("COLOR", Color);
            p.Add("COMPONENTDESCRIPTION", ComponentDescription);
            p.Add("DISPLAYMODE", DisplayMode);
            p.Add("DESIGNATORLOCKED", DesignatorLocked);
            p.Add("PARTIDLOCKED", PartIdLocked, false);
            p.Add("ALIASLIST", AliasList);
            p.Add("DESIGNITEMID", DesignItemId);
            p.Add("COMPONENTKIND", ComponentKind);

            base.ExportToParameters(p); // call base last so the parameter order is overriden
        }
    }
}
