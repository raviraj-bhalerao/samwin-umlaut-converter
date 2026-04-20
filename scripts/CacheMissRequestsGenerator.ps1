# =========================
# Cache MISS generator (rate-limit aware, SSE-safe job handling)
# =========================

$startTime = Get-Date

$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName, started at $($startTime.ToString())"

$url = "https://samwin-umlaut-converter-api.onrender.com/QueryGenerator/GetQuery?useCache"

$batchSize = 15              # intentionally near/above limit
$rateLimiterDelaySec = 10    # matches server window

$totalRequests = 300
$sent = 0

$runningJobs = @()

Write-Host "Generating cache MISS load (controlled bursts)..." -ForegroundColor Green

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

        $randomInput = "word$sent"
        $fullUrl = "$url&input=$randomInput"

        $job = Start-Job -ScriptBlock {
            param($u)

            try {
                Invoke-WebRequest -Uri $u -Method Get -UseBasicParsing -TimeoutSec 60 | Out-Null
            }
            catch {
                # ignore for load test
            }
        } -ArgumentList $fullUrl

        $runningJobs += $job
        $sent++
    }

    Write-Host "Batch dispatched. Active jobs: $($runningJobs.Count)" -ForegroundColor Yellow

    # -------------------------
    # SOFT CLEANUP (NO STOP)
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

Write-Host "`nCache miss metrics generation completed in: $($duration.ToString()) at $($endDate.ToString())"