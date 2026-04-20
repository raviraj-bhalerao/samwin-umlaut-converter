. "$PSScriptRoot\JobUtils.ps1"
# Cache MISS generator (rate-limit aware - corrected)

$startTime = Get-Date

$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName, started at $($startTime.ToString())"

$url = "https://samwin-umlaut-converter-api.onrender.com/QueryGenerator/GetQuery?useCache"

$batchSize = 15
$rateLimiterDelaySec = 10

$totalRequests = 300
$sent = 0

# GLOBAL JOB LIST
$runningJobs = @()

Write-Host "Generating cache misses (controlled batches)..." -ForegroundColor Green

while ($sent -lt $totalRequests) {

    Write-Host "Dispatching batch starting at $sent" -ForegroundColor Cyan

    1..$batchSize | ForEach-Object {

        if ($sent -ge $totalRequests) { return }

        $randomInput = "word$sent"
        $fullUrl = "$url&input=$randomInput"

        $job = Start-Job -ScriptBlock {
            param($u)
            try {
                Invoke-WebRequest -Uri $u -Method Get -UseBasicParsing -TimeoutSec 60 | Out-Null
            }
            catch {}
            finally {
                $error.Clear()
                Start-Sleep -Milliseconds 100
            }
        } -ArgumentList $fullUrl

        $runningJobs += $job
        $sent++
    }

    Write-Host "Batch dispatched (expect cache MISS + some 429)" -ForegroundColor Yellow

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

Write-Host "Cache miss metrics generation completed in: $($duration.ToString()), at $($endDate.ToString())"