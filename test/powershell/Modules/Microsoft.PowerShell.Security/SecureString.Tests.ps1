# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingConvertToSecureStringWithPlainText', '')]
param()

Describe "SecureString conversion tests" -Tags "CI" {
    BeforeAll {
        $string = "ABCD"
        $secureString = [System.Security.SecureString]::New()
        $string.ToCharArray() | ForEach-Object { $securestring.AppendChar($_) }
    }

    It "Using null arguments to ConvertFrom-SecureString produces an exception" {
        { ConvertFrom-SecureString -SecureString $null -Key $null } |
            Should -Throw -ErrorId "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.ConvertFromSecureStringCommand"
    }

    It "Using a bad key produces an exception" {
        $badkey = [byte[]]@(1,2)
        { ConvertFrom-SecureString -SecureString $secureString -Key $badkey } |
            Should -Throw -ErrorId "Argument,Microsoft.PowerShell.Commands.ConvertFromSecureStringCommand"
    }

    It "Can convert to a secure string" {
        $ss = ConvertTo-SecureString -AsPlainText -Force abcd
        $ss | Should -BeOfType SecureString
    }

    It "Can convert back from a secure string" {
        $ss1 = ConvertTo-SecureString -AsPlainText -Force $string
        $ss2 = ConvertFrom-SecureString $ss1 | ConvertTo-SecureString
        $ss2 | ConvertFrom-SecureString -AsPlainText | Should -Be $string
    }

    It "Can encode secure string with key" {
        $testString = '[8Chars][8Chars][Not8]'
        $key = [System.Text.Encoding]::UTF8.GetBytes("1234"*8)
        $ss1 = $testString | ConvertTo-SecureString -AsPlainText -Force
        $encodedStr = $ss1 | ConvertFrom-SecureString -Key $key
        $ss2 = $encodedStr | ConvertTo-SecureString -Key $key
        $ss2 | ConvertFrom-SecureString -AsPlainText | Should -BeExactly $testString
    }

    It "Using invalid secure string with ConvertFrom-SecureString produces an exception message without value" {
        $ex = { ConvertFrom-SecureString "1234" } | Should -Throw -ErrorId "CannotConvertArgumentNoMessage,Microsoft.PowerShell.Commands.ConvertFromSecureStringCommand" -PassThru
        $ex.Exception.Message | Should -Not -Match "1234"
    }

    It "Using invalid securestring with cast produces an exception message without value" {
        $ex = { [securestring]"1234" } | Should -Throw -ErrorId "ConvertToFinalInvalidCastException" -PassThru
        $ex.Exception.Message | Should -Not -Match "1234"
    }
}
