using System;
using System.Linq;
using AltiumSharp.BasicTypes;
using AltiumSharp.Records;

namespace AltiumSharp
{
    public class SchLib : SchData<SchLibHeader, SchComponent>
    {
        public override SchLibHeader Header { get; }

        public SchLib() : base()
        {
            Header = new SchLibHeader(Items);
        }

        private void AddComponent(SchComponent component)
        {
            if (string.IsNullOrEmpty(component.LibReference))
            {
                component.LibReference = $"Component_{Items.Count+1}";
            }

            var designator = component.GetPrimitivesOfType<SchDesignator>(false).Where(r => r.Name == "Designator").SingleOrDefault();
            if (designator == null)
            {
                component.Add(new SchDesignator {
                    Name = "Designator",
                    ReadOnlyState = 1,
                    Location = new CoordPoint(-5, 5)
                });
            }

            var comment = component.GetPrimitivesOfType<SchParameter>(false).Where(r => r.Name == "Comment").SingleOrDefault();
            if (designator == null)
            {
                component.Add(new SchParameter {
                    Name = "Comment",
                    Location = new CoordPoint(-5, -15)
                });
            }

            Items.Add(component);
        }
    }
}
