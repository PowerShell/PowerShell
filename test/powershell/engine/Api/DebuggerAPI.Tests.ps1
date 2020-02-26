# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Debugger APIs' -Tags 'CI' {
    Context 'Debugger API Tests' {
        BeforeAll {
            function Assert-RunspaceDebuggerIsNotStopped {
                [CmdletBinding()]
                param(
                    [Parameter(Mandatory)]
                    [runspace]
                    $Runspace
                )
                $Runspace.Debugger.InBreakpoint | Should -BeFalse
            }

            function Test-ForException {
                [CmdletBinding()]
                param(
                    [Parameter(Mandatory)]
                    [scriptblock]
                    $ScriptBlock,

                    [Parameter(Mandatory)]
                    [System.Type]
                    $ExceptionType
                )
                $errorRecord = $ScriptBlock | Should -Throw -ErrorId $ExceptionType.Name -PassThru
                $errorRecord.Exception.InnerException | Should -BeOfType $ExceptionType
            }

            $runspace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
            $runspace.Open()
        }

        AfterAll {
            if ($runspace -ne $null) {
                $runspace.Close()
                $runspace.Dispose()
            }
        }

        BeforeEach {
            $runspace.Debugger.SetDebugMode([System.Management.Automation.DebugModes]::LocalScript -bor [System.Management.Automation.DebugModes]::RemoteScript)
        }

        It 'Properly updates Debugger.DebugMode when invoking Debugger.SetDebugMode(<DebugMode>)' -TestCases @(
            @{
                DebugMode = [System.Management.Automation.DebugModes]::None
            }
            @{
                DebugMode = [System.Management.Automation.DebugModes]::Default
            }
            @{
                DebugMode = [System.Management.Automation.DebugModes]::LocalScript
            }
            @{
                DebugMode = [System.Management.Automation.DebugModes]::RemoteScript
            }
            @{
                DebugMode = [System.Management.Automation.DebugModes]::LocalScript -bor [System.Management.Automation.DebugModes]::RemoteScript
            }
        ) {
            param(
                $DebugMode
            )
            $runspace.Debugger.SetDebugMode($DebugMode)
            $runspace.Debugger.DebugMode | Should -Be $DebugMode
        }

        It 'Turns off the debugger when invoking Debugger.SetDebugMode(None)' {
            $runspace.Debugger.SetDebugMode([System.Management.Automation.DebugModes]::None)
            $runspace.Debugger.IsActive | Should -BeFalse
        }

        It 'Debugger.SetDebuggerAction throws a PSNotSupportedException when invoked while the debugger is inactive' {
            Test-ForException -ScriptBlock {
                $runspace.Debugger.SetDebuggerAction([System.Management.Automation.DebuggerResumeAction]::Continue)
            } -ExceptionType System.Management.Automation.PSNotSupportedException
        }

        It 'Debugger.GetDebuggerStopArgs returns null when the debugger is not stopped' {
            Assert-RunspaceDebuggerIsNotStopped -Runspace $runspace
            $runspace.Debugger.GetDebuggerStopArgs() | Should -Be $null
        }

        It 'Debugger.ProcessCommand throws a PSArgumentNullException when invoked with a null ''command'' argument' {
            Test-ForException -ScriptBlock {
                $runspace.Debugger.ProcessCommand($null, [System.Management.Automation.PSDataCollection[PSObject]]::new())
            } -ExceptionType System.Management.Automation.PSArgumentNullException
        }

        It 'Debugger.ProcessCommand throws a PSArgumentNullException when invoked with a null ''output'' argument' {
            Test-ForException -ScriptBlock {
                $runspace.Debugger.ProcessCommand([System.Management.Automation.PSCommand]::new(), $null)
            } -ExceptionType System.Management.Automation.PSArgumentNullException
        }

        It 'Debugger.ProcessCommand throws a PSInvalidOperationException when invoked while the debugger is not stopped' {
            Assert-RunspaceDebuggerIsNotStopped -Runspace $runspace
            Test-ForException -ScriptBlock {
                $runspace.Debugger.ProcessCommand([System.Management.Automation.PSCommand]::new(), [System.Management.Automation.PSDataCollection[PSObject]]::new())
            } -ExceptionType System.Management.Automation.PSInvalidOperationException
        }

        It 'Debugger.GetCallStack returns an empty stack from a runspace with nothing running' {
            @($runspace.Debugger.GetCallStack()) | Should -HaveCount 0
        }

        It 'Debugger.SetParent throws a PSNotImplementedException in a script debugger' {
            $runspace.Debugger.PSTypeNames | Should -Contain 'System.Management.Automation.ScriptDebugger'
            Test-ForException -ScriptBlock {
                $runspace.Debugger.SetParent($runspace.Debugger, $null, $null, $null, $null)
            } -ExceptionType System.Management.Automation.PSNotImplementedException
        }

        It 'Debugger.SetDebuggerStepMode should throw a PSInvalidOperationException when the debugger is disabled' {
            $runspace.Debugger.SetDebugMode([System.Management.Automation.DebugModes]::None)
            Test-ForException -ScriptBlock {
                $runspace.Debugger.SetDebuggerStepMode($true)
            } -ExceptionType System.Management.Automation.PSInvalidOperationException
        }

        It 'Debugger.SetDebuggerStepMode should make the debugger active when the debugger is enabled' {
            $runspace.Debugger.SetDebuggerStepMode($true)
            $runspace.Debugger.IsActive | Should -BeTrue
        }
    }

    Context 'Debugger Break Test' {
        BeforeAll {
            $runspace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
            $runspace.Open()
            $ps = [powershell]::Create($runspace)
            $ps.AddScript(@'
$maxValue = 5
$count = 0
while ($count -lt $maxValue) {
    ++$count
    'Output'
    sleep 1
}
'@)
            $debuggerStopHandler = {
                $global:DebuggerStopEventReceived = $true
            }
            $runspace.Debugger.add_DebuggerStop($debuggerStopHandler)
        }

        AfterAll {
            Remove-Variable -Name DebuggerStopEventReceived -Scope Global -ErrorAction Ignore
            $runspace.Debugger.remove_DebuggerStop($debuggerStopHandler)
            $ps.Dispose()
            $runspace.Dispose()
        }

        It 'Debugger.SetDebuggerStepMode should break into a running script' {
            $task = $ps.InvokeAsync()
            $global:DebuggerStopEventReceived = $false
            $runspace.Debugger.SetDebuggerStepMode($true)
            for ($i = 0; $i -lt 20; $i++) {
                if ($global:DebuggerStopEventReceived) {
                    break
                }
                Start-Sleep -Milliseconds 250
            }
            $runspace.Debugger.IsActive | Should -BeTrue
            $global:DebuggerStopEventReceived | Should -BeTrue
            $ps.StopAsync($null, $null)
        }
    }
}
