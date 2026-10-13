@echo off
REM Builds a single-file, self-contained Windows x64 executable (no .NET install needed on the target PC).
dotnet publish SmartTools\SmartTools.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
echo.
echo Done. Your program is in the "publish" folder: SmartToolsSuite.exe
pause
