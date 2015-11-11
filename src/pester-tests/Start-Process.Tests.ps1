$here = Split-Path -Parent $MyInvocation.MyCommand.Path
. $here/Test-Common.ps1

Describe "Start-Process" {
    $pingCommand = (Get-Command -CommandType Application ping)[0].Definition
    $pingDirectory = Split-Path $pingCommand -Parent
    $tempDir = GetTempDir
    $tempFile = $tempDir + "PSTest"
    $assetsFile = $here + "/assets/SortTest.txt"
    $windows = IsWindows
    if ($windows)
    {
        $pingParamNoStop = "localhost -t"
        $pingParamStop = "localhost -n 2"
    }
    else
    {
        $pingParamNoStop = "localhost"
        $pingParamStop = "localhost -c 2"
    }

    AfterEach {
        Stop-Process -Name ping -ErrorAction SilentlyContinue
    }

    It "Should start a process without error" {
        { Start-Process ping } | Should Not Throw
    }

    It "Should process arguments without error" {
        { Start-Process ping -ArgumentList $pingParamNoStop} | Should Not Throw

        $process = Get-Process -Name ping

        $process.Length      | Should Be 1
        $process.Id          | Should BeGreaterThan 1
        $process.ProcessName | Should Be "ping"
    }

    It "Should create process object when used with PassThru argument" {
        $process = Start-Process ping -ArgumentList $pingParamNoStop -PassThru

        $process.Length      | Should Be 1
        $process.Id          | Should BeGreaterThan 1
        $process.ProcessName | Should Be "ping"
    }

    It "Should work correctly when used with full path name" {
        $process = Start-Process $pingCommand -ArgumentList $pingParamNoStop -PassThru

        $process.Length      | Should Be 1
        $process.Id          | Should BeGreaterThan 1
        $process.ProcessName | Should Be "ping"
    }

    It "Should invoke correct path when used with FilePath argument" {
        $process = Start-Process -FilePath $pingCommand -ArgumentList $pingParamNoStop -PassThru

        $process.Length      | Should Be 1
        $process.Id          | Should BeGreaterThan 1
        $process.ProcessName | Should Be "ping"
    }

    It "Should wait for command completion if used with Wait argument" {
        Start-Process ping -ArgumentList $pingParamStop -Wait

        $process = Get-Process -Name ping -ErrorAction SilentlyContinue

        $process.Length      | Should Be 0
    }

    It "Should work correctly with WorkingDirectory argument" {
        $process = Start-Process ping -WorkingDirectory $pingDirectory -ArgumentList $pingParamNoStop -PassThru

        $process.Length      | Should Be 1
        $process.Id          | Should BeGreaterThan 1
        $process.ProcessName | Should Be "ping"
    }

    It "Should should handle stderr redirection without error" {
        $process  = Start-Process ping -ArgumentList $pingParamNoStop -PassThru -RedirectStandardError $tempFile

        $process.Length      | Should Be 1
        $process.Id          | Should BeGreaterThan 1
        $process.ProcessName | Should Be "ping"
    }

    It "Should should handle stdout redirection without error" {
        $process  = Start-Process ping -ArgumentList $pingParamStop -Wait -RedirectStandardOutput $tempFile
        $dirEntry = dir $tempFile

	$dirEntry.Length     | Should BeGreaterThan 0
    }

    It "Should should handle stdin redirection without error" {
        $process  = Start-Process sort -Wait -RedirectStandardOutput $tempFile -RedirectStandardInput $assetsFile
        $dirEntry = dir $tempFile

	$dirEntry.Length     | Should BeGreaterThan 0
    }

}


