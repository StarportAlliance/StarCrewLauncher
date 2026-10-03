@echo off
cd /d %~dp0
echo === StarCrew Launcher: pack with Velopack ===
echo.
if "%~1"=="" (
  echo Usage: pack.bat ^<version^> [rid]
  echo Example: pack.bat 0.3.0 win-x64
  echo Note: version must match the Version in StarCrew.Launcher.csproj.
  exit /b 1
)
set VERSION=%~1
set RID=%~2
if "%RID%"=="" set RID=win-x64
echo [1/3] Restoring NuGet packages...
dotnet restore StarCrew.Launcher.slnx || exit /b 1
echo.
echo [2/3] Publishing %RID%...
dotnet publish StarCrew.Launcher\StarCrew.Launcher.csproj -c Release --nologo --no-restore -r %RID% --self-contained -o publish\%RID% || exit /b 1
echo.
echo [3/3] Packing Velopack release %VERSION%...
where vpk >nul 2>nul
if errorlevel 1 (
  echo [ERROR] vpk not found. Install it first: dotnet tool install -g vpk
  exit /b 1
)-
vpk pack --packId StarCrew.Launcher --packVersion %VERSION% --packDir publish\%RID% --mainExe StarCrew.Launcher.exe --runtime %RID% || exit /b 1
echo.
echo [OK] Release ready under Releases\. Copy it to the update feed.
