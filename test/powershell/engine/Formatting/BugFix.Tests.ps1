# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Hidden properties should not be returned by the 'FirstOrDefault' primitive" -Tag CI {
    BeforeAll {
        if ($null -ne $PSStyle) {
            $outputRendering = $PSStyle.OutputRendering
            $PSStyle.OutputRendering = 'plaintext'
        }
    }

    AfterAll {
        if ($null -ne $PSStyle) {
            $PSStyle.OutputRendering = $outputRendering
        }
    }

    It "Formatting for an object with no property/field should use 'ToString'" {
        class Empty {
            [String]ToString() { return 'MyString' }
        }

        $outstring = [Empty]::new() | Out-String
        $outstring.Trim() | Should -BeExactly "MyString"

        class Empty2 { }

        $outstring = [Empty2]::new() | Out-String
        $outstring.Trim() | Should -BeLike "*.Empty2"
    }

    It "Formatting for an object with only hidden property should use 'ToString'" {
        class Hidden {
            hidden $Param = 'Foo'
            [String]ToString() { return 'MyString' }
        }

        $outstring = [Hidden]::new() | Out-String
        $outstring.Trim() | Should -BeExactly "MyString"

        class Hidden2 {
            hidden $Param = 'Foo'
        }

        $outstring = [Hidden2]::new() | Out-String
        $outstring.Trim() | Should -BeLike "*.Hidden2"
    }

    It 'Formatting for an object with no-hidden property should use the default view' {
        class Params {
            $Param = 'Foo'
            [String]ToString() { return 'MyString' }
        }

        $outstring = [Params]::new() | Out-String
        $outstring.Trim() | Should -BeExactly "Param$([System.Environment]::NewLine)-----$([System.Environment]::NewLine)Foo"

        class Params2 {
            $Param = 'Foo'
        }

        $outstring = [Params2]::new() | Out-String
        $outstring.Trim() | Should -BeExactly "Param$([System.Environment]::NewLine)-----$([System.Environment]::NewLine)Foo"
    }
}
