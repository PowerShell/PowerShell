# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Tests for paths of submodules in module manifest" -tags "CI" {

    $moduleName = 'ModuleA'
    $moduleFileName = "$moduleName.psd1"
    $submoduleName = 'ModuleB'
    $submoduleFileName = "$submoduleName.psm1"
    $moduleRootPath = Join-Path $TestDrive $moduleName
    $moduleFilePath = Join-Path $moduleRootPath $moduleFileName
    $nestedModulePath = Join-Path $moduleRootPath $submoduleName
    $nestedModuleFilePath = Join-Path $nestedModulePath $submoduleFileName

    BeforeEach {

        Remove-Module $moduleName -Force -ErrorAction SilentlyContinue
        Remove-Item $moduleRootPath -Recurse -Force -ErrorAction SilentlyContinue

        New-Item -ItemType Directory -Force -Path  $nestedModulePath
        "function TestModuleFunction{'Hello from TestModuleFunction'}" | Out-File $nestedModuleFilePath
    }

    $testCases = @(
        @{ SubModulePath = "$submoduleName" }
        @{ SubModulePath = "$submoduleName\$submoduleName" }
        @{ SubModulePath = "$submoduleName/$submoduleName" }
        @{ SubModulePath = "$submoduleName\$submoduleFileName" }
        @{ SubModulePath = "$submoduleName/$submoduleFileName" }
        @{ SubModulePath = ".\$submoduleName" }
        @{ SubModulePath = ".\$submoduleName\$submoduleName" }
        @{ SubModulePath = ".\$submoduleName/$submoduleName" }
        @{ SubModulePath = ".\$submoduleName\$submoduleFileName" }
        @{ SubModulePath = ".\$submoduleName/$submoduleFileName" }
        @{ SubModulePath = "./$submoduleName" }
        @{ SubModulePath = "./$submoduleName/$submoduleName" }
        @{ SubModulePath = "./$submoduleName\$submoduleName" }
        @{ SubModulePath = "./$submoduleName/$submoduleFileName" }
        @{ SubModulePath = "./$submoduleName\$submoduleFileName" }
    )

    It "Test if NestedModule path is <SubModulePath>" -TestCases $testCases {
        param($SubModulePath)

        New-ModuleManifest $moduleFilePath -NestedModules @($SubModulePath)
        Import-Module $moduleFilePath
        (Get-Module $moduleName).ExportedCommands.Keys.Contains('TestModuleFunction') | Should -BeTrue
    }

    It "Test if RootModule path is <SubModulePath>" -TestCases $testCases {
        param($SubModulePath)

        New-ModuleManifest $moduleFilePath -RootModule $SubModulePath
        Import-Module $moduleFilePath
        (Get-Module $moduleName).ExportedCommands.Keys.Contains('TestModuleFunction') | Should -BeTrue
    }
}
