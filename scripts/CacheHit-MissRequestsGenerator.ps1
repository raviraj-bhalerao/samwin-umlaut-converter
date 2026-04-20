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

for ($cycle = 1; $cycle -le 5; $cycle++) {

    Write-Host "Cycle $cycle - dispatching requests"

    # Fire-and-forget parallel calls
    $batchSize = 15   # Y
    $rateLimiterDelaySec = 10    # X

    $totalRequests = 100
    $sent = 0

    while ($sent -lt $totalRequests) {

        Write-Host "Dispatching batch starting at $sent" -ForegroundColor Cyan

        1..$batchSize | ForEach-Object {

            $sent++

            Start-Job -ScriptBlock {
                param($u)
                try {
                    # Added -TimeoutSec to ensure jobs don't hang forever
                    Invoke-WebRequest -Uri $u -Method Get -UseBasicParsing -TimeoutSec 10 | Out-Null
                }
                catch {}
                finally {
                    $error.Clear()
                    Start-Sleep -Milliseconds 100
                }
            } -ArgumentList $fullUrl | Out-Null

            if ($sent -ge $totalRequests) { break }
        }
        Write-Host "Batch dispatched." -ForegroundColor Yellow
        # --- CRITICAL ADDITION FOR CELERON ---
        # This stops the background processes and closes the powershell.exe instances
        Get-Job | Stop-Job
        Get-Job | Remove-Job
        # -------------------------------------
        Write-Host "Batch cleanup completed. Waiting $rateLimiterDelaySec sec..."

        # During this sleep, the jobs are running as separate powershell.exe processes
        Start-Sleep -Seconds $rateLimiterDelaySec
    }

    Write-Host "Cycle $cycle - requests dispatched"
    
    if ($cycle -lt 5) {
        Write-Host "Waiting $cacheEvictionDurationInSeconds seconds for cache eviction..."
        Start-Sleep -Seconds $cacheEvictionDurationInSeconds
    }
}

$endDate = Get-Date;
$duration = $endDate - $startTime
Write-Host "Simulation completed in : $($duration.ToString()), at $($endDate.ToString())"