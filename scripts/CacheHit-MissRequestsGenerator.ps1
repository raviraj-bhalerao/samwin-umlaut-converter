# Cache lifecycle simulation (MISS → HIT → EVICTION → MISS)
$startTime = Get-Date

$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName"

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
                    Invoke-WebRequest -Uri $u -Method Get -UseBasicParsing | Out-Null
                }
                catch {}
            } -ArgumentList $fullUrl | Out-Null

            if ($sent -ge $totalRequests) { break }
        }

        Write-Host "Batch dispatched. Waiting $rateLimiterDelaySec sec..." -ForegroundColor Yellow
        Start-Sleep -Seconds $rateLimiterDelaySec
    }

    Write-Host "Cycle $cycle - requests dispatched"

    if ($cycle -lt 5) {
        Write-Host "Waiting $cacheEvictionDurationInSeconds seconds for cache eviction..."
        Start-Sleep -Seconds $cacheEvictionDurationInSeconds
    }
}
$duration = (Get-Date) - $startTime
Write-Host "Simulation completed in : $($duration.ToString())"