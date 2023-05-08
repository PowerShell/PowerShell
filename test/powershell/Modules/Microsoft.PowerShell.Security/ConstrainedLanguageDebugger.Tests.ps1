# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

##
## ----------
## Test Note:
## ----------
## Since these tests change session and system state (constrained language and system lockdown)
## they will all use try/finally blocks instead of Pester AfterEach/AfterAll to ensure session
## and system state is restored.
## Pester AfterEach, AfterAll is not reliable when the session is constrained language or locked down.
##

Import-Module HelpersSecurity

try
{
    $defaultParamValues = $PSDefaultParameterValues.Clone()
    $PSDefaultParameterValues["it:Skip"] = !$IsWindows

    Describe "Local script debugger is disabled in system lock down mode" -Tags 'CI','RequireAdminOnWindows' {

        BeforeAll {

            # Debugger test type definition
            $debuggerTestTypeDef = @'
            using System;
            using System.Management.Automation;
            using System.Management.Automation.Runspaces;

            namespace TestRunner
            {
                public class DebuggerTester
                {
                    private Runspace _runspace;

                    public int DebuggerStopHitCount
                    {
                        private set;
                        get;
                    }

                    public DebuggerTester(Runspace runspace)
                    {
                        if (runspace.Debugger == null)
                        {
                            throw new PSArgumentException("The provided runspace script debugger cannot be null for test.");
                        }

                        _runspace = runspace;
                        _runspace.Debugger.DebuggerStop += (sender, args) =>
                        {
                            DebuggerStopHitCount += 1;
                        };
                    }
                }
            }
'@

            $script = @'
            "Hello"
            Wait-Debugger
            "Goodbye"
'@
            $scriptFilePath = Join-Path $TestDrive TScript.ps1
            $script > $scriptFilePath

            # Define debugger test type
            Add-Type -TypeDefinition $debuggerTestTypeDef

            # Test cases
            $TestCasesDisableDebugger = @(
                @{
                    testName = 'Verifies that Set-PSBreakpoint Line is disabled on locked down system'
                    scriptText = 'Set-PSBreakpoint -Script {0} -Line 1' -f $scriptFilePath
                },
                @{
                    testName = 'Verifies that Set-PSBreakpoint Statement is disabled on locked down system'
                    scriptText = 'Set-PSBreakpoint -Script {0} -Line 1 -Column 1' -f $scriptFilePath
                },
                @{
                    testName = 'Verifies that Set-PSBreakpoint Command is disabled on locked down system'
                    scriptText = 'Set-PSBreakpoint -Command {0}' -f $scriptFilePath
                },
                @{
                    testName = 'Verifies that Set-PSBreakpoint Variable is disabled on locked down system'
                    scriptText = 'Set-PSBreakpoint -Variable HelloVar'
                }
            )
        }

        AfterAll {

            if (($script:moduleDirectory -ne $null) -and (Test-Path $script:moduleDirectory))
            {
                try { Remove-Item -Path $moduleDirectory -Recurse -Force -ErrorAction SilentlyContinue } catch { }
            }
        }

        It "<testName>" -TestCases $TestCasesDisableDebugger {

            param ($scriptText)

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                # Run script in new runspace created within lock down mode.
                [powershell] $ps = [powershell]::Create([System.Management.Automation.RunspaceMode]::NewRunspace);
                $ps.AddScript($scriptText).Invoke()
                $expectedError = $ps.Streams.Error[0]
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                if ($ps -ne $null) { $ps.Dispose() }
            }

            $expectedError.FullyQualifiedErrorId | Should -Be 'NotSupported,Microsoft.PowerShell.Commands.SetPSBreakpointCommand'
        }

        It "Verifies that Wait-Debugger is disabled on locked down system" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                # Create test runspace
                [runspace] $runspace = [runspacefactory]::CreateRunspace()
                $runspace.Open()

                # Attach TestRuner.DebuggerTester DebugStop event handler to runspace
                $debuggerTester = [TestRunner.DebuggerTester]::new($runspace)

                # Run $scriptFilePath script with 'Wait-Debugger' in locked down mode
                [powershell] $ps = [powershell]::Create()
                $ps.Runspace = $runspace
                $null = $ps.AddScript('"Hello"; Wait-Debugger; "Goodbye"').Invoke()
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                if ($runspace -ne $null) { $runspace.Dispose() }
                if ($ps -ne $null) { $ps.Dispose() }
            }

            # Debugger should not have been active in lockdown mode
            $debuggerTester.DebuggerStopHitCount | Should -Be 0
        }
    }
}
finally
{
    if ($null -ne $defaultParamValues)
    {
        $Global:PSDefaultParameterValues = $defaultParamValues
    }
}
