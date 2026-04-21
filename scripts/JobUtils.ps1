function Cleanup-CompletedJobs {
    param(
        [array]$Jobs,
        [string]$Label = "Cleanup"
    )

    $beforeCount = $Jobs.Count
    $removedCount = 0
    $remainingJobs = @()

    foreach ($job in $Jobs) {

        $state = $job.State

        if ($state -in @("Completed", "Failed", "Stopped")) {

            try {
                Receive-Job $job -ErrorAction SilentlyContinue | Out-Null
            } catch {}

            try {
                Remove-Job $job -Force -ErrorAction SilentlyContinue | Out-Null
            } catch {}

            $removedCount++
        }
        else {
            $remainingJobs += $job
        }
    }

    $afterCount = $remainingJobs.Count

    Write-Host "$Label : Removed $removedCount jobs (Completed/Failed/Stopped) | Remaining: $afterCount (was $beforeCount)" -ForegroundColor DarkGray

    return ,$remainingJobs
}function Drain-AllJobs {
    param(
        [array]$Jobs
    )

    if ($Jobs.Count -eq 0) {
        Write-Host "Final drain: No remaining jobs." -ForegroundColor DarkGray
        return @()
    }

    Write-Host "Final drain started. Total jobs: $($Jobs.Count)" -ForegroundColor Cyan

    $remainingJobs = $Jobs
    $total = $Jobs.Count
    $removedTotal = 0

    while ($true) {

        $nextRound = @()
        $removedThisRound = 0

        foreach ($job in $remainingJobs) {

            # Force refresh state (VERY IMPORTANT)
            $state = $job.State

            if ($state -in @("Completed", "Failed", "Stopped")) {

                try {
                    Receive-Job $job -ErrorAction SilentlyContinue | Out-Null
                } catch {}

                try {
                    Remove-Job $job -Force -ErrorAction SilentlyContinue | Out-Null
                } catch {}

                $removedThisRound++
                $removedTotal++
            }
            else {
                $nextRound += $job
            }
        }

        $remainingJobs = $nextRound

        Write-Host "Drain progress: Removed $removedTotal / $total | Remaining: $($remainingJobs.Count)" -ForegroundColor DarkGray

        if ($remainingJobs.Count -eq 0) {
            break
        }

        # IMPORTANT: give scheduler time to update states
        Start-Sleep -Seconds 2
    }

    Write-Host "Final drain completed. Removed all $total jobs." -ForegroundColor Green

    return @()
}