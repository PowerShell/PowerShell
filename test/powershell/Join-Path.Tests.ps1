Describe "Join-Path cmdlet tests" {
    BeforeAll {
        $StartingLocation = Get-Location
    }
    AfterEach { 
        Set-Location $StartingLocation
    }
    #[DRT]
    It "should output multiple paths when called with multiple -Path targets" {
        Setup -Dir SubDir1
        (Join-Path -Path TestDrive:,$TestDrive -ChildPath "SubDir1" -resolve).Length | Should be 2
    }
    #[DRT]
    It "should throw 'Cannot find drive' when drive cannot be found" {
        {Join-Path bogusdrive:\\somedir otherdir -resolve -ea stop} | Should Throw "Cannot find drive"
    }
    #[DRT]
    It "should throw 'Cannot find path'  when item  cannot be found" {
        {Join-Path "Bogus" "Path" -resolve -ea stop} | Should Throw "Cannot find path"
    }
    #[DRT][BugId(BugDatabase.WindowsOutOfBandReleases, 905237)] Note:  Windows-specified bug, but the result should be the same on other platforms
    It "should return one object when called with a FileSystem::Redirector" {
        set-location env:\
        $result=join-path FileSystem::windir system32
        $result.Count | Should be 1
        $result       | Should BeExactly "FileSystem::windir\system32"
    }
    #[DRT][BugId(BugDatabase.WindowsOutOfBandReleases, 913084)]
    It "should be able to join-path special string 'Variable:' with 'foo'" {
        $result=Join-Path "Variable:" "foo"
        $result |Should BeExactly "Variable:\foo"
    }
    #[DRT][BugId(BugDatabase.WindowsOutOfBandReleases, 913084)]
    It "should be able to join-path special string 'Alias:' with 'foo'" {
        $result=Join-Path "Alias:" "foo"
        $result |Should BeExactly "Alias:\foo"
    }
    #[DRT][BugId(BugDatabase.WindowsOutOfBandReleases, 913084)]
    It "should be able to join-path special string 'Env:' with 'foo'" {
        $result=Join-Path "Env:" "foo"
        $result |Should BeExactly "Env:\foo"
    }
} 
