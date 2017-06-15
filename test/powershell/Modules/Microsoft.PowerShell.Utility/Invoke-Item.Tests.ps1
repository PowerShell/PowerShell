using namespace System.Diagnostics

Describe "Invoke-Item basic tests" -Tags "CI" {
    BeforeAll {
        $powershell = Join-Path $PSHOME -ChildPath powershell

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

        ## Run this test only on OSX because redirecting stderr of 'xdg-open' results in weird behavior in our Linux CI,
        ## causing this test to fail or the build to hang.
        It "Should invoke text file '<TestFile>' without error" -Skip:(!$IsOSX) -TestCases $textFileTestCases {
            param($TestFile)

            ## Redirect stderr to a file. So if 'open' failed to open the text file, an error
            ## message from 'open' would be written to the redirection file.
            $proc = Start-Process -FilePath $powershell -ArgumentList "-noprofile -c Invoke-Item '$TestFile'" `
                                  -RedirectStandardError $redirectErr `
                                  -PassThru
            $proc.WaitForExit(3000) > $null
            if (!$proc.HasExited) {
                try { $proc.Kill() } catch { }
            }
            ## If the text file was successfully opened, the redirection file should be empty since no error
            ## message was written to it.
            Get-Content $redirectErr -Raw | Should BeNullOrEmpty
        }
    }

    It "Should invoke an executable file without error" {
        $executable = Get-Command "ping" -CommandType Application | ForEach-Object Source
        $redirectFile = Join-Path -Path $TestDrive -ChildPath "redirect2.txt"

        if ($IsWindows) {
            ## 'ping.exe' on Windows writes out usage to stdout.
            & $powershell -noprofile -c "Invoke-Item '$executable'" > $redirectFile
        } else {
            ## 'ping' on Unix write out usage to stderr
            & $powershell -noprofile -c "Invoke-Item '$executable'" 2> $redirectFile
        }
        Get-Content $redirectFile -Raw | Should Match "usage: ping"
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
