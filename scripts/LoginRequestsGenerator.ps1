# JWT Login Load Generator (NO 429)
$startTime = Get-Date

$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
Write-Host "Running Script: $scriptName"

$loginUrl = "https://samwin-umlaut-converter-api.onrender.com/auth/LoginWithPassword"

$users = @(
    @{ userName = "bhaleraor"; password = "bhaleraor@1234" },
    @{ userName = "behrs";     password = "behrs@1234" }
)

$batchSize = 10       # safely under 15
$delaySec = 10        # matches rate limiter window
$totalRequests = 100

$sent = 0

Write-Host "Starting JWT login simulation (no throttling expected)..." -ForegroundColor Green

while ($sent -lt $totalRequests) {

    Write-Host "Dispatching batch starting at $sent" -ForegroundColor Cyan

    1..$batchSize | ForEach-Object {

        $user = $users[$sent % 2]

        $body = @{
            userName = $user.userName
            password = $user.password
        } | ConvertTo-Json

        Start-Job -ScriptBlock {
            param($url, $payload)
            try {
                Invoke-RestMethod `
                    -Uri $url `
                    -Method Post `
                    -ContentType "application/json" `
                    -Body $payload | Out-Null
            }
            catch {}
        } -ArgumentList $loginUrl, $body | Out-Null

        $sent++
        if ($sent -ge $totalRequests) { break }
    }

    Write-Host "Batch completed - waiting for rate window reset..." -ForegroundColor Yellow
    Start-Sleep -Seconds $delaySec
}
$duration = (Get-Date) - $startTime
Write-Host "Login load simulation completed (no 429 expected) in : $($duration.ToString())" -ForegroundColor Green