Clear-Host
Write-Host "Cleaning previous test results..."
Get-ChildItem -Path "src" -Include "TestResults" -Recurse -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Running tests with coverage..."

dotnet test src/Samwin.UmlautConverter.sln --collect:"XPlat Code Coverage" --settings coverage.runsettings

Write-Host "Generating coverage report..."

dotnet tool run reportgenerator -reports:"src/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html

Write-Host ""
Write-Host "Coverage report generated:"
Write-Host "coverage-report/index.html"

Start-Process "coverage-report/index.html"