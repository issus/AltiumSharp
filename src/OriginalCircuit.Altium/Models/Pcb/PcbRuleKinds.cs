using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

// Per-rule-kind typed classes for the Rules6 storage (mirroring Altium's design-rule tree). Each
// overrides ReadBody/WriteBody to (de)serialize its kind-specific constraint keys in canonical order,
// on top of the shared common header in PcbRule. Kinds with dynamic per-layer key blocks
// (Width, DiffPairsRouting, RoutingLayers) are not yet modeled and round-trip via the base fallback.

/// <summary>Clearance / BoardOutlineClearance rule (GAP + generic clearance + object-clearance matrix).</summary>
public class PcbClearanceRule : PcbRule
{
    public Coord Gap { get; set; }
    public Coord GenericClearance { get; set; }
    public bool IgnorePadToPadClearanceInFootprint { get; set; }
    /// <summary>Per-object clearance matrix text (empty for the common case).</summary>
    public string ObjectClearances { get; set; } = string.Empty;

    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p)
    {
        Gap = MilV(p, "GAP");
        GenericClearance = MilV(p, "GENERICCLEARANCE");
        IgnorePadToPadClearanceInFootprint = Bv(p, "IGNOREPADTOPADCLEARANCEINFOOTPRINT");
        ObjectClearances = Sv(p, "OBJECTCLEARANCES") ?? string.Empty;
    }
    internal override void WriteBody(Action<string, string> add)
    {
        add("GAP", Mil(Gap));
        add("GENERICCLEARANCE", Mil(GenericClearance));
        add("IGNOREPADTOPADCLEARANCEINFOOTPRINT", Bool(IgnorePadToPadClearanceInFootprint));
        add("OBJECTCLEARANCES", ObjectClearances);
    }
}

/// <summary>ComponentClearance rule.</summary>
public sealed class PcbComponentClearanceRule : PcbRule
{
    public Coord Gap { get; set; }
    public int CollisionCheckMode { get; set; }
    public Coord VerticalGap { get; set; }
    public bool ShowDistances { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p)
    {
        Gap = MilV(p, "GAP");
        CollisionCheckMode = Iv(p, "COLLISIONCHECKMODE");
        VerticalGap = MilV(p, "VERTICALGAP");
        ShowDistances = Bv(p, "SHOWDISTANCES");
    }
    internal override void WriteBody(Action<string, string> add)
    {
        add("GAP", Mil(Gap));
        add("COLLISIONCHECKMODE", CollisionCheckMode.ToString(System.Globalization.CultureInfo.InvariantCulture));
        add("VERTICALGAP", Mil(VerticalGap));
        add("SHOWDISTANCES", Bool(ShowDistances));
    }
}

/// <summary>Shared testpoint geometry for AssemblyTestpoint / FabricationTestpoint.</summary>
public abstract class PcbTestpointRuleBase : PcbRule
{
    public bool TestpointUnderComponent { get; set; }
    public Coord MinSize { get; set; }
    public Coord MaxSize { get; set; }
    public Coord PreferredSize { get; set; }
    public Coord MinHoleSize { get; set; }
    public Coord MaxHoleSize { get; set; }
    public Coord PreferredHoleSize { get; set; }
    public Coord TestpointGrid { get; set; }
    public bool UseGrid { get; set; }
    public Coord GridTolerance { get; set; }
    public bool AllowSideTop { get; set; }
    public bool AllowSideBottom { get; set; }
    internal override bool IsModeled => true;

    private protected void ReadTp(Dictionary<string, string> p)
    {
        TestpointUnderComponent = Bv(p, "TESTPOINTUNDERCOMPONENT");
        MinSize = MilV(p, "MINSIZE"); MaxSize = MilV(p, "MAXSIZE"); PreferredSize = MilV(p, "PREFEREDSIZE");
        MinHoleSize = MilV(p, "MINHOLESIZE"); MaxHoleSize = MilV(p, "MAXHOLESIZE"); PreferredHoleSize = MilV(p, "PREFEREDHOLESIZE");
        TestpointGrid = MilV(p, "TESTPOINTGRID"); UseGrid = Bv(p, "USEGRID"); GridTolerance = MilV(p, "GRIDTOLERANCE");
        AllowSideTop = Bv(p, "ALLOWSIDETOP"); AllowSideBottom = Bv(p, "ALLOWSIDEBOTTOM");
    }
}

/// <summary>AssemblyTestpoint rule.</summary>
public sealed class PcbAssemblyTestpointRule : PcbTestpointRuleBase
{
    internal override void ReadBody(Dictionary<string, string> p) => ReadTp(p);
    internal override void WriteBody(Action<string, string> add)
    {
        add("TESTPOINTUNDERCOMPONENT", Bool(TestpointUnderComponent));
        add("MINSIZE", Mil(MinSize)); add("MAXSIZE", Mil(MaxSize)); add("PREFEREDSIZE", Mil(PreferredSize));
        add("MINHOLESIZE", Mil(MinHoleSize)); add("MAXHOLESIZE", Mil(MaxHoleSize)); add("PREFEREDHOLESIZE", Mil(PreferredHoleSize));
        add("TESTPOINTGRID", Mil(TestpointGrid)); add("USEGRID", Bool(UseGrid)); add("GRIDTOLERANCE", Mil(GridTolerance));
        add("ALLOWSIDETOP", Bool(AllowSideTop)); add("ALLOWSIDEBOTTOM", Bool(AllowSideBottom));
    }
}

/// <summary>FabricationTestpoint rule (testpoint geometry + SIDE; different key order than assembly).</summary>
public sealed class PcbFabricationTestpointRule : PcbTestpointRuleBase
{
    public int Side { get; set; }
    internal override void ReadBody(Dictionary<string, string> p) { Side = Iv(p, "SIDE"); ReadTp(p); }
    internal override void WriteBody(Action<string, string> add)
    {
        add("SIDE", Side.ToString(System.Globalization.CultureInfo.InvariantCulture));
        add("TESTPOINTUNDERCOMPONENT", Bool(TestpointUnderComponent));
        add("MINSIZE", Mil(MinSize)); add("MAXSIZE", Mil(MaxSize)); add("PREFEREDSIZE", Mil(PreferredSize));
        add("MINHOLESIZE", Mil(MinHoleSize)); add("MAXHOLESIZE", Mil(MaxHoleSize)); add("PREFEREDHOLESIZE", Mil(PreferredHoleSize));
        add("TESTPOINTGRID", Mil(TestpointGrid));
        add("ALLOWSIDETOP", Bool(AllowSideTop)); add("ALLOWSIDEBOTTOM", Bool(AllowSideBottom));
        add("USEGRID", Bool(UseGrid)); add("GRIDTOLERANCE", Mil(GridTolerance));
    }
}

/// <summary>AssemblyTestPointUsage / SilkToBoardRegionClearance / UnpouredPolygon / UnRoutedNet — header only.</summary>
public sealed class PcbHeaderOnlyRule : PcbRule
{
    internal override bool IsModeled => true;
}

/// <summary>FabricationTestPointUsage rule.</summary>
public sealed class PcbFabricationTestPointUsageRule : PcbRule
{
    public int Valid { get; set; }
    public bool AllowMultiple { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) { Valid = Iv(p, "VALID"); AllowMultiple = Bv(p, "ALLOWMULTIPLE"); }
    internal override void WriteBody(Action<string, string> add)
    {
        add("VALID", Valid.ToString(System.Globalization.CultureInfo.InvariantCulture));
        add("ALLOWMULTIPLE", Bool(AllowMultiple));
    }
}

/// <summary>FanoutControl rule.</summary>
public sealed class PcbFanoutControlRule : PcbRule
{
    public string BgaDir { get; set; } = "Out";
    public string BgaViaMode { get; set; } = "Centered";
    public string FanoutStyle { get; set; } = "Auto";
    public string FanoutDirection { get; set; } = "Alternating";
    public Coord ViaGrid { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p)
    {
        BgaDir = Sv(p, "BGADIR") ?? BgaDir; BgaViaMode = Sv(p, "BGAVIAMODE") ?? BgaViaMode;
        FanoutStyle = Sv(p, "FANOUTSTYLE") ?? FanoutStyle; FanoutDirection = Sv(p, "FANOUTDIRECTION") ?? FanoutDirection;
        ViaGrid = MilV(p, "VIAGRID");
    }
    internal override void WriteBody(Action<string, string> add)
    {
        add("BGADIR", BgaDir); add("BGAVIAMODE", BgaViaMode); add("FANOUTSTYLE", FanoutStyle);
        add("FANOUTDIRECTION", FanoutDirection); add("VIAGRID", Mil(ViaGrid));
    }
}

/// <summary>Height rule.</summary>
public sealed class PcbHeightRule : PcbRule
{
    public Coord MinHeight { get; set; }
    public Coord MaxHeight { get; set; }
    public Coord PreferredHeight { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) { MinHeight = MilV(p, "MINHEIGHT"); MaxHeight = MilV(p, "MAXHEIGHT"); PreferredHeight = MilV(p, "PREFHEIGHT"); }
    internal override void WriteBody(Action<string, string> add) { add("MINHEIGHT", Mil(MinHeight)); add("MAXHEIGHT", Mil(MaxHeight)); add("PREFHEIGHT", Mil(PreferredHeight)); }
}

/// <summary>HoleSize rule.</summary>
public sealed class PcbHoleSizeRule : PcbRule
{
    public bool AbsoluteValues { get; set; }
    public Coord MaxLimit { get; set; }
    public Coord MinLimit { get; set; }
    public double MaxPercent { get; set; }
    public double MinPercent { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p)
    {
        AbsoluteValues = Bv(p, "ABSOLUTEVALUES"); MaxLimit = MilV(p, "MAXLIMIT"); MinLimit = MilV(p, "MINLIMIT");
        MaxPercent = Dv(p, "MAXPERCENT"); MinPercent = Dv(p, "MINPERCENT");
    }
    internal override void WriteBody(Action<string, string> add)
    {
        add("ABSOLUTEVALUES", Bool(AbsoluteValues)); add("MAXLIMIT", Mil(MaxLimit)); add("MINLIMIT", Mil(MinLimit));
        add("MAXPERCENT", F3(MaxPercent)); add("MINPERCENT", F3(MinPercent));
    }
}

/// <summary>HoleToHoleClearance rule.</summary>
public sealed class PcbHoleToHoleClearanceRule : PcbRule
{
    public Coord Gap { get; set; }
    public bool AllowStackedMicrovias { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) { Gap = MilV(p, "GAP"); AllowStackedMicrovias = Bv(p, "ALLOWSTACKEDMICROVIAS"); }
    internal override void WriteBody(Action<string, string> add) { add("GAP", Mil(Gap)); add("ALLOWSTACKEDMICROVIAS", Bool(AllowStackedMicrovias)); }
}

/// <summary>LayerPairs rule.</summary>
public sealed class PcbLayerPairsRule : PcbRule
{
    public bool Enforce { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) => Enforce = Bv(p, "ENFORCE");
    internal override void WriteBody(Action<string, string> add) => add("ENFORCE", Bool(Enforce));
}

/// <summary>Length rule.</summary>
public sealed class PcbLengthRule : PcbRule
{
    public Coord MaxLimit { get; set; }
    public Coord MinLimit { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) { MaxLimit = MilV(p, "MAXLIMIT"); MinLimit = MilV(p, "MINLIMIT"); }
    internal override void WriteBody(Action<string, string> add) { add("MAXLIMIT", Mil(MaxLimit)); add("MINLIMIT", Mil(MinLimit)); }
}

/// <summary>MatchedLengths rule.</summary>
public sealed class PcbMatchedLengthsRule : PcbRule
{
    public Coord Tolerance { get; set; }
    public bool CheckNetsInDiffPair { get; set; }
    public bool CheckDiffPairVsDiffPair { get; set; }
    public bool CheckXSignals { get; set; }
    public bool CheckOthers { get; set; }
    public bool UseDelayUnits { get; set; }
    public double DelayTolerance { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p)
    {
        Tolerance = MilV(p, "TOLERANCE"); CheckNetsInDiffPair = Bv(p, "CHECKNETSINDIFFPAIR");
        CheckDiffPairVsDiffPair = Bv(p, "CHECKDIFFPAIRVSDIFFPAIR"); CheckXSignals = Bv(p, "CHECKXSIGNALS");
        CheckOthers = Bv(p, "CHECKOTHERS"); UseDelayUnits = Bv(p, "USEDELAYUNITS"); DelayTolerance = Dv(p, "DELAYTOLERANCE");
    }
    internal override void WriteBody(Action<string, string> add)
    {
        add("TOLERANCE", Mil(Tolerance)); add("CHECKNETSINDIFFPAIR", Bool(CheckNetsInDiffPair));
        add("CHECKDIFFPAIRVSDIFFPAIR", Bool(CheckDiffPairVsDiffPair)); add("CHECKXSIGNALS", Bool(CheckXSignals));
        add("CHECKOTHERS", Bool(CheckOthers)); add("USEDELAYUNITS", Bool(UseDelayUnits)); add("DELAYTOLERANCE", F6(DelayTolerance));
    }
}

/// <summary>MinimumSolderMaskSliver rule.</summary>
public sealed class PcbMinimumSolderMaskSliverRule : PcbRule
{
    public Coord MinSolderMaskWidth { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) => MinSolderMaskWidth = MilV(p, "MINSOLDERMASKWIDTH");
    internal override void WriteBody(Action<string, string> add) => add("MINSOLDERMASKWIDTH", Mil(MinSolderMaskWidth));
}

/// <summary>NetAntennae rule.</summary>
public sealed class PcbNetAntennaeRule : PcbRule
{
    public Coord Tolerance { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) => Tolerance = MilV(p, "NETANTENNAETOLERANCE");
    internal override void WriteBody(Action<string, string> add) => add("NETANTENNAETOLERANCE", Mil(Tolerance));
}

/// <summary>PasteMaskExpansion rule.</summary>
public sealed class PcbPasteMaskExpansionRule : PcbRule
{
    public Coord Expansion { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) => Expansion = MilV(p, "EXPANSION");
    internal override void WriteBody(Action<string, string> add) => add("EXPANSION", Mil(Expansion));
}

/// <summary>PlaneClearance rule.</summary>
public sealed class PcbPlaneClearanceRule : PcbRule
{
    public Coord Clearance { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) => Clearance = MilV(p, "CLEARANCE");
    internal override void WriteBody(Action<string, string> add) => add("CLEARANCE", Mil(Clearance));
}

/// <summary>PlaneConnect rule.</summary>
public sealed class PcbPlaneConnectRule : PcbRule
{
    public string PlaneConnectStyle { get; set; } = "Relief";
    public Coord ReliefExpansion { get; set; }
    public int ReliefEntries { get; set; }
    public Coord ReliefConductorWidth { get; set; }
    public Coord ReliefAirGap { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p)
    {
        PlaneConnectStyle = Sv(p, "PLANECONNECTSTYLE") ?? PlaneConnectStyle; ReliefExpansion = MilV(p, "RELIEFEXPANSION");
        ReliefEntries = Iv(p, "RELIEFENTRIES"); ReliefConductorWidth = MilV(p, "RELIEFCONDUCTORWIDTH"); ReliefAirGap = MilV(p, "RELIEFAIRGAP");
    }
    internal override void WriteBody(Action<string, string> add)
    {
        add("PLANECONNECTSTYLE", PlaneConnectStyle); add("RELIEFEXPANSION", Mil(ReliefExpansion));
        add("RELIEFENTRIES", ReliefEntries.ToString(System.Globalization.CultureInfo.InvariantCulture));
        add("RELIEFCONDUCTORWIDTH", Mil(ReliefConductorWidth)); add("RELIEFAIRGAP", Mil(ReliefAirGap));
    }
}

/// <summary>PolygonConnect rule.</summary>
public sealed class PcbPolygonConnectRule : PcbRule
{
    public string ConnectStyle { get; set; } = "Direct";
    public Coord ReliefConductorWidth { get; set; }
    public int ReliefEntries { get; set; }
    public string PolygonReliefAngle { get; set; } = "90 Angle";
    public Coord AirGapWidth { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p)
    {
        ConnectStyle = Sv(p, "CONNECTSTYLE") ?? ConnectStyle; ReliefConductorWidth = MilV(p, "RELIEFCONDUCTORWIDTH");
        ReliefEntries = Iv(p, "RELIEFENTRIES"); PolygonReliefAngle = Sv(p, "POLYGONRELIEFANGLE") ?? PolygonReliefAngle; AirGapWidth = MilV(p, "AIRGAPWIDTH");
    }
    internal override void WriteBody(Action<string, string> add)
    {
        add("CONNECTSTYLE", ConnectStyle); add("RELIEFCONDUCTORWIDTH", Mil(ReliefConductorWidth));
        add("RELIEFENTRIES", ReliefEntries.ToString(System.Globalization.CultureInfo.InvariantCulture));
        add("POLYGONRELIEFANGLE", PolygonReliefAngle); add("AIRGAPWIDTH", Mil(AirGapWidth));
    }
}

/// <summary>RoutingCorners rule.</summary>
public sealed class PcbRoutingCornersRule : PcbRule
{
    public string CornerStyle { get; set; } = "45-Degree";
    public Coord MinSetback { get; set; }
    public Coord MaxSetback { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) { CornerStyle = Sv(p, "CORNERSTYLE") ?? CornerStyle; MinSetback = MilV(p, "MINSETBACK"); MaxSetback = MilV(p, "MAXSETBACK"); }
    internal override void WriteBody(Action<string, string> add) { add("CORNERSTYLE", CornerStyle); add("MINSETBACK", Mil(MinSetback)); add("MAXSETBACK", Mil(MaxSetback)); }
}

/// <summary>RoutingPriority rule.</summary>
public sealed class PcbRoutingPriorityRule : PcbRule
{
    public int RoutingPriorityValue { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) => RoutingPriorityValue = Iv(p, "ROUTINGPRIORITY");
    internal override void WriteBody(Action<string, string> add) => add("ROUTINGPRIORITY", RoutingPriorityValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
}

/// <summary>RoutingTopology rule.</summary>
public sealed class PcbRoutingTopologyRule : PcbRule
{
    public string Topology { get; set; } = "Shortest";
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) => Topology = Sv(p, "TOPOLOGY") ?? Topology;
    internal override void WriteBody(Action<string, string> add) => add("TOPOLOGY", Topology);
}

/// <summary>RoutingVias rule.</summary>
public sealed class PcbRoutingViasRule : PcbRule
{
    public Coord HoleWidth { get; set; }
    public Coord Width { get; set; }
    public string ViaStyle { get; set; } = "Through Hole";
    public Coord MinHoleWidth { get; set; }
    public Coord MinWidth { get; set; }
    public Coord MaxHoleWidth { get; set; }
    public Coord MaxWidth { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p)
    {
        HoleWidth = MilV(p, "HOLEWIDTH"); Width = MilV(p, "WIDTH"); ViaStyle = Sv(p, "VIASTYLE") ?? ViaStyle;
        MinHoleWidth = MilV(p, "MINHOLEWIDTH"); MinWidth = MilV(p, "MINWIDTH"); MaxHoleWidth = MilV(p, "MAXHOLEWIDTH"); MaxWidth = MilV(p, "MAXWIDTH");
    }
    internal override void WriteBody(Action<string, string> add)
    {
        add("HOLEWIDTH", Mil(HoleWidth)); add("WIDTH", Mil(Width)); add("VIASTYLE", ViaStyle);
        add("MINHOLEWIDTH", Mil(MinHoleWidth)); add("MINWIDTH", Mil(MinWidth)); add("MAXHOLEWIDTH", Mil(MaxHoleWidth)); add("MAXWIDTH", Mil(MaxWidth));
    }
}

/// <summary>ShortCircuit rule.</summary>
public sealed class PcbShortCircuitRule : PcbRule
{
    public bool Allowed { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) => Allowed = Bv(p, "ALLOWED");
    internal override void WriteBody(Action<string, string> add) => add("ALLOWED", Bool(Allowed));
}

/// <summary>SignalStimulus rule (time values carry unit suffixes; stored verbatim as text).</summary>
public sealed class PcbSignalStimulusRule : PcbRule
{
    public int Kind { get; set; }
    public int Level { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string StopTime { get; set; } = string.Empty;
    public string PeriodTime { get; set; } = string.Empty;
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p)
    {
        Kind = Iv(p, "KIND"); Level = Iv(p, "LEVEL");
        StartTime = Sv(p, "STARTTIME") ?? string.Empty; StopTime = Sv(p, "STOPTIME") ?? string.Empty; PeriodTime = Sv(p, "PERIODTIME") ?? string.Empty;
    }
    internal override void WriteBody(Action<string, string> add)
    {
        add("KIND", Kind.ToString(System.Globalization.CultureInfo.InvariantCulture));
        add("LEVEL", Level.ToString(System.Globalization.CultureInfo.InvariantCulture));
        add("STARTTIME", StartTime); add("STOPTIME", StopTime); add("PERIODTIME", PeriodTime);
    }
}

/// <summary>SilkToSilkClearance rule.</summary>
public sealed class PcbSilkToSilkClearanceRule : PcbRule
{
    public Coord Clearance { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) => Clearance = MilV(p, "SILKTOSILKCLEARANCE");
    internal override void WriteBody(Action<string, string> add) => add("SILKTOSILKCLEARANCE", Mil(Clearance));
}

/// <summary>SilkToSolderMaskClearance rule.</summary>
public sealed class PcbSilkToSolderMaskClearanceRule : PcbRule
{
    public Coord MinSilkScreenToMaskGap { get; set; }
    public bool ClearanceToExposedCopper { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) { MinSilkScreenToMaskGap = MilV(p, "MINSILKSCREENTOMASKGAP"); ClearanceToExposedCopper = Bv(p, "CLEARANCETOEXPOSEDCOPPER"); }
    internal override void WriteBody(Action<string, string> add) { add("MINSILKSCREENTOMASKGAP", Mil(MinSilkScreenToMaskGap)); add("CLEARANCETOEXPOSEDCOPPER", Bool(ClearanceToExposedCopper)); }
}

/// <summary>SolderMaskExpansion rule.</summary>
public sealed class PcbSolderMaskExpansionRule : PcbRule
{
    public Coord Expansion { get; set; }
    public bool IsTentingTop { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) { Expansion = MilV(p, "EXPANSION"); IsTentingTop = Bv(p, "ISTENTINGTOP"); }
    internal override void WriteBody(Action<string, string> add) { add("EXPANSION", Mil(Expansion)); add("ISTENTINGTOP", Bool(IsTentingTop)); }
}

/// <summary>Width rule: width limits + a dynamic per-layer width block + impedance profile.</summary>
public sealed class PcbWidthRule : PcbRule
{
    public Coord MaxLimit { get; set; }
    public Coord MinLimit { get; set; }
    public Coord PreferredWidth { get; set; }
    /// <summary>Per-layer width keys (e.g. <c>TOPLAYER_MINWIDTH</c>) in source order.</summary>
    public List<KeyValuePair<string, Coord>> LayerWidths { get; } = new();
    /// <summary>Whether the impedance-profile block (<c>IMPEDANCEPROFILEDRIVEN/ID/VALUE</c>) is present (optional).</summary>
    public bool HasImpedanceProfile { get; set; }
    public bool ImpedanceProfileDriven { get; set; }
    public string ImpedanceProfileId { get; set; } = string.Empty;
    public double ImpedanceProfileValue { get; set; }
    /// <summary>Whether the impedance min/max/favored block (<c>MINIMP/MAXIMP/FAVIMP</c>) is present.
    /// Altium writes this independently of the impedance-profile block — a rule can carry these three
    /// without the profile keys, so presence is tracked separately for byte-exact round-trip.</summary>
    public bool HasImpedance { get; set; }
    public double MinImpedance { get; set; }
    public double MaxImpedance { get; set; }
    public double FavoredImpedance { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p)
    {
        MaxLimit = MilV(p, "MAXLIMIT"); MinLimit = MilV(p, "MINLIMIT"); PreferredWidth = MilV(p, "PREFEREDWIDTH");
        HasImpedanceProfile = p.ContainsKey("IMPEDANCEPROFILEDRIVEN");
        ImpedanceProfileDriven = Bv(p, "IMPEDANCEPROFILEDRIVEN"); ImpedanceProfileId = Sv(p, "IMPEDANCEPROFILEID") ?? string.Empty;
        ImpedanceProfileValue = Dv(p, "IMPEDANCEPROFILEVALUE");
        HasImpedance = p.ContainsKey("MINIMP");
        MinImpedance = Dv(p, "MINIMP"); MaxImpedance = Dv(p, "MAXIMP"); FavoredImpedance = Dv(p, "FAVIMP");
    }
    internal override void ReadOrdered(List<KeyValuePair<string, string>> ordered)
    {
        foreach (var (key, value) in ordered)
            if (IsLayerWidthKey(key)) LayerWidths.Add(new KeyValuePair<string, Coord>(key, MilOf(value)));
    }
    internal override void WriteBody(Action<string, string> add)
    {
        add("MAXLIMIT", Mil(MaxLimit)); add("MINLIMIT", Mil(MinLimit)); add("PREFEREDWIDTH", Mil(PreferredWidth));
        foreach (var (k, v) in LayerWidths) add(k, Mil(v));
        if (HasImpedanceProfile)
        {
            add("IMPEDANCEPROFILEDRIVEN", Bool(ImpedanceProfileDriven));
            add("IMPEDANCEPROFILEID", ImpedanceProfileId);
            add("IMPEDANCEPROFILEVALUE", F6(ImpedanceProfileValue));
        }
        if (HasImpedance)
        {
            add("MINIMP", F6(MinImpedance)); add("MAXIMP", F6(MaxImpedance)); add("FAVIMP", F6(FavoredImpedance));
        }
    }
}

/// <summary>DiffPairsRouting rule: limits + a dynamic per-layer gap/width block + impedance.</summary>
public sealed class PcbDiffPairsRoutingRule : PcbRule
{
    public Coord MaxLimit { get; set; }
    public Coord MinLimit { get; set; }
    public Coord MostFreqGap { get; set; }
    /// <summary>Per-layer gap/width keys (e.g. <c>TOPLAYER_MAXGAP</c>) in source order.</summary>
    public List<KeyValuePair<string, Coord>> LayerValues { get; } = new();
    public bool HasImpedanceProfile { get; set; }
    public bool ImpedanceProfileDriven { get; set; }
    public string ImpedanceProfileId { get; set; } = string.Empty;
    public double ImpedanceProfileValue { get; set; }
    public bool HasMaxUncoupledLength { get; set; }
    public Coord MaxUncoupledLength { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p)
    {
        MaxLimit = MilV(p, "MAXLIMIT"); MinLimit = MilV(p, "MINLIMIT"); MostFreqGap = MilV(p, "MOSTFREQGAP");
        HasImpedanceProfile = p.ContainsKey("IMPEDANCEPROFILEDRIVEN");
        ImpedanceProfileDriven = Bv(p, "IMPEDANCEPROFILEDRIVEN"); ImpedanceProfileId = Sv(p, "IMPEDANCEPROFILEID") ?? string.Empty;
        ImpedanceProfileValue = Dv(p, "IMPEDANCEPROFILEVALUE");
        HasMaxUncoupledLength = p.ContainsKey("MAXUNCOUPLEDLENGTH"); MaxUncoupledLength = MilV(p, "MAXUNCOUPLEDLENGTH");
    }
    internal override void ReadOrdered(List<KeyValuePair<string, string>> ordered)
    {
        foreach (var (key, value) in ordered)
            if (IsLayerWidthKey(key)) LayerValues.Add(new KeyValuePair<string, Coord>(key, MilOf(value)));
    }
    internal override void WriteBody(Action<string, string> add)
    {
        add("MAXLIMIT", Mil(MaxLimit)); add("MINLIMIT", Mil(MinLimit)); add("MOSTFREQGAP", Mil(MostFreqGap));
        foreach (var (k, v) in LayerValues) add(k, Mil(v));
        if (HasImpedanceProfile)
        {
            add("IMPEDANCEPROFILEDRIVEN", Bool(ImpedanceProfileDriven));
            add("IMPEDANCEPROFILEID", ImpedanceProfileId);
            add("IMPEDANCEPROFILEVALUE", F6(ImpedanceProfileValue));
        }
        if (HasMaxUncoupledLength) add("MAXUNCOUPLEDLENGTH", Mil(MaxUncoupledLength));
    }
}

/// <summary>RoutingLayers rule: per-layer routability flags (the dynamic <c>{layer}_V5</c> keys).</summary>
public sealed class PcbRoutingLayersRule : PcbRule
{
    /// <summary>Per-layer routing-enabled flags in source order (layer name → enabled).</summary>
    public List<KeyValuePair<string, bool>> LayerEnabled { get; } = new();
    internal override bool IsModeled => true;
    internal override void ReadOrdered(List<KeyValuePair<string, string>> ordered)
    {
        foreach (var (key, value) in ordered)
            if (key.EndsWith("_V5", StringComparison.OrdinalIgnoreCase))
                LayerEnabled.Add(new KeyValuePair<string, bool>(key[..^3], value.Equals("TRUE", StringComparison.OrdinalIgnoreCase)));
    }
    internal override void WriteBody(Action<string, string> add)
    {
        foreach (var (layer, enabled) in LayerEnabled)
            add(layer + "_V5", Bool(enabled));
    }
}

/// <summary>SupplyNets rule.</summary>
public sealed class PcbSupplyNetsRule : PcbRule
{
    public double Voltage { get; set; }
    internal override bool IsModeled => true;
    internal override void ReadBody(Dictionary<string, string> p) => Voltage = Dv(p, "VOLTAGE");
    internal override void WriteBody(Action<string, string> add) => add("VOLTAGE", Volt(Voltage));
}
