Describe "Hierarchical paths" {
    BeforeAll {
        $data = "Hello World"
        Setup -File testFile.txt -Content $data
    }

    It "should work with Join-Path " {
        $testPath = Join-Path $TestDrive testFile.txt
        Get-Content $testPath | Should Be $data
    }

    It "should work with platform's slashes" {
        $testPath = "$TestDrive$([IO.Path]::DirectorySeparatorChar)testFile.txt"
        Get-Content $testPath | Should Be $data
    }

    It "should work with forward slashes" {
        $testPath = "$TestDrive/testFile.txt"
        Get-Content $testPath | Should Be $data
    }

    It "should work with backward slashes" {
        $testPath = "$TestDrive\testFile.txt"
        Get-Content $testPath | Should Be $data
    }

    It "Should allow escaped `\ on Linux" -Skip:$IsWindows {
        $name = "a`\weird`\file"
        $data = "Hello World"
        Setup -File $name -Content $data
        $testPath = Join-Path $TestDrive $name
        $testPath | write-host
    }

    It "should work with backward slashes for each separator" {
        $testPath = "$TestDrive\testFile.txt".Replace("/","\")
        Get-Content $testPath | should be $data
    }

    It "should work with forward slashes for each separator" {
        $testPath = "$TestDrive/testFile.txt".Replace("\","/")
        Get-Content $testPath | should be $data
    }

    It "should work even if there are too many forward slashes" {
        $testPath = "$TestDrive//////testFile.txt"
        Get-Content $testPath | should be $data
    }

    It "should work even if there are too many backward slashes" {
        $testPath = "$TestDrive\\\\\\\testFile.txt"
        Get-Content $testPath | should be $data
    }
}
