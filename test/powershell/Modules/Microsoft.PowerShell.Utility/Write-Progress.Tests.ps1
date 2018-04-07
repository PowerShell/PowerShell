# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Write-Progress DRT Unit Tests" -Tags "CI" {
    It "Should be able to throw exception when missing mandatory parameters" {
        { Write-Progress $null } | Should -Throw -ErrorId 'ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.WriteProgressCommand'
    }

    It "Should be able to throw exception when running Write-Progress with bad percentage" {
        { write-progress -activity 'myactivity' -status 'mystatus' -percent 101 } |
	    Should -Throw -ErrorId 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.WriteProgressCommand'
    }

    It "Should be able to throw exception when running Write-Progress with bad parent id " {
        { write-progress -activity 'myactivity' -status 'mystatus' -id 1 -parentid -2 } |
	    Should -Throw -ErrorId 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.WriteProgressCommand'
    }

    It "all mandatory params works" -Pending {
        { write-progress -activity 'myactivity' -status 'mystatus' } | Should -Not -Throw
    }

    It "all params works" -Pending {
        { write-progress -activity 'myactivity' -status 'mystatus' -id 1 -parentId 2 -completed:$false -current 'current' -sec 1 -percent 1 } | Should -Not -Throw
    }
}
