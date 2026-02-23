{==============================================================================
  AltiumSharp File to JSON Converter

  Converts existing Altium files (SchLib, PcbLib, SchDoc, PcbDoc) to JSON format.

  Batch Conversion (Recommended):
  1. Place Altium files in D:\src\AltiumSharp\TestData\ directory
  2. Run: RunConvertAllInDirectory
  3. JSON files are created alongside the source files

  Supported file types for batch conversion:
     - .SchLib (Schematic Library)
     - .PcbLib (PCB Footprint Library)
     - .SchDoc (Schematic Document)
     - .PcbDoc (PCB Board Document)

  Manual Single-File Export:
  1. Open the file you want to convert in Altium Designer
  2. Run the appropriate procedure:
     - RunExportCurrentSchLib - for .SchLib files
     - RunExportCurrentPcbLib - for .PcbLib files
     - RunExportCurrentSchDoc - for .SchDoc files
     - RunExportCurrentPcbDoc - for .PcbDoc files

  Debug/Info:
     - RunShowCurrentDocInfo - shows info about currently open documents
     - RunExportAllOpenDocuments - exports all documents currently open in editor
==============================================================================}

const
    // Source directory containing files to convert
    SOURCE_DIR = 'D:\src\AltiumSharp\TestData\';

    // Coordinate conversion
    MILS_TO_INTERNAL = 10000;
    MM_TO_INTERNAL = 393701;

    // JSON formatting
    JSON_INDENT = '  ';

var
    JsonOutput: TStringList;
    IndentLevel: Integer;

{==============================================================================
  JSON HELPER FUNCTIONS
==============================================================================}

procedure JsonBegin;
begin
    JsonOutput := TStringList.Create;
    IndentLevel := 0;
end;

procedure JsonEnd(FileName: String);
begin
    JsonOutput.SaveToFile(FileName);
    JsonOutput.Free;
end;

function GetIndent: String;
var
    I: Integer;
begin
    Result := '';
    for I := 1 to IndentLevel do
        Result := Result + JSON_INDENT;
end;

procedure JsonWriteLine(Line: String);
begin
    JsonOutput.Add(GetIndent + Line);
end;

procedure JsonOpenObject(Name: String);
begin
    if Name <> '' then
        JsonWriteLine('"' + Name + '": {')
    else
        JsonWriteLine('{');
    Inc(IndentLevel);
end;

procedure JsonCloseObject(AddComma: Boolean);
begin
    Dec(IndentLevel);
    if AddComma then
        JsonWriteLine('},')
    else
        JsonWriteLine('}');
end;

procedure JsonOpenArray(Name: String);
begin
    JsonWriteLine('"' + Name + '": [');
    Inc(IndentLevel);
end;

procedure JsonCloseArray(AddComma: Boolean);
var
    LastLine: String;
    LastIdx: Integer;
begin
    if JsonOutput.Count > 0 then
    begin
        LastIdx := JsonOutput.Count - 1;
        LastLine := JsonOutput[LastIdx];
        if (Length(LastLine) > 0) and (LastLine[Length(LastLine)] = ',') then
            JsonOutput[LastIdx] := Copy(LastLine, 1, Length(LastLine) - 1);
    end;
    Dec(IndentLevel);
    if AddComma then
        JsonWriteLine('],')
    else
        JsonWriteLine(']');
end;

function IsConvertibleVariant(V: Variant): Boolean;
var
    VT: Integer;
begin
    VT := VarType(V);
    // Dispatch=9, Unknown=13 are object references that can't be converted to strings
    Result := (VT <> 9) and (VT <> 13);
end;

procedure JsonWriteString(Name: String; Value: Variant; AddComma: Boolean);
var
    EscapedValue, StrValue: String;
    I: Integer;
    Ch: Char;
begin
    if not IsConvertibleVariant(Value) then
        StrValue := '(object ref)'
    else if VarIsNull(Value) or VarIsEmpty(Value) then
        StrValue := ''
    else
        StrValue := VarToStr(Value);

    // Escape special characters in JSON string manually
    EscapedValue := '';
    for I := 1 to Length(StrValue) do
    begin
        Ch := StrValue[I];
        case Ch of
            '\': EscapedValue := EscapedValue + '\\';
            '"': EscapedValue := EscapedValue + '\"';
            #13: EscapedValue := EscapedValue + '\r';
            #10: EscapedValue := EscapedValue + '\n';
            #9:  EscapedValue := EscapedValue + '\t';
        else
            EscapedValue := EscapedValue + Ch;
        end;
    end;

    if AddComma then
        JsonWriteLine('"' + Name + '": "' + EscapedValue + '",')
    else
        JsonWriteLine('"' + Name + '": "' + EscapedValue + '"');
end;

procedure JsonWriteInteger(Name: String; Value: Variant; AddComma: Boolean);
var
    Line: String;
begin
    if not IsConvertibleVariant(Value) or VarIsNull(Value) or VarIsEmpty(Value) then
        Line := '"' + Name + '": null'
    else
        Line := '"' + Name + '": ' + VarToStr(Value);
    if AddComma then Line := Line + ',';
    JsonWriteLine(Line);
end;

procedure JsonWriteFloat(Name: String; Value: Variant; AddComma: Boolean);
var
    FloatStr: String;
    I: Integer;
begin
    if not IsConvertibleVariant(Value) or VarIsNull(Value) or VarIsEmpty(Value) then
        FloatStr := 'null'
    else
    begin
        FloatStr := FloatToStr(Value);
        // Replace comma with period in case of non-US locale
        for I := 1 to Length(FloatStr) do
            if FloatStr[I] = ',' then FloatStr[I] := '.';
    end;

    if AddComma then
        JsonWriteLine('"' + Name + '": ' + FloatStr + ',')
    else
        JsonWriteLine('"' + Name + '": ' + FloatStr);
end;

procedure JsonWriteBoolean(Name: String; Value: Variant; AddComma: Boolean);
var
    Line, BoolStr: String;
begin
    if not IsConvertibleVariant(Value) or VarIsNull(Value) or VarIsEmpty(Value) then
        BoolStr := 'null'
    else if Value then
        BoolStr := 'true'
    else
        BoolStr := 'false';
    Line := '"' + Name + '": ' + BoolStr;
    if AddComma then Line := Line + ',';
    JsonWriteLine(Line);
end;

procedure JsonWriteCoord(Name: String; Value: TCoord; AddComma: Boolean);
begin
    JsonOpenObject(Name);
    JsonWriteInteger('internal', Value, True);
    JsonWriteFloat('mils', Value / MILS_TO_INTERNAL, True);
    JsonWriteFloat('mm', Value / MM_TO_INTERNAL, False);
    JsonCloseObject(AddComma);
end;

{==============================================================================
  PCB BASE PRIMITIVE PROPERTIES HELPER
==============================================================================}

{ Helper procedure to export common base primitive properties shared by all PCB objects.
  These properties come from IPCB_Primitive interface. Call this at the END of each
  export procedure, just before closing the object. }
procedure ExportPcbBasePrimitiveProperties(Prim: IPCB_Primitive);
begin
    // Object identification
    JsonWriteInteger('objectId', Prim.ObjectId, True);
    JsonWriteString('objectIDString', Prim.ObjectIDString, True);
    JsonWriteString('identifier', Prim.Identifier, True);
    JsonWriteString('handle', Prim.Handle, True);
    JsonWriteString('uniqueId', Prim.UniqueId, True);
    JsonWriteString('descriptor', Prim.Descriptor, True);
    JsonWriteString('detail', Prim.Detail, True);

    // Containment flags
    JsonWriteBoolean('inBoard', Prim.InBoard, True);
    JsonWriteBoolean('inComponent', Prim.InComponent, True);
    JsonWriteBoolean('inCoordinate', Prim.InCoordinate, True);
    JsonWriteBoolean('inDimension', Prim.InDimension, True);
    JsonWriteBoolean('inNet', Prim.InNet, True);
    JsonWriteBoolean('inPolygon', Prim.InPolygon, True);

    // State flags
    JsonWriteBoolean('enabled', Prim.Enabled, True);
    JsonWriteBoolean('used', Prim.Used, True);
    JsonWriteBoolean('selected', Prim.Selected, True);
    JsonWriteBoolean('moveable', Prim.Moveable, True);
    JsonWriteBoolean('allowGlobalEdit', Prim.AllowGlobalEdit, True);

    // DRC and routing
    JsonWriteBoolean('drcError', Prim.DRCError, True);
    JsonWriteBoolean('isPreRoute', Prim.IsPreRoute, True);
    JsonWriteBoolean('userRouted', Prim.UserRouted, True);

    // Electrical properties
    JsonWriteBoolean('isElectricalPrim', Prim.IsElectricalPrim, True);
    JsonWriteBoolean('isKeepout', Prim.IsKeepout, True);

    // Testpoint flags
    JsonWriteBoolean('isAssyTestpoint_Top', Prim.IsAssyTestpoint_Top, True);
    JsonWriteBoolean('isAssyTestpoint_Bottom', Prim.IsAssyTestpoint_Bottom, True);

    // Polygon and misc flags
    JsonWriteBoolean('polygonOutline', Prim.PolygonOutline, True);
    JsonWriteBoolean('tearDrop', Prim.TearDrop, True);
    JsonWriteBoolean('padCacheRobotFlag', Prim.PadCacheRobotFlag, True);
    JsonWriteBoolean('miscFlag1', Prim.MiscFlag1, True);
    JsonWriteBoolean('miscFlag2', Prim.MiscFlag2, True);
    JsonWriteBoolean('miscFlag3', Prim.MiscFlag3, True);

    // Union and layer
    JsonWriteInteger('unionIndex', Prim.UnionIndex, True);
    JsonWriteInteger('layer', Prim.Layer, True);
    JsonWriteInteger('layer_V6', Prim.Layer_V6, True);
    JsonWriteInteger('viewableObjectId', Prim.ViewableObjectID, True);

    // Extended primitive properties
    JsonWriteBoolean('drawAsPreview', Prim.DrawAsPreview, True);
    JsonWriteBoolean('enableDraw', Prim.EnableDraw, True);
    JsonWriteBoolean('enabled_Direct', Prim.Enabled_Direct, True);
    JsonWriteBoolean('enabled_vComponent', Prim.Enabled_vComponent, True);
    JsonWriteBoolean('enabled_vCoordinate', Prim.Enabled_vCoordinate, True);
    JsonWriteBoolean('enabled_vDimension', Prim.Enabled_vDimension, True);
    JsonWriteBoolean('enabled_vNet', Prim.Enabled_vNet, True);
    JsonWriteBoolean('enabled_vPolygon', Prim.Enabled_vPolygon, True);
    JsonWriteInteger('index', Prim.Index, True);

    // Tenting and testpoint flags (extended)
    JsonWriteBoolean('isTenting', Prim.IsTenting, True);
    JsonWriteBoolean('isTenting_Top', Prim.IsTenting_Top, True);
    JsonWriteBoolean('isTenting_Bottom', Prim.IsTenting_Bottom, True);
    JsonWriteBoolean('isTestpoint_Top', Prim.IsTestpoint_Top, True);
    JsonWriteBoolean('isTestpoint_Bottom', Prim.IsTestpoint_Bottom, True);

    // Mask and power plane properties
    JsonWriteCoord('pasteMaskExpansion', Prim.PasteMaskExpansion, True);
    JsonWriteCoord('solderMaskExpansion', Prim.SolderMaskExpansion, True);
    JsonWriteCoord('powerPlaneClearance', Prim.PowerPlaneClearance, True);
    JsonWriteInteger('powerPlaneConnectStyle', Prim.PowerPlaneConnectStyle, True);
    JsonWriteCoord('powerPlaneReliefExpansion', Prim.PowerPlaneReliefExpansion, True);
    JsonWriteCoord('reliefAirGap', Prim.ReliefAirGap, True);
    JsonWriteCoord('reliefConductorWidth', Prim.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', Prim.ReliefEntries, True);

    // Object reference properties (parent/context references)
    try
        if Prim.Net <> nil then
            JsonWriteString('net_ref', Prim.Net.Name, True)
        else
            JsonWriteString('net_ref', '', True);
    except
        JsonWriteString('net_ref', 'ERROR', True);
    end;
    try
        if Prim.Component <> nil then
            JsonWriteString('component_ref', Prim.Component.Name.Text, True)
        else
            JsonWriteString('component_ref', '', True);
    except
        JsonWriteString('component_ref', 'ERROR', True);
    end;
    try
        if Prim.Board <> nil then
            JsonWriteString('board_ref', Prim.Board.FileName, True)
        else
            JsonWriteString('board_ref', '', True);
    except
        JsonWriteString('board_ref', 'ERROR', True);
    end;
    try
        if Prim.Coordinate <> nil then
            JsonWriteString('coordinate_ref', Prim.Coordinate.UniqueId, True)
        else
            JsonWriteString('coordinate_ref', '', True);
    except
        JsonWriteString('coordinate_ref', 'ERROR', True);
    end;
    try
        if Prim.Dimension <> nil then
            JsonWriteString('dimension_ref', Prim.Dimension.UniqueId, True)
        else
            JsonWriteString('dimension_ref', '', True);
    except
        JsonWriteString('dimension_ref', 'ERROR', True);
    end;
    try
        if Prim.Polygon <> nil then
            JsonWriteString('polygon_ref', Prim.Polygon.UniqueId, True)
        else
            JsonWriteString('polygon_ref', '', True);
    except
        JsonWriteString('polygon_ref', 'ERROR', True);
    end;
    try
        JsonWriteBoolean('inSelectionMemory_0', Prim.InSelectionMemory[0], True);
    except
        JsonWriteString('inSelectionMemory', 'ERROR', True);
    end;

    // IPCB_Primitive2 extension properties
    try
        JsonWriteBoolean('isEmbeddedComponentCavity', Prim.IsEmbeddedComponentCavity, True);
    except
        JsonWriteString('isEmbeddedComponentCavity', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteBoolean('pasteMaskEnabled', Prim.PasteMaskEnabled, True);
    except
        JsonWriteString('pasteMaskEnabled', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteInteger('pasteMaskExpansionMode', Prim.PasteMaskExpansionMode, True);
    except
        JsonWriteString('pasteMaskExpansionMode', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteBoolean('pasteMaskManualEnabled', Prim.PasteMaskManualEnabled, True);
    except
        JsonWriteString('pasteMaskManualEnabled', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteFloat('pasteMaskManualPercent', Prim.PasteMaskManualPercent, True);
    except
        JsonWriteString('pasteMaskManualPercent', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteFloat('pasteMaskPercent', Prim.PasteMaskPercent, True);
    except
        JsonWriteString('pasteMaskPercent', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteBoolean('pasteMaskUsePercent', Prim.PasteMaskUsePercent, True);
    except
        JsonWriteString('pasteMaskUsePercent', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteCoord('routingMinWidth', Prim.RoutingMinWidth, True);
    except
        JsonWriteString('routingMinWidth', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteCoord('routingViaWidth', Prim.RoutingViaWidth, True);
    except
        JsonWriteString('routingViaWidth', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteInteger('solderMaskExpansionMode', Prim.SolderMaskExpansionMode, True);
    except
        JsonWriteString('solderMaskExpansionMode', 'ERROR: Could not read property', True);
    end;

    // IPCB_Primitive3D extension properties
    try
        JsonWriteInteger('faceIdx', Prim.FaceIdx, True);
    except
        JsonWriteString('faceIdx', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteFloat('faceRotation', Prim.FaceRotation, True);
    except
        JsonWriteString('faceRotation', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteInteger('faceU', Prim.FaceU, True);
    except
        JsonWriteString('faceU', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteInteger('faceV', Prim.FaceV, False);
    except
        JsonWriteString('faceV', 'ERROR: Could not read property', False);
    end;
end;

{==============================================================================
  PCB RULE BASE PROPERTIES HELPER
==============================================================================}

{ Helper procedure to export common properties shared by all PCB design rules.
  These properties come from IPCB_Rule interface. Call this at the END of each
  rule export procedure, just before closing the object. }
procedure ExportPcbBaseRuleProperties(Rule: IPCB_Rule);
begin
    // Rule identification
    JsonWriteString('name', Rule.Name, True);
    JsonWriteString('comment', Rule.Comment, True);
    JsonWriteString('uniqueId', Rule.UniqueId, True);
    JsonWriteString('identifier', Rule.Identifier, True);
    JsonWriteString('handle', Rule.Handle, True);
    JsonWriteString('descriptor', Rule.Descriptor, True);
    JsonWriteString('detail', Rule.Detail, True);
    JsonWriteString('objectIDString', Rule.ObjectIDString, True);
    JsonWriteInteger('objectId', Rule.ObjectId, True);

    // Rule configuration
    JsonWriteInteger('ruleKind', Rule.RuleKind, True);
    JsonWriteInteger('layerKind', Rule.LayerKind, True);
    JsonWriteInteger('netScope', Rule.NetScope, True);
    JsonWriteString('scope1Expression', Rule.Scope1Expression, True);
    JsonWriteString('scope2Expression', Rule.Scope2Expression, True);
    JsonWriteBoolean('isAdvanced', Rule.IsAdvanced, True);
    JsonWriteBoolean('definedByLogicalDocument', Rule.DefinedByLogicalDocument, True);
    JsonWriteBoolean('drcEnabled', Rule.DRCEnabled, True);
    JsonWriteBoolean('enabled', Rule.Enabled, True);

    // Common primitive properties for rules
    JsonWriteBoolean('inBoard', Rule.InBoard, True);
    JsonWriteBoolean('inComponent', Rule.InComponent, True);
    JsonWriteBoolean('inCoordinate', Rule.InCoordinate, True);
    JsonWriteBoolean('inDimension', Rule.InDimension, True);
    JsonWriteBoolean('inNet', Rule.InNet, True);
    JsonWriteBoolean('inPolygon', Rule.InPolygon, True);
    JsonWriteBoolean('used', Rule.Used, True);
    JsonWriteBoolean('selected', Rule.Selected, True);
    JsonWriteBoolean('moveable', Rule.Moveable, True);
    JsonWriteBoolean('allowGlobalEdit', Rule.AllowGlobalEdit, True);
    JsonWriteBoolean('drcError', Rule.DRCError, True);
    JsonWriteBoolean('isPreRoute', Rule.IsPreRoute, True);
    JsonWriteBoolean('userRouted', Rule.UserRouted, True);
    JsonWriteBoolean('isElectricalPrim', Rule.IsElectricalPrim, True);
    JsonWriteBoolean('isKeepout', Rule.IsKeepout, True);
    JsonWriteBoolean('isAssyTestpoint_Top', Rule.IsAssyTestpoint_Top, True);
    JsonWriteBoolean('isAssyTestpoint_Bottom', Rule.IsAssyTestpoint_Bottom, True);
    JsonWriteBoolean('polygonOutline', Rule.PolygonOutline, True);
    JsonWriteBoolean('tearDrop', Rule.TearDrop, True);
    JsonWriteBoolean('padCacheRobotFlag', Rule.PadCacheRobotFlag, True);
    JsonWriteBoolean('miscFlag1', Rule.MiscFlag1, True);
    JsonWriteBoolean('miscFlag2', Rule.MiscFlag2, True);
    JsonWriteBoolean('miscFlag3', Rule.MiscFlag3, True);
    JsonWriteInteger('unionIndex', Rule.UnionIndex, True);
    JsonWriteInteger('layer_V6', Rule.Layer_V6, True);
    JsonWriteInteger('viewableObjectId', Rule.ViewableObjectID, True);
    JsonWriteInteger('layer', Rule.Layer, True);

    // Extended primitive properties for rules
    JsonWriteBoolean('drawAsPreview', Rule.DrawAsPreview, True);
    JsonWriteBoolean('enableDraw', Rule.EnableDraw, True);
    JsonWriteBoolean('enabled_Direct', Rule.Enabled_Direct, True);
    JsonWriteBoolean('enabled_vComponent', Rule.Enabled_vComponent, True);
    JsonWriteBoolean('enabled_vCoordinate', Rule.Enabled_vCoordinate, True);
    JsonWriteBoolean('enabled_vDimension', Rule.Enabled_vDimension, True);
    JsonWriteBoolean('enabled_vNet', Rule.Enabled_vNet, True);
    JsonWriteBoolean('enabled_vPolygon', Rule.Enabled_vPolygon, True);
    JsonWriteInteger('index', Rule.Index, True);
    JsonWriteBoolean('isTenting', Rule.IsTenting, True);
    JsonWriteBoolean('isTenting_Top', Rule.IsTenting_Top, True);
    JsonWriteBoolean('isTenting_Bottom', Rule.IsTenting_Bottom, True);
    JsonWriteBoolean('isTestpoint_Top', Rule.IsTestpoint_Top, True);
    JsonWriteBoolean('isTestpoint_Bottom', Rule.IsTestpoint_Bottom, True);
    JsonWriteCoord('pasteMaskExpansion', Rule.PasteMaskExpansion, True);
    JsonWriteCoord('solderMaskExpansion', Rule.SolderMaskExpansion, True);
    JsonWriteCoord('powerPlaneClearance', Rule.PowerPlaneClearance, True);
    JsonWriteInteger('powerPlaneConnectStyle', Rule.PowerPlaneConnectStyle, True);
    JsonWriteCoord('powerPlaneReliefExpansion', Rule.PowerPlaneReliefExpansion, True);
    JsonWriteCoord('reliefAirGap', Rule.ReliefAirGap, True);
    JsonWriteCoord('reliefConductorWidth', Rule.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', Rule.ReliefEntries, True);

    // Object reference properties (parent/context references)
    try
        if Rule.Net <> nil then
            JsonWriteString('net_ref', Rule.Net.Name, True)
        else
            JsonWriteString('net_ref', '', True);
    except
        JsonWriteString('net_ref', 'ERROR', True);
    end;
    try
        if Rule.Component <> nil then
            JsonWriteString('component_ref', Rule.Component.Name.Text, True)
        else
            JsonWriteString('component_ref', '', True);
    except
        JsonWriteString('component_ref', 'ERROR', True);
    end;
    try
        if Rule.Board <> nil then
            JsonWriteString('board_ref', Rule.Board.FileName, True)
        else
            JsonWriteString('board_ref', '', True);
    except
        JsonWriteString('board_ref', 'ERROR', True);
    end;
    try
        if Rule.Coordinate <> nil then
            JsonWriteString('coordinate_ref', Rule.Coordinate.UniqueId, True)
        else
            JsonWriteString('coordinate_ref', '', True);
    except
        JsonWriteString('coordinate_ref', 'ERROR', True);
    end;
    try
        if Rule.Dimension <> nil then
            JsonWriteString('dimension_ref', Rule.Dimension.UniqueId, True)
        else
            JsonWriteString('dimension_ref', '', True);
    except
        JsonWriteString('dimension_ref', 'ERROR', True);
    end;
    try
        if Rule.Polygon <> nil then
            JsonWriteString('polygon_ref', Rule.Polygon.UniqueId, True)
        else
            JsonWriteString('polygon_ref', '', True);
    except
        JsonWriteString('polygon_ref', 'ERROR', True);
    end;
    try
        JsonWriteBoolean('inSelectionMemory_0', Rule.InSelectionMemory[0], False);
    except
        JsonWriteString('inSelectionMemory', 'ERROR', False);
    end;
end;

{==============================================================================
  SCH BASE GRAPHICAL OBJECT PROPERTIES HELPER
==============================================================================}

{ Helper procedure to export common properties shared by all SCH graphical objects. }

procedure ExportSchBaseProperties(Obj: ISch_GraphicalObject);
begin
    JsonWriteInteger('objectId', Obj.ObjectId, True);
    JsonWriteString('uniqueId', Obj.UniqueId, True);
    JsonWriteString('handle', Obj.Handle, True);
    JsonWriteBoolean('enableDraw', Obj.EnableDraw, True);
    JsonWriteBoolean('selection', Obj.Selection, True);
    JsonWriteBoolean('graphicallyLocked', Obj.GraphicallyLocked, True);
    JsonWriteBoolean('compilationMasked', Obj.CompilationMasked, True);
    JsonWriteBoolean('dimmed', Obj.Dimmed, True);
    JsonWriteBoolean('disabled', Obj.Disabled, True);
    JsonWriteBoolean('displayError', Obj.DisplayError, True);
    JsonWriteInteger('errorColor', Obj.ErrorColor, True);
    JsonWriteInteger('errorKind', Obj.ErrorKind, True);
    JsonWriteString('errorString', Obj.ErrorString, True);
    JsonWriteInteger('liveHighlightValue', Obj.LiveHighlightValue, True);
    JsonWriteInteger('ownerPartId', Obj.OwnerPartId, True);
    JsonWriteInteger('ownerPartDisplayMode', Obj.OwnerPartDisplayMode, True);

    // Additional ISch_GraphicalObject properties
    JsonWriteInteger('areaColor', Obj.AreaColor, True);
    JsonWriteInteger('color', Obj.Color, True);
    JsonWriteInteger('location_x', Obj.Location.X, True);
    JsonWriteInteger('location_y', Obj.Location.Y, True);

    // Object reference properties (parent/context references)
    try
        if Obj.OwnerDocument <> nil then
            JsonWriteString('ownerDocument_ref', Obj.OwnerDocument.DocumentName, True)
        else
            JsonWriteString('ownerDocument_ref', '', True);
    except
        JsonWriteString('ownerDocument_ref', 'ERROR', True);
    end;
    try
        if Obj.Container <> nil then
            JsonWriteString('container_ref', Obj.Container.UniqueId, False)
        else
            JsonWriteString('container_ref', '', False);
    except
        JsonWriteString('container_ref', 'ERROR', False);
    end;
end;

{==============================================================================
  PCB LIBRARY JSON EXPORT
==============================================================================}

procedure ExportPcbPadToJson(Pad: IPCB_Pad4; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Pad', True);
    JsonWriteString('name', Pad.Name, True);
    JsonWriteCoord('x', Pad.X, True);
    JsonWriteCoord('y', Pad.Y, True);
    JsonWriteCoord('topXSize', Pad.TopXSize, True);
    JsonWriteCoord('topYSize', Pad.TopYSize, True);
    JsonWriteCoord('holeSize', Pad.HoleSize, True);
    JsonWriteInteger('topShape', Ord(Pad.TopShape), True);
    JsonWriteInteger('holeType', Ord(Pad.HoleType), True);
    JsonWriteFloat('rotation', Pad.Rotation, True);
    JsonWriteInteger('layer', Pad.Layer, True);
    JsonWriteBoolean('plated', Pad.Plated, True);

    // Layer-specific sizes (for stacked pads)
    JsonWriteInteger('mode', Pad.Mode, True);
    JsonWriteCoord('midXSize', Pad.MidXSize, True);
    JsonWriteCoord('midYSize', Pad.MidYSize, True);
    JsonWriteCoord('botXSize', Pad.BotXSize, True);
    JsonWriteCoord('botYSize', Pad.BotYSize, True);
    JsonWriteInteger('midShape', Pad.MidShape, True);
    JsonWriteInteger('botShape', Pad.BotShape, True);

    // Hole properties
    JsonWriteFloat('holeRotation', Pad.HoleRotation, True);
    JsonWriteCoord('holeWidth', Pad.HoleWidth, True);
    JsonWriteInteger('drillType', Pad.DrillType, True);

    // Swap IDs
    JsonWriteString('swapID_Pad', Pad.SwapID_Pad, True);
    JsonWriteString('swapID_Part', Pad.SwapID_Part, True);

    // Tolerances
    JsonWriteCoord('holePositiveTolerance', Pad.HolePositiveTolerance, True);
    JsonWriteCoord('holeNegativeTolerance', Pad.HoleNegativeTolerance, True);

    // Additional IPCB_Pad properties
    JsonWriteBoolean('drawAsPreview', Pad.DrawAsPreview, True);
    JsonWriteBoolean('isSurfaceMount', Pad.IsSurfaceMount, True);
    JsonWriteBoolean('isVirtualPin', Pad.IsVirtualPin, True);
    JsonWriteInteger('jumperID', Pad.JumperID, True);
    JsonWriteInteger('ownerPart_ID', Pad.OwnerPart_ID, True);
    JsonWriteString('pinDescriptor', Pad.PinDescriptor, True);
    JsonWriteBoolean('solderMaskExpansionFromHoleEdge', Pad.SolderMaskExpansionFromHoleEdge, True);
    JsonWriteString('swappedPadName', Pad.SwappedPadName, True);

    // IPCB_Pad2 extension properties
    try
        JsonWriteCoord('pinPackageLength', Pad.PinPackageLength, True);
    except
        JsonWriteString('pinPackageLength', 'ERROR: Could not read property', True);
    end;

    // IPCB_Pad3 extension properties
    try
        JsonWriteInteger('daisyChainStyle', Pad.DaisyChainStyle, True);
    except
        JsonWriteString('daisyChainStyle', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteCoord('maxCSignalLayers', Pad.MaxXSignalLayers, True);
    except
        JsonWriteString('maxCSignalLayers', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteCoord('maxYSignalLayers', Pad.MaxYSignalLayers, True);
    except
        JsonWriteString('maxYSignalLayers', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteInteger('multiLayerHighBits', Pad.MultiLayerHighBits, True);
    except
        JsonWriteString('multiLayerHighBits', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteBoolean('padHasOffsetOnAny', Pad.PadHasOffsetOnAny, True);
    except
        JsonWriteString('padHasOffsetOnAny', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteCoord('xPadOffsetAll', Pad.XPadOffsetAll, True);
    except
        JsonWriteString('xPadOffsetAll', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteCoord('yPadOffsetAll', Pad.YPadOffsetAll, True);
    except
        JsonWriteString('yPadOffsetAll', 'ERROR: Could not read property', True);
    end;

    // IPCB_Pad4 extension properties
    try
        JsonWriteBoolean('isBottomPasteEnabled', Pad.IsBottomPasteEnabled, True);
    except
        JsonWriteString('isBottomPasteEnabled', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteBoolean('isTopPasteEnabled', Pad.IsTopPasteEnabled, True);
    except
        JsonWriteString('isTopPasteEnabled', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteCoord('maxXSignalLayers', Pad.MaxXSignalLayers, True);
    except
        JsonWriteString('maxXSignalLayers', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteInteger('multiLayerHighBits', Pad.MultiLayerHighBits, True);
    except
        JsonWriteString('multiLayerHighBits', 'ERROR: Could not read property', True);
    end;
    try JsonWriteBoolean('isFreePrimitive', Pad.IsFreePrimitive, True); except end;

    // Base primitive properties (includes tenting, testpoint, mask, power plane, relief)
    ExportPcbBasePrimitiveProperties(Pad);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbTrackToJson(Track: IPCB_Track; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Track', True);
    JsonWriteCoord('x1', Track.X1, True);
    JsonWriteCoord('y1', Track.Y1, True);
    JsonWriteCoord('x2', Track.X2, True);
    JsonWriteCoord('y2', Track.Y2, True);
    JsonWriteCoord('width', Track.Width, True);
    JsonWriteInteger('layer', Track.Layer, True);

    // Net information
    if Track.Net <> nil then
        JsonWriteString('net', Track.Net.Name, True)
    else
        JsonWriteString('net', '', True);

    // IPCB_Track3D extension properties
    try
        JsonWriteInteger('faceIdx1', Track.FaceIdx1, True);
    except
        JsonWriteString('faceIdx1', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteInteger('faceIdx2', Track.FaceIdx2, True);
    except
        JsonWriteString('faceIdx2', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteInteger('faceU1', Track.FaceU1, True);
    except
        JsonWriteString('faceU1', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteInteger('faceU2', Track.FaceU2, True);
    except
        JsonWriteString('faceU2', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteInteger('faceV1', Track.FaceV1, True);
    except
        JsonWriteString('faceV1', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteInteger('faceV2', Track.FaceV2, True);
    except
        JsonWriteString('faceV2', 'ERROR: Could not read property', True);
    end;

    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Track);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbArcToJson(Arc: IPCB_Arc; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Arc', True);
    JsonWriteCoord('xCenter', Arc.XCenter, True);
    JsonWriteCoord('yCenter', Arc.YCenter, True);
    JsonWriteCoord('radius', Arc.Radius, True);
    JsonWriteFloat('startAngle', Arc.StartAngle, True);
    JsonWriteFloat('endAngle', Arc.EndAngle, True);
    JsonWriteCoord('lineWidth', Arc.LineWidth, True);
    JsonWriteInteger('layer', Arc.Layer, True);

    // Computed endpoints (from API IPCB_Arc.csv)
    JsonWriteCoord('startX', Arc.StartX, True);
    JsonWriteCoord('startY', Arc.StartY, True);
    JsonWriteCoord('endX', Arc.EndX, True);
    JsonWriteCoord('endY', Arc.EndY, True);

    // Net information
    if Arc.Net <> nil then
        JsonWriteString('net', Arc.Net.Name, True)
    else
        JsonWriteString('net', '', True);

    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Arc);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbTextToJson(Text: IPCB_Text; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Text', True);
    JsonWriteString('text', Text.Text, True);
    JsonWriteCoord('x', Text.XLocation, True);
    JsonWriteCoord('y', Text.YLocation, True);
    JsonWriteCoord('size', Text.Size, True);
    JsonWriteFloat('rotation', Text.Rotation, True);
    JsonWriteInteger('layer', Text.Layer, True);
    JsonWriteBoolean('mirrored', Text.MirrorFlag, True);
    JsonWriteBoolean('inverted', Text.Inverted, True);
    JsonWriteBoolean('wordWrap', Text.WordWrap, True);
    JsonWriteCoord('width', Text.Width, True);
    JsonWriteBoolean('useTTFonts', Text.UseTTFonts, True);
    if Text.UseTTFonts then
        JsonWriteString('fontName', Text.FontName, True)
    else
        JsonWriteInteger('strokeFont', Text.FontID, True);

    // Extended IPCB_Text properties
    JsonWriteInteger('textKind', Text.TextKind, True);
    JsonWriteBoolean('bold', Text.Bold, True);
    JsonWriteBoolean('italic', Text.Italic, True);
    JsonWriteInteger('charSet', Text.CharSet, True);
    JsonWriteBoolean('multiLine', Text.MultiLine, True);
    JsonWriteCoord('multilineTextHeight', Text.MultilineTextHeight, True);
    JsonWriteCoord('multilineTextWidth', Text.MultilineTextWidth, True);
    JsonWriteInteger('multilineTextAutoPosition', Text.MultilineTextAutoPosition, True);
    JsonWriteBoolean('multilineTextResizeEnabled', Text.MultilineTextResizeEnabled, True);
    JsonWriteBoolean('useInvertedRectangle', Text.UseInvertedRectangle, True);
    JsonWriteCoord('invertedTTTextBorder', Text.InvertedTTTextBorder, True);
    JsonWriteInteger('invRectWidth', Text.InvRectWidth, True);
    JsonWriteInteger('invRectHeight', Text.InvRectHeight, True);
    JsonWriteInteger('ttfInvertedTextJustify', Text.TTFOffsetFromInvertedRect, True);
    JsonWriteCoord('ttfOffsetFromInvertedRect', Text.TTFOffsetFromInvertedRect, True);
    JsonWriteInteger('ttfTextWidth', Text.TTFTextWidth, True);
    JsonWriteInteger('ttfTextHeight', Text.TTFTextHeight, True);
    JsonWriteInteger('borderSpaceType', Text.BorderSpaceType, True);
    JsonWriteBoolean('disableSpecialStringConversion', Text.DisableSpecialStringConversion, True);
    JsonWriteString('underlyingString', Text.UnderlyingString, True);
    JsonWriteString('convertedString', Text.ConvertedString, True);
    JsonWriteBoolean('isRedundant', Text.IsRedundant, True);
    JsonWriteCoord('x1Location', Text.X1Location, True);
    JsonWriteCoord('y1Location', Text.Y1Location, True);
    JsonWriteCoord('x2Location', Text.X2Location, True);
    JsonWriteCoord('y2Location', Text.Y2Location, True);

    // Barcode properties
    try JsonWriteInteger('barCodeKind', Text.BarCodeKind, True); except JsonWriteString('barCodeKind', 'ERROR', True); end;
    try JsonWriteBoolean('barCodeInverted', Text.BarCodeInverted, True); except JsonWriteString('barCodeInverted', 'ERROR', True); end;
    try JsonWriteBoolean('barCodeShowText', Text.BarCodeShowText, True); except JsonWriteString('barCodeShowText', 'ERROR', True); end;
    try JsonWriteInteger('barCodeRenderMode', Text.BarCodeRenderMode, True); except JsonWriteString('barCodeRenderMode', 'ERROR', True); end;
    try JsonWriteCoord('barCodeMinWidth', Text.BarCodeMinWidth, True); except JsonWriteString('barCodeMinWidth', 'ERROR', True); end;
    try JsonWriteCoord('barCodeFullWidth', Text.BarCodeFullWidth, True); except JsonWriteString('barCodeFullWidth', 'ERROR', True); end;
    try JsonWriteCoord('barCodeFullHeight', Text.BarCodeFullHeight, True); except JsonWriteString('barCodeFullHeight', 'ERROR', True); end;
    try JsonWriteCoord('barCodeXMargin', Text.BarCodeXMargin, True); except JsonWriteString('barCodeXMargin', 'ERROR', True); end;
    try JsonWriteCoord('barCodeYMargin', Text.BarCodeYMargin, True); except JsonWriteString('barCodeYMargin', 'ERROR', True); end;
    try JsonWriteString('barCodeFontName', Text.BarCodeFontName, True); except JsonWriteString('barCodeFontName', 'ERROR', True); end;
    // SKIPPED: Text.BarCodeBitPattern - hangs on panel boards with embedded board arrays
    // (computed property that triggers special string resolution against embedded boards)

    try
        JsonWriteString('getDesignatorDisplayString', Text.GetDesignatorDisplayString, True);
    except
        JsonWriteString('getDesignatorDisplayString', 'ERROR: Could not read property', True);
    end;

    // IPCB_Text3 extension properties
    try JsonWriteBoolean('advanceSnapping', Text.AdvanceSnapping, True); except JsonWriteString('advanceSnapping', 'ERROR', True); end;
    try JsonWriteCoord('snapPointX', Text.SnapPointX, True); except JsonWriteString('snapPointX', 'ERROR', True); end;
    try JsonWriteCoord('snapPointY', Text.SnapPointY, True); except JsonWriteString('snapPointY', 'ERROR', True); end;
    try JsonWriteInteger('stringXPosition', Text.StringXPosition, True); except JsonWriteString('stringXPosition', 'ERROR', True); end;
    try JsonWriteInteger('stringYPosition', Text.StringYPosition, True); except JsonWriteString('stringYPosition', 'ERROR', True); end;

    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Text);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbFillToJson(Fill: IPCB_Fill; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Fill', True);
    JsonWriteCoord('x1', Fill.X1Location, True);
    JsonWriteCoord('y1', Fill.Y1Location, True);
    JsonWriteCoord('x2', Fill.X2Location, True);
    JsonWriteCoord('y2', Fill.Y2Location, True);
    JsonWriteFloat('rotation', Fill.Rotation, True);
    JsonWriteInteger('layer', Fill.Layer, True);

    // Center point (from API IPCB_Fill.csv)
    JsonWriteCoord('xLocation', Fill.XLocation, True);
    JsonWriteCoord('yLocation', Fill.YLocation, True);

    // Net information
    if Fill.Net <> nil then
        JsonWriteString('net', Fill.Net.Name, True)
    else
        JsonWriteString('net', '', True);

    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Fill);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbRegionToJson(Region: IPCB_Region; AddComma: Boolean);
var
    I, J: Integer;
    Contour: IPCB_Contour;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Region', True);
    JsonWriteInteger('kind', Ord(Region.Kind), True);
    JsonWriteInteger('layer', Region.Layer, True);

    // Net information
    if Region.Net <> nil then
        JsonWriteString('net', Region.Net.Name, True)
    else
        JsonWriteString('net', '', True);

    JsonWriteInteger('holeCount', Region.HoleCount, True);

    // Main contour vertices
    Contour := Region.MainContour;
    if Contour <> nil then
    begin
        JsonOpenArray('mainContour');
        for I := 0 to Contour.Count - 1 do
        begin
            JsonOpenObject('');
            JsonWriteInteger('x', Contour.x[I], True);
            JsonWriteInteger('y', Contour.y[I], False);
            JsonCloseObject(I < Contour.Count - 1);
        end;
        JsonCloseArray(True);
    end;

    // Hole contours
    if Region.HoleCount > 0 then
    begin
        JsonOpenArray('holes');
        for I := 0 to Region.HoleCount - 1 do
        begin
            Contour := Region.Holes[I];
            if Contour <> nil then
            begin
                JsonOpenArray('');
                for J := 0 to Contour.Count - 1 do
                begin
                    JsonOpenObject('');
                    JsonWriteInteger('x', Contour.x[J], True);
                    JsonWriteInteger('y', Contour.y[J], False);
                    JsonCloseObject(J < Contour.Count - 1);
                end;
                JsonCloseArray(I < Region.HoleCount - 1);
            end;
        end;
        JsonCloseArray(True);
    end;

    // Additional IPCB_Region properties
    JsonWriteString('name', Region.Name, True);
    JsonWriteCoord('cavityHeight', Region.CavityHeight, True);
    try
        JsonWriteInteger('area', Region.Area, True);
    except
        JsonWriteString('area', 'ERROR: Could not read property', True);
    end;

    // IPCB_Region2 extension properties
    try
        JsonWriteCoord('arcApproximation', Region.ArcApproximation, True);
    except
        JsonWriteString('arcApproximation', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteInteger('totalVertexCount', Region.TotalVertexCount, True);
    except
        JsonWriteString('totalVertexCount', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteBoolean('virtualCutout', Region.VirtualCutout, True);
    except
        JsonWriteString('virtualCutout', 'ERROR: Could not read property', True);
    end;

    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Region);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbPolygonToJson(Polygon: IPCB_Polygon; AddComma: Boolean);
var
    I: Integer;
    NetName: String;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Polygon', True);

    // Net information
    if Polygon.Net <> nil then
        NetName := Polygon.Net.Name
    else
        NetName := '';
    JsonWriteString('net', NetName, True);

    JsonWriteInteger('layer', Polygon.Layer, True);
    JsonWriteString('name', Polygon.Name, True);
    JsonWriteInteger('polygonType', Polygon.PolygonType, True);
    JsonWriteInteger('hatchStyle', Polygon.PolyHatchStyle, True);
    JsonWriteCoord('trackSize', Polygon.TrackSize, True);
    JsonWriteCoord('grid', Polygon.Grid, True);
    JsonWriteCoord('minTrack', Polygon.MinTrack, True);
    JsonWriteInteger('pourOver', Polygon.PourOver, True);
    JsonWriteBoolean('removeIslandsByArea', Polygon.RemoveIslandsByArea, True);
    JsonWriteBoolean('removeNarrowNecks', Polygon.RemoveNarrowNecks, True);
    JsonWriteBoolean('poured', Polygon.Poured, True);
    JsonWriteInteger('pointCount', Polygon.PointCount, True);

    // Additional polygon properties
    JsonWriteFloat('areaSize', Polygon.AreaSize, True);
    JsonWriteBoolean('removeDead', Polygon.RemoveDead, True);
    JsonWriteBoolean('useOctagons', Polygon.UseOctagons, True);
    JsonWriteBoolean('avoidObstacles', Polygon.AvoidObsticles, True);
    JsonWriteFloat('islandAreaThreshold', Polygon.IslandAreaThreshold, True);
    JsonWriteCoord('neckWidthThreshold', Polygon.NeckWidthThreshold, True);
    JsonWriteCoord('arcApproximation', Polygon.ArcApproximation, True);
    JsonWriteBoolean('ignoreViolations', Polygon.IgnoreViolations, True);
    JsonWriteInteger('pourIndex', Polygon.PourIndex, True);

    // Extended polygon properties from API IPCB_BoardOutline.csv
    JsonWriteBoolean('arcPourMode', Polygon.ArcPourMode, True);
    JsonWriteBoolean('autoGenerateName', Polygon.AutoGenerateName, True);
    JsonWriteCoord('borderWidth', Polygon.BorderWidth, True);
    JsonWriteBoolean('clipAcuteCorners', Polygon.ClipAcuteCorners, True);
    JsonWriteBoolean('drawDeadCopper', Polygon.DrawDeadCopper, True);
    JsonWriteBoolean('drawRemovedIslands', Polygon.DrawRemovedIslands, True);
    JsonWriteBoolean('drawRemovedNecks', Polygon.DrawRemovedNecks, True);
    JsonWriteBoolean('expandOutline', Polygon.ExpandOutline, True);
    JsonWriteBoolean('mitreCorners', Polygon.MitreCorners, True);
    JsonWriteBoolean('obeyPolygonCutout', Polygon.ObeyPolygonCutout, True);
    JsonWriteBoolean('optimalVoidRotation', Polygon.OptimalVoidRotation, True);
    JsonWriteBoolean('primitiveLock', Polygon.PrimitiveLock, True);

    // Export vertices
    JsonOpenArray('vertices');
    for I := 0 to Polygon.PointCount - 1 do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', Polygon.Segments[I].vx, True);
        JsonWriteInteger('y', Polygon.Segments[I].vy, True);
        JsonWriteInteger('kind', Polygon.Segments[I].Kind, False);
        JsonCloseObject(I < Polygon.PointCount - 1);
    end;
    JsonCloseArray(True);

    // Additional Polygon properties
    JsonWriteBoolean('copperPourValidate', Polygon.CopperPourValidate, True);
    JsonWriteBoolean('drawRemovedNecks', Polygon.DrawRemovedNecks, True);
    JsonWriteInteger('polyHatchStyle', Polygon.PolyHatchStyle, True);

    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Polygon);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbDimensionToJson(Dim: IPCB_Dimension; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Dimension', True);
    JsonWriteInteger('dimensionKind', Dim.DimensionKind, True);
    JsonWriteFloat('textValue', Dim.TextValue, True);
    JsonWriteCoord('textX', Dim.TextX, True);
    JsonWriteCoord('textY', Dim.TextY, True);
    JsonWriteCoord('textHeight', Dim.TextHeight, True);
    JsonWriteCoord('textWidth', Dim.TextWidth, True);
    JsonWriteString('textPrefix', Dim.TextPrefix, True);
    JsonWriteString('textSuffix', Dim.TextSuffix, True);
    JsonWriteInteger('textPrecision', Dim.TextPrecision, True);
    JsonWriteInteger('textDimensionUnit', Dim.TextDimensionUnit, True);
    JsonWriteCoord('x1Location', Dim.X1Location, True);
    JsonWriteCoord('y1Location', Dim.Y1Location, True);
    JsonWriteCoord('size', Dim.Size, True);
    JsonWriteCoord('lineWidth', Dim.LineWidth, True);
    JsonWriteCoord('arrowSize', Dim.ArrowSize, True);
    JsonWriteCoord('arrowLength', Dim.ArrowLength, True);
    JsonWriteInteger('layer', Dim.Layer, True);

    // Additional dimension properties
    JsonWriteInteger('textPosition', Dim.TextPosition, True);
    JsonWriteCoord('textGap', Dim.TextGap, True);
    JsonWriteCoord('arrowLineWidth', Dim.ArrowLineWidth, True);
    JsonWriteInteger('arrowPosition', Dim.ArrowPosition, True);
    JsonWriteCoord('extensionOffset', Dim.ExtensionOffset, True);
    JsonWriteCoord('extensionLineWidth', Dim.ExtensionLineWidth, True);
    JsonWriteCoord('extensionPickGap', Dim.ExtensionPickGap, True);
    JsonWriteInteger('style', Dim.Style, True);
    JsonWriteBoolean('bold', Dim.Bold, True);
    JsonWriteBoolean('italic', Dim.Italic, True);

    JsonWriteBoolean('useTTFonts', Dim.UseTTFonts, True);
    if Dim.UseTTFonts then
        JsonWriteString('fontName', Dim.FontName, True)
    else
        JsonWriteInteger('textFont', Dim.TextFont, True);

    // Additional dimension properties from API
    JsonWriteCoord('textLineWidth', Dim.TextLineWidth, True);
    JsonWriteString('textFormat', Dim.TextFormat, True);
    JsonWriteInteger('references_Count', Dim.References_Count, True);
    JsonWriteBoolean('primitiveLock', Dim.PrimitiveLock, True);


    // Additional IPCB_Dimension properties
    JsonWriteCoord('x', Dim.x, True);
    JsonWriteCoord('y', Dim.y, True);

    // Extension properties from IPCB_LinearDimension / IPCB_LinearDiameterDimension
    try JsonWriteFloat('angle', Dim.Angle, True); except end;

    // Extension properties from IPCB_LeaderDimension
    try JsonWriteBoolean('dot', Dim.Dot, True); except end;
    try JsonWriteCoord('dotSize', Dim.DotSize, True); except end;
    try JsonWriteBoolean('isHidden', Dim.IsHidden, True); except end;
    try JsonWriteInteger('requiredParameterSpace', Dim.RequiredParameterSpace, True); except end;
    try JsonWriteInteger('shape', Dim.Shape, True); except end;

    // Extension properties from IPCB_RadialDimension / IPCB_RadialDiameterDimension
    try JsonWriteFloat('angleStep', Dim.AngleStep, True); except end;
    try JsonWriteBoolean('drawAndPreview', Dim.DrawAndPreview, True); except end;
    try JsonWriteBoolean('isFreePrimitive', Dim.IsFreePrimitive, True); except end;

    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Dim);


    try
        JsonWriteBoolean('layerUsed_top', Dim.LayerUsed[eTopLayer], True);
    except
        JsonWriteString('layerUsed_top', 'ERROR', True);
    end;
    try
        JsonWriteInteger('references_0', Dim.References[0].ReferencePoint.X, True);
    except
        JsonWriteString('references_0', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbCoordinateToJson(Coord: IPCB_Coordinate; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Coordinate', True);
    JsonWriteCoord('x', Coord.X, True);
    JsonWriteCoord('y', Coord.Y, True);
    JsonWriteInteger('layer', Coord.Layer, True);
    JsonWriteCoord('lineWidth', Coord.LineWidth, True);
    JsonWriteCoord('size', Coord.Size, True);
    JsonWriteCoord('textHeight', Coord.TextHeight, True);
    JsonWriteCoord('textWidth', Coord.TextWidth, True);
    JsonWriteBoolean('useTTFonts', Coord.UseTTFonts, True);
    if Coord.UseTTFonts then
        JsonWriteString('fontName', Coord.FontName, True)
    else
        JsonWriteInteger('textFont', Coord.TextFont, True);


    // Additional IPCB_Coordinate properties
    JsonWriteBoolean('bold', Coord.Bold, True);
    JsonWriteBoolean('italic', Coord.Italic, True);
    JsonWriteBoolean('primitiveLock', Coord.PrimitiveLock, True);
    JsonWriteInteger('rotation', Coord.Rotation, True);
    JsonWriteInteger('style', Coord.Style, True);
    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Coord);


    try
        JsonWriteBoolean('layerUsed_top', Coord.LayerUsed[eTopLayer], False);
    except
        JsonWriteString('layerUsed_top', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

{==============================================================================
  NEW PCB EXPORT PROCEDURES - Extended Coverage
  These procedures export additional PCB object types to achieve complete
  coverage of the Altium API.
==============================================================================}

procedure ExportPcbAccordionToJson(Accordion: IPCB_Accordion; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Accordion', True);

    // Position and layer
    JsonWriteInteger('layer', Accordion.Layer, True);

    // Accordion-specific properties
    JsonWriteCoord('amplitudeIncrement', Accordion.AmplitudeIncrement, True);
    JsonWriteCoord('gap', Accordion.Gap, True);
    JsonWriteCoord('gapIncrement', Accordion.GapIncrement, True);
    JsonWriteCoord('maxAmplitude', Accordion.MaxAmplitude, True);
    JsonWriteCoord('connectionLength', Accordion.ConnectonLength, True);
    JsonWriteCoord('estimateLength', Accordion.EstimateLength, True);
    JsonWriteInteger('style', Accordion.Style, True);

    // Net information
    if Accordion.Net <> nil then
        JsonWriteString('net', Accordion.Net.Name, True)
    else
        JsonWriteString('net', '', True);


    // Additional IPCB_Accordion properties
    JsonWriteInteger('requiredParamterSpace', Accordion.RequiredParamterSpace, True);
    // Base primitive properties (includes power plane, mask, tenting, testpoint, relief)
    ExportPcbBasePrimitiveProperties(Accordion);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbAxisToJson(Axis: IPCB_Axis; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Axis', True);

    // Position and layer
    JsonWriteInteger('layer', Axis.Layer, True);


    // Additional IPCB_Axis properties
    JsonWriteFloat('directionX', Axis.DirectionX, True);
    JsonWriteFloat('directionY', Axis.DirectionY, True);
    JsonWriteFloat('directionZ', Axis.DirectionZ, True);
    JsonWriteString('name', Axis.Name, True);
    try
        JsonWriteFloat('origin_X', Axis.Origin.X, True);
        JsonWriteFloat('origin_Y', Axis.Origin.Y, True);
        JsonWriteFloat('origin_Z', Axis.Origin.Z, True);
    except
        JsonWriteString('origin', 'ERROR: Could not read Origin', True);
    end;
    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Axis);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbBendingLineToJson(BendingLine: IPCB_BendingLine; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BendingLine', True);

    // Position and layer
    JsonWriteInteger('layer', BendingLine.Layer, True);

    // Bending properties
    JsonWriteFloat('bendAngle', BendingLine.BendAngle, True);
    JsonWriteCoord('bendRadius', BendingLine.BendRadius, True);
    JsonWriteCoord('lineWidth', BendingLine.LineWidth, True);
    JsonWriteCoord('x1', BendingLine.X1, True);
    JsonWriteCoord('y1', BendingLine.Y1, True);
    JsonWriteCoord('x2', BendingLine.X2, True);
    JsonWriteCoord('y2', BendingLine.Y2, True);


    // Additional IPCB_BendingLine properties
    JsonWriteInteger('angle', BendingLine.Angle, True);
    JsonWriteInteger('foldIndex', BendingLine.FoldIndex, True);
    try
        JsonWriteCoord('fromPoint_x', BendingLine.FromPoint.X, True);
        JsonWriteCoord('fromPoint_y', BendingLine.FromPoint.Y, True);
    except
        JsonWriteString('fromPoint', 'ERROR', True);
    end;
    JsonWriteBoolean('locked', BendingLine.Locked, True);
    JsonWriteString('name', BendingLine.Name, True);
    JsonWriteCoord('radius', BendingLine.Radius, True);
    JsonWriteInteger('rotation', BendingLine.Rotation, True);
    try
        JsonWriteCoord('toPoint_x', BendingLine.ToPoint.X, True);
        JsonWriteCoord('toPoint_y', BendingLine.ToPoint.Y, True);
    except
        JsonWriteString('toPoint', 'ERROR', True);
    end;
    // Base primitive properties
    ExportPcbBasePrimitiveProperties(BendingLine);

    try
        if BendingLine.BoardRegion <> nil then
            JsonWriteString('boardRegion_ref', 'present', False)
        else
            JsonWriteString('boardRegion_ref', '', False);
    except
        JsonWriteString('boardRegion_ref', 'ERROR', False);
    end;

    JsonCloseObject(AddComma);
end;

procedure ExportPcbDrillTableToJson(DrillTable: IPCB_DrillTable; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DrillTable', True);

    // Position and size
    JsonWriteCoord('x', DrillTable.X, True);
    JsonWriteCoord('y', DrillTable.Y, True);
    JsonWriteCoord('width', DrillTable.Width, True);
    JsonWriteCoord('size', DrillTable.Size, True);
    JsonWriteInteger('layer', DrillTable.Layer, True);

    // Table properties
    JsonWriteInteger('drillTableUnits', DrillTable.DrillTableUnits, True);
    JsonWriteInteger('font', DrillTable.Font, True);
    JsonWriteCoord('lineWidth', DrillTable.LineWidth, True);
    JsonWriteBoolean('mirror', DrillTable.Mirror, True);

    // Include flags
    JsonWriteBoolean('includeFooter', DrillTable.IncludeFooter, True);
    JsonWriteBoolean('includeTitle', DrillTable.IncludeTitle, True);
    JsonWriteBoolean('includeTitleDrillLayerPair', DrillTable.IncludeTitleDrillLayerPair, True);
    JsonWriteBoolean('includeTitleIncludedHoles', DrillTable.IncludeTitleIncludedHoles, True);
    JsonWriteBoolean('includeTitlePlatingThickness', DrillTable.IncludeTitlePlatingThickness, True);
    JsonWriteBoolean('includeNonplatedPads', DrillTable.IncludeNonplatedPads, True);
    JsonWriteBoolean('includeNonslottedPads', DrillTable.IncludeNonslottedPads, True);
    JsonWriteBoolean('includePlatedPads', DrillTable.IncludePlatedPads, True);
    JsonWriteBoolean('includeSlottedPads', DrillTable.IncludeSlottedPads, True);
    JsonWriteBoolean('includeVias', DrillTable.IncludeVias, True);
    JsonWriteBoolean('separatePadsVias', DrillTable.SeparatePadsVias, True);

    // Column visibility
    JsonWriteBoolean('showColumnComment', DrillTable.ShowColumnComment, True);
    JsonWriteBoolean('showColumnObjType', DrillTable.ShowColumnObjType, True);
    JsonWriteBoolean('showColumnTolerance', DrillTable.ShowColumnTolerance, True);
    JsonWriteBoolean('showSecondaryUnits', DrillTable.ShowSecondaryUnits, True);

    // Plating thickness
    JsonWriteCoord('platingThickness', DrillTable.PlatingThickness, True);

    // Base primitive properties
    ExportPcbBasePrimitiveProperties(DrillTable);


    try
        if DrillTable.LayerPair <> nil then
            JsonWriteString('layerPair_ref', 'present', False)
        else
            JsonWriteString('layerPair_ref', '', False);
    except
        JsonWriteString('layerPair_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbEmbeddedBoardToJson(EmbeddedBoard: IPCB_EmbeddedBoard; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'EmbeddedBoard', True);

    // Position and layer
    JsonWriteInteger('layer', EmbeddedBoard.Layer, True);
    JsonWriteCoord('xLocation', EmbeddedBoard.XLocation, True);
    JsonWriteCoord('x1Location', EmbeddedBoard.X1Location, True);
    JsonWriteCoord('x2Location', EmbeddedBoard.X2Location, True);
    JsonWriteCoord('yLocation', EmbeddedBoard.YLocation, True);
    JsonWriteCoord('y1Location', EmbeddedBoard.Y1Location, True);
    JsonWriteCoord('y2Location', EmbeddedBoard.Y2Location, True);

    // File reference
    JsonWriteString('documentPath', EmbeddedBoard.DocumentPath, True);

    // Placement properties
    JsonWriteFloat('rotation', EmbeddedBoard.Rotation, True);
    JsonWriteCoord('rowSpacing', EmbeddedBoard.RowSpacing, True);
    JsonWriteCoord('colSpacing', EmbeddedBoard.ColSpacing, True);
    JsonWriteInteger('rowCount', EmbeddedBoard.RowCount, True);
    JsonWriteInteger('colCount', EmbeddedBoard.ColCount, True);
    
    // Additional IPCB_EmbeddedBoard properties
    JsonWriteBoolean('isViewport', EmbeddedBoard.IsViewport, True);
    JsonWriteBoolean('mirrorFlag', EmbeddedBoard.MirrorFlag, True);
    JsonWriteInteger('originMode', EmbeddedBoard.OriginMode, True);
    JsonWriteFloat('scale', EmbeddedBoard.Scale, True);
    JsonWriteInteger('titleFontColor', EmbeddedBoard.TitleFontColor, True);
    JsonWriteString('titleFontName', EmbeddedBoard.TitleFontName, True);
    JsonWriteInteger('titleFontSize', EmbeddedBoard.TitleFontSize, True);
    JsonWriteInteger('titleObject', EmbeddedBoard.TitleObject, True);
    JsonWriteBoolean('transmitBoardShape', EmbeddedBoard.TransmitBoardShape, True);
    JsonWriteBoolean('transmitDimensions', EmbeddedBoard.TransmitDimensions, True);
    JsonWriteBoolean('transmitDrillTable', EmbeddedBoard.TransmitDrillTable, True);
    JsonWriteBoolean('transmitLayerStackTable', EmbeddedBoard.TransmitLayerStackTable, True);
    JsonWriteInteger('transmitParametersCount', EmbeddedBoard.TransmitParametersCount, True);
    JsonWriteString('viewConfig', EmbeddedBoard.ViewConfig, True);
    JsonWriteString('viewConfigType', EmbeddedBoard.ViewConfigType, True);
    JsonWriteInteger('viewportRect', EmbeddedBoard.ViewportRect, True);
    JsonWriteString('viewportTitle', EmbeddedBoard.ViewportTitle, True);
    JsonWriteBoolean('viewportVisible', EmbeddedBoard.ViewportVisible, True);
    JsonWriteCoord('x1Location', EmbeddedBoard.X1Location, True);
    JsonWriteCoord('x2Location', EmbeddedBoard.X2Location, True);
    JsonWriteCoord('xLocation', EmbeddedBoard.XLocation, True);
    JsonWriteCoord('y1Location', EmbeddedBoard.Y1Location, True);
    JsonWriteCoord('y2Location', EmbeddedBoard.Y2Location, True);
    JsonWriteCoord('yLocation', EmbeddedBoard.YLocation, True);
    // Base primitive properties
    ExportPcbBasePrimitiveProperties(EmbeddedBoard);


    // Object reference and indexed properties
    try
        if EmbeddedBoard.ChildBoard <> nil then
            JsonWriteString('childBoard_ref', 'present', True)
        else
            JsonWriteString('childBoard_ref', '', True);
    except
        JsonWriteString('childBoard_ref', 'ERROR', True);
    end;
    try
        if EmbeddedBoard.VisibleLayers <> nil then
            JsonWriteString('visibleLayers_ref', 'present', True)
        else
            JsonWriteString('visibleLayers_ref', '', True);
    except
        JsonWriteString('visibleLayers_ref', 'ERROR', True);
    end;
    try
        JsonWriteBoolean('transmitLayersEnabled_top', EmbeddedBoard.TransmitLayersEnabled[eTopLayer], False);
    except
        JsonWriteString('transmitLayersEnabled_top', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbEmbeddedToJson(Embedded: IPCB_Embedded; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Embedded', True);

    // Position and layer
    JsonWriteInteger('layer', Embedded.Layer, True);


    // Additional IPCB_Embedded properties
    JsonWriteString('description', Embedded.Description, True);
    JsonWriteString('name', Embedded.Name, True);
    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Embedded);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbAngularDimensionToJson(Dim: IPCB_AngularDimension; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'AngularDimension', True);

    // Base dimension properties
    JsonWriteInteger('dimensionKind', Dim.DimensionKind, True);
    JsonWriteFloat('textValue', Dim.TextValue, True);
    JsonWriteCoord('textX', Dim.TextX, True);
    JsonWriteCoord('textY', Dim.TextY, True);
    JsonWriteCoord('textHeight', Dim.TextHeight, True);
    JsonWriteCoord('textWidth', Dim.TextWidth, True);
    JsonWriteString('textPrefix', Dim.TextPrefix, True);
    JsonWriteString('textSuffix', Dim.TextSuffix, True);
    JsonWriteInteger('textPrecision', Dim.TextPrecision, True);
    JsonWriteInteger('layer', Dim.Layer, True);
    JsonWriteCoord('lineWidth', Dim.LineWidth, True);
    JsonWriteCoord('arrowSize', Dim.ArrowSize, True);

    // Angular-specific properties
    JsonWriteFloat('angle', Dim.Angle, True);


    // Additional IPCB_AngularDimension properties
    JsonWriteCoord('arrowLength', Dim.ArrowLength, True);
    JsonWriteCoord('arrowLineWidth', Dim.ArrowLineWidth, True);
    JsonWriteInteger('arrowPosition', Dim.ArrowPosition, True);
    JsonWriteBoolean('bold', Dim.Bold, True);
    JsonWriteCoord('extensionLineWidth', Dim.ExtensionLineWidth, True);
    JsonWriteCoord('extensionOffset', Dim.ExtensionOffset, True);
    JsonWriteCoord('extensionPickGap', Dim.ExtensionPickGap, True);
    JsonWriteString('fontName', Dim.FontName, True);
    JsonWriteBoolean('inlet', Dim.Inlet, True);
    JsonWriteBoolean('italic', Dim.Italic, True);
    JsonWriteBoolean('primitiveLock', Dim.PrimitiveLock, True);
    JsonWriteCoord('radius', Dim.Radius, True);
    JsonWriteInteger('references_Count', Dim.References_Count, True);
    JsonWriteInteger('sector', Dim.Sector, True);
    JsonWriteCoord('size', Dim.Size, True);
    JsonWriteInteger('style', Dim.Style, True);
    JsonWriteInteger('textDimensionUnit', Dim.TextDimensionUnit, True);
    JsonWriteInteger('textFont', Dim.TextFont, True);
    JsonWriteString('textFormat', Dim.TextFormat, True);
    JsonWriteCoord('textGap', Dim.TextGap, True);
    JsonWriteCoord('textLineWidth', Dim.TextLineWidth, True);
    JsonWriteInteger('textPosition', Dim.TextPosition, True);
    JsonWriteBoolean('useTTFonts', Dim.UseTTFonts, True);
    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Dim);


    try
        JsonWriteBoolean('layerUsed_top', Dim.LayerUsed[eTopLayer], True);
    except
        JsonWriteString('layerUsed_top', 'ERROR', True);
    end;
    try
        JsonWriteInteger('references_0', Dim.References[0].ReferencePoint.X, True);
    except
        JsonWriteString('references_0', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbBaselineDimensionToJson(Dim: IPCB_BaselineDimension; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BaselineDimension', True);

    // Base dimension properties
    JsonWriteInteger('dimensionKind', Dim.DimensionKind, True);
    JsonWriteFloat('textValue', Dim.TextValue, True);
    JsonWriteCoord('textX', Dim.TextX, True);
    JsonWriteCoord('textY', Dim.TextY, True);
    JsonWriteCoord('textHeight', Dim.TextHeight, True);
    JsonWriteInteger('layer', Dim.Layer, True);
    JsonWriteCoord('lineWidth', Dim.LineWidth, True);
    JsonWriteCoord('arrowSize', Dim.ArrowSize, True);


    // Additional IPCB_BaselineDimension properties
    JsonWriteInteger('angle', Dim.Angle, True);
    JsonWriteCoord('arrowLength', Dim.ArrowLength, True);
    JsonWriteCoord('arrowLineWidth', Dim.ArrowLineWidth, True);
    JsonWriteInteger('arrowPosition', Dim.ArrowPosition, True);
    JsonWriteBoolean('bold', Dim.Bold, True);
    JsonWriteCoord('extensionLineWidth', Dim.ExtensionLineWidth, True);
    JsonWriteCoord('extensionOffset', Dim.ExtensionOffset, True);
    JsonWriteCoord('extensionPickGap', Dim.ExtensionPickGap, True);
    JsonWriteString('fontName', Dim.FontName, True);
    JsonWriteBoolean('italic', Dim.Italic, True);
    JsonWriteBoolean('primitiveLock', Dim.PrimitiveLock, True);
    JsonWriteInteger('references_Count', Dim.References_Count, True);
    JsonWriteCoord('size', Dim.Size, True);
    JsonWriteInteger('style', Dim.Style, True);
    JsonWriteInteger('textDimensionUnit', Dim.TextDimensionUnit, True);
    JsonWriteInteger('textFont', Dim.TextFont, True);
    JsonWriteString('textFormat', Dim.TextFormat, True);
    JsonWriteCoord('textGap', Dim.TextGap, True);
    JsonWriteCoord('textLineWidth', Dim.TextLineWidth, True);
    JsonWriteInteger('textLocationsCount', Dim.TextLocationsCount, True);
    JsonWriteInteger('textPosition', Dim.TextPosition, True);
    JsonWriteInteger('textPrecision', Dim.TextPrecision, True);
    JsonWriteString('textPrefix', Dim.TextPrefix, True);
    JsonWriteString('textSuffix', Dim.TextSuffix, True);
    JsonWriteCoord('textWidth', Dim.TextWidth, True);
    JsonWriteBoolean('useTTFonts', Dim.UseTTFonts, True);
    JsonWriteCoord('x', Dim.x, True);
    JsonWriteCoord('x1Location', Dim.X1Location, True);
    JsonWriteCoord('y', Dim.y, True);
    JsonWriteCoord('y1Location', Dim.Y1Location, True);
    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Dim);


    try
        JsonWriteBoolean('layerUsed_top', Dim.LayerUsed[eTopLayer], True);
    except
        JsonWriteString('layerUsed_top', 'ERROR', True);
    end;
    try
        JsonWriteInteger('references_0', Dim.References[0].ReferencePoint.X, True);
    except
        JsonWriteString('references_0', 'ERROR', True);
    end;
    try
        JsonWriteInteger('textLocations_0_X', Dim.TextLocations[0].X, True);
    except
        JsonWriteString('textLocations_0', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbCenterDimensionToJson(Dim: IPCB_CenterDimension; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CenterDimension', True);

    // Base dimension properties
    JsonWriteInteger('dimensionKind', Dim.DimensionKind, True);
    JsonWriteCoord('textHeight', Dim.TextHeight, True);
    JsonWriteInteger('layer', Dim.Layer, True);
    JsonWriteCoord('lineWidth', Dim.LineWidth, True);
    JsonWriteCoord('size', Dim.Size, True);


    // Additional IPCB_CenterDimension properties
    JsonWriteInteger('angle', Dim.Angle, True);
    JsonWriteCoord('arrowLength', Dim.ArrowLength, True);
    JsonWriteCoord('arrowLineWidth', Dim.ArrowLineWidth, True);
    JsonWriteInteger('arrowPosition', Dim.ArrowPosition, True);
    JsonWriteCoord('arrowSize', Dim.ArrowSize, True);
    JsonWriteBoolean('bold', Dim.Bold, True);
    JsonWriteCoord('extensionLineWidth', Dim.ExtensionLineWidth, True);
    JsonWriteCoord('extensionOffset', Dim.ExtensionOffset, True);
    JsonWriteCoord('extensionPickGap', Dim.ExtensionPickGap, True);
    JsonWriteBoolean('fastSetState_XSizeYSize', Dim.FastSetState_XSizeYSize, True);
    JsonWriteString('fontName', Dim.FontName, True);
    JsonWriteBoolean('italic', Dim.Italic, True);
    JsonWriteBoolean('primitiveLock', Dim.PrimitiveLock, True);
    JsonWriteInteger('references_Count', Dim.References_Count, True);
    JsonWriteBoolean('references_Validate', Dim.References_Validate, True);
    JsonWriteInteger('requiredParamterSpace', Dim.RequiredParamterSpace, True);
    JsonWriteInteger('style', Dim.Style, True);
    JsonWriteInteger('textDimensionUnit', Dim.TextDimensionUnit, True);
    JsonWriteInteger('textFont', Dim.TextFont, True);
    JsonWriteString('textFormat', Dim.TextFormat, True);
    JsonWriteCoord('textGap', Dim.TextGap, True);
    JsonWriteCoord('textLineWidth', Dim.TextLineWidth, True);
    JsonWriteInteger('textPosition', Dim.TextPosition, True);
    JsonWriteInteger('textPrecision', Dim.TextPrecision, True);
    JsonWriteString('textPrefix', Dim.TextPrefix, True);
    JsonWriteString('textSuffix', Dim.TextSuffix, True);
    JsonWriteInteger('textValue', Dim.TextValue, True);
    JsonWriteCoord('textWidth', Dim.TextWidth, True);
    JsonWriteCoord('textX', Dim.TextX, True);
    JsonWriteCoord('textY', Dim.TextY, True);
    JsonWriteBoolean('useTTFonts', Dim.UseTTFonts, True);
    JsonWriteCoord('x', Dim.x, True);
    JsonWriteCoord('x1Location', Dim.X1Location, True);
    JsonWriteCoord('y', Dim.y, True);
    JsonWriteCoord('y1Location', Dim.Y1Location, True);
    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Dim);


    try
        JsonWriteBoolean('layerUsed_top', Dim.LayerUsed[eTopLayer], True);
    except
        JsonWriteString('layerUsed_top', 'ERROR', True);
    end;
    try
        JsonWriteInteger('references_0', Dim.References[0].ReferencePoint.X, True);
    except
        JsonWriteString('references_0', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbDatumDimensionToJson(Dim: IPCB_DatumDimension; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DatumDimension', True);

    // Base dimension properties
    JsonWriteInteger('dimensionKind', Dim.DimensionKind, True);
    JsonWriteCoord('textHeight', Dim.TextHeight, True);
    JsonWriteInteger('layer', Dim.Layer, True);
    JsonWriteCoord('lineWidth', Dim.LineWidth, True);

    // Datum-specific
    JsonWriteString('text', Dim.Text, True);


    // Additional IPCB_DatumDimension properties
    JsonWriteInteger('angle', Dim.Angle, True);
    JsonWriteCoord('arrowLength', Dim.ArrowLength, True);
    JsonWriteCoord('arrowLineWidth', Dim.ArrowLineWidth, True);
    JsonWriteInteger('arrowPosition', Dim.ArrowPosition, True);
    JsonWriteCoord('arrowSize', Dim.ArrowSize, True);
    JsonWriteBoolean('bold', Dim.Bold, True);
    JsonWriteCoord('extensionLineWidth', Dim.ExtensionLineWidth, True);
    JsonWriteCoord('extensionOffset', Dim.ExtensionOffset, True);
    JsonWriteCoord('extensionPickGap', Dim.ExtensionPickGap, True);
    JsonWriteString('fontName', Dim.FontName, True);
    JsonWriteBoolean('italic', Dim.Italic, True);
    JsonWriteBoolean('primitiveLock', Dim.PrimitiveLock, True);
    JsonWriteInteger('references_Count', Dim.References_Count, True);
    JsonWriteCoord('size', Dim.Size, True);
    JsonWriteInteger('style', Dim.Style, True);
    JsonWriteInteger('textDimensionUnit', Dim.TextDimensionUnit, True);
    JsonWriteInteger('textFont', Dim.TextFont, True);
    JsonWriteString('textFormat', Dim.TextFormat, True);
    JsonWriteCoord('textGap', Dim.TextGap, True);
    JsonWriteCoord('textLineWidth', Dim.TextLineWidth, True);
    JsonWriteInteger('textPosition', Dim.TextPosition, True);
    JsonWriteInteger('textPrecision', Dim.TextPrecision, True);
    JsonWriteString('textPrefix', Dim.TextPrefix, True);
    JsonWriteString('textSuffix', Dim.TextSuffix, True);
    JsonWriteInteger('textValue', Dim.TextValue, True);
    JsonWriteCoord('textWidth', Dim.TextWidth, True);
    JsonWriteCoord('textX', Dim.TextX, True);
    JsonWriteCoord('textY', Dim.TextY, True);
    JsonWriteBoolean('useTTFonts', Dim.UseTTFonts, True);
    JsonWriteCoord('x', Dim.x, True);
    JsonWriteCoord('x1Location', Dim.X1Location, True);
    JsonWriteCoord('y', Dim.y, True);
    JsonWriteCoord('y1Location', Dim.Y1Location, True);
    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Dim);


    try
        JsonWriteBoolean('layerUsed_top', Dim.LayerUsed[eTopLayer], True);
    except
        JsonWriteString('layerUsed_top', 'ERROR', True);
    end;
    try
        JsonWriteInteger('references_0', Dim.References[0].ReferencePoint.X, True);
    except
        JsonWriteString('references_0', 'ERROR', True);
    end;
    try
        if Dim.Extension_Track[0] <> nil then
            JsonWriteString('extension_Track_0_ref', 'present', True)
        else
            JsonWriteString('extension_Track_0_ref', '', True);
    except
        JsonWriteString('extension_Track_0_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbNetToJson(Net: IPCB_Net; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Net', True);

    // Net identification
    JsonWriteString('name', Net.Name, True);
    JsonWriteInteger('netId', Net.NetId, True);
    JsonWriteString('uniqueId', Net.UniqueId, True);

    // Net properties
    JsonWriteInteger('routedLength', Net.RoutedLength, True);
    JsonWriteInteger('nodeDRCCount', Net.NodeDRCCount, True);
    JsonWriteInteger('primitiveCount', Net.PrimitiveCount, True);
    JsonWriteBoolean('selected', Net.Selected, True);
    JsonWriteBoolean('enabled', Net.Enabled, True);

    // Net color
    JsonWriteInteger('color', Net.Color, True);

    // Net-specific properties
    JsonWriteBoolean('connectivelyInvalid', Net.ConnectivelyInvalid, True);
    JsonWriteBoolean('connectsVisible', Net.ConnectsVisible, True);
    JsonWriteBoolean('inDifferentialPair', Net.InDifferentialPair, True);
    JsonWriteBoolean('jumpersVisible', Net.JumpersVisible, True);
    JsonWriteInteger('liveHighlightMode', Net.LiveHighlightMode, True);
    JsonWriteBoolean('loopRemoval', Net.LoopRemoval, True);
    JsonWriteBoolean('overrideColorForDraw', Net.OverrideColorForDraw, True);
    JsonWriteInteger('pinCount', Net.PinCount, True);
    JsonWriteBoolean('primitiveLock', Net.PrimitiveLock, True);
    JsonWriteCoord('routeLength', Net.RouteLength, True);
    JsonWriteInteger('viaCount', Net.ViaCount, True);
    JsonWriteCoord('x', Net.x, True);
    JsonWriteCoord('y', Net.y, True);

    // IPCB_Net2 extension properties
    try
        JsonWriteBoolean('isDisabledLength', Net.IsDisabledLength, True);
    except
        JsonWriteString('isDisabledLength', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteCoord('targetLength', Net.TargetLength, True);
    except
        JsonWriteString('targetLength', 'ERROR: Could not read property', True);
    end;

    // Base primitive properties (inherited from IPCB_Primitive)
    ExportPcbBasePrimitiveProperties(Net);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbNetClassToJson(NetClass: IPCB_ObjectClass; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'NetClass', True);

    // Class identification
    JsonWriteString('name', NetClass.Name, True);
    JsonWriteString('superClass', NetClass.SuperClass, True);
    JsonWriteString('subClass', NetClass.SubClass, True);
    JsonWriteString('uniqueId', NetClass.UniqueId, True);

    // Class properties
    JsonWriteInteger('memberCount', NetClass.MemberCount, True);
    JsonWriteBoolean('isFreeClass', NetClass.IsFreeClass, True);
    JsonWriteBoolean('isSystemClass', NetClass.IsSystemClass, True);

    // IPCB_ObjectClass specific properties
    JsonWriteInteger('memberKind', NetClass.MemberKind, True);

    // IPCB_ObjectClass1 extension properties
    try
        JsonWriteString('displayName', NetClass.DisplayName, True);
    except
        JsonWriteString('displayName', 'ERROR: Could not read property', True);
    end;
    try
        JsonWriteInteger('membersCount', NetClass.MembersCount, True);
    except
        JsonWriteString('membersCount', 'ERROR: Could not read property', True);
    end;

    // State
    JsonWriteBoolean('selected', NetClass.Selected, True);
    JsonWriteBoolean('enabled', NetClass.Enabled, True);

    // Base primitive properties (inherited from IPCB_Primitive)
    ExportPcbBasePrimitiveProperties(NetClass);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbDifferentialPairToJson(DiffPair: IPCB_DifferentialPair; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DifferentialPair', True);

    // Pair identification
    JsonWriteString('name', DiffPair.Name, True);
    JsonWriteString('positiveNetName', DiffPair.PositiveNetName, True);
    JsonWriteString('negativeNetName', DiffPair.NegativeNetName, True);
    JsonWriteString('uniqueId', DiffPair.UniqueId, True);

    // State
    JsonWriteBoolean('selected', DiffPair.Selected, True);
    JsonWriteBoolean('enabled', DiffPair.Enabled, True);


    // Additional IPCB_DifferentialPair properties
    JsonWriteBoolean('allowGlobalEdit', DiffPair.AllowGlobalEdit, True);
    JsonWriteString('descriptor', DiffPair.Descriptor, True);
    JsonWriteString('detail', DiffPair.Detail, True);
    JsonWriteBoolean('drawAsPreview', DiffPair.DrawAsPreview, True);
    JsonWriteBoolean('dRCError', DiffPair.DRCError, True);
    JsonWriteBoolean('enableDraw', DiffPair.EnableDraw, True);
    JsonWriteBoolean('enabled_Direct', DiffPair.Enabled_Direct, True);
    JsonWriteBoolean('enabled_vComponent', DiffPair.Enabled_vComponent, True);
    JsonWriteBoolean('enabled_vCoordinate', DiffPair.Enabled_vCoordinate, True);
    JsonWriteBoolean('enabled_vDimension', DiffPair.Enabled_vDimension, True);
    JsonWriteBoolean('enabled_vNet', DiffPair.Enabled_vNet, True);
    JsonWriteBoolean('enabled_vPolygon', DiffPair.Enabled_vPolygon, True);
    JsonWriteBoolean('gatherControl', DiffPair.GatherControl, True);
    JsonWriteString('handle', DiffPair.Handle, True);
    JsonWriteString('identifier', DiffPair.Identifier, True);
    JsonWriteBoolean('inBoard', DiffPair.InBoard, True);
    JsonWriteBoolean('inComponent', DiffPair.InComponent, True);
    JsonWriteBoolean('inCoordinate', DiffPair.InCoordinate, True);
    JsonWriteInteger('index', DiffPair.Index, True);
    JsonWriteBoolean('inDimension', DiffPair.InDimension, True);
    JsonWriteBoolean('inNet', DiffPair.InNet, True);
    JsonWriteBoolean('inPolygon', DiffPair.InPolygon, True);
    JsonWriteBoolean('isAssyTestpoint_Bottom', DiffPair.IsAssyTestpoint_Bottom, True);
    JsonWriteBoolean('isAssyTestpoint_Top', DiffPair.IsAssyTestpoint_Top, True);
    JsonWriteBoolean('isElectricalPrim', DiffPair.IsElectricalPrim, True);
    JsonWriteBoolean('isKeepout', DiffPair.IsKeepout, True);
    JsonWriteBoolean('isPreRoute', DiffPair.IsPreRoute, True);
    JsonWriteBoolean('isTenting', DiffPair.IsTenting, True);
    JsonWriteBoolean('isTenting_Bottom', DiffPair.IsTenting_Bottom, True);
    JsonWriteBoolean('isTenting_Top', DiffPair.IsTenting_Top, True);
    JsonWriteBoolean('isTestpoint_Bottom', DiffPair.IsTestpoint_Bottom, True);
    JsonWriteBoolean('isTestpoint_Top', DiffPair.IsTestpoint_Top, True);
    JsonWriteInteger('layer', DiffPair.Layer, True);
    JsonWriteInteger('layer_V6', DiffPair.Layer_V6, True);
    JsonWriteBoolean('miscFlag1', DiffPair.MiscFlag1, True);
    JsonWriteBoolean('miscFlag2', DiffPair.MiscFlag2, True);
    JsonWriteBoolean('miscFlag3', DiffPair.MiscFlag3, True);
    JsonWriteBoolean('moveable', DiffPair.Moveable, True);
    JsonWriteInteger('objectId', DiffPair.ObjectId, True);
    JsonWriteString('objectIDString', DiffPair.ObjectIDString, True);
    JsonWriteBoolean('padCacheRobotFlag', DiffPair.PadCacheRobotFlag, True);
    JsonWriteCoord('pasteMaskExpansion', DiffPair.PasteMaskExpansion, True);
    JsonWriteBoolean('polygonOutline', DiffPair.PolygonOutline, True);
    JsonWriteCoord('powerPlaneClearance', DiffPair.PowerPlaneClearance, True);
    JsonWriteInteger('powerPlaneConnectStyle', DiffPair.PowerPlaneConnectStyle, True);
    JsonWriteCoord('powerPlaneReliefExpansion', DiffPair.PowerPlaneReliefExpansion, True);
    JsonWriteCoord('reliefAirGap', DiffPair.ReliefAirGap, True);
    JsonWriteCoord('reliefConductorWidth', DiffPair.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', DiffPair.ReliefEntries, True);
    JsonWriteCoord('solderMaskExpansion', DiffPair.SolderMaskExpansion, True);
    JsonWriteBoolean('tearDrop', DiffPair.TearDrop, True);
    JsonWriteInteger('unionIndex', DiffPair.UnionIndex, True);
    JsonWriteBoolean('used', DiffPair.Used, True);
    JsonWriteBoolean('userRouted', DiffPair.UserRouted, True);
    JsonWriteInteger('viewableObjectID', DiffPair.ViewableObjectID, True);

    // Object reference properties
    try
        if DiffPair.Board <> nil then
            JsonWriteString('board_ref', 'present', True)
        else
            JsonWriteString('board_ref', '', True);
    except
        JsonWriteString('board_ref', 'ERROR', True);
    end;
    try
        if DiffPair.Component <> nil then
            JsonWriteString('component_ref', 'present', True)
        else
            JsonWriteString('component_ref', '', True);
    except
        JsonWriteString('component_ref', 'ERROR', True);
    end;
    try
        if DiffPair.Coordinate <> nil then
            JsonWriteString('coordinate_ref', 'present', True)
        else
            JsonWriteString('coordinate_ref', '', True);
    except
        JsonWriteString('coordinate_ref', 'ERROR', True);
    end;
    try
        if DiffPair.Dimension <> nil then
            JsonWriteString('dimension_ref', 'present', True)
        else
            JsonWriteString('dimension_ref', '', True);
    except
        JsonWriteString('dimension_ref', 'ERROR', True);
    end;
    try
        if DiffPair.Net <> nil then
            JsonWriteString('net_ref', 'present', True)
        else
            JsonWriteString('net_ref', '', True);
    except
        JsonWriteString('net_ref', 'ERROR', True);
    end;
    try
        if DiffPair.NegativeNet <> nil then
            JsonWriteString('negativeNet_ref', 'present', True)
        else
            JsonWriteString('negativeNet_ref', '', True);
    except
        JsonWriteString('negativeNet_ref', 'ERROR', True);
    end;
    try
        if DiffPair.Polygon <> nil then
            JsonWriteString('polygon_ref', 'present', True)
        else
            JsonWriteString('polygon_ref', '', True);
    except
        JsonWriteString('polygon_ref', 'ERROR', True);
    end;
    try
        if DiffPair.PositiveNet <> nil then
            JsonWriteString('positiveNet_ref', 'present', True)
        else
            JsonWriteString('positiveNet_ref', '', True);
    except
        JsonWriteString('positiveNet_ref', 'ERROR', True);
    end;
    try
        JsonWriteBoolean('inSelectionMemory_0', DiffPair.InSelectionMemory[0], False);
    except
        JsonWriteString('inSelectionMemory_0', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbFromToToJson(FromTo: IPCB_FromTo; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'FromTo', True);

    // Connection points
    JsonWriteCoord('x1', FromTo.X1, True);
    JsonWriteCoord('y1', FromTo.Y1, True);
    JsonWriteCoord('x2', FromTo.X2, True);
    JsonWriteCoord('y2', FromTo.Y2, True);
    JsonWriteInteger('layer1', FromTo.Layer1, True);
    JsonWriteInteger('layer2', FromTo.Layer2, True);

    // Net
    if FromTo.Net <> nil then
        JsonWriteString('net', FromTo.Net.Name, True)
    else
        JsonWriteString('net', '', True);

    // Base primitive properties
    ExportPcbBasePrimitiveProperties(FromTo);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbLayerStackToJson(Board: IPCB_Board; AddComma: Boolean);
var
    LayerStack: IPCB_LayerStack;
    Layer: IPCB_LayerObject;
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'LayerStack', True);

    LayerStack := Board.LayerStack;
    if LayerStack <> nil then
    begin
        JsonWriteInteger('layerCount', LayerStack.LayersCount, True);
        JsonWriteInteger('signalLayerCount', LayerStack.SignalLayerCount, True);

        JsonOpenArray('layers');
        for I := 1 to LayerStack.LayersCount do
        begin
            Layer := LayerStack.LayerObject[I];
            if Layer <> nil then
            begin
                JsonOpenObject('');
                JsonWriteString('name', Layer.Name, True);
                JsonWriteInteger('layerId', Layer.V7_LayerID.ID, True);
                JsonWriteBoolean('isSignalLayer', Layer.IsSignalLayer, True);
                JsonWriteBoolean('isInternalPlane', Layer.IsInternalPlane, True);
                JsonWriteBoolean('isDielectric', Layer.IsDielectric, True);
                JsonWriteCoord('copperThickness', Layer.CopperThickness, True);
                JsonWriteFloat('dielectricConstant', Layer.DielectricConst, True);
                JsonCloseObject(I < LayerStack.LayersCount);
            end;
        end;
        JsonCloseArray(False);
    end;


    // Additional IPCB_Board properties
    JsonWriteBoolean('allowGlobalEdit', Board.AllowGlobalEdit, True);
    JsonWriteInteger('bigVisibleGridMultFactor', Board.BigVisibleGridMultFactor, True);
    JsonWriteInteger('componentGridUnit', Board.ComponentGridUnit, True);
    JsonWriteInteger('currentLayerV6', Board.CurrentLayerV6, True);
    JsonWriteString('descriptor', Board.Descriptor, True);
    JsonWriteString('detail', Board.Detail, True);
    JsonWriteBoolean('drawAsPreview', Board.DrawAsPreview, True);
    JsonWriteBoolean('drawDotGrid', Board.DrawDotGrid, True);
    JsonWriteBoolean('dRCError', Board.DRCError, True);
    JsonWriteBoolean('enabled', Board.Enabled, True);
    JsonWriteBoolean('enableDraw', Board.EnableDraw, True);
    JsonWriteBoolean('enabled_Direct', Board.Enabled_Direct, True);
    JsonWriteBoolean('enabled_vComponent', Board.Enabled_vComponent, True);
    JsonWriteBoolean('enabled_vCoordinate', Board.Enabled_vCoordinate, True);
    JsonWriteBoolean('enabled_vDimension', Board.Enabled_vDimension, True);
    JsonWriteBoolean('enabled_vNet', Board.Enabled_vNet, True);
    JsonWriteBoolean('enabled_vPolygon', Board.Enabled_vPolygon, True);
    JsonWriteString('handle', Board.Handle, True);
    JsonWriteBoolean('hasServerDocument', Board.HasServerDocument, True);
    JsonWriteString('identifier', Board.Identifier, True);
    JsonWriteBoolean('inBoard', Board.InBoard, True);
    JsonWriteBoolean('inComponent', Board.InComponent, True);
    JsonWriteBoolean('inCoordinate', Board.InCoordinate, True);
    JsonWriteInteger('index', Board.Index, True);
    JsonWriteBoolean('inDimension', Board.InDimension, True);
    JsonWriteBoolean('inNet', Board.InNet, True);
    JsonWriteBoolean('inPolygon', Board.InPolygon, True);
    JsonWriteBoolean('isAssyTestpoint_Bottom', Board.IsAssyTestpoint_Bottom, True);
    JsonWriteBoolean('isAssyTestpoint_Top', Board.IsAssyTestpoint_Top, True);
    JsonWriteBoolean('isElectricalPrim', Board.IsElectricalPrim, True);
    JsonWriteBoolean('isKeepout', Board.IsKeepout, True);
    JsonWriteBoolean('isPreRoute', Board.IsPreRoute, True);
    JsonWriteBoolean('isTenting', Board.IsTenting, True);
    JsonWriteBoolean('isTenting_Bottom', Board.IsTenting_Bottom, True);
    JsonWriteBoolean('isTenting_Top', Board.IsTenting_Top, True);
    JsonWriteBoolean('isTestpoint_Bottom', Board.IsTestpoint_Bottom, True);
    JsonWriteBoolean('isTestpoint_Top', Board.IsTestpoint_Top, True);
    JsonWriteInteger('layer_V6', Board.Layer_V6, True);
    JsonWriteBoolean('miscFlag1', Board.MiscFlag1, True);
    JsonWriteBoolean('miscFlag2', Board.MiscFlag2, True);
    JsonWriteBoolean('miscFlag3', Board.MiscFlag3, True);
    JsonWriteBoolean('moveable', Board.Moveable, True);
    JsonWriteString('objectIDString', Board.ObjectIDString, True);
    JsonWriteBoolean('padCacheRobotFlag', Board.PadCacheRobotFlag, True);
    JsonWriteCoord('pasteMaskExpansion', Board.PasteMaskExpansion, True);
    try
        JsonWriteString('pCBWindow', 'OBJECT_REF', True);
    except
        JsonWriteString('pCBWindow', 'ERROR', True);
    end;
    JsonWriteInteger('polygonNameTemplate', Board.PolygonNameTemplate, True);
    JsonWriteBoolean('polygonOutline', Board.PolygonOutline, True);
    JsonWriteCoord('powerPlaneClearance', Board.PowerPlaneClearance, True);
    JsonWriteInteger('powerPlaneConnectStyle', Board.PowerPlaneConnectStyle, True);
    JsonWriteCoord('powerPlaneReliefExpansion', Board.PowerPlaneReliefExpansion, True);
    JsonWriteCoord('reliefAirGap', Board.ReliefAirGap, True);
    JsonWriteCoord('reliefConductorWidth', Board.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', Board.ReliefEntries, True);
    JsonWriteInteger('routeToolPathLayer', Board.RouteToolPathLayer, True);
    JsonWriteInteger('selectedObjectCount', Board.SelectedObjectCount, True);
    JsonWriteBoolean('selected', Board.Selected, True);

    // IPCB_Board3D extension properties
    JsonWriteInteger('c3DVersion', Board.C3DVersion, True);
    JsonWriteFloat('snapGridSize', Board.SnapGridSize, True);
    JsonWriteFloat('snapGridSizeX', Board.SnapGridSizeX, True);
    JsonWriteFloat('snapGridSizeY', Board.SnapGridSizeY, True);
    JsonWriteInteger('snapGridUnit', Ord(Board.SnapGridUnit), True);
    JsonWriteCoord('solderMaskExpansion', Board.SolderMaskExpansion, True);
    JsonWriteString('substrateFileName', Board.SubstrateFileName, True);
    JsonWriteBoolean('tearDrop', Board.TearDrop, True);
    JsonWriteFloat('trackGridSize', Board.TrackGridSize, True);
    JsonWriteInteger('unionIndex', Board.UnionIndex, True);
    JsonWriteString('uniqueId', Board.UniqueId, True);
    JsonWriteBoolean('used', Board.Used, True);
    JsonWriteBoolean('userRouted', Board.UserRouted, True);
    JsonWriteFloat('viaGridSize', Board.ViaGridSize, True);
    JsonWriteInteger('viewableObjectID', Board.ViewableObjectID, True);
    JsonWriteBoolean('viewConfigDisplaySpecialStrings', Board.ViewConfigDisplaySpecialStrings, True);
    JsonWriteBoolean('viewConfigIs3D', Board.ViewConfigIs3D, True);
    JsonWriteInteger('viewConfigTopSolderMaskColor3D', Board.ViewConfigTopSolderMaskColor3D, True);
    JsonWriteFloat('visibleGridMultFactor', Board.VisibleGridMultFactor, True);
    JsonWriteFloat('visibleGridSize', Board.VisibleGridSize, True);
    JsonWriteInteger('visibleGridUnit', Ord(Board.VisibleGridUnit), True);
    JsonWriteCoord('worldXOrigin', Board.WorldXOrigin, True);
    JsonWriteCoord('worldYOrigin', Board.WorldYOrigin, True);
    JsonWriteCoord('xCursor', Board.XCursor, True);
    JsonWriteCoord('xOrigin', Board.XOrigin, True);
    JsonWriteCoord('yCursor', Board.YCursor, True);
    JsonWriteCoord('yOrigin', Board.YOrigin, True);

    // IPCB_BoardEx extension properties
    JsonWriteBoolean('hasClearanceMatrix', Board.HasClearanceMatrix, True);
    JsonWriteBoolean('inSingleLayerMode', Board.InSingleLayerMode, True);
    JsonWriteBoolean('isLocked', Board.IsLocked, True);
    JsonWriteBoolean('selectedObjects_FilterDisabled', Board.SelectedObjects_FilterDisabled, True);
    JsonWriteInteger('singleLayer', Board.SingleLayer, True);
    JsonWriteBoolean('zoomOnViolations', Board.ZoomOnViolations, True);

    // IPCB_BoardEx2 extension properties
    JsonWriteBoolean('customShapeCompatibilityMode', Board.CustomShapeCompatibilityMode, True);
    JsonWriteBoolean('rigidFlexAdvanced', Board.RigidFlexAdvanced, True);
    JsonWriteBoolean('separateComponentLayers', Board.SeparateComponentLayers, False);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbContourToJson(Contour: IPCB_Contour; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Contour', True);

    // Contour properties
    JsonWriteInteger('pointCount', Contour.Count, True);

    JsonOpenArray('points');
    for I := 0 to Contour.Count - 1 do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', Contour.X[I], True);
        JsonWriteInteger('y', Contour.Y[I], False);
        JsonCloseObject(I < Contour.Count - 1);
    end;
    JsonCloseArray(False);


    // Additional IPCB_Contour properties
    JsonWriteFloat('area', Contour.Area, True);
    try
        JsonWriteString('vertexList', 'OBJECT_REF', True);
    except
        JsonWriteString('vertexList', 'ERROR', True);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbCopperBodyToJson(CopperBody: IPCB_CopperBody; AddComma: Boolean);
var
    I, J: Integer;
    Contour: IPCB_Contour;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CopperBody', True);

    // Layer and properties
    JsonWriteInteger('layer', CopperBody.Layer, True);

    // Net information
    if CopperBody.Net <> nil then
        JsonWriteString('net', CopperBody.Net.Name, True)
    else
        JsonWriteString('net', '', True);

    // Geometry
    JsonWriteInteger('holeCount', CopperBody.HoleCount, True);

    // Main contour
    Contour := CopperBody.MainContour;
    if Contour <> nil then
    begin
        JsonOpenArray('mainContour');
        for I := 0 to Contour.Count - 1 do
        begin
            JsonOpenObject('');
            JsonWriteInteger('x', Contour.X[I], True);
            JsonWriteInteger('y', Contour.Y[I], False);
            JsonCloseObject(I < Contour.Count - 1);
        end;
        JsonCloseArray(True);
    end;

    // Holes
    if CopperBody.HoleCount > 0 then
    begin
        JsonOpenArray('holes');
        for I := 0 to CopperBody.HoleCount - 1 do
        begin
            Contour := CopperBody.Holes[I];
            if Contour <> nil then
            begin
                JsonOpenArray('');
                for J := 0 to Contour.Count - 1 do
                begin
                    JsonOpenObject('');
                    JsonWriteInteger('x', Contour.X[J], True);
                    JsonWriteInteger('y', Contour.Y[J], False);
                    JsonCloseObject(J < Contour.Count - 1);
                end;
                JsonCloseArray(I < CopperBody.HoleCount - 1);
            end;
        end;
        JsonCloseArray(True);
    end;


    // Additional IPCB_CopperBody properties
    JsonWriteInteger('shapeCount', CopperBody.ShapeCount, True);
    // Base primitive properties
    ExportPcbBasePrimitiveProperties(CopperBody);


    try
        if CopperBody.Shape[0] <> nil then
            JsonWriteString('shape_0_ref', 'present', False)
        else
            JsonWriteString('shape_0_ref', '', False);
    except
        JsonWriteString('shape_0_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcb3DBodyToJson(Body: IPCB_ComponentBody; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', '3DBody', True);

    // Identification
    JsonWriteString('identifier', Body.Identifier, True);
    JsonWriteString('modelId', Body.ModelId, True);
    JsonWriteString('modelName', Body.Model.FileName, True);
    JsonWriteInteger('modelType', Body.ModelType, True);

    // Position
    JsonWriteCoord('standoffHeight', Body.StandoffHeight, True);
    JsonWriteCoord('overallHeight', Body.OverallHeight, True);

    // Rotation
    JsonWriteFloat('rotationX', Body.Model.RotateX, True);
    JsonWriteFloat('rotationY', Body.Model.RotateY, True);
    JsonWriteFloat('rotationZ', Body.Model.RotateZ, True);

    // Visual properties
    JsonWriteInteger('bodyColor3D', Body.BodyColor3D, True);
    JsonWriteFloat('bodyOpacity3D', Body.BodyOpacity3D, True);

    // Layer
    JsonWriteInteger('layer', Body.Layer, True);


    // Additional IPCB_ComponentBody properties
    JsonWriteInteger('area', Body.Area, True);
    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Body);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbBoardOutlineToJson(BoardOutline: IPCB_BoardOutline; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BoardOutline', True);

    // Outline properties
    JsonWriteInteger('pointCount', BoardOutline.PointCount, True);
    JsonWriteFloat('areaSize', BoardOutline.AreaSize, True);
    JsonWriteBoolean('poured', BoardOutline.Poured, True);
    JsonWriteString('name', BoardOutline.Name, True);

    // Pour settings
    JsonWriteInteger('polyHatchStyle', BoardOutline.PolyHatchStyle, True);
    JsonWriteInteger('pourOver', BoardOutline.PourOver, True);
    JsonWriteCoord('grid', BoardOutline.Grid, True);
    JsonWriteCoord('trackSize', BoardOutline.TrackSize, True);
    JsonWriteCoord('arcApproximation', BoardOutline.ArcApproximation, True);
    JsonWriteCoord('borderWidth', BoardOutline.BorderWidth, True);

    // Island and neck removal
    JsonWriteBoolean('removeDead', BoardOutline.RemoveDead, True);
    JsonWriteBoolean('removeIslandsByArea', BoardOutline.RemoveIslandsByArea, True);
    JsonWriteFloat('islandAreaThreshold', BoardOutline.IslandAreaThreshold, True);
    JsonWriteBoolean('removeNarrowNecks', BoardOutline.RemoveNarrowNecks, True);
    JsonWriteCoord('neckWidthThreshold', BoardOutline.NeckWidthThreshold, True);

    // Segments/Vertices
    JsonOpenArray('segments');
    for I := 0 to BoardOutline.PointCount - 1 do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', BoardOutline.Segments[I].vx, True);
        JsonWriteInteger('y', BoardOutline.Segments[I].vy, True);
        JsonWriteInteger('cx', BoardOutline.Segments[I].cx, True);
        JsonWriteInteger('cy', BoardOutline.Segments[I].cy, True);
        JsonWriteInteger('kind', BoardOutline.Segments[I].Kind, False);
        JsonCloseObject(I < BoardOutline.PointCount - 1);
    end;
    JsonCloseArray(True);


    // Additional IPCB_BoardOutline properties
    JsonWriteBoolean('arcPourMode', BoardOutline.ArcPourMode, True);
    JsonWriteBoolean('autoGenerateName', BoardOutline.AutoGenerateName, True);
    JsonWriteBoolean('avoidObstacles', BoardOutline.AvoidObstacles, True);
    JsonWriteBoolean('clipAcuteCorners', BoardOutline.ClipAcuteCorners, True);
    JsonWriteBoolean('drawDeadCopper', BoardOutline.DrawDeadCopper, True);
    JsonWriteBoolean('drawRemovedIslands', BoardOutline.DrawRemovedIslands, True);
    JsonWriteBoolean('drawRemovedNecks', BoardOutline.DrawRemovedNecks, True);
    JsonWriteBoolean('expandOutline', BoardOutline.ExpandOutline, True);
    JsonWriteBoolean('ignoreViolations', BoardOutline.IgnoreViolations, True);
    JsonWriteInteger('intersectedRegionsCount', BoardOutline.IntersectedRegionsCount, True);
    JsonWriteInteger('layer', BoardOutline.Layer, True);
    JsonWriteCoord('minTrack', BoardOutline.MinTrack, True);
    JsonWriteBoolean('mitreCorners', BoardOutline.MitreCorners, True);
    JsonWriteBoolean('obeyPolygonCutout', BoardOutline.ObeyPolygonCutout, True);
    JsonWriteBoolean('optimalVoidRotation', BoardOutline.OptimalVoidRotation, True);
    JsonWriteInteger('polygonType', BoardOutline.PolygonType, True);
    JsonWriteInteger('pourIndex', BoardOutline.PourIndex, True);
    JsonWriteBoolean('primitiveLock', BoardOutline.PrimitiveLock, True);
    JsonWriteBoolean('useOctagons', BoardOutline.UseOctagons, True);
    // Base primitive properties
    ExportPcbBasePrimitiveProperties(BoardOutline);


    try
        JsonWriteBoolean('layerUsed_top', BoardOutline.LayerUsed[eTopLayer], False);
    except
        JsonWriteString('layerUsed_top', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbBoardRegionToJson(BoardRegion: IPCB_BoardRegion; AddComma: Boolean);
var
    I, J: Integer;
    Contour: IPCB_Contour;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BoardRegion', True);

    // Region identification
    JsonWriteString('name', BoardRegion.Name, True);
    JsonWriteInteger('kind', BoardRegion.Kind, True);
    JsonWriteInteger('layer', BoardRegion.Layer, True);
    JsonWriteInteger('priority', BoardRegion.Priority, True);

    // Layer stack
    JsonWriteString('layerStackID', BoardRegion.LayerStackID, True);

    // Cavity properties
    JsonWriteCoord('cavityHeight', BoardRegion.CavityHeight, True);
    JsonWriteBoolean('locked3D', BoardRegion.Locked3D, True);

    // Bending lines
    JsonWriteInteger('bendingLinesCount', BoardRegion.BendingLinesCount, True);

    // Area
    JsonWriteInteger('area', BoardRegion.Area, True);

    // Main contour
    Contour := BoardRegion.MainContour;
    if Contour <> nil then
    begin
        JsonOpenArray('mainContour');
        for I := 0 to Contour.Count - 1 do
        begin
            JsonOpenObject('');
            JsonWriteInteger('x', Contour.X[I], True);
            JsonWriteInteger('y', Contour.Y[I], False);
            JsonCloseObject(I < Contour.Count - 1);
        end;
        JsonCloseArray(True);
    end;

    // Holes
    JsonWriteInteger('holeCount', BoardRegion.HoleCount, True);
    if BoardRegion.HoleCount > 0 then
    begin
        JsonOpenArray('holes');
        for I := 0 to BoardRegion.HoleCount - 1 do
        begin
            Contour := BoardRegion.Holes[I];
            if Contour <> nil then
            begin
                JsonOpenArray('');
                for J := 0 to Contour.Count - 1 do
                begin
                    JsonOpenObject('');
                    JsonWriteInteger('x', Contour.X[J], True);
                    JsonWriteInteger('y', Contour.Y[J], False);
                    JsonCloseObject(J < Contour.Count - 1);
                end;
                JsonCloseArray(I < BoardRegion.HoleCount - 1);
            end;
        end;
        JsonCloseArray(True);
    end;

    // Coverlay
    JsonWriteBoolean('customCoverlays', BoardRegion.CustomCoverlays, True);

    // Base primitive properties
    ExportPcbBasePrimitiveProperties(BoardRegion);


    // Object reference and indexed properties
    try
        if BoardRegion.BendingLines[0] <> nil then
            JsonWriteString('bendingLines_0_ref', 'present', True)
        else
            JsonWriteString('bendingLines_0_ref', '', True);
    except
        JsonWriteString('bendingLines_0_ref', 'ERROR', True);
    end;
    try
        if BoardRegion.GeometricPolygon <> nil then
            JsonWriteString('geometricPolygon_ref', 'present', True)
        else
            JsonWriteString('geometricPolygon_ref', '', True);
    except
        JsonWriteString('geometricPolygon_ref', 'ERROR', True);
    end;
    try
        if BoardRegion.LayerStack <> nil then
            JsonWriteString('layerStack_ref', 'present', False)
        else
            JsonWriteString('layerStack_ref', '', False);
    except
        JsonWriteString('layerStack_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbClearanceConstraintToJson(Rule: IPCB_ClearanceConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ClearanceConstraint', True);

    // Rule identification

    // Scope expressions

    // Clearance value
    JsonWriteCoord('gap', Rule.Gap, True);

    // Mode and flags
    JsonWriteInteger('mode', Rule.Mode, True);

    // Rule state


    // Additional IPCB_ClearanceConstraint properties
    try
        JsonWriteString('netScopeMatches', 'OBJECT_REF', True);
    except
        JsonWriteString('netScopeMatches', 'ERROR', True);
    end;
    try
        JsonWriteString('scope1Includes', 'OBJECT_REF', True);
    except
        JsonWriteString('scope1Includes', 'ERROR', True);
    end;
    try
        JsonWriteString('scope2Includes', 'OBJECT_REF', True);
    except
        JsonWriteString('scope2Includes', 'ERROR', True);
    end;
    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbWidthConstraintToJson(Rule: IPCB_WidthConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'WidthConstraint', True);

    // Rule identification

    // Scope

    // Width constraints
    JsonWriteCoord('minWidth', Rule.MinWidth, True);
    JsonWriteCoord('maxWidth', Rule.MaxWidth, True);
    JsonWriteCoord('favWidth', Rule.FavWidth, True);

    // Rule state

    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbRoutingViaStyleToJson(Rule: IPCB_RoutingViaStyle; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'RoutingViaStyle', True);

    // Rule identification

    // Scope

    // Via dimensions
    JsonWriteCoord('minViaWidth', Rule.MinViaWidth, True);
    JsonWriteCoord('maxViaWidth', Rule.MaxViaWidth, True);
    JsonWriteCoord('prefViaWidth', Rule.PrefViaWidth, True);
    JsonWriteCoord('minHoleWidth', Rule.MinHoleWidth, True);
    JsonWriteCoord('maxHoleWidth', Rule.MaxHoleWidth, True);
    JsonWriteCoord('prefHoleWidth', Rule.PrefHoleWidth, True);

    // Rule state

    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbShortCircuitConstraintToJson(Rule: IPCB_ShortCircuitConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ShortCircuitConstraint', True);

    // Rule identification

    // Scope

    // Allow short circuit
    JsonWriteBoolean('allowShortCircuit', Rule.AllowShortCircuit, True);

    // Rule state

    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbUnroutedNetConstraintToJson(Rule: IPCB_UnRoutedNetConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'UnroutedNetConstraint', True);

    // Rule identification

    // Scope

    // Rule state

    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbSolderMaskExpansionRuleToJson(Rule: IPCB_SolderMaskExpansionRule; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SolderMaskExpansionRule', True);

    // Rule identification

    // Scope

    // Expansion value
    JsonWriteCoord('expansion', Rule.Expansion, True);

    // Rule state

    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbPasteMaskExpansionRuleToJson(Rule: IPCB_PasteMaskExpansionRule; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'PasteMaskExpansionRule', True);

    // Rule identification

    // Scope

    // Expansion value
    JsonWriteCoord('expansion', Rule.Expansion, True);

    // Rule state

    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbPolygonConnectStyleToJson(Rule: IPCB_PolygonConnectStyle; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'PolygonConnectStyle', True);

    // Rule identification

    // Scope

    // Connect style
    JsonWriteInteger('connectStyle', Rule.ConnectStyle, True);
    JsonWriteCoord('conductorWidth', Rule.ConductorWidth, True);
    JsonWriteInteger('conductorEntries', Rule.ConductorEntries, True);
    JsonWriteCoord('airGapWidth', Rule.AirGapWidth, True);

    // Rule state

    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbDrillLayerPairToJson(DrillPair: IPCB_DrillLayerPair; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DrillLayerPair', True);

    // Layer pair identification
    JsonWriteString('name', DrillPair.Name, True);
    JsonWriteInteger('lowLayer', DrillPair.LowLayer, True);
    JsonWriteInteger('highLayer', DrillPair.HighLayer, True);

    // Properties
    JsonWriteInteger('drillType', DrillPair.DrillType, True);
    JsonWriteBoolean('isLaserDrill', DrillPair.IsLaserDrill, True);
    JsonWriteBoolean('isPunchDrill', DrillPair.IsPunchDrill, True);


    // Additional IPCB_DrillLayerPair properties
    JsonWriteString('description', DrillPair.Description, True);
    JsonWriteInteger('drillLayerPairType', DrillPair.DrillLayerPairType, True);
    JsonWriteBoolean('inverted', DrillPair.Inverted, True);
    JsonWriteBoolean('isBackdrill', DrillPair.IsBackdrill, True);
    JsonWriteBoolean('plotDrillDrawing', DrillPair.PlotDrillDrawing, True);
    JsonWriteBoolean('plotDrillGuide', DrillPair.PlotDrillGuide, True);

    // Object reference properties
    try
        if DrillPair.Board <> nil then
            JsonWriteString('board_ref', 'present', True)
        else
            JsonWriteString('board_ref', '', True);
    except
        JsonWriteString('board_ref', 'ERROR', True);
    end;
    try
        if DrillPair.CounterHoleParams <> nil then
            JsonWriteString('counterHoleParams_ref', 'present', True)
        else
            JsonWriteString('counterHoleParams_ref', '', True);
    except
        JsonWriteString('counterHoleParams_ref', 'ERROR', True);
    end;
    try
        if DrillPair.StartLayer <> nil then
            JsonWriteString('startLayer_ref', 'present', True)
        else
            JsonWriteString('startLayer_ref', '', True);
    except
        JsonWriteString('startLayer_ref', 'ERROR', True);
    end;
    try
        if DrillPair.StopLayer <> nil then
            JsonWriteString('stopLayer_ref', 'present', False)
        else
            JsonWriteString('stopLayer_ref', '', False);
    except
        JsonWriteString('stopLayer_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbElectricalLayerToJson(Layer: IPCB_ElectricalLayer; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ElectricalLayer', True);

    // Layer properties
    JsonWriteString('name', Layer.Name, True);
    JsonWriteCoord('copperThickness', Layer.CopperThickness, True);
    JsonWriteInteger('v6LayerID', Layer.V6_LayerID, True);
    JsonWriteBoolean('usedByPrims', Layer.UsedByPrims, True);
    JsonWriteBoolean('isInLayerStack', Layer.IsInLayerStack, True);

    // IPCB_ElectricalLayer2 extension property
    JsonWriteInteger('copperOrientation', Ord(Layer.CopperOrientation), True);


    try
        if Layer.V7_LayerID <> nil then
            JsonWriteString('v7_LayerID_ref', 'present', True)
        else
            JsonWriteString('v7_LayerID_ref', '', True);
    except
        JsonWriteString('v7_LayerID_ref', 'ERROR', True);
    end;
    try
        if Layer.Dielectric <> nil then
            JsonWriteString('dielectric_ref', 'present', False)
        else
            JsonWriteString('dielectric_ref', '', False);
    except
        JsonWriteString('dielectric_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbDielectricLayerToJson(Layer: IPCB_DielectricLayer; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DielectricLayer', True);

    // Layer properties
    JsonWriteString('name', Layer.Name, True);
    JsonWriteCoord('dielectricHeight', Layer.DielectricHeight, True);
    JsonWriteFloat('dielectricConstant', Layer.DielectricConstant, True);
    JsonWriteFloat('dielectricLossTangent', Layer.DielectricLossTangent, True);
    JsonWriteString('dielectricMaterial', Layer.DielectricMaterial, True);
    JsonWriteInteger('dielectricType', Layer.DielectricType, True);
    JsonWriteInteger('v6LayerID', Layer.V6_LayerID, True);
    JsonWriteBoolean('usedByPrims', Layer.UsedByPrims, True);
    JsonWriteBoolean('isInLayerStack', Layer.IsInLayerStack, True);
    JsonWriteBoolean('isStiffener', Layer.IsStiffener, True);


    try
        if Layer.LayerStack <> nil then
            JsonWriteString('layerStack_ref', 'present', False)
        else
            JsonWriteString('layerStack_ref', '', False);
    except
        JsonWriteString('layerStack_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbDesignVariantToJson(Variant: IPCB_DesignVariant; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DesignVariant', True);

    JsonWriteString('name', Variant.Name, True);
    JsonWriteString('variantID', Variant.VariantID, True);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbECOOptionsToJson(ECOOptions: IPCB_ECOOptions; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ECOOptions', True);

    JsonWriteString('ecoFileName', ECOOptions.ECOFileName, True);
    JsonWriteBoolean('ecoIsActive', ECOOptions.ECOIsActive, True);
    JsonWriteInteger('optionsObjectID', ECOOptions.OptionsObjectID, True);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbAuthorInfoToJson(AuthorInfo: IPCB_AuthorInfo; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'AuthorInfo', True);

    JsonWriteString('author', AuthorInfo.Author, True);
    JsonWriteString('company', AuthorInfo.Company, True);
    JsonWriteString('version', AuthorInfo.Version, True);
    JsonWriteString('email', AuthorInfo.Email, True);
    JsonWriteString('phone', AuthorInfo.Phone, True);
    JsonWriteString('address', AuthorInfo.Address, True);
    JsonWriteString('address2', AuthorInfo.Address2, True);
    JsonWriteString('city', AuthorInfo.City, True);
    JsonWriteString('stateCounty', AuthorInfo.StateCounty, True);
    JsonWriteString('country', AuthorInfo.Country, True);
    JsonWriteString('zipPostcode', AuthorInfo.ZipPostcode, True);
    JsonWriteString('imageFile', AuthorInfo.ImageFile, True);
    JsonWriteString('title', AuthorInfo.Title, True);
    JsonWriteString('approver1', AuthorInfo.Approver1, True);
    JsonWriteString('approver2', AuthorInfo.Approver2, True);
    JsonWriteString('approver3', AuthorInfo.Approver3, True);
    JsonWriteString('approver4', AuthorInfo.Approver4, True);


    // Additional IPCB_AuthorInfo properties
    JsonWriteString('id', AuthorInfo.Id, True);
    JsonWriteString('source', AuthorInfo.Source, True);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbColorIDToJson(ColorID: IPCB_ColorID; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ColorID', True);

    JsonWriteInteger('color', ColorID.Color, True);
    JsonWriteInteger('id', ColorID.ID, True);
    JsonWriteString('name', ColorID.Name, True);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbCartesianGridToJson(Grid: IPCB_CartesianGrid; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CartesianGrid', True);

    JsonWriteString('name', Grid.Name, True);
    JsonWriteCoord('xOrigin', Grid.XOrigin, True);
    JsonWriteCoord('yOrigin', Grid.YOrigin, True);
    JsonWriteCoord('xStep', Grid.XStep, True);
    JsonWriteCoord('yStep', Grid.YStep, True);
    JsonWriteFloat('rotation', Grid.Rotation, True);
    JsonWriteInteger('color', Grid.Color, True);
    JsonWriteBoolean('enabled', Grid.Enabled, True);
    JsonWriteBoolean('visible', Grid.Visible, True);


    // Additional IPCB_CartesianGrid properties
    JsonWriteInteger('colorLarge', Grid.ColorLarge, True);
    JsonWriteBoolean('componentGrid', Grid.ComponentGrid, True);
    JsonWriteInteger('displayUnit', Grid.DisplayUnit, True);
    JsonWriteInteger('drawMode', Grid.DrawMode, True);
    JsonWriteInteger('drawModeLarge', Grid.DrawModeLarge, True);
    JsonWriteInteger('drawMultiplier', Grid.DrawMultiplier, True);
    JsonWriteInteger('drawMultiplierLarge', Grid.DrawMultiplierLarge, True);
    JsonWriteInteger('gridStepX', Grid.GridStepX, True);
    JsonWriteInteger('gridStepY', Grid.GridStepY, True);
    JsonWriteBoolean('isDefault', Grid.IsDefault, True);
    JsonWriteBoolean('isMCADSource', Grid.IsMCADSource, True);
    try
        JsonWriteString('i_ObjectAddress', 'OBJECT_REF', True);
    except
        JsonWriteString('i_ObjectAddress', 'ERROR', True);
    end;
    try
        JsonWriteString('origin', 'OBJECT_REF', True);
    except
        JsonWriteString('origin', 'ERROR', True);
    end;
    JsonWriteInteger('priority', Grid.Priority, True);
    JsonWriteCoord('quadrantSizeX', Grid.QuadrantSizeX, True);
    JsonWriteCoord('quadrantSizeY', Grid.QuadrantSizeY, True);
    JsonWriteBoolean('showOrigin', Grid.ShowOrigin, True);

    try
        JsonWriteBoolean('validQuadrant_1', Grid.ValidQuadrant[1], False);
    except
        JsonWriteString('validQuadrant_1', 'ERROR', False);
    end;

    JsonCloseObject(AddComma);
end;

procedure ExportPcbDifferentialPair2ToJson(DiffPair: IPCB_DifferentialPair2; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DifferentialPair2', True);

    JsonWriteString('name', DiffPair.Name, True);
    JsonWriteString('positiveNetName', DiffPair.PositiveNetName, True);
    JsonWriteString('negativeNetName', DiffPair.NegativeNetName, True);
    JsonWriteBoolean('enabled', DiffPair.Enabled, True);
    JsonWriteBoolean('definedByLogicalDocument', DiffPair.DefinedByLogicalDocument, True);


    // Additional IPCB_DifferentialPair2 properties
    JsonWriteBoolean('allowGlobalEdit', DiffPair.AllowGlobalEdit, True);
    JsonWriteString('descriptor', DiffPair.Descriptor, True);
    JsonWriteString('detail', DiffPair.Detail, True);
    JsonWriteBoolean('drawAsPreview', DiffPair.DrawAsPreview, True);
    JsonWriteBoolean('dRCError', DiffPair.DRCError, True);
    JsonWriteBoolean('enableDraw', DiffPair.EnableDraw, True);
    JsonWriteBoolean('enabled_Direct', DiffPair.Enabled_Direct, True);
    JsonWriteBoolean('enabled_vComponent', DiffPair.Enabled_vComponent, True);
    JsonWriteBoolean('enabled_vCoordinate', DiffPair.Enabled_vCoordinate, True);
    JsonWriteBoolean('enabled_vDimension', DiffPair.Enabled_vDimension, True);
    JsonWriteBoolean('enabled_vNet', DiffPair.Enabled_vNet, True);
    JsonWriteBoolean('enabled_vPolygon', DiffPair.Enabled_vPolygon, True);
    JsonWriteBoolean('gatherControl', DiffPair.GatherControl, True);
    JsonWriteString('handle', DiffPair.Handle, True);
    JsonWriteString('identifier', DiffPair.Identifier, True);
    JsonWriteBoolean('inBoard', DiffPair.InBoard, True);
    JsonWriteBoolean('inComponent', DiffPair.InComponent, True);
    JsonWriteBoolean('inCoordinate', DiffPair.InCoordinate, True);
    JsonWriteInteger('index', DiffPair.Index, True);
    JsonWriteBoolean('inDimension', DiffPair.InDimension, True);
    JsonWriteBoolean('inNet', DiffPair.InNet, True);
    JsonWriteBoolean('inPolygon', DiffPair.InPolygon, True);
    JsonWriteBoolean('isAssyTestpoint_Bottom', DiffPair.IsAssyTestpoint_Bottom, True);
    JsonWriteBoolean('isAssyTestpoint_Top', DiffPair.IsAssyTestpoint_Top, True);
    JsonWriteBoolean('isElectricalPrim', DiffPair.IsElectricalPrim, True);
    JsonWriteBoolean('isKeepout', DiffPair.IsKeepout, True);
    JsonWriteBoolean('isPreRoute', DiffPair.IsPreRoute, True);
    JsonWriteBoolean('isTenting', DiffPair.IsTenting, True);
    JsonWriteBoolean('isTenting_Bottom', DiffPair.IsTenting_Bottom, True);
    JsonWriteBoolean('isTenting_Top', DiffPair.IsTenting_Top, True);
    JsonWriteBoolean('isTestpoint_Bottom', DiffPair.IsTestpoint_Bottom, True);
    JsonWriteBoolean('isTestpoint_Top', DiffPair.IsTestpoint_Top, True);
    JsonWriteInteger('layer', DiffPair.Layer, True);
    JsonWriteInteger('layer_V6', DiffPair.Layer_V6, True);
    JsonWriteBoolean('miscFlag1', DiffPair.MiscFlag1, True);
    JsonWriteBoolean('miscFlag2', DiffPair.MiscFlag2, True);
    JsonWriteBoolean('miscFlag3', DiffPair.MiscFlag3, True);
    JsonWriteBoolean('moveable', DiffPair.Moveable, True);
    JsonWriteInteger('objectId', DiffPair.ObjectId, True);
    JsonWriteString('objectIDString', DiffPair.ObjectIDString, True);
    JsonWriteBoolean('padCacheRobotFlag', DiffPair.PadCacheRobotFlag, True);
    JsonWriteCoord('pairAverageLength', DiffPair.PairAverageLength, True);
    JsonWriteCoord('pasteMaskExpansion', DiffPair.PasteMaskExpansion, True);
    JsonWriteBoolean('polygonOutline', DiffPair.PolygonOutline, True);
    JsonWriteCoord('powerPlaneClearance', DiffPair.PowerPlaneClearance, True);
    JsonWriteInteger('powerPlaneConnectStyle', DiffPair.PowerPlaneConnectStyle, True);
    JsonWriteCoord('powerPlaneReliefExpansion', DiffPair.PowerPlaneReliefExpansion, True);
    JsonWriteCoord('reliefAirGap', DiffPair.ReliefAirGap, True);
    JsonWriteCoord('reliefConductorWidth', DiffPair.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', DiffPair.ReliefEntries, True);
    JsonWriteBoolean('selected', DiffPair.Selected, True);
    JsonWriteCoord('solderMaskExpansion', DiffPair.SolderMaskExpansion, True);
    JsonWriteBoolean('tearDrop', DiffPair.TearDrop, True);
    JsonWriteInteger('unionIndex', DiffPair.UnionIndex, True);
    JsonWriteString('uniqueId', DiffPair.UniqueId, True);
    JsonWriteBoolean('used', DiffPair.Used, True);
    JsonWriteBoolean('userRouted', DiffPair.UserRouted, True);
    JsonWriteInteger('viewableObjectID', DiffPair.ViewableObjectID, True);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbComponentClassToJson(CompClass: IPCB_ObjectClass; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ComponentClass', True);

    JsonWriteString('name', CompClass.Name, True);
    JsonWriteString('superClass', CompClass.SuperClass, True);
    JsonWriteString('subClass', CompClass.SubClass, True);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbClearanceGapByLayerConstraintToJson(Rule: IPCB_ClearanceGapByLayerConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ClearanceGapByLayerConstraint', True);

    // Rule identification

    // Scope expressions

    // Rule state


    // Additional IPCB_ClearanceGapByLayerConstraint properties
    JsonWriteCoord('gap', Rule.Gap, True);
    JsonWriteInteger('mode', Rule.Mode, True);
    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);


    try
        JsonWriteString('ruleOnLayer', 'RuleOnLayer indexed by IDispatch key', False); // Rule.RuleOnLayer
    except
        JsonWriteString('ruleOnLayer', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbComponentClearanceConstraintToJson(Rule: IPCB_ComponentClearanceConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ComponentClearanceConstraint', True);

    // Rule identification

    // Scope expressions

    // Clearance values
    JsonWriteCoord('gap', Rule.Gap, True);
    JsonWriteCoord('verticalGap', Rule.VerticalGap, True);

    // Mode
    JsonWriteInteger('checkMode', Rule.CheckMode, True);

    // Rule state


    // Additional IPCB_ComponentClearanceConstraint properties
    JsonWriteBoolean('checkComponentBoundary', Rule.CheckComponentBoundary, True);
    JsonWriteInteger('collisionCheckMode', Rule.CollisionCheckMode, True);
    JsonWriteBoolean('doNotCheckWithout3DBody', Rule.DoNotCheckWithout3DBody, True);
    JsonWriteCoord('horizontalGap', Rule.HorizontalGap, True);
    JsonWriteBoolean('showDistances', Rule.ShowDistances, True);
    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbBoardOutlineClearanceConstraintToJson(Rule: IPCB_BoardOutlineClearanceConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BoardOutlineClearanceConstraint', True);

    // Rule identification

    // Scope expressions

    // Clearance value
    JsonWriteCoord('gap', Rule.Gap, True);

    // Rule state

    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbComponentRotationsRuleToJson(Rule: IPCB_ComponentRotationsRule; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ComponentRotationsRule', True);

    // Rule identification

    // Scope

    // Allowed rotations
    JsonWriteBoolean('allowRotation0', Rule.AllowRotation0, True);
    JsonWriteBoolean('allowRotation90', Rule.AllowRotation90, True);
    JsonWriteBoolean('allowRotation180', Rule.AllowRotation180, True);
    JsonWriteBoolean('allowRotation270', Rule.AllowRotation270, True);

    // Rule state


    // Additional IPCB_ComponentRotationsRule properties
    JsonWriteInteger('allowedRotations', Rule.AllowedRotations, True);
    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbConfinementConstraintToJson(Rule: IPCB_ConfinementConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ConfinementConstraint', True);

    // Rule identification

    // Scope expressions

    // Confinement style
    JsonWriteInteger('confinementStyle', Rule.ConfinementStyle, True);

    // Rule state


    // Additional IPCB_ConfinementConstraint properties
    JsonWriteInteger('boundingRect', Rule.BoundingRect, True);
    JsonWriteInteger('constraintLayer', Rule.ConstraintLayer, True);
    JsonWriteInteger('kind', Rule.Kind, True);
    JsonWriteBoolean('lockComponents', Rule.LockComponents, True);
    JsonWriteInteger('pointCount', Rule.PointCount, True);
    JsonWriteCoord('x', Rule.x, True);
    JsonWriteCoord('y', Rule.y, True);
    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);


    try
        JsonWriteString('segments_0', IntToStr(Rule.Segments[0].Kind), False);
    except
        JsonWriteString('segments_0', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbCreepageRuleToJson(Rule: IPCB_CreepageRule; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CreepageRule', True);

    // Rule identification

    // Scope expressions

    // Creepage value
    JsonWriteCoord('creepage', Rule.Creepage, True);

    // Rule state


    // Additional IPCB_CreepageRule properties
    JsonWriteCoord('checkDistance', Rule.CheckDistance, True);
    try
        JsonWriteString('netScopeMatches', 'OBJECT_REF', True);
    except
        JsonWriteString('netScopeMatches', 'ERROR', True);
    end;
    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbDifferentialPairsRoutingRuleToJson(Rule: IPCB_DifferentialPairsRoutingRule; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DifferentialPairsRoutingRule', True);

    // Rule identification

    // Scope

    // Routing parameters
    JsonWriteCoord('minGap', Rule.MinGap, True);
    JsonWriteCoord('maxGap', Rule.MaxGap, True);
    JsonWriteCoord('preferredGap', Rule.PreferredGap, True);
    JsonWriteCoord('minWidth', Rule.MinWidth, True);
    JsonWriteCoord('maxWidth', Rule.MaxWidth, True);
    JsonWriteCoord('preferredWidth', Rule.PreferredWidth, True);

    // Rule state


    // Additional IPCB_DifferentialPairsRoutingRule properties
    JsonWriteInteger('favoredImpedance', Rule.FavoredImpedance, True);
    JsonWriteBoolean('impedanceDriven', Rule.ImpedanceDriven, True);
    JsonWriteString('impedanceProfileId', Rule.ImpedanceProfileId, True);
    JsonWriteInteger('maxImpedance', Rule.MaxImpedance, True);
    JsonWriteCoord('maxUncoupledLength', Rule.MaxUncoupledLength, True);
    JsonWriteInteger('minImpedance', Rule.MinImpedance, True);
    try
        JsonWriteString('netScopeMatches', 'OBJECT_REF', True);
    except
        JsonWriteString('netScopeMatches', 'ERROR', True);
    end;
    // IPCB_DifferentialPairsRoutingRule3 extension properties
    JsonWriteString('filterLayerStackID', Rule.FilterLayerStackID, True);
    JsonWriteCoord('maxLimit', Rule.MaxLimit, True);
    JsonWriteCoord('minLimit', Rule.MinLimit, True);
    JsonWriteCoord('mostFrequentGap', Rule.MostFrequentGap, True);
    JsonWriteCoord('mostFrequentWidth', Rule.MostFrequentWidth, True);

    // Indexed properties (layer-specific)
    try
        JsonWriteCoord('preferedGap_top', Rule.PreferedGap[eTopLayer], True);
    except
        JsonWriteString('preferedGap_top', 'ERROR', True);
    end;
    try
        JsonWriteCoord('preferedWidth_top', Rule.PreferedWidth[eTopLayer], True);
    except
        JsonWriteString('preferedWidth_top', 'ERROR', True);
    end;

    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbFanoutControlRuleToJson(Rule: IPCB_FanoutControlRule; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'FanoutControlRule', True);

    // Rule identification

    // Scope

    // Fanout parameters
    JsonWriteInteger('fanoutStyle', Rule.FanoutStyle, True);
    JsonWriteInteger('fanoutDirection', Rule.FanoutDirection, True);

    // Rule state


    // Additional IPCB_FanoutControlRule properties
    JsonWriteInteger('bGAFanoutDirection', Rule.BGAFanoutDirection, True);
    JsonWriteInteger('bGAFanoutViaMode', Rule.BGAFanoutViaMode, True);
    JsonWriteCoord('viaGrid', Rule.ViaGrid, True);
    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbCheckNetAntennaeRuleToJson(Rule: IPCB_CheckNetAntennaeRule; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CheckNetAntennaeRule', True);

    // Rule identification

    // Scope

    // Rule state


    // Additional IPCB_CheckNetAntennaeRule properties
    JsonWriteCoord('netAntennaeTolerance', Rule.NetAntennaeTolerance, True);
    try
        JsonWriteString('netScopeMatches', 'OBJECT_REF', True);
    except
        JsonWriteString('netScopeMatches', 'ERROR', True);
    end;
    try
        JsonWriteString('scope1Includes', 'OBJECT_REF', True);
    except
        JsonWriteString('scope1Includes', 'ERROR', True);
    end;
    try
        JsonWriteString('scope2Includes', 'OBJECT_REF', True);
    except
        JsonWriteString('scope2Includes', 'ERROR', True);
    end;
    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbBackDrillingRuleToJson(Rule: IPCB_BackDrillingRule; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BackDrillingRule', True);

    // Rule identification

    // Scope

    // Back drilling parameters
    JsonWriteCoord('backDrillingDepth', Rule.BackDrillingDepth, True);

    // Rule state

    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbDaisyChainStubLengthConstraintToJson(Rule: IPCB_DaisyChainStubLengthConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DaisyChainStubLengthConstraint', True);

    // Rule identification

    // Scope

    // Stub length
    JsonWriteCoord('maxStubLength', Rule.MaxStubLength, True);

    // Rule state


    // Additional IPCB_DaisyChainStubLengthConstraint properties
    JsonWriteCoord('limit', Rule.Limit, True);
    try
        JsonWriteString('netScopeMatches', 'OBJECT_REF', True);
    except
        JsonWriteString('netScopeMatches', 'ERROR', True);
    end;
    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbMatchedLengthConstraintToJson(Rule: IPCB_MatchedLengthConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'MatchedLengthConstraint', True);

    // Rule identification

    // Scope

    // Matching tolerances
    JsonWriteCoord('tolerance', Rule.Tolerance, True);

    // Rule state

    // Base rule properties (identification, scope, flags)
    ExportPcbBaseRuleProperties(Rule);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbCustomShapeInfoToJson(ShapeInfo: IPCB_CustomShapeInfo; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CustomShapeInfo', True);

    // Shape info is primarily accessed through the pad, exporting basic info
    JsonWriteInteger('shapeKind', ShapeInfo.ShapeKind, True);


    // Additional IPCB_CustomShapeInfo properties
    try
        JsonWriteString('customShapeParameters', 'OBJECT_REF', True);
    except
        JsonWriteString('customShapeParameters', 'ERROR', True);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbCopperPolygonToJson(CopperPoly: IPCB_CopperPolygon; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CopperPolygon', True);

    // Polygon properties
    JsonWriteInteger('vertexCount', CopperPoly.VertexCount, True);
    JsonWriteBoolean('isClosed', CopperPoly.IsClosed, True);

    // Vertices
    JsonOpenArray('vertices');
    for I := 0 to CopperPoly.VertexCount - 1 do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', CopperPoly.Vertex[I].X, True);
        JsonWriteInteger('y', CopperPoly.Vertex[I].Y, False);
        JsonCloseObject(I < CopperPoly.VertexCount - 1);
    end;
    JsonCloseArray(False);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbCopperPolyArcToJson(CopperArc: IPCB_CopperPolyArc; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CopperPolyArc', True);

    // Arc properties
    JsonWriteInteger('cx', CopperArc.CX, True);
    JsonWriteInteger('cy', CopperArc.CY, True);
    JsonWriteInteger('radius', CopperArc.Radius, True);
    JsonWriteFloat('startAngle', CopperArc.StartAngle, True);
    JsonWriteFloat('endAngle', CopperArc.EndAngle, True);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbCounterHoleParamsToJson(Params: IPCB_CounterHoleParams; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CounterHoleParams', True);

    // Counter hole parameters
    JsonWriteCoord('counterHoleDiameter', Params.CounterHoleDiameter, True);
    JsonWriteCoord('counterHoleDepth', Params.CounterHoleDepth, True);
    JsonWriteFloat('counterHoleAngle', Params.CounterHoleAngle, True);
    JsonWriteInteger('counterHoleType', Params.CounterHoleType, True);


    // Additional IPCB_CounterHoleParams properties
    JsonWriteInteger('angle', Params.Angle, True);
    JsonWriteCoord('depth', Params.Depth, True);
    JsonWriteCoord('diameter', Params.Diameter, True);
    JsonWriteInteger('direction', Params.Direction, True);
    JsonWriteInteger('material', Params.Material, True);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbBackDrillingToJson(BackDrill: IPCB_BackDrilling; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BackDrilling', True);

    // Back drilling properties
    JsonWriteCoord('diameter', BackDrill.Diameter, True);
    JsonWriteCoord('depth', BackDrill.Depth, True);
    JsonWriteBoolean('enabled', BackDrill.Enabled, True);


    // Additional IPCB_BackDrilling properties
    JsonWriteBoolean('backDrillSource', BackDrill.BackDrillSource, True);
    JsonWriteInteger('bottomBackDrillStopLayer', BackDrill.BottomBackDrillStopLayer, True);
    JsonWriteCoord('bottomUnusedStub', BackDrill.BottomUnusedStub, True);
    JsonWriteCoord('holeOversize', BackDrill.HoleOversize, True);
    JsonWriteBoolean('isBackdrill', BackDrill.IsBackdrill, True);
    JsonWriteInteger('topBackDrillStopLayer', BackDrill.TopBackDrillStopLayer, True);
    JsonWriteCoord('topUnusedStub', BackDrill.TopUnusedStub, True);

    try
        if BackDrill.SourceObject <> nil then
            JsonWriteString('sourceObject_ref', 'present', False)
        else
            JsonWriteString('sourceObject_ref', '', False);
    except
        JsonWriteString('sourceObject_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbDesignRuleCheckerOptionsToJson(Options: IPCB_DesignRuleCheckerOptions; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DesignRuleCheckerOptions', True);

    // DRC Options - these are typically boolean flags for various checks
    JsonWriteBoolean('checkClearance', Options.CheckClearance, True);
    JsonWriteBoolean('checkShortCircuit', Options.CheckShortCircuit, True);
    JsonWriteBoolean('checkUnconnectedPin', Options.CheckUnconnectedPin, True);
    JsonWriteBoolean('checkUnroutedNet', Options.CheckUnroutedNet, True);


    // Additional IPCB_DesignRuleCheckerOptions properties
    JsonWriteBoolean('checkExternalNetList', Options.CheckExternalNetList, True);
    JsonWriteBoolean('doMakeDRCErrorList', Options.DoMakeDRCErrorList, True);
    JsonWriteBoolean('doMakeDRCFile', Options.DoMakeDRCFile, True);
    JsonWriteBoolean('doSubNetDetails', Options.DoSubNetDetails, True);
    JsonWriteString('externalNetListFileName', Options.ExternalNetListFileName, True);
    JsonWriteBoolean('includePCBHealth', Options.IncludePCBHealth, True);
    JsonWriteBoolean('internalPlaneWarnings', Options.InternalPlaneWarnings, True);
    JsonWriteInteger('maxViolationCount', Options.MaxViolationCount, True);
    JsonWriteInteger('onLineRuleSetToCheck', Options.OnLineRuleSetToCheck, True);
    JsonWriteInteger('optionsObjectID', Options.OptionsObjectID, True);
    JsonWriteString('reportFilename', Options.ReportFilename, True);
    JsonWriteInteger('ruleSetToCheck', Options.RuleSetToCheck, True);
    JsonWriteBoolean('verifyShortingCopper', Options.VerifyShortingCopper, True);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbElectricalParametersToJson(Params: IPCB_ElectricalParameters; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ElectricalParameters', True);

    // Electrical parameters
    JsonWriteFloat('impedance', Params.Impedance, True);
    JsonWriteFloat('maxCurrent', Params.MaxCurrent, True);


    // Additional IPCB_ElectricalParameters properties
    JsonWriteFloat('current', Params.Current, True);
    JsonWriteFloat('resistance', Params.Resistance, True);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbComponentBodyToJson(Body: IPCB_ComponentBody; AddComma: Boolean);
var
    I, J: Integer;
    Contour: IPCB_Contour;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ComponentBody', True);
    JsonWriteInteger('kind', Ord(Body.Kind), True);
    JsonWriteInteger('layer', Body.Layer, True);

    // 3D properties
    JsonWriteInteger('bodyColor3D', Body.BodyColor3D, True);
    JsonWriteFloat('bodyOpacity3D', Body.BodyOpacity3D, True);
    JsonWriteInteger('bodyProjection', Body.BodyProjection, True);
    JsonWriteCoord('cavityHeight', Body.CavityHeight, True);
    JsonWriteInteger('modelType', Body.GetState_ModelType, True);

    // Geometry
    JsonWriteInteger('holeCount', Body.HoleCount, True);
    JsonWriteInteger('axisCount', Body.AxisCount, True);

    // Main contour vertices
    Contour := Body.MainContour;
    if Contour <> nil then
    begin
        JsonOpenArray('mainContour');
        for I := 0 to Contour.Count - 1 do
        begin
            JsonOpenObject('');
            JsonWriteInteger('x', Contour.x[I], True);
            JsonWriteInteger('y', Contour.y[I], False);
            JsonCloseObject(I < Contour.Count - 1);
        end;
        JsonCloseArray(True);
    end;

    // Hole contours
    if Body.HoleCount > 0 then
    begin
        JsonOpenArray('holes');
        for I := 0 to Body.HoleCount - 1 do
        begin
            Contour := Body.Holes[I];
            if Contour <> nil then
            begin
                JsonOpenArray('');
                for J := 0 to Contour.Count - 1 do
                begin
                    JsonOpenObject('');
                    JsonWriteInteger('x', Contour.x[J], True);
                    JsonWriteInteger('y', Contour.y[J], False);
                    JsonCloseObject(J < Contour.Count - 1);
                end;
                JsonCloseArray(I < Body.HoleCount - 1);
            end;
        end;
        JsonCloseArray(True);
    end;

    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Body);


    try
        if Body.Axis[0] <> nil then
            JsonWriteString('axis_0_ref', 'present', True)
        else
            JsonWriteString('axis_0_ref', '', True);
    except
        JsonWriteString('axis_0_ref', 'ERROR', True);
    end;
    try
        if Body.GeometricPolygon <> nil then
            JsonWriteString('geometricPolygon_ref', 'present', False)
        else
            JsonWriteString('geometricPolygon_ref', '', False);
    except
        JsonWriteString('geometricPolygon_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbLibComponentToJson(Comp: IPCB_LibComponent; AddComma: Boolean);
var
    GroupIterator: IPCB_GroupIterator;
    Prim: IPCB_Primitive;
begin
    JsonOpenObject('');
    JsonWriteString('name', Comp.Name, True);
    JsonWriteString('description', Comp.Description, True);
    JsonWriteCoord('height', Comp.Height, True);

    JsonOpenArray('primitives');

    GroupIterator := Comp.GroupIterator_Create;
    GroupIterator.AddFilter_ObjectSet(MkSet(ePadObject, eTrackObject, eArcObject,
        eTextObject, eFillObject, eRegionObject, ePolyObject, eComponentBodyObject,
        eDimensionObject, eCoordinateObject));

    Prim := GroupIterator.FirstPCBObject;
    while Prim <> nil do
    begin
        case Prim.ObjectId of
            ePadObject: ExportPcbPadToJson(Prim, True);
            eTrackObject: ExportPcbTrackToJson(Prim, True);
            eArcObject: ExportPcbArcToJson(Prim, True);
            eTextObject: ExportPcbTextToJson(Prim, True);
            eFillObject: ExportPcbFillToJson(Prim, True);
            eRegionObject: ExportPcbRegionToJson(Prim, True);
            ePolyObject: ExportPcbPolygonToJson(Prim, True);
            eComponentBodyObject: ExportPcbComponentBodyToJson(Prim, True);
            eDimensionObject: ExportPcbDimensionToJson(Prim, True);
            eCoordinateObject: ExportPcbCoordinateToJson(Prim, True);
        end;
        Prim := GroupIterator.NextPCBObject;
    end;

    Comp.GroupIterator_Destroy(GroupIterator);

    JsonCloseArray(True);

    // Additional IPCB_LibComponent properties
    JsonWriteFloat('area', Comp.Area, True);
    JsonWriteInteger('componentKind', Comp.ComponentKind, True);
    JsonWriteString('itemGUID', Comp.ItemGUID, True);
    JsonWriteString('itemRevisionGUID', Comp.ItemRevisionGUID, True);
    JsonWriteBoolean('primitiveLock', Comp.PrimitiveLock, True);
    JsonWriteCoord('x', Comp.x, True);
    JsonWriteCoord('y', Comp.y, True);

    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Comp);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbLibToJson(PCBLib: IPCB_Library; JsonPath: String);
var
    LibIterator: IPCB_LibraryIterator;
    Comp: IPCB_LibComponent;
begin
    if PCBLib = nil then Exit;

    JsonBegin;
    JsonOpenObject('');

    JsonOpenObject('metadata');
    JsonWriteString('exportType', 'PcbLib', True);
    JsonWriteString('fileName', ExtractFileName(PCBLib.Board.FileName), True);
    JsonWriteString('exportedBy', 'AltiumSharp FileToJsonConverter', True);
    JsonWriteString('version', '1.0', False);
    JsonCloseObject(True);

    // IPCB_Library properties
    JsonOpenObject('libraryProperties');
    JsonWriteInteger('libraryID', PCBLib.LibraryID, True);
    JsonWriteString('folderGUID', PCBLib.FolderGUID, True);
    JsonWriteString('vaultGUID', PCBLib.VaultGUID, True);
    JsonWriteString('lifeCycleDefinitionGUID', PCBLib.LifeCycleDefinitionGUID, True);
    JsonWriteString('revisionNamingSchemeGUID', PCBLib.RevisionNamingSchemeGUID, True);
    JsonWriteString('gridGuides', PCBLib.GridNGuides, True);
    JsonWriteBoolean('isSimpleDesignMode', PCBLib.IsSimpleDesignMode, True);
    JsonWriteBoolean('isSingleComponentMode', PCBLib.IsSingleComponentMode, False);
    JsonCloseObject(True);

    JsonOpenArray('footprints');

    LibIterator := PCBLib.LibraryIterator_Create;
    LibIterator.SetState_FilterAll;

    Comp := LibIterator.FirstPCBObject;
    while Comp <> nil do
    begin
        ExportPcbLibComponentToJson(Comp, True);
        Comp := LibIterator.NextPCBObject;
    end;

    PCBLib.LibraryIterator_Destroy(LibIterator);

    JsonCloseArray(False);
    JsonCloseObject(False);

    JsonEnd(JsonPath);
end;

{==============================================================================
  SCHEMATIC LIBRARY JSON EXPORT
==============================================================================}

procedure ExportSchPinToJson(Pin: ISch_Pin; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Pin', True);

    // Basic identification
    JsonWriteString('name', Pin.Name, True);
    JsonWriteString('designator', Pin.Designator, True);
    JsonWriteString('description', Pin.Description, True);
    JsonWriteString('defaultValue', Pin.DefaultValue, True);

    // Position and orientation
    JsonWriteInteger('x', Pin.Location.X, True);
    JsonWriteInteger('y', Pin.Location.Y, True);
    JsonWriteInteger('orientation', Ord(Pin.Orientation), True);

    // Electrical properties
    JsonWriteInteger('electrical', Ord(Pin.Electrical), True);
    JsonWriteInteger('formalType', Ord(Pin.FormalType), True);

    // Dimensions
    JsonWriteInteger('pinLength', Pin.PinLength, True);
    JsonWriteInteger('pinPackageLength', Pin.PinPackageLength, True);
    JsonWriteInteger('width', Pin.Width, True);

    // Visibility
    JsonWriteBoolean('showName', Pin.ShowName, True);
    JsonWriteBoolean('showDesignator', Pin.ShowDesignator, True);
    JsonWriteBoolean('isHidden', Pin.IsHidden, True);

    // Symbol properties
    JsonWriteInteger('symbol_Inner', Ord(Pin.Symbol_Inner), True);
    JsonWriteInteger('symbol_InnerEdge', Ord(Pin.Symbol_InnerEdge), True);
    JsonWriteInteger('symbol_Outer', Ord(Pin.Symbol_Outer), True);
    JsonWriteInteger('symbol_OuterEdge', Ord(Pin.Symbol_OuterEdge), True);
    JsonWriteInteger('symbol_LineWidth', Ord(Pin.Symbol_LineWidth), True);

    // Designator customization
    JsonWriteInteger('designator_CustomColor', Pin.Designator_CustomColor, True);
    JsonWriteInteger('designator_CustomFontID', Pin.Designator_CustomFontID, True);
    JsonWriteInteger('designator_CustomPosition_Margin', Pin.Designator_CustomPosition_Margin, True);
    JsonWriteInteger('designator_CustomPosition_RotationAnchor', Ord(Pin.Designator_CustomPosition_RotationAnchor), True);
    JsonWriteInteger('designator_CustomPosition_RotationRelative', Ord(Pin.Designator_CustomPosition_RotationRelative), True);
    JsonWriteInteger('designator_FontMode', Ord(Pin.Designator_FontMode), True);
    JsonWriteInteger('designator_PositionMode', Ord(Pin.Designator_PositionMode), True);

    // Name customization
    JsonWriteInteger('name_CustomColor', Pin.Name_CustomColor, True);
    JsonWriteInteger('name_CustomFontID', Pin.Name_CustomFontID, True);
    JsonWriteInteger('name_CustomPosition_Margin', Pin.Name_CustomPosition_Margin, True);
    JsonWriteInteger('name_CustomPosition_RotationAnchor', Ord(Pin.Name_CustomPosition_RotationAnchor), True);
    JsonWriteInteger('name_CustomPosition_RotationRelative', Ord(Pin.Name_CustomPosition_RotationRelative), True);
    JsonWriteInteger('name_FontMode', Ord(Pin.Name_FontMode), True);
    JsonWriteInteger('name_PositionMode', Ord(Pin.Name_PositionMode), True);

    // Swap IDs
    JsonWriteString('swapId_Pair', Pin.SwapId_Pair, True);
    JsonWriteString('swapId_Part', Pin.SwapId_Part, True);
    JsonWriteString('swapId_PartPin', Pin.SwapId_PartPin, True);
    JsonWriteString('swapId_Pin', Pin.SwapId_Pin, True);
    JsonWriteString('hiddenNetName', Pin.HiddenNetName, True);

    // Visual properties
    JsonWriteInteger('color', Pin.Color, True);
    JsonWriteInteger('areaColor', Pin.AreaColor, True);

    // State properties

    // Propagation delay
    JsonWriteFloat('propagationDelay', Pin.PropagationDelay, True);

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Pin);


    JsonCloseObject(AddComma);
end;

procedure ExportSchLineToJson(Line: ISch_Line; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Line', True);

    // Position
    JsonWriteInteger('x1', Line.Location.X, True);
    JsonWriteInteger('y1', Line.Location.Y, True);
    JsonWriteInteger('x2', Line.Corner.X, True);
    JsonWriteInteger('y2', Line.Corner.Y, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(Line.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Line.LineStyle), True);

    // Visual properties
    JsonWriteInteger('color', Line.Color, True);
    JsonWriteInteger('areaColor', Line.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_Line properties
    // Import_FromUser is an interactive method (opens dialog), not a property - skip
    // Base graphical object properties
    ExportSchBaseProperties(Line);



    try
        if Line.BoundingRectangle <> nil then
            JsonWriteString('boundingRectangle_ref', 'present', False)
        else
            JsonWriteString('boundingRectangle_ref', '', False);
    except
        JsonWriteString('boundingRectangle_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchRectangleToJson(Rect: ISch_Rectangle; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Rectangle', True);

    // Position
    JsonWriteInteger('x1', Rect.Location.X, True);
    JsonWriteInteger('y1', Rect.Location.Y, True);
    JsonWriteInteger('x2', Rect.Corner.X, True);
    JsonWriteInteger('y2', Rect.Corner.Y, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(Rect.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Rect.LineStyle), True);

    // Fill properties
    JsonWriteBoolean('isSolid', Rect.IsSolid, True);
    JsonWriteBoolean('transparent', Rect.Transparent, True);

    // Visual properties
    JsonWriteInteger('color', Rect.Color, True);
    JsonWriteInteger('areaColor', Rect.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Rect);


    JsonCloseObject(AddComma);
end;

procedure ExportSchArcToJson(Arc: ISch_Arc; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Arc', True);

    // Position and geometry
    JsonWriteInteger('x', Arc.Location.X, True);
    JsonWriteInteger('y', Arc.Location.Y, True);
    JsonWriteInteger('radius', Arc.Radius, True);
    JsonWriteFloat('startAngle', Arc.StartAngle, True);
    JsonWriteFloat('endAngle', Arc.EndAngle, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(Arc.LineWidth), True);

    // Visual properties
    JsonWriteInteger('color', Arc.Color, True);
    JsonWriteInteger('areaColor', Arc.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_Arc properties
    try
        JsonWriteString('i_ObjectAddress', 'OBJECT_REF', True);
    except
        JsonWriteString('i_ObjectAddress', 'ERROR', True);
    end;
    // Base graphical object properties
    ExportSchBaseProperties(Arc);


    JsonCloseObject(AddComma);
end;

procedure ExportSchPolygonToJson(Polygon: ISch_Polygon; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Polygon', True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(Polygon.LineWidth), True);

    // Fill properties
    JsonWriteBoolean('isSolid', Polygon.IsSolid, True);
    JsonWriteBoolean('transparent', Polygon.Transparent, True);

    // Visual properties
    JsonWriteInteger('color', Polygon.Color, True);
    JsonWriteInteger('areaColor', Polygon.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Vertices
    JsonWriteInteger('vertexCount', Polygon.VerticesCount, True);

    JsonOpenArray('vertices');
    for I := 1 to Polygon.VerticesCount do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', Polygon.Vertex[I].X, True);
        JsonWriteInteger('y', Polygon.Vertex[I].Y, False);
        JsonCloseObject(I < Polygon.VerticesCount);
    end;
    JsonCloseArray(False);

    // Additional ISch_Polygon properties
    JsonWriteInteger('location', Polygon.Location, True);
    // Base graphical object properties
    ExportSchBaseProperties(Polygon);



    try
        if Polygon.BoundingRectangle <> nil then
            JsonWriteString('boundingRectangle_ref', 'present', False)
        else
            JsonWriteString('boundingRectangle_ref', '', False);
    except
        JsonWriteString('boundingRectangle_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchPolylineToJson(Polyline: ISch_Polyline; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Polyline', True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(Polyline.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Polyline.LineStyle), True);

    // Line shapes (arrow heads)
    JsonWriteInteger('startLineShape', Ord(Polyline.StartLineShape), True);
    JsonWriteInteger('endLineShape', Ord(Polyline.EndLineShape), True);
    JsonWriteInteger('lineShapeSize', Ord(Polyline.LineShapeSize), True);

    // Fill properties
    JsonWriteBoolean('isSolid', Polyline.IsSolid, True);
    JsonWriteBoolean('transparent', Polyline.Transparent, True);

    // Visual properties
    JsonWriteInteger('color', Polyline.Color, True);
    JsonWriteInteger('areaColor', Polyline.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Vertices
    JsonWriteInteger('vertexCount', Polyline.VerticesCount, True);

    JsonOpenArray('vertices');
    for I := 1 to Polyline.VerticesCount do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', Polyline.Vertex[I].X, True);
        JsonWriteInteger('y', Polyline.Vertex[I].Y, False);
        JsonCloseObject(I < Polyline.VerticesCount);
    end;
    JsonCloseArray(False);

    // Additional ISch_Polyline properties
    // Import_FromUser is an interactive method (opens dialog), not a property - skip
    JsonWriteInteger('location', Polyline.Location, True);
    // Base graphical object properties
    ExportSchBaseProperties(Polyline);



    try
        if Polyline.SchIterator_Create <> nil then
            JsonWriteString('schIterator_Create_ref', 'present', False)
        else
            JsonWriteString('schIterator_Create_ref', '', False);
    except
        JsonWriteString('schIterator_Create_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchLabelToJson(Lbl: ISch_Label; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Label', True);

    // Text properties
    JsonWriteString('text', Lbl.Text, True);
    JsonWriteString('formula', Lbl.Formula, True);
    JsonWriteString('calculatedValueString', Lbl.CalculatedValueString, True);
    JsonWriteString('displayString', Lbl.DisplayString, True);
    JsonWriteString('overrideDisplayString', Lbl.OverrideDisplayString, True);

    // Position and orientation
    JsonWriteInteger('x', Lbl.Location.X, True);
    JsonWriteInteger('y', Lbl.Location.Y, True);
    JsonWriteInteger('orientation', Ord(Lbl.Orientation), True);
    JsonWriteBoolean('isMirrored', Lbl.IsMirrored, True);

    // Font and text properties
    JsonWriteInteger('fontID', Lbl.FontID, True);
    JsonWriteInteger('justification', Ord(Lbl.Justification), True);

    // Visual properties
    JsonWriteInteger('color', Lbl.Color, True);
    JsonWriteInteger('areaColor', Lbl.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Lbl);


    JsonCloseObject(AddComma);
end;

procedure ExportSchEllipseToJson(Ellipse: ISch_Ellipse; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Ellipse', True);

    // Position and geometry
    JsonWriteInteger('x', Ellipse.Location.X, True);
    JsonWriteInteger('y', Ellipse.Location.Y, True);
    JsonWriteInteger('radius', Ellipse.Radius, True);
    JsonWriteInteger('secondaryRadius', Ellipse.SecondaryRadius, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(Ellipse.LineWidth), True);

    // Fill properties
    JsonWriteBoolean('isSolid', Ellipse.IsSolid, True);
    JsonWriteBoolean('transparent', Ellipse.Transparent, True);

    // Visual properties
    JsonWriteInteger('color', Ellipse.Color, True);
    JsonWriteInteger('areaColor', Ellipse.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Ellipse);


    JsonCloseObject(AddComma);
end;

procedure ExportSchRoundRectToJson(RoundRect: ISch_RoundRectangle; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'RoundRectangle', True);

    // Position
    JsonWriteInteger('x1', RoundRect.Location.X, True);
    JsonWriteInteger('y1', RoundRect.Location.Y, True);
    JsonWriteInteger('x2', RoundRect.Corner.X, True);
    JsonWriteInteger('y2', RoundRect.Corner.Y, True);

    // Corner radii
    JsonWriteInteger('cornerXRadius', RoundRect.CornerXRadius, True);
    JsonWriteInteger('cornerYRadius', RoundRect.CornerYRadius, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(RoundRect.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(RoundRect.LineStyle), True);

    // Fill properties
    JsonWriteBoolean('isSolid', RoundRect.IsSolid, True);
    JsonWriteBoolean('transparent', RoundRect.Transparent, True);

    // Visual properties
    JsonWriteInteger('color', RoundRect.Color, True);
    JsonWriteInteger('areaColor', RoundRect.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(RoundRect);



    try
        if RoundRect.BoundingRectangle <> nil then
            JsonWriteString('boundingRectangle_ref', 'present', True)
        else
            JsonWriteString('boundingRectangle_ref', '', True);
    except
        JsonWriteString('boundingRectangle_ref', 'ERROR', True);
    end;
    try
        if RoundRect.BoundingRectangle_Full <> nil then
            JsonWriteString('boundingRectangle_Full_ref', 'present', False)
        else
            JsonWriteString('boundingRectangle_Full_ref', '', False);
    except
        JsonWriteString('boundingRectangle_Full_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchTextFrameToJson(TextFrame: ISch_TextFrame; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'TextFrame', True);

    // Text content
    JsonWriteString('text', TextFrame.Text, True);

    // Position
    JsonWriteInteger('x1', TextFrame.Location.X, True);
    JsonWriteInteger('y1', TextFrame.Location.Y, True);
    JsonWriteInteger('x2', TextFrame.Corner.X, True);
    JsonWriteInteger('y2', TextFrame.Corner.Y, True);

    // Font and text properties
    JsonWriteInteger('fontID', TextFrame.FontID, True);
    JsonWriteInteger('alignment', Ord(TextFrame.Alignment), True);
    JsonWriteBoolean('wordWrap', TextFrame.WordWrap, True);
    JsonWriteBoolean('clipToRect', TextFrame.ClipToRect, True);
    JsonWriteInteger('textMargin', TextFrame.TextMargin, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(TextFrame.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(TextFrame.LineStyle), True);

    // Border and fill
    JsonWriteBoolean('showBorder', TextFrame.ShowBorder, True);
    JsonWriteBoolean('isSolid', TextFrame.IsSolid, True);
    JsonWriteBoolean('transparent', TextFrame.Transparent, True);

    // Visual properties
    JsonWriteInteger('color', TextFrame.Color, True);
    JsonWriteInteger('areaColor', TextFrame.AreaColor, True);
    JsonWriteInteger('textColor', TextFrame.TextColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_TextFrame properties
    JsonWriteString('getHash', TextFrame.GetHash, True);
    // Import_FromUser is an interactive method (opens dialog), not a property - skip
    try
        JsonWriteString('i_ObjectAddress', 'OBJECT_REF', True);
    except
        JsonWriteString('i_ObjectAddress', 'ERROR', True);
    end;
    // Base graphical object properties
    ExportSchBaseProperties(TextFrame);


    JsonCloseObject(AddComma);
end;

procedure ExportSchImageToJson(Image: ISch_Image; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Image', True);

    // Position
    JsonWriteInteger('x1', Image.Location.X, True);
    JsonWriteInteger('y1', Image.Location.Y, True);
    JsonWriteInteger('x2', Image.Corner.X, True);
    JsonWriteInteger('y2', Image.Corner.Y, True);

    // Image properties
    JsonWriteString('fileName', Image.FileName, True);
    JsonWriteBoolean('keepAspect', Image.KeepAspect, True);

    // Border and fill
    JsonWriteInteger('lineWidth', Ord(Image.LineWidth), True);
    JsonWriteBoolean('isSolid', Image.IsSolid, True);
    JsonWriteBoolean('transparent', Image.Transparent, True);

    // Visual properties
    JsonWriteInteger('color', Image.Color, True);
    JsonWriteInteger('areaColor', Image.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_Image properties
    JsonWriteBoolean('embedImage', Image.EmbedImage, True);
    JsonWriteInteger('lineStyle', Image.LineStyle, True);
    // Base graphical object properties
    ExportSchBaseProperties(Image);


    JsonCloseObject(AddComma);
end;

procedure ExportSchDesignatorToJson(Desig: ISch_Designator; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Designator', True);

    // Position
    JsonWriteInteger('x', Desig.Location.X, True);
    JsonWriteInteger('y', Desig.Location.Y, True);
    JsonWriteInteger('orientation', Ord(Desig.Orientation), True);
    JsonWriteBoolean('isMirrored', Desig.IsMirrored, True);

    // Text properties
    JsonWriteString('text', Desig.Text, True);
    JsonWriteInteger('fontID', Desig.FontID, True);
    JsonWriteInteger('justification', Ord(Desig.Justification), True);

    // Visual properties
    JsonWriteInteger('color', Desig.Color, True);
    JsonWriteInteger('areaColor', Desig.AreaColor, True);

    // Visibility
    JsonWriteBoolean('isHidden', Desig.IsHidden, True);
    JsonWriteBoolean('showName', Desig.ShowName, True);
    JsonWriteBoolean('autoposition', Desig.Autoposition, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_Designator properties
    JsonWriteBoolean('allowDatabaseSynchronize', Desig.AllowDatabaseSynchronize, True);
    JsonWriteBoolean('allowLibrarySynchronize', Desig.AllowLibrarySynchronize, True);
    JsonWriteString('description', Desig.Description, True);
    JsonWriteString('displayString', Desig.DisplayString, True);
    JsonWriteString('formula', Desig.Formula, True);
    JsonWriteBoolean('isConfigurable', Desig.IsConfigurable, True);
    JsonWriteBoolean('isRule', Desig.IsRule, True);
    JsonWriteBoolean('isSystemParameter', Desig.IsSystemParameter, True);
    JsonWriteString('name', Desig.Name, True);
    JsonWriteBoolean('nameIsReadOnly', Desig.NameIsReadOnly, True);
    JsonWriteString('overrideDisplayString', Desig.OverrideDisplayString, True);
    JsonWriteInteger('paramType', Desig.ParamType, True);
    JsonWriteString('physicalDesignator', Desig.PhysicalDesignator, True);
    JsonWriteInteger('readOnlyState', Desig.ReadOnlyState, True);
    JsonWriteInteger('textHorzAnchor', Desig.TextHorzAnchor, True);
    JsonWriteInteger('textVertAnchor', Desig.TextVertAnchor, True);
    JsonWriteBoolean('valueIsReadOnly', Desig.ValueIsReadOnly, True);
    try
        JsonWriteString('variantOption', 'OBJECT_REF', True);
    except
        JsonWriteString('variantOption', 'ERROR', True);
    end;
    // Base graphical object properties
    ExportSchBaseProperties(Desig);


    JsonCloseObject(AddComma);
end;

procedure ExportSchNoteToJson(Note: ISch_Note; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Note', True);

    // Position
    JsonWriteInteger('x1', Note.Location.X, True);
    JsonWriteInteger('y1', Note.Location.Y, True);
    JsonWriteInteger('x2', Note.Corner.X, True);
    JsonWriteInteger('y2', Note.Corner.Y, True);

    // Text properties
    JsonWriteString('text', Note.Text, True);
    JsonWriteString('author', Note.Author, True);
    JsonWriteInteger('fontID', Note.FontID, True);
    JsonWriteInteger('alignment', Ord(Note.Alignment), True);
    JsonWriteBoolean('wordWrap', Note.WordWrap, True);
    JsonWriteBoolean('clipToRect', Note.ClipToRect, True);

    // Border and fill
    JsonWriteBoolean('showBorder', Note.ShowBorder, True);
    JsonWriteBoolean('isSolid', Note.IsSolid, True);
    JsonWriteBoolean('collapsed', Note.Collapsed, True);

    // Visual properties
    JsonWriteInteger('textColor', Note.TextColor, True);
    JsonWriteInteger('color', Note.Color, True);
    JsonWriteInteger('areaColor', Note.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_Note properties
    JsonWriteInteger('lineStyle', Note.LineStyle, True);
    JsonWriteInteger('lineWidth', Note.LineWidth, True);
    JsonWriteCoord('textMargin', Note.TextMargin, True);
    JsonWriteBoolean('transparent', Note.Transparent, True);
    // Base graphical object properties
    ExportSchBaseProperties(Note);


    JsonCloseObject(AddComma);
end;

procedure ExportSchBezierToJson(Bezier: ISch_Bezier; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Bezier', True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(Bezier.LineWidth), True);
    JsonWriteInteger('lineStyle', Bezier.LineStyle, True);

    // Fill properties
    JsonWriteBoolean('isSolid', Bezier.IsSolid, True);
    JsonWriteBoolean('transparent', Bezier.Transparent, True);

    // Visual properties
    JsonWriteInteger('color', Bezier.Color, True);
    JsonWriteInteger('areaColor', Bezier.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Vertices
    JsonWriteInteger('vertexCount', Bezier.VerticesCount, True);

    JsonOpenArray('vertices');
    for I := 1 to Bezier.VerticesCount do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', Bezier.Vertex[I].X, True);
        JsonWriteInteger('y', Bezier.Vertex[I].Y, True);

    try
        if Bezier.Replicate <> nil then
            JsonWriteString('replicate_ref', 'present', False)
        else
            JsonWriteString('replicate_ref', '', False);
    except
        JsonWriteString('replicate_ref', 'ERROR', False);
    end;

    try
        if Bezier.BoundingRectangle <> nil then
            JsonWriteString('boundingRectangle_ref', 'present', False)
        else
            JsonWriteString('boundingRectangle_ref', '', False);
    except
        JsonWriteString('boundingRectangle_ref', 'ERROR', False);
    end;
        JsonCloseObject(I < Bezier.VerticesCount);
    end;
    JsonCloseArray(False);

    // Additional ISch_Bezier properties
    JsonWriteString('getState_WiringDiagramOriginUniqueId', Bezier.GetState_WiringDiagramOriginUniqueId, True);
    // Import_FromUser is an interactive method (opens dialog), not a property - skip
    try
        JsonWriteString('i_ObjectAddress', 'OBJECT_REF', True);
    except
        JsonWriteString('i_ObjectAddress', 'ERROR', True);
    end;
    JsonWriteInteger('location', Bezier.Location, True);
    // Base graphical object properties
    ExportSchBaseProperties(Bezier);


    JsonCloseObject(AddComma);
end;

procedure ExportSchEllipticalArcToJson(EArc: ISch_EllipticalArc; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'EllipticalArc', True);

    // Position and geometry
    JsonWriteInteger('x', EArc.Location.X, True);
    JsonWriteInteger('y', EArc.Location.Y, True);
    JsonWriteInteger('radius', EArc.Radius, True);
    JsonWriteInteger('secondaryRadius', EArc.SecondaryRadius, True);
    JsonWriteFloat('startAngle', EArc.StartAngle, True);
    JsonWriteFloat('endAngle', EArc.EndAngle, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(EArc.LineWidth), True);

    // Visual properties
    JsonWriteInteger('color', EArc.Color, True);
    JsonWriteInteger('areaColor', EArc.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_EllipticalArc properties
    try
        JsonWriteString('i_ObjectAddress', 'OBJECT_REF', True);
    except
        JsonWriteString('i_ObjectAddress', 'ERROR', True);
    end;
    // Base graphical object properties
    ExportSchBaseProperties(EArc);


    JsonCloseObject(AddComma);
end;

procedure ExportSchPieToJson(Pie: ISch_Pie; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Pie', True);

    // Position and geometry
    JsonWriteInteger('x', Pie.Location.X, True);
    JsonWriteInteger('y', Pie.Location.Y, True);
    JsonWriteInteger('radius', Pie.Radius, True);
    JsonWriteInteger('secondaryRadius', Pie.SecondaryRadius, True);
    JsonWriteFloat('startAngle', Pie.StartAngle, True);
    JsonWriteFloat('endAngle', Pie.EndAngle, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(Pie.LineWidth), True);

    // Fill properties
    JsonWriteBoolean('isSolid', Pie.IsSolid, True);

    // Visual properties
    JsonWriteInteger('color', Pie.Color, True);
    JsonWriteInteger('areaColor', Pie.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Pie);


    JsonCloseObject(AddComma);
end;

procedure ExportSchProbeToJson(Probe: ISch_Probe; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Probe', True);
    JsonWriteString('name', Probe.Name, True);
    JsonWriteInteger('x', Probe.Location.X, True);
    JsonWriteInteger('y', Probe.Location.Y, True);
    JsonWriteInteger('orientation', Ord(Probe.Orientation), True);
    JsonWriteInteger('style', Probe.Style, True);
    JsonWriteInteger('fontID', Probe.FontID, True);

    // Visual properties
    JsonWriteInteger('color', Probe.Color, True);
    JsonWriteInteger('areaColor', Probe.AreaColor, True);

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Probe);

    JsonCloseObject(AddComma);
end;

procedure ExportSchSignalHarnessToJson(Harness: ISch_SignalHarness; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SignalHarness', True);
    JsonWriteInteger('vertexCount', Harness.VerticesCount, True);
    JsonWriteInteger('lineWidth', Ord(Harness.LineWidth), True);
    JsonWriteInteger('lineStyle', Harness.LineStyle, True);
    JsonWriteInteger('color', Harness.Color, True);
    JsonWriteInteger('underLineColor', Harness.UnderLineColor, True);

    JsonOpenArray('vertices');
    for I := 1 to Harness.VerticesCount do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', Harness.Vertex[I].X, True);
        JsonWriteInteger('y', Harness.Vertex[I].Y, False);
        JsonCloseObject(I < Harness.VerticesCount);
    end;
    JsonCloseArray(False);
    // Base graphical object properties
    ExportSchBaseProperties(Harness);


    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessConnectorToJson(HC: ISch_HarnessConnector; AddComma: Boolean);
var
    EntryIterator: ISch_Iterator;
    Entry: ISch_HarnessEntry;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessConnector', True);
    JsonWriteString('harnessType', HC.HarnessType, True);

    // Position and dimensions
    JsonWriteInteger('x1', HC.Location.X, True);
    JsonWriteInteger('y1', HC.Location.Y, True);
    JsonWriteInteger('x2', HC.Corner.X, True);
    JsonWriteInteger('y2', HC.Corner.Y, True);
    JsonWriteInteger('xSize', HC.XSize, True);
    JsonWriteInteger('ySize', HC.YSize, True);

    // Entry properties
    JsonWriteInteger('masterEntryLocation', HC.MasterEntryLocation, True);
    JsonWriteInteger('primaryConnectionPosition', HC.PrimaryConnectionPosition, True);
    JsonWriteBoolean('hideHarnessConnectorType', HC.HideHarnessConnectorType, True);

    // Visual properties
    JsonWriteInteger('lineWidth', Ord(HC.LineWidth), True);
    JsonWriteBoolean('isSolid', HC.IsSolid, True);
    JsonWriteBoolean('transparent', HC.Transparent, True);
    JsonWriteInteger('color', HC.Color, True);
    JsonWriteInteger('areaColor', HC.AreaColor, True);

    // State flags

    // Unique identifiers

    // Export contained harness entries
    JsonOpenArray('harnessEntries');
    EntryIterator := HC.SchIterator_Create;
    EntryIterator.AddFilter_ObjectSet(MkSet(eHarnessEntry));

    Entry := EntryIterator.FirstSchObject;
    while Entry <> nil do
    begin
        ExportSchHarnessEntryToJson(Entry, True);
        Entry := EntryIterator.NextSchObject;
    end;

    HC.SchIterator_Destroy(EntryIterator);
    JsonCloseArray(False);

    // Additional ISch_HarnessConnector properties
    // Base graphical object properties
    ExportSchBaseProperties(HC);


    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessEntryToJson(HE: ISch_HarnessEntry; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessEntry', True);
    JsonWriteString('name', HE.Name, True);
    JsonWriteString('harnessType', HE.HarnessType, True);

    // Position
    JsonWriteInteger('x', HE.Location.X, True);
    JsonWriteInteger('y', HE.Location.Y, True);
    JsonWriteInteger('distanceFromTop', HE.DistanceFromTop, True);
    JsonWriteInteger('side', HE.Side, True);

    // Visual properties
    JsonWriteInteger('textColor', HE.TextColor, True);
    JsonWriteInteger('textFontID', HE.TextFontID, True);
    JsonWriteInteger('textStyle', HE.TextStyle, True);
    JsonWriteInteger('harnessColor', HE.HarnessColor, True);
    JsonWriteInteger('arrowKind', HE.ArrowKind, True);

    // Display override
    JsonWriteBoolean('overrideDisplayString', HE.OverrideDisplayString, True);
    JsonWriteString('displayString', HE.DisplayString, True);

    // State flags

    // Unique identifiers

    // Additional ISch_HarnessEntry properties
    JsonWriteInteger('areaColor', HE.AreaColor, True);
    JsonWriteInteger('color', HE.Color, True);
    // Base graphical object properties
    ExportSchBaseProperties(HE);


    try
        if HE.OwnerHarnessConnector <> nil then
            JsonWriteString('ownerHarnessConnector_ref', 'present', False)
        else
            JsonWriteString('ownerHarnessConnector_ref', '', False);
    except
        JsonWriteString('ownerHarnessConnector_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

{==============================================================================
  NEW SCHEMATIC EXPORT PROCEDURES - Extended Coverage
  These procedures export additional Schematic object types to achieve complete
  coverage of the Altium API.
==============================================================================}

procedure ExportSchCircleToJson(Circle: ISch_Circle; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Circle', True);

    // Center position
    JsonWriteInteger('x', Circle.Location.X, True);
    JsonWriteInteger('y', Circle.Location.Y, True);
    JsonWriteInteger('radius', Circle.Radius, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(Circle.LineWidth), True);

    // Fill properties
    JsonWriteBoolean('isSolid', Circle.IsSolid, True);
    JsonWriteBoolean('transparent', Circle.Transparent, True);

    // Visual properties
    JsonWriteInteger('color', Circle.Color, True);
    JsonWriteInteger('areaColor', Circle.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_Circle properties
    try
        JsonWriteString('i_ObjectAddress', 'OBJECT_REF', True);
    except
        JsonWriteString('i_ObjectAddress', 'ERROR', True);
    end;
    // Base graphical object properties
    ExportSchBaseProperties(Circle);

    try
        if Circle.BoundingRectangle <> nil then
            JsonWriteString('boundingRectangle_ref', 'present', False)
        else
            JsonWriteString('boundingRectangle_ref', '', False);
    except
        JsonWriteString('boundingRectangle_ref', 'ERROR', False);
    end;

    JsonCloseObject(AddComma);
end;

procedure ExportSchVoltageToJson(Voltage: ISch_Voltage; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Voltage', True);

    // Position
    JsonWriteInteger('x', Voltage.Location.X, True);
    JsonWriteInteger('y', Voltage.Location.Y, True);

    // Voltage properties
    JsonWriteString('text', Voltage.Text, True);
    JsonWriteInteger('orientation', Ord(Voltage.Orientation), True);

    // Visual properties
    JsonWriteInteger('color', Voltage.Color, True);
    JsonWriteInteger('areaColor', Voltage.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Voltage);


    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessBundleToJson(Bundle: ISch_HarnessBundle; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessBundle', True);

    // Properties
    JsonWriteBoolean('autoWire', Bundle.AutoWire, True);
    JsonWriteInteger('length', Bundle.Length, True);
    JsonWriteBoolean('isSolid', Bundle.IsSolid, True);
    JsonWriteBoolean('transparent', Bundle.Transparent, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(Bundle.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Bundle.LineStyle), True);

    // Visual properties
    JsonWriteInteger('color', Bundle.Color, True);
    JsonWriteInteger('areaColor', Bundle.AreaColor, True);
    JsonWriteInteger('underlineColor', Bundle.UnderlineColor, True);

    // Vertices
    JsonWriteInteger('vertexCount', Bundle.VerticesCount, True);
    JsonOpenArray('vertices');
    for I := 0 to Bundle.VerticesCount - 1 do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', Bundle.Vertex[I].X, True);
        JsonWriteInteger('y', Bundle.Vertex[I].Y, True);

    try
        if Bundle.LengthParameter <> nil then
            JsonWriteString('lengthParameter_ref', 'present', True)
        else
            JsonWriteString('lengthParameter_ref', '', True);
    except
        JsonWriteString('lengthParameter_ref', 'ERROR', True);
    end;

    try
        JsonWriteBoolean('compilationMaskedSegment_0', Bundle.CompilationMaskedSegment[0], False);
    except
        JsonWriteString('compilationMaskedSegment_0', 'ERROR', False);
    end;
        JsonCloseObject(I < Bundle.VerticesCount - 1);
    end;
    JsonCloseArray(True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_HarnessBundle properties
    JsonWriteBoolean('designatorLocked', Bundle.DesignatorLocked, True);
    JsonWriteBoolean('editingEndPoint', Bundle.EditingEndPoint, True);
    JsonWriteInteger('location', Bundle.Location, True);
    // Base graphical object properties
    ExportSchBaseProperties(Bundle);


        try
        if Bundle.Designator <> nil then
            JsonWriteString('designator_ref', Bundle.Designator.Text, True)
        else
            JsonWriteString('designator_ref', '', True);
    except
        JsonWriteString('designator_ref', 'ERROR', True);
    end;
    try
        if Bundle.Comment <> nil then
            JsonWriteString('comment_ref', Bundle.Comment.Text, True)
        else
            JsonWriteString('comment_ref', '', True);
    except
        JsonWriteString('comment_ref', 'ERROR', True);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessCableToJson(Cable: ISch_HarnessCable; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessCable', True);

    // Cable properties
    JsonWriteBoolean('autoWire', Cable.AutoWire, True);
    JsonWriteBoolean('isSolid', Cable.IsSolid, True);
    JsonWriteBoolean('transparent', Cable.Transparent, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(Cable.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Cable.LineStyle), True);

    // Visual properties
    JsonWriteInteger('color', Cable.Color, True);
    JsonWriteInteger('areaColor', Cable.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_HarnessCable properties
    JsonWriteInteger('corner', Cable.Corner, True);
    JsonWriteBoolean('designatorLocked', Cable.DesignatorLocked, True);
    JsonWriteInteger('location', Cable.Location, True);
    JsonWriteInteger('orientation', Cable.Orientation, True);
    // Base graphical object properties
    ExportSchBaseProperties(Cable);


        try
        if Cable.Designator <> nil then
            JsonWriteString('designator_ref', Cable.Designator.Text, True)
        else
            JsonWriteString('designator_ref', '', True);
    except
        JsonWriteString('designator_ref', 'ERROR', True);
    end;
    try
        if Cable.Comment <> nil then
            JsonWriteString('comment_ref', Cable.Comment.Text, True)
        else
            JsonWriteString('comment_ref', '', True);
    except
        JsonWriteString('comment_ref', 'ERROR', True);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessWireToJson(Wire: ISch_HarnessWire; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessWire', True);

    // Wire properties
    JsonWriteBoolean('autoWire', Wire.AutoWire, True);
    JsonWriteBoolean('isSolid', Wire.IsSolid, True);
    JsonWriteBoolean('transparent', Wire.Transparent, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(Wire.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Wire.LineStyle), True);

    // Visual properties
    JsonWriteInteger('color', Wire.Color, True);
    JsonWriteInteger('areaColor', Wire.AreaColor, True);
    JsonWriteInteger('underlineColor', Wire.UnderlineColor, True);

    // Vertices
    JsonWriteInteger('vertexCount', Wire.VerticesCount, True);
    JsonOpenArray('vertices');
    for I := 0 to Wire.VerticesCount - 1 do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', Wire.Vertex[I].X, True);
        JsonWriteInteger('y', Wire.Vertex[I].Y, True);

    try
        if Wire.Description <> nil then
            JsonWriteString('description_ref', 'present', True)
        else
            JsonWriteString('description_ref', '', True);
    except
        JsonWriteString('description_ref', 'ERROR', True);
    end;

    try
        JsonWriteBoolean('compilationMaskedSegment_0', Wire.CompilationMaskedSegment[0], False);
    except
        JsonWriteString('compilationMaskedSegment_0', 'ERROR', False);
    end;
        JsonCloseObject(I < Wire.VerticesCount - 1);
    end;
    JsonCloseArray(True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_HarnessWire properties
    JsonWriteBoolean('designatorLocked', Wire.DesignatorLocked, True);
    JsonWriteBoolean('editingEndPoint', Wire.EditingEndPoint, True);
    JsonWriteInteger('location', Wire.Location, True);
    // Base graphical object properties
    ExportSchBaseProperties(Wire);


        try
        if Wire.Designator <> nil then
            JsonWriteString('designator_ref', Wire.Designator.Text, True)
        else
            JsonWriteString('designator_ref', '', True);
    except
        JsonWriteString('designator_ref', 'ERROR', True);
    end;
    try
        if Wire.Comment <> nil then
            JsonWriteString('comment_ref', Wire.Comment.Text, True)
        else
            JsonWriteString('comment_ref', '', True);
    except
        JsonWriteString('comment_ref', 'ERROR', True);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessSpliceToJson(Splice: ISch_HarnessSplice; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessSplice', True);

    // Position
    JsonWriteInteger('x', Splice.Location.X, True);
    JsonWriteInteger('y', Splice.Location.Y, True);
    JsonWriteInteger('orientation', Ord(Splice.Orientation), True);
    JsonWriteBoolean('isMirrored', Splice.IsMirrored, True);

    // Visual properties
    JsonWriteInteger('color', Splice.Color, True);
    JsonWriteInteger('areaColor', Splice.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_HarnessSplice properties
    JsonWriteInteger('borderColor', Splice.BorderColor, True);
    JsonWriteString('calculatedValueString', Splice.CalculatedValueString, True);
    JsonWriteBoolean('designatorLocked', Splice.DesignatorLocked, True);
    JsonWriteString('displayString', Splice.DisplayString, True);
    JsonWriteInteger('fontID', Splice.FontID, True);
    JsonWriteString('formula', Splice.Formula, True);
    JsonWriteInteger('justification', Splice.Justification, True);
    JsonWriteString('overrideDisplayString', Splice.OverrideDisplayString, True);
    JsonWriteInteger('style', Splice.Style, True);
    JsonWriteString('text', Splice.Text, True);
    // Base graphical object properties
    ExportSchBaseProperties(Splice);


        try
        if Splice.Designator <> nil then
            JsonWriteString('designator_ref', Splice.Designator.Text, True)
        else
            JsonWriteString('designator_ref', '', True);
    except
        JsonWriteString('designator_ref', 'ERROR', True);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessShieldToJson(Shield: ISch_HarnessShield; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessShield', True);

    // Position
    JsonWriteInteger('x', Shield.Location.X, True);
    JsonWriteInteger('y', Shield.Location.Y, True);
    JsonWriteInteger('orientation', Ord(Shield.Orientation), True);
    JsonWriteBoolean('isMirrored', Shield.IsMirrored, True);

    // Visual properties
    JsonWriteInteger('color', Shield.Color, True);
    JsonWriteInteger('areaColor', Shield.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_HarnessShield properties
    JsonWriteInteger('corner', Shield.Corner, True);
    JsonWriteBoolean('isSolid', Shield.IsSolid, True);
    JsonWriteInteger('lineStyle', Shield.LineStyle, True);
    JsonWriteInteger('lineWidth', Shield.LineWidth, True);
    JsonWriteInteger('style', Shield.Style, True);
    JsonWriteBoolean('transparent', Shield.Transparent, True);
    // Base graphical object properties
    ExportSchBaseProperties(Shield);


    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessTwistToJson(Twist: ISch_HarnessTwist; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessTwist', True);

    // Position
    JsonWriteInteger('x', Twist.Location.X, True);
    JsonWriteInteger('y', Twist.Location.Y, True);
    JsonWriteInteger('orientation', Ord(Twist.Orientation), True);
    JsonWriteBoolean('isMirrored', Twist.IsMirrored, True);

    // Visual properties
    JsonWriteInteger('color', Twist.Color, True);
    JsonWriteInteger('areaColor', Twist.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_HarnessTwist properties
    JsonWriteInteger('corner', Twist.Corner, True);
    JsonWriteBoolean('isSolid', Twist.IsSolid, True);
    JsonWriteInteger('lineStyle', Twist.LineStyle, True);
    JsonWriteInteger('lineWidth', Twist.LineWidth, True);
    JsonWriteBoolean('transparent', Twist.Transparent, True);
    // Base graphical object properties
    ExportSchBaseProperties(Twist);


    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessNoConnectToJson(NoConnect: ISch_HarnessNoConnect; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessNoConnect', True);

    // Position
    JsonWriteInteger('x', NoConnect.Location.X, True);
    JsonWriteInteger('y', NoConnect.Location.Y, True);

    // Visual properties
    JsonWriteInteger('color', NoConnect.Color, True);
    JsonWriteInteger('areaColor', NoConnect.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_HarnessNoConnect properties
    JsonWriteString('calculatedValueString', NoConnect.CalculatedValueString, True);
    JsonWriteString('displayString', NoConnect.DisplayString, True);
    JsonWriteInteger('fontID', NoConnect.FontID, True);
    JsonWriteString('formula', NoConnect.Formula, True);
    JsonWriteBoolean('isMirrored', NoConnect.IsMirrored, True);
    JsonWriteInteger('justification', NoConnect.Justification, True);
    JsonWriteInteger('orientation', NoConnect.Orientation, True);
    JsonWriteString('overrideDisplayString', NoConnect.OverrideDisplayString, True);
    JsonWriteInteger('pulloffLength', NoConnect.PulloffLength, True);
    JsonWriteBoolean('showName', NoConnect.ShowName, True);
    JsonWriteInteger('stripLength', NoConnect.StripLength, True);
    JsonWriteInteger('style', NoConnect.Style, True);
    JsonWriteString('text', NoConnect.Text, True);
    // Base graphical object properties
    ExportSchBaseProperties(NoConnect);


    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessPinToJson(HarnessPin: ISch_HarnessPin; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessPin', True);

    // Position
    JsonWriteInteger('x', HarnessPin.Location.X, True);
    JsonWriteInteger('y', HarnessPin.Location.Y, True);
    JsonWriteInteger('orientation', Ord(HarnessPin.Orientation), True);

    // Pin properties
    JsonWriteString('name', HarnessPin.Name, True);
    JsonWriteString('designator', HarnessPin.Designator, True);
    JsonWriteInteger('pinLength', HarnessPin.PinLength, True);

    // Visual properties
    JsonWriteInteger('color', HarnessPin.Color, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_HarnessPin properties
    JsonWriteInteger('areaColor', HarnessPin.AreaColor, True);
    JsonWriteString('defaultValue', HarnessPin.DefaultValue, True);
    JsonWriteString('description', HarnessPin.Description, True);
    JsonWriteInteger('designator_CustomColor', HarnessPin.Designator_CustomColor, True);
    JsonWriteInteger('designator_CustomFontID', HarnessPin.Designator_CustomFontID, True);
    JsonWriteCoord('designator_CustomPosition_Margin', HarnessPin.Designator_CustomPosition_Margin, True);
    JsonWriteInteger('designator_CustomPosition_RotationAnchor', HarnessPin.Designator_CustomPosition_RotationAnchor, True);
    JsonWriteInteger('designator_CustomPosition_RotationRelative', HarnessPin.Designator_CustomPosition_RotationRelative, True);
    JsonWriteInteger('designator_FontMode', HarnessPin.Designator_FontMode, True);
    JsonWriteInteger('designator_PositionMode', HarnessPin.Designator_PositionMode, True);
    JsonWriteInteger('electrical', HarnessPin.Electrical, True);
    JsonWriteInteger('formalType', HarnessPin.FormalType, True);
    JsonWriteString('hiddenNetName', HarnessPin.HiddenNetName, True);
    JsonWriteBoolean('isHidden', HarnessPin.IsHidden, True);
    JsonWriteInteger('name_CustomColor', HarnessPin.Name_CustomColor, True);
    JsonWriteInteger('name_CustomFontID', HarnessPin.Name_CustomFontID, True);
    JsonWriteCoord('name_CustomPosition_Margin', HarnessPin.Name_CustomPosition_Margin, True);
    JsonWriteInteger('name_CustomPosition_RotationAnchor', HarnessPin.Name_CustomPosition_RotationAnchor, True);
    JsonWriteInteger('name_CustomPosition_RotationRelative', HarnessPin.Name_CustomPosition_RotationRelative, True);
    JsonWriteInteger('name_FontMode', HarnessPin.Name_FontMode, True);
    JsonWriteInteger('name_PositionMode', HarnessPin.Name_PositionMode, True);
    JsonWriteCoord('pinPackageLength', HarnessPin.PinPackageLength, True);
    JsonWriteFloat('propagationDelay', HarnessPin.PropagationDelay, True);
    JsonWriteBoolean('showDesignator', HarnessPin.ShowDesignator, True);
    JsonWriteBoolean('showName', HarnessPin.ShowName, True);
    JsonWriteString('swapId_Pair', HarnessPin.SwapId_Pair, True);
    JsonWriteString('swapId_Part', HarnessPin.SwapId_Part, True);
    JsonWriteString('swapId_PartPin', HarnessPin.SwapId_PartPin, True);
    JsonWriteString('swapId_Pin', HarnessPin.SwapId_Pin, True);
    JsonWriteInteger('symbol_Inner', HarnessPin.Symbol_Inner, True);
    JsonWriteInteger('symbol_InnerEdge', HarnessPin.Symbol_InnerEdge, True);
    JsonWriteInteger('symbol_LineWidth', HarnessPin.Symbol_LineWidth, True);
    JsonWriteInteger('symbol_Outer', HarnessPin.Symbol_Outer, True);
    JsonWriteInteger('symbol_OuterEdge', HarnessPin.Symbol_OuterEdge, True);
    JsonWriteInteger('width', HarnessPin.Width, True);
    // Base graphical object properties
    ExportSchBaseProperties(HarnessPin);


    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessWireLabelToJson(Lab: ISch_HarnessWireLabel; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessWireLabel', True);

    // Position
    JsonWriteInteger('x', Lab.Location.X, True);
    JsonWriteInteger('y', Lab.Location.Y, True);
    JsonWriteInteger('orientation', Ord(Lab.Orientation), True);

    // Text properties
    JsonWriteString('text', Lab.Text, True);

    // Visual properties
    JsonWriteInteger('color', Lab.Color, True);
    JsonWriteInteger('areaColor', Lab.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_HarnessWireLabel properties
    JsonWriteString('calculatedValueString', Lab.CalculatedValueString, True);
    JsonWriteString('displayString', Lab.DisplayString, True);
    JsonWriteInteger('fontID', Lab.FontID, True);
    JsonWriteString('formula', Lab.Formula, True);
    JsonWriteBoolean('isMirrored', Lab.IsMirrored, True);
    JsonWriteInteger('justification', Lab.Justification, True);
    JsonWriteString('overrideDisplayString', Lab.OverrideDisplayString, True);
    // Base graphical object properties
    ExportSchBaseProperties(Lab);


    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessWireBreakToJson(WireBreak: ISch_HarnessWireBreak; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessWireBreak', True);

    // Position
    JsonWriteInteger('x', WireBreak.Location.X, True);
    JsonWriteInteger('y', WireBreak.Location.Y, True);

    // Visual properties
    JsonWriteInteger('color', WireBreak.Color, True);
    JsonWriteInteger('areaColor', WireBreak.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_HarnessWireBreak properties
    JsonWriteString('calculatedValueString', WireBreak.CalculatedValueString, True);
    JsonWriteInteger('crossSheetStyle', WireBreak.CrossSheetStyle, True);
    JsonWriteString('displayString', WireBreak.DisplayString, True);
    JsonWriteInteger('fontID', WireBreak.FontID, True);
    JsonWriteString('formula', WireBreak.Formula, True);
    JsonWriteBoolean('isCustomStyle', WireBreak.IsCustomStyle, True);
    JsonWriteBoolean('isMirrored', WireBreak.IsMirrored, True);
    JsonWriteInteger('justification', WireBreak.Justification, True);
    JsonWriteInteger('orientation', WireBreak.Orientation, True);
    JsonWriteString('overrideDisplayString', WireBreak.OverrideDisplayString, True);
    JsonWriteBoolean('showNetName', WireBreak.ShowNetName, True);
    JsonWriteInteger('style', WireBreak.Style, True);
    JsonWriteString('text', WireBreak.Text, True);
    // Base graphical object properties
    ExportSchBaseProperties(WireBreak);


    JsonCloseObject(AddComma);
end;

procedure ExportSchSheetToJson(Sheet: ISch_Sheet; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Sheet', True);

    // Sheet properties
    JsonWriteInteger('sheetStyle', Sheet.SheetStyle, True);
    JsonWriteInteger('sheetSizeX', Sheet.SheetSizeX, True);
    JsonWriteInteger('sheetSizeY', Sheet.SheetSizeY, True);
    JsonWriteInteger('sheetOrientation', Sheet.SheetOrientation, True);
    JsonWriteInteger('sheetNumberSpace', Sheet.SheetNumberSpace, True);

    // Border and margin
    JsonWriteInteger('borderColor', Sheet.BorderColor, True);
    JsonWriteInteger('marginWidth', Sheet.MarginWidth, True);
    JsonWriteInteger('workspaceOrientation', Sheet.WorkspaceOrientation, True);

    // Title block
    JsonWriteString('title', Sheet.Title, True);
    JsonWriteString('documentNumber', Sheet.DocumentNumber, True);
    JsonWriteString('revision', Sheet.Revision, True);
    JsonWriteString('date', Sheet.Date, True);
    JsonWriteString('approvedBy', Sheet.ApprovedBy, True);
    JsonWriteString('checkedBy', Sheet.CheckedBy, True);
    JsonWriteString('drawnBy', Sheet.DrawnBy, True);
    JsonWriteString('engineer', Sheet.Engineer, True);
    JsonWriteString('organization', Sheet.Organization, True);
    JsonWriteString('address1', Sheet.Address1, True);
    JsonWriteString('address2', Sheet.Address2, True);
    JsonWriteString('address3', Sheet.Address3, True);
    JsonWriteString('address4', Sheet.Address4, True);
    JsonWriteBoolean('titleBlockOn', Sheet.TitleBlockOn, True);

    // Grid settings
    JsonWriteBoolean('showGrids', Sheet.ShowGrids, True);
    JsonWriteInteger('snapGridSizeX', Sheet.SnapGridSizeX, True);
    JsonWriteInteger('snapGridSizeY', Sheet.SnapGridSizeY, True);
    JsonWriteInteger('visibleGridSizeX', Sheet.VisibleGridSizeX, True);
    JsonWriteInteger('visibleGridSizeY', Sheet.VisibleGridSizeY, True);

    // Reference zones
    JsonWriteInteger('referenceZonesOn', Sheet.ReferenceZonesOn, True);

    // Visual properties
    JsonWriteInteger('areaColor', Sheet.AreaColor, True);

    // Unique identifiers

    // Additional ISch_Sheet properties
    JsonWriteBoolean('borderOn', Sheet.BorderOn, True);
    try
        JsonWriteString('busConnections', 'OBJECT_REF', True);
    except
        JsonWriteString('busConnections', 'ERROR', True);
    end;
    JsonWriteInteger('color', Sheet.Color, True);
    JsonWriteCoord('customMarginWidth', Sheet.CustomMarginWidth, True);
    JsonWriteString('customSheetStyle', Sheet.CustomSheetStyle, True);
    JsonWriteCoord('customX', Sheet.CustomX, True);
    JsonWriteCoord('customXZones', Sheet.CustomXZones, True);
    JsonWriteCoord('customY', Sheet.CustomY, True);
    JsonWriteCoord('customYZones', Sheet.CustomYZones, True);
    JsonWriteInteger('displayUnit', Sheet.DisplayUnit, True);
    JsonWriteInteger('documentBorderStyle', Sheet.DocumentBorderStyle, True);
    JsonWriteBoolean('hasUnsavedUniqueIdChanges', Sheet.HasUnsavedUniqueIdChanges, True);
    JsonWriteBoolean('hotspotGridOn', Sheet.HotspotGridOn, True);
    JsonWriteCoord('hotspotGridSize', Sheet.HotspotGridSize, True);
    JsonWriteCoord('internalTolerance', Sheet.InternalTolerance, True);
    JsonWriteBoolean('isLibrary', Sheet.IsLibrary, True);
    JsonWriteString('itemRevisionGUID', Sheet.ItemRevisionGUID, True);
    JsonWriteString('loadFormat', Sheet.LoadFormat, True);
    JsonWriteInteger('location', Sheet.Location, True);
    JsonWriteInteger('minorVersion', Sheet.MinorVersion, True);
    JsonWriteString('propsRevisionGUID', Sheet.PropsRevisionGUID, True);
    JsonWriteString('propsVaultGUID', Sheet.PropsVaultGUID, True);
    JsonWriteInteger('referenceZoneStyle', Sheet.ReferenceZoneStyle, True);
    JsonWriteString('releaseItemGUID', Sheet.ReleaseItemGUID, True);
    JsonWriteString('releaseVaultGUID', Sheet.ReleaseVaultGUID, True);
    JsonWriteInteger('schDocID', Sheet.SchDocID, True);
    JsonWriteCoord('sheetMarginWidth', Sheet.SheetMarginWidth, True);
    JsonWriteInteger('sheetNumberSpaceSize', Sheet.SheetNumberSpaceSize, True);
    // Base graphical object properties
    ExportSchBaseProperties(Sheet);


    JsonCloseObject(AddComma);
end;

procedure ExportSchLibraryComponentToJson(LibComp: ISch_Component; AddComma: Boolean);
var
    Iterator: ISch_Iterator;
    Prim: ISch_GraphicalObject;
    Pin: ISch_Pin;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'LibraryComponent', True);

    // Basic identification
    JsonWriteString('libReference', LibComp.LibReference, True);
    JsonWriteString('description', LibComp.ComponentDescription, True);
    JsonWriteString('designator', LibComp.Designator.Text, True);
    JsonWriteString('comment', LibComp.Comment.Text, True);

    // Display properties
    JsonWriteInteger('displayMode', Ord(LibComp.DisplayMode), True);
    JsonWriteInteger('displayModeCount', LibComp.DisplayModeCount, True);
    JsonWriteInteger('partCount', LibComp.PartCount, True);
    JsonWriteInteger('currentPartId', LibComp.GetState_CurrentPartID, True);

    // Component kind and type
    JsonWriteInteger('componentKind', Ord(LibComp.ComponentKind), True);
    JsonWriteBoolean('isMultiPart', LibComp.IsMultiPartComponent, True);
    JsonWriteBoolean('showHiddenFields', LibComp.ShowHiddenFields, True);
    JsonWriteBoolean('showHiddenPins', LibComp.ShowHiddenPins, True);

    // Location
    JsonWriteInteger('x', LibComp.Location.X, True);
    JsonWriteInteger('y', LibComp.Location.Y, True);
    JsonWriteInteger('orientation', Ord(LibComp.Orientation), True);
    JsonWriteBoolean('isMirrored', LibComp.IsMirrored, True);

    // Visual properties
    JsonWriteInteger('pinColor', LibComp.PinColor, True);
    JsonWriteInteger('color', LibComp.Color, True);
    JsonWriteInteger('areaColor', LibComp.AreaColor, True);

    // Configuration
    JsonWriteString('configuratorName', LibComp.ConfiguratorName, True);
    JsonWriteString('configurationParameters', LibComp.ConfigurationParameters, True);

    // State
    JsonWriteBoolean('designatorLocked', LibComp.DesignatorLocked, True);
    JsonWriteBoolean('partIdLocked', LibComp.PartIdLocked, True);
    JsonWriteBoolean('pinsMoveable', LibComp.PinsMoveable, True);

    // Unique identifiers

    // Export child primitives
    JsonOpenArray('primitives');
    Iterator := LibComp.SchIterator_Create;
    Iterator.AddFilter_ObjectSet(MkSet(ePin, eLine, eRectangle, eArc, ePolygon,
        ePolyline, eLabel, eEllipse, eRoundRectangle, eTextFrame, eBezier,
        eEllipticalArc, ePie, eImage));

    Prim := Iterator.FirstSchObject;
    while Prim <> nil do
    begin
        case Prim.ObjectId of
            ePin: ExportSchPinToJson(Prim, True);
            eLine: ExportSchLineToJson(Prim, True);
            eRectangle: ExportSchRectangleToJson(Prim, True);
            eArc: ExportSchArcToJson(Prim, True);
            ePolygon: ExportSchPolygonToJson(Prim, True);
            ePolyline: ExportSchPolylineToJson(Prim, True);
            eLabel: ExportSchLabelToJson(Prim, True);
            eEllipse: ExportSchEllipseToJson(Prim, True);
            eRoundRectangle: ExportSchRoundRectToJson(Prim, True);
            eTextFrame: ExportSchTextFrameToJson(Prim, True);
            eBezier: ExportSchBezierToJson(Prim, True);
            eEllipticalArc: ExportSchEllipticalArcToJson(Prim, True);
            ePie: ExportSchPieToJson(Prim, True);
            eImage: ExportSchImageToJson(Prim, True);
        end;
        Prim := Iterator.NextSchObject;
    end;
    LibComp.SchIterator_Destroy(Iterator);
    JsonCloseArray(False);

    // Additional ISch_Component properties
    JsonWriteString('aliasAsText', LibComp.AliasAsText, True);
    JsonWriteInteger('aliasCount', LibComp.AliasCount, True);
    JsonWriteString('configurationName', LibComp.ConfigurationName, True);
    JsonWriteBoolean('displayFieldName', LibComp.DisplayFieldName, True);
    JsonWriteString('genericComponentTemplateGUID', LibComp.GenericComponentTemplateGUID, True);
    JsonWriteBoolean('isUserConfigurable', LibComp.IsUserConfigurable, True);
    JsonWriteString('lineHighlightValue', LibComp.LineHighlightValue, True);
    JsonWriteString('revisionDetails', LibComp.RevisionDetails, True);
    JsonWriteBoolean('selectedInLibrary', LibComp.SelectedInLibrary, True);
    JsonWriteString('sheetPartFileName', LibComp.SheetPartFileName, True);
    JsonWriteString('symbolVaultGUID', LibComp.SymbolVaultGUID, True);
    JsonWriteString('targetFileName', LibComp.TargetFileName, True);
    JsonWriteBoolean('useDBTableName', LibComp.UseDBTableName, True);
    JsonWriteBoolean('useLibraryName', LibComp.UseLibraryName, True);
    // Base graphical object properties
    ExportSchBaseProperties(LibComp);

    try
        if LibComp.ChooseFromLibrary <> nil then
            JsonWriteString('chooseFromLibrary_ref', 'present', False)
        else
            JsonWriteString('chooseFromLibrary_ref', '', False);
    except
        JsonWriteString('chooseFromLibrary_ref', 'ERROR', False);
    end;

    JsonCloseObject(AddComma);
end;

procedure ExportSchFunctionalBlockToJson(FuncBlock: ISch_FunctionalBlock; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'FunctionalBlock', True);

    // Position and size
    JsonWriteInteger('x', FuncBlock.Location.X, True);
    JsonWriteInteger('y', FuncBlock.Location.Y, True);
    JsonWriteInteger('xSize', FuncBlock.XSize, True);
    JsonWriteInteger('ySize', FuncBlock.YSize, True);

    // Visual properties
    JsonWriteInteger('lineWidth', Ord(FuncBlock.LineWidth), True);
    JsonWriteInteger('color', FuncBlock.Color, True);
    JsonWriteInteger('areaColor', FuncBlock.AreaColor, True);
    JsonWriteBoolean('isSolid', FuncBlock.IsSolid, True);

    // Text properties
    JsonWriteString('text', FuncBlock.Text, True);
    JsonWriteInteger('fontID', FuncBlock.FontID, True);
    JsonWriteInteger('textColor', FuncBlock.TextColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_FunctionalBlock properties
    JsonWriteInteger('corner', FuncBlock.Corner, True);
    try
        JsonWriteString('i_ObjectAddress', 'OBJECT_REF', True);
    except
        JsonWriteString('i_ObjectAddress', 'ERROR', True);
    end;
    JsonWriteInteger('lineStyle', FuncBlock.LineStyle, True);
    JsonWriteString('name', FuncBlock.Name, True);
    JsonWriteString('schematicFileName', FuncBlock.SchematicFileName, True);
    JsonWriteBoolean('transparent', FuncBlock.Transparent, True);
    // Base graphical object properties
    ExportSchBaseProperties(FuncBlock);


    JsonCloseObject(AddComma);
end;

procedure ExportSchFunctionalTextFrameToJson(TextFrame: ISch_FunctionalTextFrame; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'FunctionalTextFrame', True);

    // Position and size
    JsonWriteInteger('x', TextFrame.Location.X, True);
    JsonWriteInteger('y', TextFrame.Location.Y, True);
    JsonWriteInteger('xSize', TextFrame.XSize, True);
    JsonWriteInteger('ySize', TextFrame.YSize, True);

    // Text properties
    JsonWriteString('text', TextFrame.Text, True);
    JsonWriteInteger('fontID', TextFrame.FontID, True);
    JsonWriteInteger('alignment', Ord(TextFrame.Alignment), True);
    JsonWriteBoolean('wordWrap', TextFrame.WordWrap, True);

    // Visual properties
    JsonWriteInteger('textColor', TextFrame.TextColor, True);
    JsonWriteInteger('color', TextFrame.Color, True);
    JsonWriteInteger('areaColor', TextFrame.AreaColor, True);
    JsonWriteBoolean('showBorder', TextFrame.ShowBorder, True);
    JsonWriteBoolean('isSolid', TextFrame.IsSolid, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_FunctionalTextFrame properties
    JsonWriteBoolean('clipToRect', TextFrame.ClipToRect, True);
    JsonWriteInteger('corner', TextFrame.Corner, True);
    try
        JsonWriteString('i_ObjectAddress', 'OBJECT_REF', True);
    except
        JsonWriteString('i_ObjectAddress', 'ERROR', True);
    end;
    JsonWriteInteger('lineStyle', TextFrame.LineStyle, True);
    JsonWriteInteger('lineWidth', TextFrame.LineWidth, True);
    JsonWriteCoord('textMargin', TextFrame.TextMargin, True);
    JsonWriteBoolean('transparent', TextFrame.Transparent, True);
    // Base graphical object properties
    ExportSchBaseProperties(TextFrame);


    JsonCloseObject(AddComma);
end;

procedure ExportSchCollapsiblePolygonToJson(Polygon: ISch_CollapsiblePolygon; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CollapsiblePolygon', True);

    // Display properties
    JsonWriteBoolean('isCollapsed', Polygon.Collapsed, True);

    // Visual properties
    JsonWriteInteger('lineWidth', Ord(Polygon.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Polygon.LineStyle), True);
    JsonWriteBoolean('isSolid', Polygon.IsSolid, True);
    JsonWriteBoolean('transparent', Polygon.Transparent, True);
    JsonWriteInteger('color', Polygon.Color, True);
    JsonWriteInteger('areaColor', Polygon.AreaColor, True);

    // Vertices
    JsonWriteInteger('vertexCount', Polygon.VerticesCount, True);
    JsonOpenArray('vertices');
    for I := 0 to Polygon.VerticesCount - 1 do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', Polygon.Vertex[I].X, True);
        JsonWriteInteger('y', Polygon.Vertex[I].Y, False);
        JsonCloseObject(I < Polygon.VerticesCount - 1);
    end;
    JsonCloseArray(True);

    // State flags

    // Unique identifiers

    // Additional ISch_CollapsiblePolygon properties
    JsonWriteInteger('location', Polygon.Location, True);
    // Base graphical object properties
    ExportSchBaseProperties(Polygon);



    try
        if Polygon.BoundingRectangle <> nil then
            JsonWriteString('boundingRectangle_ref', 'present', False)
        else
            JsonWriteString('boundingRectangle_ref', '', False);
    except
        JsonWriteString('boundingRectangle_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchCollapsibleRectangleToJson(Rect: ISch_CollapsibleRectangle; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CollapsibleRectangle', True);

    // Position
    JsonWriteInteger('x1', Rect.Location.X, True);
    JsonWriteInteger('y1', Rect.Location.Y, True);
    JsonWriteInteger('x2', Rect.Corner.X, True);
    JsonWriteInteger('y2', Rect.Corner.Y, True);

    // Display properties
    JsonWriteBoolean('isCollapsed', Rect.Collapsed, True);

    // Visual properties
    JsonWriteInteger('lineWidth', Ord(Rect.LineWidth), True);
    JsonWriteBoolean('isSolid', Rect.IsSolid, True);
    JsonWriteBoolean('transparent', Rect.Transparent, True);
    JsonWriteInteger('color', Rect.Color, True);
    JsonWriteInteger('areaColor', Rect.AreaColor, True);

    // State flags

    // Unique identifiers

    // Additional ISch_CollapsibleRectangle properties
    JsonWriteInteger('lineStyle', Rect.LineStyle, True);
    // Base graphical object properties
    ExportSchBaseProperties(Rect);


    JsonCloseObject(AddComma);
end;

procedure ExportSchImageParameterToJson(ImgParam: ISch_ImageParameter; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ImageParameter', True);

    // Position
    JsonWriteInteger('x', ImgParam.Location.X, True);
    JsonWriteInteger('y', ImgParam.Location.Y, True);

    // Image properties
    JsonWriteString('name', ImgParam.Name, True);
    JsonWriteString('text', ImgParam.Text, True);
    JsonWriteBoolean('embedImage', ImgParam.EmbedImage, True);

    // Visual properties
    JsonWriteInteger('color', ImgParam.Color, True);
    JsonWriteInteger('areaColor', ImgParam.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_ImageParameter properties
    JsonWriteBoolean('allowDatabaseSynchronize', ImgParam.AllowDatabaseSynchronize, True);
    JsonWriteBoolean('allowLibrarySynchronize', ImgParam.AllowLibrarySynchronize, True);
    JsonWriteBoolean('autoposition', ImgParam.Autoposition, True);
    JsonWriteString('calculatedValueString', ImgParam.CalculatedValueString, True);
    JsonWriteString('description', ImgParam.Description, True);
    JsonWriteString('displayString', ImgParam.DisplayString, True);
    JsonWriteInteger('fontID', ImgParam.FontID, True);
    JsonWriteString('formula', ImgParam.Formula, True);
    JsonWriteBoolean('isConfigurable', ImgParam.IsConfigurable, True);
    JsonWriteBoolean('isHidden', ImgParam.IsHidden, True);
    JsonWriteBoolean('isMirrored', ImgParam.IsMirrored, True);
    JsonWriteBoolean('isRule', ImgParam.IsRule, True);
    JsonWriteBoolean('isSystemParameter', ImgParam.IsSystemParameter, True);
    JsonWriteInteger('justification', ImgParam.Justification, True);
    JsonWriteBoolean('nameIsReadOnly', ImgParam.NameIsReadOnly, True);
    JsonWriteInteger('orientation', ImgParam.Orientation, True);
    JsonWriteString('overrideDisplayString', ImgParam.OverrideDisplayString, True);
    JsonWriteInteger('paramType', ImgParam.ParamType, True);
    JsonWriteInteger('readOnlyState', ImgParam.ReadOnlyState, True);
    JsonWriteBoolean('showName', ImgParam.ShowName, True);
    JsonWriteInteger('textHorzAnchor', ImgParam.TextHorzAnchor, True);
    JsonWriteInteger('textVertAnchor', ImgParam.TextVertAnchor, True);
    JsonWriteBoolean('valueIsReadOnly', ImgParam.ValueIsReadOnly, True);
    try
        JsonWriteString('variantOption', 'OBJECT_REF', True);
    except
        JsonWriteString('variantOption', 'ERROR', True);
    end;
    // Base graphical object properties
    ExportSchBaseProperties(ImgParam);


    JsonCloseObject(AddComma);
end;

procedure ExportSchSchematicBlockToJson(Block: ISch_SchematicBlock; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchematicBlock', True);

    // Position and size
    JsonWriteInteger('x', Block.Location.X, True);
    JsonWriteInteger('y', Block.Location.Y, True);
    JsonWriteInteger('xSize', Block.XSize, True);
    JsonWriteInteger('ySize', Block.YSize, True);

    // Block properties
    JsonWriteString('fileName', Block.FileName, True);
    JsonWriteBoolean('showHiddenFields', Block.ShowHiddenFields, True);

    // Visual properties
    JsonWriteInteger('lineWidth', Ord(Block.LineWidth), True);
    JsonWriteInteger('color', Block.Color, True);
    JsonWriteInteger('areaColor', Block.AreaColor, True);
    JsonWriteBoolean('isSolid', Block.IsSolid, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Additional ISch_SchematicBlock properties
    JsonWriteString('blockDescription', Block.BlockDescription, True);
    JsonWriteInteger('corner', Block.Corner, True);
    JsonWriteString('designItemId', Block.DesignItemId, True);
    JsonWriteString('itemGUID', Block.ItemGUID, True);
    JsonWriteInteger('lineStyle', Block.LineStyle, True);
    JsonWriteInteger('orientation', Block.Orientation, True);
    JsonWriteString('revisionGUID', Block.RevisionGUID, True);
    JsonWriteBoolean('transparent', Block.Transparent, True);
    JsonWriteString('vaultGUID', Block.VaultGUID, True);
    // Base graphical object properties
    ExportSchBaseProperties(Block);


    JsonCloseObject(AddComma);
end;

procedure ExportSchCompileMaskToJson(Mask: ISch_CompileMask; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CompileMask', True);

    // Position
    JsonWriteInteger('x1', Mask.Location.X, True);
    JsonWriteInteger('y1', Mask.Location.Y, True);
    JsonWriteInteger('x2', Mask.Corner.X, True);
    JsonWriteInteger('y2', Mask.Corner.Y, True);

    // Display properties
    JsonWriteBoolean('collapsed', Mask.Collapsed, True);

    // Visual properties
    JsonWriteInteger('lineWidth', Ord(Mask.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Mask.LineStyle), True);
    JsonWriteBoolean('isSolid', Mask.IsSolid, True);
    JsonWriteBoolean('transparent', Mask.Transparent, True);
    JsonWriteInteger('color', Mask.Color, True);
    JsonWriteInteger('areaColor', Mask.AreaColor, True);

    // State flags

    // Unique identifiers

    // Additional ISch_CompileMask properties
    try
        JsonWriteString('copyTo', 'OBJECT_REF', True);
    except
        JsonWriteString('copyTo', 'ERROR', True);
    end;
    try
        JsonWriteString('i_ObjectAddress', 'OBJECT_REF', True);
    except
        JsonWriteString('i_ObjectAddress', 'ERROR', True);
    end;
    // Base graphical object properties
    ExportSchBaseProperties(Mask);

    JsonCloseObject(AddComma);
end;

procedure ExportSchPortToJson(Port: ISch_Port; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Port', True);

    // Position
    JsonWriteInteger('x', Port.Location.X, True);
    JsonWriteInteger('y', Port.Location.Y, True);

    // Port properties
    JsonWriteString('name', Port.Name, True);
    JsonWriteInteger('ioType', Ord(Port.IOType), True);
    JsonWriteInteger('style', Ord(Port.Style), True);
    JsonWriteInteger('alignment', Ord(Port.Alignment), True);

    // Size
    JsonWriteInteger('width', Port.Width, True);
    JsonWriteInteger('height', Port.Height, True);
    JsonWriteInteger('borderWidth', Ord(Port.BorderWidth), True);
    JsonWriteBoolean('autoSize', Port.AutoSize, True);

    // Connection properties
    JsonWriteInteger('connectedEnd', Ord(Port.ConnectedEnd), True);
    JsonWriteString('crossReference', Port.CrossReference, True);
    JsonWriteBoolean('showNetName', Port.ShowNetName, True);

    // Harness properties
    JsonWriteString('harnessType', Port.HarnessType, True);
    JsonWriteInteger('harnessColor', Port.HarnessColor, True);
    JsonWriteBoolean('isCustomStyle', Port.IsCustomStyle, True);

    // Visual properties
    JsonWriteInteger('fontID', Port.FontID, True);
    JsonWriteInteger('color', Port.Color, True);
    JsonWriteInteger('areaColor', Port.AreaColor, True);
    JsonWriteInteger('textColor', Port.TextColor, True);

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Port);


    JsonCloseObject(AddComma);
end;

procedure ExportSchPowerObjectToJson(PowerObj: ISch_PowerObject; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'PowerObject', True);

    // Position
    JsonWriteInteger('x', PowerObj.Location.X, True);
    JsonWriteInteger('y', PowerObj.Location.Y, True);
    JsonWriteInteger('orientation', Ord(PowerObj.Orientation), True);
    JsonWriteBoolean('isMirrored', PowerObj.IsMirrored, True);

    // Power object properties
    JsonWriteString('text', PowerObj.Text, True);
    JsonWriteInteger('style', Ord(PowerObj.Style), True);
    JsonWriteBoolean('showNetName', PowerObj.ShowNetName, True);

    // Visual properties
    JsonWriteInteger('fontID', PowerObj.FontID, True);
    JsonWriteInteger('color', PowerObj.Color, True);

    // Additional ISch_PowerObject properties
    JsonWriteString('calculatedValueString', PowerObj.CalculatedValueString, True);
    JsonWriteString('displayString', PowerObj.DisplayString, True);
    JsonWriteString('formula', PowerObj.Formula, True);
    JsonWriteBoolean('isCustomStyle', PowerObj.IsCustomStyle, True);
    JsonWriteInteger('justification', PowerObj.Justification, True);
    JsonWriteString('overrideDisplayString', PowerObj.OverrideDisplayString, True);

    // Base graphical object properties
    ExportSchBaseProperties(PowerObj);

    JsonCloseObject(AddComma);
end;

procedure ExportSchNetLabelToJson(NetLabel: ISch_NetLabel; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'NetLabel', True);

    // Position
    JsonWriteInteger('x', NetLabel.Location.X, True);
    JsonWriteInteger('y', NetLabel.Location.Y, True);
    JsonWriteInteger('orientation', Ord(NetLabel.Orientation), True);
    JsonWriteBoolean('isMirrored', NetLabel.IsMirrored, True);

    // Net label properties
    JsonWriteString('text', NetLabel.Text, True);
    JsonWriteInteger('justification', Ord(NetLabel.Justification), True);

    // Visual properties
    JsonWriteInteger('fontID', NetLabel.FontID, True);
    JsonWriteInteger('color', NetLabel.Color, True);

    // Additional ISch_NetLabel properties
    JsonWriteString('calculatedValueString', NetLabel.CalculatedValueString, True);
    JsonWriteString('displayString', NetLabel.DisplayString, True);
    JsonWriteString('formula', NetLabel.Formula, True);
    JsonWriteString('overrideDisplayString', NetLabel.OverrideDisplayString, True);

    // Base graphical object properties
    ExportSchBaseProperties(NetLabel);

    JsonCloseObject(AddComma);
end;

procedure ExportSchBusToJson(Bus: ISch_Bus; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Bus', True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(Bus.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Bus.LineStyle), True);
    JsonWriteInteger('color', Bus.Color, True);
    JsonWriteInteger('areaColor', Bus.AreaColor, True);
    JsonWriteInteger('underlineColor', Bus.UnderlineColor, True);

    // State properties
    JsonWriteBoolean('isSolid', Bus.IsSolid, True);
    JsonWriteBoolean('transparent', Bus.Transparent, True);
    JsonWriteBoolean('autoWire', Bus.AutoWire, True);

    // Vertices
    JsonWriteInteger('vertexCount', Bus.VerticesCount, True);
    JsonOpenArray('vertices');
    for I := 1 to Bus.VerticesCount do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', Bus.Vertex[I].X, True);
        JsonWriteInteger('y', Bus.Vertex[I].Y, False);
        JsonCloseObject(I < Bus.VerticesCount);
    end;
    JsonCloseArray(True);

    // Additional ISch_Bus properties
    JsonWriteBoolean('editingEndPoint', Bus.EditingEndPoint, True);

    // Base graphical object properties
    ExportSchBaseProperties(Bus);

    JsonCloseObject(AddComma);
end;

procedure ExportSchBusEntryToJson(BusEntry: ISch_BusEntry; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BusEntry', True);

    // Position
    JsonWriteInteger('x', BusEntry.Location.X, True);
    JsonWriteInteger('y', BusEntry.Location.Y, True);
    JsonWriteInteger('cornerX', BusEntry.Corner.X, True);
    JsonWriteInteger('cornerY', BusEntry.Corner.Y, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(BusEntry.LineWidth), True);
    JsonWriteInteger('color', BusEntry.Color, True);

    // State flags

    // Unique identifiers

    // Additional ISch_BusEntry properties
    try
        JsonWriteString('i_ObjectAddress', 'OBJECT_REF', True);
    except
        JsonWriteString('i_ObjectAddress', 'ERROR', True);
    end;
    // Base graphical object properties
    ExportSchBaseProperties(BusEntry);


    JsonCloseObject(AddComma);
end;

procedure ExportSchJunctionToJson(Junction: ISch_Junction; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Junction', True);

    // Position
    JsonWriteInteger('x', Junction.Location.X, True);
    JsonWriteInteger('y', Junction.Location.Y, True);

    // Junction properties
    JsonWriteInteger('size', Ord(Junction.Size), True);
    JsonWriteInteger('color', Junction.Color, True);
    JsonWriteBoolean('manualJunction', Junction.ManualJunction, True);
    JsonWriteBoolean('locked', Junction.Locked, True);

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Junction);


    JsonCloseObject(AddComma);
end;

procedure ExportSchNoErcToJson(NoErc: ISch_NoERC; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'NoERC', True);

    // Position
    JsonWriteInteger('x', NoErc.Location.X, True);
    JsonWriteInteger('y', NoErc.Location.Y, True);
    JsonWriteInteger('orientation', Ord(NoErc.Orientation), True);

    // Visual properties
    JsonWriteInteger('color', NoErc.Color, True);
    JsonWriteBoolean('isActive', NoErc.IsActive, True);
    JsonWriteInteger('symbol', Ord(NoErc.Symbol), True);

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(NoErc);


    JsonCloseObject(AddComma);
end;

procedure ExportSchCrossSheetConnectorToJson(Connector: ISch_CrossSheetConnector; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CrossSheetConnector', True);

    // Position
    JsonWriteInteger('x', Connector.Location.X, True);
    JsonWriteInteger('y', Connector.Location.Y, True);
    JsonWriteInteger('orientation', Ord(Connector.Orientation), True);

    // Connector properties
    JsonWriteString('text', Connector.Text, True);
    JsonWriteString('crossReference', Connector.CrossReference, True);
    JsonWriteInteger('style', Ord(Connector.Style), True);

    // Visual properties
    JsonWriteInteger('fontID', Connector.FontID, True);
    JsonWriteInteger('color', Connector.Color, True);
    JsonWriteInteger('textColor', Connector.TextColor, True);

    // State flags

    // Unique identifiers

    // Additional ISch_CrossSheetConnector properties
    try
        JsonWriteString('i_ObjectAddress', 'OBJECT_REF', True);
    except
        JsonWriteString('i_ObjectAddress', 'ERROR', True);
    end;
    JsonWriteString('overrideDisplayString', Connector.OverrideDisplayString, True);

    // Additional ISch_CrossSheetConnector properties
    JsonWriteString('calculatedValueString', Connector.CalculatedValueString, True);
    JsonWriteInteger('crossSheetStyle', Connector.CrossSheetStyle, True);
    JsonWriteString('displayString', Connector.DisplayString, True);
    JsonWriteString('formula', Connector.Formula, True);
    JsonWriteBoolean('isCustomStyle', Connector.IsCustomStyle, True);
    JsonWriteBoolean('isMirrored', Connector.IsMirrored, True);
    JsonWriteInteger('justification', Connector.Justification, True);
    JsonWriteBoolean('showNetName', Connector.ShowNetName, True);

    // Base graphical object properties
    ExportSchBaseProperties(Connector);

    JsonCloseObject(AddComma);
end;

procedure ExportSchSheetSymbolToJson(SheetSymbol: ISch_SheetSymbol; AddComma: Boolean);
var
    Iterator: ISch_Iterator;
    Entry: ISch_SheetEntry;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SheetSymbol', True);

    // Position and size
    JsonWriteInteger('x', SheetSymbol.Location.X, True);
    JsonWriteInteger('y', SheetSymbol.Location.Y, True);
    JsonWriteInteger('xSize', SheetSymbol.XSize, True);
    JsonWriteInteger('ySize', SheetSymbol.YSize, True);
    JsonWriteBoolean('isMirrored', SheetSymbol.IsMirrored, True);

    // Sheet symbol properties
    JsonWriteString('fileName', SheetSymbol.FileName, True);
    JsonWriteString('sheetName', SheetSymbol.SheetName, True);

    // Visual properties
    JsonWriteInteger('lineWidth', Ord(SheetSymbol.LineWidth), True);
    JsonWriteInteger('color', SheetSymbol.Color, True);
    JsonWriteInteger('areaColor', SheetSymbol.AreaColor, True);
    JsonWriteBoolean('isSolid', SheetSymbol.IsSolid, True);

    // State flags

    // Export sheet entries
    JsonOpenArray('sheetEntries');
    Iterator := SheetSymbol.SchIterator_Create;
    Iterator.AddFilter_ObjectSet(MkSet(eSheetEntry));
    Entry := Iterator.FirstSchObject;
    while Entry <> nil do
    begin
        ExportSchSheetEntryToJson(Entry, True);
        Entry := Iterator.NextSchObject;
    end;
    SheetSymbol.SchIterator_Destroy(Iterator);
    JsonCloseArray(True);

    // Additional ISch_SheetSymbol properties
    JsonWriteString('designItemId', SheetSymbol.DesignItemId, True);
    JsonWriteString('itemGUID', SheetSymbol.ItemGUID, True);
    JsonWriteInteger('libIdentifierKind', SheetSymbol.LibIdentifierKind, True);
    JsonWriteString('libraryIdentifier', SheetSymbol.LibraryIdentifier, True);
    JsonWriteString('revisionGUID', SheetSymbol.RevisionGUID, True);
    JsonWriteBoolean('showHiddenFields', SheetSymbol.ShowHiddenFields, True);
    JsonWriteString('sourceLibraryName', SheetSymbol.SourceLibraryName, True);
    JsonWriteInteger('symbolType', SheetSymbol.SymbolType, True);
    JsonWriteString('vaultGUID', SheetSymbol.VaultGUID, True);

    // Base graphical object properties
    ExportSchBaseProperties(SheetSymbol);

    JsonCloseObject(AddComma);
end;

procedure ExportSchSheetEntryToJson(Entry: ISch_SheetEntry; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SheetEntry', True);

    // Position
    JsonWriteInteger('side', Ord(Entry.Side), True);
    JsonWriteInteger('distanceFromTop', Entry.DistanceFromTop, True);

    // Entry properties
    JsonWriteString('name', Entry.Name, True);
    JsonWriteInteger('ioType', Ord(Entry.IOType), True);
    JsonWriteInteger('style', Ord(Entry.Style), True);
    JsonWriteInteger('arrowKind', Ord(Entry.ArrowKind), True);

    // Harness properties
    JsonWriteString('harnessType', Entry.HarnessType, True);

    // Visual properties
    JsonWriteInteger('fontID', Entry.FontID, True);
    JsonWriteInteger('color', Entry.Color, True);
    JsonWriteInteger('areaColor', Entry.AreaColor, True);
    JsonWriteInteger('textColor', Entry.TextColor, True);
    JsonWriteInteger('textStyle', Ord(Entry.TextStyle), True);

    // State flags

    // Unique identifiers

    // Additional ISch_SheetEntry properties
    JsonWriteBoolean('isVertical', Entry.IsVertical, True);
    // Base graphical object properties
    ExportSchBaseProperties(Entry);


    JsonCloseObject(AddComma);
end;

procedure ExportSchParameterToJson(Param: ISch_Parameter; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Parameter', True);

    // Position
    JsonWriteInteger('x', Param.Location.X, True);
    JsonWriteInteger('y', Param.Location.Y, True);
    JsonWriteInteger('orientation', Ord(Param.Orientation), True);

    // Parameter properties
    JsonWriteString('name', Param.Name, True);
    JsonWriteString('text', Param.Text, True);
    JsonWriteBoolean('isHidden', Param.IsHidden, True);
    JsonWriteBoolean('showName', Param.ShowName, True);
    JsonWriteBoolean('readOnlyState', Param.ReadOnlyState, True);

    // Visual properties
    JsonWriteInteger('fontID', Param.FontID, True);
    JsonWriteInteger('justification', Ord(Param.Justification), True);
    JsonWriteInteger('color', Param.Color, True);

    // State flags

    // Unique identifiers

    // Additional ISch_Parameter properties
    JsonWriteBoolean('allowDatabaseSynchronize', Param.AllowDatabaseSynchronize, True);
    JsonWriteBoolean('allowLibrarySynchronize', Param.AllowLibrarySynchronize, True);
    JsonWriteBoolean('nameIsReadOnly', Param.NameIsReadOnly, True);
    JsonWriteBoolean('valueIsReadOnly', Param.ValueIsReadOnly, True);
    JsonWriteBoolean('autoposition', Param.Autoposition, True);
    JsonWriteString('calculatedValueString', Param.CalculatedValueString, True);
    JsonWriteString('description', Param.Description, True);
    JsonWriteString('displayString', Param.DisplayString, True);
    JsonWriteString('formula', Param.Formula, True);
    JsonWriteBoolean('isConfigurable', Param.IsConfigurable, True);
    JsonWriteBoolean('isMirrored', Param.IsMirrored, True);
    JsonWriteBoolean('isRule', Param.IsRule, True);
    JsonWriteBoolean('isSystemParameter', Param.IsSystemParameter, True);
    JsonWriteString('overrideDisplayString', Param.OverrideDisplayString, True);
    JsonWriteInteger('paramType', Param.ParamType, True);
    JsonWriteInteger('textHorzAnchor', Param.TextHorzAnchor, True);
    JsonWriteInteger('textVertAnchor', Param.TextVertAnchor, True);

    // Base graphical object properties
    ExportSchBaseProperties(Param);

    JsonCloseObject(AddComma);
end;

procedure ExportSchParameterSetToJson(ParamSet: ISch_ParameterSet; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ParameterSet', True);

    // Position
    JsonWriteInteger('x', ParamSet.Location.X, True);
    JsonWriteInteger('y', ParamSet.Location.Y, True);

    // Parameter set properties
    JsonWriteString('name', ParamSet.Name, True);
    JsonWriteInteger('style', Ord(ParamSet.Style), True);
    JsonWriteBoolean('showHiddenFields', ParamSet.ShowHiddenFields, True);

    // Visual properties
    JsonWriteInteger('color', ParamSet.Color, True);
    JsonWriteInteger('areaColor', ParamSet.AreaColor, True);
    JsonWriteInteger('borderWidth', Ord(ParamSet.BorderWidth), True);
    JsonWriteBoolean('isSolid', ParamSet.IsSolid, True);

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(ParamSet);


    JsonCloseObject(AddComma);
end;

procedure ExportSchBlanketToJson(Blanket: ISch_Blanket; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Blanket', True);

    // Display properties
    JsonWriteBoolean('isCollapsed', Blanket.Collapsed, True);
    // Import_FromUser is an interactive method (opens dialog), not a property - skip

    // Visual properties
    JsonWriteInteger('lineWidth', Ord(Blanket.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Blanket.LineStyle), True);
    JsonWriteBoolean('isSolid', Blanket.IsSolid, True);
    JsonWriteBoolean('transparent', Blanket.Transparent, True);
    JsonWriteInteger('color', Blanket.Color, True);
    JsonWriteInteger('areaColor', Blanket.AreaColor, True);

    // Vertices
    JsonWriteInteger('vertexCount', Blanket.VerticesCount, True);
    JsonOpenArray('vertices');
    for I := 1 to Blanket.VerticesCount do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', Blanket.Vertex[I].X, True);
        JsonWriteInteger('y', Blanket.Vertex[I].Y, False);
        JsonCloseObject(I < Blanket.VerticesCount);
    end;
    JsonCloseArray(True);

    // Base graphical object properties
    ExportSchBaseProperties(Blanket);

    JsonCloseObject(AddComma);
end;

procedure ExportSchDirectiveToJson(Directive: ISch_Directive; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Directive', True);

    // Position
    JsonWriteInteger('x', Directive.Location.X, True);
    JsonWriteInteger('y', Directive.Location.Y, True);

    // Directive properties
    JsonWriteString('text', Directive.Text, True);
    JsonWriteString('name', Directive.Name, True);

    // Vertices
    JsonWriteInteger('vertexCount', Directive.VerticesCount, True);
    JsonOpenArray('vertices');
    for I := 1 to Directive.VerticesCount do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', Directive.Vertex[I].X, True);
        JsonWriteInteger('y', Directive.Vertex[I].Y, False);
        JsonCloseObject(I < Directive.VerticesCount);
    end;
    JsonCloseArray(True);

    // Visual properties
    JsonWriteInteger('fontID', Directive.FontID, True);
    JsonWriteInteger('color', Directive.Color, True);
    JsonWriteInteger('areaColor', Directive.AreaColor, True);

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Directive);


    JsonCloseObject(AddComma);
end;

procedure ExportSchNoteToJson(Note: ISch_Note; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Note', True);

    // Position and size
    JsonWriteInteger('x', Note.Location.X, True);
    JsonWriteInteger('y', Note.Location.Y, True);
    JsonWriteInteger('cornerX', Note.Corner.X, True);
    JsonWriteInteger('cornerY', Note.Corner.Y, True);

    // Note properties
    JsonWriteString('text', Note.Text, True);
    JsonWriteString('author', Note.Author, True);

    // Visual properties
    JsonWriteInteger('fontID', Note.FontID, True);
    JsonWriteInteger('color', Note.Color, True);
    JsonWriteInteger('areaColor', Note.AreaColor, True);
    JsonWriteInteger('textColor', Note.TextColor, True);
    JsonWriteBoolean('showBorder', Note.ShowBorder, True);
    JsonWriteBoolean('isSolid', Note.IsSolid, True);

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Note);


    JsonCloseObject(AddComma);
end;

procedure ExportSchHyperlinkToJson(Hyperlink: ISch_Hyperlink; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Hyperlink', True);

    // Position
    JsonWriteInteger('x', Hyperlink.Location.X, True);
    JsonWriteInteger('y', Hyperlink.Location.Y, True);

    // Hyperlink properties
    JsonWriteString('url', Hyperlink.URL, True);
    JsonWriteString('text', Hyperlink.Text, True);
    JsonWriteBoolean('showAsText', Hyperlink.ShowAsText, True);

    // Visual properties
    JsonWriteInteger('color', Hyperlink.Color, True);

    // State flags

    // Unique identifiers

    // Additional ISch_Hyperlink properties
    JsonWriteString('displayString', Hyperlink.DisplayString, True);
    JsonWriteString('overrideDisplayString', Hyperlink.OverrideDisplayString, True);
    // Base graphical object properties
    ExportSchBaseProperties(Hyperlink);


    JsonCloseObject(AddComma);
end;

procedure ExportSchProbeToJson(Probe: ISch_Probe; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Probe', True);

    // Position
    JsonWriteInteger('x', Probe.Location.X, True);
    JsonWriteInteger('y', Probe.Location.Y, True);

    // Probe properties
    JsonWriteString('netName', Probe.NetName, True);
    JsonWriteInteger('probeNumber', Probe.ProbeNumber, True);

    // Visual properties
    JsonWriteInteger('color', Probe.Color, True);

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Probe);


    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessConnectorToJson(Connector: ISch_HarnessConnector; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessConnector', True);

    // Position
    JsonWriteInteger('x', Connector.Location.X, True);
    JsonWriteInteger('y', Connector.Location.Y, True);
    JsonWriteInteger('orientation', Ord(Connector.Orientation), True);
    JsonWriteBoolean('isMirrored', Connector.IsMirrored, True);

    // Size
    JsonWriteInteger('xSize', Connector.XSize, True);
    JsonWriteInteger('ySize', Connector.YSize, True);

    // Visual properties
    JsonWriteInteger('lineWidth', Ord(Connector.LineWidth), True);
    JsonWriteInteger('color', Connector.Color, True);
    JsonWriteInteger('areaColor', Connector.AreaColor, True);
    JsonWriteBoolean('isSolid', Connector.IsSolid, True);

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Connector);



    try
        if HC.HarnessConnectorType <> nil then
            JsonWriteString('harnessConnectorType_ref', 'present', False)
        else
            JsonWriteString('harnessConnectorType_ref', '', False);
    except
        JsonWriteString('harnessConnectorType_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessConnectorTypeToJson(ConnType: ISch_HarnessConnectorType; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessConnectorType', True);

    // Position
    JsonWriteInteger('x', ConnType.Location.X, True);
    JsonWriteInteger('y', ConnType.Location.Y, True);
    JsonWriteInteger('orientation', Ord(ConnType.Orientation), True);
    JsonWriteBoolean('isMirrored', ConnType.IsMirrored, True);

    // Properties
    JsonWriteString('text', ConnType.Text, True);
    JsonWriteInteger('fontID', ConnType.FontID, True);

    // Visual properties
    JsonWriteInteger('color', ConnType.Color, True);
    JsonWriteInteger('areaColor', ConnType.AreaColor, True);

    // State flags

    // Unique identifiers

    // Additional ISch_HarnessConnectorType properties
    JsonWriteBoolean('autoposition', ConnType.Autoposition, True);
    JsonWriteString('calculatedValueString', ConnType.CalculatedValueString, True);
    JsonWriteString('displayString', ConnType.DisplayString, True);
    JsonWriteString('formula', ConnType.Formula, True);
    JsonWriteBoolean('isHidden', ConnType.IsHidden, True);
    JsonWriteInteger('justification', ConnType.Justification, True);
    JsonWriteString('overrideDisplayString', ConnType.OverrideDisplayString, True);
    JsonWriteInteger('textHorzAnchor', ConnType.TextHorzAnchor, True);
    JsonWriteInteger('textVertAnchor', ConnType.TextVertAnchor, True);
    // Base graphical object properties
    ExportSchBaseProperties(ConnType);


    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessComponentToJson(HarnessComp: ISch_HarnessComponent; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessComponent', True);

    // Position
    JsonWriteInteger('x', HarnessComp.Location.X, True);
    JsonWriteInteger('y', HarnessComp.Location.Y, True);
    JsonWriteInteger('orientation', Ord(HarnessComp.Orientation), True);
    JsonWriteBoolean('isMirrored', HarnessComp.IsMirrored, True);

    // Component properties
    JsonWriteString('designator', HarnessComp.Designator.Text, True);
    JsonWriteString('comment', HarnessComp.Comment.Text, True);

    // Visual properties
    JsonWriteInteger('color', HarnessComp.Color, True);
    JsonWriteInteger('areaColor', HarnessComp.AreaColor, True);

    // State flags

    // Unique identifiers

    // Additional ISch_HarnessComponent properties
    JsonWriteString('aliasAsText', HarnessComp.AliasAsText, True);
    JsonWriteInteger('aliasCount', HarnessComp.AliasCount, True);
    JsonWriteString('componentDescription', HarnessComp.ComponentDescription, True);
    JsonWriteInteger('componentKind', HarnessComp.ComponentKind, True);
    JsonWriteString('configurationParameters', HarnessComp.ConfigurationParameters, True);
    JsonWriteString('configuratorName', HarnessComp.ConfiguratorName, True);
    JsonWriteInteger('currentPartID', HarnessComp.CurrentPartID, True);
    JsonWriteString('databaseLibraryName', HarnessComp.DatabaseLibraryName, True);
    JsonWriteString('databaseTableName', HarnessComp.DatabaseTableName, True);
    JsonWriteBoolean('designatorLocked', HarnessComp.DesignatorLocked, True);
    JsonWriteString('designItemId', HarnessComp.DesignItemId, True);
    JsonWriteBoolean('displayFieldNames', HarnessComp.DisplayFieldNames, True);
    JsonWriteInteger('displayMode', HarnessComp.DisplayMode, True);
    JsonWriteInteger('displayModeCount', HarnessComp.DisplayModeCount, True);
    JsonWriteBoolean('hasConfiguredPinMapping', HarnessComp.HasConfiguredPinMapping, True);
    JsonWriteBoolean('isUnmanaged', HarnessComp.IsUnmanaged, True);
    JsonWriteBoolean('isUserConfigurable', HarnessComp.IsUserConfigurable, True);
    JsonWriteString('itemsGUID', HarnessComp.ItemsGUID, True);
    JsonWriteInteger('libIdentifierKind', HarnessComp.LibIdentifierKind, True);
    JsonWriteString('libraryIdentifier', HarnessComp.LibraryIdentifier, True);
    JsonWriteString('libraryPath', HarnessComp.LibraryPath, True);
    JsonWriteString('libReference', HarnessComp.LibReference, True);
    JsonWriteBoolean('overrideColors', HarnessComp.OverrideColors, True);
    JsonWriteInteger('partCount', HarnessComp.PartCount, True);
    JsonWriteBoolean('partIdLocked', HarnessComp.PartIdLocked, True);
    JsonWriteInteger('pinColor', HarnessComp.PinColor, True);
    JsonWriteBoolean('pinsMoveable', HarnessComp.PinsMoveable, True);
    JsonWriteString('revisionDetails', HarnessComp.RevisionDetails, True);
    JsonWriteString('revisionGUID', HarnessComp.RevisionGUID, True);
    JsonWriteString('revisionHRID', HarnessComp.RevisionHRID, True);
    JsonWriteString('revisionState', HarnessComp.RevisionState, True);
    JsonWriteString('revisionStatus', HarnessComp.RevisionStatus, True);
    JsonWriteBoolean('selectedInLibrary', HarnessComp.SelectedInLibrary, True);
    JsonWriteString('sheetPartFileName', HarnessComp.SheetPartFileName, True);
    JsonWriteBoolean('showHiddenFields', HarnessComp.ShowHiddenFields, True);
    JsonWriteBoolean('showHiddenPins', HarnessComp.ShowHiddenPins, True);
    JsonWriteString('sourceLibraryName', HarnessComp.SourceLibraryName, True);
    JsonWriteString('symbolItemGUID', HarnessComp.SymbolItemGUID, True);
    JsonWriteString('symbolReference', HarnessComp.SymbolReference, True);
    JsonWriteString('symbolRevisionGUID', HarnessComp.SymbolRevisionGUID, True);
    JsonWriteString('symbolVaultGUID', HarnessComp.SymbolVaultGUID, True);
    JsonWriteString('targetFileName', HarnessComp.TargetFileName, True);
    JsonWriteBoolean('useDBTableName', HarnessComp.UseDBTableName, True);
    JsonWriteBoolean('useLibraryName', HarnessComp.UseLibraryName, True);
    try
        JsonWriteString('variantOption', 'OBJECT_REF', True);
    except
        JsonWriteString('variantOption', 'ERROR', True);
    end;
    JsonWriteString('vaultGUID', HarnessComp.VaultGUID, True);
    // Base graphical object properties
    ExportSchBaseProperties(HarnessComp);


    JsonCloseObject(AddComma);
end;

procedure ExportSchReuseSheetSymbolToJson(ReuseSymbol: ISch_ReuseSheetSymbol; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ReuseSheetSymbol', True);

    // Position and size
    JsonWriteInteger('x', ReuseSymbol.Location.X, True);
    JsonWriteInteger('y', ReuseSymbol.Location.Y, True);
    JsonWriteInteger('xSize', ReuseSymbol.XSize, True);
    JsonWriteInteger('ySize', ReuseSymbol.YSize, True);
    JsonWriteBoolean('isMirrored', ReuseSymbol.IsMirrored, True);

    // Reuse properties
    JsonWriteString('fileName', ReuseSymbol.FileName, True);
    JsonWriteString('sheetName', ReuseSymbol.SheetName, True);
    JsonWriteString('reuseBlockName', ReuseSymbol.ReuseBlockName, True);

    // Visual properties
    JsonWriteInteger('lineWidth', Ord(ReuseSymbol.LineWidth), True);
    JsonWriteInteger('color', ReuseSymbol.Color, True);
    JsonWriteInteger('areaColor', ReuseSymbol.AreaColor, True);
    JsonWriteBoolean('isSolid', ReuseSymbol.IsSolid, True);

    // State flags

    // Unique identifiers

    // Additional ISch_ReuseSheetSymbol properties
    JsonWriteString('designItemId', ReuseSymbol.DesignItemId, True);
    JsonWriteString('itemGUID', ReuseSymbol.ItemGUID, True);
    JsonWriteInteger('libIdentifierKind', ReuseSymbol.LibIdentifierKind, True);
    JsonWriteString('libraryIdentifier', ReuseSymbol.LibraryIdentifier, True);
    JsonWriteString('revisionGUID', ReuseSymbol.RevisionGUID, True);
    JsonWriteBoolean('showHiddenFields', ReuseSymbol.ShowHiddenFields, True);
    JsonWriteString('sourceLibraryName', ReuseSymbol.SourceLibraryName, True);
    JsonWriteInteger('symbolType', ReuseSymbol.SymbolType, True);
    JsonWriteString('vaultGUID', ReuseSymbol.VaultGUID, True);
    // Base graphical object properties
    ExportSchBaseProperties(ReuseSymbol);



    try
        if ReuseSymbol.SheetFileName <> nil then
            JsonWriteString('sheetFileName_ref', 'present', False)
        else
            JsonWriteString('sheetFileName_ref', '', False);
    except
        JsonWriteString('sheetFileName_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchTemplateToJson(Template: ISch_Template; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Template', True);

    // Position
    JsonWriteInteger('x', Template.Location.X, True);
    JsonWriteInteger('y', Template.Location.Y, True);

    // Template properties
    JsonWriteString('fileName', Template.FileName, True);

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Template);


    JsonCloseObject(AddComma);
end;

procedure ExportSchDocumentToJson(Doc: ISch_Document; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchDocument', True);

    // Document properties
    JsonWriteString('documentName', Doc.DocumentName, True);
    JsonWriteInteger('displayUnit', Doc.DisplayUnit, True);
    JsonWriteInteger('unitSystem', Doc.UnitSystem, True);

    // Sheet properties
    JsonWriteInteger('sheetStyle', Ord(Doc.SheetStyle), True);
    JsonWriteInteger('sheetSizeX', Doc.SheetSizeX, True);
    JsonWriteInteger('sheetSizeY', Doc.SheetSizeY, True);
    JsonWriteInteger('sheetZonesX', Doc.SheetZonesX, True);
    JsonWriteInteger('sheetZonesY', Doc.SheetZonesY, True);
    JsonWriteInteger('sheetMarginWidth', Doc.SheetMarginWidth, True);
    JsonWriteInteger('sheetNumberSpaceSize', Doc.SheetNumberSpaceSize, True);
    JsonWriteBoolean('useCustomSheet', Doc.UseCustomSheet, True);
    JsonWriteInteger('customX', Doc.CustomX, True);
    JsonWriteInteger('customY', Doc.CustomY, True);

    // Grid properties
    JsonWriteBoolean('visibleGridOn', Doc.VisibleGridOn, True);
    JsonWriteInteger('visibleGridSize', Doc.VisibleGridSize, True);
    JsonWriteBoolean('snapGridOn', Doc.SnapGridOn, True);
    JsonWriteInteger('snapGridSize', Doc.SnapGridSize, True);
    JsonWriteBoolean('hotspotGridOn', Doc.HotspotGridOn, True);
    JsonWriteInteger('hotspotGridSize', Doc.HotspotGridSize, True);

    // Display options
    JsonWriteBoolean('borderOn', Doc.BorderOn, True);
    JsonWriteBoolean('titleBlockOn', Doc.TitleBlockOn, True);
    JsonWriteBoolean('referenceZonesOn', Doc.ReferenceZonesOn, True);
    JsonWriteBoolean('showTemplateGraphics', Doc.ShowTemplateGraphics, True);

    // Template
    JsonWriteString('templateFileName', Doc.TemplateFileName, True);
    JsonWriteString('templateVaultGUID', Doc.TemplateVaultGUID, True);
    JsonWriteString('templateItemGUID', Doc.TemplateItemGUID, True);
    JsonWriteString('templateRevisionGUID', Doc.TemplateRevisionGUID, True);

    // Font
    JsonWriteInteger('systemFont', Doc.SystemFont, True);

    // Other
    JsonWriteInteger('workspaceOrientation', Ord(Doc.WorkspaceOrientation), True);
    JsonWriteBoolean('isLibrary', Doc.IsLibrary, True);

    // Colors
    JsonWriteInteger('color', Doc.Color, True);
    JsonWriteInteger('areaColor', Doc.AreaColor, True);

    // Unique identifier

    // Additional ISch_Document properties
    JsonWriteCoord('customMarginWidth', Doc.CustomMarginWidth, True);
    JsonWriteString('customSheetStyle', Doc.CustomSheetStyle, True);
    JsonWriteCoord('customXZones', Doc.CustomXZones, True);
    JsonWriteCoord('customYZones', Doc.CustomYZones, True);
    JsonWriteInteger('documentBorderStyle', Doc.DocumentBorderStyle, True);
    JsonWriteString('exportToParameters', Doc.ExportToParameters, True);
    JsonWriteCoord('internalTolerance', Doc.InternalTolerance, True);
    JsonWriteString('itemRevisionGUID', Doc.ItemRevisionGUID, True);
    JsonWriteString('loadFormat', Doc.LoadFormat, True);
    JsonWriteInteger('location', Doc.Location, True);
    JsonWriteInteger('minorVersion', Doc.MinorVersion, True);
    JsonWriteString('propsRevisionGUID', Doc.PropsRevisionGUID, True);
    JsonWriteString('propsVaultGUID', Doc.PropsVaultGUID, True);
    JsonWriteInteger('referenceZoneStyle', Doc.ReferenceZoneStyle, True);
    JsonWriteString('releaseItemGUID', Doc.ReleaseItemGUID, True);
    JsonWriteString('releaseVaultGUID', Doc.ReleaseVaultGUID, True);
    JsonWriteInteger('schDocID', Doc.SchDocID, True);
    JsonWriteString('templateRevisGUID', Doc.TemplateRevisGUID, True);
    JsonWriteString('templateRevisionHRID', Doc.TemplateRevisionHRID, True);
    JsonWriteString('templateVaultHRID', Doc.TemplateVaultHRID, True);
    // Base graphical object properties
    ExportSchBaseProperties(Doc);



    try
        if Doc.Graphical_VirtualRectangle <> nil then
            JsonWriteString('graphical_VirtualRectangle_ref', 'present', False)
        else
            JsonWriteString('graphical_VirtualRectangle_ref', '', False);
    except
        JsonWriteString('graphical_VirtualRectangle_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchComponentToJson(Comp: ISch_Component; AddComma: Boolean);
var
    Iterator: ISch_Iterator;
    Prim: ISch_GraphicalObject;
    ImplIterator: ISch_Iterator;
    Impl: ISch_Implementation;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Component', True);

    // Basic identification
    JsonWriteString('designator', Comp.Designator.Text, True);
    JsonWriteString('comment', Comp.Comment.Text, True);
    JsonWriteString('libReference', Comp.LibReference, True);
    JsonWriteString('description', Comp.ComponentDescription, True);

    // Library reference
    JsonWriteString('libraryPath', Comp.LibraryPath, True);
    JsonWriteString('libraryIdentifier', Comp.LibraryIdentifier, True);
    JsonWriteString('sourceLibraryName', Comp.SourceLibraryName, True);
    JsonWriteInteger('libIdentifierKind', Ord(Comp.LibIdentifierKind), True);

    // Database properties
    JsonWriteString('databaseLibraryName', Comp.DatabaseLibraryName, True);
    JsonWriteString('databaseTableName', Comp.DatabaseTableName, True);
    JsonWriteString('designItemId', Comp.DesignItemId, True);

    // Vault/Managed component properties
    JsonWriteString('vaultGUID', Comp.VaultGUID, True);
    JsonWriteString('vaultHRID', Comp.VaultHRID, True);
    JsonWriteString('itemGUID', Comp.ItemGUID, True);
    JsonWriteString('revisionGUID', Comp.RevisionGUID, True);
    JsonWriteString('revisionHRID', Comp.RevisionHRID, True);
    JsonWriteString('revisionState', Comp.RevisionState, True);
    JsonWriteString('revisionStatus', Comp.RevisionStatus, True);
    JsonWriteString('symbolReference', Comp.SymbolReference, True);
    JsonWriteString('symbolItemsGUID', Comp.SymbolItemGUID, True);
    JsonWriteString('symbolRevisionGUID', Comp.SymbolRevisionGUID, True);

    // Location and orientation
    JsonWriteInteger('x', Comp.Location.X, True);
    JsonWriteInteger('y', Comp.Location.Y, True);
    JsonWriteInteger('orientation', Ord(Comp.Orientation), True);
    JsonWriteBoolean('isMirrored', Comp.IsMirrored, True);

    // Display properties
    JsonWriteInteger('displayMode', Ord(Comp.DisplayMode), True);
    JsonWriteInteger('displayModeCount', Comp.DisplayModeCount, True);
    JsonWriteInteger('partCount', Comp.PartCount, True);
    JsonWriteInteger('currentPartId', Comp.GetState_CurrentPartID, True);
    JsonWriteBoolean('isMultiPart', Comp.IsMultiPartComponent, True);
    JsonWriteBoolean('showHiddenFields', Comp.ShowHiddenFields, True);
    JsonWriteBoolean('showHiddenPins', Comp.ShowHiddenPins, True);

    // Visual properties
    JsonWriteBoolean('overrideColors', Comp.OverideColors, True);
    JsonWriteInteger('pinColor', Comp.PinColor, True);
    JsonWriteInteger('color', Comp.Color, True);
    JsonWriteInteger('areaColor', Comp.AreaColor, True);

    // Configuration
    JsonWriteInteger('componentKind', Ord(Comp.ComponentKind), True);
    JsonWriteString('configurationParameters', Comp.ConfigurationParameters, True);
    JsonWriteString('configuratorName', Comp.ConfiguratorName, True);
    JsonWriteInteger('variantOption', Ord(Comp.VariantOption), True);

    // State flags
    JsonWriteBoolean('designatorLocked', Comp.DesignatorLocked, True);
    JsonWriteBoolean('partIdLocked', Comp.PartIdLocked, True);
    JsonWriteBoolean('pinsMoveable', Comp.PinsMoveable, True);
    JsonWriteBoolean('inLibrary', Comp.InLibrary, True);
    JsonWriteBoolean('inSheet', Comp.InSheet, True);
    JsonWriteBoolean('isUnmanaged', Comp.IsUnmanaged, True);

    // Unique identifiers

    JsonOpenArray('primitives');

    Iterator := Comp.SchIterator_Create;
    Iterator.AddFilter_ObjectSet(MkSet(ePin, eLine, eRectangle, eArc, ePolygon,
        ePolyline, eLabel, eEllipse, eRoundRectangle, eTextFrame, eImage,
        eBezier, eEllipticalArc, ePie));

    Prim := Iterator.FirstSchObject;
    while Prim <> nil do
    begin
        case Prim.ObjectId of
            ePin: ExportSchPinToJson(Prim, True);
            eLine: ExportSchLineToJson(Prim, True);
            eRectangle: ExportSchRectangleToJson(Prim, True);
            eArc: ExportSchArcToJson(Prim, True);
            ePolygon: ExportSchPolygonToJson(Prim, True);
            ePolyline: ExportSchPolylineToJson(Prim, True);
            eLabel: ExportSchLabelToJson(Prim, True);
            eEllipse: ExportSchEllipseToJson(Prim, True);
            eRoundRectangle: ExportSchRoundRectToJson(Prim, True);
            eTextFrame: ExportSchTextFrameToJson(Prim, True);
            eImage: ExportSchImageToJson(Prim, True);
            eBezier: ExportSchBezierToJson(Prim, True);
            eEllipticalArc: ExportSchEllipticalArcToJson(Prim, True);
            ePie: ExportSchPieToJson(Prim, True);
        end;
        Prim := Iterator.NextSchObject;
    end;

    Comp.SchIterator_Destroy(Iterator);
    JsonCloseArray(True);

    // Export implementations (footprints, simulation models, etc.)
    JsonOpenArray('implementations');
    ImplIterator := Comp.SchIterator_Create;
    ImplIterator.AddFilter_ObjectSet(MkSet(eImplementation));

    Impl := ImplIterator.FirstSchObject;
    while Impl <> nil do
    begin
        JsonOpenObject('');
        JsonWriteString('modelName', Impl.ModelName, True);
        JsonWriteString('modelType', Impl.ModelType, True);
        JsonWriteString('description', Impl.Description, True);
        JsonWriteBoolean('isCurrent', Impl.IsCurrent, False);
        Impl := ImplIterator.NextSchObject;
        JsonCloseObject(Impl <> nil);
    end;

    Comp.SchIterator_Destroy(ImplIterator);
    JsonCloseArray(False);
    // Additional ISch_Component properties
    JsonWriteString('aliasAsText', Comp.AliasAsText, True);
    JsonWriteInteger('aliasCount', Comp.AliasCount, True);
    JsonWriteString('configuratorName', Comp.ConfiguratorName, True);
    JsonWriteBoolean('displayFieldNames', Comp.DisplayFieldNames, True);
    JsonWriteString('genericComponentTemplateGUID', Comp.GenericComponentTemplateGUID, True);
    JsonWriteBoolean('isUserConfigurable', Comp.IsUserConfigurable, True);
    JsonWriteString('revisionDetails', Comp.RevisionDetails, True);
    JsonWriteBoolean('selectedInLibrary', Comp.SelectedInLibrary, True);
    JsonWriteString('sheetPartFileName', Comp.SheetPartFileName, True);
    JsonWriteString('symbolVaultGUID', Comp.SymbolVaultGUID, True);
    JsonWriteString('targetFileName', Comp.TargetFileName, True);
    JsonWriteBoolean('useDBTableName', Comp.UseDBTableName, True);
    JsonWriteBoolean('useLibraryName', Comp.UseLibraryName, True);

    // Base graphical object properties
    ExportSchBaseProperties(Comp);

    try
        JsonWriteString('alias_0', Comp.Alias[0], False);
    except
        JsonWriteString('alias_0', 'ERROR', False);
    end;

    JsonCloseObject(AddComma);
end;

procedure ExportSchLibToJson(SchLib: ISch_Lib; JsonPath: String);
var
    LibIterator: ISch_Iterator;
    Comp: ISch_Component;
begin
    if SchLib = nil then Exit;

    JsonBegin;
    JsonOpenObject('');

    JsonOpenObject('metadata');
    JsonWriteString('exportType', 'SchLib', True);
    JsonWriteString('fileName', ExtractFileName(SchLib.DocumentName), True);
    JsonWriteString('exportedBy', 'AltiumSharp FileToJsonConverter', True);
    JsonWriteString('version', '1.0', True);

    // Object reference properties
    try
        if SchLib.CurrentSchComponent <> nil then
            JsonWriteString('currentSchComponent_ref', 'present', True)
        else
            JsonWriteString('currentSchComponent_ref', '', True);
    except
        JsonWriteString('currentSchComponent_ref', 'ERROR', True);
    end;
    try
        if SchLib.Graphical_VirtualRectangle <> nil then
            JsonWriteString('graphical_VirtualRectangle_ref', 'present', True)
        else
            JsonWriteString('graphical_VirtualRectangle_ref', '', True);
    except
        JsonWriteString('graphical_VirtualRectangle_ref', 'ERROR', True);
    end;
    try
        if SchLib.I_ObjectAddress <> nil then
            JsonWriteString('i_ObjectAddress_ref', 'present', False)
        else
            JsonWriteString('i_ObjectAddress_ref', '', False);
    except
        JsonWriteString('i_ObjectAddress_ref', 'ERROR', False);
    end;
    JsonCloseObject(True);

    // ISch_Lib properties
    JsonOpenObject('libraryProperties');
    JsonWriteString('documentName', SchLib.DocumentName, True);
    JsonWriteString('description', SchLib.Description, True);
    JsonWriteBoolean('isLibrary', SchLib.IsLibrary, True);
    JsonWriteBoolean('alwaysShowCD', SchLib.AlwaysShowCD, True);
    JsonWriteInteger('areaColor', SchLib.AreaColor, True);
    JsonWriteBoolean('borderOn', SchLib.BorderOn, True);
    JsonWriteInteger('color', SchLib.Color, True);
    JsonWriteBoolean('compilationMasked', SchLib.CompilationMasked, True);
    JsonWriteCoord('customMarginWidth', SchLib.CustomMarginWidth, True);
    JsonWriteInteger('customSheetStyle', SchLib.CustomSheetStyle, True);
    JsonWriteCoord('customX', SchLib.CustomX, True);
    JsonWriteCoord('customXZones', SchLib.CustomXZones, True);
    JsonWriteCoord('customY', SchLib.CustomY, True);
    JsonWriteCoord('customYZones', SchLib.CustomYZones, True);
    JsonWriteBoolean('dimmed', SchLib.Dimmed, True);
    JsonWriteBoolean('disabled', SchLib.Disabled, True);
    JsonWriteBoolean('displayError', SchLib.DisplayError, True);
    JsonWriteInteger('displayUnit', SchLib.DisplayUnit, True);
    JsonWriteInteger('documentBorderStyle', SchLib.DocumentBorderStyle, True);
    JsonWriteBoolean('enableDraw', SchLib.EnableDraw, True);
    JsonWriteInteger('errorColor', SchLib.ErrorColor, True);
    JsonWriteInteger('errorKind', SchLib.ErrorKind, True);
    JsonWriteString('errorString', SchLib.ErrorString, True);
    JsonWriteString('folderGUID', SchLib.FolderGUID, True);
    JsonWriteBoolean('graphicallyLocked', SchLib.GraphicallyLocked, True);
    JsonWriteString('handle', SchLib.Handle, True);
    JsonWriteBoolean('hotspotGridOn', SchLib.HotspotGridOn, True);
    JsonWriteCoord('hotspotGridSize', SchLib.HotspotGridSize, True);
    JsonWriteCoord('internalTolerance', SchLib.InternalTolerance, True);
    JsonWriteBoolean('isSimpleDesignMode', SchLib.IsSimpleDesignMode, True);
    JsonWriteBoolean('isSingleComponentMode', SchLib.IsSingleComponentMode, True);
    JsonWriteString('itemRevisionGUID', SchLib.ItemRevisionGUID, True);
    JsonWriteString('lifeCycleDefinitionGUID', SchLib.LifeCycleDefinitionGUID, True);
    JsonWriteString('liveHighlightValue', SchLib.LiveHighlightValue, True);
    JsonWriteString('loadFormat', SchLib.LoadFormat, True);
    JsonWriteInteger('minorVersion', SchLib.MinorVersion, True);
    JsonWriteInteger('objectId', SchLib.ObjectId, True);
    JsonWriteInteger('ownerPartDisplayMode', SchLib.OwnerPartDisplayMode, True);
    JsonWriteInteger('ownerPartId', SchLib.OwnerPartId, True);
    JsonWriteString('propsRevisionGUID', SchLib.PropsRevisionGUID, True);
    JsonWriteString('propsVaultGUID', SchLib.PropsVaultGUID, True);
    JsonWriteBoolean('referenceZonesOn', SchLib.ReferenceZonesOn, True);
    JsonWriteInteger('referenceZoneStyle', SchLib.ReferenceZoneStyle, True);
    JsonWriteString('releaseVaultGUID', SchLib.ReleaseVaultGUID, True);
    JsonWriteString('revisionNamingSchemeGUID', SchLib.RevisionNamingSchemeGUID, True);
    JsonWriteInteger('schDocID', SchLib.SchDocID, True);
    JsonWriteBoolean('selection', SchLib.Selection, True);
    JsonWriteCoord('sheetMarginWidth', SchLib.SheetMarginWidth, True);
    JsonWriteInteger('sheetNumberSpaceSize', SchLib.SheetNumberSpaceSize, True);
    JsonWriteCoord('sheetSizeX', SchLib.SheetSizeX, True);
    JsonWriteCoord('sheetSizeY', SchLib.SheetSizeY, True);
    JsonWriteInteger('sheetStyle', SchLib.SheetStyle, True);
    JsonWriteInteger('sheetZonesX', SchLib.SheetZonesX, True);
    JsonWriteInteger('sheetZonesY', SchLib.SheetZonesY, True);
    JsonWriteBoolean('showHiddenPins', SchLib.ShowHiddenPins, True);
    JsonWriteBoolean('showTemplateGraphics', SchLib.ShowTemplateGraphics, True);
    JsonWriteBoolean('snapGridOn', SchLib.SnapGridOn, True);
    JsonWriteCoord('snapGridSize', SchLib.SnapGridSize, True);
    JsonWriteInteger('systemFont', SchLib.SystemFont, True);
    JsonWriteString('templateFileName', SchLib.TemplateFileName, True);
    JsonWriteString('templateItemGUID', SchLib.TemplateItemGUID, True);
    JsonWriteString('templateRevisionGUID', SchLib.TemplateRevisionGUID, True);
    JsonWriteString('templateRevisionHRID', SchLib.TemplateRevisionHRID, True);
    JsonWriteString('templateVaultGUID', SchLib.TemplateVaultGUID, True);
    JsonWriteString('templateVaultHRID', SchLib.TemplateVaultHRID, True);
    JsonWriteBoolean('titleBlockOn', SchLib.TitleBlockOn, True);
    JsonWriteString('uniqueId', SchLib.UniqueId, True);
    JsonWriteInteger('unitSystem', SchLib.UnitSystem, True);
    JsonWriteBoolean('useCustomSheet', SchLib.UseCustomSheet, True);
    JsonWriteBoolean('visibleGridOn', SchLib.VisibleGridOn, True);
    JsonWriteCoord('visibleGridSize', SchLib.VisibleGridSize, True);
    JsonWriteInteger('workspaceOrientation', SchLib.WorkspaceOrientation, True);
    try
        JsonWriteInteger('location_X', SchLib.Location.X, True);
        JsonWriteInteger('location_Y', SchLib.Location.Y, True);
    except
        JsonWriteString('location', 'ERROR: Could not read Location', True);
    end;
    try
        if SchLib.OwnerDocument <> nil then
            JsonWriteString('ownerDocument_ref', SchLib.OwnerDocument.DocumentName, True)
        else
            JsonWriteString('ownerDocument_ref', '', True);
    except
        JsonWriteString('ownerDocument_ref', 'ERROR', True);
    end;
    try
        if SchLib.Container <> nil then
            JsonWriteString('container_ref', SchLib.Container.UniqueId, False)
        else
            JsonWriteString('container_ref', '', False);
    except
        JsonWriteString('container_ref', 'ERROR', False);
    end;
    JsonCloseObject(True);

    JsonOpenArray('symbols');

    LibIterator := SchLib.SchLibIterator_Create;
    LibIterator.AddFilter_ObjectSet(MkSet(eSchComponent));

    Comp := LibIterator.FirstSchObject;
    while Comp <> nil do
    begin
        ExportSchComponentToJson(Comp, True);
        Comp := LibIterator.NextSchObject;
    end;

    SchLib.SchIterator_Destroy(LibIterator);

    JsonCloseArray(False);
    JsonCloseObject(False);

    JsonEnd(JsonPath);
end;

{==============================================================================
  SCHEMATIC DOCUMENT JSON EXPORT
==============================================================================}

procedure ExportSchWireToJson(Wire: ISch_Wire; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Wire', True);

    // Position
    JsonWriteInteger('x', Wire.Location.X, True);
    JsonWriteInteger('y', Wire.Location.Y, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(Wire.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Wire.LineStyle), True);

    // Visual properties
    JsonWriteInteger('color', Wire.Color, True);
    JsonWriteInteger('areaColor', Wire.AreaColor, True);
    JsonWriteInteger('underlineColor', Wire.UnderlineColor, True);

    // State properties
    JsonWriteBoolean('isSolid', Wire.IsSolid, True);
    JsonWriteBoolean('transparent', Wire.Transparent, True);
    JsonWriteBoolean('autoWire', Wire.AutoWire, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Vertices
    JsonWriteInteger('vertexCount', Wire.VerticesCount, True);

    JsonOpenArray('vertices');
    for I := 0 to Wire.VerticesCount - 1 do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', Wire.Vertex[I].X, True);
        JsonWriteInteger('y', Wire.Vertex[I].Y, False);
        JsonCloseObject(I < Wire.VerticesCount - 1);
    end;
    JsonCloseArray(False);

    // Additional ISch_Wire properties
    JsonWriteBoolean('editingEndPoint', Wire.EditingEndPoint, True);
    // Base graphical object properties
    ExportSchBaseProperties(Wire);



    try
        JsonWriteBoolean('compilationMaskedSegment_0', Wire.CompilationMaskedSegment[0], False);
    except
        JsonWriteString('compilationMaskedSegment_0', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchBusToJson(Bus: ISch_Bus; AddComma: Boolean);
var
    Ii: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Bus', True);

    // Position
    JsonWriteInteger('x', Bus.Location.X, True);
    JsonWriteInteger('y', Bus.Location.Y, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(Bus.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Bus.LineStyle), True);

    // Visual properties
    JsonWriteInteger('color', Bus.Color, True);
    JsonWriteInteger('areaColor', Bus.AreaColor, True);
    JsonWriteInteger('underlineColor', Bus.UnderlineColor, True);

    // State properties
    JsonWriteBoolean('isSolid', Bus.IsSolid, True);
    JsonWriteBoolean('transparent', Bus.Transparent, True);
    JsonWriteBoolean('autoWire', Bus.AutoWire, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Vertices
    JsonWriteInteger('vertexCount', Bus.VerticesCount, True);

    JsonOpenArray('vertices');
    for Ii := 0 to Bus.VerticesCount - 1 do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', Bus.Vertex[Ii].X, True);
        JsonWriteInteger('y', Bus.Vertex[Ii].Y, False);
        JsonCloseObject(Ii < Bus.VerticesCount - 1);
    end;
    JsonCloseArray(False);
    // Base graphical object properties
    ExportSchBaseProperties(Bus);



    try
        JsonWriteBoolean('compilationMaskedSegment_0', Bus.CompilationMaskedSegment[0], False);
    except
        JsonWriteString('compilationMaskedSegment_0', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchJunctionToJson(Junction: ISch_Junction; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Junction', True);

    // Position
    JsonWriteInteger('x', Junction.Location.X, True);
    JsonWriteInteger('y', Junction.Location.Y, True);

    // Size
    JsonWriteInteger('size', Ord(Junction.Size), True);

    // Visual properties
    JsonWriteInteger('color', Junction.Color, True);
    JsonWriteInteger('areaColor', Junction.AreaColor, True);

    // State properties
    JsonWriteBoolean('locked', Junction.Locked, True);

    // Owner properties

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Junction);


    JsonCloseObject(AddComma);
end;

procedure ExportSchNetLabelToJson(NetLabel: ISch_NetLabel; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'NetLabel', True);

    // Text properties
    JsonWriteString('text', NetLabel.Text, True);
    JsonWriteString('formula', NetLabel.Formula, True);
    JsonWriteString('calculatedValueString', NetLabel.CalculatedValueString, True);
    JsonWriteString('displayString', NetLabel.DisplayString, True);
    JsonWriteString('overrideDisplayString', NetLabel.OverrideDisplayString, True);

    // Position and orientation
    JsonWriteInteger('x', NetLabel.Location.X, True);
    JsonWriteInteger('y', NetLabel.Location.Y, True);
    JsonWriteInteger('orientation', Ord(NetLabel.Orientation), True);

    // Font and justification
    JsonWriteInteger('fontID', NetLabel.FontID, True);
    JsonWriteInteger('justification', Ord(NetLabel.Justification), True);
    JsonWriteBoolean('isMirrored', NetLabel.IsMirrored, True);

    // Visual properties
    JsonWriteInteger('color', NetLabel.Color, True);
    JsonWriteInteger('areaColor', NetLabel.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(NetLabel);


    JsonCloseObject(AddComma);
end;

procedure ExportSchPortToJson(Port: ISch_Port; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Port', True);

    // Basic properties
    JsonWriteString('name', Port.Name, True);
    JsonWriteString('overrideDisplayString', Port.OverrideDisplayString, True);

    // Position and size
    JsonWriteInteger('x', Port.Location.X, True);
    JsonWriteInteger('y', Port.Location.Y, True);
    JsonWriteInteger('width', Port.Width, True);
    JsonWriteInteger('height', Port.Height, True);

    // Style properties
    JsonWriteInteger('style', Ord(Port.Style), True);
    JsonWriteInteger('iOType', Ord(Port.IOType), True);
    JsonWriteInteger('alignment', Ord(Port.Alignment), True);
    JsonWriteInteger('connectedEnd', Ord(Port.ConnectedEnd), True);
    JsonWriteBoolean('autoSize', Port.AutoSize, True);
    JsonWriteInteger('borderWidth', Ord(Port.BorderWidth), True);
    JsonWriteInteger('fontID', Port.FontID, True);
    JsonWriteBoolean('isCustomStyle', Port.IsCustomStyle, True);
    JsonWriteBoolean('showNetName', Port.ShowNetName, True);

    // Harness properties
    JsonWriteString('harnessType', Port.HarnessType, True);
    JsonWriteInteger('harnessColor', Port.HarnessColor, True);

    // Cross reference
    JsonWriteString('crossReference', Port.CrossReference, True);

    // Visual properties
    JsonWriteInteger('color', Port.Color, True);
    JsonWriteInteger('areaColor', Port.AreaColor, True);
    JsonWriteInteger('textColor', Port.TextColor, True);

    // Owner properties

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Port);


    JsonCloseObject(AddComma);
end;

procedure ExportSchPowerPortToJson(PowerPort: ISch_PowerObject; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'PowerPort', True);

    // Text properties
    JsonWriteString('text', PowerPort.Text, True);
    JsonWriteString('formula', PowerPort.Formula, True);
    JsonWriteString('calculatedValueString', PowerPort.CalculatedValueString, True);
    JsonWriteString('displayString', PowerPort.DisplayString, True);
    JsonWriteString('overrideDisplayString', PowerPort.OverrideDisplayString, True);

    // Position and orientation
    JsonWriteInteger('x', PowerPort.Location.X, True);
    JsonWriteInteger('y', PowerPort.Location.Y, True);
    JsonWriteInteger('orientation', Ord(PowerPort.Orientation), True);

    // Style properties
    JsonWriteInteger('style', Ord(PowerPort.Style), True);
    JsonWriteInteger('fontID', PowerPort.FontID, True);
    JsonWriteInteger('justification', Ord(PowerPort.Justification), True);
    JsonWriteBoolean('isMirrored', PowerPort.IsMirrored, True);
    JsonWriteBoolean('isCustomStyle', PowerPort.IsCustomStyle, True);
    JsonWriteBoolean('showNetName', PowerPort.ShowNetName, True);

    // Visual properties
    JsonWriteInteger('color', PowerPort.Color, True);
    JsonWriteInteger('areaColor', PowerPort.AreaColor, True);

    // Owner properties

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(PowerPort);


    JsonCloseObject(AddComma);
end;

procedure ExportSchNoErcMarkerToJson(NoErc: ISch_NoERC; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'NoERC', True);
    JsonWriteInteger('x', NoErc.Location.X, True);
    JsonWriteInteger('y', NoErc.Location.Y, True);
    JsonWriteInteger('color', NoErc.Color, False);
    // Base graphical object properties
    ExportSchBaseProperties(NoErc);

    JsonCloseObject(AddComma);
end;

procedure ExportSchSheetEntryToJson(Entry: ISch_SheetEntry; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SheetEntry', True);

    // Basic properties
    JsonWriteString('name', Entry.Name, True);
    JsonWriteString('overrideDisplayString', Entry.OverrideDisplayString, True);

    // Position
    JsonWriteInteger('x', Entry.Location.X, True);
    JsonWriteInteger('y', Entry.Location.Y, True);
    JsonWriteInteger('distanceFromTop', Entry.DistanceFromTop, True);
    JsonWriteInteger('side', Ord(Entry.Side), True);

    // Type properties
    JsonWriteInteger('iOType', Ord(Entry.IOType), True);
    JsonWriteInteger('style', Ord(Entry.Style), True);
    JsonWriteInteger('arrowKind', Entry.ArrowKind, True);

    // Visual properties
    JsonWriteInteger('color', Entry.Color, True);
    JsonWriteInteger('areaColor', Entry.AreaColor, True);
    JsonWriteInteger('textColor', Entry.TextColor, True);
    JsonWriteInteger('textFontID', Entry.TextFontID, True);
    JsonWriteInteger('textStyle', Entry.TextStyle, True);

    // Harness properties
    JsonWriteString('harnessType', Entry.HarnessType, True);
    JsonWriteInteger('harnessColor', Entry.HarnessColor, True);

    // Owner properties

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Entry);



    try
        if Entry.OwnerSheetSymbol <> nil then
            JsonWriteString('ownerSheetSymbol_ref', 'present', False)
        else
            JsonWriteString('ownerSheetSymbol_ref', '', False);
    except
        JsonWriteString('ownerSheetSymbol_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchSheetSymbolToJson(SheetSymbol: ISch_SheetSymbol; AddComma: Boolean);
var
    EntryIterator: ISch_Iterator;
    SheetEntry: ISch_SheetEntry;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SheetSymbol', True);

    // Basic properties
    JsonWriteString('sheetName', SheetSymbol.SheetName.Text, True);
    JsonWriteString('sheetFileName', SheetSymbol.SheetFileName.Text, True);

    // Position and size
    JsonWriteInteger('x', SheetSymbol.Location.X, True);
    JsonWriteInteger('y', SheetSymbol.Location.Y, True);
    JsonWriteInteger('xSize', SheetSymbol.XSize, True);
    JsonWriteInteger('ySize', SheetSymbol.YSize, True);

    // Visual properties
    JsonWriteInteger('lineWidth', Ord(SheetSymbol.LineWidth), True);
    JsonWriteInteger('color', SheetSymbol.Color, True);
    JsonWriteInteger('areaColor', SheetSymbol.AreaColor, True);
    JsonWriteBoolean('isSolid', SheetSymbol.IsSolid, True);
    JsonWriteBoolean('showHiddenFields', SheetSymbol.ShowHiddenFields, True);

    // Symbol type
    JsonWriteInteger('symbolType', Ord(SheetSymbol.SymbolType), True);

    // Library/Vault properties
    JsonWriteString('designItemId', SheetSymbol.DesignItemId, True);
    JsonWriteString('libraryIdentifier', SheetSymbol.LibraryIdentifier, True);
    JsonWriteInteger('libIdentifierKind', Ord(SheetSymbol.LibIdentifierKind), True);
    JsonWriteString('sourceLibraryName', SheetSymbol.SourceLibraryName, True);
    JsonWriteString('vaultGUID', SheetSymbol.VaultGUID, True);
    JsonWriteString('itemGUID', SheetSymbol.ItemGUID, True);
    JsonWriteString('revisionGUID', SheetSymbol.RevisionGUID, True);

    // Owner properties

    // State flags

    // Unique identifiers

    // Export sheet entries
    JsonOpenArray('entries');
    EntryIterator := SheetSymbol.SchIterator_Create;
    EntryIterator.AddFilter_ObjectSet(MkSet(eSheetEntry));

    SheetEntry := EntryIterator.FirstSchObject;
    while SheetEntry <> nil do
    begin
        ExportSchSheetEntryToJson(SheetEntry, True);
        SheetEntry := EntryIterator.NextSchObject;
    end;

    SheetSymbol.SchIterator_Destroy(EntryIterator);
    JsonCloseArray(False);
    // Base graphical object properties
    ExportSchBaseProperties(SheetSymbol);


    JsonCloseObject(AddComma);
end;

procedure ExportSchParameterToJson(Param: ISch_Parameter; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Parameter', True);

    // Basic properties
    JsonWriteString('name', Param.Name, True);
    JsonWriteString('text', Param.Text, True);
    JsonWriteString('description', Param.Description, True);

    // Formula and calculated values
    JsonWriteString('formula', Param.Formula, True);
    JsonWriteString('calculatedValueString', Param.CalculatedValueString, True);
    JsonWriteString('displayString', Param.DisplayString, True);
    JsonWriteString('overrideDisplayString', Param.OverrideDisplayString, True);

    // Position and orientation
    JsonWriteInteger('x', Param.Location.X, True);
    JsonWriteInteger('y', Param.Location.Y, True);
    JsonWriteInteger('orientation', Ord(Param.Orientation), True);
    JsonWriteBoolean('isMirrored', Param.IsMirrored, True);

    // Font and text properties
    JsonWriteInteger('fontID', Param.FontID, True);
    JsonWriteInteger('justification', Ord(Param.Justification), True);
    JsonWriteInteger('textHorzAnchor', Ord(Param.TextHorzAnchor), True);
    JsonWriteInteger('textVertAnchor', Ord(Param.TextVertAnchor), True);

    // Visual properties
    JsonWriteInteger('color', Param.Color, True);
    JsonWriteInteger('areaColor', Param.AreaColor, True);

    // Visibility and behavior
    JsonWriteBoolean('isHidden', Param.IsHidden, True);
    JsonWriteBoolean('showName', Param.ShowName, True);
    JsonWriteBoolean('autoposition', Param.Autoposition, True);

    // Type and state
    JsonWriteInteger('paramType', Ord(Param.ParamType), True);
    JsonWriteInteger('readOnlyState', Ord(Param.ReadOnlyState), True);
    JsonWriteBoolean('isConfigurable', Param.IsConfigurable, True);
    JsonWriteBoolean('isRule', Param.IsRule, True);
    JsonWriteBoolean('isSystemParameter', Param.IsSystemParameter, True);

    // Owner properties

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Param);


    JsonCloseObject(AddComma);
end;

procedure ExportSchParameterSetToJson(ParamSet: ISch_ParameterSet; AddComma: Boolean);
var
    ParamIterator: ISch_Iterator;
    Param: ISch_Parameter;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ParameterSet', True);

    // Position
    JsonWriteInteger('x', ParamSet.Location.X, True);
    JsonWriteInteger('y', ParamSet.Location.Y, True);
    JsonWriteInteger('orientation', Ord(ParamSet.Orientation), True);

    // Style properties
    JsonWriteInteger('style', ParamSet.Style, True);
    JsonWriteInteger('color', ParamSet.Color, True);
    JsonWriteInteger('areaColor', ParamSet.AreaColor, True);

    // State flags

    // Unique identifiers

    // Export contained parameters
    JsonOpenArray('parameters');
    ParamIterator := ParamSet.SchIterator_Create;
    ParamIterator.AddFilter_ObjectSet(MkSet(eParameter));

    Param := ParamIterator.FirstSchObject;
    while Param <> nil do
    begin
        ExportSchParameterToJson(Param, True);
        Param := ParamIterator.NextSchObject;
    end;

    ParamSet.SchIterator_Destroy(ParamIterator);
    JsonCloseArray(False);
    // Base graphical object properties
    ExportSchBaseProperties(ParamSet);


    JsonCloseObject(AddComma);
end;

procedure ExportSchCrossSheetConnectorToJson(CrossConn: ISch_CrossSheetConnector; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CrossSheetConnector', True);
    JsonWriteString('text', CrossConn.Text, True);

    // Position and orientation
    JsonWriteInteger('x', CrossConn.Location.X, True);
    JsonWriteInteger('y', CrossConn.Location.Y, True);
    JsonWriteInteger('orientation', Ord(CrossConn.Orientation), True);
    JsonWriteBoolean('isMirrored', CrossConn.IsMirrored, True);

    // Style properties
    JsonWriteInteger('crossSheetStyle', Ord(CrossConn.CrossSheetStyle), True);
    JsonWriteInteger('style', CrossConn.Style, True);
    JsonWriteBoolean('isCustomStyle', CrossConn.IsCustomStyle, True);
    JsonWriteBoolean('showNetName', CrossConn.ShowNetName, True);
    JsonWriteInteger('justification', Ord(CrossConn.Justification), True);
    JsonWriteInteger('fontID', CrossConn.FontID, True);

    // Formula and calculated values
    JsonWriteString('formula', CrossConn.Formula, True);
    JsonWriteString('calculatedValueString', CrossConn.CalculatedValueString, True);
    JsonWriteString('displayString', CrossConn.DisplayString, True);

    // Visual properties
    JsonWriteInteger('color', CrossConn.Color, True);
    JsonWriteInteger('areaColor', CrossConn.AreaColor, True);

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(CrossConn);

    JsonCloseObject(AddComma);
end;

procedure ExportSchBlanketToJson(Blanket: ISch_Blanket; AddComma: Boolean);
var
    ParamIterator: ISch_Iterator;
    Param: ISch_Parameter;
    Isb: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Blanket', True);

    // Display properties
    JsonWriteBoolean('isCollapsed', Blanket.Collapsed, True);

    // Visual properties
    JsonWriteInteger('lineWidth', Ord(Blanket.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Blanket.LineStyle), True);
    JsonWriteBoolean('isSolid', Blanket.IsSolid, True);
    JsonWriteBoolean('transparent', Blanket.Transparent, True);
    JsonWriteInteger('color', Blanket.Color, True);
    JsonWriteInteger('areaColor', Blanket.AreaColor, True);

    // Vertices
    JsonWriteInteger('vertexCount', Blanket.VerticesCount, True);
    JsonOpenArray('vertices');
    for Isb := 0 to Blanket.VerticesCount - 1 do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', Blanket.Vertex[Isb].X, True);
        JsonWriteInteger('y', Blanket.Vertex[Isb].Y, False);
        JsonCloseObject(Isb < Blanket.VerticesCount - 1);
    end;
    JsonCloseArray(True);

    // State flags

    // Unique identifiers

    // Blankets can contain parameters (design directives)
    JsonOpenArray('parameters');
    ParamIterator := Blanket.SchIterator_Create;
    ParamIterator.AddFilter_ObjectSet(MkSet(eParameter));

    Param := ParamIterator.FirstSchObject;
    while Param <> nil do
    begin
        ExportSchParameterToJson(Param, True);
        Param := ParamIterator.NextSchObject;
    end;

    Blanket.SchIterator_Destroy(ParamIterator);
    JsonCloseArray(False);
    // Base graphical object properties
    ExportSchBaseProperties(Blanket);


    JsonCloseObject(AddComma);
end;

procedure ExportSchBusEntryToJson(BusEntry: ISch_BusEntry; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BusEntry', True);

    // Position
    JsonWriteInteger('x1', BusEntry.Location.X, True);
    JsonWriteInteger('y1', BusEntry.Location.Y, True);
    JsonWriteInteger('x2', BusEntry.Corner.X, True);
    JsonWriteInteger('y2', BusEntry.Corner.Y, True);

    // Visual properties
    JsonWriteInteger('lineWidth', Ord(BusEntry.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(BusEntry.LineStyle), True);
    JsonWriteInteger('color', BusEntry.Color, True);
    JsonWriteInteger('areaColor', BusEntry.AreaColor, True);

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(BusEntry);

    JsonCloseObject(AddComma);
end;

procedure ExportSchHyperlinkToJson(Hyperlink: ISch_Hyperlink; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Hyperlink', True);
    JsonWriteString('url', Hyperlink.Url, True);
    JsonWriteString('text', Hyperlink.Text, True);

    // Position and orientation
    JsonWriteInteger('x', Hyperlink.Location.X, True);
    JsonWriteInteger('y', Hyperlink.Location.Y, True);
    JsonWriteInteger('orientation', Ord(Hyperlink.Orientation), True);
    JsonWriteBoolean('isMirrored', Hyperlink.IsMirrored, True);

    // Text properties
    JsonWriteInteger('fontID', Hyperlink.FontID, True);
    JsonWriteInteger('justification', Ord(Hyperlink.Justification), True);
    JsonWriteString('formula', Hyperlink.Formula, True);

    // Visual properties
    JsonWriteInteger('color', Hyperlink.Color, True);
    JsonWriteInteger('areaColor', Hyperlink.AreaColor, True);

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Hyperlink);

    JsonCloseObject(AddComma);
end;

procedure ExportSchDirectiveToJson(Directive: ISch_Directive; AddComma: Boolean);
var
    ParamIterator: ISch_Iterator;
    Param: ISch_Parameter;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Directive', True);
    JsonWriteString('text', Directive.Text, True);

    // Position
    JsonWriteInteger('x', Directive.Location.X, True);
    JsonWriteInteger('y', Directive.Location.Y, True);

    // Visual properties
    JsonWriteInteger('color', Directive.Color, True);
    JsonWriteInteger('areaColor', Directive.AreaColor, True);

    // State flags

    // Unique identifiers

    // Contained parameters
    JsonOpenArray('parameters');
    ParamIterator := Directive.SchIterator_Create;
    ParamIterator.AddFilter_ObjectSet(MkSet(eParameter));

    Param := ParamIterator.FirstSchObject;
    while Param <> nil do
    begin
        ExportSchParameterToJson(Param, True);
        Param := ParamIterator.NextSchObject;
    end;

    Directive.SchIterator_Destroy(ParamIterator);
    JsonCloseArray(False);
    // Base graphical object properties
    ExportSchBaseProperties(Directive);


    JsonCloseObject(AddComma);
end;

procedure ExportSchSymbolToJson(Symbol: ISch_Symbol; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Symbol', True);
    JsonWriteInteger('symbol', Ord(Symbol.Symbol), True);

    // Position and orientation
    JsonWriteInteger('x', Symbol.Location.X, True);
    JsonWriteInteger('y', Symbol.Location.Y, True);
    JsonWriteInteger('orientation', Ord(Symbol.Orientation), True);
    JsonWriteBoolean('isMirrored', Symbol.IsMirrored, True);

    // Size properties
    JsonWriteInteger('scaleFactor', Symbol.ScaleFactor, True);
    JsonWriteInteger('lineWidth', Ord(Symbol.LineWidth), True);

    // Visual properties
    JsonWriteInteger('color', Symbol.Color, True);
    JsonWriteInteger('areaColor', Symbol.AreaColor, True);

    // State flags

    // Unique identifiers
    // Base graphical object properties
    ExportSchBaseProperties(Symbol);

    JsonCloseObject(AddComma);
end;

procedure ExportSchTemplateToJson(Template: ISch_Template; AddComma: Boolean);
var
    Iterator: ISch_Iterator;
    Prim: ISch_GraphicalObject;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Template', True);
    JsonWriteString('fileName', Template.FileName, True);

    // Position
    JsonWriteInteger('x', Template.Location.X, True);
    JsonWriteInteger('y', Template.Location.Y, True);

    // Visual properties
    JsonWriteInteger('color', Template.Color, True);
    JsonWriteInteger('areaColor', Template.AreaColor, True);

    // State flags

    // Unique identifiers

    // Export contained primitives
    JsonOpenArray('primitives');
    Iterator := Template.SchIterator_Create;
    Iterator.AddFilter_ObjectSet(MkSet(eLine, eRectangle, eArc, ePolygon,
        ePolyline, eLabel, eEllipse, eRoundRectangle, eTextFrame));

    Prim := Iterator.FirstSchObject;
    while Prim <> nil do
    begin
        case Prim.ObjectId of
            eLine: ExportSchLineToJson(Prim, True);
            eRectangle: ExportSchRectangleToJson(Prim, True);
            eArc: ExportSchArcToJson(Prim, True);
            ePolygon: ExportSchPolygonToJson(Prim, True);
            ePolyline: ExportSchPolylineToJson(Prim, True);
            eLabel: ExportSchLabelToJson(Prim, True);
            eEllipse: ExportSchEllipseToJson(Prim, True);
            eRoundRectangle: ExportSchRoundRectToJson(Prim, True);
            eTextFrame: ExportSchTextFrameToJson(Prim, True);
        end;
        Prim := Iterator.NextSchObject;
    end;

    Template.SchIterator_Destroy(Iterator);
    JsonCloseArray(False);
    // Base graphical object properties
    ExportSchBaseProperties(Template);


    JsonCloseObject(AddComma);
end;

procedure ExportSchDocComponentToJson(Comp: ISch_Component; AddComma: Boolean);
var
    PinIterator: ISch_Iterator;
    Pin: ISch_Pin;
    ParamIterator: ISch_Iterator;
    Param: ISch_Parameter;
    ImplIterator: ISch_Iterator;
    Impl: ISch_Implementation;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Component', True);

    // Basic identification
    JsonWriteString('designator', Comp.Designator.Text, True);
    JsonWriteString('comment', Comp.Comment.Text, True);
    JsonWriteString('libReference', Comp.LibReference, True);
    JsonWriteString('description', Comp.ComponentDescription, True);

    // Library reference
    JsonWriteString('libraryPath', Comp.LibraryPath, True);
    JsonWriteString('libraryIdentifier', Comp.LibraryIdentifier, True);
    JsonWriteString('sourceLibraryName', Comp.SourceLibraryName, True);
    JsonWriteInteger('libIdentifierKind', Ord(Comp.LibIdentifierKind), True);

    // Database properties
    JsonWriteString('databaseLibraryName', Comp.DatabaseLibraryName, True);
    JsonWriteString('databaseTableName', Comp.DatabaseTableName, True);
    JsonWriteString('designItemId', Comp.DesignItemId, True);

    // Vault/Managed component properties
    JsonWriteString('vaultGUID', Comp.VaultGUID, True);
    JsonWriteString('vaultHRID', Comp.VaultHRID, True);
    JsonWriteString('itemGUID', Comp.ItemGUID, True);
    JsonWriteString('revisionGUID', Comp.RevisionGUID, True);
    JsonWriteString('revisionHRID', Comp.RevisionHRID, True);
    JsonWriteString('revisionState', Comp.RevisionState, True);
    JsonWriteString('revisionStatus', Comp.RevisionStatus, True);
    JsonWriteString('symbolReference', Comp.SymbolReference, True);
    JsonWriteString('symbolItemsGUID', Comp.SymbolItemGUID, True);
    JsonWriteString('symbolRevisionGUID', Comp.SymbolRevisionGUID, True);

    // Location and orientation
    JsonWriteInteger('x', Comp.Location.X, True);
    JsonWriteInteger('y', Comp.Location.Y, True);
    JsonWriteInteger('orientation', Ord(Comp.Orientation), True);
    JsonWriteBoolean('isMirrored', Comp.IsMirrored, True);

    // Display properties
    JsonWriteInteger('displayMode', Ord(Comp.DisplayMode), True);
    JsonWriteInteger('displayModeCount', Comp.DisplayModeCount, True);
    JsonWriteInteger('partCount', Comp.PartCount, True);
    JsonWriteInteger('currentPartId', Comp.GetState_CurrentPartID, True);
    JsonWriteBoolean('isMultiPart', Comp.IsMultiPartComponent, True);
    JsonWriteBoolean('showHiddenFields', Comp.ShowHiddenFields, True);
    JsonWriteBoolean('showHiddenPins', Comp.ShowHiddenPins, True);

    // Visual properties
    JsonWriteBoolean('overrideColors', Comp.OverideColors, True);
    JsonWriteInteger('pinColor', Comp.PinColor, True);
    JsonWriteInteger('color', Comp.Color, True);
    JsonWriteInteger('areaColor', Comp.AreaColor, True);

    // Configuration
    JsonWriteInteger('componentKind', Ord(Comp.ComponentKind), True);
    JsonWriteString('configurationParameters', Comp.ConfigurationParameters, True);
    JsonWriteString('configuratorName', Comp.ConfiguratorName, True);
    JsonWriteInteger('variantOption', Ord(Comp.VariantOption), True);

    // State flags
    JsonWriteBoolean('designatorLocked', Comp.DesignatorLocked, True);
    JsonWriteBoolean('partIdLocked', Comp.PartIdLocked, True);
    JsonWriteBoolean('pinsMoveable', Comp.PinsMoveable, True);
    JsonWriteBoolean('inLibrary', Comp.InLibrary, True);
    JsonWriteBoolean('inSheet', Comp.InSheet, True);
    JsonWriteBoolean('isUnmanaged', Comp.IsUnmanaged, True);

    // Unique identifiers

    // Export pins
    JsonOpenArray('pins');
    PinIterator := Comp.SchIterator_Create;
    PinIterator.AddFilter_ObjectSet(MkSet(ePin));

    Pin := PinIterator.FirstSchObject;
    while Pin <> nil do
    begin
        ExportSchPinToJson(Pin, True);
        Pin := PinIterator.NextSchObject;
    end;

    Comp.SchIterator_Destroy(PinIterator);
    JsonCloseArray(True);

    // Export parameters
    JsonOpenArray('parameters');
    ParamIterator := Comp.SchIterator_Create;
    ParamIterator.AddFilter_ObjectSet(MkSet(eParameter));

    Param := ParamIterator.FirstSchObject;
    while Param <> nil do
    begin
        JsonOpenObject('');
        JsonWriteString('name', Param.Name, True);
        JsonWriteString('text', Param.Text, True);
        JsonWriteBoolean('isHidden', Param.IsHidden, False);
        JsonCloseObject(True);
        Param := ParamIterator.NextSchObject;
    end;

    Comp.SchIterator_Destroy(ParamIterator);
    JsonCloseArray(True);

    // Export implementations (footprints, simulation models, etc.)
    JsonOpenArray('implementations');
    ImplIterator := Comp.SchIterator_Create;
    ImplIterator.AddFilter_ObjectSet(MkSet(eImplementation));

    Impl := ImplIterator.FirstSchObject;
    while Impl <> nil do
    begin
        JsonOpenObject('');
        JsonWriteString('modelName', Impl.ModelName, True);
        JsonWriteString('modelType', Impl.ModelType, True);
        JsonWriteString('description', Impl.Description, True);
        JsonWriteBoolean('isCurrent', Impl.IsCurrent, False);
        Impl := ImplIterator.NextSchObject;
        JsonCloseObject(Impl <> nil);
    end;

    Comp.SchIterator_Destroy(ImplIterator);
    JsonCloseArray(False);
    // Base graphical object properties
    ExportSchBaseProperties(Comp);


    JsonCloseObject(AddComma);
end;

// ====================================================================
// NEW EXPORT PROCEDURES FOR INTERFACES WITHOUT EXISTING PROCS
// ====================================================================

procedure ExportSchSheetFileNameToJson(Obj: ISch_SheetFileName; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchSheetFileName', True);

    // Base schematic properties
    ExportSchBaseProperties(Obj);

    JsonWriteBoolean('autoposition', Obj.Autoposition, True);
    JsonWriteString('calculatedValueString', Obj.CalculatedValueString, True);
    JsonWriteString('displayString', Obj.DisplayString, True);
    JsonWriteInteger('fontID', Obj.FontID, True);
    JsonWriteString('formula', Obj.Formula, True);
    JsonWriteBoolean('isHidden', Obj.IsHidden, True);
    JsonWriteBoolean('isMirrored', Obj.IsMirrored, True);
    JsonWriteInteger('justification', Ord(Obj.Justification), True);
    JsonWriteInteger('orientation', Ord(Obj.Orientation), True);
    JsonWriteString('overrideDisplayString', Obj.OverrideDisplayString, True);
    JsonWriteString('text', Obj.Text, True);
    JsonWriteInteger('textHorzAnchor', Ord(Obj.TextHorzAnchor), True);
    JsonWriteInteger('textVertAnchor', Ord(Obj.TextVertAnchor), False);


    try
        if Obj.I_ObjectAddress <> nil then
            JsonWriteString('i_ObjectAddress_ref', 'present', False)
        else
            JsonWriteString('i_ObjectAddress_ref', '', False);
    except
        JsonWriteString('i_ObjectAddress_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchSheetNameToJson(Obj: ISch_SheetName; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchSheetName', True);

    // Base schematic properties
    ExportSchBaseProperties(Obj);

    JsonWriteBoolean('autoposition', Obj.Autoposition, True);
    JsonWriteString('calculatedValueString', Obj.CalculatedValueString, True);
    JsonWriteString('displayString', Obj.DisplayString, True);
    JsonWriteInteger('fontID', Obj.FontID, True);
    JsonWriteString('formula', Obj.Formula, True);
    JsonWriteBoolean('isHidden', Obj.IsHidden, True);
    JsonWriteBoolean('isMirrored', Obj.IsMirrored, True);
    JsonWriteInteger('justification', Ord(Obj.Justification), True);
    JsonWriteInteger('orientation', Ord(Obj.Orientation), True);
    JsonWriteString('overrideDisplayString', Obj.OverrideDisplayString, True);
    JsonWriteString('text', Obj.Text, True);
    JsonWriteInteger('textHorzAnchor', Ord(Obj.TextHorzAnchor), True);
    JsonWriteInteger('textVertAnchor', Ord(Obj.TextVertAnchor), False);


    try
        if Obj.I_ObjectAddress <> nil then
            JsonWriteString('i_ObjectAddress_ref', 'present', False)
        else
            JsonWriteString('i_ObjectAddress_ref', '', False);
    except
        JsonWriteString('i_ObjectAddress_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchComplexTextToJson(Obj: ISch_ComplexText; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchComplexText', True);

    // Base schematic properties
    ExportSchBaseProperties(Obj);

    JsonWriteBoolean('autoposition', Obj.Autoposition, True);
    JsonWriteString('displayString', Obj.DisplayString, True);
    JsonWriteInteger('fontID', Obj.FontID, True);
    JsonWriteString('formula', Obj.Formula, True);
    JsonWriteBoolean('isHidden', Obj.IsHidden, True);
    JsonWriteBoolean('isMirrored', Obj.IsMirrored, True);
    JsonWriteInteger('justification', Ord(Obj.Justification), True);
    JsonWriteInteger('orientation', Ord(Obj.Orientation), True);
    JsonWriteString('overrideDisplayString', Obj.OverrideDisplayString, True);
    JsonWriteString('text', Obj.Text, True);
    JsonWriteInteger('textHorzAnchor', Ord(Obj.TextHorzAnchor), True);
    JsonWriteInteger('textVertAnchor', Ord(Obj.TextVertAnchor), False);

    JsonCloseObject(AddComma);
end;

procedure ExportSchFunctionalConnectionLineToJson(Obj: ISch_FunctionalConnectionLine; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchFunctionalConnectionLine', True);

    // Base schematic properties
    ExportSchBaseProperties(Obj);

    JsonWriteBoolean('autoWire', Obj.AutoWire, True);
    JsonWriteBoolean('editingEndPoint', Obj.EditingEndPoint, True);
    JsonWriteBoolean('isSolid', Obj.IsSolid, True);
    JsonWriteInteger('lineStyle', Ord(Obj.LineStyle), True);
    JsonWriteInteger('lineWidth', Obj.LineWidth, True);
    JsonWriteBoolean('transparent', Obj.Transparent, True);
    JsonWriteInteger('underlineColor', Obj.UnderlineColor, True);
    JsonWriteInteger('verticesCount', Obj.VerticesCount, True);


    try
        JsonWriteInteger('vertex_0_X', Obj.Vertex[0].X, True);
        JsonWriteInteger('vertex_0_Y', Obj.Vertex[0].Y, True);
    except
        JsonWriteString('vertex_0', 'ERROR', True);
    end;

    try
        JsonWriteBoolean('compilationMaskedSegment_0', Obj.CompilationMaskedSegment[0], False);
    except
        JsonWriteString('compilationMaskedSegment_0', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchBasicPolylineToJson(Obj: ISch_BasicPolyline; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchBasicPolyline', True);

    // Base schematic properties
    ExportSchBaseProperties(Obj);

    JsonWriteBoolean('isSolid', Obj.IsSolid, True);
    JsonWriteInteger('lineStyle', Ord(Obj.LineStyle), True);
    JsonWriteInteger('lineWidth', Obj.LineWidth, True);
    JsonWriteBoolean('transparent', Obj.Transparent, True);
    JsonWriteInteger('verticesCount', Obj.VerticesCount, True);

    // Vertices
    JsonOpenArray('vertices');
    try
        for I := 1 to Obj.VerticesCount do
        begin
            JsonOpenObject('');
            JsonWriteInteger('x', Obj.Vertex[I].X, True);
            JsonWriteInteger('y', Obj.Vertex[I].Y, True);

    try
        if Obj.I_ObjectAddress <> nil then
            JsonWriteString('i_ObjectAddress_ref', 'present', False)
        else
            JsonWriteString('i_ObjectAddress_ref', '', False);
    except
        JsonWriteString('i_ObjectAddress_ref', 'ERROR', False);
    end;

    try
        if Obj.BoundingRectangle <> nil then
            JsonWriteString('boundingRectangle_ref', 'present', False)
        else
            JsonWriteString('boundingRectangle_ref', '', False);
    except
        JsonWriteString('boundingRectangle_ref', 'ERROR', False);
    end;
            JsonCloseObject(I < Obj.VerticesCount);
        end;
    except
    end;
    JsonCloseArray(False);

    JsonCloseObject(AddComma);
end;

procedure ExportSchConnectionLineToJson(Obj: ISch_ConnectionLine; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchConnectionLine', True);

    // Base schematic properties
    ExportSchBaseProperties(Obj);

    JsonWriteBoolean('isInferred', Obj.IsInferred, True);
    JsonWriteInteger('lineStyle', Ord(Obj.LineStyle), True);
    JsonWriteInteger('lineWidth', Obj.LineWidth, True);
    try
        JsonWriteInteger('corner_X', Obj.Corner.X, True);
        JsonWriteInteger('corner_Y', Obj.Corner.Y, True);
    except
        JsonWriteString('corner', 'ERROR: Could not read Corner', True);
    end;

    try
        if Obj.I_ObjectAddress <> nil then
            JsonWriteString('i_ObjectAddress_ref', 'present', False)
        else
            JsonWriteString('i_ObjectAddress_ref', '', False);
    except
        JsonWriteString('i_ObjectAddress_ref', 'ERROR', False);
    end;

    JsonCloseObject(AddComma);
end;

procedure ExportSchRectangularGroupToJson(Obj: ISch_RectangularGroup; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchRectangularGroup', True);

    // Base schematic properties
    ExportSchBaseProperties(Obj);

    JsonWriteCoord('xSize', Obj.XSize, True);
    JsonWriteCoord('ySize', Obj.YSize, False);

    JsonCloseObject(AddComma);
end;

procedure ExportSchLineViewToJson(Obj: ISch_LineView; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchLineView', True);

    // Base schematic properties
    ExportSchBaseProperties(Obj);

    // Import_FromUser is an interactive method (opens dialog), not a property - skip


    try
        if Obj.BoundingRectangle <> nil then
            JsonWriteString('boundingRectangle_ref', 'present', False)
        else
            JsonWriteString('boundingRectangle_ref', '', False);
    except
        JsonWriteString('boundingRectangle_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchParametrizedGroupToJson(Obj: ISch_ParametrizedGroup; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchParametrizedGroup', True);

    // Base schematic properties
    ExportSchBaseProperties(Obj);

    JsonCloseObject(AddComma);
end;

procedure ExportSchLibCompLinkToJson(Obj: ISch_LibraryComponent; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchLibraryComponent', True);

    JsonWriteString('designItemId', Obj.DesignItemId, True);
    JsonWriteString('itemGUID', Obj.ItemGUID, True);
    JsonWriteString('libraryPath', Obj.LibraryPath, True);
    JsonWriteString('revisionGUID', Obj.RevisionGUID, True);
    JsonWriteString('sourceLibraryName', Obj.SourceLibraryName, True);
    JsonWriteBoolean('useLibraryName', Obj.UseLibraryName, True);
    JsonWriteString('vaultGUID', Obj.VaultGUID, False);

    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessLogicalSignalToJson(Obj: ISch_HarnessLogicalSignal; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchHarnessLogicalSignal', True);

    // Base schematic properties
    ExportSchBaseProperties(Obj);

    JsonWriteInteger('lineStyle', Ord(Obj.LineStyle), True);
    JsonWriteInteger('lineWidth', Obj.LineWidth, True);
    try
        JsonWriteInteger('corner_X', Obj.Corner.X, True);
        JsonWriteInteger('corner_Y', Obj.Corner.Y, False);
    except
        JsonWriteString('corner', 'ERROR: Could not read Corner', False);
    end;

    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessLayoutCPConnectorToJson(Obj: ISch_HarnessLayoutConnectionPointConnector; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchHarnessLayoutConnectionPointConnector', True);

    JsonWriteString('uniqueId', Obj.UniqueId, True);
    JsonWriteInteger('pinsCount', Obj.PinsCount, True);


    try
        JsonWriteString('pinId_0', Obj.PinId[0], False);
    except
        JsonWriteString('pinId_0', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

// ====================================================================
// HARNESS DOCUMENT EXPORT PROCEDURES
// ====================================================================

procedure ExportSchHarnessLibraryToJson(Lib: ISch_HarnessLibrary; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchHarnessLibrary', True);
    ExportSchBaseProperties(Lib);

    JsonWriteBoolean('alwaysShowD', Lib.AlwaysShowD, True);
    JsonWriteBoolean('borderOn', Lib.BorderOn, True);
    JsonWriteCoord('customMarginWidth', Lib.CustomMarginWidth, True);
    JsonWriteString('customSheetStyle', Lib.CustomSheetStyle, True);
    JsonWriteCoord('customX', Lib.CustomX, True);
    JsonWriteCoord('customXZones', Lib.CustomXZones, True);
    JsonWriteCoord('customY', Lib.CustomY, True);
    JsonWriteCoord('customYZones', Lib.CustomYZones, True);
    JsonWriteString('description', Lib.Description, True);
    JsonWriteInteger('displayUnit', Ord(Lib.DisplayUnit), True);
    JsonWriteInteger('documentBorderStyle', Ord(Lib.DocumentBorderStyle), True);
    JsonWriteString('documentName', Lib.DocumentName, True);
    JsonWriteString('folderGUID', Lib.FolderGUID, True);
    JsonWriteBoolean('hotspotGridOn', Lib.HotspotGridOn, True);
    JsonWriteCoord('hotspotGridSize', Lib.HotspotGridSize, True);
    JsonWriteCoord('internalTolerance', Lib.InternalTolerance, True);
    JsonWriteBoolean('isLibrary', Lib.IsLibrary, True);
    JsonWriteBoolean('isSingleDesignMode', Lib.IsSingleDesignMode, True);
    JsonWriteBoolean('isSingleComponentMode', Lib.IsSingleComponentMode, True);
    JsonWriteString('itemRevisionGUID', Lib.ItemRevisionGUID, True);
    JsonWriteString('lifeCycleDefinitionGUID', Lib.LifeCycleDefinitionGUID, True);
    JsonWriteString('loadFormat', Lib.LoadFormat, True);
    JsonWriteInteger('minorVersion', Lib.MinorVersion, True);
    JsonWriteString('propsRevisionGUID', Lib.PropsRevisionGUID, True);
    JsonWriteString('propsVaultGUID', Lib.PropsVaultGUID, True);
    JsonWriteBoolean('referenceZonesOn', Lib.ReferenceZonesOn, True);
    JsonWriteInteger('referenceZoneStyle', Ord(Lib.ReferenceZoneStyle), True);
    JsonWriteString('releaseItemGUID', Lib.ReleaseItemGUID, True);
    JsonWriteString('releaseVaultGUID', Lib.ReleaseVaultGUID, True);
    JsonWriteString('revisionNamingSchemeGUID', Lib.RevisionNamingSchemeGUID, True);
    JsonWriteInteger('schDocID', Lib.SchDocID, True);
    JsonWriteCoord('sheetMarginWidth', Lib.SheetMarginWidth, True);
    JsonWriteInteger('sheetNumberSpaceSize', Lib.SheetNumberSpaceSize, True);
    JsonWriteCoord('sheetSizeX', Lib.SheetSizeX, True);
    JsonWriteCoord('sheetSizeY', Lib.SheetSizeY, True);
    JsonWriteInteger('sheetStyle', Ord(Lib.SheetStyle), True);
    JsonWriteInteger('sheetZonesX', Lib.SheetZonesX, True);
    JsonWriteInteger('sheetZonesY', Lib.SheetZonesY, True);
    JsonWriteBoolean('showHiddenPins', Lib.ShowHiddenPins, True);
    JsonWriteBoolean('showTemplateGraphics', Lib.ShowTemplateGraphics, True);
    JsonWriteBoolean('snapGridOn', Lib.SnapGridOn, True);
    JsonWriteCoord('snapGridSize', Lib.SnapGridSize, True);
    JsonWriteInteger('systemFont', Lib.SystemFont, True);
    JsonWriteString('templateFileName', Lib.TemplateFileName, True);
    JsonWriteString('templateItemGUID', Lib.TemplateItemGUID, True);
    JsonWriteString('templateRevisionGUID', Lib.TemplateRevisionGUID, True);
    JsonWriteString('templateRevisionHRID', Lib.TemplateRevisionHRID, True);
    JsonWriteString('templateVaultGUID', Lib.TemplateVaultGUID, True);
    JsonWriteString('templateVaultHRID', Lib.TemplateVaultHRID, True);
    JsonWriteBoolean('titleBlockOn', Lib.TitleBlockOn, True);
    JsonWriteInteger('unitSystem', Ord(Lib.UnitSystem), True);
    JsonWriteBoolean('useCustomSheet', Lib.UseCustomSheet, True);
    JsonWriteBoolean('visibleGridOn', Lib.VisibleGridOn, True);
    JsonWriteCoord('visibleGridSize', Lib.VisibleGridSize, True);
    JsonWriteInteger('workspaceOrientation', Ord(Lib.WorkspaceOrientation), True);


    try
        if Lib.CurrentSchComponent <> nil then
            JsonWriteString('currentSchComponent_ref', 'present', False)
        else
            JsonWriteString('currentSchComponent_ref', '', False);
    except
        JsonWriteString('currentSchComponent_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessDocumentToJson(Doc: ISch_HarnessDocument; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchHarnessDocument', True);
    ExportSchBaseProperties(Doc);

    JsonWriteBoolean('borderOn', Doc.BorderOn, True);
    JsonWriteCoord('customMarginWidth', Doc.CustomMarginWidth, True);
    JsonWriteString('customSheetStyle', Doc.CustomSheetStyle, True);
    JsonWriteCoord('customX', Doc.CustomX, True);
    JsonWriteCoord('customXZones', Doc.CustomXZones, True);
    JsonWriteCoord('customY', Doc.CustomY, True);
    JsonWriteCoord('customYZones', Doc.CustomYZones, True);
    JsonWriteInteger('displayUnit', Ord(Doc.DisplayUnit), True);
    JsonWriteInteger('documentBorderStyle', Ord(Doc.DocumentBorderStyle), True);
    JsonWriteString('documentName', Doc.DocumentName, True);
    JsonWriteBoolean('hotspotGridOn', Doc.HotspotGridOn, True);
    JsonWriteCoord('hotspotGridSize', Doc.HotspotGridSize, True);
    JsonWriteCoord('internalTolerance', Doc.InternalTolerance, True);
    JsonWriteBoolean('isLibrary', Doc.IsLibrary, True);
    JsonWriteString('itemRevisionGUID', Doc.ItemRevisionGUID, True);
    JsonWriteInteger('lengthUnit', Ord(Doc.LengthUnit), True);
    JsonWriteString('loadFormat', Doc.LoadFormat, True);
    JsonWriteInteger('minorVersion', Doc.MinorVersion, True);
    JsonWriteString('propsRevisionGUID', Doc.PropsRevisionGUID, True);
    JsonWriteString('propsVaultGUID', Doc.PropsVaultGUID, True);
    JsonWriteBoolean('referenceZonesOn', Doc.ReferenceZonesOn, True);
    JsonWriteInteger('referenceZoneStyle', Ord(Doc.ReferenceZoneStyle), True);
    JsonWriteString('releaseItemGUID', Doc.ReleaseItemGUID, True);
    JsonWriteString('releaseVaultGUID', Doc.ReleaseVaultGUID, True);
    JsonWriteInteger('schDocID', Doc.SchDocID, True);
    JsonWriteCoord('sheetMarginWidth', Doc.SheetMarginWidth, True);
    JsonWriteInteger('sheetNumberSpaceSize', Doc.SheetNumberSpaceSize, True);
    JsonWriteCoord('sheetSizeX', Doc.SheetSizeX, True);
    JsonWriteCoord('sheetSizeY', Doc.SheetSizeY, True);
    JsonWriteInteger('sheetStyle', Ord(Doc.SheetStyle), True);
    JsonWriteInteger('sheetZonesX', Doc.SheetZonesX, True);
    JsonWriteInteger('sheetZonesY', Doc.SheetZonesY, True);
    JsonWriteBoolean('showTemplateGraphics', Doc.ShowTemplateGraphics, True);
    JsonWriteBoolean('snapGridOn', Doc.SnapGridOn, True);
    JsonWriteCoord('snapGridSize', Doc.SnapGridSize, True);
    JsonWriteInteger('systemFont', Doc.SystemFont, True);
    JsonWriteString('templateFileName', Doc.TemplateFileName, True);
    JsonWriteString('templateItemGUID', Doc.TemplateItemGUID, True);
    JsonWriteString('templateRevisionGUID', Doc.TemplateRevisionGUID, True);
    JsonWriteString('templateRevisionHRID', Doc.TemplateRevisionHRID, True);
    JsonWriteString('templateVaultGUID', Doc.TemplateVaultGUID, True);
    JsonWriteString('templateVaultHRID', Doc.TemplateVaultHRID, True);
    JsonWriteBoolean('titleBlockOn', Doc.TitleBlockOn, True);
    JsonWriteInteger('unitSystem', Ord(Doc.UnitSystem), True);
    JsonWriteBoolean('useCustomSheet', Doc.UseCustomSheet, True);
    JsonWriteBoolean('visibleGridOn', Doc.VisibleGridOn, True);
    JsonWriteCoord('visibleGridSize', Doc.VisibleGridSize, True);
    JsonWriteInteger('workspaceOrientation', Ord(Doc.WorkspaceOrientation), False);

    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessLayoutDrawingToJson(Drawing: ISch_HarnessLayoutDrawing; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchHarnessLayoutDrawing', True);
    ExportSchBaseProperties(Drawing);

    JsonWriteBoolean('borderOn', Drawing.BorderOn, True);
    JsonWriteCoord('customMarginWidth', Drawing.CustomMarginWidth, True);
    JsonWriteString('customSheetStyle', Drawing.CustomSheetStyle, True);
    JsonWriteCoord('customX', Drawing.CustomX, True);
    JsonWriteCoord('customXZones', Drawing.CustomXZones, True);
    JsonWriteCoord('customY', Drawing.CustomY, True);
    JsonWriteCoord('customYZones', Drawing.CustomYZones, True);
    JsonWriteInteger('displayUnit', Ord(Drawing.DisplayUnit), True);
    JsonWriteInteger('documentBorderStyle', Ord(Drawing.DocumentBorderStyle), True);
    JsonWriteString('documentName', Drawing.DocumentName, True);
    JsonWriteBoolean('hotspotGridOn', Drawing.HotspotGridOn, True);
    JsonWriteCoord('hotspotGridSize', Drawing.HotspotGridSize, True);
    JsonWriteCoord('internalTolerance', Drawing.InternalTolerance, True);
    JsonWriteBoolean('isLibrary', Drawing.IsLibrary, True);
    JsonWriteString('itemRevisionGUID', Drawing.ItemRevisionGUID, True);
    JsonWriteInteger('lengthUnit', Ord(Drawing.LengthUnit), True);
    JsonWriteString('loadFormat', Drawing.LoadFormat, True);
    JsonWriteInteger('minorVersion', Drawing.MinorVersion, True);
    JsonWriteString('propsRevisionGUID', Drawing.PropsRevisionGUID, True);
    JsonWriteString('propsVaultGUID', Drawing.PropsVaultGUID, True);
    JsonWriteBoolean('referenceZonesOn', Drawing.ReferenceZonesOn, True);
    JsonWriteInteger('referenceZoneStyle', Ord(Drawing.ReferenceZoneStyle), True);
    JsonWriteString('releaseItemGUID', Drawing.ReleaseItemGUID, True);
    JsonWriteString('releaseVaultGUID', Drawing.ReleaseVaultGUID, True);
    JsonWriteInteger('schDocID', Drawing.SchDocID, True);
    JsonWriteCoord('sheetMarginWidth', Drawing.SheetMarginWidth, True);
    JsonWriteInteger('sheetNumberSpaceSize', Drawing.SheetNumberSpaceSize, True);
    JsonWriteCoord('sheetSizeX', Drawing.SheetSizeX, True);
    JsonWriteCoord('sheetSizeY', Drawing.SheetSizeY, True);
    JsonWriteInteger('sheetStyle', Ord(Drawing.SheetStyle), True);
    JsonWriteInteger('sheetZonesX', Drawing.SheetZonesX, True);
    JsonWriteInteger('sheetZonesY', Drawing.SheetZonesY, True);
    JsonWriteBoolean('showTemplateGraphics', Drawing.ShowTemplateGraphics, True);
    JsonWriteBoolean('snapGridOn', Drawing.SnapGridOn, True);
    JsonWriteCoord('snapGridSize', Drawing.SnapGridSize, True);
    JsonWriteInteger('systemFont', Drawing.SystemFont, True);
    JsonWriteString('templateFileName', Drawing.TemplateFileName, True);
    JsonWriteString('templateItemGUID', Drawing.TemplateItemGUID, True);
    JsonWriteString('templateRevisionGUID', Drawing.TemplateRevisionGUID, True);
    JsonWriteString('templateRevisionHRID', Drawing.TemplateRevisionHRID, True);
    JsonWriteString('templateVaultGUID', Drawing.TemplateVaultGUID, True);
    JsonWriteString('templateVaultHRID', Drawing.TemplateVaultHRID, True);
    JsonWriteBoolean('titleBlockOn', Drawing.TitleBlockOn, True);
    JsonWriteInteger('unitSystem', Ord(Drawing.UnitSystem), True);
    JsonWriteBoolean('useCustomSheet', Drawing.UseCustomSheet, True);
    JsonWriteBoolean('visibleGridOn', Drawing.VisibleGridOn, True);
    JsonWriteCoord('visibleGridSize', Drawing.VisibleGridSize, True);
    JsonWriteInteger('workspaceOrientation', Ord(Drawing.WorkspaceOrientation), False);

    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessWiringDiagramToJson(Diagram: ISch_HarnessWiringDiagram; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchHarnessWiringDiagram', True);
    ExportSchBaseProperties(Diagram);

    JsonWriteBoolean('borderOn', Diagram.BorderOn, True);
    JsonWriteCoord('customMarginWidth', Diagram.CustomMarginWidth, True);
    JsonWriteString('customSheetStyle', Diagram.CustomSheetStyle, True);
    JsonWriteCoord('customX', Diagram.CustomX, True);
    JsonWriteCoord('customXZones', Diagram.CustomXZones, True);
    JsonWriteCoord('customY', Diagram.CustomY, True);
    JsonWriteCoord('customYZones', Diagram.CustomYZones, True);
    JsonWriteInteger('displayUnit', Ord(Diagram.DisplayUnit), True);
    JsonWriteInteger('documentBorderStyle', Ord(Diagram.DocumentBorderStyle), True);
    JsonWriteString('documentName', Diagram.DocumentName, True);
    JsonWriteBoolean('hotspotGridOn', Diagram.HotspotGridOn, True);
    JsonWriteCoord('hotspotGridSize', Diagram.HotspotGridSize, True);
    JsonWriteCoord('internalTolerance', Diagram.InternalTolerance, True);
    JsonWriteBoolean('isLibrary', Diagram.IsLibrary, True);
    JsonWriteString('itemRevisionGUID', Diagram.ItemRevisionGUID, True);
    JsonWriteInteger('lengthUnit', Ord(Diagram.LengthUnit), True);
    JsonWriteString('loadFormat', Diagram.LoadFormat, True);
    JsonWriteInteger('minorVersion', Diagram.MinorVersion, True);
    JsonWriteString('propsRevisionGUID', Diagram.PropsRevisionGUID, True);
    JsonWriteString('propsVaultGUID', Diagram.PropsVaultGUID, True);
    JsonWriteBoolean('referenceZonesOn', Diagram.ReferenceZonesOn, True);
    JsonWriteInteger('referenceZoneStyle', Ord(Diagram.ReferenceZoneStyle), True);
    JsonWriteString('releaseItemGUID', Diagram.ReleaseItemGUID, True);
    JsonWriteString('releaseVaultGUID', Diagram.ReleaseVaultGUID, True);
    JsonWriteInteger('schDocID', Diagram.SchDocID, True);
    JsonWriteCoord('sheetMarginWidth', Diagram.SheetMarginWidth, True);
    JsonWriteInteger('sheetNumberSpaceSize', Diagram.SheetNumberSpaceSize, True);
    JsonWriteCoord('sheetSizeX', Diagram.SheetSizeX, True);
    JsonWriteCoord('sheetSizeY', Diagram.SheetSizeY, True);
    JsonWriteInteger('sheetStyle', Ord(Diagram.SheetStyle), True);
    JsonWriteInteger('sheetZonesX', Diagram.SheetZonesX, True);
    JsonWriteInteger('sheetZonesY', Diagram.SheetZonesY, True);
    JsonWriteBoolean('showTemplateGraphics', Diagram.ShowTemplateGraphics, True);
    JsonWriteBoolean('snapGridOn', Diagram.SnapGridOn, True);
    JsonWriteCoord('snapGridSize', Diagram.SnapGridSize, True);
    JsonWriteInteger('systemFont', Diagram.SystemFont, True);
    JsonWriteString('templateFileName', Diagram.TemplateFileName, True);
    JsonWriteString('templateItemGUID', Diagram.TemplateItemGUID, True);
    JsonWriteString('templateRevisionGUID', Diagram.TemplateRevisionGUID, True);
    JsonWriteString('templateRevisionHRID', Diagram.TemplateRevisionHRID, True);
    JsonWriteString('templateVaultGUID', Diagram.TemplateVaultGUID, True);
    JsonWriteString('templateVaultHRID', Diagram.TemplateVaultHRID, True);
    JsonWriteBoolean('titleBlockOn', Diagram.TitleBlockOn, True);
    JsonWriteInteger('unitSystem', Ord(Diagram.UnitSystem), True);
    JsonWriteBoolean('useCustomSheet', Diagram.UseCustomSheet, True);
    JsonWriteBoolean('visibleGridOn', Diagram.VisibleGridOn, True);
    JsonWriteCoord('visibleGridSize', Diagram.VisibleGridSize, True);
    JsonWriteInteger('workspaceOrientation', Ord(Diagram.WorkspaceOrientation), False);

    JsonCloseObject(AddComma);
end;

// ====================================================================
// HARNESS LAYOUT OBJECT EXPORT PROCEDURES
// ====================================================================

procedure ExportSchHarnessLayoutLabelToJson(Lbl: ISch_HarnessLayoutLabel; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchHarnessLayoutLabel', True);
    ExportSchBaseProperties(Lbl);

    JsonWriteInteger('alignment', Ord(Lbl.Alignment), True);
    JsonWriteString('calculatedValueString', Lbl.CalculatedValueString, True);
    JsonWriteBoolean('designatorLocked', Lbl.DesignatorLocked, True);
    JsonWriteString('displayString', Lbl.DisplayString, True);
    JsonWriteInteger('fontID', Lbl.FontID, True);
    JsonWriteString('formula', Lbl.Formula, True);
    JsonWriteBoolean('isMirrored', Lbl.IsMirrored, True);
    JsonWriteInteger('justification', Ord(Lbl.Justification), True);
    JsonWriteInteger('orientation', Ord(Lbl.Orientation), True);
    JsonWriteString('overrideDisplayString', Lbl.OverrideDisplayString, True);
    JsonWriteString('text', Lbl.Text, True);
    JsonWriteInteger('textColor', Lbl.TextColor, False);

        try
        if Lbl.Designator <> nil then
            JsonWriteString('designator_ref', Lbl.Designator.Text, True)
        else
            JsonWriteString('designator_ref', '', True);
    except
        JsonWriteString('designator_ref', 'ERROR', True);
    end;
    try
        if Lbl.Comment <> nil then
            JsonWriteString('comment_ref', Lbl.Comment.Text, True)
        else
            JsonWriteString('comment_ref', '', True);
    except
        JsonWriteString('comment_ref', 'ERROR', True);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessLayoutConnectionPointToJson(CP: ISch_HarnessLayoutConnectionPoint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchHarnessLayoutConnectionPoint', True);
    ExportSchBaseProperties(CP);

    JsonWriteInteger('borderColor', CP.BorderColor, True);
    JsonWriteString('displayString', CP.DisplayString, True);
    JsonWriteInteger('fontID', CP.FontID, True);
    JsonWriteString('formula', CP.Formula, True);
    JsonWriteBoolean('isMirrored', CP.IsMirrored, True);
    JsonWriteInteger('justification', Ord(CP.Justification), True);
    JsonWriteInteger('orientation', Ord(CP.Orientation), True);
    JsonWriteString('overrideDisplayString', CP.OverrideDisplayString, True);
    JsonWriteBoolean('showName', CP.ShowName, True);
    JsonWriteInteger('style', Ord(CP.Style), True);
    JsonWriteString('text', CP.Text, False);

    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessLayoutCoveringToJson(Covering: ISch_HarnessLayoutCovering; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchHarnessLayoutCovering', True);
    ExportSchBaseProperties(Covering);

    JsonWriteInteger('borderSize', Covering.BorderSize, True);
    JsonWriteBoolean('designatorLocked', Covering.DesignatorLocked, True);
    JsonWriteInteger('itemsCount', Covering.ItemsCount, True);
    JsonWriteInteger('thickness', Covering.Thickness, True);
    JsonWriteBoolean('transparent', Covering.Transparent, True);

    try
        if Covering.Designator <> nil then
            JsonWriteString('designator_ref', Covering.Designator.Text, True)
        else
            JsonWriteString('designator_ref', '', True);
    except
        JsonWriteString('designator_ref', 'ERROR', True);
    end;
    try
        if Covering.Comment <> nil then
            JsonWriteString('comment_ref', Covering.Comment.Text, True)
        else
            JsonWriteString('comment_ref', '', True);
    except
        JsonWriteString('comment_ref', 'ERROR', True);
    end;
    try
        if Covering.Item[0] <> nil then
            JsonWriteString('item_0_ref', 'present', False)
        else
            JsonWriteString('item_0_ref', '', False);
    except
        JsonWriteString('item_0_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

// ====================================================================
// HARNESS DATA EXPORT PROCEDURES
// ====================================================================

procedure ExportSchHarnessWireDataToJson(Data: ISch_HarnessWireData; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchHarnessWireData', True);
    JsonWriteInteger('color', Data.Color, True);
    JsonWriteString('colorName', Data.ColorName, True);
    JsonWriteString('comment', Data.Comment, True);
    JsonWriteString('description', Data.Description, True);
    JsonWriteString('handle', Data.Handle, True);
    JsonWriteInteger('length', Data.Length, True);
    JsonWriteString('name', Data.Name, True);
    JsonWriteInteger('objectId', Data.ObjectId, True);
    JsonWriteString('uniqueId', Data.UniqueId, True);
    try
        if Data.OwnerDocument <> nil then
            JsonWriteString('ownerDocument_ref', Data.OwnerDocument.DocumentName, True)
        else
            JsonWriteString('ownerDocument_ref', '', True);
    except
        JsonWriteString('ownerDocument_ref', 'ERROR', True);
    end;
    try
        if Data.Container <> nil then
            JsonWriteString('container_ref', Data.Container.UniqueId, False)
        else
            JsonWriteString('container_ref', '', False);
    except
        JsonWriteString('container_ref', 'ERROR', False);
    end;

    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessSpliceDataToJson(Data: ISch_HarnessSpliceData; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchHarnessSpliceData', True);
    JsonWriteString('designator', Data.Designator, True);
    JsonWriteString('handle', Data.Handle, True);
    JsonWriteInteger('objectId', Data.ObjectId, True);
    JsonWriteInteger('style', Ord(Data.Style), True);
    JsonWriteString('uniqueId', Data.UniqueId, True);
    try
        if Data.OwnerDocument <> nil then
            JsonWriteString('ownerDocument_ref', Data.OwnerDocument.DocumentName, True)
        else
            JsonWriteString('ownerDocument_ref', '', True);
    except
        JsonWriteString('ownerDocument_ref', 'ERROR', True);
    end;
    try
        if Data.Container <> nil then
            JsonWriteString('container_ref', Data.Container.UniqueId, False)
        else
            JsonWriteString('container_ref', '', False);
    except
        JsonWriteString('container_ref', 'ERROR', False);
    end;

    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessBundleSubLineDataToJson(Data: ISch_HarnessBundleSubLineData; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchHarnessBundleSubLineData', True);
    JsonWriteString('handle', Data.Handle, True);
    JsonWriteInteger('length', Data.Length, True);
    JsonWriteInteger('objectId', Data.ObjectId, True);
    JsonWriteString('uniqueId', Data.UniqueId, True);
    try
        if Data.OwnerDocument <> nil then
            JsonWriteString('ownerDocument_ref', Data.OwnerDocument.DocumentName, True)
        else
            JsonWriteString('ownerDocument_ref', '', True);
    except
        JsonWriteString('ownerDocument_ref', 'ERROR', True);
    end;
    try
        if Data.Container <> nil then
            JsonWriteString('container_ref', Data.Container.UniqueId, False)
        else
            JsonWriteString('container_ref', '', False);
    except
        JsonWriteString('container_ref', 'ERROR', False);
    end;

    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessCableDataToJson(Data: ISch_HarnessCableData; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchHarnessCableData', True);
    JsonWriteString('handle', Data.Handle, True);
    JsonWriteInteger('length', Data.Length, True);
    JsonWriteInteger('objectId', Data.ObjectId, True);
    JsonWriteString('uniqueId', Data.UniqueId, True);
    try
        if Data.OwnerDocument <> nil then
            JsonWriteString('ownerDocument_ref', Data.OwnerDocument.DocumentName, True)
        else
            JsonWriteString('ownerDocument_ref', '', True);
    except
        JsonWriteString('ownerDocument_ref', 'ERROR', True);
    end;
    try
        if Data.Container <> nil then
            JsonWriteString('container_ref', Data.Container.UniqueId, False)
        else
            JsonWriteString('container_ref', '', False);
    except
        JsonWriteString('container_ref', 'ERROR', False);
    end;

    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessNoConnectDataToJson(Data: ISch_HarnessNoConnectData; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchHarnessNoConnectData', True);
    JsonWriteString('designator', Data.Designator, True);
    JsonWriteString('handle', Data.Handle, True);
    JsonWriteInteger('objectId', Data.ObjectId, True);
    JsonWriteString('uniqueId', Data.UniqueId, True);
    try
        if Data.OwnerDocument <> nil then
            JsonWriteString('ownerDocument_ref', Data.OwnerDocument.DocumentName, True)
        else
            JsonWriteString('ownerDocument_ref', '', True);
    except
        JsonWriteString('ownerDocument_ref', 'ERROR', True);
    end;
    try
        if Data.Container <> nil then
            JsonWriteString('container_ref', Data.Container.UniqueId, False)
        else
            JsonWriteString('container_ref', '', False);
    except
        JsonWriteString('container_ref', 'ERROR', False);
    end;

    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessShieldDataToJson(Data: ISch_HarnessShieldData; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchHarnessShieldData', True);
    JsonWriteString('handle', Data.Handle, True);
    JsonWriteInteger('objectId', Data.ObjectId, True);
    JsonWriteString('uniqueId', Data.UniqueId, True);
    try
        if Data.OwnerDocument <> nil then
            JsonWriteString('ownerDocument_ref', Data.OwnerDocument.DocumentName, True)
        else
            JsonWriteString('ownerDocument_ref', '', True);
    except
        JsonWriteString('ownerDocument_ref', 'ERROR', True);
    end;
    try
        if Data.Container <> nil then
            JsonWriteString('container_ref', Data.Container.UniqueId, False)
        else
            JsonWriteString('container_ref', '', False);
    except
        JsonWriteString('container_ref', 'ERROR', False);
    end;

    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessTwistDataToJson(Data: ISch_HarnessTwistData; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchHarnessTwistData', True);
    JsonWriteString('handle', Data.Handle, True);
    JsonWriteInteger('objectId', Data.ObjectId, True);
    JsonWriteString('uniqueId', Data.UniqueId, True);
    try
        if Data.OwnerDocument <> nil then
            JsonWriteString('ownerDocument_ref', Data.OwnerDocument.DocumentName, True)
        else
            JsonWriteString('ownerDocument_ref', '', True);
    except
        JsonWriteString('ownerDocument_ref', 'ERROR', True);
    end;
    try
        if Data.Container <> nil then
            JsonWriteString('container_ref', Data.Container.UniqueId, False)
        else
            JsonWriteString('container_ref', '', False);
    except
        JsonWriteString('container_ref', 'ERROR', False);
    end;

    JsonCloseObject(AddComma);
end;

// ====================================================================
// PCB EXPORT PROCEDURES (remaining interfaces)
// ====================================================================

procedure ExportPcbClearanceMatrixConstraintToJson(Rule: IPCB_ClearanceMatrixConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'PcbClearanceMatrixConstraint', True);
    ExportPcbBaseRuleProperties(Rule);

    JsonWriteString('data', Rule.Data, True);
    JsonWriteCoord('gap', Rule.Gap, True);
    JsonWriteInteger('mode', Ord(Rule.Mode), True);


    // Object reference properties
    try
        if Rule.ClearanceRules <> nil then
            JsonWriteString('clearanceRules_ref', 'present', True)
        else
            JsonWriteString('clearanceRules_ref', '', True);
    except
        JsonWriteString('clearanceRules_ref', 'ERROR', True);
    end;
    try
        if Rule.DefaultSameClearanceRule <> nil then
            JsonWriteString('defaultSameClearanceRule_ref', 'present', True)
        else
            JsonWriteString('defaultSameClearanceRule_ref', '', True);
    except
        JsonWriteString('defaultSameClearanceRule_ref', 'ERROR', True);
    end;
    try
        if Rule.SameClearanceRules <> nil then
            JsonWriteString('sameClearanceRules_ref', 'present', True)
        else
            JsonWriteString('sameClearanceRules_ref', '', True);
    except
        JsonWriteString('sameClearanceRules_ref', 'ERROR', True);
    end;
    try
        JsonWriteString('ruleOnLayer', 'RuleOnLayer indexed by IDispatch key', False); // Rule.RuleOnLayer
    except
        JsonWriteString('ruleOnLayer', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbDielectricObjectToJson(Obj: IPCB_DielectricObject; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'PcbDielectricObject', True);
    JsonWriteFloat('dielectricConstant', Obj.DielectricConstant, True);
    JsonWriteCoord('dielectricHeight', Obj.DielectricHeight, True);
    JsonWriteFloat('dielectricLossTangent', Obj.DielectricLossTangent, True);
    JsonWriteString('dielectricMaterial', Obj.DielectricMaterial, True);
    JsonWriteInteger('dielectricType', Ord(Obj.DielectricType), True);
    JsonWriteBoolean('isInLayerStack', Obj.IsInLayerStack, True);
    JsonWriteBoolean('isStiffener', Obj.IsStiffener, True);
    JsonWriteString('name', Obj.Name, True);
    JsonWriteBoolean('usedByPrims', Obj.UsedByPrims, True);
    JsonWriteInteger('v6_LayerID', Obj.V6_LayerID, False);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbFullComponentToJson(Comp: IPCB_FullComponent; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'PcbFullComponent', True);
    JsonWriteString('comment', Comp.Comment, True);
    JsonWriteString('componentID', Comp.ComponentID, True);
    JsonWriteString('description', Comp.Description, True);
    JsonWriteString('designator', Comp.Designator, True);
    JsonWriteString('itemGUID', Comp.ItemGUID, True);
    JsonWriteInteger('kind', Ord(Comp.Kind), True);
    JsonWriteString('libraryItemID', Comp.LibraryItemID, True);
    JsonWriteString('libraryName', Comp.LibraryName, True);
    JsonWriteString('revisionGUID', Comp.RevisionGUID, True);
    JsonWriteString('vaultGUID', Comp.VaultGUID, True);


    // Object reference properties
    try
        if Comp.DesignVariant <> nil then
            JsonWriteString('designVariant_ref', 'present', True)
        else
            JsonWriteString('designVariant_ref', '', True);
    except
        JsonWriteString('designVariant_ref', 'ERROR', True);
    end;
    try
        if Comp.Footprint <> nil then
            JsonWriteString('footprint_ref', 'present', True)
        else
            JsonWriteString('footprint_ref', '', True);
    except
        JsonWriteString('footprint_ref', 'ERROR', True);
    end;
    try
        if Comp.Parameters <> nil then
            JsonWriteString('parameters_ref', 'present', True)
        else
            JsonWriteString('parameters_ref', '', True);
    except
        JsonWriteString('parameters_ref', 'ERROR', True);
    end;
    try
        if Comp.SystemParameters <> nil then
            JsonWriteString('systemParameters_ref', 'present', False)
        else
            JsonWriteString('systemParameters_ref', '', False);
    except
        JsonWriteString('systemParameters_ref', 'ERROR', False);
    end;
    JsonCloseObject(AddComma);
end;

procedure ExportPcbColorOverrideOptionsToJson(Opts: IPCB_ColorOverrideOptions; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'PcbColorOverrideOptions', True);
    JsonWriteString('actualTexture', Opts.ActualTexture, True);
    JsonWriteBoolean('colorOverrideActive', Opts.ColorOverrideActive, True);
    JsonWriteInteger('pattern', Ord(Opts.Pattern), True);
    JsonWriteInteger('zoom', Ord(Opts.Zoom), False);

    JsonCloseObject(AddComma);
end;

procedure ExportSchDocToJson(SchDoc: ISch_Document; JsonPath: String);
var
    Iterator: ISch_Iterator;
    Prim: ISch_GraphicalObject;
begin
    if SchDoc = nil then Exit;

    JsonBegin;
    JsonOpenObject('');

    JsonOpenObject('metadata');
    JsonWriteString('exportType', 'SchDoc', True);
    JsonWriteString('fileName', ExtractFileName(SchDoc.DocumentName), True);
    JsonWriteString('exportedBy', 'AltiumSharp FileToJsonConverter', True);
    JsonWriteString('version', '1.0', False);
    JsonCloseObject(True);

    JsonOpenArray('objects');

    Iterator := SchDoc.SchIterator_Create;
    Iterator.AddFilter_ObjectSet(MkSet(eSchComponent, eWire, eBus, eJunction,
        eNetLabel, ePort, ePowerObject, eNoERC, eSheetSymbol,
        eParameterSet, eCrossSheetConnector, eBlanket, eBusEntry,
        eLine, eRectangle, eArc, ePolygon, ePolyline, eLabel, eEllipse,
        eRoundRectangle, eTextFrame, eImage, eDesignator, eNote, eBezier,
        eEllipticalArc, ePie, eProbe, eSignalHarness, eHarnessConnector,
        eHarnessEntry, eCompileMask));

    Prim := Iterator.FirstSchObject;
    while Prim <> nil do
    begin
        case Prim.ObjectId of
            eSchComponent: ExportSchDocComponentToJson(Prim, True);
            eWire: ExportSchWireToJson(Prim, True);
            eBus: ExportSchBusToJson(Prim, True);
            eJunction: ExportSchJunctionToJson(Prim, True);
            eNetLabel: ExportSchNetLabelToJson(Prim, True);
            ePort: ExportSchPortToJson(Prim, True);
            ePowerObject: ExportSchPowerPortToJson(Prim, True);
            eNoERC: ExportSchNoErcMarkerToJson(Prim, True);
            eSheetSymbol: ExportSchSheetSymbolToJson(Prim, True);
            eParameterSet: ExportSchParameterSetToJson(Prim, True);
            eCrossSheetConnector: ExportSchCrossSheetConnectorToJson(Prim, True);
            eBlanket: ExportSchBlanketToJson(Prim, True);
            eBusEntry: ExportSchBusEntryToJson(Prim, True);
            eLine: ExportSchLineToJson(Prim, True);
            eRectangle: ExportSchRectangleToJson(Prim, True);
            eArc: ExportSchArcToJson(Prim, True);
            ePolygon: ExportSchPolygonToJson(Prim, True);
            ePolyline: ExportSchPolylineToJson(Prim, True);
            eLabel: ExportSchLabelToJson(Prim, True);
            eEllipse: ExportSchEllipseToJson(Prim, True);
            eRoundRectangle: ExportSchRoundRectToJson(Prim, True);
            eTextFrame: ExportSchTextFrameToJson(Prim, True);
            eImage: ExportSchImageToJson(Prim, True);
            eDesignator: ExportSchDesignatorToJson(Prim, True);
            eNote: ExportSchNoteToJson(Prim, True);
            eBezier: ExportSchBezierToJson(Prim, True);
            eEllipticalArc: ExportSchEllipticalArcToJson(Prim, True);
            ePie: ExportSchPieToJson(Prim, True);
            eProbe: ExportSchProbeToJson(Prim, True);
            eSignalHarness: ExportSchSignalHarnessToJson(Prim, True);
            eHarnessConnector: ExportSchHarnessConnectorToJson(Prim, True);
            eHarnessEntry: ExportSchHarnessEntryToJson(Prim, True);
            eCompileMask: ExportSchCompileMaskToJson(Prim, True);
        end;
        Prim := Iterator.NextSchObject;
    end;

    SchDoc.SchIterator_Destroy(Iterator);

    JsonCloseArray(False);
    JsonCloseObject(False);

    JsonEnd(JsonPath);
end;

{==============================================================================
  PCB DOCUMENT JSON EXPORT
==============================================================================}

procedure ExportPcbViaToJson(Via: IPCB_Via; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Via', True);
    JsonWriteCoord('x', Via.X, True);
    JsonWriteCoord('y', Via.Y, True);
    JsonWriteCoord('holeSize', Via.HoleSize, True);
    JsonWriteCoord('size', Via.Size, True);
    JsonWriteInteger('lowLayer', Via.LowLayer, True);
    JsonWriteInteger('highLayer', Via.HighLayer, True);

    // Additional via properties
    JsonWriteInteger('mode', Via.Mode, True);
    JsonWriteInteger('layer', Via.Layer, True);
    JsonWriteBoolean('plated', Via.Plated, True);
    JsonWriteBoolean('isBackdrill', Via.IsBackdrill, True);
    JsonWriteInteger('drillLayerPairType', Via.DrillLayerPairType, True);
    JsonWriteCoord('height', Via.Height, True);
    JsonWriteCoord('holeNegativeTolerance', Via.HoleNegativeTolerance, True);
    JsonWriteCoord('holePositiveTolerance', Via.HolePositiveTolerance, True);
    JsonWriteBoolean('solderMaskExpansionFromHoleEdge', Via.SolderMaskExpansionFromHoleEdge, True);

    // Net information
    if Via.Net <> nil then
        JsonWriteString('net', Via.Net.Name, True)
    else
        JsonWriteString('net', '', True);

    // Base primitive properties (includes tenting, testpoint, mask, power plane, relief)
    ExportPcbBasePrimitiveProperties(Via);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbConnectionToJson(Conn: IPCB_Connection; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Connection', True);
    JsonWriteCoord('x1', Conn.X1, True);
    JsonWriteCoord('y1', Conn.Y1, True);
    JsonWriteCoord('x2', Conn.X2, True);
    JsonWriteCoord('y2', Conn.Y2, True);
    JsonWriteInteger('layer', Conn.Layer, True);
    JsonWriteInteger('layer1', Conn.Layer1, True);
    JsonWriteInteger('layer2', Conn.Layer2, True);
    JsonWriteInteger('mode', Conn.Mode, True);

    // Net information
    if Conn.Net <> nil then
        JsonWriteString('net', Conn.Net.Name, True)
    else
        JsonWriteString('net', '', True);

    // IPCB_Connection3D extension properties
    JsonWriteInteger('faceIdx1', Conn.FaceIdx1, True);
    JsonWriteInteger('faceIdx2', Conn.FaceIdx2, True);
    JsonWriteInteger('faceU1', Conn.FaceU1, True);
    JsonWriteInteger('faceU2', Conn.FaceU2, True);
    JsonWriteInteger('faceV1', Conn.FaceV1, True);
    JsonWriteInteger('faceV2', Conn.FaceV2, True);

    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Conn);

    JsonCloseObject(AddComma);
end;

procedure ExportPcbComponentToJson(Comp: IPCB_Component; AddComma: Boolean);
var
    GroupIterator: IPCB_GroupIterator;
    Prim: IPCB_Primitive;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Component', True);
    JsonWriteString('name', Comp.Name.Text, True);
    JsonWriteString('pattern', Comp.Pattern, True);
    JsonWriteString('sourceDesignator', Comp.SourceDesignator, True);
    JsonWriteString('comment', Comp.Comment.Text, True);
    JsonWriteCoord('x', Comp.X, True);
    JsonWriteCoord('y', Comp.Y, True);
    JsonWriteFloat('rotation', Comp.Rotation, True);
    JsonWriteInteger('layer', Comp.Layer, True);

    // Additional component properties
    JsonWriteInteger('componentKind', Comp.ComponentKind, True);
    JsonWriteCoord('height', Comp.Height, True);
    JsonWriteBoolean('nameOn', Comp.NameOn, True);
    JsonWriteBoolean('commentOn', Comp.CommentOn, True);
    JsonWriteString('sourceUniqueId', Comp.SourceUniqueId, True);
    JsonWriteString('sourceFootprintLibrary', Comp.SourceFootprintLibrary, True);
    JsonWriteString('sourceComponentLibrary', Comp.SourceComponentLibrary, True);
    JsonWriteString('footprintDescription', Comp.FootprintDescription, True);
    JsonWriteBoolean('isBGA', Comp.IsBGA, True);

    // Extended component properties from API IPCB_Component.csv
    JsonWriteString('defaultPCB3DModel', Comp.DefaultPCB3DModel, True);
    JsonWriteBoolean('enablePartSwapping', Comp.EnablePartSwapping, True);
    JsonWriteBoolean('enablePinSwapping', Comp.EnablePinSwapping, True);
    JsonWriteBoolean('flippedOnLayer', Comp.FlippedOnLayer, True);
    JsonWriteString('footprintConfiguratorName', Comp.FootprintConfiguratorName, True);
    JsonWriteInteger('groupNum', Comp.GroupNum, True);
    JsonWriteString('itemGUID', Comp.ItemGUID, True);
    JsonWriteString('itemRevisionGUID', Comp.ItemRevisionGUID, True);
    JsonWriteBoolean('jumpersVisible', Comp.JumpersVisible, True);
    JsonWriteBoolean('lockStrings', Comp.LockStrings, True);
    JsonWriteString('modelHash', Comp.ModelHash, True);
    JsonWriteInteger('nameAutoPosition', Comp.NameAutoPosition, True);
    JsonWriteInteger('commentAutoPosition', Comp.CommentAutoPosition, True);
    JsonWriteBoolean('primitiveLock', Comp.PrimitiveLock, True);
    JsonWriteString('sourceCompDesignItemID', Comp.SourceCompDesignItemID, True);
    JsonWriteString('sourceHierarchicalPath', Comp.SourceHierarchicalPath, True);
    JsonWriteString('sourceDescription', Comp.SourceDescription, True);
    JsonWriteString('sourceLibReference', Comp.SourceLibReference, True);
    JsonWriteString('vaultGUID', Comp.VaultGUID, True);
    JsonWriteInteger('axisCount', Comp.AxisCount, True);
    JsonWriteInteger('channelOffset', Comp.ChannelOffset, True);

    JsonOpenArray('primitives');

    GroupIterator := Comp.GroupIterator_Create;
    GroupIterator.AddFilter_ObjectSet(MkSet(ePadObject, eTrackObject, eArcObject,
        eTextObject, eFillObject, eRegionObject, ePolyObject, eComponentBodyObject));

    Prim := GroupIterator.FirstPCBObject;
    while Prim <> nil do
    begin
        case Prim.ObjectId of
            ePadObject: ExportPcbPadToJson(Prim, True);
            eTrackObject: ExportPcbTrackToJson(Prim, True);
            eArcObject: ExportPcbArcToJson(Prim, True);
            eTextObject: ExportPcbTextToJson(Prim, True);
            eFillObject: ExportPcbFillToJson(Prim, True);
            eRegionObject: ExportPcbRegionToJson(Prim, True);
            ePolyObject: ExportPcbPolygonToJson(Prim, True);
            eComponentBodyObject: ExportPcbComponentBodyToJson(Prim, True);
        end;
        Prim := GroupIterator.NextPCBObject;
    end;

    Comp.GroupIterator_Destroy(GroupIterator);

    JsonCloseArray(True);


    // Additional IPCB_Component properties
    JsonWriteString('footprintConfigurableParameters_Encoded', Comp.FootprintConfigurableParameters_Encoded, True);
    JsonWriteInteger('fPGADisplayMode', Comp.FPGADisplayMode, True);
    JsonWriteString('packageSpecificHash', Comp.PackageSpecificHash, True);
    // Base primitive properties
    ExportPcbBasePrimitiveProperties(Comp);

    // Indexed/object ref properties
    try
        if Comp.Axis[0] <> nil then
            JsonWriteString('axis_0_ref', 'present', True)
        else
            JsonWriteString('axis_0_ref', '', True);
    except
        JsonWriteString('axis_0_ref', 'ERROR', True);
    end;
    try
        JsonWriteBoolean('layerUsed_top', Comp.LayerUsed[eTopLayer], False);
    except
        JsonWriteString('layerUsed_top', 'ERROR', False);
    end;

    JsonCloseObject(AddComma);
end;

procedure ExportPcbDocToJson(Board: IPCB_Board; JsonPath: String);
var
    Iterator: IPCB_BoardIterator;
    Prim: IPCB_Primitive;
begin
    if Board = nil then Exit;

    JsonBegin;
    JsonOpenObject('');

    JsonOpenObject('metadata');
    JsonWriteString('exportType', 'PcbDoc', True);
    JsonWriteString('fileName', ExtractFileName(Board.FileName), True);
    JsonWriteString('exportedBy', 'AltiumSharp FileToJsonConverter', True);
    JsonWriteString('version', '2.0', False);
    JsonCloseObject(True);

    // Board dimensions
    JsonOpenObject('boardOutline');
    JsonWriteCoord('xOrigin', Board.XOrigin, True);
    JsonWriteCoord('yOrigin', Board.YOrigin, True);
    JsonWriteCoord('width', Board.BoundingRectangle.Right - Board.BoundingRectangle.Left, True);
    JsonWriteCoord('height', Board.BoundingRectangle.Top - Board.BoundingRectangle.Bottom, False);
    JsonCloseObject(True);

    // Extended board properties from API IPCB_Board.csv
    JsonOpenObject('boardProperties');
    JsonWriteBoolean('automaticSplitPlanes', Board.AutomaticSplitPlanes, True);
    JsonWriteFloat('bigVisibleGridSize', Board.BigVisibleGridSize, True);
    JsonWriteInteger('bigVisibleGridUnit', Board.BigVisibleGridUnit, True);
    JsonWriteInteger('boardID', Board.BoardID, True);
    JsonWriteFloat('boardVersion', Board.BoardVersion, True);
    JsonWriteFloat('componentGridSize', Board.ComponentGridSize, True);
    JsonWriteFloat('componentGridSizeX', Board.ComponentGridSizeX, True);
    JsonWriteFloat('componentGridSizeY', Board.ComponentGridSizeY, True);
    JsonWriteInteger('currentLayer', Board.CurrentLayer, True);
    JsonWriteInteger('layer', Board.Layer, True);
    JsonWriteInteger('objectId', Board.ObjectId, True);
    JsonWriteInteger('displayUnit', Board.DisplayUnit, True);
    JsonWriteInteger('drillLayerPairsCount', Board.DrillLayerPairsCount, True);
    JsonWriteString('internalPlane1NetName', Board.InternalPlane1NetName, True);
    JsonWriteString('internalPlane2NetName', Board.InternalPlane2NetName, True);
    JsonWriteString('internalPlane3NetName', Board.InternalPlane3NetName, True);
    JsonWriteString('internalPlane4NetName', Board.InternalPlane4NetName, False);
    JsonCloseObject(True);

    // Board sub-object properties
    JsonOpenObject('routingOptions');
    try
        JsonWriteBoolean('showSignalLayersOnly', Board.RoutingOptions.ShowSignalLayersOnly, False);
    except
        JsonWriteString('error', 'Could not read RoutingOptions', False);
    end;
    JsonCloseObject(True);

    JsonOpenObject('designVariants');
    try
        JsonWriteInteger('count', Board.DesignVariants.Count, False);
    except
        JsonWriteString('error', 'Could not read DesignVariants', False);
    end;
    JsonCloseObject(True);

    // Additional Board sub-object properties
    JsonOpenObject('boardLayerSetManager');
    try
        JsonWriteInteger('allSetsCount', Board.BoardLayerSetManager.AllSetsCount, False);
    except
        JsonWriteString('error', 'Could not read BoardLayerSetManager', False);
    end;
    JsonCloseObject(True);

    // BoardEx/Ex2/Ex3 extension properties
    JsonOpenObject('boardExObjectRefs');
    try
        if Board.FullComponents <> nil then
            JsonWriteString('fullComponents_ref', 'present', True)
        else
            JsonWriteString('fullComponents_ref', '', True);
    except
        JsonWriteString('fullComponents_ref', 'ERROR', True);
    end;
    try
        if Board.ViaManager <> nil then
            JsonWriteString('viaManager_ref', 'present', True)
        else
            JsonWriteString('viaManager_ref', '', True);
    except
        JsonWriteString('viaManager_ref', 'ERROR', True);
    end;
    try
        if Board.ViaStructureManager <> nil then
            JsonWriteString('viaStructureManager_ref', 'present', True)
        else
            JsonWriteString('viaStructureManager_ref', '', True);
    except
        JsonWriteString('viaStructureManager_ref', 'ERROR', True);
    end;
    try
        if Board.DrillLayerPairManager <> nil then
            JsonWriteString('drillLayerPairManager_ref', 'present', True)
        else
            JsonWriteString('drillLayerPairManager_ref', '', True);
    except
        JsonWriteString('drillLayerPairManager_ref', 'ERROR', True);
    end;
    try
        if Board.SilkscreenClipperSettings <> nil then
            JsonWriteString('silkscreenClipperSettings_ref', 'present', False)
        else
            JsonWriteString('silkscreenClipperSettings_ref', '', False);
    except
        JsonWriteString('silkscreenClipperSettings_ref', 'ERROR', False);
    end;
    JsonCloseObject(True);


    // Board object reference properties (IPCB_Board / IPCB_Board3D)
    JsonOpenObject('boardObjectRefs');
    try
        if Board.Board <> nil then
            JsonWriteString('board_ref', 'present', True)
        else
            JsonWriteString('board_ref', '', True);
    except
        JsonWriteString('board_ref', 'ERROR', True);
    end;
    try
        if Board.Component <> nil then
            JsonWriteString('component_ref', 'present', True)
        else
            JsonWriteString('component_ref', '', True);
    except
        JsonWriteString('component_ref', 'ERROR', True);
    end;
    try
        if Board.Coordinate <> nil then
            JsonWriteString('coordinate_ref', 'present', True)
        else
            JsonWriteString('coordinate_ref', '', True);
    except
        JsonWriteString('coordinate_ref', 'ERROR', True);
    end;
    try
        if Board.Dimension <> nil then
            JsonWriteString('dimension_ref', 'present', True)
        else
            JsonWriteString('dimension_ref', '', True);
    except
        JsonWriteString('dimension_ref', 'ERROR', True);
    end;
    try
        if Board.ECOOptions <> nil then
            JsonWriteString('ecoOptions_ref', 'present', True)
        else
            JsonWriteString('ecoOptions_ref', '', True);
    except
        JsonWriteString('ecoOptions_ref', 'ERROR', True);
    end;
    try
        if Board.LayerStack_V7 <> nil then
            JsonWriteString('layerStack_V7_ref', 'present', True)
        else
            JsonWriteString('layerStack_V7_ref', '', True);
    except
        JsonWriteString('layerStack_V7_ref', 'ERROR', True);
    end;
    try
        if Board.MasterLayerStack <> nil then
            JsonWriteString('masterLayerStack_ref', 'present', True)
        else
            JsonWriteString('masterLayerStack_ref', '', True);
    except
        JsonWriteString('masterLayerStack_ref', 'ERROR', True);
    end;
    try
        if Board.MechanicalPairs <> nil then
            JsonWriteString('mechanicalPairs_ref', 'present', True)
        else
            JsonWriteString('mechanicalPairs_ref', '', True);
    except
        JsonWriteString('mechanicalPairs_ref', 'ERROR', True);
    end;
    try
        if Board.Net <> nil then
            JsonWriteString('net_ref', 'present', True)
        else
            JsonWriteString('net_ref', '', True);
    except
        JsonWriteString('net_ref', 'ERROR', True);
    end;
    try
        if Board.OutputOptions <> nil then
            JsonWriteString('outputOptions_ref', 'present', True)
        else
            JsonWriteString('outputOptions_ref', '', True);
    except
        JsonWriteString('outputOptions_ref', 'ERROR', True);
    end;
    try
        if Board.PadViaCache <> nil then
            JsonWriteString('padViaCache_ref', 'present', True)
        else
            JsonWriteString('padViaCache_ref', '', True);
    except
        JsonWriteString('padViaCache_ref', 'ERROR', True);
    end;
    try
        if Board.PadViaLibrary <> nil then
            JsonWriteString('padViaLibrary_ref', 'present', True)
        else
            JsonWriteString('padViaLibrary_ref', '', True);
    except
        JsonWriteString('padViaLibrary_ref', 'ERROR', True);
    end;
    try
        if Board.PCB3DMovieManager <> nil then
            JsonWriteString('pcb3DMovieManager_ref', 'present', True)
        else
            JsonWriteString('pcb3DMovieManager_ref', '', True);
    except
        JsonWriteString('pcb3DMovieManager_ref', 'ERROR', True);
    end;
    try
        if Board.PCBSheet <> nil then
            JsonWriteString('pcbSheet_ref', 'present', True)
        else
            JsonWriteString('pcbSheet_ref', '', True);
    except
        JsonWriteString('pcbSheet_ref', 'ERROR', True);
    end;
    try
        if Board.PinPairsManager <> nil then
            JsonWriteString('pinPairsManager_ref', 'present', True)
        else
            JsonWriteString('pinPairsManager_ref', '', True);
    except
        JsonWriteString('pinPairsManager_ref', 'ERROR', True);
    end;
    try
        if Board.PlacerOptions <> nil then
            JsonWriteString('placerOptions_ref', 'present', True)
        else
            JsonWriteString('placerOptions_ref', '', True);
    except
        JsonWriteString('placerOptions_ref', 'ERROR', True);
    end;
    try
        if Board.Polygon <> nil then
            JsonWriteString('polygon_ref', 'present', True)
        else
            JsonWriteString('polygon_ref', '', True);
    except
        JsonWriteString('polygon_ref', 'ERROR', True);
    end;
    try
        if Board.PrimitiveCounter <> nil then
            JsonWriteString('primitiveCounter_ref', 'present', True)
        else
            JsonWriteString('primitiveCounter_ref', '', True);
    except
        JsonWriteString('primitiveCounter_ref', 'ERROR', True);
    end;
    try
        if Board.PrinterOptions <> nil then
            JsonWriteString('printerOptions_ref', 'present', True)
        else
            JsonWriteString('printerOptions_ref', '', True);
    except
        JsonWriteString('printerOptions_ref', 'ERROR', True);
    end;
    try
        if Board.RouteBody <> nil then
            JsonWriteString('routeBody_ref', 'present', True)
        else
            JsonWriteString('routeBody_ref', '', True);
    except
        JsonWriteString('routeBody_ref', 'ERROR', True);
    end;
    try
        if Board.RouteCore <> nil then
            JsonWriteString('routeCore_ref', 'present', True)
        else
            JsonWriteString('routeCore_ref', '', True);
    except
        JsonWriteString('routeCore_ref', 'ERROR', True);
    end;
    try
        if Board.Viewport <> nil then
            JsonWriteString('viewport_ref', 'present', True)
        else
            JsonWriteString('viewport_ref', '', True);
    except
        JsonWriteString('viewport_ref', 'ERROR', True);
    end;
    JsonCloseObject(True);

    // Board indexed properties
    JsonOpenObject('boardIndexedProps');
    try
        JsonWriteBoolean('inSelectionMemory_0', Board.InSelectionMemory[0], True);
    except
        JsonWriteString('inSelectionMemory_0', 'ERROR', True);
    end;
    try
        JsonWriteString('internalPlaneNetName_1', Board.InternalPlaneNetName[1], True);
    except
        JsonWriteString('internalPlaneNetName_1', 'ERROR', True);
    end;
    try
        JsonWriteInteger('layerColor_top', Board.LayerColor[eTopLayer], True);
    except
        JsonWriteString('layerColor_top', 'ERROR', True);
    end;
    try
        JsonWriteBoolean('layerIsDisplayed_top', Board.LayerIsDisplayed[eTopLayer], True);
    except
        JsonWriteString('layerIsDisplayed_top', 'ERROR', True);
    end;
    try
        JsonWriteBoolean('layerIsUsed_top', Board.LayerIsUsed[eTopLayer], True);
    except
        JsonWriteString('layerIsUsed_top', 'ERROR', True);
    end;
    try
        if Board.LayerPair[0] <> nil then
            JsonWriteString('layerPair_0_ref', 'present', True)
        else
            JsonWriteString('layerPair_0_ref', '', True);
    except
        JsonWriteString('layerPair_0_ref', 'ERROR', True);
    end;
    JsonCloseObject(True);

    JsonOpenArray('objects');

    Iterator := Board.BoardIterator_Create;
    Iterator.AddFilter_ObjectSet(MkSet(eComponentObject, ePadObject, eTrackObject,
        eArcObject, eTextObject, eFillObject, eRegionObject, eViaObject,
        ePolyObject, eDimensionObject, eCoordinateObject, eConnectionObject,
        eComponentBodyObject, eEmbeddedBoardObject, eEmbeddedObject));
    Iterator.AddFilter_LayerSet(AllLayers);
    Iterator.AddFilter_Method(eProcessAll);

    Prim := Iterator.FirstPCBObject;
    while Prim <> nil do
    begin
        case Prim.ObjectId of
            eComponentObject: ExportPcbComponentToJson(Prim, True);
            ePadObject: ExportPcbPadToJson(Prim, True);
            eTrackObject: ExportPcbTrackToJson(Prim, True);
            eArcObject: ExportPcbArcToJson(Prim, True);
            eTextObject: ExportPcbTextToJson(Prim, True);
            eFillObject: ExportPcbFillToJson(Prim, True);
            eRegionObject: ExportPcbRegionToJson(Prim, True);
            eViaObject: ExportPcbViaToJson(Prim, True);
            ePolyObject: ExportPcbPolygonToJson(Prim, True);
            eDimensionObject: ExportPcbDimensionToJson(Prim, True);
            eCoordinateObject: ExportPcbCoordinateToJson(Prim, True);
            eConnectionObject: ExportPcbConnectionToJson(Prim, True);
            eComponentBodyObject: ExportPcbComponentBodyToJson(Prim, True);
            eEmbeddedBoardObject: ExportPcbEmbeddedBoardToJson(Prim, True);
            eEmbeddedObject: ExportPcbEmbeddedToJson(Prim, True);
        end;
        Prim := Iterator.NextPCBObject;
    end;

    Board.BoardIterator_Destroy(Iterator);

    JsonCloseArray(False);
    JsonCloseObject(False);

    JsonEnd(JsonPath);
end;

{==============================================================================
  ENTRY POINTS
==============================================================================}

// Export currently open PCB Library to JSON
procedure RunExportCurrentPcbLib;
var
    PCBLib: IPCB_Library;
    Board: IPCB_Board;
    JsonPath: String;
begin
    // Try to get current PCB library
    PCBLib := PCBServer.GetCurrentPCBLibrary;
    if PCBLib <> nil then
    begin
        JsonPath := ChangeFileExt(PCBLib.Board.FileName, '.json');
        ExportPcbLibToJson(PCBLib, JsonPath);
        ShowMessage('Exported to: ' + JsonPath);
        Exit;
    end;

    // Also try via current board
    Board := PCBServer.GetCurrentPCBBoard;
    if Board <> nil then
    begin
        if Board.IsLibrary then
        begin
            PCBLib := PCBServer.GetCurrentPCBLibrary;
            if PCBLib <> nil then
            begin
                JsonPath := ChangeFileExt(Board.FileName, '.json');
                ExportPcbLibToJson(PCBLib, JsonPath);
                ShowMessage('Exported to: ' + JsonPath);
                Exit;
            end;
        end;
    end;

    ShowMessage('No PCB Library is currently open. Please open a .PcbLib file first.');
end;

// Export currently open Schematic Library to JSON
procedure RunExportCurrentSchLib;
var
    CurrentDoc: ISch_Document;
    SchLib: ISch_Lib;
    JsonPath: String;
begin
    // Get current schematic document
    CurrentDoc := SchServer.GetCurrentSchDocument;
    if CurrentDoc = nil then
    begin
        ShowMessage('No Schematic document is currently open. Please open a .SchLib file first.');
        Exit;
    end;

    // Check if it's a schematic library (ObjectId = eSchLib)
    if CurrentDoc.ObjectId <> eSchLib then
    begin
        ShowMessage('Current document is not a Schematic Library. Please open a .SchLib file.' + #13#10 +
                    'Current document type: ' + IntToStr(CurrentDoc.ObjectId));
        Exit;
    end;

    // Cast to ISch_Lib
    SchLib := CurrentDoc;
    if SchLib = nil then
    begin
        ShowMessage('Failed to access Schematic Library interface.');
        Exit;
    end;

    JsonPath := ChangeFileExt(SchLib.DocumentName, '.json');
    ExportSchLibToJson(SchLib, JsonPath);
    ShowMessage('Exported to: ' + JsonPath);
end;

// Export currently open Schematic Document to JSON
procedure RunExportCurrentSchDoc;
var
    CurrentDoc: ISch_Document;
    JsonPath: String;
begin
    // Get current schematic document
    CurrentDoc := SchServer.GetCurrentSchDocument;
    if CurrentDoc = nil then
    begin
        ShowMessage('No Schematic document is currently open. Please open a .SchDoc file first.');
        Exit;
    end;

    // Check if it's a schematic document (not a library)
    if CurrentDoc.ObjectId = eSchLib then
    begin
        ShowMessage('Current document is a Schematic Library, not a Schematic Document.' + #13#10 +
                    'Use RunExportCurrentSchLib for .SchLib files.');
        Exit;
    end;

    JsonPath := ChangeFileExt(CurrentDoc.DocumentName, '.json');
    ExportSchDocToJson(CurrentDoc, JsonPath);
    ShowMessage('Exported to: ' + JsonPath);
end;

// Export currently open PCB Document to JSON
procedure RunExportCurrentPcbDoc;
var
    Board: IPCB_Board;
    JsonPath: String;
begin
    // Get current PCB board
    Board := PCBServer.GetCurrentPCBBoard;
    if Board = nil then
    begin
        ShowMessage('No PCB document is currently open. Please open a .PcbDoc file first.');
        Exit;
    end;

    // Check if it's a board (not a library)
    if Board.IsLibrary then
    begin
        ShowMessage('Current document is a PCB Library, not a PCB Document.' + #13#10 +
                    'Use RunExportCurrentPcbLib for .PcbLib files.');
        Exit;
    end;

    JsonPath := ChangeFileExt(Board.FileName, '.json');
    ExportPcbDocToJson(Board, JsonPath);
    ShowMessage('Exported to: ' + JsonPath);
end;

{------------------------------------------------------------------------------
  Helper: Open a file from disk using WorkspaceManager:OpenObject
  This method works for standalone files that are not part of a project
------------------------------------------------------------------------------}
function OpenFileFromDisk(Kind: String; FilePath: String): Boolean;
begin
    Result := False;
    if not FileExists(FilePath) then Exit;

    ResetParameters;
    AddStringParameter('Kind', Kind);
    AddStringParameter('FileName', FilePath);
    RunProcess('WorkspaceManager:OpenObject');

    Result := True;
end;

{------------------------------------------------------------------------------
  Helper: Close the currently focused document
------------------------------------------------------------------------------}
procedure CloseCurrentDocument;
begin
    ResetParameters;
    AddStringParameter('ObjectKind', 'FocusedDocument');
    RunProcess('WorkspaceManager:CloseObject');
end;

// Batch convert all files in TestData directory
procedure RunConvertAllInDirectory;
var
    FilesList: TStringList;
    I: Integer;
    FilePath, JsonPath: String;
    PCBLib: IPCB_Library;
    CurrentDoc: ISch_Document;
    SchLib: ISch_Lib;
    ConvertedCount, SkippedCount: Integer;
    StatusMsg: String;
begin
    ConvertedCount := 0;
    SkippedCount := 0;
    StatusMsg := 'Source: ' + SOURCE_DIR + #13#10#13#10;

    // Check if source directory exists
    if not DirectoryExists(SOURCE_DIR) then
    begin
        ShowMessage('Source directory not found: ' + SOURCE_DIR);
        Exit;
    end;

    FilesList := TStringList.Create;
    try
        // Convert all PcbLib files
        FilesList.Clear;
        FindFiles(SOURCE_DIR, '*.PcbLib', faAnyFile, False, FilesList);
        StatusMsg := StatusMsg + 'Found ' + IntToStr(FilesList.Count) + ' PcbLib files' + #13#10;

        for I := 0 to FilesList.Count - 1 do
        begin
            FilePath := FilesList.Strings[I];
            JsonPath := ChangeFileExt(FilePath, '.json');

            try
                if OpenFileFromDisk('PCBLIB', FilePath) then
                begin
                    PCBLib := PCBServer.GetCurrentPCBLibrary;
                    if PCBLib <> nil then
                    begin
                        ExportPcbLibToJson(PCBLib, JsonPath);
                        Inc(ConvertedCount);
                        StatusMsg := StatusMsg + 'OK: ' + ExtractFileName(FilePath) + #13#10;
                    end
                    else
                    begin
                        Inc(SkippedCount);
                        StatusMsg := StatusMsg + 'SKIP (no lib interface): ' + ExtractFileName(FilePath) + #13#10;
                    end;
                    CloseCurrentDocument;
                end
                else
                begin
                    Inc(SkippedCount);
                    StatusMsg := StatusMsg + 'SKIP (file not found): ' + ExtractFileName(FilePath) + #13#10;
                end;
            except
                Inc(SkippedCount);
                StatusMsg := StatusMsg + 'ERROR: ' + ExtractFileName(FilePath) + #13#10;
                try CloseCurrentDocument; except end;
            end;
        end;

        // Convert all SchLib files
        FilesList.Clear;
        FindFiles(SOURCE_DIR, '*.SchLib', faAnyFile, False, FilesList);
        StatusMsg := StatusMsg + #13#10 + 'Found ' + IntToStr(FilesList.Count) + ' SchLib files' + #13#10;

        for I := 0 to FilesList.Count - 1 do
        begin
            FilePath := FilesList.Strings[I];
            JsonPath := ChangeFileExt(FilePath, '.json');

            try
                if OpenFileFromDisk('SCHLIB', FilePath) then
                begin
                    CurrentDoc := SchServer.GetCurrentSchDocument;
                    if (CurrentDoc <> nil) and (CurrentDoc.ObjectId = eSchLib) then
                    begin
                        SchLib := CurrentDoc;
                        ExportSchLibToJson(SchLib, JsonPath);
                        Inc(ConvertedCount);
                        StatusMsg := StatusMsg + 'OK: ' + ExtractFileName(FilePath) + #13#10;
                    end
                    else
                    begin
                        Inc(SkippedCount);
                        StatusMsg := StatusMsg + 'SKIP (not schlib): ' + ExtractFileName(FilePath) + #13#10;
                    end;
                    CloseCurrentDocument;
                end
                else
                begin
                    Inc(SkippedCount);
                    StatusMsg := StatusMsg + 'SKIP (file not found): ' + ExtractFileName(FilePath) + #13#10;
                end;
            except
                Inc(SkippedCount);
                StatusMsg := StatusMsg + 'ERROR: ' + ExtractFileName(FilePath) + #13#10;
                try CloseCurrentDocument; except end;
            end;
        end;

        // Convert all SchDoc files
        FilesList.Clear;
        FindFiles(SOURCE_DIR, '*.SchDoc', faAnyFile, False, FilesList);
        StatusMsg := StatusMsg + #13#10 + 'Found ' + IntToStr(FilesList.Count) + ' SchDoc files' + #13#10;

        for I := 0 to FilesList.Count - 1 do
        begin
            FilePath := FilesList.Strings[I];
            JsonPath := ChangeFileExt(FilePath, '.json');

            try
                if OpenFileFromDisk('SCH', FilePath) then
                begin
                    CurrentDoc := SchServer.GetCurrentSchDocument;
                    if CurrentDoc <> nil then
                    begin
                        ExportSchDocToJson(CurrentDoc, JsonPath);
                        Inc(ConvertedCount);
                        StatusMsg := StatusMsg + 'OK: ' + ExtractFileName(FilePath) + #13#10;
                    end
                    else
                    begin
                        Inc(SkippedCount);
                        StatusMsg := StatusMsg + 'SKIP (no doc interface): ' + ExtractFileName(FilePath) + #13#10;
                    end;
                    CloseCurrentDocument;
                end
                else
                begin
                    Inc(SkippedCount);
                    StatusMsg := StatusMsg + 'SKIP (file not found): ' + ExtractFileName(FilePath) + #13#10;
                end;
            except
                Inc(SkippedCount);
                StatusMsg := StatusMsg + 'ERROR: ' + ExtractFileName(FilePath) + #13#10;
                try CloseCurrentDocument; except end;
            end;
        end;

        // Convert all PcbDoc files
        FilesList.Clear;
        FindFiles(SOURCE_DIR, '*.PcbDoc', faAnyFile, False, FilesList);
        StatusMsg := StatusMsg + #13#10 + 'Found ' + IntToStr(FilesList.Count) + ' PcbDoc files' + #13#10;

        for I := 0 to FilesList.Count - 1 do
        begin
            FilePath := FilesList.Strings[I];
            JsonPath := ChangeFileExt(FilePath, '.json');

            try
                if OpenFileFromDisk('PCB', FilePath) then
                begin
                    // For PcbDoc, we need to get the board
                    if PCBServer.GetCurrentPCBBoard <> nil then
                    begin
                        ExportPcbDocToJson(PCBServer.GetCurrentPCBBoard, JsonPath);
                        Inc(ConvertedCount);
                        StatusMsg := StatusMsg + 'OK: ' + ExtractFileName(FilePath) + #13#10;
                    end
                    else
                    begin
                        Inc(SkippedCount);
                        StatusMsg := StatusMsg + 'SKIP (no board interface): ' + ExtractFileName(FilePath) + #13#10;
                    end;
                    CloseCurrentDocument;
                end
                else
                begin
                    Inc(SkippedCount);
                    StatusMsg := StatusMsg + 'SKIP (file not found): ' + ExtractFileName(FilePath) + #13#10;
                end;
            except
                Inc(SkippedCount);
                StatusMsg := StatusMsg + 'ERROR: ' + ExtractFileName(FilePath) + #13#10;
                try CloseCurrentDocument; except end;
            end;
        end;

    finally
        FilesList.Free;
    end;

    ShowMessage('Converted: ' + IntToStr(ConvertedCount) + ', Skipped: ' + IntToStr(SkippedCount) + #13#10#13#10 + StatusMsg);
end;

// Export all open documents (all open tabs, regardless of project)
procedure RunExportAllOpenDocuments;
var
    ServerModule: IServerModule;
    ServerDoc: IServerDocument;
    I: Integer;
    PCBLib: IPCB_Library;
    CurrentSchDoc: ISch_Document;
    SchLib: ISch_Lib;
    JsonPath, FileName: String;
    ConvertedCount: Integer;
    StatusMsg: String;
begin
    ConvertedCount := 0;
    StatusMsg := '';

    // Export all open PcbLib documents
    ServerModule := Client.ServerModuleByName('PCBLib');
    if ServerModule <> nil then
    begin
        StatusMsg := StatusMsg + 'PCBLib Server: ' + IntToStr(ServerModule.DocumentCount) + ' open documents' + #13#10;
        for I := 0 to ServerModule.DocumentCount - 1 do
        begin
            ServerDoc := ServerModule.Documents(I);
            if ServerDoc = nil then Continue;

            FileName := ServerDoc.FileName;
            StatusMsg := StatusMsg + '  ' + ExtractFileName(FileName) + #13#10;

            Client.ShowDocument(ServerDoc);
            PCBLib := PCBServer.GetCurrentPCBLibrary;
            if PCBLib <> nil then
            begin
                JsonPath := ChangeFileExt(FileName, '.json');
                ExportPcbLibToJson(PCBLib, JsonPath);
                Inc(ConvertedCount);
                StatusMsg := StatusMsg + '    -> Exported' + #13#10;
            end
            else
                StatusMsg := StatusMsg + '    -> No library interface' + #13#10;
        end;
    end
    else
        StatusMsg := StatusMsg + 'PCBLib Server: Not available' + #13#10;

    StatusMsg := StatusMsg + #13#10;

    // Export all open SchLib documents
    ServerModule := Client.ServerModuleByName('SchLib');
    if ServerModule <> nil then
    begin
        StatusMsg := StatusMsg + 'SchLib Server: ' + IntToStr(ServerModule.DocumentCount) + ' open documents' + #13#10;
        for I := 0 to ServerModule.DocumentCount - 1 do
        begin
            ServerDoc := ServerModule.Documents(I);
            if ServerDoc = nil then Continue;

            FileName := ServerDoc.FileName;
            StatusMsg := StatusMsg + '  ' + ExtractFileName(FileName) + #13#10;

            Client.ShowDocument(ServerDoc);
            CurrentSchDoc := SchServer.GetCurrentSchDocument;
            if (CurrentSchDoc <> nil) and (CurrentSchDoc.ObjectId = eSchLib) then
            begin
                SchLib := CurrentSchDoc;
                JsonPath := ChangeFileExt(FileName, '.json');
                ExportSchLibToJson(SchLib, JsonPath);
                Inc(ConvertedCount);
                StatusMsg := StatusMsg + '    -> Exported' + #13#10;
            end
            else
                StatusMsg := StatusMsg + '    -> No library interface' + #13#10;
        end;
    end
    else
        StatusMsg := StatusMsg + 'SchLib Server: Not available' + #13#10;

    ShowMessage('Converted ' + IntToStr(ConvertedCount) + ' documents.' + #13#10#13#10 + StatusMsg);
end;

// Debug: Show info about current document
procedure RunShowCurrentDocInfo;
var
    PCBLib: IPCB_Library;
    Board: IPCB_Board;
    SchDoc: ISch_Document;
    Workspace: IWorkspace;
    Project: IProject;
    Msg: String;
begin
    Msg := 'Current Document Info:' + #13#10#13#10;

    // PCB info
    Msg := Msg + '=== PCB Server ===' + #13#10;
    Board := PCBServer.GetCurrentPCBBoard;
    if Board <> nil then
    begin
        Msg := Msg + 'Current Board: ' + Board.FileName + #13#10;
        Msg := Msg + 'IsLibrary: ';
        if Board.IsLibrary then Msg := Msg + 'True' else Msg := Msg + 'False';
        Msg := Msg + #13#10;
    end
    else
        Msg := Msg + 'No current PCB board' + #13#10;

    PCBLib := PCBServer.GetCurrentPCBLibrary;
    if PCBLib <> nil then
        Msg := Msg + 'Current PCB Library: Yes' + #13#10
    else
        Msg := Msg + 'Current PCB Library: No' + #13#10;

    // Schematic info
    Msg := Msg + #13#10 + '=== Schematic Server ===' + #13#10;
    SchDoc := SchServer.GetCurrentSchDocument;
    if SchDoc <> nil then
    begin
        Msg := Msg + 'Current Sch Document: ' + SchDoc.DocumentName + #13#10;
        Msg := Msg + 'ObjectId: ' + IntToStr(SchDoc.ObjectId) + #13#10;
        Msg := Msg + 'eSchLib constant: ' + IntToStr(eSchLib) + #13#10;
        if SchDoc.ObjectId = eSchLib then
            Msg := Msg + 'Is SchLib: True' + #13#10
        else
            Msg := Msg + 'Is SchLib: False' + #13#10;
    end
    else
        Msg := Msg + 'No current schematic document' + #13#10;

    // Workspace info
    Msg := Msg + #13#10 + '=== Workspace ===' + #13#10;
    Workspace := GetWorkspace;
    if Workspace <> nil then
    begin
        Project := Workspace.DM_FocusedProject;
        if Project <> nil then
        begin
            Msg := Msg + 'Focused Project: ' + Project.DM_ProjectFileName + #13#10;
            Msg := Msg + 'Document Count: ' + IntToStr(Project.DM_LogicalDocumentCount) + #13#10;
        end
        else
            Msg := Msg + 'No focused project' + #13#10;
    end
    else
        Msg := Msg + 'No workspace' + #13#10;

    ShowMessage(Msg);
end;

end.
