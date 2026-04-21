. "$PSScriptRoot\JobUtils.ps1"

$startTime = Get-Date
$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName, started at $startTime"

$normalUrl = "https://samwin-umlaut-converter-api.onrender.com/WeatherForecast"
$errorUrl  = "https://samwin-umlaut-converter-api.onrender.com/WeatherForecast?simulateError"

# =========================
# CONFIG (suite-aligned model)
# =========================
$totalRequests = 500

$windowSeconds = 10
$targetPerWindow = 13               # consistent with API scripts
$intervalMs = [math]::Floor(($windowSeconds * 1000) / $targetPerWindow)

$maxConcurrency = 30

# traffic mix (business logic layer)
$successRate = 70                   # 70% normal, 30% error

# burst model (same across suite)
$burstChance = 0.10
$burstMin = 2
$burstMax = 4

$runningJobs = @()

Write-Host "Starting mixed traffic simulation (steady + burst + error mix)..." -ForegroundColor Green

# =========================
# MAIN LOOP
# =========================
for ($sent = 0; $sent -lt $totalRequests; ) {

    # -------------------------
    # concurrency control
    # -------------------------
    while ($runningJobs.Count -ge $maxConcurrency) {
        $runningJobs = Cleanup-CompletedJobs -Jobs $runningJobs -Label "Throttle cleanup"
        Start-Sleep -Milliseconds 50
    }

    # -------------------------
    # decide request type
    # -------------------------
    $rand = Get-Random -Minimum 1 -Maximum 101

    if ($rand -le $successRate) {
        $urlToCall = $normalUrl
    }
    else {
        $urlToCall = $errorUrl
    }

    # -------------------------
    # main request
    # -------------------------
    $job = Start-Job -ScriptBlock {
        param($u)

        Start-Sleep -Milliseconds (Get-Random -Minimum 30 -Maximum 120)

        try {
            Invoke-RestMethod -Uri $u -Method Get -TimeoutSec 60 | Out-Null
        }
        catch {}
        finally {
            $error.Clear()
        }

    } -ArgumentList $urlToCall

    $runningJobs += $job
    $sent++

    # -------------------------
    # optional burst (same pattern as other scripts)
    # -------------------------
    if ((Get-Random -Minimum 1 -Maximum 101) -le ($burstChance * 100)) {

        $burstSize = Get-Random -Minimum $burstMin -Maximum ($burstMax + 1)

        Write-Host "Mixed traffic burst: $burstSize requests" -ForegroundColor Magenta

        1..$burstSize | ForEach-Object {

            if ($sent -ge $totalRequests) { return }

            Start-Sleep -Milliseconds (Get-Random -Minimum 30 -Maximum 100)

            $randBurst = Get-Random -Minimum 1 -Maximum 101

            if ($randBurst -le $successRate) {
                $burstUrl = $normalUrl
            }
            else {
                $burstUrl = $errorUrl
            }

            $burstJob = Start-Job -ScriptBlock {
                param($u)

                try {
                    Invoke-RestMethod -Uri $u -Method Get -TimeoutSec 60 | Out-Null
                }
                catch {}
                finally {
                    $error.Clear()
                }

            } -ArgumentList $burstUrl

            $runningJobs += $burstJob
            $sent++
        }
    }

    # -------------------------
    # steady pacing + jitter (core model)
    # -------------------------
    $sleepMs = $intervalMs + (Get-Random -Minimum -120 -Maximum 180)
    if ($sleepMs -lt 0) { $sleepMs = 0 }

    Start-Sleep -Milliseconds $sleepMs

    # cleanup
    $runningJobs = Cleanup-CompletedJobs -Jobs $runningJobs -Label "Loop cleanup"
}

# =========================
# FINAL DRAIN
# =========================
$runningJobs = Drain-AllJobs -Jobs $runningJobs

$endDate = Get-Date
Write-Host "Mixed traffic simulation completed in: $($endDate - $startTime)" -ForegroundColor Green