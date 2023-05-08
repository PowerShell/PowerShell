# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Ensure that terminating errors terminate when importing the module.
trap {throw $_}

# Strict mode FTW.
Set-StrictMode -Version 3.0

# Enable explicit export so that there are no surprises with commands exported from the module.
Export-ModuleMember

# Grab the internal ScriptPosition property once and re-use it in the ps1xml file
$internalExtentProperty = [System.Management.Automation.InvocationInfo].GetProperty('ScriptPosition', [System.Reflection.BindingFlags]'NonPublic,Instance')

# A debugger handler that can be used to automatically control the debugger
$debuggerStopHandler = {
    param($s, $e)
    # If we're not handling a debugger stop event during the execution of
    # Test-Debugger, then simply continue execution
    if (@(Get-Variable -Name dbgCmdQueue,dbgResults -Scope Script -ErrorAction Ignore).Count -ne 2) {
        $e.ResumeAction = [System.Management.Automation.DebuggerResumeAction]::Continue
        return
    }
    do {
        if ($script:dbgCmdQueue.Count -eq 0) {
            # If there are no more commands to process, continue execution
            $stringDbgCommand = 'c'
        } else {
            $stringDbgCommand = $script:dbgCmdQueue.Dequeue()
        }
        $dbgCmd = [System.Management.Automation.PSCommand]::new()
        $dbgCmd.AddScript($stringDbgCommand) > $null
        $output = [System.Management.Automation.PSDataCollection[PSObject]]::new()
        $result = $Host.Runspace.Debugger.ProcessCommand($dbgCmd, $output)
        if ($stringDbgCommand -eq '$?' -and $output.Count -eq 1) {
            $output[0] = $PSDebugContext.Trigger -isnot [System.Management.Automation.ErrorRecord]
        }
        $script:dbgResults += [pscustomobject]@{
            PSTypeName          = 'DebuggerCommandResult'
            Command             = $stringDbgCommand
            Context             = $PSDebugContext
            Output              = $output
            EvaluatedByDebugger = $result.EvaluatedByDebugger
            ResumeAction        = $result.ResumeAction
        }
    } while ($result -eq $null -or $result.ResumeAction -eq $null)
    $e.ResumeAction = $result.ResumeAction
}

# A flag to identify if the debugger handler has been added or not
$debuggerStopHandlerRegistered = $false

function Register-DebuggerHandler {
    [CmdletBinding()]
    [OutputType([System.Void])]
    param()
    try {
        $callerEAP = $ErrorActionPreference
        # We disable debugger interactivity so that all debugger events go through
        # the DebuggerStop event only (i.e. breakpoints don't actually generate a
        # prompt for user interaction)
        $Host.DebuggerEnabled = $false
        $Host.Runspace.Debugger.add_DebuggerStop($script:debuggerStopHandler)
        $script:debuggerStopHandlerRegistered = $true
    } catch {
        Write-Error -ErrorRecord $_ -ErrorAction $callerEAP
    }
}
Export-ModuleMember -Function Register-DebuggerHandler

function Unregister-DebuggerHandler {
    [CmdletBinding()]
    [OutputType([System.Void])]
    param()
    try {
        $callerEAP = $ErrorActionPreference
        $Host.Runspace.Debugger.remove_DebuggerStop($script:debuggerStopHandler)
        $Host.DebuggerEnabled = $true
        $script:debuggerStopHandlerRegistered = $false
    } catch {
        Write-Error -ErrorRecord $_ -ErrorAction $callerEAP
    }
}
Export-ModuleMember -Function Unregister-DebuggerHandler

function Test-Debugger {
    [CmdletBinding()]
    [OutputType('DebuggerCommandResult')]
    param(
        [Parameter(Position=0, Mandatory)]
        [ValidateNotNullOrEmpty()]
        [Alias('sb')]
        [ScriptBlock]
        $ScriptBlock,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $CommandQueue
    )
    try {
        $callerEAP = $ErrorActionPreference
        #  If the debugger is not set up properly, notify the user with an error message
        if (-not $script:debuggerStopHandlerRegistered -or $Host.DebuggerEnabled) {
            $message = 'You must invoke Register-DebuggerHandler before invoking Test-Debugger, and Unregister-DebuggerHandler after invoking Test-Debugger. As a best practice, invoke Register-DebuggerHandler in the BeforeAll block and Unregister-DebuggerHandler in the AfterAll block of your test script.'
            $exception = [System.InvalidOperationException]::new($message)
            $errorRecord = [System.Management.Automation.ErrorRecord]::new($exception, $exception.GetType().Name, 'InvalidOperation', $null)
            throw $errorRecord
        }
        $script:dbgResults = @()
        $script:dbgCmdQueue = [System.Collections.Queue]::new()
        foreach ($command in $CommandQueue) {
            $script:dbgCmdQueue.Enqueue($command)
        }
        # We re-create the script block before invoking it to ensure that it will
        # work regardless of where the script itself was defined in the test file.
        # We also silence any standard output because this invocation is about the
        # debugger output, not the output of the script itself.
        & {
            [System.Diagnostics.DebuggerStepThrough()]
            [CmdletBinding()]
            param()
            try {
                $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
                [ScriptBlock]::Create($ScriptBlock).Invoke() > $null
            } catch {
                Write-Error -ErrorRecord $_ -ErrorAction Stop
            }
        }
        $script:dbgResults
    } catch {
        Write-Error -ErrorRecord $_ -ErrorAction $callerEAP
    } finally {
        Remove-Variable -Name dbgResults -Scope Script -ErrorAction Ignore
        Remove-Variable -Name dbgCmdQueue -Scope Script -ErrorAction Ignore
    }
}
Export-ModuleMember -Function Test-Debugger

function Get-DebuggerExtent {
    [CmdletBinding()]
    param(
        [Parameter(Position=0, Mandatory, ValueFromPipeline)]
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
    [CmdletBinding(DefaultParameterSetName='SingleLineExtent')]
    param(
        [Parameter(Position=0, Mandatory, ValueFromPipeline)]
        [ValidateNotNull()]
        [PSTypeName('DebuggerCommandResult')]
        $DebuggerCommandResult,

        [Parameter(Mandatory, ParameterSetName='SingleLineExtent')]
        [ValidateRange(1, [int]::MaxValue)]
        [int]
        $Line,

        [Parameter(Mandatory, ParameterSetName='MultilineExtent')]
        [ValidateRange(1, [int]::MaxValue)]
        [int]
        $FromLine,

        [Parameter(Mandatory)]
        [ValidateRange(1, [int]::MaxValue)]
        [int]
        $FromColumn,

        [Parameter(Mandatory, ParameterSetName='MultilineExtent')]
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
        $extent.StartLineNumber | Should -Be $(if ($PSCmdlet.ParameterSetName -eq 'SingleLineExtent') {$Line} else {$FromLine})
        $extent.StartColumnNumber | Should -Be $FromColumn
        $extent.EndLineNumber | Should -Be $(if ($PSCmdlet.ParameterSetName -eq 'SingleLineExtent') {$Line} else {$ToLine})
        $extent.EndColumnNumber | Should -Be $ToColumn
    }
}
Export-ModuleMember -Function ShouldHaveExtent

function ShouldHaveSameExtentAs {
    [CmdletBinding()]
    param(
        [Parameter(Position=0, Mandatory, ValueFromPipeline)]
        [ValidateNotNull()]
        [PSTypeName('DebuggerCommandResult')]
        $SourceDebuggerCommandResult,

        [Parameter(Position=1, Mandatory)]
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
