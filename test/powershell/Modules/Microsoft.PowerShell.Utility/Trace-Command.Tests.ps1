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
            $results = $log | ForEach-Object {[datetime]::Parse($_.Split("=")[1])}

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

    Context "MethodInvocation traces" {

        BeforeAll {
            $filePath = Join-Path $TestDrive 'testtracefile.txt'

            class MyClass {
                MyClass() {}
                MyClass([int]$arg) {}

                [void]Method() { return }
                [void]Method([string]$arg) { return }
                [void]Method([int]$arg) { return }

                [string]ReturnMethod() { return "foo" }

                static [void]StaticMethod() { return }
                static [void]StaticMethod([string]$arg) { return }
            }

            # C# classes support more features than pwsh classes
            Add-Type -TypeDefinition @'
namespace TraceCommandTests;

public sealed class OverloadTests
{
    public int PropertySetter { get; set; }

    public OverloadTests() {}
    public OverloadTests(int value)
    {
        PropertySetter = value;
    }

    public void GenericMethod<T>()
    {}

    public T GenericMethodWithArg<T>(T obj) => obj;

    public void MethodWithDefault(string arg1, int optional = 1)
    {}

    public void MethodWithOut(out int val)
    {
        val = 1;
    }

    public void MethodWithRef(ref int val)
    {
        val = 1;
    }
}
'@
        }

        AfterEach {
            Remove-Item $filePath -Force -ErrorAction SilentlyContinue
        }

        It "Traces instance method" {
            $myClass = [MyClass]::new()
            Trace-Command -Name MethodInvocation -Expression {
                $myClass.Method(1)
            } -FilePath $filePath
            Get-Content $filePath | Should -BeLike "*Invoking method: void Method(int arg)"
        }

        It "Traces static method" {
            Trace-Command -Name MethodInvocation -Expression {
                [MyClass]::StaticMethod(1)
            } -FilePath $filePath
            Get-Content $filePath | Should -BeLike "*Invoking method: static void StaticMethod(string arg)"
        }

        It "Traces method with return type" {
            $myClass = [MyClass]::new()
            Trace-Command -Name MethodInvocation -Expression {
                $myClass.ReturnMethod()
            } -FilePath $filePath
            Get-Content $filePath | Should -BeLike "*Invoking method: string ReturnMethod()"
        }

        It "Traces constructor" {
            Trace-Command -Name MethodInvocation -Expression {
                [TraceCommandTests.OverloadTests]::new("1234")
            } -FilePath $filePath
            Get-Content $filePath | Should -BeLike "*Invoking method: TraceCommandTests.OverloadTests new(int value)"
        }

        It "Traces Property setter invoked as a method" {
            $obj = [TraceCommandTests.OverloadTests]::new()
            Trace-Command -Name MethodInvocation -Expression {
                $obj.set_PropertySetter(1234)
            } -FilePath $filePath
            Get-Content $filePath | Should -BeLike "*Invoking method: void set_PropertySetter(int value)"
        }

        It "Traces generic method" {
            $obj = [TraceCommandTests.OverloadTests]::new()
            Trace-Command -Name MethodInvocation -Expression {
                $obj.GenericMethod[int]()
            } -FilePath $filePath
            Get-Content $filePath | Should -BeLike "*Invoking method: void GenericMethod``[int``]()"
        }

        It "Traces generic method with argument" {
            $obj = [TraceCommandTests.OverloadTests]::new()
            Trace-Command -Name MethodInvocation -Expression {
                $obj.GenericMethodWithArg("foo")
            } -FilePath $filePath
            Get-Content $filePath | Should -BeLike "*Invoking method: string GenericMethodWithArg``[string``](string obj)"
        }

        It "Traces .NET call with default value" {
            $obj = [TraceCommandTests.OverloadTests]::new()
            Trace-Command -Name MethodInvocation -Expression {
                $obj.MethodWithDefault("foo")
            } -FilePath $filePath
            Get-Content $filePath | Should -BeLike "*Invoking method: void MethodWithDefault(string arg1, int optional = 1)"
        }

        It "Traces method with ref argument" {
            $obj = [TraceCommandTests.OverloadTests]::new()
            $v = 1

            Trace-Command -Name MethodInvocation -Expression {
                $obj.MethodWithRef([ref]$v)
            } -FilePath $filePath
            # [ref] goes through the binder so will trigger the first trace
            Get-Content $filePath | Select-Object -Skip 1 | Should -BeLike "*Invoking method: void MethodWithRef(``[ref``] int val)"
        }

        It "Traces method with out argument" {
            $obj = [TraceCommandTests.OverloadTests]::new()
            $v = 1

            Trace-Command -Name MethodInvocation -Expression {
                $obj.MethodWithOut([ref]$v)
            } -FilePath $filePath
            # [ref] goes through the binder so will trigger the first trace
            Get-Content $filePath | Select-Object -Skip 1 | Should -BeLike "*Invoking method: void MethodWithOut(``[ref``] int val)"
        }

        It "Traces a binding error" {
            Trace-Command -Name MethodInvocation -Expression {
                # try/catch is used as error formatter will hit the trace as well
                try {
                    [System.Runtime.InteropServices.Marshal]::SizeOf([int])
                }
                catch {
                    # Satisfy codefactor
                    $_ | Out-Null
                }
            } -FilePath $filePath
            # type fqn is used, the wildcard avoids hardcoding that
            Get-Content $filePath | Should -BeLike "*Invoking method: static int SizeOf``[System.RuntimeType, *``](System.RuntimeType, * structure)"
        }

        It "Traces LINQ call" {
            Trace-Command -Name MethodInvocation -Expression {
                [System.Linq.Enumerable]::Union([int[]]@(1, 2), [int[]]@(3, 4))
            } -FilePath $filePath
            Get-Content $filePath | Should -BeLike "*Invoking method: static System.Collections.Generic.IEnumerable``[int``] Union``[int``](System.Collections.Generic.IEnumerable``[int``] first, System.Collections.Generic.IEnumerable``[int``] second)"
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
