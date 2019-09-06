# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Disable-PSBreakpoint' -Tags 'CI' {
	
    BeforeEach {
        # Set some breakpoints
        $lineBp = Set-PSBreakpoint -Line ([int]::MaxValue) -Script $PSCommandPath
        $cmdBp = Set-PSBreakpoint -Command Test-ThisIsNotReallyACommand
        $varBp = Set-PSBreakpoint -Variable thisIsNotReallyAVariable
    }

    AfterEach {
        # Clean up after ourselves
        Get-PSBreakpoint | Remove-PSBreakpoint
    }

    It 'Should disable breakpoints using pipeline input by value' {
        foreach ($bp in $lineBp, $cmdBp, $varBp) {
            $bp = $bp | Disable-PSBreakpoint -PassThru
            $bp.Enabled | Should -Not -BeTrue
        }
    }

    It 'Should disable breakpoints using pipeline input by property name' {
        foreach ($bp in $lineBp, $cmdBp, $varBp) {
            $bp = [pscustomobject]@{ Id = $bp.Id } | Disable-PSBreakpoint -PassThru
            $bp.Enabled | Should -Not -BeTrue
        }
    }
}
