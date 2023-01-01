using System;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public enum PcbPrimitiveObjectId
    {
        None,
        Arc,
        Pad,
        Via,
        Track,
        Text,
        Fill,
        Region = 11,
        ComponentBody
    }

    [Flags]
    public enum PcbFlags
    {
        None = 0,
        Unknown2 = 2,
        Unlocked = 4,
        Unknown8 = 8,
        Unknown16 = 16,
        TentingTop = 32,
        TentingBottom = 64,
        FabricationTop = 128,
        FabricationBottom = 256,
        KeepOut = 512
    }

    public enum PcbStackMode
    {
        Simple,
        TopMiddleBottom,
        FullStack
    }

    public class PcbPrimitiveDisplayInfo
    {
        public string Name { get; }
        public Coord? SizeX { get; }
        public Coord? SizeY { get; }
        public PcbPrimitiveDisplayInfo() { }
        public PcbPrimitiveDisplayInfo(string name, Coord? sizeX, Coord? sizeY) =>
            (Name, SizeX, SizeY) = (name, sizeX, sizeY);
    }

    public abstract class PcbPrimitive : Primitive
    {
        public virtual PcbPrimitiveDisplayInfo GetDisplayInfo() => new PcbPrimitiveDisplayInfo();

        public abstract PcbPrimitiveObjectId ObjectId { get; }

        public Layer Layer { get; set; }

        public PcbFlags Flags { get; set; }

        public bool IsLocked
        {
            get => !Flags.HasFlag(PcbFlags.Unlocked);
            set => Flags = Flags.WithFlag(PcbFlags.Unlocked, !value);
        }

        public bool IsTentingTop
        {
            get => Flags.HasFlag(PcbFlags.TentingTop);
            set => Flags = Flags.WithFlag(PcbFlags.TentingTop, value);
        }

        public bool IsTentingBottom
        {
            get => Flags.HasFlag(PcbFlags.TentingBottom);
            set => Flags = Flags.WithFlag(PcbFlags.TentingBottom, value);
        }

        public bool IsKeepOut
        {
            get => Flags.HasFlag(PcbFlags.KeepOut);
            set => Flags = Flags.WithFlag(PcbFlags.KeepOut, value);
        }

        public bool IsFabricationTop
        {
            get => Flags.HasFlag(PcbFlags.FabricationTop);
            set => Flags = Flags.WithFlag(PcbFlags.FabricationTop, value);
        }

        public bool IsFabricationBottom
        {
            get => Flags.HasFlag(PcbFlags.FabricationBottom);
            set => Flags = Flags.WithFlag(PcbFlags.FabricationBottom, value);
        }

        public string UniqueId { get; set; }

        public override CoordRect CalculateBounds() => CoordRect.Empty;

        protected PcbPrimitive()
        {
            Layer = LayerMetadata.Get("TopLayer").Id;
            Flags = PcbFlags.Unlocked | PcbFlags.Unknown8;
        }
    }

    public class PcbUnknown : PcbPrimitive
    {
        private PcbPrimitiveObjectId _objectId;
        public override PcbPrimitiveObjectId ObjectId => _objectId;

        public PcbUnknown(PcbPrimitiveObjectId objectId)
        {
            _objectId = objectId;
        }
    }
}
