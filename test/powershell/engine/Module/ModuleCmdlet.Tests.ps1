# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$script:ModuleNames = [System.Collections.ArrayList]::new()

function New-TestModule
{
    param(
        [string]$Content
    )

    $guid = [guid]::NewGuid()
    $modBasePath = Join-Path -Path $TestDrive -ChildPath $guid
    $modName = "$guid.psm1"
    $modPath = Join-Path -Path $modBasePath -ChildPath $modName

    $modDir = New-Item -Path $modBasePath -ItemType Directory -Force
    $null = New-Item -Path $modPath -Value $Content -Force

    $null = $script:ModuleNames.Add($modName)

    @{
        "BaseDir" = $modDir;
        "Name" = $modName;
        "Path" = $modPath
    }
}

function New-TestModuleWithSubModule
{
    param(
        [string]$MainModContent,
        [string]$SubModContent
    )

    $guid = [guid]::NewGuid()
    $modBasePath = Join-Path -Path $TestDrive -ChildPath $guid
    $modName = "$guid.psm1"
    $subModName = "Sub.psm1"
    $modPath = Join-Path -Path $modBasePath -ChildPath $modName
    $subModPath = Join-Path -Path $modBasePath -ChildPath $subModName

    $modDir = New-Item -Path $modBasePath -ItemType Directory -Force
    $null = New-Item -Path $modPath -Value ("using module $subModPath`n" + $MainModContent)
    $null = New-Item -Path $subModPath -Value $SubModContent

    $null = $script:ModuleNames.Add($modName)

    @{
        "BaseDir" = $modDir;
        "Name" = $modName;
        "SubName" = $subModName;
        "Path" = $modPath;
        "SubPath" = $subModPath;
    }
}

function Remove-TestModule
{
    [cmdletbinding()]
    param(
        [parameter(ValueFromPipeline)]
        [psmoduleinfo]$ModuleInfo
    )

    process
    {
        Remove-Module -ModuleInfo $_ -Force
        if (Test-Path $_.ModuleBase)
        {
            Remove-Item -Path $_.ModuleBase -Recurse -Force
        }
    }
}

function SetupModules
{
    $script:ModuleNames = [System.Collections.ArrayList]::new()
}

function TearDownModules
{
    Get-Module $script:ModuleNames | Remove-TestModule
}

Describe "Import-Module by name" -Tag "Feature" {

    BeforeAll {
        SetupModules

        $simpleModContent = @'
function Test-FirstModuleFunction
{
    Write-Output "TESTSTRING"
}
'@
    }

    AfterAll {
        TearDownModules
    }

    Context "Simple psm1 module" {
        It "Imports a simple module" {
            $modData = New-TestModule -Content $simpleModContent
            Import-Module $modData.Path
            Test-FirstModuleFunction | Should -BeExactly "TESTSTRING"
        }

        It "Passes the imported module out when -PassThru is used" {
            $modData = New-TestModule -Content $simpleModContent
            $mod = Import-Module $modData.Path -PassThru
            $mod.ExportedFunctions.Keys | Should -Contain "Test-FirstModuleFunction"
        }

        It "Passes the imported module out as a custom object when -AsCustomObject is used" {
            $modData = New-TestModule -Content $simpleModContent
            $mod = Import-Module $modData.Path -AsCustomObject
            $mod | Should -BeOfType "PSCustomObject"
            $mod.'Test-FirstModuleFunction'() | Should -BeExactly "TESTSTRING"
        }

        It "Re-imports modules from file with new members when -Force is used" {
            $modData = New-TestModule -Content $simpleModContent
            $module = Import-Module $modData.Path -PassThru

            $module.ExportedFunctions.Count | Should -Be 1
            $module.ExportedFunctions.Keys  | Should -Contain "Test-FirstModuleFunction"
            $module.ExportedFunctions.Keys  | Should -Not -Contain "Test-SecondModuleFunction"

            $newModuleContent = (Get-Content $modData.Path | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }"
            Set-Content -Force -Path $modData.Path -Value $newModuleContent

            $module = Import-Module $modData.Path -PassThru -Force
            Test-SecondModuleFunction | Should -BeExactly "SECONDSTRING"
        }

        It "Re-imports modules from file with new members when -Force is used with -PassThru" {
            $modData = New-TestModule -Content $simpleModContent
            $module = Import-Module $modData.Path -PassThru

            $module.ExportedFunctions.Count                                    | Should -Be 1
            $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction")  | Should -BeTrue
            $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeFalse

            $newModuleContent = (Get-Content $modData.Path | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }"
            Set-Content -Force -Path $modData.Path -Value $newModuleContent

            $module = Import-Module $modData.Path -PassThru -Force
            $module.ExportedFunctions.Count                                    | Should -Be 2
            $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction")  | Should -BeTrue
            $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeTrue
        }

        It "Re-imports modules from file with new members when -Force is used with -AsCustomObject" {
            $modData = New-TestModule -Content $simpleModContent
            $module = Import-Module $modData.Path -PassThru

            $module.ExportedFunctions.Count                                    | Should -Be 1
            $module.ExportedFunctions.ContainsKey("Test-FirstModuleFunction")  | Should -BeTrue
            $module.ExportedFunctions.ContainsKey("Test-SecondModuleFunction") | Should -BeFalse

            $newModuleContent = (Get-Content $modData.Path | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }"
            Set-Content -Force -Path $modData.Path -Value $newModuleContent

            $module = Import-Module $modData.Path -AsCustomObject -Force
            $module.'Test-SecondModuleFunction'() | Should -BeExactly "SECONDSTRING"
        }
    }

    Context "Classes in modules" {
        BeforeAll {
            SetupModules
        }

        AfterAll {
            TearDownModules
        }

        It "Uses updated class definitions when the module is reloaded" {
            $content = @'
$passedArgs = $Args
class Root { $passedArgs = $passedArgs }
function Get-PassedArgsRoot { [Root]::new().passedArgs }
function Get-PassedArgsNoRoot { $passedArgs }
'@

            $modData = New-TestModule -Content $content

            Import-Module $modData.Path -ArgumentList 'value1'
            $rootVal = Get-PassedArgsRoot
            $noRootVal = Get-PassedArgsNoRoot
            $rootVal | Should -BeExactly $noRootVal

            Import-Module $modData.Path -ArgumentList 'value2'
            $rootVal = Get-PassedArgsRoot
            $rootNoVal = Get-PassedArgsNoRoot
            $rootVal | Should -BeExactly $rootNoVal
        }

        It "Uses updated class definitions in later imports rather than cached values" {
            $modSrc1 = @'
class MyObj
{
    [string]$Name

    Sub()
    {
        $this.Name = "X"
    }
}
'@

            $modSrc2 = @'
class MyObj
{
    [string]$Name

    Sub()
    {
        $this.Name = "Y"
    }
}
'@
            $modData = New-TestModule -Content $modSrc1

            $mod1 = Import-Module $modData.Path -PassThru

            Set-Content -Path $modData.Path -Value $subSrc2
            $mod2 = Import-Module $modData.Path -PassThru

            $firstTypes = $mod1.GetExportedTypeDefinitions()
            $secondTypes = $mod2.GetExportedTypeDefinitions()

            $firstTypes.MyObj.Equals($secondTypes.Sub) | Should -BeFalse
        }
    }

    Context "Parameter handling" {
        BeforeAll {
            SetupModules

            $validationTests = @(
                @{ paramName="Variable" },
                @{ paramName="Function" },
                @{ paramName="Cmdlet" },
                @{ paramName="Variable" }
            )

            $simpleModContent = @'
function Test-FirstModuleFunction
{
    Write-Output "TESTSTRING"
}
'@

            $ps = [powershell]::Create()
        }

        AfterAll {
            TearDownModules
            $ps.Dispose()
        }

        AfterEach {
            $ps.Streams.ClearStreams()
        }

        It "Validates a null -<paramName> parameter" -TestCases $validationTests {
            param($paramName)

            $modData = New-TestModule $simpleModContent
            $sb = [scriptblock]::Create("Import-Module $($modData.Path) -$paramName `$null")
            $sb | Should -Throw -ErrorId "ParameterArgumentValidationError"
        }

        It "Ignores whitespace -MaximumVersion parameter" {
            $modData = New-TestModule $simpleModContent
            Import-Module $modData.Path -MaximumVersion "    "
            Test-FirstModuleFunction | Should -BeExactly "TESTSTRING"
        }
    }
}

Describe "Import-Module by PSModuleInfo" -Tag "Feature" {
    BeforeAll {
        SetupModules

        $simpleModContent = @'
function Test-FirstModuleFunction
{
    Write-Output "TESTSTRING"
}
'@
    }

    AfterAll {
        TearDownModules
    }

    BeforeEach {
        $modData = New-TestModule -Content $simpleModContent
        $modInfo = Import-Module $modData.Path -PassThru
        Remove-Module $modInfo
    }

    It "Imports a module by moduleinfo" {
        { Test-FirstModuleFunction } | Should -Throw -ErrorId "CommandNotFoundException"

        Import-Module $modInfo

        Test-FirstModuleFunction | Should -BeExactly "TESTSTRING"
    }

    It "Passes the imported module out when -PassThru is used" {
        $mod = Import-Module $modInfo -PassThru

        $mod.ExportedFunctions.Keys | Should -Contain "Test-FirstModuleFunction"
    }

    It "Passes the imported module out as a custom object when -AsCustomObject is used" {
        $mod = Import-Module $modInfo -AsCustomObject

        $mod                              | Should -BeOfType "PSCustomObject"
        $mod.'Test-FirstModuleFunction'() | Should -BeExactly "TESTSTRING"
    }

    It "Re-imports modules from file with new members when -Force is used" {
        $module = Import-Module $modInfo -PassThru

        $module.ExportedFunctions.Count | Should -Be 1
        $module.ExportedFunctions.Keys  | Should -Contain "Test-FirstModuleFunction"
        $module.ExportedFunctions.Keys  | Should -Not -Contain "Test-SecondModuleFunction"

        $newModuleContent = (Get-Content $modData.Path | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }"
        Set-Content -Force -Path $modData.Path -Value $newModuleContent

        Import-Module $modInfo -Force
        Test-SecondModuleFunction | Should -BeExactly "SECONDSTRING"
    }

    It "Re-imports modules from file with new members when -Force is used with -PassThru" -Skip {
        $module = Import-Module $modInfo -PassThru

        $module.ExportedFunctions.Count | Should -Be 1
        $module.ExportedFunctions.Keys  | Should -Contain "Test-FirstModuleFunction"
        $module.ExportedFunctions.Keys  | Should -Not -Contain "Test-SecondModuleFunction"

        $newModuleContent = (Get-Content $modData.Path | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }"
        Set-Content -Force -Path $modData.Path -Value $newModuleContent

        $module = Import-Module $modInfo -PassThru -Force

        $module.ExportedFunctions.Count | Should -Be 2
        $module.ExportedFunctions.Keys  | Should -Contain "Test-FirstModuleFunction"
        $module.ExportedFunctions.Keys  | Should -Contain "Test-SecondModuleFunction"
    }

    It "Re-imports modules from file with new members when -Force is used with -AsCustomObject" -Skip {
        $module = Import-Module $modInfo -PassThru

        $module.ExportedFunctions.Count | Should -Be 1
        $module.ExportedFunctions.Keys  | Should -Contain "Test-FirstModuleFunction"
        $module.ExportedFunctions.Keys  | Should -Not -Contain "Test-SecondModuleFunction"

        $newModuleContent = (Get-Content $modData.Path | Out-String) + "`nfunction Test-SecondModuleFunction { Write-Output 'SECONDSTRING' }"
        Set-Content -Force -Path $modData.Path -Value $newModuleContent

        $module = Import-Module $modInfo -AsCustomObject -Force
        $module.'Test-SecondModuleFunction'() | Should -BeExactly "SECONDSTRING"
    }
}

Describe "Import-Module with nested modules" -Tag "Feature" {
    BeforeAll {
        SetupModules
    }

    AfterAll {
        TearDownModules
    }

    It "Resolves submodule classes with 'using module'" {
        $modSrc = @"
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
        $modData = New-TestModuleWithSubModule -MainModContent $modSrc -SubModContent $subModSrc

        Import-Module $modData.BaseDir

        $subObj = Test-MainModuleFunc
        $subObj.Name | Should -BeExactly "CLASSSTRING"
    }

    It "Refreshes nested modules when -Force is used with Import-Module" {
$mainSrc = @'
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
        $modData = New-TestModuleWithSubModule -MainModContent $mainSrc -SubModContent $sub1Src

        Import-Module $modData.BaseDir

        MainFunc | Should -BeExactly "FIRST"

        Set-Content -Path $modData.SubPath -Value $sub2Src -Force

        Import-Module $modData.BaseDir -Force

        MainFunc | Should -BeExactly "SECOND"
    }

    It "Uses cached class definitions in non-force-reloaded submodules" {
        $mainModSrc = @"
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
        $modData = New-TestModuleWithSubModule -MainModContent $mainModSrc -SubModContent $subModSrc1

        Import-Module $modData.BaseDir

        Test-SubClassMain | Should -BeExactly "FIRST"

        Set-Content -Path $modData.SubPath -Value $subModSrc2 -Force

        Import-Module $modData.BaseDir

        Test-SubClassMain | Should -BeExactly "FIRST"
    }

    It "Uses updated class definitions in force-reloaded submodules" {
        $mainModSrc = @"
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
        $modData = New-TestModuleWithSubModule -MainModContent $mainModSrc -SubModContent $subModSrc1

        Import-Module $modData.BaseDir

        Test-SubClassMain | Should -BeExactly "FIRST"

        Set-Content -Path $modData.SubPath -Value $subModSrc2 -Force

        Import-Module -Force $modData.BaseDir

        Test-SubClassMain | Should -BeExactly "SECOND"
    }
}
