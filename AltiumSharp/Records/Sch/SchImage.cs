using System;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchImage : SchRectangle
    {
        public override int Record => 30;
        public bool KeepAspect { get; internal set; }
        public bool EmbedImage { get; internal set; }
        public string Filename { get; internal set; }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            KeepAspect = p["KEEPASPECT"].AsBool();
            EmbedImage = p["EMBEDIMAGE"].AsBool();
            Filename = p["FILENAME"].AsStringOrDefault();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("KEEPASPECT", KeepAspect);
            p.Add("EMBEDIMAGE", EmbedImage);
            p.Add("FILENAME", Filename);
        }
    }
}
