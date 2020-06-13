# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# This came from monad/tests/ci/PowerShell/tests/Commands/Cmdlets/pester.utility.command.tests.ps1
Describe "Trace-Command" -tags "CI" {

    Context "Listener options" {
        BeforeAll {
            $logFile = New-Item "TestDrive:/traceCommandLog.txt" -Force
            $actualLogFile = New-Item "TestDrive:/actualTraceCommandLog.txt" -Force
        }

        AfterEach {
            Remove-Item "TestDrive:/traceCommandLog.txt" -Force -ErrorAction SilentlyContinue
            Remove-Item "TestDrive:/actualTraceCommandLog.txt" -Force -ErrorAction SilentlyContinue
        }

        # LogicalOperationStack is not in .NET Core
        It "LogicalOperationStack works" -Skip:$IsCoreCLR {
            $keyword = "Trace_Command_ListenerOption_LogicalOperationStack_Foo"
            $stack = [System.Diagnostics.Trace]::CorrelationManager.LogicalOperationStack
            $stack.Push($keyword)

            Trace-Command -Name * -Expression {Write-Output Foo} -ListenerOption LogicalOperationStack -FilePath $logfile

            $log = Get-Content $logfile | Where-Object {$_ -like "*LogicalOperationStack=$keyword*"}
            $log.Count | Should -BeGreaterThan 0
        }

        # GetStackTrace is not in .NET Core
        It "Callstack works" -Skip:$IsCoreCLR {
            Trace-Command -Name * -Expression {Write-Output Foo} -ListenerOption Callstack -FilePath $logfile
            $log = Get-Content $logfile | Where-Object {$_ -like "*Callstack=   * System.Environment.GetStackTrace(Exception e, Boolean needFileInfo)*"}
            $log.Count | Should -BeGreaterThan 0
        }

        It "Datetime works" {
            $expectedDate = Trace-Command -Name * -Expression {Get-Date} -ListenerOption DateTime -FilePath $logfile
            $log = Get-Content $logfile | Where-Object {$_ -like "*DateTime=*"}
            $results = $log | ForEach-Object {[DateTime]::Parse($_.Split("=")[1])}

            ## allow a gap of 6 seconds. All traces should be finished within 6 seconds.
            $allowedGap = [timespan](60 * 1000 * 1000)
            $results | ForEach-Object {
                    $actualGap = $_ - $expectedDate;
                    if ($expectedDate -gt $_)
                    {
                        $actualGap = $expectedDate - $_;
                    }

                    $allowedGap | Should -BeGreaterThan $actualGap
                }
        }

        It "None options has no effect" {
            Trace-Command -Name * -Expression {Write-Output Foo} -ListenerOption None -FilePath $actualLogfile
            Trace-Command -Name * -Expression {Write-Output Foo} -FilePath $logfile

            Compare-Object (Get-Content $actualLogfile) (Get-Content $logfile) | Should -BeNullOrEmpty
        }

        It "ThreadID works" {
            Trace-Command -Name * -Expression {Write-Output Foo} -ListenerOption ThreadId -FilePath $logfile
            $log = Get-Content $logfile | Where-Object {$_ -like "*ThreadID=*"}
            $results = $log | ForEach-Object {$_.Split("=")[1]}

            $results | ForEach-Object { $_ | Should -Be ([threading.thread]::CurrentThread.ManagedThreadId) }
        }

        It "Timestamp creates logs in ascending order" {
            Trace-Command -Name * -Expression {Write-Output Foo} -ListenerOption Timestamp -FilePath $logfile
            $log = Get-Content $logfile | Where-Object {$_ -like "*Timestamp=*"}
            $results = $log | ForEach-Object {$_.Split("=")[1]}
            $sortedResults = $results | Sort-Object
            $sortedResults | Should -Be $results
        }

        It "ProcessId logs current process Id" {
            Trace-Command -Name * -Expression {Write-Output Foo} -ListenerOption ProcessId -FilePath $logfile
            $log = Get-Content $logfile | Where-Object {$_ -like "*ProcessID=*"}
            $results = $log | ForEach-Object {$_.Split("=")[1]}

            $results | ForEach-Object { $_ | Should -Be $PID }
        }
    }

    Context "Trace-Command tests for code coverage" {

        BeforeAll {
            $filePath = Join-Path $TestDrive 'testtracefile.txt'
        }

        AfterEach {
            Remove-Item $filePath -Force -ErrorAction SilentlyContinue
        }

        It "Get non-existing trace source" {
            { '34E7F9FA-EBFB-4D21-A7D2-D7D102E2CC2F' | Get-TraceSource -ErrorAction Stop} | Should -Throw -ErrorId 'TraceSourceNotFound,Microsoft.PowerShell.Commands.GetTraceSourceCommand'
        }

        It "Set-TraceSource to file and RemoveFileListener wildcard" {
            $null = Set-TraceSource -Name "ParameterBinding" -Option ExecutionFlow -FilePath $filePath -Force -ListenerOption "ProcessId,TimeStamp" -PassThru
            Set-TraceSource -Name "ParameterBinding" -RemoveFileListener *
            Get-Content $filePath -Raw | Should -Match 'ParameterBinding Information'
        }

        It "Trace-Command -Command with error" {
            { Trace-Command -Name ParameterBinding -Command 'Get-PSDrive' -ArgumentList 'NonExistingDrive' -Option ExecutionFlow -FilePath $filePath -Force -ListenerOption "ProcessId,TimeStamp" -ErrorAction Stop } |
                Should -Throw -ErrorId 'GetLocationNoMatchingDrive,Microsoft.PowerShell.Commands.TraceCommandCommand'
        }

        It "Trace-Command fails for non-filesystem paths" {
            { Trace-Command -Name ParameterBinding -Expression {$null} -FilePath "Env:\Test" -ErrorAction Stop } | Should -Throw -ErrorId 'FileListenerPathResolutionFailed,Microsoft.PowerShell.Commands.TraceCommandCommand'
        }

        It "Trace-Command to readonly file" {
            $null = New-Item $filePath -Force
            Set-ItemProperty $filePath -Name IsReadOnly -Value $true
            Trace-Command -Name ParameterBinding -Command 'Get-PSDrive' -FilePath $filePath -Force
            Get-Content $filePath -Raw | Should -Match 'ParameterBinding Information'
        }

        It "Trace-Command using Path parameter alias" {
            $null = New-Item $filePath -Force
            Trace-Command -Name ParameterBinding -Command 'Get-PSDrive' -Path $filePath -Force
            Get-Content $filePath -Raw | Should -Match 'ParameterBinding Information'
        }

        It "Trace-Command contains wildcard characters" {
            $a = Trace-Command -Name ParameterB* -Command 'get-alias'
            $a.count | Should -BeGreaterThan 0
        }
    }
}
