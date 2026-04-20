# Cache MISS generator (rate-limit aware)
$startTime = Get-Date

$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName, started at $startTime.ToString()"

$url = "https://samwin-umlaut-converter-api.onrender.com/QueryGenerator/GetQuery?useCache"

$batchSize = 15     # >15 to trigger 429
$rateLimiterDelaySec = 10      # matches rate limiter window

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
            finally {
                $error.Clear()
                # --- HIGHLIGHTED CHANGE 4: Micro-Throttle ---
                # A tiny pause (10ms) helps the OS manage the network buffer 
                # without significantly slowing down your burst test.
                Start-Sleep -Milliseconds 10                    
            }
        } -ArgumentList $fullUrl | Out-Null

        $sent++
        if ($sent -ge $totalRequests) { break }
    }

    Write-Host "Batch dispatched (expect cache MISS + some 429)" -ForegroundColor Yellow

    # --- CRITICAL ADDITION FOR CELERON ---
    # This stops the background processes and closes the powershell.exe instances
    Get-Job | Stop-Job
    Get-Job | Remove-Job
    # -------------------------------------
    Write-Host "Batch cleanup completed. Waiting $rateLimiterDelaySec sec..."

    Start-Sleep -Seconds $rateLimiterDelaySec
}


$endDate = Get-Date;
$duration = $endDate - $startTime

Write-Host "Cache miss metrics generation completed in : $($duration.ToString()), at $endDate.ToString()"