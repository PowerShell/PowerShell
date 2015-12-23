$here = Split-Path -Parent $MyInvocation.MyCommand.Path
. $here/Test-Common.ps1

Describe "Start-Process" {
    $pingCommand = (Get-Command -CommandType Application ping)[0].Definition
    $pingDirectory = Split-Path $pingCommand -Parent
    $tempFile = "$(GetTempDir)/PSTest"
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

    It "Should process arguments without error" {
        $process = Start-Process ping -ArgumentList $pingParamNoStop -PassThru

        $process.Length      | Should Be 1
        $process.Id          | Should BeGreaterThan 1
        $process.ProcessName | Should Be "ping"

        Stop-Process -Id $process.Id
    }

    It "Should work correctly when used with full path name" {
        $process = Start-Process $pingCommand -ArgumentList $pingParamNoStop -PassThru

        $process.Length      | Should Be 1
        $process.Id          | Should BeGreaterThan 1
        $process.ProcessName | Should Be "ping"

        Stop-Process -Id $process.Id
    }

    It "Should invoke correct path when used with FilePath argument" {
        $process = Start-Process -FilePath $pingCommand -ArgumentList $pingParamNoStop -PassThru

        $process.Length      | Should Be 1
        $process.Id          | Should BeGreaterThan 1
        $process.ProcessName | Should Be "ping"

        Stop-Process -Id $process.Id
    }

    It "Should wait for command completion if used with Wait argument" {
        $process = Start-Process ping -ArgumentList $pingParamStop -Wait -PassThru
        ( Get-Process -Id $process.Id -ErrorAction SilentlyContinue ) | Should BeNullOrEmpty
    }

    It "Should work correctly with WorkingDirectory argument" {
        $process = Start-Process ping -WorkingDirectory $pingDirectory -ArgumentList $pingParamNoStop -PassThru

        $process.Length      | Should Be 1
        $process.Id          | Should BeGreaterThan 1
        $process.ProcessName | Should Be "ping"

        Stop-Process -Id $process.Id
    }

    It "Should should handle stderr redirection without error" {
        $process = Start-Process ping -ArgumentList $pingParamStop -PassThru -RedirectStandardError $tempFile

        $process.Length      | Should Be 1
        $process.Id          | Should BeGreaterThan 1
        $process.ProcessName | Should Be "ping"
    }

    It "Should should handle stdout redirection without error" {
        $process = Start-Process ping -ArgumentList $pingParamStop -Wait -RedirectStandardOutput $tempFile
        $dirEntry = dir $tempFile
	$dirEntry.Length | Should BeGreaterThan 0
    }

    It "Should should handle stdin redirection without error" {
        $process = Start-Process sort -Wait -RedirectStandardOutput $tempFile -RedirectStandardInput $assetsFile
        $dirEntry = dir $tempFile
	$dirEntry.Length | Should BeGreaterThan 0
    }
}
