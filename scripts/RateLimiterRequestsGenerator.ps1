# ===============================
# CONFIG
# ===============================

$startTime = Get-Date

$defaultBaseUrl = "https://samwin-umlaut-converter-api.onrender.com"

$baseUrlInput = Read-Host "Enter base URL (press Enter for default: $defaultBaseUrl)"

if ([string]::IsNullOrWhiteSpace($baseUrlInput)) {
    $baseUrl = $defaultBaseUrl
}
else {
    $baseUrl = $baseUrlInput
}

$token = Read-Host "Enter JWT token"

$endpoint = "$baseUrl/WeatherForecast"

# ===============================
# TEST SETTINGS
# ===============================

$totalRequests = 100
$batchSize = 30     # >15 → guarantees 429
$delayMs = 2000     # small delay between bursts

$sent = 0

Write-Host "`nStarting Rate Limit Test (Expect heavy 429)..." -ForegroundColor Cyan
Write-Host "Endpoint: $endpoint"
Write-Host "Total Requests: $totalRequests"
Write-Host "Batch Size: $batchSize`n"

# ===============================
# TEST LOOP
# ===============================

while ($sent -lt $totalRequests) {

    Write-Host "Dispatching burst starting at $sent" -ForegroundColor Yellow

    1..$batchSize | ForEach-Object {

        if ($sent -ge $totalRequests) { break }

        try {
            $headers = @{}

            if (-not [string]::IsNullOrWhiteSpace($token)) {
                $headers["Authorization"] = "Bearer $token"
            }

            $response = Invoke-WebRequest `
                -Uri $endpoint `
                -Method GET `
                -Headers $headers `
                -UseBasicParsing `
                -ErrorAction Stop

            Write-Host "Request $sent => 200" -ForegroundColor Green
        }
        catch {
            $statusCode = $null
            $retryAfter = $null

            if ($_.Exception.Response -ne $null) {
                $statusCode = $_.Exception.Response.StatusCode.value__

                if ($_.Exception.Response.Headers -ne $null) {
                    $retryAfter = $_.Exception.Response.Headers["Retry-After"]
                }
            }

            if ($statusCode -eq 429) {
                Write-Host "Request $sent => 429 | Retry-After: $retryAfter sec" -ForegroundColor Red
            }
            else {
                Write-Host "Request $sent => ERROR ($statusCode)" -ForegroundColor Magenta
            }
        }

        $sent++
    }

    Write-Host "Burst complete. Short pause..." -ForegroundColor Cyan
    Start-Sleep -Milliseconds $delayMs
}
$duration = (Get-Date) - $startTime
Write-Host "`nTest Completed in  : $($duration.ToString())`n" -ForegroundColor Cyan