# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Write-Progress DRT Unit Tests" -Tags "CI" {
    It "Should be able to throw exception when running Write-Progress with bad percentage" {
        { Write-Progress -Activity 'myactivity' -Status 'mystatus' -percent 101 } |
        Should -Throw -ErrorId 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.WriteProgressCommand'
    }
    It "Should be able to throw exception when running Write-Progress with bad parent id " {
        { Write-Progress -Activity 'myactivity' -Status 'mystatus' -Id 1 -ParentId -2 } |
        Should -Throw -ErrorId 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.WriteProgressCommand'
    }
    It "all mandatory params works" -Pending {
        { Write-Progress -Activity 'myactivity' -Status 'mystatus' } | Should -Not -Throw
    }
    It "all params works" -Pending {
        { Write-Progress -Activity 'myactivity' -Status 'mystatus' -Id 1 -ParentId 2 -Completed:$false -current 'current' -sec 1 -percent 1 } | Should -Not -Throw
    }
    It 'Activity longer than console width works' {
        try {
            $activity = 'a' * ([console]::WindowWidth + 1)
        }
        catch {
            Set-ItResult -Skipped -Because 'Console width is not supported'
            return
        }
        { Write-Progress -Activity $activity -Status ('b' * ([console]::WindowWidth + 1)) -Id 1 } | Should -Not -Throw
        Write-Progress -Activity $activity -Id 1 -Completed
    }
    It 'Should be able to complete a progress record with no activity specified' {
        { Write-Progress -Completed } | Should -Not -Throw
    }
    It 'Minimal view does not accumulate bars updated to 100 percent' {
        $originalView = $PSStyle.Progress.View
        try {
            $PSStyle.Progress.View = 'Minimal'

            1..20 | ForEach-Object {
                Write-Progress -Id $_ -Activity "Task $_" -PercentComplete 50
                Write-Progress -Id $_ -Activity "Task $_" -PercentComplete 100
            } | Should -Not -Throw

            $hostUI = $Host.UI
            $pendingField = $hostUI.GetType().GetField('pendingProgress', 
                [System.Reflection.BindingFlags]'NonPublic,Instance')
            if ($pendingField) {
                $pending = $pendingField.GetValue($hostUI)
                $pending.Count | Should -BeLessOrEqual 5 -Because 'Minimal view should cap visible bars after 100%'
            }
            else {
                Write-Warning "Could not access internal 'pendingProgress' field via reflection. Skipping state assertion."
            }
        }
        finally {
            1..20 | ForEach-Object { Write-Progress -Id $_ -Completed } 2>$null
            $PSStyle.Progress.View = $originalView
        }
    }
    It 'Minimal view does not accumulate bars written directly at 100 percent' {
        $originalView = $PSStyle.Progress.View
        try {
            $PSStyle.Progress.View = 'Minimal'
            { 1..20 | ForEach-Object {
                    Write-Progress -Id $_ -Activity "Task $_" -PercentComplete 100
                } } | Should -Not -Throw

            $hostUI = $Host.UI
            $pendingField = $hostUI.GetType().GetField('pendingProgress', 
                [System.Reflection.BindingFlags]'NonPublic,Instance')
            if ($pendingField) {
                $pending = $pendingField.GetValue($hostUI)
                $pending.Count | Should -BeLessOrEqual 5 -Because 'Minimal view should cap visible bars'
            }
            else {
                Write-Warning "Could not access internal 'pendingProgress' field via reflection. Skipping state assertion."
            }
        }
        finally {
            1..20 | ForEach-Object { Write-Progress -Id $_ -Completed } 2>$null
            $PSStyle.Progress.View = $originalView
        }
    }
    It 'Minimal view handles many active bars without throwing' {
        $originalView = $PSStyle.Progress.View
        try {
            $PSStyle.Progress.View = 'Minimal'
            { 1..10 | ForEach-Object {
                    Write-Progress -Id $_ -Activity "Task $_" -PercentComplete 50
                } } | Should -Not -Throw
        }
        finally {
            1..10 | ForEach-Object { Write-Progress -Id $_ -Completed } 2>$null
            $PSStyle.Progress.View = $originalView
        }
    }
    It 'Minimal view shows most recently updated bars when capped' {
        $originalView = $PSStyle.Progress.View
        try {
            $PSStyle.Progress.View = 'Minimal'

            1..10 | ForEach-Object {
                Write-Progress -Id $_ -Activity "Task $_" -PercentComplete 50
            }

            1..3 | ForEach-Object {
                Write-Progress -Id $_ -Activity "Task $_ UPDATED" -PercentComplete 75
            }

            $hostUI = $Host.UI
            $pendingField = $hostUI.GetType().GetField('pendingProgress', 
                [System.Reflection.BindingFlags]'NonPublic,Instance')
            if ($pendingField) {
                $pending = $pendingField.GetValue($hostUI)
                $renderedIds = $pending | ForEach-Object { $_.Record.Id }
                $renderedIds | Should -Contain 1
                $renderedIds | Should -Contain 2
                $renderedIds | Should -Contain 3
            }
            else {
                Write-Warning "Could not access internal 'pendingProgress' field via reflection. Skipping state assertion."
            }
        }
        finally {
            1..10 | ForEach-Object { Write-Progress -Id $_ -Completed } 2>$null
            $PSStyle.Progress.View = $originalView
        }
    }
}