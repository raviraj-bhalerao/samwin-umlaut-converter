# Mixed traffic (NO 429, controlled batches)
$startTime = Get-Date

$normalUrl = "https://samwin-umlaut-converter-api.onrender.com/WeatherForecast"
$errorUrl  = "https://samwin-umlaut-converter-api.onrender.com/WeatherForecast?simulateError"

$batchSize = 10      # safely under 15
$delaySec  = 10      # rate limiter window
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
        } -ArgumentList $normalUrl, $errorUrl, $rand | Out-Null

        $sent++
        if ($sent -ge $totalRequests) { break }
    }

    Write-Host "Batch dispatched (70% success / 30% error)" -ForegroundColor Yellow

    Start-Sleep -Seconds $delaySec
}
$duration = (Get-Date) - $startTime
Write-Host "Mixed traffic simulation completed (no 429) in : $($duration.ToString())" -ForegroundColor Green