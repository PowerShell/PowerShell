using namespace System.Diagnostics

Describe "Invoke-Item on non-Windows" -Tags "CI" {

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
        Setup -File testfile.txt -Content "Hello World"
        $testfile = Join-Path $TestDrive testfile.txt
    }

    It "Should invoke a text file without error on non-Windows" -Skip:($IsWindows) {
        $debugfn = NewProcessStartInfo "-noprofile ""``Invoke-Item $testfile`n" -RedirectStdIn
        $process = RunPowerShell $debugfn
        EnsureChildHasExited $process
        $process.ExitCode | Should Be 0
    }
}

Describe "Invoke-Item tests on Windows" -Tags "CI","RequireAdminOnWindows" {
    BeforeAll {
        if ($IsWindows) {
            $testfilename = "testfile.!!testext!!"
            $testfilepath = Join-Path $TestDrive $testfilename
            $renamedtestfilename = "renamedtestfile.!!testext!!"
            $renamedtestfilepath = Join-Path $TestDrive $renamedtestfilename
            remove-item $testfilepath -ErrorAction SilentlyContinue
            remove-item $renamedtestfilepath -ErrorAction SilentlyContinue
            new-item $testfilepath | Out-Null
            cmd.exe /c assoc .!!testext!!=!!testext!!.FileType | Out-Null
            cmd.exe /c ftype !!testext!!.FileType=cmd.exe /c rename $testfilepath $renamedtestfilename | Out-Null
        }
    }

    AfterAll {
        if ($IsWindows) {
            remove-item $testfilepath -ErrorAction SilentlyContinue
            remove-item $renamedtestfilepath -ErrorAction SilentlyContinue
            cmd.exe /c assoc !!testext!!=
            cmd.exe /c ftype !!testext!!.FileType=
        }
    }

    It "Should invoke a file without error on Windows w/o .NET Core" -Skip:(-not $IsWindows -or ($IsWindows -and $IsCoreCLR)) {
        invoke-item $testfilepath
        # Waiting subprocess start and rename file
        {
            $startTime = [Datetime]::Now
            while (-not (test-path $renamedtestfilepath))
            {
                Start-Sleep -Milliseconds  100
                if (([Datetime]::Now - $startTime) -ge [timespan]"00:00:05") { throw "Timeout exception" }
            }
        } | Should Not throw
    }

    It "Should throw 'not supported' on Windows with .NET Core" -Skip:(-not ($IsWindows -and $IsCoreCLR)) {
        { Invoke-Item $testfilepath } | Should Throw "Operation is not supported on this platform."
    }
}
