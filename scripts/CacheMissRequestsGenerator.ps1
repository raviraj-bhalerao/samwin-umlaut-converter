# Cache MISS generator

$url = "https://samwin-umlaut-converter-api.onrender.com/QueryGenerator/GetQuery?useCache"

Write-Host "Generating cache misses..."

for ($i = 1; $i -le 300; $i++) {
    $randomInput = "word$i"
    $fullUrl = "$url&input=$randomInput"

    Invoke-RestMethod -Uri $fullUrl -Method Get | Out-Null
    Start-Sleep -Milliseconds 200
}