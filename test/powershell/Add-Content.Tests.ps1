Describe "Add-Content cmdlet tests" {
    $file1 = "file1.txt"
    Setup -File "$file1"

    Context "Add-Content should actually add content" {
        It "should Add-Content to testdrive:\$file1" {
            $result=add-content -path testdrive:\$file1 -value "ExpectedContent" -passthru 
            $result| Should be "ExpectedContent"
        }
        It "should return expected string from testdrive:\$file1" {
            $result = get-content -path testdrive:\$file1
            $result | Should BeExactly "ExpectedContent"
        }
        It "should Add-Content to testdrive:\dynamicfile.txt with dynamic parameters" {
            $result=add-content -path testdrive:\dynamicfile.txt -value "ExpectedContent" -passthru
            $result| Should BeExactly "ExpectedContent"
        }
        It "should return expected string from testdrive:\dynamicfile.txt" {
            $result = get-content -path testdrive:\dynamicfile.txt
            $result | Should BeExactly "ExpectedContent"
        }
        It "should Add-Content to testdrive:\$file1 even when -Value is `$null" {
            $AsItWas=get-content testdrive:\$file1
            {add-content -path testdrive:\$file1 -value $null -ea stop} | Should Not Throw
            get-content testdrive:\$file1 | Should BeExactly $AsItWas
        }
        It "should throw `"Cannot bind argument to parameter 'Path'`" when -Path is `$null" {
            {add-content -path $null -value "ShouldNotWorkBecausePathIsNull" -ea stop} | Should Throw "Cannot bind argument to parameter 'Path'"
        }
        It "should throw `"Cannot bind argument to parameter 'Path'`" when -Path is `$()" {
            #[DRT][BugId(BugDatabase.WindowsOutOfBandReleases, 903880)]
            {add-content -path $() -value "ShouldNotWorkBecausePathIsInvalid" -ea stop} | Should Throw "Cannot bind argument to parameter 'Path'"
        }
        It "should throw 'PSNotSupportedException' when you add-content to an unsupported provider" -Skip:($IsLinux -Or $IsOSX) {
            #[DRT][BugId(BugDatabase.WindowsOutOfBandReleases, 906022)]
            {add-content -path HKLM:\\software\\microsoft -value "ShouldNotWorkBecausePathIsUnsupported" -ea stop} | Should Throw "IContentCmdletProvider interface is not implemented"
        }
        It "should be able to pass multiple [string]`$objects to Add-Content through the pipeline to output a dynamic Path file" {
            #[DRT][BugId(BugDatabase.WindowsOutOfBandReleases, 9058182)]
            "hello","world"|add-content testdrive:\dynamicfile2.txt
            $result=get-content testdrive:\dynamicfile2.txt
            $result.length |Should be 2
            $result[0]     |Should be "hello"
            $result[1]     |Should be "world"
        }
    }
} 
