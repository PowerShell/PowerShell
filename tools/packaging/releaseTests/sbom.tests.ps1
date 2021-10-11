Describe "Verify SBOMs" {
    BeforeAll {
        Import-Module ../packaging.psd1
    }

    Context "Zip files" {
        BeforeAll {
            $testCases = @()
            Get-ChildItem $env:PACKAGE_FOLDER -Filter *.zip -Recurse | ForEach-Object {
                $extractedPath = Join-Path Testdrive:\ -ChildPath ([System.io.path]::GetRandomFileName())
                $null = New-Item -Path $extractedPath -ItemType Directory -Force
                Expand-Archive -Path $_.FullName -DestinationPath $extractedPath
                $testCases += @{
                    FilePath = $_.FullName
                    Name = $_.Name
                    ExtractedPath = $extractedPath
                }
            }
        }
        foreach($case in $testCases) {
            $extractedPath = Join-Path Testdrive:\ -ChildPath ([System.io.path]::GetRandomFileName())
            $null = New-Item -Path $extractedPath -ItemType Directory -Force
            Expand-Archive -Path $case.FilePath -DestinationPath $extractedPath
            $name = $case.Name
            $manifestPath = Join-Path $extractedPath -ChildPath '/_manifest/spdx_2.2/manifest.spdx.json'
            It "$name has a BOM" {
                $manifestPath | Should -Exist
            }
            Test-PackageManifest -PackagePath $extractedPath | ForEach-Object {
                $status = $_.Status
                $expected = $_.ExpectedHash
                $actual = $_.ActualHash
                $file = $_.File

                switch($status) {
                    # cover match and mismatch
                    default {
                        It "$name should have $file with matching hash" {
                            $status | Should -Be "Match" -Because  "$actual should be $expected"
                        }
                    }
                    "MissingFromPackage" {
                        It "$name should have $file with matching hash" {
                            $status | Should -Be "Match" -Because  "All files in manifest should exist in package"
                        }
                    }
                    "MissingFromManifest" {
                        It "Manifest for $name should have $file" {
                            $status | Should -Be "Match" -Because "All files in package should exist in manifest"
                        }
                    }
                }
            }
        }
    }
}
