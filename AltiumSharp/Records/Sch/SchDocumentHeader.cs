using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public abstract class SchDocumentHeader : SchPrimitive
    {
        public string UniqueId { get; internal set; }
        public List<(string FontName, int Size, int Rotation, bool Italic, bool Bold, bool Underline)> FontId { get; } =
            new List<(string FontName, int Size, int Rotation, bool Italic, bool Bold, bool Underline)>();
        public bool UseMbcs { get; internal set; }
        public bool IsBoc { get; internal set; }
        public bool HotSpotGridOn { get; internal set; }
        public int HotSpotGridSize { get; internal set; }
        public int SheetStyle { get; internal set; }
        public int SystemFont { get; internal set; }
        public bool BorderOn { get; internal set; }
        public int SheetNumberSpaceSize { get; internal set; }
        public Color AreaColor { get; internal set; }
        public bool SnapGridOn { get; internal set; }
        public Coord SnapGridSize { get; internal set; }
        public bool VisibleGridOn { get; internal set; }
        public Coord VisibleGridSize { get; internal set; }
        public int CustomX { get; internal set; }
        public int CustomY { get; internal set; }
        public bool UseCustomSheet { get; internal set; }
        public bool ReferenceZonesOn { get; internal set; }
        public bool ShowTemplateGraphics { get; internal set; }
        public Unit DisplayUnit { get; internal set; }

        public SchDocumentHeader() : base()
        {
            FontId.Add(("Times New Roman", 10, 0, false, false, false));
            UseMbcs = true;
            IsBoc = true;
            BorderOn = true;
            AreaColor = ColorTranslator.FromWin32(16317695);
            SnapGridOn = true;
            SnapGridSize = 10;
            VisibleGridOn = true;
            VisibleGridSize = 10;
        }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            UniqueId = p["UNIQUEID"].AsStringOrDefault();
            var fontIdCount = p["FONTIDCOUNT"].AsIntOrDefault();
            FontId.Clear();
            for (int i = 0; i < fontIdCount; ++i)
            {
                var fontId = (
                    p[$"FONTNAME{i+1}"].AsStringOrDefault(),
                    p[$"SIZE{i+1}"].AsIntOrDefault(),
                    p[$"ROTATION{i+1}"].AsIntOrDefault(),
                    p[$"ITALIC{i+1}"].AsBool(),
                    p[$"BOLD{i+1}"].AsBool(),
                    p[$"UNDERLINE{i+1}"].AsBool()
                );
                FontId.Add(fontId);
            }
            UseMbcs = p["USEMBCS"].AsBool();
            IsBoc = p["ISBOC"].AsBool();
            HotSpotGridOn = p["HOTSPOTGRIDON"].AsBool();
            HotSpotGridSize = p["HOTSPOTGRIDSIZE"].AsIntOrDefault();
            SheetStyle = p["SHEETSTYLE"].AsIntOrDefault();
            SystemFont = p["SYSTEMFONT"].AsIntOrDefault(1);
            BorderOn = p["BORDERON"].AsBool();
            SheetNumberSpaceSize = p["SHEETNUMBERSPACESIZE"].AsIntOrDefault();
            AreaColor = p["AREACOLOR"].AsColorOrDefault();
            SnapGridOn = p["SNAPGRIDON"].AsBool();
            SnapGridSize = Utils.DxpFracToCoord(p["SNAPGRIDSIZE"].AsIntOrDefault(), p["SNAPGRIDSIZE_FRAC"].AsIntOrDefault());
            VisibleGridOn = p["VISIBLEGRIDON"].AsBool();
            VisibleGridSize = Utils.DxpFracToCoord(p["VISIBLEGRIDSIZE"].AsIntOrDefault(), p["VISIBLEGRIDSIZE_FRAC"].AsIntOrDefault());
            CustomX = p["CUSTOMX"].AsIntOrDefault();
            CustomY = p["CUSTOMY"].AsIntOrDefault();
            UseCustomSheet = p["USECUSTOMSHEET"].AsBool();
            ReferenceZonesOn = p["REFERENCEZONESON"].AsBool();
            ShowTemplateGraphics = p["SHOWTEMPLATEGRAPHICS"].AsBool();
            DisplayUnit = (Unit)p["DISPLAY_UNIT"].AsIntOrDefault();
        }
        
        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("UNIQUEID", UniqueId);
            p.Add("FONTIDCOUNT", FontId.Count);
            for (var i = 0; i < FontId.Count; ++i)
            {
                var fontId = FontId[i];
                p.Add($"SIZE{i+1}", fontId.Size);
                p.Add($"ROTATION{i+1}", fontId.Rotation);
                p.Add($"ITALIC{i+1}", fontId.Italic);
                p.Add($"BOLD{i+1}", fontId.Bold);
                p.Add($"UNDERLINE{i+1}", fontId.Underline);
                p.Add($"FONTNAME{i+1}", fontId.FontName);
            }
            p.Add("USEMBCS", UseMbcs);
            p.Add("ISBOC", IsBoc);
            p.Add("HOTSPOTGRIDON", HotSpotGridOn);
            p.Add("HOTSPOTGRIDSIZE", HotSpotGridSize);
            p.Add("SHEETSTYLE", SheetStyle);
            p.Add("SYSTEMFONT", SystemFont);
            p.Add("BORDERON", BorderOn);
            p.Add("SHEETNUMBERSPACESIZE", SheetNumberSpaceSize);
            p.Add("AREACOLOR", AreaColor);
            p.Add("SNAPGRIDON", SnapGridOn);
            {
                var (n, f) = Utils.CoordToDxpFrac(SnapGridSize);
                if (n != 0 || f != 0) p.Add("SNAPGRIDSIZE", n);
                if (f != 0) p.Add("SNAPGRIDSIZE" + "_FRAC", f);
            }
            p.Add("VISIBLEGRIDON", VisibleGridOn);
            {
                var (n, f) = Utils.CoordToDxpFrac(VisibleGridSize);
                if (n != 0 || f != 0) p.Add("VISIBLEGRIDSIZE", n);
                if (f != 0) p.Add("VISIBLEGRIDSIZE" + "_FRAC", f);
            }
            p.Add("CUSTOMX", CustomX);
            p.Add("CUSTOMY", CustomY);
            p.Add("USECUSTOMSHEET", UseCustomSheet);
            p.Add("REFERENCEZONESON", ReferenceZonesOn);
            p.Add("SHOWTEMPLATEGRAPHICS", ShowTemplateGraphics);
            p.Add("DISPLAY_UNIT", (int)DisplayUnit, false);
        }
    }
}
