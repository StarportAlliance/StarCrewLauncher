@echo off
cd /d %~dp0
echo === StarCrew Launcher: local quality gates (same as CI) ===
echo [1/4] Restoring local tools...
dotnet tool restore || exit /b 1
echo [2/4] Restoring NuGet packages...
dotnet restore StarportAlliance.StarCrewLauncher.slnx || exit /b 1
echo [3/4] Checking format (CSharpier) and style (dotnet format)...
dotnet csharpier check . || exit /b 1
dotnet format style StarportAlliance.StarCrewLauncher.slnx --verify-no-changes --no-restore || exit /b 1
echo [4/4] Building (warnings as errors)...
rem ReleaseGate keeps gate outputs in bin\ReleaseGate so a running Release app never locks the build.
dotnet build StarportAlliance.StarCrewLauncher\StarportAlliance.StarCrewLauncher.csproj -c ReleaseGate --nologo --no-restore -p:TreatWarningsAsErrors=true || exit /b 1
echo.
echo [OK] All gates passed.
