# Mixed traffic (NO 429, controlled batches)
$startTime = Get-Date

$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName, started at $startTime.ToString()"

$normalUrl = "https://samwin-umlaut-converter-api.onrender.com/WeatherForecast"
$errorUrl = "https://samwin-umlaut-converter-api.onrender.com/WeatherForecast?simulateError"

$batchSize = 10      # safely under 15
$rateLimiterDelaySec = 10      # rate limiter window
$totalRequests = 500

$sent = 0

Write-Host "Starting mixed traffic simulation (no 429 expected)..." -ForegroundColor Green

while ($sent -lt $totalRequests) {

    Write-Host "Dispatching batch starting at $sent" -ForegroundColor Cyan

    1..$batchSize | ForEach-Object {

        $rand = Get-Random -Minimum 1 -Maximum 10

        Start-Job -ScriptBlock {
            param($nUrl, $eUrl, $r)

            try {
                if ($r -le 7) {
                    Invoke-RestMethod -Uri $nUrl -Method Get | Out-Null
                }
                else {
                    Invoke-RestMethod -Uri $eUrl -Method Get -ErrorAction Stop | Out-Null
                }
            }
            catch {}
            finally {
                $error.Clear()
                # --- HIGHLIGHTED CHANGE 4: Micro-Throttle ---
                # A tiny pause (10ms) helps the OS manage the network buffer 
                # without significantly slowing down your burst test.
                Start-Sleep -Milliseconds 10                    
            }
        } -ArgumentList $normalUrl, $errorUrl, $rand | Out-Null

        $sent++
        if ($sent -ge $totalRequests) { break }
    }

    Write-Host "Batch dispatched (70% success / 30% error)" -ForegroundColor Yellow
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
Write-Host "Mixed traffic simulation completed (no 429) in : $($duration.ToString()), at $endDate.ToString()" -ForegroundColor Green