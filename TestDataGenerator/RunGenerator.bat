@echo off
echo Running AltiumSharp Test Data Generator...
echo.

set ALTIUM_EXE=C:\Program Files\Altium\AD25\X2.EXE
set PROJECT_PATH=D:\src\AltiumSharp\TestDataGenerator\TestDataGenerator.PrjPcb
set PROC_NAME=RunGenerateAll

echo Altium Path: %ALTIUM_EXE%
echo Project: %PROJECT_PATH%
echo Procedure: %PROC_NAME%
echo.

start "" "%ALTIUM_EXE%" -RScriptingSystem:RunScript(ProjectName="%PROJECT_PATH%"^|ProcName="%PROC_NAME%")

echo Altium Designer launching...
