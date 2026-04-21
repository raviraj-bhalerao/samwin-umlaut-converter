. "$PSScriptRoot\MetricsTrafficGeneratingEngine.ps1"
$startTime = Get-Date

Clear-Host

$scenariosToRun = @("cache_hit", "cache_miss", "login", "mixed", "ratelimit")
#$scenariosToRun = @("cache_miss")

foreach ($scenario in $scenariosToRun) {

    Write-Host "===================================" -ForegroundColor Cyan
    Write-Host "Running Scenario: $scenario"
    Write-Host "==================================="

    Start-MetricsTrafficEngine -Scenario $scenario

    Start-Sleep -Seconds 5   # small cooldown between scenarios
}
$endTime = Get-Date
Write-Host "Runner completed in $($endTime - $startTime) at $endTime" -ForegroundColor Green
