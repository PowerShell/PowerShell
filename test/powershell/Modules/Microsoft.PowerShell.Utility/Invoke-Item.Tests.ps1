using namespace System.Diagnostics

Describe "Invoke-Item basic tests" -Tags "CI" {
    BeforeAll {
        $isNanoOrIot = [System.Management.Automation.Platform]::IsNanoServer -or
                       [System.Management.Automation.Platform]::IsIoT

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

    It "Should invoke text file '<TestFile>' without error" -Skip:$isNanoOrIot -TestCases $textFileTestCases {
        param($TestFile)

        $old_pids = Get-Process | ForEach-Object Id
        Invoke-Item -Path $TestFile
        $new_pids = Get-Process | ForEach-Object Id

        $diff = @(Compare-Object $old_pids $new_pids)
        $diff.Count | Should Be 1
        $diff[0].SideIndicator | Should Be "=>"
        $proc_name = Get-Process -Id $diff[0].InputObject | ForEach-Object Name

        if ($IsLinux) {
            $proc_name | Should Be "xdg-open"
        } elseif ($IsOSX) {
            $proc_name | Should Be "open"
        }
        ## On Windows, the name would be the default application associated with the file extension.
        ## It varies depending on the setting, so we don't test the name for Windows.
    }

    It "Should invoke an executable file without error" {
        $executable, $procName = if ($IsLinux -or $IsOSX) {
            Get-Command "ifconfig" -CommandType Application | ForEach-Object Source
            "ifconfig"
        } else {
            Get-Command "ipconfig" -CommandType Application | ForEach-Object Source
            "ipconfig"
        }

        Invoke-Item -Path $executable
        $proc = Get-Process -Name $procName
        $proc | Should Not BeNullOrEmpty
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
                Start-Sleep -Milliseconds  100
                if (([Datetime]::Now - $startTime) -ge [timespan]"00:00:05") { throw "Timeout exception" }
            }
        } | Should Not throw
    }

    It "Should start a file without error on Windows full SKUs" -Skip:(-not $isFullWin) {
        Start-Process $testfilepath -Wait
        Test-Path $renamedtestfilepath | Should Be $true
    }
}
