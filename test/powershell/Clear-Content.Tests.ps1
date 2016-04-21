Describe "Clear-Content cmdlet tests" {
    $file1 = "file1.txt"
    Setup -File "$file1"

    Context "Clear-Content should actually clear content" {
        It "should clear-Content of testdrive:\$file1" {
            set-content -path testdrive:\$file1 -value "ExpectedContent" -passthru | Should be "ExpectedContent"
            clear-content -Path testdrive:\$file1
        }
        It "shouldn't get any content from testdrive:\$file1" {
            $result = get-content -path testdrive:\$file1
            $result | Should BeExactly $null
        }

        It "should throw `"Cannot bind argument to parameter 'Path'`" when -Path is `$null" {
            {clear-content -path $null -ea stop} | Should Throw "Cannot bind argument to parameter 'Path'"
        }
        It "should throw `"Cannot bind argument to parameter 'Path'`" when -Path is `$()" {
            #[DRT][BugId(BugDatabase.WindowsOutOfBandReleases, 903880)]
            {clear-content -path $() -ea stop} | Should Throw "Cannot bind argument to parameter 'Path'"
        }
        It "should throw 'PSNotSupportedException' when you set-content to an unsupported provider" {
            #[DRT][BugId(BugDatabase.WindowsOutOfBandReleases, 906022)]
            {clear-content -path HKLM:\\software\\microsoft -ea stop} | Should Throw "IContentCmdletProvider interface is not implemented"
        }
    }
} 
