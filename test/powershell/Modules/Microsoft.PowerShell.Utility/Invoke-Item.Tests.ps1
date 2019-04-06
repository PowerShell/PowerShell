# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
using namespace System.Diagnostics

Describe "Invoke-Item basic tests" -Tags "Feature" {
    BeforeAll {
        $powershell = Join-Path $PSHOME -ChildPath pwsh

        $testFile1 = Join-Path -Path $TestDrive -ChildPath "text1.txt"
        New-Item -Path $testFile1 -ItemType File -Force > $null

        $testFolder = Join-Path -Path $TestDrive -ChildPath "My Folder"
        New-Item -Path $testFolder -ItemType Directory -Force > $null
        $testFile2 = Join-Path -Path $testFolder -ChildPath "text2.txt"
        New-Item -Path $testFile2 -ItemType File -Force > $null

        $textFileTestCases = @(
            @{ TestFile = $testFile1 },
            @{ TestFile = $testFile2 })
    }

    Context "Invoke a text file on Unix" {
        BeforeEach {
            $redirectErr = Join-Path -Path $TestDrive -ChildPath "error.txt"
        }

        AfterEach {
            Remove-Item -Path $redirectErr -Force -ErrorAction SilentlyContinue
        }

        ## Run this test only on macOS because redirecting stderr of 'xdg-open' results in weird behavior in our Linux CI,
        ## causing this test to fail or the build to not respond.
        It "Should invoke text file '<TestFile>' without error on Mac" -Skip:(!$IsMacOS) -TestCases $textFileTestCases {
            param($TestFile)

            $expectedTitle = Split-Path $TestFile -Leaf
            open -F -a TextEdit
            $beforeCount = [int]('tell application "TextEdit" to count of windows' | osascript)
            Invoke-Item -Path $TestFile
            $startTime = Get-Date
            $title = [String]::Empty
            while (((Get-Date) - $startTime).TotalSeconds -lt 30 -and ($title -ne $expectedTitle))
            {
                Start-Sleep -Milliseconds 100
                $title = 'tell application "TextEdit" to get name of front window' | osascript
            }
            $afterCount = [int]('tell application "TextEdit" to count of windows' | osascript)
            $afterCount | Should -Be ($beforeCount + 1)
            $title | Should -Be $expectedTitle
            "tell application ""TextEdit"" to close window ""$expectedTitle""" | osascript
            'tell application "TextEdit" to quit' | osascript
        }
    }

    It "Should invoke an executable file without error" {
        # In case there is a couple of ping executables, we take the first one.
        $ping = (Get-Command "ping" -CommandType Application | Select-Object -First 1).Source
        $redirectFile = Join-Path -Path $TestDrive -ChildPath "redirect2.txt"

        if ($IsWindows) {
            if ([System.Management.Automation.Platform]::IsNanoServer -or [System.Management.Automation.Platform]::IsIoT) {
                ## On headless SKUs, we use `UseShellExecute = false`
                ## 'ping.exe' on Windows writes out usage to stdout.
                & $powershell -noprofile -c "Invoke-Item '$ping'" > $redirectFile
                Get-Content $redirectFile -Raw | Should -Match "usage: ping"
            } else {
                ## On full desktop, we use `UseShellExecute = true` to align with Windows PowerShell
                $notepad = Get-Command "notepad.exe" -CommandType Application | ForEach-Object Source
                $notepadProcessName = "notepad"
                Get-Process -Name $notepadProcessName | Stop-Process -Force
                Invoke-Item -Path $notepad
                $notepadProcess = Get-Process -Name $notepadProcessName
                # we need BeIn because multiple notepad processes could be running
                $notepadProcess.Name | Should -BeIn $notepadProcessName
                Stop-Process -InputObject $notepadProcess
            }
        } else {
            ## On Unix, we use `UseShellExecute = false`
            ## 'ping' on Unix write out usage to stderr
            ## some ping show 'usage: ping' and others show 'ping:'
            & $powershell -noprofile -c "Invoke-Item '$ping'" 2> $redirectFile
            Get-Content $redirectFile -Raw | Should -Match "usage: ping|ping:"
        }
    }

    Context "Invoke a folder" {
        BeforeAll {
            $supportedEnvironment = $true
            if ($IsLinux)
            {
                $appFolder = "$HOME/.local/share/applications"
                if (Test-Path $appFolder)
                {
                    $mimeDefault = xdg-mime query default inode/directory
                    Remove-Item $HOME/InvokeItemTest.Success -Force -ErrorAction SilentlyContinue
                    Set-Content -Path "$appFolder/InvokeItemTest.desktop" -Force -Value @"
[Desktop Entry]
Version=1.0
Name=InvokeItemTest
Comment=Validate Invoke-Item for directory
Exec=/bin/sh -c 'echo %u > ~/InvokeItemTest.Success'
Icon=utilities-terminal
Terminal=true
Type=Application
Categories=Application;
"@
                    xdg-mime default InvokeItemTest.desktop inode/directory
                }
                else
                {
                    $supportedEnvironment = $false
                }
            }
        }

        AfterAll {
            if ($IsLinux -and $supportedEnvironment)
            {
                xdg-mime default $mimeDefault inode/directory
                Remove-Item $appFolder/InvokeItemTest.desktop -Force -ErrorAction SilentlyContinue
                Remove-Item $HOME/InvokeItemTest.Success -Force -ErrorAction SilentlyContinue
            }
        }

        It "Should invoke a folder without error" -Skip:(!$supportedEnvironment) {
            if ($IsWindows)
            {
                $shell = New-Object -ComObject "Shell.Application"
                $windows = $shell.Windows()

                $before = $windows.Count
                Invoke-Item -Path ~
                # may take time for explorer to open window
                Wait-UntilTrue -sb { $windows.Count -gt $before } -TimeoutInMilliseconds (10*1000) -IntervalInMilliseconds 100 > $null
                $after = $windows.Count

                $before + 1 | Should -Be $after
                $item = $windows.Item($after - 1)
                $item.LocationURL | Should -Match ((Resolve-Path ~) -replace '\\', '/')
                ## close the windows explorer
                $item.Quit()
            }
            elseif ($IsLinux)
            {
                # validate on Unix by reassociating default app for directories
                Invoke-Item -Path $PSHOME
                # may take time for handler to start
                Wait-FileToBePresent -File "$HOME/InvokeItemTest.Success" -TimeoutInSeconds 10 -IntervalInMilliseconds 100
                Get-Content $HOME/InvokeItemTest.Success | Should -Be $PSHOME
            }
            else
            {
                # validate on MacOS by using AppleScript
                $beforeCount = [int]('tell application "Finder" to count of windows' | osascript)
                Invoke-Item -Path $PSHOME
                $startTime = Get-Date
                $expectedTitle = Split-Path $PSHOME -Leaf
                $title = [String]::Empty
                while (((Get-Date) - $startTime).TotalSeconds -lt 10 -and ($title -ne $expectedTitle))
                {
                    Start-Sleep -Milliseconds 100
                    $title = 'tell application "Finder" to get name of front window' | osascript
                }
                $afterCount = [int]('tell application "Finder" to count of windows' | osascript)
                $afterCount | Should -Be ($beforeCount + 1)
                $title | Should -Be $expectedTitle
                'tell application "Finder" to close front window' | osascript
            }
        }
    }
}

Describe "Invoke-Item tests on Windows" -Tags "CI","RequireAdminOnWindows" {
    BeforeAll {
        $isNanoServer = [System.Management.Automation.Platform]::IsNanoServer
        $isIot = [System.Management.Automation.Platform]::IsIoT
        $isFullWin = $IsWindows -and !$isNanoServer -and !$isIot

        if ($isFullWin) {
            $testfilename = "testfile.!!testext!!"
            $testfilepath = Join-Path $TestDrive $testfilename
            $renamedtestfilename = "renamedtestfile.!!testext!!"
            $renamedtestfilepath = Join-Path $TestDrive $renamedtestfilename

            cmd.exe /c assoc .!!testext!!=!!testext!!.FileType | Out-Null
            cmd.exe /c ftype !!testext!!.FileType=cmd.exe /c rename $testfilepath $renamedtestfilename | Out-Null
        }
    }

    AfterAll {
        if ($IsWindows) {
            cmd.exe /c assoc !!testext!!=
            cmd.exe /c ftype !!testext!!.FileType=
        }
    }

    BeforeEach {
        New-Item $testfilepath -ItemType File | Out-Null
    }

    AfterEach {
        Remove-Item $testfilepath -ErrorAction SilentlyContinue
        Remove-Item $renamedtestfilepath -ErrorAction SilentlyContinue
    }

    It "Should invoke a file without error on Windows full SKUs" -Skip:(-not $isFullWin) {
        invoke-item $testfilepath
        # Waiting subprocess start and rename file
        {
            $startTime = [Datetime]::Now
            while (-not (test-path $renamedtestfilepath))
            {
                Start-Sleep -Milliseconds 100
                if (([Datetime]::Now - $startTime) -ge [timespan]"00:00:05") { throw "Timeout exception" }
            }
        } | Should -Not -throw
    }

    It "Should start a file without error on Windows full SKUs" -Skip:(-not $isFullWin) {
        Start-Process $testfilepath -Wait
        Test-Path $renamedtestfilepath | Should -BeTrue
    }
}
