# ===============================
# PARAMS & CONFIG
# ===============================
Param(
    [switch]$i  # Use -i to trigger interactive mode
)

$startTime = Get-Date
$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName, started at $startTime.ToString()"

$defaultBaseUrl = "https://samwin-umlaut-converter-api.onrender.com"
$baseUrl = $defaultBaseUrl
$token = ""

# If the -i switch is provided, ask for input
if ($i) {
    $baseUrlInput = Read-Host "Enter base URL (press Enter for default: $defaultBaseUrl)"
    if (-not [string]::IsNullOrWhiteSpace($baseUrlInput)) {
        $baseUrl = $baseUrlInput
    }
    $token = Read-Host "Enter JWT token"
}
else {
    Write-Host "Running in non-interactive mode using defaults." -ForegroundColor Gray
}

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
        if ($sent -ge $totalRequests) { return } # 'return' inside ForEach-Object acts like 'continue'

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

            if ($null -ne $_.Exception.Response) {
                $statusCode = $_.Exception.Response.StatusCode.value__
                if ($null -ne $_.Exception.Response.Headers) {
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
        finally {
            $sent++
            $error.Clear()

            # --- HIGHLIGHTED CHANGE 4: Micro-Throttle ---
            # A tiny pause (10ms) helps the OS manage the network buffer 
            # without significantly slowing down your burst test.
            Start-Sleep -Milliseconds 10            
        }
    }

    Write-Host "Batch dispatched" -ForegroundColor Cyan
    # --- CRITICAL ADDITION FOR CELERON ---
    # This stops the background processes and closes the powershell.exe instances
    Get-Job | Stop-Job
    Get-Job | Remove-Job
    # -------------------------------------
    Write-Host "Batch cleanup completed. Short pause..."

    Start-Sleep -Milliseconds $delayMs
}

$endDate = Get-Date;
$duration = $endDate - $startTime
Write-Host "Test Completed in : $($duration.ToString()), at $endDate.ToString(), at $endDate.ToString()" -ForegroundColor Cyan