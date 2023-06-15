# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'Native pipeline should have proper encoding' -tags 'CI' {
    It '$OutputEncoding should be set to UTF8 without BOM' {
        $OutputEncoding.BodyName | Should -Be "utf-8"
        $OutputEncoding.GetPreamble().Length | Should -Be 0
    }
}

Describe 'native commands with pipeline' -tags 'Feature' {

    BeforeAll {
        $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"
    }

    It "native | ps | native doesn't block" {
        $iss = [initialsessionstate]::CreateDefault2();
        $rs = [runspacefactory]::CreateRunspace($iss)
        $rs.Open()

        $ps = [powershell]::Create()
        $ps.Runspace = $rs

        $ps.AddScript("& $powershell -noprofile -command '100;
            Start-Sleep -Seconds 100' |
            ForEach-Object { if (`$_ -eq 100) { 'foo'; exit; }}").BeginInvoke()

        # waiting 30 seconds, because powershell startup time could be long on the slow machines,
        # such as CI
        Wait-UntilTrue { $rs.RunspaceAvailability -eq 'Available' } -timeout 30000 -interval 100 | Should -BeTrue

        $ps.Stop()
        $rs.ResetRunspaceState()
    }

    It "native | native | native should work fine" {

        if ($IsWindows) {
            $result = @(ping.exe | findstr.exe count | findstr.exe ping)
            $result[0] | Should -Match "Usage: ping"
        } else {
            $result = @(ps aux | grep pwsh | grep -v grep)
            $result[0] | Should -Match "pwsh"
        }
    }

    It 'native command should be killed when pipeline is disposed' -Skip:($IsWindows) {
        $yes = (Get-Process 'yes' -ErrorAction Ignore).Count
        yes | Select-Object -First 2
        # wait a little to be sure that the process is ended
        Start-Sleep -Milliseconds 500
        (Get-Process 'yes' -ErrorAction Ignore).Count | Should -Be $yes
    }

    It 'native command should still execute if the current working directory no longer exists with command: <command>' -Skip:($IsWindows) -TestCases @(
        @{ command = 'ps' }
        @{ command = 'start-process ps -nonewwindow'}
    ){
        param($command)

        $wd = New-Item testdrive:/tmp -ItemType directory
        $lock = New-Item testdrive:/lock -ItemType file
        $script = @"
            while (`$null -ne (Get-Item "$lock" -ErrorAction Ignore)) {
                Start-Sleep -Seconds 1
            }

            try {
                `$out = $command
            }
            catch {
                `$null = Set-Content -Path "$testdrive/error" -Value (`$_ | Out-String)
            }

            `$null = Set-Content -Path "$testdrive/out" -Value `$out
"@

        $pwsh = Start-Process -FilePath "${PSHOME}/pwsh" -WorkingDirectory $wd -ArgumentList @('-noprofile','-command',$script)

        Remove-Item -Path $wd -Force
        Remove-Item $lock
        $start = Get-Date

        try {
            while ($null -eq (Get-Item "$testdrive/error" -ErrorAction Ignore) -and $null -eq (Get-Item "$testdrive/out" -ErrorAction Ignore)) {
                if (((Get-Date) - $start).TotalSeconds -gt 60) {
                    throw "Timeout"
                }

                Start-Sleep -Seconds 1
            }
        }
        finally {
            $pwsh | Stop-Process -Force -ErrorAction Ignore
        }

        $err = Get-Item -Path "$testdrive/error" -ErrorAction Ignore
        $err | Should -BeNullOrEmpty -Because $err
        $out = Get-Item -Path "$testdrive/out" -ErrorAction Ignore
        $out | Should -Not -BeNullOrEmpty
    }
}

Describe "Native Command Processor" -tags "Feature" {

    # If powershell receives a StopProcessing, it should kill the native process and all child processes
    # this test should pass and no longer Pending when #2561 is fixed
    It "Should kill native process tree" -Pending {

        # make sure no test processes are running
        Get-Process testexe -ErrorAction SilentlyContinue | Stop-Process

        [int] $numToCreate = 2

        $ps = [PowerShell]::Create().AddCommand("testexe")
        $ps.AddArgument("-createchildprocess")
        $ps.AddArgument($numToCreate)
        $async = $ps.BeginInvoke()
        $ps.InvocationStateInfo.State | Should -Be "Running"

        [bool] $childrenCreated = $false
        while (-Not $childrenCreated)
        {
            $childprocesses = Get-Process testexe -ErrorAction SilentlyContinue
            if ($childprocesses.count -eq $numToCreate+1)
            {
                $childrenCreated = $true
            }
        }

        $startTime = Get-Date
        $beginsync = $ps.BeginStop($null, $async)
        # wait no more than 5 secs for the processes to be terminated, otherwise test has failed
        while (((Get-Date) - $startTime).TotalSeconds -lt 5)
        {
            if (($childprocesses.hasexited -eq $true).count -eq $numToCreate+1)
            {
                break
            }
        }
        $childprocesses = Get-Process testexe
        $count = $childprocesses.count
        $childprocesses | Stop-Process
        $count | Should -Be 0
    }

    It "Should not block running Windows executables" -Skip:(!$IsWindows -or !(Get-Command notepad.exe)) {
        if (Test-IsWindowsArm64) {
            Set-ItResult -Pending -Because "Needs investigation"
        }

        function FindNewNotepad
        {
            Get-Process -Name notepad -ErrorAction Ignore | Where-Object { $_.Id -NotIn $dontKill }
        }

        # We need to kill the windows process we start and can't know the process id, so get a list of
        # notepad processes already running and don't kill any of those.
        $dontKill = Get-Process -Name notepad -ErrorAction Ignore | ForEach-Object { $_.Id }

        try
        {
            $ps = [powershell]::Create().AddScript('notepad.exe; "ran notepad"')
            $async = $ps.BeginInvoke()

            # Wait for up to 30 seconds for either the pipeline to finish (should mean the test succeeded) or
            # for a new instance of notepad to have started (which mean we're blocked)
            $counter = 0
            while (!$async.AsyncWaitHandle.WaitOne(10000) -and $counter -lt 3 -and !(FindNewNotepad))
            {
                $counter++
            }

            # Stop the new instance of notepad
            $newNotepad = FindNewNotepad
            $newNotepad | Should -Not -BeNullOrEmpty
            $newNotepad | Stop-Process

            $async.IsCompleted | Should -BeTrue
            $ps.EndInvoke($async) | Should -Be "ran notepad"
        }
        finally
        {
            if (!$async.IsCompleted)
            {
                $ps.Stop()
            }
            $ps.Dispose()
        }
    }

    It "OutputEncoding should be used" -Skip:(!$IsWindows -or !(Get-Command sfc.exe)) {

        $originalOutputEncoding = [Console]::OutputEncoding
        try {
            [Console]::OutputEncoding = [System.Text.Encoding]::Unicode
            sfc | Out-String | Should -Not -Match "`0"
        }
        finally {
            [Console]::OutputEncoding = $originalOutputEncoding
        }
    }

    It '$ErrorActionPreference does not apply to redirected stderr output' {
        pwsh -noprofile -command '$ErrorActionPreference = ''Stop''; testexe -stderr stop 2>$null; ''hello''; $error; $?' | Should -BeExactly 'hello','True'
    }

    It 'Can start an elevated associated process correctly' -Skip:(
        !$IsWindows -or (!(Test-Path (Join-Path -Path $env:windir -ChildPath 'system32' -AdditionalChildPath 'diskmgmt.msc')))
    ) {
        # test bug https://github.com/PowerShell/PowerShell/issues/13744 where console is blocked
        diskmgmt.msc
        Wait-UntilTrue -sb { (Get-Process mmc).Count -gt 0 } -TimeoutInMilliseconds 5000 -IntervalInMilliseconds 1000 | Should -BeTrue
        Get-Process mmc | Stop-Process
    }
}

Describe "Open a text file with NativeCommandProcessor" -tags @("Feature", "RequireAdminOnWindows") {
    BeforeAll {
        if ($IsWindows) {
            $TestFile = Join-Path -Path $TestDrive -ChildPath "TextFileTest.foo"
        } else {
            $TestFile = Join-Path -Path $TestDrive -ChildPath "TextFileTest.txt"
        }
        Set-Content -Path $TestFile -Value "Hello" -Force
        $supportedEnvironment = $true

        if ($IsLinux) {
            $appFolder = "$HOME/.local/share/applications"
            $supportedEnvironment = Test-Path $appFolder
            if ($supportedEnvironment) {
                $mimeDefault = xdg-mime query default text/plain
                Remove-Item $HOME/nativeCommandProcessor.Success -Force -ErrorAction SilentlyContinue
                Set-Content -Path "$appFolder/nativeCommandProcessor.desktop" -Force -Value @"
[Desktop Entry]
Version=1.0
Name=nativeCommandProcessor
Comment=Validate_native_command_processor_open_text_file
Exec=/bin/sh -c 'echo %u > ~/nativeCommandProcessor.Success'
Icon=utilities-terminal
Terminal=true
Type=Application
Categories=Application;
"@
                xdg-mime default nativeCommandProcessor.desktop text/plain
            }
        }
        elseif ($IsWindows) {
            $supportedEnvironment = [System.Management.Automation.Platform]::IsWindowsDesktop
            if ($supportedEnvironment) {
                cmd /c assoc .foo=foofile
                cmd /c ftype foofile=cmd /c echo %1^> $TestDrive\foo.txt
                Remove-Item $TestDrive\foo.txt -Force -ErrorAction SilentlyContinue
            }
        }
    }

    AfterAll {
        Remove-Item -Path $TestFile -Force -ErrorAction SilentlyContinue

        if ($IsLinux -and $supportedEnvironment) {
            xdg-mime default $mimeDefault text/plain
            Remove-Item $appFolder/nativeCommandProcessor.desktop -Force -ErrorAction SilentlyContinue
            Remove-Item $HOME/nativeCommandProcessor.Success -Force -ErrorAction SilentlyContinue
        }
        elseif ($IsWindows -and $supportedEnvironment) {
            cmd /c assoc .foo=
            cmd /c ftype foofile=
        }
    }

    It "Should open text file without error" -Skip:(!$supportedEnvironment) {
        if ($IsMacOS) {
            Set-TestInconclusive -Message "AppleScript is not currently reliable on Az Pipelines"
            $expectedTitle = Split-Path $TestFile -Leaf
            open -F -a TextEdit
            $beforeCount = [int]('tell application "TextEdit" to count of windows' | osascript)
            & $TestFile
            $startTime = Get-Date
            $title = [String]::Empty
            while (((Get-Date) - $startTime).TotalSeconds -lt 30 -and ($title -ne $expectedTitle)) {
                Start-Sleep -Milliseconds 100
                $title = 'tell application "TextEdit" to get name of front window' | osascript
            }
            $afterCount = [int]('tell application "TextEdit" to count of windows' | osascript)
            $afterCount | Should -Be ($beforeCount + 1)
            $title | Should -BeExactly $expectedTitle
            "tell application ""TextEdit"" to close window ""$expectedTitle""" | osascript
            'tell application "TextEdit" to quit' | osascript
        }
        elseif ($IsLinux) {
            # Validate on Linux by reassociating default app for text file
            & $TestFile
            # It may take time for handler to start
            Wait-FileToBePresent -File "$HOME/nativeCommandProcessor.Success" -TimeoutInSeconds 10 -IntervalInMilliseconds 100 | Should -BeTrue
            Get-Content $HOME/nativeCommandProcessor.Success | Should -BeExactly $TestFile
        }
        else {
            & $TestFile
            Wait-FileToBePresent -File $TestDrive\foo.txt -TimeoutInSeconds 10 -IntervalInMilliseconds 100 | Should -BeTrue
            Get-Content $TestDrive\foo.txt | Should -BeExactly $TestFile
        }
    }

    It "Opening a file with an unregistered extension on Windows should fail" -Skip:(!$IsWindows) {
        { $dllFile = "$PSHOME\System.Management.Automation.dll"; & $dllFile } | Should -Throw -ErrorId "NativeCommandFailed"
    }
}

Describe "Run native command from a mounted FAT-format VHD" -tags @("Feature", "RequireAdminOnWindows") {
    BeforeAll {
        if (-not $IsWindows) {
            return;
        }
        else {
            $storageModule = Get-Module -Name 'Storage' -ListAvailable -ErrorAction SilentlyContinue

            if (-not $storageModule) {
                Write-Verbose -Verbose "Storage module is not available."
                return;
            }
        }

        $vhdx = Join-Path -Path $TestDrive -ChildPath ncp.vhdx

        if (Test-Path -Path $vhdx) {
            Remove-item -Path $vhdx -Force
        }

        $create_vhdx = Join-Path -Path $TestDrive -ChildPath 'create_vhdx.txt'

        Set-Content -Path $create_vhdx -Force -Value @"
            create vdisk file="$vhdx" maximum=20 type=fixed
            select vdisk file="$vhdx"
            attach vdisk
            convert mbr
            create partition primary
            format fs=fat
            assign letter="T"
            detach vdisk
"@

        diskpart.exe /s $create_vhdx
        Mount-DiskImage -ImagePath $vhdx > $null

        Copy-Item "$env:WinDir\System32\whoami.exe" "T:\whoami.exe"
    }

    AfterAll {
        if ($IsWindows) {
            $storageModule = Get-Module -Name 'Storage' -ListAvailable -ErrorAction SilentlyContinue

            if (-not $storageModule) {
                Write-Verbose -Verbose "Storage module is not available."
                return;
            }

            Dismount-DiskImage -ImagePath $vhdx
            Remove-Item $vhdx, $create_vhdx -Force
        }
    }

    It "Should run 'whoami.exe' from FAT file system without error" -Skip:(!$IsWindows) {
        $expected = & "$env:WinDir\System32\whoami.exe"
        $result = T:\whoami.exe
        $result | Should -BeExactly $expected
    }
}

Describe "Native application invocation and getting cursor position" -Tags 'CI' {
    It "Invoking a native application should not collect the cursor position" -Skip:($IsWindows) {
        $expectCmd = Get-Command expect -Type Application -ErrorAction Ignore
        $dateCmd = Get-Command date -Type Application -ErrorAction Ignore
        # if date or expect are missing mark the test as pending
        # test setup will need to ensure that these programs are present.
        $missing = @()
        if ($null -eq $expectCmd) {
            $missing += "expect"
        }
        if ($null -eq $dateCmd) {
            $missing += "date"
        }
        if ($missing.count -ne 0) {
            $message = "missing command(s) {0}" -f ($missing -join ", ")
            Set-ItResult -Pending $message
        }

        $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"
        $commandString = "spawn $powershell -nopro -c /bin/date; expect eof"
        [string]$result = expect -c $commandString
        $result.IndexOf("`e[6n") | Should -Be -1 -Because $result.replace("`e","``e").replace("`u{7}","<BELL>")
    }
}
