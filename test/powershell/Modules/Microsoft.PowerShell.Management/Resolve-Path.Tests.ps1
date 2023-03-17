# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Resolve-Path returns proper path" -Tag "CI" {
    BeforeAll {
        $driveName = "RvpaTest"
        $root = Join-Path $TestDrive "fakeroot"
        $file = Join-Path $root "file.txt"
        $null = New-Item -Path $root -ItemType Directory -Force
        $null = New-Item -Path $file -ItemType File -Force
        $null = New-PSDrive -Name $driveName -PSProvider FileSystem -Root $root

        $testRoot = Join-Path $TestDrive ""
        $fakeRoot = Join-Path "$driveName`:" ""

        $relCases = @(
            @{ wd = $fakeRoot; target = $testRoot; expected = $testRoot }
            @{ wd = $testRoot; target = Join-Path $fakeRoot "file.txt"; expected = Join-Path "." "fakeroot" "file.txt" }
        )
    }
    AfterAll {
        Remove-PSDrive -Name $driveName -Force
    }
    It "Resolve-Path returns resolved paths" {
        Resolve-Path $TESTDRIVE | Should -BeExactly "$TESTDRIVE"
    }
    It "Resolve-Path handles provider qualified paths" {
        $result = Resolve-Path Filesystem::$TESTDRIVE
        $result.providerpath | Should -BeExactly "$TESTDRIVE"
    }
    It "Resolve-Path provides proper error on invalid location" {
        { Resolve-Path $TESTDRIVE/this.directory.is.invalid -ErrorAction Stop } | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.ResolvePathCommand"
    }
    It "Resolve-Path -Path should return correct drive path" {
        $result = Resolve-Path -Path "TestDrive:\\\\\"
        ($result.Path.TrimEnd('/\')) | Should -BeExactly "TestDrive:"
    }
    It "Resolve-Path -LiteralPath should return correct drive path" {
        $result = Resolve-Path -LiteralPath "TestDrive:\\\\\"
        ($result.Path.TrimEnd('/\')) | Should -BeExactly "TestDrive:"
    }
    It "Resolve-Path -Relative '<target>' should return correct path on '<wd>'" -TestCases $relCases {
        param($wd, $target, $expected)
        try {
            Push-Location -Path $wd
            Resolve-Path -Path $target -Relative | Should -BeExactly $expected
        }
        finally {
            Pop-Location
        }
    }
    It 'Resolve-Path -Relative should support user specified base paths' {
        $Expected = Join-Path -Path .\ -ChildPath fakeroot
        Resolve-Path -Path $fakeRoot -Relative -RelativeBasePath $testRoot | Should -BeExactly $Expected
    }
}
