
Describe "Resolve-Path returns proper path" -Tag "CI" {
    It "Resolve-Path returns resolved paths" {
        Resolve-Path $TESTDRIVE | Should be "$TESTDRIVE"
    }
    It "Resolve-Path handles provider qualified paths" {
        $result = Resolve-Path Filesystem::$TESTDRIVE
        $result.providerpath | should be "$TESTDRIVE"
    }
    It "Resolve-Path provides proper error on invalid location" {
        { Resolve-Path $TESTDRIVE/this.directory.is.invalid -ErrorAction Stop } | ShouldBeErrorId "PathNotFound,Microsoft.PowerShell.Commands.ResolvePathCommand"
    }
    It "Resolve-Path -Path should return correct drive path" {
        $result = Resolve-Path -Path "TestDrive:\\\\\"
        ($result.Path.TrimEnd('/\')) | Should Be "TestDrive:"
    }
    It "Resolve-Path -LiteralPath should return correct drive path" {
        $result = Resolve-Path -LiteralPath "TestDrive:\\\\\"
        ($result.Path.TrimEnd('/\')) | Should Be "TestDrive:"
    }
}
