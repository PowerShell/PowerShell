Describe "Start-Process" -Tags @("Feature") {

    BeforeAll {
        $isNanoServer = [System.Management.Automation.Platform]::IsNanoServer
        $isIot = [System.Management.Automation.Platform]::IsIoT
        $isFullWin = $IsWindows -and !$isNanoServer -and !$isIot

        $pingCommand = (Get-Command -CommandType Application ping)[0].Definition
        $pingDirectory = Split-Path $pingCommand -Parent
        $tempFile = Join-Path -Path $TestDrive -ChildPath PSTest
        $assetsFile = Join-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath assets) -ChildPath SortTest.txt
        if ($IsWindows) {
            $pingParam = "-n 2 localhost"
        }
        elseif ($IsLinux -Or $IsMacOS) {
	        $pingParam = "-c 2 localhost"
        }
    }

    # Note that ProcessName may still be `powershell` due to dotnet/corefx#5378
    # This has been fixed on Linux, but not on macOS

    It "Should process arguments without error" {
	    $process = Start-Process ping -ArgumentList $pingParam -PassThru -RedirectStandardOutput "$TESTDRIVE/output"

	    $process.Length      | Should Be 1
	    $process.Id          | Should BeGreaterThan 1
	    # $process.ProcessName | Should Be "ping"
    }

    It "Should work correctly when used with full path name" {
	    $process = Start-Process $pingCommand -ArgumentList $pingParam -PassThru -RedirectStandardOutput "$TESTDRIVE/output"

	    $process.Length      | Should Be 1
	    $process.Id          | Should BeGreaterThan 1
	    # $process.ProcessName | Should Be "ping"
    }

    It "Should invoke correct path when used with FilePath argument" {
	    $process = Start-Process -FilePath $pingCommand -ArgumentList $pingParam -PassThru -RedirectStandardOutput "$TESTDRIVE/output"

	    $process.Length      | Should Be 1
	    $process.Id          | Should BeGreaterThan 1
	    # $process.ProcessName | Should Be "ping"
    }

    It "Should wait for command completion if used with Wait argument" {
	    $process = Start-Process ping -ArgumentList $pingParam -Wait -PassThru -RedirectStandardOutput "$TESTDRIVE/output"
    }

    It "Should work correctly with WorkingDirectory argument" {
	    $process = Start-Process ping -WorkingDirectory $pingDirectory -ArgumentList $pingParam -PassThru -RedirectStandardOutput "$TESTDRIVE/output"

	    $process.Length      | Should Be 1
	    $process.Id          | Should BeGreaterThan 1
	    # $process.ProcessName | Should Be "ping"
    }

    It "Should handle stderr redirection without error" {
	    $process = Start-Process ping -ArgumentList $pingParam -PassThru -RedirectStandardError $tempFile -RedirectStandardOutput "$TESTDRIVE/output"

	    $process.Length      | Should Be 1
	    $process.Id          | Should BeGreaterThan 1
	    # $process.ProcessName | Should Be "ping"
    }

    It "Should handle stdout redirection without error" {
	    $process = Start-Process ping -ArgumentList $pingParam -Wait -RedirectStandardOutput $tempFile
	    $dirEntry = get-childitem $tempFile
	    $dirEntry.Length | Should BeGreaterThan 0
    }

    # Marking this test 'pending' to unblock daily builds. Filed issue : https://github.com/PowerShell/PowerShell/issues/2396
    It "Should handle stdin redirection without error" -Pending {
	    $process = Start-Process sort -Wait -RedirectStandardOutput $tempFile -RedirectStandardInput $assetsFile
	    $dirEntry = get-childitem $tempFile
	    $dirEntry.Length | Should BeGreaterThan 0
    }

    ## -Verb is supported in PowerShell core on Windows full desktop.
    It "Should give an error when -Verb parameter is used" -Skip:$isFullWin {
        { Start-Process -Verb runas -FilePath $pingCommand } | ShouldBeErrorId "NotSupportedException,Microsoft.PowerShell.Commands.StartProcessCommand"
    }

    ## -WindowStyle is supported in PowerShell core on Windows full desktop.
    It "Should give an error when -WindowStyle parameter is used" -Skip:$isFullWin {
        { Start-Process -FilePath $pingCommand -WindowStyle Normal } | ShouldBeErrorId "NotSupportedException,Microsoft.PowerShell.Commands.StartProcessCommand"
    }

    It "Should give an error when both -NoNewWindow and -WindowStyle are specified" -Skip:(!$isFullWin) {
        { Start-Process -FilePath $pingCommand -NoNewWindow -WindowStyle Normal -ErrorAction Stop } | ShouldBeErrorId "InvalidOperationException,Microsoft.PowerShell.Commands.StartProcessCommand"
    }

    It "Should start cmd.exe with Verb 'open' and WindowStyle 'Minimized'" -Skip:(!$isFullWin) {
        $fileToWrite = Join-Path $TestDrive "VerbTest.txt"
        $process = Start-Process cmd.exe -ArgumentList "/c echo abc > $fileToWrite" -Verb open -WindowStyle Minimized -PassThru
        $process.Name | Should Be "cmd"
        $process.WaitForExit()
        Test-Path $fileToWrite | Should Be $true
    }

    It "Should start notepad.exe with ShellExecute" -Skip:(!$isFullWin) {
        $process = Start-Process notepad -PassThru -WindowStyle Normal
        $process.Name | Should Be "notepad"
        $process | Stop-Process
    }

    It "Should be able to use the -WhatIf switch without performing the actual action" {
        $pingOutput = Join-Path $TestDrive "pingOutput.txt"
        { Start-Process -Wait $pingCommand -ArgumentList $pingParam -RedirectStandardOutput $pingOutput -WhatIf -ErrorAction Stop } | Should Not Throw
        $pingOutput | Should Not Exist 
    }

    It "Should return null when using -WhatIf switch with -PassThru" {
        Start-Process $pingCommand -ArgumentList $pingParam -PassThru -WhatIf | Should Be $null
   }
}

Describe "Start-Process -Timeout" -Tags "Feature","Slow" {

    BeforeAll {
        if ($IsWindows) {
            $pingParam = "-n 30 localhost"
        }
        elseif ($IsLinux -Or $IsMacOS) {
            $pingParam = "-c 30 localhost"
        }
    }

    It "Should work correctly if process completes before specified exit time-out" {
        Start-Process ping -ArgumentList $pingParam -ExitTimeout 40000 -RedirectStandardOutput "$TESTDRIVE/output" | Should Be $null
    }

    It "Should give an error when the specified exit time-out is exceeded" {
        { Start-Process ping -ArgumentList $pingParam -ExitTimeout 20000 -RedirectStandardOutput "$TESTDRIVE/output" } | ShouldBeErrorId "StartProcessExitTimeoutExceeded,Microsoft.PowerShell.Commands.StartProcessCommand"
    }

    It "Should use exit time-out value when both -ExitTimeout and -Wait are passed" {
        { Start-Process ping -ArgumentList $pingParam -ExitTimeout 20000 -Wait -RedirectStandardOutput "$TESTDRIVE/output" } | ShouldBeErrorId "StartProcessExitTimeoutExceeded,Microsoft.PowerShell.Commands.StartProcessCommand"
    }

    # This is based on the test "Should kill native process tree" in
    # test\powershell\Language\Scripting\NativeExecution\NativeCommandProcessor.Tests.ps1
    It "Should stop any descendant processes when the specified exit time-out is exceeded" {
        Get-Process testexe -ErrorAction SilentlyContinue | Stop-Process

        { Start-Process testexe -ArgumentList "-createchildprocess 6" -ExitTimeout 10000 -RedirectStandardOutput "$TESTDRIVE/output" } | ShouldBeErrorId "StartProcessExitTimeoutExceeded,Microsoft.PowerShell.Commands.StartProcessCommand"

        # Waiting for a second, as the $testexe processes may still be exiting
        # and the Get-Process cmdlet will count them accidentally
        Start-Sleep 1

        $childprocesses = Get-Process testexe -ErrorAction SilentlyContinue
        $childprocesses.count | Should Be 0
    }
}

Describe "Start-Process tests requiring admin" -Tags "Feature","RequireAdminOnWindows" {

    BeforeEach {
        cmd /c assoc .foo=foofile
        cmd /c ftype foofile=cmd /c echo %1^> $testdrive\foo.txt
        Remove-Item $testdrive\foo.txt -Force -ErrorAction SilentlyContinue
    }

    AfterEach {
        cmd /c assoc .foo=
        cmd /c ftype foofile=
    }

    It "Should open the application that is associated a file" -Skip:(!$isFullWin) {
        $fooFile = Join-Path $TestDrive "FooTest.foo"
        New-Item $fooFile -ItemType File -Force
        Start-Process $fooFile

        Wait-FileToBePresent -File "$testdrive\foo.txt" -TimeoutInSeconds 10 -IntervalInMilliseconds 100

        "$testdrive\foo.txt" | Should Exist
        Get-Content $testdrive\foo.txt | Should BeExactly $fooFile
    }
}
