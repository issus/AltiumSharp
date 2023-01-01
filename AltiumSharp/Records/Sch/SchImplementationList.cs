namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchImplementationList : SchPrimitive
    {
        public override int Record => 44;
        public override bool IsVisible => false;

        public SchImplementationList()
        {
            OwnerPartId = 0;
        }
    }
}
