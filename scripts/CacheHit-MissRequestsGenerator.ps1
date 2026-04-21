. "$PSScriptRoot\JobUtils.ps1"

$startTime = Get-Date
Write-Host "Starting cache + rate-limit aware load test..."

# =========================
# CONFIG
# =========================
$windowSeconds = 10
$permitLimit = 15

$targetPerWindow = 13          # stay under limiter
$durationSeconds = 385         # ~500 requests total

$maxConcurrency = 30

# Burst config (controlled realism)
$burstChance = 0.12            # 12% chance
$burstMin = 2
$burstMax = 4

$url = "https://samwin-umlaut-converter-api.onrender.com/QueryGenerator/GetQuery?useCache"

$inputs = @(
    "schwaerzwaelder","huettenbaecker","straessle","fuessener",
    "knoepflemacher","gruenwaelder","roehrlbaecker","schoenbaer",
    "suessmilch","uebelhoer","faehrbaeker","loewenbraeu",
    "knoedlbuegel","spaetbloeher","muehlstueck"
)

$query = ($inputs | ForEach-Object { "input=$_" }) -join "&"
$fullUrl = "$url&$query"

# =========================
# TIMING CONTROL
# =========================
$intervalMs = [math]::Floor(($windowSeconds * 1000) / $targetPerWindow)

$nextTick = Get-Date
$endTime = $startTime.AddSeconds($durationSeconds)

$runningJobs = @()

# =========================
# MAIN LOOP
# =========================
while ((Get-Date) -lt $endTime) {

    # -------------------------
    # CONCURRENCY CONTROL
    # -------------------------
    while ($runningJobs.Count -ge $maxConcurrency) {
        $runningJobs = Cleanup-CompletedJobs -Jobs $runningJobs -Label "Throttle cleanup"
        Start-Sleep -Milliseconds 50
    }

    # -------------------------
    # BASE REQUEST (steady flow)
    # -------------------------
    $job = Start-Job -ScriptBlock {
        param($u)

        Start-Sleep -Milliseconds (Get-Random -Minimum 30 -Maximum 120)

        try {
            Invoke-WebRequest -Uri $u -Method Get -UseBasicParsing -TimeoutSec 300 | Out-Null
        }
        catch {}
        finally {
            $error.Clear()
        }

    } -ArgumentList $fullUrl

    $runningJobs += $job

    # -------------------------
    # OPTIONAL BURST (controlled)
    # -------------------------
    if ((Get-Random -Minimum 1 -Maximum 101) -le ($burstChance * 100)) {

        $burstSize = Get-Random -Minimum $burstMin -Maximum ($burstMax + 1)

        Write-Host "Burst triggered: $burstSize requests" -ForegroundColor Magenta

        1..$burstSize | ForEach-Object {

            Start-Sleep -Milliseconds (Get-Random -Minimum 30 -Maximum 100)

            $burstJob = Start-Job -ScriptBlock {
                param($u)

                try {
                    Invoke-WebRequest -Uri $u -Method Get -UseBasicParsing -TimeoutSec 300 | Out-Null
                }
                catch {}
                finally {
                    $error.Clear()
                }

            } -ArgumentList $fullUrl

            $runningJobs += $burstJob
        }
    }

    # -------------------------
    # RATE-CONTROLLED SCHEDULING (no drift + mild jitter)
    # -------------------------
    $nextTick = $nextTick.AddMilliseconds($intervalMs)

    $sleepMs = ($nextTick - (Get-Date)).TotalMilliseconds

    # jitter for realism (bounded)
    $jitter = Get-Random -Minimum -120 -Maximum 180
    $sleepMs = [math]::Max(0, $sleepMs + $jitter)

    Start-Sleep -Milliseconds $sleepMs

    # -------------------------
    # CLEANUP
    # -------------------------
    $runningJobs = Cleanup-CompletedJobs -Jobs $runningJobs -Label "Loop cleanup"
}

# =========================
# FINAL DRAIN
# =========================
$runningJobs = Drain-AllJobs -Jobs $runningJobs

$endTime = Get-Date
Write-Host "Completed at: $endTime"
Write-Host "Total duration: $($endTime - $startTime)"