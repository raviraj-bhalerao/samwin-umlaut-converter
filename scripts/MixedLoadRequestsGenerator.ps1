# =========================
# Mixed traffic simulation (stable, no job killing)
# =========================

$startTime = Get-Date

$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName, started at $($startTime.ToString())"

$normalUrl = "https://samwin-umlaut-converter-api.onrender.com/WeatherForecast"
$errorUrl  = "https://samwin-umlaut-converter-api.onrender.com/WeatherForecast?simulateError"

$batchSize = 10
$rateLimiterDelaySec = 10
$totalRequests = 500

$sent = 0
$runningJobs = @()

Write-Host "Starting mixed traffic simulation (stable load pattern)..." -ForegroundColor Green

# =========================
# MAIN LOOP
# =========================
while ($sent -lt $totalRequests) {

    Write-Host "`nDispatching batch starting at $sent" -ForegroundColor Cyan

    # -------------------------
    # BURST BATCH
    # -------------------------
    1..$batchSize | ForEach-Object {

        if ($sent -ge $totalRequests) { return }

        $rand = Get-Random -Minimum 1 -Maximum 10

        $job = Start-Job -ScriptBlock {
            param($nUrl, $eUrl, $r)

            try {
                if ($r -le 7) {
                    Invoke-RestMethod -Uri $nUrl -Method Get -TimeoutSec 60 | Out-Null
                }
                else {
                    Invoke-RestMethod -Uri $eUrl -Method Get -ErrorAction Stop -TimeoutSec 60 | Out-Null
                }
            }
            catch {
                # expected errors for simulation
            }
        } -ArgumentList $normalUrl, $errorUrl, $rand

        $runningJobs += $job
        $sent++
    }

    Write-Host "Batch dispatched | Active jobs: $($runningJobs.Count)" -ForegroundColor Yellow

    # -------------------------
    # SOFT CLEANUP ONLY
    # -------------------------
    $runningJobs = $runningJobs | Where-Object {

        if ($_.State -eq "Completed") {
            try {
                Receive-Job $_ | Out-Null
            } catch {}

            Remove-Job $_ | Out-Null
            return $false
        }

        return $true
    }

    # -------------------------
    # RATE LIMIT WINDOW
    # -------------------------
    Write-Host "Waiting $rateLimiterDelaySec sec for rate limiter window..."
    Start-Sleep -Seconds $rateLimiterDelaySec
}

# =========================
# FINAL DRAIN (IMPORTANT)
# =========================
Write-Host "`nFinal drain started..."

if ($runningJobs.Count -gt 0) {
    $runningJobs | Wait-Job | Out-Null
    $runningJobs | Receive-Job | Out-Null
    $runningJobs | Remove-Job | Out-Null
}

$runningJobs = @()

# =========================
# END REPORT
# =========================
$endDate = Get-Date
$duration = $endDate - $startTime

Write-Host "`nMixed traffic simulation completed in: $($duration.ToString()) at $($endDate.ToString())" -ForegroundColor Green