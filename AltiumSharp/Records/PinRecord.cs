using System;
using System.Drawing;
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
    public enum PinOptions
    {
        Rotated = 0x01,
        Flipped = 0x02,
        Hide = 0x04,
        DisplayNameVisible = 0x08,
        DesignatorVisible = 0x10,
        GraphicallyLocked = 0x40,
    }

    public class PinRecord : SchPrimitive
    {
        public PinSymbol SymbolInnerEdge { get; internal set; }
        public PinSymbol SymbolOuterEdge { get; internal set; }
        public PinSymbol SymbolInside { get; internal set; }
        public PinSymbol SymbolOutside { get; internal set; }
        public LineWidth SymbolLineWidth { get; internal set; }
        public string Description { get; internal set; }
        public PinElectricalType Electrical { get; internal set; }
        public int PinConglomerate { get; internal set; }
        public PinOptions Flags { get; internal set; }
        public Coord PinLength { get; internal set; }
        public CoordPoint Location { get; internal set; }
        public Color Color { get; internal set; }
        public string Name { get; internal set; }
        public string Designator { get; internal set; }

        public CoordPoint GetCorner()
        {
            if (Flags.HasFlag(PinOptions.Rotated))
            {
                if (Flags.HasFlag(PinOptions.Flipped))
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
                if (Flags.HasFlag(PinOptions.Flipped))
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

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            SymbolInnerEdge = (PinSymbol)p["SYMBOL_INNEREDGE"].AsIntOrDefault();
            SymbolOuterEdge = (PinSymbol)p["SYMBOL_OUTEREDGE"].AsIntOrDefault();
            SymbolInside = (PinSymbol)p["SYMBOL_INSIDE"].AsIntOrDefault();
            SymbolOutside = (PinSymbol)p["SYMBOL_OUTSIDE"].AsIntOrDefault();
            Description = p["DESCRIPTION"].AsStringOrDefault();
            Electrical = (PinElectricalType)p["ELECTRICAL"].AsIntOrDefault();
            Flags = (PinOptions)p["FLAGS"].AsIntOrDefault(); // TODO: check this
            PinConglomerate = p["PINCONGLOMERATE"].AsIntOrDefault();
            PinLength = p["PINLENGTH"].AsIntOrDefault();
            Location = new CoordPoint(
                Utils.DxpFracToCoord(p["LOCATION.X"].AsIntOrDefault(), p["LOCATION.X_FRAC"].AsIntOrDefault()),
                Utils.DxpFracToCoord(p["LOCATION.Y"].AsIntOrDefault(), p["LOCATION.Y_FRAC"].AsIntOrDefault()));
            Color = p["COLOR"].AsColorOrDefault();
            Name = p["NAME"].AsStringOrDefault();
            Designator = p["DESIGNATOR"].AsStringOrDefault();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("SYMBOL_INNEREDGE", (int)SymbolInnerEdge);
            p.Add("SYMBOL_OUTEREDGE", (int)SymbolOuterEdge);
            p.Add("SYMBOL_INSIDE", (int)SymbolInside);
            p.Add("SYMBOL_OUTSIDE", (int)SymbolOutside);
            p.Add("DESCRIPTION", Description);
            p.Add("ELECTRICAL", (int)Electrical);
            p.Add("FLAGS", (int)Flags); // TODO: check this
            p.Add("PINCONGLOMERATE", PinConglomerate);
            p.Add("PINLENGTH", PinLength);
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
            p.Add("COLOR", Color);
            p.Add("NAME", Name);
            p.Add("DESIGNATOR", Designator);
        }

    }
}
 