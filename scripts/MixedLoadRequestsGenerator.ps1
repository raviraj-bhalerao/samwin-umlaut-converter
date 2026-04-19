# Mixed traffic

$normalUrl = "https://samwin-umlaut-converter-api.onrender.com/WeatherForecast"
$errorUrl = "https://samwin-umlaut-converter-api.onrender.com/WeatherForecast?simulateError"

for ($i = 1; $i -le 1000; $i++) {

    $rand = Get-Random -Minimum 1 -Maximum 10

    if ($rand -le 7) {
        Invoke-RestMethod -Uri $normalUrl -Method Get | Out-Null
    }
    else {
        try {
            Invoke-RestMethod -Uri $errorUrl -Method Get -ErrorAction Stop
        } catch {}
    }

    Start-Sleep -Milliseconds 150
}