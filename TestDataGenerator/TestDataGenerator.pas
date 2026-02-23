{==============================================================================
  AltiumSharp Test Data Generator

  Master script for generating comprehensive test files for AltiumSharp
  validation and reverse engineering.

  Creates standalone:
  - PcbLib: PCB footprint library with all primitive types
  - SchLib: Schematic symbol library with all primitive types
  - PcbDoc: PCB document with all object types and nets
  - SchDoc: Schematic document with components and connections

  Each file is accompanied by a JSON export containing all entity data
  extracted directly from Altium's object model.

  Usage:
  - Run GenerateAllTestData to create everything
  - Or run individual procedures for specific file types
==============================================================================}

const
    // Output directory for generated test files
    OUTPUT_DIR = 'D:\src\AltiumSharp\TestData\Generated\';
    // STEP files directory (relative to script location)
    STEP_DIR = 'D:\src\AltiumSharp\TestDataGenerator\step\';

    // Coordinate conversion: 10,000 internal units = 1 mil
    MILS_TO_INTERNAL = 10000;
    MM_TO_INTERNAL = 393701;  // 10000000 / 25.4

    // JSON formatting
    JSON_INDENT = '  ';

var
    JsonOutput: TStringList;
    IndentLevel: Integer;

{ Forward declarations }
procedure ExportPcbLibToJson(PCBLib: IPCB_Library; JsonPath: String); forward;
procedure ExportSchLibToJson(SchLib: ISch_Lib; JsonPath: String); forward;
procedure ExportPcbDocToJson(Board: IPCB_Board; JsonPath: String); forward;
procedure ExportSchDocToJson(SchDoc: ISch_Document; JsonPath: String); forward;
procedure ExportHarnessDocToJson(HarDoc: ISch_HarnessDocument; JsonPath: String); forward;
procedure GenerateHarnessDocTestFile; forward;
procedure RemoveDefaultSchLibComponent(SchLib: ISch_Lib); forward;
procedure CreateSchPin(Comp: ISch_Component; Name, Designator: String;
    X, Y: Integer; Orientation: TRotationBy90; Electrical: TPinElectrical); forward;
procedure CreateSchPinEx(Comp: ISch_Component; Name, Designator: String;
    X, Y: Integer; Orientation: TRotationBy90; Electrical: TPinElectrical;
    IsHidden: Boolean); forward;
procedure CreateSchPinWithSymbol(Comp: ISch_Component; Name, Designator: String;
    X, Y: Integer; Orientation: TRotationBy90; Electrical: TPinElectrical;
    Symbol: TPinSymbol); forward;
procedure CreateSchTriangle(Owner: ISch_BasicContainer;
    X1, Y1, X2, Y2, X3, Y3: Integer; IsSolid: Boolean); forward;
procedure CreateSchLabel(Owner: ISch_BasicContainer; X, Y: Integer;
    Text: String; FontID: TFontID); forward;
procedure CreateSchLine(Owner: ISch_BasicContainer; X1, Y1, X2, Y2: Integer;
    LineWidth: TSize); forward;
procedure CreateSchRectangle(Owner: ISch_BasicContainer; X1, Y1, X2, Y2: Integer;
    LineWidth: TSize; IsSolid: Boolean); forward;
procedure CreateSchArc(Owner: ISch_BasicContainer; X, Y, Radius: Integer;
    StartAngle, EndAngle: Double; LineWidth: TSize); forward;
procedure CreateSchEllipse(Owner: ISch_BasicContainer; X, Y, RadiusX, RadiusY: Integer;
    LineWidth: TSize; IsSolid: Boolean); forward;
procedure CreateSchCircle(Owner: ISch_BasicContainer; X, Y, Radius: Integer;
    LineWidth: TSize; IsSolid: Boolean); forward;
procedure CreateSchRoundRectangle(Owner: ISch_BasicContainer; X1, Y1, X2, Y2: Integer;
    CornerXRadius, CornerYRadius: Integer; LineWidth: TSize; IsSolid: Boolean); forward;
procedure CreateSchTextFrame(Owner: ISch_BasicContainer; X1, Y1, X2, Y2: Integer;
    Text: String; FontID: TFontID; ShowBorder: Boolean); forward;
procedure CreateSchLineStyled(Owner: ISch_BasicContainer; X1, Y1, X2, Y2: Integer;
    LineWidth: TSize; LineStyle: TLineStyle; Color: TColor); forward;
procedure CreateSchRectFull(Owner: ISch_BasicContainer; X1, Y1, X2, Y2: Integer;
    LineWidth: TSize; LineStyle: TLineStyle; IsSolid, Transparent: Boolean;
    Color, AreaColor: TColor); forward;
procedure CreateSchRoundRectFull(Owner: ISch_BasicContainer; X1, Y1, X2, Y2: Integer;
    CornerXRadius, CornerYRadius: Integer; LineWidth: TSize; LineStyle: TLineStyle;
    IsSolid, Transparent: Boolean; Color, AreaColor: TColor); forward;
procedure CreateSchTextFrameFull(Owner: ISch_BasicContainer; X1, Y1, X2, Y2: Integer;
    Text: String; FontID: TFontID; Alignment: THorizontalAlign;
    ShowBorder, IsSolid, Transparent, ClipToRect, WordWrap: Boolean;
    Color, AreaColor, TextColor: TColor; LineWidth: TSize; LineStyle: TLineStyle;
    TextMargin: Integer); forward;
procedure CreateSchLabelFull(Owner: ISch_BasicContainer; X, Y: Integer;
    Text: String; FontID: TFontID; Color: TColor; Orientation: TRotationBy90;
    Justification: TTextJustification; IsMirrored: Boolean); forward;
procedure CreateSchArcStyled(Owner: ISch_BasicContainer; X, Y, Radius: Integer;
    StartAngle, EndAngle: Double; LineWidth: TSize; Color: TColor); forward;
procedure CreateSchEllipseFull(Owner: ISch_BasicContainer; X, Y, RadiusX, RadiusY: Integer;
    LineWidth: TSize; IsSolid, Transparent: Boolean; Color, AreaColor: TColor); forward;
procedure CreateSchPolylineStyled(Owner: ISch_BasicContainer;
    X1, Y1, X2, Y2, X3, Y3, X4, Y4: Integer;
    LineWidth: TSize; LineStyle: TLineStyle; Color: TColor;
    StartShape, EndShape: TLineShape; ShapeSize: TSize); forward;
procedure CreateSchPolygonFull(Owner: ISch_BasicContainer;
    X1, Y1, X2, Y2, X3, Y3: Integer; IsSolid: Boolean; Color, AreaColor: TColor); forward;
procedure CreateSchPieFull(Owner: ISch_BasicContainer; X, Y, Radius: Integer;
    StartAngle, EndAngle: Double; LineWidth: TSize; IsSolid: Boolean;
    Color, AreaColor: TColor); forward;
procedure CreateSchPinFull(Comp: ISch_Component; Name, Designator: String;
    X, Y: Integer; Orientation: TRotationBy90; Electrical: TPinElectrical;
    PinLength: Integer; IsHidden: Boolean; ShowName, ShowDesignator: Boolean;
    NameColor, DesignatorColor: TColor; NameFontID, DesignatorFontID: TFontID;
    SymbolInner, SymbolOuter: TIeeeSymbol; SymbolLineWidth: TSize;
    HiddenNetName, Description, DefaultValue: String); forward;
procedure CreateSchEllipticalArc(Owner: ISch_BasicContainer; X, Y, RadiusX, RadiusY: Integer;
    StartAngle, EndAngle: Double; LineWidth: TSize; Color: TColor); forward;
procedure CreateSchPolyline4(Owner: ISch_BasicContainer;
    X1, Y1, X2, Y2, X3, Y3, X4, Y4: Integer;
    LineWidth: TSize; IsSolid: Boolean); forward;
procedure CreateSchPolygon4(Owner: ISch_BasicContainer;
    X1, Y1, X2, Y2, X3, Y3, X4, Y4: Integer;
    LineWidth: TSize; IsSolid: Boolean); forward;
procedure CreateSchBezier4(Owner: ISch_BasicContainer;
    X1, Y1, X2, Y2, X3, Y3, X4, Y4: Integer;
    LineWidth: TSize); forward;

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

procedure JsonWriteString(Name, Value: String; AddComma: Boolean);
var
    EscapedValue: String;
    I: Integer;
    Ch: Char;
begin
    EscapedValue := '';
    for I := 1 to Length(Value) do
    begin
        Ch := Value[I];
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

procedure JsonWriteInteger(Name: String; Value: Integer; AddComma: Boolean);
begin
    if AddComma then
        JsonWriteLine('"' + Name + '": ' + VarToStr(Value) + ',')
    else
        JsonWriteLine('"' + Name + '": ' + VarToStr(Value));
end;

procedure JsonWriteFloat(Name: String; Value: Double; AddComma: Boolean);
var
    FloatStr: String;
    I: Integer;
begin
    FloatStr := FloatToStr(Value);
    for I := 1 to Length(FloatStr) do
        if FloatStr[I] = ',' then FloatStr[I] := '.';

    if AddComma then
        JsonWriteLine('"' + Name + '": ' + FloatStr + ',')
    else
        JsonWriteLine('"' + Name + '": ' + FloatStr);
end;

procedure JsonWriteBoolean(Name: String; Value: Boolean; AddComma: Boolean);
var
    BoolStr: String;
begin
    if Value then BoolStr := 'true' else BoolStr := 'false';
    if AddComma then
        JsonWriteLine('"' + Name + '": ' + BoolStr + ',')
    else
        JsonWriteLine('"' + Name + '": ' + BoolStr);
end;

procedure JsonWriteCoord(Name: String; Value: TCoord; AddComma: Boolean);
begin
    JsonOpenObject(Name);
    JsonWriteInteger('internal', Value, True);
    JsonWriteFloat('mils', CoordToMils(Value), True);
    JsonWriteFloat('mm', CoordToMMs(Value), False);
    JsonCloseObject(AddComma);
end;

{==============================================================================
  FILE CREATION UTILITIES
==============================================================================}

function EnsureDirectoryExists(DirPath: String): Boolean;
begin
    Result := True;
    // Create directory and all parent directories if needed
    if not DirectoryExists(DirPath) then
    begin
        ForceDirectories(DirPath);
    end;
end;

procedure SaveDocumentAs(CurrentFilePath: String; NewFilePath: String; DocType: String);
var
    Doc: IServerDocument;
begin
    // Use GetDocumentByPath since ActiveDocument doesn't exist in DelphiScript
    Doc := Client.GetDocumentByPath(CurrentFilePath);
    if Doc <> nil then
    begin
        Doc.SetFileName(NewFilePath);
        Doc.DoFileSave(DocType);
    end;
end;

function CreateNewPcbLib(FilePath: String): IPCB_Library;
var
    Doc: IServerDocument;
begin
    Result := nil;

    // Create a new standalone PCB library document (not added to any project)
    Doc := Client.OpenNewDocument('PcbLib', FilePath, '', False);
    if Doc = nil then Exit;
    Client.ShowDocument(Doc);

    // Get the current PCB library
    Result := PCBServer.GetCurrentPCBLibrary;
end;

function OpenOrCreatePcbLib(FilePath: String): IPCB_Library;
var
    Doc: IServerDocument;
begin
    Result := nil;

    // Try to open existing file first
    if FileExists(FilePath) then
    begin
        Doc := Client.OpenDocument('PCBLIB', FilePath);
        if Doc <> nil then
        begin
            Client.ShowDocument(Doc);
            Result := PCBServer.GetCurrentPCBLibrary;
        end;
    end
    else
    begin
        Result := CreateNewPcbLib(FilePath);
    end;
end;

function CreateNewSchLib(FilePath: String): ISch_Lib;
var
    Doc: IServerDocument;
begin
    Result := nil;

    // Create a new standalone schematic library document (not added to any project)
    Doc := Client.OpenNewDocument('SchLib', FilePath, '', False);
    if Doc = nil then Exit;
    Client.ShowDocument(Doc);

    // Get the current schematic library
    Result := SchServer.GetCurrentSchDocument;
end;

function CreateNewPcbDoc(FilePath: String): IPCB_Board;
var
    Doc: IServerDocument;
begin
    Result := nil;

    // Create a new standalone PCB document (not added to any project)
    Doc := Client.OpenNewDocument('Pcb', FilePath, '', False);
    if Doc = nil then Exit;
    Client.ShowDocument(Doc);

    // Get the current PCB board
    Result := PCBServer.GetCurrentPCBBoard;
end;

function CreateNewSchDoc(FilePath: String): ISch_Document;
var
    Doc: IServerDocument;
begin
    Result := nil;

    // Create a new standalone schematic document (not added to any project)
    Doc := Client.OpenNewDocument('Sch', FilePath, '', False);
    if Doc = nil then Exit;
    Client.ShowDocument(Doc);

    // Get the current schematic document
    Result := SchServer.GetCurrentSchDocument;
end;

procedure SaveDocument(FilePath: String);
var
    Doc: IServerDocument;
begin
    Doc := Client.GetDocumentByPath(FilePath);
    if Doc <> nil then
        Doc.DoFileSave('');
end;

procedure CloseDocument(FilePath: String);
var
    Doc: IServerDocument;
begin
    Doc := Client.GetDocumentByPath(FilePath);
    if Doc <> nil then
        Client.CloseDocument(Doc);
end;

{==============================================================================
  PCB PRIMITIVE CREATORS
==============================================================================}

function MilsToCoord(Mils: Double): TCoord;
begin
    Result := Round(Mils * MILS_TO_INTERNAL);
end;

function MMToCoord(MM: Double): TCoord;
begin
    Result := Round(MM * MM_TO_INTERNAL);
end;

procedure CreatePcbPad(Comp: IPCB_LibComponent; Name: String; X, Y: TCoord;
    TopXSize, TopYSize: TCoord; HoleSize: TCoord; Shape: TShape;
    Layer: TLayer; Rotation: Double);
var
    Pad: IPCB_Pad;
begin
    Pad := PCBServer.PCBObjectFactory(ePadObject, eNoDimension, eCreate_Default);
    if Pad = nil then Exit;

    Pad.Name := Name;
    Pad.X := X;
    Pad.Y := Y;
    Pad.TopXSize := TopXSize;
    Pad.TopYSize := TopYSize;
    Pad.HoleSize := HoleSize;
    Pad.Mode := ePadMode_Simple;
    Pad.TopShape := Shape;
    Pad.Layer := Layer;
    Pad.Rotation := Rotation;
    // SMD pads (no hole) are not plated
    if HoleSize = 0 then
        Pad.Plated := False
    else
        Pad.Plated := True;

    Comp.AddPCBObject(Pad);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Pad.I_ObjectAddress);
end;

procedure CreatePcbSmdRoundedRectPad(Comp: IPCB_LibComponent; Name: String; X, Y: TCoord;
    TopXSize, TopYSize: TCoord; Layer: TLayer; Rotation: Double; CornerRadiusPct: Integer);
var
    Pad: IPCB_Pad;
begin
    Pad := PCBServer.PCBObjectFactory(ePadObject, eNoDimension, eCreate_Default);
    if Pad = nil then Exit;

    Pad.Name := Name;
    Pad.X := X;
    Pad.Y := Y;
    Pad.TopXSize := TopXSize;
    Pad.TopYSize := TopYSize;
    Pad.HoleSize := 0;
    Pad.Mode := ePadMode_Simple;
    Pad.Layer := Layer;
    Pad.Rotation := Rotation;
    Pad.Plated := False;

    // Set rounded rectangular shape on top layer
    Pad.SetState_StackShapeOnLayer(eTopLayer, eRoundedRectangular);
    Pad.SetState_StackCRPctOnLayer(eTopLayer, CornerRadiusPct);

    Comp.AddPCBObject(Pad);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Pad.I_ObjectAddress);
end;

procedure CreatePcbTrack(Comp: IPCB_LibComponent; X1, Y1, X2, Y2, Width: TCoord; Layer: TLayer);
var
    Track: IPCB_Track;
begin
    Track := PCBServer.PCBObjectFactory(eTrackObject, eNoDimension, eCreate_Default);
    if Track = nil then Exit;

    Track.X1 := X1;
    Track.Y1 := Y1;
    Track.X2 := X2;
    Track.Y2 := Y2;
    Track.Width := Width;
    Track.Layer := Layer;

    Comp.AddPCBObject(Track);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Track.I_ObjectAddress);
end;

procedure CreatePcbArc(Comp: IPCB_LibComponent; XCenter, YCenter, Radius, Width: TCoord;
    StartAngle, EndAngle: Double; Layer: TLayer);
var
    Arc: IPCB_Arc;
begin
    Arc := PCBServer.PCBObjectFactory(eArcObject, eNoDimension, eCreate_Default);
    if Arc = nil then Exit;

    Arc.XCenter := XCenter;
    Arc.YCenter := YCenter;
    Arc.Radius := Radius;
    Arc.LineWidth := Width;
    Arc.StartAngle := StartAngle;
    Arc.EndAngle := EndAngle;
    Arc.Layer := Layer;

    Comp.AddPCBObject(Arc);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Arc.I_ObjectAddress);
end;

procedure CreatePcbFill(Comp: IPCB_LibComponent; X1, Y1, X2, Y2: TCoord;
    Layer: TLayer; Rotation: Double);
var
    Fill: IPCB_Fill;
begin
    Fill := PCBServer.PCBObjectFactory(eFillObject, eNoDimension, eCreate_Default);
    if Fill = nil then Exit;

    Fill.X1Location := X1;
    Fill.Y1Location := Y1;
    Fill.X2Location := X2;
    Fill.Y2Location := Y2;
    Fill.Layer := Layer;
    Fill.Rotation := Rotation;

    Comp.AddPCBObject(Fill);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Fill.I_ObjectAddress);
end;

procedure CreatePcbText(Comp: IPCB_LibComponent; X, Y: TCoord; Text: String;
    Height, Width: TCoord; Layer: TLayer; Rotation: Double; Mirror: Boolean);
var
    PcbText: IPCB_Text;
begin
    PcbText := PCBServer.PCBObjectFactory(eTextObject, eNoDimension, eCreate_Default);
    if PcbText = nil then Exit;

    PcbText.XLocation := X;
    PcbText.YLocation := Y;
    PcbText.Text := Text;
    PcbText.Size := Height;
    PcbText.Width := Width;
    PcbText.Layer := Layer;
    PcbText.Rotation := Rotation;
    PcbText.MirrorFlag := Mirror;

    Comp.AddPCBObject(PcbText);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, PcbText.I_ObjectAddress);
end;

procedure CreatePcbTrueTypeText(Comp: IPCB_LibComponent; X, Y: TCoord; Text: String;
    Height: TCoord; Layer: TLayer; Rotation: Double; Mirror: Boolean;
    FontName: String; Bold, Italic: Boolean);
var
    PcbText: IPCB_Text;
begin
    PcbText := PCBServer.PCBObjectFactory(eTextObject, eNoDimension, eCreate_Default);
    if PcbText = nil then Exit;

    PcbText.XLocation := X;
    PcbText.YLocation := Y;
    PcbText.Text := Text;
    PcbText.Size := Height;
    PcbText.Layer := Layer;
    PcbText.Rotation := Rotation;
    PcbText.MirrorFlag := Mirror;
    PcbText.UseTTFonts := True;
    PcbText.FontName := FontName;
    PcbText.Bold := Bold;
    PcbText.Italic := Italic;

    Comp.AddPCBObject(PcbText);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, PcbText.I_ObjectAddress);
end;

procedure CreatePcbVia(Board: IPCB_Board; X, Y, HoleSize, Size: TCoord);
var
    Via: IPCB_Via;
begin
    Via := PCBServer.PCBObjectFactory(eViaObject, eNoDimension, eCreate_Default);
    if Via = nil then Exit;

    Via.X := X;
    Via.Y := Y;
    Via.HoleSize := HoleSize;
    Via.Size := Size;
    Via.LowLayer := eTopLayer;
    Via.HighLayer := eBottomLayer;
    Board.AddPCBObject(Via);
    PCBServer.SendMessageToRobots(Board.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Via.I_ObjectAddress);
end;

procedure CreatePcbRegion(Comp: IPCB_LibComponent; Layer: TLayer);
var
    Region: IPCB_Region;
    Contour: IPCB_Contour;
begin
    Region := PCBServer.PCBObjectFactory(eRegionObject, eNoDimension, eCreate_Default);
    if Region = nil then Exit;

    // Create a simple rectangular region
    Contour := Region.MainContour;
    Contour.AddPoint(MilsToCoord(0), MilsToCoord(0));
    Contour.AddPoint(MilsToCoord(100), MilsToCoord(0));
    Contour.AddPoint(MilsToCoord(100), MilsToCoord(50));
    Contour.AddPoint(MilsToCoord(0), MilsToCoord(50));

    Region.Layer := Layer;

    Comp.AddPCBObject(Region);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Region.I_ObjectAddress);
end;

procedure CreatePcbInvertedText(Comp: IPCB_LibComponent; X, Y: TCoord; Text: String;
    Height, Width: TCoord; Layer: TLayer; Rotation: Double);
var
    PcbText: IPCB_Text;
begin
    PcbText := PCBServer.PCBObjectFactory(eTextObject, eNoDimension, eCreate_Default);
    if PcbText = nil then Exit;

    PcbText.XLocation := X;
    PcbText.YLocation := Y;
    PcbText.Text := Text;
    PcbText.Size := Height;
    PcbText.Width := Width;
    PcbText.Layer := Layer;
    PcbText.Rotation := Rotation;
    PcbText.MirrorFlag := False;
    // Inverted text requires TrueType font (stroke fonts don't support inversion)
    PcbText.UseTTFonts := True;
    PcbText.FontName := 'Arial';
    PcbText.Inverted := True;

    Comp.AddPCBObject(PcbText);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, PcbText.I_ObjectAddress);
end;

procedure CreatePcbBarcodeText(Comp: IPCB_LibComponent; X, Y: TCoord; Text: String;
    Height: TCoord; Layer: TLayer; Rotation: Double; BarCodeKind: Integer);
var
    PcbText: IPCB_Text;
begin
    PcbText := PCBServer.PCBObjectFactory(eTextObject, eNoDimension, eCreate_Default);
    if PcbText = nil then Exit;

    PcbText.XLocation := X;
    PcbText.YLocation := Y;
    PcbText.Text := Text;
    PcbText.Size := Height;
    PcbText.Layer := Layer;
    PcbText.Rotation := Rotation;
    PcbText.MirrorFlag := False;
    PcbText.TextKind := eText_Barcode;
    PcbText.BarCodeKind := BarCodeKind;

    Comp.AddPCBObject(PcbText);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, PcbText.I_ObjectAddress);
end;

procedure CreatePcbTextWithJustification(Comp: IPCB_LibComponent; X, Y: TCoord; Text: String;
    Height, Width: TCoord; Layer: TLayer; Rotation: Double; Justification: Integer);
var
    PcbText: IPCB_Text;
begin
    PcbText := PCBServer.PCBObjectFactory(eTextObject, eNoDimension, eCreate_Default);
    if PcbText = nil then Exit;

    PcbText.XLocation := X;
    PcbText.YLocation := Y;
    PcbText.Text := Text;
    PcbText.Size := Height;
    PcbText.Width := Width;
    PcbText.Layer := Layer;
    PcbText.Rotation := Rotation;
    PcbText.MirrorFlag := False;
    PcbText.TTFTextWidth := MilsToCoord(500);  // Bounding width for justification
    PcbText.TTFTextHeight := MilsToCoord(100);
    PcbText.UseTTFonts := True;
    PcbText.FontName := 'Arial';

    Comp.AddPCBObject(PcbText);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, PcbText.I_ObjectAddress);
end;

procedure CreatePcbPolygon(Comp: IPCB_LibComponent; Layer: TLayer; HatchStyle: Integer);
var
    Polygon: IPCB_Polygon;
    Segment: TPolySegment;
begin
    Polygon := PCBServer.PCBObjectFactory(ePolyObject, eNoDimension, eCreate_Default);
    if Polygon = nil then Exit;

    Polygon.Layer := Layer;
    Polygon.PolyHatchStyle := HatchStyle;

    // Create a simple rectangular polygon (200x100 mils)
    Polygon.PointCount := 4;

    Segment := TPolySegment;
    Segment.Kind := ePolySegmentLine;

    Segment.vx := MilsToCoord(-100);
    Segment.vy := MilsToCoord(-50);
    Polygon.Segments[0] := Segment;

    Segment.vx := MilsToCoord(100);
    Segment.vy := MilsToCoord(-50);
    Polygon.Segments[1] := Segment;

    Segment.vx := MilsToCoord(100);
    Segment.vy := MilsToCoord(50);
    Polygon.Segments[2] := Segment;

    Segment.vx := MilsToCoord(-100);
    Segment.vy := MilsToCoord(50);
    Polygon.Segments[3] := Segment;

    Polygon.SetState_CopperPourValid;
    Polygon.Rebuild;

    Comp.AddPCBObject(Polygon);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Polygon.I_ObjectAddress);
end;

procedure CreatePcbRegionCutout(Comp: IPCB_LibComponent; Layer: TLayer);
var
    Region: IPCB_Region;
    Contour: IPCB_Contour;
begin
    Region := PCBServer.PCBObjectFactory(eRegionObject, eNoDimension, eCreate_Default);
    if Region = nil then Exit;

    Region.Layer := Layer;
    Region.Kind := eRegionKind_Cutout;

    // Create a simple rectangular cutout region
    Contour := Region.MainContour.Replicate;
    Contour.Count := 4;
    Contour.X[1] := MilsToCoord(-50);
    Contour.Y[1] := MilsToCoord(-25);
    Contour.X[2] := MilsToCoord(50);
    Contour.Y[2] := MilsToCoord(-25);
    Contour.X[3] := MilsToCoord(50);
    Contour.Y[3] := MilsToCoord(25);
    Contour.X[4] := MilsToCoord(-50);
    Contour.Y[4] := MilsToCoord(25);
    Region.SetOutlineContour(Contour);

    Comp.AddPCBObject(Region);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Region.I_ObjectAddress);
end;

procedure CreatePcb3DBody(Comp: IPCB_LibComponent; StandoffHeight, OverallHeight: TCoord;
    BodyProjection: Integer);
var
    Body: IPCB_ComponentBody;
    Contour: IPCB_Contour;
begin
    Body := PCBServer.PCBObjectFactory(eComponentBodyObject, eNoDimension, eCreate_Default);
    if Body = nil then Exit;

    Body.Layer := eMechanical1;
    Body.StandoffHeight := StandoffHeight;
    Body.OverallHeight := OverallHeight;
    Body.BodyProjection := BodyProjection;

    // Create simple rectangular outline using Region contour pattern
    Contour := Body.MainContour.Replicate;
    Contour.Count := 4;
    Contour.X[1] := MilsToCoord(-100);
    Contour.Y[1] := MilsToCoord(-50);
    Contour.X[2] := MilsToCoord(100);
    Contour.Y[2] := MilsToCoord(-50);
    Contour.X[3] := MilsToCoord(100);
    Contour.Y[3] := MilsToCoord(50);
    Contour.X[4] := MilsToCoord(-100);
    Contour.Y[4] := MilsToCoord(50);
    Body.SetOutlineContour(Contour);

    Comp.AddPCBObject(Body);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Body.I_ObjectAddress);
end;

procedure CreatePcb3DBodyFromStep(Comp: IPCB_LibComponent; StepFilePath: String;
    RotX, RotY, RotZ: Double; OffsetZ: TCoord);
var
    Body: IPCB_ComponentBody;
    Model: IPCB_Model;
begin
    Body := PCBServer.PCBObjectFactory(eComponentBodyObject, eNoDimension, eCreate_Default);
    if Body = nil then Exit;

    // Load model from STEP file
    Model := Body.ModelFactory_FromFilename(StepFilePath, False);
    if Model = nil then Exit;

    // Set rotation if needed (for SolidWorks models that have Y-up instead of Z-up)
    if (RotX <> 0) or (RotY <> 0) or (RotZ <> 0) or (OffsetZ <> 0) then
        Model.SetState(RotX, RotY, RotZ, OffsetZ);

    Body.SetState_FromModel;
    Body.Model := Model;

    Comp.AddPCBObject(Body);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Body.I_ObjectAddress);
end;

procedure CreatePcbKeepoutTrack(Comp: IPCB_LibComponent; X1, Y1, X2, Y2, Width: TCoord; Layer: TLayer);
var
    Track: IPCB_Track;
begin
    Track := PCBServer.PCBObjectFactory(eTrackObject, eNoDimension, eCreate_Default);
    if Track = nil then Exit;

    Track.X1 := X1;
    Track.Y1 := Y1;
    Track.X2 := X2;
    Track.Y2 := Y2;
    Track.Width := Width;
    Track.Layer := Layer;
    Track.IsKeepout := True;

    Comp.AddPCBObject(Track);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Track.I_ObjectAddress);
end;

procedure CreatePcbKeepoutRegion(Comp: IPCB_LibComponent; Layer: TLayer);
var
    Region: IPCB_Region;
    Contour: IPCB_Contour;
begin
    Region := PCBServer.PCBObjectFactory(eRegionObject, eNoDimension, eCreate_Default);
    if Region = nil then Exit;

    Region.Layer := Layer;
    Region.IsKeepout := True;

    // Create a simple rectangular keepout region
    Contour := Region.MainContour;
    Contour.AddPoint(MilsToCoord(-75), MilsToCoord(-40));
    Contour.AddPoint(MilsToCoord(75), MilsToCoord(-40));
    Contour.AddPoint(MilsToCoord(75), MilsToCoord(40));
    Contour.AddPoint(MilsToCoord(-75), MilsToCoord(40));

    Comp.AddPCBObject(Region);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Region.I_ObjectAddress);
end;

// TODO: Dimension creation via script is not working - References API is undocumented
// Keeping these procedures commented out until a working approach is found.
// JSON export of dimensions from existing files still works (see ExportPcbDimensionToJson).
{
procedure CreatePcbDimension(Comp: IPCB_LibComponent; X1, Y1, X2, Y2: TCoord;
    TextHeight: TCoord; Layer: TLayer);
var
    Dimension: IPCB_Dimension;
begin
    Dimension := PCBServer.PCBObjectFactory(eDimensionObject, eNoDimension, eCreate_Default);
    if Dimension = nil then Exit;

    Dimension.DimensionKind := eLinear;
    Dimension.Layer := Layer;
    Dimension.TextHeight := TextHeight;
    Dimension.LineWidth := MilsToCoord(5);
    Dimension.ArrowSize := MilsToCoord(20);
    Dimension.TextGap := MilsToCoord(10);

    Dimension.References_Add(Point(X1, Y1));
    Dimension.References_Add(Point(X2, Y2));

    Comp.AddPCBObject(Dimension);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Dimension.I_ObjectAddress);
end;
}

{==============================================================================
  EXTENDED PCB PRIMITIVE CREATORS (with full property coverage)
==============================================================================}

{ Creates a pad with extended properties for comprehensive testing }
procedure CreatePcbPadExtended(Comp: IPCB_LibComponent; Name: String; X, Y: TCoord;
    TopXSize, TopYSize: TCoord; HoleSize: TCoord; Shape: TShape;
    Layer: TLayer; Rotation: Double;
    IsTenting: Boolean);
var
    Pad: IPCB_Pad;
    PadCache: TPadCache;
begin
    Pad := PCBServer.PCBObjectFactory(ePadObject, eNoDimension, eCreate_Default);
    if Pad = nil then Exit;

    // Basic properties
    Pad.Name := Name;
    Pad.X := X;
    Pad.Y := Y;
    Pad.TopXSize := TopXSize;
    Pad.TopYSize := TopYSize;
    Pad.HoleSize := HoleSize;
    Pad.Mode := ePadMode_Simple;
    Pad.TopShape := Shape;
    Pad.Layer := Layer;
    Pad.Rotation := Rotation;

    // Plating
    if HoleSize = 0 then
        Pad.Plated := False
    else
        Pad.Plated := True;

    Comp.AddPCBObject(Pad);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Pad.I_ObjectAddress);

    // Extended properties via pad cache
    PadCache := Pad.GetState_Cache;

    // Tenting
    Pad.IsTenting := IsTenting;
    Pad.IsTenting_Top := IsTenting;
    Pad.IsTenting_Bottom := IsTenting;

    // Mask expansions - must be set via cache
    if not IsTenting then begin
        PadCache.SolderMaskExpansion := MilsToCoord(4);
        PadCache.SolderMaskExpansionValid := eCacheManual;
        PadCache.PasteMaskExpansion := MilsToCoord(0);
        PadCache.PasteMaskExpansionValid := eCacheManual;
    end;

    Pad.SetState_Cache := PadCache;

    Pad.SwapID_Pad := 'A';
    Pad.SwapID_Part := '1';

end;

{ Creates a polygon with extended properties }
procedure CreatePcbPolygonExtended(Comp: IPCB_LibComponent; Layer: TLayer;
    HatchStyle: Integer; TrackSize, Grid: TCoord;
    RemoveIslands, RemoveNecks, UseThermals: Boolean);
var
    Polygon: IPCB_Polygon;
    I: Integer;
begin
    Polygon := PCBServer.PCBObjectFactory(ePolyObject, eNoDimension, eCreate_Default);
    if Polygon = nil then Exit;

    Polygon.Layer := Layer;
    Polygon.PolyHatchStyle := HatchStyle;
    Polygon.TrackSize := TrackSize;
    Polygon.Grid := Grid;
    Polygon.MinTrack := MilsToCoord(5);
    Polygon.PourOver := ePolygonPourOver_SameNet;

    // Extended polygon properties
    Polygon.RemoveIslandsByArea := RemoveIslands;
    Polygon.RemoveNarrowNecks := RemoveNecks;
    Polygon.RemoveDead := True;
    Polygon.UseOctagons := False;
    Polygon.AvoidObsticles := True;
    Polygon.IslandAreaThreshold := 10.0;
    Polygon.NeckWidthThreshold := MilsToCoord(10);
    Polygon.ArcApproximation := MilsToCoord(0.5);
    Polygon.IgnoreViolations := False;

    // New extended properties from API
    Polygon.ArcPourMode := False;
    Polygon.AutoGenerateName := True;
    Polygon.BorderWidth := MilsToCoord(0);
    Polygon.ClipAcuteCorners := True;
    Polygon.DrawDeadCopper := False;
    Polygon.DrawRemovedIslands := False;
    Polygon.DrawRemovedNecks := False;
    Polygon.ExpandOutline := False;
    Polygon.MitreCorners := False;
    Polygon.ObeyPolygonCutout := True;
    Polygon.OptimalVoidRotation := True;
    Polygon.PrimitiveLock := False;

    // Add polygon outline vertices (hexagon shape for variety)
    Polygon.PointCount := 6;
    Polygon.Segments[0].vx := MilsToCoord(50);
    Polygon.Segments[0].vy := MilsToCoord(0);
    Polygon.Segments[0].Kind := ePolySegmentLine;
    Polygon.Segments[1].vx := MilsToCoord(25);
    Polygon.Segments[1].vy := MilsToCoord(43);
    Polygon.Segments[1].Kind := ePolySegmentLine;
    Polygon.Segments[2].vx := MilsToCoord(-25);
    Polygon.Segments[2].vy := MilsToCoord(43);
    Polygon.Segments[2].Kind := ePolySegmentLine;
    Polygon.Segments[3].vx := MilsToCoord(-50);
    Polygon.Segments[3].vy := MilsToCoord(0);
    Polygon.Segments[3].Kind := ePolySegmentLine;
    Polygon.Segments[4].vx := MilsToCoord(-25);
    Polygon.Segments[4].vy := MilsToCoord(-43);
    Polygon.Segments[4].Kind := ePolySegmentLine;
    Polygon.Segments[5].vx := MilsToCoord(25);
    Polygon.Segments[5].vy := MilsToCoord(-43);
    Polygon.Segments[5].Kind := ePolySegmentLine;

    Comp.AddPCBObject(Polygon);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Polygon.I_ObjectAddress);
end;

{ Creates a 3D body with extended properties including color and opacity }
procedure CreatePcb3DBodyExtended(Comp: IPCB_LibComponent;
    StandoffHeight, OverallHeight: TCoord;
    BodyProjection: Integer; BodyColor: TColor; BodyOpacity: Double);
var
    Body: IPCB_ComponentBody;
    Contour: IPCB_Contour;
begin
    Body := PCBServer.PCBObjectFactory(eComponentBodyObject, eNoDimension, eCreate_Default);
    if Body = nil then Exit;

    Body.Layer := eMechanical1;
    Body.StandoffHeight := StandoffHeight;
    Body.OverallHeight := OverallHeight;
    Body.BodyProjection := BodyProjection;

    // Extended 3D properties
    Body.BodyColor3D := BodyColor;
    Body.BodyOpacity3D := BodyOpacity;

    // Create rectangular outline
    Contour := Body.MainContour.Replicate;
    Contour.Count := 4;
    Contour.X[1] := MilsToCoord(-100);
    Contour.Y[1] := MilsToCoord(-50);
    Contour.X[2] := MilsToCoord(100);
    Contour.Y[2] := MilsToCoord(-50);
    Contour.X[3] := MilsToCoord(100);
    Contour.Y[3] := MilsToCoord(50);
    Contour.X[4] := MilsToCoord(-100);
    Contour.Y[4] := MilsToCoord(50);
    Body.SetOutlineContour(Contour);

    Comp.AddPCBObject(Body);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Body.I_ObjectAddress);
end;

{ Creates a region with a hole inside }
procedure CreatePcbRegionWithHole(Comp: IPCB_LibComponent; Layer: TLayer);
var
    Region: IPCB_Region;
    MainContour, HoleContour: IPCB_Contour;
begin
    Region := PCBServer.PCBObjectFactory(eRegionObject, eNoDimension, eCreate_Default);
    if Region = nil then Exit;

    Region.Layer := Layer;

    // Create outer rectangular contour
    MainContour := Region.MainContour;
    MainContour.AddPoint(MilsToCoord(-100), MilsToCoord(-75));
    MainContour.AddPoint(MilsToCoord(100), MilsToCoord(-75));
    MainContour.AddPoint(MilsToCoord(100), MilsToCoord(75));
    MainContour.AddPoint(MilsToCoord(-100), MilsToCoord(75));

    // Add a hole in the center
    HoleContour := Region.Holes[0];
    if HoleContour <> nil then
    begin
        HoleContour.AddPoint(MilsToCoord(-30), MilsToCoord(-30));
        HoleContour.AddPoint(MilsToCoord(30), MilsToCoord(-30));
        HoleContour.AddPoint(MilsToCoord(30), MilsToCoord(30));
        HoleContour.AddPoint(MilsToCoord(-30), MilsToCoord(30));
    end;

    Comp.AddPCBObject(Region);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Region.I_ObjectAddress);
end;

{
procedure CreatePcbDimensionExtended(Comp: IPCB_LibComponent; X1, Y1, X2, Y2: TCoord;
    TextHeight: TCoord; Layer: TLayer; UseTTFonts: Boolean; FontName: String);
var
    Dimension: IPCB_Dimension;
begin
    Dimension := PCBServer.PCBObjectFactory(eDimensionObject, eNoDimension, eCreate_Default);
    if Dimension = nil then Exit;

    Dimension.DimensionKind := eLinear;
    Dimension.Layer := Layer;
    Dimension.TextHeight := TextHeight;
    Dimension.LineWidth := MilsToCoord(5);
    Dimension.ArrowSize := MilsToCoord(20);
    Dimension.TextGap := MilsToCoord(10);

    Dimension.TextLineWidth := MilsToCoord(3);
    Dimension.PrimitiveLock := False;
    Dimension.Bold := False;
    Dimension.Italic := False;
    Dimension.UseTTFonts := UseTTFonts;
    if UseTTFonts then
        Dimension.FontName := FontName;

    Dimension.ExtensionOffset := MilsToCoord(5);
    Dimension.ExtensionLineWidth := MilsToCoord(3);
    Dimension.ExtensionPickGap := MilsToCoord(10);
    Dimension.ArrowLineWidth := MilsToCoord(3);

    Dimension.References_Add(Point(X1, Y1));
    Dimension.References_Add(Point(X2, Y2));

    Comp.AddPCBObject(Dimension);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Dimension.I_ObjectAddress);
end;
}

{==============================================================================
  PCB LIBRARY GENERATOR
==============================================================================}

// Helper to finalize a component after adding all primitives
procedure FinalizeLibComponent(PCBLib: IPCB_Library; Comp: IPCB_LibComponent);
begin
    // Register component with library board
    PCBServer.SendMessageToRobots(PCBLib.Board.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Comp.I_ObjectAddress);
    PCBLib.CurrentComponent := Comp;
end;

// Remove the default empty component that Altium creates in new libraries
procedure RemoveDefaultPcbLibComponent(PCBLib: IPCB_Library);
var
    Iterator: IPCB_LibraryIterator;
    Comp: IPCB_LibComponent;
begin
    if PCBLib = nil then Exit;

    Iterator := PCBLib.LibraryIterator_Create;
    Iterator.SetState_FilterAll;
    Comp := Iterator.FirstPCBObject;
    if Comp <> nil then
        PCBLib.RemoveComponent(Comp);
    PCBLib.LibraryIterator_Destroy(Iterator);
end;

procedure GeneratePcbLibTestFootprints(PCBLib: IPCB_Library);
var
    Comp: IPCB_LibComponent;
begin
    if PCBLib = nil then Exit;

    // === Footprint 1: PAD_TH_ROUND - Through-hole round pad ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'PAD_TH_ROUND';
    Comp.Description := 'Through-hole round pad test';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcbPad(Comp, '1', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(60), MilsToCoord(60), MilsToCoord(30),
        eRounded, eMultiLayer, 0);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 2: PAD_TH_RECTANGULAR - Through-hole rectangular pad ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'PAD_TH_RECTANGULAR';
    Comp.Description := 'Through-hole rectangular pad test';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcbPad(Comp, '1', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(80), MilsToCoord(50), MilsToCoord(30),
        eRectangular, eMultiLayer, 0);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 3: PAD_TH_OCTAGONAL - Through-hole octagonal pad ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'PAD_TH_OCTAGONAL';
    Comp.Description := 'Through-hole octagonal pad test';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcbPad(Comp, '1', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(70), MilsToCoord(70), MilsToCoord(30),
        eOctagonal, eMultiLayer, 0);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 4: PAD_SMD_RECTANGULAR - SMD rectangular pad ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'PAD_SMD_RECTANGULAR';
    Comp.Description := 'SMD rectangular pad test';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcbPad(Comp, '1', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(50), MilsToCoord(30), 0,
        eRectangular, eTopLayer, 0);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 5: PAD_SMD_ROUNDED - SMD rounded rectangular pad ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'PAD_SMD_ROUNDED';
    Comp.Description := 'SMD rounded rectangular pad test';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    // Use 50% corner radius for a fully rounded end
    CreatePcbSmdRoundedRectPad(Comp, '1', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(50), MilsToCoord(30), eTopLayer, 0, 50);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 6: PAD_ROTATED - Rotated pad at 45 degrees ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'PAD_ROTATED';
    Comp.Description := 'Rotated pad at 45 degrees';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcbPad(Comp, '1', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(60), MilsToCoord(40), MilsToCoord(25),
        eRectangular, eMultiLayer, 45);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 7: TRACKS_MULTILAYER - Tracks on multiple layers ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'TRACKS_MULTILAYER';
    Comp.Description := 'Tracks on various layers';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcbTrack(Comp, MilsToCoord(-100), MilsToCoord(0),
        MilsToCoord(100), MilsToCoord(0), MilsToCoord(10), eTopLayer);
    CreatePcbTrack(Comp, MilsToCoord(-100), MilsToCoord(-50),
        MilsToCoord(100), MilsToCoord(-50), MilsToCoord(10), eBottomLayer);
    CreatePcbTrack(Comp, MilsToCoord(0), MilsToCoord(-100),
        MilsToCoord(0), MilsToCoord(100), MilsToCoord(8), eTopOverlay);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 8: ARCS_TEST - Arcs with various angles ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'ARCS_TEST';
    Comp.Description := 'Arcs with various angles';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcbArc(Comp, MilsToCoord(0), MilsToCoord(0), MilsToCoord(50),
        MilsToCoord(10), 0, 90, eTopLayer);
    CreatePcbArc(Comp, MilsToCoord(0), MilsToCoord(0), MilsToCoord(75),
        MilsToCoord(10), 90, 180, eTopLayer);
    CreatePcbArc(Comp, MilsToCoord(0), MilsToCoord(0), MilsToCoord(100),
        MilsToCoord(10), 0, 360, eTopLayer);  // Full circle
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 9: FILLS_TEST - Fill regions ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'FILLS_TEST';
    Comp.Description := 'Fill regions';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcbFill(Comp, MilsToCoord(-50), MilsToCoord(-25),
        MilsToCoord(50), MilsToCoord(25), eTopLayer, 0);
    CreatePcbFill(Comp, MilsToCoord(-40), MilsToCoord(-60),
        MilsToCoord(40), MilsToCoord(-40), eBottomLayer, 45);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 10: TEXT_TEST - Text objects ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'TEXT_TEST';
    Comp.Description := 'Text objects with various properties';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcbText(Comp, MilsToCoord(0), MilsToCoord(0), 'TOP',
        MilsToCoord(50), MilsToCoord(5), eTopOverlay, 0, False);
    CreatePcbText(Comp, MilsToCoord(0), MilsToCoord(-75), 'ROTATED',
        MilsToCoord(40), MilsToCoord(4), eTopOverlay, 45, False);
    CreatePcbText(Comp, MilsToCoord(0), MilsToCoord(-150), 'MIRROR',
        MilsToCoord(40), MilsToCoord(4), eBottomOverlay, 0, True);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 11: TEXT_TRUETYPE - TrueType font text objects ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'TEXT_TRUETYPE';
    Comp.Description := 'TrueType font text objects';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcbTrueTypeText(Comp, MilsToCoord(0), MilsToCoord(0), 'Arial Normal',
        MilsToCoord(50), eTopOverlay, 0, False, 'Arial', False, False);
    CreatePcbTrueTypeText(Comp, MilsToCoord(0), MilsToCoord(-75), 'Arial Bold',
        MilsToCoord(50), eTopOverlay, 0, False, 'Arial', True, False);
    CreatePcbTrueTypeText(Comp, MilsToCoord(0), MilsToCoord(-150), 'Arial Italic',
        MilsToCoord(50), eTopOverlay, 0, False, 'Arial', False, True);
    CreatePcbTrueTypeText(Comp, MilsToCoord(0), MilsToCoord(-225), 'Times New Roman',
        MilsToCoord(50), eTopOverlay, 0, False, 'Times New Roman', False, False);
    CreatePcbTrueTypeText(Comp, MilsToCoord(0), MilsToCoord(-300), 'Courier Rotated',
        MilsToCoord(40), eTopOverlay, 45, False, 'Courier New', False, False);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 12: REGIONS_TEST - Region objects ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'REGIONS_TEST';
    Comp.Description := 'Region objects';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcbRegion(Comp, eTopLayer);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 13: COMPLEX_FOOTPRINT - Multiple primitives ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'COMPLEX_FOOTPRINT';
    Comp.Description := 'Complex footprint with multiple primitive types';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    // Two pads
    CreatePcbPad(Comp, '1', MilsToCoord(-50), MilsToCoord(0),
        MilsToCoord(60), MilsToCoord(60), MilsToCoord(30),
        eRounded, eMultiLayer, 0);
    CreatePcbPad(Comp, '2', MilsToCoord(50), MilsToCoord(0),
        MilsToCoord(60), MilsToCoord(60), MilsToCoord(30),
        eRounded, eMultiLayer, 0);
    // Outline
    CreatePcbTrack(Comp, MilsToCoord(-80), MilsToCoord(-40),
        MilsToCoord(80), MilsToCoord(-40), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(-80), MilsToCoord(40),
        MilsToCoord(80), MilsToCoord(40), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(-80), MilsToCoord(-40),
        MilsToCoord(-80), MilsToCoord(40), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(80), MilsToCoord(-40),
        MilsToCoord(80), MilsToCoord(40), MilsToCoord(8), eTopOverlay);
    // Pin 1 indicator
    CreatePcbArc(Comp, MilsToCoord(-50), MilsToCoord(30), MilsToCoord(5),
        MilsToCoord(2), 0, 360, eTopOverlay);
    // Reference designator
    CreatePcbText(Comp, MilsToCoord(0), MilsToCoord(50), '.Designator',
        MilsToCoord(40), MilsToCoord(4), eTopOverlay, 0, False);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 14: TEXT_INVERTED - Inverted (knockout) text ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'TEXT_INVERTED';
    Comp.Description := 'Inverted (knockout) text test';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    // Create a fill behind the text to show the knockout effect
    CreatePcbFill(Comp, MilsToCoord(-150), MilsToCoord(-30),
        MilsToCoord(150), MilsToCoord(70), eTopLayer, 0);
    CreatePcbInvertedText(Comp, MilsToCoord(-100), MilsToCoord(0), 'INVERTED',
        MilsToCoord(50), MilsToCoord(5), eTopLayer, 0);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 15: TEXT_BARCODE - Barcode text ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'TEXT_BARCODE';
    Comp.Description := 'Barcode text test';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    // Code39 barcode (type 0)
    CreatePcbBarcodeText(Comp, MilsToCoord(0), MilsToCoord(0), '123456',
        MilsToCoord(100), eTopOverlay, 0, 0);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 16: REGION_CUTOUT - Cutout region ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'REGION_CUTOUT';
    Comp.Description := 'Cutout region for polygon pours';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcbRegionCutout(Comp, eTopLayer);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 17: BODY_3D - 3D component body ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'BODY_3D';
    Comp.Description := '3D extruded component body';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcb3DBody(Comp, MilsToCoord(0), MilsToCoord(100), 0);  // 0 = extruded
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 18: KEEPOUT_REGION - Keepout region ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'KEEPOUT_REGION';
    Comp.Description := 'Keepout region test';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcbKeepoutRegion(Comp, eKeepOutLayer);
    // Also add a keepout track
    CreatePcbKeepoutTrack(Comp, MilsToCoord(-100), MilsToCoord(-60),
        MilsToCoord(100), MilsToCoord(-60), MilsToCoord(10), eKeepOutLayer);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 19: BODY_3D_STEP - 3D body from STEP file ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'BODY_3D_STEP';
    Comp.Description := '3D body loaded from STEP file';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    // Load a QFN package STEP model
    CreatePcb3DBodyFromStep(Comp, STEP_DIR + 'PSEMI QFN-24 4x4.step', 0, 0, 0, 0);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 20: PAD_EXTENDED - Pad with extended properties ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'PAD_EXTENDED';
    Comp.Description := 'Pad with tenting, testpoint, power plane properties';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    // Tented pad with thermal relief
    CreatePcbPadExtended(Comp, '1', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(60), MilsToCoord(60), MilsToCoord(30),
        eRounded, eMultiLayer, 0,
        True); // IsTenting
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 21: POLYGON_EXTENDED - Polygon with extended properties ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'POLYGON_EXTENDED';
    Comp.Description := 'Polygon with extended pour properties';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcbPolygonExtended(Comp, eTopLayer,
        ePolySolid,        // HatchStyle
        MilsToCoord(10),   // TrackSize
        MilsToCoord(20),   // Grid
        True,              // RemoveIslands
        True,              // RemoveNecks
        True);             // UseThermals
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 22: BODY_3D_EXTENDED - 3D body with color and opacity ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'BODY_3D_EXTENDED';
    Comp.Description := '3D body with custom color and opacity';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcb3DBodyExtended(Comp,
        MilsToCoord(10),   // StandoffHeight
        MilsToCoord(150),  // OverallHeight
        0,                 // BodyProjection (0 = extruded)
        $00FF00,           // BodyColor (green)
        0.8);              // BodyOpacity (80%)
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 23: REGION_WITH_HOLE - Region with internal hole ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'REGION_WITH_HOLE';
    Comp.Description := 'Region with internal hole/cutout';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    CreatePcbRegionWithHole(Comp, eTopLayer);
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    // === Footprint 24: DIMENSION_EXTENDED - Commented out (dimension creation via script not working) ===
    // Comp := PCBServer.CreatePCBLibComp;
    // Comp.Name := 'DIMENSION_EXTENDED';
    // Comp.Description := 'Dimension with TrueType font and extended properties';
    // PCBLib.RegisterComponent(Comp);
    // PCBServer.PreProcess;
    // CreatePcbDimensionExtended(Comp, ...);
    // FinalizeLibComponent(PCBLib, Comp);
    // PCBServer.PostProcess;

    // === Footprint 25: COMPLEX_EXTENDED - Complex footprint with extended primitives ===
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := 'COMPLEX_EXTENDED';
    Comp.Description := 'Complex footprint with all extended property types';
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;
    // Two extended pads with different properties
    CreatePcbPadExtended(Comp, '1', MilsToCoord(-75), MilsToCoord(0),
        MilsToCoord(50), MilsToCoord(50), MilsToCoord(25),
        eRounded, eMultiLayer, 0,
        True);  // Tented
    CreatePcbPadExtended(Comp, '2', MilsToCoord(75), MilsToCoord(0),
        MilsToCoord(50), MilsToCoord(50), MilsToCoord(25),
        eRounded, eMultiLayer, 0,
        False);  // No tenting
    // Outline tracks
    CreatePcbTrack(Comp, MilsToCoord(-120), MilsToCoord(-50),
        MilsToCoord(120), MilsToCoord(-50), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(-120), MilsToCoord(50),
        MilsToCoord(120), MilsToCoord(50), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(-120), MilsToCoord(-50),
        MilsToCoord(-120), MilsToCoord(50), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(120), MilsToCoord(-50),
        MilsToCoord(120), MilsToCoord(50), MilsToCoord(8), eTopOverlay);
    // Extended 3D body
    CreatePcb3DBodyExtended(Comp,
        MilsToCoord(0), MilsToCoord(80), 0,
        $0000FF, 0.9);  // Blue, 90% opacity
    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;

    PCBLib.Board.ViewManager_FullUpdate;
end;

{==============================================================================
  INDIVIDUAL PCB LIBRARY FILE GENERATION
==============================================================================}

// Forward declarations for individual footprint creators
procedure CreateFootprint_PAD_TH_ROUND(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_PAD_TH_RECTANGULAR(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_PAD_TH_OCTAGONAL(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_PAD_SMD_RECTANGULAR(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_PAD_SMD_ROUNDED(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_PAD_ROTATED(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_TRACKS_MULTILAYER(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_ARCS_TEST(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_FILLS_TEST(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_TEXT_TEST(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_TEXT_TRUETYPE(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_REGIONS_TEST(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_TEXT_INVERTED(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_TEXT_BARCODE(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_REGION_CUTOUT(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_BODY_3D(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_KEEPOUT_REGION(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_BODY_3D_STEP(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_PAD_EXTENDED(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_POLYGON_EXTENDED(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_BODY_3D_EXTENDED(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_REGION_WITH_HOLE(Comp: IPCB_LibComponent); forward;
// procedure CreateFootprint_DIMENSION_EXTENDED(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_COMPLEX_EXTENDED(Comp: IPCB_LibComponent); forward;

// New extended footprint forward declarations
procedure CreateFootprint_PAD_TH_OBLONG(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_PAD_TH_SLOT(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_PAD_SMD_ROUND(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_PAD_SMD_OBLONG(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_PAD_STACKED_MODES(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_PAD_HOLE_TYPES(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_PAD_THERMAL_RELIEF(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_VIA_TYPES(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_TRACK_WIDTHS(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_ARC_ANGLES(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_POLYGON_SOLID(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_POLYGON_HATCHED_90(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_POLYGON_HATCHED_45(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_TEXT_DESIGNATOR(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_TEXT_COMMENT(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_REGION_COPPER(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_REGION_KEEPOUT_TRACK(Comp: IPCB_LibComponent); forward;
// procedure CreateFootprint_DIMENSION_LINEAR(Comp: IPCB_LibComponent); forward;
// procedure CreateFootprint_DIMENSION_RADIAL(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_MULTIPIN_QFP(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_MULTIPIN_BGA(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_MULTIPIN_SOP(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_PAD_COUNTER_HOLE(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_PAD_BACK_DRILL(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_PAD_CUSTOM_SHAPE(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_VIA_BLIND_TOP(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_VIA_BURIED(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_REGION_SLOT(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_REGION_CAVITY(Comp: IPCB_LibComponent); forward;
// procedure CreateFootprint_DIMENSION_ANGULAR(Comp: IPCB_LibComponent); forward;
// procedure CreateFootprint_DIMENSION_BASELINE(Comp: IPCB_LibComponent); forward;
// procedure CreateFootprint_DIMENSION_CENTER(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_TEXT_ALL_STROKE_FONTS(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_TEXT_MULTILINE(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_POLYGON_DIRECT_CONNECT(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_POLYGON_THERMAL(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_POLYGON_ISLANDS(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_MULTIPIN_SOIC(Comp: IPCB_LibComponent); forward;
// Additional via types
procedure CreateFootprint_VIA_BLIND_BOTTOM(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_VIA_MICROVIA(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_VIA_TENTING(Comp: IPCB_LibComponent); forward;
// Additional track/arc types
procedure CreateFootprint_TRACK_ALL_LAYERS(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_TRACK_KEEPOUT(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_ARC_WIDTHS(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_ARC_FULL_CIRCLES(Comp: IPCB_LibComponent); forward;
// Additional region types
procedure CreateFootprint_REGION_COMPLEX(Comp: IPCB_LibComponent); forward;
// Additional dimension types
// procedure CreateFootprint_DIMENSION_DATUM(Comp: IPCB_LibComponent); forward;
// Additional text types
procedure CreateFootprint_TEXT_TRUETYPE_FONTS(Comp: IPCB_LibComponent); forward;
procedure CreateFootprint_TEXT_BARCODE_ALL(Comp: IPCB_LibComponent); forward;
// Additional polygon types
procedure CreateFootprint_POLYGON_OUTLINE(Comp: IPCB_LibComponent); forward;
// Special PCB objects
procedure CreateFootprint_COORDINATE(Comp: IPCB_LibComponent); forward;

// Individual footprint creators
procedure CreateFootprint_PAD_TH_ROUND(Comp: IPCB_LibComponent);
begin
    CreatePcbPad(Comp, '1', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(60), MilsToCoord(60), MilsToCoord(30),
        eRounded, eMultiLayer, 0);
end;

procedure CreateFootprint_PAD_TH_RECTANGULAR(Comp: IPCB_LibComponent);
begin
    CreatePcbPad(Comp, '1', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(80), MilsToCoord(50), MilsToCoord(30),
        eRectangular, eMultiLayer, 0);
end;

procedure CreateFootprint_PAD_TH_OCTAGONAL(Comp: IPCB_LibComponent);
begin
    CreatePcbPad(Comp, '1', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(70), MilsToCoord(70), MilsToCoord(30),
        eOctagonal, eMultiLayer, 0);
end;

procedure CreateFootprint_PAD_SMD_RECTANGULAR(Comp: IPCB_LibComponent);
begin
    CreatePcbPad(Comp, '1', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(50), MilsToCoord(30), 0,
        eRectangular, eTopLayer, 0);
end;

procedure CreateFootprint_PAD_SMD_ROUNDED(Comp: IPCB_LibComponent);
begin
    CreatePcbSmdRoundedRectPad(Comp, '1', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(50), MilsToCoord(30), eTopLayer, 0, 50);
end;

procedure CreateFootprint_PAD_ROTATED(Comp: IPCB_LibComponent);
begin
    CreatePcbPad(Comp, '1', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(60), MilsToCoord(40), MilsToCoord(25),
        eRectangular, eMultiLayer, 45);
end;

procedure CreateFootprint_TRACKS_MULTILAYER(Comp: IPCB_LibComponent);
begin
    CreatePcbTrack(Comp, MilsToCoord(-100), MilsToCoord(0),
        MilsToCoord(100), MilsToCoord(0), MilsToCoord(10), eTopLayer);
    CreatePcbTrack(Comp, MilsToCoord(-100), MilsToCoord(-50),
        MilsToCoord(100), MilsToCoord(-50), MilsToCoord(10), eBottomLayer);
    CreatePcbTrack(Comp, MilsToCoord(0), MilsToCoord(-100),
        MilsToCoord(0), MilsToCoord(100), MilsToCoord(8), eTopOverlay);
end;

procedure CreateFootprint_ARCS_TEST(Comp: IPCB_LibComponent);
begin
    CreatePcbArc(Comp, MilsToCoord(0), MilsToCoord(0), MilsToCoord(50),
        MilsToCoord(10), 0, 90, eTopLayer);
    CreatePcbArc(Comp, MilsToCoord(0), MilsToCoord(0), MilsToCoord(75),
        MilsToCoord(10), 90, 180, eTopLayer);
    CreatePcbArc(Comp, MilsToCoord(0), MilsToCoord(0), MilsToCoord(100),
        MilsToCoord(10), 0, 360, eTopLayer);
end;

procedure CreateFootprint_FILLS_TEST(Comp: IPCB_LibComponent);
begin
    CreatePcbFill(Comp, MilsToCoord(-50), MilsToCoord(-25),
        MilsToCoord(50), MilsToCoord(25), eTopLayer, 0);
    CreatePcbFill(Comp, MilsToCoord(-40), MilsToCoord(-60),
        MilsToCoord(40), MilsToCoord(-40), eBottomLayer, 45);
end;

procedure CreateFootprint_TEXT_TEST(Comp: IPCB_LibComponent);
begin
    CreatePcbText(Comp, MilsToCoord(0), MilsToCoord(0), 'TOP',
        MilsToCoord(50), MilsToCoord(5), eTopOverlay, 0, False);
    CreatePcbText(Comp, MilsToCoord(0), MilsToCoord(-75), 'ROTATED',
        MilsToCoord(40), MilsToCoord(4), eTopOverlay, 45, False);
    CreatePcbText(Comp, MilsToCoord(0), MilsToCoord(-150), 'MIRROR',
        MilsToCoord(40), MilsToCoord(4), eBottomOverlay, 0, True);
end;

procedure CreateFootprint_TEXT_TRUETYPE(Comp: IPCB_LibComponent);
begin
    CreatePcbTrueTypeText(Comp, MilsToCoord(0), MilsToCoord(0), 'Arial Normal',
        MilsToCoord(50), eTopOverlay, 0, False, 'Arial', False, False);
    CreatePcbTrueTypeText(Comp, MilsToCoord(0), MilsToCoord(-75), 'Arial Bold',
        MilsToCoord(50), eTopOverlay, 0, False, 'Arial', True, False);
    CreatePcbTrueTypeText(Comp, MilsToCoord(0), MilsToCoord(-150), 'Arial Italic',
        MilsToCoord(50), eTopOverlay, 0, False, 'Arial', False, True);
end;

procedure CreateFootprint_REGIONS_TEST(Comp: IPCB_LibComponent);
begin
    CreatePcbRegion(Comp, eTopLayer);
end;

procedure CreateFootprint_TEXT_INVERTED(Comp: IPCB_LibComponent);
begin
    CreatePcbFill(Comp, MilsToCoord(-150), MilsToCoord(-30),
        MilsToCoord(150), MilsToCoord(70), eTopLayer, 0);
    CreatePcbInvertedText(Comp, MilsToCoord(-100), MilsToCoord(0), 'INVERTED',
        MilsToCoord(50), MilsToCoord(5), eTopLayer, 0);
end;

procedure CreateFootprint_TEXT_BARCODE(Comp: IPCB_LibComponent);
begin
    CreatePcbBarcodeText(Comp, MilsToCoord(0), MilsToCoord(0), '123456',
        MilsToCoord(100), eTopOverlay, 0, 0);
end;

procedure CreateFootprint_REGION_CUTOUT(Comp: IPCB_LibComponent);
begin
    CreatePcbRegionCutout(Comp, eTopLayer);
end;

procedure CreateFootprint_BODY_3D(Comp: IPCB_LibComponent);
begin
    CreatePcb3DBody(Comp, MilsToCoord(0), MilsToCoord(100), 0);
end;

procedure CreateFootprint_KEEPOUT_REGION(Comp: IPCB_LibComponent);
begin
    CreatePcbKeepoutRegion(Comp, eKeepOutLayer);
    CreatePcbKeepoutTrack(Comp, MilsToCoord(-100), MilsToCoord(-60),
        MilsToCoord(100), MilsToCoord(-60), MilsToCoord(10), eKeepOutLayer);
end;

procedure CreateFootprint_BODY_3D_STEP(Comp: IPCB_LibComponent);
begin
    CreatePcb3DBodyFromStep(Comp, STEP_DIR + 'PSEMI QFN-24 4x4.step', 0, 0, 0, 0);
end;

procedure CreateFootprint_PAD_EXTENDED(Comp: IPCB_LibComponent);
begin
    CreatePcbPadExtended(Comp, '1', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(60), MilsToCoord(60), MilsToCoord(30),
        eRounded, eMultiLayer, 0,
        True);
end;

procedure CreateFootprint_POLYGON_EXTENDED(Comp: IPCB_LibComponent);
begin
    CreatePcbPolygonExtended(Comp, eTopLayer,
        ePolySolid, MilsToCoord(10), MilsToCoord(20),
        True, True, True);
end;

procedure CreateFootprint_BODY_3D_EXTENDED(Comp: IPCB_LibComponent);
begin
    CreatePcb3DBodyExtended(Comp,
        MilsToCoord(10), MilsToCoord(150), 0,
        $00FF00, 0.8);
end;

procedure CreateFootprint_REGION_WITH_HOLE(Comp: IPCB_LibComponent);
begin
    CreatePcbRegionWithHole(Comp, eTopLayer);
end;

{
procedure CreateFootprint_DIMENSION_EXTENDED(Comp: IPCB_LibComponent);
begin
    CreatePcbDimensionExtended(Comp,
        MilsToCoord(-100), MilsToCoord(0),
        MilsToCoord(100), MilsToCoord(0),
        MilsToCoord(30), eTopOverlay, True, 'Arial');
end;
}

procedure CreateFootprint_COMPLEX_EXTENDED(Comp: IPCB_LibComponent);
begin
    CreatePcbPadExtended(Comp, '1', MilsToCoord(-75), MilsToCoord(0),
        MilsToCoord(50), MilsToCoord(50), MilsToCoord(25),
        eRounded, eMultiLayer, 0, True);
    CreatePcbPadExtended(Comp, '2', MilsToCoord(75), MilsToCoord(0),
        MilsToCoord(50), MilsToCoord(50), MilsToCoord(25),
        eRounded, eMultiLayer, 0, False);
    CreatePcbTrack(Comp, MilsToCoord(-120), MilsToCoord(-50),
        MilsToCoord(120), MilsToCoord(-50), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(-120), MilsToCoord(50),
        MilsToCoord(120), MilsToCoord(50), MilsToCoord(8), eTopOverlay);
    CreatePcb3DBodyExtended(Comp, MilsToCoord(0), MilsToCoord(80), 0,
        $0000FF, 0.9);
end;

{==============================================================================
  NEW EXTENDED PCB FOOTPRINT CREATORS
  Additional footprint types for comprehensive API coverage
==============================================================================}

procedure CreateFootprint_PAD_TH_OBLONG(Comp: IPCB_LibComponent);
var
    Pad: IPCB_Pad;
begin
    // Create oblong through-hole pad
    Pad := PCBServer.PCBObjectFactory(ePadObject, eNoDimension, eCreate_Default);
    if Pad = nil then Exit;

    Pad.Name := '1';
    Pad.X := MilsToCoord(0);
    Pad.Y := MilsToCoord(0);
    Pad.TopXSize := MilsToCoord(80);
    Pad.TopYSize := MilsToCoord(50);
    Pad.HoleSize := MilsToCoord(25);
    Pad.HoleWidth := MilsToCoord(50);  // Slot width
    Pad.HoleType := eSlotHole;
    Pad.TopShape := eRounded;
    Pad.Layer := eMultiLayer;
    Pad.Plated := True;

    Comp.AddPCBObject(Pad);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Pad.I_ObjectAddress);
end;

procedure CreateFootprint_PAD_TH_SLOT(Comp: IPCB_LibComponent);
var
    Pad: IPCB_Pad;
begin
    // Create slotted through-hole pad
    Pad := PCBServer.PCBObjectFactory(ePadObject, eNoDimension, eCreate_Default);
    if Pad = nil then Exit;

    Pad.Name := '1';
    Pad.X := MilsToCoord(0);
    Pad.Y := MilsToCoord(0);
    Pad.TopXSize := MilsToCoord(100);
    Pad.TopYSize := MilsToCoord(40);
    Pad.HoleSize := MilsToCoord(20);
    Pad.HoleWidth := MilsToCoord(60);
    Pad.HoleType := eSlotHole;
    Pad.TopShape := eRectangular;
    Pad.Layer := eMultiLayer;
    Pad.Plated := True;
    Pad.HoleRotation := 0;

    Comp.AddPCBObject(Pad);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Pad.I_ObjectAddress);
end;

procedure CreateFootprint_PAD_SMD_ROUND(Comp: IPCB_LibComponent);
begin
    // Create round SMD pad
    CreatePcbPad(Comp, '1', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(40), MilsToCoord(40), 0,
        eRounded, eTopLayer, 0);
end;

procedure CreateFootprint_PAD_SMD_OBLONG(Comp: IPCB_LibComponent);
begin
    // Create oblong SMD pad
    CreatePcbSmdRoundedRectPad(Comp, '1', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(80), MilsToCoord(30), eTopLayer, 0, 100);  // 100% = fully rounded ends
end;

procedure CreateFootprint_PAD_STACKED_MODES(Comp: IPCB_LibComponent);
var
    Pad: IPCB_Pad;
begin
    // Pad with different shapes on top/mid/bottom layers
    Pad := PCBServer.PCBObjectFactory(ePadObject, eNoDimension, eCreate_Default);
    if Pad = nil then Exit;

    Pad.Name := '1';
    Pad.X := MilsToCoord(0);
    Pad.Y := MilsToCoord(0);
    Pad.HoleSize := MilsToCoord(30);
    Pad.Layer := eMultiLayer;
    Pad.Plated := True;

    // Set mode to Top-Mid-Bottom
    Pad.Mode := ePadMode_ExternalStack;

    // Top shape
    Pad.TopXSize := MilsToCoord(70);
    Pad.TopYSize := MilsToCoord(70);
    Pad.TopShape := eRounded;

    // Mid shape
    Pad.MidXSize := MilsToCoord(50);
    Pad.MidYSize := MilsToCoord(50);
    Pad.MidShape := eRounded;

    // Bottom shape
    Pad.BotXSize := MilsToCoord(80);
    Pad.BotYSize := MilsToCoord(80);
    Pad.BotShape := eOctagonal;

    Comp.AddPCBObject(Pad);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Pad.I_ObjectAddress);
end;

procedure CreateFootprint_PAD_HOLE_TYPES(Comp: IPCB_LibComponent);
var
    Pad: IPCB_Pad;
begin
    // Round hole
    CreatePcbPad(Comp, '1', MilsToCoord(-100), MilsToCoord(0),
        MilsToCoord(60), MilsToCoord(60), MilsToCoord(30),
        eRounded, eMultiLayer, 0);

    // Square hole
    Pad := PCBServer.PCBObjectFactory(ePadObject, eNoDimension, eCreate_Default);
    if Pad <> nil then
    begin
        Pad.Name := '2';
        Pad.X := MilsToCoord(0);
        Pad.Y := MilsToCoord(0);
        Pad.TopXSize := MilsToCoord(70);
        Pad.TopYSize := MilsToCoord(70);
        Pad.HoleSize := MilsToCoord(35);
        Pad.HoleType := eSquareHole;
        Pad.TopShape := eRectangular;
        Pad.Layer := eMultiLayer;
        Pad.Plated := True;
        Comp.AddPCBObject(Pad);
        PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
            PCBM_BoardRegisteration, Pad.I_ObjectAddress);
    end;

    // Slot hole
    Pad := PCBServer.PCBObjectFactory(ePadObject, eNoDimension, eCreate_Default);
    if Pad <> nil then
    begin
        Pad.Name := '3';
        Pad.X := MilsToCoord(100);
        Pad.Y := MilsToCoord(0);
        Pad.TopXSize := MilsToCoord(80);
        Pad.TopYSize := MilsToCoord(50);
        Pad.HoleSize := MilsToCoord(25);
        Pad.HoleWidth := MilsToCoord(50);
        Pad.HoleType := eSlotHole;
        Pad.TopShape := eRounded;
        Pad.Layer := eMultiLayer;
        Pad.Plated := True;
        Comp.AddPCBObject(Pad);
        PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
            PCBM_BoardRegisteration, Pad.I_ObjectAddress);
    end;
end;

procedure CreateFootprint_PAD_THERMAL_RELIEF(Comp: IPCB_LibComponent);
begin
    // 2-entry thermal relief
    CreatePcbPadExtended(Comp, '1', MilsToCoord(-100), MilsToCoord(0),
        MilsToCoord(60), MilsToCoord(60), MilsToCoord(30),
        eRounded, eMultiLayer, 0, False);

    // 4-entry thermal relief (default)
    CreatePcbPadExtended(Comp, '2', MilsToCoord(0), MilsToCoord(0),
        MilsToCoord(60), MilsToCoord(60), MilsToCoord(30),
        eRounded, eMultiLayer, 0, False);

    // Direct connect (no thermal)
    CreatePcbPadExtended(Comp, '3', MilsToCoord(100), MilsToCoord(0),
        MilsToCoord(60), MilsToCoord(60), MilsToCoord(30),
        eRounded, eMultiLayer, 0, False);
end;

procedure CreateFootprint_VIA_TYPES(Comp: IPCB_LibComponent);
var
    Via: IPCB_Via;
begin
    // Standard through-hole via
    Via := PCBServer.PCBObjectFactory(eViaObject, eNoDimension, eCreate_Default);
    if Via <> nil then
    begin
        Via.X := MilsToCoord(-100);
        Via.Y := MilsToCoord(0);
        Via.HoleSize := MilsToCoord(10);
        Via.Size := MilsToCoord(24);
        Via.LowLayer := eTopLayer;
        Via.HighLayer := eBottomLayer;
        Via.IsTenting := False;
        Comp.AddPCBObject(Via);
        PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
            PCBM_BoardRegisteration, Via.I_ObjectAddress);
    end;

    // Tented via (top only)
    Via := PCBServer.PCBObjectFactory(eViaObject, eNoDimension, eCreate_Default);
    if Via <> nil then
    begin
        Via.X := MilsToCoord(0);
        Via.Y := MilsToCoord(0);
        Via.HoleSize := MilsToCoord(8);
        Via.Size := MilsToCoord(20);
        Via.LowLayer := eTopLayer;
        Via.HighLayer := eBottomLayer;
        Via.IsTenting := True;
        Via.IsTenting_Top := True;
        Via.IsTenting_Bottom := False;
        Comp.AddPCBObject(Via);
        PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
            PCBM_BoardRegisteration, Via.I_ObjectAddress);
    end;

    // Tented via (both sides)
    Via := PCBServer.PCBObjectFactory(eViaObject, eNoDimension, eCreate_Default);
    if Via <> nil then
    begin
        Via.X := MilsToCoord(100);
        Via.Y := MilsToCoord(0);
        Via.HoleSize := MilsToCoord(6);
        Via.Size := MilsToCoord(18);
        Via.LowLayer := eTopLayer;
        Via.HighLayer := eBottomLayer;
        Via.IsTenting := True;
        Via.IsTenting_Top := True;
        Via.IsTenting_Bottom := True;
        Comp.AddPCBObject(Via);
        PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
            PCBM_BoardRegisteration, Via.I_ObjectAddress);
    end;
end;

procedure CreateFootprint_TRACK_WIDTHS(Comp: IPCB_LibComponent);
begin
    // Different track widths: 2, 4, 6, 8, 10, 20, 50 mils
    CreatePcbTrack(Comp, MilsToCoord(-100), MilsToCoord(100),
        MilsToCoord(100), MilsToCoord(100), MilsToCoord(2), eTopLayer);
    CreatePcbTrack(Comp, MilsToCoord(-100), MilsToCoord(75),
        MilsToCoord(100), MilsToCoord(75), MilsToCoord(4), eTopLayer);
    CreatePcbTrack(Comp, MilsToCoord(-100), MilsToCoord(50),
        MilsToCoord(100), MilsToCoord(50), MilsToCoord(6), eTopLayer);
    CreatePcbTrack(Comp, MilsToCoord(-100), MilsToCoord(25),
        MilsToCoord(100), MilsToCoord(25), MilsToCoord(8), eTopLayer);
    CreatePcbTrack(Comp, MilsToCoord(-100), MilsToCoord(0),
        MilsToCoord(100), MilsToCoord(0), MilsToCoord(10), eTopLayer);
    CreatePcbTrack(Comp, MilsToCoord(-100), MilsToCoord(-35),
        MilsToCoord(100), MilsToCoord(-35), MilsToCoord(20), eTopLayer);
    CreatePcbTrack(Comp, MilsToCoord(-100), MilsToCoord(-85),
        MilsToCoord(100), MilsToCoord(-85), MilsToCoord(50), eTopLayer);
end;

procedure CreateFootprint_ARC_ANGLES(Comp: IPCB_LibComponent);
begin
    // Arcs with different sweep angles
    CreatePcbArc(Comp, MilsToCoord(0), MilsToCoord(0), MilsToCoord(80),
        MilsToCoord(8), 0, 45, eTopLayer);      // 45 degrees
    CreatePcbArc(Comp, MilsToCoord(0), MilsToCoord(0), MilsToCoord(100),
        MilsToCoord(8), 0, 90, eTopLayer);      // 90 degrees
    CreatePcbArc(Comp, MilsToCoord(0), MilsToCoord(0), MilsToCoord(120),
        MilsToCoord(8), 0, 180, eTopLayer);     // 180 degrees
    CreatePcbArc(Comp, MilsToCoord(0), MilsToCoord(0), MilsToCoord(140),
        MilsToCoord(8), 0, 270, eTopLayer);     // 270 degrees
    CreatePcbArc(Comp, MilsToCoord(0), MilsToCoord(0), MilsToCoord(160),
        MilsToCoord(8), 0, 360, eTopLayer);     // Full circle
end;

procedure CreateFootprint_POLYGON_SOLID(Comp: IPCB_LibComponent);
begin
    CreatePcbPolygonExtended(Comp, eTopLayer,
        ePolySolid, MilsToCoord(10), MilsToCoord(20),
        True, True, True);
end;

procedure CreateFootprint_POLYGON_HATCHED_90(Comp: IPCB_LibComponent);
begin
    CreatePcbPolygonExtended(Comp, eTopLayer,
        ePolyHatch90, MilsToCoord(10), MilsToCoord(20),
        True, True, True);
end;

procedure CreateFootprint_POLYGON_HATCHED_45(Comp: IPCB_LibComponent);
begin
    CreatePcbPolygonExtended(Comp, eTopLayer,
        ePolyHatch45, MilsToCoord(10), MilsToCoord(20),
        True, True, True);
end;

procedure CreateFootprint_TEXT_DESIGNATOR(Comp: IPCB_LibComponent);
begin
    // Special .Designator text field
    CreatePcbText(Comp, MilsToCoord(0), MilsToCoord(0), '.Designator',
        MilsToCoord(50), MilsToCoord(5), eTopOverlay, 0, False);
end;

procedure CreateFootprint_TEXT_COMMENT(Comp: IPCB_LibComponent);
begin
    // Special .Comment text field
    CreatePcbText(Comp, MilsToCoord(0), MilsToCoord(0), '.Comment',
        MilsToCoord(50), MilsToCoord(5), eTopOverlay, 0, False);
end;

procedure CreateFootprint_REGION_COPPER(Comp: IPCB_LibComponent);
begin
    CreatePcbRegion(Comp, eTopLayer);
end;

procedure CreateFootprint_REGION_KEEPOUT_TRACK(Comp: IPCB_LibComponent);
begin
    // Keepout tracks forming a box
    CreatePcbKeepoutTrack(Comp, MilsToCoord(-100), MilsToCoord(-50),
        MilsToCoord(100), MilsToCoord(-50), MilsToCoord(10), eKeepOutLayer);
    CreatePcbKeepoutTrack(Comp, MilsToCoord(100), MilsToCoord(-50),
        MilsToCoord(100), MilsToCoord(50), MilsToCoord(10), eKeepOutLayer);
    CreatePcbKeepoutTrack(Comp, MilsToCoord(100), MilsToCoord(50),
        MilsToCoord(-100), MilsToCoord(50), MilsToCoord(10), eKeepOutLayer);
    CreatePcbKeepoutTrack(Comp, MilsToCoord(-100), MilsToCoord(50),
        MilsToCoord(-100), MilsToCoord(-50), MilsToCoord(10), eKeepOutLayer);
end;

{
procedure CreateFootprint_DIMENSION_LINEAR(Comp: IPCB_LibComponent);
begin
    CreatePcbDimension(Comp,
        MilsToCoord(-100), MilsToCoord(0),
        MilsToCoord(100), MilsToCoord(0),
        MilsToCoord(30), eTopOverlay);
end;

procedure CreateFootprint_DIMENSION_RADIAL(Comp: IPCB_LibComponent);
var
    Dimension: IPCB_Dimension;
begin
    Dimension := PCBServer.PCBObjectFactory(eDimensionObject, eNoDimension, eCreate_Default);
    if Dimension = nil then Exit;

    Dimension.DimensionKind := eDiameter;
    Dimension.Layer := eTopOverlay;
    Dimension.TextHeight := MilsToCoord(30);
    Dimension.LineWidth := MilsToCoord(5);
    Dimension.ArrowSize := MilsToCoord(20);

    Dimension.References_Add(Point(MilsToCoord(0), MilsToCoord(0)));
    Dimension.References_Add(Point(MilsToCoord(100), MilsToCoord(0)));

    Comp.AddPCBObject(Dimension);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Dimension.I_ObjectAddress);
end;
}

procedure CreateFootprint_MULTIPIN_QFP(Comp: IPCB_LibComponent);
var
    I: Integer;
    X, Y: TCoord;
begin
    // QFP-44 style footprint
    // Left side (pins 1-11)
    for I := 1 to 11 do
    begin
        Y := MilsToCoord(250) - MilsToCoord((I - 1) * 50);
        CreatePcbPad(Comp, IntToStr(I), MilsToCoord(-300), Y,
            MilsToCoord(60), MilsToCoord(20), 0,
            eRectangular, eTopLayer, 0);
    end;
    // Bottom side (pins 12-22)
    for I := 12 to 22 do
    begin
        X := MilsToCoord(-250) + MilsToCoord((I - 12) * 50);
        CreatePcbPad(Comp, IntToStr(I), X, MilsToCoord(-300),
            MilsToCoord(20), MilsToCoord(60), 0,
            eRectangular, eTopLayer, 0);
    end;
    // Right side (pins 23-33)
    for I := 23 to 33 do
    begin
        Y := MilsToCoord(-250) + MilsToCoord((I - 23) * 50);
        CreatePcbPad(Comp, IntToStr(I), MilsToCoord(300), Y,
            MilsToCoord(60), MilsToCoord(20), 0,
            eRectangular, eTopLayer, 0);
    end;
    // Top side (pins 34-44)
    for I := 34 to 44 do
    begin
        X := MilsToCoord(250) - MilsToCoord((I - 34) * 50);
        CreatePcbPad(Comp, IntToStr(I), X, MilsToCoord(300),
            MilsToCoord(20), MilsToCoord(60), 0,
            eRectangular, eTopLayer, 0);
    end;

    // Add outline
    CreatePcbTrack(Comp, MilsToCoord(-250), MilsToCoord(-250),
        MilsToCoord(250), MilsToCoord(-250), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(250), MilsToCoord(-250),
        MilsToCoord(250), MilsToCoord(250), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(250), MilsToCoord(250),
        MilsToCoord(-250), MilsToCoord(250), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(-250), MilsToCoord(250),
        MilsToCoord(-250), MilsToCoord(-250), MilsToCoord(8), eTopOverlay);

    // Pin 1 marker
    CreatePcbArc(Comp, MilsToCoord(-200), MilsToCoord(200), MilsToCoord(20),
        MilsToCoord(4), 0, 360, eTopOverlay);
end;

procedure CreateFootprint_MULTIPIN_BGA(Comp: IPCB_LibComponent);
var
    Row, Col: Integer;
    X, Y: TCoord;
    PinName: String;
begin
    // Simple 6x6 BGA footprint
    for Row := 0 to 5 do
    begin
        for Col := 0 to 5 do
        begin
            X := MilsToCoord(-125) + MilsToCoord(Col * 50);
            Y := MilsToCoord(125) - MilsToCoord(Row * 50);
            PinName := Chr(Ord('A') + Row) + IntToStr(Col + 1);
            CreatePcbPad(Comp, PinName, X, Y,
                MilsToCoord(25), MilsToCoord(25), 0,
                eRounded, eTopLayer, 0);
        end;
    end;

    // Add outline
    CreatePcbTrack(Comp, MilsToCoord(-175), MilsToCoord(-175),
        MilsToCoord(175), MilsToCoord(-175), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(175), MilsToCoord(-175),
        MilsToCoord(175), MilsToCoord(175), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(175), MilsToCoord(175),
        MilsToCoord(-175), MilsToCoord(175), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(-175), MilsToCoord(175),
        MilsToCoord(-175), MilsToCoord(-175), MilsToCoord(8), eTopOverlay);

    // Pin A1 marker
    CreatePcbArc(Comp, MilsToCoord(-140), MilsToCoord(140), MilsToCoord(15),
        MilsToCoord(4), 0, 360, eTopOverlay);
end;

procedure CreateFootprint_MULTIPIN_SOP(Comp: IPCB_LibComponent);
var
    I: Integer;
begin
    // SOP-8 style footprint
    for I := 1 to 4 do
    begin
        CreatePcbPad(Comp, IntToStr(I),
            MilsToCoord(-75) + MilsToCoord((I - 1) * 50), MilsToCoord(-135),
            MilsToCoord(25), MilsToCoord(60), 0,
            eRectangular, eTopLayer, 0);
        CreatePcbPad(Comp, IntToStr(9 - I),
            MilsToCoord(-75) + MilsToCoord((I - 1) * 50), MilsToCoord(135),
            MilsToCoord(25), MilsToCoord(60), 0,
            eRectangular, eTopLayer, 0);
    end;

    // Add outline
    CreatePcbTrack(Comp, MilsToCoord(-125), MilsToCoord(-100),
        MilsToCoord(125), MilsToCoord(-100), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(125), MilsToCoord(-100),
        MilsToCoord(125), MilsToCoord(100), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(125), MilsToCoord(100),
        MilsToCoord(-125), MilsToCoord(100), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(-125), MilsToCoord(100),
        MilsToCoord(-125), MilsToCoord(-100), MilsToCoord(8), eTopOverlay);

    // Pin 1 marker
    CreatePcbArc(Comp, MilsToCoord(-100), MilsToCoord(-70), MilsToCoord(10),
        MilsToCoord(3), 0, 360, eTopOverlay);
end;

procedure CreateFootprint_PAD_COUNTER_HOLE(Comp: IPCB_LibComponent);
var
    Pad: IPCB_Pad;
begin
    // Counter-bored pad
    Pad := PCBServer.PCBObjectFactory(ePadObject, eNoDimension, eCreate_Default);
    if Pad = nil then Exit;
    Pad.Name := '1';
    Pad.X := MilsToCoord(0);
    Pad.Y := MilsToCoord(0);
    Pad.TopXSize := MilsToCoord(80);
    Pad.TopYSize := MilsToCoord(80);
    Pad.HoleSize := MilsToCoord(40);
    Pad.TopShape := eRounded;
    Pad.Layer := eMultiLayer;
    Pad.Plated := True;
    // Counter hole properties would be set via extended API
    Comp.AddPCBObject(Pad);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Pad.I_ObjectAddress);
end;

procedure CreateFootprint_PAD_BACK_DRILL(Comp: IPCB_LibComponent);
var
    Pad: IPCB_Pad;
begin
    // Back-drilled via pad (used for high-speed designs)
    Pad := PCBServer.PCBObjectFactory(ePadObject, eNoDimension, eCreate_Default);
    if Pad = nil then Exit;
    Pad.Name := '1';
    Pad.X := MilsToCoord(0);
    Pad.Y := MilsToCoord(0);
    Pad.TopXSize := MilsToCoord(60);
    Pad.TopYSize := MilsToCoord(60);
    Pad.HoleSize := MilsToCoord(30);
    Pad.TopShape := eRounded;
    Pad.Layer := eMultiLayer;
    Pad.Plated := True;
    // Back drilling properties would be set via extended API
    Comp.AddPCBObject(Pad);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Pad.I_ObjectAddress);
end;

procedure CreateFootprint_PAD_CUSTOM_SHAPE(Comp: IPCB_LibComponent);
var
    Pad: IPCB_Pad;
begin
    // Custom shaped pad (octagon approximation)
    Pad := PCBServer.PCBObjectFactory(ePadObject, eNoDimension, eCreate_Default);
    if Pad = nil then Exit;
    Pad.Name := '1';
    Pad.X := MilsToCoord(0);
    Pad.Y := MilsToCoord(0);
    Pad.TopXSize := MilsToCoord(100);
    Pad.TopYSize := MilsToCoord(100);
    Pad.HoleSize := MilsToCoord(50);
    Pad.TopShape := eOctagonal;
    Pad.Layer := eMultiLayer;
    Pad.Plated := True;
    Comp.AddPCBObject(Pad);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Pad.I_ObjectAddress);
end;

procedure CreateFootprint_VIA_BLIND_TOP(Comp: IPCB_LibComponent);
var
    Via: IPCB_Via;
begin
    // Blind via from top layer
    Via := PCBServer.PCBObjectFactory(eViaObject, eNoDimension, eCreate_Default);
    if Via = nil then Exit;
    Via.X := MilsToCoord(0);
    Via.Y := MilsToCoord(0);
    Via.Size := MilsToCoord(30);
    Via.HoleSize := MilsToCoord(15);
    Via.LowLayer := eTopLayer;
    Via.HighLayer := eMidLayer1;
    Comp.AddPCBObject(Via);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Via.I_ObjectAddress);
end;

procedure CreateFootprint_VIA_BURIED(Comp: IPCB_LibComponent);
var
    Via: IPCB_Via;
begin
    // Buried via (inner layer to inner layer)
    Via := PCBServer.PCBObjectFactory(eViaObject, eNoDimension, eCreate_Default);
    if Via = nil then Exit;
    Via.X := MilsToCoord(0);
    Via.Y := MilsToCoord(0);
    Via.Size := MilsToCoord(25);
    Via.HoleSize := MilsToCoord(12);
    Via.LowLayer := eMidLayer1;
    Via.HighLayer := eMidLayer2;
    Comp.AddPCBObject(Via);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Via.I_ObjectAddress);
end;

procedure CreateFootprint_REGION_SLOT(Comp: IPCB_LibComponent);
var
    Region: IPCB_Region;
    Contour: IPCB_Contour;
begin
    // Slot-shaped region (elongated cutout)
    Region := PCBServer.PCBObjectFactory(eRegionObject, eNoDimension, eCreate_Default);
    if Region = nil then Exit;

    Region.Layer := eMultiLayer;
    Region.Kind := eRegionKind_Cutout;

    // Create slot contour (elongated rectangle)
    Contour := PCBServer.PCBContourFactory;
    Contour.AddPoint(MilsToCoord(-100), MilsToCoord(-25));
    Contour.AddPoint(MilsToCoord(100), MilsToCoord(-25));
    Contour.AddPoint(MilsToCoord(100), MilsToCoord(25));
    Contour.AddPoint(MilsToCoord(-100), MilsToCoord(25));
    Region.SetOutlineContour(Contour);

    Comp.AddPCBObject(Region);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Region.I_ObjectAddress);
end;

procedure CreateFootprint_REGION_CAVITY(Comp: IPCB_LibComponent);
var
    Region: IPCB_Region;
    Contour: IPCB_Contour;
begin
    // Cavity region (for embedded components)
    Region := PCBServer.PCBObjectFactory(eRegionObject, eNoDimension, eCreate_Default);
    if Region = nil then Exit;

    Region.Layer := eTopLayer;
    Region.Kind := eRegionKind_Copper;
    Region.CavityHeight := MilsToCoord(50);

    // Create cavity contour
    Contour := PCBServer.PCBContourFactory;
    Contour.AddPoint(MilsToCoord(-150), MilsToCoord(-100));
    Contour.AddPoint(MilsToCoord(150), MilsToCoord(-100));
    Contour.AddPoint(MilsToCoord(150), MilsToCoord(100));
    Contour.AddPoint(MilsToCoord(-150), MilsToCoord(100));
    Region.SetOutlineContour(Contour);

    Comp.AddPCBObject(Region);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Region.I_ObjectAddress);
end;

{
procedure CreateFootprint_DIMENSION_ANGULAR(Comp: IPCB_LibComponent);
var
    Dimension: IPCB_Dimension;
begin
    Dimension := PCBServer.PCBObjectFactory(eDimensionObject, eNoDimension, eCreate_Default);
    if Dimension = nil then Exit;

    Dimension.DimensionKind := eAngular;
    Dimension.Layer := eTopOverlay;
    Dimension.TextHeight := MilsToCoord(30);
    Dimension.LineWidth := MilsToCoord(5);
    Dimension.ArrowSize := MilsToCoord(20);

    Dimension.References_Add(Point(MilsToCoord(0), MilsToCoord(0)));
    Dimension.References_Add(Point(MilsToCoord(100), MilsToCoord(0)));
    Dimension.References_Add(Point(MilsToCoord(0), MilsToCoord(100)));

    Comp.AddPCBObject(Dimension);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Dimension.I_ObjectAddress);
end;

procedure CreateFootprint_DIMENSION_BASELINE(Comp: IPCB_LibComponent);
var
    Dimension: IPCB_Dimension;
begin
    Dimension := PCBServer.PCBObjectFactory(eDimensionObject, eNoDimension, eCreate_Default);
    if Dimension = nil then Exit;

    Dimension.DimensionKind := eBaseline;
    Dimension.Layer := eTopOverlay;
    Dimension.TextHeight := MilsToCoord(30);
    Dimension.LineWidth := MilsToCoord(5);
    Dimension.ArrowSize := MilsToCoord(20);

    Dimension.References_Add(Point(MilsToCoord(-100), MilsToCoord(0)));
    Dimension.References_Add(Point(MilsToCoord(0), MilsToCoord(0)));
    Dimension.References_Add(Point(MilsToCoord(100), MilsToCoord(0)));

    Comp.AddPCBObject(Dimension);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Dimension.I_ObjectAddress);
end;

procedure CreateFootprint_DIMENSION_CENTER(Comp: IPCB_LibComponent);
var
    Dimension: IPCB_Dimension;
begin
    Dimension := PCBServer.PCBObjectFactory(eDimensionObject, eNoDimension, eCreate_Default);
    if Dimension = nil then Exit;

    Dimension.DimensionKind := eCenter;
    Dimension.Layer := eTopOverlay;
    Dimension.TextHeight := MilsToCoord(30);
    Dimension.LineWidth := MilsToCoord(5);

    Dimension.References_Add(Point(MilsToCoord(0), MilsToCoord(0)));

    Comp.AddPCBObject(Dimension);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Dimension.I_ObjectAddress);
end;
}

procedure CreateFootprint_TEXT_ALL_STROKE_FONTS(Comp: IPCB_LibComponent);
var
    I: Integer;
    Text: IPCB_Text;
begin
    // Create text with various stroke font IDs
    for I := 0 to 7 do
    begin
        Text := PCBServer.PCBObjectFactory(eTextObject, eNoDimension, eCreate_Default);
        if Text = nil then Continue;

        Text.XLocation := MilsToCoord(-100);
        Text.YLocation := MilsToCoord(200 - I * 50);
        Text.Text := 'Font ' + IntToStr(I);
        Text.Size := MilsToCoord(40);
        Text.Width := MilsToCoord(4);
        Text.Layer := eTopOverlay;
        Text.FontID := I;
        Text.UseTTFonts := False;

        Comp.AddPCBObject(Text);
        PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
            PCBM_BoardRegisteration, Text.I_ObjectAddress);
    end;
end;

procedure CreateFootprint_TEXT_MULTILINE(Comp: IPCB_LibComponent);
var
    Text: IPCB_Text;
begin
    Text := PCBServer.PCBObjectFactory(eTextObject, eNoDimension, eCreate_Default);
    if Text = nil then Exit;

    Text.XLocation := MilsToCoord(0);
    Text.YLocation := MilsToCoord(0);
    Text.Text := 'Line 1' + #13#10 + 'Line 2' + #13#10 + 'Line 3';
    Text.Size := MilsToCoord(40);
    Text.Width := MilsToCoord(4);
    Text.Layer := eTopOverlay;
    Comp.AddPCBObject(Text);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Text.I_ObjectAddress);

    // Multiline properties must be set via SetState methods after registration
    // WordWrap is read-only on IPCB_Text (only settable on schematic text frames)
    Text.SetState_Multiline(True);
    Text.MultilineTextWidth := MilsToCoord(200);
    Text.MultilineTextHeight := MilsToCoord(150);
end;

procedure CreateFootprint_POLYGON_DIRECT_CONNECT(Comp: IPCB_LibComponent);
begin
    CreatePcbPolygonExtended(Comp, eTopLayer,
        ePolySolid, MilsToCoord(10), MilsToCoord(20),
        True, True, True);
    // Direct connect is set via PowerPlaneConnectStyle
end;

procedure CreateFootprint_POLYGON_THERMAL(Comp: IPCB_LibComponent);
begin
    CreatePcbPolygonExtended(Comp, eTopLayer,
        ePolySolid, MilsToCoord(10), MilsToCoord(20),
        True, True, True);
    // Thermal relief entries are set via ReliefEntries
end;

procedure CreateFootprint_POLYGON_ISLANDS(Comp: IPCB_LibComponent);
begin
    CreatePcbPolygonExtended(Comp, eTopLayer,
        ePolySolid, MilsToCoord(10), MilsToCoord(20),
        True, True, True);
    // Island removal is set via RemoveIslandsByArea
end;

procedure CreateFootprint_MULTIPIN_SOIC(Comp: IPCB_LibComponent);
var
    I: Integer;
begin
    // SOIC-16 style footprint
    for I := 1 to 8 do
    begin
        CreatePcbPad(Comp, IntToStr(I),
            MilsToCoord(-175) + MilsToCoord((I - 1) * 50), MilsToCoord(-200),
            MilsToCoord(25), MilsToCoord(60), 0,
            eRectangular, eTopLayer, 0);
        CreatePcbPad(Comp, IntToStr(17 - I),
            MilsToCoord(-175) + MilsToCoord((I - 1) * 50), MilsToCoord(200),
            MilsToCoord(25), MilsToCoord(60), 0,
            eRectangular, eTopLayer, 0);
    end;

    // Add outline
    CreatePcbTrack(Comp, MilsToCoord(-200), MilsToCoord(-150),
        MilsToCoord(200), MilsToCoord(-150), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(200), MilsToCoord(-150),
        MilsToCoord(200), MilsToCoord(150), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(200), MilsToCoord(150),
        MilsToCoord(-200), MilsToCoord(150), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(-200), MilsToCoord(150),
        MilsToCoord(-200), MilsToCoord(-150), MilsToCoord(8), eTopOverlay);

    // Pin 1 marker
    CreatePcbArc(Comp, MilsToCoord(-175), MilsToCoord(-120), MilsToCoord(15),
        MilsToCoord(3), 0, 360, eTopOverlay);
end;

procedure CreateFootprint_VIA_BLIND_BOTTOM(Comp: IPCB_LibComponent);
var
    Via: IPCB_Via;
begin
    // Blind via from bottom layer
    Via := PCBServer.PCBObjectFactory(eViaObject, eNoDimension, eCreate_Default);
    if Via = nil then Exit;
    Via.X := MilsToCoord(0);
    Via.Y := MilsToCoord(0);
    Via.Size := MilsToCoord(30);
    Via.HoleSize := MilsToCoord(15);
    Via.LowLayer := eMidLayer1;
    Via.HighLayer := eBottomLayer;
    Comp.AddPCBObject(Via);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Via.I_ObjectAddress);
end;

procedure CreateFootprint_VIA_MICROVIA(Comp: IPCB_LibComponent);
var
    Via: IPCB_Via;
begin
    // Microvia (very small, single layer span)
    Via := PCBServer.PCBObjectFactory(eViaObject, eNoDimension, eCreate_Default);
    if Via = nil then Exit;
    Via.X := MilsToCoord(0);
    Via.Y := MilsToCoord(0);
    Via.Size := MilsToCoord(12);
    Via.HoleSize := MilsToCoord(6);
    Via.LowLayer := eTopLayer;
    Via.HighLayer := eMidLayer1;
    Comp.AddPCBObject(Via);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Via.I_ObjectAddress);
end;

procedure CreateFootprint_VIA_TENTING(Comp: IPCB_LibComponent);
var
    Via: IPCB_Via;
begin
    // Via with all tenting options
    // Top tented
    Via := PCBServer.PCBObjectFactory(eViaObject, eNoDimension, eCreate_Default);
    if Via = nil then Exit;
    Via.X := MilsToCoord(-100);
    Via.Y := MilsToCoord(0);
    Via.Size := MilsToCoord(30);
    Via.HoleSize := MilsToCoord(15);
    Via.LowLayer := eTopLayer;
    Via.HighLayer := eBottomLayer;
    Via.IsTenting_Top := True;
    Via.IsTenting_Bottom := False;
    Comp.AddPCBObject(Via);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Via.I_ObjectAddress);

    // Bottom tented
    Via := PCBServer.PCBObjectFactory(eViaObject, eNoDimension, eCreate_Default);
    if Via = nil then Exit;
    Via.X := MilsToCoord(0);
    Via.Y := MilsToCoord(0);
    Via.Size := MilsToCoord(30);
    Via.HoleSize := MilsToCoord(15);
    Via.LowLayer := eTopLayer;
    Via.HighLayer := eBottomLayer;
    Via.IsTenting_Top := False;
    Via.IsTenting_Bottom := True;
    Comp.AddPCBObject(Via);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Via.I_ObjectAddress);

    // Both tented
    Via := PCBServer.PCBObjectFactory(eViaObject, eNoDimension, eCreate_Default);
    if Via = nil then Exit;
    Via.X := MilsToCoord(100);
    Via.Y := MilsToCoord(0);
    Via.Size := MilsToCoord(30);
    Via.HoleSize := MilsToCoord(15);
    Via.LowLayer := eTopLayer;
    Via.HighLayer := eBottomLayer;
    Via.IsTenting_Top := True;
    Via.IsTenting_Bottom := True;
    Comp.AddPCBObject(Via);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Via.I_ObjectAddress);
end;

procedure CreateFootprint_TRACK_ALL_LAYERS(Comp: IPCB_LibComponent);
begin
    // Tracks on all signal layers
    CreatePcbTrack(Comp, MilsToCoord(-200), MilsToCoord(150),
        MilsToCoord(200), MilsToCoord(150), MilsToCoord(10), eTopLayer);
    CreatePcbTrack(Comp, MilsToCoord(-200), MilsToCoord(100),
        MilsToCoord(200), MilsToCoord(100), MilsToCoord(10), eMidLayer1);
    CreatePcbTrack(Comp, MilsToCoord(-200), MilsToCoord(50),
        MilsToCoord(200), MilsToCoord(50), MilsToCoord(10), eMidLayer2);
    CreatePcbTrack(Comp, MilsToCoord(-200), MilsToCoord(0),
        MilsToCoord(200), MilsToCoord(0), MilsToCoord(10), eBottomLayer);
    // Overlay tracks
    CreatePcbTrack(Comp, MilsToCoord(-200), MilsToCoord(-50),
        MilsToCoord(200), MilsToCoord(-50), MilsToCoord(8), eTopOverlay);
    CreatePcbTrack(Comp, MilsToCoord(-200), MilsToCoord(-100),
        MilsToCoord(200), MilsToCoord(-100), MilsToCoord(8), eBottomOverlay);
end;

procedure CreateFootprint_TRACK_KEEPOUT(Comp: IPCB_LibComponent);
var
    Track: IPCB_Track;
begin
    // Keepout track forming a boundary
    Track := PCBServer.PCBObjectFactory(eTrackObject, eNoDimension, eCreate_Default);
    if Track = nil then Exit;
    Track.X1 := MilsToCoord(-100);
    Track.Y1 := MilsToCoord(-100);
    Track.X2 := MilsToCoord(100);
    Track.Y2 := MilsToCoord(-100);
    Track.Width := MilsToCoord(10);
    Track.Layer := eKeepOutLayer;
    Comp.AddPCBObject(Track);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Track.I_ObjectAddress);

    Track := PCBServer.PCBObjectFactory(eTrackObject, eNoDimension, eCreate_Default);
    Track.X1 := MilsToCoord(100);
    Track.Y1 := MilsToCoord(-100);
    Track.X2 := MilsToCoord(100);
    Track.Y2 := MilsToCoord(100);
    Track.Width := MilsToCoord(10);
    Track.Layer := eKeepOutLayer;
    Comp.AddPCBObject(Track);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Track.I_ObjectAddress);

    Track := PCBServer.PCBObjectFactory(eTrackObject, eNoDimension, eCreate_Default);
    Track.X1 := MilsToCoord(100);
    Track.Y1 := MilsToCoord(100);
    Track.X2 := MilsToCoord(-100);
    Track.Y2 := MilsToCoord(100);
    Track.Width := MilsToCoord(10);
    Track.Layer := eKeepOutLayer;
    Comp.AddPCBObject(Track);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Track.I_ObjectAddress);

    Track := PCBServer.PCBObjectFactory(eTrackObject, eNoDimension, eCreate_Default);
    Track.X1 := MilsToCoord(-100);
    Track.Y1 := MilsToCoord(100);
    Track.X2 := MilsToCoord(-100);
    Track.Y2 := MilsToCoord(-100);
    Track.Width := MilsToCoord(10);
    Track.Layer := eKeepOutLayer;
    Comp.AddPCBObject(Track);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Track.I_ObjectAddress);
end;

procedure CreateFootprint_ARC_WIDTHS(Comp: IPCB_LibComponent);
begin
    // Arcs with various widths
    CreatePcbArc(Comp, MilsToCoord(-150), MilsToCoord(0), MilsToCoord(80),
        MilsToCoord(2), 0, 270, eTopLayer);
    CreatePcbArc(Comp, MilsToCoord(-50), MilsToCoord(0), MilsToCoord(80),
        MilsToCoord(5), 0, 270, eTopLayer);
    CreatePcbArc(Comp, MilsToCoord(50), MilsToCoord(0), MilsToCoord(80),
        MilsToCoord(10), 0, 270, eTopLayer);
    CreatePcbArc(Comp, MilsToCoord(150), MilsToCoord(0), MilsToCoord(80),
        MilsToCoord(20), 0, 270, eTopLayer);
end;

procedure CreateFootprint_ARC_FULL_CIRCLES(Comp: IPCB_LibComponent);
begin
    // Full circles (360 degree arcs)
    CreatePcbArc(Comp, MilsToCoord(-100), MilsToCoord(0), MilsToCoord(50),
        MilsToCoord(8), 0, 360, eTopLayer);
    CreatePcbArc(Comp, MilsToCoord(0), MilsToCoord(0), MilsToCoord(75),
        MilsToCoord(8), 0, 360, eTopLayer);
    CreatePcbArc(Comp, MilsToCoord(100), MilsToCoord(0), MilsToCoord(100),
        MilsToCoord(8), 0, 360, eTopOverlay);
end;

procedure CreateFootprint_REGION_COMPLEX(Comp: IPCB_LibComponent);
var
    Region: IPCB_Region;
    MainContour, HoleContour: IPCB_Contour;
begin
    // Complex region with multiple holes
    Region := PCBServer.PCBObjectFactory(eRegionObject, eNoDimension, eCreate_Default);
    if Region = nil then Exit;

    Region.Layer := eTopLayer;
    Region.Kind := eRegionKind_Copper;

    // Main outline
    MainContour := PCBServer.PCBContourFactory;
    MainContour.AddPoint(MilsToCoord(-200), MilsToCoord(-150));
    MainContour.AddPoint(MilsToCoord(200), MilsToCoord(-150));
    MainContour.AddPoint(MilsToCoord(200), MilsToCoord(150));
    MainContour.AddPoint(MilsToCoord(-200), MilsToCoord(150));
    Region.SetOutlineContour(MainContour);

    // Add hole 1
    HoleContour := PCBServer.PCBContourFactory;
    HoleContour.AddPoint(MilsToCoord(-100), MilsToCoord(-50));
    HoleContour.AddPoint(MilsToCoord(-50), MilsToCoord(-50));
    HoleContour.AddPoint(MilsToCoord(-50), MilsToCoord(50));
    HoleContour.AddPoint(MilsToCoord(-100), MilsToCoord(50));
    Region.GeometricPolygon.AddContourIsHole(HoleContour, True);

    // Add hole 2
    HoleContour := PCBServer.PCBContourFactory;
    HoleContour.AddPoint(MilsToCoord(50), MilsToCoord(-50));
    HoleContour.AddPoint(MilsToCoord(100), MilsToCoord(-50));
    HoleContour.AddPoint(MilsToCoord(100), MilsToCoord(50));
    HoleContour.AddPoint(MilsToCoord(50), MilsToCoord(50));
    Region.GeometricPolygon.AddContourIsHole(HoleContour, True);

    Comp.AddPCBObject(Region);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Region.I_ObjectAddress);
end;

{
procedure CreateFootprint_DIMENSION_DATUM(Comp: IPCB_LibComponent);
var
    Dimension: IPCB_Dimension;
begin
    Dimension := PCBServer.PCBObjectFactory(eDimensionObject, eNoDimension, eCreate_Default);
    if Dimension = nil then Exit;

    Dimension.DimensionKind := eDatum;
    Dimension.Layer := eTopOverlay;
    Dimension.TextHeight := MilsToCoord(30);
    Dimension.LineWidth := MilsToCoord(5);

    Dimension.References_Add(Point(MilsToCoord(0), MilsToCoord(0)));

    Comp.AddPCBObject(Dimension);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Dimension.I_ObjectAddress);
end;
}

procedure CreateFootprint_TEXT_TRUETYPE_FONTS(Comp: IPCB_LibComponent);
var
    Text: IPCB_Text;
begin
    // TrueType font text
    Text := PCBServer.PCBObjectFactory(eTextObject, eNoDimension, eCreate_Default);
    if Text = nil then Exit;

    Text.XLocation := MilsToCoord(-100);
    Text.YLocation := MilsToCoord(100);
    Text.Text := 'Arial TTF';
    Text.Size := MilsToCoord(50);
    Text.Layer := eTopOverlay;
    Text.UseTTFonts := True;
    Text.FontName := 'Arial';

    Comp.AddPCBObject(Text);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Text.I_ObjectAddress);

    // Bold TrueType
    Text := PCBServer.PCBObjectFactory(eTextObject, eNoDimension, eCreate_Default);
    if Text = nil then Exit;

    Text.XLocation := MilsToCoord(-100);
    Text.YLocation := MilsToCoord(0);
    Text.Text := 'Bold TTF';
    Text.Size := MilsToCoord(50);
    Text.Layer := eTopOverlay;
    Text.UseTTFonts := True;
    Text.FontName := 'Arial';
    Text.Bold := True;

    Comp.AddPCBObject(Text);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Text.I_ObjectAddress);

    // Italic TrueType
    Text := PCBServer.PCBObjectFactory(eTextObject, eNoDimension, eCreate_Default);
    if Text = nil then Exit;

    Text.XLocation := MilsToCoord(-100);
    Text.YLocation := MilsToCoord(-100);
    Text.Text := 'Italic TTF';
    Text.Size := MilsToCoord(50);
    Text.Layer := eTopOverlay;
    Text.UseTTFonts := True;
    Text.FontName := 'Arial';
    Text.Italic := True;

    Comp.AddPCBObject(Text);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Text.I_ObjectAddress);
end;

procedure CreateFootprint_TEXT_BARCODE_ALL(Comp: IPCB_LibComponent);
var
    Text: IPCB_Text;
begin
    // Code 39 barcode
    Text := PCBServer.PCBObjectFactory(eTextObject, eNoDimension, eCreate_Default);
    if Text = nil then Exit;

    Text.XLocation := MilsToCoord(-200);
    Text.YLocation := MilsToCoord(150);
    Text.Text := '12345';
    Text.Layer := eTopOverlay;
    Text.TextKind := eText_Barcode;
    Text.BarCodeKind := 0; // Code 39
    Text.BarCodeFullWidth := MilsToCoord(200);
    Text.BarCodeFullHeight := MilsToCoord(100);
    Text.BarCodeShowText := True;

    Comp.AddPCBObject(Text);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Text.I_ObjectAddress);

    // Code 128 barcode
    Text := PCBServer.PCBObjectFactory(eTextObject, eNoDimension, eCreate_Default);
    if Text = nil then Exit;

    Text.XLocation := MilsToCoord(-200);
    Text.YLocation := MilsToCoord(0);
    Text.Text := 'ABC123';
    Text.Layer := eTopOverlay;
    Text.TextKind := eText_Barcode;
    Text.BarCodeKind := 1; // Code 128
    Text.BarCodeFullWidth := MilsToCoord(200);
    Text.BarCodeFullHeight := MilsToCoord(100);
    Text.BarCodeShowText := True;

    Comp.AddPCBObject(Text);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Text.I_ObjectAddress);

    // QR code
    Text := PCBServer.PCBObjectFactory(eTextObject, eNoDimension, eCreate_Default);
    if Text = nil then Exit;

    Text.XLocation := MilsToCoord(-200);
    Text.YLocation := MilsToCoord(-150);
    Text.Text := 'https://test.com';
    Text.Layer := eTopOverlay;
    Text.TextKind := eText_Barcode;
    Text.BarCodeKind := 4; // QR Code
    Text.BarCodeFullWidth := MilsToCoord(150);
    Text.BarCodeFullHeight := MilsToCoord(150);
    Text.BarCodeShowText := False;

    Comp.AddPCBObject(Text);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Text.I_ObjectAddress);
end;

procedure CreateFootprint_POLYGON_OUTLINE(Comp: IPCB_LibComponent);
var
    Polygon: IPCB_Polygon;
    Segment: TPolySegment;
begin
    // Outline-only polygon (no fill)
    Polygon := PCBServer.PCBObjectFactory(ePolyObject, eNoDimension, eCreate_Default);
    if Polygon = nil then Exit;

    Polygon.Layer := eTopLayer;
    Polygon.PolyHatchStyle := ePolyNoHatch; // Outline only
    Polygon.MinTrack := MilsToCoord(10);
    Polygon.ArcApproximation := MilsToCoord(1);
    Polygon.BorderWidth := MilsToCoord(10);

    Polygon.PointCount := 4;

    // Add vertices using Segments array
    Segment := Polygon.Segments[0];
    Segment.Kind := ePolySegmentLine;
    Segment.vx := MilsToCoord(-100);
    Segment.vy := MilsToCoord(-100);
    Polygon.Segments[0] := Segment;

    Segment := Polygon.Segments[1];
    Segment.Kind := ePolySegmentLine;
    Segment.vx := MilsToCoord(100);
    Segment.vy := MilsToCoord(-100);
    Polygon.Segments[1] := Segment;

    Segment := Polygon.Segments[2];
    Segment.Kind := ePolySegmentLine;
    Segment.vx := MilsToCoord(100);
    Segment.vy := MilsToCoord(100);
    Polygon.Segments[2] := Segment;

    Segment := Polygon.Segments[3];
    Segment.Kind := ePolySegmentLine;
    Segment.vx := MilsToCoord(-100);
    Segment.vy := MilsToCoord(100);
    Polygon.Segments[3] := Segment;

    Comp.AddPCBObject(Polygon);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Polygon.I_ObjectAddress);
end;

procedure CreateFootprint_COORDINATE(Comp: IPCB_LibComponent);
var
    Coordinate: IPCB_Coordinate;
begin
    // Coordinate object (origin marker)
    Coordinate := PCBServer.PCBObjectFactory(eCoordinateObject, eNoDimension, eCreate_Default);
    if Coordinate = nil then Exit;

    Coordinate.X := MilsToCoord(0);
    Coordinate.Y := MilsToCoord(0);
    Coordinate.Layer := eTopOverlay;

    Comp.AddPCBObject(Coordinate);
    PCBServer.SendMessageToRobots(Comp.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Coordinate.I_ObjectAddress);
end;

// Generate a single PCB library file with one footprint
procedure GenerateSinglePcbLibFile(FootprintName, Description: String);
var
    PCBLib: IPCB_Library;
    Comp: IPCB_LibComponent;
    Doc: IServerDocument;
    FilePath, JsonPath, IndividualDir: String;
begin
    IndividualDir := OUTPUT_DIR + 'Individual\PCB\';
    EnsureDirectoryExists(IndividualDir);

    FilePath := IndividualDir + FootprintName + '.PcbLib';
    JsonPath := IndividualDir + FootprintName + '.json';

    // Delete existing files
    if FileExists(FilePath) then DeleteFile(FilePath);
    if FileExists(JsonPath) then DeleteFile(JsonPath);

    // Create new document
    Doc := Client.OpenNewDocument('PcbLib', FilePath, '', False);
    if Doc = nil then Exit;
    Client.ShowDocument(Doc);
    PCBLib := PCBServer.GetCurrentPCBLibrary;
    if PCBLib = nil then Exit;

    // Remove default component
    RemoveDefaultPcbLibComponent(PCBLib);

    // Create the component
    Comp := PCBServer.CreatePCBLibComp;
    Comp.Name := FootprintName;
    Comp.Description := Description;
    PCBLib.RegisterComponent(Comp);
    PCBServer.PreProcess;

    // Call the appropriate creator based on footprint name
    if FootprintName = 'PAD_TH_ROUND' then CreateFootprint_PAD_TH_ROUND(Comp)
    else if FootprintName = 'PAD_TH_RECTANGULAR' then CreateFootprint_PAD_TH_RECTANGULAR(Comp)
    else if FootprintName = 'PAD_TH_OCTAGONAL' then CreateFootprint_PAD_TH_OCTAGONAL(Comp)
    else if FootprintName = 'PAD_SMD_RECTANGULAR' then CreateFootprint_PAD_SMD_RECTANGULAR(Comp)
    else if FootprintName = 'PAD_SMD_ROUNDED' then CreateFootprint_PAD_SMD_ROUNDED(Comp)
    else if FootprintName = 'PAD_ROTATED' then CreateFootprint_PAD_ROTATED(Comp)
    else if FootprintName = 'TRACKS_MULTILAYER' then CreateFootprint_TRACKS_MULTILAYER(Comp)
    else if FootprintName = 'ARCS_TEST' then CreateFootprint_ARCS_TEST(Comp)
    else if FootprintName = 'FILLS_TEST' then CreateFootprint_FILLS_TEST(Comp)
    else if FootprintName = 'TEXT_TEST' then CreateFootprint_TEXT_TEST(Comp)
    else if FootprintName = 'TEXT_TRUETYPE' then CreateFootprint_TEXT_TRUETYPE(Comp)
    else if FootprintName = 'REGIONS_TEST' then CreateFootprint_REGIONS_TEST(Comp)
    else if FootprintName = 'TEXT_INVERTED' then CreateFootprint_TEXT_INVERTED(Comp)
    else if FootprintName = 'TEXT_BARCODE' then CreateFootprint_TEXT_BARCODE(Comp)
    else if FootprintName = 'REGION_CUTOUT' then CreateFootprint_REGION_CUTOUT(Comp)
    else if FootprintName = 'BODY_3D' then CreateFootprint_BODY_3D(Comp)
    else if FootprintName = 'KEEPOUT_REGION' then CreateFootprint_KEEPOUT_REGION(Comp)
    else if FootprintName = 'BODY_3D_STEP' then CreateFootprint_BODY_3D_STEP(Comp)
    else if FootprintName = 'PAD_EXTENDED' then CreateFootprint_PAD_EXTENDED(Comp)
    else if FootprintName = 'POLYGON_EXTENDED' then CreateFootprint_POLYGON_EXTENDED(Comp)
    else if FootprintName = 'BODY_3D_EXTENDED' then CreateFootprint_BODY_3D_EXTENDED(Comp)
    else if FootprintName = 'REGION_WITH_HOLE' then CreateFootprint_REGION_WITH_HOLE(Comp)
    // else if FootprintName = 'DIMENSION_EXTENDED' then CreateFootprint_DIMENSION_EXTENDED(Comp)
    else if FootprintName = 'COMPLEX_EXTENDED' then CreateFootprint_COMPLEX_EXTENDED(Comp)
    // New extended footprints
    else if FootprintName = 'PAD_TH_OBLONG' then CreateFootprint_PAD_TH_OBLONG(Comp)
    else if FootprintName = 'PAD_TH_SLOT' then CreateFootprint_PAD_TH_SLOT(Comp)
    else if FootprintName = 'PAD_SMD_ROUND' then CreateFootprint_PAD_SMD_ROUND(Comp)
    else if FootprintName = 'PAD_SMD_OBLONG' then CreateFootprint_PAD_SMD_OBLONG(Comp)
    else if FootprintName = 'PAD_STACKED_MODES' then CreateFootprint_PAD_STACKED_MODES(Comp)
    else if FootprintName = 'PAD_HOLE_TYPES' then CreateFootprint_PAD_HOLE_TYPES(Comp)
    else if FootprintName = 'PAD_THERMAL_RELIEF' then CreateFootprint_PAD_THERMAL_RELIEF(Comp)
    else if FootprintName = 'VIA_TYPES' then CreateFootprint_VIA_TYPES(Comp)
    else if FootprintName = 'TRACK_WIDTHS' then CreateFootprint_TRACK_WIDTHS(Comp)
    else if FootprintName = 'ARC_ANGLES' then CreateFootprint_ARC_ANGLES(Comp)
    else if FootprintName = 'POLYGON_SOLID' then CreateFootprint_POLYGON_SOLID(Comp)
    else if FootprintName = 'POLYGON_HATCHED_90' then CreateFootprint_POLYGON_HATCHED_90(Comp)
    else if FootprintName = 'POLYGON_HATCHED_45' then CreateFootprint_POLYGON_HATCHED_45(Comp)
    else if FootprintName = 'TEXT_DESIGNATOR' then CreateFootprint_TEXT_DESIGNATOR(Comp)
    else if FootprintName = 'TEXT_COMMENT' then CreateFootprint_TEXT_COMMENT(Comp)
    else if FootprintName = 'REGION_COPPER' then CreateFootprint_REGION_COPPER(Comp)
    else if FootprintName = 'REGION_KEEPOUT_TRACK' then CreateFootprint_REGION_KEEPOUT_TRACK(Comp)
    // else if FootprintName = 'DIMENSION_LINEAR' then CreateFootprint_DIMENSION_LINEAR(Comp)
    // else if FootprintName = 'DIMENSION_RADIAL' then CreateFootprint_DIMENSION_RADIAL(Comp)
    else if FootprintName = 'MULTIPIN_QFP' then CreateFootprint_MULTIPIN_QFP(Comp)
    else if FootprintName = 'MULTIPIN_BGA' then CreateFootprint_MULTIPIN_BGA(Comp)
    else if FootprintName = 'MULTIPIN_SOP' then CreateFootprint_MULTIPIN_SOP(Comp)
    // Additional extended footprints
    else if FootprintName = 'PAD_COUNTER_HOLE' then CreateFootprint_PAD_COUNTER_HOLE(Comp)
    else if FootprintName = 'PAD_BACK_DRILL' then CreateFootprint_PAD_BACK_DRILL(Comp)
    else if FootprintName = 'PAD_CUSTOM_SHAPE' then CreateFootprint_PAD_CUSTOM_SHAPE(Comp)
    else if FootprintName = 'VIA_BLIND_TOP' then CreateFootprint_VIA_BLIND_TOP(Comp)
    else if FootprintName = 'VIA_BURIED' then CreateFootprint_VIA_BURIED(Comp)
    else if FootprintName = 'REGION_SLOT' then CreateFootprint_REGION_SLOT(Comp)
    else if FootprintName = 'REGION_CAVITY' then CreateFootprint_REGION_CAVITY(Comp)
    // else if FootprintName = 'DIMENSION_ANGULAR' then CreateFootprint_DIMENSION_ANGULAR(Comp)
    // else if FootprintName = 'DIMENSION_BASELINE' then CreateFootprint_DIMENSION_BASELINE(Comp)
    // else if FootprintName = 'DIMENSION_CENTER' then CreateFootprint_DIMENSION_CENTER(Comp)
    else if FootprintName = 'TEXT_ALL_STROKE_FONTS' then CreateFootprint_TEXT_ALL_STROKE_FONTS(Comp)
    else if FootprintName = 'TEXT_MULTILINE' then CreateFootprint_TEXT_MULTILINE(Comp)
    else if FootprintName = 'POLYGON_DIRECT_CONNECT' then CreateFootprint_POLYGON_DIRECT_CONNECT(Comp)
    else if FootprintName = 'POLYGON_THERMAL' then CreateFootprint_POLYGON_THERMAL(Comp)
    else if FootprintName = 'POLYGON_ISLANDS' then CreateFootprint_POLYGON_ISLANDS(Comp)
    else if FootprintName = 'MULTIPIN_SOIC' then CreateFootprint_MULTIPIN_SOIC(Comp)
    // Additional via types
    else if FootprintName = 'VIA_BLIND_BOTTOM' then CreateFootprint_VIA_BLIND_BOTTOM(Comp)
    else if FootprintName = 'VIA_MICROVIA' then CreateFootprint_VIA_MICROVIA(Comp)
    else if FootprintName = 'VIA_TENTING' then CreateFootprint_VIA_TENTING(Comp)
    // Additional track/arc types
    else if FootprintName = 'TRACK_ALL_LAYERS' then CreateFootprint_TRACK_ALL_LAYERS(Comp)
    else if FootprintName = 'TRACK_KEEPOUT' then CreateFootprint_TRACK_KEEPOUT(Comp)
    else if FootprintName = 'ARC_WIDTHS' then CreateFootprint_ARC_WIDTHS(Comp)
    else if FootprintName = 'ARC_FULL_CIRCLES' then CreateFootprint_ARC_FULL_CIRCLES(Comp)
    // Additional region types
    else if FootprintName = 'REGION_COMPLEX' then CreateFootprint_REGION_COMPLEX(Comp)
    // Additional dimension types
    // else if FootprintName = 'DIMENSION_DATUM' then CreateFootprint_DIMENSION_DATUM(Comp)
    // Additional text types
    else if FootprintName = 'TEXT_TRUETYPE_FONTS' then CreateFootprint_TEXT_TRUETYPE_FONTS(Comp)
    else if FootprintName = 'TEXT_BARCODE_ALL' then CreateFootprint_TEXT_BARCODE_ALL(Comp)
    // Additional polygon types
    else if FootprintName = 'POLYGON_OUTLINE' then CreateFootprint_POLYGON_OUTLINE(Comp)
    // Special PCB objects
    else if FootprintName = 'COORDINATE' then CreateFootprint_COORDINATE(Comp);

    FinalizeLibComponent(PCBLib, Comp);
    PCBServer.PostProcess;
    PCBLib.Board.ViewManager_FullUpdate;

    // Save and export
    Doc.DoFileSave('PcbLib');
    ExportPcbLibToJson(PCBLib, JsonPath);
    CloseDocument(FilePath);
end;

procedure GenerateIndividualPcbLibFiles;
begin
    // Generate individual files for each footprint type
    GenerateSinglePcbLibFile('PAD_TH_ROUND', 'Through-hole round pad test');
    GenerateSinglePcbLibFile('PAD_TH_RECTANGULAR', 'Through-hole rectangular pad test');
    GenerateSinglePcbLibFile('PAD_TH_OCTAGONAL', 'Through-hole octagonal pad test');
    GenerateSinglePcbLibFile('PAD_SMD_RECTANGULAR', 'SMD rectangular pad test');
    GenerateSinglePcbLibFile('PAD_SMD_ROUNDED', 'SMD rounded rectangular pad test');
    GenerateSinglePcbLibFile('PAD_ROTATED', 'Rotated pad at 45 degrees');
    GenerateSinglePcbLibFile('TRACKS_MULTILAYER', 'Tracks on various layers');
    GenerateSinglePcbLibFile('ARCS_TEST', 'Arcs with various angles');
    GenerateSinglePcbLibFile('FILLS_TEST', 'Fill regions');
    GenerateSinglePcbLibFile('TEXT_TEST', 'Text objects with various properties');
    GenerateSinglePcbLibFile('TEXT_TRUETYPE', 'TrueType font text objects');
    GenerateSinglePcbLibFile('REGIONS_TEST', 'Region objects');
    GenerateSinglePcbLibFile('TEXT_INVERTED', 'Inverted (knockout) text test');
    GenerateSinglePcbLibFile('TEXT_BARCODE', 'Barcode text test');
    GenerateSinglePcbLibFile('REGION_CUTOUT', 'Cutout region for polygon pours');
    GenerateSinglePcbLibFile('BODY_3D', '3D extruded component body');
    GenerateSinglePcbLibFile('KEEPOUT_REGION', 'Keepout region test');
    GenerateSinglePcbLibFile('BODY_3D_STEP', '3D body loaded from STEP file');
    GenerateSinglePcbLibFile('PAD_EXTENDED', 'Pad with tenting, testpoint, power plane properties');
    GenerateSinglePcbLibFile('POLYGON_EXTENDED', 'Polygon with extended pour properties');
    GenerateSinglePcbLibFile('BODY_3D_EXTENDED', '3D body with custom color and opacity');
    GenerateSinglePcbLibFile('REGION_WITH_HOLE', 'Region with internal hole/cutout');
    // GenerateSinglePcbLibFile('DIMENSION_EXTENDED', 'Dimension with TrueType font and extended properties');
    GenerateSinglePcbLibFile('COMPLEX_EXTENDED', 'Complex footprint with all extended property types');

    // New extended footprints
    GenerateSinglePcbLibFile('PAD_TH_OBLONG', 'Oblong through-hole pad');
    GenerateSinglePcbLibFile('PAD_TH_SLOT', 'Slotted through-hole pad');
    GenerateSinglePcbLibFile('PAD_SMD_ROUND', 'Round SMD pad');
    GenerateSinglePcbLibFile('PAD_SMD_OBLONG', 'Oblong SMD pad');
    GenerateSinglePcbLibFile('PAD_STACKED_MODES', 'Pad with different top/mid/bottom shapes');
    GenerateSinglePcbLibFile('PAD_HOLE_TYPES', 'Pads with round, square, and slot holes');
    GenerateSinglePcbLibFile('PAD_THERMAL_RELIEF', 'Pads with various thermal relief entries');
    GenerateSinglePcbLibFile('VIA_TYPES', 'Vias with various tenting options');
    GenerateSinglePcbLibFile('TRACK_WIDTHS', 'Tracks with various widths (2-50 mils)');
    GenerateSinglePcbLibFile('ARC_ANGLES', 'Arcs with various sweep angles');
    GenerateSinglePcbLibFile('POLYGON_SOLID', 'Solid polygon pour');
    GenerateSinglePcbLibFile('POLYGON_HATCHED_90', '90-degree hatched polygon');
    GenerateSinglePcbLibFile('POLYGON_HATCHED_45', '45-degree hatched polygon');
    GenerateSinglePcbLibFile('TEXT_DESIGNATOR', 'Designator special text field');
    GenerateSinglePcbLibFile('TEXT_COMMENT', 'Comment special text field');
    GenerateSinglePcbLibFile('REGION_COPPER', 'Standard copper region');
    GenerateSinglePcbLibFile('REGION_KEEPOUT_TRACK', 'Keepout tracks forming boundary');
    // GenerateSinglePcbLibFile('DIMENSION_LINEAR', 'Linear dimension object');
    // GenerateSinglePcbLibFile('DIMENSION_RADIAL', 'Radial/diameter dimension object');
    GenerateSinglePcbLibFile('MULTIPIN_QFP', 'QFP-44 style multi-pin footprint');
    GenerateSinglePcbLibFile('MULTIPIN_BGA', '6x6 BGA style footprint');
    GenerateSinglePcbLibFile('MULTIPIN_SOP', 'SOP-8 style footprint');

    // Additional extended footprints
    GenerateSinglePcbLibFile('PAD_COUNTER_HOLE', 'Pad with counter hole (counterbore/countersink)');
    GenerateSinglePcbLibFile('PAD_BACK_DRILL', 'Pad with back-drilled hole');
    GenerateSinglePcbLibFile('PAD_CUSTOM_SHAPE', 'Pad with custom polygon shape');
    GenerateSinglePcbLibFile('VIA_BLIND_TOP', 'Blind via from top layer');
    GenerateSinglePcbLibFile('VIA_BURIED', 'Buried via between inner layers');
    GenerateSinglePcbLibFile('REGION_SLOT', 'Slot-shaped cutout region');
    GenerateSinglePcbLibFile('REGION_CAVITY', 'Cavity region for embedded components');
    // GenerateSinglePcbLibFile('DIMENSION_ANGULAR', 'Angular dimension object');
    // GenerateSinglePcbLibFile('DIMENSION_BASELINE', 'Baseline dimension object');
    // GenerateSinglePcbLibFile('DIMENSION_CENTER', 'Center mark dimension object');
    GenerateSinglePcbLibFile('TEXT_ALL_STROKE_FONTS', 'Text with all stroke font IDs');
    GenerateSinglePcbLibFile('TEXT_MULTILINE', 'Multiline text with word wrap');
    GenerateSinglePcbLibFile('POLYGON_DIRECT_CONNECT', 'Polygon with direct connect style');
    GenerateSinglePcbLibFile('POLYGON_THERMAL', 'Polygon with thermal relief entries');
    GenerateSinglePcbLibFile('POLYGON_ISLANDS', 'Polygon with island removal');
    GenerateSinglePcbLibFile('MULTIPIN_SOIC', 'SOIC-16 style multi-pin footprint');

    // Additional via types
    GenerateSinglePcbLibFile('VIA_BLIND_BOTTOM', 'Blind via from bottom layer');
    GenerateSinglePcbLibFile('VIA_MICROVIA', 'Microvia single layer span');
    GenerateSinglePcbLibFile('VIA_TENTING', 'Vias with all tenting options');

    // Additional track/arc types
    GenerateSinglePcbLibFile('TRACK_ALL_LAYERS', 'Tracks on all signal layers');
    GenerateSinglePcbLibFile('TRACK_KEEPOUT', 'Keepout track boundary');
    GenerateSinglePcbLibFile('ARC_WIDTHS', 'Arcs with various line widths');
    GenerateSinglePcbLibFile('ARC_FULL_CIRCLES', 'Full 360-degree circles');

    // Additional region types
    GenerateSinglePcbLibFile('REGION_BOARD_OUTLINE', 'Board outline region');
    GenerateSinglePcbLibFile('REGION_COMPLEX', 'Region with multiple holes');

    // Additional dimension types
    // GenerateSinglePcbLibFile('DIMENSION_DATUM', 'Datum dimension object');

    // Additional text types
    GenerateSinglePcbLibFile('TEXT_TRUETYPE_FONTS', 'TrueType font variations');
    GenerateSinglePcbLibFile('TEXT_BARCODE_ALL', 'Various barcode types');

    // Additional polygon types
    GenerateSinglePcbLibFile('POLYGON_OUTLINE', 'Outline-only polygon');

    // Special PCB objects
    GenerateSinglePcbLibFile('COORDINATE', 'Coordinate origin marker');
end;

{==============================================================================
  INDIVIDUAL SCHEMATIC LIBRARY FILE GENERATOR
==============================================================================}

procedure GenerateSingleSchLibFile(SymbolName, Description, Designator: String);
var
    SchLib: ISch_Lib;
    Comp: ISch_Component;
    Doc: IServerDocument;
    SchDoc: ISch_Document;
    FilePath, JsonPath, IndividualDir: String;
begin
    IndividualDir := OUTPUT_DIR + 'Individual\SchLib\';
    EnsureDirectoryExists(IndividualDir);

    FilePath := IndividualDir + SymbolName + '.SchLib';
    JsonPath := IndividualDir + SymbolName + '.json';

    // Delete existing files
    if FileExists(FilePath) then DeleteFile(FilePath);
    if FileExists(JsonPath) then DeleteFile(JsonPath);

    // Create new document
    Doc := Client.OpenNewDocument('SchLib', FilePath, '', False);
    if Doc = nil then Exit;
    Client.ShowDocument(Doc);
    SchLib := SchServer.GetCurrentSchDocument;
    if SchLib = nil then Exit;

    // Remove default component
    RemoveDefaultSchLibComponent(SchLib);

    SchDoc := SchServer.GetCurrentSchDocument;
    if SchDoc = nil then Exit;
    SchServer.ProcessControl.PreProcess(SchDoc, '');

    // Create the component
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := Designator;
    Comp.Comment.Text := SymbolName;
    Comp.LibReference := SymbolName;
    Comp.ComponentDescription := Description;

    // Call the appropriate creator based on symbol name
    if SymbolName = 'RESISTOR' then begin
        CreateSchPin(Comp, 'P1', '1', -200, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P2', '2', 200, 0, eRotate0, eElectricPassive);
        CreateSchRectangle(Comp, -200, -100, 200, 100, eSmall, False);
    end
    else if SymbolName = 'CAPACITOR' then begin
        CreateSchPin(Comp, 'P1', '1', -200, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P2', '2', 200, 0, eRotate0, eElectricPassive);
        CreateSchLine(Comp, -100, -100, -100, 100, eMedium);
        CreateSchLine(Comp, 100, -100, 100, 100, eMedium);
    end
    else if SymbolName = 'CIRCLE_FILLED' then begin
        CreateSchCircle(Comp, 0, 0, 200, eMedium, True);
    end
    else if SymbolName = 'CIRCLE_OUTLINE' then begin
        CreateSchCircle(Comp, 0, 0, 200, eMedium, False);
    end
    else if SymbolName = 'ELLIPSE_TEST' then begin
        CreateSchEllipse(Comp, 0, 0, 200, 100, eMedium, False);
    end
    else if SymbolName = 'ROUNDRECT_TEST' then begin
        CreateSchRoundRectangle(Comp, -200, -100, 200, 100, 50, 50, eMedium, False);
    end
    else if SymbolName = 'TEXTFRAME_TEST' then begin
        CreateSchTextFrame(Comp, -200, -100, 200, 100, 'Test Frame', 1, True);
    end
    else if SymbolName = 'ARC_FULL' then begin
        CreateSchArc(Comp, 0, 0, 200, 0, 360, eMedium);
    end
    else if SymbolName = 'POLYLINE_TEST' then begin
        CreateSchPolyline4(Comp, -200, -100, -100, 100, 100, -100, 200, 100, eMedium, False);
    end
    else if SymbolName = 'POLYGON_TEST' then begin
        CreateSchPolygon4(Comp, 0, -150, -150, 0, 0, 150, 150, 0, eMedium, True);
    end
    // Logic gates - XOR
    else if SymbolName = 'XOR_GATE' then begin
        CreateSchPin(Comp, 'A', '1', -400, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'B', '2', -400, -100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'Y', '3', 400, 0, eRotate0, eElectricOutput);
        // XOR gate shape (OR gate + extra arc)
        CreateSchArc(Comp, -350, 0, 100, 270, 180, eMedium);
        CreateSchArc(Comp, -300, 0, 100, 270, 180, eMedium);
        CreateSchArc(Comp, -100, 300, 350, 270, 60, eMedium);
        CreateSchArc(Comp, -100, -300, 350, 30, 60, eMedium);
    end
    // Logic gates - NAND
    else if SymbolName = 'NAND_GATE' then begin
        CreateSchPin(Comp, 'A', '1', -400, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'B', '2', -400, -100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'Y', '3', 400, 0, eRotate0, eElectricOutput);
        // AND gate + bubble
        CreateSchLine(Comp, -200, -150, -200, 150, eMedium);
        CreateSchLine(Comp, -200, 150, 100, 150, eMedium);
        CreateSchLine(Comp, -200, -150, 100, -150, eMedium);
        CreateSchArc(Comp, 100, 0, 150, 270, 180, eMedium);
        CreateSchCircle(Comp, 280, 0, 30, eSmall, False);
    end
    // Logic gates - NOR
    else if SymbolName = 'NOR_GATE' then begin
        CreateSchPin(Comp, 'A', '1', -400, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'B', '2', -400, -100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'Y', '3', 400, 0, eRotate0, eElectricOutput);
        // OR gate + bubble
        CreateSchArc(Comp, -300, 0, 100, 270, 180, eMedium);
        CreateSchArc(Comp, -100, 300, 350, 270, 60, eMedium);
        CreateSchArc(Comp, -100, -300, 350, 30, 60, eMedium);
        CreateSchCircle(Comp, 280, 0, 30, eSmall, False);
    end
    // Logic gates - XNOR
    else if SymbolName = 'XNOR_GATE' then begin
        CreateSchPin(Comp, 'A', '1', -400, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'B', '2', -400, -100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'Y', '3', 400, 0, eRotate0, eElectricOutput);
        // XOR gate + bubble
        CreateSchArc(Comp, -350, 0, 100, 270, 180, eMedium);
        CreateSchArc(Comp, -300, 0, 100, 270, 180, eMedium);
        CreateSchArc(Comp, -100, 300, 350, 270, 60, eMedium);
        CreateSchArc(Comp, -100, -300, 350, 30, 60, eMedium);
        CreateSchCircle(Comp, 280, 0, 30, eSmall, False);
    end
    // Buffer gate
    else if SymbolName = 'BUFFER_GATE' then begin
        CreateSchPin(Comp, 'A', '1', -400, 0, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'Y', '2', 400, 0, eRotate0, eElectricOutput);
        CreateSchTriangle(Comp, -200, 150, -200, -150, 200, 0, False);
    end
    // Schmitt trigger
    else if SymbolName = 'SCHMITT_TRIGGER' then begin
        CreateSchPin(Comp, 'A', '1', -400, 0, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'Y', '2', 400, 0, eRotate0, eElectricOutput);
        CreateSchTriangle(Comp, -200, 150, -200, -150, 200, 0, False);
        // Hysteresis symbol inside
        CreateSchLine(Comp, -50, 0, 0, 50, eSmall);
        CreateSchLine(Comp, 0, 50, 50, 50, eSmall);
        CreateSchLine(Comp, 0, -50, 50, 0, eSmall);
        CreateSchLine(Comp, -50, -50, 0, -50, eSmall);
    end
    // D Flip-Flop
    else if SymbolName = 'DFF' then begin
        CreateSchPin(Comp, 'D', '1', -300, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'CLK', '2', -300, 0, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'Q', '3', 300, 100, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'QN', '4', 300, -100, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'RST', '5', 0, -200, eRotate90, eElectricInput);
        CreateSchRectangle(Comp, -300, -200, 300, 200, eSmall, False);
        // Clock symbol
        CreateSchLine(Comp, -300, -20, -270, 0, eSmall);
        CreateSchLine(Comp, -270, 0, -300, 20, eSmall);
    end
    // JK Flip-Flop
    else if SymbolName = 'JKFF' then begin
        CreateSchPin(Comp, 'J', '1', -300, 150, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'CLK', '2', -300, 0, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'K', '3', -300, -150, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'Q', '4', 300, 100, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'QN', '5', 300, -100, eRotate0, eElectricOutput);
        CreateSchRectangle(Comp, -300, -200, 300, 250, eSmall, False);
        CreateSchLine(Comp, -300, -20, -270, 0, eSmall);
        CreateSchLine(Comp, -270, 0, -300, 20, eSmall);
    end
    // 555 Timer IC
    else if SymbolName = 'TIMER_555' then begin
        CreateSchPin(Comp, 'GND', '1', -300, -200, eRotate180, eElectricPower);
        CreateSchPin(Comp, 'TRIG', '2', -300, -100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'OUT', '3', 300, 0, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'RESET', '4', 0, 300, eRotate270, eElectricInput);
        CreateSchPin(Comp, 'CTRL', '5', 300, -100, eRotate0, eElectricInput);
        CreateSchPin(Comp, 'THRES', '6', -300, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'DISCH', '7', -300, 200, eRotate180, eElectricOutput);
        CreateSchPin(Comp, 'VCC', '8', 0, 300, eRotate270, eElectricPower);
        CreateSchRectangle(Comp, -300, -300, 300, 300, eSmall, False);
    end
    // Crystal oscillator
    else if SymbolName = 'CRYSTAL' then begin
        CreateSchPin(Comp, 'P1', '1', -300, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P2', '2', 300, 0, eRotate0, eElectricPassive);
        CreateSchRectangle(Comp, -100, -100, 100, 100, eSmall, False);
        CreateSchLine(Comp, -150, -150, -150, 150, eMedium);
        CreateSchLine(Comp, 150, -150, 150, 150, eMedium);
    end
    // Fuse
    else if SymbolName = 'FUSE' then begin
        CreateSchPin(Comp, 'P1', '1', -200, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P2', '2', 200, 0, eRotate0, eElectricPassive);
        CreateSchRectangle(Comp, -150, -50, 150, 50, eSmall, False);
        CreateSchLine(Comp, -100, 0, 100, 0, eSmall);
    end
    // Relay coil
    else if SymbolName = 'RELAY_COIL' then begin
        CreateSchPin(Comp, 'A1', '1', -200, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'A2', '2', 200, 0, eRotate0, eElectricPassive);
        CreateSchRectangle(Comp, -150, -75, 150, 75, eSmall, False);
        // Coil arcs inside
        CreateSchArc(Comp, -100, 0, 30, 0, 180, eSmall);
        CreateSchArc(Comp, -50, 0, 30, 0, 180, eSmall);
        CreateSchArc(Comp, 0, 0, 30, 0, 180, eSmall);
        CreateSchArc(Comp, 50, 0, 30, 0, 180, eSmall);
    end
    // Transformer
    else if SymbolName = 'TRANSFORMER' then begin
        CreateSchPin(Comp, 'P1', '1', -300, 100, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P2', '2', -300, -100, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'S1', '3', 300, 100, eRotate0, eElectricPassive);
        CreateSchPin(Comp, 'S2', '4', 300, -100, eRotate0, eElectricPassive);
        // Primary coils
        CreateSchArc(Comp, -100, 60, 30, 0, 180, eMedium);
        CreateSchArc(Comp, -100, 20, 30, 0, 180, eMedium);
        CreateSchArc(Comp, -100, -20, 30, 0, 180, eMedium);
        CreateSchArc(Comp, -100, -60, 30, 0, 180, eMedium);
        // Secondary coils
        CreateSchArc(Comp, 100, 60, 30, 0, 180, eMedium);
        CreateSchArc(Comp, 100, 20, 30, 0, 180, eMedium);
        CreateSchArc(Comp, 100, -20, 30, 0, 180, eMedium);
        CreateSchArc(Comp, 100, -60, 30, 0, 180, eMedium);
        // Core lines
        CreateSchLine(Comp, -20, -100, -20, 100, eMedium);
        CreateSchLine(Comp, 20, -100, 20, 100, eMedium);
    end
    // Switch SPST
    else if SymbolName = 'SWITCH_SPST' then begin
        CreateSchPin(Comp, 'P1', '1', -200, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P2', '2', 200, 0, eRotate0, eElectricPassive);
        CreateSchCircle(Comp, -100, 0, 20, eSmall, True);
        CreateSchCircle(Comp, 100, 0, 20, eSmall, True);
        CreateSchLine(Comp, -80, 0, 80, 50, eMedium);
    end
    // Switch SPDT
    else if SymbolName = 'SWITCH_SPDT' then begin
        CreateSchPin(Comp, 'COM', '1', -200, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'NO', '2', 200, 100, eRotate0, eElectricPassive);
        CreateSchPin(Comp, 'NC', '3', 200, -100, eRotate0, eElectricPassive);
        CreateSchCircle(Comp, -100, 0, 20, eSmall, True);
        CreateSchCircle(Comp, 100, 100, 20, eSmall, True);
        CreateSchCircle(Comp, 100, -100, 20, eSmall, True);
        CreateSchLine(Comp, -80, 0, 80, 80, eMedium);
    end
    // Connector 2-pin
    else if SymbolName = 'CONN_2PIN' then begin
        CreateSchPin(Comp, 'PIN1', '1', -300, 100, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'PIN2', '2', -300, 0, eRotate180, eElectricPassive);
        CreateSchRectangle(Comp, -300, -50, 0, 150, eSmall, False);
    end
    // Test point
    else if SymbolName = 'TESTPOINT' then begin
        CreateSchPin(Comp, 'TP', '1', 0, -100, eRotate90, eElectricPassive);
        CreateSchCircle(Comp, 0, 0, 50, eMedium, False);
    end
    // NC pin symbol
    else if SymbolName = 'NC_SYMBOL' then begin
        CreateSchPin(Comp, 'NC', '1', 0, -100, eRotate90, eElectricPassive);
        CreateSchLine(Comp, -50, 50, 50, -50, eMedium);
        CreateSchLine(Comp, -50, -50, 50, 50, eMedium);
    end
    // Voltage regulator
    else if SymbolName = 'VREG' then begin
        CreateSchPin(Comp, 'VIN', '1', -300, 0, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'GND', '2', 0, -200, eRotate90, eElectricPower);
        CreateSchPin(Comp, 'VOUT', '3', 300, 0, eRotate0, eElectricOutput);
        CreateSchRectangle(Comp, -300, -200, 300, 100, eSmall, False);
    end
    // Optocoupler
    else if SymbolName = 'OPTOCOUPLER' then begin
        CreateSchPin(Comp, 'A', '1', -300, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'K', '2', -300, -100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'C', '3', 300, 100, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'E', '4', 300, -100, eRotate0, eElectricOutput);
        CreateSchRectangle(Comp, -200, -150, 200, 150, eSmall, False);
        // LED symbol left side
        CreateSchTriangle(Comp, -150, 50, -150, -30, -100, 10, True);
        CreateSchLine(Comp, -100, -30, -100, 50, eSmall);
        // Phototransistor right side
        CreateSchLine(Comp, 100, -50, 100, 50, eSmall);
        CreateSchLine(Comp, 100, 50, 150, 100, eSmall);
        CreateSchLine(Comp, 100, -50, 150, -100, eSmall);
        // Light arrows
        CreateSchLine(Comp, -50, 30, 50, 30, eSmall);
        CreateSchLine(Comp, -50, -30, 50, -30, eSmall);
    end
    // Schottky diode
    else if SymbolName = 'SCHOTTKY' then begin
        CreateSchPin(Comp, 'A', '1', -300, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'K', '2', 300, 0, eRotate0, eElectricPassive);
        CreateSchTriangle(Comp, -100, 100, -100, -100, 100, 0, True);
        CreateSchLine(Comp, 100, -100, 100, 100, eMedium);
        // Schottky bar hooks
        CreateSchLine(Comp, 100, 100, 130, 100, eSmall);
        CreateSchLine(Comp, 130, 100, 130, 70, eSmall);
        CreateSchLine(Comp, 100, -100, 70, -100, eSmall);
        CreateSchLine(Comp, 70, -100, 70, -70, eSmall);
    end
    // TVS diode
    else if SymbolName = 'TVS_DIODE' then begin
        CreateSchPin(Comp, 'P1', '1', -300, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P2', '2', 300, 0, eRotate0, eElectricPassive);
        // Back-to-back zener symbol
        CreateSchTriangle(Comp, -100, 100, -100, -100, 0, 0, True);
        CreateSchTriangle(Comp, 100, 100, 100, -100, 0, 0, True);
        CreateSchLine(Comp, 0, -100, 0, 100, eMedium);
    end
    // Varistor
    else if SymbolName = 'VARISTOR' then begin
        CreateSchPin(Comp, 'P1', '1', -200, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P2', '2', 200, 0, eRotate0, eElectricPassive);
        CreateSchRectangle(Comp, -100, -75, 100, 75, eSmall, False);
        CreateSchLine(Comp, -100, -100, 100, 100, eSmall);
    end
    // Thermistor
    else if SymbolName = 'THERMISTOR' then begin
        CreateSchPin(Comp, 'P1', '1', -200, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P2', '2', 200, 0, eRotate0, eElectricPassive);
        CreateSchRectangle(Comp, -150, -50, 150, 50, eSmall, False);
        CreateSchLine(Comp, -200, -100, 200, 100, eSmall);
        CreateSchLabel(Comp, -50, -80, 'T', 1);
    end
    // LDR (Light dependent resistor)
    else if SymbolName = 'LDR' then begin
        CreateSchPin(Comp, 'P1', '1', -200, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P2', '2', 200, 0, eRotate0, eElectricPassive);
        CreateSchRectangle(Comp, -150, -50, 150, 50, eSmall, False);
        // Light arrows
        CreateSchLine(Comp, -100, 80, -50, 100, eSmall);
        CreateSchLine(Comp, 0, 80, 50, 100, eSmall);
    end
    // Triac
    else if SymbolName = 'TRIAC' then begin
        CreateSchPin(Comp, 'MT1', '1', -200, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'MT2', '2', 200, 0, eRotate0, eElectricPassive);
        CreateSchPin(Comp, 'G', '3', 0, -150, eRotate90, eElectricInput);
        // Back-to-back thyristor symbol
        CreateSchTriangle(Comp, -100, 80, -100, -80, 0, 0, True);
        CreateSchTriangle(Comp, 100, 80, 100, -80, 0, 0, True);
    end
    // SCR (Thyristor)
    else if SymbolName = 'SCR' then begin
        CreateSchPin(Comp, 'A', '1', -200, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'K', '2', 200, 0, eRotate0, eElectricPassive);
        CreateSchPin(Comp, 'G', '3', 0, -150, eRotate90, eElectricInput);
        CreateSchTriangle(Comp, -100, 80, -100, -80, 100, 0, True);
        CreateSchLine(Comp, 100, -80, 100, 80, eMedium);
    end
    // Bidirectional pin
    else if SymbolName = 'IO_PIN' then begin
        CreateSchPin(Comp, 'P1', '1', -300, 0, eRotate180, eElectricIO);
        CreateSchRectangle(Comp, -300, -100, 0, 100, eSmall, False);
    end
    // Open collector pin
    else if SymbolName = 'OC_PIN' then begin
        CreateSchPin(Comp, 'OC', '1', -300, 0, eRotate180, eElectricOpenCollector);
        CreateSchRectangle(Comp, -300, -100, 0, 100, eSmall, False);
    end
    // High-Z pin
    else if SymbolName = 'HIZ_PIN' then begin
        CreateSchPin(Comp, 'HZ', '1', -300, 0, eRotate180, eElectricHiZ);
        CreateSchRectangle(Comp, -300, -100, 0, 100, eSmall, False);
    end
    // Emitter pin
    else if SymbolName = 'EMITTER_PIN' then begin
        CreateSchPin(Comp, 'OE', '1', -300, 0, eRotate180, eElectricOpenEmitter);
        CreateSchRectangle(Comp, -300, -100, 0, 100, eSmall, False);
    end
    // Pin with all IEEE symbols
    else if SymbolName = 'IEEE_PINS' then begin
        CreateSchPin(Comp, 'CLK', '1', -400, 200, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'INV', '2', -400, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'ACTLOW', '3', -400, 0, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'DATA', '4', -400, -100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'OUT', '5', 400, 100, eRotate0, eElectricOutput);
        CreateSchRectangle(Comp, -400, -200, 400, 300, eSmall, False);
    end
    // Multi-part symbol (part A)
    else if SymbolName = 'MULTIPART_A' then begin
        CreateSchPin(Comp, 'A', '1', -300, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'B', '2', -300, -100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'Y', '3', 300, 0, eRotate0, eElectricOutput);
        CreateSchTriangle(Comp, -200, 150, -200, -150, 200, 0, False);
    end
    // Multi-part symbol (part B - same gates)
    else if SymbolName = 'MULTIPART_B' then begin
        CreateSchPin(Comp, 'A', '4', -300, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'B', '5', -300, -100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'Y', '6', 300, 0, eRotate0, eElectricOutput);
        CreateSchTriangle(Comp, -200, 150, -200, -150, 200, 0, False);
    end
    // Quad switch
    else if SymbolName = 'QUAD_SWITCH' then begin
        CreateSchPin(Comp, '1A', '1', -300, 300, eRotate180, eElectricPassive);
        CreateSchPin(Comp, '1B', '2', 300, 300, eRotate0, eElectricPassive);
        CreateSchPin(Comp, '2A', '3', -300, 100, eRotate180, eElectricPassive);
        CreateSchPin(Comp, '2B', '4', 300, 100, eRotate0, eElectricPassive);
        CreateSchPin(Comp, '3A', '5', -300, -100, eRotate180, eElectricPassive);
        CreateSchPin(Comp, '3B', '6', 300, -100, eRotate0, eElectricPassive);
        CreateSchPin(Comp, '4A', '7', -300, -300, eRotate180, eElectricPassive);
        CreateSchPin(Comp, '4B', '8', 300, -300, eRotate0, eElectricPassive);
        CreateSchRectangle(Comp, -300, -400, 300, 400, eSmall, False);
    end
    // IC with many pins (16-pin)
    else if SymbolName = 'IC_16PIN' then begin
        CreateSchPin(Comp, 'P1', '1', -400, 350, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P2', '2', -400, 250, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P3', '3', -400, 150, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P4', '4', -400, 50, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P5', '5', -400, -50, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P6', '6', -400, -150, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P7', '7', -400, -250, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P8', '8', -400, -350, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'P9', '9', 400, -350, eRotate0, eElectricPassive);
        CreateSchPin(Comp, 'P10', '10', 400, -250, eRotate0, eElectricPassive);
        CreateSchPin(Comp, 'P11', '11', 400, -150, eRotate0, eElectricPassive);
        CreateSchPin(Comp, 'P12', '12', 400, -50, eRotate0, eElectricPassive);
        CreateSchPin(Comp, 'P13', '13', 400, 50, eRotate0, eElectricPassive);
        CreateSchPin(Comp, 'P14', '14', 400, 150, eRotate0, eElectricPassive);
        CreateSchPin(Comp, 'P15', '15', 400, 250, eRotate0, eElectricPassive);
        CreateSchPin(Comp, 'P16', '16', 400, 350, eRotate0, eElectricPassive);
        CreateSchRectangle(Comp, -400, -400, 400, 400, eSmall, False);
    end
    // Power input pin symbol
    else if SymbolName = 'POWER_IN_PIN' then begin
        CreateSchPin(Comp, 'PWR', '1', 0, 0, eRotate90, eElectricPower);
        CreateSchLine(Comp, -50, 100, 50, 100, eSmall);
    end
    // Power output pin symbol
    else if SymbolName = 'POWER_OUT_PIN' then begin
        CreateSchPin(Comp, 'VOUT', '1', 0, 0, eRotate270, eElectricPower);
        CreateSchLine(Comp, -50, -100, 50, -100, eSmall);
    end
    // Ground symbol
    else if SymbolName = 'GND_SYMBOL' then begin
        CreateSchPin(Comp, 'GND', '1', 0, 0, eRotate90, eElectricPower);
        CreateSchLine(Comp, -60, 100, 60, 100, eSmall);
        CreateSchLine(Comp, -40, 120, 40, 120, eSmall);
        CreateSchLine(Comp, -20, 140, 20, 140, eSmall);
    end
    // VCC symbol
    else if SymbolName = 'VCC_SYMBOL' then begin
        CreateSchPin(Comp, 'VCC', '1', 0, 0, eRotate270, eElectricPower);
        CreateSchLine(Comp, -50, -100, 50, -100, eSmall);
    end
    // VDD symbol
    else if SymbolName = 'VDD_SYMBOL' then begin
        CreateSchPin(Comp, 'VDD', '1', 0, 0, eRotate270, eElectricPower);
        CreateSchTriangle(Comp, -50, -100, 0, -150, 50, -100, True);
    end
    // VSS symbol
    else if SymbolName = 'VSS_SYMBOL' then begin
        CreateSchPin(Comp, 'VSS', '1', 0, 0, eRotate90, eElectricPower);
        CreateSchTriangle(Comp, -50, 100, 0, 150, 50, 100, True);
    end
    // Hidden power pin test
    else if SymbolName = 'HIDDEN_PWR_PIN' then begin
        CreateSchPinEx(Comp, 'VCC', '1', 0, 100, eRotate270, eElectricPower, True); // Hidden
        CreateSchPinEx(Comp, 'GND', '2', 0, -100, eRotate90, eElectricPower, True); // Hidden
        CreateSchRectangle(Comp, -100, -100, 100, 100, eSmall, False);
    end
    // Clock pin with IEEE symbol
    else if SymbolName = 'CLOCK_PIN' then begin
        CreateSchPinWithSymbol(Comp, 'CLK', '1', -200, 0, eRotate180, eElectricInput, eClock);
        CreateSchRectangle(Comp, -200, -100, 200, 100, eSmall, False);
    end
    // Inverted pin
    else if SymbolName = 'INVERTED_PIN' then begin
        CreateSchPinWithSymbol(Comp, 'EN', '1', -200, 0, eRotate180, eElectricInput, eDot);
        CreateSchRectangle(Comp, -200, -100, 200, 100, eSmall, False);
    end
    // Active low pin
    else if SymbolName = 'ACTIVE_LOW_PIN' then begin
        CreateSchPinWithSymbol(Comp, 'RST', '1', -200, 0, eRotate180, eElectricInput, eActiveLowInput);
        CreateSchRectangle(Comp, -200, -100, 200, 100, eSmall, False);
    end
    // Analog input symbol
    else if SymbolName = 'ANALOG_IN' then begin
        CreateSchPin(Comp, 'AIN', '1', -200, 0, eRotate180, eElectricInput);
        CreateSchLine(Comp, -100, 50, -100, -50, eSmall);
        CreateSchArc(Comp, 0, 0, 100, 0, 360, eSmall);
        CreateSchLabel(Comp, 0, 0, 'A', 1);
    end
    // Analog output symbol
    else if SymbolName = 'ANALOG_OUT' then begin
        CreateSchPin(Comp, 'AOUT', '1', 200, 0, eRotate0, eElectricOutput);
        CreateSchArc(Comp, 0, 0, 100, 0, 360, eSmall);
        CreateSchLabel(Comp, 0, 0, 'A', 1);
    end
    // DAC symbol
    else if SymbolName = 'DAC_SYMBOL' then begin
        CreateSchPin(Comp, 'D0', '1', -300, 150, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'D1', '2', -300, 50, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'D2', '3', -300, -50, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'D3', '4', -300, -150, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'VOUT', '5', 300, 0, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'VCC', '6', 0, 250, eRotate270, eElectricPower);
        CreateSchPin(Comp, 'GND', '7', 0, -250, eRotate90, eElectricPower);
        CreateSchRectangle(Comp, -300, -200, 300, 200, eSmall, False);
        CreateSchLabel(Comp, 0, 0, 'DAC', 1);
    end
    // ADC symbol
    else if SymbolName = 'ADC_SYMBOL' then begin
        CreateSchPin(Comp, 'VIN', '1', -300, 0, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'D0', '2', 300, 150, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'D1', '3', 300, 50, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'D2', '4', 300, -50, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'D3', '5', 300, -150, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'VCC', '6', 0, 250, eRotate270, eElectricPower);
        CreateSchPin(Comp, 'GND', '7', 0, -250, eRotate90, eElectricPower);
        CreateSchRectangle(Comp, -300, -200, 300, 200, eSmall, False);
        CreateSchLabel(Comp, 0, 0, 'ADC', 1);
    end
    // Comparator symbol
    else if SymbolName = 'COMPARATOR' then begin
        CreateSchPin(Comp, '+', '1', -300, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, '-', '2', -300, -100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'OUT', '3', 300, 0, eRotate0, eElectricOutput);
        CreateSchTriangle(Comp, -200, 200, -200, -200, 200, 0, False);
        CreateSchLabel(Comp, -150, 100, '+', 1);
        CreateSchLabel(Comp, -150, -100, '-', 1);
    end
    // Instrumentation amplifier
    else if SymbolName = 'INSTR_AMP' then begin
        CreateSchPin(Comp, 'IN+', '1', -300, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'IN-', '2', -300, -100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'OUT', '3', 300, 0, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'RG1', '4', 0, 200, eRotate270, eElectricPassive);
        CreateSchPin(Comp, 'RG2', '5', 0, -200, eRotate90, eElectricPassive);
        CreateSchPin(Comp, 'V+', '6', 100, 200, eRotate270, eElectricPower);
        CreateSchPin(Comp, 'V-', '7', 100, -200, eRotate90, eElectricPower);
        CreateSchTriangle(Comp, -200, 200, -200, -200, 200, 0, False);
    end
    // Voltage reference
    else if SymbolName = 'VREF' then begin
        CreateSchPin(Comp, 'IN', '1', -200, 0, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'OUT', '2', 200, 0, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'GND', '3', 0, -150, eRotate90, eElectricPower);
        CreateSchRectangle(Comp, -200, -100, 200, 100, eSmall, False);
        CreateSchLabel(Comp, 0, 0, 'REF', 1);
    end
    // LDO regulator
    else if SymbolName = 'LDO_REG' then begin
        CreateSchPin(Comp, 'VIN', '1', -300, 50, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'VOUT', '2', 300, 50, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'GND', '3', 0, -150, eRotate90, eElectricPower);
        CreateSchPin(Comp, 'EN', '4', -300, -50, eRotate180, eElectricInput);
        CreateSchRectangle(Comp, -300, -100, 300, 100, eSmall, False);
        CreateSchLabel(Comp, 0, 0, 'LDO', 1);
    end
    // DC-DC converter
    else if SymbolName = 'DCDC_CONV' then begin
        CreateSchPin(Comp, 'VIN', '1', -300, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'VOUT', '2', 300, 100, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'GND', '3', 0, -200, eRotate90, eElectricPower);
        CreateSchPin(Comp, 'SW', '4', 300, -50, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'FB', '5', 300, -150, eRotate0, eElectricInput);
        CreateSchPin(Comp, 'EN', '6', -300, 0, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'PGND', '7', -100, -200, eRotate90, eElectricPower);
        CreateSchRectangle(Comp, -300, -150, 300, 150, eSmall, False);
        CreateSchLabel(Comp, 0, 0, 'DC/DC', 1);
    end
    // Motor driver H-bridge
    else if SymbolName = 'HBRIDGE' then begin
        CreateSchPin(Comp, 'IN1', '1', -300, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'IN2', '2', -300, 0, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'EN', '3', -300, -100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'OUT1', '4', 300, 100, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'OUT2', '5', 300, -100, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'VCC', '6', 0, 200, eRotate270, eElectricPower);
        CreateSchPin(Comp, 'GND', '7', 0, -200, eRotate90, eElectricPower);
        CreateSchRectangle(Comp, -300, -150, 300, 150, eSmall, False);
        CreateSchLabel(Comp, 0, 0, 'H-BRIDGE', 1);
    end
    // Microcontroller (8-bit basic)
    else if SymbolName = 'MCU_8BIT' then begin
        CreateSchPin(Comp, 'VCC', '1', 0, 400, eRotate270, eElectricPower);
        CreateSchPin(Comp, 'GND', '2', 0, -400, eRotate90, eElectricPower);
        CreateSchPin(Comp, 'RST', '3', -400, 300, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'XTAL1', '4', -400, 200, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'XTAL2', '5', -400, 100, eRotate180, eElectricOutput);
        CreateSchPin(Comp, 'PA0', '6', -400, 0, eRotate180, eElectricIO);
        CreateSchPin(Comp, 'PA1', '7', -400, -100, eRotate180, eElectricIO);
        CreateSchPin(Comp, 'PA2', '8', -400, -200, eRotate180, eElectricIO);
        CreateSchPin(Comp, 'PA3', '9', -400, -300, eRotate180, eElectricIO);
        CreateSchPin(Comp, 'PB0', '10', 400, -300, eRotate0, eElectricIO);
        CreateSchPin(Comp, 'PB1', '11', 400, -200, eRotate0, eElectricIO);
        CreateSchPin(Comp, 'PB2', '12', 400, -100, eRotate0, eElectricIO);
        CreateSchPin(Comp, 'PB3', '13', 400, 0, eRotate0, eElectricIO);
        CreateSchPin(Comp, 'TX', '14', 400, 100, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'RX', '15', 400, 200, eRotate0, eElectricInput);
        CreateSchPin(Comp, 'INT', '16', 400, 300, eRotate0, eElectricInput);
        CreateSchRectangle(Comp, -400, -350, 400, 350, eSmall, False);
        CreateSchLabel(Comp, 0, 0, 'MCU', 1);
    end
    // Memory chip
    else if SymbolName = 'MEMORY_IC' then begin
        CreateSchPin(Comp, 'A0', '1', -400, 300, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'A1', '2', -400, 200, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'A2', '3', -400, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'A3', '4', -400, 0, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'A4', '5', -400, -100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'A5', '6', -400, -200, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'A6', '7', -400, -300, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'D0', '8', 400, 300, eRotate0, eElectricIO);
        CreateSchPin(Comp, 'D1', '9', 400, 200, eRotate0, eElectricIO);
        CreateSchPin(Comp, 'D2', '10', 400, 100, eRotate0, eElectricIO);
        CreateSchPin(Comp, 'D3', '11', 400, 0, eRotate0, eElectricIO);
        CreateSchPin(Comp, 'CE', '12', 400, -100, eRotate0, eElectricInput);
        CreateSchPin(Comp, 'OE', '13', 400, -200, eRotate0, eElectricInput);
        CreateSchPin(Comp, 'WE', '14', 400, -300, eRotate0, eElectricInput);
        CreateSchPin(Comp, 'VCC', '15', 0, 400, eRotate270, eElectricPower);
        CreateSchPin(Comp, 'GND', '16', 0, -400, eRotate90, eElectricPower);
        CreateSchRectangle(Comp, -400, -350, 400, 350, eSmall, False);
        CreateSchLabel(Comp, 0, 0, 'SRAM', 1);
    end
    // SPI Flash
    else if SymbolName = 'SPI_FLASH' then begin
        CreateSchPin(Comp, 'CS', '1', -300, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'DO', '2', -300, 0, eRotate180, eElectricOutput);
        CreateSchPin(Comp, 'WP', '3', -300, -100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'GND', '4', 0, -200, eRotate90, eElectricPower);
        CreateSchPin(Comp, 'DI', '5', 300, -100, eRotate0, eElectricInput);
        CreateSchPin(Comp, 'CLK', '6', 300, 0, eRotate0, eElectricInput);
        CreateSchPin(Comp, 'HOLD', '7', 300, 100, eRotate0, eElectricInput);
        CreateSchPin(Comp, 'VCC', '8', 0, 200, eRotate270, eElectricPower);
        CreateSchRectangle(Comp, -300, -150, 300, 150, eSmall, False);
        CreateSchLabel(Comp, 0, 0, 'FLASH', 1);
    end
    // I2C EEPROM
    else if SymbolName = 'I2C_EEPROM' then begin
        CreateSchPin(Comp, 'A0', '1', -300, 100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'A1', '2', -300, 0, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'A2', '3', -300, -100, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'GND', '4', 0, -200, eRotate90, eElectricPower);
        CreateSchPin(Comp, 'SDA', '5', 300, 0, eRotate0, eElectricIO);
        CreateSchPin(Comp, 'SCL', '6', 300, 100, eRotate0, eElectricInput);
        CreateSchPin(Comp, 'WP', '7', 300, -100, eRotate0, eElectricInput);
        CreateSchPin(Comp, 'VCC', '8', 0, 200, eRotate270, eElectricPower);
        CreateSchRectangle(Comp, -300, -150, 300, 150, eSmall, False);
        CreateSchLabel(Comp, 0, 0, 'EEPROM', 1);
    end
    // USB connector symbol
    else if SymbolName = 'USB_CONN' then begin
        CreateSchPin(Comp, 'VBUS', '1', -300, 100, eRotate180, eElectricPower);
        CreateSchPin(Comp, 'D-', '2', -300, 0, eRotate180, eElectricIO);
        CreateSchPin(Comp, 'D+', '3', -300, -100, eRotate180, eElectricIO);
        CreateSchPin(Comp, 'GND', '4', -300, -200, eRotate180, eElectricPower);
        CreateSchPin(Comp, 'SHIELD', '5', 300, 0, eRotate0, eElectricPassive);
        CreateSchRectangle(Comp, -300, -250, 300, 150, eSmall, False);
        CreateSchLabel(Comp, 0, 0, 'USB', 1);
    end
    // HDMI connector
    else if SymbolName = 'HDMI_CONN' then begin
        CreateSchPin(Comp, 'D2+', '1', -400, 350, eRotate180, eElectricIO);
        CreateSchPin(Comp, 'D2S', '2', -400, 250, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'D2-', '3', -400, 150, eRotate180, eElectricIO);
        CreateSchPin(Comp, 'D1+', '4', -400, 50, eRotate180, eElectricIO);
        CreateSchPin(Comp, 'D1S', '5', -400, -50, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'D1-', '6', -400, -150, eRotate180, eElectricIO);
        CreateSchPin(Comp, 'D0+', '7', -400, -250, eRotate180, eElectricIO);
        CreateSchPin(Comp, 'D0S', '8', -400, -350, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'D0-', '9', 400, -350, eRotate0, eElectricIO);
        CreateSchPin(Comp, 'CLK+', '10', 400, -250, eRotate0, eElectricIO);
        CreateSchPin(Comp, 'CLKS', '11', 400, -150, eRotate0, eElectricPassive);
        CreateSchPin(Comp, 'CLK-', '12', 400, -50, eRotate0, eElectricIO);
        CreateSchPin(Comp, 'CEC', '13', 400, 50, eRotate0, eElectricIO);
        CreateSchPin(Comp, 'HPD', '14', 400, 150, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'SCL', '15', 400, 250, eRotate0, eElectricIO);
        CreateSchPin(Comp, 'SDA', '16', 400, 350, eRotate0, eElectricIO);
        CreateSchPin(Comp, '+5V', '17', 0, 450, eRotate270, eElectricPower);
        CreateSchPin(Comp, 'GND', '18', 0, -450, eRotate90, eElectricPower);
        CreateSchRectangle(Comp, -400, -400, 400, 400, eSmall, False);
        CreateSchLabel(Comp, 0, 0, 'HDMI', 1);
    end
    // RJ45 Ethernet jack
    else if SymbolName = 'RJ45_CONN' then begin
        CreateSchPin(Comp, 'TX+', '1', -300, 150, eRotate180, eElectricOutput);
        CreateSchPin(Comp, 'TX-', '2', -300, 50, eRotate180, eElectricOutput);
        CreateSchPin(Comp, 'RX+', '3', -300, -50, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'RX-', '6', -300, -150, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'LED_G', '4', 300, 100, eRotate0, eElectricInput);
        CreateSchPin(Comp, 'LED_Y', '5', 300, 0, eRotate0, eElectricInput);
        CreateSchPin(Comp, 'SHIELD', '7', 300, -100, eRotate0, eElectricPassive);
        CreateSchRectangle(Comp, -300, -200, 300, 200, eSmall, False);
        CreateSchLabel(Comp, 0, 0, 'RJ45', 1);
    end
    // Audio jack
    else if SymbolName = 'AUDIO_JACK' then begin
        CreateSchPin(Comp, 'TIP', '1', -200, 100, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'RING', '2', -200, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'SLEEVE', '3', -200, -100, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'DET', '4', 200, 0, eRotate0, eElectricOutput);
        CreateSchRectangle(Comp, -200, -150, 200, 150, eSmall, False);
        CreateSchLabel(Comp, 0, 0, 'JACK', 1);
    end
    // SD card slot
    else if SymbolName = 'SD_CARD' then begin
        CreateSchPin(Comp, 'DAT2', '1', -300, 250, eRotate180, eElectricIO);
        CreateSchPin(Comp, 'DAT3', '2', -300, 150, eRotate180, eElectricIO);
        CreateSchPin(Comp, 'CMD', '3', -300, 50, eRotate180, eElectricIO);
        CreateSchPin(Comp, 'VDD', '4', -300, -50, eRotate180, eElectricPower);
        CreateSchPin(Comp, 'CLK', '5', -300, -150, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'VSS', '6', -300, -250, eRotate180, eElectricPower);
        CreateSchPin(Comp, 'DAT0', '7', 300, 100, eRotate0, eElectricIO);
        CreateSchPin(Comp, 'DAT1', '8', 300, 0, eRotate0, eElectricIO);
        CreateSchPin(Comp, 'DET', '9', 300, -100, eRotate0, eElectricOutput);
        CreateSchRectangle(Comp, -300, -300, 300, 300, eSmall, False);
        CreateSchLabel(Comp, 0, 0, 'SD', 1);
    end
    // Antenna symbol
    else if SymbolName = 'ANTENNA' then begin
        CreateSchPin(Comp, 'ANT', '1', 0, -100, eRotate90, eElectricPassive);
        CreateSchLine(Comp, 0, 0, 0, 100, eMedium);
        CreateSchLine(Comp, -80, 100, 0, 0, eMedium);
        CreateSchLine(Comp, 80, 100, 0, 0, eMedium);
    end
    // RF balun
    else if SymbolName = 'BALUN' then begin
        CreateSchPin(Comp, 'IN', '1', -200, 0, eRotate180, eElectricInput);
        CreateSchPin(Comp, 'OUT+', '2', 200, 50, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'OUT-', '3', 200, -50, eRotate0, eElectricOutput);
        CreateSchPin(Comp, 'GND', '4', 0, -150, eRotate90, eElectricPower);
        CreateSchRectangle(Comp, -200, -100, 200, 100, eSmall, False);
        CreateSchLabel(Comp, 0, 0, 'BALUN', 1);
    end
    // EMI filter
    else if SymbolName = 'EMI_FILTER' then begin
        CreateSchPin(Comp, 'IN', '1', -200, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'OUT', '2', 200, 0, eRotate0, eElectricPassive);
        CreateSchPin(Comp, 'GND', '3', 0, -100, eRotate90, eElectricPower);
        CreateSchRectangle(Comp, -200, -50, 200, 50, eSmall, False);
        CreateSchLabel(Comp, 0, 25, 'EMI', 1);
    end
    // Ferrite bead
    else if SymbolName = 'FERRITE_BEAD' then begin
        CreateSchPin(Comp, '1', '1', -150, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, '2', '2', 150, 0, eRotate0, eElectricPassive);
        CreateSchRectangle(Comp, -100, -30, 100, 30, eSmall, True);
    end
    // Common mode choke
    else if SymbolName = 'CM_CHOKE' then begin
        CreateSchPin(Comp, '1A', '1', -200, 50, eRotate180, eElectricPassive);
        CreateSchPin(Comp, '1B', '2', 200, 50, eRotate0, eElectricPassive);
        CreateSchPin(Comp, '2A', '3', -200, -50, eRotate180, eElectricPassive);
        CreateSchPin(Comp, '2B', '4', 200, -50, eRotate0, eElectricPassive);
        CreateSchArc(Comp, -75, 50, 25, 0, 180, eSmall);
        CreateSchArc(Comp, -25, 50, 25, 0, 180, eSmall);
        CreateSchArc(Comp, 25, 50, 25, 0, 180, eSmall);
        CreateSchArc(Comp, 75, 50, 25, 0, 180, eSmall);
        CreateSchArc(Comp, -75, -50, 25, 180, 360, eSmall);
        CreateSchArc(Comp, -25, -50, 25, 180, 360, eSmall);
        CreateSchArc(Comp, 25, -50, 25, 180, 360, eSmall);
        CreateSchArc(Comp, 75, -50, 25, 180, 360, eSmall);
        CreateSchLine(Comp, -100, 75, 100, 75, eSmall);
        CreateSchLine(Comp, -100, -75, 100, -75, eSmall);
    end
    // ESD protection
    else if SymbolName = 'ESD_PROT' then begin
        CreateSchPin(Comp, 'IO', '1', -200, 0, eRotate180, eElectricIO);
        CreateSchPin(Comp, 'GND', '2', 0, -100, eRotate90, eElectricPower);
        CreateSchLine(Comp, -100, 50, -100, -50, eSmall);
        CreateSchLine(Comp, -100, -50, 0, 0, eSmall);
        CreateSchLine(Comp, 0, 0, -100, 50, eSmall);
        CreateSchLine(Comp, 0, -50, 0, 50, eSmall);
    end
    // Battery symbol
    else if SymbolName = 'BATTERY' then begin
        CreateSchPin(Comp, '+', '1', 0, 100, eRotate270, eElectricPower);
        CreateSchPin(Comp, '-', '2', 0, -100, eRotate90, eElectricPower);
        CreateSchLine(Comp, -40, 50, 40, 50, eMedium);
        CreateSchLine(Comp, -20, 25, 20, 25, eSmall);
        CreateSchLine(Comp, -40, 0, 40, 0, eMedium);
        CreateSchLine(Comp, -20, -25, 20, -25, eSmall);
        CreateSchLine(Comp, -40, -50, 40, -50, eMedium);
    end
    // Solar cell
    else if SymbolName = 'SOLAR_CELL' then begin
        CreateSchPin(Comp, '+', '1', 0, 100, eRotate270, eElectricPower);
        CreateSchPin(Comp, '-', '2', 0, -100, eRotate90, eElectricPower);
        CreateSchRectangle(Comp, -60, -60, 60, 60, eSmall, False);
        CreateSchLine(Comp, -80, 80, -40, 40, eSmall);
        CreateSchLine(Comp, -50, 80, -40, 40, eSmall);
        CreateSchLine(Comp, -60, 100, 0, 40, eSmall);
        CreateSchLine(Comp, -30, 100, 0, 40, eSmall);
    end
    // Piezo buzzer
    else if SymbolName = 'PIEZO_BUZZER' then begin
        CreateSchPin(Comp, '+', '1', -150, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, '-', '2', 150, 0, eRotate0, eElectricPassive);
        CreateSchArc(Comp, 0, 0, 60, 0, 360, eSmall);
        CreateSchLine(Comp, -60, -20, -60, 20, eSmall);
    end
    // Speaker
    else if SymbolName = 'SPEAKER' then begin
        CreateSchPin(Comp, '+', '1', -150, 25, eRotate180, eElectricPassive);
        CreateSchPin(Comp, '-', '2', -150, -25, eRotate180, eElectricPassive);
        CreateSchRectangle(Comp, -100, -50, -30, 50, eSmall, False);
        CreateSchPolygon4(Comp, -30, -50, 50, -100, 50, 100, -30, 50, eSmall, False);
    end
    // Microphone
    else if SymbolName = 'MICROPHONE' then begin
        CreateSchPin(Comp, 'OUT', '1', 0, -150, eRotate90, eElectricOutput);
        CreateSchPin(Comp, 'GND', '2', 50, -150, eRotate90, eElectricPower);
        CreateSchArc(Comp, 0, 0, 60, 0, 180, eSmall);
        CreateSchLine(Comp, -60, 0, -60, -50, eSmall);
        CreateSchLine(Comp, 60, 0, 60, -50, eSmall);
        CreateSchLine(Comp, -60, -50, 60, -50, eSmall);
    end
    // Photodiode
    else if SymbolName = 'PHOTODIODE' then begin
        CreateSchPin(Comp, 'A', '1', -100, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, 'K', '2', 100, 0, eRotate0, eElectricPassive);
        CreateSchTriangle(Comp, -30, 40, -30, -40, 30, 0, True);
        CreateSchLine(Comp, 30, -40, 30, 40, eMedium);
        CreateSchLine(Comp, -60, 60, -30, 30, eSmall);
        CreateSchLine(Comp, -30, 60, 0, 30, eSmall);
    end
    // Phototransistor
    else if SymbolName = 'PHOTOTRANS' then begin
        CreateSchPin(Comp, 'C', '1', 0, 100, eRotate270, eElectricPassive);
        CreateSchPin(Comp, 'E', '2', 0, -100, eRotate90, eElectricPassive);
        CreateSchArc(Comp, 0, 0, 60, 0, 360, eSmall);
        CreateSchLine(Comp, -30, 30, -30, -30, eSmall);
        CreateSchLine(Comp, -30, -30, 30, -60, eSmall);
        CreateSchLine(Comp, -30, 30, 30, 60, eSmall);
        CreateSchLine(Comp, -70, 50, -50, 30, eSmall);
        CreateSchLine(Comp, -50, 50, -30, 30, eSmall);
    end
    // Thermistor NTC
    else if SymbolName = 'NTC' then begin
        CreateSchPin(Comp, '1', '1', -150, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, '2', '2', 150, 0, eRotate0, eElectricPassive);
        CreateSchRectangle(Comp, -80, -25, 80, 25, eSmall, False);
        CreateSchLine(Comp, -100, 50, -60, 50, eSmall);
        CreateSchLine(Comp, -60, 50, 60, -50, eSmall);
    end
    // PTC thermistor
    else if SymbolName = 'PTC' then begin
        CreateSchPin(Comp, '1', '1', -150, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, '2', '2', 150, 0, eRotate0, eElectricPassive);
        CreateSchRectangle(Comp, -80, -25, 80, 25, eSmall, False);
        CreateSchLine(Comp, -60, -50, 60, 50, eSmall);
        CreateSchLine(Comp, 60, 50, 100, 50, eSmall);
    end
    // Potentiometer
    else if SymbolName = 'POTENTIOMETER' then begin
        CreateSchPin(Comp, '1', '1', -150, 0, eRotate180, eElectricPassive);
        CreateSchPin(Comp, '2', '2', 150, 0, eRotate0, eElectricPassive);
        CreateSchPin(Comp, 'W', '3', 0, -100, eRotate90, eElectricPassive);
        CreateSchRectangle(Comp, -80, -25, 80, 25, eSmall, False);
        CreateSchLine(Comp, 0, -25, -20, -50, eSmall);
        CreateSchLine(Comp, 0, -25, 20, -50, eSmall);
    end
    // Trimmer capacitor
    else if SymbolName = 'TRIMMER_CAP' then begin
        CreateSchPin(Comp, '1', '1', 0, 100, eRotate270, eElectricPassive);
        CreateSchPin(Comp, '2', '2', 0, -100, eRotate90, eElectricPassive);
        CreateSchLine(Comp, -30, 30, 30, 30, eMedium);
        CreateSchLine(Comp, -30, -30, 30, -30, eMedium);
        CreateSchLine(Comp, -40, -60, 40, 60, eSmall);
        CreateSchLine(Comp, 20, 60, 40, 60, eSmall);
        CreateSchLine(Comp, 40, 60, 40, 40, eSmall);
    end

    //==========================================================================
    // PROPERTY TEST SYMBOLS - Exercise all available object properties
    //==========================================================================

    // COLOR_TEST - Test various colors on different object types
    else if SymbolName = 'COLOR_TEST' then begin
        // Red rectangle
        CreateSchRectFull(Comp, -400, 200, -200, 300, eSmall, eLineStyleSolid, True, False, clRed, clRed);
        // Green rectangle
        CreateSchRectFull(Comp, -150, 200, 50, 300, eSmall, eLineStyleSolid, True, False, clGreen, clGreen);
        // Blue rectangle
        CreateSchRectFull(Comp, 100, 200, 300, 300, eSmall, eLineStyleSolid, True, False, clBlue, clBlue);
        // Yellow rectangle
        CreateSchRectFull(Comp, -400, 50, -200, 150, eSmall, eLineStyleSolid, True, False, clYellow, clYellow);
        // Cyan rectangle
        CreateSchRectFull(Comp, -150, 50, 50, 150, eSmall, eLineStyleSolid, True, False, clAqua, clAqua);
        // Magenta rectangle
        CreateSchRectFull(Comp, 100, 50, 300, 150, eSmall, eLineStyleSolid, True, False, clFuchsia, clFuchsia);
        // Black rectangle
        CreateSchRectFull(Comp, -400, -100, -200, 0, eSmall, eLineStyleSolid, True, False, clBlack, clBlack);
        // White rectangle with black border
        CreateSchRectFull(Comp, -150, -100, 50, 0, eSmall, eLineStyleSolid, True, False, clBlack, clWhite);
        // Gray rectangle
        CreateSchRectFull(Comp, 100, -100, 300, 0, eSmall, eLineStyleSolid, True, False, $808080, $808080);
        // Colored lines
        CreateSchLineStyled(Comp, -400, -150, 300, -150, eMedium, eLineStyleSolid, clRed);
        CreateSchLineStyled(Comp, -400, -180, 300, -180, eMedium, eLineStyleSolid, clGreen);
        CreateSchLineStyled(Comp, -400, -210, 300, -210, eMedium, eLineStyleSolid, clBlue);
        // Colored arcs
        CreateSchArcStyled(Comp, -300, -300, 50, 0, 360, eMedium, clRed);
        CreateSchArcStyled(Comp, -150, -300, 50, 0, 360, eMedium, clGreen);
        CreateSchArcStyled(Comp, 0, -300, 50, 0, 360, eMedium, clBlue);
        CreateSchArcStyled(Comp, 150, -300, 50, 0, 360, eMedium, clYellow);
        // Colored labels
        CreateSchLabelFull(Comp, -300, -400, 'RED', 1, clRed, eRotate0, eJustify_Center, False);
        CreateSchLabelFull(Comp, -100, -400, 'GREEN', 1, clGreen, eRotate0, eJustify_Center, False);
        CreateSchLabelFull(Comp, 100, -400, 'BLUE', 1, clBlue, eRotate0, eJustify_Center, False);
    end

    // LINESTYLE_TEST - Test all line styles
    else if SymbolName = 'LINESTYLE_TEST' then begin
        // Solid lines at different widths
        CreateSchLineStyled(Comp, -300, 250, 300, 250, eSmall, eLineStyleSolid, clBlack);
        CreateSchLabelFull(Comp, -400, 250, 'Solid Small', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
        CreateSchLineStyled(Comp, -300, 200, 300, 200, eMedium, eLineStyleSolid, clBlack);
        CreateSchLabelFull(Comp, -400, 200, 'Solid Medium', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
        CreateSchLineStyled(Comp, -300, 150, 300, 150, eLarge, eLineStyleSolid, clBlack);
        CreateSchLabelFull(Comp, -400, 150, 'Solid Large', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
        // Dashed lines
        CreateSchLineStyled(Comp, -300, 50, 300, 50, eSmall, eLineStyleDashed, clBlack);
        CreateSchLabelFull(Comp, -400, 50, 'Dashed Small', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
        CreateSchLineStyled(Comp, -300, 0, 300, 0, eMedium, eLineStyleDashed, clBlack);
        CreateSchLabelFull(Comp, -400, 0, 'Dashed Medium', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
        CreateSchLineStyled(Comp, -300, -50, 300, -50, eLarge, eLineStyleDashed, clBlack);
        CreateSchLabelFull(Comp, -400, -50, 'Dashed Large', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
        // Dotted lines
        CreateSchLineStyled(Comp, -300, -150, 300, -150, eSmall, eLineStyleDotted, clBlack);
        CreateSchLabelFull(Comp, -400, -150, 'Dotted Small', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
        CreateSchLineStyled(Comp, -300, -200, 300, -200, eMedium, eLineStyleDotted, clBlack);
        CreateSchLabelFull(Comp, -400, -200, 'Dotted Medium', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
        CreateSchLineStyled(Comp, -300, -250, 300, -250, eLarge, eLineStyleDotted, clBlack);
        CreateSchLabelFull(Comp, -400, -250, 'Dotted Large', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
        // Rectangles with different line styles
        CreateSchRectFull(Comp, -300, -400, -100, -300, eSmall, eLineStyleSolid, False, False, clBlack, clWhite);
        CreateSchRectFull(Comp, -50, -400, 150, -300, eSmall, eLineStyleDashed, False, False, clBlack, clWhite);
        CreateSchRectFull(Comp, 200, -400, 400, -300, eSmall, eLineStyleDotted, False, False, clBlack, clWhite);
    end

    // TRANSPARENCY_TEST - Test solid vs transparent fills
    else if SymbolName = 'TRANSPARENCY_TEST' then begin
        // Background grid to show transparency
        CreateSchLineStyled(Comp, -350, -350, -350, 350, eSmall, eLineStyleSolid, $C0C0C0);
        CreateSchLineStyled(Comp, -250, -350, -250, 350, eSmall, eLineStyleSolid, $C0C0C0);
        CreateSchLineStyled(Comp, -150, -350, -150, 350, eSmall, eLineStyleSolid, $C0C0C0);
        CreateSchLineStyled(Comp, -50, -350, -50, 350, eSmall, eLineStyleSolid, $C0C0C0);
        CreateSchLineStyled(Comp, 50, -350, 50, 350, eSmall, eLineStyleSolid, $C0C0C0);
        CreateSchLineStyled(Comp, 150, -350, 150, 350, eSmall, eLineStyleSolid, $C0C0C0);
        CreateSchLineStyled(Comp, 250, -350, 250, 350, eSmall, eLineStyleSolid, $C0C0C0);
        CreateSchLineStyled(Comp, 350, -350, 350, 350, eSmall, eLineStyleSolid, $C0C0C0);
        // Solid opaque rectangle (hides grid)
        CreateSchRectFull(Comp, -300, 150, -50, 300, eMedium, eLineStyleSolid, True, False, clBlue, clYellow);
        CreateSchLabelFull(Comp, -175, 100, 'Solid Opaque', 1, clBlack, eRotate0, eJustify_Center, False);
        // Solid transparent rectangle (shows grid through fill)
        CreateSchRectFull(Comp, 50, 150, 300, 300, eMedium, eLineStyleSolid, True, True, clBlue, clYellow);
        CreateSchLabelFull(Comp, 175, 100, 'Solid Transparent', 1, clBlack, eRotate0, eJustify_Center, False);
        // Outline only (no fill)
        CreateSchRectFull(Comp, -300, -100, -50, 50, eMedium, eLineStyleSolid, False, False, clRed, clWhite);
        CreateSchLabelFull(Comp, -175, -150, 'Outline Only', 1, clBlack, eRotate0, eJustify_Center, False);
        // Ellipses with transparency
        CreateSchEllipseFull(Comp, 175, -25, 100, 60, eMedium, True, False, clGreen, clRed);
        CreateSchLabelFull(Comp, 175, -150, 'Ellipse Opaque', 1, clBlack, eRotate0, eJustify_Center, False);
        // Round rect with transparency
        CreateSchRoundRectFull(Comp, -300, -350, -50, -200, 30, 30, eMedium, eLineStyleSolid, True, True, clFuchsia, clAqua);
        CreateSchLabelFull(Comp, -175, -380, 'RoundRect Transparent', 1, clBlack, eRotate0, eJustify_Center, False);
    end

    // PIN_PROPS_TEST - Test all pin properties
    else if SymbolName = 'PIN_PROPS_TEST' then begin
        // Test all electrical types with custom colors
        CreateSchPinFull(Comp, 'INPUT', '1', -400, 300, eRotate180, eElectricInput,
            200, False, True, True, clRed, clBlue, 1, 1, eNoSymbol, eNoSymbol, eSmall, '', 'Input pin', '');
        CreateSchPinFull(Comp, 'OUTPUT', '2', -400, 200, eRotate180, eElectricOutput,
            200, False, True, True, clGreen, clFuchsia, 1, 1, eNoSymbol, eNoSymbol, eSmall, '', 'Output pin', '');
        CreateSchPinFull(Comp, 'BIDIR', '3', -400, 100, eRotate180, eElectricIO,
            200, False, True, True, clBlue, clYellow, 1, 1, eNoSymbol, eNoSymbol, eSmall, '', 'Bidirectional pin', '');
        CreateSchPinFull(Comp, 'PASSIVE', '4', -400, 0, eRotate180, eElectricPassive,
            200, False, True, True, clBlack, clBlack, 1, 1, eNoSymbol, eNoSymbol, eSmall, '', 'Passive pin', '');
        CreateSchPinFull(Comp, 'HIZ', '5', -400, -100, eRotate180, eElectricHiZ,
            200, False, True, True, clAqua, clAqua, 1, 1, eNoSymbol, eNoSymbol, eSmall, '', 'Hi-Z pin', '');
        CreateSchPinFull(Comp, 'OEMI', '6', -400, -200, eRotate180, eElectricOpenEmitter,
            200, False, True, True, clFuchsia, clFuchsia, 1, 1, eNoSymbol, eNoSymbol, eSmall, '', 'Open emitter', '');
        CreateSchPinFull(Comp, 'OCOL', '7', -400, -300, eRotate180, eElectricOpenCollector,
            200, False, True, True, clOlive, clOlive, 1, 1, eNoSymbol, eNoSymbol, eSmall, '', 'Open collector', '');
        CreateSchPinFull(Comp, 'POWER', '8', 400, 300, eRotate0, eElectricPower,
            200, False, True, True, clRed, clRed, 1, 1, eNoSymbol, eNoSymbol, eSmall, 'VCC', 'Power pin', '3.3V');
        // Test IEEE symbols
        CreateSchPinFull(Comp, 'CLK', '9', 400, 100, eRotate0, eElectricInput,
            200, False, True, True, clBlack, clBlack, 1, 1, eClock, eNoSymbol, eSmall, '', 'Clock input', '');
        CreateSchPinFull(Comp, 'INVERTED', '10', 400, 0, eRotate0, eElectricOutput,
            200, False, True, True, clBlack, clBlack, 1, 1, eNoSymbol, eInvert, eSmall, '', 'Inverted output', '');
        CreateSchPinFull(Comp, 'ACTLOWOUT', '11', 400, -100, eRotate0, eElectricInput,
            200, False, True, True, clBlack, clBlack, 1, 1, eNoSymbol, eActiveLowOutput, eSmall, '', 'Active low', '');
        CreateSchPinFull(Comp, 'ANALOGIN', '12', 400, -200, eRotate0, eElectricPassive,
            200, False, True, True, clBlack, clBlack, 1, 1, eAnalogSignalIn, eNoSymbol, eSmall, '', 'Analog', '');
        // Test hidden pins
        CreateSchPinFull(Comp, 'HIDDEN', '13', 400, -300, eRotate0, eElectricPower,
            200, True, True, True, clBlack, clBlack, 1, 1, eNoSymbol, eNoSymbol, eSmall, 'GND', 'Hidden power', '0V');
        // Symbol body
        CreateSchRectFull(Comp, -200, -350, 200, 350, eMedium, eLineStyleSolid, False, False, clBlack, clWhite);
    end

    // LABEL_JUSTIFY_TEST - Test all label justification options
    else if SymbolName = 'LABEL_JUSTIFY_TEST' then begin
        // Reference grid
        CreateSchLineStyled(Comp, -300, 0, 300, 0, eSmall, eLineStyleDashed, $808080);
        CreateSchLineStyled(Comp, 0, -300, 0, 300, eSmall, eLineStyleDashed, $808080);
        // Top row - bottom aligned
        CreateSchLabelFull(Comp, -200, 200, 'BL', 1, clRed, eRotate0, eJustify_BottomLeft, False);
        CreateSchLabelFull(Comp, 0, 200, 'BC', 1, clGreen, eRotate0, eJustify_BottomCenter, False);
        CreateSchLabelFull(Comp, 200, 200, 'BR', 1, clBlue, eRotate0, eJustify_BottomRight, False);
        // Middle row - center aligned
        CreateSchLabelFull(Comp, -200, 0, 'CL', 1, clRed, eRotate0, eJustify_CenterLeft, False);
        CreateSchLabelFull(Comp, 0, 0, 'CC', 1, clGreen, eRotate0, eJustify_Center, False);
        CreateSchLabelFull(Comp, 200, 0, 'CR', 1, clBlue, eRotate0, eJustify_CenterRight, False);
        // Bottom row - top aligned
        CreateSchLabelFull(Comp, -200, -200, 'TL', 1, clRed, eRotate0, eJustify_TopLeft, False);
        CreateSchLabelFull(Comp, 0, -200, 'TC', 1, clGreen, eRotate0, eJustify_TopCenter, False);
        CreateSchLabelFull(Comp, 200, -200, 'TR', 1, clBlue, eRotate0, eJustify_TopRight, False);
        // Orientation tests
        CreateSchLabelFull(Comp, -300, 100, 'Rot0', 1, clBlack, eRotate0, eJustify_Center, False);
        CreateSchLabelFull(Comp, -300, 50, 'Rot90', 1, clBlack, eRotate90, eJustify_Center, False);
        CreateSchLabelFull(Comp, -300, 0, 'Rot180', 1, clBlack, eRotate180, eJustify_Center, False);
        CreateSchLabelFull(Comp, -300, -50, 'Rot270', 1, clBlack, eRotate270, eJustify_Center, False);
        // Mirrored test
        CreateSchLabelFull(Comp, 300, 100, 'Normal', 1, clFuchsia, eRotate0, eJustify_Center, False);
        CreateSchLabelFull(Comp, 300, 0, 'Mirrored', 1, clFuchsia, eRotate0, eJustify_Center, True);
    end

    // POLYLINE_ARROW_TEST - Test polyline arrow shapes
    else if SymbolName = 'POLYLINE_ARROW_TEST' then begin
        // No arrows
        CreateSchPolylineStyled(Comp, -300, 300, -100, 300, -100, 250, 100, 250,
            eMedium, eLineStyleSolid, clBlack, eLineShapeNone, eLineShapeNone, eMedium);
        CreateSchLabelFull(Comp, -350, 275, 'None', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
        // Start arrow
        CreateSchPolylineStyled(Comp, -300, 200, -100, 200, -100, 150, 100, 150,
            eMedium, eLineStyleSolid, clRed, eLineShapeArrow, eLineShapeNone, eMedium);
        CreateSchLabelFull(Comp, -350, 175, 'Start Arrow', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
        // End arrow
        CreateSchPolylineStyled(Comp, -300, 100, -100, 100, -100, 50, 100, 50,
            eMedium, eLineStyleSolid, clGreen, eLineShapeNone, eLineShapeArrow, eMedium);
        CreateSchLabelFull(Comp, -350, 75, 'End Arrow', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
        // Both arrows
        CreateSchPolylineStyled(Comp, -300, 0, -100, 0, -100, -50, 100, -50,
            eMedium, eLineStyleSolid, clBlue, eLineShapeArrow, eLineShapeArrow, eMedium);
        CreateSchLabelFull(Comp, -350, -25, 'Both Arrows', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
        // Solid tail start
        CreateSchPolylineStyled(Comp, -300, -100, -100, -100, -100, -150, 100, -150,
            eMedium, eLineStyleSolid, clFuchsia, eLineShapeSolidTail, eLineShapeNone, eMedium);
        CreateSchLabelFull(Comp, -350, -125, 'Solid Tail Start', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
        // Solid arrow end
        CreateSchPolylineStyled(Comp, -300, -200, -100, -200, -100, -250, 100, -250,
            eMedium, eLineStyleSolid, clAqua, eLineShapeNone, eLineShapeSolidArrow, eMedium);
        CreateSchLabelFull(Comp, -350, -225, 'Solid Arrow End', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
        // Different line styles with arrows
        CreateSchPolylineStyled(Comp, -300, -300, -100, -300, -100, -350, 100, -350,
            eLarge, eLineStyleDashed, clOlive, eLineShapeArrow, eLineShapeArrow, eLarge);
        CreateSchLabelFull(Comp, -350, -325, 'Dashed + Arrows', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
    end

    // TEXTFRAME_TEST - Test text frame properties
    else if SymbolName = 'TEXTFRAME_TEST' then begin
        // Left aligned text
        CreateSchTextFrameFull(Comp, -400, 200, -100, 350, 'Left Aligned Text', 1, eLeftAlign,
            True, True, False, True, True, clBlack, clYellow, clBlack, eSmall, eLineStyleSolid, 10);
        // Center aligned text
        CreateSchTextFrameFull(Comp, -50, 200, 250, 350, 'Center Aligned Text', 1, eHorizontalCentreAlign,
            True, True, False, True, True, clBlue, clAqua, clBlue, eSmall, eLineStyleSolid, 10);
        // Right aligned text
        CreateSchTextFrameFull(Comp, 300, 200, 600, 350, 'Right Aligned Text', 1, eRightAlign,
            True, True, False, True, True, clRed, clFuchsia, clRed, eSmall, eLineStyleSolid, 10);
        // No border
        CreateSchTextFrameFull(Comp, -400, 0, -100, 150, 'No Border', 1, eLeftAlign,
            False, True, False, True, True, clBlack, clGreen, clBlack, eSmall, eLineStyleSolid, 10);
        // Dashed border
        CreateSchTextFrameFull(Comp, -50, 0, 250, 150, 'Dashed Border', 1, eHorizontalCentreAlign,
            True, False, False, True, True, clBlack, clWhite, clBlack, eMedium, eLineStyleDashed, 10);
        // Dotted border
        CreateSchTextFrameFull(Comp, 300, 0, 600, 150, 'Dotted Border', 1, eHorizontalCentreAlign,
            True, False, False, True, True, clBlack, clWhite, clBlack, eMedium, eLineStyleDotted, 10);
        // Word wrap with long text
        CreateSchTextFrameFull(Comp, -400, -200, 100, -50, 'This is a long text that should wrap to multiple lines within the text frame', 1, eLeftAlign,
            True, True, False, True, True, clBlack, clWhite, clGreen, eSmall, eLineStyleSolid, 5);
        // Large text margin
        CreateSchTextFrameFull(Comp, 150, -200, 600, -50, 'Large Margin (30)', 1, eHorizontalCentreAlign,
            True, True, False, True, True, clBlack, $E0E0E0, clBlack, eSmall, eLineStyleSolid, 30);
        // Transparent fill
        CreateSchTextFrameFull(Comp, -400, -400, 0, -250, 'Transparent Fill', 1, eLeftAlign,
            True, True, True, True, True, clBlue, clYellow, clBlue, eMedium, eLineStyleSolid, 10);
    end

    // ROUNDRECT_TEST_FULL - Test round rectangle corner variations
    else if SymbolName = 'ROUNDRECT_TEST_FULL' then begin
        // Small corners
        CreateSchRoundRectFull(Comp, -400, 200, -200, 350, 10, 10, eSmall, eLineStyleSolid, True, False, clBlack, clRed);
        CreateSchLabelFull(Comp, -300, 150, 'R=10', 1, clBlack, eRotate0, eJustify_Center, False);
        // Medium corners
        CreateSchRoundRectFull(Comp, -150, 200, 50, 350, 25, 25, eSmall, eLineStyleSolid, True, False, clBlack, clGreen);
        CreateSchLabelFull(Comp, -50, 150, 'R=25', 1, clBlack, eRotate0, eJustify_Center, False);
        // Large corners
        CreateSchRoundRectFull(Comp, 100, 200, 300, 350, 50, 50, eSmall, eLineStyleSolid, True, False, clBlack, clBlue);
        CreateSchLabelFull(Comp, 200, 150, 'R=50', 1, clBlack, eRotate0, eJustify_Center, False);
        // Asymmetric corners (X != Y)
        CreateSchRoundRectFull(Comp, -400, -50, -200, 100, 50, 20, eSmall, eLineStyleSolid, True, False, clBlack, clFuchsia);
        CreateSchLabelFull(Comp, -300, -100, 'X=50,Y=20', 1, clBlack, eRotate0, eJustify_Center, False);
        CreateSchRoundRectFull(Comp, -150, -50, 50, 100, 20, 50, eSmall, eLineStyleSolid, True, False, clBlack, clAqua);
        CreateSchLabelFull(Comp, -50, -100, 'X=20,Y=50', 1, clBlack, eRotate0, eJustify_Center, False);
        // Different line styles
        CreateSchRoundRectFull(Comp, 100, -50, 300, 100, 30, 30, eMedium, eLineStyleDashed, True, False, clBlue, clYellow);
        CreateSchLabelFull(Comp, 200, -100, 'Dashed', 1, clBlack, eRotate0, eJustify_Center, False);
        // Outline only
        CreateSchRoundRectFull(Comp, -400, -300, -200, -150, 40, 40, eLarge, eLineStyleSolid, False, False, clRed, clWhite);
        CreateSchLabelFull(Comp, -300, -350, 'Outline', 1, clBlack, eRotate0, eJustify_Center, False);
        // Transparent
        CreateSchRoundRectFull(Comp, -150, -300, 50, -150, 30, 30, eSmall, eLineStyleSolid, True, True, clGreen, clYellow);
        CreateSchLabelFull(Comp, -50, -350, 'Transparent', 1, clBlack, eRotate0, eJustify_Center, False);
    end

    // ELLIPSE_ARC_TEST - Test ellipses and elliptical arcs
    else if SymbolName = 'ELLIPSE_ARC_TEST' then begin
        // Circle (equal radii)
        CreateSchEllipseFull(Comp, -300, 250, 50, 50, eSmall, True, False, clBlack, clRed);
        CreateSchLabelFull(Comp, -300, 150, 'Circle', 1, clBlack, eRotate0, eJustify_Center, False);
        // Horizontal ellipse
        CreateSchEllipseFull(Comp, 0, 250, 80, 40, eSmall, True, False, clBlack, clGreen);
        CreateSchLabelFull(Comp, 0, 150, 'H-Ellipse', 1, clBlack, eRotate0, eJustify_Center, False);
        // Vertical ellipse
        CreateSchEllipseFull(Comp, 300, 250, 40, 80, eSmall, True, False, clBlack, clBlue);
        CreateSchLabelFull(Comp, 300, 150, 'V-Ellipse', 1, clBlack, eRotate0, eJustify_Center, False);
        // Outline ellipse
        CreateSchEllipseFull(Comp, -300, 0, 60, 40, eMedium, False, False, clFuchsia, clWhite);
        CreateSchLabelFull(Comp, -300, -100, 'Outline', 1, clBlack, eRotate0, eJustify_Center, False);
        // Transparent ellipse
        CreateSchEllipseFull(Comp, 0, 0, 60, 40, eSmall, True, True, clAqua, clYellow);
        CreateSchLabelFull(Comp, 0, -100, 'Transparent', 1, clBlack, eRotate0, eJustify_Center, False);
        // Elliptical arcs
        CreateSchEllipticalArc(Comp, -300, -250, 60, 40, 0, 90, eMedium, clRed);
        CreateSchLabelFull(Comp, -300, -350, '0-90', 1, clBlack, eRotate0, eJustify_Center, False);
        CreateSchEllipticalArc(Comp, 0, -250, 60, 40, 45, 180, eMedium, clGreen);
        CreateSchLabelFull(Comp, 0, -350, '45-180', 1, clBlack, eRotate0, eJustify_Center, False);
        CreateSchEllipticalArc(Comp, 300, -250, 60, 40, 90, 270, eMedium, clBlue);
        CreateSchLabelFull(Comp, 300, -350, '90-270', 1, clBlack, eRotate0, eJustify_Center, False);
    end

    // PIE_TEST - Test pie shapes
    else if SymbolName = 'PIE_TEST' then begin
        // Quarter pie
        CreateSchPieFull(Comp, -300, 250, 80, 0, 90, eSmall, True, clBlack, clRed);
        CreateSchLabelFull(Comp, -300, 100, '0-90', 1, clBlack, eRotate0, eJustify_Center, False);
        // Half pie
        CreateSchPieFull(Comp, 0, 250, 80, 0, 180, eSmall, True, clBlack, clGreen);
        CreateSchLabelFull(Comp, 0, 100, '0-180', 1, clBlack, eRotate0, eJustify_Center, False);
        // Three quarter pie
        CreateSchPieFull(Comp, 300, 250, 80, 0, 270, eSmall, True, clBlack, clBlue);
        CreateSchLabelFull(Comp, 300, 100, '0-270', 1, clBlack, eRotate0, eJustify_Center, False);
        // Different angles
        CreateSchPieFull(Comp, -300, -50, 80, 45, 135, eMedium, True, clFuchsia, clYellow);
        CreateSchLabelFull(Comp, -300, -200, '45-135', 1, clBlack, eRotate0, eJustify_Center, False);
        CreateSchPieFull(Comp, 0, -50, 80, 90, 300, eMedium, True, clAqua, clOlive);
        CreateSchLabelFull(Comp, 0, -200, '90-300', 1, clBlack, eRotate0, eJustify_Center, False);
        // Outline pie
        CreateSchPieFull(Comp, 300, -50, 80, 30, 150, eLarge, False, clRed, clWhite);
        CreateSchLabelFull(Comp, 300, -200, 'Outline', 1, clBlack, eRotate0, eJustify_Center, False);
    end

    // POLYGON_COLORS_TEST - Test polygon colors
    else if SymbolName = 'POLYGON_COLORS_TEST' then begin
        // Solid triangles
        CreateSchPolygonFull(Comp, -350, 100, -250, 300, -150, 100, True, clBlack, clRed);
        CreateSchPolygonFull(Comp, -100, 100, 0, 300, 100, 100, True, clBlack, clGreen);
        CreateSchPolygonFull(Comp, 150, 100, 250, 300, 350, 100, True, clBlack, clBlue);
        // Outline triangles
        CreateSchPolygonFull(Comp, -350, -200, -250, 0, -150, -200, False, clRed, clWhite);
        CreateSchPolygonFull(Comp, -100, -200, 0, 0, 100, -200, False, clGreen, clWhite);
        CreateSchPolygonFull(Comp, 150, -200, 250, 0, 350, -200, False, clBlue, clWhite);
        // Different colors
        CreateSchPolygonFull(Comp, -350, -500, -250, -300, -150, -500, True, clFuchsia, clYellow);
        CreateSchPolygonFull(Comp, -100, -500, 0, -300, 100, -500, True, clAqua, clOlive);
        CreateSchPolygonFull(Comp, 150, -500, 250, -300, 350, -500, True, clNavy, clLime);
    end

    // FONT_TEST - Test different font IDs
    else if SymbolName = 'FONT_TEST' then begin
        // Font ID 1 (default)
        CreateSchLabelFull(Comp, 0, 300, 'Font ID 1', 1, clBlack, eRotate0, eJustify_Center, False);
        // Font ID 2
        CreateSchLabelFull(Comp, 0, 200, 'Font ID 2', 2, clRed, eRotate0, eJustify_Center, False);
        // Font ID 3
        CreateSchLabelFull(Comp, 0, 100, 'Font ID 3', 3, clGreen, eRotate0, eJustify_Center, False);
        // Font ID 4
        CreateSchLabelFull(Comp, 0, 0, 'Font ID 4', 4, clBlue, eRotate0, eJustify_Center, False);
        // Font ID 5
        CreateSchLabelFull(Comp, 0, -100, 'Font ID 5', 5, clFuchsia, eRotate0, eJustify_Center, False);
        // Font ID 6
        CreateSchLabelFull(Comp, 0, -200, 'Font ID 6', 6, clAqua, eRotate0, eJustify_Center, False);
        // Pins with different font IDs for name and designator
        CreateSchPinFull(Comp, 'Pin1', 'A', -400, 300, eRotate180, eElectricInput,
            200, False, True, True, clRed, clBlue, 2, 3, eNoSymbol, eNoSymbol, eSmall, '', '', '');
        CreateSchPinFull(Comp, 'Pin2', 'B', -400, 200, eRotate180, eElectricOutput,
            200, False, True, True, clGreen, clFuchsia, 4, 5, eNoSymbol, eNoSymbol, eSmall, '', '', '');
        // Symbol body
        CreateSchRectFull(Comp, -200, -250, 200, 350, eSmall, eLineStyleSolid, False, False, clBlack, clWhite);
    end;


    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    SchServer.ProcessControl.PostProcess(SchDoc, '');

    // Save and export
    Doc.DoFileSave('SchLib');
    ExportSchLibToJson(SchLib, JsonPath);
    CloseDocument(FilePath);
end;

procedure GenerateIndividualSchLibFiles;
begin
    // Generate individual files for shape test symbols
    GenerateSingleSchLibFile('CIRCLE_FILLED', 'Filled circle test', 'X?');
    GenerateSingleSchLibFile('CIRCLE_OUTLINE', 'Outline circle test', 'X?');
    GenerateSingleSchLibFile('ELLIPSE_TEST', 'Ellipse test', 'X?');
    GenerateSingleSchLibFile('ROUNDRECT_TEST', 'Rounded rectangle test', 'X?');
    GenerateSingleSchLibFile('TEXTFRAME_TEST', 'Text frame test', 'X?');
    GenerateSingleSchLibFile('ARC_FULL', 'Full arc (circle) test', 'X?');
    GenerateSingleSchLibFile('POLYLINE_TEST', 'Polyline test', 'X?');
    GenerateSingleSchLibFile('POLYGON_TEST', 'Polygon test', 'X?');

    // Basic component symbols
    GenerateSingleSchLibFile('RESISTOR', 'Basic resistor symbol', 'R?');
    GenerateSingleSchLibFile('CAPACITOR', 'Basic capacitor symbol', 'C?');

    // Logic gates
    GenerateSingleSchLibFile('XOR_GATE', 'XOR gate symbol', 'U?');
    GenerateSingleSchLibFile('NAND_GATE', 'NAND gate symbol', 'U?');
    GenerateSingleSchLibFile('NOR_GATE', 'NOR gate symbol', 'U?');
    GenerateSingleSchLibFile('XNOR_GATE', 'XNOR gate symbol', 'U?');
    GenerateSingleSchLibFile('BUFFER_GATE', 'Buffer gate symbol', 'U?');
    GenerateSingleSchLibFile('SCHMITT_TRIGGER', 'Schmitt trigger symbol', 'U?');

    // Flip-flops and sequential logic
    GenerateSingleSchLibFile('DFF', 'D Flip-Flop symbol', 'U?');
    GenerateSingleSchLibFile('JKFF', 'JK Flip-Flop symbol', 'U?');

    // ICs
    GenerateSingleSchLibFile('TIMER_555', '555 Timer IC symbol', 'U?');
    GenerateSingleSchLibFile('IC_16PIN', '16-pin generic IC symbol', 'U?');

    // Passive components
    GenerateSingleSchLibFile('CRYSTAL', 'Crystal oscillator symbol', 'Y?');
    GenerateSingleSchLibFile('FUSE', 'Fuse symbol', 'F?');
    GenerateSingleSchLibFile('RELAY_COIL', 'Relay coil symbol', 'K?');
    GenerateSingleSchLibFile('TRANSFORMER', 'Transformer symbol', 'T?');
    GenerateSingleSchLibFile('THERMISTOR', 'Thermistor symbol', 'RT?');
    GenerateSingleSchLibFile('VARISTOR', 'Varistor symbol', 'RV?');
    GenerateSingleSchLibFile('LDR', 'Light dependent resistor symbol', 'R?');

    // Switches
    GenerateSingleSchLibFile('SWITCH_SPST', 'SPST switch symbol', 'SW?');
    GenerateSingleSchLibFile('SWITCH_SPDT', 'SPDT switch symbol', 'SW?');
    GenerateSingleSchLibFile('QUAD_SWITCH', 'Quad switch IC symbol', 'U?');

    // Diodes and semiconductors
    GenerateSingleSchLibFile('SCHOTTKY', 'Schottky diode symbol', 'D?');
    GenerateSingleSchLibFile('TVS_DIODE', 'TVS diode symbol', 'D?');
    GenerateSingleSchLibFile('TRIAC', 'Triac symbol', 'Q?');
    GenerateSingleSchLibFile('SCR', 'SCR thyristor symbol', 'Q?');

    // Power and interface
    GenerateSingleSchLibFile('VREG', 'Voltage regulator symbol', 'U?');
    GenerateSingleSchLibFile('OPTOCOUPLER', 'Optocoupler symbol', 'U?');

    // Connectors and test points
    GenerateSingleSchLibFile('CONN_2PIN', '2-pin connector symbol', 'J?');
    GenerateSingleSchLibFile('TESTPOINT', 'Test point symbol', 'TP?');
    GenerateSingleSchLibFile('NC_SYMBOL', 'No-connect symbol', 'NC?');

    // Pin type variations
    GenerateSingleSchLibFile('BIDIR_PIN', 'Bidirectional pin test', 'X?');
    GenerateSingleSchLibFile('OC_PIN', 'Open collector pin test', 'X?');
    GenerateSingleSchLibFile('HIZ_PIN', 'High-Z pin test', 'X?');
    GenerateSingleSchLibFile('EMITTER_PIN', 'Open emitter pin test', 'X?');
    GenerateSingleSchLibFile('IEEE_PINS', 'IEEE symbol pins test', 'U?');

    // Multi-part symbols
    GenerateSingleSchLibFile('MULTIPART_A', 'Multi-part symbol A', 'U?');
    GenerateSingleSchLibFile('MULTIPART_B', 'Multi-part symbol B', 'U?');

    // Power symbols
    GenerateSingleSchLibFile('POWER_IN_PIN', 'Power input pin symbol', 'PWR?');
    GenerateSingleSchLibFile('POWER_OUT_PIN', 'Power output pin symbol', 'PWR?');
    GenerateSingleSchLibFile('GND_SYMBOL', 'Ground symbol', 'GND?');
    GenerateSingleSchLibFile('VCC_SYMBOL', 'VCC power symbol', 'VCC?');
    GenerateSingleSchLibFile('VDD_SYMBOL', 'VDD power symbol', 'VDD?');
    GenerateSingleSchLibFile('VSS_SYMBOL', 'VSS power symbol', 'VSS?');
    GenerateSingleSchLibFile('HIDDEN_PWR_PIN', 'Hidden power pin test', 'U?');

    // IEEE pin symbol variations
    GenerateSingleSchLibFile('CLOCK_PIN', 'Clock pin with IEEE symbol', 'U?');
    GenerateSingleSchLibFile('INVERTED_PIN', 'Inverted pin with dot', 'U?');
    GenerateSingleSchLibFile('ACTIVE_LOW_PIN', 'Active low pin symbol', 'U?');

    // Analog/mixed signal
    GenerateSingleSchLibFile('ANALOG_IN', 'Analog input symbol', 'AIN?');
    GenerateSingleSchLibFile('ANALOG_OUT', 'Analog output symbol', 'AOUT?');
    GenerateSingleSchLibFile('DAC_SYMBOL', 'DAC symbol', 'U?');
    GenerateSingleSchLibFile('ADC_SYMBOL', 'ADC symbol', 'U?');
    GenerateSingleSchLibFile('COMPARATOR', 'Comparator symbol', 'U?');
    GenerateSingleSchLibFile('INSTR_AMP', 'Instrumentation amplifier', 'U?');
    GenerateSingleSchLibFile('VREF', 'Voltage reference symbol', 'U?');

    // Power management
    GenerateSingleSchLibFile('LDO_REG', 'LDO regulator symbol', 'U?');
    GenerateSingleSchLibFile('DCDC_CONV', 'DC-DC converter symbol', 'U?');
    GenerateSingleSchLibFile('HBRIDGE', 'H-bridge motor driver', 'U?');

    // Digital ICs
    GenerateSingleSchLibFile('MCU_8BIT', '8-bit microcontroller', 'U?');
    GenerateSingleSchLibFile('MEMORY_IC', 'Memory IC symbol', 'U?');
    GenerateSingleSchLibFile('SPI_FLASH', 'SPI Flash symbol', 'U?');
    GenerateSingleSchLibFile('I2C_EEPROM', 'I2C EEPROM symbol', 'U?');

    // Connectors
    GenerateSingleSchLibFile('USB_CONN', 'USB connector symbol', 'J?');
    GenerateSingleSchLibFile('HDMI_CONN', 'HDMI connector symbol', 'J?');
    GenerateSingleSchLibFile('RJ45_CONN', 'RJ45 Ethernet jack', 'J?');
    GenerateSingleSchLibFile('AUDIO_JACK', 'Audio jack symbol', 'J?');
    GenerateSingleSchLibFile('SD_CARD', 'SD card slot symbol', 'J?');

    // RF components
    GenerateSingleSchLibFile('ANTENNA', 'Antenna symbol', 'ANT?');
    GenerateSingleSchLibFile('BALUN', 'RF balun symbol', 'T?');
    GenerateSingleSchLibFile('EMI_FILTER', 'EMI filter symbol', 'FL?');
    GenerateSingleSchLibFile('FERRITE_BEAD', 'Ferrite bead symbol', 'FB?');
    GenerateSingleSchLibFile('CM_CHOKE', 'Common mode choke', 'L?');
    GenerateSingleSchLibFile('ESD_PROT', 'ESD protection symbol', 'D?');

    // Power sources
    GenerateSingleSchLibFile('BATTERY', 'Battery symbol', 'BT?');
    GenerateSingleSchLibFile('SOLAR_CELL', 'Solar cell symbol', 'SC?');

    // Electro-acoustic
    GenerateSingleSchLibFile('PIEZO_BUZZER', 'Piezo buzzer symbol', 'BZ?');
    GenerateSingleSchLibFile('SPEAKER', 'Speaker symbol', 'LS?');
    GenerateSingleSchLibFile('MICROPHONE', 'Microphone symbol', 'MIC?');

    // Opto-electronics
    GenerateSingleSchLibFile('PHOTODIODE', 'Photodiode symbol', 'D?');
    GenerateSingleSchLibFile('PHOTOTRANS', 'Phototransistor symbol', 'Q?');

    // Temperature sensors
    GenerateSingleSchLibFile('NTC', 'NTC thermistor symbol', 'RT?');
    GenerateSingleSchLibFile('PTC', 'PTC thermistor symbol', 'RT?');

    // Adjustable components
    GenerateSingleSchLibFile('POTENTIOMETER', 'Potentiometer symbol', 'RV?');
    GenerateSingleSchLibFile('TRIMMER_CAP', 'Trimmer capacitor symbol', 'C?');

    //==========================================================================
    // PROPERTY TEST SYMBOLS - Exercise all available object properties
    //==========================================================================
    GenerateSingleSchLibFile('COLOR_TEST', 'Test colors on all object types', 'X?');
    GenerateSingleSchLibFile('LINESTYLE_TEST', 'Test solid/dashed/dotted line styles', 'X?');
    GenerateSingleSchLibFile('TRANSPARENCY_TEST', 'Test solid vs transparent fills', 'X?');
    GenerateSingleSchLibFile('PIN_PROPS_TEST', 'Test all pin properties', 'X?');
    GenerateSingleSchLibFile('LABEL_JUSTIFY_TEST', 'Test all label justification options', 'X?');
    GenerateSingleSchLibFile('POLYLINE_ARROW_TEST', 'Test polyline arrow shapes', 'X?');
    GenerateSingleSchLibFile('TEXTFRAME_TEST', 'Test text frame properties', 'X?');
    GenerateSingleSchLibFile('ROUNDRECT_TEST_FULL', 'Test round rectangle corner variations', 'X?');
    GenerateSingleSchLibFile('ELLIPSE_ARC_TEST', 'Test ellipses and elliptical arcs', 'X?');
    GenerateSingleSchLibFile('PIE_TEST', 'Test pie shapes', 'X?');
    GenerateSingleSchLibFile('POLYGON_COLORS_TEST', 'Test polygon colors', 'X?');
    GenerateSingleSchLibFile('FONT_TEST', 'Test different font IDs', 'X?');
end;

{==============================================================================
  PCB LIBRARY JSON EXPORTER
==============================================================================}

procedure ExportPcbPadToJson(Pad: IPCB_Primitive; AddComma: Boolean);
var
    Pad4: IPCB_Pad4;
begin
    Pad4 := Pad;
    JsonOpenObject('');
    JsonWriteString('objectType', 'Pad', True);
    JsonWriteString('name', Pad4.Name, True);
    JsonWriteString('pinDescriptor', Pad4.PinDescriptor, True);
    JsonWriteCoord('x', Pad4.X, True);
    JsonWriteCoord('y', Pad4.Y, True);
    JsonWriteInteger('mode', Pad4.Mode, True);
    // IPCB_Pad4 specific properties
    JsonWriteCoord('maxXSignalLayers', Pad4.MaxXSignalLayers, True);
    JsonWriteCoord('maxYSignalLayers', Pad4.MaxYSignalLayers, True);
    JsonWriteCoord('pinPackageLength', Pad4.PinPackageLength, True);
    JsonWriteCoord('xPadOffsetAll', Pad4.XPadOffsetAll, True);
    JsonWriteCoord('yPadOffsetAll', Pad4.YPadOffsetAll, True);
    // Size properties
    JsonWriteCoord('topXSize', Pad4.TopXSize, True);
    JsonWriteCoord('topYSize', Pad4.TopYSize, True);
    JsonWriteCoord('midXSize', Pad4.MidXSize, True);
    JsonWriteCoord('midYSize', Pad4.MidYSize, True);
    JsonWriteCoord('botXSize', Pad4.BotXSize, True);
    JsonWriteCoord('botYSize', Pad4.BotYSize, True);
    // Shape properties
    JsonWriteInteger('topShape', Pad4.TopShape, True);
    JsonWriteInteger('midShape', Pad4.MidShape, True);
    JsonWriteInteger('botShape', Pad4.BotShape, True);
    // Hole properties
    JsonWriteCoord('holeSize', Pad4.HoleSize, True);
    JsonWriteCoord('holeWidth', Pad4.HoleWidth, True);
    JsonWriteFloat('holeRotation', Pad4.HoleRotation, True);
    JsonWriteInteger('holeType', Pad4.HoleType, True);
    JsonWriteInteger('drillType', Pad4.DrillType, True);
    JsonWriteCoord('holePositiveTolerance', Pad4.HolePositiveTolerance, True);
    JsonWriteCoord('holeNegativeTolerance', Pad4.HoleNegativeTolerance, True);
    JsonWriteBoolean('plated', Pad4.Plated, True);
    // Rotation and layer
    JsonWriteFloat('rotation', Pad4.Rotation, True);
    JsonWriteInteger('layer', Pad4.Layer, True);
    // Swapping IDs
    JsonWriteString('swapID_Pad', Pad4.SwapID_Pad, True);
    JsonWriteString('swapID_Part', Pad4.SwapID_Part, True);
    JsonWriteInteger('ownerPartID', Pad4.OwnerPart_ID, True);
    JsonWriteString('swappedPadName', Pad4.SwappedPadName, True);
    JsonWriteInteger('jumperID', Pad4.JumperID, True);
    // Plane connection
    JsonWriteInteger('powerPlaneConnectStyle', Pad4.PowerPlaneConnectStyle, True);
    JsonWriteCoord('reliefConductorWidth', Pad4.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', Pad4.ReliefEntries, True);
    JsonWriteCoord('reliefAirGap', Pad4.ReliefAirGap, True);
    // Mask expansions
    JsonWriteCoord('pasteMaskExpansion', Pad4.PasteMaskExpansion, True);
    JsonWriteCoord('solderMaskExpansion', Pad4.SolderMaskExpansion, True);
    JsonWriteBoolean('solderMaskExpansionFromHoleEdge', Pad4.SolderMaskExpansionFromHoleEdge, True);
    JsonWriteCoord('powerPlaneClearance', Pad4.PowerPlaneClearance, True);
    JsonWriteCoord('powerPlaneReliefExpansion', Pad4.PowerPlaneReliefExpansion, True);
    // Flags and states
    JsonWriteBoolean('isTenting', Pad4.IsTenting, True);
    JsonWriteBoolean('isTenting_Top', Pad4.IsTenting_Top, True);
    JsonWriteBoolean('isTenting_Bottom', Pad4.IsTenting_Bottom, True);
    JsonWriteBoolean('isTestpoint_Top', Pad4.IsTestpoint_Top, True);
    JsonWriteBoolean('isTestpoint_Bottom', Pad4.IsTestpoint_Bottom, True);
    JsonWriteBoolean('isAssyTestpoint_Top', Pad4.IsAssyTestpoint_Top, True);
    JsonWriteBoolean('isAssyTestpoint_Bottom', Pad4.IsAssyTestpoint_Bottom, True);
    JsonWriteBoolean('isKeepout', Pad4.IsKeepout, True);
    JsonWriteBoolean('inComponent', Pad4.InComponent, True);
    JsonWriteBoolean('inNet', Pad4.InNet, True);
    JsonWriteBoolean('drcError', Pad4.DRCError, True);
    JsonWriteBoolean('miscFlag1', Pad4.MiscFlag1, True);
    JsonWriteBoolean('miscFlag2', Pad4.MiscFlag2, True);
    JsonWriteBoolean('miscFlag3', Pad4.MiscFlag3, True);
    // Identifier strings
    JsonWriteString('objectIDString', Pad4.ObjectIDString, True);
    JsonWriteString('identifier', Pad4.Identifier, True);
    JsonWriteString('descriptor', Pad4.Descriptor, True);
    JsonWriteString('detail', Pad4.Detail, True);
    JsonWriteString('uniqueId', Pad4.UniqueId, True);
        // Type-specific properties
    JsonWriteBoolean('drawAsPreview', Pad4.DrawAsPreview, True);
    JsonWriteInteger('daisyChainStyle', Pad4.DaisyChainStyle, True);
    JsonWriteBoolean('isBottomPasteEnabled', Pad4.IsBottomPasteEnabled, True);
    JsonWriteBoolean('isTopPasteEnabled', Pad4.IsTopPasteEnabled, True);
    JsonWriteBoolean('isCounterHole', Pad4.IsCounterHole, True);
    JsonWriteBoolean('isPadStack', Pad4.IsPadStack, True);
    JsonWriteBoolean('isSurfaceMount', Pad4.IsSurfaceMount, True);
    JsonWriteBoolean('isVirtualPin', Pad4.IsVirtualPin, True);
    JsonWriteBoolean('hasCornerRadiusChamfer', Pad4.HasCornerRadiusChamfer, True);
    JsonWriteBoolean('hasCustomChamferedRectangle', Pad4.HasCustomChamferedRectangle, True);
    JsonWriteBoolean('hasCustomDonut', Pad4.HasCustomDonut, True);
    JsonWriteBoolean('hasCustomMaskDonutShapes', Pad4.HasCustomMaskDonutShapes, True);
    JsonWriteBoolean('hasCustomMaskShapes', Pad4.HasCustomMaskShapes, True);
    JsonWriteBoolean('hasCustomRoundedRectangle', Pad4.HasCustomRoundedRectangle, True);
    JsonWriteBoolean('hasCustomShapes', Pad4.HasCustomShapes, True);
    JsonWriteBoolean('hasRoundedRectangularShapes', Pad4.HasRoundedRectangularShapes, True);
    JsonWriteInteger('multiLayerHighBits', Pad4.MultiLayerHighBits, True);
    JsonWriteBoolean('padHasOffsetOnAny', Pad4.PadHasOffsetOnAny, True);
    JsonWriteBoolean('solderMaskExpansionFromHoleEdgeWithRule', Pad4.SolderMaskExpansionFromHoleEdgeWithRule, True);
    JsonWriteInteger('getHoleXSize', Pad4.GetHoleXSize, True);
    JsonWriteInteger('getHoleYSize', Pad4.GetHoleYSize, True);
    JsonWriteInteger('getState_BottomLayer', Pad4.GetState_BottomLayer, True);
    JsonWriteInteger('getState_TopLayer', Pad4.GetState_TopLayer, True);
    JsonWriteString('getState_HoleString', Pad4.GetState_HoleString, True);
    JsonWriteString('getState_SwapID_Gate', Pad4.GetState_SwapID_Gate, True);
        // IPCB_Primitive base properties
    JsonWriteInteger('layer_V6', Pad4.Layer_V6, True);
    JsonWriteBoolean('inBoard', Pad4.InBoard, True);
    JsonWriteBoolean('inCoordinate', Pad4.InCoordinate, True);
    JsonWriteBoolean('inDimension', Pad4.InDimension, True);
    JsonWriteBoolean('inPolygon', Pad4.InPolygon, True);
    JsonWriteBoolean('moveable', Pad4.Moveable, True);
    JsonWriteBoolean('selected', Pad4.Selected, True);
    JsonWriteBoolean('used', Pad4.Used, True);
    JsonWriteBoolean('userRouted', Pad4.UserRouted, True);
    JsonWriteBoolean('enabled', Pad4.Enabled, True);
    JsonWriteBoolean('isPreRoute', Pad4.IsPreRoute, True);
    JsonWriteBoolean('polygonOutline', Pad4.PolygonOutline, True);
    JsonWriteBoolean('tearDrop', Pad4.TearDrop, True);
    JsonWriteBoolean('isElectricalPrim', Pad4.IsElectricalPrim, True);
    JsonWriteBoolean('allowGlobalEdit', Pad4.AllowGlobalEdit, True);
    JsonWriteBoolean('drawAsPreview', Pad4.DrawAsPreview, True);
    JsonWriteBoolean('enableDraw', Pad4.EnableDraw, True);
    JsonWriteInteger('index', Pad4.Index, True);
    JsonWriteInteger('unionIndex', Pad4.UnionIndex, True);
    JsonWriteString('handle', Pad4.Handle, True);
        // Additional readable properties
    JsonWriteBoolean('isFreePrimitive', Pad4.IsFreePrimitive, True);
    JsonWriteBoolean('isHidden', Pad4.IsHidden, True);
    JsonWriteBoolean('isHoleSizeValid', Pad4.IsHoleSizeValid, False);
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
    JsonWriteCoord('length', Track.GetState_Length, True);
    JsonWriteInteger('layer', Track.Layer, True);
    // Flags
    JsonWriteBoolean('isKeepout', Track.IsKeepout, True);
    JsonWriteBoolean('inComponent', Track.InComponent, True);
    JsonWriteBoolean('inNet', Track.InNet, True);
    JsonWriteBoolean('moveable', Track.Moveable, True);
    JsonWriteBoolean('drcError', Track.DRCError, True);
    JsonWriteBoolean('miscFlag1', Track.MiscFlag1, True);
    JsonWriteBoolean('miscFlag2', Track.MiscFlag2, True);
    JsonWriteBoolean('miscFlag3', Track.MiscFlag3, True);
    // Identifier strings
    JsonWriteString('objectIDString', Track.ObjectIDString, True);
    JsonWriteString('identifier', Track.Identifier, True);
    JsonWriteString('descriptor', Track.Descriptor, True);
    JsonWriteString('detail', Track.Detail, True);
        // IPCB_Primitive base properties
    JsonWriteInteger('layer_V6', Track.Layer_V6, True);
    JsonWriteCoord('pasteMaskExpansion', Track.PasteMaskExpansion, True);
    JsonWriteCoord('solderMaskExpansion', Track.SolderMaskExpansion, True);
    JsonWriteCoord('powerPlaneClearance', Track.PowerPlaneClearance, True);
    JsonWriteCoord('powerPlaneReliefExpansion', Track.PowerPlaneReliefExpansion, True);
    JsonWriteInteger('powerPlaneConnectStyle', Track.PowerPlaneConnectStyle, True);
    JsonWriteCoord('reliefAirGap', Track.ReliefAirGap, True);
    JsonWriteCoord('reliefConductorWidth', Track.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', Track.ReliefEntries, True);
    JsonWriteBoolean('inBoard', Track.InBoard, True);
    JsonWriteBoolean('inCoordinate', Track.InCoordinate, True);
    JsonWriteBoolean('inDimension', Track.InDimension, True);
    JsonWriteBoolean('inPolygon', Track.InPolygon, True);
    JsonWriteBoolean('selected', Track.Selected, True);
    JsonWriteBoolean('used', Track.Used, True);
    JsonWriteBoolean('userRouted', Track.UserRouted, True);
    JsonWriteBoolean('enabled', Track.Enabled, True);
    JsonWriteBoolean('isPreRoute', Track.IsPreRoute, True);
    JsonWriteBoolean('polygonOutline', Track.PolygonOutline, True);
    JsonWriteBoolean('tearDrop', Track.TearDrop, True);
    JsonWriteBoolean('isTestpoint_Bottom', Track.IsTestpoint_Bottom, True);
    JsonWriteBoolean('isTestpoint_Top', Track.IsTestpoint_Top, True);
    JsonWriteBoolean('isAssyTestpoint_Bottom', Track.IsAssyTestpoint_Bottom, True);
    JsonWriteBoolean('isAssyTestpoint_Top', Track.IsAssyTestpoint_Top, True);
    JsonWriteBoolean('isTenting', Track.IsTenting, True);
    JsonWriteBoolean('isTenting_Bottom', Track.IsTenting_Bottom, True);
    JsonWriteBoolean('isTenting_Top', Track.IsTenting_Top, True);
    JsonWriteBoolean('isElectricalPrim', Track.IsElectricalPrim, True);
    JsonWriteBoolean('allowGlobalEdit', Track.AllowGlobalEdit, True);
    JsonWriteBoolean('drawAsPreview', Track.DrawAsPreview, True);
    JsonWriteBoolean('enableDraw', Track.EnableDraw, True);
    JsonWriteInteger('index', Track.Index, True);
    JsonWriteInteger('unionIndex', Track.UnionIndex, True);
    JsonWriteString('uniqueId', Track.UniqueId, True);
    JsonWriteString('handle', Track.Handle, True);
        // Additional readable properties
    JsonWriteBoolean('isFreePrimitive', Track.IsFreePrimitive, True);
    JsonWriteBoolean('isHidden', Track.IsHidden, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbArcToJson(Arc: IPCB_Arc; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Arc', True);
    JsonWriteCoord('xCenter', Arc.XCenter, True);
    JsonWriteCoord('yCenter', Arc.YCenter, True);
    JsonWriteCoord('radius', Arc.Radius, True);
    JsonWriteCoord('lineWidth', Arc.LineWidth, True);
    JsonWriteFloat('startAngle', Arc.StartAngle, True);
    JsonWriteFloat('endAngle', Arc.EndAngle, True);
    // Computed endpoints
    JsonWriteCoord('startX', Arc.StartX, True);
    JsonWriteCoord('startY', Arc.StartY, True);
    JsonWriteCoord('endX', Arc.EndX, True);
    JsonWriteCoord('endY', Arc.EndY, True);
    JsonWriteInteger('layer', Arc.Layer, True);
    JsonWriteInteger('layer_V6', Arc.Layer_V6, True);
    // Mask/clearance expansions
    JsonWriteCoord('pasteMaskExpansion', Arc.PasteMaskExpansion, True);
    JsonWriteCoord('solderMaskExpansion', Arc.SolderMaskExpansion, True);
    JsonWriteCoord('powerPlaneClearance', Arc.PowerPlaneClearance, True);
    JsonWriteCoord('powerPlaneReliefExpansion', Arc.PowerPlaneReliefExpansion, True);
    JsonWriteInteger('powerPlaneConnectStyle', Arc.PowerPlaneConnectStyle, True);
    // Thermal relief
    JsonWriteCoord('reliefAirGap', Arc.ReliefAirGap, True);
    JsonWriteCoord('reliefConductorWidth', Arc.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', Arc.ReliefEntries, True);
    // State flags
    JsonWriteBoolean('isKeepout', Arc.IsKeepout, True);
    JsonWriteBoolean('inComponent', Arc.InComponent, True);
    JsonWriteBoolean('inNet', Arc.InNet, True);
    JsonWriteBoolean('inBoard', Arc.InBoard, True);
    JsonWriteBoolean('inCoordinate', Arc.InCoordinate, True);
    JsonWriteBoolean('inDimension', Arc.InDimension, True);
    JsonWriteBoolean('inPolygon', Arc.InPolygon, True);
    JsonWriteBoolean('moveable', Arc.Moveable, True);
    JsonWriteBoolean('selected', Arc.Selected, True);
    JsonWriteBoolean('used', Arc.Used, True);
    JsonWriteBoolean('userRouted', Arc.UserRouted, True);
    JsonWriteBoolean('drcError', Arc.DRCError, True);
    JsonWriteBoolean('enabled', Arc.Enabled, True);
    JsonWriteBoolean('isPreRoute', Arc.IsPreRoute, True);
    JsonWriteBoolean('polygonOutline', Arc.PolygonOutline, True);
    JsonWriteBoolean('tearDrop', Arc.TearDrop, True);
    // Test point flags
    JsonWriteBoolean('isTestpoint_Bottom', Arc.IsTestpoint_Bottom, True);
    JsonWriteBoolean('isTestpoint_Top', Arc.IsTestpoint_Top, True);
    JsonWriteBoolean('isAssyTestpoint_Bottom', Arc.IsAssyTestpoint_Bottom, True);
    JsonWriteBoolean('isAssyTestpoint_Top', Arc.IsAssyTestpoint_Top, True);
    JsonWriteBoolean('isTenting', Arc.IsTenting, True);
    JsonWriteBoolean('isTenting_Bottom', Arc.IsTenting_Bottom, True);
    JsonWriteBoolean('isTenting_Top', Arc.IsTenting_Top, True);
    JsonWriteBoolean('isElectricalPrim', Arc.IsElectricalPrim, True);
    // Misc flags
    JsonWriteBoolean('allowGlobalEdit', Arc.AllowGlobalEdit, True);
    JsonWriteBoolean('drawAsPreview', Arc.DrawAsPreview, True);
    JsonWriteBoolean('enableDraw', Arc.EnableDraw, True);
    JsonWriteBoolean('miscFlag1', Arc.MiscFlag1, True);
    JsonWriteBoolean('miscFlag2', Arc.MiscFlag2, True);
    JsonWriteBoolean('miscFlag3', Arc.MiscFlag3, True);
    // Index and identifier
    JsonWriteInteger('index', Arc.Index, True);
    JsonWriteInteger('unionIndex', Arc.UnionIndex, True);
    // Identifier strings
    JsonWriteString('uniqueId', Arc.UniqueId, True);
    JsonWriteString('handle', Arc.Handle, True);
    JsonWriteString('objectIDString', Arc.ObjectIDString, True);
    JsonWriteString('identifier', Arc.Identifier, True);
    JsonWriteString('descriptor', Arc.Descriptor, True);
    JsonWriteString('detail', Arc.Detail, True);
        // Additional readable properties
    JsonWriteBoolean('isFreePrimitive', Arc.IsFreePrimitive, True);
    JsonWriteBoolean('isHidden', Arc.IsHidden, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbFillToJson(Fill: IPCB_Fill; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Fill', True);
    JsonWriteCoord('xLocation', Fill.XLocation, True);
    JsonWriteCoord('yLocation', Fill.YLocation, True);
    JsonWriteCoord('x1', Fill.X1Location, True);
    JsonWriteCoord('y1', Fill.Y1Location, True);
    JsonWriteCoord('x2', Fill.X2Location, True);
    JsonWriteCoord('y2', Fill.Y2Location, True);
    JsonWriteCoord('width', Fill.GetState_Width, True);
    JsonWriteCoord('length', Fill.GetState_Length, True);
    JsonWriteFloat('rotation', Fill.Rotation, True);
    JsonWriteInteger('layer', Fill.Layer, True);
    JsonWriteInteger('layer_V6', Fill.Layer_V6, True);
    // Mask expansions
    JsonWriteCoord('pasteMaskExpansion', Fill.PasteMaskExpansion, True);
    JsonWriteCoord('solderMaskExpansion', Fill.SolderMaskExpansion, True);
    JsonWriteCoord('powerPlaneClearance', Fill.PowerPlaneClearance, True);
    JsonWriteCoord('powerPlaneReliefExpansion', Fill.PowerPlaneReliefExpansion, True);
    JsonWriteInteger('powerPlaneConnectStyle', Fill.PowerPlaneConnectStyle, True);
    // Thermal relief
    JsonWriteCoord('reliefAirGap', Fill.ReliefAirGap, True);
    JsonWriteCoord('reliefConductorWidth', Fill.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', Fill.ReliefEntries, True);
    // State flags
    JsonWriteBoolean('isKeepout', Fill.IsKeepout, True);
    JsonWriteBoolean('inComponent', Fill.InComponent, True);
    JsonWriteBoolean('inNet', Fill.InNet, True);
    JsonWriteBoolean('inBoard', Fill.InBoard, True);
    JsonWriteBoolean('inCoordinate', Fill.InCoordinate, True);
    JsonWriteBoolean('inDimension', Fill.InDimension, True);
    JsonWriteBoolean('inPolygon', Fill.InPolygon, True);
    JsonWriteBoolean('moveable', Fill.Moveable, True);
    JsonWriteBoolean('selected', Fill.Selected, True);
    JsonWriteBoolean('used', Fill.Used, True);
    JsonWriteBoolean('userRouted', Fill.UserRouted, True);
    JsonWriteBoolean('drcError', Fill.DRCError, True);
    JsonWriteBoolean('enabled', Fill.Enabled, True);
    JsonWriteBoolean('isPreRoute', Fill.IsPreRoute, True);
    JsonWriteBoolean('polygonOutline', Fill.PolygonOutline, True);
    JsonWriteBoolean('tearDrop', Fill.TearDrop, True);
    // Test point flags
    JsonWriteBoolean('isTestpoint_Bottom', Fill.IsTestpoint_Bottom, True);
    JsonWriteBoolean('isTestpoint_Top', Fill.IsTestpoint_Top, True);
    JsonWriteBoolean('isAssyTestpoint_Bottom', Fill.IsAssyTestpoint_Bottom, True);
    JsonWriteBoolean('isAssyTestpoint_Top', Fill.IsAssyTestpoint_Top, True);
    JsonWriteBoolean('isTenting', Fill.IsTenting, True);
    JsonWriteBoolean('isTenting_Bottom', Fill.IsTenting_Bottom, True);
    JsonWriteBoolean('isTenting_Top', Fill.IsTenting_Top, True);
    JsonWriteBoolean('isElectricalPrim', Fill.IsElectricalPrim, True);
    // Misc flags
    JsonWriteBoolean('allowGlobalEdit', Fill.AllowGlobalEdit, True);
    JsonWriteBoolean('drawAsPreview', Fill.DrawAsPreview, True);
    JsonWriteBoolean('enableDraw', Fill.EnableDraw, True);
    JsonWriteBoolean('miscFlag1', Fill.MiscFlag1, True);
    JsonWriteBoolean('miscFlag2', Fill.MiscFlag2, True);
    JsonWriteBoolean('miscFlag3', Fill.MiscFlag3, True);
    // Index and identifier
    JsonWriteInteger('index', Fill.Index, True);
    JsonWriteInteger('unionIndex', Fill.UnionIndex, True);
    // Identifier strings
    JsonWriteString('uniqueId', Fill.UniqueId, True);
    JsonWriteString('handle', Fill.Handle, True);
    JsonWriteString('objectIDString', Fill.ObjectIDString, True);
    JsonWriteString('identifier', Fill.Identifier, True);
    JsonWriteString('descriptor', Fill.Descriptor, True);
    JsonWriteString('detail', Fill.Detail, True);
        // Additional readable properties
    JsonWriteBoolean('isFreePrimitive', Fill.IsFreePrimitive, True);
    JsonWriteBoolean('isHidden', Fill.IsHidden, True);
    JsonWriteBoolean('isRedundant', Fill.IsRedundant, True);
    JsonWriteCoord('getState_LocationX', Fill.GetState_LocationX, True);
    JsonWriteCoord('getState_LocationY', Fill.GetState_LocationY, False);
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
    JsonWriteCoord('width', Text.Width, True);
    JsonWriteFloat('rotation', Text.Rotation, True);
    JsonWriteBoolean('mirrorFlag', Text.MirrorFlag, True);
    JsonWriteInteger('layer', Text.Layer, True);
    // Font properties
    JsonWriteBoolean('useTTFonts', Text.UseTTFonts, True);
    JsonWriteString('fontName', Text.FontName, True);
    JsonWriteInteger('fontID', Text.FontID, True);
    JsonWriteBoolean('bold', Text.Bold, True);
    JsonWriteBoolean('italic', Text.Italic, True);
    JsonWriteInteger('textKind', Text.TextKind, True);
    // Text display properties
    JsonWriteBoolean('inverted', Text.Inverted, True);
    JsonWriteBoolean('useInvertedRectangle', Text.UseInvertedRectangle, True);
    JsonWriteBoolean('wordWrap', Text.WordWrap, True);
    JsonWriteBoolean('multiLine', Text.Multiline, True);
    JsonWriteInteger('multilineTextAutoPosition', Ord(Text.MultilineTextAutoPosition), True);
    JsonWriteCoord('multilineTextHeight', Text.MultilineTextHeight, True);
    JsonWriteCoord('multilineTextWidth', Text.MultilineTextWidth, True);
    JsonWriteBoolean('multilineTextResizeEnabled', Text.MultilineTextResizeEnabled, True);
    // TTF text properties
    JsonWriteCoord('ttfTextWidth', Text.TTFTextWidth, True);
    JsonWriteCoord('ttfTextHeight', Text.TTFTextHeight, True);
    JsonWriteInteger('ttfInvertedTextJustify', Ord(Text.TTFInvertedTextJustify), True);
    // Snapping
    JsonWriteBoolean('advanceSnapping', Text.AdvanceSnapping, True);
    JsonWriteCoord('snapPointX', Text.SnapPointX, True);
    JsonWriteCoord('snapPointY', Text.SnapPointY, True);
    // Flags
    JsonWriteBoolean('moveable', Text.Moveable, True);
    JsonWriteBoolean('allowGlobalEdit', Text.AllowGlobalEdit, True);
    JsonWriteBoolean('enableDraw', Text.EnableDraw, True);
    JsonWriteBoolean('padCacheRobotFlag', Text.PadCacheRobotFlag, True);
    JsonWriteBoolean('miscFlag1', Text.MiscFlag1, True);
    JsonWriteBoolean('miscFlag2', Text.MiscFlag2, True);
    JsonWriteBoolean('miscFlag3', Text.MiscFlag3, True);
    // Border properties
    JsonWriteInteger('borderSpaceType', Text.BorderSpaceType, True);
    // Identifier strings
    JsonWriteString('objectIDString', Text.ObjectIDString, True);
    JsonWriteString('identifier', Text.Identifier, True);
    JsonWriteString('descriptor', Text.Descriptor, True);
    JsonWriteString('detail', Text.Detail, True);
    // Barcode properties (only meaningful when TextKind = eText_Barcode)
    JsonWriteInteger('barCodeKind', Text.BarCodeKind, True);
    JsonWriteInteger('barCodeRenderMode', Text.BarCodeRenderMode, True);
    JsonWriteCoord('barCodeFullWidth', Text.BarCodeFullWidth, True);
    JsonWriteCoord('barCodeFullHeight', Text.BarCodeFullHeight, True);
    JsonWriteCoord('barCodeXMargin', Text.BarCodeXMargin, True);
    JsonWriteCoord('barCodeYMargin', Text.BarCodeYMargin, True);
    JsonWriteCoord('barCodeMinWidth', Text.BarCodeMinWidth, True);
    JsonWriteBoolean('barCodeShowText', Text.BarCodeShowText, True);
    JsonWriteBoolean('barCodeInverted', Text.BarCodeInverted, True);
    JsonWriteString('barCodeFontName', Text.BarCodeFontName, True);
        // Type-specific properties
    JsonWriteString('barCodeBitPattern', Text.BarCodeBitPattern, True);
    JsonWriteInteger('charSet', Text.CharSet, True);
    JsonWriteString('convertedString', Text.ConvertedString, True);
    JsonWriteBoolean('disableSpecialStringConversion', Text.DisableSpecialStringConversion, True);
    JsonWriteString('getDesignatorDisplayString', Text.GetDesignatorDisplayString, True);
    JsonWriteBoolean('inAutoDimension', Text.InAutoDimension, True);
    JsonWriteCoord('invertedTTTextBorder', Text.InvertedTTTextBorder, True);
    JsonWriteInteger('invRectHeight', Text.InvRectHeight, True);
    JsonWriteInteger('invRectWidth', Text.InvRectWidth, True);
    JsonWriteBoolean('isComment', Text.IsComment, True);
    JsonWriteBoolean('isDesignator', Text.IsDesignator, True);
    JsonWriteBoolean('isRedundant', Text.IsRedundant, True);
    JsonWriteCoord('ttfOffsetFromInvertedRect', Text.TTFOffsetFromInvertedRect, True);
    JsonWriteString('underlyingString', Text.UnderlyingString, True);
    JsonWriteCoord('x1Location', Text.X1Location, True);
    JsonWriteCoord('y1Location', Text.Y1Location, True);
    JsonWriteCoord('x2Location', Text.X2Location, True);
    JsonWriteCoord('y2Location', Text.Y2Location, True);
        // IPCB_Primitive base properties
    JsonWriteInteger('layer_V6', Text.Layer_V6, True);
    JsonWriteCoord('pasteMaskExpansion', Text.PasteMaskExpansion, True);
    JsonWriteCoord('solderMaskExpansion', Text.SolderMaskExpansion, True);
    JsonWriteCoord('powerPlaneClearance', Text.PowerPlaneClearance, True);
    JsonWriteCoord('powerPlaneReliefExpansion', Text.PowerPlaneReliefExpansion, True);
    JsonWriteInteger('powerPlaneConnectStyle', Text.PowerPlaneConnectStyle, True);
    JsonWriteCoord('reliefAirGap', Text.ReliefAirGap, True);
    JsonWriteCoord('reliefConductorWidth', Text.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', Text.ReliefEntries, True);
    JsonWriteBoolean('isKeepout', Text.IsKeepout, True);
    JsonWriteBoolean('inComponent', Text.InComponent, True);
    JsonWriteBoolean('inNet', Text.InNet, True);
    JsonWriteBoolean('inBoard', Text.InBoard, True);
    JsonWriteBoolean('inCoordinate', Text.InCoordinate, True);
    JsonWriteBoolean('inDimension', Text.InDimension, True);
    JsonWriteBoolean('inPolygon', Text.InPolygon, True);
    JsonWriteBoolean('selected', Text.Selected, True);
    JsonWriteBoolean('used', Text.Used, True);
    JsonWriteBoolean('userRouted', Text.UserRouted, True);
    JsonWriteBoolean('drcError', Text.DRCError, True);
    JsonWriteBoolean('enabled', Text.Enabled, True);
    JsonWriteBoolean('isPreRoute', Text.IsPreRoute, True);
    JsonWriteBoolean('polygonOutline', Text.PolygonOutline, True);
    JsonWriteBoolean('tearDrop', Text.TearDrop, True);
    JsonWriteBoolean('isTestpoint_Bottom', Text.IsTestpoint_Bottom, True);
    JsonWriteBoolean('isTestpoint_Top', Text.IsTestpoint_Top, True);
    JsonWriteBoolean('isAssyTestpoint_Bottom', Text.IsAssyTestpoint_Bottom, True);
    JsonWriteBoolean('isAssyTestpoint_Top', Text.IsAssyTestpoint_Top, True);
    JsonWriteBoolean('isTenting', Text.IsTenting, True);
    JsonWriteBoolean('isTenting_Bottom', Text.IsTenting_Bottom, True);
    JsonWriteBoolean('isTenting_Top', Text.IsTenting_Top, True);
    JsonWriteBoolean('isElectricalPrim', Text.IsElectricalPrim, True);
    JsonWriteBoolean('drawAsPreview', Text.DrawAsPreview, True);
    JsonWriteInteger('index', Text.Index, True);
    JsonWriteInteger('unionIndex', Text.UnionIndex, True);
    JsonWriteString('uniqueId', Text.UniqueId, True);
    JsonWriteString('handle', Text.Handle, True);
        // Additional readable properties
    JsonWriteBoolean('isFreePrimitive', Text.IsFreePrimitive, True);
    JsonWriteBoolean('isHidden', Text.IsHidden, True);
    JsonWriteBoolean('canEditMultilineRectSize', Text.CanEditMultilineRectSize, True);
    JsonWriteInteger('getState_BarCodeMinPixelSize', Text.GetState_BarCodeMinPixelSize, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbRegionToJson(Region: IPCB_Region; AddComma: Boolean);
var
    I: Integer;
    Contour: IPCB_Contour;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Region', True);
    JsonWriteInteger('layer', Region.Layer, True);
    JsonWriteInteger('layer_V6', Region.Layer_V6, True);
    JsonWriteInteger('kind', Ord(Region.Kind), True);
    JsonWriteString('name', Region.Name, True);
    JsonWriteInteger('holeCount', Region.HoleCount, True);
    JsonWriteInteger('area', Region.Area, True);
    JsonWriteCoord('cavityHeight', Region.CavityHeight, True);
    // Mask expansions
    JsonWriteCoord('pasteMaskExpansion', Region.PasteMaskExpansion, True);
    JsonWriteCoord('solderMaskExpansion', Region.SolderMaskExpansion, True);
    JsonWriteCoord('powerPlaneClearance', Region.PowerPlaneClearance, True);
    JsonWriteCoord('powerPlaneReliefExpansion', Region.PowerPlaneReliefExpansion, True);
    JsonWriteInteger('powerPlaneConnectStyle', Region.PowerPlaneConnectStyle, True);
    // Thermal relief
    JsonWriteCoord('reliefAirGap', Region.ReliefAirGap, True);
    JsonWriteCoord('reliefConductorWidth', Region.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', Region.ReliefEntries, True);
    // State flags
    JsonWriteBoolean('isKeepout', Region.IsKeepout, True);
    JsonWriteBoolean('tearDrop', Region.TearDrop, True);
    JsonWriteBoolean('polygonOutline', Region.PolygonOutline, True);
    JsonWriteBoolean('inPolygon', Region.InPolygon, True);
    JsonWriteBoolean('inComponent', Region.InComponent, True);
    JsonWriteBoolean('inNet', Region.InNet, True);
    JsonWriteBoolean('inBoard', Region.InBoard, True);
    JsonWriteBoolean('inCoordinate', Region.InCoordinate, True);
    JsonWriteBoolean('inDimension', Region.InDimension, True);
    JsonWriteBoolean('isElectricalPrim', Region.IsElectricalPrim, True);
    JsonWriteBoolean('moveable', Region.Moveable, True);
    JsonWriteBoolean('selected', Region.Selected, True);
    JsonWriteBoolean('used', Region.Used, True);
    JsonWriteBoolean('userRouted', Region.UserRouted, True);
    JsonWriteBoolean('drcError', Region.DRCError, True);
    JsonWriteBoolean('enabled', Region.Enabled, True);
    JsonWriteBoolean('isPreRoute', Region.IsPreRoute, True);
    // Test point and tenting flags
    JsonWriteBoolean('isTestpoint_Bottom', Region.IsTestpoint_Bottom, True);
    JsonWriteBoolean('isTestpoint_Top', Region.IsTestpoint_Top, True);
    JsonWriteBoolean('isAssyTestpoint_Bottom', Region.IsAssyTestpoint_Bottom, True);
    JsonWriteBoolean('isAssyTestpoint_Top', Region.IsAssyTestpoint_Top, True);
    JsonWriteBoolean('isTenting', Region.IsTenting, True);
    JsonWriteBoolean('isTenting_Bottom', Region.IsTenting_Bottom, True);
    JsonWriteBoolean('isTenting_Top', Region.IsTenting_Top, True);
    // Misc flags
    JsonWriteBoolean('allowGlobalEdit', Region.AllowGlobalEdit, True);
    JsonWriteBoolean('drawAsPreview', Region.DrawAsPreview, True);
    JsonWriteBoolean('enableDraw', Region.EnableDraw, True);
    JsonWriteBoolean('miscFlag1', Region.MiscFlag1, True);
    JsonWriteBoolean('miscFlag2', Region.MiscFlag2, True);
    JsonWriteBoolean('miscFlag3', Region.MiscFlag3, True);
    // Index
    JsonWriteInteger('index', Region.Index, True);
    JsonWriteInteger('unionIndex', Region.UnionIndex, True);
    // Identifier strings
    JsonWriteString('handle', Region.Handle, True);
    JsonWriteString('objectIDString', Region.ObjectIDString, True);
    JsonWriteString('identifier', Region.Identifier, True);
    JsonWriteString('descriptor', Region.Descriptor, True);
    JsonWriteString('detail', Region.Detail, True);
    JsonWriteString('uniqueId', Region.UniqueId, True);

    Contour := Region.MainContour;
    if Contour <> nil then
    begin
        JsonOpenArray('contourPoints');
        for I := 0 to Contour.Count - 1 do
        begin
            JsonOpenObject('');
            JsonWriteCoord('x', Contour.X[I], True);
            JsonWriteCoord('y', Contour.Y[I], True);
            JsonCloseObject(I < Contour.Count - 1);
        end;
        JsonCloseArray(False);
    end;

        // Type-specific properties
    JsonWriteBoolean('isSimpleRegion', Region.IsSimpleRegion, True);
        // Additional readable properties
    JsonWriteBoolean('isFreePrimitive', Region.IsFreePrimitive, True);
    JsonWriteBoolean('isHidden', Region.IsHidden, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbPolygonToJson(Polygon: IPCB_Polygon; AddComma: Boolean);
var
    I: Integer;
    Contour: IPCB_Contour;
    Segment: TPolySegment;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Polygon', True);
    JsonWriteString('name', Polygon.Name, True);
    JsonWriteInteger('layer', Polygon.Layer, True);
    JsonWriteCoord('x', Polygon.X, True);
    JsonWriteCoord('y', Polygon.Y, True);
    // Pour properties
    JsonWriteInteger('polyHatchStyle', Ord(Polygon.PolyHatchStyle), True);
    JsonWriteInteger('polygonType', Ord(Polygon.PolygonType), True);
    JsonWriteInteger('pourOver', Ord(Polygon.PourOver), True);
    JsonWriteBoolean('pourInvalid', Polygon.GetState_CopperPourInvalid, True);
    JsonWriteBoolean('poured', Polygon.Poured, True);
    JsonWriteInteger('pourIndex', Polygon.PourIndex, True);
    // Grid and track sizes
    JsonWriteCoord('grid', Polygon.Grid, True);
    JsonWriteCoord('trackSize', Polygon.TrackSize, True);
    JsonWriteCoord('minTrack', Polygon.MinTrack, True);
    JsonWriteCoord('borderWidth', Polygon.BorderWidth, True);
    JsonWriteCoord('arcApproximation', Polygon.ArcApproximation, True);
    // Island and neck removal
    JsonWriteBoolean('removeIslandsByArea', Polygon.RemoveIslandsByArea, True);
    JsonWriteFloat('islandAreaThreshold', Polygon.IslandAreaThreshold, True);
    JsonWriteBoolean('removeNarrowNecks', Polygon.RemoveNarrowNecks, True);
    JsonWriteCoord('neckWidthThreshold', Polygon.NeckWidthThreshold, True);
    // Other options
    JsonWriteBoolean('removeDead', Polygon.RemoveDead, True);
    JsonWriteBoolean('useOctagons', Polygon.UseOctagons, True);
    JsonWriteBoolean('avoidObsticles', Polygon.AvoidObsticles, True);
    JsonWriteBoolean('expandOutline', Polygon.ExpandOutline, True);
    JsonWriteBoolean('clipAcuteCorners', Polygon.ClipAcuteCorners, True);
    JsonWriteBoolean('mitreCorners', Polygon.MitreCorners, True);
    JsonWriteBoolean('arcPourMode', Polygon.ArcPourMode, True);
    // Display options
    JsonWriteBoolean('drawRemovedNecks', Polygon.DrawRemovedNecks, True);
    JsonWriteBoolean('drawRemovedIslands', Polygon.DrawRemovedIslands, True);
    JsonWriteBoolean('drawDeadCopper', Polygon.DrawDeadCopper, True);
    // Area
    JsonWriteFloat('areaSize', Polygon.AreaSize, True);
    JsonWriteInteger('pointCount', Polygon.PointCount, True);
    // Flags
    JsonWriteBoolean('autoGenerateName', Polygon.AutoGenerateName, True);
    JsonWriteBoolean('ignoreViolations', Polygon.IgnoreViolations, True);
    JsonWriteBoolean('primitiveLock', Polygon.PrimitiveLock, True);
    JsonWriteBoolean('polygonOutline', Polygon.PolygonOutline, True);
    JsonWriteBoolean('inNet', Polygon.InNet, True);
    JsonWriteBoolean('moveable', Polygon.Moveable, True);
    JsonWriteBoolean('drcError', Polygon.DRCError, True);
    JsonWriteBoolean('miscFlag1', Polygon.MiscFlag1, True);
    JsonWriteBoolean('miscFlag2', Polygon.MiscFlag2, True);
    JsonWriteBoolean('miscFlag3', Polygon.MiscFlag3, True);
    // Identifier strings
    JsonWriteString('objectIDString', Polygon.ObjectIDString, True);
    JsonWriteString('identifier', Polygon.Identifier, True);
    JsonWriteString('descriptor', Polygon.Descriptor, True);
    JsonWriteString('detail', Polygon.Detail, True);

    JsonWriteString('uniqueId', Polygon.UniqueId, True);

    // Export segments (the polygon definition vertices)
    if Polygon.PointCount > 0 then
    begin
        JsonOpenArray('segments');
        for I := 0 to Polygon.PointCount - 1 do
        begin
            Segment := Polygon.Segments[I];
            JsonOpenObject('');
            JsonWriteInteger('kind', Segment.Kind, True);
            JsonWriteCoord('vx', Segment.vx, True);
            JsonWriteCoord('vy', Segment.vy, True);
            JsonWriteCoord('cx', Segment.cx, True);
            JsonWriteCoord('cy', Segment.cy, False);
            JsonCloseObject(I < Polygon.PointCount - 1);
        end;
        JsonCloseArray(True);
    end;

        // IPCB_Primitive base properties
    JsonWriteInteger('layer_V6', Polygon.Layer_V6, True);
    JsonWriteCoord('pasteMaskExpansion', Polygon.PasteMaskExpansion, True);
    JsonWriteCoord('solderMaskExpansion', Polygon.SolderMaskExpansion, True);
    JsonWriteCoord('powerPlaneClearance', Polygon.PowerPlaneClearance, True);
    JsonWriteCoord('powerPlaneReliefExpansion', Polygon.PowerPlaneReliefExpansion, True);
    JsonWriteInteger('powerPlaneConnectStyle', Polygon.PowerPlaneConnectStyle, True);
    JsonWriteCoord('reliefAirGap', Polygon.ReliefAirGap, True);
    JsonWriteCoord('reliefConductorWidth', Polygon.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', Polygon.ReliefEntries, True);
    JsonWriteBoolean('isKeepout', Polygon.IsKeepout, True);
    JsonWriteBoolean('inCoordinate', Polygon.InCoordinate, True);
    JsonWriteBoolean('inDimension', Polygon.InDimension, True);
    JsonWriteBoolean('inPolygon', Polygon.InPolygon, True);
    JsonWriteBoolean('selected', Polygon.Selected, True);
    JsonWriteBoolean('used', Polygon.Used, True);
    JsonWriteBoolean('userRouted', Polygon.UserRouted, True);
    JsonWriteBoolean('enabled', Polygon.Enabled, True);
    JsonWriteBoolean('isPreRoute', Polygon.IsPreRoute, True);
    JsonWriteBoolean('tearDrop', Polygon.TearDrop, True);
    JsonWriteBoolean('isTestpoint_Bottom', Polygon.IsTestpoint_Bottom, True);
    JsonWriteBoolean('isTestpoint_Top', Polygon.IsTestpoint_Top, True);
    JsonWriteBoolean('isAssyTestpoint_Bottom', Polygon.IsAssyTestpoint_Bottom, True);
    JsonWriteBoolean('isAssyTestpoint_Top', Polygon.IsAssyTestpoint_Top, True);
    JsonWriteBoolean('isTenting', Polygon.IsTenting, True);
    JsonWriteBoolean('isTenting_Bottom', Polygon.IsTenting_Bottom, True);
    JsonWriteBoolean('isTenting_Top', Polygon.IsTenting_Top, True);
    JsonWriteBoolean('isElectricalPrim', Polygon.IsElectricalPrim, True);
    JsonWriteBoolean('allowGlobalEdit', Polygon.AllowGlobalEdit, True);
    JsonWriteBoolean('drawAsPreview', Polygon.DrawAsPreview, True);
    JsonWriteInteger('index', Polygon.Index, True);
    JsonWriteInteger('unionIndex', Polygon.UnionIndex, True);
    JsonWriteString('handle', Polygon.Handle, True);
    // Type-specific properties
    JsonWriteString('getDefaultName', Polygon.GetDefaultName, True);
    JsonWriteBoolean('getState_CopperPourInvalid', Polygon.GetState_CopperPourInvalid, True);
    JsonWriteBoolean('copperPourValidate', Polygon.CopperPourValidate, True);
        // IPCB_Primitive base properties
    JsonWriteBoolean('inComponent', Polygon.InComponent, True);
    JsonWriteBoolean('inBoard', Polygon.InBoard, True);
    JsonWriteBoolean('enableDraw', Polygon.EnableDraw, True);
        // Additional readable properties
    JsonWriteBoolean('isFreePrimitive', Polygon.IsFreePrimitive, True);
    JsonWriteBoolean('isHidden', Polygon.IsHidden, True);
    JsonWriteBoolean('avoidObstacles', Polygon.AvoidObsticles, True);
    JsonWriteBoolean('obeyPolygonCutout', Polygon.ObeyPolygonCutout, True);
    JsonWriteBoolean('optimalVoidRotation', Polygon.OptimalVoidRotation, True);
    JsonWriteBoolean('drawRemovedNecks', Polygon.DrawRemovedNecks, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbComponentBodyToJson(Body: IPCB_ComponentBody; AddComma: Boolean);
var
    I: Integer;
    Contour: IPCB_Contour;
    VT: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ComponentBody', True);
    JsonWriteInteger('layer', Body.Layer, True);
    JsonWriteCoord('standoffHeight', Body.StandoffHeight, True);
    JsonWriteCoord('overallHeight', Body.OverallHeight, True);
    JsonWriteInteger('bodyProjection', Body.BodyProjection, True);
    // 3D properties - check VarType before accessing as some may not exist for STEP models
    VT := VarType(Body.BodyColor3D);
    if (VT = varInteger) or (VT = varSmallInt) or (VT = varByte) then
        JsonWriteInteger('bodyColor3D', Body.BodyColor3D, True)
    else
        JsonWriteInteger('bodyColor3D', 0, True);
    VT := VarType(Body.BodyOpacity3D);
    if (VT = varInteger) or (VT = varSmallInt) or (VT = varByte) then
        JsonWriteInteger('bodyOpacity3D', Body.BodyOpacity3D, True)
    else
        JsonWriteInteger('bodyOpacity3D', 0, True);
    VT := VarType(Body.OverrideColor);
    if (VT = varBoolean) then
        JsonWriteBoolean('overrideColor', Body.OverrideColor, True)
    else
        JsonWriteBoolean('overrideColor', False, True);
    // Texture properties
    VT := VarType(Body.Texture);
    if (VT = varOleStr) or (VT = varString) then
        JsonWriteString('texture', Body.Texture, True)
    else
        JsonWriteString('texture', '', True);
    VT := VarType(Body.TextureCenter);
    if (VT = varInteger) or (VT = varSmallInt) or (VT = varByte) then
        JsonWriteInteger('textureCenter', Body.TextureCenter, True)
    else
        JsonWriteInteger('textureCenter', 0, True);
    VT := VarType(Body.TextureSize);
    if (VT = varInteger) or (VT = varSmallInt) or (VT = varByte) then
        JsonWriteCoord('textureSize', Body.TextureSize, True)
    else
        JsonWriteCoord('textureSize', 0, True);
    VT := VarType(Body.TextureRotation);
    if (VT = varDouble) or (VT = varSingle) then
        JsonWriteFloat('textureRotation', Body.TextureRotation, True)
    else
        JsonWriteFloat('textureRotation', 0.0, True);
    // Region properties (ComponentBody extends Region)
    JsonWriteInteger('kind', Body.Kind, True);
    VT := VarType(Body.Name);
    if (VT = varOleStr) or (VT = varString) then
        JsonWriteString('name', Body.Name, True)
    else
        JsonWriteString('name', '', True);
    VT := VarType(Body.HoleCount);
    if (VT = varInteger) or (VT = varSmallInt) or (VT = varByte) then
        JsonWriteInteger('holeCount', Body.HoleCount, True)
    else
        JsonWriteInteger('holeCount', 0, True);
    VT := VarType(Body.Area);
    if (VT = varInteger) or (VT = varSmallInt) or (VT = varByte) then
        JsonWriteInteger('area', Body.Area, True)
    else
        JsonWriteInteger('area', 0, True);
    VT := VarType(Body.CavityHeight);
    if (VT = varInteger) or (VT = varSmallInt) or (VT = varByte) then
        JsonWriteCoord('cavityHeight', Body.CavityHeight, True)
    else
        JsonWriteCoord('cavityHeight', 0, True);
    // Flags
    VT := VarType(Body.ModelHasChanged);
    if (VT = varBoolean) then
        JsonWriteBoolean('modelHasChanged', Body.ModelHasChanged, True)
    else
        JsonWriteBoolean('modelHasChanged', False, True);
    JsonWriteBoolean('enableDraw', Body.EnableDraw, True);
    JsonWriteBoolean('moveable', Body.Moveable, True);
    JsonWriteBoolean('inBoard', Body.InBoard, True);
    JsonWriteBoolean('inComponent', Body.InComponent, True);
    VT := VarType(Body.DRCError);
    if (VT = varBoolean) then
        JsonWriteBoolean('drcError', Body.DRCError, True)
    else
        JsonWriteBoolean('drcError', False, True);
    VT := VarType(Body.MiscFlag1);
    if (VT = varBoolean) then
        JsonWriteBoolean('miscFlag1', Body.MiscFlag1, True)
    else
        JsonWriteBoolean('miscFlag1', False, True);
    VT := VarType(Body.MiscFlag2);
    if (VT = varBoolean) then
        JsonWriteBoolean('miscFlag2', Body.MiscFlag2, True)
    else
        JsonWriteBoolean('miscFlag2', False, True);
    VT := VarType(Body.MiscFlag3);
    if (VT = varBoolean) then
        JsonWriteBoolean('miscFlag3', Body.MiscFlag3, True)
    else
        JsonWriteBoolean('miscFlag3', False, True);
    // Identifier strings
    JsonWriteString('objectIDString', Body.ObjectIDString, True);
    JsonWriteString('identifier', Body.Identifier, True);
    JsonWriteString('descriptor', Body.Descriptor, True);
    VT := VarType(Body.Detail);
    if (VT = varOleStr) or (VT = varString) then
        JsonWriteString('detail', Body.Detail, True)
    else
        JsonWriteString('detail', '', True);
    JsonWriteString('uniqueId', Body.UniqueId, True);

    Contour := Body.MainContour;
    if Contour <> nil then
    begin
        JsonOpenArray('contourPoints');
        for I := 0 to Contour.Count - 1 do
        begin
            JsonOpenObject('');
            JsonWriteCoord('x', Contour.X[I], True);
            JsonWriteCoord('y', Contour.Y[I], True);
            JsonCloseObject(I < Contour.Count - 1);
        end;
        JsonCloseArray(False);
    end;

        // IPCB_Primitive base properties
    JsonWriteInteger('layer_V6', Body.Layer_V6, True);
    JsonWriteCoord('pasteMaskExpansion', Body.PasteMaskExpansion, True);
    JsonWriteCoord('solderMaskExpansion', Body.SolderMaskExpansion, True);
    JsonWriteCoord('powerPlaneClearance', Body.PowerPlaneClearance, True);
    JsonWriteCoord('powerPlaneReliefExpansion', Body.PowerPlaneReliefExpansion, True);
    JsonWriteInteger('powerPlaneConnectStyle', Body.PowerPlaneConnectStyle, True);
    JsonWriteCoord('reliefAirGap', Body.ReliefAirGap, True);
    JsonWriteCoord('reliefConductorWidth', Body.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', Body.ReliefEntries, True);
    JsonWriteBoolean('isKeepout', Body.IsKeepout, True);
    JsonWriteBoolean('inNet', Body.InNet, True);
    JsonWriteBoolean('inCoordinate', Body.InCoordinate, True);
    JsonWriteBoolean('inDimension', Body.InDimension, True);
    JsonWriteBoolean('inPolygon', Body.InPolygon, True);
    JsonWriteBoolean('selected', Body.Selected, True);
    JsonWriteBoolean('used', Body.Used, True);
    JsonWriteBoolean('userRouted', Body.UserRouted, True);
    JsonWriteBoolean('enabled', Body.Enabled, True);
    JsonWriteBoolean('isPreRoute', Body.IsPreRoute, True);
    JsonWriteBoolean('polygonOutline', Body.PolygonOutline, True);
    JsonWriteBoolean('tearDrop', Body.TearDrop, True);
    JsonWriteBoolean('isTestpoint_Bottom', Body.IsTestpoint_Bottom, True);
    JsonWriteBoolean('isTestpoint_Top', Body.IsTestpoint_Top, True);
    JsonWriteBoolean('isAssyTestpoint_Bottom', Body.IsAssyTestpoint_Bottom, True);
    JsonWriteBoolean('isAssyTestpoint_Top', Body.IsAssyTestpoint_Top, True);
    JsonWriteBoolean('isTenting', Body.IsTenting, True);
    JsonWriteBoolean('isTenting_Bottom', Body.IsTenting_Bottom, True);
    JsonWriteBoolean('isTenting_Top', Body.IsTenting_Top, True);
    JsonWriteBoolean('isElectricalPrim', Body.IsElectricalPrim, True);
    JsonWriteBoolean('allowGlobalEdit', Body.AllowGlobalEdit, True);
    JsonWriteBoolean('drawAsPreview', Body.DrawAsPreview, True);
    JsonWriteInteger('index', Body.Index, True);
    JsonWriteInteger('unionIndex', Body.UnionIndex, True);
    JsonWriteString('handle', Body.Handle, True);
    // Type-specific properties
    JsonWriteBoolean('isSimpleRegion', Body.IsSimpleRegion, True);
    JsonWriteInteger('getState_ModelType', Body.GetState_ModelType, True);
    JsonWriteInteger('axisCount', Body.AxisCount, True);
        // Additional readable properties
    JsonWriteBoolean('isFreePrimitive', Body.IsFreePrimitive, True);
    JsonWriteBoolean('isHidden', Body.IsHidden, True);
    JsonWriteInteger('getState_SnapCount', Body.GetState_SnapCount, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbDimensionToJson(Dimension: IPCB_Dimension; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Dimension', True);
    JsonWriteInteger('layer', Dimension.Layer, True);
    JsonWriteInteger('dimensionKind', Ord(Dimension.DimensionKind), True);
    JsonWriteCoord('x', Dimension.X, True);
    JsonWriteCoord('y', Dimension.Y, True);
    // Location points
    JsonWriteCoord('x1Location', Dimension.X1Location, True);
    JsonWriteCoord('y1Location', Dimension.Y1Location, True);
    // Text properties
    JsonWriteCoord('textX', Dimension.TextX, True);
    JsonWriteCoord('textY', Dimension.TextY, True);
    JsonWriteCoord('textHeight', Dimension.TextHeight, True);
    JsonWriteCoord('textWidth', Dimension.TextWidth, True);
    JsonWriteCoord('textLineWidth', Dimension.TextLineWidth, True);
    JsonWriteCoord('textGap', Dimension.TextGap, True);
    JsonWriteInteger('textPosition', Ord(Dimension.TextPosition), True);
    JsonWriteString('textFormat', Dimension.TextFormat, True);
    JsonWriteInteger('textDimensionUnit', Ord(Dimension.TextDimensionUnit), True);
    JsonWriteInteger('textPrecision', Dimension.TextPrecision, True);
    JsonWriteString('textPrefix', Dimension.TextPrefix, True);
    JsonWriteString('textSuffix', Dimension.TextSuffix, True);
    JsonWriteFloat('textValue', Dimension.TextValue, True);
    // Line properties
    JsonWriteCoord('size', Dimension.Size, True);
    JsonWriteCoord('lineWidth', Dimension.LineWidth, True);
    // Arrow properties
    JsonWriteCoord('arrowSize', Dimension.ArrowSize, True);
    JsonWriteCoord('arrowLineWidth', Dimension.ArrowLineWidth, True);
    JsonWriteCoord('arrowLength', Dimension.ArrowLength, True);
    JsonWriteInteger('arrowPosition', Ord(Dimension.ArrowPosition), True);
    // Extension line properties
    JsonWriteCoord('extensionOffset', Dimension.ExtensionOffset, True);
    JsonWriteCoord('extensionLineWidth', Dimension.ExtensionLineWidth, True);
    JsonWriteCoord('extensionPickGap', Dimension.ExtensionPickGap, True);
    // Font properties
    JsonWriteBoolean('useTTFonts', Dimension.UseTTFonts, True);
    JsonWriteString('fontName', Dimension.FontName, True);
    JsonWriteBoolean('bold', Dimension.Bold, True);
    JsonWriteBoolean('italic', Dimension.Italic, True);
    // Other properties
    JsonWriteInteger('style', Ord(Dimension.Style), True);
    JsonWriteInteger('referencesCount', Dimension.References_Count, True);
    // Flags
    JsonWriteBoolean('primitiveLock', Dimension.PrimitiveLock, True);
    JsonWriteBoolean('moveable', Dimension.Moveable, True);
    JsonWriteBoolean('inComponent', Dimension.InComponent, True);
    JsonWriteBoolean('drcError', Dimension.DRCError, True);
    JsonWriteBoolean('miscFlag1', Dimension.MiscFlag1, True);
    JsonWriteBoolean('miscFlag2', Dimension.MiscFlag2, True);
    JsonWriteBoolean('miscFlag3', Dimension.MiscFlag3, True);
    // Identifier strings
    JsonWriteString('objectIDString', Dimension.ObjectIDString, True);
    JsonWriteString('identifier', Dimension.Identifier, True);
    JsonWriteString('descriptor', Dimension.Descriptor, True);
    JsonWriteString('detail', Dimension.Detail, True);
    JsonWriteString('uniqueId', Dimension.UniqueId, True);
        // IPCB_Primitive base properties
    JsonWriteInteger('layer_V6', Dimension.Layer_V6, True);
    JsonWriteBoolean('inBoard', Dimension.InBoard, True);
    JsonWriteBoolean('inPolygon', Dimension.InPolygon, True);
    JsonWriteBoolean('selected', Dimension.Selected, True);
    JsonWriteBoolean('used', Dimension.Used, True);
    JsonWriteBoolean('userRouted', Dimension.UserRouted, True);
    JsonWriteBoolean('enabled', Dimension.Enabled, True);
    JsonWriteBoolean('polygonOutline', Dimension.PolygonOutline, True);
    JsonWriteBoolean('tearDrop', Dimension.TearDrop, True);
    JsonWriteBoolean('allowGlobalEdit', Dimension.AllowGlobalEdit, True);
    JsonWriteBoolean('drawAsPreview', Dimension.DrawAsPreview, True);
    JsonWriteBoolean('enableDraw', Dimension.EnableDraw, True);
    JsonWriteInteger('index', Dimension.Index, True);
    JsonWriteInteger('unionIndex', Dimension.UnionIndex, True);
    JsonWriteString('handle', Dimension.Handle, True);
    // Type-specific properties
    JsonWriteInteger('textFont', Dimension.TextFont, True);
        // IPCB_Primitive base properties
    JsonWriteCoord('pasteMaskExpansion', Dimension.PasteMaskExpansion, True);
    JsonWriteCoord('solderMaskExpansion', Dimension.SolderMaskExpansion, True);
    JsonWriteCoord('powerPlaneClearance', Dimension.PowerPlaneClearance, True);
    JsonWriteCoord('powerPlaneReliefExpansion', Dimension.PowerPlaneReliefExpansion, True);
    JsonWriteInteger('powerPlaneConnectStyle', Dimension.PowerPlaneConnectStyle, True);
    JsonWriteCoord('reliefAirGap', Dimension.ReliefAirGap, True);
    JsonWriteCoord('reliefConductorWidth', Dimension.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', Dimension.ReliefEntries, True);
    JsonWriteBoolean('isKeepout', Dimension.IsKeepout, True);
    JsonWriteBoolean('inNet', Dimension.InNet, True);
    JsonWriteBoolean('inCoordinate', Dimension.InCoordinate, True);
    JsonWriteBoolean('inDimension', Dimension.InDimension, True);
    JsonWriteBoolean('isPreRoute', Dimension.IsPreRoute, True);
    JsonWriteBoolean('isTestpoint_Bottom', Dimension.IsTestpoint_Bottom, True);
    JsonWriteBoolean('isTestpoint_Top', Dimension.IsTestpoint_Top, True);
    JsonWriteBoolean('isAssyTestpoint_Bottom', Dimension.IsAssyTestpoint_Bottom, True);
    JsonWriteBoolean('isAssyTestpoint_Top', Dimension.IsAssyTestpoint_Top, True);
    JsonWriteBoolean('isTenting', Dimension.IsTenting, True);
    JsonWriteBoolean('isTenting_Bottom', Dimension.IsTenting_Bottom, True);
    JsonWriteBoolean('isTenting_Top', Dimension.IsTenting_Top, True);
    JsonWriteBoolean('isElectricalPrim', Dimension.IsElectricalPrim, True);
        // Additional readable properties
    JsonWriteBoolean('isFreePrimitive', Dimension.IsFreePrimitive, True);
    JsonWriteBoolean('isHidden', Dimension.IsHidden, True);
    JsonWriteBoolean('references_Validate', Dimension.References_Validate, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbViaToJson(Via: IPCB_Via; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Via', True);
    JsonWriteCoord('x', Via.X, True);
    JsonWriteCoord('y', Via.Y, True);
    JsonWriteInteger('mode', Ord(Via.Mode), True);
    // Size and hole
    JsonWriteCoord('size', Via.Size, True);
    JsonWriteCoord('holeSize', Via.HoleSize, True);
    JsonWriteCoord('height', Via.Height, True);
    // Layer span
    JsonWriteInteger('lowLayer', Via.LowLayer, True);
    JsonWriteInteger('highLayer', Via.HighLayer, True);
    JsonWriteInteger('layer', Via.Layer, True);
    // Tolerances
    JsonWriteCoord('holePositiveTolerance', Via.HolePositiveTolerance, True);
    JsonWriteCoord('holeNegativeTolerance', Via.HoleNegativeTolerance, True);
    // Plane connection
    JsonWriteInteger('powerPlaneConnectStyle', Ord(Via.PowerPlaneConnectStyle), True);
    JsonWriteCoord('reliefConductorWidth', Via.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', Via.ReliefEntries, True);
    JsonWriteCoord('reliefAirGap', Via.ReliefAirGap, True);
    // Mask expansions
    JsonWriteCoord('pasteMaskExpansion', Via.PasteMaskExpansion, True);
    JsonWriteCoord('solderMaskExpansion', Via.SolderMaskExpansion, True);
    JsonWriteBoolean('solderMaskExpansionFromHoleEdge', Via.SolderMaskExpansionFromHoleEdge, True);
    JsonWriteCoord('powerPlaneClearance', Via.PowerPlaneClearance, True);
    JsonWriteCoord('powerPlaneReliefExpansion', Via.PowerPlaneReliefExpansion, True);
    // Flags
    JsonWriteBoolean('isTenting', Via.IsTenting, True);
    JsonWriteBoolean('isTenting_Top', Via.IsTenting_Top, True);
    JsonWriteBoolean('isTenting_Bottom', Via.IsTenting_Bottom, True);
    JsonWriteBoolean('isTestpoint_Top', Via.IsTestpoint_Top, True);
    JsonWriteBoolean('isTestpoint_Bottom', Via.IsTestpoint_Bottom, True);
    JsonWriteBoolean('isAssyTestpoint_Top', Via.IsAssyTestpoint_Top, True);
    JsonWriteBoolean('isAssyTestpoint_Bottom', Via.IsAssyTestpoint_Bottom, True);
    JsonWriteBoolean('inComponent', Via.InComponent, True);
    JsonWriteBoolean('inNet', Via.InNet, True);
    JsonWriteBoolean('isElectricalPrim', Via.IsElectricalPrim, True);
    JsonWriteBoolean('moveable', Via.Moveable, True);
    JsonWriteBoolean('isBackdrill', Via.IsBackdrill, True);
    JsonWriteBoolean('isKeepout', Via.IsKeepout, True);
    JsonWriteBoolean('drcError', Via.DRCError, True);
    JsonWriteBoolean('miscFlag1', Via.MiscFlag1, True);
    JsonWriteBoolean('miscFlag2', Via.MiscFlag2, True);
    JsonWriteBoolean('miscFlag3', Via.MiscFlag3, True);
    // Identifier strings
    JsonWriteString('objectIDString', Via.ObjectIDString, True);
    JsonWriteString('identifier', Via.Identifier, True);
    JsonWriteString('descriptor', Via.Descriptor, True);
    JsonWriteString('detail', Via.Detail, True);
        // IPCB_Primitive base properties
    JsonWriteBoolean('inCoordinate', Via.InCoordinate, True);
    JsonWriteBoolean('inDimension', Via.InDimension, True);
    JsonWriteBoolean('isPreRoute', Via.IsPreRoute, True);
    // Type-specific properties
    JsonWriteInteger('drillLayerPairType', Via.DrillLayerPairType, True);
    JsonWriteBoolean('isCounterHole', Via.IsCounterHole, True);
        // IPCB_Primitive base properties
    JsonWriteInteger('layer_V6', Via.Layer_V6, True);
    JsonWriteBoolean('inBoard', Via.InBoard, True);
    JsonWriteBoolean('inPolygon', Via.InPolygon, True);
    JsonWriteBoolean('selected', Via.Selected, True);
    JsonWriteBoolean('used', Via.Used, True);
    JsonWriteBoolean('userRouted', Via.UserRouted, True);
    JsonWriteBoolean('enabled', Via.Enabled, True);
    JsonWriteBoolean('polygonOutline', Via.PolygonOutline, True);
    JsonWriteBoolean('tearDrop', Via.TearDrop, True);
    JsonWriteBoolean('allowGlobalEdit', Via.AllowGlobalEdit, True);
    JsonWriteBoolean('drawAsPreview', Via.DrawAsPreview, True);
    JsonWriteBoolean('enableDraw', Via.EnableDraw, True);
    JsonWriteInteger('index', Via.Index, True);
    JsonWriteInteger('unionIndex', Via.UnionIndex, True);
    JsonWriteString('uniqueId', Via.UniqueId, True);
    JsonWriteString('handle', Via.Handle, True);
        // Additional readable properties
    JsonWriteBoolean('isFreePrimitive', Via.IsFreePrimitive, True);
    JsonWriteBoolean('isHidden', Via.IsHidden, True);
    JsonWriteBoolean('plated', Via.Plated, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbComponentToJson(Comp: IPCB_Component; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Component', True);
    // Basic identification
    JsonWriteString('pattern', Comp.Pattern, True);
    JsonWriteString('sourceDesignator', Comp.SourceDesignator, True);
    JsonWriteString('sourceUniqueId', Comp.SourceUniqueId, True);
    JsonWriteString('sourceHierarchicalPath', Comp.SourceHierarchicalPath, True);
    JsonWriteString('sourceFootprintLibrary', Comp.SourceFootprintLibrary, True);
    JsonWriteString('sourceComponentLibrary', Comp.SourceComponentLibrary, True);
    JsonWriteString('sourceLibReference', Comp.SourceLibReference, True);
    JsonWriteString('sourceCompDesignItemID', Comp.SourceCompDesignItemID, True);
    JsonWriteString('sourceDescription', Comp.SourceDescription, True);
    JsonWriteString('footprintDescription', Comp.FootprintDescription, True);
    // Location and orientation
    JsonWriteCoord('x', Comp.X, True);
    JsonWriteCoord('y', Comp.Y, True);
    JsonWriteFloat('rotation', Comp.Rotation, True);
    JsonWriteCoord('height', Comp.Height, True);
    JsonWriteInteger('layer', Comp.Layer, True);
    // Component kind
    JsonWriteInteger('componentKind', Ord(Comp.ComponentKind), True);
    // Display options
    JsonWriteBoolean('nameOn', Comp.NameOn, True);
    JsonWriteBoolean('commentOn', Comp.CommentOn, True);
    JsonWriteBoolean('lockStrings', Comp.LockStrings, True);
    JsonWriteInteger('nameAutoPosition', Ord(Comp.NameAutoPosition), True);
    JsonWriteInteger('commentAutoPosition', Ord(Comp.CommentAutoPosition), True);
    // Grouping
    JsonWriteInteger('groupNum', Comp.GroupNum, True);
    JsonWriteInteger('axisCount', Comp.AxisCount, True);
    JsonWriteInteger('channelOffset', Comp.ChannelOffset, True);
    // 3D model
    JsonWriteString('defaultPCB3DModel', Comp.DefaultPCB3DModel, True);
    // BGA and swapping
    JsonWriteBoolean('isBGA', Comp.IsBGA, True);
    JsonWriteBoolean('enablePinSwapping', Comp.EnablePinSwapping, True);
    JsonWriteBoolean('enablePartSwapping', Comp.EnablePartSwapping, True);
    // Vault/managed component info
    JsonWriteString('vaultGUID', Comp.VaultGUID, True);
    JsonWriteString('itemGUID', Comp.ItemGUID, True);
    JsonWriteString('itemRevisionGUID', Comp.ItemRevisionGUID, True);
    // Configurator
    JsonWriteString('footprintConfigurableParameters_Encoded', Comp.FootprintConfigurableParameters_Encoded, True);
    JsonWriteString('footprintConfiguratorName', Comp.FootprintConfiguratorName, True);
    // Flags
    JsonWriteBoolean('flippedOnLayer', Comp.FlippedOnLayer, True);
    JsonWriteBoolean('jumpersVisible', Comp.JumpersVisible, True);
    JsonWriteBoolean('primitiveLock', Comp.PrimitiveLock, True);
    JsonWriteBoolean('moveable', Comp.Moveable, True);
    JsonWriteBoolean('drcError', Comp.DRCError, True);
    JsonWriteBoolean('miscFlag1', Comp.MiscFlag1, True);
    JsonWriteBoolean('miscFlag2', Comp.MiscFlag2, True);
    JsonWriteBoolean('miscFlag3', Comp.MiscFlag3, True);
    // Identifier strings
    JsonWriteString('objectIDString', Comp.ObjectIDString, True);
    JsonWriteString('identifier', Comp.Identifier, True);
    JsonWriteString('descriptor', Comp.Descriptor, True);
    JsonWriteString('detail', Comp.Detail, True);
    JsonWriteString('uniqueId', Comp.UniqueId, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbAccordionToJson(Accordion: IPCB_Accordion; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Accordion', True);
    // Position and size
    JsonWriteCoord('amplitudeIncrement', Accordion.AmplitudeIncrement, True);
    JsonWriteCoord('maxAmplitude', Accordion.MaxAmplitude, True);
    JsonWriteCoord('gap', Accordion.Gap, True);
    JsonWriteCoord('gapIncrement', Accordion.GapIncrement, True);
    JsonWriteCoord('connectionLength', Accordion.ConnectonLength, True);
    JsonWriteCoord('estimateLength', Accordion.EstimateLength, True);
    JsonWriteInteger('style', Accordion.Style, True);
    JsonWriteInteger('layer', Accordion.Layer, True);
    JsonWriteInteger('layer_V6', Accordion.Layer_V6, True);
    // Mask/clearance
    JsonWriteCoord('pasteMaskExpansion', Accordion.PasteMaskExpansion, True);
    JsonWriteCoord('solderMaskExpansion', Accordion.SolderMaskExpansion, True);
    JsonWriteCoord('powerPlaneClearance', Accordion.PowerPlaneClearance, True);
    JsonWriteCoord('powerPlaneReliefExpansion', Accordion.PowerPlaneReliefExpansion, True);
    JsonWriteInteger('powerPlaneConnectStyle', Accordion.PowerPlaneConnectStyle, True);
    JsonWriteCoord('reliefAirGap', Accordion.ReliefAirGap, True);
    JsonWriteCoord('reliefConductorWidth', Accordion.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', Accordion.ReliefEntries, True);
    // State flags
    JsonWriteBoolean('isKeepout', Accordion.IsKeepout, True);
    JsonWriteBoolean('inComponent', Accordion.InComponent, True);
    JsonWriteBoolean('inNet', Accordion.InNet, True);
    JsonWriteBoolean('inBoard', Accordion.InBoard, True);
    JsonWriteBoolean('inPolygon', Accordion.InPolygon, True);
    JsonWriteBoolean('moveable', Accordion.Moveable, True);
    JsonWriteBoolean('selected', Accordion.Selected, True);
    JsonWriteBoolean('used', Accordion.Used, True);
    JsonWriteBoolean('userRouted', Accordion.UserRouted, True);
    JsonWriteBoolean('drcError', Accordion.DRCError, True);
    JsonWriteBoolean('enabled', Accordion.Enabled, True);
    JsonWriteBoolean('tearDrop', Accordion.TearDrop, True);
    JsonWriteBoolean('polygonOutline', Accordion.PolygonOutline, True);
    // Misc flags
    JsonWriteBoolean('allowGlobalEdit', Accordion.AllowGlobalEdit, True);
    JsonWriteBoolean('drawAsPreview', Accordion.DrawAsPreview, True);
    JsonWriteBoolean('enableDraw', Accordion.EnableDraw, True);
    JsonWriteBoolean('miscFlag1', Accordion.MiscFlag1, True);
    JsonWriteBoolean('miscFlag2', Accordion.MiscFlag2, True);
    JsonWriteBoolean('miscFlag3', Accordion.MiscFlag3, True);
    // Identifiers
    JsonWriteInteger('index', Accordion.Index, True);
    JsonWriteInteger('unionIndex', Accordion.UnionIndex, True);
    JsonWriteString('uniqueId', Accordion.UniqueId, True);
    JsonWriteString('handle', Accordion.Handle, True);
    JsonWriteString('objectIDString', Accordion.ObjectIDString, True);
    JsonWriteString('identifier', Accordion.Identifier, True);
    JsonWriteString('descriptor', Accordion.Descriptor, True);
    JsonWriteString('detail', Accordion.Detail, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbBoardOutlineToJson(Outline: IPCB_BoardOutline; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BoardOutline', True);
    // Position
    JsonWriteCoord('x', Outline.X, True);
    JsonWriteCoord('y', Outline.Y, True);
    // Properties
    JsonWriteString('name', Outline.Name, True);
    JsonWriteInteger('layer', Outline.Layer, True);
    JsonWriteInteger('layer_V6', Outline.Layer_V6, True);
    JsonWriteCoord('borderWidth', Outline.BorderWidth, True);
    JsonWriteCoord('arcApproximation', Outline.ArcApproximation, True);
    JsonWriteFloat('areaSize', Outline.AreaSize, True);
    JsonWriteCoord('grid', Outline.Grid, True);
    JsonWriteCoord('trackSize', Outline.TrackSize, True);
    JsonWriteCoord('minTrack', Outline.MinTrack, True);
    JsonWriteInteger('pointCount', Outline.PointCount, True);
    JsonWriteInteger('pourIndex', Outline.PourIndex, True);
    JsonWriteInteger('polygonType', Outline.PolygonType, True);
    JsonWriteInteger('polyHatchStyle', Outline.PolyHatchStyle, True);
    JsonWriteInteger('pourOver', Outline.PourOver, True);
    // Boolean flags
    JsonWriteBoolean('poured', Outline.Poured, True);
    JsonWriteBoolean('autoGenerateName', Outline.AutoGenerateName, True);
    JsonWriteBoolean('arcPourMode', Outline.ArcPourMode, True);
    JsonWriteBoolean('avoidObstacles', Outline.AvoidObstacles, True);
    JsonWriteBoolean('clipAcuteCorners', Outline.ClipAcuteCorners, True);
    JsonWriteBoolean('drawDeadCopper', Outline.DrawDeadCopper, True);
    JsonWriteBoolean('drawRemovedIslands', Outline.DrawRemovedIslands, True);
    JsonWriteBoolean('drawRemovedNecks', Outline.DrawRemovedNecks, True);
    JsonWriteBoolean('expandOutline', Outline.ExpandOutline, True);
    JsonWriteBoolean('ignoreViolations', Outline.IgnoreViolations, True);
    JsonWriteBoolean('mitreCorners', Outline.MitreCorners, True);
    JsonWriteBoolean('obeyPolygonCutout', Outline.ObeyPolygonCutout, True);
    JsonWriteBoolean('optimalVoidRotation', Outline.OptimalVoidRotation, True);
    JsonWriteBoolean('primitiveLock', Outline.PrimitiveLock, True);
    JsonWriteBoolean('removeDead', Outline.RemoveDead, True);
    JsonWriteBoolean('removeIslandsByArea', Outline.RemoveIslandsByArea, True);
    JsonWriteBoolean('removeNarrowNecks', Outline.RemoveNarrowNecks, True);
    JsonWriteBoolean('useOctagons', Outline.UseOctagons, True);
    JsonWriteFloat('islandAreaThreshold', Outline.IslandAreaThreshold, True);
    JsonWriteCoord('neckWidthThreshold', Outline.NeckWidthThreshold, True);
    // Mask/clearance
    JsonWriteCoord('pasteMaskExpansion', Outline.PasteMaskExpansion, True);
    JsonWriteCoord('solderMaskExpansion', Outline.SolderMaskExpansion, True);
    JsonWriteCoord('powerPlaneClearance', Outline.PowerPlaneClearance, True);
    JsonWriteCoord('powerPlaneReliefExpansion', Outline.PowerPlaneReliefExpansion, True);
    JsonWriteInteger('powerPlaneConnectStyle', Outline.PowerPlaneConnectStyle, True);
    JsonWriteCoord('reliefAirGap', Outline.ReliefAirGap, True);
    JsonWriteCoord('reliefConductorWidth', Outline.ReliefConductorWidth, True);
    JsonWriteInteger('reliefEntries', Outline.ReliefEntries, True);
    // State flags
    JsonWriteBoolean('isKeepout', Outline.IsKeepout, True);
    JsonWriteBoolean('inComponent', Outline.InComponent, True);
    JsonWriteBoolean('inNet', Outline.InNet, True);
    JsonWriteBoolean('inBoard', Outline.InBoard, True);
    JsonWriteBoolean('selected', Outline.Selected, True);
    JsonWriteBoolean('used', Outline.Used, True);
    JsonWriteBoolean('drcError', Outline.DRCError, True);
    JsonWriteBoolean('enabled', Outline.Enabled, True);
    // Identifiers
    JsonWriteString('uniqueId', Outline.UniqueId, True);
    JsonWriteString('handle', Outline.Handle, True);
    JsonWriteString('objectIDString', Outline.ObjectIDString, True);
    JsonWriteString('identifier', Outline.Identifier, True);
    JsonWriteString('descriptor', Outline.Descriptor, True);
    JsonWriteString('detail', Outline.Detail, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbContourToJson(Contour: IPCB_Contour; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Contour', True);
    JsonWriteInteger('count', Contour.Count, True);
    JsonWriteFloat('area', Contour.Area, True);
    JsonWriteBoolean('isCW', Contour.IsCW, True);
    // Export vertices
    JsonOpenArray('vertices');
    for I := 0 to Contour.Count - 1 do begin
        JsonOpenObject('');
        JsonWriteCoord('x', Contour.X[I], True);
        JsonWriteCoord('y', Contour.Y[I], False);
        JsonCloseObject(I < Contour.Count - 1);
    end;
    JsonCloseArray(False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbConnectionToJson(Connection: IPCB_Connection; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Connection', True);
    JsonWriteCoord('x1', Connection.X1, True);
    JsonWriteCoord('y1', Connection.Y1, True);
    JsonWriteCoord('x2', Connection.X2, True);
    JsonWriteCoord('y2', Connection.Y2, True);
    JsonWriteInteger('layer', Connection.Layer, True);
    JsonWriteBoolean('isKeepout', Connection.IsKeepout, True);
    JsonWriteBoolean('inComponent', Connection.InComponent, True);
    JsonWriteBoolean('inNet', Connection.InNet, True);
    JsonWriteBoolean('selected', Connection.Selected, True);
    JsonWriteBoolean('drcError', Connection.DRCError, True);
    JsonWriteString('uniqueId', Connection.UniqueId, True);
    JsonWriteString('handle', Connection.Handle, True);
    JsonWriteString('objectIDString', Connection.ObjectIDString, True);
    JsonWriteString('identifier', Connection.Identifier, True);
    JsonWriteString('descriptor', Connection.Descriptor, True);
    JsonWriteString('detail', Connection.Detail, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbCoordinateToJson(Coord: IPCB_Coordinate; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Coordinate', True);
    JsonWriteCoord('x', Coord.X, True);
    JsonWriteCoord('y', Coord.Y, True);
    JsonWriteCoord('x1', Coord.X1, True);
    JsonWriteCoord('y1', Coord.Y1, True);
    JsonWriteCoord('x2', Coord.X2, True);
    JsonWriteCoord('y2', Coord.Y2, True);
    JsonWriteInteger('layer', Coord.Layer, True);
    JsonWriteBoolean('isKeepout', Coord.IsKeepout, True);
    JsonWriteBoolean('selected', Coord.Selected, True);
    JsonWriteBoolean('drcError', Coord.DRCError, True);
    JsonWriteString('uniqueId', Coord.UniqueId, True);
    JsonWriteString('handle', Coord.Handle, True);
    JsonWriteString('objectIDString', Coord.ObjectIDString, True);
    JsonWriteString('identifier', Coord.Identifier, True);
    JsonWriteString('descriptor', Coord.Descriptor, True);
    JsonWriteString('detail', Coord.Detail, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbAngularDimensionToJson(Dim: IPCB_AngularDimension; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'AngularDimension', True);
    // Base dimension properties
    JsonWriteCoord('x', Dim.X, True);
    JsonWriteCoord('y', Dim.Y, True);
    JsonWriteInteger('layer', Dim.Layer, True);
    JsonWriteCoord('lineWidth', Dim.LineWidth, True);
    JsonWriteCoord('textHeight', Dim.TextHeight, True);
    JsonWriteCoord('textWidth', Dim.TextWidth, True);
    JsonWriteFloat('textRotation', Dim.TextRotation, True);
    JsonWriteCoord('textX', Dim.TextX, True);
    JsonWriteCoord('textY', Dim.TextY, True);
    JsonWriteCoord('arrowSize', Dim.ArrowSize, True);
    JsonWriteInteger('arrowPosition', Dim.ArrowPosition, True);
    JsonWriteInteger('textPosition', Dim.TextPosition, True);
    JsonWriteInteger('textDimensionUnit', Dim.TextDimensionUnit, True);
    JsonWriteInteger('textPrecision', Dim.TextPrecision, True);
    JsonWriteString('textPrefix', Dim.TextPrefix, True);
    JsonWriteString('textSuffix', Dim.TextSuffix, True);
    JsonWriteInteger('textFormat', Dim.TextFormat, True);
    JsonWriteInteger('textFont', Dim.TextFont, True);
    JsonWriteBoolean('textBold', Dim.TextBold, True);
    JsonWriteBoolean('textItalic', Dim.TextItalic, True);
    // State flags
    JsonWriteBoolean('isKeepout', Dim.IsKeepout, True);
    JsonWriteBoolean('selected', Dim.Selected, True);
    JsonWriteBoolean('drcError', Dim.DRCError, True);
    JsonWriteString('uniqueId', Dim.UniqueId, True);
    JsonWriteString('handle', Dim.Handle, True);
    JsonWriteString('objectIDString', Dim.ObjectIDString, True);
    JsonWriteString('identifier', Dim.Identifier, True);
    JsonWriteString('descriptor', Dim.Descriptor, True);
    JsonWriteString('detail', Dim.Detail, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbBaselineDimensionToJson(Dim: IPCB_BaselineDimension; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BaselineDimension', True);
    JsonWriteCoord('x', Dim.X, True);
    JsonWriteCoord('y', Dim.Y, True);
    JsonWriteInteger('layer', Dim.Layer, True);
    JsonWriteCoord('lineWidth', Dim.LineWidth, True);
    JsonWriteCoord('textHeight', Dim.TextHeight, True);
    JsonWriteCoord('arrowSize', Dim.ArrowSize, True);
    JsonWriteBoolean('isKeepout', Dim.IsKeepout, True);
    JsonWriteBoolean('selected', Dim.Selected, True);
    JsonWriteString('uniqueId', Dim.UniqueId, True);
    JsonWriteString('handle', Dim.Handle, True);
    JsonWriteString('objectIDString', Dim.ObjectIDString, True);
    JsonWriteString('identifier', Dim.Identifier, True);
    JsonWriteString('descriptor', Dim.Descriptor, True);
    JsonWriteString('detail', Dim.Detail, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbCenterDimensionToJson(Dim: IPCB_CenterDimension; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CenterDimension', True);
    JsonWriteCoord('x', Dim.X, True);
    JsonWriteCoord('y', Dim.Y, True);
    JsonWriteInteger('layer', Dim.Layer, True);
    JsonWriteCoord('lineWidth', Dim.LineWidth, True);
    JsonWriteCoord('size', Dim.Size, True);
    JsonWriteBoolean('isKeepout', Dim.IsKeepout, True);
    JsonWriteBoolean('selected', Dim.Selected, True);
    JsonWriteString('uniqueId', Dim.UniqueId, True);
    JsonWriteString('handle', Dim.Handle, True);
    JsonWriteString('objectIDString', Dim.ObjectIDString, True);
    JsonWriteString('identifier', Dim.Identifier, True);
    JsonWriteString('descriptor', Dim.Descriptor, True);
    JsonWriteString('detail', Dim.Detail, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbDatumDimensionToJson(Dim: IPCB_DatumDimension; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DatumDimension', True);
    JsonWriteCoord('x', Dim.X, True);
    JsonWriteCoord('y', Dim.Y, True);
    JsonWriteInteger('layer', Dim.Layer, True);
    JsonWriteCoord('lineWidth', Dim.LineWidth, True);
    JsonWriteCoord('textHeight', Dim.TextHeight, True);
    JsonWriteCoord('arrowSize', Dim.ArrowSize, True);
    JsonWriteBoolean('isKeepout', Dim.IsKeepout, True);
    JsonWriteBoolean('selected', Dim.Selected, True);
    JsonWriteString('uniqueId', Dim.UniqueId, True);
    JsonWriteString('handle', Dim.Handle, True);
    JsonWriteString('objectIDString', Dim.ObjectIDString, True);
    JsonWriteString('identifier', Dim.Identifier, True);
    JsonWriteString('descriptor', Dim.Descriptor, True);
    JsonWriteString('detail', Dim.Detail, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbDrillLayerPairToJson(Pair: IPCB_DrillLayerPair; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DrillLayerPair', True);
    JsonWriteInteger('lowLayer', Pair.LowLayer, True);
    JsonWriteInteger('highLayer', Pair.HighLayer, True);
    JsonWriteString('name', Pair.Name, True);
    JsonWriteBoolean('isValid', Pair.IsValid, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbBoardRegionToJson(Region: IPCB_BoardRegion; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BoardRegion', True);
    JsonWriteString('name', Region.Name, True);
    JsonWriteCoord('x', Region.X, True);
    JsonWriteCoord('y', Region.Y, True);
    JsonWriteInteger('layer', Region.Layer, True);
    JsonWriteInteger('kind', Region.Kind, True);
    JsonWriteBoolean('isKeepout', Region.IsKeepout, True);
    JsonWriteBoolean('selected', Region.Selected, True);
    JsonWriteBoolean('drcError', Region.DRCError, True);
    JsonWriteString('uniqueId', Region.UniqueId, True);
    JsonWriteString('handle', Region.Handle, True);
    JsonWriteString('objectIDString', Region.ObjectIDString, True);
    JsonWriteString('identifier', Region.Identifier, True);
    JsonWriteString('descriptor', Region.Descriptor, True);
    JsonWriteString('detail', Region.Detail, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbEmbeddedToJson(Embedded: IPCB_Embedded; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Embedded', True);
    JsonWriteCoord('x', Embedded.X, True);
    JsonWriteCoord('y', Embedded.Y, True);
    JsonWriteInteger('layer', Embedded.Layer, True);
    JsonWriteBoolean('isKeepout', Embedded.IsKeepout, True);
    JsonWriteBoolean('selected', Embedded.Selected, True);
    JsonWriteString('uniqueId', Embedded.UniqueId, True);
    JsonWriteString('handle', Embedded.Handle, True);
    JsonWriteString('objectIDString', Embedded.ObjectIDString, True);
    JsonWriteString('identifier', Embedded.Identifier, True);
    JsonWriteString('descriptor', Embedded.Descriptor, True);
    JsonWriteString('detail', Embedded.Detail, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbEmbeddedBoardToJson(EmbeddedBoard: IPCB_EmbeddedBoard; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'EmbeddedBoard', True);
    JsonWriteCoord('x', EmbeddedBoard.X, True);
    JsonWriteCoord('y', EmbeddedBoard.Y, True);
    JsonWriteInteger('layer', EmbeddedBoard.Layer, True);
    JsonWriteFloat('rotation', EmbeddedBoard.Rotation, True);
    JsonWriteString('documentPath', EmbeddedBoard.DocumentPath, True);
    JsonWriteBoolean('isKeepout', EmbeddedBoard.IsKeepout, True);
    JsonWriteBoolean('selected', EmbeddedBoard.Selected, True);
    JsonWriteString('uniqueId', EmbeddedBoard.UniqueId, True);
    JsonWriteString('handle', EmbeddedBoard.Handle, True);
    JsonWriteString('objectIDString', EmbeddedBoard.ObjectIDString, True);
    JsonWriteString('identifier', EmbeddedBoard.Identifier, True);
    JsonWriteString('descriptor', EmbeddedBoard.Descriptor, True);
    JsonWriteString('detail', EmbeddedBoard.Detail, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbBendingLineToJson(BendingLine: IPCB_BendingLine; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BendingLine', True);
    JsonWriteCoord('x1', BendingLine.X1, True);
    JsonWriteCoord('y1', BendingLine.Y1, True);
    JsonWriteCoord('x2', BendingLine.X2, True);
    JsonWriteCoord('y2', BendingLine.Y2, True);
    JsonWriteCoord('radius', BendingLine.Radius, True);
    JsonWriteFloat('angle', BendingLine.Angle, True);
    JsonWriteInteger('foldIndex', BendingLine.FoldIndex, True);
    JsonWriteInteger('layer', BendingLine.Layer, True);
    JsonWriteBoolean('isKeepout', BendingLine.IsKeepout, True);
    JsonWriteBoolean('selected', BendingLine.Selected, True);
    JsonWriteString('uniqueId', BendingLine.UniqueId, True);
    JsonWriteString('handle', BendingLine.Handle, True);
    JsonWriteString('objectIDString', BendingLine.ObjectIDString, True);
    JsonWriteString('identifier', BendingLine.Identifier, True);
    JsonWriteString('descriptor', BendingLine.Descriptor, True);
    JsonWriteString('detail', BendingLine.Detail, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbAxisToJson(Axis: IPCB_Axis; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Axis', True);
    JsonWriteCoord('x', Axis.X, True);
    JsonWriteCoord('y', Axis.Y, True);
    JsonWriteFloat('rotation', Axis.Rotation, True);
    JsonWriteInteger('layer', Axis.Layer, True);
    JsonWriteBoolean('isKeepout', Axis.IsKeepout, True);
    JsonWriteBoolean('selected', Axis.Selected, True);
    JsonWriteString('uniqueId', Axis.UniqueId, True);
    JsonWriteString('handle', Axis.Handle, True);
    JsonWriteString('objectIDString', Axis.ObjectIDString, True);
    JsonWriteString('identifier', Axis.Identifier, True);
    JsonWriteString('descriptor', Axis.Descriptor, True);
    JsonWriteString('detail', Axis.Detail, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbDifferentialPairToJson(DiffPair: IPCB_DifferentialPair; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DifferentialPair', True);
    JsonWriteString('name', DiffPair.Name, True);
    JsonWriteString('positiveNetName', DiffPair.PositiveNetName, True);
    JsonWriteString('negativeNetName', DiffPair.NegativeNetName, True);
    JsonWriteBoolean('enabled', DiffPair.Enabled, True);
    JsonWriteString('uniqueId', DiffPair.UniqueId, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcb3DBodyToJson(Body: IPCB_3DBody; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', '3DBody', True);
    JsonWriteInteger('faceCount', Body.FaceCount, True);
    JsonWriteInteger('triangleCount', Body.TriangleCount, True);
    JsonWriteInteger('vertexCount', Body.VertexCount, True);
    JsonWriteInteger('partsCount', Body.PartsCount, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbCopperBodyToJson(CopperBody: IPCB_CopperBody; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CopperBody', True);
    JsonWriteInteger('layer', CopperBody.Layer, True);
    JsonWriteInteger('shapeCount', CopperBody.ShapeCount, True);
    JsonWriteString('hash', CopperBody.GetState_Hash, True);
    JsonWriteString('suffix', CopperBody.GetState_Suffix, True);
    JsonWriteInteger('xOffset', CopperBody.GetXOffset, True);
    JsonWriteInteger('yOffset', CopperBody.GetYOffset, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbCopperPolygonToJson(CopperPoly: IPCB_CopperPolygon; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CopperPolygon', True);
    JsonWriteInteger('borderVertexCount', CopperPoly.BorderVertexCount, True);
    JsonWriteInteger('capIndexCount', CopperPoly.CapIndexCount, True);
    JsonWriteInteger('lineIndexCount', CopperPoly.LineIndexCount, True);
    JsonWriteInteger('vertexCount', CopperPoly.VertexCount, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbDesignVariantToJson(Variant: IPCB_DesignVariant; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DesignVariant', True);
    JsonWriteString('name', Variant.Name, True);
    JsonWriteString('variantID', Variant.VariantID, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbDrillTableToJson(DrillTable: IPCB_DrillTable; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DrillTable', True);
    JsonWriteCoord('x', DrillTable.x, True);
    JsonWriteCoord('y', DrillTable.y, True);
    JsonWriteCoord('width', DrillTable.Width, True);
    JsonWriteCoord('lineWidth', DrillTable.LineWidth, True);
    JsonWriteCoord('size', DrillTable.Size, True);
    JsonWriteInteger('layer', DrillTable.Layer, True);
    JsonWriteInteger('font', DrillTable.Font, True);
    JsonWriteInteger('drillTableUnits', Ord(DrillTable.DrillTableUnits), True);
    JsonWriteBoolean('includeFooter', DrillTable.IncludeFooter, True);
    JsonWriteBoolean('includeTitle', DrillTable.IncludeTitle, True);
    JsonWriteBoolean('includeVias', DrillTable.IncludeVias, True);
    JsonWriteBoolean('includePlatedPads', DrillTable.IncludePlatedPads, True);
    JsonWriteBoolean('includeNonplatedPads', DrillTable.IncludeNonplatedPads, True);
    JsonWriteBoolean('includeSlottedPads', DrillTable.IncludeSlottedPads, True);
    JsonWriteBoolean('includeNonslottedPads', DrillTable.IncludeNonslottedPads, True);
    JsonWriteBoolean('separatePadsVias', DrillTable.SeparatePadsVias, True);
    JsonWriteBoolean('showColumnComment', DrillTable.ShowColumnComment, True);
    JsonWriteBoolean('showColumnObjType', DrillTable.ShowColumnObjType, True);
    JsonWriteBoolean('showColumnTolerance', DrillTable.ShowColumnTolerance, True);
    JsonWriteBoolean('showSecondaryUnits', DrillTable.ShowSecondaryUnits, True);
    JsonWriteBoolean('mirror', DrillTable.Mirror, True);
    JsonWriteCoord('platingThickness', DrillTable.PlatingThickness, True);
    JsonWriteBoolean('selected', DrillTable.Selected, True);
    JsonWriteString('uniqueId', DrillTable.UniqueId, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbElectricalLayerToJson(ElecLayer: IPCB_ElectricalLayer; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ElectricalLayer', True);
    JsonWriteString('name', ElecLayer.Name, True);
    JsonWriteCoord('copperThickness', ElecLayer.CopperThickness, True);
    JsonWriteInteger('v6LayerID', ElecLayer.V6_LayerID, True);
    JsonWriteBoolean('usedByPrims', ElecLayer.UsedByPrims, True);
    JsonWriteBoolean('isInLayerStack', ElecLayer.IsInLayerStack, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbDielectricLayerToJson(DielectricLayer: IPCB_DielectricLayer; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DielectricLayer', True);
    JsonWriteString('name', DielectricLayer.Name, True);
    JsonWriteCoord('dielectricHeight', DielectricLayer.DielectricHeight, True);
    JsonWriteDouble('dielectricConstant', DielectricLayer.DielectricConstant, True);
    JsonWriteInteger('dielectricType', Ord(DielectricLayer.DielectricType), True);
    JsonWriteString('material', DielectricLayer.Material, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbLayerStackToJson(LayerStack: IPCB_LayerStack; AddComma: Boolean);
var
    I: Integer;
    Layer: IPCB_ElectricalLayer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'LayerStack', True);
    JsonWriteInteger('signalLayerCount', LayerStack.SignalLayerCount, True);
    JsonWriteInteger('dielectricLayerCount', LayerStack.DielectricLayerCount, True);
    JsonWriteBoolean('isFlex', LayerStack.IsFlex, True);
    JsonWriteString('name', LayerStack.Name, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbMasterLayerStackToJson(MasterStack: IPCB_MasterLayerStack; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'MasterLayerStack', True);
    JsonWriteInteger('layerStackCount', MasterStack.LayerStackCount, True);
    JsonWriteInteger('currentLayerStackIndex', MasterStack.CurrentLayerStackIndex, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbECOOptionsToJson(ECOOptions: IPCB_ECOOptions; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ECOOptions', True);
    JsonWriteString('ecoFileName', ECOOptions.ECOFileName, True);
    JsonWriteBoolean('ecoIsActive', ECOOptions.ECOIsActive, True);
    JsonWriteInteger('optionsObjectID', Ord(ECOOptions.OptionsObjectID), False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbAuthorInfoToJson(AuthorInfo: IPCB_AuthorInfo; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'AuthorInfo', True);
    JsonWriteString('author', AuthorInfo.Author, True);
    JsonWriteString('company', AuthorInfo.Company, True);
    JsonWriteString('address1', AuthorInfo.Address1, True);
    JsonWriteString('address2', AuthorInfo.Address2, True);
    JsonWriteString('address3', AuthorInfo.Address3, True);
    JsonWriteString('address4', AuthorInfo.Address4, True);
    JsonWriteString('telephone', AuthorInfo.Telephone, True);
    JsonWriteString('fax', AuthorInfo.Fax, True);
    JsonWriteString('email', AuthorInfo.Email, True);
    JsonWriteString('lastSavedTime', AuthorInfo.LastSavedTime, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbCartesianGridToJson(Grid: IPCB_CartesianGrid; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CartesianGrid', True);
    JsonWriteString('name', Grid.Name, True);
    JsonWriteCoord('originX', Grid.Origin.X, True);
    JsonWriteCoord('originY', Grid.Origin.Y, True);
    JsonWriteCoord('stepX', Grid.Step.X, True);
    JsonWriteCoord('stepY', Grid.Step.Y, True);
    JsonWriteCoord('multipleX', Grid.Multiple.X, True);
    JsonWriteCoord('multipleY', Grid.Multiple.Y, True);
    JsonWriteDouble('rotation', Grid.Rotation, True);
    JsonWriteInteger('color', Grid.Color, True);
    JsonWriteBoolean('visible', Grid.Visible, True);
    JsonWriteBoolean('on', Grid.On, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbBackDrillingToJson(BackDrilling: IPCB_BackDrilling; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BackDrilling', True);
    JsonWriteCoord('drillDepthFromTop', BackDrilling.DrillDepthFromTop, True);
    JsonWriteCoord('drillDepthFromBottom', BackDrilling.DrillDepthFromBottom, True);
    JsonWriteCoord('backDrillDiameter', BackDrilling.BackDrillDiameter, True);
    JsonWriteBoolean('backDrilledFromTop', BackDrilling.BackDrilledFromTop, True);
    JsonWriteBoolean('backDrilledFromBottom', BackDrilling.BackDrilledFromBottom, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbCounterHoleParamsToJson(Params: IPCB_CounterHoleParams; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CounterHoleParams', True);
    JsonWriteCoord('width', Params.Width, True);
    JsonWriteCoord('depth', Params.Depth, True);
    JsonWriteDouble('angle', Params.Angle, True);
    JsonWriteInteger('shape', Ord(Params.Shape), False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbDiePadBondInfoToJson(BondInfo: IPCB_DiePadBondInfo; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DiePadBondInfo', True);
    JsonWriteInteger('connectionCount', BondInfo.ConnectionCount, True);
    JsonWriteBoolean('diePadShape', BondInfo.DiePadShape, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbColorIDToJson(ColorID: IPCB_ColorID; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ColorID', True);
    JsonWriteInteger('colorId', ColorID.Id, True);
    JsonWriteString('name', ColorID.Name, True);
    JsonWriteInteger('color', ColorID.Color, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbColorOverrideOptionsToJson(ColorOpt: IPCB_ColorOverrideOptions; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ColorOverrideOptions', True);
    JsonWriteBoolean('overrideAll', ColorOpt.OverrideAll, True);
    JsonWriteInteger('allColor', ColorOpt.AllColor, True);
    JsonWriteInteger('optionsObjectID', Ord(ColorOpt.OptionsObjectID), False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbBoardRoutingOptionsToJson(RoutOpt: IPCB_BoardRoutingOptions; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BoardRoutingOptions', True);
    JsonWriteBoolean('autorouterEnabled', RoutOpt.AutorouterEnabled, True);
    JsonWriteInteger('routingLayers', RoutOpt.RoutingLayers, True);
    JsonWriteCoord('defaultTrackWidth', RoutOpt.DefaultTrackWidth, True);
    JsonWriteCoord('defaultViaDiameter', RoutOpt.DefaultViaDiameter, True);
    JsonWriteCoord('defaultViaHoleSize', RoutOpt.DefaultViaHoleSize, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbDesignRuleCheckerOptionsToJson(DRCOpt: IPCB_DesignRuleCheckerOptions; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DesignRuleCheckerOptions', True);
    JsonWriteBoolean('onlineEnabled', DRCOpt.OnlineEnabled, True);
    JsonWriteBoolean('batchEnabled', DRCOpt.BatchEnabled, True);
    JsonWriteBoolean('checkUnroutedNets', DRCOpt.CheckUnroutedNets, True);
    JsonWriteBoolean('checkClearance', DRCOpt.CheckClearance, True);
    JsonWriteBoolean('checkShortCircuit', DRCOpt.CheckShortCircuit, True);
    JsonWriteBoolean('checkUnconnectedPins', DRCOpt.CheckUnconnectedPins, True);
    JsonWriteBoolean('checkMinimumHoleSize', DRCOpt.CheckMinimumHoleSize, True);
    JsonWriteBoolean('checkMaximumViaCount', DRCOpt.CheckMaximumViaCount, True);
    JsonWriteInteger('optionsObjectID', Ord(DRCOpt.OptionsObjectID), False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbClearanceConstraintToJson(Rule: IPCB_ClearanceConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ClearanceConstraint', True);
    JsonWriteString('name', Rule.Name, True);
    JsonWriteCoord('gap', Rule.Gap, True);
    JsonWriteBoolean('enabled', Rule.Enabled, True);
    JsonWriteInteger('priority', Rule.Priority, True);
    JsonWriteString('uniqueId', Rule.UniqueId, True);
    JsonWriteString('comment', Rule.Comment, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbComponentClearanceConstraintToJson(Rule: IPCB_ComponentClearanceConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ComponentClearanceConstraint', True);
    JsonWriteString('name', Rule.Name, True);
    JsonWriteCoord('verticalGap', Rule.VerticalGap, True);
    JsonWriteCoord('horizontalGap', Rule.HorizontalGap, True);
    JsonWriteBoolean('enabled', Rule.Enabled, True);
    JsonWriteInteger('priority', Rule.Priority, True);
    JsonWriteString('uniqueId', Rule.UniqueId, True);
    JsonWriteString('comment', Rule.Comment, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbBoardOutlineClearanceConstraintToJson(Rule: IPCB_BoardOutlineClearanceConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BoardOutlineClearanceConstraint', True);
    JsonWriteString('name', Rule.Name, True);
    JsonWriteCoord('gap', Rule.Gap, True);
    JsonWriteBoolean('enabled', Rule.Enabled, True);
    JsonWriteInteger('priority', Rule.Priority, True);
    JsonWriteString('uniqueId', Rule.UniqueId, True);
    JsonWriteString('comment', Rule.Comment, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbConfinementConstraintToJson(Rule: IPCB_ConfinementConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ConfinementConstraint', True);
    JsonWriteString('name', Rule.Name, True);
    JsonWriteBoolean('enabled', Rule.Enabled, True);
    JsonWriteInteger('priority', Rule.Priority, True);
    JsonWriteInteger('confinementKind', Ord(Rule.ConfinementKind), True);
    JsonWriteString('uniqueId', Rule.UniqueId, True);
    JsonWriteString('comment', Rule.Comment, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbCreepageRuleToJson(Rule: IPCB_CreepageRule; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CreepageRule', True);
    JsonWriteString('name', Rule.Name, True);
    JsonWriteCoord('creepageDistance', Rule.CreepageDistance, True);
    JsonWriteBoolean('enabled', Rule.Enabled, True);
    JsonWriteInteger('priority', Rule.Priority, True);
    JsonWriteString('uniqueId', Rule.UniqueId, True);
    JsonWriteString('comment', Rule.Comment, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbDifferentialPairsRoutingRuleToJson(Rule: IPCB_DifferentialPairsRoutingRule; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DifferentialPairsRoutingRule', True);
    JsonWriteString('name', Rule.Name, True);
    JsonWriteCoord('gap', Rule.Gap, True);
    JsonWriteCoord('width', Rule.Width, True);
    JsonWriteBoolean('enabled', Rule.Enabled, True);
    JsonWriteInteger('priority', Rule.Priority, True);
    JsonWriteString('uniqueId', Rule.UniqueId, True);
    JsonWriteString('comment', Rule.Comment, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbDaisyChainStubLengthConstraintToJson(Rule: IPCB_DaisyChainStubLengthConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'DaisyChainStubLengthConstraint', True);
    JsonWriteString('name', Rule.Name, True);
    JsonWriteCoord('minLength', Rule.MinLength, True);
    JsonWriteCoord('maxLength', Rule.MaxLength, True);
    JsonWriteBoolean('enabled', Rule.Enabled, True);
    JsonWriteInteger('priority', Rule.Priority, True);
    JsonWriteString('uniqueId', Rule.UniqueId, True);
    JsonWriteString('comment', Rule.Comment, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbMatchedLengthConstraintToJson(Rule: IPCB_MatchedLengthConstraint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'MatchedLengthConstraint', True);
    JsonWriteString('name', Rule.Name, True);
    JsonWriteCoord('tolerance', Rule.Tolerance, True);
    JsonWriteBoolean('enabled', Rule.Enabled, True);
    JsonWriteInteger('priority', Rule.Priority, True);
    JsonWriteString('uniqueId', Rule.UniqueId, True);
    JsonWriteString('comment', Rule.Comment, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbCheckNetAntennaeRuleToJson(Rule: IPCB_CheckNetAntennaeRule; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CheckNetAntennaeRule', True);
    JsonWriteString('name', Rule.Name, True);
    JsonWriteBoolean('enabled', Rule.Enabled, True);
    JsonWriteInteger('priority', Rule.Priority, True);
    JsonWriteString('uniqueId', Rule.UniqueId, True);
    JsonWriteString('comment', Rule.Comment, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbBackDrillingRuleToJson(Rule: IPCB_BackDrillingRule; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BackDrillingRule', True);
    JsonWriteString('name', Rule.Name, True);
    JsonWriteCoord('backDrillDiameter', Rule.BackDrillDiameter, True);
    JsonWriteBoolean('enabled', Rule.Enabled, True);
    JsonWriteInteger('priority', Rule.Priority, True);
    JsonWriteString('uniqueId', Rule.UniqueId, True);
    JsonWriteString('comment', Rule.Comment, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbFanoutControlRuleToJson(Rule: IPCB_FanoutControlRule; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'FanoutControlRule', True);
    JsonWriteString('name', Rule.Name, True);
    JsonWriteInteger('fanoutStyle', Ord(Rule.FanoutStyle), True);
    JsonWriteCoord('fanoutLength', Rule.FanoutLength, True);
    JsonWriteBoolean('enabled', Rule.Enabled, True);
    JsonWriteInteger('priority', Rule.Priority, True);
    JsonWriteString('uniqueId', Rule.UniqueId, True);
    JsonWriteString('comment', Rule.Comment, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbComponentRotationsRuleToJson(Rule: IPCB_ComponentRotationsRule; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ComponentRotationsRule', True);
    JsonWriteString('name', Rule.Name, True);
    JsonWriteBoolean('enabled', Rule.Enabled, True);
    JsonWriteInteger('priority', Rule.Priority, True);
    JsonWriteString('uniqueId', Rule.UniqueId, True);
    JsonWriteString('comment', Rule.Comment, False);
    JsonCloseObject(AddComma);
end;

procedure ExportPcbLibToJson(PCBLib: IPCB_Library; JsonPath: String);
var
    LibComp: IPCB_LibComponent;
    Iterator: IPCB_GroupIterator;
    Prim: IPCB_Primitive;
    CompCount, I: Integer;
    IsLastComp: Boolean;
begin
    if PCBLib = nil then Exit;

    JsonBegin;
    JsonOpenObject('');

    // Metadata
    JsonOpenObject('metadata');
    JsonWriteString('exportType', 'PcbLib', True);
    JsonWriteString('fileName', ExtractFileName(PCBLib.Board.FileName), True);
    JsonWriteString('exportedBy', 'AltiumSharp TestDataGenerator', True);
    JsonWriteString('version', '1.0', False);
    JsonCloseObject(True);

    // Footprints array
    JsonOpenArray('footprints');

    CompCount := PCBLib.ComponentCount;
    for I := 0 to CompCount - 1 do
    begin
        LibComp := PCBLib.GetComponent(I);
        if LibComp = nil then Continue;

        IsLastComp := (I = CompCount - 1);

        JsonOpenObject('');
        JsonWriteString('name', LibComp.Name, True);
        JsonWriteString('description', LibComp.Description, True);
        JsonWriteCoord('height', LibComp.Height, True);

        // Primitives array
        JsonOpenArray('primitives');

        Iterator := LibComp.GroupIterator_Create;
        Iterator.AddFilter_ObjectSet(MkSet(ePadObject, eTrackObject, eArcObject, eFillObject, eTextObject, eRegionObject, ePolyObject, eComponentBodyObject, eDimensionObject, eViaObject));

        Prim := Iterator.FirstPCBObject;
        while Prim <> nil do
        begin
            case Prim.ObjectId of
                ePadObject: ExportPcbPadToJson(Prim, True);
                eTrackObject: ExportPcbTrackToJson(Prim, True);
                eArcObject: ExportPcbArcToJson(Prim, True);
                eFillObject: ExportPcbFillToJson(Prim, True);
                eTextObject: ExportPcbTextToJson(Prim, True);
                eRegionObject: ExportPcbRegionToJson(Prim, True);
                ePolyObject: ExportPcbPolygonToJson(Prim, True);
                eComponentBodyObject: ExportPcbComponentBodyToJson(Prim, True);
                eDimensionObject: ExportPcbDimensionToJson(Prim, True);
                eViaObject: ExportPcbViaToJson(Prim, True);
            end;

            Prim := Iterator.NextPCBObject;
        end;

        LibComp.GroupIterator_Destroy(Iterator);

        JsonCloseArray(False);  // primitives
        JsonCloseObject(not IsLastComp);  // footprint
    end;

    JsonCloseArray(False);  // footprints
    JsonCloseObject(False);  // root

    JsonEnd(JsonPath);
end;

{==============================================================================
  SCHEMATIC PRIMITIVE CREATORS
==============================================================================}

procedure CreateSchPin(Comp: ISch_Component; Name, Designator: String;
    X, Y: Integer; Orientation: TRotationBy90; Electrical: TPinElectrical);
var
    Pin: ISch_Pin;
begin
    Pin := SchServer.SchObjectFactory(ePin, eCreate_Default);
    if Pin = nil then Exit;

    Pin.Name := Name;
    Pin.Designator := Designator;
    Pin.Location := Point(MilsToCoord(X), MilsToCoord(Y));
    Pin.Orientation := Orientation;
    Pin.Electrical := Electrical;
    Pin.PinLength := MilsToCoord(300);
    Pin.OwnerPartId := 1;

    Comp.AddSchObject(Pin);
    SchServer.RobotManager.SendMessage(Comp.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Pin.I_ObjectAddress);
end;

procedure CreateSchPinEx(Comp: ISch_Component; Name, Designator: String;
    X, Y: Integer; Orientation: TRotationBy90; Electrical: TPinElectrical;
    IsHidden: Boolean);
var
    Pin: ISch_Pin;
begin
    Pin := SchServer.SchObjectFactory(ePin, eCreate_Default);
    if Pin = nil then Exit;

    Pin.Name := Name;
    Pin.Designator := Designator;
    Pin.Location := Point(MilsToCoord(X), MilsToCoord(Y));
    Pin.Orientation := Orientation;
    Pin.Electrical := Electrical;
    Pin.PinLength := MilsToCoord(300);
    Pin.IsHidden := IsHidden;
    Pin.OwnerPartId := 1;

    Comp.AddSchObject(Pin);
    SchServer.RobotManager.SendMessage(Comp.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Pin.I_ObjectAddress);
end;

procedure CreateSchPinWithSymbol(Comp: ISch_Component; Name, Designator: String;
    X, Y: Integer; Orientation: TRotationBy90; Electrical: TPinElectrical;
    Symbol: TPinSymbol);
var
    Pin: ISch_Pin;
begin
    Pin := SchServer.SchObjectFactory(ePin, eCreate_Default);
    if Pin = nil then Exit;

    Pin.Name := Name;
    Pin.Designator := Designator;
    Pin.Location := Point(MilsToCoord(X), MilsToCoord(Y));
    Pin.Orientation := Orientation;
    Pin.Electrical := Electrical;
    Pin.PinLength := MilsToCoord(300);
    Pin.Symbol_Inner := Symbol;
    Pin.OwnerPartId := 1;

    Comp.AddSchObject(Pin);
    SchServer.RobotManager.SendMessage(Comp.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Pin.I_ObjectAddress);
end;

procedure CreateSchLine(Owner: ISch_BasicContainer; X1, Y1, X2, Y2: Integer;
    LineWidth: TSize);
var
    Line: ISch_Line;
begin
    Line := SchServer.SchObjectFactory(eLine, eCreate_Default);
    if Line = nil then Exit;

    Line.Location := Point(MilsToCoord(X1), MilsToCoord(Y1));
    Line.Corner := Point(MilsToCoord(X2), MilsToCoord(Y2));
    Line.Color := $000000;
    Line.LineWidth := LineWidth;
    Line.OwnerPartId := 1;

    Owner.AddSchObject(Line);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Line.I_ObjectAddress);
end;

procedure CreateSchRectangle(Owner: ISch_BasicContainer; X1, Y1, X2, Y2: Integer;
    LineWidth: TSize; IsSolid: Boolean);
var
    Rect: ISch_Rectangle;
begin
    Rect := SchServer.SchObjectFactory(eRectangle, eCreate_Default);
    if Rect = nil then Exit;

    Rect.Location := Point(MilsToCoord(X1), MilsToCoord(Y1));
    Rect.Corner := Point(MilsToCoord(X2), MilsToCoord(Y2));
    Rect.Color := $000000;
    Rect.AreaColor := $FFFFFF;
    Rect.LineWidth := LineWidth;
    Rect.IsSolid := IsSolid;
    Rect.OwnerPartId := 1;

    Owner.AddSchObject(Rect);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Rect.I_ObjectAddress);
end;

procedure CreateSchArc(Owner: ISch_BasicContainer; X, Y, Radius: Integer;
    StartAngle, EndAngle: Double; LineWidth: TSize);
var
    Arc: ISch_Arc;
begin
    Arc := SchServer.SchObjectFactory(eArc, eCreate_Default);
    if Arc = nil then Exit;

    Arc.Location := Point(MilsToCoord(X), MilsToCoord(Y));
    Arc.Radius := MilsToCoord(Radius);
    Arc.StartAngle := StartAngle;
    Arc.EndAngle := EndAngle;
    Arc.Color := $000000;
    Arc.LineWidth := LineWidth;
    Arc.OwnerPartId := 1;

    Owner.AddSchObject(Arc);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Arc.I_ObjectAddress);
end;

procedure CreateSchTriangle(Owner: ISch_BasicContainer;
    X1, Y1, X2, Y2, X3, Y3: Integer; IsSolid: Boolean);
var
    Polygon: ISch_Polygon;
begin
    Polygon := SchServer.SchObjectFactory(ePolygon, eCreate_Default);
    if Polygon = nil then Exit;

    Polygon.VerticesCount := 3;
    Polygon.Vertex[1] := Point(MilsToCoord(X1), MilsToCoord(Y1));
    Polygon.Vertex[2] := Point(MilsToCoord(X2), MilsToCoord(Y2));
    Polygon.Vertex[3] := Point(MilsToCoord(X3), MilsToCoord(Y3));

    Polygon.Color := $000000;
    Polygon.AreaColor := $FFFFFF;
    Polygon.IsSolid := IsSolid;
    Polygon.OwnerPartId := 1;

    Owner.AddSchObject(Polygon);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Polygon.I_ObjectAddress);
end;

procedure CreateSchLabel(Owner: ISch_BasicContainer; X, Y: Integer;
    Text: String; FontID: TFontID);
var
    Lbl: ISch_Label;
begin
    Lbl := SchServer.SchObjectFactory(eLabel, eCreate_Default);
    if Lbl = nil then Exit;

    Lbl.Location := Point(MilsToCoord(X), MilsToCoord(Y));
    Lbl.Text := Text;
    Lbl.FontID := FontID;
    Lbl.Color := $000000;
    Lbl.OwnerPartId := 1;

    Owner.AddSchObject(Lbl);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Lbl.I_ObjectAddress);
end;

procedure CreateSchEllipse(Owner: ISch_BasicContainer; X, Y, RadiusX, RadiusY: Integer;
    LineWidth: TSize; IsSolid: Boolean);
var
    Ellipse: ISch_Ellipse;
begin
    Ellipse := SchServer.SchObjectFactory(eEllipse, eCreate_Default);
    if Ellipse = nil then Exit;

    Ellipse.Location := Point(MilsToCoord(X), MilsToCoord(Y));
    Ellipse.Radius := MilsToCoord(RadiusX);
    Ellipse.SecondaryRadius := MilsToCoord(RadiusY);
    Ellipse.Color := $000000;
    Ellipse.AreaColor := $FFFFFF;
    Ellipse.LineWidth := LineWidth;
    Ellipse.IsSolid := IsSolid;
    Ellipse.OwnerPartId := 1;

    Owner.AddSchObject(Ellipse);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Ellipse.I_ObjectAddress);
end;

procedure CreateSchCircle(Owner: ISch_BasicContainer; X, Y, Radius: Integer;
    LineWidth: TSize; IsSolid: Boolean);
begin
    // A circle is just an ellipse with equal radii
    CreateSchEllipse(Owner, X, Y, Radius, Radius, LineWidth, IsSolid);
end;

procedure CreateSchRoundRectangle(Owner: ISch_BasicContainer; X1, Y1, X2, Y2: Integer;
    CornerXRadius, CornerYRadius: Integer; LineWidth: TSize; IsSolid: Boolean);
var
    RoundRect: ISch_RoundRectangle;
begin
    RoundRect := SchServer.SchObjectFactory(eRoundRectangle, eCreate_Default);
    if RoundRect = nil then Exit;

    RoundRect.Location := Point(MilsToCoord(X1), MilsToCoord(Y1));
    RoundRect.Corner := Point(MilsToCoord(X2), MilsToCoord(Y2));
    RoundRect.CornerXRadius := MilsToCoord(CornerXRadius);
    RoundRect.CornerYRadius := MilsToCoord(CornerYRadius);
    RoundRect.Color := $000000;
    RoundRect.AreaColor := $FFFFFF;
    RoundRect.LineWidth := LineWidth;
    RoundRect.IsSolid := IsSolid;
    RoundRect.OwnerPartId := 1;

    Owner.AddSchObject(RoundRect);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, RoundRect.I_ObjectAddress);
end;

procedure CreateSchTextFrame(Owner: ISch_BasicContainer; X1, Y1, X2, Y2: Integer;
    Text: String; FontID: TFontID; ShowBorder: Boolean);
var
    TextFrame: ISch_TextFrame;
begin
    TextFrame := SchServer.SchObjectFactory(eTextFrame, eCreate_Default);
    if TextFrame = nil then Exit;

    TextFrame.Location := Point(MilsToCoord(X1), MilsToCoord(Y1));
    TextFrame.Corner := Point(MilsToCoord(X2), MilsToCoord(Y2));
    TextFrame.Text := Text;
    TextFrame.FontID := FontID;
    TextFrame.Color := $000000;
    TextFrame.AreaColor := $FFFFFF;
    TextFrame.ShowBorder := ShowBorder;
    TextFrame.IsSolid := False;
    TextFrame.OwnerPartId := 1;

    Owner.AddSchObject(TextFrame);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, TextFrame.I_ObjectAddress);
end;

// Creates a polyline with 4 vertices (DelphiScript doesn't support array parameters)
procedure CreateSchPolyline4(Owner: ISch_BasicContainer;
    X1, Y1, X2, Y2, X3, Y3, X4, Y4: Integer;
    LineWidth: TSize; IsSolid: Boolean);
var
    Polyline: ISch_Polyline;
begin
    Polyline := SchServer.SchObjectFactory(ePolyline, eCreate_Default);
    if Polyline = nil then Exit;

    Polyline.VerticesCount := 4;
    Polyline.Vertex[1] := Point(MilsToCoord(X1), MilsToCoord(Y1));
    Polyline.Vertex[2] := Point(MilsToCoord(X2), MilsToCoord(Y2));
    Polyline.Vertex[3] := Point(MilsToCoord(X3), MilsToCoord(Y3));
    Polyline.Vertex[4] := Point(MilsToCoord(X4), MilsToCoord(Y4));
    Polyline.Color := $000000;
    Polyline.LineWidth := LineWidth;
    Polyline.OwnerPartId := 1;

    Owner.AddSchObject(Polyline);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Polyline.I_ObjectAddress);
end;

// Creates a closed polygon with 4 vertices
procedure CreateSchPolygon4(Owner: ISch_BasicContainer;
    X1, Y1, X2, Y2, X3, Y3, X4, Y4: Integer;
    LineWidth: TSize; IsSolid: Boolean);
var
    Polygon: ISch_Polygon;
begin
    Polygon := SchServer.SchObjectFactory(ePolygon, eCreate_Default);
    if Polygon = nil then Exit;

    Polygon.VerticesCount := 4;
    Polygon.Vertex[1] := Point(MilsToCoord(X1), MilsToCoord(Y1));
    Polygon.Vertex[2] := Point(MilsToCoord(X2), MilsToCoord(Y2));
    Polygon.Vertex[3] := Point(MilsToCoord(X3), MilsToCoord(Y3));
    Polygon.Vertex[4] := Point(MilsToCoord(X4), MilsToCoord(Y4));
    Polygon.Color := $000000;
    Polygon.AreaColor := $FFFFFF;
    Polygon.LineWidth := LineWidth;
    Polygon.IsSolid := IsSolid;
    Polygon.OwnerPartId := 1;

    Owner.AddSchObject(Polygon);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Polygon.I_ObjectAddress);
end;

// Creates a bezier curve with 4 control points
procedure CreateSchBezier4(Owner: ISch_BasicContainer;
    X1, Y1, X2, Y2, X3, Y3, X4, Y4: Integer;
    LineWidth: TSize);
var
    Bezier: ISch_Bezier;
begin
    Bezier := SchServer.SchObjectFactory(eBezier, eCreate_Default);
    if Bezier = nil then Exit;

    Bezier.VerticesCount := 4;
    Bezier.Vertex[1] := Point(MilsToCoord(X1), MilsToCoord(Y1));
    Bezier.Vertex[2] := Point(MilsToCoord(X2), MilsToCoord(Y2));
    Bezier.Vertex[3] := Point(MilsToCoord(X3), MilsToCoord(Y3));
    Bezier.Vertex[4] := Point(MilsToCoord(X4), MilsToCoord(Y4));
    Bezier.Color := $000000;
    Bezier.LineWidth := LineWidth;
    Bezier.OwnerPartId := 1;

    Owner.AddSchObject(Bezier);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Bezier.I_ObjectAddress);
end;

{==============================================================================
  ENHANCED HELPER PROCEDURES WITH FULL PROPERTY SUPPORT
==============================================================================}

// Enhanced pin creation with all properties
procedure CreateSchPinFull(Comp: ISch_Component; Name, Designator: String;
    X, Y: Integer; Orientation: TRotationBy90; Electrical: TPinElectrical;
    PinLength: Integer; IsHidden: Boolean; ShowName, ShowDesignator: Boolean;
    NameColor, DesignatorColor: TColor; NameFontID, DesignatorFontID: TFontID;
    SymbolInner, SymbolOuter: TIeeeSymbol; SymbolLineWidth: TSize;
    HiddenNetName, Description, DefaultValue: String);
var
    Pin: ISch_Pin;
begin
    Pin := SchServer.SchObjectFactory(ePin, eCreate_Default);
    if Pin = nil then Exit;

    Pin.Name := Name;
    Pin.Designator := Designator;
    Pin.Location := Point(MilsToCoord(X), MilsToCoord(Y));
    Pin.Orientation := Orientation;
    Pin.Electrical := Electrical;
    Pin.PinLength := MilsToCoord(PinLength);
    Pin.IsHidden := IsHidden;
    Pin.ShowName := ShowName;
    Pin.ShowDesignator := ShowDesignator;
    Pin.Name_CustomColor := NameColor;
    Pin.Designator_CustomColor := DesignatorColor;
    Pin.Name_CustomFontID := NameFontID;
    Pin.Designator_CustomFontID := DesignatorFontID;
    Pin.Symbol_Inner := SymbolInner;
    Pin.Symbol_Outer := SymbolOuter;
    Pin.Symbol_LineWidth := SymbolLineWidth;
    Pin.HiddenNetName := HiddenNetName;
    Pin.Description := Description;
    Pin.DefaultValue := DefaultValue;
    Pin.OwnerPartId := 1;

    Comp.AddSchObject(Pin);
    SchServer.RobotManager.SendMessage(Comp.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Pin.I_ObjectAddress);
end;

// Enhanced line creation with color and style
procedure CreateSchLineStyled(Owner: ISch_BasicContainer; X1, Y1, X2, Y2: Integer;
    LineWidth: TSize; LineStyle: TLineStyle; Color: TColor);
var
    Line: ISch_Line;
begin
    Line := SchServer.SchObjectFactory(eLine, eCreate_Default);
    if Line = nil then Exit;

    Line.Location := Point(MilsToCoord(X1), MilsToCoord(Y1));
    Line.Corner := Point(MilsToCoord(X2), MilsToCoord(Y2));
    Line.Color := Color;
    Line.LineWidth := LineWidth;
    Line.LineStyle := LineStyle;
    Line.OwnerPartId := 1;

    Owner.AddSchObject(Line);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Line.I_ObjectAddress);
end;

// Enhanced rectangle with all properties
procedure CreateSchRectFull(Owner: ISch_BasicContainer; X1, Y1, X2, Y2: Integer;
    LineWidth: TSize; LineStyle: TLineStyle; IsSolid, Transparent: Boolean;
    Color, AreaColor: TColor);
var
    Rect: ISch_Rectangle;
begin
    Rect := SchServer.SchObjectFactory(eRectangle, eCreate_Default);
    if Rect = nil then Exit;

    Rect.Location := Point(MilsToCoord(X1), MilsToCoord(Y1));
    Rect.Corner := Point(MilsToCoord(X2), MilsToCoord(Y2));
    Rect.Color := Color;
    Rect.AreaColor := AreaColor;
    Rect.LineWidth := LineWidth;
    Rect.LineStyle := LineStyle;
    Rect.IsSolid := IsSolid;
    Rect.Transparent := Transparent;
    Rect.OwnerPartId := 1;

    Owner.AddSchObject(Rect);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Rect.I_ObjectAddress);
end;

// Enhanced label with all properties
procedure CreateSchLabelFull(Owner: ISch_BasicContainer; X, Y: Integer;
    Text: String; FontID: TFontID; Color: TColor; Orientation: TRotationBy90;
    Justification: TTextJustification; IsMirrored: Boolean);
var
    Lbl: ISch_Label;
begin
    Lbl := SchServer.SchObjectFactory(eLabel, eCreate_Default);
    if Lbl = nil then Exit;

    Lbl.Location := Point(MilsToCoord(X), MilsToCoord(Y));
    Lbl.Text := Text;
    Lbl.FontID := FontID;
    Lbl.Color := Color;
    Lbl.Orientation := Orientation;
    Lbl.Justification := Justification;
    Lbl.IsMirrored := IsMirrored;
    Lbl.OwnerPartId := 1;

    Owner.AddSchObject(Lbl);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Lbl.I_ObjectAddress);
end;

// Enhanced arc with color
procedure CreateSchArcStyled(Owner: ISch_BasicContainer; X, Y, Radius: Integer;
    StartAngle, EndAngle: Double; LineWidth: TSize; Color: TColor);
var
    Arc: ISch_Arc;
begin
    Arc := SchServer.SchObjectFactory(eArc, eCreate_Default);
    if Arc = nil then Exit;

    Arc.Location := Point(MilsToCoord(X), MilsToCoord(Y));
    Arc.Radius := MilsToCoord(Radius);
    Arc.StartAngle := StartAngle;
    Arc.EndAngle := EndAngle;
    Arc.Color := Color;
    Arc.LineWidth := LineWidth;
    Arc.OwnerPartId := 1;

    Owner.AddSchObject(Arc);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Arc.I_ObjectAddress);
end;

// Enhanced ellipse with all properties
procedure CreateSchEllipseFull(Owner: ISch_BasicContainer; X, Y, RadiusX, RadiusY: Integer;
    LineWidth: TSize; IsSolid, Transparent: Boolean; Color, AreaColor: TColor);
var
    Ellipse: ISch_Ellipse;
begin
    Ellipse := SchServer.SchObjectFactory(eEllipse, eCreate_Default);
    if Ellipse = nil then Exit;

    Ellipse.Location := Point(MilsToCoord(X), MilsToCoord(Y));
    Ellipse.Radius := MilsToCoord(RadiusX);
    Ellipse.SecondaryRadius := MilsToCoord(RadiusY);
    Ellipse.Color := Color;
    Ellipse.AreaColor := AreaColor;
    Ellipse.LineWidth := LineWidth;
    Ellipse.IsSolid := IsSolid;
    Ellipse.Transparent := Transparent;
    Ellipse.OwnerPartId := 1;

    Owner.AddSchObject(Ellipse);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Ellipse.I_ObjectAddress);
end;

// Enhanced polyline with arrows and styles
procedure CreateSchPolylineStyled(Owner: ISch_BasicContainer;
    X1, Y1, X2, Y2, X3, Y3, X4, Y4: Integer;
    LineWidth: TSize; LineStyle: TLineStyle; Color: TColor;
    StartShape, EndShape: TLineShape; ShapeSize: TSize);
var
    Polyline: ISch_Polyline;
begin
    Polyline := SchServer.SchObjectFactory(ePolyline, eCreate_Default);
    if Polyline = nil then Exit;

    Polyline.VerticesCount := 4;
    Polyline.Vertex[1] := Point(MilsToCoord(X1), MilsToCoord(Y1));
    Polyline.Vertex[2] := Point(MilsToCoord(X2), MilsToCoord(Y2));
    Polyline.Vertex[3] := Point(MilsToCoord(X3), MilsToCoord(Y3));
    Polyline.Vertex[4] := Point(MilsToCoord(X4), MilsToCoord(Y4));
    Polyline.Color := Color;
    Polyline.LineWidth := LineWidth;
    Polyline.LineStyle := LineStyle;
    Polyline.StartLineShape := StartShape;
    Polyline.EndLineShape := EndShape;
    Polyline.LineShapeSize := ShapeSize;
    Polyline.OwnerPartId := 1;

    Owner.AddSchObject(Polyline);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Polyline.I_ObjectAddress);
end;

// Enhanced round rectangle with all properties
procedure CreateSchRoundRectFull(Owner: ISch_BasicContainer; X1, Y1, X2, Y2: Integer;
    CornerXRadius, CornerYRadius: Integer; LineWidth: TSize; LineStyle: TLineStyle;
    IsSolid, Transparent: Boolean; Color, AreaColor: TColor);
var
    RoundRect: ISch_RoundRectangle;
begin
    RoundRect := SchServer.SchObjectFactory(eRoundRectangle, eCreate_Default);
    if RoundRect = nil then Exit;

    RoundRect.Location := Point(MilsToCoord(X1), MilsToCoord(Y1));
    RoundRect.Corner := Point(MilsToCoord(X2), MilsToCoord(Y2));
    RoundRect.CornerXRadius := MilsToCoord(CornerXRadius);
    RoundRect.CornerYRadius := MilsToCoord(CornerYRadius);
    RoundRect.Color := Color;
    RoundRect.AreaColor := AreaColor;
    RoundRect.LineWidth := LineWidth;
    RoundRect.LineStyle := LineStyle;
    RoundRect.IsSolid := IsSolid;
    RoundRect.Transparent := Transparent;
    RoundRect.OwnerPartId := 1;

    Owner.AddSchObject(RoundRect);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, RoundRect.I_ObjectAddress);
end;

// Enhanced text frame with all properties
procedure CreateSchTextFrameFull(Owner: ISch_BasicContainer; X1, Y1, X2, Y2: Integer;
    Text: String; FontID: TFontID; Alignment: THorizontalAlign;
    ShowBorder, IsSolid, Transparent, ClipToRect, WordWrap: Boolean;
    Color, AreaColor, TextColor: TColor; LineWidth: TSize; LineStyle: TLineStyle;
    TextMargin: Integer);
var
    TextFrame: ISch_TextFrame;
begin
    TextFrame := SchServer.SchObjectFactory(eTextFrame, eCreate_Default);
    if TextFrame = nil then Exit;

    TextFrame.Location := Point(MilsToCoord(X1), MilsToCoord(Y1));
    TextFrame.Corner := Point(MilsToCoord(X2), MilsToCoord(Y2));
    TextFrame.Text := Text;
    TextFrame.FontID := FontID;
    TextFrame.Alignment := Alignment;
    TextFrame.ShowBorder := ShowBorder;
    TextFrame.IsSolid := IsSolid;
    TextFrame.Transparent := Transparent;
    TextFrame.ClipToRect := ClipToRect;
    TextFrame.WordWrap := WordWrap;
    TextFrame.Color := Color;
    TextFrame.AreaColor := AreaColor;
    TextFrame.TextColor := TextColor;
    TextFrame.LineWidth := LineWidth;
    TextFrame.LineStyle := LineStyle;
    TextFrame.TextMargin := MilsToCoord(TextMargin);
    TextFrame.OwnerPartId := 1;

    Owner.AddSchObject(TextFrame);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, TextFrame.I_ObjectAddress);
end;

// Enhanced polygon with colors
procedure CreateSchPolygonFull(Owner: ISch_BasicContainer;
    X1, Y1, X2, Y2, X3, Y3: Integer; IsSolid: Boolean; Color, AreaColor: TColor);
var
    Polygon: ISch_Polygon;
begin
    Polygon := SchServer.SchObjectFactory(ePolygon, eCreate_Default);
    if Polygon = nil then Exit;

    Polygon.VerticesCount := 3;
    Polygon.Vertex[1] := Point(MilsToCoord(X1), MilsToCoord(Y1));
    Polygon.Vertex[2] := Point(MilsToCoord(X2), MilsToCoord(Y2));
    Polygon.Vertex[3] := Point(MilsToCoord(X3), MilsToCoord(Y3));
    Polygon.Color := Color;
    Polygon.AreaColor := AreaColor;
    Polygon.IsSolid := IsSolid;
    Polygon.OwnerPartId := 1;

    Owner.AddSchObject(Polygon);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Polygon.I_ObjectAddress);
end;

// Enhanced pie (arc with fill)
procedure CreateSchPieFull(Owner: ISch_BasicContainer; X, Y, Radius: Integer;
    StartAngle, EndAngle: Double; LineWidth: TSize; IsSolid: Boolean;
    Color, AreaColor: TColor);
var
    Pie: ISch_Pie;
begin
    Pie := SchServer.SchObjectFactory(ePie, eCreate_Default);
    if Pie = nil then Exit;

    Pie.Location := Point(MilsToCoord(X), MilsToCoord(Y));
    Pie.Radius := MilsToCoord(Radius);
    Pie.StartAngle := StartAngle;
    Pie.EndAngle := EndAngle;
    Pie.Color := Color;
    Pie.AreaColor := AreaColor;
    Pie.LineWidth := LineWidth;
    Pie.IsSolid := IsSolid;
    Pie.OwnerPartId := 1;

    Owner.AddSchObject(Pie);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Pie.I_ObjectAddress);
end;

// Enhanced elliptical arc
procedure CreateSchEllipticalArc(Owner: ISch_BasicContainer; X, Y, RadiusX, RadiusY: Integer;
    StartAngle, EndAngle: Double; LineWidth: TSize; Color: TColor);
var
    EArc: ISch_EllipticalArc;
begin
    EArc := SchServer.SchObjectFactory(eEllipticalArc, eCreate_Default);
    if EArc = nil then Exit;

    EArc.Location := Point(MilsToCoord(X), MilsToCoord(Y));
    EArc.Radius := MilsToCoord(RadiusX);
    EArc.SecondaryRadius := MilsToCoord(RadiusY);
    EArc.StartAngle := StartAngle;
    EArc.EndAngle := EndAngle;
    EArc.Color := Color;
    EArc.LineWidth := LineWidth;
    EArc.OwnerPartId := 1;

    Owner.AddSchObject(EArc);
    SchServer.RobotManager.SendMessage(Owner.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, EArc.I_ObjectAddress);
end;

{==============================================================================
  SCHEMATIC LIBRARY GENERATOR
==============================================================================}

// Remove the default empty component that Altium creates in new libraries
procedure RemoveDefaultSchLibComponent(SchLib: ISch_Lib);
var
    Iterator: ISch_Iterator;
    Comp: ISch_Component;
begin
    if SchLib = nil then Exit;

    Iterator := SchLib.SchLibIterator_Create;
    Iterator.AddFilter_ObjectSet(MkSet(eSchComponent));
    Comp := Iterator.FirstSchObject;
    if Comp <> nil then
        SchLib.RemoveSchComponent(Comp);
    SchLib.SchIterator_Destroy(Iterator);
end;

procedure GenerateSchLibTestSymbols(SchLib: ISch_Lib);
var
    Comp: ISch_Component;
    SchDoc: ISch_Document;
begin
    if SchLib = nil then Exit;

    SchDoc := SchServer.GetCurrentSchDocument;
    if SchDoc = nil then Exit;

    SchServer.ProcessControl.PreProcess(SchDoc, '');

    // === Symbol 1: RESISTOR - Basic 2-pin passive ===
    // All coordinates on 100 mil grid, pins 300 mil long
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'R?';
    Comp.Comment.Text := 'Resistor';
    Comp.LibReference := 'RESISTOR';
    Comp.ComponentDescription := 'Basic 2-pin resistor symbol';

    // Pins at body edges (300 mil pin length extends outward)
    CreateSchPin(Comp, 'P1', '1', -200, 0, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'P2', '2', 200, 0, eRotate0, eElectricPassive);

    // Draw resistor body (rectangle style, 400x100 mils)
    CreateSchRectangle(Comp, -200, -100, 200, 100, eSmall, False);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 2: CAPACITOR - Capacitor symbol ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'C?';
    Comp.Comment.Text := 'Capacitor';
    Comp.LibReference := 'CAPACITOR';
    Comp.ComponentDescription := 'Basic capacitor symbol';

    CreateSchPin(Comp, 'P1', '1', -200, 0, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'P2', '2', 200, 0, eRotate0, eElectricPassive);

    // Draw capacitor plates (two vertical lines, 200 mils tall, 100 mils apart)
    CreateSchLine(Comp, -100, -100, -100, 100, eMedium);
    CreateSchLine(Comp, 100, -100, 100, 100, eMedium);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 3: OPAMP - Operational amplifier ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'U?';
    Comp.Comment.Text := 'OpAmp';
    Comp.LibReference := 'OPAMP';
    Comp.ComponentDescription := 'Operational amplifier symbol';

    // Pins on 100 mil grid
    CreateSchPin(Comp, 'IN+', '1', -400, 100, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'IN-', '2', -400, -100, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'OUT', '3', 400, 0, eRotate0, eElectricOutput);
    CreateSchPin(Comp, 'V+', '4', 0, 300, eRotate270, eElectricPower);
    CreateSchPin(Comp, 'V-', '5', 0, -300, eRotate90, eElectricPower);

    // Draw triangle body (600 mils wide, 400 mils tall)
    CreateSchTriangle(Comp, -300, 300, -300, -300, 300, 0, False);

    // Plus and minus signs inside triangle
    CreateSchLabel(Comp, -200, 100, '+', 1);
    CreateSchLabel(Comp, -200, -100, '-', 1);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 4: CONNECTOR_4 - 4-pin connector ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'J?';
    Comp.Comment.Text := 'Connector';
    Comp.LibReference := 'CONNECTOR_4';
    Comp.ComponentDescription := '4-pin connector symbol';

    // Pins spaced 100 mils apart vertically
    CreateSchPin(Comp, 'PIN1', '1', -300, 200, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'PIN2', '2', -300, 100, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'PIN3', '3', -300, 0, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'PIN4', '4', -300, -100, eRotate180, eElectricPassive);

    // Draw connector body (200x400 mils)
    CreateSchRectangle(Comp, -300, -200, 0, 300, eSmall, False);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 5: LED - Light emitting diode ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'D?';
    Comp.Comment.Text := 'LED';
    Comp.LibReference := 'LED';
    Comp.ComponentDescription := 'LED symbol with arrows';

    CreateSchPin(Comp, 'A', '1', -300, 0, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'K', '2', 300, 0, eRotate0, eElectricPassive);

    // Diode triangle (200 mils wide)
    CreateSchTriangle(Comp, -100, 100, -100, -100, 100, 0, True);

    // Cathode bar (200 mils tall)
    CreateSchLine(Comp, 100, -100, 100, 100, eMedium);

    // Light arrows (100 mil lines)
    CreateSchLine(Comp, 0, 100, 100, 200, eSmall);
    CreateSchLine(Comp, 100, 100, 200, 200, eSmall);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 6: TRANSISTOR_NPN - NPN transistor ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'Q?';
    Comp.Comment.Text := 'NPN';
    Comp.LibReference := 'TRANSISTOR_NPN';
    Comp.ComponentDescription := 'NPN transistor symbol';

    CreateSchPin(Comp, 'B', '1', -300, 0, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'C', '2', 100, 300, eRotate90, eElectricOutput);
    CreateSchPin(Comp, 'E', '3', 100, -300, eRotate270, eElectricOutput);

    // Base vertical line (200 mils)
    CreateSchLine(Comp, -100, -100, -100, 100, eMedium);
    // Collector line
    CreateSchLine(Comp, -100, 100, 100, 200, eSmall);
    // Emitter line
    CreateSchLine(Comp, -100, -100, 100, -200, eSmall);
    // Circle for body (radius 150 mils)
    CreateSchArc(Comp, 0, 0, 200, 0, 360, eSmall);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 7: CIRCLE_FILLED - Filled circle test ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'Circle Filled';
    Comp.LibReference := 'CIRCLE_FILLED';
    Comp.ComponentDescription := 'Filled circle using ellipse with equal radii';

    // A circle is an ellipse with equal radii
    CreateSchCircle(Comp, 0, 0, 200, eMedium, True);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 8: CIRCLE_OUTLINE - Outline circle test ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'Circle Outline';
    Comp.LibReference := 'CIRCLE_OUTLINE';
    Comp.ComponentDescription := 'Outline circle using ellipse with equal radii';

    CreateSchCircle(Comp, 0, 0, 200, eMedium, False);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 9: ELLIPSE_TEST - Ellipse with different radii ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'Ellipse';
    Comp.LibReference := 'ELLIPSE_TEST';
    Comp.ComponentDescription := 'Ellipse with different X and Y radii';

    CreateSchEllipse(Comp, 0, 0, 300, 150, eMedium, False);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 10: ROUNDRECT_TEST - Rounded rectangle test ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'RoundRect';
    Comp.LibReference := 'ROUNDRECT_TEST';
    Comp.ComponentDescription := 'Rounded rectangle test';

    CreateSchRoundRectangle(Comp, -200, -100, 200, 100, 50, 50, eMedium, False);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 11: TEXTFRAME_TEST - Text frame test ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'TextFrame';
    Comp.LibReference := 'TEXTFRAME_TEST';
    Comp.ComponentDescription := 'Text frame test';

    CreateSchTextFrame(Comp, -200, -100, 200, 100, 'Test Frame', 1, True);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 12: ARC_FULL - Full 360 degree arc (circle) ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'Arc Full';
    Comp.LibReference := 'ARC_FULL';
    Comp.ComponentDescription := 'Full 360 degree arc (circle)';

    CreateSchArc(Comp, 0, 0, 200, 0, 360, eMedium);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 13: POLYLINE_TEST - Open polyline with 4 vertices ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'Polyline';
    Comp.LibReference := 'POLYLINE_TEST';
    Comp.ComponentDescription := 'Open polyline with 4 vertices';

    // A zigzag pattern
    CreateSchPolyline4(Comp, -200, -100, -100, 100, 100, -100, 200, 100, eMedium, False);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 14: POLYGON_TEST - Closed polygon with 4 vertices ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'Polygon';
    Comp.LibReference := 'POLYGON_TEST';
    Comp.ComponentDescription := 'Closed polygon (diamond shape)';

    // A diamond shape
    CreateSchPolygon4(Comp, 0, -150, -150, 0, 0, 150, 150, 0, eMedium, True);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    {==========================================================================
      NEW EXTENDED SCHEMATIC SYMBOLS - Additional coverage
    ==========================================================================}

    // === Symbol 15: INDUCTOR - Inductor symbol ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'L?';
    Comp.Comment.Text := 'Inductor';
    Comp.LibReference := 'INDUCTOR';
    Comp.ComponentDescription := 'Basic inductor symbol';

    CreateSchPin(Comp, 'P1', '1', -300, 0, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'P2', '2', 300, 0, eRotate0, eElectricPassive);

    // Draw inductor arcs (4 bumps)
    CreateSchArc(Comp, -150, 0, 50, 0, 180, eMedium);
    CreateSchArc(Comp, -50, 0, 50, 0, 180, eMedium);
    CreateSchArc(Comp, 50, 0, 50, 0, 180, eMedium);
    CreateSchArc(Comp, 150, 0, 50, 0, 180, eMedium);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 16: DIODE - Standard diode symbol ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'D?';
    Comp.Comment.Text := 'Diode';
    Comp.LibReference := 'DIODE';
    Comp.ComponentDescription := 'Standard diode symbol';

    CreateSchPin(Comp, 'A', '1', -300, 0, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'K', '2', 300, 0, eRotate0, eElectricPassive);

    // Diode triangle
    CreateSchTriangle(Comp, -100, 100, -100, -100, 100, 0, True);
    // Cathode bar
    CreateSchLine(Comp, 100, -100, 100, 100, eMedium);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 17: ZENER - Zener diode symbol ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'D?';
    Comp.Comment.Text := 'Zener';
    Comp.LibReference := 'ZENER';
    Comp.ComponentDescription := 'Zener diode symbol';

    CreateSchPin(Comp, 'A', '1', -300, 0, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'K', '2', 300, 0, eRotate0, eElectricPassive);

    // Diode triangle
    CreateSchTriangle(Comp, -100, 100, -100, -100, 100, 0, True);
    // Zener bar (with bends)
    CreateSchLine(Comp, 100, -100, 100, 100, eMedium);
    CreateSchLine(Comp, 50, -100, 100, -100, eSmall);
    CreateSchLine(Comp, 100, 100, 150, 100, eSmall);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 18: TRANSISTOR_PNP - PNP transistor ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'Q?';
    Comp.Comment.Text := 'PNP';
    Comp.LibReference := 'TRANSISTOR_PNP';
    Comp.ComponentDescription := 'PNP transistor symbol';

    CreateSchPin(Comp, 'B', '1', -300, 0, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'C', '2', 100, -300, eRotate270, eElectricOutput);
    CreateSchPin(Comp, 'E', '3', 100, 300, eRotate90, eElectricOutput);

    // Base vertical line
    CreateSchLine(Comp, -100, -100, -100, 100, eMedium);
    // Emitter line (with arrow towards base)
    CreateSchLine(Comp, 100, 200, -100, 100, eSmall);
    // Collector line
    CreateSchLine(Comp, -100, -100, 100, -200, eSmall);
    // Circle for body
    CreateSchArc(Comp, 0, 0, 200, 0, 360, eSmall);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 19: NMOS - N-channel MOSFET ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'Q?';
    Comp.Comment.Text := 'NMOS';
    Comp.LibReference := 'NMOS';
    Comp.ComponentDescription := 'N-channel MOSFET symbol';

    CreateSchPin(Comp, 'G', '1', -300, 0, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'D', '2', 100, 300, eRotate90, eElectricOutput);
    CreateSchPin(Comp, 'S', '3', 100, -300, eRotate270, eElectricOutput);

    // Gate vertical line
    CreateSchLine(Comp, -100, -100, -100, 100, eMedium);
    // Channel line
    CreateSchLine(Comp, 0, -100, 0, 100, eMedium);
    // Drain connection
    CreateSchLine(Comp, 0, 100, 100, 100, eSmall);
    CreateSchLine(Comp, 100, 100, 100, 200, eSmall);
    // Source connection
    CreateSchLine(Comp, 0, -100, 100, -100, eSmall);
    CreateSchLine(Comp, 100, -100, 100, -200, eSmall);
    // Body connection
    CreateSchLine(Comp, 0, 0, 100, 0, eSmall);
    // Arrow on source
    CreateSchLine(Comp, 50, -100, 100, -100, eSmall);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 20: AND_GATE - AND logic gate ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'U?';
    Comp.Comment.Text := 'AND';
    Comp.LibReference := 'AND_GATE';
    Comp.ComponentDescription := '2-input AND gate symbol';

    CreateSchPin(Comp, 'A', '1', -400, 100, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'B', '2', -400, -100, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'Y', '3', 400, 0, eRotate0, eElectricOutput);

    // Left edge
    CreateSchLine(Comp, -200, -150, -200, 150, eMedium);
    // Top line
    CreateSchLine(Comp, -200, 150, 100, 150, eMedium);
    // Bottom line
    CreateSchLine(Comp, -200, -150, 100, -150, eMedium);
    // Curved front (approximated with arc)
    CreateSchArc(Comp, 100, 0, 150, 270, 180, eMedium);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 21: OR_GATE - OR logic gate ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'U?';
    Comp.Comment.Text := 'OR';
    Comp.LibReference := 'OR_GATE';
    Comp.ComponentDescription := '2-input OR gate symbol';

    CreateSchPin(Comp, 'A', '1', -400, 100, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'B', '2', -400, -100, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'Y', '3', 400, 0, eRotate0, eElectricOutput);

    // Back curve (approximated)
    CreateSchArc(Comp, -300, 0, 100, 270, 180, eMedium);
    // Top curve
    CreateSchArc(Comp, -100, 300, 350, 270, 60, eMedium);
    // Bottom curve
    CreateSchArc(Comp, -100, -300, 350, 30, 60, eMedium);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 22: NOT_GATE - Inverter gate ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'U?';
    Comp.Comment.Text := 'NOT';
    Comp.LibReference := 'NOT_GATE';
    Comp.ComponentDescription := 'Inverter gate symbol';

    CreateSchPin(Comp, 'A', '1', -400, 0, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'Y', '2', 400, 0, eRotate0, eElectricOutput);

    // Triangle body
    CreateSchTriangle(Comp, -200, 150, -200, -150, 200, 0, False);
    // Output bubble
    CreateSchCircle(Comp, 230, 0, 30, eSmall, False);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 23: CONNECTOR_8 - 8-pin connector ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'J?';
    Comp.Comment.Text := 'Connector 8';
    Comp.LibReference := 'CONNECTOR_8';
    Comp.ComponentDescription := '8-pin connector symbol';

    CreateSchPin(Comp, 'PIN1', '1', -300, 400, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'PIN2', '2', -300, 300, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'PIN3', '3', -300, 200, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'PIN4', '4', -300, 100, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'PIN5', '5', -300, 0, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'PIN6', '6', -300, -100, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'PIN7', '7', -300, -200, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'PIN8', '8', -300, -300, eRotate180, eElectricPassive);

    CreateSchRectangle(Comp, -300, -400, 0, 500, eSmall, False);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 24: CAPACITOR_POL - Polarized capacitor ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'C?';
    Comp.Comment.Text := 'Cap Pol';
    Comp.LibReference := 'CAPACITOR_POL';
    Comp.ComponentDescription := 'Polarized capacitor symbol';

    CreateSchPin(Comp, '+', '1', -200, 0, eRotate180, eElectricPassive);
    CreateSchPin(Comp, '-', '2', 200, 0, eRotate0, eElectricPassive);

    // Positive plate (straight line)
    CreateSchLine(Comp, -100, -100, -100, 100, eMedium);
    // Negative plate (curved)
    CreateSchArc(Comp, 200, 0, 120, 120, 120, eMedium);
    // Plus sign
    CreateSchLabel(Comp, -150, 80, '+', 1);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 25: VARIABLE_RES - Variable resistor/potentiometer ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'R?';
    Comp.Comment.Text := 'Pot';
    Comp.LibReference := 'VARIABLE_RES';
    Comp.ComponentDescription := 'Variable resistor/potentiometer symbol';

    CreateSchPin(Comp, 'P1', '1', -200, 0, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'P2', '2', 200, 0, eRotate0, eElectricPassive);
    CreateSchPin(Comp, 'WIPER', '3', 0, -200, eRotate90, eElectricPassive);

    // Resistor body
    CreateSchRectangle(Comp, -200, -50, 200, 50, eSmall, False);
    // Arrow/wiper
    CreateSchLine(Comp, 0, -100, 0, -50, eSmall);
    CreateSchLine(Comp, -30, -70, 0, -100, eSmall);
    CreateSchLine(Comp, 30, -70, 0, -100, eSmall);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 26: POWER_GND - Ground power symbol ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := '';
    Comp.Comment.Text := 'GND';
    Comp.LibReference := 'POWER_GND';
    Comp.ComponentDescription := 'Ground power symbol';

    CreateSchPin(Comp, 'GND', '1', 0, 100, eRotate270, eElectricPower);

    // Ground symbol lines
    CreateSchLine(Comp, -100, 0, 100, 0, eMedium);
    CreateSchLine(Comp, -60, -30, 60, -30, eMedium);
    CreateSchLine(Comp, -20, -60, 20, -60, eMedium);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 27: POWER_VCC - VCC power symbol ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := '';
    Comp.Comment.Text := 'VCC';
    Comp.LibReference := 'POWER_VCC';
    Comp.ComponentDescription := 'VCC power symbol';

    CreateSchPin(Comp, 'VCC', '1', 0, -100, eRotate90, eElectricPower);

    // VCC symbol (bar with arrow)
    CreateSchLine(Comp, -100, 0, 100, 0, eMedium);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 28: MULTIPART_2 - Two-part component ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'U?';
    Comp.Comment.Text := 'MultiPart';
    Comp.LibReference := 'MULTIPART_2';
    Comp.ComponentDescription := 'Two-part component (e.g., dual op-amp)';
    Comp.PartCount := 2;

    // Part 1 pins
    CreateSchPin(Comp, 'IN+', '1', -400, 100, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'IN-', '2', -400, -100, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'OUT', '3', 400, 0, eRotate0, eElectricOutput);

    // Part 1 body
    CreateSchTriangle(Comp, -300, 200, -300, -200, 300, 0, False);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 29: PIN_TYPES - All pin electrical types ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'U?';
    Comp.Comment.Text := 'Pin Types';
    Comp.LibReference := 'PIN_TYPES';
    Comp.ComponentDescription := 'Component with all pin electrical types';

    CreateSchPin(Comp, 'INPUT', '1', -400, 300, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'OUTPUT', '2', 400, 300, eRotate0, eElectricOutput);
    CreateSchPin(Comp, 'IO', '3', -400, 200, eRotate180, eElectricIO);
    CreateSchPin(Comp, 'PASSIVE', '4', 400, 200, eRotate0, eElectricPassive);
    CreateSchPin(Comp, 'OC', '5', -400, 100, eRotate180, eElectricOpenCollector);
    CreateSchPin(Comp, 'OE', '6', 400, 100, eRotate0, eElectricOpenEmitter);
    CreateSchPin(Comp, 'POWER', '7', -400, 0, eRotate180, eElectricPower);
    CreateSchPin(Comp, 'HIZIMP', '8', 400, 0, eRotate0, eElectricHiZ);

    CreateSchRectangle(Comp, -400, -100, 400, 400, eSmall, False);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 30: BEZIER_TEST - Bezier curve test ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'Bezier';
    Comp.LibReference := 'BEZIER_TEST';
    Comp.ComponentDescription := 'Bezier curve test';

    CreateSchBezier4(Comp, -200, 0, -100, 150, 100, -150, 200, 0, eMedium);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 31: XOR_GATE - XOR gate ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'U?';
    Comp.Comment.Text := 'XOR';
    Comp.LibReference := 'XOR_GATE';
    Comp.ComponentDescription := 'XOR gate symbol';

    CreateSchPin(Comp, 'A', '1', -400, 100, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'B', '2', -400, -100, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'Y', '3', 400, 0, eRotate0, eElectricOutput);
    CreateSchArc(Comp, -350, 0, 100, 270, 180, eMedium);
    CreateSchArc(Comp, -300, 0, 100, 270, 180, eMedium);
    CreateSchArc(Comp, -100, 300, 350, 270, 60, eMedium);
    CreateSchArc(Comp, -100, -300, 350, 30, 60, eMedium);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 32: NAND_GATE - NAND gate ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'U?';
    Comp.Comment.Text := 'NAND';
    Comp.LibReference := 'NAND_GATE';
    Comp.ComponentDescription := 'NAND gate symbol';

    CreateSchPin(Comp, 'A', '1', -400, 100, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'B', '2', -400, -100, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'Y', '3', 400, 0, eRotate0, eElectricOutput);
    CreateSchLine(Comp, -200, -150, -200, 150, eMedium);
    CreateSchLine(Comp, -200, 150, 100, 150, eMedium);
    CreateSchLine(Comp, -200, -150, 100, -150, eMedium);
    CreateSchArc(Comp, 100, 0, 150, 270, 180, eMedium);
    CreateSchCircle(Comp, 280, 0, 30, eSmall, False);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 33: DFF - D Flip-Flop ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'U?';
    Comp.Comment.Text := 'DFF';
    Comp.LibReference := 'DFF';
    Comp.ComponentDescription := 'D Flip-Flop symbol';

    CreateSchPin(Comp, 'D', '1', -300, 100, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'CLK', '2', -300, 0, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'Q', '3', 300, 100, eRotate0, eElectricOutput);
    CreateSchPin(Comp, 'QN', '4', 300, -100, eRotate0, eElectricOutput);
    CreateSchPin(Comp, 'RST', '5', 0, -200, eRotate90, eElectricInput);
    CreateSchRectangle(Comp, -300, -200, 300, 200, eSmall, False);
    CreateSchLine(Comp, -300, -20, -270, 0, eSmall);
    CreateSchLine(Comp, -270, 0, -300, 20, eSmall);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 34: TIMER_555 - 555 Timer IC ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'U?';
    Comp.Comment.Text := '555';
    Comp.LibReference := 'TIMER_555';
    Comp.ComponentDescription := '555 Timer IC symbol';

    CreateSchPin(Comp, 'GND', '1', -300, -200, eRotate180, eElectricPower);
    CreateSchPin(Comp, 'TRIG', '2', -300, -100, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'OUT', '3', 300, 0, eRotate0, eElectricOutput);
    CreateSchPin(Comp, 'RESET', '4', 0, 300, eRotate270, eElectricInput);
    CreateSchPin(Comp, 'CTRL', '5', 300, -100, eRotate0, eElectricInput);
    CreateSchPin(Comp, 'THRES', '6', -300, 100, eRotate180, eElectricInput);
    CreateSchPin(Comp, 'DISCH', '7', -300, 200, eRotate180, eElectricOutput);
    CreateSchPin(Comp, 'VCC', '8', 0, 300, eRotate270, eElectricPower);
    CreateSchRectangle(Comp, -300, -300, 300, 300, eSmall, False);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 35: CRYSTAL - Crystal oscillator ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'Y?';
    Comp.Comment.Text := 'Crystal';
    Comp.LibReference := 'CRYSTAL';
    Comp.ComponentDescription := 'Crystal oscillator symbol';

    CreateSchPin(Comp, 'P1', '1', -300, 0, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'P2', '2', 300, 0, eRotate0, eElectricPassive);
    CreateSchRectangle(Comp, -100, -100, 100, 100, eSmall, False);
    CreateSchLine(Comp, -150, -150, -150, 150, eMedium);
    CreateSchLine(Comp, 150, -150, 150, 150, eMedium);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 36: TRANSFORMER - Transformer ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'T?';
    Comp.Comment.Text := 'Xfmr';
    Comp.LibReference := 'TRANSFORMER';
    Comp.ComponentDescription := 'Transformer symbol';

    CreateSchPin(Comp, 'P1', '1', -300, 100, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'P2', '2', -300, -100, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'S1', '3', 300, 100, eRotate0, eElectricPassive);
    CreateSchPin(Comp, 'S2', '4', 300, -100, eRotate0, eElectricPassive);
    CreateSchArc(Comp, -100, 60, 30, 0, 180, eMedium);
    CreateSchArc(Comp, -100, 20, 30, 0, 180, eMedium);
    CreateSchArc(Comp, -100, -20, 30, 0, 180, eMedium);
    CreateSchArc(Comp, -100, -60, 30, 0, 180, eMedium);
    CreateSchArc(Comp, 100, 60, 30, 0, 180, eMedium);
    CreateSchArc(Comp, 100, 20, 30, 0, 180, eMedium);
    CreateSchArc(Comp, 100, -20, 30, 0, 180, eMedium);
    CreateSchArc(Comp, 100, -60, 30, 0, 180, eMedium);
    CreateSchLine(Comp, -20, -100, -20, 100, eMedium);
    CreateSchLine(Comp, 20, -100, 20, 100, eMedium);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 37: SCHOTTKY - Schottky diode ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'D?';
    Comp.Comment.Text := 'Schottky';
    Comp.LibReference := 'SCHOTTKY';
    Comp.ComponentDescription := 'Schottky diode symbol';

    CreateSchPin(Comp, 'A', '1', -300, 0, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'K', '2', 300, 0, eRotate0, eElectricPassive);
    CreateSchTriangle(Comp, -100, 100, -100, -100, 100, 0, True);
    CreateSchLine(Comp, 100, -100, 100, 100, eMedium);
    CreateSchLine(Comp, 100, 100, 130, 100, eSmall);
    CreateSchLine(Comp, 130, 100, 130, 70, eSmall);
    CreateSchLine(Comp, 100, -100, 70, -100, eSmall);
    CreateSchLine(Comp, 70, -100, 70, -70, eSmall);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 38: IC_16PIN - 16-pin generic IC ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'U?';
    Comp.Comment.Text := 'IC16';
    Comp.LibReference := 'IC_16PIN';
    Comp.ComponentDescription := '16-pin generic IC symbol';

    CreateSchPin(Comp, 'P1', '1', -400, 350, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'P2', '2', -400, 250, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'P3', '3', -400, 150, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'P4', '4', -400, 50, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'P5', '5', -400, -50, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'P6', '6', -400, -150, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'P7', '7', -400, -250, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'P8', '8', -400, -350, eRotate180, eElectricPassive);
    CreateSchPin(Comp, 'P9', '9', 400, -350, eRotate0, eElectricPassive);
    CreateSchPin(Comp, 'P10', '10', 400, -250, eRotate0, eElectricPassive);
    CreateSchPin(Comp, 'P11', '11', 400, -150, eRotate0, eElectricPassive);
    CreateSchPin(Comp, 'P12', '12', 400, -50, eRotate0, eElectricPassive);
    CreateSchPin(Comp, 'P13', '13', 400, 50, eRotate0, eElectricPassive);
    CreateSchPin(Comp, 'P14', '14', 400, 150, eRotate0, eElectricPassive);
    CreateSchPin(Comp, 'P15', '15', 400, 250, eRotate0, eElectricPassive);
    CreateSchPin(Comp, 'P16', '16', 400, 350, eRotate0, eElectricPassive);
    CreateSchRectangle(Comp, -400, -400, 400, 400, eSmall, False);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    //==========================================================================
    // PROPERTY TEST SYMBOLS - Exercise all available object properties
    //==========================================================================

    // === Symbol 39: COLOR_TEST - Tests different colors ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'ColorTest';
    Comp.LibReference := 'COLOR_TEST';
    Comp.ComponentDescription := 'Tests colors on various objects';

    // Colored rectangles
    CreateSchRectFull(Comp, -300, 200, -100, 300, eSmall, eLineStyleSolid, True, False, clRed, clRed);
    CreateSchRectFull(Comp, -50, 200, 150, 300, eSmall, eLineStyleSolid, True, False, clGreen, clGreen);
    CreateSchRectFull(Comp, 200, 200, 400, 300, eSmall, eLineStyleSolid, True, False, clBlue, clBlue);
    // Colored lines
    CreateSchLineStyled(Comp, -300, 150, 400, 150, eMedium, eLineStyleSolid, clRed);
    CreateSchLineStyled(Comp, -300, 100, 400, 100, eMedium, eLineStyleSolid, clGreen);
    CreateSchLineStyled(Comp, -300, 50, 400, 50, eMedium, eLineStyleSolid, clBlue);
    // Colored arcs
    CreateSchArcStyled(Comp, -200, -50, 40, 0, 360, eMedium, clFuchsia);
    CreateSchArcStyled(Comp, 0, -50, 40, 0, 360, eMedium, clAqua);
    CreateSchArcStyled(Comp, 200, -50, 40, 0, 360, eMedium, clYellow);
    // Colored labels
    CreateSchLabelFull(Comp, -200, -150, 'RED', 1, clRed, eRotate0, eJustify_Center, False);
    CreateSchLabelFull(Comp, 0, -150, 'GREEN', 1, clGreen, eRotate0, eJustify_Center, False);
    CreateSchLabelFull(Comp, 200, -150, 'BLUE', 1, clBlue, eRotate0, eJustify_Center, False);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 40: LINESTYLE_TEST - Tests line styles ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'LineStyleTest';
    Comp.LibReference := 'LINESTYLE_TEST';
    Comp.ComponentDescription := 'Tests solid/dashed/dotted line styles';

    // Solid lines at different widths
    CreateSchLineStyled(Comp, -200, 200, 200, 200, eSmall, eLineStyleSolid, clBlack);
    CreateSchLineStyled(Comp, -200, 150, 200, 150, eMedium, eLineStyleSolid, clBlack);
    CreateSchLineStyled(Comp, -200, 100, 200, 100, eLarge, eLineStyleSolid, clBlack);
    // Dashed lines
    CreateSchLineStyled(Comp, -200, 0, 200, 0, eSmall, eLineStyleDashed, clBlue);
    CreateSchLineStyled(Comp, -200, -50, 200, -50, eMedium, eLineStyleDashed, clBlue);
    CreateSchLineStyled(Comp, -200, -100, 200, -100, eLarge, eLineStyleDashed, clBlue);
    // Dotted lines
    CreateSchLineStyled(Comp, -200, -200, 200, -200, eSmall, eLineStyleDotted, clRed);
    CreateSchLineStyled(Comp, -200, -250, 200, -250, eMedium, eLineStyleDotted, clRed);
    CreateSchLineStyled(Comp, -200, -300, 200, -300, eLarge, eLineStyleDotted, clRed);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 41: TRANSPARENCY_TEST - Tests transparency ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'TransparencyTest';
    Comp.LibReference := 'TRANSPARENCY_TEST';
    Comp.ComponentDescription := 'Tests solid vs transparent fills';

    // Background grid
    CreateSchLineStyled(Comp, -200, -200, -200, 200, eSmall, eLineStyleSolid, $C0C0C0);
    CreateSchLineStyled(Comp, 0, -200, 0, 200, eSmall, eLineStyleSolid, $C0C0C0);
    CreateSchLineStyled(Comp, 200, -200, 200, 200, eSmall, eLineStyleSolid, $C0C0C0);
    // Opaque rectangle
    CreateSchRectFull(Comp, -250, 50, -50, 180, eMedium, eLineStyleSolid, True, False, clBlue, clYellow);
    CreateSchLabelFull(Comp, -150, 0, 'Opaque', 1, clBlack, eRotate0, eJustify_Center, False);
    // Transparent rectangle
    CreateSchRectFull(Comp, 50, 50, 250, 180, eMedium, eLineStyleSolid, True, True, clBlue, clYellow);
    CreateSchLabelFull(Comp, 150, 0, 'Transparent', 1, clBlack, eRotate0, eJustify_Center, False);
    // Outline only
    CreateSchRectFull(Comp, -150, -180, 150, -50, eMedium, eLineStyleSolid, False, False, clRed, clWhite);
    CreateSchLabelFull(Comp, 0, -200, 'Outline Only', 1, clBlack, eRotate0, eJustify_Center, False);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 42: PIN_PROPS_TEST - Tests all pin properties ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'U?';
    Comp.Comment.Text := 'PinPropsTest';
    Comp.LibReference := 'PIN_PROPS_TEST';
    Comp.ComponentDescription := 'Tests all pin properties';

    // Various electrical types with colors
    CreateSchPinFull(Comp, 'INPUT', '1', -400, 200, eRotate180, eElectricInput,
        200, False, True, True, clRed, clBlue, 1, 1, eNoSymbol, eNoSymbol, eSmall, '', 'Input pin', '');
    CreateSchPinFull(Comp, 'OUTPUT', '2', -400, 100, eRotate180, eElectricOutput,
        200, False, True, True, clGreen, clFuchsia, 1, 1, eNoSymbol, eNoSymbol, eSmall, '', 'Output pin', '');
    CreateSchPinFull(Comp, 'IO', '3', -400, 0, eRotate180, eElectricIO,
        200, False, True, True, clBlue, clYellow, 1, 1, eNoSymbol, eNoSymbol, eSmall, '', 'Bidir pin', '');
    CreateSchPinFull(Comp, 'POWER', '4', -400, -100, eRotate180, eElectricPower,
        200, False, True, True, clRed, clRed, 1, 1, eNoSymbol, eNoSymbol, eSmall, 'VCC', 'Power pin', '3.3V');
    // IEEE symbols
    CreateSchPinFull(Comp, 'CLK', '5', 400, 200, eRotate0, eElectricInput,
        200, False, True, True, clBlack, clBlack, 1, 1, eClock, eNoSymbol, eSmall, '', 'Clock', '');
    CreateSchPinFull(Comp, 'INV', '6', 400, 100, eRotate0, eElectricOutput,
        200, False, True, True, clBlack, clBlack, 1, 1, eNoSymbol, eInvert, eSmall, '', 'Inverted', '');
    CreateSchPinFull(Comp, 'ACTLOW', '7', 400, 0, eRotate0, eElectricInput,
        200, False, True, True, clBlack, clBlack, 1, 1, eNoSymbol, eActiveLowInput, eSmall, '', 'Active low input', '');
    // Hidden power pin
    CreateSchPinFull(Comp, 'GND', '8', 400, -100, eRotate0, eElectricPower,
        200, True, True, True, clBlack, clBlack, 1, 1, eNoSymbol, eNoSymbol, eSmall, 'GND', 'Ground', '0V');
    // Body
    CreateSchRectFull(Comp, -200, -200, 200, 250, eSmall, eLineStyleSolid, False, False, clBlack, clWhite);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 43: TEXTFRAME_PROPS - Tests text frame properties ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'TextFrameTest';
    Comp.LibReference := 'TEXTFRAME_PROPS';
    Comp.ComponentDescription := 'Tests text frame properties';

    CreateSchTextFrameFull(Comp, -350, 100, -50, 200, 'Left Align', 1, eLeftAlign,
        True, True, False, True, True, clBlack, clYellow, clBlack, eSmall, eLineStyleSolid, 10);
    CreateSchTextFrameFull(Comp, 0, 100, 300, 200, 'Center Align', 1, eHorizontalCentreAlign,
        True, True, False, True, True, clBlue, clAqua, clBlue, eSmall, eLineStyleSolid, 10);
    CreateSchTextFrameFull(Comp, -350, -50, -50, 50, 'No Border', 1, eLeftAlign,
        False, True, False, True, True, clBlack, clGreen, clBlack, eSmall, eLineStyleSolid, 10);
    CreateSchTextFrameFull(Comp, 0, -50, 300, 50, 'Dashed Border', 1, eHorizontalCentreAlign,
        True, False, False, True, True, clRed, clWhite, clRed, eMedium, eLineStyleDashed, 10);
    CreateSchTextFrameFull(Comp, -350, -200, 300, -100, 'Word wrap long text example', 1, eLeftAlign,
        True, True, False, True, True, clBlack, clWhite, clFuchsia, eSmall, eLineStyleSolid, 5);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 44: ARROW_SHAPES - Tests polyline arrow shapes ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'ArrowShapes';
    Comp.LibReference := 'ARROW_SHAPES';
    Comp.ComponentDescription := 'Tests polyline arrow shapes';

    CreateSchPolylineStyled(Comp, -300, 200, -100, 200, -100, 150, 100, 150,
        eMedium, eLineStyleSolid, clBlack, eLineShapeNone, eLineShapeNone, eMedium);
    CreateSchLabelFull(Comp, -350, 175, 'None', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
    CreateSchPolylineStyled(Comp, -300, 100, -100, 100, -100, 50, 100, 50,
        eMedium, eLineStyleSolid, clRed, eLineShapeArrow, eLineShapeNone, eMedium);
    CreateSchLabelFull(Comp, -350, 75, 'Start', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
    CreateSchPolylineStyled(Comp, -300, 0, -100, 0, -100, -50, 100, -50,
        eMedium, eLineStyleSolid, clGreen, eLineShapeNone, eLineShapeArrow, eMedium);
    CreateSchLabelFull(Comp, -350, -25, 'End', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
    CreateSchPolylineStyled(Comp, -300, -100, -100, -100, -100, -150, 100, -150,
        eMedium, eLineStyleSolid, clBlue, eLineShapeArrow, eLineShapeArrow, eMedium);
    CreateSchLabelFull(Comp, -350, -125, 'Both', 1, clBlack, eRotate0, eJustify_CenterLeft, False);
    CreateSchPolylineStyled(Comp, -300, -200, -100, -200, -100, -250, 100, -250,
        eMedium, eLineStyleSolid, clFuchsia, eLineShapeSolidTail, eLineShapeSolidArrow, eMedium);
    CreateSchLabelFull(Comp, -350, -225, 'Solid', 1, clBlack, eRotate0, eJustify_CenterLeft, False);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 45: ELLIPSE_PIE_TEST - Tests ellipses and pies ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'EllipsePieTest';
    Comp.LibReference := 'ELLIPSE_PIE_TEST';
    Comp.ComponentDescription := 'Tests ellipses and pie shapes';

    // Ellipses
    CreateSchEllipseFull(Comp, -250, 150, 50, 50, eSmall, True, False, clBlack, clRed);
    CreateSchEllipseFull(Comp, 0, 150, 70, 40, eSmall, True, False, clBlack, clGreen);
    CreateSchEllipseFull(Comp, 250, 150, 40, 70, eSmall, True, False, clBlack, clBlue);
    // Elliptical arcs
    CreateSchEllipticalArc(Comp, -250, 0, 50, 30, 0, 180, eMedium, clRed);
    CreateSchEllipticalArc(Comp, 0, 0, 50, 30, 45, 270, eMedium, clGreen);
    CreateSchEllipticalArc(Comp, 250, 0, 50, 30, 90, 360, eMedium, clBlue);
    // Pies
    CreateSchPieFull(Comp, -250, -150, 60, 0, 90, eSmall, True, clBlack, clYellow);
    CreateSchPieFull(Comp, 0, -150, 60, 45, 180, eSmall, True, clBlack, clAqua);
    CreateSchPieFull(Comp, 250, -150, 60, 0, 270, eSmall, True, clBlack, clFuchsia);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 46: ROUNDRECT_CORNERS - Tests round rectangle corners ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'RoundRectTest';
    Comp.LibReference := 'ROUNDRECT_CORNERS';
    Comp.ComponentDescription := 'Tests round rectangle corner radii';

    CreateSchRoundRectFull(Comp, -300, 100, -100, 200, 10, 10, eSmall, eLineStyleSolid, True, False, clBlack, clRed);
    CreateSchRoundRectFull(Comp, -50, 100, 150, 200, 30, 30, eSmall, eLineStyleSolid, True, False, clBlack, clGreen);
    CreateSchRoundRectFull(Comp, 200, 100, 400, 200, 50, 50, eSmall, eLineStyleSolid, True, False, clBlack, clBlue);
    // Asymmetric corners
    CreateSchRoundRectFull(Comp, -300, -50, -100, 50, 50, 15, eSmall, eLineStyleSolid, True, False, clBlack, clYellow);
    CreateSchRoundRectFull(Comp, -50, -50, 150, 50, 15, 50, eSmall, eLineStyleSolid, True, False, clBlack, clAqua);
    // Different line styles
    CreateSchRoundRectFull(Comp, 200, -50, 400, 50, 25, 25, eMedium, eLineStyleDashed, True, False, clFuchsia, clWhite);
    // Outline
    CreateSchRoundRectFull(Comp, -100, -200, 200, -100, 40, 40, eLarge, eLineStyleSolid, False, False, clRed, clWhite);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 47: POLYGON_COLORS - Tests polygon colors ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'PolygonColors';
    Comp.LibReference := 'POLYGON_COLORS';
    Comp.ComponentDescription := 'Tests polygon colors';

    // Solid triangles
    CreateSchPolygonFull(Comp, -300, 0, -200, 150, -100, 0, True, clBlack, clRed);
    CreateSchPolygonFull(Comp, -50, 0, 50, 150, 150, 0, True, clBlack, clGreen);
    CreateSchPolygonFull(Comp, 200, 0, 300, 150, 400, 0, True, clBlack, clBlue);
    // Outline triangles
    CreateSchPolygonFull(Comp, -300, -200, -200, -50, -100, -200, False, clFuchsia, clWhite);
    CreateSchPolygonFull(Comp, -50, -200, 50, -50, 150, -200, False, clAqua, clWhite);
    CreateSchPolygonFull(Comp, 200, -200, 300, -50, 400, -200, False, clYellow, clWhite);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    // === Symbol 48: LABEL_JUSTIFICATION - Tests label justification ===
    Comp := SchServer.SchObjectFactory(eSchComponent, eCreate_Default);
    Comp.Designator.Text := 'X?';
    Comp.Comment.Text := 'LabelJustify';
    Comp.LibReference := 'LABEL_JUSTIFICATION';
    Comp.ComponentDescription := 'Tests label justification and orientation';

    // Grid reference
    CreateSchLineStyled(Comp, -200, 0, 200, 0, eSmall, eLineStyleDashed, $808080);
    CreateSchLineStyled(Comp, 0, -200, 0, 200, eSmall, eLineStyleDashed, $808080);
    // Justification tests
    CreateSchLabelFull(Comp, -150, 150, 'BL', 1, clRed, eRotate0, eJustify_BottomLeft, False);
    CreateSchLabelFull(Comp, 0, 150, 'BC', 1, clGreen, eRotate0, eJustify_BottomCenter, False);
    CreateSchLabelFull(Comp, 150, 150, 'BR', 1, clBlue, eRotate0, eJustify_BottomRight, False);
    CreateSchLabelFull(Comp, -150, 0, 'CL', 1, clRed, eRotate0, eJustify_CenterLeft, False);
    CreateSchLabelFull(Comp, 0, 0, 'CC', 1, clGreen, eRotate0, eJustify_Center, False);
    CreateSchLabelFull(Comp, 150, 0, 'CR', 1, clBlue, eRotate0, eJustify_CenterRight, False);
    CreateSchLabelFull(Comp, -150, -150, 'TL', 1, clRed, eRotate0, eJustify_TopLeft, False);
    CreateSchLabelFull(Comp, 0, -150, 'TC', 1, clGreen, eRotate0, eJustify_TopCenter, False);
    CreateSchLabelFull(Comp, 150, -150, 'TR', 1, clBlue, eRotate0, eJustify_TopRight, False);
    // Mirrored
    CreateSchLabelFull(Comp, 250, 50, 'Normal', 1, clFuchsia, eRotate0, eJustify_Center, False);
    CreateSchLabelFull(Comp, 250, -50, 'Mirrored', 1, clFuchsia, eRotate0, eJustify_Center, True);

    SchLib.AddSchComponent(Comp);
    SchServer.RobotManager.SendMessage(SchLib.I_ObjectAddress,
        c_BroadCast, SCHM_PrimitiveRegistration, Comp.I_ObjectAddress);

    SchServer.ProcessControl.PostProcess(SchDoc, '');
end;

{==============================================================================
  SCHEMATIC LIBRARY JSON EXPORTER
==============================================================================}
{
procedure Cheat(Obji: IPCB_3);
begin
     Obji.
end;
}

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
    JsonWriteInteger('ownerPartId', Pin.OwnerPartId, True);
    JsonWriteInteger('ownerPartDisplayMode', Ord(Pin.OwnerPartDisplayMode), True);
    JsonWriteBoolean('graphicallyLocked', Pin.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Pin.Disabled, True);
    JsonWriteBoolean('dimmed', Pin.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Pin.CompilationMasked, True);

    // Propagation delay
    JsonWriteFloat('propagationDelay', Pin.PropagationDelay, True);

    // Unique identifiers
    JsonWriteString('uniqueId', Pin.UniqueId, True);
    JsonWriteString('handle', Pin.Handle, False);

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
    JsonWriteInteger('ownerPartId', Line.OwnerPartId, True);
    JsonWriteInteger('ownerPartDisplayMode', Ord(Line.OwnerPartDisplayMode), True);

    // State flags
    JsonWriteBoolean('graphicallyLocked', Line.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Line.Disabled, True);
    JsonWriteBoolean('dimmed', Line.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Line.CompilationMasked, True);

    // Unique identifiers
    JsonWriteString('uniqueId', Line.UniqueId, True);
    JsonWriteString('handle', Line.Handle, False);

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
    JsonWriteInteger('ownerPartId', Rect.OwnerPartId, True);
    JsonWriteInteger('ownerPartDisplayMode', Ord(Rect.OwnerPartDisplayMode), True);

    // State flags
    JsonWriteBoolean('graphicallyLocked', Rect.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Rect.Disabled, True);
    JsonWriteBoolean('dimmed', Rect.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Rect.CompilationMasked, True);

    // Unique identifiers
    JsonWriteString('uniqueId', Rect.UniqueId, True);
    JsonWriteString('handle', Rect.Handle, False);

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
    JsonWriteInteger('ownerPartId', Arc.OwnerPartId, True);
    JsonWriteInteger('ownerPartDisplayMode', Ord(Arc.OwnerPartDisplayMode), True);

    // State flags
    JsonWriteBoolean('graphicallyLocked', Arc.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Arc.Disabled, True);
    JsonWriteBoolean('dimmed', Arc.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Arc.CompilationMasked, True);

    // Unique identifiers
    JsonWriteString('uniqueId', Arc.UniqueId, True);
    JsonWriteString('handle', Arc.Handle, False);

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
    JsonWriteInteger('ownerPartId', Polygon.OwnerPartId, True);
    JsonWriteInteger('ownerPartDisplayMode', Ord(Polygon.OwnerPartDisplayMode), True);

    // State flags
    JsonWriteBoolean('graphicallyLocked', Polygon.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Polygon.Disabled, True);
    JsonWriteBoolean('dimmed', Polygon.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Polygon.CompilationMasked, True);

    // Unique identifiers
    JsonWriteString('uniqueId', Polygon.UniqueId, True);
    JsonWriteString('handle', Polygon.Handle, True);

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
    JsonWriteInteger('ownerPartId', Lbl.OwnerPartId, True);
    JsonWriteInteger('ownerPartDisplayMode', Ord(Lbl.OwnerPartDisplayMode), True);

    // State flags
    JsonWriteBoolean('graphicallyLocked', Lbl.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Lbl.Disabled, True);
    JsonWriteBoolean('dimmed', Lbl.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Lbl.CompilationMasked, True);

    // Unique identifiers
    JsonWriteString('uniqueId', Lbl.UniqueId, True);
    JsonWriteString('handle', Lbl.Handle, False);

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
    JsonWriteInteger('ownerPartId', Ellipse.OwnerPartId, True);
    JsonWriteInteger('ownerPartDisplayMode', Ord(Ellipse.OwnerPartDisplayMode), True);

    // State flags
    JsonWriteBoolean('graphicallyLocked', Ellipse.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Ellipse.Disabled, True);
    JsonWriteBoolean('dimmed', Ellipse.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Ellipse.CompilationMasked, True);

    // Unique identifiers
    JsonWriteString('uniqueId', Ellipse.UniqueId, True);
    JsonWriteString('handle', Ellipse.Handle, False);

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
    JsonWriteInteger('ownerPartId', RoundRect.OwnerPartId, True);
    JsonWriteInteger('ownerPartDisplayMode', Ord(RoundRect.OwnerPartDisplayMode), True);

    // State flags
    JsonWriteBoolean('graphicallyLocked', RoundRect.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', RoundRect.Disabled, True);
    JsonWriteBoolean('dimmed', RoundRect.Dimmed, True);
    JsonWriteBoolean('compilationMasked', RoundRect.CompilationMasked, True);

    // Unique identifiers
    JsonWriteString('uniqueId', RoundRect.UniqueId, True);
    JsonWriteString('handle', RoundRect.Handle, False);

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
    JsonWriteInteger('ownerPartId', TextFrame.OwnerPartId, True);
    JsonWriteInteger('ownerPartDisplayMode', Ord(TextFrame.OwnerPartDisplayMode), True);

    // State flags
    JsonWriteBoolean('graphicallyLocked', TextFrame.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', TextFrame.Disabled, True);
    JsonWriteBoolean('dimmed', TextFrame.Dimmed, True);
    JsonWriteBoolean('compilationMasked', TextFrame.CompilationMasked, True);

    // Unique identifiers
    JsonWriteString('uniqueId', TextFrame.UniqueId, True);
    JsonWriteString('handle', TextFrame.Handle, False);

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
    JsonWriteInteger('ownerPartId', Polyline.OwnerPartId, True);
    JsonWriteInteger('ownerPartDisplayMode', Ord(Polyline.OwnerPartDisplayMode), True);

    // State flags
    JsonWriteBoolean('graphicallyLocked', Polyline.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Polyline.Disabled, True);
    JsonWriteBoolean('dimmed', Polyline.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Polyline.CompilationMasked, True);

    // Unique identifiers
    JsonWriteString('uniqueId', Polyline.UniqueId, True);
    JsonWriteString('handle', Polyline.Handle, True);

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

    JsonCloseObject(AddComma);
end;

procedure ExportSchImageToJson(Image: ISch_Image; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Image', True);

    // Position and size
    JsonWriteInteger('x1', Image.Location.X, True);
    JsonWriteInteger('y1', Image.Location.Y, True);
    JsonWriteInteger('x2', Image.Corner.X, True);
    JsonWriteInteger('y2', Image.Corner.Y, True);
    JsonWriteBoolean('keepAspect', Image.KeepAspect, True);
    JsonWriteBoolean('embedImage', Image.EmbedImage, True);
    JsonWriteString('fileName', Image.FileName, True);

    // Border properties
    JsonWriteBoolean('isSolid', Image.IsSolid, True);
    JsonWriteInteger('lineWidth', Ord(Image.LineWidth), True);
    JsonWriteInteger('color', Image.Color, True);
    JsonWriteInteger('areaColor', Image.AreaColor, True);

    // Owner properties
    JsonWriteInteger('ownerPartId', Image.OwnerPartId, True);
    JsonWriteInteger('ownerPartDisplayMode', Ord(Image.OwnerPartDisplayMode), True);

    // State flags
    JsonWriteBoolean('graphicallyLocked', Image.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Image.Disabled, True);
    JsonWriteBoolean('dimmed', Image.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Image.CompilationMasked, True);

    // Unique identifiers
    JsonWriteString('uniqueId', Image.UniqueId, True);
    JsonWriteString('handle', Image.Handle, False);
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

    // Visual properties
    JsonWriteInteger('color', Bezier.Color, True);
    JsonWriteInteger('areaColor', Bezier.AreaColor, True);

    // Owner properties
    JsonWriteInteger('ownerPartId', Bezier.OwnerPartId, True);
    JsonWriteInteger('ownerPartDisplayMode', Ord(Bezier.OwnerPartDisplayMode), True);

    // State flags
    JsonWriteBoolean('graphicallyLocked', Bezier.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Bezier.Disabled, True);
    JsonWriteBoolean('dimmed', Bezier.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Bezier.CompilationMasked, True);

    // Unique identifiers
    JsonWriteString('uniqueId', Bezier.UniqueId, True);
    JsonWriteString('handle', Bezier.Handle, True);

    // Vertices
    JsonWriteInteger('vertexCount', Bezier.VerticesCount, True);
    JsonOpenArray('vertices');
    for I := 1 to Bezier.VerticesCount do
    begin
        JsonOpenObject('');
        JsonWriteInteger('x', Bezier.Vertex[I].X, True);
        JsonWriteInteger('y', Bezier.Vertex[I].Y, False);
        JsonCloseObject(I < Bezier.VerticesCount);
    end;
    JsonCloseArray(False);

    JsonCloseObject(AddComma);
end;

procedure ExportSchEllipticalArcToJson(EArc: ISch_EllipticalArc; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'EllipticalArc', True);

    // Position and size
    JsonWriteInteger('x', EArc.Location.X, True);
    JsonWriteInteger('y', EArc.Location.Y, True);
    JsonWriteInteger('radiusX', EArc.Radius, True);
    JsonWriteInteger('secondaryRadius', EArc.SecondaryRadius, True);

    // Arc angles
    JsonWriteFloat('startAngle', EArc.StartAngle, True);
    JsonWriteFloat('endAngle', EArc.EndAngle, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(EArc.LineWidth), True);
    JsonWriteInteger('color', EArc.Color, True);
    JsonWriteInteger('areaColor', EArc.AreaColor, True);

    // Owner properties
    JsonWriteInteger('ownerPartId', EArc.OwnerPartId, True);
    JsonWriteInteger('ownerPartDisplayMode', Ord(EArc.OwnerPartDisplayMode), True);

    // State flags
    JsonWriteBoolean('graphicallyLocked', EArc.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', EArc.Disabled, True);
    JsonWriteBoolean('dimmed', EArc.Dimmed, True);
    JsonWriteBoolean('compilationMasked', EArc.CompilationMasked, True);

    // Unique identifiers
    JsonWriteString('uniqueId', EArc.UniqueId, True);
    JsonWriteString('handle', EArc.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchPieToJson(Pie: ISch_Pie; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Pie', True);

    // Position and size
    JsonWriteInteger('x', Pie.Location.X, True);
    JsonWriteInteger('y', Pie.Location.Y, True);
    JsonWriteInteger('radius', Pie.Radius, True);

    // Arc angles
    JsonWriteFloat('startAngle', Pie.StartAngle, True);
    JsonWriteFloat('endAngle', Pie.EndAngle, True);

    // Fill properties                             /model
    JsonWriteBoolean('isSolid', Pie.IsSolid, True);

    // Line properties
    JsonWriteInteger('lineWidth', Ord(Pie.LineWidth), True);
    JsonWriteInteger('color', Pie.Color, True);
    JsonWriteInteger('areaColor', Pie.AreaColor, True);

    // Owner properties
    JsonWriteInteger('ownerPartId', Pie.OwnerPartId, True);
    JsonWriteInteger('ownerPartDisplayMode', Ord(Pie.OwnerPartDisplayMode), True);

    // State flags
    JsonWriteBoolean('graphicallyLocked', Pie.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Pie.Disabled, True);
    JsonWriteBoolean('dimmed', Pie.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Pie.CompilationMasked, True);

    // Unique identifiers
    JsonWriteString('uniqueId', Pie.UniqueId, True);
    JsonWriteString('handle', Pie.Handle, False);
    JsonCloseObject(AddComma);
end;

// Comprehensive Schematic Export Procedures

procedure ExportSchWireToJson(Wire: ISch_Wire; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Wire', True);
    JsonWriteCoord('x', Wire.Location.X, True);
    JsonWriteCoord('y', Wire.Location.Y, True);
    JsonWriteInteger('verticesCount', Wire.VerticesCount, True);
    JsonWriteInteger('lineWidth', Ord(Wire.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Wire.LineStyle), True);
    JsonWriteInteger('color', Wire.Color, True);
    JsonWriteBoolean('autoWire', Wire.AutoWire, True);
    JsonWriteBoolean('isSolid', Wire.IsSolid, True);
    JsonWriteBoolean('transparent', Wire.Transparent, True);
    JsonWriteBoolean('graphicallyLocked', Wire.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Wire.Disabled, True);
    JsonWriteBoolean('dimmed', Wire.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Wire.CompilationMasked, True);
    JsonWriteString('uniqueId', Wire.UniqueId, True);
    JsonWriteString('handle', Wire.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchBusToJson(Bus: ISch_Bus; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Bus', True);
    JsonWriteCoord('x', Bus.Location.X, True);
    JsonWriteCoord('y', Bus.Location.Y, True);
    JsonWriteInteger('verticesCount', Bus.VerticesCount, True);
    JsonWriteInteger('lineWidth', Ord(Bus.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Bus.LineStyle), True);
    JsonWriteInteger('color', Bus.Color, True);
    JsonWriteBoolean('autoWire', Bus.AutoWire, True);
    JsonWriteBoolean('isSolid', Bus.IsSolid, True);
    JsonWriteBoolean('transparent', Bus.Transparent, True);
    JsonWriteBoolean('graphicallyLocked', Bus.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Bus.Disabled, True);
    JsonWriteBoolean('dimmed', Bus.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Bus.CompilationMasked, True);
    JsonWriteString('uniqueId', Bus.UniqueId, True);
    JsonWriteString('handle', Bus.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchBusEntryToJson(BusEntry: ISch_BusEntry; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'BusEntry', True);
    JsonWriteCoord('x', BusEntry.Location.X, True);
    JsonWriteCoord('y', BusEntry.Location.Y, True);
    JsonWriteCoord('cornerX', BusEntry.Corner.X, True);
    JsonWriteCoord('cornerY', BusEntry.Corner.Y, True);
    JsonWriteInteger('lineWidth', Ord(BusEntry.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(BusEntry.LineStyle), True);
    JsonWriteInteger('color', BusEntry.Color, True);
    JsonWriteBoolean('graphicallyLocked', BusEntry.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', BusEntry.Disabled, True);
    JsonWriteBoolean('dimmed', BusEntry.Dimmed, True);
    JsonWriteBoolean('compilationMasked', BusEntry.CompilationMasked, True);
    JsonWriteString('uniqueId', BusEntry.UniqueId, True);
    JsonWriteString('handle', BusEntry.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchJunctionToJson(Junction: ISch_Junction; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Junction', True);
    JsonWriteCoord('x', Junction.Location.X, True);
    JsonWriteCoord('y', Junction.Location.Y, True);
    JsonWriteInteger('size', Ord(Junction.Size), True);
    JsonWriteInteger('color', Junction.Color, True);
    JsonWriteBoolean('locked', Junction.Locked, True);
    JsonWriteBoolean('graphicallyLocked', Junction.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Junction.Disabled, True);
    JsonWriteBoolean('dimmed', Junction.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Junction.CompilationMasked, True);
    JsonWriteString('uniqueId', Junction.UniqueId, True);
    JsonWriteString('handle', Junction.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchNetLabelToJson(NetLabel: ISch_NetLabel; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'NetLabel', True);
    JsonWriteCoord('x', NetLabel.Location.X, True);
    JsonWriteCoord('y', NetLabel.Location.Y, True);
    JsonWriteString('text', NetLabel.Text, True);
    JsonWriteInteger('orientation', Ord(NetLabel.Orientation), True);
    JsonWriteInteger('justification', Ord(NetLabel.Justification), True);
    JsonWriteInteger('fontID', NetLabel.FontID, True);
    JsonWriteInteger('color', NetLabel.Color, True);
    JsonWriteString('formula', NetLabel.Formula, True);
    JsonWriteString('displayString', NetLabel.DisplayString, True);
    JsonWriteString('calculatedValueString', NetLabel.CalculatedValueString, True);
    JsonWriteBoolean('isMirrored', NetLabel.IsMirrored, True);
    JsonWriteBoolean('graphicallyLocked', NetLabel.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', NetLabel.Disabled, True);
    JsonWriteBoolean('dimmed', NetLabel.Dimmed, True);
    JsonWriteBoolean('compilationMasked', NetLabel.CompilationMasked, True);
    JsonWriteString('uniqueId', NetLabel.UniqueId, True);
    JsonWriteString('handle', NetLabel.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchPortToJson(Port: ISch_Port; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Port', True);
    JsonWriteCoord('x', Port.Location.X, True);
    JsonWriteCoord('y', Port.Location.Y, True);
    JsonWriteString('name', Port.Name, True);
    JsonWriteInteger('ioType', Ord(Port.IOType), True);
    JsonWriteInteger('style', Ord(Port.Style), True);
    JsonWriteInteger('alignment', Ord(Port.Alignment), True);
    JsonWriteCoord('width', Port.Width, True);
    JsonWriteCoord('height', Port.Height, True);
    JsonWriteInteger('borderWidth', Ord(Port.BorderWidth), True);
    JsonWriteInteger('fontID', Port.FontID, True);
    JsonWriteInteger('color', Port.Color, True);
    JsonWriteInteger('areaColor', Port.AreaColor, True);
    JsonWriteInteger('textColor', Port.TextColor, True);
    JsonWriteString('harnessType', Port.HarnessType, True);
    JsonWriteInteger('harnessColor', Port.HarnessColor, True);
    JsonWriteBoolean('autoSize', Port.AutoSize, True);
    JsonWriteBoolean('isCustomStyle', Port.IsCustomStyle, True);
    JsonWriteBoolean('showNetName', Port.ShowNetName, True);
    JsonWriteString('crossReference', Port.CrossReference, True);
    JsonWriteInteger('connectedEnd', Ord(Port.ConnectedEnd), True);
    JsonWriteBoolean('graphicallyLocked', Port.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Port.Disabled, True);
    JsonWriteBoolean('dimmed', Port.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Port.CompilationMasked, True);
    JsonWriteString('uniqueId', Port.UniqueId, True);
    JsonWriteString('handle', Port.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchPowerObjectToJson(PowerObj: ISch_PowerObject; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'PowerObject', True);
    JsonWriteCoord('x', PowerObj.Location.X, True);
    JsonWriteCoord('y', PowerObj.Location.Y, True);
    JsonWriteString('text', PowerObj.Text, True);
    JsonWriteInteger('style', Ord(PowerObj.Style), True);
    JsonWriteInteger('orientation', Ord(PowerObj.Orientation), True);
    JsonWriteInteger('fontID', PowerObj.FontID, True);
    JsonWriteInteger('color', PowerObj.Color, True);
    JsonWriteBoolean('showNetName', PowerObj.ShowNetName, True);
    JsonWriteBoolean('isMirrored', PowerObj.IsMirrored, True);
    JsonWriteBoolean('isCustomStyle', PowerObj.IsCustomStyle, True);
    JsonWriteInteger('justification', Ord(PowerObj.Justification), True);
    JsonWriteString('formula', PowerObj.Formula, True);
    JsonWriteString('displayString', PowerObj.DisplayString, True);
    JsonWriteString('calculatedValueString', PowerObj.CalculatedValueString, True);
    JsonWriteBoolean('graphicallyLocked', PowerObj.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', PowerObj.Disabled, True);
    JsonWriteBoolean('dimmed', PowerObj.Dimmed, True);
    JsonWriteBoolean('compilationMasked', PowerObj.CompilationMasked, True);
    JsonWriteString('uniqueId', PowerObj.UniqueId, True);
    JsonWriteString('handle', PowerObj.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchSheetSymbolToJson(SheetSymbol: ISch_SheetSymbol; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SheetSymbol', True);
    JsonWriteCoord('x', SheetSymbol.Location.X, True);
    JsonWriteCoord('y', SheetSymbol.Location.Y, True);
    JsonWriteCoord('xSize', SheetSymbol.XSize, True);
    JsonWriteCoord('ySize', SheetSymbol.YSize, True);
    JsonWriteInteger('lineWidth', Ord(SheetSymbol.LineWidth), True);
    JsonWriteInteger('color', SheetSymbol.Color, True);
    JsonWriteInteger('areaColor', SheetSymbol.AreaColor, True);
    JsonWriteBoolean('isSolid', SheetSymbol.IsSolid, True);
    JsonWriteString('designItemId', SheetSymbol.DesignItemId, True);
    JsonWriteString('libraryIdentifier', SheetSymbol.LibraryIdentifier, True);
    JsonWriteString('sourceLibraryName', SheetSymbol.SourceLibraryName, True);
    JsonWriteInteger('libIdentifierKind', Ord(SheetSymbol.LibIdentifierKind), True);
    JsonWriteString('vaultGUID', SheetSymbol.VaultGUID, True);
    JsonWriteString('itemGUID', SheetSymbol.ItemGUID, True);
    JsonWriteString('revisionGUID', SheetSymbol.RevisionGUID, True);
    JsonWriteInteger('symbolType', Ord(SheetSymbol.SymbolType), True);
    JsonWriteBoolean('showHiddenFields', SheetSymbol.ShowHiddenFields, True);
    JsonWriteBoolean('graphicallyLocked', SheetSymbol.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', SheetSymbol.Disabled, True);
    JsonWriteBoolean('dimmed', SheetSymbol.Dimmed, True);
    JsonWriteBoolean('compilationMasked', SheetSymbol.CompilationMasked, True);
    JsonWriteString('uniqueId', SheetSymbol.UniqueId, True);
    JsonWriteString('handle', SheetSymbol.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchSheetEntryToJson(SheetEntry: ISch_SheetEntry; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SheetEntry', True);
    JsonWriteCoord('x', SheetEntry.Location.X, True);
    JsonWriteCoord('y', SheetEntry.Location.Y, True);
    JsonWriteString('name', SheetEntry.Name, True);
    JsonWriteInteger('ioType', Ord(SheetEntry.IOType), True);
    JsonWriteInteger('style', Ord(SheetEntry.Style), True);
    JsonWriteInteger('side', Ord(SheetEntry.Side), True);
    JsonWriteInteger('arrowKind', Ord(SheetEntry.ArrowKind), True);
    JsonWriteCoord('distanceFromTop', SheetEntry.DistanceFromTop, True);
    JsonWriteInteger('color', SheetEntry.Color, True);
    JsonWriteInteger('textColor', SheetEntry.TextColor, True);
    JsonWriteInteger('textFontID', SheetEntry.TextFontID, True);
    JsonWriteInteger('textStyle', Ord(SheetEntry.TextStyle), True);
    JsonWriteString('harnessType', SheetEntry.HarnessType, True);
    JsonWriteInteger('harnessColor', SheetEntry.HarnessColor, True);
    JsonWriteBoolean('graphicallyLocked', SheetEntry.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', SheetEntry.Disabled, True);
    JsonWriteBoolean('dimmed', SheetEntry.Dimmed, True);
    JsonWriteBoolean('compilationMasked', SheetEntry.CompilationMasked, True);
    JsonWriteString('uniqueId', SheetEntry.UniqueId, True);
    JsonWriteString('handle', SheetEntry.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchBlanketToJson(Blanket: ISch_Blanket; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Blanket', True);
    JsonWriteCoord('x', Blanket.Location.X, True);
    JsonWriteCoord('y', Blanket.Location.Y, True);
    JsonWriteInteger('verticesCount', Blanket.VerticesCount, True);
    JsonWriteInteger('lineWidth', Ord(Blanket.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Blanket.LineStyle), True);
    JsonWriteInteger('color', Blanket.Color, True);
    JsonWriteInteger('areaColor', Blanket.AreaColor, True);
    JsonWriteBoolean('collapsed', Blanket.Collapsed, True);
    JsonWriteBoolean('isSolid', Blanket.IsSolid, True);
    JsonWriteBoolean('transparent', Blanket.Transparent, True);
    JsonWriteBoolean('graphicallyLocked', Blanket.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Blanket.Disabled, True);
    JsonWriteBoolean('dimmed', Blanket.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Blanket.CompilationMasked, True);
    JsonWriteString('uniqueId', Blanket.UniqueId, True);
    JsonWriteString('handle', Blanket.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchDirectiveToJson(Directive: ISch_Directive; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Directive', True);
    JsonWriteCoord('x', Directive.Location.X, True);
    JsonWriteCoord('y', Directive.Location.Y, True);
    JsonWriteString('text', Directive.Text, True);
    JsonWriteInteger('color', Directive.Color, True);
    JsonWriteBoolean('graphicallyLocked', Directive.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Directive.Disabled, True);
    JsonWriteBoolean('dimmed', Directive.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Directive.CompilationMasked, True);
    JsonWriteString('uniqueId', Directive.UniqueId, True);
    JsonWriteString('handle', Directive.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchProbeToJson(Probe: ISch_Probe; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Probe', True);
    JsonWriteCoord('x', Probe.Location.X, True);
    JsonWriteCoord('y', Probe.Location.Y, True);
    JsonWriteString('name', Probe.Name, True);
    JsonWriteInteger('orientation', Ord(Probe.Orientation), True);
    JsonWriteInteger('style', Ord(Probe.Style), True);
    JsonWriteInteger('color', Probe.Color, True);
    JsonWriteBoolean('graphicallyLocked', Probe.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Probe.Disabled, True);
    JsonWriteBoolean('dimmed', Probe.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Probe.CompilationMasked, True);
    JsonWriteString('uniqueId', Probe.UniqueId, True);
    JsonWriteString('handle', Probe.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchNoERCToJson(NoERC: ISch_NoERC; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'NoERC', True);
    JsonWriteCoord('x', NoERC.Location.X, True);
    JsonWriteCoord('y', NoERC.Location.Y, True);
    JsonWriteInteger('orientation', Ord(NoERC.Orientation), True);
    JsonWriteInteger('color', NoERC.Color, True);
    JsonWriteBoolean('isActive', NoERC.IsActive, True);
    JsonWriteBoolean('suppressAll', NoERC.SuppressAll, True);
    JsonWriteBoolean('graphicallyLocked', NoERC.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', NoERC.Disabled, True);
    JsonWriteBoolean('dimmed', NoERC.Dimmed, True);
    JsonWriteBoolean('compilationMasked', NoERC.CompilationMasked, True);
    JsonWriteString('uniqueId', NoERC.UniqueId, True);
    JsonWriteString('handle', NoERC.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchParameterToJson(Param: ISch_Parameter; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Parameter', True);
    JsonWriteCoord('x', Param.Location.X, True);
    JsonWriteCoord('y', Param.Location.Y, True);
    JsonWriteString('name', Param.Name, True);
    JsonWriteString('text', Param.Text, True);
    JsonWriteInteger('orientation', Ord(Param.Orientation), True);
    JsonWriteInteger('fontID', Param.FontID, True);
    JsonWriteInteger('color', Param.Color, True);
    JsonWriteBoolean('isHidden', Param.IsHidden, True);
    JsonWriteBoolean('readOnlyState', Param.ReadOnlyState, True);
    JsonWriteBoolean('showName', Param.ShowName, True);
    JsonWriteBoolean('graphicallyLocked', Param.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Param.Disabled, True);
    JsonWriteBoolean('dimmed', Param.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Param.CompilationMasked, True);
    JsonWriteString('uniqueId', Param.UniqueId, True);
    JsonWriteString('handle', Param.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchCrossSheetConnectorToJson(Connector: ISch_CrossSheetConnector; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CrossSheetConnector', True);
    JsonWriteCoord('x', Connector.Location.X, True);
    JsonWriteCoord('y', Connector.Location.Y, True);
    JsonWriteString('text', Connector.Text, True);
    JsonWriteInteger('orientation', Ord(Connector.Orientation), True);
    JsonWriteInteger('fontID', Connector.FontID, True);
    JsonWriteInteger('color', Connector.Color, True);
    JsonWriteInteger('connectorType', Ord(Connector.ConnectorType), True);
    JsonWriteBoolean('graphicallyLocked', Connector.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Connector.Disabled, True);
    JsonWriteBoolean('dimmed', Connector.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Connector.CompilationMasked, True);
    JsonWriteString('uniqueId', Connector.UniqueId, True);
    JsonWriteString('handle', Connector.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchNoteToJson(Note: ISch_Note; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Note', True);
    JsonWriteCoord('x', Note.Location.X, True);
    JsonWriteCoord('y', Note.Location.Y, True);
    JsonWriteString('text', Note.Text, True);
    JsonWriteInteger('fontID', Note.FontID, True);
    JsonWriteInteger('color', Note.Color, True);
    JsonWriteInteger('areaColor', Note.AreaColor, True);
    JsonWriteBoolean('collapsed', Note.Collapsed, True);
    JsonWriteBoolean('graphicallyLocked', Note.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Note.Disabled, True);
    JsonWriteBoolean('dimmed', Note.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Note.CompilationMasked, True);
    JsonWriteString('uniqueId', Note.UniqueId, True);
    JsonWriteString('handle', Note.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchTemplateToJson(Template: ISch_Template; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Template', True);
    JsonWriteCoord('x', Template.Location.X, True);
    JsonWriteCoord('y', Template.Location.Y, True);
    JsonWriteString('fileName', Template.FileName, True);
    JsonWriteInteger('color', Template.Color, True);
    JsonWriteBoolean('graphicallyLocked', Template.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Template.Disabled, True);
    JsonWriteBoolean('dimmed', Template.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Template.CompilationMasked, True);
    JsonWriteString('uniqueId', Template.UniqueId, True);
    JsonWriteString('handle', Template.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHyperlinkToJson(Hyperlink: ISch_Hyperlink; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Hyperlink', True);
    JsonWriteCoord('x', Hyperlink.Location.X, True);
    JsonWriteCoord('y', Hyperlink.Location.Y, True);
    JsonWriteString('text', Hyperlink.Text, True);
    JsonWriteString('url', Hyperlink.URL, True);
    JsonWriteInteger('fontID', Hyperlink.FontID, True);
    JsonWriteInteger('color', Hyperlink.Color, True);
    JsonWriteBoolean('graphicallyLocked', Hyperlink.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Hyperlink.Disabled, True);
    JsonWriteBoolean('dimmed', Hyperlink.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Hyperlink.CompilationMasked, True);
    JsonWriteString('uniqueId', Hyperlink.UniqueId, True);
    JsonWriteString('handle', Hyperlink.Handle, False);
    JsonCloseObject(AddComma);
end;

// Harness Export Procedures

procedure ExportSchHarnessConnectorToJson(Connector: ISch_HarnessConnector; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessConnector', True);
    JsonWriteCoord('x', Connector.Location.X, True);
    JsonWriteCoord('y', Connector.Location.Y, True);
    JsonWriteCoord('xSize', Connector.XSize, True);
    JsonWriteCoord('ySize', Connector.YSize, True);
    JsonWriteInteger('lineWidth', Ord(Connector.LineWidth), True);
    JsonWriteInteger('color', Connector.Color, True);
    JsonWriteInteger('areaColor', Connector.AreaColor, True);
    JsonWriteString('harnessType', Connector.HarnessType, True);
    JsonWriteCoord('primaryConnectionPosition', Connector.PrimaryConnectionPosition, True);
    JsonWriteCoord('masterEntryLocationX', Connector.MasterEntryLocation.X, True);
    JsonWriteCoord('masterEntryLocationY', Connector.MasterEntryLocation.Y, True);
    JsonWriteBoolean('hideHarnessConnectorType', Connector.HideHarnessConnectorType, True);
    JsonWriteBoolean('graphicallyLocked', Connector.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Connector.Disabled, True);
    JsonWriteBoolean('dimmed', Connector.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Connector.CompilationMasked, True);
    JsonWriteString('uniqueId', Connector.UniqueId, True);
    JsonWriteString('handle', Connector.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessEntryToJson(Entry: ISch_HarnessEntry; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessEntry', True);
    JsonWriteCoord('x', Entry.Location.X, True);
    JsonWriteCoord('y', Entry.Location.Y, True);
    JsonWriteString('name', Entry.Name, True);
    JsonWriteInteger('side', Ord(Entry.Side), True);
    JsonWriteInteger('color', Entry.Color, True);
    JsonWriteInteger('textColor', Entry.TextColor, True);
    JsonWriteBoolean('graphicallyLocked', Entry.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Entry.Disabled, True);
    JsonWriteBoolean('dimmed', Entry.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Entry.CompilationMasked, True);
    JsonWriteString('uniqueId', Entry.UniqueId, True);
    JsonWriteString('handle', Entry.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessWireToJson(Wire: ISch_HarnessWire; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessWire', True);
    JsonWriteCoord('x', Wire.Location.X, True);
    JsonWriteCoord('y', Wire.Location.Y, True);
    JsonWriteInteger('verticesCount', Wire.VerticesCount, True);
    JsonWriteInteger('lineWidth', Ord(Wire.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Wire.LineStyle), True);
    JsonWriteInteger('color', Wire.Color, True);
    JsonWriteBoolean('autoWire', Wire.AutoWire, True);
    JsonWriteBoolean('isSolid', Wire.IsSolid, True);
    JsonWriteBoolean('transparent', Wire.Transparent, True);
    JsonWriteBoolean('designatorLocked', Wire.DesignatorLocked, True);
    JsonWriteString('borderColorName', Wire.GetState_BorderColorName, True);
    JsonWriteString('primaryColorName', Wire.GetState_PrimaryColorName, True);
    JsonWriteInteger('secondaryColor', Wire.GetState_SecondaryColor, True);
    JsonWriteString('secondaryColorName', Wire.GetState_SecondaryColorName, True);
    JsonWriteInteger('tertiaryColor', Wire.GetState_TertiaryColor, True);
    JsonWriteString('tertiaryColorName', Wire.GetState_TertiaryColorName, True);
    JsonWriteBoolean('graphicallyLocked', Wire.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Wire.Disabled, True);
    JsonWriteBoolean('dimmed', Wire.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Wire.CompilationMasked, True);
    JsonWriteString('uniqueId', Wire.UniqueId, True);
    JsonWriteString('handle', Wire.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessBundleToJson(Bundle: ISch_HarnessBundle; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessBundle', True);
    JsonWriteCoord('x', Bundle.Location.X, True);
    JsonWriteCoord('y', Bundle.Location.Y, True);
    JsonWriteInteger('verticesCount', Bundle.VerticesCount, True);
    JsonWriteInteger('lineWidth', Ord(Bundle.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Bundle.LineStyle), True);
    JsonWriteInteger('color', Bundle.Color, True);
    JsonWriteBoolean('autoWire', Bundle.AutoWire, True);
    JsonWriteBoolean('isSolid', Bundle.IsSolid, True);
    JsonWriteBoolean('transparent', Bundle.Transparent, True);
    JsonWriteBoolean('designatorLocked', Bundle.DesignatorLocked, True);
    JsonWriteInteger('length', Bundle.Length, True);
    JsonWriteBoolean('isLengthSetManually', Bundle.IsLengthSetManually, True);
    JsonWriteBoolean('showBreakSymbol', Bundle.GetState_ShowBreakSymbol, True);
    JsonWriteBoolean('graphicallyLocked', Bundle.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Bundle.Disabled, True);
    JsonWriteBoolean('dimmed', Bundle.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Bundle.CompilationMasked, True);
    JsonWriteString('uniqueId', Bundle.UniqueId, True);
    JsonWriteString('handle', Bundle.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessSpliceToJson(Splice: ISch_HarnessSplice; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessSplice', True);
    JsonWriteCoord('x', Splice.Location.X, True);
    JsonWriteCoord('y', Splice.Location.Y, True);
    JsonWriteString('text', Splice.Text, True);
    JsonWriteInteger('orientation', Ord(Splice.Orientation), True);
    JsonWriteInteger('style', Ord(Splice.Style), True);
    JsonWriteInteger('fontID', Splice.FontID, True);
    JsonWriteInteger('color', Splice.Color, True);
    JsonWriteInteger('borderColor', Splice.BorderColor, True);
    JsonWriteBoolean('isMirrored', Splice.IsMirrored, True);
    JsonWriteBoolean('designatorLocked', Splice.DesignatorLocked, True);
    JsonWriteInteger('justification', Ord(Splice.Justification), True);
    JsonWriteString('formula', Splice.Formula, True);
    JsonWriteString('displayString', Splice.DisplayString, True);
    JsonWriteString('calculatedValueString', Splice.CalculatedValueString, True);
    JsonWriteBoolean('graphicallyLocked', Splice.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Splice.Disabled, True);
    JsonWriteBoolean('dimmed', Splice.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Splice.CompilationMasked, True);
    JsonWriteString('uniqueId', Splice.UniqueId, True);
    JsonWriteString('handle', Splice.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessShieldToJson(Shield: ISch_HarnessShield; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessShield', True);
    JsonWriteCoord('x', Shield.Location.X, True);
    JsonWriteCoord('y', Shield.Location.Y, True);
    JsonWriteString('text', Shield.Text, True);
    JsonWriteInteger('orientation', Ord(Shield.Orientation), True);
    JsonWriteInteger('style', Ord(Shield.Style), True);
    JsonWriteInteger('fontID', Shield.FontID, True);
    JsonWriteInteger('color', Shield.Color, True);
    JsonWriteBoolean('isMirrored', Shield.IsMirrored, True);
    JsonWriteBoolean('designatorLocked', Shield.DesignatorLocked, True);
    JsonWriteBoolean('graphicallyLocked', Shield.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Shield.Disabled, True);
    JsonWriteBoolean('dimmed', Shield.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Shield.CompilationMasked, True);
    JsonWriteString('uniqueId', Shield.UniqueId, True);
    JsonWriteString('handle', Shield.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessTwistToJson(Twist: ISch_HarnessTwist; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessTwist', True);
    JsonWriteCoord('x', Twist.Location.X, True);
    JsonWriteCoord('y', Twist.Location.Y, True);
    JsonWriteString('text', Twist.Text, True);
    JsonWriteInteger('orientation', Ord(Twist.Orientation), True);
    JsonWriteInteger('style', Ord(Twist.Style), True);
    JsonWriteInteger('fontID', Twist.FontID, True);
    JsonWriteInteger('color', Twist.Color, True);
    JsonWriteBoolean('isMirrored', Twist.IsMirrored, True);
    JsonWriteBoolean('designatorLocked', Twist.DesignatorLocked, True);
    JsonWriteBoolean('graphicallyLocked', Twist.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Twist.Disabled, True);
    JsonWriteBoolean('dimmed', Twist.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Twist.CompilationMasked, True);
    JsonWriteString('uniqueId', Twist.UniqueId, True);
    JsonWriteString('handle', Twist.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessCableToJson(Cable: ISch_HarnessCable; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessCable', True);
    JsonWriteCoord('x', Cable.Location.X, True);
    JsonWriteCoord('y', Cable.Location.Y, True);
    JsonWriteString('text', Cable.Text, True);
    JsonWriteInteger('orientation', Ord(Cable.Orientation), True);
    JsonWriteInteger('style', Ord(Cable.Style), True);
    JsonWriteInteger('fontID', Cable.FontID, True);
    JsonWriteInteger('color', Cable.Color, True);
    JsonWriteBoolean('isMirrored', Cable.IsMirrored, True);
    JsonWriteBoolean('designatorLocked', Cable.DesignatorLocked, True);
    JsonWriteBoolean('graphicallyLocked', Cable.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Cable.Disabled, True);
    JsonWriteBoolean('dimmed', Cable.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Cable.CompilationMasked, True);
    JsonWriteString('uniqueId', Cable.UniqueId, True);
    JsonWriteString('handle', Cable.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessNoConnectToJson(NoConnect: ISch_HarnessNoConnect; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessNoConnect', True);
    JsonWriteCoord('x', NoConnect.Location.X, True);
    JsonWriteCoord('y', NoConnect.Location.Y, True);
    JsonWriteInteger('orientation', Ord(NoConnect.Orientation), True);
    JsonWriteInteger('color', NoConnect.Color, True);
    JsonWriteBoolean('graphicallyLocked', NoConnect.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', NoConnect.Disabled, True);
    JsonWriteBoolean('dimmed', NoConnect.Dimmed, True);
    JsonWriteBoolean('compilationMasked', NoConnect.CompilationMasked, True);
    JsonWriteString('uniqueId', NoConnect.UniqueId, True);
    JsonWriteString('handle', NoConnect.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessWireLabelToJson(WireLabel: ISch_HarnessWireLabel; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessWireLabel', True);
    JsonWriteCoord('x', WireLabel.Location.X, True);
    JsonWriteCoord('y', WireLabel.Location.Y, True);
    JsonWriteString('text', WireLabel.Text, True);
    JsonWriteInteger('orientation', Ord(WireLabel.Orientation), True);
    JsonWriteInteger('fontID', WireLabel.FontID, True);
    JsonWriteInteger('color', WireLabel.Color, True);
    JsonWriteBoolean('isMirrored', WireLabel.IsMirrored, True);
    JsonWriteBoolean('graphicallyLocked', WireLabel.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', WireLabel.Disabled, True);
    JsonWriteBoolean('dimmed', WireLabel.Dimmed, True);
    JsonWriteBoolean('compilationMasked', WireLabel.CompilationMasked, True);
    JsonWriteString('uniqueId', WireLabel.UniqueId, True);
    JsonWriteString('handle', WireLabel.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessWireBreakToJson(WireBreak: ISch_HarnessWireBreak; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessWireBreak', True);
    JsonWriteCoord('x', WireBreak.Location.X, True);
    JsonWriteCoord('y', WireBreak.Location.Y, True);
    JsonWriteInteger('orientation', Ord(WireBreak.Orientation), True);
    JsonWriteInteger('color', WireBreak.Color, True);
    JsonWriteBoolean('graphicallyLocked', WireBreak.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', WireBreak.Disabled, True);
    JsonWriteBoolean('dimmed', WireBreak.Dimmed, True);
    JsonWriteBoolean('compilationMasked', WireBreak.CompilationMasked, True);
    JsonWriteString('uniqueId', WireBreak.UniqueId, True);
    JsonWriteString('handle', WireBreak.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessPinToJson(HarnessPin: ISch_HarnessPin; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessPin', True);
    JsonWriteCoord('x', HarnessPin.Location.X, True);
    JsonWriteCoord('y', HarnessPin.Location.Y, True);
    JsonWriteString('name', HarnessPin.Name, True);
    JsonWriteString('designator', HarnessPin.Designator, True);
    JsonWriteInteger('orientation', Ord(HarnessPin.Orientation), True);
    JsonWriteInteger('color', HarnessPin.Color, True);
    JsonWriteBoolean('graphicallyLocked', HarnessPin.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', HarnessPin.Disabled, True);
    JsonWriteBoolean('dimmed', HarnessPin.Dimmed, True);
    JsonWriteBoolean('compilationMasked', HarnessPin.CompilationMasked, True);
    JsonWriteString('uniqueId', HarnessPin.UniqueId, True);
    JsonWriteString('handle', HarnessPin.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchCircleToJson(Circle: ISch_Circle; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Circle', True);
    JsonWriteCoord('x', Circle.Location.X, True);
    JsonWriteCoord('y', Circle.Location.Y, True);
    JsonWriteCoord('radius', Circle.Radius, True);
    JsonWriteInteger('lineWidth', Ord(Circle.LineWidth), True);
    JsonWriteInteger('color', Circle.Color, True);
    JsonWriteInteger('areaColor', Circle.AreaColor, True);
    JsonWriteBoolean('isSolid', Circle.IsSolid, True);
    JsonWriteBoolean('transparent', Circle.Transparent, True);
    JsonWriteBoolean('graphicallyLocked', Circle.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Circle.Disabled, True);
    JsonWriteBoolean('dimmed', Circle.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Circle.CompilationMasked, True);
    JsonWriteString('uniqueId', Circle.UniqueId, True);
    JsonWriteString('handle', Circle.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchDesignatorToJson(Designator: ISch_Designator; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Designator', True);
    JsonWriteCoord('x', Designator.Location.X, True);
    JsonWriteCoord('y', Designator.Location.Y, True);
    JsonWriteString('name', Designator.Name, True);
    JsonWriteString('text', Designator.Text, True);
    JsonWriteString('description', Designator.Description, True);
    JsonWriteString('physicalDesignator', Designator.PhysicalDesignator, True);
    JsonWriteInteger('orientation', Ord(Designator.Orientation), True);
    JsonWriteInteger('fontID', Designator.FontID, True);
    JsonWriteInteger('color', Designator.Color, True);
    JsonWriteInteger('justification', Ord(Designator.Justification), True);
    JsonWriteInteger('textHorzAnchor', Ord(Designator.TextHorzAnchor), True);
    JsonWriteInteger('textVertAnchor', Ord(Designator.TextVertAnchor), True);
    JsonWriteBoolean('isHidden', Designator.IsHidden, True);
    JsonWriteBoolean('isMirrored', Designator.IsMirrored, True);
    JsonWriteBoolean('autoposition', Designator.Autoposition, True);
    JsonWriteBoolean('showName', Designator.ShowName, True);
    JsonWriteBoolean('isConfigurable', Designator.IsConfigurable, True);
    JsonWriteBoolean('isSystemParameter', Designator.IsSystemParameter, True);
    JsonWriteBoolean('isRule', Designator.IsRule, True);
    JsonWriteBoolean('nameIsReadOnly', Designator.NameIsReadOnly, True);
    JsonWriteBoolean('valueIsReadOnly', Designator.ValueIsReadOnly, True);
    JsonWriteBoolean('allowLibrarySynchronize', Designator.AllowLibrarySynchronize, True);
    JsonWriteBoolean('allowDatabaseSynchronize', Designator.AllowDatabaseSynchronize, True);
    JsonWriteString('formula', Designator.Formula, True);
    JsonWriteString('displayString', Designator.DisplayString, True);
    JsonWriteString('calculatedValueString', Designator.CalculatedValueString, True);
    JsonWriteInteger('paramType', Ord(Designator.ParamType), True);
    JsonWriteInteger('readOnlyState', Ord(Designator.ReadOnlyState), True);
    JsonWriteBoolean('graphicallyLocked', Designator.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Designator.Disabled, True);
    JsonWriteBoolean('dimmed', Designator.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Designator.CompilationMasked, True);
    JsonWriteString('uniqueId', Designator.UniqueId, True);
    JsonWriteString('handle', Designator.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchParameterSetToJson(ParamSet: ISch_ParameterSet; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ParameterSet', True);
    JsonWriteCoord('x', ParamSet.Location.X, True);
    JsonWriteCoord('y', ParamSet.Location.Y, True);
    JsonWriteString('name', ParamSet.Name, True);
    JsonWriteInteger('orientation', Ord(ParamSet.Orientation), True);
    JsonWriteInteger('style', Ord(ParamSet.Style), True);
    JsonWriteInteger('color', ParamSet.Color, True);
    JsonWriteBoolean('graphicallyLocked', ParamSet.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', ParamSet.Disabled, True);
    JsonWriteBoolean('dimmed', ParamSet.Dimmed, True);
    JsonWriteBoolean('compilationMasked', ParamSet.CompilationMasked, True);
    JsonWriteString('uniqueId', ParamSet.UniqueId, True);
    JsonWriteString('handle', ParamSet.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchVoltageToJson(Voltage: ISch_Voltage; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Voltage', True);
    JsonWriteCoord('x', Voltage.Location.X, True);
    JsonWriteCoord('y', Voltage.Location.Y, True);
    JsonWriteString('text', Voltage.Text, True);
    JsonWriteInteger('orientation', Ord(Voltage.Orientation), True);
    JsonWriteInteger('fontID', Voltage.FontID, True);
    JsonWriteInteger('color', Voltage.Color, True);
    JsonWriteBoolean('graphicallyLocked', Voltage.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Voltage.Disabled, True);
    JsonWriteBoolean('dimmed', Voltage.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Voltage.CompilationMasked, True);
    JsonWriteString('uniqueId', Voltage.UniqueId, True);
    JsonWriteString('handle', Voltage.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchConnectionLineToJson(ConnLine: ISch_ConnectionLine; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ConnectionLine', True);
    JsonWriteCoord('x', ConnLine.Location.X, True);
    JsonWriteCoord('y', ConnLine.Location.Y, True);
    JsonWriteInteger('verticesCount', ConnLine.VerticesCount, True);
    JsonWriteInteger('lineWidth', Ord(ConnLine.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(ConnLine.LineStyle), True);
    JsonWriteInteger('color', ConnLine.Color, True);
    JsonWriteBoolean('graphicallyLocked', ConnLine.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', ConnLine.Disabled, True);
    JsonWriteBoolean('dimmed', ConnLine.Dimmed, True);
    JsonWriteBoolean('compilationMasked', ConnLine.CompilationMasked, True);
    JsonWriteString('uniqueId', ConnLine.UniqueId, True);
    JsonWriteString('handle', ConnLine.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchCompileMaskToJson(CompileMask: ISch_CompileMask; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CompileMask', True);
    JsonWriteCoord('x', CompileMask.Location.X, True);
    JsonWriteCoord('y', CompileMask.Location.Y, True);
    JsonWriteInteger('color', CompileMask.Color, True);
    JsonWriteBoolean('graphicallyLocked', CompileMask.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', CompileMask.Disabled, True);
    JsonWriteBoolean('dimmed', CompileMask.Dimmed, True);
    JsonWriteBoolean('compilationMasked', CompileMask.CompilationMasked, True);
    JsonWriteString('uniqueId', CompileMask.UniqueId, True);
    JsonWriteString('handle', CompileMask.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchComplexTextToJson(ComplexText: ISch_ComplexText; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ComplexText', True);
    JsonWriteCoord('x', ComplexText.Location.X, True);
    JsonWriteCoord('y', ComplexText.Location.Y, True);
    JsonWriteString('text', ComplexText.Text, True);
    JsonWriteInteger('fontID', ComplexText.FontID, True);
    JsonWriteInteger('color', ComplexText.Color, True);
    JsonWriteBoolean('graphicallyLocked', ComplexText.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', ComplexText.Disabled, True);
    JsonWriteBoolean('dimmed', ComplexText.Dimmed, True);
    JsonWriteBoolean('compilationMasked', ComplexText.CompilationMasked, True);
    JsonWriteString('uniqueId', ComplexText.UniqueId, True);
    JsonWriteString('handle', ComplexText.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchFunctionalBlockToJson(FuncBlock: ISch_FunctionalBlock; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'FunctionalBlock', True);
    JsonWriteCoord('x', FuncBlock.Location.X, True);
    JsonWriteCoord('y', FuncBlock.Location.Y, True);
    JsonWriteString('name', FuncBlock.Name, True);
    JsonWriteInteger('color', FuncBlock.Color, True);
    JsonWriteInteger('areaColor', FuncBlock.AreaColor, True);
    JsonWriteBoolean('graphicallyLocked', FuncBlock.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', FuncBlock.Disabled, True);
    JsonWriteBoolean('dimmed', FuncBlock.Dimmed, True);
    JsonWriteBoolean('compilationMasked', FuncBlock.CompilationMasked, True);
    JsonWriteString('uniqueId', FuncBlock.UniqueId, True);
    JsonWriteString('handle', FuncBlock.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchLineViewToJson(LineView: ISch_LineView; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'LineView', True);
    JsonWriteCoord('x', LineView.Location.X, True);
    JsonWriteCoord('y', LineView.Location.Y, True);
    JsonWriteInteger('color', LineView.Color, True);
    JsonWriteBoolean('graphicallyLocked', LineView.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', LineView.Disabled, True);
    JsonWriteBoolean('dimmed', LineView.Dimmed, True);
    JsonWriteBoolean('compilationMasked', LineView.CompilationMasked, True);
    JsonWriteString('uniqueId', LineView.UniqueId, True);
    JsonWriteString('handle', LineView.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchCollapsiblePolygonToJson(CollPoly: ISch_CollapsiblePolygon; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CollapsiblePolygon', True);
    JsonWriteCoord('x', CollPoly.Location.X, True);
    JsonWriteCoord('y', CollPoly.Location.Y, True);
    JsonWriteInteger('verticesCount', CollPoly.VerticesCount, True);
    JsonWriteInteger('lineWidth', Ord(CollPoly.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(CollPoly.LineStyle), True);
    JsonWriteInteger('color', CollPoly.Color, True);
    JsonWriteInteger('areaColor', CollPoly.AreaColor, True);
    JsonWriteBoolean('isSolid', CollPoly.IsSolid, True);
    JsonWriteBoolean('collapsed', CollPoly.Collapsed, True);
    JsonWriteBoolean('graphicallyLocked', CollPoly.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', CollPoly.Disabled, True);
    JsonWriteBoolean('dimmed', CollPoly.Dimmed, True);
    JsonWriteBoolean('compilationMasked', CollPoly.CompilationMasked, True);
    JsonWriteString('uniqueId', CollPoly.UniqueId, True);
    JsonWriteString('handle', CollPoly.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchCollapsibleRectangleToJson(CollRect: ISch_CollapsibleRectangle; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'CollapsibleRectangle', True);
    JsonWriteCoord('x', CollRect.Location.X, True);
    JsonWriteCoord('y', CollRect.Location.Y, True);
    JsonWriteCoord('cornerX', CollRect.Corner.X, True);
    JsonWriteCoord('cornerY', CollRect.Corner.Y, True);
    JsonWriteInteger('lineWidth', Ord(CollRect.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(CollRect.LineStyle), True);
    JsonWriteInteger('color', CollRect.Color, True);
    JsonWriteInteger('areaColor', CollRect.AreaColor, True);
    JsonWriteBoolean('isSolid', CollRect.IsSolid, True);
    JsonWriteBoolean('collapsed', CollRect.Collapsed, True);
    JsonWriteBoolean('graphicallyLocked', CollRect.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', CollRect.Disabled, True);
    JsonWriteBoolean('dimmed', CollRect.Dimmed, True);
    JsonWriteBoolean('compilationMasked', CollRect.CompilationMasked, True);
    JsonWriteString('uniqueId', CollRect.UniqueId, True);
    JsonWriteString('handle', CollRect.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchReuseSheetSymbolToJson(ReuseSymbol: ISch_ReuseSheetSymbol; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ReuseSheetSymbol', True);
    JsonWriteCoord('x', ReuseSymbol.Location.X, True);
    JsonWriteCoord('y', ReuseSymbol.Location.Y, True);
    JsonWriteCoord('xSize', ReuseSymbol.XSize, True);
    JsonWriteCoord('ySize', ReuseSymbol.YSize, True);
    JsonWriteInteger('color', ReuseSymbol.Color, True);
    JsonWriteInteger('areaColor', ReuseSymbol.AreaColor, True);
    JsonWriteBoolean('graphicallyLocked', ReuseSymbol.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', ReuseSymbol.Disabled, True);
    JsonWriteBoolean('dimmed', ReuseSymbol.Dimmed, True);
    JsonWriteBoolean('compilationMasked', ReuseSymbol.CompilationMasked, True);
    JsonWriteString('uniqueId', ReuseSymbol.UniqueId, True);
    JsonWriteString('handle', ReuseSymbol.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchSchematicBlockToJson(SchBlock: ISch_SchematicBlock; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'SchematicBlock', True);
    JsonWriteCoord('x', SchBlock.Location.X, True);
    JsonWriteCoord('y', SchBlock.Location.Y, True);
    JsonWriteCoord('xSize', SchBlock.XSize, True);
    JsonWriteCoord('ySize', SchBlock.YSize, True);
    JsonWriteInteger('color', SchBlock.Color, True);
    JsonWriteInteger('areaColor', SchBlock.AreaColor, True);
    JsonWriteBoolean('graphicallyLocked', SchBlock.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', SchBlock.Disabled, True);
    JsonWriteBoolean('dimmed', SchBlock.Dimmed, True);
    JsonWriteBoolean('compilationMasked', SchBlock.CompilationMasked, True);
    JsonWriteString('uniqueId', SchBlock.UniqueId, True);
    JsonWriteString('handle', SchBlock.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchImageParameterToJson(ImgParam: ISch_ImageParameter; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'ImageParameter', True);
    JsonWriteCoord('x', ImgParam.Location.X, True);
    JsonWriteCoord('y', ImgParam.Location.Y, True);
    JsonWriteString('name', ImgParam.Name, True);
    JsonWriteInteger('color', ImgParam.Color, True);
    JsonWriteBoolean('graphicallyLocked', ImgParam.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', ImgParam.Disabled, True);
    JsonWriteBoolean('dimmed', ImgParam.Dimmed, True);
    JsonWriteBoolean('compilationMasked', ImgParam.CompilationMasked, True);
    JsonWriteString('uniqueId', ImgParam.UniqueId, True);
    JsonWriteString('handle', ImgParam.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchSymbolToJson(Symbol: ISch_Symbol; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'Symbol', True);
    JsonWriteCoord('x', Symbol.Location.X, True);
    JsonWriteCoord('y', Symbol.Location.Y, True);
    JsonWriteString('name', Symbol.Name, True);
    JsonWriteInteger('color', Symbol.Color, True);
    JsonWriteBoolean('graphicallyLocked', Symbol.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Symbol.Disabled, True);
    JsonWriteBoolean('dimmed', Symbol.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Symbol.CompilationMasked, True);
    JsonWriteString('uniqueId', Symbol.UniqueId, True);
    JsonWriteString('handle', Symbol.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessWiringDiagramToJson(WiringDiagram: ISch_HarnessWiringDiagram; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessWiringDiagram', True);
    JsonWriteCoord('x', WiringDiagram.Location.X, True);
    JsonWriteCoord('y', WiringDiagram.Location.Y, True);
    JsonWriteInteger('color', WiringDiagram.Color, True);
    JsonWriteBoolean('graphicallyLocked', WiringDiagram.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', WiringDiagram.Disabled, True);
    JsonWriteBoolean('dimmed', WiringDiagram.Dimmed, True);
    JsonWriteBoolean('compilationMasked', WiringDiagram.CompilationMasked, True);
    JsonWriteString('uniqueId', WiringDiagram.UniqueId, True);
    JsonWriteString('handle', WiringDiagram.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessLayoutDrawingToJson(LayoutDrawing: ISch_HarnessLayoutDrawing; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessLayoutDrawing', True);
    JsonWriteCoord('x', LayoutDrawing.Location.X, True);
    JsonWriteCoord('y', LayoutDrawing.Location.Y, True);
    JsonWriteInteger('color', LayoutDrawing.Color, True);
    JsonWriteBoolean('graphicallyLocked', LayoutDrawing.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', LayoutDrawing.Disabled, True);
    JsonWriteBoolean('dimmed', LayoutDrawing.Dimmed, True);
    JsonWriteBoolean('compilationMasked', LayoutDrawing.CompilationMasked, True);
    JsonWriteString('uniqueId', LayoutDrawing.UniqueId, True);
    JsonWriteString('handle', LayoutDrawing.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessLayoutLabelToJson(LayoutLabel: ISch_HarnessLayoutLabel; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessLayoutLabel', True);
    JsonWriteCoord('x', LayoutLabel.Location.X, True);
    JsonWriteCoord('y', LayoutLabel.Location.Y, True);
    JsonWriteString('text', LayoutLabel.Text, True);
    JsonWriteInteger('fontID', LayoutLabel.FontID, True);
    JsonWriteInteger('color', LayoutLabel.Color, True);
    JsonWriteBoolean('graphicallyLocked', LayoutLabel.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', LayoutLabel.Disabled, True);
    JsonWriteBoolean('dimmed', LayoutLabel.Dimmed, True);
    JsonWriteBoolean('compilationMasked', LayoutLabel.CompilationMasked, True);
    JsonWriteString('uniqueId', LayoutLabel.UniqueId, True);
    JsonWriteString('handle', LayoutLabel.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessLayoutCoveringToJson(Covering: ISch_HarnessLayoutCovering; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessLayoutCovering', True);
    JsonWriteCoord('x', Covering.Location.X, True);
    JsonWriteCoord('y', Covering.Location.Y, True);
    JsonWriteInteger('color', Covering.Color, True);
    JsonWriteBoolean('graphicallyLocked', Covering.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', Covering.Disabled, True);
    JsonWriteBoolean('dimmed', Covering.Dimmed, True);
    JsonWriteBoolean('compilationMasked', Covering.CompilationMasked, True);
    JsonWriteString('uniqueId', Covering.UniqueId, True);
    JsonWriteString('handle', Covering.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessLayoutConnectionPointToJson(ConnPoint: ISch_HarnessLayoutConnectionPoint; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessLayoutConnectionPoint', True);
    JsonWriteCoord('x', ConnPoint.Location.X, True);
    JsonWriteCoord('y', ConnPoint.Location.Y, True);
    JsonWriteInteger('color', ConnPoint.Color, True);
    JsonWriteBoolean('graphicallyLocked', ConnPoint.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', ConnPoint.Disabled, True);
    JsonWriteBoolean('dimmed', ConnPoint.Dimmed, True);
    JsonWriteBoolean('compilationMasked', ConnPoint.CompilationMasked, True);
    JsonWriteString('uniqueId', ConnPoint.UniqueId, True);
    JsonWriteString('handle', ConnPoint.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessConnectorTypeToJson(ConnType: ISch_HarnessConnectorType; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessConnectorType', True);
    JsonWriteCoord('x', ConnType.Location.X, True);
    JsonWriteCoord('y', ConnType.Location.Y, True);
    JsonWriteString('name', ConnType.Name, True);
    JsonWriteInteger('color', ConnType.Color, True);
    JsonWriteBoolean('graphicallyLocked', ConnType.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', ConnType.Disabled, True);
    JsonWriteBoolean('dimmed', ConnType.Dimmed, True);
    JsonWriteBoolean('compilationMasked', ConnType.CompilationMasked, True);
    JsonWriteString('uniqueId', ConnType.UniqueId, True);
    JsonWriteString('handle', ConnType.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessComponentToJson(HarnessComp: ISch_HarnessComponent; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessComponent', True);
    JsonWriteCoord('x', HarnessComp.Location.X, True);
    JsonWriteCoord('y', HarnessComp.Location.Y, True);
    JsonWriteString('name', HarnessComp.Name, True);
    JsonWriteInteger('color', HarnessComp.Color, True);
    JsonWriteBoolean('graphicallyLocked', HarnessComp.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', HarnessComp.Disabled, True);
    JsonWriteBoolean('dimmed', HarnessComp.Dimmed, True);
    JsonWriteBoolean('compilationMasked', HarnessComp.CompilationMasked, True);
    JsonWriteString('uniqueId', HarnessComp.UniqueId, True);
    JsonWriteString('handle', HarnessComp.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchHarnessLogicalSignalToJson(LogicalSignal: ISch_HarnessLogicalSignal; AddComma: Boolean);
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessLogicalSignal', True);
    JsonWriteCoord('x', LogicalSignal.Location.X, True);
    JsonWriteCoord('y', LogicalSignal.Location.Y, True);
    JsonWriteString('name', LogicalSignal.Name, True);
    JsonWriteInteger('color', LogicalSignal.Color, True);
    JsonWriteBoolean('graphicallyLocked', LogicalSignal.GraphicallyLocked, True);
    JsonWriteBoolean('disabled', LogicalSignal.Disabled, True);
    JsonWriteBoolean('dimmed', LogicalSignal.Dimmed, True);
    JsonWriteBoolean('compilationMasked', LogicalSignal.CompilationMasked, True);
    JsonWriteString('uniqueId', LogicalSignal.UniqueId, True);
    JsonWriteString('handle', LogicalSignal.Handle, False);
    JsonCloseObject(AddComma);
end;

procedure ExportSchComponentToJson(Comp: ISch_Component; AddComma: Boolean);
var
    Iterator: ISch_Iterator;
    Prim: ISch_GraphicalObject;
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
    JsonWriteString('symbolItemGUID', Comp.SymbolItemGUID, True);
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
    JsonWriteBoolean('overideColors', Comp.OverideColors, True);
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
    JsonWriteBoolean('graphicallyLocked', Comp.GraphicallyLocked, True);

    // Unique identifiers
    JsonWriteString('uniqueId', Comp.UniqueId, True);
    JsonWriteString('handle', Comp.Handle, True);

    // Export child primitives
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

    JsonCloseArray(False);  // primitives
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

    // Metadata
    JsonOpenObject('metadata');
    JsonWriteString('exportType', 'SchLib', True);
    JsonWriteString('fileName', ExtractFileName(SchLib.DocumentName), True);
    JsonWriteString('exportedBy', 'AltiumSharp TestDataGenerator', True);
    JsonWriteString('version', '1.0', False);
    JsonCloseObject(True);

    // Symbols array
    JsonOpenArray('symbols');

    // Iterate all components in the library using SchLibIterator_Create
    LibIterator := SchLib.SchLibIterator_Create;
    LibIterator.AddFilter_ObjectSet(MkSet(eSchComponent));

    Comp := LibIterator.FirstSchObject;
    while Comp <> nil do
    begin
        ExportSchComponentToJson(Comp, True);
        Comp := LibIterator.NextSchObject;
    end;

    SchLib.SchIterator_Destroy(LibIterator);

    JsonCloseArray(False);  // symbols
    JsonCloseObject(False);  // root

    JsonEnd(JsonPath);
end;

{==============================================================================
  SCHEMATIC DOCUMENT JSON EXPORTER
==============================================================================}

procedure ExportSchDocToJson(SchDoc: ISch_Document; JsonPath: String);
var
    Iterator: ISch_Iterator;
    Primitive: ISch_GraphicalObject;
    Wire: ISch_Wire;
    Bus: ISch_Bus;
    BusEntry: ISch_BusEntry;
    Junction: ISch_Junction;
    NetLabel: ISch_NetLabel;
    Port: ISch_Port;
    PowerObject: ISch_PowerObject;
    SheetSymbol: ISch_SheetSymbol;
    NoERC: ISch_NoERC;
    Parameter: ISch_Parameter;
    Comp: ISch_Component;
    Line: ISch_Line;
    Rectangle: ISch_Rectangle;
    I: Integer;
begin
    if SchDoc = nil then Exit;

    JsonBegin;
    JsonOpenObject('');
    JsonWriteString('exportType', 'SchDoc', True);
    JsonWriteString('fileName', ExtractFileName(SchDoc.DocumentName), True);
    JsonWriteString('documentName', SchDoc.DocumentName, True);

    // Export primitives array
    JsonOpenArray('primitives');

    Iterator := SchDoc.SchIterator_Create;
    Iterator.AddFilter_ObjectSet(MkSet(eWire, eBus, eBusEntry, eJunction,
        eNetLabel, ePort, ePowerObject, eSheetSymbol, eNoERC, eParameter,
        eSchComponent, eLine, eRectangle, eArc, eEllipse, eRoundRectangle));

    Primitive := Iterator.FirstSchObject;
    I := 0;
    while Primitive <> nil do
    begin
        if I > 0 then
            JsonOutput.Strings[JsonOutput.Count - 1] := JsonOutput.Strings[JsonOutput.Count - 1] + ',';

        case Primitive.ObjectId of
            eWire:
            begin
                Wire := Primitive;
                JsonOpenObject('');
                JsonWriteString('objectType', 'Wire', True);
                JsonWriteInteger('x1', Wire.Location.X, True);
                JsonWriteInteger('y1', Wire.Location.Y, True);
                JsonWriteInteger('vertexCount', Wire.VerticesCount, True);
                JsonWriteInteger('lineWidth', Ord(Wire.LineWidth), True);
                JsonWriteInteger('color', Wire.Color, True);
                JsonWriteString('uniqueId', Wire.UniqueId, False);
                JsonCloseObject(False);
            end;
            eBus:
            begin
                Bus := Primitive;
                JsonOpenObject('');
                JsonWriteString('objectType', 'Bus', True);
                JsonWriteInteger('x1', Bus.Location.X, True);
                JsonWriteInteger('y1', Bus.Location.Y, True);
                JsonWriteInteger('vertexCount', Bus.VerticesCount, True);
                JsonWriteInteger('lineWidth', Ord(Bus.LineWidth), True);
                JsonWriteInteger('color', Bus.Color, True);
                JsonWriteString('uniqueId', Bus.UniqueId, False);
                JsonCloseObject(False);
            end;
            eBusEntry:
            begin
                BusEntry := Primitive;
                JsonOpenObject('');
                JsonWriteString('objectType', 'BusEntry', True);
                JsonWriteInteger('x1', BusEntry.Location.X, True);
                JsonWriteInteger('y1', BusEntry.Location.Y, True);
                JsonWriteInteger('x2', BusEntry.Corner.X, True);
                JsonWriteInteger('y2', BusEntry.Corner.Y, True);
                JsonWriteInteger('color', BusEntry.Color, True);
                JsonWriteString('uniqueId', BusEntry.UniqueId, False);
                JsonCloseObject(False);
            end;
            eJunction:
            begin
                Junction := Primitive;
                JsonOpenObject('');
                JsonWriteString('objectType', 'Junction', True);
                JsonWriteInteger('x', Junction.Location.X, True);
                JsonWriteInteger('y', Junction.Location.Y, True);
                JsonWriteInteger('size', Ord(Junction.Size), True);
                JsonWriteInteger('color', Junction.Color, True);
                JsonWriteString('uniqueId', Junction.UniqueId, False);
                JsonCloseObject(False);
            end;
            eNetLabel:
            begin
                NetLabel := Primitive;
                JsonOpenObject('');
                JsonWriteString('objectType', 'NetLabel', True);
                JsonWriteInteger('x', NetLabel.Location.X, True);
                JsonWriteInteger('y', NetLabel.Location.Y, True);
                JsonWriteString('text', NetLabel.Text, True);
                JsonWriteInteger('orientation', Ord(NetLabel.Orientation), True);
                JsonWriteInteger('color', NetLabel.Color, True);
                JsonWriteString('uniqueId', NetLabel.UniqueId, False);
                JsonCloseObject(False);
            end;
            ePort:
            begin
                Port := Primitive;
                JsonOpenObject('');
                JsonWriteString('objectType', 'Port', True);
                JsonWriteInteger('x', Port.Location.X, True);
                JsonWriteInteger('y', Port.Location.Y, True);
                JsonWriteString('name', Port.Name, True);
                JsonWriteInteger('ioType', Ord(Port.IOType), True);
                JsonWriteInteger('style', Ord(Port.Style), True);
                JsonWriteInteger('alignment', Ord(Port.Alignment), True);
                JsonWriteInteger('width', Port.Width, True);
                JsonWriteInteger('height', Port.Height, True);
                JsonWriteInteger('Color', Port.Color, True);
                JsonWriteInteger('areaColor', Port.AreaColor, True);
                JsonWriteInteger('textColor', Port.TextColor, True);
                JsonWriteString('uniqueId', Port.UniqueId, False);
                JsonCloseObject(False);
            end;
            ePowerObject:
            begin
                PowerObject := Primitive;
                JsonOpenObject('');
                JsonWriteString('objectType', 'PowerObject', True);
                JsonWriteInteger('x', PowerObject.Location.X, True);
                JsonWriteInteger('y', PowerObject.Location.Y, True);
                JsonWriteString('text', PowerObject.Text, True);
                JsonWriteInteger('style', Ord(PowerObject.Style), True);
                JsonWriteInteger('orientation', Ord(PowerObject.Orientation), True);
                JsonWriteInteger('color', PowerObject.Color, True);
                JsonWriteBoolean('showNetName', PowerObject.ShowNetName, True);
                JsonWriteString('uniqueId', PowerObject.UniqueId, False);
                JsonCloseObject(False);
            end;
            eSheetSymbol:
            begin
                SheetSymbol := Primitive;
                JsonOpenObject('');
                JsonWriteString('objectType', 'SheetSymbol', True);
                JsonWriteInteger('x', SheetSymbol.Location.X, True);
                JsonWriteInteger('y', SheetSymbol.Location.Y, True);
                JsonWriteInteger('xSize', SheetSymbol.XSize, True);
                JsonWriteInteger('ySize', SheetSymbol.YSize, True);
                JsonWriteString('sheetName', SheetSymbol.SheetName, True);
                JsonWriteString('fileName', SheetSymbol.FileName, True);
                JsonWriteString('designator', SheetSymbol.Designator, True);
                JsonWriteBoolean('isMirrored', SheetSymbol.IsMirrored, True);
                JsonWriteInteger('color', SheetSymbol.Color, True);
                JsonWriteInteger('areaColor', SheetSymbol.AreaColor, True);
                JsonWriteString('uniqueId', SheetSymbol.UniqueId, False);
                JsonCloseObject(False);
            end;
            eNoERC:
            begin
                NoERC := Primitive;
                JsonOpenObject('');
                JsonWriteString('objectType', 'NoERC', True);
                JsonWriteInteger('x', NoERC.Location.X, True);
                JsonWriteInteger('y', NoERC.Location.Y, True);
                JsonWriteInteger('symbol', Ord(NoERC.Symbol), True);
                JsonWriteInteger('orientation', Ord(NoERC.Orientation), True);
                JsonWriteInteger('color', NoERC.Color, True);
                JsonWriteString('uniqueId', NoERC.UniqueId, False);
                JsonCloseObject(False);
            end;
            eParameter:
            begin
                Parameter := Primitive;
                JsonOpenObject('');
                JsonWriteString('objectType', 'Parameter', True);
                JsonWriteInteger('x', Parameter.Location.X, True);
                JsonWriteInteger('y', Parameter.Location.Y, True);
                JsonWriteString('name', Parameter.Name, True);
                JsonWriteString('text', Parameter.Text, True);
                JsonWriteBoolean('isHidden', Parameter.IsHidden, True);
                JsonWriteInteger('color', Parameter.Color, True);
                JsonWriteString('uniqueId', Parameter.UniqueId, False);
                JsonCloseObject(False);
            end;
            eSchComponent:
            begin
                Comp := Primitive;
                ExportSchComponentToJson(Comp, False);
            end;
            eLine:
            begin
                Line := Primitive;
                ExportSchLineToJson(Line, False);
            end;
            eRectangle:
            begin
                Rectangle := Primitive;
                ExportSchRectangleToJson(Rectangle, False);
            end;
        end;

        Inc(I);
        Primitive := Iterator.NextSchObject;
    end;

    SchDoc.SchIterator_Destroy(Iterator);

    JsonCloseArray(False);  // primitives
    JsonCloseObject(False);  // root

    JsonEnd(JsonPath);
end;

{==============================================================================
  PCB DOCUMENT JSON EXPORTER
==============================================================================}

procedure ExportPcbDocToJson(Board: IPCB_Board; JsonPath: String);
var
    Iterator: IPCB_BoardIterator;
    Primitive: IPCB_Primitive;
    I: Integer;
begin
    if Board = nil then Exit;

    JsonBegin;
    JsonOpenObject('');
    JsonWriteString('exportType', 'PcbDoc', True);
    JsonWriteString('fileName', ExtractFileName(Board.FileName), True);
    JsonWriteString('displayUnit', Board.DisplayUnit, True);
    JsonWriteCoord('boardXOrigin', Board.XOrigin, True);
    JsonWriteCoord('boardYOrigin', Board.YOrigin, True);

    // Export board information
    JsonOpenObject('boardInfo');
    JsonWriteCoord('boundingRectLeft', Board.BoundingRectangle.Left, True);
    JsonWriteCoord('boundingRectBottom', Board.BoundingRectangle.Bottom, True);
    JsonWriteCoord('boundingRectRight', Board.BoundingRectangle.Right, True);
    JsonWriteCoord('boundingRectTop', Board.BoundingRectangle.Top, True);
    JsonCloseObject(True);

    // Export primitives array
    JsonOpenArray('primitives');

    Iterator := Board.BoardIterator_Create;
    Iterator.AddFilter_ObjectSet(MkSet(eTrackObject, eArcObject, eViaObject,
        ePadObject, eTextObject, eFillObject, eRegionObject, ePolyObject,
        eDimensionObject, eComponentObject, eNetObject));
    Iterator.AddFilter_LayerSet(AllLayers);

    Primitive := Iterator.FirstPCBObject;
    I := 0;
    while Primitive <> nil do
    begin
        if I > 0 then
            JsonOutput.Strings[JsonOutput.Count - 1] := JsonOutput.Strings[JsonOutput.Count - 1] + ',';

        case Primitive.ObjectId of
            eTrackObject: ExportPcbTrackToJson(Primitive, False);
            eArcObject: ExportPcbArcToJson(Primitive, False);
            eViaObject: ExportPcbViaToJson(Primitive, False);
            ePadObject: ExportPcbPadToJson(Primitive, False);
            eTextObject: ExportPcbTextToJson(Primitive, False);
            eFillObject: ExportPcbFillToJson(Primitive, False);
            eRegionObject: ExportPcbRegionToJson(Primitive, False);
            ePolyObject: ExportPcbPolygonToJson(Primitive, False);
            eDimensionObject: ExportPcbDimensionToJson(Primitive, False);
            eNetObject:
            begin
                JsonOpenObject('');
                JsonWriteString('objectType', 'Net', True);
                JsonWriteString('name', Primitive.Name, False);
                JsonCloseObject(False);
            end;
        end;

        Inc(I);
        Primitive := Iterator.NextPCBObject;
    end;

    Board.BoardIterator_Destroy(Iterator);

    JsonCloseArray(False);  // primitives
    JsonCloseObject(False);  // root

    JsonEnd(JsonPath);
end;

{==============================================================================
  MAIN GENERATION PROCEDURES
==============================================================================}

procedure GeneratePcbLibrary;
var
    PCBLib: IPCB_Library;
    Doc: IServerDocument;
    FilePath, JsonPath: String;
begin
    EnsureDirectoryExists(OUTPUT_DIR);

    FilePath := OUTPUT_DIR + 'TestPcbLib.PcbLib';
    JsonPath := OUTPUT_DIR + 'TestPcbLib.json';

    // Delete existing files to start fresh
    if FileExists(FilePath) then DeleteFile(FilePath);
    if FileExists(JsonPath) then DeleteFile(JsonPath);

    // Create new standalone PCB library (not added to any project)
    Doc := Client.OpenNewDocument('PcbLib', FilePath, '', False);
    if Doc = nil then Exit;
    Client.ShowDocument(Doc);
    PCBLib := PCBServer.GetCurrentPCBLibrary;
    if PCBLib = nil then Exit;

    // Remove the default empty component
    RemoveDefaultPcbLibComponent(PCBLib);

    // Generate test footprints
    GeneratePcbLibTestFootprints(PCBLib);

    // Save document
    Doc.DoFileSave('PcbLib');

    PCBLib.Board.ViewManager_FullUpdate;

    // Export to JSON
    ExportPcbLibToJson(PCBLib, JsonPath);

    // Close the document tab
    CloseDocument(FilePath);

    // Also generate individual PCB library files
    GenerateIndividualPcbLibFiles;
end;

procedure GenerateSchLibrary;
var
    SchLib: ISch_Lib;
    Doc: IServerDocument;
    FilePath, JsonPath: String;
begin
    EnsureDirectoryExists(OUTPUT_DIR);

    FilePath := OUTPUT_DIR + 'TestSchLib.SchLib';
    JsonPath := OUTPUT_DIR + 'TestSchLib.json';

    // Delete existing files to start fresh
    if FileExists(FilePath) then DeleteFile(FilePath);
    if FileExists(JsonPath) then DeleteFile(JsonPath);

    // Create new standalone schematic library (not added to any project)
    Doc := Client.OpenNewDocument('SchLib', FilePath, '', False);
    if Doc = nil then Exit;
    Client.ShowDocument(Doc);
    SchLib := SchServer.GetCurrentSchDocument;
    if SchLib = nil then Exit;

    // Remove the default empty component
    RemoveDefaultSchLibComponent(SchLib);

    // Generate test symbols
    GenerateSchLibTestSymbols(SchLib);

    // Save document
    Doc.DoFileSave('SchLib');

    // Export to JSON
    ExportSchLibToJson(SchLib, JsonPath);

    // Close the document tab
    CloseDocument(FilePath);

    // Generate individual SchLib files
    GenerateIndividualSchLibFiles;
end;

{==============================================================================
  SCHEMATIC DOCUMENT (SchDoc) GENERATOR
==============================================================================}

procedure GenerateSchDocTestFile;
var
    SchDoc: ISch_Document;
    Doc: IServerDocument;
    FilePath, JsonPath: String;
    Wire: ISch_Wire;
    Port: ISch_Port;
    PowerObject: ISch_PowerObject;
    NetLabel: ISch_NetLabel;
    SheetSymbol: ISch_SheetSymbol;
    SheetEntry: ISch_SheetEntry;
    Junction: ISch_Junction;
    NoERC: ISch_NoERC;
    Comp: ISch_Component;
    Bus: ISch_Bus;
    BusEntry: ISch_BusEntry;
    Parameter: ISch_Parameter;
    CrossSheetConnector: ISch_CrossSheetConnector;
    Blanket: ISch_Blanket;
    I: Integer;
begin
    FilePath := OUTPUT_DIR + 'TestSchDoc.SchDoc';
    JsonPath := OUTPUT_DIR + 'TestSchDoc.json';

    // Delete existing files
    if FileExists(FilePath) then DeleteFile(FilePath);
    if FileExists(JsonPath) then DeleteFile(JsonPath);

    // Create new schematic document
    Doc := Client.OpenNewDocument('Sch', FilePath, '', False);
    if Doc = nil then Exit;
    Client.ShowDocument(Doc);

    SchDoc := SchServer.GetCurrentSchDocument;
    if SchDoc = nil then Exit;

    SchServer.ProcessControl.PreProcess(SchDoc, '');

    // === Add test wires ===
    // Horizontal wire
    Wire := SchServer.SchObjectFactory(eWire, eCreate_Default);
    Wire.Location := Point(MilsToCoord(1000), MilsToCoord(1000));
    Wire.SetState_Vertex(1, Point(MilsToCoord(1500), MilsToCoord(1000)));
    Wire.LineWidth := eSmall;
    SchDoc.RegisterSchObjectInContainer(Wire);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, Wire.I_ObjectAddress);

    // Vertical wire
    Wire := SchServer.SchObjectFactory(eWire, eCreate_Default);
    Wire.Location := Point(MilsToCoord(1500), MilsToCoord(1000));
    Wire.SetState_Vertex(1, Point(MilsToCoord(1500), MilsToCoord(1500)));
    Wire.LineWidth := eSmall;
    SchDoc.RegisterSchObjectInContainer(Wire);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, Wire.I_ObjectAddress);

    // === Add test bus ===
    Bus := SchServer.SchObjectFactory(eBus, eCreate_Default);
    Bus.Location := Point(MilsToCoord(2000), MilsToCoord(1000));
    Bus.SetState_Vertex(1, Point(MilsToCoord(2500), MilsToCoord(1000)));
    Bus.LineWidth := eMedium;
    SchDoc.RegisterSchObjectInContainer(Bus);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, Bus.I_ObjectAddress);

    // === Add bus entry ===
    BusEntry := SchServer.SchObjectFactory(eBusEntry, eCreate_Default);
    BusEntry.Location := Point(MilsToCoord(2000), MilsToCoord(1000));
    BusEntry.Corner := Point(MilsToCoord(2100), MilsToCoord(1100));
    SchDoc.RegisterSchObjectInContainer(BusEntry);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, BusEntry.I_ObjectAddress);

    // === Add junction ===
    Junction := SchServer.SchObjectFactory(eJunction, eCreate_Default);
    Junction.Location := Point(MilsToCoord(1500), MilsToCoord(1000));
    Junction.Size := eMedium;
    SchDoc.RegisterSchObjectInContainer(Junction);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, Junction.I_ObjectAddress);

    // === Add net labels ===
    NetLabel := SchServer.SchObjectFactory(eNetLabel, eCreate_Default);
    NetLabel.Location := Point(MilsToCoord(1200), MilsToCoord(1000));
    NetLabel.Text := 'NET1';
    NetLabel.Orientation := eRotate0;
    SchDoc.RegisterSchObjectInContainer(NetLabel);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, NetLabel.I_ObjectAddress);

    NetLabel := SchServer.SchObjectFactory(eNetLabel, eCreate_Default);
    NetLabel.Location := Point(MilsToCoord(2200), MilsToCoord(1000));
    NetLabel.Text := 'BUS[0..7]';
    NetLabel.Orientation := eRotate0;
    SchDoc.RegisterSchObjectInContainer(NetLabel);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, NetLabel.I_ObjectAddress);

    // === Add ports - various styles ===
    Port := SchServer.SchObjectFactory(ePort, eCreate_Default);
    Port.Location := Point(MilsToCoord(500), MilsToCoord(1000));
    Port.Name := 'INPUT_PORT';
    Port.IOType := ePortInput;
    Port.Style := ePortLeft;
    SchDoc.RegisterSchObjectInContainer(Port);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, Port.I_ObjectAddress);

    Port := SchServer.SchObjectFactory(ePort, eCreate_Default);
    Port.Location := Point(MilsToCoord(3000), MilsToCoord(1000));
    Port.Name := 'OUTPUT_PORT';
    Port.IOType := ePortOutput;
    Port.Style := ePortRight;
    SchDoc.RegisterSchObjectInContainer(Port);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, Port.I_ObjectAddress);

    Port := SchServer.SchObjectFactory(ePort, eCreate_Default);
    Port.Location := Point(MilsToCoord(1500), MilsToCoord(2000));
    Port.Name := 'BIDIR_PORT';
    Port.IOType := ePortBidirectional;
    Port.Style := ePortLeftRight;
    SchDoc.RegisterSchObjectInContainer(Port);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, Port.I_ObjectAddress);

    // === Add power objects - various styles ===
    PowerObject := SchServer.SchObjectFactory(ePowerObject, eCreate_Default);
    PowerObject.Location := Point(MilsToCoord(500), MilsToCoord(500));
    PowerObject.Text := 'VCC';
    PowerObject.Style := ePowerBar;
    SchDoc.RegisterSchObjectInContainer(PowerObject);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, PowerObject.I_ObjectAddress);

    PowerObject := SchServer.SchObjectFactory(ePowerObject, eCreate_Default);
    PowerObject.Location := Point(MilsToCoord(1000), MilsToCoord(500));
    PowerObject.Text := 'GND';
    PowerObject.Style := ePowerGndPower;
    SchDoc.RegisterSchObjectInContainer(PowerObject);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, PowerObject.I_ObjectAddress);

    PowerObject := SchServer.SchObjectFactory(ePowerObject, eCreate_Default);
    PowerObject.Location := Point(MilsToCoord(1500), MilsToCoord(500));
    PowerObject.Text := 'EARTH';
    PowerObject.Style := ePowerGndEarth;
    SchDoc.RegisterSchObjectInContainer(PowerObject);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, PowerObject.I_ObjectAddress);

    PowerObject := SchServer.SchObjectFactory(ePowerObject, eCreate_Default);
    PowerObject.Location := Point(MilsToCoord(2000), MilsToCoord(500));
    PowerObject.Text := '+5V';
    PowerObject.Style := ePowerCircle;
    SchDoc.RegisterSchObjectInContainer(PowerObject);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, PowerObject.I_ObjectAddress);

    // === Add sheet symbol ===
    {
    SheetSymbol := SchServer.SchObjectFactory(eSheetSymbol, eCreate_Default);
    SheetSymbol.Location := Point(MilsToCoord(3500), MilsToCoord(1000));
    SheetSymbol.XSize := MilsToCoord(500);
    SheetSymbol.YSize := MilsToCoord(400);
    SheetSymbol.SelectSchSheetFileName('SubSheet.SchDoc');
    //SheetSymbol.SheetName := 'SubSheet';  // readonly
    //SheetSymbol.SheetFileName := 'SubSheet.SchDoc';  // readonly
    SheetSymbol.Designator := 'Sheet1';

    // Add sheet entries
    SheetEntry := SchServer.SchObjectFactory(eSheetEntry, eCreate_Default);
    SheetEntry.Name := 'IN1';
    SheetEntry.IOType := ePortInput;
    SheetEntry.Side := eSheetLeftSide;
    SheetEntry.DistanceFromTop := MilsToCoord(100);
    SheetSymbol.AddSchObject(SheetEntry);

    SheetEntry := SchServer.SchObjectFactory(eSheetEntry, eCreate_Default);
    SheetEntry.Name := 'OUT1';
    SheetEntry.IOType := ePortOutput;
    SheetEntry.Side := eSheetRightSide;
    SheetEntry.DistanceFromTop := MilsToCoord(100);
    SheetSymbol.AddSchObject(SheetEntry);

    SchDoc.RegisterSchObjectInContainer(SheetSymbol);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, SheetSymbol.I_ObjectAddress);
    }

    // === Add cross-sheet connector ===
    CrossSheetConnector := SchServer.SchObjectFactory(eCrossSheetConnector, eCreate_Default);
    CrossSheetConnector.Location := Point(MilsToCoord(4200), MilsToCoord(1000));
    CrossSheetConnector.Text := 'NET1';
    SchDoc.RegisterSchObjectInContainer(CrossSheetConnector);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, CrossSheetConnector.I_ObjectAddress);

    // === Add blanket ===
    Blanket := SchServer.SchObjectFactory(eBlanket, eCreate_Default);
    Blanket.Location := Point(MilsToCoord(500), MilsToCoord(2000));
    SchDoc.RegisterSchObjectInContainer(Blanket);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, Blanket.I_ObjectAddress);

    // === Add NoERC marker ===
    NoERC := SchServer.SchObjectFactory(eNoERC, eCreate_Default);
    NoERC.Location := Point(MilsToCoord(2500), MilsToCoord(1500));
    NoERC.Symbol := eNoERC;
    SchDoc.RegisterSchObjectInContainer(NoERC);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, NoERC.I_ObjectAddress);

    // === Add parameter (document-level) ===
    Parameter := SchServer.SchObjectFactory(eParameter, eCreate_Default);
    Parameter.Location := Point(MilsToCoord(100), MilsToCoord(100));
    Parameter.Name := 'TestParameter';
    Parameter.Text := 'TestValue';
    Parameter.IsHidden := False;
    SchDoc.RegisterSchObjectInContainer(Parameter);
    SchServer.RobotManager.SendMessage(SchDoc.I_ObjectAddress, c_BroadCast,
        SCHM_PrimitiveRegistration, Parameter.I_ObjectAddress);

    SchServer.ProcessControl.PostProcess(SchDoc, '');

    // Save the document
    Doc.DoFileSave('Sch');

    // Export to JSON
    ExportSchDocToJson(SchDoc, JsonPath);

    CloseDocument(FilePath);
end;

{==============================================================================
  PCB DOCUMENT (PcbDoc) GENERATOR
==============================================================================}

procedure GeneratePcbDocTestFile;
var
    PCBBoard: IPCB_Board;
    Doc: IServerDocument;
    FilePath, JsonPath: String;
    Track: IPCB_Track;
    Arc: IPCB_Arc;
    Via: IPCB_Via;
    Pad: IPCB_Pad;
    Text: IPCB_Text;
    Fill: IPCB_Fill;
    Region: IPCB_Region;
    Polygon: IPCB_Polygon;
    Dimension: IPCB_Dimension;
    Net: IPCB_Net;
    Contour: IPCB_Contour;
    Segment: TPolySegment;

    I: Integer;
begin
    FilePath := OUTPUT_DIR + 'TestPcbDoc.PcbDoc';
    JsonPath := OUTPUT_DIR + 'TestPcbDoc.json';

    // Delete existing files
    if FileExists(FilePath) then DeleteFile(FilePath);
    if FileExists(JsonPath) then DeleteFile(JsonPath);

    // Create new PCB document
    Doc := Client.OpenNewDocument('PCB', FilePath, '', False);
    if Doc = nil then Exit;
    Client.ShowDocument(Doc);

    PCBBoard := PCBServer.GetCurrentPCBBoard;
    if PCBBoard = nil then Exit;

    PCBServer.PreProcess;

    // === Create test nets ===
    Net := PCBServer.PCBObjectFactory(eNetObject, eNoDimension, eCreate_Default);
    Net.Name := 'NET1';
    PCBBoard.AddPCBObject(Net);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Net.I_ObjectAddress);

    Net := PCBServer.PCBObjectFactory(eNetObject, eNoDimension, eCreate_Default);
    Net.Name := 'VCC';
    PCBBoard.AddPCBObject(Net);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Net.I_ObjectAddress);

    Net := PCBServer.PCBObjectFactory(eNetObject, eNoDimension, eCreate_Default);
    Net.Name := 'GND';
    PCBBoard.AddPCBObject(Net);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Net.I_ObjectAddress);

    // === Add test tracks - various widths and layers ===
    // Top layer track
    Track := PCBServer.PCBObjectFactory(eTrackObject, eNoDimension, eCreate_Default);
    Track.X1 := MilsToCoord(1000);
    Track.Y1 := MilsToCoord(1000);
    Track.X2 := MilsToCoord(2000);
    Track.Y2 := MilsToCoord(1000);
    Track.Width := MilsToCoord(10);
    Track.Layer := eTopLayer;
    PCBBoard.AddPCBObject(Track);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Track.I_ObjectAddress);

    // Bottom layer track
    Track := PCBServer.PCBObjectFactory(eTrackObject, eNoDimension, eCreate_Default);
    Track.X1 := MilsToCoord(1000);
    Track.Y1 := MilsToCoord(900);
    Track.X2 := MilsToCoord(2000);
    Track.Y2 := MilsToCoord(900);
    Track.Width := MilsToCoord(15);
    Track.Layer := eBottomLayer;
    PCBBoard.AddPCBObject(Track);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Track.I_ObjectAddress);

    // Top overlay track
    Track := PCBServer.PCBObjectFactory(eTrackObject, eNoDimension, eCreate_Default);
    Track.X1 := MilsToCoord(1000);
    Track.Y1 := MilsToCoord(800);
    Track.X2 := MilsToCoord(2000);
    Track.Y2 := MilsToCoord(800);
    Track.Width := MilsToCoord(8);
    Track.Layer := eTopOverlay;
    PCBBoard.AddPCBObject(Track);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Track.I_ObjectAddress);

    // === Add test arcs ===
    Arc := PCBServer.PCBObjectFactory(eArcObject, eNoDimension, eCreate_Default);
    Arc.XCenter := MilsToCoord(1500);
    Arc.YCenter := MilsToCoord(1500);
    Arc.Radius := MilsToCoord(200);
    Arc.LineWidth := MilsToCoord(10);
    Arc.StartAngle := 0;
    Arc.EndAngle := 90;
    Arc.Layer := eTopLayer;
    PCBBoard.AddPCBObject(Arc);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Arc.I_ObjectAddress);

    // Full circle arc
    Arc := PCBServer.PCBObjectFactory(eArcObject, eNoDimension, eCreate_Default);
    Arc.XCenter := MilsToCoord(2000);
    Arc.YCenter := MilsToCoord(1500);
    Arc.Radius := MilsToCoord(150);
    Arc.LineWidth := MilsToCoord(10);
    Arc.StartAngle := 0;
    Arc.EndAngle := 360;
    Arc.Layer := eTopLayer;
    PCBBoard.AddPCBObject(Arc);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Arc.I_ObjectAddress);

    // === Add test vias ===
    // Standard through-hole via
    Via := PCBServer.PCBObjectFactory(eViaObject, eNoDimension, eCreate_Default);
    Via.X := MilsToCoord(1200);
    Via.Y := MilsToCoord(1000);
    Via.Size := MilsToCoord(40);
    Via.HoleSize := MilsToCoord(20);
    Via.LowLayer := eTopLayer;
    Via.HighLayer := eBottomLayer;
    PCBBoard.AddPCBObject(Via);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Via.I_ObjectAddress);

    // Tented via
    Via := PCBServer.PCBObjectFactory(eViaObject, eNoDimension, eCreate_Default);
    Via.X := MilsToCoord(1400);
    Via.Y := MilsToCoord(1000);
    Via.Size := MilsToCoord(30);
    Via.HoleSize := MilsToCoord(15);
    Via.LowLayer := eTopLayer;
    Via.HighLayer := eBottomLayer;
    Via.IsTenting_Top := True;
    Via.IsTenting_Bottom := True;
    PCBBoard.AddPCBObject(Via);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Via.I_ObjectAddress);

    // === Add test pads ===
    // Through-hole round pad
    Pad := PCBServer.PCBObjectFactory(ePadObject, eNoDimension, eCreate_Default);
    Pad.X := MilsToCoord(2500);
    Pad.Y := MilsToCoord(1000);
    Pad.TopXSize := MilsToCoord(60);
    Pad.TopYSize := MilsToCoord(60);
    Pad.HoleSize := MilsToCoord(30);
    Pad.TopShape := eRounded;
    Pad.Layer := eMultiLayer;
    Pad.Name := '1';
    PCBBoard.AddPCBObject(Pad);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Pad.I_ObjectAddress);

    // SMD rectangular pad
    Pad := PCBServer.PCBObjectFactory(ePadObject, eNoDimension, eCreate_Default);
    Pad.X := MilsToCoord(2700);
    Pad.Y := MilsToCoord(1000);
    Pad.TopXSize := MilsToCoord(50);
    Pad.TopYSize := MilsToCoord(30);
    Pad.HoleSize := 0;
    Pad.TopShape := eRectangular;
    Pad.Layer := eTopLayer;
    Pad.Name := '2';
    PCBBoard.AddPCBObject(Pad);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Pad.I_ObjectAddress);

    // === Add test text ===
    Text := PCBServer.PCBObjectFactory(eTextObject, eNoDimension, eCreate_Default);
    Text.XLocation := MilsToCoord(1000);
    Text.YLocation := MilsToCoord(2000);
    Text.Text := 'Test PCB Document';
    Text.Size := MilsToCoord(100);
    Text.Width := MilsToCoord(10);
    Text.Layer := eTopOverlay;
    Text.Bold := False;
    Text.Italic := False;
    PCBBoard.AddPCBObject(Text);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Text.I_ObjectAddress);

    // TrueType text
    Text := PCBServer.PCBObjectFactory(eTextObject, eNoDimension, eCreate_Default);
    Text.XLocation := MilsToCoord(1000);
    Text.YLocation := MilsToCoord(2200);
    Text.Text := 'TrueType Font';
    Text.Size := MilsToCoord(80);
    Text.Layer := eTopOverlay;
    Text.UseTTFonts := True;
    Text.FontName := 'Arial';
    Text.Bold := True;
    Text.Italic := False;
    PCBBoard.AddPCBObject(Text);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Text.I_ObjectAddress);

    // === Add test fill ===
    Fill := PCBServer.PCBObjectFactory(eFillObject, eNoDimension, eCreate_Default);
    Fill.X1Location := MilsToCoord(3000);
    Fill.Y1Location := MilsToCoord(1000);
    Fill.X2Location := MilsToCoord(3200);
    Fill.Y2Location := MilsToCoord(1200);
    Fill.Layer := eTopLayer;
    Fill.Rotation := 0;
    PCBBoard.AddPCBObject(Fill);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Fill.I_ObjectAddress);

    // === Add test region ===
    Region := PCBServer.PCBObjectFactory(eRegionObject, eNoDimension, eCreate_Default);
    Region.Layer := eTopLayer;
    Region.Kind := eRegionKind_Copper;

    Contour := PCBServer.PCBContourFactory;
    Contour.AddPoint(MilsToCoord(3500), MilsToCoord(1000));
    Contour.AddPoint(MilsToCoord(3800), MilsToCoord(1000));
    Contour.AddPoint(MilsToCoord(3800), MilsToCoord(1300));
    Contour.AddPoint(MilsToCoord(3500), MilsToCoord(1300));
    Region.SetOutlineContour(Contour);

    PCBBoard.AddPCBObject(Region);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Region.I_ObjectAddress);

    // === Add test polygon ===
    Polygon := PCBServer.PCBObjectFactory(ePolyObject, eNoDimension, eCreate_Default);
    Polygon.Layer := eTopLayer;
    Polygon.PolyHatchStyle := ePolySolid;
    Polygon.MinTrack := MilsToCoord(10);
    Polygon.ArcApproximation := MilsToCoord(1);
    Polygon.RemoveIslandsByArea := True;
    Polygon.IslandAreaThreshold := 10;

    Polygon.PointCount := 4;

    // Add vertices using Segments array
    Segment := Polygon.Segments[0];
    Segment.Kind := ePolySegmentLine;
    Segment.vx := MilsToCoord(4000);
    Segment.vy := MilsToCoord(1000);
    Polygon.Segments[0] := Segment;

    Segment := Polygon.Segments[1];
    Segment.Kind := ePolySegmentLine;
    Segment.vx := MilsToCoord(4500);
    Segment.vy := MilsToCoord(1000);
    Polygon.Segments[1] := Segment;

    Segment := Polygon.Segments[2];
    Segment.Kind := ePolySegmentLine;
    Segment.vx := MilsToCoord(4500);
    Segment.vy := MilsToCoord(1500);
    Polygon.Segments[2] := Segment;

    Segment := Polygon.Segments[3];
    Segment.Kind := ePolySegmentLine;
    Segment.vx := MilsToCoord(4000);
    Segment.vy := MilsToCoord(1500);
    Polygon.Segments[3] := Segment;

    PCBBoard.AddPCBObject(Polygon);
    PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
        PCBM_BoardRegisteration, Polygon.I_ObjectAddress);

    // === Add test dimension ===
    // Commented out - dimension creation via script not working (References API undocumented)
    // Dimension := PCBServer.PCBObjectFactory(eDimensionObject, eLinearDimension, eCreate_Default);
    // Dimension.Layer := eTopOverlay;
    // Dimension.TextHeight := MilsToCoord(60);
    // Dimension.LineWidth := MilsToCoord(8);
    // Dimension.ArrowSize := MilsToCoord(50);
    // Dimension.TextGap := MilsToCoord(10);
    // Dimension.Bold := False;
    // Dimension.Italic := False;
    // Dimension.References_Add(Point(MilsToCoord(1000), MilsToCoord(2500)));
    // Dimension.References_Add(Point(MilsToCoord(2000), MilsToCoord(2500)));
    // PCBBoard.AddPCBObject(Dimension);
    // PCBServer.SendMessageToRobots(PCBBoard.I_ObjectAddress, c_Broadcast,
    //     PCBM_BoardRegisteration, Dimension.I_ObjectAddress);

    PCBServer.PostProcess;
    PCBBoard.ViewManager_FullUpdate;

    // Save the document
    Doc.DoFileSave('PCB');

    // Export to JSON
    ExportPcbDocToJson(PCBBoard, JsonPath);

    CloseDocument(FilePath);
end;

procedure GenerateAllTestData;
begin
    GeneratePcbLibrary;
    GenerateSchLibrary;
    GeneratePcbDocTestFile;
    GenerateSchDocTestFile;
    //GenerateHarnessDocTestFile;
end;

{==============================================================================
  ENTRY POINTS (Callable from Run Script dialog)
==============================================================================}

// Generate all test files
procedure RunGenerateAll;
begin
    GenerateAllTestData;
end;

// Generate only PCB library
procedure RunGeneratePcbLib;
begin
    GeneratePcbLibrary;
end;

// Generate only Schematic library
procedure RunGenerateSchLib;
begin
    GenerateSchLibrary;
end;

// Generate only PCB document
procedure RunGeneratePcbDoc;
begin
    GeneratePcbDocTestFile;
end;

// Generate only Schematic document
procedure RunGenerateSchDoc;
begin
    GenerateSchDocTestFile;
end;

{==============================================================================
  HARNESS DOCUMENT (HarDoc) GENERATOR
==============================================================================}

procedure ExportHarnessConnectorToJson(Connector: ISch_HarnessConnector; AddComma: Boolean);
var
    Iterator: ISch_Iterator;
    Entry, NextEntry: ISch_HarnessEntry;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessConnector', True);
    JsonWriteString('uniqueId', Connector.UniqueId, True);
    JsonWriteString('handle', Connector.Handle, True);
    JsonWriteString('harnessType', Connector.HarnessType, True);
    JsonWriteCoord('xSize', Connector.XSize, True);
    JsonWriteCoord('ySize', Connector.YSize, True);
    JsonWriteInteger('lineWidth', Ord(Connector.LineWidth), True);
    JsonWriteCoord('primaryConnectionPosition', Connector.PrimaryConnectionPosition, True);
    JsonWriteBoolean('hideHarnessConnectorType', Connector.HideHarnessConnectorType, True);
    JsonWriteBoolean('selection', Connector.Selection, True);
    JsonWriteBoolean('graphicallyLocked', Connector.GraphicallyLocked, True);

    // Export entries
    JsonOpenArray('entries');
    Iterator := Connector.SchIterator_Create;
    if Iterator <> nil then begin
        Iterator.AddFilter_ObjectSet(MkSet(eHarnessEntry));
        Entry := Iterator.FirstSchObject;
        while Entry <> nil do begin
            JsonOpenObject('');
            JsonWriteString('name', Entry.Name, True);
            JsonWriteString('uniqueId', Entry.UniqueId, True);
            JsonWriteString('harnessType', Entry.HarnessType, True);
            JsonWriteInteger('side', Ord(Entry.Side), True);
            JsonWriteCoord('distanceFromTop', Entry.DistanceFromTop, True);
            JsonWriteInteger('textFontId', Entry.TextFontID, True);
            JsonWriteInteger('textColor', Entry.TextColor, True);
            JsonWriteInteger('textStyle', Ord(Entry.TextStyle), False);
            NextEntry := Iterator.NextSchObject;
            JsonCloseObject(NextEntry <> nil);
            Entry := NextEntry;
        end;
        Connector.SchIterator_Destroy(Iterator);
    end;
    JsonCloseArray(False);
    JsonCloseObject(AddComma);
end;

procedure ExportHarnessWireToJson(Wire: ISch_HarnessWire; AddComma: Boolean);
var
    I: Integer;
begin
    JsonOpenObject('');
    JsonWriteString('objectType', 'HarnessWire', True);
    JsonWriteString('uniqueId', Wire.UniqueId, True);
    JsonWriteString('handle', Wire.Handle, True);
    JsonWriteInteger('lineWidth', Ord(Wire.LineWidth), True);
    JsonWriteInteger('lineStyle', Ord(Wire.LineStyle), True);
    JsonWriteInteger('color', Wire.Color, True);
    JsonWriteBoolean('isSolid', Wire.IsSolid, True);
    JsonWriteBoolean('autoWire', Wire.AutoWire, True);
    JsonWriteBoolean('transparent', Wire.Transparent, True);
    JsonWriteBoolean('designatorLocked', Wire.DesignatorLocked, True);
    JsonWriteInteger('verticesCount', Wire.VerticesCount, True);

    // Export vertices
    JsonOpenArray('vertices');
    for I := 1 to Wire.VerticesCount do begin
        JsonOpenObject('');
        JsonWriteCoord('x', Wire.Vertex[I].X, True);
        JsonWriteCoord('y', Wire.Vertex[I].Y, False);
        JsonCloseObject(I < Wire.VerticesCount);
    end;
    JsonCloseArray(False);
    JsonCloseObject(AddComma);
end;

procedure ExportHarnessDocToJson(HarDoc: ISch_HarnessDocument; JsonPath: String);
var
    Iterator: ISch_Iterator;
    Obj: ISch_BasicContainer;
    I: Integer;
begin
    JsonBegin;
    JsonOpenObject('');
    JsonWriteString('documentType', 'HarnessDocument', True);
    JsonWriteString('documentName', HarDoc.DocumentName, True);
    JsonWriteString('uniqueId', HarDoc.UniqueId, True);
    JsonWriteInteger('lengthUnit', Ord(HarDoc.LengthUnit), True);
    JsonWriteInteger('displayUnit', Ord(HarDoc.DisplayUnit), True);
    JsonWriteInteger('unitSystem', Ord(HarDoc.UnitSystem), True);
    JsonWriteCoord('sheetSizeX', HarDoc.SheetSizeX, True);
    JsonWriteCoord('sheetSizeY', HarDoc.SheetSizeY, True);
    JsonWriteInteger('sheetStyle', Ord(HarDoc.SheetStyle), True);
    JsonWriteBoolean('borderOn', HarDoc.BorderOn, True);
    JsonWriteBoolean('titleBlockOn', HarDoc.TitleBlockOn, True);
    JsonWriteBoolean('referenceZonesOn', HarDoc.ReferenceZonesOn, True);
    JsonWriteBoolean('snapGridOn', HarDoc.SnapGridOn, True);
    JsonWriteCoord('snapGridSize', HarDoc.SnapGridSize, True);
    JsonWriteBoolean('visibleGridOn', HarDoc.VisibleGridOn, True);
    JsonWriteCoord('visibleGridSize', HarDoc.VisibleGridSize, True);
    JsonWriteInteger('systemFont', HarDoc.SystemFont, True);

    // Export all objects
    JsonOpenArray('objects');
    I := 0;

    Iterator := HarDoc.SchIterator_Create;
    if Iterator <> nil then begin
        Obj := Iterator.FirstSchObject;
        while Obj <> nil do begin
            if I > 0 then
                JsonOutput.Strings[JsonOutput.Count - 1] := JsonOutput.Strings[JsonOutput.Count - 1] + ',';
            case Obj.ObjectId of
                eHarnessConnector: begin
                    ExportHarnessConnectorToJson(Obj, False);
                    Inc(I);
                end;
                eHarnessWire: begin
                    ExportHarnessWireToJson(Obj, False);
                    Inc(I);
                end;
            end;
            Obj := Iterator.NextSchObject;
        end;
        HarDoc.SchIterator_Destroy(Iterator);
    end;
    JsonCloseArray(False);
    JsonCloseObject(False);  // root

    JsonEnd(JsonPath);
end;

procedure GenerateHarnessDocTestFile;
var
    HarDoc: ISch_HarnessDocument;
    Doc: IServerDocument;
    FilePath, JsonPath: String;
    Connector: ISch_HarnessConnector;
    Entry: ISch_HarnessEntry;
    Wire: ISch_HarnessWire;
begin
    FilePath := OUTPUT_DIR + 'TestHarness.HarDoc';
    JsonPath := OUTPUT_DIR + 'TestHarness.json';

    // Delete existing files
    if FileExists(FilePath) then DeleteFile(FilePath);
    if FileExists(JsonPath) then DeleteFile(JsonPath);

    // Create new Harness document
    Doc := Client.OpenNewDocument('Harness', FilePath, '', False);
    if Doc = nil then Exit;
    Client.ShowDocument(Doc);

    HarDoc := SchServer.GetCurrentSchDocument;
    if HarDoc = nil then Exit;

    SchServer.ProcessControl.PreProcess(HarDoc, '');

    // Create a harness connector (source)
    Connector := SchServer.SchObjectFactory(eHarnessConnector, eCreate_Default);
    if Connector <> nil then begin
        Connector.MoveToXY(MilsToCoord(1000), MilsToCoord(2000));
        Connector.XSize := MilsToCoord(400);
        Connector.YSize := MilsToCoord(600);
        Connector.HarnessType := 'Signal';
        Connector.LineWidth := eSmall;

        // Add entries to connector
        Entry := Connector.CreateEntry;
        if Entry <> nil then begin
            Entry.Name := 'PWR';
            Entry.HarnessType := 'Power';
            Entry.Side := eLeftSide;
            Entry.DistanceFromTop := MilsToCoord(100);
        end;

        Entry := Connector.CreateEntry;
        if Entry <> nil then begin
            Entry.Name := 'GND';
            Entry.HarnessType := 'Power';
            Entry.Side := eLeftSide;
            Entry.DistanceFromTop := MilsToCoord(200);
        end;

        Entry := Connector.CreateEntry;
        if Entry <> nil then begin
            Entry.Name := 'DATA';
            Entry.HarnessType := 'Signal';
            Entry.Side := eLeftSide;
            Entry.DistanceFromTop := MilsToCoord(300);
        end;

        HarDoc.AddSchObject(Connector);
        SchServer.RobotManager.SendMessage(HarDoc.I_ObjectAddress,
            c_BroadCast, SCHM_PrimitiveRegistration, Connector.I_ObjectAddress);
    end;

    // Create a second harness connector (destination)
    Connector := SchServer.SchObjectFactory(eHarnessConnector, eCreate_Default);
    if Connector <> nil then begin
        Connector.MoveToXY(MilsToCoord(3000), MilsToCoord(2000));
        Connector.XSize := MilsToCoord(400);
        Connector.YSize := MilsToCoord(600);
        Connector.HarnessType := 'Signal';
        Connector.LineWidth := eSmall;

        Entry := Connector.CreateEntry;
        if Entry <> nil then begin
            Entry.Name := 'PWR';
            Entry.HarnessType := 'Power';
            Entry.Side := eRightSide;
            Entry.DistanceFromTop := MilsToCoord(100);
        end;

        Entry := Connector.CreateEntry;
        if Entry <> nil then begin
            Entry.Name := 'GND';
            Entry.HarnessType := 'Power';
            Entry.Side := eRightSide;
            Entry.DistanceFromTop := MilsToCoord(200);
        end;

        Entry := Connector.CreateEntry;
        if Entry <> nil then begin
            Entry.Name := 'DATA';
            Entry.HarnessType := 'Signal';
            Entry.Side := eRightSide;
            Entry.DistanceFromTop := MilsToCoord(300);
        end;

        HarDoc.AddSchObject(Connector);
        SchServer.RobotManager.SendMessage(HarDoc.I_ObjectAddress,
            c_BroadCast, SCHM_PrimitiveRegistration, Connector.I_ObjectAddress);
    end;

    // Create harness wires connecting the entries
    Wire := SchServer.SchObjectFactory(eHarnessWire, eCreate_Default);
    if Wire <> nil then begin
        Wire.VerticesCount := 2;
        Wire.Vertex[1] := Point(MilsToCoord(1400), MilsToCoord(2100));
        Wire.Vertex[2] := Point(MilsToCoord(2600), MilsToCoord(2100));
        Wire.LineWidth := eMedium;
        Wire.Color := $000000;

        HarDoc.AddSchObject(Wire);
        SchServer.RobotManager.SendMessage(HarDoc.I_ObjectAddress,
            c_BroadCast, SCHM_PrimitiveRegistration, Wire.I_ObjectAddress);
    end;

    Wire := SchServer.SchObjectFactory(eHarnessWire, eCreate_Default);
    if Wire <> nil then begin
        Wire.VerticesCount := 2;
        Wire.Vertex[1] := Point(MilsToCoord(1400), MilsToCoord(2000));
        Wire.Vertex[2] := Point(MilsToCoord(2600), MilsToCoord(2000));
        Wire.LineWidth := eMedium;
        Wire.Color := $0000FF; // Red for GND

        HarDoc.AddSchObject(Wire);
        SchServer.RobotManager.SendMessage(HarDoc.I_ObjectAddress,
            c_BroadCast, SCHM_PrimitiveRegistration, Wire.I_ObjectAddress);
    end;

    Wire := SchServer.SchObjectFactory(eHarnessWire, eCreate_Default);
    if Wire <> nil then begin
        Wire.VerticesCount := 4;
        Wire.Vertex[1] := Point(MilsToCoord(1400), MilsToCoord(1900));
        Wire.Vertex[2] := Point(MilsToCoord(1800), MilsToCoord(1900));
        Wire.Vertex[3] := Point(MilsToCoord(2200), MilsToCoord(1900));
        Wire.Vertex[4] := Point(MilsToCoord(2600), MilsToCoord(1900));
        Wire.LineWidth := eSmall;
        Wire.Color := $00FF00; // Green for data

        HarDoc.AddSchObject(Wire);
        SchServer.RobotManager.SendMessage(HarDoc.I_ObjectAddress,
            c_BroadCast, SCHM_PrimitiveRegistration, Wire.I_ObjectAddress);
    end;

    SchServer.ProcessControl.PostProcess(HarDoc, '');

    // Save and export
    Doc.DoFileSave('Harness');
    ExportHarnessDocToJson(HarDoc, JsonPath);
    CloseDocument(FilePath);
end;

// Generate Harness document
procedure RunGenerateHarDoc;
begin
    GenerateHarnessDocTestFile;
end;

// Export current PCB library to JSON
procedure RunExportCurrentPcbLib;
var
    PCBLib: IPCB_Library;
    JsonPath: String;
begin
    PCBLib := PCBServer.GetCurrentPCBLibrary;
    if PCBLib = nil then
        Exit;

    JsonPath := ChangeFileExt(PCBLib.Board.FileName, '.json');
    ExportPcbLibToJson(PCBLib, JsonPath);
end;

// Export current Schematic library to JSON
procedure RunExportCurrentSchLib;
var
    SchLib: ISch_Lib;
    JsonPath: String;
begin
    SchLib := SchServer.GetCurrentSchDocument;
    if SchLib = nil then
        Exit;

    JsonPath := ChangeFileExt(SchLib.DocumentName, '.json');
    ExportSchLibToJson(SchLib, JsonPath);
end;

end.
