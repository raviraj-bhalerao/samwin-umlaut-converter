# ===============================
# PARAMS & CONFIG
# ===============================
Param(
    [switch]$i
)

$startTime = Get-Date
$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path

Write-Host "Running Script: $scriptName, started at $($startTime.ToString())"

$defaultBaseUrl = "https://samwin-umlaut-converter-api.onrender.com"
$baseUrl = $defaultBaseUrl
$token = ""

# ===============================
# INTERACTIVE MODE
# ===============================
if ($i) {
    $baseUrlInput = Read-Host "Enter base URL (Enter = default)"
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
$batchSize = 30       # intentionally > 15 to trigger 429
$delayMs = 2000
$sent = 0

$runningJobs = @()

Write-Host "`nStarting Rate Limit Test (Expect 429 bursts)..." -ForegroundColor Cyan
Write-Host "Endpoint: $endpoint"
Write-Host "Total Requests: $totalRequests"
Write-Host "Batch Size: $batchSize`n"

# ===============================
# MAIN LOOP
# ===============================
while ($sent -lt $totalRequests) {

    Write-Host "Dispatching burst starting at $sent" -ForegroundColor Yellow

    # -------------------------
    # BURST REQUESTS
    # -------------------------
    1..$batchSize | ForEach-Object {

        if ($sent -ge $totalRequests) { return }

        $headers = @{}
        if (-not [string]::IsNullOrWhiteSpace($token)) {
            $headers["Authorization"] = "Bearer $token"
        }

        $job = Start-Job -ScriptBlock {
            param($url, $hdrs)

            try {
                Invoke-WebRequest `
                    -Uri $url `
                    -Method GET `
                    -Headers $hdrs `
                    -UseBasicParsing `
                    -TimeoutSec 60 | Out-Null
            }
            catch {
                # expected: 429, network throttling, etc.
            }
        } -ArgumentList $endpoint, $headers

        $runningJobs += $job
        $sent++
    }

    Write-Host "Batch dispatched | Active jobs: $($runningJobs.Count)" -ForegroundColor Cyan

    # -------------------------
    # SOFT CLEANUP ONLY (NO STOP)
    # -------------------------
    $runningJobs = $runningJobs | Where-Object {

        if ($_.State -eq "Completed") {
            try {
                Receive-Job $_ | Out-Null
            } catch {}

            Remove-Job $_ | Out-Null
            return $false
        }

        return $true
    }

    Write-Host "Batch cleanup completed. Short pause..." -ForegroundColor Gray

    Start-Sleep -Milliseconds $delayMs
}

# ===============================
# FINAL DRAIN (IMPORTANT)
# ===============================
Write-Host "`nFinal drain started..."

if ($runningJobs.Count -gt 0) {
    $runningJobs | Wait-Job | Out-Null
    $runningJobs | Receive-Job | Out-Null
    $runningJobs | Remove-Job | Out-Null
}

$runningJobs = @()

# ===============================
# REPORT
# ===============================
$endDate = Get-Date
$duration = $endDate - $startTime

Write-Host "`nTest Completed in: $($duration.ToString()) at $($endDate.ToString())" -ForegroundColor Cyan