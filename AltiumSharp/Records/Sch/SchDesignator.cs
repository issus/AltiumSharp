namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchDesignator : SchParameter
    {
        public override int Record => 34;
        public override bool IsVisible =>
            base.IsVisible && OwnerIndex > 0;

        public SchDesignator() : base()
        {
            ReadOnlyState = 1;
        }
    }
}
