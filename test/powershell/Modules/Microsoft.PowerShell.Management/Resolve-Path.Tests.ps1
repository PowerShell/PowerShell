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
}
