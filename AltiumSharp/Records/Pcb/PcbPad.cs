using System;
using System.Collections.Generic;
using System.Linq;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public enum PcbPadTemplate { Tht, SmtTop, SmtBottom }

    public enum PcbPadShape { Round = 1, Rectangular, Octogonal, RoundedRectangle = 9
    };

    public enum PcbPadHoleShape { Round = 0, Square, Slot };

    public enum PcbPadPart { TopLayer, BottomLayer, TopSolder, BottomSolder, Hole }

    public class PcbPad : PcbPrimitive
    {
        public override PcbPrimitiveDisplayInfo GetDisplayInfo() =>
            new PcbPrimitiveDisplayInfo(Designator, Math.Max(SizeTop.X, SizeBottom.X), Math.Max(SizeTop.Y, SizeBottom.Y));

        public override PcbPrimitiveObjectId ObjectId => PcbPrimitiveObjectId.Pad;
        public string Designator { get; set; }
        public CoordPoint Location { get; set; }
        public double Rotation { get; set; }
        public bool IsPlated { get; set; }
        public int JumperId { get; set; }

        private PcbStackMode _stackMode;
        public PcbStackMode StackMode
        {
            get => _stackMode;
            set
            {
                _stackMode = value;
                switch (value)
                {
                    case PcbStackMode.Simple:
                        SizeMiddle = SizeTop;
                        SizeBottom = SizeTop;
                        ShapeMiddle = ShapeTop;
                        ShapeBottom = ShapeTop;
                        CornerRadiusMid = CornerRadiusTop;
                        CornerRadiusBot = CornerRadiusTop;
                        break;
                    case PcbStackMode.TopMiddleBottom:
                        SizeMiddle = SizeMiddle; // overrides all middle values
                        ShapeMiddle = ShapeMiddle;
                        CornerRadiusMid = CornerRadiusMid;
                        break;
                    default:
                        break;
                }
            }
        }
        
        public CoordPoint SizeTop
        {
            get => SizeLayers[0];
            set => SizeLayers[0] = value;
        }
        
        public PcbPadShape ShapeTop
        {
            get => ShapeLayers[0];
            set => ShapeLayers[0] = value;
        }
        
        public byte CornerRadiusTop
        {
            get => ShapeTop == PcbPadShape.RoundedRectangle ? CornerRadiusPercentage[0] : (byte)0;
            set => CornerRadiusPercentage[0] = value;
        }
        
        public CoordPoint SizeMiddle
        {
            get => SizeLayers[1];
            set
            {
                for (int i = 1; i < 31; ++i) SizeLayers[i] = value;
            }
        }
        
        public PcbPadShape ShapeMiddle
        {
            get => ShapeLayers[1];
            set
            {
                for (int i = 1; i < 31; ++i) ShapeLayers[i] = value;
            }
        }
        
        public byte CornerRadiusMid
        {
            get => ShapeMiddle == PcbPadShape.RoundedRectangle ? CornerRadiusPercentage[1] : (byte)0;
            set
            {
                for (int i = 1; i < 31; ++i) CornerRadiusPercentage[i] = value;
            }
        }
        
        public CoordPoint SizeBottom
        {
            get => SizeLayers[31];
            set => SizeLayers[31] = value;
        }
        
        public PcbPadShape ShapeBottom
        {
            get => ShapeLayers[31];
            set => ShapeLayers[31] = value;
        }
        
        public byte CornerRadiusBot
        {
            get => ShapeBottom == PcbPadShape.RoundedRectangle ? CornerRadiusPercentage[31] : (byte)0;
            set => CornerRadiusPercentage[31] = value;
        }
        
        public CoordPoint OffsetFromHoleCenter
        {
            get => OffsetsFromHoleCenter[0];
            set => OffsetsFromHoleCenter[0] = value;
        }

        public Coord HoleSize { get; set; }
        public PcbPadHoleShape HoleShape { get; set; }
        public double HoleRotation { get; set; }
        public Coord HoleSlotLength { get; set; }
        public bool PasteMaskExpansionManual { get; set; }
        public Coord PasteMaskExpansion { get; set; }
        public bool SolderMaskExpansionManual { get; set; }
        public Coord SolderMaskExpansion { get; set; }
        public IList<CoordPoint> SizeMiddleLayers => SizeLayers.Skip(2).Take(SizeLayers.Count - 3).ToArray();
        public IList<PcbPadShape> ShapeMiddleLayers => ShapeLayers.Skip(2).Take(ShapeLayers.Count - 3).ToArray();
        public IList<CoordPoint> OffsetsFromHoleCenter { get; } = new CoordPoint[32];
        public IList<CoordPoint> SizeLayers { get; } = new CoordPoint[32];
        public IList<PcbPadShape> ShapeLayers { get; } = new PcbPadShape[32];
        public IList<byte> CornerRadiusPercentage { get; } = new byte[32];
        public bool HasHole => Layer == LayerMetadata.Get("MultiLayer").Id;
        
        public bool SolderMaskTentingTop
        {
            get => (Flags & 32) == 32;
            set => Flags |= 32;
        }

        public bool SolderMaskTentingBottom
        {
            get => (Flags & 64) == 64;
            set => Flags |= 64;
        }

        internal bool HasRoundedRectangles =>
            ShapeLayers.Any(s => s == PcbPadShape.RoundedRectangle);

        internal bool NeedsFullStackData =>
            (StackMode == PcbStackMode.FullStack) || HasRoundedRectangles ||
            OffsetsFromHoleCenter.Any(o => o != CoordPoint.Zero);

        public PcbPad(PcbPadTemplate template = PcbPadTemplate.Tht)
        {
            switch (template)
            {
                case PcbPadTemplate.Tht:
                    Layer = LayerMetadata.Get("MultiLayer").Id;
                    break;
                case PcbPadTemplate.SmtTop:
                    Layer = LayerMetadata.Get("TopLayer").Id;
                    break;
                case PcbPadTemplate.SmtBottom:
                    Layer = LayerMetadata.Get("BottomLayer").Id;
                    break;
            }
            IsPlated = true;
            StackMode = PcbStackMode.Simple;

            var defaultSize = CoordPoint.FromMils(60, 60);
            var defaultShape = PcbPadShape.Round;
            byte defaultRadiusPercentage = 50;
            SizeTop = defaultSize;
            SizeMiddle = defaultSize;
            SizeBottom = defaultSize;
            ShapeTop = defaultShape;
            ShapeMiddle = defaultShape;
            ShapeBottom = defaultShape;
            HoleSize = Coord.FromMils(30);
            HoleShape = PcbPadHoleShape.Round;
            for (int i = 0; i < SizeMiddleLayers.Count; ++i) SizeMiddleLayers[i] = defaultSize;
            for (int i = 0; i < ShapeLayers.Count; ++i) ShapeLayers[i] = defaultShape;
            for (int i = 0; i < CornerRadiusPercentage.Count; ++i) CornerRadiusPercentage[i] = defaultRadiusPercentage;
        }

        internal CoordRect CalculatePartRect(PcbPadPart part, bool useAbsolutePosition)
        {
            var solderMaskExpansion = SolderMaskExpansionManual ? SolderMaskExpansion : Utils.MilsToCoord(8);
            var solderMaskExpansionTop = SolderMaskTentingTop ? Utils.MilsToCoord(0) : solderMaskExpansion;
            var solderMaskExpansionBottom = SolderMaskTentingBottom ? Utils.MilsToCoord(0) : solderMaskExpansion;
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
                    width = SizeTop.X + solderMaskExpansionTop;
                    height = SizeTop.Y + solderMaskExpansionTop;
                    break;
                case PcbPadPart.BottomSolder:
                    width = SizeBottom.X + solderMaskExpansionBottom;
                    height = SizeBottom.Y + solderMaskExpansionBottom;
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
