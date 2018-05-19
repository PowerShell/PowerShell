# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Tests OutVariable only" -Tags "CI" {
    BeforeAll {

        function get-foo1
        {
            [CmdletBinding()]
            param()

            "foo"
        }

        function get-foo2
        {
            [CmdletBinding()]
            param()

            $pscmdlet.writeobject("foo")
        }

        function get-bar
        {
            [CmdletBinding()]
            param()

            "bar"
            get-foo1 -outVariable script:a
        }
    }

    $testdata = @(
                    @{ Name = 'Updating OutVariable Case 1: pipe string';
                        Command = "get-foo1";
                        OutVariable = 'a';
                        Expected = 'foo'
                        },
                    @{ Name = 'Updating OutVariable Case 2: $pscmdlet.writeobject';
                        Command = "get-foo2";
                        OutVariable = 'a';
                        Expected = 'foo'
                        },
                    @{ Name = 'Appending OutVariable Case 1: pipe string';
                        Command = "get-foo1";
                        OutVariable = 'a';
                        PreSet = 'a','b';
                        Expected = @("a", "b", "foo")
                        },
                    @{ Name = 'Appending OutVariable Case 2: $pscmdlet.writeobject';
                        Command = "get-foo2";
                        OutVariable = 'a';
                        PreSet = 'a','b';
                        Expected = @("a", "b", "foo")
                        }
                    )

    It 'Test: <Name>' -TestCases $testdata {
        param ( $Name, $Command, $OutVariable, $PreSet, $Expected )
        if($null -ne $PreSet)
        {
            Set-Variable -Name $OutVariable -Value $PreSet
            & $Command -OutVariable +$OutVariable > $null
        }
        else
        {
            & $Command -OutVariable $OutVariable > $null
        }
        $a = Get-Variable -ValueOnly $OutVariable
        $a | Should -BeExactly $Expected
    }

    It 'Nested OutVariable' {

        get-bar -outVariable b > $null
        $script:a | Should -BeExactly 'foo'
        $b | Should -BeExactly @("bar", "foo")
    }
}

Describe "Test ErrorVariable only" -Tags "CI" {
    BeforeAll {
        function get-foo1
        {
            [CmdletBinding()]
            param()

            write-error "foo"
        }

        function get-foo2
        {
            [CmdletBinding()]
            param()

            $pscmdlet.WriteError($script:foo[0])
        }

        function get-bar
        {
            [CmdletBinding()]
            param()

            write-error "bar"
            get-foo1 -errorVariable script:a
        }
    }

    $testdata1 = @(
                    @{ Name = 'Updating ErrorVariable Case 1: write-error';
                       Command = "get-foo1";
                       ErrorVariable = 'a';
                       Expected = 'foo'
                     },
                     @{ Name = 'Updating ErrorVariable Case 2: $pscmdlet.WriteError';
                       Command = "get-foo1";
                       ErrorVariable = 'a';
                       Expected = 'foo'
                     },
                    @{ Name = 'Appending ErrorVariable Case 1: pipe string';
                        Command = "get-foo1";
                        ErrorVariable = 'a';
                        PreSet = @('a','b');
                        Expected = @("a", "b", "foo")
                        }
                    )

    It '<Name>' -TestCases $testdata1 {
        param ( $Name, $Command, $ErrorVariable, $PreSet, $Expected )
        if($null -ne $PreSet)
        {
            Set-Variable -Name $ErrorVariable -Value $PreSet
            & $Command -ErrorVariable +$ErrorVariable 2> $null
        }
        else
        {
            & $Command -ErrorVariable $ErrorVariable 2> $null

        }
        $a = (Get-Variable -ValueOnly $ErrorVariable) | ForEach-Object {$_.ToString()}
        $a | Should -BeExactly $Expected
    }

    It 'Appending ErrorVariable Case 2: $pscmdlet.writeerror' {
        write-error "foo" -errorVariable script:foo 2> $null
        $a = 'a','b'

        get-foo2 -errorVariable +a 2> $null

        $a.count | Should -Be 3
        $a| ForEach-Object {$_.ToString()} | Should -BeExactly @('a', 'b', 'foo')
    }

    It 'Nested ErrorVariable' {

        get-bar -errorVariable b 2> $null

        $script:a | Should -BeExactly 'foo'
        $b | Should -BeExactly @("bar","foo")
    }

    It 'Nested ErrorVariable with redirection' {

        get-bar -errorVariable b 2>&1 > $null

        $script:a | Should -BeExactly 'foo'
        $b | Should -BeExactly @("bar", "foo")
    }

}

Describe "Update both OutVariable and ErrorVariable" -Tags "CI" {
    BeforeAll {

        function get-foo
        {
          [CmdletBinding()]
          param()

          write-output "foo-output"
          write-error  "foo-error"
        }

        function get-foo1
        {
            [CmdletBinding()]
            param()

            write-error "foo"
        }

        function get-foo2
        {
            [CmdletBinding()]
            param()

            $pscmdlet.WriteError($script:foo[0])
        }

        function get-bar
        {
            [CmdletBinding()]
            param()

            write-error "bar"
            get-foo1 -errorVariable script:a
        }

        function get-foo3
        {
            [CmdletBinding()]
            param()

            "foo-output-0"
            write-output "foo-output-1"
            write-error "foo-error"
        }

        function get-bar2
        {
            [CmdletBinding()]
            param()

            "bar-output-0"
            write-output "bar-output-1"
            write-error "bar-error"
            get-foo3 -OutVariable script:foo_out -errorVariable script:foo_err
        }
    }

    It 'Update OutVariable and ErrorVariable' {

        get-foo3 -OutVariable out -errorVariable err 2> $null > $null

        $out | Should -BeExactly @("foo-output-0", "foo-output-1")
        $err | Should -BeExactly "foo-error"
    }

    It 'Update OutVariable and ErrorVariable' {

        get-bar2 -OutVariable script:bar_out -errorVariable script:bar_err 2> $null > $null

        $foo_out | Should -BeExactly @("foo-output-0", "foo-output-1")
        $foo_err | Should -BeExactly 'foo-error'

        $bar_out | Should -BeExactly @("bar-output-0", "bar-output-1", "foo-output-0", "foo-output-1")
        $bar_err | Should -BeExactly @("bar-error", "foo-error")
    }

    It 'Verify that exceptions are added to the ErrorVariable' {
        function get-foo4
        {
            [CmdletBinding()]
            param()

            write-error "foo-error"

            try
            {
                throw "foo-exception"
            }
            catch
            {}
        }

        get-foo4 -errorVariable err 2> $null

        $err | Should -BeExactly @("foo-error", "foo-exception")
    }

    It 'Error variable in multi-command pipeline' {
        function get-foo5
        {
          [CmdletBinding()]
          param([Parameter(ValueFromPipeline = $true)][string] $foo)

          process
          {
            write-output $foo
            write-error  $foo
          }
        }

        (get-foo5 "foo-message" -ErrorVariable foo_err1 -ov foo_out1 | get-foo5 -ErrorVariable foo_err2 -ov foo_out2 | get-foo5 -ErrorVariable foo_err3 -ov foo_out3) 2>&1 > $null

        $foo_out1 | Should -BeExactly "foo-message"
        $foo_out2 | Should -BeExactly "foo-message"
        $foo_out3 | Should -BeExactly "foo-message"
        $foo_err1 | Should -BeExactly "foo-message"
        $foo_err2 | Should -BeExactly "foo-message"
        $foo_err3 | Should -BeExactly "foo-message"
    }

    Context 'Error variable in multi-command pipeline (with native cmdlet)' {

        BeforeAll {
            (get-foo -ErrorVariable foo_err | get-item -ErrorVariable get_item_err ) 2>&1 > $null
        }

        It '$foo_err should be "foo-error"' {
            $foo_err | Should -BeExactly "foo-error"
        }

        It '$get_item_err.count and $get_item_err[0].exception' {
            $get_item_err.count | Should -Be 1
            $get_item_err[0].exception | Should -Not -BeNullOrEmpty
            $get_item_err[0].exception | Should -BeOftype 'System.Management.Automation.ItemNotFoundException'
        }
    }

    It 'Multi-command pipeline with nested commands' {

        function get-bar3
        {
            [CmdletBinding()]
            param([Parameter(ValueFromPipeline = $true)][string] $i)

            write-error  'bar-error'
            write-output 'bar-output'
            get-foo
        }

        (get-foo -errorVariable foo_err | get-bar3 -errorVariable bar_err) 2>&1 > $null

        $foo_err | Should -BeExactly 'foo-error'
        $bar_err | Should -BeExactly @("bar-error", "foo-error")
    }

    It 'multi-command pipeline with nested commands' {

        function get-foo6
        {
            [CmdletBinding()]
            param([Parameter(ValueFromPipeline = $true)][string] $i)

            write-error  "foo-error"
            write-output $i
        }

        function get-bar4
        {
            [CmdletBinding()]
            param([Parameter(ValueFromPipeline = $true)][string] $i)

            write-error  "bar-error"
            get-foo6 "foo-output" -errorVariable script:foo_err1 | get-foo6 -errorVariable script:foo_err2
        }

        get-bar4 -errorVariable script:bar_err 2>&1 > $null

        $script:foo_err1 | Should -BeExactly "foo-error"
        $script:foo_err2 | Should -BeExactly "foo-error"
        $script:bar_err | Should -BeExactly @("bar-error", "foo-error")
    }

    It 'Nested output variables' {
        function get-foo7
        {
            [CmdletBinding()]
            param([Parameter(ValueFromPipeline = $true)][string] $output)

            $output
            write-error "foo-error"
        }

        function get-bar5
        {
            [CmdletBinding()]
            param()

            "bar-output"
            write-error  "bar-error"
            get-foo7 "foo-output" -ErrorVariable script:foo_err1 -ov script:foo_out1 | get-foo7 -ErrorVariable script:foo_err2 -ov script:foo_out2
            get-foo7 "foo-output" -ErrorVariable script:foo_err3 -ov script:foo_out3 | get-foo7 -ErrorVariable script:foo_err4 -ov script:foo_out4
        }

        get-bar5 -ErrorVariable script:bar_err -ov script:bar_out 2>&1 > $null

        $script:foo_out1 | Should -BeExactly "foo-output"
        $script:foo_err1 | Should -BeExactly "foo-error"

        $script:foo_out2 | Should -BeExactly "foo-output"
        $script:foo_err2 | Should -BeExactly "foo-error"

        $script:foo_out3 | Should -BeExactly "foo-output"
        $script:foo_err3 | Should -BeExactly "foo-error"

        $script:foo_out4 | Should -BeExactly "foo-output"
        $script:foo_err4 | Should -BeExactly "foo-error"

        $script:bar_out | Should -BeExactly @("bar-output", "foo-output", "foo-output")
        $script:bar_err | Should -BeExactly @("bar-error", "foo-error", "foo-error")
    }
}

