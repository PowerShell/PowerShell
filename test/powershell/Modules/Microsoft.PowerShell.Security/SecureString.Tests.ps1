# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "SecureString conversion tests" -Tags "CI" {
    BeforeAll {
        $string = "ABCD"
        $secureString = [System.Security.SecureString]::New()
        $string.ToCharArray() | ForEach-Object { $securestring.AppendChar($_) }
    }

    It "using null arguments to ConvertFrom-SecureString produces an exception" {
        { ConvertFrom-SecureString -SecureString $null -Key $null } |
            Should -Throw -ErrorId "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.ConvertFromSecureStringCommand"
    }

    It "using a bad key produces an exception" {
        $badkey = [byte[]]@(1,2)
        { ConvertFrom-SecureString -SecureString $secureString -Key $badkey } |
            Should -Throw -ErrorId "Argument,Microsoft.PowerShell.Commands.ConvertFromSecureStringCommand"
    }

    It "Can convert to a secure string" {
        $ss = ConvertTo-SecureString -AsPlainText -Force abcd
        $ss | Should -BeOfType SecureString
    }

    It "can convert back from a secure string" {
        $secret = "abcd"
        $ss1 = ConvertTo-SecureString -AsPlainText -Force $secret
        $ss2 = ConvertFrom-SecureString $ss1 | ConvertTo-SecureString
        $ss2 | ConvertFrom-SecureString -AsPlainText | Should -Be $secret
    }
}
