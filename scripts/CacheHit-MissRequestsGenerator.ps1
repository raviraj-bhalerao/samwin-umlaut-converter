# Cache lifecycle simulation (MISS → HIT → EVICTION → MISS)

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

for ($cycle = 1; $cycle -le 5; $cycle++) {

    Write-Host "Cycle $cycle - dispatching requests"

    # Fire-and-forget parallel calls
    1..100 | ForEach-Object {

        $percent = ($_ / 100) * 100
        Write-Host "Dispatching request $_ / 100" -ForegroundColor Cyan

        Start-Job -ScriptBlock {
            param($u)
            try {
                Invoke-WebRequest -Uri $u -Method Get -UseBasicParsing | Out-Null
            }
            catch {}
        } -ArgumentList $fullUrl | Out-Null
    }

    Write-Host "Cycle $cycle - requests dispatched"

    if ($cycle -lt 5) {
        Write-Host "Waiting 2 minutes for cache eviction..."
        Start-Sleep -Seconds 60
    }
}

Write-Host "Simulation complete"