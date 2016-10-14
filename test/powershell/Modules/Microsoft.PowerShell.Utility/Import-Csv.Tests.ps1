Describe "Import-Csv DRT Unit Tests" -Tags "CI" {
    BeforeAll {
        $fileToGenerate = Join-Path $TestDrive -ChildPath "importCSVTest.csv"
        $psObject = [pscustomobject]@{ "First" = "1"; "Second" = "2" } 
    }
    
    It "Test import-csv with a delimiter parameter" {
        $delimiter = ';'        
        $psObject | Export-Csv -Path $fileToGenerate -Delimiter $delimiter
        $returnObject = Import-Csv -Path $fileToGenerate -Delimiter $delimiter
        $returnObject.First | Should Be 1
        $returnObject.Second | Should Be 2
    }

    It "Test import-csv with UseCulture parameter" {
        $psObject | Export-Csv -Path $fileToGenerate -UseCulture
        $returnObject = Import-Csv -Path $fileToGenerate -UseCulture
        $returnObject.First | Should Be 1
        $returnObject.Second | Should Be 2
    }
}

Describe "Import-Csv File Format Tests" -Tags "CI" {
    BeforeAll {
        # The file is w/o header
        $testImportCsv1 = Join-Path -Path (Join-Path $PSScriptRoot -ChildPath assets) -ChildPath TestImportCsv1.csv
        # The file is with header
        $testImportCsv2 = Join-Path -Path (Join-Path $PSScriptRoot -ChildPath assets) -ChildPath TestImportCsv2.csv
        # The file is W3C Extended Log File Format
        $testImportCsv3 = Join-Path -Path (Join-Path $PSScriptRoot -ChildPath assets) -ChildPath TestImportCsv3.csv
        
        $testCSVfiles = $testImportCsv1, $testImportCsv2, $testImportCsv3
        $orginalHeader = "Column1","Column2","Column 3"
        $customHeader = "test1","test2","test3"
    }
    # Test set is the same for all file formats
    foreach ($testCsv in $testCSVfiles) {
       $FileName = (dir $testCsv).Name
        Context "Next test file: $FileName" {
            BeforeAll {
                if ($FileName -eq "TestImportCsv1.csv") {
                    # The file does not have header
                    # (w/o Delimiter here we get throw (bug?)) 
                    $HeaderParams = @{Header = $orginalHeader; Delimiter = ","}
                    $CustomHeaderParams = @{Header = $customHeader; Delimiter = ","}
                } else {
                    # The files have header
                    $HeaderParams = @{Delimiter = ","}
                    $CustomHeaderParams = @{Header = $customHeader; Delimiter = ","}
                }

            }
            It "Should be able to call without error" {
                { Import-Csv $testCsv @HeaderParams } | Should Not Throw
            }

            It "Should be able to assign to a variable" {
                $actual = Import-Csv -Path $testCsv @HeaderParams

                $actual                     | Should Not BeNullOrEmpty
                $actual.GetType().BaseType  | Should Be array
                $actual.count               | Should Be 4
            }

            It "Should be able to import all fields" {
                $actual = Import-Csv -Path $testCsv @HeaderParams
                $actualfields = $actual[0].psobject.Properties.Name
                $actualfields | Should Be $orginalHeader
            }

            It "Should be able to import all fields with custom header" {
                $actual = Import-Csv -Path $testCsv @CustomHeaderParams
                $actualfields = $actual[0].psobject.Properties.Name
                $actualfields | Should Be $customHeader
            }

            It "Should be able to import rights values" {
                $actual = Import-Csv -Path $testCsv @HeaderParams
                $actual[0].'Column1'  | Should Be "data1"
                $actual[0].'Column2'  | Should Be "1"
                $actual[0].'Column 3' | Should Be "A"
            }
            # Is the test unnecessary? Remove?
            It "Should be able to use the alias without error" {
                { ipcsv $testCsv } | Should Not Throw
            }
            # Is the test unnecessary? Remove?
            It "Should have the same output between the alias and the full cmdlet name" {
                $actualAlias = ipcsv -Path $testCsv @HeaderParams
                $actualCmdlet = Import-Csv -Path $testCsv @HeaderParams
                $actualAlias[0].'Column1'  | Should Be $actualCmdlet[0].'Column1'
                $actualAlias[0].'Column2'  | Should Be $actualCmdlet[0].'Column2' 
                $actualAlias[0].'Column 3' | Should Be $actualCmdlet[0].'Column 3'
            }

        }
    }
}

Describe "Import-Csv #Type Tests" -Tags "CI" {
    BeforeAll {
        $testfile = Join-Path $TestDrive -ChildPath "testfile.csv"
        Remove-Item -Path $testfile -Force -ErrorAction SilentlyContinue
        $processlist = (Get-Process)[0..1]
        $processlist | Export-Csv -Path $testfile -Force
        # Import-Csv add "CSV:" before actual type
        # (Why #HandleCount ? See Issue #1812)
        $expectedProcessType = "CSV:System.Diagnostics.Process#HandleCount"
    }

    AfterAll {
        Remove-Item -Path $testfile -Force
    }

    It "Test import-csv import Object" {
        $importObjectList = Import-Csv -Path $testfile
        $processlist.Count | Should Be $importObjectList.Count

        $importType = $importObjectList[0].psobject.TypeNames[0]
        $importType | Should Be $expectedProcessType
    }
}
