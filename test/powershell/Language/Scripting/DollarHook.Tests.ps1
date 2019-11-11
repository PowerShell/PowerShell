# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Tests for setting $? for execution success' -Tag 'CI' {
    BeforeAll {
        $script:hookValues = [System.Collections.Generic.List[bool]]::new()

        function Hook
        {
            param([Parameter(Position = 0)][bool]$Value)

            $script:hookValues.Add($Value)
        }

        $testCases = @(
            @{ Script = '"Hi"; Hook $?'; Results = @($true) }
            @{ Script = 'Write-Output "Hi"; Hook $?'; Results = @($true) }
            @{ Script = 'Write-Error "Bad"; Hook $?'; Results = @($false) }
            @{ Script = 'Write-Error "Bad"; Hook $?; Hook $?'; Results = @($false, $true) }
            @{ Script = '(Write-Output "Hi"); Hook $?'; Results = @($true) }
            @{ Script = '("Hi"); Hook $?'; Results = @($true) }
            @{ Script = '$("Hi"); Hook $?'; Results = @($true) }
            @{ Script = '(Write-Error "Bad"); Hook $?; Hook $?'; Results = @($false, $true) }
            @{ Script = '$(Write-Error "Bad"); Hook $?; Hook $?'; Results = @($false, $true) }
            @{ Script = 'testexe -returncode 0; Hook $?'; Results = @($true) }
            @{ Script = 'testexe -returncode 1; Hook $?; Hook $?'; Results = @($false, $true) }
            @{ Script = '(testexe -returncode 0); Hook $?'; Results = @($true) }
            @{ Script = '(testexe -returncode 1); Hook $?; Hook $?'; Results = @($false, $true) }
            @{ Script = '$(testexe -returncode 1); Hook $?; Hook $?'; Results = @($false, $true) }
        )
    }

    AfterEach {
        $script:hookValues.Clear()
    }

    It 'Sets $? for case ''<Script>''' -TestCases $testCases {
        param([string]$Script, [object[]]$Results)

        Invoke-Expression $Script 2>&1 > $null

        $script:hookValues | Should -Be $Results
    }
}
