# =========================
# Cache lifecycle simulation (MISS → HIT → EVICTION)
# =========================

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

$query = ($inputs | ForEach-Object { "input=$_" }) -join "&"
$fullUrl = "$url&$query"

Write-Host "Starting cache lifecycle simulation..."

# =========================
# CONFIG
# =========================
$cycleCount = 5

# Based on observed behavior:
# 15 SSE streams × ~12 sec each ≈ ~180 sec total
$cycleCutoffSeconds = 220   # safe buffer (3.5 min)
$evictionSeconds = 60

$runningJobs = @()

# =========================
# MAIN LOOP
# =========================
for ($cycle = 1; $cycle -le $cycleCount; $cycle++) {

    Write-Host "`nCycle $cycle - dispatching request"

    # -------------------------
    # Start SINGLE SSE request
    # -------------------------
    $job = Start-Job -ScriptBlock {
        param($u)

        try {
            Invoke-WebRequest -Uri $u -Method Get -UseBasicParsing -TimeoutSec 300 | Out-Null
        }
        catch {
            # ignore errors for load test
        }
    } -ArgumentList $fullUrl

    $runningJobs += $job

    Write-Host "Request started. Waiting for SSE completion (~$cycleCutoffSeconds sec)..."

    # -------------------------
    # CYCLE WAIT + SOFT CLEANUP
    # -------------------------
    $elapsed = 0
    $cleanupInterval = 10

    while ($elapsed -lt $cycleCutoffSeconds) {

        Start-Sleep -Seconds $cleanupInterval
        $elapsed += $cleanupInterval

        # remove only completed jobs (NON-BLOCKING)
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

        Write-Host "Cycle $cycle | ${elapsed}s elapsed | Active jobs: $($runningJobs.Count)"
    }

    Write-Host "Cycle $cycle completed (no forced drain here)"

    # -------------------------
    # CACHE EVICTION WAIT
    # -------------------------
    if ($cycle -lt $cycleCount) {
        Write-Host "Waiting $evictionSeconds seconds for cache eviction..."
        Start-Sleep -Seconds $evictionSeconds
    }
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

Write-Host "`nSimulation completed in: $($duration.ToString()) at $($endDate.ToString())"