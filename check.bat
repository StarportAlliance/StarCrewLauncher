@echo off
cd /d %~dp0
echo === StarCrew Launcher: local quality gates (same as CI) ===
echo.
echo [1/4] Restoring local tools...
dotnet tool restore || exit /b 1
echo.
echo [2/4] Checking format (CSharpier) and style (dotnet format)...
dotnet csharpier check . || exit /b 1
dotnet format style StarCrew.Launcher.slnx --verify-no-changes --no-restore || exit /b 1
echo.
echo [3/4] Building (warnings as errors)...
dotnet build StarCrew.Launcher.slnx -c Release --nologo --no-restore -p:TreatWarningsAsErrors=true || exit /b 1
echo.
echo [4/4] Running all tests...
call test.bat
