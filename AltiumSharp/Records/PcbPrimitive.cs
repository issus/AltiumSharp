using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public enum PcbPrimitiveType
    {
        Arc = 1,
        Pad,
        Unknown3,
        Track,
        TextString,
        Fill,
        PolyRegion = 11,
        Model
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

        public PcbPrimitiveType Type { get; internal set; }

        public Layer Layer { get; internal set; }

        public ushort Flags { get; internal set; }

        public bool IsLocked => (Flags & 0x0004) != 0x0004; // TODO: add constant

        public override CoordRect CalculateBounds() => CoordRect.Empty;
    }
}
