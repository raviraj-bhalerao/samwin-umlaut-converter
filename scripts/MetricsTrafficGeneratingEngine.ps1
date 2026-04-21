. "$PSScriptRoot\JobUtils.ps1"

function Start-MetricsTrafficEngine {
    param(
        [string]$Scenario = "mixed"
    )

    $startTime = Get-Date
    Write-Host "Traffic Engine started - Scenario: $Scenario"

    $maxConcurrency = 30
    $totalRequests = 300
    $runningJobs = @()

    # =========================
    # HARD-CODED HIT STRING
    # =========================
    $hitQuery = "input=schwaerzwaelder&input=huettenbaecker&input=straessle&input=fuessener&input=knoepflemacher&input=gruenwaelder&input=roehrlbaecker&input=schoenbaer&input=suessmilch&input=uebelhoer&input=faehrbaeker&input=loewenbraeu&input=knoedlbuegel&input=spaetbloeher&input=muehlstueck"

    # =========================
    # URLS
    # =========================
    $baseHitUrl = "https://samwin-umlaut-converter-api.onrender.com/QueryGenerator/GetQuery?useCache"
    $baseMissUrl = "https://samwin-umlaut-converter-api.onrender.com/QueryGenerator/GetQuery?useCache"
    $weatherUrl = "https://samwin-umlaut-converter-api.onrender.com/WeatherForecast"
    $errorUrl = "https://samwin-umlaut-converter-api.onrender.com/WeatherForecast?simulateError"
    $loginUrl = "https://samwin-umlaut-converter-api.onrender.com/auth/LoginWithPassword"

    $intervalMs = 700

    # 🔥 FIX: dedicated MISS counter (guarantees uniqueness)
    $missCounter = 0

    for ($sent = 0; $sent -lt $totalRequests; ) {

        while ($runningJobs.Count -ge $maxConcurrency) {
            $runningJobs = Cleanup-CompletedJobs -Jobs $runningJobs
            Start-Sleep -Milliseconds 50
        }

        # =========================
        # SCENARIO SWITCH
        # =========================
        switch ($Scenario) {

            "cache_hit" {
                $url = "$baseHitUrl&$hitQuery"
                $method = "GET"
                $body = $null
                Write-Host "[HIT ] full-query" -ForegroundColor Green
            }

            "cache_miss" {
                $randomInput = "word$missCounter"
                $missCounter++
                $url = "$baseMissUrl&input=$randomInput"
                $method = "GET"
                $body = $null
                Write-Host "[MISS] $randomInput" -ForegroundColor Yellow
            }

            "mixed" {
                $rand = Get-Random -Minimum 1 -Maximum 101
                if ($rand -le 70) {
                    $url = $weatherUrl
                }
                else {
                    $url = $errorUrl
                }
                $method = "GET"
                $body = $null
                Write-Host "[MIXED]" -ForegroundColor Cyan
            }

            "login" {
                $url = $loginUrl
                $method = "POST"
                $body = @{
                    userName = "bhaleraor"
                    password = "bhaleraor@1234"
                } | ConvertTo-Json
                Write-Host "[LOGIN]" -ForegroundColor Blue
            }

            "ratelimit" {
                $url = $weatherUrl
                $method = "GET"
                $body = $null
                Write-Host "[RL TEST]" -ForegroundColor Red
            }

            default {
                throw "Unknown scenario: $Scenario"
            }
        }

        # =========================
        # REQUEST
        # =========================
        $job = Start-Job -ScriptBlock {
            param($u, $m, $b)
            Write-Host "Sending request: $u" -ForegroundColor Yellow

            try {
                if ($m -eq "POST") {
                    Invoke-RestMethod -Uri $u -Method POST -Body $b -ContentType "application/json" -TimeoutSec 60 | Out-Null
                }
                else {
                    Invoke-WebRequest -Uri $u -Method GET -UseBasicParsing -TimeoutSec 60 | Out-Null
                }
            }
            catch {}
        } -ArgumentList $url, $method, $body

        $runningJobs += $job
        $sent++

        # =========================
        # BURST
        # =========================
        if ((Get-Random -Minimum 1 -Maximum 101) -le 10) {

            $burstSize = Get-Random -Minimum 2 -Maximum 5
            Write-Host "Burst: $burstSize" -ForegroundColor Magenta

            1..$burstSize | ForEach-Object {

                if ($sent -ge $totalRequests) { return }

                switch ($Scenario) {

                    "cache_hit" {
                        $url = "$baseHitUrl&$hitQuery"
                        $method = "GET"
                        $body = $null
                    }

                    "cache_miss" {
                        $randomInput = "word$missCounter"
                        $missCounter++
                        $url = "$baseMissUrl&input=$randomInput"
                        $method = "GET"
                        $body = $null
                    }

                    "mixed" {
                        $rand = Get-Random -Minimum 1 -Maximum 101
                        if ($rand -le 70) { $url = $weatherUrl } else { $url = $errorUrl }
                        $method = "GET"
                        $body = $null
                    }

                    "login" {
                        $url = $loginUrl
                        $method = "POST"
                        $body = @{
                            userName = "bhaleraor"
                            password = "bhaleraor@1234"
                        } | ConvertTo-Json
                    }

                    "ratelimit" {
                        $url = $weatherUrl
                        $method = "GET"
                        $body = $null
                    }
                }

                $job = Start-Job -ScriptBlock {
                    param($u, $m, $b)
                    Write-Host "Sending request: $u" -ForegroundColor Yellow

                    try {
                        if ($m -eq "POST") {
                            Invoke-RestMethod -Uri $u -Method POST -Body $b -ContentType "application/json" -TimeoutSec 60 | Out-Null
                        }
                        else {
                            Invoke-WebRequest -Uri $u -Method GET -UseBasicParsing -TimeoutSec 60 | Out-Null
                        }
                    }
                    catch {}
                } -ArgumentList $url, $method, $body

                $runningJobs += $job
                $sent++
            }
        }

        # =========================
        # pacing + jitter
        # =========================
        $sleep = $intervalMs + (Get-Random -Minimum -100 -Maximum 150)
        if ($sleep -lt 0) { $sleep = 0 }

        Start-Sleep -Milliseconds $sleep

        $runningJobs = Cleanup-CompletedJobs -Jobs $runningJobs
    }

    # =========================
    # FINAL DRAIN
    # =========================
    $runningJobs = Drain-AllJobs -Jobs $runningJobs

    $endTime = Get-Date
    Write-Host "Completed in $($endTime - $startTime)" -ForegroundColor Green
}