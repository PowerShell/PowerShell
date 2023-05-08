# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Alias tests" -Tags "CI" {

    BeforeAll {
        $testPath = Join-Path testdrive:\ ("testAlias\.test")
        New-Item -ItemType Directory -Path $testPath -Force | Out-Null

        class TestData
        {
            [string] $testName
            [string] $testFile
            [string] $expectedError

            TestData($name, $file, $errorId)
            {
                $this.testName = $name
                $this.testFile = $file
                $this.expectedError = $errorId
            }
        }
    }

    Context "Export-Alias literal path" {
        BeforeAll {
            $csvFile = Join-Path $testPath "alias.csv"
            $ps1File = Join-Path $testPath "alias.ps1"

            $testCases = @()
            $testCases += [TestData]::new("CSV", $csvFile, [NullString]::Value)
            $testCases += [TestData]::new("PS1", $ps1File, [NullString]::Value)
            $testCases += [TestData]::new("Empty string", "", "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.ExportAliasCommand")
            $testCases += [TestData]::new("Null", [NullString]::Value, "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.ExportAliasCommand")
            $testCases += [TestData]::new("Non filesystem provider", 'env:\alias.ps1', "ReadWriteFileNotFileSystemProvider,Microsoft.PowerShell.Commands.ExportAliasCommand")
        }

        $testCases | ForEach-Object {

            It "for $($_.testName)" {

                $test = $_
                try
                {
                    Export-Alias -LiteralPath $test.testFile -ErrorAction SilentlyContinue
                }
                catch
                {
                    $exportAliasError = $_
                }

                if($null -eq $test.expectedError)
                {
                   Test-Path -LiteralPath $test.testFile | Should -BeTrue
                }
                else
                {
                    $exportAliasError.FullyqualifiedErrorId | Should -Be $test.expectedError
                }
            }

            AfterEach {
                Remove-Item -LiteralPath $test.testFile -Force -ErrorAction SilentlyContinue
            }
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

        BeforeAll {
            $csvFile = Join-Path $testPath "alias.csv"
            $ps1File = Join-Path $testPath "alias.ps1"

            $testCases = @()
            $testCases += [TestData]::new("Empty string", "", "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.ImportAliasCommand")
            $testCases += [TestData]::new("Null", [NullString]::Value, "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.ImportAliasCommand")
            $testCases += [TestData]::new("Non filesystem provider", 'env:\alias.ps1', "NotSupported,Microsoft.PowerShell.Commands.ImportAliasCommand")
        }

        $testCases | ForEach-Object {

            It "for $($_.testName)" {
                $test = $_

                { Import-Alias -LiteralPath $test.testFile -ErrorAction SilentlyContinue } | Should -Throw -ErrorId $test.expectedError
            }
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
