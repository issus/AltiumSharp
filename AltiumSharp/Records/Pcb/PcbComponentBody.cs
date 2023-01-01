using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class PcbComponentBody : PcbPrimitive
    {
        public override PcbPrimitiveObjectId ObjectId => PcbPrimitiveObjectId.ComponentBody;
        public List<CoordPoint> Outline { get; } = new List<CoordPoint>();

        public string V7Layer { get; set; }
        public string Name { get; set; }
        public int Kind { get; set; }
        public int SubPolyIndex { get; set; }
        public int UnionIndex { get; set; }
        public Coord ArcResolution { get; set; }
        public bool IsShapeBased { get; set; }
        public Coord StandOffHeight { get; set; }
        public Coord OverallHeight { get; set; }
        public int BodyProjection { get; set; }
        public Color BodyColor3D { get; set; }
        public double BodyOpacity3D { get; set; }
        public string Identifier { get; set; }
        public string Texture { get; set; }
        public CoordPoint TextureCenter { get; set; }
        public CoordPoint TextureSize { get; set; }
        public double TextureRotation { get; set; }
        public string ModelId { get; set; }
        public int ModelChecksum { get; set; }
        public bool ModelEmbed { get; set; }
        public CoordPoint Model2DLocation { get; set; }
        public double Model2DRotation { get; set; }
        public double Model3DRotX { get; set; }
        public double Model3DRotY { get; set; }
        public double Model3DRotZ { get; set; }
        public Coord Model3DDz { get; set; }
        public int ModelSnapCount { get; set; }
        public int ModelType { get; set; }
        
        public string StepModel { get; set; }

        public PcbComponentBody()
        {
            V7Layer = "MECHANICAL1";
            SubPolyIndex = -1;
            BodyColor3D = ColorTranslator.FromWin32(14737632);
            BodyOpacity3D = 1.0;
            ModelEmbed = true;
            ModelType = 1;
        }

        public override CoordRect CalculateBounds()
        {
            return new CoordRect(
                new CoordPoint(Outline.Min(p => p.X), Outline.Min(p => p.Y)),
                new CoordPoint(Outline.Max(p => p.X), Outline.Max(p => p.Y)));
        }

        public void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            V7Layer = p["V7_LAYER"].AsStringOrDefault();
            Name = p["NAME"].AsStringOrDefault();
            Kind = p["KIND"].AsIntOrDefault();
            SubPolyIndex = p["SUBPOLYINDEX"].AsIntOrDefault();
            UnionIndex = p["UNIONINDEX"].AsIntOrDefault();
            ArcResolution = p["ARCRESOLUTION"].AsCoord();
            IsShapeBased = p["ISSHAPEBASED"].AsBool();
            StandOffHeight = p["STANDOFFHEIGHT"].AsCoord();
            OverallHeight = p["OVERALLHEIGHT"].AsCoord();
            BodyProjection = p["BODYPROJECTION"].AsIntOrDefault();
            ArcResolution = p["ARCRESOLUTION"].AsCoord();
            BodyColor3D = p["BODYCOLOR3D"].AsColorOrDefault();
            BodyOpacity3D = p["BODYOPACITY3D"].AsDoubleOrDefault();
            Identifier = new string(p["IDENTIFIER"].AsIntList(',').Select(v => (char)v).ToArray());
            Texture = p["TEXTURE"].AsStringOrDefault();
            TextureCenter = new CoordPoint(p["TEXTURECENTERX"].AsCoord(), p["TEXTURECENTERY"].AsCoord());
            TextureSize = new CoordPoint(p["TEXTURESIZEX"].AsCoord(), p["TEXTURESIZEY"].AsCoord());
            TextureRotation = p["TEXTUREROTATION"].AsDouble();
            ModelId = p["MODELID"].AsStringOrDefault();
            ModelChecksum = p["MODEL.CHECKSUM"].AsIntOrDefault();
            ModelEmbed = p["MODEL.EMBED"].AsBool();
            Model2DLocation = new CoordPoint(p["MODEL.2D.X"].AsCoord(), p["MODEL.2D.Y"].AsCoord());
            Model2DRotation = p["MODEL.2D.ROTATION"].AsDoubleOrDefault();
            Model3DRotX = p["MODEL.3D.ROTX"].AsDoubleOrDefault();
            Model3DRotY = p["MODEL.3D.ROTY"].AsDoubleOrDefault();
            Model3DRotZ = p["MODEL.3D.ROTZ"].AsDoubleOrDefault();
            Model3DDz = p["MODEL.3D.DZ"].AsCoord();
            ModelSnapCount = p["MODEL.SNAPCOUNT"].AsIntOrDefault();
            ModelType = p["MODEL.MODELTYPE"].AsIntOrDefault();
        }

        public void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            p.UseLongBooleans = true;

            p.Add("V7_LAYER", V7Layer);
            p.Add("NAME", Name);
            p.Add("KIND", Kind);
            p.Add("SUBPOLYINDEX", SubPolyIndex);
            p.Add("UNIONINDEX", UnionIndex);
            p.Add("ARCRESOLUTION", ArcResolution);
            p.Add("ISSHAPEBASED", IsShapeBased);
            p.Add("STANDOFFHEIGHT", StandOffHeight);
            p.Add("OVERALLHEIGHT", OverallHeight);
            p.Add("BODYPROJECTION", BodyProjection);
            p.Add("ARCRESOLUTION", ArcResolution);
            p.Add("BODYCOLOR3D", BodyColor3D);
            p.Add("BODYOPACITY3D", BodyOpacity3D);
            p.Add("IDENTIFIER", string.Join(",", Identifier?.Select(c => (int)c) ?? Enumerable.Empty<int>()));
            p.Add("TEXTURE", Texture);
            p.Add("TEXTURECENTERX", TextureCenter.X);
            p.Add("TEXTURECENTERY", TextureCenter.Y);
            p.Add("TEXTURESIZEX", TextureSize.X);
            p.Add("TEXTURESIZEY", TextureSize.Y);
            p.Add("TEXTUREROTATION", TextureRotation);
            p.Add("MODELID", ModelId);
            p.Add("MODEL.CHECKSUM", ModelChecksum);
            p.Add("MODEL.EMBED", ModelEmbed);
            p.Add("MODEL.2D.X", Model2DLocation.X);
            p.Add("MODEL.2D.Y", Model2DLocation.Y);
            p.Add("MODEL.2D.ROTATION", Model2DRotation);
            p.Add("MODEL.3D.ROTX", Model3DRotX);
            p.Add("MODEL.3D.ROTY", Model3DRotY);
            p.Add("MODEL.3D.ROTZ", Model3DRotZ);
            p.Add("MODEL.3D.DZ", Model3DDz);
            p.Add("MODEL.SNAPCOUNT", ModelSnapCount);
            p.Add("MODEL.MODELTYPE", ModelType);
        }

        public ParameterCollection ExportToParameters()
        {
            var parameters = new ParameterCollection();
            ExportToParameters(parameters);
            return parameters;
        }
    }
}
