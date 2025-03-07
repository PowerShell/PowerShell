# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'PipelineStopToken tests' -Tags 'CI' {

    BeforeAll {
        Function Invoke-WithStop {
            [CmdletBinding()]
            param (
                [Parameter(Mandatory)]
                [ScriptBlock]
                $ScriptBlock,

                [Parameter(ValueFromRemainingArguments)]
                [object[]]
                $ArgumentList
            )

            $ps = [PowerShell]::Create()
            $null = $ps.AddScript("'start'`n" + $ScriptBlock.ToString())
            foreach ($arg in $ArgumentList) {
                $null = $ps.AddArgument($arg)
            }

            $inPipe = [System.Management.Automation.PSDataCollection[object]]::new()
            $inPipe.Complete()
            $outPipe = [System.Management.Automation.PSDataCollection[object]]::new()

            # Use an event to make sure Stop is called once the pipeline has started
            # and not before.
            $eventId = [Guid]::NewGuid().ToString()
            Register-ObjectEvent -InputObject $outPipe -EventName DataAdded -SourceIdentifier $eventId
            try {
                $task = $ps.BeginInvoke($inPipe, $outPipe)
                Wait-Event -SourceIdentifier $eventId | Remove-Event
            }
            finally {
                Remove-Event -SourceIdentifier $eventId -ErrorAction SilentlyContinue
                Unregister-Event -SourceIdentifier $eventId
            }

            $ps.Stop()
            $ps.Streams.Error | Write-Error
            { $ps.EndInvoke($task) } | Should -Throw -ErrorId PipelineStoppedException
        }
    }

    It 'Signal advanced function to stop' {
        $start = Get-Date

        Invoke-WithStop -ScriptBlock {
            Function Test-FunctionWithStop {
                [CmdletBinding()]
                param ([Parameter()][int]$Timeout)

                [System.Threading.Tasks.Task]::Delay($Timeout * 1000, $PSCmdlet.PipelineStopToken).GetAwaiter().GetResult()
            }

            Test-FunctionWithStop -Timeout 10
        }

        $end = (Get-Date) - $start
        $end.TotalSeconds | Should -BeLessThan 10
    }

    It 'Signals compiled cmdlet to stop' {
        $binaryAssembly = Add-Type @'
using System;
using System.Management.Automation;
using System.Threading.Tasks;

namespace PipelineStoppedToken.Tests;

[Cmdlet(VerbsDiagnostic.Test, "CmdletWithStop")]
public sealed class TestCmdletWithStop : Cmdlet
{
    [Parameter]
    public int Timeout { get; set; }

    protected override void EndProcessing()
    {
        Task.Delay(Timeout * 1000, PipelineStopToken).GetAwaiter().GetResult();
    }
}
'@ -PassThru | ForEach-Object Assembly | Select-Object -First 1

        $start = Get-Date
        Invoke-WithStop -ScriptBlock {
            Import-Module -Assembly $args[0]

            Test-CmdletWithStop -Timeout 10
         } -ArgumentList $binaryAssembly

        $end = (Get-Date) - $start
        $end.TotalSeconds | Should -BeLessThan 10
    }
}
