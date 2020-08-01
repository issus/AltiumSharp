namespace AltiumSharp.Records
{
    public class SchDesignator : SchParameter
    {
        public override int Record => 34;

        public SchDesignator() : base()
        {
            ReadOnlyState = 1;
        }
    }
}
