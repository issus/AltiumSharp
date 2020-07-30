using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class PcbLibHeader
    {
        public string Filename { get; internal set; }
        public string Kind => "Protel_Advanced_PCB_Library";
        public string Version { get; private set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public int V9MasterStackStyle { get; internal set; }
        public string V9MasterStackId { get; internal set; }
        public string V9MasterStackName { get; internal set; }
        public bool V9MasterStackShowTopDielectric { get; internal set; }
        public bool V9MasterStackShowBottomDielectric { get; internal set; }
        public bool V9MasterStackIsFlex { get; internal set; }
        public List<(string Id, string Name, int LayerId, bool UsedByPrims, int DielType, double DielConst, Coord DielHeight, string DielMaterial, Coord COverLayEXPansiOn, Coord CopThick, int ComponentPlacement)> V9StackLayer { get; internal set; }
        public List<(int LayerId, bool UsedByPrims, string Id, string Name, int DielType, double DielConst, Coord DielHeight, string DielMaterial, Coord COverLayEXPansiOn, Coord CopThick, int ComponentPlacement, Coord PullBackDistance, bool MechEnabled)> V9CacheLayer { get; internal set; }
        public string LayerMasterStackV8Name { get; internal set; }
        public bool LayerMasterStackV8ShowTopDielectric { get; internal set; }
        public bool LayerMasterStackV8ShowBottomDielectric { get; internal set; }
        public bool LayerMasterStackV8IsFlex { get; internal set; }
        public List<(string Id, string Name, int LayerId, bool UsedByPrims, int DielType, double DielConst, Coord DielHeight, string DielMaterial, Coord COverLayEXPansiOn, Coord CopThick, int ComponentPlacement, bool MechEnabled)> LayerV8 { get; internal set; }
        public Coord TopHeight { get; internal set; }
        public string TopMaterial { get; internal set; }
        public int BottomType { get; internal set; }
        public double BottomConst { get; internal set; }
        public Coord BottomHeight { get; internal set; }
        public string BottomMaterial { get; internal set; }
        public int LayersTackStyle { get; internal set; }
        public bool ShowTopDielectric { get; internal set; }
        public bool ShowBottomDielectric { get; internal set; }
        public List<(string Name, int Prev, int Next, bool MechEnabled, Coord CopThick, int DielType, double DielConst, Coord DielHeight, string DielMaterial)> Layer { get; internal set; }
        public List<(string Name, int Prev, int Next, bool MechEnabled, Coord CopThick, int DielType, double DielConst, Coord DielHeight, string DielMaterial, int LayerId)> LayerV7 { get; internal set; }
        public Coord BigVisibleGridSize { get; internal set; }
        public Coord VisibleGridSize { get; internal set; }
        public Coord SnapGridSize { get; internal set; }
        public Coord SnapGridSizeX { get; internal set; }
        public Coord SnapGridSizeY { get; internal set; }
        public Coord ElectricalGridRange { get; internal set; }
        public bool ElectricalGridEnabled { get; internal set; }
        public bool DotGrid { get; internal set; }
        public bool DotGridLarge { get; internal set; }
        public Unit DisplayUnit { get; internal set; }
        public double ToggleLayers { get; internal set; }
        public bool ShowDefaultSets { get; internal set; }
        public List<(string Name, string Layers, string ActiveLayer, bool IsCurrent, bool IsLocked, bool FlipBoard)> Layersets { get; internal set; }
        public int Cfg2DPrimDrawMode { get; internal set; }
        public string Cfg2DLayerOpacityTopLayer { get; internal set; }
        public List<string> Cfg2DLayerOpacityMidLayer { get; internal set; }
        public string Cfg2DLayerOpacityBottomOverLay { get; internal set; }
        public string Cfg2DLayerOpacityTopPaste { get; internal set; }
        public string Cfg2DLayerOpacityBottomPaste { get; internal set; }
        public string Cfg2DLayerOpacityTopSolder { get; internal set; }
        public string Cfg2DLayerOpacityBottomSolder { get; internal set; }
        public string Cfg2DLayerOpacityInternalPlane1 { get; internal set; }
        public string Cfg2DLayerOpacityInternalPlane2 { get; internal set; }
        public string Cfg2DLayerOpacityInternalPlane3 { get; internal set; }
        public string Cfg2DLayerOpacityInternalPlane4 { get; internal set; }
        public string Cfg2DLayerOpacityInternalPlane5 { get; internal set; }
        public string Cfg2DLayerOpacityInternalPlane6 { get; internal set; }
        public string Cfg2DLayerOpacityInternalPlane7 { get; internal set; }
        public string Cfg2DLayerOpacityInternalPlane8 { get; internal set; }
        public string Cfg2DLayerOpacityInternalPlane9 { get; internal set; }
        public string Cfg2DLayerOpacityInternalPlane10 { get; internal set; }
        public string Cfg2DLayerOpacityInternalPlane11 { get; internal set; }
        public string Cfg2DLayerOpacityInternalPlane12 { get; internal set; }
        public string Cfg2DLayerOpacityInternalPlane13 { get; internal set; }
        public string Cfg2DLayerOpacityInternalPlane14 { get; internal set; }
        public string Cfg2DLayerOpacityInternalPlane15 { get; internal set; }
        public string Cfg2DLayerOpacityInternalPlane16 { get; internal set; }
        public string Cfg2DLayerOpacityDrillGuide { get; internal set; }
        public string Cfg2DLayerOpacityKeepoutLayer { get; internal set; }
        public string Cfg2DLayerOpacityMechanical1 { get; internal set; }
        public string Cfg2DLayerOpacityMechanical2 { get; internal set; }
        public string Cfg2DLayerOpacityMechanical3 { get; internal set; }
        public string Cfg2DLayerOpacityMechanical4 { get; internal set; }
        public string Cfg2DLayerOpacityMechanical5 { get; internal set; }
        public string Cfg2DLayerOpacityMechanical6 { get; internal set; }
        public string Cfg2DLayerOpacityMechanical7 { get; internal set; }
        public string Cfg2DLayerOpacityMechanical8 { get; internal set; }
        public string Cfg2DLayerOpacityMechanical9 { get; internal set; }
        public string Cfg2DLayerOpacityMechanical10 { get; internal set; }
        public string Cfg2DLayerOpacityMechanical11 { get; internal set; }
        public string Cfg2DLayerOpacityMechanical12 { get; internal set; }
        public string Cfg2DLayerOpacityMechanical13 { get; internal set; }
        public string Cfg2DLayerOpacityMechanical14 { get; internal set; }
        public string Cfg2DLayerOpacityMechanical15 { get; internal set; }
        public string Cfg2DLayerOpacityMechanical16 { get; internal set; }
        public string Cfg2DLayerOpacityDrillDrawing { get; internal set; }
        public string Cfg2DLayerOpacityMultiLayer { get; internal set; }
        public string Cfg2DLayerOpacityCOnNectLayer { get; internal set; }
        public string Cfg2DLayerOpacityBackGroundLayer { get; internal set; }
        public string Cfg2DLayerOpacityDrcErrorLayer { get; internal set; }
        public string Cfg2DLayerOpacityHighlightLayer { get; internal set; }
        public string Cfg2DLayerOpacityGridColor1 { get; internal set; }
        public string Cfg2DLayerOpacityGridColor10 { get; internal set; }
        public string Cfg2DLayerOpacityPadHoleLayer { get; internal set; }
        public string Cfg2DLayerOpacityViaHoleLayer { get; internal set; }
        public double Cfg2DToggleLayers { get; internal set; }
        public string Cfg2DToggleLayersSet { get; internal set; }
        public double Cfg2DWorkspaceColAlpha0 { get; internal set; }
        public double Cfg2DWorkspaceColAlpha1 { get; internal set; }
        public double Cfg2DWorkspaceColAlpha2 { get; internal set; }
        public double Cfg2DWorkspaceColAlpha3 { get; internal set; }
        public double Cfg2DWorkspaceColAlpha4 { get; internal set; }
        public double Cfg2DWorkspaceColAlpha5 { get; internal set; }
        public double Cfg2DWorkspaceColAlpha6 { get; internal set; }
        public double Cfg2DWorkspaceColAlpha7 { get; internal set; }
        public double Cfg2DWorkspaceColAlpha8 { get; internal set; }
        public double Cfg2DWorkspaceColAlpha9 { get; internal set; }
        public double Cfg2DWorkspaceColAlpha10 { get; internal set; }
        public double Cfg2DWorkspaceColAlpha11 { get; internal set; }
        public int Cfg2DMechLayerInSingleLayerMode { get; internal set; }
        public string Cfg2DMechLayerInSingleLayerModeSet { get; internal set; }
        public int Cfg2DMechLayerLinkedToSheet { get; internal set; }
        public string Cfg2DMechLayerLinkedToSheetSet { get; internal set; }
        public string Cfg2DCurrentLayer { get; internal set; }
        public bool Cfg2DDisplaySpecialStrings { get; internal set; }
        public bool Cfg2DShowTestPoints { get; internal set; }
        public bool Cfg2DShowOriginMarker { get; internal set; }
        public int Cfg2DEyeDist { get; internal set; }
        public bool Cfg2DShowStatusInfo { get; internal set; }
        public bool Cfg2DShowPadNets { get; internal set; }
        public bool Cfg2DShowPadNumberS { get; internal set; }
        public bool Cfg2DShowViaNets { get; internal set; }
        public bool Cfg2DUSetRansparentLayers { get; internal set; }
        public int Cfg2DPlaneDrawMode { get; internal set; }
        public int Cfg2DDisplayNetNamesOnTracks { get; internal set; }
        public int Cfg2DFromToSDisplayMode { get; internal set; }
        public int Cfg2DPadTypeSDisplayMode { get; internal set; }
        public int Cfg2DSingleLayerModeState { get; internal set; }
        public Color Cfg2DOriginMarkerColor { get; internal set; }
        public bool Cfg2DShowComponentRefPoint { get; internal set; }
        public Color Cfg2DComponentRefPointColor { get; internal set; }
        public bool Cfg2DPosItiveTopSolderMask { get; internal set; }
        public bool Cfg2DPosItiveBottomSolderMask { get; internal set; }
        public double Cfg2DTopPosItivesolderMaskAlpha { get; internal set; }
        public double Cfg2DBottomPosItivesolderMaskAlpha { get; internal set; }
        public bool Cfg2DAllCOnNectiOnSInSingleLayerMode { get; internal set; }
        public bool Cfg2DMultiColorEdcOnNectiOnS { get; internal set; }
        public string BoardInsightViewConfigurationName { get; internal set; }
        public double VisibleGridMultFactor { get; internal set; }
        public double BigVisibleGridMultFactor { get; internal set; }
        public string Current2D3DViewState { get; internal set; }
        public CoordRect ViewPort { get; set; }
        public string Property2DConfigType { get; internal set; }
        public string Property2DConfiguration { get; internal set; }
        public string Property2DConfigFullFilename { get; internal set; }
        public string Property3DConfigType { get; internal set; }
        public string Property3DConfiguration { get; internal set; }
        public string Property3DConfigFullFilename { get; internal set; }
        public CoordPoint3D LookAt { get; internal set; }
        public double EyeRotationX { get; internal set; }
        public double EyeRotationY { get; internal set; }
        public double EyeRotationZ { get; internal set; }
        public double ZoomMult { get; internal set; }
        public CoordPoint ViewSize { get; internal set; }
        public Coord EgRange { get; internal set; }
        public double EgMult { get; internal set; }
        public bool EgEnabled { get; internal set; }
        public bool EgSnapToBoardOutline { get; internal set; }
        public bool EgSnapToArcCenters { get; internal set; }
        public bool EgUseAllLayers { get; internal set; }
        public bool OgSnapEnabled { get; internal set; }
        public bool MgSnapEnabled { get; internal set; }
        public bool PointGuideEnabled { get; internal set; }
        public bool GridSnapEnabled { get; internal set; }
        public bool NearObjectsEnabled { get; internal set; }
        public bool FarObjectsEnabled { get; internal set; }
        public double NearObjectSet { get; internal set; }
        public double FarObjectSet { get; internal set; }
        public Coord NearDistance { get; internal set; }
        public double BoardVersion { get; internal set; }
        public string VaultGuid { get; internal set; }
        public string FolderGuid { get; internal set; }
        public string LifeCycleDefinitionGuid { get; internal set; }
        public string RevisionNamingSchemeGuid { get; internal set; }

        public PcbLibHeader()
        {
            Version = "3.00";
        }

        public void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            Filename = p["FILENAME"].AsStringOrDefault();
            var kind = p["KIND"].AsStringOrDefault();
            if (kind != Kind) throw new ArgumentOutOfRangeException($"{nameof(p)}[\"KIND\"]");
            Version = p["VERSION"].AsStringOrDefault();
            Date = p["DATE"].AsStringOrDefault();
            Time = p["TIME"].AsStringOrDefault();
            V9MasterStackStyle = p["V9_MASTERSTACK_STYLE"].AsIntOrDefault();
            V9MasterStackId = p["V9_MASTERSTACK_ID"].AsStringOrDefault();
            V9MasterStackName = p["V9_MASTERSTACK_NAME"].AsStringOrDefault();
            V9MasterStackShowTopDielectric = p["V9_MASTERSTACK_SHOWTOPDIELECTRIC"].AsBool();
            V9MasterStackShowBottomDielectric = p["V9_MASTERSTACK_SHOWBOTTOMDIELECTRIC"].AsBool();
            V9MasterStackIsFlex = p["V9_MASTERSTACK_ISFLEX"].AsBool();
            V9StackLayer = Enumerable.Range(
                    p.Select(kv => Regex.Match(kv.Item1, string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_ID", @"(\d+)")).Groups[1].Value).Where(v => !string.IsNullOrEmpty(v)).DefaultIfEmpty("0").Min(v => int.Parse(v, CultureInfo.InvariantCulture)),
                    p.Select(kv => Regex.Match(kv.Item1, string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_ID", @"(\d+)")).Groups[1].Value).Where(v => !string.IsNullOrEmpty(v)).Count())
                .Select(i => (
                    p[string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_ID", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_NAME", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_LAYERID", i)].AsIntOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_USEDBYPRIMS", i)].AsBool(),
                    p[string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_DIELTYPE", i)].AsIntOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_DIELCONST", i)].AsDoubleOrDefault(),
                    Utils.DxpFracToCoord(p[string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_DIELHEIGHT", i)].AsIntOrDefault(), p["V9_STACK_LAYER6_DIELHEIGHT_FRAC"].AsIntOrDefault()),
                    p[string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_DIELMATERIAL", i)].AsStringOrDefault(),
                    Utils.DxpFracToCoord(p[string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_COVERLAY_EXPANSION", i)].AsIntOrDefault(), p["V9_STACK_LAYER6_COVERLAY_EXPANSION_FRAC"].AsIntOrDefault()),
                    Utils.DxpFracToCoord(p[string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_COPTHICK", i)].AsIntOrDefault(), p["V9_STACK_LAYER5_COPTHICK_FRAC"].AsIntOrDefault()),
                    p[string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_COMPONENTPLACEMENT", i)].AsIntOrDefault()))
                .ToList();
            V9CacheLayer = Enumerable.Range(
                    p.Select(kv => Regex.Match(kv.Item1, string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_LAYERID", @"(\d+)")).Groups[1].Value).Where(v => !string.IsNullOrEmpty(v)).DefaultIfEmpty("0").Min(v => int.Parse(v, CultureInfo.InvariantCulture)),
                    p.Select(kv => Regex.Match(kv.Item1, string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_LAYERID", @"(\d+)")).Groups[1].Value).Where(v => !string.IsNullOrEmpty(v)).Count())
                .Select(i => (
                    p[string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_LAYERID", i)].AsIntOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_USEDBYPRIMS", i)].AsBool(),
                    p[string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_ID", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_NAME", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_DIELTYPE", i)].AsIntOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_DIELCONST", i)].AsDoubleOrDefault(),
                    Utils.DxpFracToCoord(p[string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_DIELHEIGHT", i)].AsIntOrDefault(), p["V9_CACHE_LAYER18_DIELHEIGHT_FRAC"].AsIntOrDefault()),
                    p[string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_DIELMATERIAL", i)].AsStringOrDefault(),
                    Utils.DxpFracToCoord(p[string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_COVERLAY_EXPANSION", i)].AsIntOrDefault(), p["V9_CACHE_LAYER18_COVERLAY_EXPANSION_FRAC"].AsIntOrDefault()),
                    Utils.DxpFracToCoord(p[string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_COPTHICK", i)].AsIntOrDefault(), p["V9_CACHE_LAYER66_COPTHICK_FRAC"].AsIntOrDefault()),
                    p[string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_COMPONENTPLACEMENT", i)].AsIntOrDefault(),
                    Utils.DxpFracToCoord(p[string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_PULLBACKDISTANCE", i)].AsIntOrDefault(), p["V9_CACHE_LAYER66_PULLBACKDISTANCE_FRAC"].AsIntOrDefault()),
                    p[string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_MECHENABLED", i)].AsBool()))
                .ToList();
            LayerMasterStackV8Name = p["LAYERMASTERSTACK_V8NAME"].AsStringOrDefault();
            LayerMasterStackV8ShowTopDielectric = p["LAYERMASTERSTACK_V8SHOWTOPDIELECTRIC"].AsBool();
            LayerMasterStackV8ShowBottomDielectric = p["LAYERMASTERSTACK_V8SHOWBOTTOMDIELECTRIC"].AsBool();
            LayerMasterStackV8IsFlex = p["LAYERMASTERSTACK_V8ISFLEX"].AsBool();
            LayerV8 = Enumerable.Range(
                    p.Select(kv => Regex.Match(kv.Item1, string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}ID", @"(\d+)")).Groups[1].Value).Where(v => !string.IsNullOrEmpty(v)).DefaultIfEmpty("0").Min(v => int.Parse(v, CultureInfo.InvariantCulture)),
                    p.Select(kv => Regex.Match(kv.Item1, string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}ID", @"(\d+)")).Groups[1].Value).Where(v => !string.IsNullOrEmpty(v)).Count())
                .Select(i => (
                    p[string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}ID", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}NAME", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}LAYERID", i)].AsIntOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}USEDBYPRIMS", i)].AsBool(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}DIELTYPE", i)].AsIntOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}DIELCONST", i)].AsDoubleOrDefault(),
                    Utils.DxpFracToCoord(p[string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}DIELHEIGHT", i)].AsIntOrDefault(), p["LAYER_V8_6DIELHEIGHT_FRAC"].AsIntOrDefault()),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}DIELMATERIAL", i)].AsStringOrDefault(),
                    Utils.DxpFracToCoord(p[string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}COVERLAY_EXPANSION", i)].AsIntOrDefault(), p["LAYER_V8_6COVERLAY_EXPANSION_FRAC"].AsIntOrDefault()),
                    Utils.DxpFracToCoord(p[string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}COPTHICK", i)].AsIntOrDefault(), p["LAYER_V8_5COPTHICK_FRAC"].AsIntOrDefault()),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}COMPONENTPLACEMENT", i)].AsIntOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}MECHENABLED", i)].AsBool()))
                .ToList();
            TopHeight = Utils.DxpFracToCoord(p["TOPHEIGHT"].AsIntOrDefault(), p["TOPHEIGHT_FRAC"].AsIntOrDefault());
            TopMaterial = p["TOPMATERIAL"].AsStringOrDefault();
            BottomType = p["BOTTOMTYPE"].AsIntOrDefault();
            BottomConst = p["BOTTOMCONST"].AsDoubleOrDefault();
            BottomHeight = Utils.DxpFracToCoord(p["BOTTOMHEIGHT"].AsIntOrDefault(), p["BOTTOMHEIGHT_FRAC"].AsIntOrDefault());
            BottomMaterial = p["BOTTOMMATERIAL"].AsStringOrDefault();
            LayersTackStyle = p["LAYERSTACKSTYLE"].AsIntOrDefault();
            ShowTopDielectric = p["SHOWTOPDIELECTRIC"].AsBool();
            ShowBottomDielectric = p["SHOWBOTTOMDIELECTRIC"].AsBool();
            Layer = Enumerable.Range(
                    p.Select(kv => Regex.Match(kv.Item1, string.Format(CultureInfo.InvariantCulture, "LAYER{0}NAME", @"(\d+)")).Groups[1].Value).Where(v => !string.IsNullOrEmpty(v)).DefaultIfEmpty("0").Min(v => int.Parse(v, CultureInfo.InvariantCulture)),
                    p.Select(kv => Regex.Match(kv.Item1, string.Format(CultureInfo.InvariantCulture, "LAYER{0}NAME", @"(\d+)")).Groups[1].Value).Where(v => !string.IsNullOrEmpty(v)).Count())
                .Select(i => (
                    p[string.Format(CultureInfo.InvariantCulture, "LAYER{0}NAME", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYER{0}PREV", i)].AsIntOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYER{0}NEXT", i)].AsIntOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYER{0}MECHENABLED", i)].AsBool(),
                    Utils.DxpFracToCoord(p[string.Format(CultureInfo.InvariantCulture, "LAYER{0}COPTHICK", i)].AsIntOrDefault(), p["LAYER82COPTHICK_FRAC"].AsIntOrDefault()),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYER{0}DIELTYPE", i)].AsIntOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYER{0}DIELCONST", i)].AsDoubleOrDefault(),
                    Utils.DxpFracToCoord(p[string.Format(CultureInfo.InvariantCulture, "LAYER{0}DIELHEIGHT", i)].AsIntOrDefault(), p["LAYER82DIELHEIGHT_FRAC"].AsIntOrDefault()),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYER{0}DIELMATERIAL", i)].AsStringOrDefault()))
                .ToList();
            LayerV7 = Enumerable.Range(
                    p.Select(kv => Regex.Match(kv.Item1, string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}NAME", @"(\d+)")).Groups[1].Value).Where(v => !string.IsNullOrEmpty(v)).DefaultIfEmpty("0").Min(v => int.Parse(v, CultureInfo.InvariantCulture)),
                    p.Select(kv => Regex.Match(kv.Item1, string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}NAME", @"(\d+)")).Groups[1].Value).Where(v => !string.IsNullOrEmpty(v)).Count())
                .Select(i => (
                    p[string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}NAME", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}PREV", i)].AsIntOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}NEXT", i)].AsIntOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}MECHENABLED", i)].AsBool(),
                    Utils.DxpFracToCoord(p[string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}COPTHICK", i)].AsIntOrDefault(), p["LAYERV7_15COPTHICK_FRAC"].AsIntOrDefault()),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}DIELTYPE", i)].AsIntOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}DIELCONST", i)].AsDoubleOrDefault(),
                    Utils.DxpFracToCoord(p[string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}DIELHEIGHT", i)].AsIntOrDefault(), p["LAYERV7_15DIELHEIGHT_FRAC"].AsIntOrDefault()),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}DIELMATERIAL", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}LAYERID", i)].AsIntOrDefault()))
                .ToList();
            BigVisibleGridSize = Coord.FromInt32((int)p["BIGVISIBLEGRIDSIZE"].AsDoubleOrDefault());
            VisibleGridSize = Coord.FromInt32((int)p["VISIBLEGRIDSIZE"].AsDoubleOrDefault());
            SnapGridSize = (int)p["SNAPGRIDSIZE"].AsDoubleOrDefault();
            SnapGridSizeX = (int)p["SNAPGRIDSIZEX"].AsDoubleOrDefault();
            SnapGridSizeY = (int)p["SNAPGRIDSIZEY"].AsDoubleOrDefault();
            ElectricalGridRange = Utils.DxpFracToCoord(p["ELECTRICALGRIDRANGE"].AsIntOrDefault(), p["ELECTRICALGRIDRANGE_FRAC"].AsIntOrDefault());
            ElectricalGridEnabled = p["ELECTRICALGRIDENABLED"].AsBool();
            DotGrid = p["DOTGRID"].AsBool();
            DotGridLarge = p["DOTGRIDLARGE"].AsBool();
            DisplayUnit = p["DISPLAYUNIT"].AsIntOrDefault() == 0 ? Unit.Millimeter : Unit.Mil;
            ToggleLayers = p["TOGGLELAYERS"].AsDoubleOrDefault();
            ShowDefaultSets = p["SHOWDEFAULTSETS"].AsBool();
            Layersets = Enumerable.Range(1, p["LAYERSETSCOUNT"].AsInt())
                .Select(i => (
                    p[string.Format(CultureInfo.InvariantCulture, "LAYERSET{0}NAME", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYERSET{0}LAYERS", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYERSET{0}ACTIVELAYER.7", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYERSET{0}ISCURRENT", i)].AsBool(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYERSET{0}ISLOCKED", i)].AsBool(),
                    p[string.Format(CultureInfo.InvariantCulture, "LAYERSET{0}FLIPBOARD", i)].AsBool()))
                .ToList();
            Cfg2DPrimDrawMode = p["CFG2D.PRIMDRAWMODE"].AsIntOrDefault();
            Cfg2DLayerOpacityTopLayer = p["CFG2D.LAYEROPACITY.TOPLAYER"].AsStringOrDefault();
            Cfg2DLayerOpacityMidLayer = Enumerable.Range(
                    p.Select(kv => Regex.Match(kv.Item1, string.Format(CultureInfo.InvariantCulture, "CFG2D.LAYEROPACITY.MIDLAYER{0}", @"(\d+)")).Groups[1].Value).Where(v => !string.IsNullOrEmpty(v)).DefaultIfEmpty("0").Min(v => int.Parse(v, CultureInfo.InvariantCulture)),
                    p.Select(kv => Regex.Match(kv.Item1, string.Format(CultureInfo.InvariantCulture, "CFG2D.LAYEROPACITY.MIDLAYER{0}", @"(\d+)")).Groups[1].Value).Where(v => !string.IsNullOrEmpty(v)).Count())
                .Select(i =>
                    p[string.Format(CultureInfo.InvariantCulture, "CFG2D.LAYEROPACITY.MIDLAYER{0}", i)].AsStringOrDefault())
                .ToList();
            Cfg2DLayerOpacityBottomOverLay = p["CFG2D.LAYEROPACITY.BOTTOMOVERLAY"].AsStringOrDefault();
            Cfg2DLayerOpacityTopPaste = p["CFG2D.LAYEROPACITY.TOPPASTE"].AsStringOrDefault();
            Cfg2DLayerOpacityBottomPaste = p["CFG2D.LAYEROPACITY.BOTTOMPASTE"].AsStringOrDefault();
            Cfg2DLayerOpacityTopSolder = p["CFG2D.LAYEROPACITY.TOPSOLDER"].AsStringOrDefault();
            Cfg2DLayerOpacityBottomSolder = p["CFG2D.LAYEROPACITY.BOTTOMSOLDER"].AsStringOrDefault();
            Cfg2DLayerOpacityInternalPlane1 = p["CFG2D.LAYEROPACITY.INTERNALPLANE1"].AsStringOrDefault();
            Cfg2DLayerOpacityInternalPlane2 = p["CFG2D.LAYEROPACITY.INTERNALPLANE2"].AsStringOrDefault();
            Cfg2DLayerOpacityInternalPlane3 = p["CFG2D.LAYEROPACITY.INTERNALPLANE3"].AsStringOrDefault();
            Cfg2DLayerOpacityInternalPlane4 = p["CFG2D.LAYEROPACITY.INTERNALPLANE4"].AsStringOrDefault();
            Cfg2DLayerOpacityInternalPlane5 = p["CFG2D.LAYEROPACITY.INTERNALPLANE5"].AsStringOrDefault();
            Cfg2DLayerOpacityInternalPlane6 = p["CFG2D.LAYEROPACITY.INTERNALPLANE6"].AsStringOrDefault();
            Cfg2DLayerOpacityInternalPlane7 = p["CFG2D.LAYEROPACITY.INTERNALPLANE7"].AsStringOrDefault();
            Cfg2DLayerOpacityInternalPlane8 = p["CFG2D.LAYEROPACITY.INTERNALPLANE8"].AsStringOrDefault();
            Cfg2DLayerOpacityInternalPlane9 = p["CFG2D.LAYEROPACITY.INTERNALPLANE9"].AsStringOrDefault();
            Cfg2DLayerOpacityInternalPlane10 = p["CFG2D.LAYEROPACITY.INTERNALPLANE10"].AsStringOrDefault();
            Cfg2DLayerOpacityInternalPlane11 = p["CFG2D.LAYEROPACITY.INTERNALPLANE11"].AsStringOrDefault();
            Cfg2DLayerOpacityInternalPlane12 = p["CFG2D.LAYEROPACITY.INTERNALPLANE12"].AsStringOrDefault();
            Cfg2DLayerOpacityInternalPlane13 = p["CFG2D.LAYEROPACITY.INTERNALPLANE13"].AsStringOrDefault();
            Cfg2DLayerOpacityInternalPlane14 = p["CFG2D.LAYEROPACITY.INTERNALPLANE14"].AsStringOrDefault();
            Cfg2DLayerOpacityInternalPlane15 = p["CFG2D.LAYEROPACITY.INTERNALPLANE15"].AsStringOrDefault();
            Cfg2DLayerOpacityInternalPlane16 = p["CFG2D.LAYEROPACITY.INTERNALPLANE16"].AsStringOrDefault();
            Cfg2DLayerOpacityDrillGuide = p["CFG2D.LAYEROPACITY.DRILLGUIDE"].AsStringOrDefault();
            Cfg2DLayerOpacityKeepoutLayer = p["CFG2D.LAYEROPACITY.KEEPOUTLAYER"].AsStringOrDefault();
            Cfg2DLayerOpacityMechanical1 = p["CFG2D.LAYEROPACITY.MECHANICAL1"].AsStringOrDefault();
            Cfg2DLayerOpacityMechanical2 = p["CFG2D.LAYEROPACITY.MECHANICAL2"].AsStringOrDefault();
            Cfg2DLayerOpacityMechanical3 = p["CFG2D.LAYEROPACITY.MECHANICAL3"].AsStringOrDefault();
            Cfg2DLayerOpacityMechanical4 = p["CFG2D.LAYEROPACITY.MECHANICAL4"].AsStringOrDefault();
            Cfg2DLayerOpacityMechanical5 = p["CFG2D.LAYEROPACITY.MECHANICAL5"].AsStringOrDefault();
            Cfg2DLayerOpacityMechanical6 = p["CFG2D.LAYEROPACITY.MECHANICAL6"].AsStringOrDefault();
            Cfg2DLayerOpacityMechanical7 = p["CFG2D.LAYEROPACITY.MECHANICAL7"].AsStringOrDefault();
            Cfg2DLayerOpacityMechanical8 = p["CFG2D.LAYEROPACITY.MECHANICAL8"].AsStringOrDefault();
            Cfg2DLayerOpacityMechanical9 = p["CFG2D.LAYEROPACITY.MECHANICAL9"].AsStringOrDefault();
            Cfg2DLayerOpacityMechanical10 = p["CFG2D.LAYEROPACITY.MECHANICAL10"].AsStringOrDefault();
            Cfg2DLayerOpacityMechanical11 = p["CFG2D.LAYEROPACITY.MECHANICAL11"].AsStringOrDefault();
            Cfg2DLayerOpacityMechanical12 = p["CFG2D.LAYEROPACITY.MECHANICAL12"].AsStringOrDefault();
            Cfg2DLayerOpacityMechanical13 = p["CFG2D.LAYEROPACITY.MECHANICAL13"].AsStringOrDefault();
            Cfg2DLayerOpacityMechanical14 = p["CFG2D.LAYEROPACITY.MECHANICAL14"].AsStringOrDefault();
            Cfg2DLayerOpacityMechanical15 = p["CFG2D.LAYEROPACITY.MECHANICAL15"].AsStringOrDefault();
            Cfg2DLayerOpacityMechanical16 = p["CFG2D.LAYEROPACITY.MECHANICAL16"].AsStringOrDefault();
            Cfg2DLayerOpacityDrillDrawing = p["CFG2D.LAYEROPACITY.DRILLDRAWING"].AsStringOrDefault();
            Cfg2DLayerOpacityMultiLayer = p["CFG2D.LAYEROPACITY.MULTILAYER"].AsStringOrDefault();
            Cfg2DLayerOpacityCOnNectLayer = p["CFG2D.LAYEROPACITY.CONNECTLAYER"].AsStringOrDefault();
            Cfg2DLayerOpacityBackGroundLayer = p["CFG2D.LAYEROPACITY.BACKGROUNDLAYER"].AsStringOrDefault();
            Cfg2DLayerOpacityDrcErrorLayer = p["CFG2D.LAYEROPACITY.DRCERRORLAYER"].AsStringOrDefault();
            Cfg2DLayerOpacityHighlightLayer = p["CFG2D.LAYEROPACITY.HIGHLIGHTLAYER"].AsStringOrDefault();
            Cfg2DLayerOpacityGridColor1 = p["CFG2D.LAYEROPACITY.GRIDCOLOR1"].AsStringOrDefault();
            Cfg2DLayerOpacityGridColor10 = p["CFG2D.LAYEROPACITY.GRIDCOLOR10"].AsStringOrDefault();
            Cfg2DLayerOpacityPadHoleLayer = p["CFG2D.LAYEROPACITY.PADHOLELAYER"].AsStringOrDefault();
            Cfg2DLayerOpacityViaHoleLayer = p["CFG2D.LAYEROPACITY.VIAHOLELAYER"].AsStringOrDefault();
            Cfg2DToggleLayers = p["CFG2D.TOGGLELAYERS"].AsDoubleOrDefault();
            Cfg2DToggleLayersSet = p["CFG2D.TOGGLELAYERS.SET"].AsStringOrDefault();
            Cfg2DWorkspaceColAlpha0 = p["CFG2D.WORKSPACECOLALPHA0"].AsDoubleOrDefault();
            Cfg2DWorkspaceColAlpha1 = p["CFG2D.WORKSPACECOLALPHA1"].AsDoubleOrDefault();
            Cfg2DWorkspaceColAlpha2 = p["CFG2D.WORKSPACECOLALPHA2"].AsDoubleOrDefault();
            Cfg2DWorkspaceColAlpha3 = p["CFG2D.WORKSPACECOLALPHA3"].AsDoubleOrDefault();
            Cfg2DWorkspaceColAlpha4 = p["CFG2D.WORKSPACECOLALPHA4"].AsDoubleOrDefault();
            Cfg2DWorkspaceColAlpha5 = p["CFG2D.WORKSPACECOLALPHA5"].AsDoubleOrDefault();
            Cfg2DWorkspaceColAlpha6 = p["CFG2D.WORKSPACECOLALPHA6"].AsDoubleOrDefault();
            Cfg2DWorkspaceColAlpha7 = p["CFG2D.WORKSPACECOLALPHA7"].AsDoubleOrDefault();
            Cfg2DWorkspaceColAlpha8 = p["CFG2D.WORKSPACECOLALPHA8"].AsDoubleOrDefault();
            Cfg2DWorkspaceColAlpha9 = p["CFG2D.WORKSPACECOLALPHA9"].AsDoubleOrDefault();
            Cfg2DWorkspaceColAlpha10 = p["CFG2D.WORKSPACECOLALPHA10"].AsDoubleOrDefault();
            Cfg2DWorkspaceColAlpha11 = p["CFG2D.WORKSPACECOLALPHA11"].AsDoubleOrDefault();
            Cfg2DMechLayerInSingleLayerMode = p["CFG2D.MECHLAYERINSINGLELAYERMODE"].AsIntOrDefault();
            Cfg2DMechLayerInSingleLayerModeSet = p["CFG2D.MECHLAYERINSINGLELAYERMODE.SET"].AsStringOrDefault();
            Cfg2DMechLayerLinkedToSheet = p["CFG2D.MECHLAYERLINKEDTOSHEET"].AsIntOrDefault();
            Cfg2DMechLayerLinkedToSheetSet = p["CFG2D.MECHLAYERLINKEDTOSHEET.SET"].AsStringOrDefault();
            Cfg2DCurrentLayer = p["CFG2D.CURRENTLAYER"].AsStringOrDefault();
            Cfg2DDisplaySpecialStrings = p["CFG2D.DISPLAYSPECIALSTRINGS"].AsBool();
            Cfg2DShowTestPoints = p["CFG2D.SHOWTESTPOINTS"].AsBool();
            Cfg2DShowOriginMarker = p["CFG2D.SHOWORIGINMARKER"].AsBool();
            Cfg2DEyeDist = p["CFG2D.EYEDIST"].AsIntOrDefault();
            Cfg2DShowStatusInfo = p["CFG2D.SHOWSTATUSINFO"].AsBool();
            Cfg2DShowPadNets = p["CFG2D.SHOWPADNETS"].AsBool();
            Cfg2DShowPadNumberS = p["CFG2D.SHOWPADNUMBERS"].AsBool();
            Cfg2DShowViaNets = p["CFG2D.SHOWVIANETS"].AsBool();
            Cfg2DUSetRansparentLayers = p["CFG2D.USETRANSPARENTLAYERS"].AsBool();
            Cfg2DPlaneDrawMode = p["CFG2D.PLANEDRAWMODE"].AsIntOrDefault();
            Cfg2DDisplayNetNamesOnTracks = p["CFG2D.DISPLAYNETNAMESONTRACKS"].AsIntOrDefault();
            Cfg2DFromToSDisplayMode = p["CFG2D.FROMTOSDISPLAYMODE"].AsIntOrDefault();
            Cfg2DPadTypeSDisplayMode = p["CFG2D.PADTYPESDISPLAYMODE"].AsIntOrDefault();
            Cfg2DSingleLayerModeState = p["CFG2D.SINGLELAYERMODESTATE"].AsIntOrDefault();
            Cfg2DOriginMarkerColor = p["CFG2D.ORIGINMARKERCOLOR"].AsColorOrDefault();
            Cfg2DShowComponentRefPoint = p["CFG2D.SHOWCOMPONENTREFPOINT"].AsBool();
            Cfg2DComponentRefPointColor = p["CFG2D.COMPONENTREFPOINTCOLOR"].AsColorOrDefault();
            Cfg2DPosItiveTopSolderMask = p["CFG2D.POSITIVETOPSOLDERMASK"].AsBool();
            Cfg2DPosItiveBottomSolderMask = p["CFG2D.POSITIVEBOTTOMSOLDERMASK"].AsBool();
            Cfg2DTopPosItivesolderMaskAlpha = p["CFG2D.TOPPOSITIVESOLDERMASKALPHA"].AsDoubleOrDefault();
            Cfg2DBottomPosItivesolderMaskAlpha = p["CFG2D.BOTTOMPOSITIVESOLDERMASKALPHA"].AsDoubleOrDefault();
            Cfg2DAllCOnNectiOnSInSingleLayerMode = p["CFG2D.ALLCONNECTIONSINSINGLELAYERMODE"].AsBool();
            Cfg2DMultiColorEdcOnNectiOnS = p["CFG2D.MULTICOLOREDCONNECTIONS"].AsBool();
            BoardInsightViewConfigurationName = p["BOARDINSIGHTVIEWCONFIGURATIONNAME"].AsStringOrDefault();
            VisibleGridMultFactor = p["VISIBLEGRIDMULTFACTOR"].AsDoubleOrDefault();
            BigVisibleGridMultFactor = p["BIGVISIBLEGRIDMULTFACTOR"].AsDoubleOrDefault();
            Current2D3DViewState = p["CURRENT2D3DVIEWSTATE"].AsStringOrDefault();
            ViewPort = new CoordRect(
                Coord.FromInt32(p["VP.LX"].AsIntOrDefault()),
                Coord.FromInt32(p["VP.HX"].AsIntOrDefault() - p["VP.LX"].AsIntOrDefault()),
                Coord.FromInt32(p["VP.LY"].AsIntOrDefault()),
                Coord.FromInt32(p["VP.HY"].AsIntOrDefault() - p["VP.LY"].AsIntOrDefault()));
            Property2DConfigType = p["2DCONFIGTYPE"].AsStringOrDefault();
            Property2DConfiguration = p["2DCONFIGURATION"].AsStringOrDefault();
            Property2DConfigFullFilename = p["2DCONFIGFULLFILENAME"].AsStringOrDefault();
            Property3DConfigType = p["3DCONFIGTYPE"].AsStringOrDefault();
            Property3DConfiguration = p["3DCONFIGURATION"].AsStringOrDefault();
            Property3DConfigFullFilename = p["3DCONFIGFULLFILENAME"].AsStringOrDefault();
            LookAt = new CoordPoint3D(
                Coord.FromInt32((int)p["LOOKAT.X"].AsDoubleOrDefault()),
                Coord.FromInt32((int)p["LOOKAT.Y"].AsDoubleOrDefault()),
                Coord.FromInt32((int)p["LOOKAT.Z"].AsDoubleOrDefault()));
            EyeRotationX = p["EYEROTATION.X"].AsDoubleOrDefault();
            EyeRotationY = p["EYEROTATION.Y"].AsDoubleOrDefault();
            EyeRotationZ = p["EYEROTATION.Z"].AsDoubleOrDefault();
            ZoomMult = p["ZOOMMULT"].AsDoubleOrDefault();
            ViewSize = new CoordPoint(
                Coord.FromInt32((int)p["VIEWSIZE.X"].AsDoubleOrDefault()),
                Coord.FromInt32((int)p["VIEWSIZE.Y"].AsDoubleOrDefault()));
            EgRange = p["EGRANGE"].AsCoord();
            EgMult = p["EGMULT"].AsDoubleOrDefault();
            EgEnabled = p["EGENABLED"].AsBool();
            EgSnapToBoardOutline = p["EGSNAPTOBOARDOUTLINE"].AsBool();
            EgSnapToArcCenters = p["EGSNAPTOARCCENTERS"].AsBool();
            EgUseAllLayers = p["EGUSEALLLAYERS"].AsBool();
            OgSnapEnabled = p["OGSNAPENABLED"].AsBool();
            MgSnapEnabled = p["MGSNAPENABLED"].AsBool();
            PointGuideEnabled = p["POINTGUIDEENABLED"].AsBool();
            GridSnapEnabled = p["GRIDSNAPENABLED"].AsBool();
            NearObjectsEnabled = p["NEAROBJECTSENABLED"].AsBool();
            FarObjectsEnabled = p["FAROBJECTSENABLED"].AsBool();
            NearObjectSet = p["NEAROBJECTSET"].AsDoubleOrDefault();
            FarObjectSet = p["FAROBJECTSET"].AsDoubleOrDefault();
            NearDistance = Utils.DxpFracToCoord(p["NEARDISTANCE"].AsIntOrDefault(), p["NEARDISTANCE_FRAC"].AsIntOrDefault());
            BoardVersion = p["BOARDVERSION"].AsDoubleOrDefault();
            VaultGuid = p["VAULTGUID"].AsStringOrDefault();
            FolderGuid = p["FOLDERGUID"].AsStringOrDefault();
            LifeCycleDefinitionGuid = p["LIFECYCLEDEFINITIONGUID"].AsStringOrDefault();
            RevisionNamingSchemeGuid = p["REVISIONNAMINGSCHEMEGUID"].AsStringOrDefault();

            Console.WriteLine("-=-=-=-");
            Console.WriteLine(p.Data);
            Console.WriteLine();
            Console.WriteLine(p.ToString());
            Console.WriteLine("");
            Console.WriteLine(ExportToParameters().ToString());
            Console.WriteLine();
        }

        private void AddParamRecord(ParameterCollection p)
        {
            if (p.Contains("RECORD"))
            {
                p.AddKey("RECORD", true);
            }
            else
            {
                p.Add("RECORD", "Board");
            }
        }

        public void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            p.Add("FILENAME", Filename);
            p.Add("KIND", Kind);
            p.Add("VERSION", Version);
            p.Add("DATE", Date);
            p.Add("TIME", Time);
            p.Add("V9_MASTERSTACK_STYLE", V9MasterStackStyle, false);
            p.Add("V9_MASTERSTACK_ID", V9MasterStackId);
            p.Add("V9_MASTERSTACK_NAME", V9MasterStackName);
            p.Add("V9_MASTERSTACK_SHOWTOPDIELECTRIC", V9MasterStackShowTopDielectric);
            p.Add("V9_MASTERSTACK_SHOWBOTTOMDIELECTRIC", V9MasterStackShowBottomDielectric);
            p.Add("V9_MASTERSTACK_ISFLEX", V9MasterStackIsFlex);
            for (var i = 0; i < V9StackLayer.Count; i++)
            {
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_ID", i), V9StackLayer[i].Id);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_NAME", i), V9StackLayer[i].Name);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_LAYERID", i), V9StackLayer[i].LayerId);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_USEDBYPRIMS", i), V9StackLayer[i].UsedByPrims);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_DIELTYPE", i), V9StackLayer[i].DielType);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_DIELCONST", i), V9StackLayer[i].DielConst);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_DIELHEIGHT", i), V9StackLayer[i].DielHeight);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_DIELMATERIAL", i), V9StackLayer[i].DielMaterial);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_COVERLAY_EXPANSION", i), V9StackLayer[i].COverLayEXPansiOn);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_COPTHICK", i), V9StackLayer[i].CopThick);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_STACK_LAYER{0}_COMPONENTPLACEMENT", i), V9StackLayer[i].ComponentPlacement);
            }
            for (var i = 0; i < V9CacheLayer.Count; i++)
            {
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_LAYERID", i), V9CacheLayer[i].LayerId);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_USEDBYPRIMS", i), V9CacheLayer[i].UsedByPrims);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_ID", i), V9CacheLayer[i].Id);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_NAME", i), V9CacheLayer[i].Name);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_DIELTYPE", i), V9CacheLayer[i].DielType);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_DIELCONST", i), V9CacheLayer[i].DielConst);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_DIELHEIGHT", i), V9CacheLayer[i].DielHeight);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_DIELMATERIAL", i), V9CacheLayer[i].DielMaterial);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_COVERLAY_EXPANSION", i), V9CacheLayer[i].COverLayEXPansiOn);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_COPTHICK", i), V9CacheLayer[i].CopThick);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_COMPONENTPLACEMENT", i), V9CacheLayer[i].ComponentPlacement);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_PULLBACKDISTANCE", i), V9CacheLayer[i].PullBackDistance);
                p.Add(string.Format(CultureInfo.InvariantCulture, "V9_CACHE_LAYER{0}_MECHENABLED", i), V9CacheLayer[i].MechEnabled);
            }
            p.Add("LAYERMASTERSTACK_V8NAME", LayerMasterStackV8Name);
            p.Add("LAYERMASTERSTACK_V8SHOWTOPDIELECTRIC", LayerMasterStackV8ShowTopDielectric);
            p.Add("LAYERMASTERSTACK_V8SHOWBOTTOMDIELECTRIC", LayerMasterStackV8ShowBottomDielectric);
            p.Add("LAYERMASTERSTACK_V8ISFLEX", LayerMasterStackV8IsFlex);
            for (var i = 0; i < LayerV8.Count; i++)
            {
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}ID", i), LayerV8[i].Id);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}NAME", i), LayerV8[i].Name);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}LAYERID", i), LayerV8[i].LayerId);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}USEDBYPRIMS", i), LayerV8[i].UsedByPrims);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}DIELTYPE", i), LayerV8[i].DielType);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}DIELCONST", i), LayerV8[i].DielConst);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}DIELHEIGHT", i), LayerV8[i].DielHeight);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}DIELMATERIAL", i), LayerV8[i].DielMaterial);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}COVERLAY_EXPANSION", i), LayerV8[i].COverLayEXPansiOn);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}COPTHICK", i), LayerV8[i].CopThick);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}COMPONENTPLACEMENT", i), LayerV8[i].ComponentPlacement);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYER_V8_{0}MECHENABLED", i), LayerV8[i].MechEnabled);
            }
            {
                var (n, f) = Utils.CoordToDxpFrac(TopHeight);
                if (n != 0 || f != 0) p.Add("TOPHEIGHT", n);
                if (f != 0) p.Add("TOPHEIGHT" + "_FRAC", f);
            }
            p.Add("TOPMATERIAL", TopMaterial);
            p.Add("BOTTOMTYPE", BottomType);
            p.Add("BOTTOMCONST", BottomConst);
            {
                var (n, f) = Utils.CoordToDxpFrac(BottomHeight);
                if (n != 0 || f != 0) p.Add("BOTTOMHEIGHT", n);
                if (f != 0) p.Add("BOTTOMHEIGHT" + "_FRAC", f);
            }
            p.Add("BOTTOMMATERIAL", BottomMaterial);
            p.Add("LAYERSTACKSTYLE", LayersTackStyle);
            p.Add("SHOWTOPDIELECTRIC", ShowTopDielectric);
            p.Add("SHOWBOTTOMDIELECTRIC", ShowBottomDielectric);
            for (var i = 0; i < Layer.Count; i++)
            {
                if (i > 0 && i % 5 == 0) AddParamRecord(p);
                p.Add($"LAYER{i + 1}NAME", Layer[i].Name);
                p.Add($"LAYER{i + 1}PREV", Layer[i].Prev);
                p.Add($"LAYER{i + 1}NEXT", Layer[i].Next);
                p.Add($"LAYER{i + 1}MECHENABLED", Layer[i].MechEnabled);
                p.Add($"LAYER{i + 1}COPTHICK", Layer[i].CopThick);
                p.Add($"LAYER{i + 1}DIELTYPE", Layer[i].DielType);
                p.Add($"LAYER{i + 1}DIELCONST", Layer[i].DielConst);
                p.Add($"LAYER{i + 1}DIELHEIGHT", Layer[i].DielHeight);
                p.Add($"LAYER{i + 1}DIELMATERIAL", Layer[i].DielMaterial);
            }
            for (var i = 0; i < LayerV7.Count; i++)
            {
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}NAME", i), LayerV7[i].Name);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}PREV", i), LayerV7[i].Prev);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}NEXT", i), LayerV7[i].Next);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}MECHENABLED", i), LayerV7[i].MechEnabled);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}COPTHICK", i), LayerV7[i].CopThick);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}DIELTYPE", i), LayerV7[i].DielType);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}DIELCONST", i), LayerV7[i].DielConst);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}DIELHEIGHT", i), LayerV7[i].DielHeight);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}DIELMATERIAL", i), LayerV7[i].DielMaterial);
                p.Add(string.Format(CultureInfo.InvariantCulture, "LAYERV7_{0}LAYERID", i), LayerV7[i].LayerId);
            }

            AddParamRecord(p);
            p.Add("BIGVISIBLEGRIDSIZE", (double)BigVisibleGridSize.ToInt32());
            p.Add("VISIBLEGRIDSIZE", (double)VisibleGridSize.ToInt32());
            p.Add("SNAPGRIDSIZE", (double)SnapGridSize.ToInt32());
            p.Add("SNAPGRIDSIZEX", (double)SnapGridSizeX.ToInt32());
            p.Add("SNAPGRIDSIZEY", (double)SnapGridSizeY.ToInt32());
            {
                var (n, f) = Utils.CoordToDxpFrac(ElectricalGridRange);
                p.Add("ELECTRICALGRIDRANGE", n);
                p.Add("ELECTRICALGRIDRANGE_FRAC", f);
            }
            p.Add("ELECTRICALGRIDENABLED", ElectricalGridEnabled);
            p.Add("DOTGRID", DotGrid);
            p.Add("DOTGRIDLARGE", DotGridLarge);
            p.Add("DISPLAYUNIT", DisplayUnit == Unit.Mil ? 1 : 0);
            p.Add("TOGGLELAYERS", ToggleLayers);
            p.Add("SHOWDEFAULTSETS", ShowDefaultSets);
            p.Add("LAYERSETSCOUNT", Layersets.Count);
            for (var i = 0; i < Layersets.Count; i++)
            {
                p.Add($"LAYERSET{i + 1}NAME", Layersets[i].Name);
                p.Add($"LAYERSET{i + 1}LAYERS", Layersets[i].Layers);
                p.Add($"LAYERSET{i + 1}ACTIVELAYER.7", Layersets[i].ActiveLayer);
                p.Add($"LAYERSET{i + 1}ISCURRENT", Layersets[i].IsCurrent);
                p.Add($"LAYERSET{i + 1}ISLOCKED", Layersets[i].IsLocked);
                p.Add($"LAYERSET{i + 1}FLIPBOARD", Layersets[i].FlipBoard);
            }
            p.Add("CFG2D.PRIMDRAWMODE", Cfg2DPrimDrawMode);
            p.Add("CFG2D.LAYEROPACITY.TOPLAYER", Cfg2DLayerOpacityTopLayer);
            for (var i = 0; i < Cfg2DLayerOpacityMidLayer.Count; i++)
            {
                p.Add($"CFG2D.LAYEROPACITY.MIDLAYER{i + 1}", Cfg2DLayerOpacityMidLayer[i]);
            }
            p.Add("CFG2D.LAYEROPACITY.BOTTOMOVERLAY", Cfg2DLayerOpacityBottomOverLay);
            p.Add("CFG2D.LAYEROPACITY.TOPPASTE", Cfg2DLayerOpacityTopPaste);
            p.Add("CFG2D.LAYEROPACITY.BOTTOMPASTE", Cfg2DLayerOpacityBottomPaste);
            p.Add("CFG2D.LAYEROPACITY.TOPSOLDER", Cfg2DLayerOpacityTopSolder);
            p.Add("CFG2D.LAYEROPACITY.BOTTOMSOLDER", Cfg2DLayerOpacityBottomSolder);
            p.Add("CFG2D.LAYEROPACITY.INTERNALPLANE1", Cfg2DLayerOpacityInternalPlane1);
            p.Add("CFG2D.LAYEROPACITY.INTERNALPLANE2", Cfg2DLayerOpacityInternalPlane2);
            p.Add("CFG2D.LAYEROPACITY.INTERNALPLANE3", Cfg2DLayerOpacityInternalPlane3);
            p.Add("CFG2D.LAYEROPACITY.INTERNALPLANE4", Cfg2DLayerOpacityInternalPlane4);
            p.Add("CFG2D.LAYEROPACITY.INTERNALPLANE5", Cfg2DLayerOpacityInternalPlane5);
            p.Add("CFG2D.LAYEROPACITY.INTERNALPLANE6", Cfg2DLayerOpacityInternalPlane6);
            p.Add("CFG2D.LAYEROPACITY.INTERNALPLANE7", Cfg2DLayerOpacityInternalPlane7);
            p.Add("CFG2D.LAYEROPACITY.INTERNALPLANE8", Cfg2DLayerOpacityInternalPlane8);
            p.Add("CFG2D.LAYEROPACITY.INTERNALPLANE9", Cfg2DLayerOpacityInternalPlane9);
            p.Add("CFG2D.LAYEROPACITY.INTERNALPLANE10", Cfg2DLayerOpacityInternalPlane10);
            p.Add("CFG2D.LAYEROPACITY.INTERNALPLANE11", Cfg2DLayerOpacityInternalPlane11);
            p.Add("CFG2D.LAYEROPACITY.INTERNALPLANE12", Cfg2DLayerOpacityInternalPlane12);
            p.Add("CFG2D.LAYEROPACITY.INTERNALPLANE13", Cfg2DLayerOpacityInternalPlane13);
            p.Add("CFG2D.LAYEROPACITY.INTERNALPLANE14", Cfg2DLayerOpacityInternalPlane14);
            p.Add("CFG2D.LAYEROPACITY.INTERNALPLANE15", Cfg2DLayerOpacityInternalPlane15);
            p.Add("CFG2D.LAYEROPACITY.INTERNALPLANE16", Cfg2DLayerOpacityInternalPlane16);
            p.Add("CFG2D.LAYEROPACITY.DRILLGUIDE", Cfg2DLayerOpacityDrillGuide);
            p.Add("CFG2D.LAYEROPACITY.KEEPOUTLAYER", Cfg2DLayerOpacityKeepoutLayer);
            p.Add("CFG2D.LAYEROPACITY.MECHANICAL1", Cfg2DLayerOpacityMechanical1);
            p.Add("CFG2D.LAYEROPACITY.MECHANICAL2", Cfg2DLayerOpacityMechanical2);
            p.Add("CFG2D.LAYEROPACITY.MECHANICAL3", Cfg2DLayerOpacityMechanical3);
            p.Add("CFG2D.LAYEROPACITY.MECHANICAL4", Cfg2DLayerOpacityMechanical4);
            p.Add("CFG2D.LAYEROPACITY.MECHANICAL5", Cfg2DLayerOpacityMechanical5);
            p.Add("CFG2D.LAYEROPACITY.MECHANICAL6", Cfg2DLayerOpacityMechanical6);
            p.Add("CFG2D.LAYEROPACITY.MECHANICAL7", Cfg2DLayerOpacityMechanical7);
            p.Add("CFG2D.LAYEROPACITY.MECHANICAL8", Cfg2DLayerOpacityMechanical8);
            p.Add("CFG2D.LAYEROPACITY.MECHANICAL9", Cfg2DLayerOpacityMechanical9);
            p.Add("CFG2D.LAYEROPACITY.MECHANICAL10", Cfg2DLayerOpacityMechanical10);
            p.Add("CFG2D.LAYEROPACITY.MECHANICAL11", Cfg2DLayerOpacityMechanical11);
            p.Add("CFG2D.LAYEROPACITY.MECHANICAL12", Cfg2DLayerOpacityMechanical12);
            p.Add("CFG2D.LAYEROPACITY.MECHANICAL13", Cfg2DLayerOpacityMechanical13);
            p.Add("CFG2D.LAYEROPACITY.MECHANICAL14", Cfg2DLayerOpacityMechanical14);
            p.Add("CFG2D.LAYEROPACITY.MECHANICAL15", Cfg2DLayerOpacityMechanical15);
            p.Add("CFG2D.LAYEROPACITY.MECHANICAL16", Cfg2DLayerOpacityMechanical16);
            p.Add("CFG2D.LAYEROPACITY.DRILLDRAWING", Cfg2DLayerOpacityDrillDrawing);
            p.Add("CFG2D.LAYEROPACITY.MULTILAYER", Cfg2DLayerOpacityMultiLayer);
            p.Add("CFG2D.LAYEROPACITY.CONNECTLAYER", Cfg2DLayerOpacityCOnNectLayer);
            p.Add("CFG2D.LAYEROPACITY.BACKGROUNDLAYER", Cfg2DLayerOpacityBackGroundLayer);
            p.Add("CFG2D.LAYEROPACITY.DRCERRORLAYER", Cfg2DLayerOpacityDrcErrorLayer);
            p.Add("CFG2D.LAYEROPACITY.HIGHLIGHTLAYER", Cfg2DLayerOpacityHighlightLayer);
            p.Add("CFG2D.LAYEROPACITY.GRIDCOLOR1", Cfg2DLayerOpacityGridColor1);
            p.Add("CFG2D.LAYEROPACITY.GRIDCOLOR10", Cfg2DLayerOpacityGridColor10);
            p.Add("CFG2D.LAYEROPACITY.PADHOLELAYER", Cfg2DLayerOpacityPadHoleLayer);
            p.Add("CFG2D.LAYEROPACITY.VIAHOLELAYER", Cfg2DLayerOpacityViaHoleLayer);
            p.Add("CFG2D.TOGGLELAYERS", Cfg2DToggleLayers);
            p.Add("CFG2D.TOGGLELAYERS.SET", Cfg2DToggleLayersSet);
            p.Add("CFG2D.WORKSPACECOLALPHA0", Cfg2DWorkspaceColAlpha0);
            p.Add("CFG2D.WORKSPACECOLALPHA1", Cfg2DWorkspaceColAlpha1);
            p.Add("CFG2D.WORKSPACECOLALPHA2", Cfg2DWorkspaceColAlpha2);
            p.Add("CFG2D.WORKSPACECOLALPHA3", Cfg2DWorkspaceColAlpha3);
            p.Add("CFG2D.WORKSPACECOLALPHA4", Cfg2DWorkspaceColAlpha4);
            p.Add("CFG2D.WORKSPACECOLALPHA5", Cfg2DWorkspaceColAlpha5);
            p.Add("CFG2D.WORKSPACECOLALPHA6", Cfg2DWorkspaceColAlpha6);
            p.Add("CFG2D.WORKSPACECOLALPHA7", Cfg2DWorkspaceColAlpha7);
            p.Add("CFG2D.WORKSPACECOLALPHA8", Cfg2DWorkspaceColAlpha8);
            p.Add("CFG2D.WORKSPACECOLALPHA9", Cfg2DWorkspaceColAlpha9);
            p.Add("CFG2D.WORKSPACECOLALPHA10", Cfg2DWorkspaceColAlpha10);
            p.Add("CFG2D.WORKSPACECOLALPHA11", Cfg2DWorkspaceColAlpha11);
            p.Add("CFG2D.MECHLAYERINSINGLELAYERMODE", Cfg2DMechLayerInSingleLayerMode);
            p.Add("CFG2D.MECHLAYERINSINGLELAYERMODE.SET", Cfg2DMechLayerInSingleLayerModeSet);
            p.Add("CFG2D.MECHLAYERLINKEDTOSHEET", Cfg2DMechLayerLinkedToSheet);
            p.Add("CFG2D.MECHLAYERLINKEDTOSHEET.SET", Cfg2DMechLayerLinkedToSheetSet);
            p.Add("CFG2D.CURRENTLAYER", Cfg2DCurrentLayer);
            p.Add("CFG2D.DISPLAYSPECIALSTRINGS", Cfg2DDisplaySpecialStrings);
            p.Add("CFG2D.SHOWTESTPOINTS", Cfg2DShowTestPoints);
            p.Add("CFG2D.SHOWORIGINMARKER", Cfg2DShowOriginMarker);
            p.Add("CFG2D.EYEDIST", Cfg2DEyeDist);
            p.Add("CFG2D.SHOWSTATUSINFO", Cfg2DShowStatusInfo);
            p.Add("CFG2D.SHOWPADNETS", Cfg2DShowPadNets);
            p.Add("CFG2D.SHOWPADNUMBERS", Cfg2DShowPadNumberS);
            p.Add("CFG2D.SHOWVIANETS", Cfg2DShowViaNets);
            p.Add("CFG2D.USETRANSPARENTLAYERS", Cfg2DUSetRansparentLayers);
            p.Add("CFG2D.PLANEDRAWMODE", Cfg2DPlaneDrawMode);
            p.Add("CFG2D.DISPLAYNETNAMESONTRACKS", Cfg2DDisplayNetNamesOnTracks);
            p.Add("CFG2D.FROMTOSDISPLAYMODE", Cfg2DFromToSDisplayMode);
            p.Add("CFG2D.PADTYPESDISPLAYMODE", Cfg2DPadTypeSDisplayMode);
            p.Add("CFG2D.SINGLELAYERMODESTATE", Cfg2DSingleLayerModeState);
            p.Add("CFG2D.ORIGINMARKERCOLOR", Cfg2DOriginMarkerColor);
            p.Add("CFG2D.SHOWCOMPONENTREFPOINT", Cfg2DShowComponentRefPoint);
            p.Add("CFG2D.COMPONENTREFPOINTCOLOR", Cfg2DComponentRefPointColor);
            p.Add("CFG2D.POSITIVETOPSOLDERMASK", Cfg2DPosItiveTopSolderMask);
            p.Add("CFG2D.POSITIVEBOTTOMSOLDERMASK", Cfg2DPosItiveBottomSolderMask);
            p.Add("CFG2D.TOPPOSITIVESOLDERMASKALPHA", Cfg2DTopPosItivesolderMaskAlpha);
            p.Add("CFG2D.BOTTOMPOSITIVESOLDERMASKALPHA", Cfg2DBottomPosItivesolderMaskAlpha);
            p.Add("CFG2D.ALLCONNECTIONSINSINGLELAYERMODE", Cfg2DAllCOnNectiOnSInSingleLayerMode);
            p.Add("CFG2D.MULTICOLOREDCONNECTIONS", Cfg2DMultiColorEdcOnNectiOnS);
            p.Add("BOARDINSIGHTVIEWCONFIGURATIONNAME", BoardInsightViewConfigurationName);
            p.Add("VISIBLEGRIDMULTFACTOR", VisibleGridMultFactor);
            p.Add("BIGVISIBLEGRIDMULTFACTOR", BigVisibleGridMultFactor);

            AddParamRecord(p);
            p.Add("CURRENT2D3DVIEWSTATE", Current2D3DViewState);

            AddParamRecord(p);
            p.Add("VP.LX", ViewPort.Location1.X.ToInt32());
            p.Add("VP.HX", ViewPort.Location2.X.ToInt32());
            p.Add("VP.LY", ViewPort.Location1.Y.ToInt32());
            p.Add("VP.HY", ViewPort.Location2.Y.ToInt32());

            AddParamRecord(p);
            p.Add("2DCONFIGTYPE", Property2DConfigType);
            p.Add("2DCONFIGURATION", Property2DConfiguration);
            
            AddParamRecord(p);
            p.Add("2DCONFIGFULLFILENAME", Property2DConfigFullFilename);

            AddParamRecord(p);
            p.Add("3DCONFIGTYPE", Property3DConfigType);
            p.Add("3DCONFIGURATION", Property3DConfiguration);

            AddParamRecord(p);
            p.Add("3DCONFIGFULLFILENAME", Property3DConfigFullFilename);

            AddParamRecord(p);
            p.Add("LOOKAT.X", (double)LookAt.X.ToInt32());
            p.Add("LOOKAT.Y", (double)LookAt.Y.ToInt32());
            p.Add("LOOKAT.Z", (double)LookAt.Z.ToInt32());
            p.Add("EYEROTATION.X", EyeRotationX);
            p.Add("EYEROTATION.Y", EyeRotationY);
            p.Add("EYEROTATION.Z", EyeRotationZ);
            p.Add("ZOOMMULT", ZoomMult);
            p.Add("VIEWSIZE.X", (double)ViewSize.X.ToInt32());
            p.Add("VIEWSIZE.Y", (double)ViewSize.Y.ToInt32());
            p.Add("EGRANGE", EgRange);
            p.Add("EGMULT", EgMult);
            p.Add("EGENABLED", EgEnabled);
            p.Add("EGSNAPTOBOARDOUTLINE", EgSnapToBoardOutline);
            p.Add("EGSNAPTOARCCENTERS", EgSnapToArcCenters);
            p.Add("EGUSEALLLAYERS", EgUseAllLayers);
            p.Add("OGSNAPENABLED", OgSnapEnabled);
            p.Add("MGSNAPENABLED", MgSnapEnabled);
            p.Add("POINTGUIDEENABLED", PointGuideEnabled);
            p.Add("GRIDSNAPENABLED", GridSnapEnabled);
            p.Add("NEAROBJECTSENABLED", NearObjectsEnabled);
            p.Add("FAROBJECTSENABLED", FarObjectsEnabled);
            p.Add("NEAROBJECTSET", NearObjectSet);
            p.Add("FAROBJECTSET", FarObjectSet);
            {
                var (n, f) = Utils.CoordToDxpFrac(NearDistance);
                p.Add("NEARDISTANCE", n);
                p.Add("NEARDISTANCE_FRAC", f);
            }
            p.Add("BOARDVERSION", BoardVersion);
            p.Add("VAULTGUID", VaultGuid);
            p.Add("FOLDERGUID", FolderGuid);
            p.Add("LIFECYCLEDEFINITIONGUID", LifeCycleDefinitionGuid);
            p.Add("REVISIONNAMINGSCHEMEGUID", RevisionNamingSchemeGuid);
        }

        public ParameterCollection ExportToParameters()
        {
            var parameters = new ParameterCollection();
            ExportToParameters(parameters);
            return parameters;
        }
    }
}
