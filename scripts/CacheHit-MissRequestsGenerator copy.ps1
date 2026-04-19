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


Write-Host "Cycle $cycle - dispatching requests"

# Fire-and-forget parallel calls
    Start-Job -ScriptBlock {
        param($u)
        try {
            Invoke-WebRequest -Uri $u -Method Get -UseBasicParsing | Out-Null
        } catch {}
    } -ArgumentList $fullUrl | Out-Null


Write-Host "Simulation complete"