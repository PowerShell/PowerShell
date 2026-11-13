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
    Context "Set-Content should work with -Delimiter parameter" {
        BeforeAll {
            $testPath = "$TESTDRIVE/test.txt"
            $content = "a", "b", "c"
            $nestedContent = "a", @("b", "c", "d")
        }

        It "Should throw an exception if -Delimiter and -NoNewLine parameters are used together" {
            { $content | Set-Content -Path $testPath -Delimiter "`n" -NoNewLine -ErrorAction Stop } | Should -Throw -ErrorId 'GetContentWriterArgumentError'
        }

        It "Should throw an exception if -Delimiter and -AsByteStream parameters are used together" {
            { $content | Set-Content -Path $testPath -Delimiter "`n" -AsByteStream -ErrorAction Stop } | Should -Throw -ErrorId 'GetContentWriterArgumentError'
        }

        It "Should throw an exception if -NoTrailingDelimiter is used without -Delimiter parameter" {
            { $content | Set-Content -Path $testPath -NoTrailingDelimiter -ErrorAction Stop } | Should -Throw -ErrorId 'GetContentWriterArgumentError'
        }

        It "Should create file with newlines as delimiter" {
            $stringData = "a$([System.Environment]::NewLine)b$([System.Environment]::NewLine)c$([System.Environment]::NewLine)"

            $content | Set-Content -Path $testPath -Delimiter ([System.Environment]::NewLine)
            Get-Content -Path $testPath -Raw | Should -BeExactly $stringData

            Set-Content -Value $content -Path $testPath -Delimiter ([System.Environment]::NewLine)
            Get-Content -Path $testPath -Raw | Should -BeExactly $stringData
        }

        It "Should create file with newlines as delimiter and suppress final newline delimiter" {
            $stringData = "a$([System.Environment]::NewLine)b$([System.Environment]::NewLine)c"

            $content | Set-Content -Path $testPath -Delimiter ([System.Environment]::NewLine) -NoTrailingDelimiter
            Get-Content -Path $testPath -Raw | Should -BeExactly $stringData

            Set-Content -Value $content -Path $testPath -Delimiter ([System.Environment]::NewLine) -NoTrailingDelimiter
            Get-Content -Path $testPath -Raw | Should -BeExactly $stringData
        }

        It "Should create file with commas as delimiter" {
            $stringData = "a,b,c,"

            $content | Set-Content -Path $testPath -Delimiter ","
            Get-Content -Path $testPath -Raw | Should -BeExactly $stringData

            Set-Content -Value $content -Path $testPath -Delimiter ","
            Get-Content -Path $testPath -Raw | Should -BeExactly $stringData
        }

        It "Should create file with commas as delimiter and suppress final delimiter" {
            $stringData = "a,b,c"

            $content | Set-Content -Path $testPath -Delimiter "," -NoTrailingDelimiter
            Get-Content -Path $testPath -Raw | Should -BeExactly $stringData

            Set-Content -Value $content -Path $testPath -Delimiter "," -NoTrailingDelimiter
            Get-Content -Path $testPath -Raw | Should -BeExactly $stringData
        }

        It "Should create file with commas as delimiter using nested content" {
            $stringData = "a,b,c,d,"

            $nestedContent | Set-Content -Path $testPath -Delimiter ","
            Get-Content -Path $testPath -Raw | Should -BeExactly $stringData

            Set-Content -Value $nestedContent -Path $testPath -Delimiter ","
            Get-Content -Path $testPath -Raw | Should -BeExactly $stringData
        }

        It "Should create file with commas as delimiter using nested content and suppress final delimiter" {
            $stringData = "a,b,c,d"

            $nestedContent | Set-Content -Path $testPath -Delimiter "," -NoTrailingDelimiter
            Get-Content -Path $testPath -Raw | Should -BeExactly $stringData

            Set-Content -Value $nestedContent -Path $testPath -Delimiter "," -NoTrailingDelimiter
            Get-Content -Path $testPath -Raw | Should -BeExactly $stringData
        }

        AfterEach {
            if (Test-Path -Path $testPath) {
                Remove-Item -Path $testPath -Force
            }
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
