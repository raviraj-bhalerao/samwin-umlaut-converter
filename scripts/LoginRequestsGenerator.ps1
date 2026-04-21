. "$PSScriptRoot\JobUtils.ps1"

$startTime = Get-Date
$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName, started at $startTime"

$loginUrl = "https://samwin-umlaut-converter-api.onrender.com/auth/LoginWithPassword"

# =========================
# CONFIG (aligned with suite)
# =========================
$totalRequests = 100

$windowSeconds = 10
$targetPerWindow = 8              # LOWER than API scripts (login is heavier)
$intervalMs = [math]::Floor(($windowSeconds * 1000) / $targetPerWindow)

$maxConcurrency = 20

# burst model (light for auth)
$burstChance = 0.08
$burstMin = 1
$burstMax = 2

$users = @(
    @{ userName = "bhaleraor"; password = "bhaleraor@1234" },
    @{ userName = "behrs"; password = "behrs@1234" }
)

$runningJobs = @()

Write-Host "Starting JWT login simulation (steady + burst + jitter)..." -ForegroundColor Green

# =========================
# MAIN LOOP
# =========================
for ($sent = 0; $sent -lt $totalRequests; ) {

    # -------------------------
    # concurrency control
    # -------------------------
    while ($runningJobs.Count -ge $maxConcurrency) {
        $runningJobs = Cleanup-CompletedJobs -Jobs $runningJobs -Label "Throttle cleanup"
        Start-Sleep -Milliseconds 50
    }

    # -------------------------
    # user selection
    # -------------------------
    $user = $users[$sent % 2]

    $body = @{
        userName = $user.userName
        password = $user.password
    } | ConvertTo-Json

    # -------------------------
    # main login request
    # -------------------------
    $job = Start-Job -ScriptBlock {
        param($url, $payload)

        Start-Sleep -Milliseconds (Get-Random -Minimum 40 -Maximum 150)

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
        }

    } -ArgumentList $loginUrl, $body

    $runningJobs += $job
    $sent++

    # -------------------------
    # LIGHT BURST (auth-safe)
    # -------------------------
    if ((Get-Random -Minimum 1 -Maximum 101) -le ($burstChance * 100)) {

        $burstSize = Get-Random -Minimum $burstMin -Maximum ($burstMax + 1)

        Write-Host "Login burst: $burstSize requests" -ForegroundColor Magenta

        1..$burstSize | ForEach-Object {

            if ($sent -ge $totalRequests) { return }

            Start-Sleep -Milliseconds (Get-Random -Minimum 30 -Maximum 90)

            $userBurst = $users[$sent % 2]

            $burstBody = @{
                userName = $userBurst.userName
                password = $userBurst.password
            } | ConvertTo-Json

            $burstJob = Start-Job -ScriptBlock {
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
                }

            } -ArgumentList $loginUrl, $burstBody

            $runningJobs += $burstJob
            $sent++
        }
    }

    # -------------------------
    # steady pacing + jitter
    # -------------------------
    $sleepMs = $intervalMs + (Get-Random -Minimum -100 -Maximum 150)
    if ($sleepMs -lt 0) { $sleepMs = 0 }

    Start-Sleep -Milliseconds $sleepMs

    # cleanup
    $runningJobs = Cleanup-CompletedJobs -Jobs $runningJobs -Label "Loop cleanup"
}

# =========================
# FINAL DRAIN
# =========================
$runningJobs = Drain-AllJobs -Jobs $runningJobs

$endDate = Get-Date
Write-Host "Login load simulation completed in: $($endDate - $startTime)" -ForegroundColor Green