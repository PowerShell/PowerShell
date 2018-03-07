# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Resolve-Path returns proper path" -Tag "CI" {
    It "Resolve-Path returns resolved paths" {
        Resolve-Path $TESTDRIVE | Should -BeExactly "$TESTDRIVE"
    }
    It "Resolve-Path handles provider qualified paths" {
        $result = Resolve-Path Filesystem::$TESTDRIVE
        $result.providerpath | Should -BeExactly "$TESTDRIVE"
    }
    It "Resolve-Path provides proper error on invalid location" {
        try {
            Resolve-Path $TESTDRIVE/this.directory.is.invalid -ea stop
            throw "execution OK"
        }
        catch {
            $_.fullyqualifiederrorid | Should -BeExactly "PathNotFound,Microsoft.PowerShell.Commands.ResolvePathCommand"
        }
    }
    It "Resolve-Path -Path should return correct drive path" {
        $result = Resolve-Path -Path "TestDrive:\\\\\"
        ($result.Path.TrimEnd('/\')) | Should -BeExactly "TestDrive:"
    }
    It "Resolve-Path -LiteralPath should return correct drive path" {
        $result = Resolve-Path -LiteralPath "TestDrive:\\\\\"
        ($result.Path.TrimEnd('/\')) | Should -BeExactly "TestDrive:"
    }
}
