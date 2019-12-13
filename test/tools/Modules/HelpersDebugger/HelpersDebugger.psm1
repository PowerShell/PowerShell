# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

# Ensure that terminating errors terminate when importing the module.
trap { throw $_ }

# Strict mode FTW.
Set-StrictMode -Version Latest

# Enable explicit export so that there are no surprises with commands exported from the module.
Export-ModuleMember

# Grab the internal ScriptPosition property once and re-use it in the ps1xml file
$internalExtentProperty = [System.Management.Automation.InvocationInfo].GetProperty('ScriptPosition', [System.Reflection.BindingFlags]'NonPublic,Instance')

# A dictionary to track runspaces whose debugger has been registered,
# along with their debug command queue and results collection
$runspacesRegistered = @{}

# A debugger handler that can be used to run debugger commands
$debuggerStopHandler = {
    [CmdletBinding()]
    [System.Diagnostics.DebuggerHidden()]
    param($s, $e)
    # Manually reference the runspacesRegistered from the module (this is
    # because we won't have access to script scope when invoking script files)
    $rsData = & (Get-Module -Name HelpersDebugger) {$runspacesRegistered}
    # Lookup the runspace associated with the current debugger to make sure we
    # have collections we can work with
    $runspaceFound = $false
    foreach ($runspace in $rsData.Keys) {
        if ($runspace.Debugger -eq $s) {
            $runspaceFound = $true
            break
        }
    }
    # If no runspace was found, or if we don't have collections to work with
    # in this event handler, then we're not handling a debugger stop event
    # during the execution of Test-Debugger, and we can continue execution
    if (-not $runspaceFound -or $rsData[$runspace] -eq $null) {
        $e.ResumeAction = [System.Management.Automation.DebuggerResumeAction]::Continue
        return
    }
    do {
        if ($rsData[$runspace].DbgCmdQueue.Count -eq 0) {
            # If there are no more commands to process, continue execution
            $stringDbgCommand = 'c'
        } else {
            $stringDbgCommand = $rsData[$runspace].DbgCmdQueue.Dequeue()
        }
        $dbgCmd = [System.Management.Automation.PSCommand]::new()
        $dbgCmd.AddScript($stringDbgCommand) > $null
        $output = [System.Management.Automation.PSDataCollection[PSObject]]::new()
        $result = $Host.Runspace.Debugger.ProcessCommand($dbgCmd, $output)
        if ($stringDbgCommand -eq '$?' -and $output.Count -eq 1) {
            $output[0] = $PSDebugContext.Trigger -isnot [System.Management.Automation.ErrorRecord]
        }
        $rsData[$runspace].DbgResults.Add([pscustomobject]@{
                PSTypeName          = 'DebuggerCommandResult'
                Command             = $stringDbgCommand
                Context             = $PSDebugContext
                Output              = $output
                EvaluatedByDebugger = $result.EvaluatedByDebugger
                ResumeAction        = $result.ResumeAction
            })
    } while ($result -eq $null -or $result.ResumeAction -eq $null)
    $e.ResumeAction = $result.ResumeAction
}

# A debugger handler that is invoked when a breakpoint is updated
$breakpointUpdatedHandler = {
    [CmdletBinding()]
    [System.Diagnostics.DebuggerHidden()]
    param($s, $e)
    # Manually reference the runspacesRegistered from the module (this is
    # because we won't have access to script scope when invoking script files)
    $rsData = & (Get-Module -Name HelpersDebugger) {$runspacesRegistered}
    # Lookup the runspace associated with the current debugger to make sure we
    # have collections we can work with
    $runspaceFound = $false
    foreach ($runspace in $rsData.Keys) {
        if ($runspace.Debugger -eq $s) {
            $runspaceFound = $true
            break
        }
    }
    # If no runspace was found, or if we don't have collections to work with
    # in this event handler, then we're not handling a breakpoint updated event
    # during the execution of Test-Debugger, so there is nothing to do
    if (-not $runspaceFound -or $rsData[$runspace] -eq $null) {
        return
    }
    # Capture the breakpoint update details so that we can verify them later
    $rsData[$runspace].BpsUpdated.Add([pscustomobject]@{
        PSTypeName          = 'UpdatedBreakpoint'
        Breakpoint          = $e.Breakpoint
        BreakpointCount     = $e.BreakpointCount
        UpdateType          = $e.UpdateType
    })
}

function Register-DebuggerHandler {
    [CmdletBinding()]
    [OutputType([System.Void])]
    param(
        [ValidateNotNull()]
        [runspace]
        $Runspace = $host.Runspace
    )
    try {
        $callerEAP = $ErrorActionPreference
        if (-not $script:runspacesRegistered.ContainsKey($Runspace)) {
            $Runspace.Debugger.add_DebuggerStop($script:debuggerStopHandler)
            $Runspace.Debugger.add_BreakpointUpdated($script:breakpointUpdatedHandler)
            $script:runspacesRegistered.Add($Runspace, $null)
            # Disable debugger interactivity so that all debugger events go
            # through the DebuggerStop event only (i.e. breakpoints don't
            # actually generate a prompt for user interaction)
            $host.DebuggerEnabled = $false
        }
    } catch {
        Write-Error -ErrorRecord $_ -ErrorAction $callerEAP
    }
}
Export-ModuleMember -Function Register-DebuggerHandler

function Unregister-DebuggerHandler {
    [CmdletBinding()]
    [OutputType([System.Void])]
    param(
        [ValidateNotNull()]
        [runspace]
        $Runspace = $host.Runspace
    )
    try {
        $callerEAP = $ErrorActionPreference
        if ($script:runspacesRegistered.ContainsKey($Runspace)) {
            $Runspace.Debugger.remove_DebuggerStop($script:debuggerStopHandler)
            $Runspace.Debugger.remove_BreakpointUpdated($script:breakpointUpdatedHandler)
            $script:runspacesRegistered.Remove($Runspace)
            # Re-enable debugger interactivity so that breakpoints generate a
            # prompt for user interaction
            if ($script:runspacesRegistered.Count -eq 0) {
                $host.DebuggerEnabled = $true
            }
        }
    } catch {
        Write-Error -ErrorRecord $_ -ErrorAction $callerEAP
    }
}
Export-ModuleMember -Function Unregister-DebuggerHandler

function Test-Debugger {
    [CmdletBinding(DefaultParameterSetName = 'DebuggerCommands')]
    [OutputType('DebuggerCommandResult', ParameterSetName = 'DebuggerCommands')]
    [OutputType('UpdatedBreakpoint', ParameterSetName = 'BreakpointUpdates')]
    param(
        [Parameter(Position = 0, Mandatory)]
        [ValidateNotNullOrEmpty()]
        [Alias('sb')]
        [scriptblock]
        $ScriptBlock,

        [Parameter(ParameterSetName = 'DebuggerCommands')]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $CommandQueue,

        [Parameter(ParameterSetName = 'BreakpointUpdates')]
        [ValidateRange(1, [int]::MaxValue)]
        [int]
        $BreakpointUpdates,

        [ValidateNotNull()]
        [runspace]
        $Runspace = $host.Runspace
    )
    try {
        $callerEAP = $ErrorActionPreference
        # If the debugger is not set up properly, notify the user with an
        # error message
        if (-not $script:runspacesRegistered.ContainsKey($Runspace) -or $host.DebuggerEnabled) {
            $message = 'You must invoke Register-DebuggerHandler before invoking Test-Debugger, and Unregister-DebuggerHandler after invoking Test-Debugger. As a best practice, invoke Register-DebuggerHandler in the BeforeAll block and Unregister-DebuggerHandler in the AfterAll block of your test script.'
            $exception = [System.InvalidOperationException]::new($message)
            $errorRecord = [System.Management.Automation.ErrorRecord]::new($exception, $exception.GetType().Name, 'InvalidOperation', $null)
            throw $errorRecord
        }
        # If the runspace debugger is already running a test, notify the user
        # with an error message
        if ($script:runspacesRegistered[$Runspace] -ne $null) {
            $message = "You can only run one debugger command test per runspace at a time. Runspace '$($Runspace.InstanceId)' is currently busy running another debugger command test."
            $exception = [System.InvalidOperationException]::new($message)
            $errorRecord = [System.Management.Automation.ErrorRecord]::new($exception, $exception.GetType().Name, 'InvalidOperation', $null)
            throw $errorRecord
        }
        # Setup collections for debug commands and for the results of those
        # debug commands
        $dbgCmdQueue = [System.Collections.Queue]::new()
        foreach ($command in $CommandQueue) {
            $dbgCmdQueue.Enqueue($command)
        }
        $script:runspacesRegistered[$Runspace] = [ordered]@{
            DbgCmdQueue = $dbgCmdQueue
            DbgResults  = [System.Collections.ArrayList]::new()
            BpsUpdated  = [System.Collections.ArrayList]::new()
        }
        # We re-create the script block before invoking it to ensure that it will
        # work regardless of where the script itself was defined in the test file.
        # We also discard any standard output because this invocation is about the
        # debugger output, not the output of the script itself.
        & {
            [System.Diagnostics.DebuggerStepThrough()]
            [CmdletBinding()]
            param()
            try {
                $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
                if ($Runspace -eq $host.Runspace) {
                    [scriptblock]::Create($ScriptBlock).Invoke() > $null
                } else {
                    [powershell]::Create($Runspace).AddScript($ScriptBlock).Invoke() > $null
                }
            } catch {
                Write-Error -ErrorRecord $_ -ErrorAction Stop
            }
        }
        switch ($PSCmdlet.ParameterSetName) {
            'DebuggerCommands' {
                # Return the results of the debugger commands that were processed
                $script:runspacesRegistered[$Runspace].DbgResults
                break
            }

            'BreakpointUpdates' {
                # Wait a short time for breakpoint updates to complete
                $now = [DateTime]::UtcNow
                while ($script:runspacesRegistered[$Runspace].BpsUpdated.Count -lt $BreakpointUpdates) {
                    Start-Sleep -Milliseconds 100
                    if ([DateTime]::UtcNow.Subtract($now).TotalMilliseconds -gt 1000) {
                        Write-Warning -Message "Only received $($script:runspacesRegistered[$Runspace].BpsUpdated.Count) of ${BreakpointUpdates} breakpoint update records before timeout."
                        break
                    }
                }
                # Return the breakpoint update records that have been received
                $script:runspacesRegistered[$Runspace].BpsUpdated
                break
            }
        }
    } catch {
        Write-Error -ErrorRecord $_ -ErrorAction $callerEAP
    } finally {
        $script:runspacesRegistered[$Runspace] = $null
    }
}
Export-ModuleMember -Function Test-Debugger

function Get-DebuggerExtent {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0, Mandatory, ValueFromPipeline)]
        [ValidateNotNull()]
        [PSTypeName('DebuggerCommandResult')]
        $DebuggerCommandResult
    )
    process {
        try {
            $callerEAP = $ErrorActionPreference
            $script:internalExtentProperty.GetValue($DebuggerCommandResult.Context.InvocationInfo)
        } catch {
            Write-Error -ErrorRecord $_ -ErrorAction $callerEAP
        }
    }
}

function ShouldHaveExtent {
    [CmdletBinding(DefaultParameterSetName = 'SingleLineExtent')]
    param(
        [Parameter(Position = 0, Mandatory, ValueFromPipeline)]
        [ValidateNotNull()]
        [PSTypeName('DebuggerCommandResult')]
        $DebuggerCommandResult,

        [Parameter(Mandatory, ParameterSetName = 'SingleLineExtent')]
        [ValidateRange(1, [int]::MaxValue)]
        [int]
        $Line,

        [Parameter(Mandatory, ParameterSetName = 'MultilineExtent')]
        [ValidateRange(1, [int]::MaxValue)]
        [int]
        $FromLine,

        [Parameter(Mandatory)]
        [ValidateRange(1, [int]::MaxValue)]
        [int]
        $FromColumn,

        [Parameter(Mandatory, ParameterSetName = 'MultilineExtent')]
        [ValidateRange(1, [int]::MaxValue)]
        [int]
        $ToLine,

        [Parameter(Mandatory)]
        [ValidateRange(1, [int]::MaxValue)]
        [int]
        $ToColumn
    )
    process {
        $extent = Get-DebuggerExtent -DebuggerCommandResult $DebuggerCommandResult
        $extent.StartLineNumber | Should -Be $(if ($PSCmdlet.ParameterSetName -eq 'SingleLineExtent') { $Line } else { $FromLine })
        $extent.StartColumnNumber | Should -Be $FromColumn
        $extent.EndLineNumber | Should -Be $(if ($PSCmdlet.ParameterSetName -eq 'SingleLineExtent') { $Line } else { $ToLine })
        $extent.EndColumnNumber | Should -Be $ToColumn
    }
}
Export-ModuleMember -Function ShouldHaveExtent

function ShouldHaveSameExtentAs {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0, Mandatory, ValueFromPipeline)]
        [ValidateNotNull()]
        [PSTypeName('DebuggerCommandResult')]
        $SourceDebuggerCommandResult,

        [Parameter(Position = 1, Mandatory)]
        [ValidateNotNull()]
        [Alias('DebuggerCommandResult')]
        [PSTypeName('DebuggerCommandResult')]
        $TargetDebuggerCommandResult
    )
    begin {
        $targetExtent = Get-DebuggerExtent -DebuggerCommandResult $TargetDebuggerCommandResult
    }
    process {
        $sourceExtent = Get-DebuggerExtent -DebuggerCommandResult $SourceDebuggerCommandResult
        $sourceExtent | Should -Be $targetExtent
    }
}
Export-ModuleMember -Function ShouldHaveSameExtentAs
