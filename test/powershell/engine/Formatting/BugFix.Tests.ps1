# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Bug fixes related to formatting' -Tag CI {

    It "Formatting for an object with no property/field should use 'ToString'" {
        class Empty {
            [String]ToString() { return 'MyString' }
        }

        $outstring = [Empty]::new() | Out-String
        $outstring.Trim() | Should -BeExactly "MyString"
    }

    It "Formatting for an object with only hidden property should use 'ToString'" {
        class Hidden {
            hidden $Param = 'Foo'
            [String]ToString() { return 'MyString' }
        }

        $outstring = [Hidden]::new() | Out-String
        $outstring.Trim() | Should -BeExactly "MyString"
    }

    It 'Formatting for an object with no-hidden property should use the default view' {
        class Params {
            $Param = 'Foo'
            [String]ToString() { return 'MyString' }
        }

        $outstring = [Params]::new() | Out-String
        $outstring.Trim() | Should -BeExactly "Param$([System.Environment]::NewLine)-----$([System.Environment]::NewLine)Foo"
    }
}
