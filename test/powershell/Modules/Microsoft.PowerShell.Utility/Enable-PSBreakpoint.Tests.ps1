# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Enable-PSBreakpoint' -Tags 'CI' {
	
    BeforeEach {
        # Set some breakpoints
        $lineBp = Set-PSBreakpoint -Line ([int]::MaxValue) -Script $PSCommandPath | Disable-PSBreakpoint -PassThru
        $cmdBp = Set-PSBreakpoint -Command Test-ThisIsNotReallyACommand | Disable-PSBreakpoint -PassThru
        $varBp = Set-PSBreakpoint -Variable thisIsNotReallyAVariable | Disable-PSBreakpoint -PassThru
    }

    AfterEach {
        # Clean up after ourselves
        Get-PSBreakpoint | Remove-PSBreakpoint
    }

    It 'Should enable breakpoints using pipeline input by value' {
        foreach ($bp in $lineBp, $cmdBp, $varBp) {
            $bp = $bp | Enable-PSBreakpoint -PassThru
            $bp.Enabled | Should -BeTrue
        }
    }

    It 'Should enable breakpoints using pipeline input by property name' {
        foreach ($bp in $lineBp, $cmdBp, $varBp) {
            $bp = [pscustomobject]@{ Id = $bp.Id } | Enable-PSBreakpoint -PassThru
            $bp.Enabled | Should -BeTrue
        }
    }
}
