# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Resolve-Path returns proper path" -Tag "CI" {
    BeforeDiscovery {
        $driveName = "RvpaTest"
        # Use placeholder paths for discovery - actual values set in BeforeAll
        $discoveryRoot = "PLACEHOLDER_ROOT"
        $discoveryTestRoot = "PLACEHOLDER_TESTROOT"
        $fakeRoot = "${driveName}:"
        $sep = [System.IO.Path]::DirectorySeparatorChar

        $relCases = @(
            @{ CaseId = 'fakeRoot-to-testRoot'; wd_desc = $fakeRoot; target_desc = $discoveryTestRoot }
            @{ CaseId = 'testRoot-to-fakeFile'; wd_desc = $discoveryTestRoot; target_desc = "${fakeRoot}${sep}file.txt" }
        )

        $rbpCases = @(
            @{ Scenario = "Absolute Path, Absolute ReleativeBasePath"; Path = $discoveryRoot; Basepath = $discoveryTestRoot; Expected = @($discoveryRoot, ".${sep}fakeroot"); CD = $null }
            @{ Scenario = "Relative Path, Absolute ReleativeBasePath"; Path = ".${sep}fakeroot"; Basepath = $discoveryTestRoot; Expected = @($discoveryRoot, ".${sep}fakeroot"); CD = $null }
            @{ Scenario = "Relative Path, Relative ReleativeBasePath"; Path = ".${sep}fakeroot"; Basepath = ".${sep}"; Expected = @($discoveryRoot, ".${sep}fakeroot"); CD = $discoveryTestRoot }
            @{ Scenario = "Invalid Path, Absolute ReleativeBasePath"; Path = "${discoveryTestRoot}${sep}ThisPathDoesNotExist"; Basepath = $discoveryRoot; Expected = $null; CD = $null }
            @{ Scenario = "Invalid Path, Invalid ReleativeBasePath"; Path = "${discoveryTestRoot}${sep}ThisPathDoesNotExist"; Basepath = "${discoveryTestRoot}${sep}ThisPathDoesNotExist"; Expected = $null; CD = $null }
        )

        $hiddenFilePrefix = ($IsLinux -or $IsMacOS) ? '.' : ''
        $hiddenCases = @(
            @{ Path = ".${sep}${hiddenFilePrefix}test1.txt"; BasePath = 'PLACEHOLDER'; Force = $false; ExpectedResult = "PLACEHOLDER${sep}${hiddenFilePrefix}test1.txt" }
            @{ Path = ".${sep}${hiddenFilePrefix}test2.txt"; BasePath = 'PLACEHOLDER'; Force = $false; ExpectedResult = "PLACEHOLDER${sep}${hiddenFilePrefix}test2.txt" }
            @{ Path = ".${sep}${hiddenFilePrefix}test*.txt"; BasePath = 'PLACEHOLDER'; Force = $false; ExpectedResult = $null }
            @{ Path = ".${sep}${hiddenFilePrefix}test1.txt"; BasePath = 'PLACEHOLDER'; Force = $true; ExpectedResult = "PLACEHOLDER${sep}${hiddenFilePrefix}test1.txt" }
            @{ Path = ".${sep}${hiddenFilePrefix}test2.txt"; BasePath = 'PLACEHOLDER'; Force = $true; ExpectedResult = "PLACEHOLDER${sep}${hiddenFilePrefix}test2.txt" }
            @{ Path = ".${sep}${hiddenFilePrefix}test*.txt"; BasePath = 'PLACEHOLDER'; Force = $true; ExpectedResult = "PLACEHOLDER" }
        )
    }

    BeforeAll {
        $driveName = "RvpaTest"
        $root = Join-Path $TestDrive "fakeroot"
        $file = Join-Path $root "file.txt"
        $null = New-Item -Path $root -ItemType Directory -Force
        $null = New-Item -Path $file -ItemType File -Force
        $null = New-PSDrive -Name $driveName -PSProvider FileSystem -Root $root

        $testRoot = Join-Path $TestDrive ""
        $fakeRoot = Join-Path "$driveName`:" ""

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
    It "Resolve-Path -Relative '<target_desc>' should return correct path on '<wd_desc>'" -TestCases $relCases {
        param($CaseId)
        $relData = @{
            'fakeRoot-to-testRoot'  = @{ wd = $fakeRoot; target = $testRoot; expected = $testRoot }
            'testRoot-to-fakeFile'  = @{ wd = $testRoot; target = (Join-Path $fakeRoot "file.txt"); expected = (Join-Path "." "fakeroot" "file.txt") }
        }
        $tc = $relData[$CaseId]
        try {
            Push-Location -Path $tc.wd
            Resolve-Path -Path $tc.target -Relative | Should -BeExactly $tc.expected
        }
        finally {
            Pop-Location
        }
    }
    It 'Resolve-Path RelativeBasePath should handle <Scenario>' -TestCases $rbpCases {
        param($Scenario)

        $sep = [System.IO.Path]::DirectorySeparatorChar
        $rbpData = @{
            "Absolute Path, Absolute ReleativeBasePath"  = @{ Path = $root;              Basepath = $testRoot; Expected = @($root, ".${sep}fakeroot"); CD = $null }
            "Relative Path, Absolute ReleativeBasePath"  = @{ Path = ".${sep}fakeroot";  Basepath = $testRoot; Expected = @($root, ".${sep}fakeroot"); CD = $null }
            "Relative Path, Relative ReleativeBasePath"  = @{ Path = ".${sep}fakeroot";  Basepath = ".${sep}"; Expected = @($root, ".${sep}fakeroot"); CD = $testRoot }
            "Invalid Path, Absolute ReleativeBasePath"   = @{ Path = (Join-Path $testRoot ThisPathDoesNotExist); Basepath = $root; Expected = $null; CD = $null }
            "Invalid Path, Invalid ReleativeBasePath"    = @{ Path = (Join-Path $testRoot ThisPathDoesNotExist); Basepath = (Join-Path $testRoot ThisPathDoesNotExist); Expected = $null; CD = $null }
        }
        $tc = $rbpData[$Scenario]
        $Path = $tc.Path; $BasePath = $tc.Basepath; $Expected = $tc.Expected; $CD = $tc.CD

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

    It "Resolve-Path -Path '<Path>' -RelativeBasePath '<BasePath>' -Force:<Force> should return '<ExpectedResult>'" -TestCases $hiddenCases {
        param($Path, $BasePath, $Force)
        # Use actual runtime values from BeforeAll
        $Path = $Path -replace 'PLACEHOLDER', $TestDrive
        $BasePath = $TestDrive
        $ExpectedResult = if ($Path -match '\*' -and -not $Force) {
            $null
        } elseif ($Path -match '\*' -and $Force) {
            @($hiddenFilePath1, $hiddenFilePath2)
        } elseif ($Path -match 'test1') {
            $hiddenFilePath1
        } else {
            $hiddenFilePath2
        }
        (Resolve-Path -Path $Path -RelativeBasePath $BasePath -Force:$Force).Path | Should -BeExactly $ExpectedResult
    }
}
