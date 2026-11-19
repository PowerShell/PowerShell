# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'Basic engine APIs' -Tags "CI" {
    Context 'powershell::Create' {
        It 'can create default instance' {
            [powershell]::Create() | Should -Not -BeNullOrEmpty
        }

        It 'can create instance with runspace' {
            $rs = [runspacefactory]::CreateRunspace()
            $ps = [powershell]::Create($rs)
            $ps | Should -Not -BeNullOrEmpty
            $ps.Runspace | Should -Be $rs
            $ps.Dispose()
            $rs.Dispose()
        }

        It 'cannot create instance with null runspace' {
            { [powershell]::Create([runspace]$null) } | Should -Throw -ErrorId 'PSArgumentNullException'
        }

        It "can load the default snapin 'Microsoft.WSMan.Management'" -Skip:(-not $IsWindows) {
            $ps = [powershell]::Create()
            $ps.AddScript("Get-Command -Name Test-WSMan") > $null

            $result = $ps.Invoke()
            $result.Count | Should -Be 1
            $result[0].Source | Should -BeExactly "Microsoft.WSMan.Management"
        }
    }

    Context 'executioncontext' {
        It 'args are passed correctly' {
            $result = $ExecutionContext.SessionState.InvokeCommand.InvokeScript('"`$args:($args); `$input:($input)"', 1, 2, 3)
            $result | Should -BeExactly '$args:(1 2 3); $input:()'
        }
    }

    Context 'powershell::GetSteppablePipeline' {
        BeforeAll {
            function Invoke-SteppableWriteError {
                [CmdletBinding()]
                param(
                    [switch] $MergeErrors
                )

                begin {
                    $ps = [powershell]::Create([System.Management.Automation.RunspaceMode]::CurrentRunspace)
                    $null = $ps.AddCommand((Get-Command -Name Write-Error)).AddParameter('Message', 'boom')
                    if ($MergeErrors) {
                        $ps.Commands.Commands[0].MergeMyResults('Error', 'Output')
                    }

                    $step = $ps.GetSteppablePipeline()
                    $step.Begin($PSCmdlet)
                }

                end {
                    $step.End()
                    $ps.Dispose()
                }
            }
        }

        It "gives the pipeline's commands the redirections of the command that built it" {
            $result = Invoke-SteppableWriteError 2>&1
            $result | Should -BeOfType System.Management.Automation.ErrorRecord
        }

        It "keeps a merge the pipeline's command sets for itself over the redirections of the command that built it" {
            $result = Invoke-SteppableWriteError -MergeErrors 2>$null
            $result | Should -BeOfType System.Management.Automation.ErrorRecord
        }

        It "points errors from the pipeline's commands at the line that called the command that built it" {
            $call = { Invoke-SteppableWriteError 2>&1 }
            $result = & $call
            $result.InvocationInfo.ScriptName | Should -Be $call.File
            $result.InvocationInfo.ScriptLineNumber | Should -Be $call.StartPosition.StartLine
            $result.InvocationInfo.InvocationName | Should -BeExactly 'Invoke-SteppableWriteError'
        }

        It "sends errors from each of the pipeline's commands to the output of a command that merges them, not through the commands after it" {
            function Invoke-SteppableGetItemSelectObject {
                [CmdletBinding()]
                param()

                begin {
                    $ps = [powershell]::Create([System.Management.Automation.RunspaceMode]::CurrentRunspace)
                    $null = $ps.AddCommand((Get-Command -Name Get-Item)).AddParameter('LiteralPath', (Join-Path -Path $TestDrive -ChildPath 'Missing'))
                    $null = $ps.AddCommand((Get-Command -Name Select-Object)).AddParameter('Property', 'Name')
                    $step = $ps.GetSteppablePipeline()
                    $step.Begin($PSCmdlet)
                }

                end {
                    $step.End()
                    $ps.Dispose()
                }
            }

            $result = Invoke-SteppableGetItemSelectObject 2>&1
            $result | Should -HaveCount 1
            $result | Should -BeOfType System.Management.Automation.ErrorRecord
        }

        It "merges a native command's stderr into the output of a command that merges its errors" {
            function Invoke-SteppableNative {
                [CmdletBinding()]
                param()

                begin {
                    $ps = [powershell]::Create([System.Management.Automation.RunspaceMode]::CurrentRunspace)
                    $null = $ps.AddCommand((Get-Command -Name testexe -CommandType Application | Select-Object -First 1)).AddArgument('-stderrandout').AddArgument('native')
                    $step = $ps.GetSteppablePipeline()
                    $step.Begin($PSCmdlet)
                }

                end {
                    $step.End()
                    $ps.Dispose()
                }
            }

            # testexe writes 'native' to stdout and its reverse to stderr.
            $result = Invoke-SteppableNative 2>&1
            $errors = @($result | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] })
            $errors | Should -HaveCount 1
            $errors[0].Exception.Message | Should -BeExactly 'evitan'
            $result | Where-Object { $_ -is [string] } | Should -BeExactly 'native'
        }

        It "<Expectation> the stream redirections of the command running when <Method>() builds the pipeline" -TestCases @(
            @{ Method = 'GetSteppablePipeline'; Expectation = 'takes'; ExpectMerged = $true }
            @{ Method = 'GetIndependentSteppablePipeline'; Expectation = 'does not take'; ExpectMerged = $false }
        ) {
            param($Method, $ExpectMerged)

            # A remoting server builds its pipeline through GetIndependentSteppablePipeline() from an event, while an unrelated command is running.
            function Invoke-SteppableWriteWarningFrom {
                [CmdletBinding()]
                param(
                    [string] $Method
                )

                begin {
                    $ps = [powershell]::Create([System.Management.Automation.RunspaceMode]::CurrentRunspace)
                    $null = $ps.AddCommand((Get-Command -Name Write-Warning)).AddParameter('Message', 'boom')
                    $step = [powershell].GetMethod($Method, [System.Reflection.BindingFlags]'Instance, Public, NonPublic', $null, [type[]]@(), $null).Invoke($ps, $null)
                    $step.Begin($false)
                }

                end {
                    $null = $step.End()
                    $ps.Dispose()
                }
            }

            $result = Invoke-SteppableWriteWarningFrom -Method $Method 3>&1
            @($result | Where-Object { $_ -is [System.Management.Automation.WarningRecord] }).Count -gt 0 | Should -Be $ExpectMerged
        }
    }
}

Describe "Clean up open Runspaces when exit powershell process" -Tags "Feature" {
    It "PowerShell process should not freeze at exit" {
        $command = @'
-c $rs = [runspacefactory]::CreateRunspacePool(1,5)
$rs.Open()
$ps = [powershell]::Create()
$ps.RunspacePool = $rs
$null = $ps.AddScript(1).Invoke()
Write-Output "should_not_stop_responding_at_exit"
exit
'@
        $outputFile = New-Item -Path $TestDrive\output.txt -ItemType File
        $process = Start-Process "$PSHOME/pwsh" -ArgumentList $command -PassThru -RedirectStandardOutput $outputFile
        $hasExited = Wait-UntilTrue -sb { $process.HasExited } -TimeoutInMilliseconds 10000 -IntervalInMilliseconds 100

        $expectedOutput = Get-Content $outputFile

        if (! $hasExited) {
            Write-Verbose -Verbose -Message "Process has not exited after 10 second wait."
            Write-Verbose -Verbose -Message "Process object is: $($process | Select-Object 'ProcessName','Id','HasExited' | Out-String)"

            Stop-Process $process -Force
        }

        $hasExited | Should -BeTrue -Because "Process did not exit in 10 seconds as: $expectedOutput"
        $expectedOutput | Should -BeExactly 'should_not_stop_responding_at_exit'
    }
}

Describe "EndInvoke() should return a collection of results" -Tags "CI" {
    BeforeAll {
        $ps = [powershell]::Create()
        $ps.AddScript("'Hello'; 'World'") > $null
    }

    It "BeginInvoke() and then EndInvoke() should return a collection of results" {
        $async = $ps.BeginInvoke()
        $result = $ps.EndInvoke($async)

        $result.Count | Should -BeExactly 2
        $result[0] | Should -BeExactly "Hello"
        $result[1] | Should -BeExactly "World"
    }

    It "BeginInvoke() and then EndInvoke() should return a collection of results after a previous Stop() call" {
        $async = $ps.BeginInvoke()
        $ps.Stop()

        $async = $ps.BeginInvoke()
        $result = $ps.EndInvoke($async)

        $result.Count | Should -BeExactly 2
        $result[0] | Should -BeExactly "Hello"
        $result[1] | Should -BeExactly "World"
    }

    It "BeginInvoke() and then EndInvoke() should return a collection of results after a previous BeginStop()/EndStop() call" {
        $asyncInvoke = $ps.BeginInvoke()
        $asyncStop = $ps.BeginStop($null, $null)
        $ps.EndStop($asyncStop)

        $asyncInvoke = $ps.BeginInvoke()
        $result = $ps.EndInvoke($asyncInvoke)

        $result.Count | Should -BeExactly 2
        $result[0] | Should -BeExactly "Hello"
        $result[1] | Should -BeExactly "World"
    }
}
