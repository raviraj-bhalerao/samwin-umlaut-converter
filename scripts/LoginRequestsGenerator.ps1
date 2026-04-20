# JWT Login Load Generator (NO 429 - corrected)

$startTime = Get-Date

$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName, started at $($startTime.ToString())"

$loginUrl = "https://samwin-umlaut-converter-api.onrender.com/auth/LoginWithPassword"

$users = @(
    @{ userName = "bhaleraor"; password = "bhaleraor@1234" },
    @{ userName = "behrs";     password = "behrs@1234" }
)

$batchSize = 10
$rateLimiterDelaySec = 10
$totalRequests = 100

$sent = 0

# GLOBAL JOB LIST
$runningJobs = @()

Write-Host "Starting JWT login simulation (no throttling expected)..." -ForegroundColor Green

while ($sent -lt $totalRequests) {

    Write-Host "Dispatching batch starting at $sent" -ForegroundColor Cyan

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
            catch {}
            finally {
                $error.Clear()
                Start-Sleep -Milliseconds 100
            }
        } -ArgumentList $loginUrl, $body

        $runningJobs += $job
        $sent++
    }

    Write-Host "Batch dispatched" -ForegroundColor Yellow

    # --- SOFT CLEANUP (ONLY COMPLETED JOBS) ---
    $runningJobs = $runningJobs | Where-Object {
        if ($_.State -eq "Completed") {
            try { Receive-Job $_ | Out-Null } catch {}
            Remove-Job $_ | Out-Null
            return $false
        }
        return $true
    }

    Write-Host "Batch cleanup (completed jobs only). Waiting $rateLimiterDelaySec sec..."

    Start-Sleep -Seconds $rateLimiterDelaySec
}

# =========================
# FINAL DRAIN
# =========================
Write-Host "`nFinal drain started..."

if ($runningJobs.Count -gt 0) {
    $runningJobs | Wait-Job | Out-Null
    $runningJobs | Receive-Job | Out-Null
    $runningJobs | Remove-Job | Out-Null
}

$runningJobs = @()

$endDate = Get-Date
$duration = $endDate - $startTime

Write-Host "Login load simulation completed (no 429 expected) in: $($duration.ToString()), at $($endDate.ToString())" -ForegroundColor Green