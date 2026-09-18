# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Localized resource files validation" -Tags "CI" {

    BeforeDiscovery {
        # -TestCases is read during Discovery, so the data has to be built here.
        # The same lists are repeated in BeforeAll below, because variables set
        # during discovery are not available during the run phase.
        $langSet = 'cs', 'de', 'es', 'fr', 'it', 'ja', 'ko', 'pl', 'pt-BR', 'ru', 'tr', 'zh-Hans', 'zh-Hant'
        $asmDirs = @(
            'Microsoft.Management.Infrastructure.CimCmdlets'
            'Microsoft.Management.UI.Internal'
            'Microsoft.PowerShell.Commands.Diagnostics'
            'Microsoft.PowerShell.Commands.Management'
            'Microsoft.PowerShell.Commands.Utility'
            'Microsoft.PowerShell.ConsoleHost'
            'Microsoft.PowerShell.CoreCLR.Eventing'
            'Microsoft.PowerShell.Security'
            'Microsoft.WSMan.Management'
            'System.Management.Automation'
        )

        $testCases1 = @(
            $asmDirs | ForEach-Object { @{ AssemblyDir = $_ } }
        )
        $testCases2 = @(
            $langSet | ForEach-Object { @{ Language = $_ } }
        )
    }

    BeforeAll {
        $repoSrcDir = (Resolve-Path (Join-Path $PSScriptRoot ../../../../src)).Path
        $skipSatelliteAssemblyTest = !$IsWindows
        if (!$skipSatelliteAssemblyTest -and $env:PIPELINE_REPOSITORY_NAME -eq 'Release-Automation') {
            ## Only MSIX packages include the localized satellite assemblies.
            $isMSIXApp = Test-Path -Path (Join-Path $PSHOME 'AppxManifest.xml')
            $skipSatelliteAssemblyTest = -not $isMSIXApp
        }

        # Folders that exist in 'resources' folder but are not a localized resource directory.
        $excludeDirs = @('Graphics')

        # VS_Main_Languages set, the same as what .NET uses for localization.
        $langSet = 'cs', 'de', 'es', 'fr', 'it', 'ja', 'ko', 'pl', 'pt-BR', 'ru', 'tr', 'zh-Hans', 'zh-Hant'
        $asmDirs = @(
            'Microsoft.Management.Infrastructure.CimCmdlets'
            'Microsoft.Management.UI.Internal'
            'Microsoft.PowerShell.Commands.Diagnostics'
            'Microsoft.PowerShell.Commands.Management'
            'Microsoft.PowerShell.Commands.Utility'
            'Microsoft.PowerShell.ConsoleHost'
            'Microsoft.PowerShell.CoreCLR.Eventing'
            'Microsoft.PowerShell.Security'
            'Microsoft.WSMan.Management'
            'System.Management.Automation'
        )

        $asmNames = $asmDirs | ForEach-Object {
            if ($_ -eq 'Microsoft.Management.UI.Internal') {
                'Microsoft.PowerShell.GraphicalHost'
            } else {
                $_
            }
        }
    }

    It "Assembly folders with resources should match with records" {
        try {
            Push-Location $repoSrcDir
            $assemblies = Get-ChildItem 'resources' -Recurse -Directory | ForEach-Object { $_.Parent.Name }

            ## Verifies that the assembly folders containing resource files match the expected records in $asmDirs.
            $result = Compare-Object -ReferenceObject $asmDirs -DifferenceObject $assemblies
            $result | Should -Be $null -Because "The assembly folders with resources do not match the expected records:`n$($result | Out-String)."

            ## Verifies that the default English resource files exist and match the expected count.
            $defaultCultureResources = $assemblies | ForEach-Object { Get-ChildItem "$_/resources/*.resx" }
            $defaultCultureResources | Should -HaveCount 150 -Because "Default English resource files count: $($defaultCultureResources.Count); Expected count: 150."
        }
        finally {
            Pop-Location
        }
    }

    It "Localized resource files for '<AssemblyDir>' should match the records" -TestCases $testCases1 {
        param($AssemblyDir)

        ## Get the full path to the resources directory for the current assembly folder.
        $resDir = Join-Path $repoSrcDir $AssemblyDir 'resources'
        ## Get all the localized resource directory names under the resources directory.
        $locDirNames = @(Get-ChildItem $resDir -Directory | ForEach-Object Name | Where-Object { $_ -notin $excludeDirs })

        ## Verifies that the localized resource directories match the expected language set.
        $results = Compare-Object -ReferenceObject $langSet -DifferenceObject $locDirNames
        $results | Should -Be $null -Because "The localized resource directories for '$AssemblyDir' do not match the expected language set:`n$($results | Out-String)."

        ## Get the default English resource file names without extensions.
        $default = @(Get-ChildItem "$resDir/*.resx" | ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Name) })

        foreach ($lang in $langSet) {
            ## Get the full path to the localized resource directory for the current language.
            $langDir = Join-Path $resDir $lang
            ## Get all the localized resource file names without extensions for the current language.
            $langResources = @(Get-ChildItem "$langDir/*.resx" | ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Name) })

            ## The localized resource file should have the same base name as the default English resource file, with the language suffix added.
            $expectedNames = @($default | ForEach-Object { "$_.$lang" })
            ## Verifies that the localized resource file names match the expected names.
            $langResults = Compare-Object -ReferenceObject $expectedNames -DifferenceObject $langResources
            $langResults | Should -Be $null -Because "The names of localized resource files for '$lang' in '$AssemblyDir' do not match the default English files:`n$($langResults | Out-String)."
        }
    }

    It "Satellite assemblies should be produced for '<Language>'" -TestCases $testCases2 -Skip:$skipSatelliteAssemblyTest {
        param($Language)

        $satDir = Join-Path $PSHOME $Language
        Test-Path $satDir | Should -BeTrue -Because "Satellite directory for language '$Language' should exist."

        foreach ($asmName in $asmNames) {
            $satAssemblyPath = Join-Path $satDir "$asmName.resources.dll"
            Test-Path $satAssemblyPath | Should -BeTrue -Because "Satellite assembly '$asmName.resources.dll' for language '$Language' should exist."
        }
    }
}
