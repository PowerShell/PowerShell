using namespace System.Diagnostics

Describe "Invoke-Item" {

    $tmpDirectory         = $TestDrive
    $testfile             = "testfile.txt"
    $testfolder           = "newDirectory"
    $testlink             = "testlink"
    $FullyQualifiedFile   = Join-Path -Path $tmpDirectory -ChildPath $testfile
    $FullyQualifiedFolder = Join-Path -Path $tmpDirectory -ChildPath $testfolder
    $FullyQualifiedLink   = Join-Path -Path $tmpDirectory -ChildPath $testlink
 
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

    function Clean-State
    {
        if (Test-Path $FullyQualifiedLink)
        {
	        Remove-Item $FullyQualifiedLink -Force
        }

        if (Test-Path $FullyQualifiedFile)
        {
	        Remove-Item $FullyQualifiedFile -Force
        }

        if (Test-Path $FullyQualifiedFolder)
        {
	    Remove-Item $FullyQualifiedFolder -Force
        }
    }

    BeforeAll {
        $powershell = Join-Path -Path $PsHome -ChildPath "powershell"
    }

#Both tests are pending due to a bug in Invoke-Item on Windows. Fixed for Linux

    It "Should call the function without error" -Pending { 
        { New-Item -Name $testfile -Path $tmpDirectory -ItemType file } | Should Not Throw
    }

    It "Should invoke a text file without error" -Pending {
        $debugfn = NewProcessStartInfo "-noprofile ""``Invoke-Item $FullyQualifiedFile`n" -RedirectStdIn
        $process = RunPowerShell $debugfn
        EnsureChildHasExited $process
        $process.ExitCode | Should Be 0
    }    
}
