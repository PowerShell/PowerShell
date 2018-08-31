# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$moduleRootFilePath = Split-Path -Path $PSScriptRoot -Parent

# Identify the repository root path of the resource module
$repoRootPath = (Resolve-Path -LiteralPath (Join-path $moduleRootFilePath "../..")).ProviderPath

Import-Module "$repoRootPath/tools/releaseTools.psm1"

Describe 'Common Tests - Package Reference' -Tag 'CI' {
    BeforeAll {
        $testCases = @()
        Get-NewOfficalPackage -IncludeAll | ForEach-Object {
            $testCases += @{
                CsProj = $_.CsProj
                PackageName = $_.PackageName
                CsProjVersion = $_.CsProjVersion
                NuGetRevision = $_.NuGetRevision
                NuGetVersion = $_.NuGetVersion
            }
        }
    }

    # This test should always be enabled
    It "<CsProj> reference to <PackageName> <CsProjVersion> should not need to be updated by a revision" -TestCases $testCases {
        param(
            [string]
            $CsProj,

            [string]
            $PackageName,

            [string]
            $CsProjVersion,

            [string]
            $NuGetRevision,

            [string]
            $NuGetVersion
        )

        $NuGetRevision | Should -BeExactly $CsProjVersion
    }

    # This test should be enabled when we are developing
    It "<CsProj> reference to <PackageName> <CsProjVersion> should not need to be updated by a new version" -TestCases $testCases {
        param(
            [string]
            $CsProj,

            [string]
            $PackageName,

            [string]
            $CsProjVersion,

            [string]
            $NuGetRevision,

            [string]
            $NuGetVersion
        )

        $NuGetVersion | Should -BeExactly $CsProjVersion
    }
}
