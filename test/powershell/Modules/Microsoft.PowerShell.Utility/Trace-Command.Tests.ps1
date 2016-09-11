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

            Trace-Command -Name * -Expression {echo Foo} -ListenerOption LogicalOperationStack -FilePath $logfile

            $log = Get-Content $logfile | Where-Object {$_ -like "*LogicalOperationStack=$keyword*"}            
            $log.Count | Should BeGreaterThan 0
        } 

        # GetStackTrace is not in .NET Core
        It "Callstack works" -Skip:$IsCoreCLR {
            Trace-Command -Name * -Expression {echo Foo} -ListenerOption Callstack -FilePath $logfile
            $log = Get-Content $logfile | Where-Object {$_ -like "*Callstack=   * System.Environment.GetStackTrace(Exception e, Boolean needFileInfo)*"}
            $log.Count | Should BeGreaterThan 0
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

                    $allowedGap | Should BeGreaterThan $actualGap
                }
        }

        It "None options has no effect" {            
            Trace-Command -Name * -Expression {echo Foo} -ListenerOption None -FilePath $actualLogfile
            Trace-Command -name * -Expression {echo Foo} -FilePath $logfile

            Compare-Object (Get-Content $actualLogfile) (Get-Content $logfile) | Should BeNullOrEmpty
        }

        It "ThreadID works" {
            Trace-Command -Name * -Expression {echo Foo} -ListenerOption ThreadId -FilePath $logfile
            $log = Get-Content $logfile | Where-Object {$_ -like "*ThreadID=*"}
            $results = $log | ForEach-Object {$_.Split("=")[1]}

            $results | % { $_ | Should Be ([threading.thread]::CurrentThread.ManagedThreadId) }
        }

        It "Timestamp creates logs in ascending order" {
            Trace-Command -Name * -Expression {echo Foo} -ListenerOption Timestamp -FilePath $logfile
            $log = Get-Content $logfile | Where-Object {$_ -like "*Timestamp=*"}
            $results = $log | ForEach-Object {$_.Split("=")[1]}
            $sortedResults = $results | Sort-Object
            $sortedResults | Should Be $results 
        }

        It "ProcessId logs current process Id" {
            Trace-Command -Name * -Expression {echo Foo} -ListenerOption ProcessId -FilePath $logfile
            $log = Get-Content $logfile | Where-Object {$_ -like "*ProcessID=*"}
            $results = $log | ForEach-Object {$_.Split("=")[1]}

            $results | ForEach-Object { $_ | Should Be $pid }
        }
    }        
}
