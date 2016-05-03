Describe "Join-Path cmdlet tests" {
    BeforeAll {
        $StartingLocation = Get-Location
    }
    AfterEach { 
        Set-Location $StartingLocation
    }
    It "should output multiple paths when called with multiple -Path targets" -Tag DRT {
        Setup -Dir SubDir1
        (Join-Path -Path TestDrive:,$TestDrive -ChildPath "SubDir1" -resolve).Length | Should be 2
    }
    It "should throw 'Cannot find drive' when drive cannot be found" -Tag DRT {
        {Join-Path bogusdrive:\\somedir otherdir -resolve -ea stop} | Should Throw "Cannot find drive"
    }
    It "should throw 'Cannot find path'  when item  cannot be found" -Tag DRT {
        {Join-Path "Bogus" "Path" -resolve -ea stop} | Should Throw "Cannot find path"
    }
    #[BugId(BugDatabase.WindowsOutOfBandReleases, 905237)] Note:  Windows-specified bug, but the result should be the same on other platforms
    It "should return one object when called with a FileSystem::Redirector" -Tag DRT {
        set-location env:\
        $result=join-path FileSystem::windir system32
        $result.Count | Should be 1
        $result       | Should BeExactly "FileSystem::windir\system32"
    }
    #[BugId(BugDatabase.WindowsOutOfBandReleases, 913084)]
    It "should be able to join-path special string 'Variable:' with 'foo'" -Tag DRT {
        $result=Join-Path "Variable:" "foo"
        $result.Count | Should be 1
        $result       | Should BeExactly "Variable:\foo"
    }
    #[BugId(BugDatabase.WindowsOutOfBandReleases, 913084)]
    It "should be able to join-path special string 'Alias:' with 'foo'" -Tag DRT {
        $result=Join-Path "Alias:" "foo"
        $result.Count | Should be 1
        $result       | Should BeExactly "Alias:\foo"
    }
    #[BugId(BugDatabase.WindowsOutOfBandReleases, 913084)]
    It "should be able to join-path special string 'Env:' with 'foo'" -Tag DRT {
        $result=Join-Path "Env:" "foo"
        $result.Count | Should be 1
        $result       | Should BeExactly "Env:\foo"
    }
} 
