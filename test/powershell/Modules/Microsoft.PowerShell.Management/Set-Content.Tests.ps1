# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Set-Content cmdlet tests" -Tags "CI" {
    BeforeAll {
        $file1 = "file1.txt"
        $filePath1 = Join-Path $testdrive $file1
        # if the registry doesn't exist, don't run those tests
        $skipRegistry = ! (Test-Path hklm:/)
    }

    It "A warning should be emitted if both -AsByteStream and -Encoding are used together" {
        $testfile = "${TESTDRIVE}\bfile.txt"
        "test" | Set-Content $testfile
        $result = Get-Content -AsByteStream -Encoding Unicode -Path $testfile -WarningVariable contentWarning *> $null
        $contentWarning.Message | Should -Match "-AsByteStream"
    }

    Context "Set-Content should create a file if it does not exist" {
        AfterEach {
          Remove-Item -Path $filePath1 -Force -ErrorAction SilentlyContinue
        }
        It "should create a file if it does not exist" {
            Set-Content -Path $filePath1 -Value "$file1"
            $result = Get-Content -Path $filePath1
            $result | Should -Be "$file1"
        }
    }
    Context "Set-Content/Get-Content should set/get the content of an exisiting file" {
        BeforeAll {
          New-Item -Path $filePath1 -ItemType File -Force
        }
        It "should set-Content of testdrive\$file1" {
            Set-Content -Path $filePath1 -Value "ExpectedContent"
            $result = Get-Content -Path $filePath1
            $result | Should -Be "ExpectedContent"
        }
        It "should return expected string from testdrive\$file1" {
            $result = Get-Content -Path $filePath1
            $result | Should -BeExactly "ExpectedContent"
        }
        It "should Set-Content to testdrive\dynamicfile.txt with dynamic parameters" {
            Set-Content -Path $testdrive\dynamicfile.txt -Value "ExpectedContent"
            $result = Get-Content -Path $testdrive\dynamicfile.txt
            $result | Should -BeExactly "ExpectedContent"
        }
        It "should return expected string from testdrive\dynamicfile.txt" {
            $result = Get-Content -Path $testdrive\dynamicfile.txt
            $result | Should -BeExactly "ExpectedContent"
        }
        It "should remove existing content from testdrive\$file1 when the -Value is `$null" {
            $AsItWas=Get-Content $filePath1
            $AsItWas | Should -BeExactly "ExpectedContent"
            Set-Content -Path $filePath1 -Value $null -ErrorAction Stop
            $AsItIs=Get-Content $filePath1
            $AsItIs | Should -Not -Be $AsItWas
        }
        It "should throw 'ParameterArgumentValidationErrorNullNotAllowed' when -Path is `$null" {
            { Set-Content -Path $null -Value "ShouldNotWorkBecausePathIsNull" -ErrorAction Stop } | Should -Throw -ErrorId "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.SetContentCommand"
        }
        It "should throw 'ParameterArgumentValidationErrorNullNotAllowed' when -Path is `$()" {
            { Set-Content -Path $() -Value "ShouldNotWorkBecausePathIsInvalid" -ErrorAction Stop } | Should -Throw -ErrorId "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.SetContentCommand"
        }
        It "should throw 'PSNotSupportedException' when you Set-Content to an unsupported provider" -Skip:$skipRegistry {
            { Set-Content -Path HKLM:\\software\\microsoft -Value "ShouldNotWorkBecausePathIsUnsupported" -ErrorAction Stop } | Should -Throw -ErrorId "NotSupported,Microsoft.PowerShell.Commands.SetContentCommand"
        }
        #[BugId(BugDatabase.WindowsOutOfBandReleases, 9058182)]
        It "should be able to pass multiple [string]`$objects to Set-Content through the pipeline to output a dynamic Path file" {
            "hello","world"|Set-Content $testdrive\dynamicfile2.txt
            $result=Get-Content $testdrive\dynamicfile2.txt
            $result.length | Should -Be 2
            $result[0]     | Should -BeExactly "hello"
            $result[1]     | Should -BeExactly "world"
        }
    }
    Context "Set-Content should work with alternate data streams on Windows" {
        BeforeAll {
            if ( -Not $IsWindows )
            {
                return
            }
            $altStreamPath = "$TESTDRIVE/altStream.txt"
            $altStreamPath2 = "$TESTDRIVE/altStream2.txt"
            $altStreamDirectory = "$TESTDRIVE/altstreamdir"
            $altStreamDirectory2 = "$TESTDRIVE/altstream2dir"
            $stringData = "test data"
            $streamName = "test"
            $absentStreamName = "noExist"
            $item = New-Item -type file $altStreamPath
            $altstreamdiritem = New-Item -type directory $altStreamDirectory
        }
        It "Should create a new data stream on a file" -Skip:(-Not $IsWindows) {
            Set-Content -Path $altStreamPath -Stream $streamName -Value $stringData
            Get-Content -Path $altStreamPath -Stream $streamName | Should -BeExactly $stringData
        }
        It "Should create a new data stream on a file using colon syntax" -Skip:(-Not $IsWindows) {
            Set-Content -Path ${altStreamPath2}:${streamName} -Value $stringData
            Get-Content -Path ${altStreamPath2} -Stream $streamName | Should -BeExactly $stringData
        }
        It "Should create a new data stream on a directory" -Skip:(-Not $IsWindows) {
            Set-Content -Path $altStreamDirectory -Stream $streamName -Value $stringData
            Get-Content -Path $altStreamDirectory -Stream $streamName | Should -BeExactly $stringData
        }
        It "Should create a new data stream on a directory using colon syntax" -Skip:(-Not $IsWindows) {
            Set-Content -Path ${altStreamDirectory2}:${streamName} -Value $stringData
            Get-Content -Path ${altStreamDirectory2} -Stream ${streamName} | Should -BeExactly $stringData
        }
    }

    Context "Set-Content -WhatIf should emit a single message" {
        BeforeAll {
            $whatIfFile = Join-Path $TESTDRIVE "whatif.txt"
            # Ensure file exists so Set-Content will perform truncate + write path
            Set-Content -Path $whatIfFile -Value "seed"
        }
        AfterAll {
            Remove-Item -Path $whatIfFile -Force -ErrorAction SilentlyContinue
        }
        It "should produce one WhatIf message when setting content" {
            $transcriptPath = Join-Path $TESTDRIVE 'whatif-transcript.txt'
            Start-Transcript -Path $transcriptPath -Force | Out-Null
            try {
                Set-Content -Path $whatIfFile -Value "new" -WhatIf
            }
            finally {
                Stop-Transcript | Out-Null
            }
            $text = Get-Content -Path $transcriptPath -Raw
            ($text -split "`r?`n") | Where-Object { $_ -match '^What if:' } | Measure-Object | Select-Object -ExpandProperty Count | Should -Be 1
        }
    }
}

Describe "Set-Content should work for PSDrive with UNC path as root" -Tags @('CI', 'RequireAdminOnWindows') {
    BeforeAll {
        $file1 = "file1.txt"
        #create a random folder
        $randomFolderName = "TestFolder_" + (Get-Random).ToString()
        $randomFolderPath = Join-Path $testdrive $randomFolderName
        $null = New-Item -Path $randomFolderPath -ItemType Directory -ErrorAction SilentlyContinue
    }
    # test is Pending due to https://github.com/PowerShell/PowerShell/issues/3883
    It "should create a file in a psdrive with UNC path as root" -Pending {
        try
        {
            # create share
            net share testshare=$randomFolderPath /grant:everyone,FULL
            New-PSDrive -Name Foo -Root \\localhost\testshare -PSProvider FileSystem
            Set-Content -Path Foo:\$file1 -Value "$file1"
            $result = Get-Content -Path Foo:\$file1
            $result | Should -BeExactly "$file1"
        }
        finally
        {
            Remove-PSDrive -Name Foo
            net share testshare /delete
        }
    }
}
