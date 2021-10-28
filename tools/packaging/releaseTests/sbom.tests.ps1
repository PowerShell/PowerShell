Describe "Verify SBOMs" {
    BeforeAll {
        Write-Verbose "In Describe BeforeAll" -Verbose
        Import-Module $PSScriptRoot/../../../build.psm1
        Import-Module $PSScriptRoot/../packaging.psd1 -Force
        $matchCases = @()
        $testCases = @()
        $missingFromPackageCases = @()
        $missingFromManifestCases = @()
        Write-Verbose "${env:PACKAGE_FOLDER}" -Verbose
        Get-ChildItem $env:PACKAGE_FOLDER -Filter *.zip | ForEach-Object {
            Write-Verbose "Found $($_.Name)..." -Verbose
            $testCases += @{
                FilePath = $_.FullName
                Name = $_.Name
            }
        }

        foreach($case in $testCases) {
            $name = $case.Name
            Write-Verbose "Testing $name..." -Verbose
            $extractedPath = Join-Path Testdrive:\ -ChildPath ([System.io.path]::GetRandomFileName())
            $null = New-Item -Path $extractedPath -ItemType Directory -Force
            Expand-Archive -Path $case.FilePath -DestinationPath $extractedPath
            $manifestPath = Join-Path $extractedPath -ChildPath '/_manifest/spdx_2.2/manifest.spdx.json'
            It "$name has a BOM" {
                $manifestPath | Should -Exist
            }
            Test-PackageManifest -PackagePath $extractedPath | ForEach-Object {
                $status = $_.Status
                $expectedHash = $_.ExpectedHash
                $actual = $_.ActualHash
                $file = $_.File

                switch($status) {
                    # cover match and mismatch
                    default {
                        $matchCases += @{
                            Name = $name
                            File = $file
                            ActualHash = $actual
                            ExpectedHash = $ExpectedHash
                            Status = $status
                        }
                    }
                    "MissingFromPackage" {
                        $missingFromPackageCases = @{
                            Name = $name
                            File = $file
                            ActualHash = $actual
                            ExpectedHash = $ExpectedHash
                            Status = $status
                        }
                    }
                    "MissingFromManifest" {
                        $missingFromManifestCases = @{
                            Name = $name
                            File = $file
                            ActualHash = $actual
                            ExpectedHash = $ExpectedHash
                            Status = $status
                        }
                    }
                }
            }
        }
    }

    Context "Zip files" {
        BeforeAll {
            Write-Verbose "In Context BeforeAll" -Verbose
        }

        It "<name> should have <file> with matching hash" -TestCases $matchCases {
            param(
                $Name,
                $File,
                $ActualHash,
                $ExpectedHash,
                $Status
            )

            $status | Should -Be "Match" -Because  "$actualHash should be $expectedHash"
        }

        It "<name> should have <file> with matching hash" -TestCases $missingFromPackageCases -Skip:($missingFromPackageCases.Count -eq 0)  {
            param(
                $Name,
                $File,
                $ActualHash,
                $ExpectedHash,
                $Status
            )

            $status | Should -Be "Match" -Because "All files in manifest should exist in package"
        }

        It "Manifest for <name> should have <file>" -TestCases $missingFromManifestCases -Skip:($missingFromManifestCases.Count -eq 0) {
            param(
                $Name,
                $File,
                $ActualHash,
                $ExpectedHash,
                $Status
            )

            $status | Should -Be "Match" -Because "All files in package should exist in manifest"
        }
    }
}
