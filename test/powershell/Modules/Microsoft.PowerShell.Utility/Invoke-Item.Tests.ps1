using namespace System.Diagnostics

Describe "Invoke-Item" {

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

    BeforeAll {
        $powershell = Join-Path -Path $PsHome -ChildPath "powershell"
        $testfile =  Setup -File testfile.txt -Content '"Hello World"' -pass
    }

    It "Should invoke a text file without error" -Skip:(!$IsWindows) {
        $debugfn = NewProcessStartInfo "-noprofile ""``Invoke-Item $testfile`n" -RedirectStdIn
        $process = RunPowerShell $debugfn
        EnsureChildHasExited $process
        $process.ExitCode | Should Be 0
    }

    It "Should throw not supported on Windows with .NET Core" -Skip:($IsLinux -or $IsOSX -or !$IsCore) {
        { Invoke-Item $testfile }| Should Throw "Operation is not supported on this platform."
    }
}
