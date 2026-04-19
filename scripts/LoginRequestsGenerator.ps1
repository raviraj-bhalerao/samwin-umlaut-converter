# JWT Login Load Generator

$loginUrl = "https://samwin-umlaut-converter-api.onrender.com/auth/LoginWithPassword"

$users = @(
    @{ userName = "bhaleraor"; password = "bhaleraor@1234" },
    @{ userName = "behrs";     password = "behrs@1234" }
)

Write-Host "Starting JWT login load simulation..." -ForegroundColor Green

for ($cycle = 1; $cycle -le 5; $cycle++) {

    Write-Host "Cycle $cycle - sending login burst" -ForegroundColor Cyan

    1..50 | ForEach-Object {

        $user = $users[$_ % 2]

        $body = @{
            userName = $user.userName
            password = $user.password
        } | ConvertTo-Json

        Start-Job -ScriptBlock {
            param($url, $payload)

            try {
                $response = Invoke-RestMethod `
                    -Uri $url `
                    -Method Post `
                    -ContentType "application/json" `
                    -Body $payload

                # Optional: print only successful JWT response once in a while
                if ($response -and (Get-Random -Minimum 1 -Maximum 20 -eq 1)) {
                    Write-Host "JWT received"
                }
            }
            catch {
                # silently ignore 429 or auth errors (expected under rate limit)
            }

        } -ArgumentList $loginUrl, $body | Out-Null
    }

    Write-Host "Cycle $cycle dispatched (rate limiter will shape traffic)" -ForegroundColor Yellow

    # IMPORTANT: aligns with your rate limit window (10s)
    Start-Sleep -Seconds 10
}

Write-Host "JWT load simulation complete" -ForegroundColor Green