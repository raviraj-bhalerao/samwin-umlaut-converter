# ===============================
# PARAMS & CONFIG
# ===============================
Param(
    [switch]$i
)

$startTime = Get-Date
$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path

Write-Host "Running Script: $scriptName, started at $startTime"

$defaultBaseUrl = "https://samwin-umlaut-converter-api.onrender.com"
$baseUrl = $defaultBaseUrl
$token = ""

# ===============================
# INTERACTIVE MODE
# ===============================
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
# TEST SETTINGS (metrics-focused)
# ===============================
$totalRequests = 30

$windowSeconds = 10
$targetPerWindow = 18        # intentionally ABOVE limiter (15) → generate 429s
$intervalMs = [math]::Floor(($windowSeconds * 1000) / $targetPerWindow)

$maxConcurrency = 25

# burst model (small + realistic)
$burstChance = 0.12
$burstMin = 2
$burstMax = 4

$sent = 0
$runningJobs = @()

Write-Host "Starting Rate Limit Metrics Simulation (expected 429s)..." -ForegroundColor Cyan
Write-Host "Endpoint: $endpoint"
Write-Host "Total Requests: $totalRequests"

# ===============================
# MAIN LOOP (steady + overload + bursts)
# ===============================
for ($sent = 0; $sent -lt $totalRequests; ) {

    # -------------------------
    # concurrency control
    # -------------------------
    while ($runningJobs.Count -ge $maxConcurrency) {
        $runningJobs = Cleanup-CompletedJobs -Jobs $runningJobs -Label "Throttle cleanup"
        Start-Sleep -Milliseconds 50
    }

    # -------------------------
    # headers
    # -------------------------
    $headers = @{}
    if (-not [string]::IsNullOrWhiteSpace($token)) {
        $headers["Authorization"] = "Bearer $token"
    }

    $currentIndex = $sent

    # -------------------------
    # request job
    # -------------------------
    $job = Start-Job -ScriptBlock {
        param($url, $hdrs, $idx)

        Start-Sleep -Milliseconds (Get-Random -Minimum 20 -Maximum 120)

        try {
            Invoke-WebRequest `
                -Uri $url `
                -Method GET `
                -Headers $hdrs `
                -UseBasicParsing `
                -TimeoutSec 60 | Out-Null

            Write-Output "Request $idx => 200"
        }
        catch {
            $statusCode = $null
            $retryAfter = $null

            if ($null -ne $_.Exception.Response) {
                $statusCode = $_.Exception.Response.StatusCode.value__
                if ($_.Exception.Response.Headers) {
                    $retryAfter = $_.Exception.Response.Headers["Retry-After"]
                }
            }

            if ($statusCode -eq 429) {
                Write-Output "Request $idx => 429 | Retry-After: $retryAfter sec"
            }
            else {
                Write-Output "Request $idx => ERROR ($statusCode)"
            }
        }

    } -ArgumentList $endpoint, $headers, $currentIndex

    $runningJobs += $job
    $sent++

    # -------------------------
    # controlled burst (adds realism, not spikes)
    # -------------------------
    if ((Get-Random -Minimum 1 -Maximum 101) -le ($burstChance * 100)) {

        $burstSize = Get-Random -Minimum $burstMin -Maximum ($burstMax + 1)

        Write-Host "Burst: $burstSize requests" -ForegroundColor Magenta

        1..$burstSize | ForEach-Object {

            $burstIndex = $sent

            if ($sent -ge $totalRequests) { return }

            Start-Sleep -Milliseconds (Get-Random -Minimum 20 -Maximum 90)

            $burstJob = Start-Job -ScriptBlock {
                param($url, $hdrs, $idx)

                try {
                    Invoke-WebRequest `
                        -Uri $url `
                        -Method GET `
                        -Headers $hdrs `
                        -UseBasicParsing `
                        -TimeoutSec 60 | Out-Null

                    Write-Output "Request $idx => 200"
                }
                catch {
                    $statusCode = $null
                    if ($_.Exception.Response) {
                        $statusCode = $_.Exception.Response.StatusCode.value__
                    }

                    Write-Output "Request $idx => ERROR ($statusCode)"
                }

            } -ArgumentList $endpoint, $headers, $burstIndex

            $runningJobs += $burstJob
            $sent++
        }
    }

    # -------------------------
    # steady pacing + jitter (CRITICAL)
    # -------------------------
    $sleepMs = $intervalMs + (Get-Random -Minimum -100 -Maximum 150)
    if ($sleepMs -lt 0) { $sleepMs = 0 }

    Start-Sleep -Milliseconds $sleepMs

    # cleanup completed jobs
    $remainingJobs = @()

    foreach ($jobItem in $runningJobs) {
        if ($jobItem.State -in @("Completed", "Failed", "Stopped")) {

            try {
                $output = Receive-Job $jobItem -ErrorAction SilentlyContinue

                foreach ($line in $output) {
                    if ($line -match "429") {
                        Write-Host $line -ForegroundColor Red
                    }
                    elseif ($line -match "200") {
                        Write-Host $line -ForegroundColor Green
                    }
                    else {
                        Write-Host $line -ForegroundColor Magenta
                    }
                }
            } catch {}

            Remove-Job $jobItem -Force -ErrorAction SilentlyContinue
        }
        else {
            $remainingJobs += $jobItem
        }
    }

    $runningJobs = $remainingJobs
}

# ===============================
# FINAL DRAIN
# ===============================
$runningJobs = Drain-AllJobs -Jobs $runningJobs

$endDate = Get-Date
$duration = $endDate - $startTime

Write-Host "Rate Limit Metrics Completed in: $($duration.ToString()), at $endDate" -ForegroundColor Cyan