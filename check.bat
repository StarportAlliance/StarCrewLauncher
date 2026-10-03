@echo off
cd /d %~dp0
echo === StarCrew Launcher: local quality gates (same as CI) ===
echo.
echo [1/5] Restoring local tools...
dotnet tool restore || exit /b 1
echo.
echo [2/5] Restoring NuGet packages...
dotnet restore StarCrew.Launcher.slnx || exit /b 1
echo.
echo [3/5] Checking format (CSharpier) and style (dotnet format)...
dotnet csharpier check . || exit /b 1
dotnet format style StarCrew.Launcher.slnx --verify-no-changes --no-restore || exit /b 1
echo.
echo [4/5] Building (warnings as errors)...
dotnet build StarCrew.Launcher.slnx -c Release --nologo --no-restore -p:TreatWarningsAsErrors=true || exit /b 1
echo.
echo [5/5] Running all tests...
call test.bat
