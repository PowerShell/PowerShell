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

Describe "Import-Module by name" -Tag "Feature" {

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
            $mod.ExportedFunctions.Keys | Should -Contain "Test-FirstModuleFunction"
        }

        It "Passes the imported module out as a custom object when -AsCustomObject is used" {
            $modItem = New-SimpleModule
            $mod = Import-Module $modItem.PSPath -AsCustomObject
            $mod | Should -BeOfType "PSCustomObject"
            $mod.'Test-FirstModuleFunction'() | Should -BeExactly "TESTSTRING"
        }

        It "Re-imports modules from file with new members when -Force is used" {
            $modItem = New-SimpleModule
            $module = Import-Module $modItem.PSPath -PassThru

            $module.ExportedFunctions.Count | Should -Be 1
            $module.ExportedFunctions.Keys | Should -Contain "Test-FirstModuleFunction"
            $module.ExportedFunctions.Keys | Should -Not -Contain "Test-SecondModuleFunction"

            New-Item -Force -Path $modItem.PSPath -Value (((Get-Content $modItem) | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }")

            $module = Import-Module $modItem.PSPath -PassThru -Force
            Test-SecondModuleFunction | Should -BeExactly "SECONDSTRING"
        }

        It "Re-imports modules from file with new members when -Force is used with -PassThru" {
            $modItem = New-SimpleModule
            $module = Import-Module $modItem.PSPath -PassThru

            $module.ExportedFunctions.Count | Should -Be 1
            $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeTrue
            $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeFalse

            New-Item -Force -Path $modItem.PSPath -Value (((Get-Content $modItem) | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }")

            $module = Import-Module $modItem.PSPath -PassThru -Force
            $module.ExportedFunctions.Count | Should -Be 2
            $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeTrue
            $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeTrue
        }

        It "Re-imports modules from file with new members when -Force is used with -AsCustomObject" {
            $modItem = New-SimpleModule
            $module = Import-Module $modItem.PSPath -PassThru

            $module.ExportedFunctions.Count | Should -Be 1
            $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeTrue
            $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeFalse

            New-Item -Force -Path $modItem.PSPath -Value (((Get-Content $modItem) | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }")

            $module = Import-Module $modItem.PSPath -AsCustomObject -Force
            $module.'Test-SecondModuleFunction'() | Should -BeExactly "SECONDSTRING"
        }
    }

    Context "Classes in modules" {
        It "Uses updated class definitions when the module is reloaded" {
            $content = @'
$passedArgs = $Args
class Root { $passedArgs = $passedArgs }
function Get-PassedArgsRoot { [Root]::new().passedArgs }
function Get-PassedArgsNoRoot { $passedArgs }
'@

            $rootModName = "rootMod"
            $rootModDir = "$TestDrive\$rootModName"
            New-Item -Path $rootModDir -ItemType Directory
            New-Item -Path $rootModDir -Name "$rootModName.psm1" -Value $content

            Import-Module $rootModDir -ArgumentList 'value1'
            $rootVal = Get-PassedArgsRoot
            $noRootVal = Get-PassedArgsNoRoot
            $rootVal | Should -BeExactly $noRootVal

            Import-Module $rootModDir -ArgumentList 'value2'
            $rootVal = Get-PassedArgsRoot
            $rootNoVal = Get-PassedArgsNoRoot
            $rootVal | Should -BeExactly $noRootVal
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

        AfterEach {
            $ps.Streams.ClearStreams()
        }

        It "Validates a null -<paramName> parameter" -TestCases $validationTests {
            param($paramName)

            $modItem = New-SimpleModule
            $sb = [scriptblock]::Create("Import-Module $($modItem.PSPath) -$paramName `$null")
            $sb | Should -Throw -ErrorId "ParameterArgumentValidationError"
        }

        It "Ignores whitespace -MaximumVersion parameter" {
            $modItem = New-SimpleModule
            Import-Module $modItem.PSPath -MaximumVersion "    "
            Test-FirstModuleFunction | Should -BeExactly "TESTSTRING"
        }
    }
}

Describe "Import-Module by PSModuleInfo" -Tag "Feature" {
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
        { Test-FirstModuleFunction } | Should -Throw -ErrorId "CommandNotFoundException"

        Import-Module $modInfo
        Test-FirstModuleFunction | Should -BeExactly "TESTSTRING"
    }

    It "Passes the imported module out when -PassThru is used" {
        $mod = Import-Module $modInfo -PassThru
        $mod.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeTrue
    }

    It "Passes the imported module out as a custom object when -AsCustomObject is used" {
        $mod = Import-Module $modInfo -AsCustomObject
        $mod.GetType() | Should -BeExactly "PSCustomObject"
        $mod.'Test-FirstModuleFunction'() | Should -BeExactly "TESTSTRING"
    }

    It "Re-imports modules from file with new members when -Force is used" {
        $module = Import-Module $modInfo -PassThru

        $module.ExportedFunctions.Count | Should -Be 1
        $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeTrue
        $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeFalse

        New-Item -Force -Path $modItem.PSPath -Value (((Get-Content $modItem) | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }")

        $module = Import-Module $modInfo -PassThru -Force
        Test-SecondModuleFunction | Should -BeExactly "SECONDSTRING"
    }

    It "Re-imports modules from file with new members when -Force is used with -PassThru" {
        $module = Import-Module $modInfo -PassThru

        $module.ExportedFunctions.Count | Should -Be 1
        $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeTrue
        $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeFalse

        New-Item -Force -Path $modItem.PSPath -Value (((Get-Content $modItem) | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }")

        $module = Import-Module $modInfo -PassThru -Force
        $module.ExportedFunctions.Count | Should -Be 2
        $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeTrue
        $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeTrue
    }

    It "Re-imports modules from file with new members when -Force is used with -AsCustomObject" {
        $module = Import-Module $modInfo -PassThru

        $module.ExportedFunctions.Count | Should -Be 1
        $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction") | Should -BeTrue
        $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeFalse

        New-Item -Force -Path $modItem.PSPath -Value (((Get-Content $modItem) | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }")

        $module = Import-Module $modInfo -AsCustomObject -Force
        $module.'Test-SecondModuleFunction'() | Should -BeExactly "SECONDSTRING"
    }
}

Describe "Import-Module with nested modules" -Tag "Feature" {
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

    It "Resolves submodule classes with 'using module'" {
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

    It "Uses cached class definitions in non-force-reloaded submodules" {
        $modName = "subClassMod"
        $subModName = "SubObj"
        $modPath = "$TestDrive\$modName"

        Get-Module $modName | Remove-Module -Force
        if (Test-Path $modPath)
        {
            Remove-Item $modPath -Force -Recurse
        }

        $mainModSrc = @"
using module $modPath\SubObj.psm1
function Test-SubClassMain { [SubObj]::new().Id }
"@

        $subModSrc1 = @'
class SubObj
{
    [string]$Id

    SubObj()
    {
        $this.Id = "FIRST"
    }
}
'@

        $subModSrc2 = @'
class SubObj
{
    [string]$Id

    SubObj()
    {
        $this.Id = "SECOND"
    }
}
'@

        New-Item -Path $modPath -ItemType Directory

        New-Item -Path $modPath -Name "$modName.psm1" -Value $mainModSrc
        New-Item -Path $modPath -Name "$subModName.psm1" -Value $subModSrc1

        Import-Module $modPath

        Test-SubClassMain | Should -BeExactly "FIRST"

        Set-Content -Path "$modPath\$subModName.psm1" -Value $subModSrc2 -Force

        Import-Module $modPath

        Test-SubClassMain | Should -BeExactly "FIRST"
    }

    It "Uses updated class definitions in force-reloaded submodules" {
        $modName = "subClassMod"
        $subModName = "SubObj"
        $modPath = "$TestDrive\$modName"

        Get-Module $modName | Remove-Module -Force
        Get-Module $subModName | Remove-Module -Force
        if (Test-Path $modPath) { Remove-Item $modPath -Force -Recurse }

        $mainModSrc = @"
using module $modPath\SubObj.psm1
function Test-SubClassMain { [SubObj]::new().Id }
"@

        $subModSrc1 = @'
class SubObj
{
    [string]$Id

    SubObj()
    {
        $this.Id = "FIRST"
    }
}
'@

        $subModSrc2 = @'
class SubObj
{
    [string]$Id

    SubObj()
    {
        $this.Id = "SECOND"
    }
}
'@

        New-Item -Path $modPath -ItemType Directory

        New-Item -Path $modPath -Name "$modName.psm1" -Value $mainModSrc
        New-Item -Path $modPath -Name "$subModName.psm1" -Value $subModSrc1

        Import-Module $modPath

        Test-SubClassMain | Should -BeExactly "FIRST"

        Set-Content -Path "$modPath\$subModName.psm1" -Value $subModSrc2 -Force

        Import-Module -Force $modPath

        Test-SubClassMain | Should -BeExactly "SECOND"
    }

    It "Uses updated class definitions in later imports rather than cached values" {
        $subSrc1 = @'
class Sub
{
    [string]$Name

    Sub()
    {
        $this.Name = "X"
    }
}
'@

        $subSrc2 = @'
class Sub
{
    [string]$Name

    Sub()
    {
        $this.Name = "Y"
    }
}
'@
        $subName = "subStandalone.psm1"
        $subPath = "$TestDrive\$subName"

        New-Item -Path $subPath -Value $subSrc1
        $sub1 = Import-Module $subPath -PassThru

        Set-Content -Path $subPath -Value $subSrc2
        $sub2 = Import-Module $subPath -PassThru

        $firstTypes = $sub1.GetExportedTypeDefinitions()
        $secondTypes = $sub2.GetExportedTypeDefinitions()

        $firstTypes.Sub.Equals($secondTypes.Sub) | Should -BeTrue
    }
}
