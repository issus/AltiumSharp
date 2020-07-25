using AltiumSharp.Records;

namespace AltiumSharp
{
    public class SchLib : SchData<SchLibHeader, SchComponent>
    {
        public override SchLibHeader Header { get; } = new SchLibHeader();

        public SchLib() : base()
        {

        }
    }
}
