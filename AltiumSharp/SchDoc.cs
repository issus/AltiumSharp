using System.Collections.Generic;
using System.Linq;
using AltiumSharp.BasicTypes;
using AltiumSharp.Records;

namespace AltiumSharp
{
    public class SchDoc : SchData<SchSheetHeader, SchPrimitive>
    {
        public override SchSheetHeader Header => Items.OfType<SchSheetHeader>().SingleOrDefault();

        public SchDoc() : base()
        {

        }
    }
}
