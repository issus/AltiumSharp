using System;
using System.Collections.Generic;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
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
        None = 0x00,
        Rotated = 0x01,
        Flipped = 0x02,
        Hide = 0x04,
        DisplayNameVisible = 0x08,
        DesignatorVisible = 0x10,
        Unknown = 0x20,
        GraphicallyLocked = 0x40,
    }

    public class SchPin : SchGraphicalObject
    {
        public override int Record => 2;
        public PinSymbol SymbolInnerEdge { get; set; }
        public PinSymbol SymbolOuterEdge { get; set; }
        public PinSymbol SymbolInside { get; set; }
        public PinSymbol SymbolOutside { get; set; }
        public LineWidth SymbolLineWidth { get; set; }
        public string Description { get; set; }
        public int FormalType { get; set; }
        public PinElectricalType Electrical { get; set; }
        public PinConglomerateFlags PinConglomerate { get; set; }
        public Coord PinLength { get; set; }
        public string Name { get; set; }
        public string Designator { get; set; }
        public string SwapIdGroup { get; set; }
        public int SwapIdPart { get; set; }
        public string SwapIdSequence { get; set; }
        public string HiddenNetName { get; set; }
        public string DefaultValue { get; set; }
        public double PinPropagationDelay { get; set; }
        public string UniqueId { get; set; }

        public override bool IsVisible => base.IsVisible && !PinConglomerate.HasFlag(PinConglomerateFlags.Hide);

        public bool IsNameVisible
        {
            get => PinConglomerate.HasFlag(PinConglomerateFlags.DisplayNameVisible);
            set => PinConglomerate = PinConglomerate.WithFlag(PinConglomerateFlags.DisplayNameVisible, value);
        }

        public bool IsDesignatorVisible
        {
            get => PinConglomerate.HasFlag(PinConglomerateFlags.DesignatorVisible);
            set => PinConglomerate = PinConglomerate.WithFlag(PinConglomerateFlags.DesignatorVisible, value);
        }

        public TextOrientations Orientation
        {
            get => TextOrientations.None
                .WithFlag(TextOrientations.Rotated, PinConglomerate.HasFlag(PinConglomerateFlags.Rotated))
                .WithFlag(TextOrientations.Flipped, PinConglomerate.HasFlag(PinConglomerateFlags.Flipped));
            set => PinConglomerate = PinConglomerate
                .WithFlag(PinConglomerateFlags.Rotated, value.HasFlag(TextOrientations.Rotated))
                .WithFlag(PinConglomerateFlags.Flipped, value.HasFlag(TextOrientations.Flipped));
        }

        public SchPin() : base()
        {
            Electrical = PinElectricalType.Passive;
            PinConglomerate =
                PinConglomerateFlags.DisplayNameVisible |
                PinConglomerateFlags.DesignatorVisible |
                PinConglomerateFlags.Unknown;
            PinLength = Utils.DxpFracToCoord(30, 0);
            Designator = null;
            Name = null;
        }

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
            PinPropagationDelay = p["PINPROPAGATIONDELAY"].AsDoubleOrDefault();
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
                p.Add("PINLENGTH", n);
                p.Add("PINLENGTH_FRAC", f);
            }
            p.MoveKeys("LOCATION.X");
            p.Add("NAME", Name);
            p.Add("DESIGNATOR", Designator);
            p.Add("SWAPIDPART", SwapIdPart);
            p.Add("PINPROPAGATIONDELAY", PinPropagationDelay);
        }

        protected override bool DoAdd(SchPrimitive primitive)
        {
            if (primitive == null) return false;

            if (primitive is SchParameter parameter)
            {
                if (parameter.Name == "PinUniqueId")
                {
                    UniqueId = parameter.Text;
                    return false;
                }
                else if (parameter.Name == "HiddenNetName")
                {
                    HiddenNetName = parameter.Text;
                    return false;
                }
            }
            return true;
        }

        protected override IEnumerable<SchPrimitive> DoGetParameters()
        {
            if (!string.IsNullOrEmpty(HiddenNetName))
            {
                yield return new SchParameter { Name = "HiddenNetName", Text = HiddenNetName };
            }

            if (!string.IsNullOrEmpty(UniqueId))
            {
                yield return new SchParameter {Name = "PinUniqueId", Text = UniqueId, Location = Location};
            }
        }
    }
}
