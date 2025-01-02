# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Tests for (error, warning, etc) action preference" -Tags "CI" {
    $commonActionPreferenceParameterTestCases = foreach ($commonParameterName in [System.Management.Automation.Cmdlet]::CommonParameters | Select-String Action) {
        @{
            ActionPreferenceParameterName = $commonParameterName
        }
    }

    $actionPreferenceVariableTestCases = foreach ($variable in Get-Variable -Name *Preference -Scope Global | Where-Object Value -Is [System.Management.Automation.ActionPreference]) {
        @{
            ActionPreferenceVariableName = $variable.Name
            StreamName = $variable.Name -replace '(Action)?Preference$'
        }
    }

    $actionPreferenceVariableValueTestCases = @(
        @{
            Value = [System.Management.Automation.ActionPreference]::Suspend
            DisplayValue = '[System.Management.Automation.ActionPreference]::Suspend'
        }
        @{
            Value        = 'Suspend'
            DisplayValue = '''Suspend'''
        }
    )

    BeforeAll {
        $orgin = $GLOBAL:errorActionPreference

        function Join-TestCase {
            [OutputType([Hashtable[]])]
            [CmdletBinding()]
            param(
                [Hashtable[]]$Set1,
                [Hashtable[]]$Set2
            )
            foreach ($ht1 in $Set1) {
                foreach ($ht2 in $Set2) {
                    $ht1 + $ht2
                }
            }
        }

        function Test-ActionPreferenceVariableSuspendValue {
            [CmdletBinding()]
            param(
                $Value
            )
            if ($DebugPreference -eq $Value) {
                Write-Debug -Message 'A debug message'
            } elseif ($ErrorActionPreference -eq $Value) {
                Write-Error -Message 'An error message'
            } elseif ($InformationPreference -eq $Value) {
                Write-Information -MessageData 'Some information'
            } elseif ($ProgressPreference -eq $Value) {
                Write-Progress -Activity 'Some progress'
            } elseif ($VerbosePreference -eq $Value) {
                Write-Verbose -Message 'A verbose message'
            } elseif ($WarningPreference -eq $Value) {
                Write-Warning -Message 'A warning message'
            }
        }
    }

    AfterAll {
        if ($GLOBAL:errorActionPreference -ne $orgin) {
            $GLOBAL:errorActionPreference = $orgin
        }
    }

    Context 'Setting ErrorActionPreference to stop prevents user from getting the error exception' {
        $err = $null
        try {
            Get-ChildItem nosuchfile.nosuchextension -ErrorAction stop -ErrorVariable err
        } catch { }

        It '$err.Count' { $err.Count | Should -Be 1 }
        It '$err[0] should not be $null' { $err[0] | Should -Not -BeNullOrEmpty }
        It '$err[0].GetType().Name' { $err[0] | Should -BeOfType System.Management.Automation.ActionPreferenceStopException }
        It '$err[0].ErrorRecord' { $err[0].ErrorRecord | Should -Not -BeNullOrEmpty }
        It '$err[0].ErrorRecord.Exception.GetType().Name' { $err[0].ErrorRecord.Exception | Should -BeOfType System.Management.Automation.ItemNotFoundException }
    }

    It 'Action preference of Ignore can be set as a preference variable using a string value' {
        try {
            Remove-Variable -Name ErrorActionPreference -Scope Global -Force
            $GLOBAL:ErrorActionPreference = 'Ignore'
            $errorCount = $error.Count
            Get-Process -Name asdfasdfasdf
            $error.Count | Should -BeExactly $errorCount
        } finally {
            Remove-Variable -Name ErrorActionPreference -Scope Global
            # Re-create the action preference variable as a strongly typed variable like it was before
            [System.Management.Automation.ActionPreference]$GLOBAL:ErrorActionPreference = $orgin
        }
    }

    It 'Action preference of Ignore can be set as a preference variable using an enumerated value' {
        try {
            $GLOBAL:ErrorActionPreference = [System.Management.Automation.ActionPreference]::Ignore
            $errorCount = $error.Count
            Get-Process -Name asdfasdfasdf
            $error.Count | Should -BeExactly $errorCount
        } finally {
            $GLOBAL:ErrorActionPreference = $orgin
        }
    }

    It 'The $global:<ActionPreferenceVariableName> variable does not support Suspend' -TestCases $actionPreferenceVariableTestCases {
        param($ActionPreferenceVariableName)

        $e = {
            Set-Variable -Name $ActionPreferenceVariableName -Scope Global -Value ([System.Management.Automation.ActionPreference]::Suspend)
        } | Should -Throw -ErrorId RuntimeException -PassThru

        $e.CategoryInfo.Reason | Should -BeExactly 'ArgumentTransformationMetadataException'
    }

    It 'A local $<ActionPreferenceVariableName> variable does not support <DisplayValue>' -TestCases (Join-TestCase -Set1 $actionPreferenceVariableTestCases -Set2 $actionPreferenceVariableValueTestCases) {
        param(
            $ActionPreferenceVariableName,
            $StreamName,
            $Value,
            $DisplayValue
        )

        $e = {
            Set-Variable -Name $ActionPreferenceVariableName -Value $Value
            Test-ActionPreferenceVariableSuspendValue -Value $Value
        } | Should -Throw -ErrorId "System.NotSupportedException$(if ($StreamName -ne 'Error') {",Microsoft.PowerShell.Commands.Write${StreamName}Command"})" -PassThru

        $e.CategoryInfo.Reason | Should -BeExactly 'NotSupportedException'
    }

    It 'enum disambiguation works' {
        $errorCount = $error.Count
        Get-Process -Name asdfasdfsadfsadf -ErrorAction Ig

        $error.Count | Should -BeExactly $errorCount
    }

    #issue 2076
    It 'The -<ActionPreferenceParameterName> common parameter does not support Suspend on cmdlets' -TestCases $commonActionPreferenceParameterTestCases {
        param($ActionPreferenceParameterName)

        $commonParameters = @{
            "${ActionPreferenceParameterName}" = [System.Management.Automation.ActionPreference]::Suspend
        }

        { Write-Output -InputObject Test @commonParameters } | Should -Throw -ErrorId "ParameterBindingFailed,Microsoft.PowerShell.Commands.WriteOutputCommand"
    }

    It 'The -<ActionPreferenceParameterName> common parameter does not support Suspend on functions' -TestCases $commonActionPreferenceParameterTestCases {
        param($ActionPreferenceParameterName)

        function MyHelperFunction {
            [CmdletBinding()]
            param()
            "Hello"
        }

        $commonParameters = @{
            "${ActionPreferenceParameterName}" = [System.Management.Automation.ActionPreference]::Suspend
        }

        { MyHelperFunction -ErrorAction Suspend } | Should -Throw -ErrorId "ParameterBindingFailed,MyHelperFunction"
    }

    It '<switch> does not take precedence over $ErrorActionPreference' -TestCases @(
        @{switch = "Verbose" },
        @{switch = "Debug" }
    ) {
        param($switch)
        $ErrorActionPreference = "SilentlyContinue"
        $params = @{
            ItemType = "File";
            Path     = "$testdrive\test.txt";
            Confirm  = $false
        }
        New-Item @params > $null
        $params += @{$switch = $true }
        { New-Item @params } | Should -Not -Throw
        $ErrorActionPreference = "Stop"
        { New-Item @params } | Should -Throw -ErrorId "NewItemIOError,Microsoft.PowerShell.Commands.NewItemCommand"
        Remove-Item "$testdrive\test.txt" -Force
    }

    It "Parameter binding '-<name>' throws correctly (no NRE) if argument is <argValue>" -TestCases @(
        @{ name = "ErrorAction";       argValue = "null";           arguments = @{ ErrorAction = $null } }
        @{ name = "WarningAction";     argValue = "null";           arguments = @{ WarningAction = $null } }
        @{ name = "InformationAction"; argValue = "null";           arguments = @{ InformationAction = $null } }
        @{ name = "ErrorAction";       argValue = "AutomationNull"; arguments = @{ ErrorAction = [System.Management.Automation.Internal.AutomationNull]::Value } }
        @{ name = "WarningAction";     argValue = "AutomationNull"; arguments = @{ WarningAction = [System.Management.Automation.Internal.AutomationNull]::Value } }
        @{ name = "InformationAction"; argValue = "AutomationNull"; arguments = @{ InformationAction = [System.Management.Automation.Internal.AutomationNull]::Value } }
        @{ name = "ProgressAction";    argValue = "AutomationNull"; arguments = @{ ProgressAction = [System.Management.Automation.Internal.AutomationNull]::Value } }
    ) {
        param($arguments)

        $err = $null
        try {
            Test-Path .\noexistfile.ps1 @arguments
        } catch {
            $err = $_
        }

        $err.FullyQualifiedErrorId | Should -BeExactly "ParameterBindingFailed,Microsoft.PowerShell.Commands.TestPathCommand"
        $err.Exception.InnerException.InnerException | Should -BeOfType "System.Management.Automation.PSInvalidCastException"
    }
}

Describe 'ActionPreference.Break tests' -Tag 'CI' {

    BeforeAll {
        Register-DebuggerHandler
    }

    AfterAll {
        Unregister-DebuggerHandler
    }

    Context '-ErrorAction Break should break on a non-terminating error' {
        BeforeAll {
            $testScript = {
                function Test-Break {
                    [CmdletBinding()]
                    param()
                    try {
                        # Generate a non-terminating error
                        Write-Error 'This is a non-terminating error.'
                        # Do something afterwards
                        'This should still run'
                    } catch {
                        'Do nothing'
                    } finally {
                        'This finally runs'
                    }
                }
                Test-Break -ErrorAction Break
            }

            $results = @(Test-Debugger -Scriptblock $testScript -CommandQueue 'v', 'v')
        }

        It 'Should show 3 debugger commands were invoked' {
            # There is always an implicit 'c' command that keeps the debugger automation moving
            $results.Count | Should -Be 3
        }

        It 'The breakpoint should be the statement that generated the non-terminating error' {
            $results[0] | ShouldHaveExtent -Line 7 -FromColumn 25 -ToColumn 71
        }

        It 'The second statement should be the statement after that which generated the non-terminating error' {
            $results[1] | ShouldHaveExtent -Line 9 -FromColumn 25 -ToColumn 48
        }

        It 'The third statement should be the statement in the finally block' {
            $results[2] | ShouldHaveExtent -Line 13 -FromColumn 25 -ToColumn 44
        }
    }

    Context '-ErrorAction Break should break on a terminating error' {
        BeforeAll {
            $testScript = {
                function Test-Break {
                    [CmdletBinding()]
                    param()
                    try {
                        # Generate a terminating error
                        Get-Process -TheAnswer 42
                        # Do something afterwards
                        'This should not run'
                    } catch {
                        'Do nothing'
                    } finally {
                        'This finally runs'
                    }
                }
                Test-Break -ErrorAction Break
            }

            $results = @(Test-Debugger -Scriptblock $testScript -CommandQueue 'v', 'v')
        }

        It 'Should show 3 debugger commands were invoked' {
            # There is always an implicit 'c' command that keeps the debugger automation moving
            $results.Count | Should -Be 3
        }

        It 'The breakpoint should be the statement that generated the terminating error' {
            $results[0] | ShouldHaveExtent -Line 7 -FromColumn 25 -ToColumn 50
        }

        It 'The second statement should be the statement in the catch block where the terminating error is caught' {
            $results[1] | ShouldHaveExtent -Line 11 -FromColumn 25 -ToColumn 37
        }

        It 'The third statement should be the statement in the finally block' {
            $results[2] | ShouldHaveExtent -Line 13 -FromColumn 25 -ToColumn 44
        }
    }

    Context '-ErrorAction Break should not break on a naked rethrow' {
        BeforeAll {
            $testScript = {
                function Test-Break {
                    [CmdletBinding()]
                    param()
                    try {
                        try {
                            # Generate a terminating error
                            Get-Process -TheAnswer 42
                        } catch {
                            throw
                        }
                    } catch {
                        # Swallow the exception here
                    }
                }
                Test-Break -ErrorAction Break
            }

            $results = @(Test-Debugger -Scriptblock $testScript)
        }

        It 'Should show 1 debugger command was invoked' {
            # ErrorAction break should only trigger on the initial terminating error
            $results.Count | Should -Be 1
        }

        It 'The breakpoint should be the statement that generated the terminating error' {
            $results[0] | ShouldHaveExtent -Line 8 -FromColumn 29 -ToColumn 54
        }
    }

    Context '-ErrorAction Break should break when throwing a specific error or object' {
        BeforeAll {
            $testScript = {
                function Test-Break {
                    [CmdletBinding()]
                    param()
                    try {
                        try {
                            # Generate a terminating error
                            Get-Process -TheAnswer 42
                        } catch {
                            throw $_
                        }
                    } catch {
                        # Swallow the exception here
                    }
                }
                Test-Break -ErrorAction Break
            }

            $results = @(Test-Debugger -Scriptblock $testScript)
        }

        It 'Should show 2 debugger commands were invoked' {
            # ErrorAction break should trigger on the initial terminating error and the throw
            # since it throws a "new" error (throwing anything is considered a new terminating
            # error)
            $results.Count | Should -Be 2
        }

        It 'The first breakpoint should be the statement that generated the terminating error' {
            $results[0] | ShouldHaveExtent -Line 8 -FromColumn 29 -ToColumn 54
        }

        It 'The second breakpoint should be the statement that threw $_' {
            $results[1] | ShouldHaveExtent -Line 10 -FromColumn 29 -ToColumn 37
        }
    }

    Context 'Other message types should break on their corresponding messages when requested' {
        BeforeAll {
            $testScript = {
                function Test-Break {
                    [CmdletBinding()]
                    param()
                    Write-Warning -Message 'This is a warning message'
                    Write-Verbose -Message 'This is a verbose message'
                    Write-Debug -Message 'This is a debug message'
                    Write-Information -MessageData 'This is an information message'
                    Write-Progress -Activity 'This shows progress'
                    Write-Progress -Activity 'This shows progress' -Completed
                }
                Test-Break -WarningAction Break -InformationAction Break *>$null
                $WarningPreference = $VerbosePreference = $DebugPreference = $InformationPreference = $ProgressPreference = [System.Management.Automation.ActionPreference]::Break
                Test-Break *>$null
            }
            $results = @(Test-Debugger -Scriptblock $testScript)
        }

        It 'Should show 8 debugger commands were invoked' {
            # When no debugger commands are provided, 'c' is invoked every time a breakpoint is hit
            $results.Count | Should -Be 8
        }

        It 'Write-Warning should trigger a breakpoint from -WarningAction Break' {
            $results[0] | ShouldHaveExtent -Line 5 -FromColumn 21 -ToColumn 71
        }

        It 'Write-Information should trigger a breakpoint from -InformationAction Break' {
            $results[1] | ShouldHaveExtent -Line 8 -FromColumn 21 -ToColumn 84
        }

        It 'Write-Warning should trigger a breakpoint from $WarningPreference = [System.Management.Automation.ActionPreference]::Break' {
            $results[2] | ShouldHaveExtent -Line 5 -FromColumn 21 -ToColumn 71
        }

        It 'Write-Verbose should trigger a breakpoint from $VerbosePreference = [System.Management.Automation.ActionPreference]::Break' {
            $results[3] | ShouldHaveExtent -Line 6 -FromColumn 21 -ToColumn 71
        }

        It 'Write-Debug should trigger a breakpoint from $DebugPreference = [System.Management.Automation.ActionPreference]::Break' {
            $results[4] | ShouldHaveExtent -Line 7 -FromColumn 21 -ToColumn 67
        }

        It 'Write-Information should trigger a breakpoint from $InformationPreference = [System.Management.Automation.ActionPreference]::Break' {
            $results[5] | ShouldHaveExtent -Line 8 -FromColumn 21 -ToColumn 84
        }

        It 'Write-Progress should trigger a breakpoint from $ProgressPreference = [System.Management.Automation.ActionPreference]::Break' {
            $results[6] | ShouldHaveExtent -Line 9 -FromColumn 21 -ToColumn 67
        }
    }

    Context 'ActionPreference.Break in jobs' {

        BeforeAll {
            $job = Start-Job {
                $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Break
                Get-Process -TheAnswer 42
            }
        }

        AfterAll {
            Remove-Job -Job $job -Force
        }

        It 'ActionPreference.Break should break in a running job' {
            Wait-UntilTrue -sb { $job.State -eq 'AtBreakpoint' } -TimeoutInMilliseconds (60 * 1000) -IntervalInMilliseconds 100 | Should -BeTrue
        }
    }
}
