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

    It "Formatting for an object with only hidden property should use 'ToString' after a Get-Member call" {
        class Hidden {
            hidden $Param = 'Foo'
            [String]ToString() { return 'MyString' }
        }

        $hiddenObjectOne = [Hidden]::new()
        $hiddenObjectOne | Get-Member | Out-Null
        $outstring = $hiddenObjectOne | Out-String
        $outstring.Trim() | Should -BeExactly "MyString"

        class Hidden2 {
            hidden $Param = 'Foo'
        }

        $hiddenObjectTwo = [Hidden2]::new()
        $hiddenObjectTwo | Get-Member | Out-Null
        $outstring = $hiddenObjectTwo | Out-String
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

Describe "'Format-Table/List/Custom -Property' should not throw NullRef exception" -Tag CI {

    It "'<Command> -Property' requires value to be not null and not empty" -TestCases @(
        @{ Command = "Format-Table"; NameInErrorId = "FormatTableCommand" }
        @{ Command = "Format-List"; NameInErrorId = "FormatListCommand" }
        @{ Command = "Format-Custom"; NameInErrorId = "FormatCustomCommand" }
    ) {
        param($Command, $NameInErrorId)

        { Get-Process -Id $PID | & $Command -Property @() } | Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.$NameInErrorId"
        { Get-Process -Id $PID | & $Command -Property $null } | Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.$NameInErrorId"
    }
}
