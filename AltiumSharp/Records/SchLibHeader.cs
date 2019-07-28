using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchLibHeader // TODO: Unify with Record31?
    {
        public string Header { get; internal set; }
        public int Weight { get; internal set; }
        public int MinorVersion { get; internal set; }
        public List<(int Size, string FontName, int Rotation, bool Italic, bool Bold, bool Underline)> FontId { get; internal set; }
        public bool UseMbcs { get; internal set; }
        public bool IsBoc { get; internal set; }
        public int SheetStyle { get; internal set; }
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
        public Unit DisplayUnit { get; internal set; }
        public List<(string LibRef, string CompDescr, int PartCount)> Comp { get; internal set; }

        public void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            Header = p["HEADER"].AsStringOrDefault();
            Weight = p["WEIGHT"].AsIntOrDefault();
            MinorVersion = p["MINORVERSION"].AsIntOrDefault();
            FontId = Enumerable.Range(1, p["FONTIDCOUNT"].AsIntOrDefault())
                .Select(i => (
                    p[string.Format(CultureInfo.InvariantCulture, "SIZE{0}", i)].AsIntOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "FONTNAME{0}", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "ROTATION{0}", i)].AsIntOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "ITALIC{0}", i)].AsBool(),
                    p[string.Format(CultureInfo.InvariantCulture, "BOLD{0}", i)].AsBool(),
                    p[string.Format(CultureInfo.InvariantCulture, "UNDERLINE{0}", i)].AsBool()))
                .ToList();
            UseMbcs = p["USEMBCS"].AsBool();
            IsBoc = p["ISBOC"].AsBool();
            SheetStyle = p["SHEETSTYLE"].AsIntOrDefault();
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
            DisplayUnit = (Unit)p["DISPLAY_UNIT"].AsIntOrDefault();
            Comp = Enumerable.Range(0, p["COMPCOUNT"].AsIntOrDefault())
                .Select(i => (
                    p[string.Format(CultureInfo.InvariantCulture, "LIBREF{0}", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "COMPDESCR{0}", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "PARTCOUNT{0}", i)].AsIntOrDefault()))
                .ToList();
        }
        
        public void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            p.Add("HEADER", Header);
            p.Add("WEIGHT", Weight);
            p.Add("MINORVERSION", MinorVersion);
            p.Add("FONTIDCOUNT", FontId.Count);
            for (var i = 0; i < FontId.Count; i++)
            {
                p.Add(string.Format(CultureInfo.InvariantCulture, "SIZE{0}", i+1), FontId[i].Size);
                p.Add(string.Format(CultureInfo.InvariantCulture, "FONTNAME{0}", i+1), FontId[i].FontName);
                p.Add(string.Format(CultureInfo.InvariantCulture, "ROTATION{0}", i+1), FontId[i].Rotation);
                p.Add(string.Format(CultureInfo.InvariantCulture, "ITALIC{0}", i+1), FontId[i].Italic);
                p.Add(string.Format(CultureInfo.InvariantCulture, "BOLD{0}", i+1), FontId[i].Bold);
                p.Add(string.Format(CultureInfo.InvariantCulture, "UNDERLINE{0}", i + 1), FontId[i].Underline);
            }
            p.Add("USEMBCS", UseMbcs);
            p.Add("ISBOC", IsBoc);
            p.Add("SHEETSTYLE", SheetStyle);
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
            p.Add("DISPLAY_UNIT", (int)DisplayUnit);
            p.Add("COMPCOUNT", Comp.Count);
            for (var i = 0; i < Comp.Count; i++)
            {
                p.Add(string.Format(CultureInfo.InvariantCulture, "LIBREF{0}", i), Comp[i].LibRef);
                p.Add(string.Format(CultureInfo.InvariantCulture, "COMPDESCR{0}", i), Comp[i].CompDescr);
                p.Add(string.Format(CultureInfo.InvariantCulture, "PARTCOUNT{0}", i), Comp[i].PartCount);
            }
        }
    }
}
