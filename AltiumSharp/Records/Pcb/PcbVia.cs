using System;
using System.Collections.Generic;
using System.Linq;
using OriginalCircuit.AltiumSharp.BasicTypes;
using OriginalCircuit.AltiumSharp.Records;

namespace OriginalCircuit.AltiumSharp
{
    public class PcbVia : PcbPrimitive
    {
        public override PcbPrimitiveDisplayInfo GetDisplayInfo() =>
            new PcbPrimitiveDisplayInfo($"{FromLayer} to {ToLayer}", Diameter, null);

        public override PcbPrimitiveObjectId ObjectId => PcbPrimitiveObjectId.Via;

        public CoordPoint Location { get; set; }
        public Coord HoleSize { get; set; }
        public Layer FromLayer { get; set; }
        public Layer ToLayer { get; set; }
        public Coord ThermalReliefAirGapWidth { get; set; }
        public int ThermalReliefConductors { get; set; }
        public Coord ThermalReliefConductorsWidth { get; set; }
        public bool SolderMaskExpansionManual { get; set; }
        public Coord SolderMaskExpansion { get; set; }
        public PcbStackMode DiameterStackMode { get; set; }
        public IList<Coord> Diameters { get; } = new Coord[32];
        
        public Coord Diameter
        {
            get => Diameters[Diameters.Count - 1];
            set => SetDiameterAll(value);
        }
        
        public Coord DiameterTop
        {
            get => DiameterStackMode == PcbStackMode.Simple ? Diameter : Diameters[0];
            set => SetDiameterTop(value);
        }
        
        public Coord DiameterMiddle
        {
            get => DiameterStackMode == PcbStackMode.Simple ? Diameter : Diameters[1];
            set => SetDiameterMiddle(value);
        }
        
        public Coord DiameterBottom
        {
            get => DiameterStackMode == PcbStackMode.Simple ? Diameter : Diameters[Diameters.Count - 1];
            set => SetDiameterBottom(value);
        }

        public PcbVia() : base()
        {
            Diameter = Coord.FromMils(50);
            HoleSize = Coord.FromMils(28);
            FromLayer = 1;
            ToLayer = 32;
            ThermalReliefAirGapWidth = Coord.FromMils(10);
            ThermalReliefConductors = 4;
            ThermalReliefConductorsWidth = Coord.FromMils(10);
            SolderMaskExpansionManual = false;
            SolderMaskExpansion = Coord.FromMils(4);
        }

        internal List<Layer> GetParts()
        {
            var result = new List<Layer>();
            if (ToLayer.Name == "BottomLayer" && !IsTentingBottom)
            {
                result.Add(LayerMetadata.Get("BottomSolder").Id);
            }
            if (FromLayer.Name == "TopLayer" && !IsTentingTop)
            {
                result.Add(LayerMetadata.Get("TopSolder").Id);
            }
            result.Add(Layer);
            result.Add(LayerMetadata.Get("ViaHoleLayer").Id);
            if (FromLayer.Name != "TopLayer" || ToLayer.Name != "BottomLayer")
            {
                result.Add(FromLayer);
                result.Add(ToLayer);
            }
            return result;
        }

        internal CoordRect CalculatePartRect(LayerMetadata metadata, bool useAbsolutePosition)
        {
            var solderMaskExpansion = SolderMaskExpansionManual ? SolderMaskExpansion : Utils.MilsToCoord(8);
            var solderMaskExpansionTop = IsTentingTop ? Utils.MilsToCoord(0) : solderMaskExpansion;
            var solderMaskExpansionBottom = IsTentingBottom ? Utils.MilsToCoord(0) : solderMaskExpansion;
            Coord diameter;
            if (metadata.Name == "TopSolder")
            {
                diameter = DiameterTop + solderMaskExpansionTop;
            }
            else if (metadata.Name == "BottomSolder")
            {
                diameter = DiameterBottom + solderMaskExpansionBottom;
            }
            else if (metadata.Name == "MultiLayer")
            {
                diameter = Diameter;
            }
            else
            {
                diameter = HoleSize;
            }

            Coord x = -diameter / 2;
            Coord y = -diameter / 2;
            if (useAbsolutePosition)
            {
                x += Location.X;
                y += Location.Y;
            }
            return new CoordRect(x, y, diameter, diameter);
        }

        public override CoordRect CalculateBounds()
        {
            var result = CoordRect.Empty;
            foreach (var p in GetParts())
            {
                result = CoordRect.Union(result, CalculatePartRect(p.Metadata, true));
            }
            return result;
        }

        private void SetDiameterAll(Coord value)
        {
            for (int i = 0; i < Diameters.Count; ++i)
            {
                Diameters[i] = value;
            }
        }

        private void SetDiameterTop(Coord value)
        {
            if (DiameterStackMode == PcbStackMode.Simple)
            {
                SetDiameterAll(value);
            }
            else
            {
                Diameters[0] = value;
            }
        }

        private void SetDiameterMiddle(Coord value)
        {
            if (DiameterStackMode == PcbStackMode.Simple)
            {
                SetDiameterAll(value);
            }
            else
            {
                for (int i = 1; i < Diameters.Count - 1; ++i)
                {
                    Diameters[i] = value;
                }
            }
        }

        private void SetDiameterBottom(Coord value)
        {
            if (DiameterStackMode == PcbStackMode.Simple)
            {
                SetDiameterAll(value);
            }
            else
            {
                Diameters[Diameters.Count - 1] = value;
            }
        }
    }
}