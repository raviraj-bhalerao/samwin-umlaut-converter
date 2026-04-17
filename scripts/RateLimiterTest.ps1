# ===============================
# CONFIG - BASE URL (with fallback)
# ===============================

$defaultBaseUrl = "https://samwin-umlaut-converter-api.onrender.com"

$baseUrlInput = Read-Host "Enter base URL (press Enter for default: $defaultBaseUrl)"

if ([string]::IsNullOrWhiteSpace($baseUrlInput)) {
    $baseUrl = $defaultBaseUrl
}
else {
    $baseUrl = $baseUrlInput
}

# ===============================
# INPUT TOKEN
# ===============================

$token = Read-Host "Enter JWT token"

# ===============================
# BUILD FINAL URL
# ===============================

$endpoint = "$baseUrl/WeatherForecast"

# ===============================
# TEST SETTINGS
# ===============================

$totalRequests = 30
$sleepBetweenMs = 1000

Write-Host "`nStarting Rate Limit Test..." -ForegroundColor Cyan
Write-Host "Base URL: $baseUrl"
Write-Host "Endpoint: $endpoint"
Write-Host "Requests: $totalRequests`n"

# ===============================
# TEST LOOP
# ===============================

for ($i = 1; $i -le $totalRequests; $i++) {
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
            
        $status = $response.StatusCode
        $retryAfter = $response.Headers["Retry-After"]

        if ($retryAfter) {
            Write-Host "Request $i => $status | Retry-After: $retryAfter sec" -ForegroundColor Yellow
        }
        else {
            Write-Host "Request $i => $status" -ForegroundColor Green
        }
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
            Write-Host "Request $i => 429 TOO MANY REQUESTS | Retry-After: $retryAfter sec" -ForegroundColor Red
        }
        elseif ($statusCode) {
            Write-Host "Request $i => ERROR ($statusCode)" -ForegroundColor Magenta
        }
        else {
            Write-Host "Request $i => ERROR (No response - possible network/auth issue)" -ForegroundColor DarkRed
        }
    }

    Start-Sleep -Milliseconds $sleepBetweenMs
}

Write-Host "`nTest Completed.`n" -ForegroundColor Cyan