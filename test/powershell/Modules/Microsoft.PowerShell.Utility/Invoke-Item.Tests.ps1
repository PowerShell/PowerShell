# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
using namespace System.Diagnostics

function Invoke-AppleScript
{
    param(
        [string]$Script,
        [switch]$PassThru
    )

    Write-Verbose "running applescript: $Script"

    $result = $Script | osascript
    if($PassThru.IsPresent)
    {
        return $result
    }
}

function Get-WindowCountMacOS {
    param(
        [string]$Name
    )

    $processCount = @(Get-Process $Name -ErrorAction Ignore).Count

    if($processCount -eq 0)
    {
        return 0
    }

    $title = Get-WindowsTitleMacOS -name $Name

    if(!$title)
    {
        return 0
    }

    $windowCount = [int](Invoke-AppleScript -Script ('tell application "{0}" to count of windows' -f $Name) -PassThru)
    return $windowCount
}

function Get-WindowsTitleMacOS {
    param(
        [string]$Name
    )

    return Invoke-AppleScript -Script ('tell application "{0}" to get name of front window' -f $Name) -PassThru
}

function Stop-ProcessMacOS {
    param(
        [string]$Name,
        [switch]$QuitFirst
    )

    if($QuitFirst.IsPresent)
    {
        Invoke-AppleScript -Script ('tell application "{0}" to quit' -f $Name)
    }

    Get-Process -Name $Name -ErrorAction Ignore | Stop-Process -Force
}

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
            @{ TestFile = $testFile1; Name='file in root' },
            @{ TestFile = $testFile2; Name='file in subDirectory' })
    }

    Context "Invoke a text file on Unix" {
        BeforeEach {
            $redirectErr = Join-Path -Path $TestDrive -ChildPath "error.txt"

            if($IsMacOS)
            {
                Stop-ProcessMacOs -Name TextEdit -QuitFirst
            }
        }

        AfterEach {
            Remove-Item -Path $redirectErr -Force -ErrorAction SilentlyContinue

        }

        AfterAll{
            if($IsMacOS)
            {
                Stop-ProcessMacOs -Name TextEdit
            }
        }

        ## Run this test only on macOS because redirecting stderr of 'xdg-open' results in weird behavior in our Linux CI,
        ## causing this test to fail or the build to not respond.
        It "Should invoke text file '<Name>' without error on Mac" -Pending -TestCases $textFileTestCases {
            param($TestFile)

            $expectedTitle = Split-Path $TestFile -Leaf
            open -F -a TextEdit
            $beforeCount = Get-WindowCountMacOS -Name TextEdit
            Invoke-Item -Path $TestFile
            $startTime = Get-Date
            $title = [String]::Empty
            while (((Get-Date) - $startTime).TotalSeconds -lt 30 -and ($title -ne $expectedTitle))
            {
                Start-Sleep -Milliseconds 100
                $title = Get-WindowsTitleMacOS -Name TextEdit
            }
            $afterCount = Get-WindowCountMacOS -Name TextEdit
            $afterCount | Should -Be ($beforeCount + 1) -Because "There should be one more 'textEdit' windows open than when the tests started and there was $beforeCount"
            $title | Should -Be $expectedTitle
            Invoke-AppleScript -Script ('tell application "{0}" to close window "{1}"' -f 'TextEdit', $expectedTitle)
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
        }
        if ($IsFreeBSD) {
            & $powershell -noprofile -c "Invoke-Item '$ping'" 2> $redirectFile
            Get-Content $redirectFile -Raw | Should -Match "usage:"
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

        BeforeEach {

            if($IsMacOS)
            {
                Get-Process -Name Finder | Stop-Process -Force
            }
        }

        AfterAll{
            if($IsMacOS)
            {
                Stop-ProcessMacOs -Name Finder
            }
        }


        It "Should invoke a folder without error" -Skip:(!$supportedEnvironment) {
            if ($IsWindows)
            {
                if (Test-IsWindowsArm64) {
                    Set-ItResult -Pending -Because "Shell.Application errors with COMException: The server process could not be started because the configured identity is incorrect. Check the username and password."
                }

                $shell = New-Object -ComObject "Shell.Application"
                $windows = $shell.Windows()

                $before = $windows.Count
                Invoke-Item -Path ~
                # may take time for explorer to open window
                Wait-UntilTrue -sb { $windows.Count -gt $before } -TimeoutInMilliseconds (10*1000) -IntervalInMilliseconds 100 | Should -BeTrue
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
                Wait-FileToBePresent -File "$HOME/InvokeItemTest.Success" -TimeoutInSeconds 10 -IntervalInMilliseconds 100 | Should -BeTrue
                Get-Content $HOME/InvokeItemTest.Success | Should -Be $PSHOME
            }
            elseif ($IsFreeBSD)
            {
                Set-TestInconclusive -Message "This test can not be handled on FreeBSD at this time"
            }
            else
            {
                Set-TestInconclusive -Message "AppleScript is not currently reliable on Az Pipelines"
                # validate on MacOS by using AppleScript
                $beforeCount = Get-WindowCountMacOS -Name Finder
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
        Invoke-Item $testfilepath
        # Waiting subprocess start and rename file
        {
            $startTime = [Datetime]::Now
            while (-not (Test-Path $renamedtestfilepath))
            {
                Start-Sleep -Milliseconds 100
                if (([Datetime]::Now - $startTime) -ge [timespan]"00:00:05") { throw "Timeout exception" }
            }
        } | Should -Not -Throw
    }

    It "Should start a file without error on Windows full SKUs" -Skip:(-not $isFullWin) {
        Start-Process $testfilepath -Wait
        Test-Path $renamedtestfilepath | Should -BeTrue
    }
}
