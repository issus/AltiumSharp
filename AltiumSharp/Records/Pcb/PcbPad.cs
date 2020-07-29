using System;
using System.Collections.Generic;
using System.Linq;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public enum PcbPadShape { Round = 1, Rectangular, Octogonal };

    public enum PcbPadHoleShape { Round = 0, Square, Slot };

    public enum PcbPadPart { TopLayer, BottomLayer, TopSolder, BottomSolder, Hole }

    public class PcbPad : PcbPrimitive
    {
        public override PcbPrimitiveDisplayInfo GetDisplayInfo() =>
            new PcbPrimitiveDisplayInfo(Designator, Math.Max(SizeTop.X, SizeBottom.X), Math.Max(SizeTop.Y, SizeBottom.Y));

        public override PcbPrimitiveObjectId ObjectId => PcbPrimitiveObjectId.Pad;
        public string Designator { get; internal set; }
        public string UnknownString { get; internal set; }
        public CoordPoint Location { get; internal set; }
        public double Rotation { get; internal set; }
        public CoordPoint SizeTop { get; internal set; }
        public PcbPadShape ShapeTop { get; internal set; }
        public byte CornerRadiusTop => CornerRadiusPercentage.FirstOrDefault();
        public CoordPoint SizeMiddle { get; internal set; }
        public PcbPadShape ShapeMiddle { get; internal set; }
        public byte CornerRadiusMid => CornerRadiusPercentage.ElementAtOrDefault(1);
        public CoordPoint SizeBottom { get; internal set; }
        public PcbPadShape ShapeBottom { get; internal set; }
        public byte CornerRadiusBot => CornerRadiusPercentage.LastOrDefault();
        public CoordPoint OffsetFromHoleCenter => OffsetsFromHoleCenter.FirstOrDefault();
        public Coord HoleSize { get; internal set; }
        public PcbPadHoleShape HoleShape { get; internal set; }
        public double HoleRotation { get; internal set; }
        public Coord HoleSlotLength { get; internal set; }
        public Layer ToLayer { get; internal set; }
        public Layer FromLayer { get; internal set; }
        public List<CoordPoint> SizeMiddleLayers { get; } = new List<CoordPoint>();
        public List<PcbPadShape> ShapeMiddleLayers { get; } = new List<PcbPadShape>();
        public List<CoordPoint> OffsetsFromHoleCenter { get; internal set; } = new List<CoordPoint>();
        public List<byte> CornerRadiusPercentage { get; internal set; } = new List<byte>();
        public bool HasHole => Layer == LayerMetadata.Get("MultiLayer").Id;

        internal CoordRect CalculatePartRect(PcbPadPart part, bool useAbsolutePosition)
        {
            Coord width, height;
            var offset = OffsetFromHoleCenter;
            switch (part)
            {
                case PcbPadPart.TopLayer:
                    width = SizeTop.X;
                    height = SizeTop.Y;
                    break;
                case PcbPadPart.BottomLayer:
                    width = SizeBottom.X;
                    height = SizeBottom.Y;
                    break;
                case PcbPadPart.TopSolder:
                    width = SizeTop.X + Utils.MilsToCoord(5); // TODO: get padding size
                    height = SizeTop.Y + Utils.MilsToCoord(5);
                    break;
                case PcbPadPart.BottomSolder:
                    width = SizeBottom.X + Utils.MilsToCoord(5);
                    height = SizeBottom.Y + Utils.MilsToCoord(5);
                    break;
                case PcbPadPart.Hole:
                    width = PcbPadHoleShape.Slot == HoleShape ? HoleSlotLength : HoleSize;
                    height = HoleSize;
                    offset = CoordPoint.Zero;
                    break;
                default:
                    return CoordRect.Empty;
            }
            Coord x = offset.X - width  / 2;
            Coord y = offset.Y - height / 2;
            if (useAbsolutePosition)
            {
                x += Location.X;
                y += Location.Y;
            }
            return new CoordRect(x, y, width, height);
        }

        internal CoordRect CalculatePartBounds(PcbPadPart part) =>
            CoordRect.FromRotatedRect(CalculatePartRect(part, true), Location, Rotation + (part == PcbPadPart.Hole ? HoleRotation : 0));

        public override CoordRect CalculateBounds()
        {
            var result = CalculatePartBounds(PcbPadPart.BottomSolder);
            result = CoordRect.Union(result, CalculatePartBounds(PcbPadPart.TopSolder));
            result = CoordRect.Union(result, CalculatePartBounds(PcbPadPart.BottomLayer));
            result = CoordRect.Union(result, CalculatePartBounds(PcbPadPart.TopLayer));
            if (HasHole)
            {
                result = CoordRect.Union(result, CalculatePartBounds(PcbPadPart.Hole));
            }
            return result;
        }
    }
}
