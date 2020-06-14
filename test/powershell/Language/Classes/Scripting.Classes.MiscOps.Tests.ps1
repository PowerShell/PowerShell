# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'Misc Test' -Tags "CI" {

    Context 'Where' {
        class C1 {
        [int[]] $Wheels = @(1,2,3);
        [string] Foo() {
            return (1..10).Where({ $PSItem -in $this.Wheels; }) -join ';'
        }

        [string] Bar()
        {
             return (1..10 | Where-Object  { $PSItem -in $this.Wheels; }) -join ';'
        }
        }
        It 'Invoke Where' {
                [C1]::new().Foo() | Should -Be "1;2;3"
        }
        It 'Pipe to where' {
                [C1]::new().Bar() | Should -Be "1;2;3"
        }
    }

    Context 'ForEach' {
        class C1 {
        [int[]] $Wheels = @(1,2,3);
        [string] Foo() {
            $ret=""
            Foreach($PSItem in $this.Wheels) { $ret +="$PSItem;"}
            return $ret
        }

        [string] Bar()
        {
            $ret = ""
            $this.Wheels | ForEach-Object { $ret += "$_;" }
            return $ret
        }
        }
        It 'Invoke Foreach' {
                [C1]::new().Foo() | Should -Be "1;2;3;"
        }
        It 'Pipe to Foreach' {
                [C1]::new().Bar() | Should -Be "1;2;3;"
        }
    }

    Context 'Class instantiation' {
        Class C1 {
            [string] Foo() {
                return (Get-TestText)
            }
        }

        BeforeAll {
            $ExpectedTextFromBoundInstance   = "Class C1 was defined in this Runspace"
            $ExpectedTextFromUnboundInstance = "New Runspace without class C1 defined"

            ## Define 'Get-TestText' in the current Runspace
            function Get-TestText { return $ExpectedTextFromBoundInstance }

            $NewRunspaceFunctionDefinitions = @"
    ## Define 'Get-TestText' in the new Runspace
    function Get-TestText { return '$ExpectedTextFromUnboundInstance' }

    ## Define the function to create an instance of the given type using the default constructor
    function New-UnboundInstance([Type]`$type) { `$type::new() }

    ## Define the function to call 'Foo()' on the given C1 instance, and return the result
    function Run-Foo(`$C1Instance) { `$C1Instance.Foo() }
"@
            ## Create a new Runspace and define helper functions in it
            $powershell = [powershell]::Create()
            $powershell.AddScript($NewRunspaceFunctionDefinitions).Invoke() > $null
            $powershell.Commands.Clear()

            function InstantiateInNewRunspace([Type]$type) {
                try {
                    $result = $powershell.AddCommand("New-UnboundInstance").AddParameter("type", $type).Invoke()
                    $result.Count | Should -Be 1
                    return $result[0]
                } finally {
                    $powershell.Commands.Clear()
                }
            }

            function RunFooInNewRunspace($instance) {
                try {
                    $result = $powershell.AddCommand("Run-Foo").AddParameter("C1Instance", $instance).Invoke()
                    $result.Count | Should -Be 1
                    return $result[0]
                } finally {
                    $powershell.Commands.Clear()
                }
            }
        }

        AfterAll {
            $powershell.Dispose()
        }

        It "Create instance that is bound to a SessionState" {
            $instance = [C1]::new()
            ## For a bound class instance, the execution of an instance method is
            ## done in the Runspace/SessionState the instance is bound to.
            $instance.Foo() | Should -BeExactly $ExpectedTextFromBoundInstance
            RunFooInNewRunspace $instance | Should -BeExactly $ExpectedTextFromBoundInstance
        }

        It "Create instance that is NOT bound to a SessionState" {
            $instance = InstantiateInNewRunspace ([C1])
            ## For an unbound class instance, the execution of an instance method is done in
            ## the Runspace/SessionState where the call to the instance method is made.
            $instance.Foo() | Should -BeExactly $ExpectedTextFromBoundInstance
            RunFooInNewRunspace $instance | Should -BeExactly $ExpectedTextFromUnboundInstance
        }
    }
}
