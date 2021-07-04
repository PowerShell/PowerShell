# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Function Pipeline Behaviour' -Tag 'CI' {

    BeforeAll {
        $filePath = "$TestDrive\output.txt"
        if (Test-Path $filePath) {
            Remove-Item $filePath -Force
        }
    }

    Context "'Clean' block runs when any other named blocks run" {

        AfterEach {
            if (Test-Path $filePath) {
                Remove-Item $filePath -Force
            }
        }

        It "'Clean' block executes only if at least one of the other named blocks executed" {
            ## The 'Clean' block is for cleanup purpose. When none of other named blocks execute,
            ## there is no point to execute the 'Clean' block, so it will be skipped in this case.
            function test-1 {
                clean { 'clean-redirected-output' > $filePath }
            }

            function test-2 {
                End { 'end' }
                clean { 'clean-redirected-output' > $filePath }
            }

            ## The 'Clean' block is skipped.
            test-1 | Should -BeNullOrEmpty
            Test-Path -Path $filePath | Should -BeFalse

            ## The 'Clean' block runs.
            test-2 | Should -BeExactly 'end'
            Test-Path -Path $filePath | Should -BeTrue
            Get-Content $filePath | Should -BeExactly 'clean-redirected-output'
        }

        It "'Clean' block is skipped when the command doesn't run due to no input from upstream command" {
            function test-1 ([switch] $WriteOutput) {
                Process {
                    if ($WriteOutput) {
                        Write-Output 'process'
                    } else {
                        Write-Verbose -Verbose 'process'
                    }
                }
            }

            function test-2 {
                Process { Write-Output "test-2: $_" }
                clean { Write-Warning 'test-2-clean-warning' }
            }

            ## No output from 'test-1.Process', so 'test-2.Process' didn't run, and thus 'test-2.Clean' was skipped.
            test-1 | test-2 *>&1 | Should -BeNullOrEmpty

            ## Output from 'test-1.Process' would trigger 'test-2.Process' to run, and thus 'test-2.Clean' would run.
            $output = test-1 -WriteOutput | test-2 *>&1
            $output | Should -Be @('test-2: process', 'test-2-clean-warning')
        }

        It "'Clean' block is skipped when the command doesn't run due to terminating error from upstream Process block" {
            function test-1 ([switch] $ThrowException) {
                Process {
                    if ($ThrowException) {
                        throw 'process'
                    } else {
                        Write-Output 'process'
                    }
                }
            }

            function test-2 {
                Process { Write-Output "test-2: $_" }
                clean { 'clean-redirected-output' > $filePath }
            }

            $failure = $null
            try { test-1 -ThrowException | test-2 } catch { $failure = $_ }
            $failure | Should -Not -BeNullOrEmpty
            $failure.Exception.Message | Should -BeExactly 'process'
            ## 'test-2' didn't run because 'test-1' throws terminating exception, so 'test-2.Clean' didn't run either.
            Test-Path -Path $filePath | Should -BeFalse

            test-1 | test-2 | Should -BeExactly 'test-2: process'
            Test-Path -Path $filePath | Should -BeTrue
            Get-Content $filePath | Should -BeExactly 'clean-redirected-output'
        }

        It "'Clean' block is skipped when the command doesn't run due to terminating error from upstream Begin block" {
            function test-1 {
                Begin { throw 'begin' }
                End { 'end' }
            }

            function test-2 {
                Begin { 'begin' }
                Process { Write-Output "test-2: $_" }
                clean { 'clean-redirected-output' > $filePath }
            }

            $failure = $null
            try { test-1 | test-2 } catch { $failure = $_ }
            $failure | Should -Not -BeNullOrEmpty
            $failure.Exception.Message | Should -BeExactly 'begin'
            ## 'test-2' didn't run because 'test-1' throws terminating exception, so 'test-2.Clean' didn't run either.
            Test-Path -Path $filePath | Should -BeFalse
        }

        It "'Clean' block runs when '<BlockName>' runs" -TestCases @(
            @{ Script = { [CmdletBinding()]param() begin { 'output' }  clean { Write-Warning 'clean-warning' } }; BlockName = 'Begin' }
            @{ Script = { [CmdletBinding()]param() process { 'output' }  clean { Write-Warning 'clean-warning' } }; BlockName = 'Process' }
            @{ Script = { [CmdletBinding()]param() end { 'output' }  clean { Write-Warning 'clean-warning' } }; BlockName = 'End' }
        ) {
            param($Script, $BlockName)

            & $Script -WarningVariable wv | Should -BeExactly 'output'
            $wv | Should -BeExactly 'clean-warning'
        }

        It "'Clean' block runs when '<BlockName>' throws terminating error" -TestCases @(
            @{ Script = { [CmdletBinding()]param() begin { throw 'failure' }  clean { Write-Warning 'clean-warning' } }; BlockName = 'Begin' }
            @{ Script = { [CmdletBinding()]param() process { throw 'failure' }  clean { Write-Warning 'clean-warning' } }; BlockName = 'Process' }
            @{ Script = { [CmdletBinding()]param() end { throw 'failure' }  clean { Write-Warning 'clean-warning' } }; BlockName = 'End' }
        ) {
            param($Script, $BlockName)

            $failure = $null
            try { & $Script -WarningVariable wv } catch { $failure = $_ }
            $failure | Should -Not -BeNullOrEmpty
            $failure.Exception.Message | Should -BeExactly 'failure'
            $wv | Should -BeExactly 'clean-warning'
        }

        It "'Clean' block runs in pipeline - simple function" {
            function test-1 {
                param([switch] $EmitError)
                process {
                    if ($EmitError) {
                        throw 'test-1-process-error'
                    } else {
                        Write-Output 'test-1'
                    }
                }

                clean { 'test-1-clean' >> $filePath }
            }

            function test-2 {
                begin { Write-Verbose -Verbose 'test-2-begin' }
                process { $_ }
                clean { 'test-2-clean' >> $filePath }
            }

            function test-3 {
                end { Write-Verbose -Verbose 'test-3-end' }
                clean { 'test-3-clean' >> $filePath }
            }

            ## All command will run, so all 'Clean' blocks will run
            test-1 | test-2 | test-3
            Test-Path $filePath | Should -BeTrue
            $content = Get-Content $filePath
            $content | Should -Be @('test-1-clean', 'test-2-clean', 'test-3-clean')

            $failure = $null
            Remove-Item $filePath -Force
            try {
                test-1 -EmitError | test-2 | test-3
            } catch {
                $failure = $_
            }

            ## Exception is thrown from 'test-1.Process'. By that time, the 'test-2.Begin' has run,
            ## so 'test-2.Clean' will run. However, 'test-3.End' won't run, so 'test-3.Clean' won't run.
            $failure | Should -Not -BeNullOrEmpty
            $failure.Exception.Message | Should -BeExactly 'test-1-process-error'
            Test-Path $filePath | Should -BeTrue
            $content = Get-Content $filePath
            $content | Should -Be @('test-1-clean', 'test-2-clean')
        }

        It "'Clean' block runs in pipeline - advanced function" {
            function test-1 {
                [CmdletBinding()]
                param([switch] $EmitError)
                process {
                    if ($EmitError) {
                        throw 'test-1-process-error'
                    } else {
                        Write-Output 'test-1'
                    }
                }

                clean { 'test-1-clean' >> $filePath }
            }

            function test-2 {
                [CmdletBinding()]
                param(
                    [Parameter(ValueFromPipeline)]
                    $pipeInput
                )

                begin { Write-Verbose -Verbose 'test-2-begin' }
                process { $pipeInput }
                clean { 'test-2-clean' >> $filePath }
            }

            function test-3 {
                [CmdletBinding()]
                param(
                    [Parameter(ValueFromPipeline)]
                    $pipeInput
                )

                end { Write-Verbose -Verbose 'test-3-end' }
                clean { 'test-3-clean' >> $filePath }
            }

            ## All command will run, so all 'Clean' blocks will run
            test-1 | test-2 | test-3
            Test-Path $filePath | Should -BeTrue
            $content = Get-Content $filePath
            $content | Should -Be @('test-1-clean', 'test-2-clean', 'test-3-clean')


            $failure = $null
            Remove-Item $filePath -Force
            ## Exception will be thrown from 'test-1.Process'. By that time, the 'test-2.Begin' has run,
            ## so 'test-2.Clean' will run. However, 'test-3.End' won't run, so 'test-3.Clean' won't run.
            try {
                test-1 -EmitError | test-2 | test-3
            } catch {
                $failure = $_
            }
            $failure | Should -Not -BeNullOrEmpty
            $failure.Exception.Message | Should -BeExactly 'test-1-process-error'
            Test-Path $filePath | Should -BeTrue
            $content = Get-Content $filePath
            $content | Should -Be @('test-1-clean', 'test-2-clean')
        }

        It 'does not execute End {} if the pipeline is halted during Process {}' {
            # We don't need Should -Not -Throw as if this reaches end{} and throws the test will fail anyway.
            1..10 |
                & {
                    begin { "BEGIN" }
                    process { "PROCESS $_" }
                    end { "END"; throw "This should not be reached." }
                } |
                Select-Object -First 3 |
                Should -Be @( "BEGIN", "PROCESS 1", "PROCESS 2" )
        }

        It "still executes 'Clean' block if the pipeline is halted" {
            1..10 |
                & {
                    process { $_ }
                    clean { "Clean block hit" > $filePath }
                } |
                Select-Object -First 1 |
                Should -Be 1

            Test-Path $filePath | Should -BeTrue
            Get-Content $filePath | Should -BeExactly 'Clean block hit'
        }
    }

    Context 'Streams from Named Blocks' {

        It 'Permits output from named block: <Script>' -TestCases @(
            @{ Script = { begin { 10 } }; ExpectedResult = 10 }
            @{ Script = { process { 15 } }; ExpectedResult = 15 }
            @{ Script = { end { 22 } }; ExpectedResult = 22 }
        ) {
            param($Script, $ExpectedResult)
            & $Script | Should -Be $ExpectedResult
        }

        It "Does not allow output from 'Clean' block" {
            & { end { } clean { 11 } } | Should -BeNullOrEmpty
        }

        It "OutVariable should not capture anything from 'Clean' block" {
            function test {
                [CmdletBinding()]
                param()

                Begin { 'begin' }
                Process { 'process' }
                End { 'end' }
                clean { 'clean' }
            }

            test -OutVariable ov | Should -Be @( 'begin', 'process', 'end' )
            $ov | Should -Be @( 'begin', 'process', 'end' )
        }

        It "Other streams can be captured from 'Clean' block" {
            function test {
                [CmdletBinding()]
                param()

                End { }
                clean {
                    Write-Output 'clean-output'
                    Write-Warning 'clean-warning'
                    Write-Verbose -Verbose 'clean-verbose'
                    Write-Debug -Debug 'clean-debug'
                    Write-Information 'clean-information'
                }
            }

            test -OutVariable ov -WarningVariable wv -InformationVariable iv
            $ov.Count | Should -Be 0
            $wv | Should -BeExactly 'clean-warning'
            $iv | Should -BeExactly 'clean-information'

            $allStreams = test *>&1
            $allStreams | Should -Be @('clean-warning', 'clean-verbose', 'clean-debug', 'clean-information')
        }

        It 'passes output for begin, then process, then end, then clean' {
            $Script = {
                clean { Write-Warning 'clean-warning' }
                process { "PROCESS" }
                begin { "BEGIN" }
                end { "END" }
            }

            $results = & $Script 3>&1
            $results | Should -Be @( "BEGIN", "PROCESS", "END", "clean-warning" )
        }
    }

    Context "Steppable pipeline" {

        AfterEach {
            if (Test-Path $filePath) {
                Remove-Item $filePath -Force
            }
        }

        It "'Clean' runs when steppable pipeline runs to the end successfully (<Block> block)" -TestCases @(
            @{ Script = { begin { 'BEGIN' } clean { 'clean is hit' > $filePath } }; Block = 'Begin'; ProcessResult = @('BEGIN'); EndResult = $null }
            @{ Script = { process { 'PROCESS' } clean { 'clean is hit' > $filePath } }; Block = 'Process'; ProcessResult = @('PROCESS'); EndResult = $null }
            @{ Script = { end { 'END' } clean { 'clean is hit' > $filePath } }; Block = 'End'; ProcessResult = $null; EndResult = @('END') }
            @{ Script = { begin { 'BEGIN' } process { 'PROCESS' } end { 'END' } clean { 'clean is hit' > $filePath } }; Block = 'All'; ProcessResult = @('BEGIN', 'PROCESS'); EndResult = @('END') }
        ) {
            param($Script, $ProcessResult, $EndResult)

            try {
                $step = { & $Script }.GetSteppablePipeline()
                $step.Begin($false)
                $step.Process() | Should -Be $ProcessResult
                $step.End() | Should -Be $EndResult
            }
            finally {
                $step.Dispose()
            }

            Test-Path $filePath | Should -BeTrue
            Get-Content $filePath | Should -BeExactly 'clean is hit'
        }

        It "'Clean' runs when exception thrown from '<Block>' block" -TestCases @(
            @{ Script = { begin { throw 'begin-error' } clean { 'clean is hit' > $filePath } }; Block = 'Process'; ErrorMessage = 'begin-error' }
            @{ Script = { process { throw 'process-error' } clean { 'clean is hit' > $filePath } }; Block = 'Process'; ErrorMessage = 'process-error' }
            @{ Script = { end { throw 'end-error' } clean { 'clean is hit' > $filePath } }; Block = 'End'; ErrorMessage = 'end-error' }
        ) {
            param($Script, $ErrorMessage)

            $failure = $null
            $step = { & $Script }.GetSteppablePipeline()

            try {
                $step.Begin($false)
                $step.Process()
                $step.End()
            } catch {
                $failure = $_
            }
            finally {
                $step.Dispose()
            }

            $failure | Should -Not -BeNullOrEmpty
            $failure.Exception.Message | Should -BeExactly $ErrorMessage

            Test-Path $filePath | Should -BeTrue
            Get-Content $filePath | Should -BeExactly 'clean is hit'
        }

        It "'Clean' runs when we explicitly call it on a steppable pipeline" {
            $script = { begin { 'begin' > $filePath } clean { 'clean is hit' >> $filePath } }
            $step = { & $script }.GetSteppablePipeline()

            try {
                $step.Begin($false)
                $step.Clean()
            }
            finally {
                $step.Dispose()
            }

            Test-Path $filePath | Should -BeTrue
            Get-Content $filePath | Should -BeExactly @('begin', 'clean is hit')
        }

        It "Calling 'Clean' on steppable pipeline after it has run automatically upon Exception won't trigger the 'Clean' block to run again" {
            $script = { begin { throw 'begin-error' } clean { 'clean is hit' > $filePath } }
            $step = { & $script }.GetSteppablePipeline()
            $failure = $null

            try {
                $step.Begin($false)
                $step.Process()
                $step.End()
            }
            catch {
                $failure = $_
            }

            $failure | Should -Not -BeNullOrEmpty
            $failure.Exception.Message | Should -BeExactly 'begin-error'

            Test-Path $filePath | Should -BeTrue
            Get-Content $filePath | Should -BeExactly 'clean is hit'

            Remove-Item $filePath -Force -ErrorAction Stop
            ## The 'Clean' block has already run automatically after the exception was thrown from 'Begin',
            ## and it won't run again when calling it explicitly.
            $step.Clean()
            Test-Path $filePath | Should -BeFalse

            ## Dispose the steppable pipeline.
            $step.Dispose()
        }

        It "Calling 'Clean' on steppable pipeline after it has run automatically upon success won't trigger the 'Clean' block to run again" {
            $script = { end { 'END' } clean { 'clean is hit' > $filePath } }
            $step = { & $script }.GetSteppablePipeline()

            $step.Begin($false)
            $step.Process()
            $step.End()

            Test-Path $filePath | Should -BeTrue
            Get-Content $filePath | Should -BeExactly 'clean is hit'

            Remove-Item $filePath -Force -ErrorAction Stop
            ## The 'Clean' block has already run automatically after the exception was thrown from 'Begin',
            ## and it won't run again when calling it explicitly.
            $step.Clean()
            Test-Path $filePath | Should -BeFalse

            ## Dispose the steppable pipeline.
            $step.Dispose()
        }
    }

    Context "'exit' statement in command" {

        AfterEach {
            if (Test-Path $filePath) {
                Remove-Item $filePath -Force
            }
        }

        It "'Clean' block runs when 'exit' is used in other named blocks" {
            pwsh -c "& { process { exit 122 } clean { 'Clean block is hit' > $filePath } }"

            $LASTEXITCODE | Should -BeExactly 122
            Test-Path $filePath | Should -BeTrue
            Get-Content $filePath | Should -BeExactly 'Clean block is hit'
        }
    }

    <#
    Context 'Ctrl-C behavior' {

        BeforeAll {
            $powershell = $null
        }

        AfterEach {
            if ($powershell) {
                $powershell.Dispose()
                $powershell = $null
            }
        }

        It 'still executes clean {} when StopProcessing() is triggered mid-pipeline' {
            $script = @"
                function test {
                    begin {}
                    process {
                        Start-Sleep -Seconds 10
                    }
                    end {}
                    clean {
                        Write-Information "CLEAN"
                    }

                }
"@
            $powershell = [powershell]::Create()
            $powershell.AddScript($script).AddStatement().AddCommand('test') > $null

            $asyncResult = $powershell.BeginInvoke()
            Start-Sleep -Seconds 2
            $powershell.Stop()

            $powershell.EndInvoke($asyncResult) > $null
            $powershell.Streams.Information[0].MessageData | Should -BeExactly "CLEAN"
        }

        It 'still completes clean {} execution when StopProcessing() is triggered mid-clean {}' {
            $script = @"
                function test {
                    begin {}
                    process {
                        "PROCESS"
                    }
                    end {}
                    clean {
                        Start-Sleep -Seconds 10
                        Write-Information "CLEAN"
                    }

                }
"@
            $powershell = [powershell]::Create()
            $powershell.AddScript($script).AddStatement().AddCommand('test') > $null

            $asyncResult = $powershell.BeginInvoke()
            Start-Sleep -Seconds 2
            $powershell.Stop()

            $output = $powershell.EndInvoke($asyncResult)

            $output | Should -Be "PROCESS"
            $powershell.Streams.Information[0].MessageData | Should -BeExactly "CLEAN"
        }
    }
    #>
}
