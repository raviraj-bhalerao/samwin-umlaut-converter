$startTime = Get-Date
$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName, started at $startTime.ToString()"

$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName"

Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\LoginRequestsGenerator.ps1
.\MixedLoadRequestsGenerator.ps1
.\RateLimiterRequestsGenerator.ps1
.\CacheHit-MissRequestsGenerator.ps1
.\CacheMissRequestsGenerator.ps1

$endDate = Get-Date;
$duration = $endDate - $startTime
Write-Host "Total time to completion in : $($duration.ToString()), at $endDate.ToString()" -ForegroundColor Green