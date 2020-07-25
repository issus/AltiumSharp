using System.Linq;
using AltiumSharp.Records;

namespace AltiumSharp
{
    public class SchDoc : SchData<SheetRecord, SchPrimitive>
    {
        public override SheetRecord Header => Items.OfType<SheetRecord>().SingleOrDefault();

        public SchDoc() : base()
        {

        }
    }
}
