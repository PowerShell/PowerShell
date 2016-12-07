Describe "Set-Content cmdlet tests" -Tags "CI" {
    BeforeAll {
        $file1 = "file1.txt"
        $filePath1 = join-path $testdrive $file1
        # if the registry doesn't exist, don't run those tests
        $skipRegistry = ! (test-path hklm:/)
    }
    Context "Set-Content should create a file if it does not exist" {
        AfterEach {
          Remove-Item -path $filePath1 -Force -ErrorAction SilentlyContinue
        }
        It "should create a file if it does not exist" {
            set-content -path $filePath1 -value "$file1"
            $result = Get-Content -path $filePath1
            $result| Should be "$file1"
        }
    }
    Context "Set-Content/Get-Content should set/get the content of an exisiting file" {
        BeforeAll {
          New-Item -Path $filePath1 -ItemType File -Force
        }
        It "should set-Content of testdrive\$file1" {
            set-content -path $filePath1 -value "ExpectedContent"
            $result = Get-Content -path $filePath1
            $result| Should be "ExpectedContent"
        }
        It "should return expected string from testdrive\$file1" {
            $result = get-content -path $filePath1
            $result | Should BeExactly "ExpectedContent"
        }
        It "should Set-Content to testdrive\dynamicfile.txt with dynamic parameters" {
            set-content -path $testdrive\dynamicfile.txt -value "ExpectedContent"
            $result = Get-Content -path $testdrive\dynamicfile.txt
            $result| Should BeExactly "ExpectedContent"
        }
        It "should return expected string from testdrive\dynamicfile.txt" {
            $result = get-content -path $testdrive\dynamicfile.txt
            $result | Should BeExactly "ExpectedContent"
        }
        It "should remove existing content from testdrive\$file1 when the -Value is `$null" {
            $AsItWas=get-content $filePath1
            $AsItWas |Should BeExactly "ExpectedContent"
            set-content -path $filePath1 -value $null -ea stop
            $AsItIs=get-content $filePath1
            $AsItIs| Should Not Be $AsItWas
        }
        It "should throw 'ParameterArgumentValidationErrorNullNotAllowed' when -Path is `$null" {
            try {
                set-content -path $null -value "ShouldNotWorkBecausePathIsNull" -ea stop
                Throw "Previous statement unexpectedly succeeded..."
            }
            catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.SetContentCommand"
            }
        }
        It "should throw 'ParameterArgumentValidationErrorNullNotAllowed' when -Path is `$()" {
            try {
                set-content -path $() -value "ShouldNotWorkBecausePathIsInvalid" -ea stop
                Throw "Previous statement unexpectedly succeeded..."
            }
            catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.SetContentCommand"
            }
        }
        It "should throw 'PSNotSupportedException' when you set-content to an unsupported provider" -skip:$skipRegistry {
            try {
                set-content -path HKLM:\\software\\microsoft -value "ShouldNotWorkBecausePathIsUnsupported" -ea stop
                Throw "Previous statement unexpectedly succeeded..."
            }
            catch {
                $_.FullyQualifiedErrorId | Should Be "NotSupported,Microsoft.PowerShell.Commands.SetContentCommand"
            }
        }
        #[BugId(BugDatabase.WindowsOutOfBandReleases, 9058182)]
        It "should be able to pass multiple [string]`$objects to Set-Content through the pipeline to output a dynamic Path file" {
            "hello","world"|set-content $testdrive\dynamicfile2.txt
            $result=get-content $testdrive\dynamicfile2.txt
            $result.length |Should be 2
            $result[0]     |Should be "hello"
            $result[1]     |Should be "world"
        }
    }
}

Describe "Set-Content should work for PSDrive with UNC path as root" -Tags @('CI', 'RequireAdminOnWindows') {
    BeforeAll {
        $file1 = "file1.txt"
        $filePath1 = join-path $testdrive $file1
        #create a random folder
        $randomFolderName = "TestFolder_" + (Get-Random).ToString()
        $randomFolderPath = join-path $testdrive $randomFolderName
        $null = New-Item -Path $randomFolderPath -ItemType Directory -ErrorAction SilentlyContinue
    }
    It "should create a file in a psdrive with UNC path as root" -skip:(-not $IsWindows){
        try
        {
            # create share
            net share testshare=$randomFolderPath /grant:everyone,FULL
            New-PSDrive -Name Foo -Root \\localhost\testshare -PSProvider FileSystem
            set-content -path Foo:\$file1 -value "$file1"
            $result = Get-Content -path Foo:\$file1
            $result| Should be "$file1"
        }
        finally
        {
            Remove-PSDrive -Name Foo
            net share testshare /delete
        }
    }
}
