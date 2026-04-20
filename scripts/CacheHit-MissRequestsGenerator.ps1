. "$PSScriptRoot\JobUtils.ps1"
# Cache lifecycle simulation (MISS → HIT → EVICTION → MISS)
$startTime = Get-Date

$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName, started at $($startTime.ToString())"

$url = "https://samwin-umlaut-converter-api.onrender.com/QueryGenerator/GetQuery?useCache"

$inputs = @(
    "schwaerzwaelder", "huettenbaecker", "straessle", "fuessener",
    "knoepflemacher", "gruenwaelder", "roehrlbaecker", "schoenbaer",
    "suessmilch", "uebelhoer", "faehrbaeker", "loewenbraeu",
    "knoedlbuegel", "spaetbloeher", "muehlstueck"
)

$cacheEvictionDurationInSeconds = 60
$query = ($inputs | ForEach-Object { "input=$_" }) -join "&"
$fullUrl = "$url&$query"

Write-Host "Starting cache lifecycle simulation..."

# GLOBAL JOB LIST (across cycles)
$runningJobs = @()

for ($cycle = 1; $cycle -le 5; $cycle++) {

    Write-Host "`nCycle $cycle - dispatching requests"

    $batchSize = 15
    $rateLimiterDelaySec = 10

    $totalRequests = 100
    $sent = 0

    while ($sent -lt $totalRequests) {

        Write-Host "Dispatching batch starting at $sent" -ForegroundColor Cyan

        1..$batchSize | ForEach-Object {

            if ($sent -ge $totalRequests) { return }

            $job = Start-Job -ScriptBlock {
                param($u)
                try {
                    Invoke-WebRequest -Uri $u -Method Get -UseBasicParsing -TimeoutSec 300 | Out-Null
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

        Write-Host "Batch dispatched." -ForegroundColor Yellow

        # --- SOFT CLEANUP (ONLY COMPLETED JOBS) ---
        $runningJobs = Cleanup-CompletedJobs -Jobs $runningJobs -Label "Batch cleanup"

        Write-Host "Waiting $rateLimiterDelaySec sec..."

        Start-Sleep -Seconds $rateLimiterDelaySec
    }

    Write-Host "Cycle $cycle - requests dispatched"

    # --- END OF CYCLE CLEANUP (ONLY COMPLETED, NO WAIT) ---
    $runningJobs = Cleanup-CompletedJobs -Jobs $runningJobs -Label "Cycle cleanup"

    if ($cycle -lt 5) {
        Write-Host "Waiting $cacheEvictionDurationInSeconds seconds for cache eviction..."
        Start-Sleep -Seconds $cacheEvictionDurationInSeconds
    }
}

# =========================
# FINAL DRAIN (ONLY HERE)
# =========================
$runningJobs = Drain-AllJobs -Jobs $runningJobs

$runningJobs = @()

$endDate = Get-Date
$duration = $endDate - $startTime

Write-Host "Simulation completed in: $($duration.ToString()), at $($endDate.ToString())"