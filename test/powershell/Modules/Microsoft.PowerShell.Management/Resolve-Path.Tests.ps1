Describe "Resolve-Path returns proper path" -Tag "CI" {
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
    It "Resolve-Path -Relative should return correct path on different drive" {
        $base = Join-Path "TestDrive:" "ResolvePath.relative"
        $root = Join-Path $base "fakeroot"
        $file = Join-Path $root "file.txt"
        $driveName = "RvpaTest"
        $null = New-Item -Path $base -ItemType Directory -Force
        $null = New-Item -Path $root -ItemType Directory -Force
        $null = New-Item -Path $file -ItemType File -Force
        $null = New-PSDrive -Name $driveName -PSProvider FileSystem -Root $root
        $driveRoot = Join-Path "$driveName`:" ""
        $driveFile = Join-Path "$driveName`:" "file.txt"
        try {
            Push-Location -Path $driveRoot
            Resolve-Path -Path $base -Relative | Should Be $base
        }
        finally {
            Pop-Location
        }
        try {
            Push-Location -Path $base
            Resolve-Path -Path $driveFile -Relative | Should Be $(Resolve-Path -Path $file -Relative)
        }
        finally {
            Pop-Location
        }
    }
}
