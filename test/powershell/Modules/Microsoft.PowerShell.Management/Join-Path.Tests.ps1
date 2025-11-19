# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Join-Path cmdlet tests" -Tags "CI" {
  $SepChar=[io.path]::DirectorySeparatorChar
  BeforeAll {
    $StartingLocation = Get-Location
  }
  AfterEach {
    Set-Location $StartingLocation
  }
  It "should output multiple paths when called with multiple -Path targets" {
    Setup -Dir SubDir1
    (Join-Path -Path TestDrive:,$TestDrive -ChildPath "SubDir1" -Resolve).Length | Should -Be 2
  }
  It "should throw 'DriveNotFound' when called with -Resolve and drive does not exist" {
    { Join-Path bogusdrive:\\somedir otherdir -Resolve -ErrorAction Stop; Throw "Previous statement unexpectedly succeeded..." } |
      Should -Throw -ErrorId "DriveNotFound,Microsoft.PowerShell.Commands.JoinPathCommand"
  }
  It "should throw 'PathNotFound' when called with -Resolve and item does not exist" {
    { Join-Path "Bogus" "Path" -Resolve -ErrorAction Stop; Throw "Previous statement unexpectedly succeeded..." } |
      Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.JoinPathCommand"
  }
  #[BugId(BugDatabase.WindowsOutOfBandReleases, 905237)] Note: Result should be the same on non-Windows platforms too
  It "should return one object when called with a Windows FileSystem::Redirector" {
    Set-Location ("env:"+$SepChar)
    $result=Join-Path FileSystem::windir system32
    $result.Count | Should -Be 1
    $result       | Should -BeExactly ("FileSystem::windir"+$SepChar+"system32")
  }
  #[BugId(BugDatabase.WindowsOutOfBandReleases, 913084)]
  It "should be able to join-path special string 'Variable:' with 'foo'" {
    $result=Join-Path "Variable:" "foo"
    $result.Count | Should -Be 1
    $result       | Should -BeExactly ("Variable:"+$SepChar+"foo")
  }
  #[BugId(BugDatabase.WindowsOutOfBandReleases, 913084)]
  It "should be able to join-path special string 'Alias:' with 'foo'" {
    $result=Join-Path "Alias:" "foo"
    $result.Count | Should -Be 1
    $result       | Should -BeExactly ("Alias:"+$SepChar+"foo")
  }
  #[BugId(BugDatabase.WindowsOutOfBandReleases, 913084)]
  It "should be able to join-path special string 'Env:' with 'foo'" {
    $result=Join-Path "Env:" "foo"
    $result.Count | Should -Be 1
    $result       | Should -BeExactly ("Env:"+$SepChar+"foo")
  }
  It "should be able to join multiple child paths passed by position with remaining arguments" {
    $result = Join-Path one two three four five
    $result.Count | Should -Be 1
    $result       | Should -BeExactly "one${sepChar}two${sepChar}three${sepChar}four${sepChar}five"
  }
  It "Join-Path -Path <Path> -ChildPath <ChildPath> should return '<ExpectedResult>'" -TestCases @(
    @{
        Path = 'one'
        ChildPath = 'two', 'three'
        ExpectedResult = "one${sepChar}two${sepChar}three"
    }
    @{
        Path = 'one', 'two'
        ChildPath = 'three', 'four'
        ExpectedResult = @(
            "one${sepChar}three${sepChar}four"
            "two${sepChar}three${sepChar}four"
        )
    }
    @{
        Path = 'one'
        ChildPath = @()
        ExpectedResult = "one${sepChar}"
    }
    @{
        Path = 'one'
        ChildPath = $null
        ExpectedResult = "one${sepChar}"
    }
    @{
        Path = 'one'
        ChildPath = [string]::Empty
        ExpectedResult = "one${sepChar}"
    }
  ) {
    param($Path, $ChildPath, $ExpectedResult)
    $result = Join-Path -Path $Path -ChildPath $ChildPath
    $result | Should -BeExactly $ExpectedResult
  }
  It "should change extension when -Extension parameter is specified" {
    $result = Join-Path -Path "C:\folder" -ChildPath "file.txt" -Extension ".log"
    $result | Should -BeExactly "C:\folder\file.log"
  }
  It "should add extension to file without extension" {
    $result = Join-Path -Path "C:\folder" -ChildPath "file" -Extension ".txt"
    $result | Should -BeExactly "C:\folder\file.txt"
  }
  It "should remove extension when empty string is specified" {
    $result = Join-Path -Path "C:\folder" -ChildPath "file.txt" -Extension ""
    $result | Should -BeExactly "C:\folder\file"
  }
  It "should change extension for multiple paths" {
    $result = Join-Path -Path "C:\folder1", "C:\folder2" -ChildPath "file.txt" -Extension ".log"
    $result.Count | Should -Be 2
    $result[0] | Should -BeExactly "C:\folder1\file.log"
    $result[1] | Should -BeExactly "C:\folder2\file.log"
  }
  It "should handle extension with or without leading dot" {
    $result1 = Join-Path -Path "C:\folder" -ChildPath "file.txt" -Extension ".log"
    $result2 = Join-Path -Path "C:\folder" -ChildPath "file.txt" -Extension "log"
    $result1 | Should -BeExactly "C:\folder\file.log"
    $result2 | Should -BeExactly "C:\folder\file.log"
  }
  It "should accept Extension from pipeline by property name" {
    $obj = [PSCustomObject]@{ Path = "C:\folder"; ChildPath = "file.txt"; Extension = ".log" }
    $result = $obj | Join-Path
    $result | Should -BeExactly "C:\folder\file.log"
  }
}
