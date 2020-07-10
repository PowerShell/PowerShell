# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Additional static method tests" -Tags "CI" {

    Context "Basic static member methods" {
        BeforeAll {
            function Get-Name { "YES" }
        }

        It "test basic static constructor" {
            class Foo {
                static [string] $Name
                static Foo() { [Foo]::Name = Get-Name }
            }

            [Foo]::Name | Should -Be "Yes"
        }

        It "test basic static method" {
            class Foo {
                static [string] GetName() { return (Get-Name) }
            }

            [Foo]::GetName() | Should -Be "Yes"
        }
    }

    Context "Class defined in different Runspace" {
        BeforeAll {
@'
class Foo
{
    static [string] $Name
    static Foo() { [Foo]::Name = Get-Name }

    static [string] GetName()
    {
        return (Get-AnotherName)
    }
}
'@ | Set-Content -Path $TestDrive\class.ps1 -Force

            ## Define the functions that [Foo] depends on in the default Runspace.
            function Get-Name { "Default Runspace - Name" }
            function Get-AnotherName { "Default Runspace - AnotherName" }

            ## Create another Runspace PS1
            $ps1 = [powershell]::Create()
            ## Create another Runspace PS2
            $ps2 = [powershell]::Create()

            function RunScriptInPS {
                param(
                    [powershell] $PowerShell,
                    [string] $Script,
                    [switch] $IgnoreResult
                )
                $result = $PowerShell.AddScript($Script).Invoke()
                $PowerShell.Commands.Clear()

                if (-not $IgnoreResult) {
                    return $result
                }
            }

            ## Define the functions that [Foo] depends on in PS1 Runspace.
            RunScriptInPS -PowerShell $ps1 -Script "function Get-Name { 'PS1 Runspace - Name' }" -IgnoreResult
            RunScriptInPS -PowerShell $ps1 -Script "function Get-AnotherName { 'PS1 Runspace - AnotherName' }" -IgnoreResult

            # Dot source class.ps1 in the current Runspace
            . $TestDrive\class.ps1
            # And then dot source class.ps1 in the PS1 Runspace
            RunScriptInPS -PowerShell $ps1 -Script ". $TestDrive\class.ps1" -IgnoreResult
        }

        AfterAll {
            # Dispose both Runspaces
            $ps1.Dispose()
            $ps2.Dispose()
        }

        It "Static constructor should run in the triggering Runspace if the class has been defined in that Runspace" {

            ## The static constructor is triggered by accessing '[Foo]::Name' which happens in the current Runspace.
            ## The class 'Foo' has been defined in the current Runspace, so it uses the current Runspace to run the
            ## static constructor.
            [Foo]::Name | Should -BeExactly "Default Runspace - Name"

            ## Static constructor runs only once, so accessing the Name property in the PS1 Runspace will just return
            ## the existing value.
            RunScriptInPS -PowerShell $ps1 -Script "[Foo]::Name" | Should -BeExactly "Default Runspace - Name"
        }

        It "Static method use the Runspace where the call happens if the class has been defined in that Runspace" {

            ## We call the static method in the current Runspace. The class 'Foo' has been defined
            ## in the current Runspace, so it will use it to run the method.
            [Foo]::GetName() | Should -BeExactly "Default Runspace - AnotherName"

            ## We call the static method in PS1 Runspace. The class 'Foo' has been defined in the
            ## PS1 Runspace, so it will use it to run the method.
            RunScriptInPS -PowerShell $ps1 -Script "[Foo]::GetName()" | Should -BeExactly 'PS1 Runspace - AnotherName'
        }

        It "Static method use the default SessionState if it's called in a Runspace where the class is not defined" {

            ## Define the functions that [Foo] depends on in PS2 Runspace.
            RunScriptInPS -PowerShell $ps2 -Script "function Get-Name { 'PS2 Runspace - Name' }" -IgnoreResult
            RunScriptInPS -PowerShell $ps2 -Script "function Get-AnotherName { 'PS2 Runspace - AnotherName' }" -IgnoreResult

            ## Define the function to call the static method 'GetName' on the passed-in type
            RunScriptInPS -PowerShell $ps2 -Script 'function Call-GetName([type] $type) { $type::GetName() }' -IgnoreResult

            ## We call the static method in PS2 Runspace. The class is not defined in this Runspace,
            ## so the default SessionState will be used to run the method. The default SessionState
            ## is always the one where the class was defined most recently. In this case, the class
            ## was lastly defined in PS1 Runspace, os the method will be invoked in PS1 Runspace.
            $result = $ps2.AddCommand("Call-GetName").AddParameter("type", [Foo]).Invoke()
            $result | Should -BeExactly 'PS1 Runspace - AnotherName'
        }
    }
}
