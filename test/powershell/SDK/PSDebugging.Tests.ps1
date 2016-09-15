using namespace System.Diagnostics
using namespace System.Management.Automation.Internal

Describe "PowerShell Command Debugging" -tags "CI" {

    BeforeAll {
        $powershell = Join-Path -Path $PsHome -ChildPath "powershell"
    }

    function NewProcessStartInfo([string]$CommandLine, [switch]$RedirectStdIn)
    {
        return [ProcessStartInfo]@{
            FileName               = $powershell
            Arguments              = $CommandLine
            RedirectStandardInput  = $RedirectStdIn
            RedirectStandardOutput = $true
            RedirectStandardError  = $true
            UseShellExecute        = $false
        }
    }

    function RunPowerShell([ProcessStartInfo]$debugfn)
    {
        $process = [Process]::Start($debugfn)
        return $process
    }


    function EnsureChildHasExited([Process]$process, [int]$WaitTimeInMS = 15000)
    {
        $process.WaitForExit($WaitTimeInMS)

        if (!$process.HasExited)
        {
            $process.HasExited | Should Be $true
            $process.Kill()
        }
    }

    It "Should be able to step into debugging" {
        $debugfn = NewProcessStartInfo "-noprofile ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n")

        $process.StandardInput.Write("foo`n")

        $process.StandardInput.Write("s`n")

        $process.StandardInput.Write("s`n")

        $process.StandardInput.Write("s`n")

        $process.StandardInput.Close()

        EnsureChildHasExited $process
        $process.ExitCode | Should Be 0
    }

    It "Should be able to continue into debugging" {
        $debugfn = NewProcessStartInfo "-noprofile ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n")

        $process.StandardInput.Write("foo`n")

        $process.StandardInput.Write("c`n")
        $process.StandardOutput.ReadLine()
        $process.StandardOutput.ReadLine()

        $process.StandardInput.Close()

        EnsureChildHasExited $process
        $process.ExitCode | Should Be 0
    }

    It -Pending "Should be able to list help for debugging" {
        $debugfn = NewProcessStartInfo "-noprofile ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n")

        $process.StandardInput.Write("foo`n")

        $process.StandardInput.Write("h`n")

        foreach ($i in 1..38) {
            $line = $process.StandardOutput.ReadLine()
        }

        $process.StandardInput.Write("s`n")
        $process.StandardInput.Write("s`n")
        $process.StandardInput.Write("s`n")
        $process.StandardInput.Close()

        EnsureChildHasExited $process
        $line | Should Be  "For instructions about how to customize your debugger prompt, type `"help about_prompt`"."
    }


    It "Should be able to step over debugging" {
        $debugfn = NewProcessStartInfo "-noprofile ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n")

        $process.StandardInput.Write("foo`n")
        $process.StandardInput.Write("v`n")
        $process.StandardInput.Write("v`n")
        $process.StandardInput.Write("v`n")

        $process.StandardInput.Close()

        EnsureChildHasExited $process
        $process.ExitCode | Should Be 0
    }


    It "Should be able to step out of debugging" {
        $debugfn = NewProcessStartInfo "-noprofile ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n")

        $process.StandardInput.Write("foo`n")
        $process.StandardInput.Write("o`n")

        $process.StandardInput.Close()

        EnsureChildHasExited $process
        $process.ExitCode | Should Be 0
    }

    It "Should be able to quit debugging" {
        $debugfn = NewProcessStartInfo "-noprofile ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n")

        $process.StandardInput.Write("foo`n")
        $process.StandardInput.Write("q`n")

        $process.StandardInput.Close()

        EnsureChildHasExited $process
        $process.ExitCode | Should Be 0
    }

    It -Pending "Should be able to list source code in debugging" {
        $debugfn = NewProcessStartInfo "-noprofile ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n") | Write-Host

        $process.StandardInput.Write("foo`n") | Write-Host
        $process.StandardInput.Write("l`n") | Write-Host

        foreach ($i in 1..19) {
            $line = $process.StandardOutput.ReadLine()
        }

        $process.StandardInput.Write("`n")
        $process.StandardInput.Write("`n")
        $process.StandardInput.Write("`n")

        $line | Should Be "    1:* `$function:foo = { 'bar' }"
        $process.StandardInput.Close()
        EnsureChildHasExited $process

    }


    It -Pending "Should be able to get the call stack in debugging" {
        $debugfn = NewProcessStartInfo "-noprofile ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n") | Write-Host

        $process.StandardInput.Write("foo`n") | Write-Host
        $process.StandardInput.Write("k`n") | Write-Host

        foreach ($i in 1..20) {
            $line = $process.StandardOutput.ReadLine()
        }

        $line | Should Be "foo           {}        <No file>"
        $process.StandardInput.Close()
        EnsureChildHasExited $process

    }


}

# Scripting\Debugging\RunspaceDebuggingTests.cs
Describe "Runspace Debugging API tests" -tag CI {
    Context "PSStandaloneMonitorRunspaceInfo tests" {
        BeforeAll {
            $runspace = [runspacefactory]::CreateRunspace()
            $runspaceType = [PSMonitorRunspaceType]::WorkflowInlineScript
            $monitorInfo = [PSStandaloneMonitorRunspaceInfo]::new($runspace)
            $instanceId = $runspace.InstanceId
            $parentDebuggerId = [guid]::newguid()
            $ps = [powershell]::Create()
            $embeddedRunspaceInfo = [PSEmbeddedMonitorRunspaceInfo]::New($runspace,$runspaceType,$ps, $parentDebuggerId)
        }
        AfterAll {
            $runspace.Dispose()
            $ps.dispose()
        }
        
        It "PSStandaloneMonitorRunspaceInfo should throw when called with a null argument to the constructor" {
            try {
                [PSStandaloneMonitorRunspaceInfo]::new($null)
                throw "Execution should have thrown"
            }
            Catch {
                $_.FullyQualifiedErrorId | should be PSArgumentNullException
            }
        }

        it "PSStandaloneMonitorRunspaceInfo properties should have proper values" {
            $monitorInfo.Runspace.InstanceId | Should be $InstanceId
            $monitorInfo.RunspaceType | Should be "StandAlone"
            $monitorInfo.NestedDebugger | Should BeNullOrEmpty
        }

        It "Embedded runspace properties should have proper values" {
            $embeddedRunspaceInfo.Runspace.InstanceId | should be $InstanceId
            $embeddedRunspaceInfo.ParentDebuggerId | should be $parentDebuggerId
            $embeddedRunspaceInfo.Command.InstanceId | should be $ps.InstanceId
            $embeddedRunspaceInfo.NestedDebugger | Should BeNullOrEmpty
        }
    }
    Context "Test Monitor RunspaceInfo API tests" {
        BeforeAll {
            $runspace = [runspacefactory]::CreateRunspace()
            $runspace.Open()
            $associationId = [guid]::newguid()
            $runspaceInfo = [PSStandaloneMonitorRunspaceInfo]::new($runspace)
        }
        AfterAll {
            $runspace.Dispose()
        }

        It "DebuggerUtils StartMonitoringRunspace requires non-null debugger" {
            try {
                [DebuggerUtils]::StartMonitoringRunspace($null,$runspaceInfo)
                throw "Execution should have thrown"
            }
            catch { 
                $_.fullyqualifiederrorid | should be PSArgumentNullException
            }
        }
        It "DebuggerUtils StartMonitoringRunspace requires non-null runspaceInfo" {
            try {
                [DebuggerUtils]::StartMonitoringRunspace($runspace.Debugger,$null)
                throw "Execution should have thrown"
            }
            catch { 
                $_.fullyqualifiederrorid | should be PSArgumentNullException
            }
        }

        It "DebuggerUtils EndMonitoringRunspace requires non-null debugger" {
            try {
                [DebuggerUtils]::EndMonitoringRunspace($null,$runspaceInfo)
                throw "Execution should have thrown"
            }
            catch { 
                $_.fullyqualifiederrorid | should be PSArgumentNullException
            }
        }
        It "DebuggerUtils EndMonitoringRunspace requires non-null runspaceInfo" {
            try {
                [DebuggerUtils]::EndMonitoringRunspace($runspace.Debugger,$null)
                throw "Execution should have thrown"
            }
            catch { 
                $_.fullyqualifiederrorid | should be PSArgumentNullException
            }
        }

    }
}



