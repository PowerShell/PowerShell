# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

$FeatureEnabled = $EnabledExperimentalFeatures.Contains('Microsoft.PowerShell.Utility.PSDebugRunspaceWithBreakpoints')

Describe "`Enable-RunspaceDebug -Breakpoint` Unit Tests - Feature-Enabled" -Tags "CI" {

    BeforeAll {
        if (!$FeatureEnabled) {
            Write-Verbose "Test Suite Skipped. The test suite requires the experimental feature 'Microsoft.PowerShell.Utility.PSDebugRunspaceWithBreakpoints' to be enabled." -Verbose
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = $true
            return
        }

        #Set up script file 1
        $scriptFileName1 = Join-Path $TestDrive -ChildPath breakpointTestScript.ps1

        $contents = @"
function Hello
{
    `$greeting = 'Hello, world!'
    write-host `$greeting
}

function Goodbye
{
    `$message = 'Good bye, cruel world!'
    write-host `$message
}

Hello
Goodbye
"@

        $contents > $scriptFileName1

        # The breakpoints are created here because when the tests are run with the experimental feature off,
        # this command does not exist and the Pester tests fail to work
        $breakpointArr = @(
            New-PSBreakpoint -Line 12 $scriptFileName1
            New-PSBreakpoint -Line 13 $scriptFileName1
        )

        $iss = [initialsessionstate]::CreateDefault2();
        $testRunspace1 = [runspacefactory]::CreateRunspace($iss)
        $testRunspace1.Name = "TestRunspaceDebuggerReset"
        $testRunspace1.Open()
    }

    AfterAll {
        if (!$FeatureEnabled) {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
            return
        }

        # Clean up
        $testRunspace1.Dispose()
    }

    It "Can set breakpoints in the runspace - <Name>" -TestCases @(
        @{
            Name = "Current runspace"
            Runspace = [System.Management.Automation.Runspaces.Runspace]::DefaultRunspace
            Breakpoints = $breakpointArr
        },
        @{
            Name = $testRunspace1.Name
            Runspace = $testRunspace1
            Breakpoints = $breakpointArr
        }
    ) {
        param($Runspace, $Breakpoints)
        Enable-RunspaceDebug -Breakpoint $Breakpoints -Runspace $Runspace
        $Runspace.Debugger.GetBreakpoints() | Should -Be @($Breakpoints)
    }
}
