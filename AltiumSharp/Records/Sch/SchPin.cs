using System;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public enum PinElectricalType
    {
        Input = 0, InputOutput, Output, OpenCollector, Passive, HiZ, OpenEmitter, Power
    }

    public enum PinSymbol
    {
        None = 0,
        Dot = 1,
        RightLeftSignalFlow = 2,
        Clock = 3,
        ActiveLowInput = 4,
        AnalogSignalIn = 5,
        NotLogicConnection = 6,
        PostponedOutput = 8,
        OpenCollector = 9,
        HiZ = 10,
        HighCurrent = 11,
        Pulse = 12,
        Schmitt = 13,
        ActiveLowOutput = 17,
        OpenCollectorPullUp = 22,
        OpenEmitter = 23,
        OpenEmitterPullUp = 24,
        DigitalSignalIn = 25,
        ShiftLeft = 30,
        OpenOutput = 32,
        LeftRightSignalFlow = 33,
        BidirectionalSignalFlow = 34,
    }

    [Flags]
    public enum PinConglomerateFlags
    {
        Rotated = 0x01,
        Flipped = 0x02,
        Hide = 0x04,
        DisplayNameVisible = 0x08,
        DesignatorVisible = 0x10,
        GraphicallyLocked = 0x40,
    }

    public class SchPin : SchGraphicalObject
    {
        public override int Record => 2;
        public PinSymbol SymbolInnerEdge { get; internal set; }
        public PinSymbol SymbolOuterEdge { get; internal set; }
        public PinSymbol SymbolInside { get; internal set; }
        public PinSymbol SymbolOutside { get; internal set; }
        public LineWidth SymbolLineWidth { get; internal set; }
        public string Description { get; internal set; }
        public int FormalType { get; internal set; }
        public PinElectricalType Electrical { get; internal set; }
        public PinConglomerateFlags PinConglomerate { get; internal set; }
        public Coord PinLength { get; internal set; }
        public string Name { get; internal set; }
        public string Designator { get; internal set; }
        public int SwapIdPart { get; internal set; }
        public string UniqueId { get; internal set; }

        public CoordPoint GetCorner()
        {
            if (PinConglomerate.HasFlag(PinConglomerateFlags.Rotated))
            {
                if (PinConglomerate.HasFlag(PinConglomerateFlags.Flipped))
                {
                    return new CoordPoint(Location.X, Location.Y - PinLength);
                }
                else
                {
                    return new CoordPoint(Location.X, Location.Y + PinLength);
                }
            }
            else
            {
                if (PinConglomerate.HasFlag(PinConglomerateFlags.Flipped))
                {
                    return new CoordPoint(Location.X - PinLength, Location.Y);
                }
                else
                {
                    return new CoordPoint(Location.X + PinLength, Location.Y);
                }
            }
        }

        public override CoordRect CalculateBounds() =>
            new CoordRect(Location, GetCorner());

        public override bool IsVisible => base.IsVisible && !PinConglomerate.HasFlag(PinConglomerateFlags.Hide);

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            SymbolInnerEdge = (PinSymbol)p["SYMBOL_INNEREDGE"].AsIntOrDefault();
            SymbolOuterEdge = (PinSymbol)p["SYMBOL_OUTEREDGE"].AsIntOrDefault();
            SymbolInside = (PinSymbol)p["SYMBOL_INSIDE"].AsIntOrDefault();
            SymbolOutside = (PinSymbol)p["SYMBOL_OUTSIDE"].AsIntOrDefault();
            SymbolLineWidth = p["SYMBOL_LINEWIDTH"].AsEnumOrDefault<LineWidth>();
            Description = p["DESCRIPTION"].AsStringOrDefault();
            FormalType = p["FORMALTYPE"].AsIntOrDefault();
            Electrical = p["ELECTRICAL"].AsEnumOrDefault<PinElectricalType>();
            PinConglomerate = (PinConglomerateFlags)p["PINCONGLOMERATE"].AsIntOrDefault(); 
            PinLength = Utils.DxpFracToCoord(p["PINLENGTH"].AsIntOrDefault(), p["PINLENGTH_FRAC"].AsIntOrDefault());
            Name = p["NAME"].AsStringOrDefault();
            Designator = p["DESIGNATOR"].AsStringOrDefault();
            SwapIdPart = p["SWAPIDPART"].AsIntOrDefault();
            UniqueId = p["UNIQUEID"].AsStringOrDefault();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.SetBookmark();
            p.Add("SYMBOL_INNEREDGE", SymbolInnerEdge);
            p.Add("SYMBOL_OUTEREDGE", SymbolOuterEdge);
            p.Add("SYMBOL_INSIDE", (int)SymbolInside);
            p.Add("SYMBOL_OUTSIDE", (int)SymbolOutside);
            p.Add("SYMBOL_LINEWIDTH", SymbolLineWidth);
            p.Add("DESCRIPTION", Description);
            p.Add("FORMALTYPE", FormalType);
            p.Add("ELECTRICAL", Electrical);
            p.Add("PINCONGLOMERATE", (int)PinConglomerate);
            {
                var (n, f) = Utils.CoordToDxpFrac(PinLength);
                if (n != 0 || f != 0) p.Add("PINLENGTH", n);
                if (f != 0) p.Add("PINLENGTH" + "_FRAC", f);
            }
            p.MoveKeys("LOCATION.X");
            p.Add("NAME", Name);
            p.Add("DESIGNATOR", Designator);
            p.Add("SWAPIDPART", SwapIdPart);
            p.Add("UNIQUEID", UniqueId);
        }

    }
}
 