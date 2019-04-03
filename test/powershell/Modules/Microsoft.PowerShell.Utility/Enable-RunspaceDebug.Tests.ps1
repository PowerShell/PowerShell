# Copyright (c) Microsoft Corporation. All rights reserved.
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

        # Set up script file 2
        $scriptFileName2 = Join-Path -Path $PSScriptRoot -ChildPath psbreakpointtestscript.ps1

        "`$var = 1 " > $scriptFileName2

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

        # clean up
        Remove-Item -Path $scriptFileName1 -Force -ErrorAction SilentlyContinue
        Remove-Item -Path $scriptFileName2 -Force -ErrorAction SilentlyContinue
        $testRunspace1.Dispose()
    }

    It "Can set breakpoints in the runspace - <Name>" -TestCases @(
        @{
            Name = "Current runspace"
            Runspace = [System.Management.Automation.Runspaces.Runspace]::DefaultRunspace
            Breakpoints = New-PSBreakpoint -Line 13 -Script $scriptFileName1
        },
        @{
            Name = $testRunspace1.Name
            Runspace = $testRunspace1
            Breakpoints = New-PSBreakpoint -Line 13 -Script $scriptFileName1
        }
    ) {
        param($Runspace, $Breakpoints)
        Enable-RunspaceDebug -Breakpoint $Breakpoints -Runspace $Runspace
        $Runspace.Debugger.GetBreakpoints() | Should -Be @($Breakpoints)
    }
}

Describe "`Enable-RunspaceDebug -Breakpoint` Unit Tests - Feature-Disabled" -Tags "CI" {

    BeforeAll {
        if ($FeatureEnabled) {
            Write-Verbose "Test Suite Skipped. The test suite requires the experimental feature 'Microsoft.PowerShell.Utility.PSDebugRunspaceWithBreakpoints' to be disabled." -Verbose
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = $true
            return
        }
    }

    AfterAll {
        if ($FeatureEnabled) {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
            return
        }
    }

    It "Should not have `Enable-RunspaceDebug -Breakpoint` available" {
        { Enable-RunspaceDebug -Breakpoint } | Should -Throw -ErrorId NamedParameterNotFound
    }
}
