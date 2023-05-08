# Copyright (c) Microsoft Corporation.
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

            $PSCmdlet.writeobject("foo")
        }

        function get-bar
        {
            [CmdletBinding()]
            param()

            "bar"
            get-foo1 -OutVariable script:a
        }
    }

    $testdata = @(
                    @{ Name = 'Updating OutVariable Case 1: pipe string';
                        Command = "get-foo1";
                        OutVariable = 'a';
                        Expected = 'foo'
                        },
                    @{ Name = 'Updating OutVariable Case 2: $PSCmdlet.writeobject';
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
                    @{ Name = 'Appending OutVariable Case 2: $PSCmdlet.writeobject';
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

        get-bar -OutVariable b > $null
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

            Write-Error "foo"
        }

        function get-foo2
        {
            [CmdletBinding()]
            param()

            $PSCmdlet.WriteError($script:foo[0])
        }

        function get-bar
        {
            [CmdletBinding()]
            param()

            Write-Error "bar"
            get-foo1 -ErrorVariable script:a
        }
    }

    $testdata1 = @(
                    @{ Name = 'Updating ErrorVariable Case 1: write-error';
                       Command = "get-foo1";
                       ErrorVariable = 'a';
                       Expected = 'foo'
                     },
                     @{ Name = 'Updating ErrorVariable Case 2: $PSCmdlet.WriteError';
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

    It 'Appending ErrorVariable Case 2: $PSCmdlet.writeerror' {
        Write-Error "foo" -ErrorVariable script:foo 2> $null
        $a = 'a','b'

        get-foo2 -ErrorVariable +a 2> $null

        $a.count | Should -Be 3
        $a| ForEach-Object {$_.ToString()} | Should -BeExactly @('a', 'b', 'foo')
    }

    It 'Nested ErrorVariable' {

        get-bar -ErrorVariable b 2> $null

        $script:a | Should -BeExactly 'foo'
        $b | Should -BeExactly @("bar","foo")
    }

    It 'Nested ErrorVariable with redirection' {

        get-bar -ErrorVariable b 2>&1 > $null

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

          Write-Output "foo-output"
          Write-Error  "foo-error"
        }

        function get-foo1
        {
            [CmdletBinding()]
            param()

            Write-Error "foo"
        }

        function get-foo2
        {
            [CmdletBinding()]
            param()

            $PSCmdlet.WriteError($script:foo[0])
        }

        function get-bar
        {
            [CmdletBinding()]
            param()

            Write-Error "bar"
            get-foo1 -ErrorVariable script:a
        }

        function get-foo3
        {
            [CmdletBinding()]
            param()

            "foo-output-0"
            Write-Output "foo-output-1"
            Write-Error "foo-error"
        }

        function get-bar2
        {
            [CmdletBinding()]
            param()

            "bar-output-0"
            Write-Output "bar-output-1"
            Write-Error "bar-error"
            get-foo3 -OutVariable script:foo_out -ErrorVariable script:foo_err
        }
    }

    It 'Update OutVariable and ErrorVariable' {

        get-foo3 -OutVariable out -ErrorVariable err 2> $null > $null

        $out | Should -BeExactly @("foo-output-0", "foo-output-1")
        $err | Should -BeExactly "foo-error"
    }

    It 'Update OutVariable and ErrorVariable' {

        get-bar2 -OutVariable script:bar_out -ErrorVariable script:bar_err 2> $null > $null

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

            Write-Error "foo-error"

            try
            {
                throw "foo-exception"
            }
            catch
            {}
        }

        get-foo4 -ErrorVariable err 2> $null

        $err | Should -BeExactly @("foo-error", "foo-exception")
    }

    It 'Error variable in multi-command pipeline' {
        function get-foo5
        {
          [CmdletBinding()]
          param([Parameter(ValueFromPipeline = $true)][string] $foo)

          process
          {
            Write-Output $foo
            Write-Error  $foo
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
            (get-foo -ErrorVariable foo_err | Get-Item -ErrorVariable get_item_err ) 2>&1 > $null
        }

        It '$foo_err should be "foo-error"' {
            $foo_err | Should -BeExactly "foo-error"
        }

        It '$get_item_err.count and $get_item_err[0].exception' {
            $get_item_err.count | Should -Be 1
            $get_item_err[0].exception | Should -Not -BeNullOrEmpty
            $get_item_err[0].exception | Should -BeOfType System.Management.Automation.ItemNotFoundException
        }
    }

    It 'Multi-command pipeline with nested commands' {

        function get-bar3
        {
            [CmdletBinding()]
            param([Parameter(ValueFromPipeline = $true)][string] $i)

            Write-Error  'bar-error'
            Write-Output 'bar-output'
            get-foo
        }

        (get-foo -ErrorVariable foo_err | get-bar3 -ErrorVariable bar_err) 2>&1 > $null

        $foo_err | Should -BeExactly 'foo-error'
        $bar_err | Should -BeExactly @("bar-error", "foo-error")
    }

    It 'multi-command pipeline with nested commands' {

        function get-foo6
        {
            [CmdletBinding()]
            param([Parameter(ValueFromPipeline = $true)][string] $i)

            Write-Error  "foo-error"
            Write-Output $i
        }

        function get-bar4
        {
            [CmdletBinding()]
            param([Parameter(ValueFromPipeline = $true)][string] $i)

            Write-Error  "bar-error"
            get-foo6 "foo-output" -ErrorVariable script:foo_err1 | get-foo6 -ErrorVariable script:foo_err2
        }

        get-bar4 -ErrorVariable script:bar_err 2>&1 > $null

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
            Write-Error "foo-error"
        }

        function get-bar5
        {
            [CmdletBinding()]
            param()

            "bar-output"
            Write-Error  "bar-error"
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

