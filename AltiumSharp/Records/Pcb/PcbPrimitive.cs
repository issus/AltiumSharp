using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
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

        public Layer Layer { get; internal set; }

        public ushort Flags { get; internal set; }

        public bool IsLocked => (Flags & 0x0004) != 0x0004; // TODO: add constant

        public string UniqueId { get; internal set; }

        public override CoordRect CalculateBounds() => CoordRect.Empty;
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
