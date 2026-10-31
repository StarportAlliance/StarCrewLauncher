param([int]$Threshold = 80)

$report = Get-ChildItem (Join-Path $PSScriptRoot 'TestResults') -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $report) {
    Write-Host "No coverage report found under TestResults."
    exit 1
}

[xml]$coverage = Get-Content $report.FullName
$lineRate = [double]$coverage.coverage.'line-rate' * 100

Write-Host ("Line coverage: {0:N2}% (threshold {1}%)" -f $lineRate, $Threshold)
if ($lineRate -lt $Threshold) {
    Write-Host "Coverage gate failed."
    exit 1
}
