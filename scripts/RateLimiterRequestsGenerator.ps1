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
$batchSize = 30
$delayMs = 2000
$sent = 0

# GLOBAL JOB LIST
$runningJobs = @()

Write-Host "`nStarting Rate Limit Test (Expect heavy 429)..." -ForegroundColor Cyan
Write-Host "Endpoint: $endpoint"
Write-Host "Total Requests: $totalRequests"
Write-Host "Batch Size: $batchSize`n"

# ===============================
# MAIN LOOP
# ===============================
while ($sent -lt $totalRequests) {

    Write-Host "----------------------------------------" -ForegroundColor DarkGray
    Write-Host "Dispatching burst starting at $sent" -ForegroundColor Yellow

    1..$batchSize | ForEach-Object {

        if ($sent -ge $totalRequests) { return }

        $headers = @{}
        if (-not [string]::IsNullOrWhiteSpace($token)) {
            $headers["Authorization"] = "Bearer $token"
        }

        $currentIndex = $sent

        $job = Start-Job -ScriptBlock {
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
                $retryAfter = $null

                if ($null -ne $_.Exception.Response) {
                    $statusCode = $_.Exception.Response.StatusCode.value__
                    if ($null -ne $_.Exception.Response.Headers) {
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
    }

    Write-Host "Batch dispatched | Total sent so far: $sent" -ForegroundColor Cyan

    # ===============================
    # SOFT CLEANUP (ONLY COMPLETED JOBS)
    # ===============================
    $remainingJobs = @()
    $beforeCount = $runningJobs.Count
    $removedCount = 0

    foreach ($job in $runningJobs) {
        if ($job.State -eq "Completed") {
            try {
                $output = Receive-Job $job
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
            }
            catch {}

            Remove-Job $job | Out-Null
            $removedCount++
        }
        else {
            $remainingJobs += $job
        }
    }

    $runningJobs = $remainingJobs
    $afterCount = $runningJobs.Count

    Write-Host "Batch cleanup: Removed $removedCount | Remaining running jobs: $afterCount (was $beforeCount)" -ForegroundColor DarkGray

    Start-Sleep -Milliseconds $delayMs
}

# ===============================
# FINAL DRAIN (STREAMING)
# ===============================
Write-Host "`n========================================" -ForegroundColor DarkGray
Write-Host "Final drain started. Remaining jobs: $($runningJobs.Count)" -ForegroundColor Cyan

$totalJobs = $runningJobs.Count
$removedTotal = 0

while ($runningJobs.Count -gt 0) {

    $remainingJobs = @()
    $removedThisRound = 0

    foreach ($job in $runningJobs) {

        if ($job.State -eq "Completed") {
            try {
                $output = Receive-Job $job
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
            }
            catch {}

            Remove-Job $job | Out-Null
            $removedThisRound++
            $removedTotal++
        }
        else {
            $remainingJobs += $job
        }
    }

    $runningJobs = $remainingJobs

    Write-Host "Drain progress: Removed $removedTotal / $totalJobs | Remaining: $($runningJobs.Count)" -ForegroundColor DarkGray

    if ($runningJobs.Count -gt 0) {
        Start-Sleep -Seconds 5
    }
}

Write-Host "Final drain completed. All jobs processed." -ForegroundColor Green

# ===============================
# REPORT
# ===============================
$endDate = Get-Date
$duration = $endDate - $startTime

Write-Host "`nTest Completed in: $($duration.ToString()), at $($endDate.ToString())" -ForegroundColor Cyan