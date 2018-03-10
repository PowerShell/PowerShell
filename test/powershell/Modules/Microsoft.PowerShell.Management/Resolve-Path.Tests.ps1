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
        Remove-PSDrive -Name $driveName -PSProvider FileSystem
    }
    It "Resolve-Path returns resolved paths" {
        Resolve-Path $TESTDRIVE | Should be "$TESTDRIVE"
    }
    It "Resolve-Path handles provider qualified paths" {
        $result = Resolve-Path Filesystem::$TESTDRIVE
        $result.providerpath | should be "$TESTDRIVE"
    }
    It "Resolve-Path provides proper error on invalid location" {
        try {
            Resolve-Path $TESTDRIVE/this.directory.is.invalid -ea stop
            throw "execution OK"
        }
        catch {
            $_.fullyqualifiederrorid | should be "PathNotFound,Microsoft.PowerShell.Commands.ResolvePathCommand"
        }
    }
    It "Resolve-Path -Path should return correct drive path" {
        $result = Resolve-Path -Path "TestDrive:\\\\\"
        ($result.Path.TrimEnd('/\')) | Should Be "TestDrive:"
    }
    It "Resolve-Path -LiteralPath should return correct drive path" {
        $result = Resolve-Path -LiteralPath "TestDrive:\\\\\"
        ($result.Path.TrimEnd('/\')) | Should Be "TestDrive:"
    }
    It "Resolve-Path -Relative should return correct path on different drive" -TestCases $relCases {
        param($wd, $target, $expected)
        try {
            Push-Location -Path $wd
            Resolve-Path -Path $target -Relative | Should BeExactly $expected
        }
        finally {
            Pop-Location
        }
    }
}
