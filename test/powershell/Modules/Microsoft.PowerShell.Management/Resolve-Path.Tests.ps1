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

    It 'returns filenames starting with period correctly' {
        $testFile = Join-Path $TestDrive ".testfile"
        $null = New-Item -Path $testFile -ItemType File -Force
        try {
            Push-Location $TestDrive
            $result = Resolve-Path -Path ".testfile" -Relative
            $result | Should -BeExactly (Join-Path '.' '.testfile')
        }
        finally {
            Pop-Location
        }
    }

    It 'does not return path containing both current and parent directory' {
        $testDir = Join-Path $TestDrive "testDir"
        $null = New-Item -Path $testDir -ItemType Directory -Force
        $testFile = Join-Path $testDrive "testfile"
        $null = New-Item -Path $testFile -ItemType File -Force
        try {
            Push-Location $testDir
            $result = Resolve-Path -Path "..\testfile" -Relative
            $result | Should -BeExactly (Join-Path '..' 'testfile')
        }
        finally {
            Pop-Location
        }
    }
}
