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
        $result = Get-Content -AsByteStream -Encoding Unicode -Path $testfile -WarningVariable contentWarning *>$null
        $contentWarning.Message | Should Match "-AsByteStream"
    }

    Context "Set-Content should create a file if it does not exist" {
        AfterEach {
          Remove-Item -Path $filePath1 -Force -ErrorAction SilentlyContinue
        }
        It "should create a file if it does not exist" {
            Set-Content -Path $filePath1 -Value "$file1"
            $result = Get-Content -Path $filePath1
            $result| Should be "$file1"
        }
    }
    Context "Set-Content/Get-Content should set/get the content of an exisiting file" {
        BeforeAll {
          New-Item -Path $filePath1 -ItemType File -Force
        }
        It "should set-Content of testdrive\$file1" {
            Set-Content -Path $filePath1 -Value "ExpectedContent"
            $result = Get-Content -Path $filePath1
            $result| Should be "ExpectedContent"
        }
        It "should return expected string from testdrive\$file1" {
            $result = Get-Content -Path $filePath1
            $result | Should BeExactly "ExpectedContent"
        }
        It "should Set-Content to testdrive\dynamicfile.txt with dynamic parameters" {
            Set-Content -Path $testdrive\dynamicfile.txt -Value "ExpectedContent"
            $result = Get-Content -Path $testdrive\dynamicfile.txt
            $result| Should BeExactly "ExpectedContent"
        }
        It "should return expected string from testdrive\dynamicfile.txt" {
            $result = Get-Content -Path $testdrive\dynamicfile.txt
            $result | Should BeExactly "ExpectedContent"
        }
        It "should remove existing content from testdrive\$file1 when the -Value is `$null" {
            $AsItWas=Get-Content $filePath1
            $AsItWas |Should BeExactly "ExpectedContent"
            Set-Content -Path $filePath1 -Value $null -EA stop
            $AsItIs=Get-Content $filePath1
            $AsItIs| Should Not Be $AsItWas
        }
        It "should throw 'ParameterArgumentValidationErrorNullNotAllowed' when -Path is `$null" {
            try {
                Set-Content -Path $null -Value "ShouldNotWorkBecausePathIsNull" -EA stop
                Throw "Previous statement unexpectedly succeeded..."
            }
            catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.SetContentCommand"
            }
        }
        It "should throw 'ParameterArgumentValidationErrorNullNotAllowed' when -Path is `$()" {
            try {
                Set-Content -Path $() -Value "ShouldNotWorkBecausePathIsInvalid" -EA stop
                Throw "Previous statement unexpectedly succeeded..."
            }
            catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.SetContentCommand"
            }
        }
        It "should throw 'PSNotSupportedException' when you Set-Content to an unsupported provider" -skip:$skipRegistry {
            try {
                Set-Content -Path HKLM:\\software\\microsoft -Value "ShouldNotWorkBecausePathIsUnsupported" -EA stop
                Throw "Previous statement unexpectedly succeeded..."
            }
            catch {
                $_.FullyQualifiedErrorId | Should Be "NotSupported,Microsoft.PowerShell.Commands.SetContentCommand"
            }
        }
        #[BugId(BugDatabase.WindowsOutOfBandReleases, 9058182)]
        It "should be able to pass multiple [string]`$objects to Set-Content through the pipeline to output a dynamic Path file" {
            "hello","world"|Set-Content $testdrive\dynamicfile2.txt
            $result=Get-Content $testdrive\dynamicfile2.txt
            $result.length |Should be 2
            $result[0]     |Should be "hello"
            $result[1]     |Should be "world"
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
            $result| Should be "$file1"
        }
        finally
        {
            Remove-PSDrive -Name Foo
            net share testshare /delete
        }
    }
}
