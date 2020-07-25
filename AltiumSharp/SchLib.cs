using System;
using System.Linq;
using AltiumSharp.Records;

namespace AltiumSharp
{
    public class SchLib : SchData<SchLibHeader, SchComponent>
    {
        public override SchLibHeader Header { get; } = new SchLibHeader();

        public SchLib() : base()
        {

        }

        private void AddComponent(SchComponent component)
        {
            if (string.IsNullOrEmpty(component.LibReference))
            {
                component.LibReference = $"Component_{Items.Count+1}";
            }

            var designator = component.GetPrimitivesOfType<DesignatorLabelRecord>(false).Where(r => r.Name == "Designator").SingleOrDefault();
            if (designator == null)
            {
                component.Add(new DesignatorLabelRecord { Name = "Designator", ReadOnlyState = 1 });
            }

            var comment = component.GetPrimitivesOfType<SchParameter>(false).Where(r => r.Name == "Comment").SingleOrDefault();
            if (designator == null)
            {
                component.Add(new SchParameter { Name = "Comment" });
            }

            Items.Add(component);
        }
    }
}
