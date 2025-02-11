# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Start-Process" -Tag "Feature","RequireAdminOnWindows" {

    BeforeAll {
        $isNanoServer = [System.Management.Automation.Platform]::IsNanoServer
        $isIot = [System.Management.Automation.Platform]::IsIoT
        $isFullWin = $IsWindows -and !$isNanoServer -and !$isIot
        $extraArgs = @{}
        if ($isFullWin) {
            $extraArgs.WindowStyle = "Hidden"
        }

        $pingCommand = (Get-Command -CommandType Application ping)[0].Definition
        $pingDirectory = Split-Path $pingCommand -Parent
        $tempFile = Join-Path -Path $TestDrive -ChildPath PSTest
        $tempDirectory = Join-Path -Path $TestDrive -ChildPath 'PSPath[]'
        New-Item $tempDirectory -ItemType Directory  -Force
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
        $process = Start-Process ping -ArgumentList $pingParam -PassThru -RedirectStandardOutput "$TESTDRIVE/output" @extraArgs

        $process.Length      | Should -Be 1
        $process.Id          | Should -BeGreaterThan 1
        # $process.ProcessName | Should -Be "ping"
    }

    It "Should work correctly when used with full path name" {
        $process = Start-Process $pingCommand -ArgumentList $pingParam -PassThru -RedirectStandardOutput "$TESTDRIVE/output"  @extraArgs

        $process.Length      | Should -Be 1
        $process.Id          | Should -BeGreaterThan 1
        # $process.ProcessName | Should -Be "ping"
    }

    It "Should invoke correct path when used with FilePath argument" {
        $process = Start-Process -FilePath $pingCommand -ArgumentList $pingParam -PassThru -RedirectStandardOutput "$TESTDRIVE/output" @extraArgs

        $process.Length      | Should -Be 1
        $process.Id          | Should -BeGreaterThan 1
        # $process.ProcessName | Should -Be "ping"
    }

    It "Should invoke correct path when used with Path alias argument" {
        $process = Start-Process -Path $pingCommand -ArgumentList $pingParam -PassThru -RedirectStandardOutput "$TESTDRIVE/output" @extraArgs

        $process.Length | Should -Be 1
        $process.Id     | Should -BeGreaterThan 1
    }

    It "Should wait for command completion if used with Wait argument" {
        $process = Start-Process ping -ArgumentList $pingParam -Wait -PassThru -RedirectStandardOutput "$TESTDRIVE/output" @extraArgs
    }

    It "Should work correctly with WorkingDirectory argument" {
        $process = Start-Process ping -WorkingDirectory $pingDirectory -ArgumentList $pingParam -PassThru -RedirectStandardOutput "$TESTDRIVE/output" @extraArgs

        $process.Length      | Should -Be 1
        $process.Id          | Should -BeGreaterThan 1
        # $process.ProcessName | Should -Be "ping"
    }

    It "Should work correctly within an unspecified WorkingDirectory with wildcard-type characters" {
        Push-Location -LiteralPath $tempDirectory
        $process = Start-Process ping -ArgumentList $pingParam -PassThru -RedirectStandardOutput "$TESTDRIVE/output" @extraArgs
        $process.Length      | Should -Be 1
        $process.Id          | Should -BeGreaterThan 1
        # $process.ProcessName | Should -Be "ping"
        Pop-Location
    }

    It "Should handle stderr redirection without error" {
        $process = Start-Process ping -ArgumentList $pingParam -PassThru -RedirectStandardError $tempFile -RedirectStandardOutput "$TESTDRIVE/output"  @extraArgs

        $process.Length      | Should -Be 1
        $process.Id          | Should -BeGreaterThan 1
        # $process.ProcessName | Should -Be "ping"
    }

    It "Should handle stdout redirection without error" {
        $process = Start-Process ping -ArgumentList $pingParam -Wait -RedirectStandardOutput $tempFile  @extraArgs
        $dirEntry = Get-ChildItem $tempFile
        $dirEntry.Length | Should -BeGreaterThan 0
    }

    It "Should handle stdin redirection without error" {
        $process = Start-Process sort -Wait -RedirectStandardOutput $tempFile -RedirectStandardInput $assetsFile  @extraArgs
        $dirEntry = Get-ChildItem $tempFile
        $dirEntry.Length | Should -BeGreaterThan 0
    }

    ## -Verb is supported in PowerShell on Windows full desktop.
    It "Should give an error when -Verb parameter is used" -Skip:$isFullWin {
        { Start-Process -Verb runas -FilePath $pingCommand } | Should -Throw -ErrorId "NotSupportedException,Microsoft.PowerShell.Commands.StartProcessCommand"
    }

    ## -WindowStyle is supported in PowerShell on Windows full desktop.
    It "Should give an error when -WindowStyle parameter is used" -Skip:$isFullWin {
        { Start-Process -FilePath $pingCommand -WindowStyle Normal } | Should -Throw -ErrorId "NotSupportedException,Microsoft.PowerShell.Commands.StartProcessCommand"
    }

    It "Should give an error when both -NoNewWindow and -WindowStyle are specified" -Skip:(!$isFullWin) {
        { Start-Process -FilePath $pingCommand -NoNewWindow -WindowStyle Normal -ErrorAction Stop } | Should -Throw -ErrorId "InvalidOperationException,Microsoft.PowerShell.Commands.StartProcessCommand"
    }

    It "ExitCode returns with -NoNewWindow, -PassThru and -Wait" {
        $process = Start-Process -FilePath $pingCommand -ArgumentList $pingParam -NoNewWindow -PassThru -Wait -ErrorAction Stop
        $process.ExitCode | Should -Be 0
    }

    It "Should start cmd.exe with Verb 'open' and WindowStyle 'Minimized'" -Skip:(!$isFullWin) {
        $fileToWrite = Join-Path $TestDrive "VerbTest.txt"
        $process = Start-Process cmd.exe -ArgumentList "/c echo abc > $fileToWrite" -Verb open -WindowStyle Minimized -PassThru
        $process.Name | Should -Be "cmd"
        $process.WaitForExit()
        Test-Path $fileToWrite | Should -BeTrue
    }

    It "Should start notepad.exe with ShellExecute" -Skip:(!$isFullWin) {
        $process = Start-Process notepad.exe -PassThru -WindowStyle Normal
        $process.Name | Should -Be "notepad"
        $process | Stop-Process
    }

    It "Should be able to use the -WhatIf switch without performing the actual action" {
        $pingOutput = Join-Path $TestDrive "pingOutput.txt"
        { Start-Process -Wait $pingCommand -ArgumentList $pingParam -RedirectStandardOutput $pingOutput -WhatIf -ErrorAction Stop  @extraArgs} | Should -Not -Throw
        $pingOutput | Should -Not -Exist
    }

    It "Should return null when using -WhatIf switch with -PassThru" {
        Start-Process $pingCommand -ArgumentList $pingParam -PassThru -WhatIf | Should -BeNullOrEmpty
    }

    It 'Should run without errors when -ArgumentList is $null' {
         $process = Start-Process $pingCommand -ArgumentList $null -PassThru @extraArgs
         $process.Length      | Should -Be 1
         $process.Id          | Should -BeGreaterThan 1
    }

    It "Should run without errors when -ArgumentList is @()" {
        $process = Start-Process $pingCommand -ArgumentList @() -PassThru @extraArgs
        $process.Length      | Should -Be 1
        $process.Id          | Should -BeGreaterThan 1
    }

    It "Should run without errors when -ArgumentList is ''" {
        $process = Start-Process $pingCommand -ArgumentList '' -PassThru @extraArgs
        $process.Length      | Should -Be 1
        $process.Id          | Should -BeGreaterThan 1
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

        Wait-FileToBePresent -File "$testdrive\foo.txt" -TimeoutInSeconds 10 -IntervalInMilliseconds 100 | Should -BeTrue
        Get-Content $testdrive\foo.txt | Should -BeExactly $fooFile
    }
}

Describe "Environment Tests" -Tags "Feature" {

    It "UseNewEnvironment parameter should reset environment variables for child process" {

        $PWSH = (Get-Process -Id $PID).MainModule.FileName
        $outputFile = Join-Path -Path $TestDrive -ChildPath output.txt

        $env:TestEnvVariable | Should -BeNullOrEmpty

        $env:TestEnvVariable = 1
        $userName = $env:USERNAME

        try {
            Start-Process $PWSH -ArgumentList '-NoProfile','-Command Write-Output \"$($env:TestEnvVariable);$($env:USERNAME)\"' -RedirectStandardOutput $outputFile -Wait
            Get-Content -LiteralPath $outputFile | Should -BeExactly "1;$userName"

            # Check that:
            # 1. Environment variables is resetted (TestEnvVariable is removed)
            # 2. Environment variables comes from current user profile
            Start-Process $PWSH -ArgumentList '-NoProfile','-Command Write-Output \"$($env:TestEnvVariable);$($env:USERNAME)\"' -RedirectStandardOutput $outputFile -Wait -UseNewEnvironment
            Get-Content -LiteralPath $outputFile | Should -BeExactly ";$userName"
        } finally {
            $env:TestEnvVariable = $null
        }
    }

    It '-Environment adds or replaces environment variables to child process' {
        $outputfile = Join-Path -Path $TestDrive -ChildPath output.txt
        Start-Process pwsh -ArgumentList '-NoProfile','-Nologo','-OutputFormat xml','-Command get-childitem env:' -Wait -Environment @{ a = 1; B = 'hello'; TERM = 'dumb'; PATH = 'mine' } -RedirectStandardOutput $outputfile
        $out = Import-Clixml $outputfile
        ($out | Where-Object { $_.Name -eq 'a' }).Value | Should -Be 1
        ($out | Where-Object { $_.Name -eq 'B' }).Value | Should -BeExactly 'hello'
        ($out | Where-Object { $_.Name -eq 'TERM' }).Value | Should -BeExactly 'dumb'
        $pathSeparator = [System.IO.Path]::PathSeparator
        if ($IsWindows) {
            ($out | Where-Object { $_.Name -eq 'PATH' }).Value | Should -BeLike "*${pathSeparator}mine${pathSeparator}*"
        } else {
            ($out | Where-Object { $_.Name -eq 'PATH' }).Value | Should -BeLike "*${pathSeparator}mine"
        }
    }

    It '-Environment can remove an environment variable from child process' {
        try {
            $env:existing = 1 # set a variable that we will remove
            $env:nonexisting = $null # validate that removing a non-existing variable is a no-op
            $outputfile = Join-Path -Path $TestDrive -ChildPath output.txt
            Start-Process pwsh -ArgumentList '-NoProfile','-Nologo','-OutputFormat xml','-Command get-childitem env:' -Wait -Environment @{ existing = $null; nonexisting = $null } -RedirectStandardOutput $outputfile
            $out = Import-Clixml $outputfile
            $out | Where-Object { $_.Name -eq 'existing' } | Should -BeNullOrEmpty
            $out | Where-Object { $_.Name -eq 'nonexisting' } | Should -BeNullOrEmpty
        } finally {
            $env:existing = $null
        }
    }
}
