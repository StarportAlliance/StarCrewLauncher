@echo off
cd /d %~dp0
echo === StarCrew Launcher: build app and run UI smoke tests ===
echo.
echo [1/2] Building launcher (Release)...
dotnet build StarCrew.Launcher/StarCrew.Launcher.csproj -c Release
if errorlevel 1 (
  echo.
  echo [FAIL] Launcher build failed.
  if not defined CI pause
  exit /b 1
)
echo.
echo [2/2] Running UI smoke tests (Release)...
dotnet test StarCrew.Launcher.UITests/ -c Release --nologo
if errorlevel 1 (
  echo.
  echo [FAIL] UI smoke tests failed.
  if not defined CI pause
  exit /b 1
)
echo.
echo [OK] UI smoke tests passed.
if not defined CI pause
