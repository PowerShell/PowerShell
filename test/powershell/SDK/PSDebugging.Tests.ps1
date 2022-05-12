# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
using namespace System.Diagnostics
using namespace System.Management.Automation.Internal

Describe "PowerShell Command Debugging" -tags "CI" {

    BeforeAll {
        $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"
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
            $process.HasExited | Should -BeTrue
            $process.Kill()
        }
    }

    It "Should be able to step into debugging" {
        $debugfn = NewProcessStartInfo "-noprofile -c ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n")

        $process.StandardInput.Write("foo`n")

        $process.StandardInput.Write("s`n")

        $process.StandardInput.Write("s`n")

        $process.StandardInput.Write("s`n")

        $process.StandardInput.Close()

        EnsureChildHasExited $process
        $process.ExitCode | Should -Be 0
    }

    It "Should be able to continue into debugging" {
        $debugfn = NewProcessStartInfo "-noprofile -c ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n")

        $process.StandardInput.Write("foo`n")

        $process.StandardInput.Write("c`n")
        $process.StandardOutput.ReadLine()
        $process.StandardOutput.ReadLine()

        $process.StandardInput.Close()

        EnsureChildHasExited $process
        $process.ExitCode | Should -Be 0
    }

    It -Pending "Should be able to list help for debugging" {
        $debugfn = NewProcessStartInfo "-noprofile -c ""`$function:foo = { 'bar' }""" -RedirectStdIn
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
        $line | Should -BeExactly  "For instructions about how to customize your debugger prompt, type `"help about_prompt`"."
    }

    It "Should be able to step over debugging" {
        $debugfn = NewProcessStartInfo "-noprofile -c ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n")

        $process.StandardInput.Write("foo`n")
        $process.StandardInput.Write("v`n")
        $process.StandardInput.Write("v`n")
        $process.StandardInput.Write("v`n")

        $process.StandardInput.Close()

        EnsureChildHasExited $process
        $process.ExitCode | Should -Be 0
    }

    It "Should be able to step out of debugging" {
        $debugfn = NewProcessStartInfo "-noprofile -c ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n")

        $process.StandardInput.Write("foo`n")
        $process.StandardInput.Write("o`n")

        $process.StandardInput.Close()

        EnsureChildHasExited $process
        $process.ExitCode | Should -Be 0
    }

    It "Should be able to quit debugging" {
        $debugfn = NewProcessStartInfo "-noprofile -c ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n")

        $process.StandardInput.Write("foo`n")
        $process.StandardInput.Write("q`n")

        $process.StandardInput.Close()

        EnsureChildHasExited $process
        $process.ExitCode | Should -Be 0
    }

    It -Pending "Should be able to list source code in debugging" {
        $debugfn = NewProcessStartInfo "-noprofile -c ""`$function:foo = { 'bar' }""" -RedirectStdIn
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

        $line | Should -BeExactly "    1:* `$function:foo = { 'bar' }"
        $process.StandardInput.Close()
        EnsureChildHasExited $process

    }

    It -Pending "Should be able to get the call stack in debugging" {
        $debugfn = NewProcessStartInfo "-noprofile -c ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n") | Write-Host

        $process.StandardInput.Write("foo`n") | Write-Host
        $process.StandardInput.Write("k`n") | Write-Host

        foreach ($i in 1..20) {
            $line = $process.StandardOutput.ReadLine()
        }

        $line | Should -BeExactly "foo           {}        <No file>"
        $process.StandardInput.Close()
        EnsureChildHasExited $process

    }

}

# Scripting\Debugging\RunspaceDebuggingTests.cs
Describe "Runspace Debugging API tests" -Tag CI {
    Context "PSStandaloneMonitorRunspaceInfo tests" {
        BeforeAll {
            $runspace = [runspacefactory]::CreateRunspace()
            $runspaceType = [PSMonitorRunspaceType]::InvokeCommand
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
            { [PSStandaloneMonitorRunspaceInfo]::new($null) } |
                Should -Throw -ErrorId 'PSArgumentNullException'
        }

        It "PSStandaloneMonitorRunspaceInfo properties should have proper values" {
            $monitorInfo.Runspace.InstanceId | Should -Be $InstanceId
            $monitorInfo.RunspaceType | Should -BeExactly "Standalone"
            $monitorInfo.NestedDebugger | Should -BeNullOrEmpty
        }

        It "Embedded runspace properties should have proper values" {
            $embeddedRunspaceInfo.Runspace.InstanceId | Should -Be $InstanceId
            $embeddedRunspaceInfo.ParentDebuggerId | Should -Be $parentDebuggerId
            $embeddedRunspaceInfo.Command.InstanceId | Should -Be $ps.InstanceId
            $embeddedRunspaceInfo.NestedDebugger | Should -BeNullOrEmpty
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
            { [DebuggerUtils]::StartMonitoringRunspace($null,$runspaceInfo) } |
                Should -Throw -ErrorId 'PSArgumentNullException'
        }
        It "DebuggerUtils StartMonitoringRunspace requires non-null runspaceInfo" {
            { [DebuggerUtils]::StartMonitoringRunspace($runspace.Debugger,$null) } |
                Should -Throw -ErrorId 'PSArgumentNullException'
        }

        It "DebuggerUtils EndMonitoringRunspace requires non-null debugger" {
            { [DebuggerUtils]::EndMonitoringRunspace($null,$runspaceInfo) } |
                Should -Throw -ErrorId 'PSArgumentNullException'
        }
        It "DebuggerUtils EndMonitoringRunspace requires non-null runspaceInfo" {
            { [DebuggerUtils]::EndMonitoringRunspace($runspace.Debugger,$null) } |
                Should -Throw -ErrorId 'PSArgumentNullException'
        }

    }
}

