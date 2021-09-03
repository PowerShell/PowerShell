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

    It 'Sets $? correctly for single expression with redirection ''<Expression>''' -TestCases @(
        @{ Expression = 'Write-Error "Bad"; Hook $?; "b" > $null; Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; "b" >> $null; Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; "b" > TESTDRIVE:\out.txt; Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; "b" >> TESTDRIVE:\out.txt; Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; "b" 2> $null; Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; "b" 2>> $null; Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; "b" 2> TESTDRIVE:\out.txt; Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; "b" 2>> TESTDRIVE:\out.txt; Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; "b" 2>&1; Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; "b" 2>&1; Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; "b" 2>&1 > $null; Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; "b" 2>&1 > TESTDRIVE:\out.txt; Hook $?'; HookResults = $($false, $true); }

        @{ Expression = 'Write-Error "Bad"; Hook $?; ("b" > $null); Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; ("b" >> $null); Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; ("b" > TESTDRIVE:\out.txt); Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; ("b" >> TESTDRIVE:\out.txt); Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; ("b" 2> $null); Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; ("b" 2>> $null); Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; ("b" 2> TESTDRIVE:\out.txt); Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; ("b" 2>> TESTDRIVE:\out.txt); Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; ("b" 2>&1); Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; ("b" 2>&1 > $null); Hook $?'; HookResults = $($false, $true); }
        @{ Expression = 'Write-Error "Bad"; Hook $?; ("b" 2>&1 > TESTDRIVE:\out.txt); Hook $?'; HookResults = $($false, $true); }
    ) {
        param([string]$Expression, [object[]]$HookResults)

        Invoke-Expression $Expression 2>&1 >$null

        $script:hookValues | Should -Be $HookResults
    }

    Context 'Validate $? with potential terminating error' {

        ## Script execution directly in Pester tests will be enclosed in try/catch by the Pester,
        ## and therefore, general exceptions thrown from an expression like "1/0" will be turned
        ## into a terminating exception, which will stop the execution of remaining scripts.
        ##
        ## For those test cases, we have to use a PowerShell instance, so as to keep the default
        ## error handling behavior for the general exceptions.

        BeforeAll {
            $pwsh = [powershell]::Create()

            function Invoke([string] $script)
            {
                $pwsh.Commands.Clear()
                $pwsh.Streams.ClearStreams()
                $pwsh.AddScript($script).Invoke()
            }

            $root = Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString())
            $null = Invoke "New-PSDrive -Name TESTDRIVE -PSProvider FileSystem -Root $root"
        }

        Afterall {
            $null = Invoke "Remove-PSDrive -Name TESTDRIVE -PSProvider FileSystem -Force"
            $pwsh.Dispose()
        }

        It 'Sets $? correctly for single expression with redirection ''<Expression>''' -TestCases @(
            @{ Expression = '1/0 > $null; $?'; Result = $false; }
            @{ Expression = '1/0 >> $null; $?'; Result = $false; }
            @{ Expression = '1/0 > TESTDRIVE:\out.txt; $?'; Result = $false; }
            @{ Expression = '1/0 >> TESTDRIVE:\out.txt; $?'; Result = $false; }
            @{ Expression = '1/0 2>&1; $?'; Result = $false; }
            @{ Expression = '1/0 2>&1 > $null; $?'; Result = $false; }
            @{ Expression = '1/0 2>&1 > TESTDRIVE:\out.txt; $?'; Result = $false; }

            @{ Expression = '"b" > NonExistDrive:\nowhere.txt; $?'; Result = $false; }
            @{ Expression = '"b" >> NonExistDrive:\nowhere.txt; $?'; Result = $false; }
            @{ Expression = '"b" 2>&1 > NonExistDrive:\nowhere.txt; $?'; Result = $false; }
        ) {
            param([string]$Expression, $Result)

            Invoke $Expression | Should -Be $Result
        }
    }
}
