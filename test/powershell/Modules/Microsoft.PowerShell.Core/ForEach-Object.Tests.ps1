# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "ForEach-Object" -Tags "CI" {
    BeforeAll {
        $testModulePsd1 = Join-Path $TestDrive "ForEachObjectTest.psd1"
        $testModulePsm1 = Join-Path $TestDrive "ForEachObjectTest.psm1"
        Set-Content -Path $testModulePsm1 -Value @'
    function Zoo { "ForEachObjectTest-Zoo" }
    function GetScriptBlock { return { Zoo } }
'@
        New-ModuleManifest -Path $testModulePsd1 -RootModule $testModulePsm1 -FunctionsToExport "GetScriptBlock"
        Import-Module $testModulePsd1
    }

    AfterAll {
        Remove-Module -Name ForEachObjectTest
    }

    It "Foreach-Object should execute script block in caller scope" {
        $null = 1..2 | ForEach-Object { $bar = 100 }
        Get-Variable -Name bar -Scope 0 -ValueOnly | Should -BeExactly 100
    }

    It "Foreach-Object should execute script block in caller scope regardless of the invocation operator in use" {
        $null = 1..2 | . ForEach-Object { $bar = "bar" }
        $null = 1..2 | & ForEach-Object { $foo = "foo" }

        Get-Variable -Name bar -Scope 0 -ValueOnly | Should -BeExactly "bar"
        Get-Variable -Name foo -Scope 0 -ValueOnly | Should -BeExactly "foo"
    }

    It "Foreach-Object should execute script block in the module scope if specified" {
        { 1 | ForEach-Object { Zoo } } | Should -Throw -ErrorId "CommandNotFoundException"

        $m = Get-Module ForEachObjectTest
        1 | & $m ForEach-Object { Zoo } | Should -BeExactly "ForEachObjectTest-Zoo"
    }

    It "ForEach-Object should execute script block in the session state that the script block is associated with" {
        $sbToUse = GetScriptBlock
        1 | ForEach-Object $sbToUse | Should -BeExactly "ForEachObjectTest-Zoo"
    }

    It "ForEach-Object scriptblock should get the 'InvocationInfo' from the caller scope" {
        $file = New-Item TestDrive:\test.ps1 -ItemType File -Force
        Set-Content -Path $file -Value '1 | ForEach-Object { $MyInvocation.MyCommand.Name }'
        TestDrive:\test.ps1 | Should -BeExactly "test.ps1"
    }
}
