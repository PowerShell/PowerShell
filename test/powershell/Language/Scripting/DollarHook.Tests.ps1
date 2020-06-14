# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Tests for setting $? for execution success' -Tag 'CI' {
    BeforeAll {
        $script:hookValues = [System.Collections.Generic.List[bool]]::new()

        function Hook
        {
            param([Parameter(Position = 0)][bool]$Value)

            $script:hookValues.Add($Value)
        }

    }

    AfterEach {
        $script:hookValues.Clear()
    }

    It 'Sets $? for case ''<Script>''' -TestCases @(
        # Cmdlets
        @{ Script = 'Write-Output "Hi"; Hook $?'; Results = @($true) }
        @{ Script = 'Write-Error "Bad"; Hook $?'; Results = @($false) }
        @{ Script = 'Write-Error "Bad"; Hook $?; Hook $?'; Results = @($false, $true) }

        # Native executables
        @{ Script = 'testexe -returncode 0; Hook $?'; Results = @($true) }
        @{ Script = 'testexe -returncode 1; Hook $?; Hook $?'; Results = @($false, $true) }

        # Pure values
        @{ Script = '"Hi"; Hook $?'; Results = @($true) }
        @{ Script = 'Write-Error "Bad"; "Hi"; Hook $?'; Results = @($true) }

        # Paren expressions (subpipelines)
        @{ Script = '(Write-Output "Hi"); Hook $?'; Results = @($true) }
        @{ Script = 'Write-Error "Bad"; ("Hi"); Hook $?'; Results = @($true) }
        @{ Script = '(Write-Error "Bad"); Hook $?; Hook $?'; Results = @($false, $true) }
        @{ Script = '(testexe -returncode 0); Hook $?'; Results = @($true) }
        @{ Script = '(testexe -returncode 1); Hook $?; Hook $?'; Results = @($false, $true) }

        # Subexpressions
        @{ Script = '$("Hi"); Hook $?'; Results = @($true) }
        @{ Script = 'Write-Error "Bad"; $("Hi"); Hook $?'; Results = @($true) }
        @{ Script = '$(); Hook $?'; Results = @($true) }
        @{ Script = 'Write-Error "Bad"; $(); Hook $?'; Results = @($true) }
        @{ Script = '$(Write-Error "Bad"); Hook $?; Hook $?'; Results = @($false, $true) }
        @{ Script = '$(testexe -returncode 1); Hook $?; Hook $?'; Results = @($false, $true) }
        @{ Script = 'Write-Error "Bad"; $(trap { continue }); Hook $?'; Results = @($true) }
        @{ Script = 'Write-Error "Bad"; $(trap { continue } "Hi"); Hook $?'; Results = @($true) }

        # Array expressions
        @{ Script = '@("Hi"); Hook $?'; Results = @($true) }
        @{ Script = '@(); Hook $?'; Results = @($true) }
        @{ Script = 'Write-Error "Bad"; @("Hi"); Hook $?'; Results = @($true) }
        @{ Script = 'Write-Error "Bad"; @(); Hook $?'; Results = @($true) }
        @{ Script = 'Write-Error "Bad"; @("Hi", "There"); Hook $?'; Results = @($true) }
        @{ Script = 'Write-Error "Bad"; @("Hi"; "There"); Hook $?'; Results = @($true) }
        @{ Script = 'Write-Error "Bad"; @(trap { continue }); Hook $?'; Results = @($true) }
        @{ Script = 'Write-Error "Bad"; @(trap { continue } "Hi"); Hook $?'; Results = @($true) }
        @{ Script = 'Write-Error "Bad"; @(trap { continue } "Hi", "There"); Hook $?'; Results = @($true) }
        @{ Script = '@(Write-Error "Bad"); Hook $?; Hook $?'; Results = @($false, $true) }
        @{ Script = '@(Write-Error "Bad"; "Hi"); Hook $?; Hook $?'; Results = @($true, $true) }
        @{ Script = '@("Hi"; Write-Error "Bad"); Hook $?; Hook $?'; Results = @($false, $true) }
        @{ Script = '@(Write-Error "Bad"; Write-Output "Hi"); Hook $?; Hook $?'; Results = @($true, $true) }
        @{ Script = '@(Write-Output "Hi"; Write-Error "Bad"); Hook $?; Hook $?'; Results = @($false, $true) }
    ) {
        param([string]$Script, [object[]]$Results)

        Invoke-Expression $Script 2>&1 > $null

        $script:hookValues | Should -Be $Results
    }

    It 'Sets $? correctly for string expression ''<Expression>''' -TestCases @(
        @{ Expression = '"Hi"; Hook $?'; HookResults = $($true); PipelineResults = @('Hi') }
        @{ Expression = '"Hi $("Good")"; Hook $?'; HookResults = $($true); PipelineResults = @('Hi Good') }
        @{ Expression = '"Hi $(Write-Error "Bad")"; Hook $?'; HookResults = $($true); PipelineResults = @('Hi ') }
        @{ Expression = '"Hi $(Write-Output "Good")"; Hook $?'; HookResults = $($true); PipelineResults = @('Hi Good') }
    ) {
        param([string]$Expression, [object[]]$HookResults, [object[]]$PipelineResults)

        $output = Invoke-Expression $Expression 2>$null

        $script:hookValues | Should -Be $HookResults
        $output | Should -Be $PipelineResults
    }
}
