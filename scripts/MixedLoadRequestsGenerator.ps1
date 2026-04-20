. "$PSScriptRoot\JobUtils.ps1"
# Mixed traffic (NO 429, controlled batches - corrected)

$startTime = Get-Date

$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName, started at $($startTime.ToString())"

$normalUrl = "https://samwin-umlaut-converter-api.onrender.com/WeatherForecast"
$errorUrl = "https://samwin-umlaut-converter-api.onrender.com/WeatherForecast?simulateError"

$batchSize = 10
$rateLimiterDelaySec = 10
$totalRequests = 500

$sent = 0

# GLOBAL JOB LIST
$runningJobs = @()

Write-Host "Starting mixed traffic simulation (no 429 expected)..." -ForegroundColor Green

while ($sent -lt $totalRequests) {

    Write-Host "Dispatching batch starting at $sent" -ForegroundColor Cyan

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
            catch {}
            finally {
                $error.Clear()
                Start-Sleep -Milliseconds 100
            }
        } -ArgumentList $normalUrl, $errorUrl, $rand

        $runningJobs += $job
        $sent++
    }

    Write-Host "Batch dispatched (70% success / 30% error)" -ForegroundColor Yellow

    # --- SOFT CLEANUP (ONLY COMPLETED JOBS) ---
    $runningJobs = Cleanup-CompletedJobs -Jobs $runningJobs -Label "Batch cleanup"
    Write-Host "Batch cleanup (completed jobs only). Waiting $rateLimiterDelaySec sec..."

    Start-Sleep -Seconds $rateLimiterDelaySec
}

# =========================
# FINAL DRAIN
# =========================
$runningJobs = Drain-AllJobs -Jobs $runningJobs

$runningJobs = @()

$endDate = Get-Date
$duration = $endDate - $startTime

Write-Host "Mixed traffic simulation completed (no 429) in: $($duration.ToString()), at $($endDate.ToString())" -ForegroundColor Green