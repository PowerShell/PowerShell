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
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Import-Module -Name {0} -Force
                Set-PSBreakpoint -Command PublicFn
                PublicFn
'@ -f $trustedManifestFilePath

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

    Describe "Cross language debugger get-item commands should not have access to FullLanguage trusted functions through provider" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {

            # Trusted module that will always run in FullLanguage mode
            $scriptModuleName = "ImportTrustedModuleForTestA_System32"
            $moduleFilePath = Join-Path $TestDrive ($scriptModuleName + ".psm1")
            $script = @'
                function PublicFn { "PublicFn"; PrivateFn }
                function PrivateFn { "PrivateFn" }

                Export-ModuleMember -Function PublicFn
'@
            $script > $moduleFilePath

            # Import and run module function script
            $scriptIM = @'
                Import-Module -Name {0} -Force
                $null = Set-PSBreakpoint -command PublicFn
                PublicFn
'@ -f $moduleFilePath

            # Debugger stop event handler object.
            $type = @'
                using System;
                using System.Collections.Generic;
                using System.Collections.ObjectModel;
                using System.Management.Automation;
                using System.Management.Automation.Runspaces;

                public class DebuggerStopEventHandler
                {
                    private Runspace _runspace;
                    public object GetItemResult
                    {
                        get;
                        internal set;
                    }
                    public object GetChildItemResult
                    {
                        get;
                        internal set;
                    }
                    public object CopyItemResult
                    {
                        get;
                        internal set;
                    }
                    public object FunctionVariableResult
                    {
                        get;
                        internal set;
                    }
                    public object RenameItemResult
                    {
                        get;
                        internal set;
                    }
                    public DebuggerStopEventHandler(Runspace runspace)
                    {
                        _runspace = runspace;
                        _runspace.Debugger.DebuggerStop += (sender, args) =>
                        {
                            var debugger = sender as Debugger;

                            PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();
                            PSCommand command = new PSCommand();

                            command.AddScript(@"Get-Item -Path function:\PrivateFn 2>&1");
                            debugger.ProcessCommand(command, output);
                            GetItemResult = (output.Count > 0) ? (output[0].BaseObject) : null;

                            command.Clear();
                            output.Clear();
                            command.AddScript(@"Get-ChildItem -Path function:\PrivateFn 2>&1");
                            debugger.ProcessCommand(command, output);
                            GetChildItemResult = (output.Count > 0) ? (output[0].BaseObject) : null;

                            command.Clear();
                            output.Clear();
                            command.AddScript(@"Copy-Item -Path function:\PrivateFn -Destination function:\MyPrivateFn 2>&1");
                            debugger.ProcessCommand(command, output);
                            CopyItemResult = (output.Count > 0) ? (output[0].BaseObject) : null;

                            command.Clear();
                            output.Clear();
                            command.AddScript(@"${function:\PrivateFn}");
                            debugger.ProcessCommand(command, output);
                            FunctionVariableResult = (output.Count > 0) ? (output[0].BaseObject): null;

                            command.Clear();
                            output.Clear();
                            command.AddScript(@"Rename-Item -Path function:\PrivateFn -NewName function:\MyPrivateFn -Passthru 2>&1");
                            debugger.ProcessCommand(command, output);
                            RenameItemResult = (output.Count > 0) ? (output[0].BaseObject) : null;
                        };
                    }
                    public void Reset() { GetItemResult = null; GetChildItemResult = null; CopyItemResult = null; }
                }
'@

            try { Add-Type -TypeDefinition $type } catch { }

            # Create runspace and debugger event handler
            [runspace] $rs = [runspacefactory]::CreateRunspace($host)
            $rs.Open()
            $rs.Debugger.SetDebugMode(@('LocalScript','RemoteScript'))
            $debuggerStopHandler = [DebuggerStopEventHandler]::New($rs)

            # Create PowerShell to run module script
            [powershell] $ps = [powershell]::Create()
            $ps.Runspace = $rs
            $ps.AddScript($scriptIM)
        }

        AfterAll {

            if ($rs -ne $null) { $rs.Dispose() }
            if ($ps -ne $null) { $ps.Dispose() }
        }

        It "Verifies that same language mode trusted public functions *are* accessible from debugger through Get-Item, Get-ChildItem, Copy-Item, Rename-Item, Variable" {

            # Test
            $results = $ps.Invoke()

            # Results.  Only PublicFn is returned since PrivateFn is renamed.
            $results[0] | Should Be "PublicFn"

            # Expected Get-Item function:\PrivateFn returns FunctionInfo object
            ($debuggerStopHandler.GetItemResult -is [System.Management.Automation.FunctionInfo]) | Should Be $true

            # Expected Get-ChildItem function:\PrivateFn returns FunctionInfo object
            ($debuggerStopHandler.GetChildItemResult -is [System.Management.Automation.FunctionInfo])  | Should Be $true

            # Expected Copy-Item function:\PrivateFn succeeds with no error output
            $debuggerStopHandler.CopyItemResult | Should Be $null

            # Expected function variable succeeds
            ($debuggerStopHandler.FunctionVariableResult -is [scriptblock]) | Should Be $true

            # Expected Rename-Item function:\PrivateFn returns FunctionInfo object
            ($debuggerStopHandler.RenameItemResult -is [System.Management.Automation.FunctionInfo]) | Should Be $true
        }

        It "Verifies that cross language mode trusted public functions *are not* accessible through Get-Item, Get-ChildItem, Copy-Item, Rename-Item, Variable" {

            # Test
            $debuggerStopHandler.Reset()
            try
            {
                $rs.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                $results = $ps.Invoke()
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            # Results
            $results[0] | Should Be "PublicFn"
            $results[1] | Should Be "PrivateFn"

            # Expected Get-Item function:\PrivateFn returns error
            $debuggerStopHandler.GetItemResult.FullyQualifiedErrorId | Should Be "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand"

            # Expected Get-ChildItem function:\PrivateFn returns error
            $debuggerStopHandler.GetChildItemResult.FullyQualifiedErrorId | Should Be "PathNotFound,Microsoft.PowerShell.Commands.GetChildItemCommand"

            # Expected Copy-Item fails with error output
            $debuggerStopHandler.CopyItemResult.FullyQualifiedErrorId | Should Be "PathNotFound,Microsoft.PowerShell.Commands.CopyItemCommand"

            # Expected function variable fails
            $debuggerStopHandler.FunctionVariableResult | Should Be $null

            # Expected Rename-Item function:\PrivateFn fails with error
            $debuggerStopHandler.RenameItemResult.FullyQualifiedErrorId | Should Be "PathNotFound,Microsoft.PowerShell.Commands.RenameItemCommand"
        }
    }

    Describe "Cross language debugger Action scripts should not have access to FullLanguage trusted functions through provider" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {

            # Trusted script that will always run in FullLanguage mode
            $scriptFileName = "TrustedScriptForTestB_System32"
            $scriptFilePath = Join-Path $TestDrive ($scriptFileName + ".ps1")
            $script = @'
                function PublicFn { PrivateFn -typeDef 'public class Hello { public new static void ToString() { System.Console.WriteLine("Hello!"); } }'; [Hello]::ToString(); }
                function PrivateFn { param ([string]$typeDef) Add-Type -TypeDefinition $typeDef }
                PublicFn
                "Complete"
'@
            $script > $scriptFilePath
        }

        AfterAll {

            Get-PSBreakpoint | Remove-PSBreakpoint
        }

        It "Verifies that debugger stop Action scriptblock cannot access PrivateFn" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                # Set breakpoint on script
                Set-PSBreakpoint -Script $scriptFilePath -Line 4 -Action {
                    & (Get-Item -Path function:\PrivateFn) -typeDef @'
                    public class Foo {
                        public new static void ToString() {
                            System.Console.WriteLine("pwnd!");
                        }
                    }
'@
                }

                # Run script
                & $scriptFilePath
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            try
            {
                # Verify that Action scriptblock did not create Foo type using PrivateFn
                [Foo]::ToString()
                throw "No Exception!"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be "TypeNotFound"
            }
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
