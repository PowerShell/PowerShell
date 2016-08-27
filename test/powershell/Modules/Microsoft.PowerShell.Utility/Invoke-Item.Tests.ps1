using namespace System.Diagnostics

Describe "Invoke-Item" -Tags "CI" {

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

    It "Should invoke a text file without error" -Skip:($IsWindows -and $IsCoreCLR) {
        $debugfn = NewProcessStartInfo "-noprofile ""``Invoke-Item $testfile`n" -RedirectStdIn
        $process = RunPowerShell $debugfn
        EnsureChildHasExited $process
        $process.ExitCode | Should Be 0
    }

}

Describe "Invoke-Item tests" -Tags "CI" {

    # Helper functions
    #
    function Get-AppNameFor
    {
        param (
            [ValidateSet("txt", "html")]
            [String]
            $Extension
        )

        [string]$appName = (Get-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.$Extension\OpenWithList").a
        $appName = $appName.Replace(".EXE", "")

        if (-not $appName)
        {
            throw "unable to find app associate with extension '$Extension'"
        }

        return $appName
    }

    function New-TxtFile
    {
        param ($path)
    
        if (-not $path)
        {
            $path = Join-Path $env:TEMP "$((Get-Random).ToString() + '.txt')"
        }

        if (Test-Path $path)
        {
            remove-item $path -Force -ea SilentlyContinue
        }

        Set-Content -Path $path -Value "Sample file" -Force

        return $path
    }

    It "Invoke-Item opens txt file with the default txt reader" -Skip:($IsLinux -or $IsOSX) {

        $appName = Get-AppNameFor -Extension txt
        $txtFilePath = New-TxtFile

        try
        {
            try { $processIds =  @(Get-Process -Name $appName -ea SilentlyContinue | % Id) } catch {$processIds = @()}

            # Open the file via invoke-item
            Invoke-Item $txtFilePath

            # wait while the files opens
            Start-Sleep -Milliseconds 500

            $newProcessId = @(Get-Process -Name $appName -ea SilentlyContinue | % Id)

            $newProcessId.Count -gt $processIds.Count | Should Be $true
        }
        finally
        {
            # Close the open explorer instance
            foreach ($id in $newProcessId)
            {
                if ($processIds -notcontains $id)
                {
                    Stop-Process -Id $id -ea SilentlyContinue
                }
            }

            Remove-Item $txtFilePath -Force -ea SilentlyContinue
        }
    }
}
