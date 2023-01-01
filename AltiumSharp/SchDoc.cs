using System.Collections.Generic;
using System.Linq;
using OriginalCircuit.AltiumSharp.BasicTypes;
using OriginalCircuit.AltiumSharp.Records;

namespace OriginalCircuit.AltiumSharp
{
    public class SchDoc : SchData<SchSheetHeader, SchPrimitive>
    {
        public override SchSheetHeader Header => Items.OfType<SchSheetHeader>().SingleOrDefault();

        public SchDoc() : base()
        {

        }
    }
}
