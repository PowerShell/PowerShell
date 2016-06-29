using namespace System.Diagnostics

Describe "PowerShell Command Debugging" {

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



