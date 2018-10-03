# Copyright (c) Microsoft Corporation. All rights reserved.
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

    Describe "Trusted module on locked down machine should not expose private functions to script debugger command processing" -Tags 'CI','RequireAdminOnWindows' {

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
                    private readonly string _privateFnName;

                    [Flags]
                    public enum TestResults
                    {
                        NoResult = 0x0,
                        DebuggerStopHandled = 0x1,
                        PrivateFnFound = 0x2
                    };

                    public TestResults TestResult
                    {
                        private set;
                        get;
                    }

                    public Exception ScriptException
                    {
                        private set;
                        get;
                    }

                    public DebuggerTester(Runspace runspace, string privateFnName)
                    {
                        if (runspace.Debugger == null)
                        {
                            throw new PSArgumentException("The provided runspace script debugger cannot be null for test.");
                        }

                        _runspace = runspace;
                        _privateFnName = privateFnName;
                        _runspace.Debugger.DebuggerStop += (sender, args) =>
                        {
                            try
                            {
                                // Within the debugger stop handler, make sure trusted private functions are not accessible.
                                string commandText = string.Format(@"Get-Command ""{0}""", _privateFnName);
                                PSCommand command = new PSCommand();
                                command.AddCommand(new Command(commandText, true));
                                PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();

                                _runspace.Debugger.ProcessCommand(command, output);
                                if ((output.Count > 0) && (output[0].BaseObject is CommandInfo))
                                {
                                    TestResult |= TestResults.PrivateFnFound;
                                }
                            }
                            catch (Exception e)
                            {
                                ScriptException = e;
                                System.Console.WriteLine(e.Message);
                            }
                            TestResult |= TestResults.DebuggerStopHandled;
                        };
                    }
                }
            }
'@

            $modulePath = Join-Path $TestDrive Modules
            if (Test-Path -Path $modulePath)
            {
                try { Remove-Item -Path $modulePath -Recurse -Force -ErrorAction SilentlyContinue } catch { }
            }

            # Trusted module
            $trustedModuleName = "TrustedModule_System32"
            $trustedModuleDirectory = Join-Path $modulePath $trustedModuleName
            New-Item -ItemType Directory -Path $trustedModuleDirectory -Force -ErrorAction SilentlyContinue
            $trustedModuleFilePath = Join-Path $trustedModuleDirectory "$($trustedModuleName).psm1"
            $trustedManifestFilePath = Join-Path $trustedModuleDirectory "$($trustedModuleName).psd1"
            @'
            function PublicFn {
                Write-Output PrivateFn "PublicFn"
            }

            function PrivateFn {
                param ([string] $msg)

                Write-Output $msg
            }
'@ > $trustedModuleFilePath
            $modManifest = "@{ ModuleVersion = '1.0'" + ("; RootModule = '{0}'" -f $trustedModuleFilePath) + "; FunctionsToExport = 'PublicFn' }"
            $modManifest > $trustedManifestFilePath

            # Create test runspace
            [runspace] $runspace = [runspacefactory]::CreateRunspace()
            $runspace.Open()

            # Create debugger test object
            Add-Type -TypeDefinition $debuggerTestTypeDef
        }

        AfterAll {

            if ($runspace -ne $null) { $runspace.Dispose() }
        }

        It "Verifies that private trusted module function is not available in script debugger" {

            # Run debugger access test
            $debuggerTester = [TestRunner.DebuggerTester]::new($runspace, "PrivateFn")

            # Script to invoke the script debugger so that $debuggerTester can handle
            # the debugger stop event and test for access of private functions within the
            # script debugger command processor.
            $script = @'
                Import-Module -Name HelpersSecurity
                Import-Module -Name {0} -Force
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Import-Module -Name {1} -Force
                Set-PSBreakpoint -Command PublicFn
                PublicFn
'@ -f "$languageModuleDirectory\TestCmdletForConstrainedLanguage.dll", $trustedManifestFilePath

            [powershell] $ps = [powershell]::Create()
            $ps.Runspace = $runspace

            try
            {
                $ps.AddScript($script).BeginInvoke()
        
                # Wait for debugger test result for up to ten seconds
                $count = 0
                while (($debuggerTester.TestResult -eq 0) -and ($count++ -lt 40))
                {
                    Start-Sleep -Milliseconds 250
                }
            }
            finally
            {
                # Revert lockdown
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            # Verify that PrivateFn function name is not accessible
            $debuggerTester.TestResult | Should -Match "DebuggerStopHandled"
            $debuggerTester.TestResult | Should -Not -Match "PrivateFnFound"
            $debuggerTester.ScriptException | Should -BeNullOrEmpty
        }
    }
}
finally
{
    if ($defaultParamValues -ne $null)
    {
        $Global:PSDefaultParameterValues = $defaultParamValues
    }
}
