using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
        public double PinPropagationDelay { get; internal set; }
        public string UniqueId { get; internal set; }

        public override bool IsVisible => base.IsVisible && !PinConglomerate.HasFlag(PinConglomerateFlags.Hide);

        public TextOrientations Orientation
        {
            get => TextOrientations.None
                .WithFlag(TextOrientations.Rotated, PinConglomerate.HasFlag(PinConglomerateFlags.Rotated))
                .WithFlag(TextOrientations.Flipped, PinConglomerate.HasFlag(PinConglomerateFlags.Flipped));
            set => PinConglomerate = PinConglomerate
                .WithFlag(PinConglomerateFlags.Rotated, value.HasFlag(TextOrientations.Rotated))
                .WithFlag(PinConglomerateFlags.Flipped, value.HasFlag(TextOrientations.Flipped));
        }

        private Regex _designatorParser = new Regex(@"^(.*?)(\d+)\s*$");

        public SchPin() : base()
        {
            Electrical = PinElectricalType.Passive;
            PinConglomerate =
                PinConglomerateFlags.DisplayNameVisible |
                PinConglomerateFlags.DesignatorVisible |
                PinConglomerateFlags.Unknown;
            PinLength = Utils.DxpFracToCoord(30, 0);
            UniqueId = Utils.GenerateUniqueId();
            Designator = GenerateDesignator();
            Name = Designator;
        }

        /// <summary>
        /// Generates a new designator by taking the last designator in lexicographical order
        /// and then incrementing any ending integer.
        /// </summary>
        /// <remarks>
        /// This mimicks the behavior of AD's context menu "Place > Pin", which works very differently
        /// from AD's Properties pin list "Add" button, and the context menu behavior was chosen as it
        /// seemed more intuitive.
        /// </remarks>
        private string GenerateDesignator()
        {
            var largestDesignator = (Owner as IContainer)?.GetPrimitivesOfType<SchPin>(false)
                .OrderBy(p => p.Designator ?? "")
                .LastOrDefault()?.Designator;
            if (largestDesignator != null)
            {
                return _designatorParser.Replace(largestDesignator, match =>
                        $"{match.Captures[1]}{int.Parse(match.Captures[2].Value, CultureInfo.InvariantCulture) + 1}");
            }
            else
            {
                return "1";
            }
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
                p.Add("PINLENGTH" + "_FRAC", f);
            }
            p.MoveKeys("LOCATION.X");
            p.Add("NAME", Name);
            p.Add("DESIGNATOR", Designator);
            p.Add("SWAPIDPART", SwapIdPart);
            p.Add("PINPROPAGATIONDELAY", PinPropagationDelay);
            p.Add("UNIQUEID", UniqueId);
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
            }
            return true;
        }

        protected override IEnumerable<SchPrimitive> DoGetParameters()
        {
            return new SchPrimitive[] {
                new SchParameter{ Name = "PinUniqueId", Text = UniqueId }
            };
        }
    }
}
