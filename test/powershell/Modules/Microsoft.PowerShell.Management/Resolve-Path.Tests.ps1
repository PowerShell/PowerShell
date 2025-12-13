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

        $hiddenFilePrefix = ($IsLinux -or $IsMacOS) ? '.' : ''

        $hiddenFilePath1 = Join-Path -Path $TestDrive -ChildPath "$($hiddenFilePrefix)test1.txt"
        $hiddenFilePath2 = Join-Path -Path $TestDrive -ChildPath "$($hiddenFilePrefix)test2.txt"

        $hiddenFile1 = New-Item -Path $hiddenFilePath1 -ItemType File
        $hiddenFile2 = New-Item -Path $hiddenFilePath2 -ItemType File

        $relativeHiddenFilePath1 = ".$([System.IO.Path]::DirectorySeparatorChar)$($hiddenFilePrefix)test1.txt"
        $relativeHiddenFilePath2 = ".$([System.IO.Path]::DirectorySeparatorChar)$($hiddenFilePrefix)test2.txt"

        if ($IsWindows) {
            $hiddenFile1.Attributes = "Hidden"
            $hiddenFile2.Attributes = "Hidden"
        }

        $hiddenFileWildcardPath = Join-Path -Path $TestDrive -ChildPath "$($hiddenFilePrefix)test*.txt"
        $relativeHiddenFileWildcardPath = ".$([System.IO.Path]::DirectorySeparatorChar)$($hiddenFilePrefix)test*.txt"
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
    It 'Resolve-Path RelativeBasePath should handle <Scenario>' -TestCases @(
        @{
            Scenario = "Absolute Path, Absolute ReleativeBasePath"
            Path     = $root
            Basepath = $testRoot
            Expected = $root, ".$([System.IO.Path]::DirectorySeparatorChar)fakeroot"
            CD       = $null
        }
        @{
            Scenario = "Relative Path, Absolute ReleativeBasePath"
            Path     = ".$([System.IO.Path]::DirectorySeparatorChar)fakeroot"
            Basepath = $testRoot
            Expected = $root, ".$([System.IO.Path]::DirectorySeparatorChar)fakeroot"
            CD       = $null
        }
        @{
            Scenario = "Relative Path, Relative ReleativeBasePath"
            Path     = ".$([System.IO.Path]::DirectorySeparatorChar)fakeroot"
            Basepath = ".$([System.IO.Path]::DirectorySeparatorChar)"
            Expected = $root, ".$([System.IO.Path]::DirectorySeparatorChar)fakeroot"
            CD       = $testRoot
        }
        @{
            Scenario = "Invalid Path, Absolute ReleativeBasePath"
            Path     = Join-Path $testRoot ThisPathDoesNotExist
            Basepath = $root
            Expected = $null
            CD       = $null
        }
        @{
            Scenario = "Invalid Path, Invalid ReleativeBasePath"
            Path     = Join-Path $testRoot ThisPathDoesNotExist
            Basepath = Join-Path $testRoot ThisPathDoesNotExist
            Expected = $null
            CD       = $null
        }
    ) -Test {
        param($Path, $BasePath, $Expected, $CD)

        if ($null -eq $Expected)
        {
            {Resolve-Path -Path $Path -RelativeBasePath $BasePath -ErrorAction Stop} | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.ResolvePathCommand"
            {Resolve-Path -Path $Path -RelativeBasePath $BasePath -ErrorAction Stop -Relative} | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.ResolvePathCommand"
        }
        else
        {
            try
            {
                $OldLocation = if ($null -ne $CD)
                {
                    $PWD
                    Set-Location $CD
                }

                (Resolve-Path -Path $Path -RelativeBasePath $BasePath).ProviderPath | Should -BeExactly $Expected[0]
                Resolve-Path -Path $Path -RelativeBasePath $BasePath -Relative | Should -BeExactly $Expected[1]
            }
            finally
            {
                if ($null -ne $OldLocation)
                {
                    Set-Location $OldLocation
                }
            }
        }
    }

    It "Resolve-Path -Path '<Path>' -RelativeBasePath '<BasePath>' -Force:<Force> should return '<ExpectedResult>'" -TestCases @(
        @{
            Path           = $relativeHiddenFilePath1
            BasePath       = $TestDrive
            Force          = $false
            ExpectedResult = $hiddenFilePath1
        }
        @{
            Path           = $relativeHiddenFilePath2
            BasePath       = $TestDrive
            Force          = $false
            ExpectedResult = $hiddenFilePath2
        }
        @{
            Path           = $relativeHiddenFileWildcardPath
            BasePath       = $TestDrive
            Force          = $false
            ExpectedResult = $null
        }
        @{
            Path           = $relativeHiddenFilePath1
            BasePath       = $TestDrive
            Force          = $true
            ExpectedResult = $hiddenFilePath1
        }
        @{
            Path           = $relativeHiddenFilePath2
            BasePath       = $TestDrive
            Force          = $true
            ExpectedResult = $hiddenFilePath2
        }
        @{
            Path           = $relativeHiddenFileWildcardPath
            BasePath       = $TestDrive
            Force          = $true
            ExpectedResult = @($hiddenFilePath1, $hiddenFilePath2)
        }
    ) {
        param($Path, $BasePath, $Force, $ExpectedResult)
        (Resolve-Path -Path $Path -RelativeBasePath $BasePath -Force:$Force).Path | Should -BeExactly $ExpectedResult
    }
}

Describe "If the path specified by the parameter `-RelativeBasePath` contains wildcard characters, the command `Resolve-Path` should not produce any errors." -Tags "CI" {
    BeforeAll {
        $testFolder = 'TestDrive:\[Folder]'
        New-Item -ItemType Directory -Path $testFolder
        Push-Location -LiteralPath $testFolder
    }

    It "Should succeed in resolving a path when the relative base path contains a wildcard character" {
        Resolve-Path -LiteralPath . -RelativeBasePath .           | Should -BeTrue
        Resolve-Path -LiteralPath . -RelativeBasePath . -Relative | Should -BeTrue
        Resolve-Path -Path .        -RelativeBasePath .           | Should -BeTrue
        Resolve-Path -Path .        -RelativeBasePath . -Relative | Should -BeTrue
    }

    AfterAll {
        Pop-Location
    }
}
