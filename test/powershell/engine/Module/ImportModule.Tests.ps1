# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

function New-SimpleModule
{
    $moduleContent = @'
function Test-FirstModuleFunction
{
    Write-Output "TESTSTRING"
}
'@

    New-Item -Force -Path $TestDrive -Name "simple.psm1" -Value $moduleContent
}

Describe "Import-Module by name" -Tag Feature {

    BeforeAll {
        $modPath = (New-SimpleModule).PSPath
        $modRef = Import-Module $modPath -PassThru
    }

    BeforeEach {
        Get-Module $modRef.Name | Remove-Module
    }

    Context "Simple psm1 module" {
        It "Imports a simple module" {
            $modItem = New-SimpleModule
            Import-Module $modItem.PSPath
            Test-FirstModuleFunction | Should -BeExactly "TESTSTRING"
        }

        It "Passes the imported module out when -PassThru is used" {
            $modItem = New-SimpleModule
            $mod = Import-Module $modItem.PSPath -PassThru
            $mod.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeExactly $true
        }

        It "Passes the imported module out as a custom object when -AsCustomObject is used" {
            $modItem = New-SimpleModule
            $mod = Import-Module $modItem.PSPath -AsCustomObject
            $mod.GetType() | Should -BeExactly "PSCustomObject"
            $mod.'Test-FirstModuleFunction'() | Should -BeExactly "TESTSTRING"
        }

        It "Re-imports modules from file with new members when -Force is used" {
            $modItem = New-SimpleModule
            $module = Import-Module $modItem.PSPath -PassThru

            $module.ExportedFunctions.Count | Should -BeExactly 1
            $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeExactly $true
            $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeExactly $false

            New-Item -Force -Path $modItem.PSPath -Value (((Get-Content $modItem) | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }")

            $module = Import-Module $modItem.PSPath -PassThru -Force
            Test-SecondModuleFunction | Should -BeExactly "SECONDSTRING"
        }

        It "Re-imports modules from file with new members when -Force is used with -PassThru" {
            $modItem = New-SimpleModule
            $module = Import-Module $modItem.PSPath -PassThru

            $module.ExportedFunctions.Count | Should -BeExactly 1
            $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeExactly $true
            $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeExactly $false

            New-Item -Force -Path $modItem.PSPath -Value (((Get-Content $modItem) | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }")

            $module = Import-Module $modItem.PSPath -PassThru -Force
            $module.ExportedFunctions.Count | Should -BeExactly 2
            $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeExactly $true
            $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeExactly $true
        }

        It "Re-imports modules from file with new members when -Force is used with -AsCustomObject" {
            $modItem = New-SimpleModule
            $module = Import-Module $modItem.PSPath -PassThru

            $module.ExportedFunctions.Count | Should -BeExactly 1
            $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeExactly $true
            $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeExactly $false

            New-Item -Force -Path $modItem.PSPath -Value (((Get-Content $modItem) | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }")

            $module = Import-Module $modItem.PSPath -AsCustomObject -Force
            $module.'Test-SecondModuleFunction'() | Should -BeExactly "SECONDSTRING"
        }
    }

    Context "Parameter handling" {
        BeforeAll {
            $validationTests = @(
                @{ paramName="Variable" },
                @{ paramName="Function" },
                @{ paramName="Cmdlet" },
                @{ paramName="Variable" }
            )

            $ps = [powershell]::Create()
        }

        AfterAll {
            $ps.Dispose()
        }

        It "Validates a null -<paramName> parameter" -TestCases $validationTests {
            param($paramName)

            $modItem = New-SimpleModule
            try
            {
                $ps.AddScript("Import-Module $($modItem.PSPath) -$paramName `$null")
                $ps.Invoke()
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should -BeExactly "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.ImportModuleCommand"
            }
        }

        It "Ignores whitespace -MaximumVersion parameter" {
            $modItem = New-SimpleModule
            Import-Module $modItem.PSPath -MaximumVersion "    "
            Test-FirstModuleFunction | Should -BeExactly "TESTSTRING"
        }
    }
}

Describe "Import-Module by PSModuleInfo" -Tag Feature {
    BeforeAll {
        $modPath = (New-SimpleModule).PSPath
        $modRef = Import-Module $modPath -PassThru
    }

    BeforeEach {
        Get-Module $modRef.Name | Remove-Module
        $modItem = New-SimpleModule
        $modInfo = (Get-Module $modItem.PSPath -ListAvailable)[0]
    }

    It "Imports a module by moduleinfo" {
        try
        {
            Test-FirstModuleFunction
        }
        catch
        {
            $_.Exception | Should -BeOfType [System.Management.Automation.CommandNotFoundException]
        }

        Import-Module $modInfo
        Test-FirstModuleFunction | Should -BeExactly "TESTSTRING"
    }

    It "Passes the imported module out when -PassThru is used" {
        $mod = Import-Module $modInfo -PassThru
        $mod.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeExactly $true
    }

    It "Passes the imported module out as a custom object when -AsCustomObject is used" {
        $mod = Import-Module $modInfo -AsCustomObject
        $mod.GetType() | Should -BeExactly "PSCustomObject"
        $mod.'Test-FirstModuleFunction'() | Should -BeExactly "TESTSTRING"
    }

    It "Re-imports modules from file with new members when -Force is used" {
        $module = Import-Module $modInfo -PassThru

        $module.ExportedFunctions.Count | Should -BeExactly 1
        $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeExactly $true
        $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeExactly $false

        New-Item -Force -Path $modItem.PSPath -Value (((Get-Content $modItem) | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }")

        $module = Import-Module $modInfo -PassThru -Force
        Test-SecondModuleFunction | Should -BeExactly "SECONDSTRING"
    }

    It "Re-imports modules from file with new members when -Force is used with -PassThru" {
        $module = Import-Module $modInfo -PassThru

        $module.ExportedFunctions.Count | Should -BeExactly 1
        $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeExactly $true
        $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeExactly $false

        New-Item -Force -Path $modItem.PSPath -Value (((Get-Content $modItem) | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }")

        $module = Import-Module $modInfo -PassThru -Force
        $module.ExportedFunctions.Count | Should -BeExactly 2
        $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeExactly $true
        $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeExactly $true
    }

    It "Re-imports modules from file with new members when -Force is used with -AsCustomObject" {
        $module = Import-Module $modInfo -PassThru

        $module.ExportedFunctions.Count | Should -BeExactly 1
        $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeExactly $true
        $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeExactly $false

        New-Item -Force -Path $modItem.PSPath -Value (((Get-Content $modItem) | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }")

        $module = Import-Module $modInfo -AsCustomObject -Force
        $module.'Test-SecondModuleFunction'() | Should -BeExactly "SECONDSTRING"
    }
}

Describe "Import-Module with nested modules" {
    BeforeAll {
        $modName = "nestMod"
        $modDir = "$TestDrive\$modName"
        $modPath = "$modDir\$modName.psm1"
        $subModPath = "$modDir\sub.psm1"
    }

    BeforeEach {
        New-Item -Path $modDir -ItemType Directory
    }

    AfterEach {
        Get-Module $modName | Remove-Module -Force
        Remove-Item -Path $modDir -Recurse -Force
    }

    It "Only uses submodule code indirectly without importing members" {
        $modSrc = @'
Import-Module $PSScriptRoot/sub.psm1

function Test-MainModuleFunc
{
    Test-SubModuleFunc
}
'@

        $subModSrc = @'
function Test-SubModuleFunc
{
    Write-Output "SUBMODULESTRING"
}
'@

        New-Item -Path $modPath -Value $modSrc -Force
        New-Item -Path $subModPath -Value $subModSrc -Force

        Import-Module $modPath

        Test-MainModuleFunc | Should -BeExactly "SUBMODULESTRING"

        try
        {
            Test-SubModuleFunc
        }
        catch
        {
            $_.Exception | Should -BeOfType [System.Management.Automation.CommandNotFoundException]
        }
    }

    It "Should resolve submodule classes with 'using module'" {
        $modSrc = @"
using module $subModPath

function Test-MainModuleFunc
{
    [Sub]::new()
}
"@

        $subModSrc = @'
class Sub
{
    [string]$Name

    Sub()
    {
        $this.Name = 'CLASSSTRING'
    }
}
'@

        New-Item -Path $modPath -Value $modSrc -Force
        New-Item -Path $subModPath -Value $subModSrc -Force

        Import-Module $modDir

        $subObj = Test-MainModuleFunc
        $subObj.Name | Should -BeExactly "CLASSSTRING"
    }

    It "Refreshes nested modules when -Force is used with Import-Module" {
$mainSrc = @'
Import-Module $PSScriptRoot/sub.psm1

function MainFunc
{
    SubFunc
}
'@

$sub1Src = @'
function SubFunc
{
    "FIRST"
}
'@

$sub2Src = @'
function SubFunc
{
    "SECOND"
}
'@

        New-Item -Path $modPath -Value $mainSrc
        New-Item -Path $subModPath -Value $sub1Src

        Import-Module $modDir

        MainFunc | Should -BeExactly "FIRST"

        New-Item -Path $subModPath -Value $sub2Src -Force

        Import-Module $modDir -Force

        MainFunc | Should -BeExactly "SECOND"
    }
}
