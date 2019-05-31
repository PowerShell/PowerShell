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

    Describe "Local script debugger is disabled in system lock down mode" -Tags 'CI','RequireAdminOnWindows' {

        BeforeAll {

            # Invoke-LanguageModeTestingSupportCmdlet definition
            $languageModeCmdletDef = @'
            using System;
            using System.Globalization;
            using System.Reflection;
            using System.Collections;
            using System.Collections.Generic;
            using System.IO;
            using System.Security;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Management.Automation;

            /// <summary>Adds a new type to the Application Domain</summary>
            [Cmdlet("Invoke", "LanguageModeTestingSupportCmdlet")]
            public sealed class InvokeLanguageModeTestingSupportCmdlet : PSCmdlet
            {
                [Parameter()]
                public SwitchParameter EnableFullLanguageMode
                {
                    get { return enableFullLanguageMode; }
                    set { enableFullLanguageMode = value; }
                }
                private SwitchParameter enableFullLanguageMode;

                [Parameter()]
                public SwitchParameter SetLockdownMode
                {
                    get { return setLockdownMode; }
                    set { setLockdownMode = value; }
                }
                private SwitchParameter setLockdownMode;

                [Parameter()]
                public SwitchParameter RevertLockdownMode
                {
                    get { return revertLockdownMode; }
                    set { revertLockdownMode = value; }
                }
                private SwitchParameter revertLockdownMode;

                protected override void BeginProcessing()
                {
                    if(enableFullLanguageMode)
                    {
                        SessionState.LanguageMode = PSLanguageMode.FullLanguage;
                    }

                    if(setLockdownMode)
                    {
                        Environment.SetEnvironmentVariable("__PSLockdownPolicy", "0x80000007", EnvironmentVariableTarget.Machine);
                    }

                    if(revertLockdownMode)
                    {
                        Environment.SetEnvironmentVariable("__PSLockdownPolicy", null, EnvironmentVariableTarget.Machine);
                    }
                }
            }
'@

            if (-not (Get-Command Invoke-LanguageModeTestingSupportCmdlet -ea Ignore))
            {
                $languageModeModuleName = "LanguageModeModule"
                $modulePath = [System.IO.Path]::GetFileNameWithoutExtension([IO.Path]::GetRandomFileName())
                $script:moduleDirectory = join-path "$PSScriptRoot\$modulePath" $languageModeModuleName
                if (-not (Test-Path $moduleDirectory))
                {
                    $null = New-Item -ItemType Directory $moduleDirectory -Force
                }

                try
                {
                    Add-Type -TypeDefinition $languageModeCmdletDef -OutputAssembly $moduleDirectory\TestCmdletForConstrainedLanguage.dll -ErrorAction Ignore
                } catch {}

                Import-Module -Name $moduleDirectory\TestCmdletForConstrainedLanguage.dll
            }

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
        }

        AfterAll {

            if (($script:moduleDirectory -ne $null) -and (Test-Path $script:moduleDirectory))
            {
                try { Remove-Item -Path $moduleDirectory -Recurse -Force -ErrorAction SilentlyContinue } catch { }
            }
        }

        It "Verifies that Set-PSBreakpoint Line is disabled on locked down system" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                Set-PSBreakpoint -Script $scriptFilePath -Line 1
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should Be 'NotSupported,Microsoft.PowerShell.Commands.SetPSBreakpointCommand'
        }

        It "Verifies that Set-PSBreakpoint Statement is disabled on locked down system" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                Set-PSBreakpoint -Script $scriptFilePath -Line 1 -Column 1
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should Be 'NotSupported,Microsoft.PowerShell.Commands.SetPSBreakpointCommand'
        }

        It "Verifies that Set-PSBreakpoint Command is disabled on locked down system" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                Set-PSBreakpoint -Command $scriptFilePath
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should Be 'NotSupported,Microsoft.PowerShell.Commands.SetPSBreakpointCommand'
        }

        It "Verifies that Set-PSBreakpoint Variable is disabled on locked down system" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                Set-PSBreakpoint -Variable HelloVar
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should Be 'NotSupported,Microsoft.PowerShell.Commands.SetPSBreakpointCommand'
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
            $debuggerTester.DebuggerStopHitCount | Should Be 0
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
