function Cleanup-CompletedJobs {
    param(
        [array]$Jobs,
        [string]$Label = "Cleanup"
    )

    $beforeCount = $Jobs.Count
    $removedCount = 0

    $remainingJobs = $Jobs | Where-Object {
        if ($_.State -eq "Completed") {
            try { Receive-Job $_ | Out-Null } catch {}
            Remove-Job $_ | Out-Null
            $script:removedCount++
            return $false
        }
        return $true
    }

    $afterCount = $remainingJobs.Count

    Write-Host "$Label : Removed $removedCount completed jobs | Remaining: $afterCount (was $beforeCount)" -ForegroundColor DarkGray

    return ,$remainingJobs
}
function Drain-AllJobs {
    param(
        [array]$Jobs
    )

    if ($Jobs.Count -eq 0) {
        Write-Host "Final drain: No remaining jobs." -ForegroundColor DarkGray
        return @()
    }

    Write-Host "`nFinal drain started. Total jobs: $($Jobs.Count)" -ForegroundColor Cyan

    $remainingJobs = $Jobs
    $total = $Jobs.Count
    $removedTotal = 0

    while ($remainingJobs.Count -gt 0) {

        $currentRemaining = @()
        $removedThisRound = 0

        foreach ($job in $remainingJobs) {

            if ($job.State -eq "Completed") {
                try { Receive-Job $job | Out-Null } catch {}
                Remove-Job $job | Out-Null
                $removedThisRound++
                $removedTotal++
            }
            else {
                $currentRemaining += $job
            }
        }

        $remainingJobs = $currentRemaining

        Write-Host "Drain progress: Removed $removedTotal / $total | Remaining: $($remainingJobs.Count)" -ForegroundColor DarkGray

        if ($remainingJobs.Count -gt 0) {
            Start-Sleep -Seconds 5
        }
    }

    Write-Host "Final drain completed. Removed all $total jobs." -ForegroundColor Green

    return @()
}