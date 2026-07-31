# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Localized resource files validation" -Tags "CI" {
    BeforeAll {
        $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot ../../../..)).Path

        $locProjectPath = Join-Path $repoRoot 'Localize' 'LocProject.json'
        $content = Get-Content -Path $locProjectPath -Raw
        $locProject = ConvertFrom-Json -InputObject $content
    }

    It 'Validate LocItems in LocProject.json' {
        $locProject.Projects.Count | Should -Be 1
        $project = $locProject.Projects[0]
        $project.LanguageSet | Should -BeExactly 'VS_Main_Languages'

        $project.LocItems |
            ForEach-Object {
                $sourceFile = $_.SourceFile
                $index = $sourceFile.LastIndexOf('\')
                $parentDir = $sourceFile.Substring(0, $index)
                $realSourceFile = Join-Path $repoRoot $sourceFile

                Test-Path -Path $realSourceFile | Should -BeTrue
                $_.OutputPath | Should -BeExactly "$parentDir\"
                $_.CopyOption | Should -BeExactly 'LangIDOnPathAndName'
            }
    }

    It 'Validate total resource count' {
        $srcDir = Join-Path $repoRoot 'src'
        $project = $locProject.Projects[0]

        try {
            Push-Location -Path $srcDir
            $resDirs = Get-ChildItem 'resources' -Recurse -Directory | ForEach-Object FullName

            $totalResourceCount = 0
            foreach ($resDir in $resDirs) {
                $count = Get-ChildItem -Path "$resDir/*.resx" | Measure-Object | ForEach-Object Count
                $totalResourceCount += $count
            }

            $project.LocItems.Count | Should -Be $totalResourceCount
        }
        finally {
            Pop-Location
        }
    }
}
