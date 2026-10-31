@echo off
cd /d %~dp0
echo === StarCrew Launcher: local quality gates (same as CI) ===
echo [1/6] Restoring local tools...
dotnet tool restore || exit /b 1
echo [2/6] Restoring NuGet packages...
dotnet restore StarCrew.Launcher.slnx || exit /b 1
echo [3/6] Checking format (CSharpier) and style (dotnet format)...
dotnet csharpier check . || exit /b 1
dotnet format style StarCrew.Launcher.slnx --verify-no-changes --no-restore || exit /b 1
echo [4/6] Building (warnings as errors)...
rem ReleaseGate keeps gate outputs in bin\ReleaseGate so a running Release app never locks the build.
rem Build csproj directly: the slnx only declares Debug/Release solution configurations.
rem UI smoke tests launch this same ReleaseGate exe (SmokeTests.ResolveLauncherExe), so no extra Release build is needed.
dotnet build StarCrew.Launcher\StarCrew.Launcher.csproj -c ReleaseGate --nologo --no-restore -p:TreatWarningsAsErrors=true || exit /b 1
dotnet build StarCrew.Launcher.Tests\StarCrew.Launcher.Tests.csproj -c ReleaseGate --nologo --no-restore -p:TreatWarningsAsErrors=true || exit /b 1
echo [5/6] Unit tests + coverage (threshold enforced)...
dotnet test StarCrew.Launcher.Tests\StarCrew.Launcher.Tests.csproj -c ReleaseGate --nologo --settings coverlet.runsettings --collect "XPlat Code Coverage" --results-directory ./TestResults --logger "trx;LogFileName=test.trx" || goto fail
powershell -NoProfile -ExecutionPolicy Bypass -File check-coverage.ps1 || goto fail
echo [6/6] UI smoke tests (launches the real window, no clicks)...
dotnet test StarCrew.Launcher.UITests\StarCrew.Launcher.UITests.csproj -c ReleaseGate --nologo --results-directory ./TestResults --logger "trx;LogFileName=uitest.trx" || goto fail
goto ok
:fail
echo.
echo [FAIL] Gates failed. See output above and TestResults\ for details.
exit /b 1
:ok
echo.
echo [OK] All gates passed. Coverage: TestResults\*\coverage.cobertura.xml.
