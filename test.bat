@echo off
cd /d %~dp0
echo === StarCrew Launcher: run all tests ===
echo.
rem ReleaseGate keeps gate outputs in bin\ReleaseGate so a running Release app never locks the build.
rem Run csproj directly: the slnx only declares Debug/Release solution configurations.
dotnet test StarCrew.Launcher.Tests\StarCrew.Launcher.Tests.csproj -c ReleaseGate --nologo --settings coverlet.runsettings --collect "XPlat Code Coverage" --results-directory ./TestResults --logger "trx;LogFileName=test.trx"
if errorlevel 1 goto fail
dotnet test StarCrew.Launcher.UITests\StarCrew.Launcher.UITests.csproj -c ReleaseGate --nologo --results-directory ./TestResults --logger "trx;LogFileName=uitest.trx"
if errorlevel 1 goto fail
goto ok
:fail
echo.
echo [FAIL] Tests failed. See TestResults\ for details.
exit /b 1
:ok
echo.
echo [OK] All tests passed. Coverage: TestResults\*\coverage.cobertura.xml.
