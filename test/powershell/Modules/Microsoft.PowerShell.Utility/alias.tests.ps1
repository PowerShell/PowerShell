# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Alias tests" -Tags "CI" {

    BeforeAll {
        $testPath = Join-Path testdrive:\ ("testAlias\.test")
        New-Item -ItemType Directory -Path $testPath -Force | Out-Null
    }

    Context "Export-Alias literal path" {
        BeforeAll {
            $csvFile = Join-Path $testPath "alias.csv"
            $ps1File = Join-Path $testPath "alias.ps1"
        }

        BeforeDiscovery {
            $testCases = @(
                @{ testName = "CSV"; testFile = "alias.csv"; expectedError = $null; useTestPath = $true }
                @{ testName = "PS1"; testFile = "alias.ps1"; expectedError = $null; useTestPath = $true }
                @{ testName = "Empty string"; testFile = ""; expectedError = "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.ExportAliasCommand"; useTestPath = $false }
                @{ testName = "Null"; testFile = $null; expectedError = "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.ExportAliasCommand"; useTestPath = $false }
                @{ testName = "Non filesystem provider"; testFile = 'env:\alias.ps1'; expectedError = "ReadWriteFileNotFileSystemProvider,Microsoft.PowerShell.Commands.ExportAliasCommand"; useTestPath = $false }
            )
        }

        It "for <testName>" -TestCases $testCases {
            param($testName, $testFile, $expectedError, $useTestPath)
            $actualFile = if ($useTestPath) { Join-Path $testPath $testFile } else { $testFile }
            try {
                Export-Alias -LiteralPath $actualFile -ErrorAction SilentlyContinue
            }
            catch {
                $exportAliasError = $_
            }

            if ($null -eq $expectedError) {
                Test-Path -LiteralPath $actualFile | Should -BeTrue
            }
            else {
                $exportAliasError.FullyqualifiedErrorId | Should -Be $expectedError
            }
        }

        AfterEach {
            Remove-Item -LiteralPath (Join-Path $testPath "alias.csv") -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath (Join-Path $testPath "alias.ps1") -Force -ErrorAction SilentlyContinue
        }

        It "when file exists with NoClobber" {
            Export-Alias -LiteralPath $csvFile
            { Export-Alias -LiteralPath $csvFile -NoClobber } | Should -Throw -ErrorId "NoClobber,Microsoft.PowerShell.Commands.ExportAliasCommand"
        }
    }

    Context "Export-All inside a literal path" {
        BeforeEach {
            Push-Location -LiteralPath $testPath
        }

        It "with a CSV file" {
            Export-Alias "alias.csv"
            Test-Path -LiteralPath (Join-Path $testPath "alias.csv") | Should -BeTrue
        }

        It "with NoClobber" {
            $path = Export-Alias alias.csv

            { Export-Alias alias.csv -NoClobber } | Should -Throw -ErrorId "NoClobber,Microsoft.PowerShell.Commands.ExportAliasCommand"
        }

        AfterEach {
            Pop-Location
        }
    }

    Context "Import-Alias literal path" {

        BeforeDiscovery {
            $testCases = @(
                @{ testName = "Empty string"; testFile = ""; expectedError = "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.ImportAliasCommand" }
                @{ testName = "Null"; testFile = $null; expectedError = "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.ImportAliasCommand" }
                @{ testName = "Non filesystem provider"; testFile = 'env:\alias.ps1'; expectedError = "NotSupported,Microsoft.PowerShell.Commands.ImportAliasCommand" }
            )
        }

        It "for <testName>" -TestCases $testCases {
            param($testName, $testFile, $expectedError)
            { Import-Alias -LiteralPath $testFile -ErrorAction SilentlyContinue } | Should -Throw -ErrorId $expectedError
        }

        It "can be done from a CSV file" {

            # alias file definition content
            $aliasDefinition = @'
            "myuh","update-help","","ReadOnly, AllScope"
'@

            $aliasFile = Join-Path $testPath "alias.csv"
            $aliasDefinition | Out-File -LiteralPath $aliasFile

            Import-Alias -LiteralPath $aliasFile

            # Verify that the alias was imported
            $definedAlias = Get-Alias myuh

            $definedAlias | Should -Not -BeNullOrEmpty
            $definedAlias.Name | Should -BeExactly "myuh"
            $definedAlias.Definition | Should -Be "update-help"
        }
    }
}
