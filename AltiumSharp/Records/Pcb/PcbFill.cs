namespace OriginalCircuit.AltiumSharp.Records
{
    public class PcbFill : PcbRectangularPrimitive
    {
        public override PcbPrimitiveDisplayInfo GetDisplayInfo() =>
            new PcbPrimitiveDisplayInfo("", Width, Height);

        public override PcbPrimitiveObjectId ObjectId => PcbPrimitiveObjectId.Fill;
    }
}
