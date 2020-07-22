using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public enum PcbPrimitiveObjectId
    {
        NoObject,
        Arc = 1,
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

    public class PcbPrimitive : Primitive
    {
        public virtual PcbPrimitiveDisplayInfo GetDisplayInfo() => new PcbPrimitiveDisplayInfo();

        public PcbPrimitiveObjectId ObjectId { get; internal set; }

        public Layer Layer { get; internal set; }

        public ushort Flags { get; internal set; }

        public bool IsLocked => (Flags & 0x0004) != 0x0004; // TODO: add constant

        public string UniqueId { get; internal set; }

        public override CoordRect CalculateBounds() => CoordRect.Empty;
    }
}
