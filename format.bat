@echo off
cd /d %~dp0
echo === StarCrew Launcher: apply formatting (CSharpier + dotnet format) ===
echo.
echo [1/4] Restoring local tools...
dotnet tool restore || exit /b 1
echo.
echo [2/4] Restoring NuGet packages...
dotnet restore StarCrew.Launcher.slnx || exit /b 1
echo.
echo [3/4] Formatting (CSharpier)...
dotnet csharpier format . || exit /b 1
echo.
echo [4/4] Fixing style (dotnet format)...
dotnet format style StarCrew.Launcher.slnx --no-restore || exit /b 1
echo.
echo [OK] Formatting done. Run check.bat to verify.
if not defined CI pause
