# Cache MISS generator (rate-limit aware)
$startTime = Get-Date

$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName"

$url = "https://samwin-umlaut-converter-api.onrender.com/QueryGenerator/GetQuery?useCache"

$batchSize = 15     # >15 to trigger 429
$delaySec = 10      # matches rate limiter window

$totalRequests = 300
$sent = 0

Write-Host "Generating cache misses (controlled batches)..." -ForegroundColor Green

while ($sent -lt $totalRequests) {

    Write-Host "Dispatching batch starting at $sent" -ForegroundColor Cyan

    1..$batchSize | ForEach-Object {

        $randomInput = "word$sent"
        $fullUrl = "$url&input=$randomInput"

        Start-Job -ScriptBlock {
            param($u)
            try {
                Invoke-WebRequest -Uri $u -Method Get -UseBasicParsing | Out-Null
            }
            catch {}
        } -ArgumentList $fullUrl | Out-Null

        $sent++
        if ($sent -ge $totalRequests) { break }
    }

    Write-Host "Batch dispatched (expect cache MISS + some 429)" -ForegroundColor Yellow

    Start-Sleep -Seconds $delaySec
}
$duration = (Get-Date) - $startTime
Write-Host "Cache miss metrics generation completed in : $($duration.ToString())"