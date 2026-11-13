param(
    [string]$LogDir = (Join-Path $PSScriptRoot '..\MyriadCreation\Logs')
)

$ErrorActionPreference = 'Stop'
$failures = New-Object System.Collections.Generic.List[string]

$baselinePath = Join-Path $LogDir 'm05-baseline-test.json'
if (-not (Test-Path $baselinePath)) {
    $failures.Add("missing baseline: $baselinePath")
} else {
    $b = Get-Content $baselinePath -Raw | ConvertFrom-Json
    if ($b.avgMs -le 0) { $failures.Add("avgMs must be > 0 (was $($b.avgMs))") }
    if ($b.maxMs -gt 5000) { $failures.Add("maxMs must be <= 5000 (was $($b.maxMs))") }
    if ($b.memoryMb -gt 2048) { $failures.Add("memoryMb must be <= 2048 (was $($b.memoryMb))") }
    if ($b.realms -le 0) { $failures.Add("realms must be > 0 (was $($b.realms))") }
}

$hotspotPath = Join-Path $LogDir 'm05-hotspots-test.csv'
if (-not (Test-Path $hotspotPath)) {
    $failures.Add("missing hotspots: $hotspotPath")
} elseif ((Get-Content $hotspotPath | Measure-Object -Line).Lines -lt 2) {
    $failures.Add("hotspots csv has no rows")
}

$yearlyPath = Join-Path $LogDir 'm05-yearly-test.csv'
if (-not (Test-Path $yearlyPath)) {
    $failures.Add("missing one-year profile: $yearlyPath")
} else {
    $rows = (Get-Content $yearlyPath | Measure-Object -Line).Lines
    if ($rows -lt 366) { $failures.Add("one-year csv expected >= 366 lines (was $rows)") }
}

if ($failures.Count -gt 0) {
    Write-Host "M0.5 acceptance FAILED:" -ForegroundColor Red
    foreach ($f in $failures) { Write-Host " - $f" -ForegroundColor Red }
    exit 1
}

Write-Host "M0.5 acceptance OK (baseline + hotspots + one-year profile)" -ForegroundColor Green
exit 0
