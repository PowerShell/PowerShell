# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Write-Progress DRT Unit Tests" -Tags "CI" {
    It "Should be able to throw exception when missing mandatory parameters" {
        { Write-Progress $null } | Should -Throw -ErrorId 'ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.WriteProgressCommand'
    }

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
        $activity = 'a' * ([console]::WindowWidth + 1)
        { Write-Progress -Activity $activity -Status ('b' * ([console]::WindowWidth + 1)) -Id 1 } | Should -Not -Throw
        Write-Progress -Activity $activity -Id 1 -Completed
    }
}
