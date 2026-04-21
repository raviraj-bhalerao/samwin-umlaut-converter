. "$PSScriptRoot\JobUtils.ps1"

$startTime = Get-Date
$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName, started at $startTime"

$url = "https://samwin-umlaut-converter-api.onrender.com/QueryGenerator/GetQuery?useCache"

# =========================
# CONFIG (METRICS CONSISTENT MODEL)
# =========================
$totalRequests = 300

$windowSeconds = 10
$targetPerWindow = 13                 # safe under 15 req / 10 sec limiter
$intervalMs = [math]::Floor(($windowSeconds * 1000) / $targetPerWindow)

$maxConcurrency = 30

# burst model (same as other scripts)
$burstChance = 0.10
$burstMin = 2
$burstMax = 4

$runningJobs = @()

Write-Host "Generating cache MISS (steady + burst + jitter)..." -ForegroundColor Green

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
    # main request
    # -------------------------
    $randomInput = "word$sent"
    $fullUrl = "$url&input=$randomInput"

    $job = Start-Job -ScriptBlock {
        param($u)

        Start-Sleep -Milliseconds (Get-Random -Minimum 30 -Maximum 120)

        try {
            Invoke-WebRequest -Uri $u -Method Get -UseBasicParsing -TimeoutSec 60 | Out-Null
        }
        catch {}
        finally {
            $error.Clear()
        }

    } -ArgumentList $fullUrl

    $runningJobs += $job
    $sent++

    # -------------------------
    # optional burst (small + rare)
    # -------------------------
    if ((Get-Random -Minimum 1 -Maximum 101) -le ($burstChance * 100)) {

        $burstSize = Get-Random -Minimum $burstMin -Maximum ($burstMax + 1)

        Write-Host "Burst: $burstSize MISS requests" -ForegroundColor Magenta

        1..$burstSize | ForEach-Object {

            if ($sent -ge $totalRequests) { return }

            Start-Sleep -Milliseconds (Get-Random -Minimum 30 -Maximum 100)

            $burstInput = "word$sent-burst"
            $burstUrl = "$url&input=$burstInput"

            $burstJob = Start-Job -ScriptBlock {
                param($u)

                try {
                    Invoke-WebRequest -Uri $u -Method Get -UseBasicParsing -TimeoutSec 60 | Out-Null
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
    # steady pacing (same model as other scripts)
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
$duration = $endDate - $startTime

Write-Host "Cache MISS metrics generation completed in: $($duration.ToString()), at $endDate"