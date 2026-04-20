# =========================
# JWT Login Load Generator (stable, no job killing)
# =========================

$startTime = Get-Date

$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName, started at $($startTime.ToString())"

$loginUrl = "https://samwin-umlaut-converter-api.onrender.com/auth/LoginWithPassword"

$users = @(
    @{ userName = "bhaleraor"; password = "bhaleraor@1234" },
    @{ userName = "behrs";     password = "behrs@1234" }
)

$batchSize = 10              # safe under rate limiter
$rateLimiterDelaySec = 10
$totalRequests = 100

$sent = 0
$runningJobs = @()

Write-Host "Starting JWT login simulation (stable load pattern)..." -ForegroundColor Green

# =========================
# MAIN LOOP
# =========================
while ($sent -lt $totalRequests) {

    Write-Host "`nDispatching batch starting at $sent" -ForegroundColor Cyan

    # -------------------------
    # BURST BATCH
    # -------------------------
    1..$batchSize | ForEach-Object {

        if ($sent -ge $totalRequests) { return }

        $user = $users[$sent % 2]

        $body = @{
            userName = $user.userName
            password = $user.password
        } | ConvertTo-Json

        $job = Start-Job -ScriptBlock {
            param($url, $payload)

            try {
                Invoke-RestMethod `
                    -Uri $url `
                    -Method Post `
                    -ContentType "application/json" `
                    -Body $payload `
                    -TimeoutSec 60 | Out-Null
            }
            catch {
                # ignore for load testing
            }
        } -ArgumentList $loginUrl, $body

        $runningJobs += $job
        $sent++
    }

    Write-Host "Batch dispatched. Active jobs: $($runningJobs.Count)" -ForegroundColor Yellow

    # -------------------------
    # SOFT CLEANUP (NO Stop-Job)
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

    # -------------------------
    # RATE LIMIT WINDOW
    # -------------------------
    Write-Host "Waiting $rateLimiterDelaySec sec for rate limiter window..."
    Start-Sleep -Seconds $rateLimiterDelaySec
}

# =========================
# FINAL DRAIN (IMPORTANT)
# =========================
Write-Host "`nFinal drain started..."

if ($runningJobs.Count -gt 0) {
    $runningJobs | Wait-Job | Out-Null
    $runningJobs | Receive-Job | Out-Null
    $runningJobs | Remove-Job | Out-Null
}

$runningJobs = @()

# =========================
# END REPORT
# =========================
$endDate = Get-Date
$duration = $endDate - $startTime

Write-Host "`nJWT login simulation completed in: $($duration.ToString()) at $($endDate.ToString())" -ForegroundColor Green