@echo off
cd /d %~dp0
echo === StarCrew Launcher: run all tests ===
echo.
dotnet test StarCrew.Launcher.slnx -c Release --nologo --settings coverlet.runsettings --collect "XPlat Code Coverage" --results-directory ./TestResults --logger "trx;LogFileName=test.trx"
if errorlevel 1 (
  echo.
  echo [FAIL] Tests failed. See TestResults\test.trx for details.
  if not defined CI pause
  exit /b 1
)
echo.
echo [OK] All tests passed. Coverage: TestResults\*\coverage.cobertura.xml.
if not defined CI pause
