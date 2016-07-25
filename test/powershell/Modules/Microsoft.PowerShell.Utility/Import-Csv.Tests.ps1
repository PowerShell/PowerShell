Describe "Import-Csv" -Tags "CI" {
    $testCsv = Join-Path -Path (Join-Path $PSScriptRoot -ChildPath assets) -ChildPath TestCsv.csv

    It "Should be able to call without error" {
	    { Import-Csv $testCsv } | Should Not Throw
        }

    It "Should be able to assign to a variable" {
	    $actual = Import-Csv $testCsv

	    $actual                    | Should Not BeNullOrEmpty
	    $actual.GetType().BaseType | Should Be array
    }

    It "Should have the data from the csv file" {
	    $actualContent = $(Get-Content $testCsv)[0]
	    $testContent   = $($(Import-Csv $testCsv) | Get-Member) | ? { $_.MemberType -eq "NoteProperty" } | % { $_.Name } | Select-Object -First 1

	    $actualContent.IndexOf($testContent) | Should BeGreaterThan -1
    }

    It "Should be able to prepend a custom header" {
	    $header = "test1","test2","test3"

	    $originalContent = $($(Import-Csv $testCsv) | Get-Member) | ? { $_.MemberType -eq "NoteProperty" } | % { $_.Name } | Select-Object -First 1

	    $testContent = $($(Import-Csv $testCsv -Header $header) | Get-Member) | ? { $_.MemberType -eq "NoteProperty" } | % { $_.Name } | Select-Object -First 3

	    # the original csv file doesn't contain the headers
        $originalContent.IndexOf($header[0]) | Should Be -1

        # but it does with the -Header switch!
        $testContent[0] | Should Be $header[0]
        $testContent[1] | Should Be $header[1]
        $testContent[2] | Should Be $header[2]
    }

    It "Should be able to use the alias without error" {
        { Import-Csv $testCsv } | Should Not Throw
    }

    It "Should have the same output between the alias and the full cmdlet name" {
        $alias  = $($(ipcsv $testCsv) | Get-Member) | ? { $_.MemberType -eq "NoteProperty" } | % { $_.Name } | Select-Object -First 1
        $cmdlet = $($(Import-Csv $testCsv) | Get-Member) | ? { $_.MemberType -eq "NoteProperty" } | % { $_.Name } | Select-Object -First 1

        $alias[0] | Should Be $cmdlet[0]
        $alias[1] | Should Be $cmdlet[1]
        $alias[2] | Should Be $cmdlet[2]

    }
}

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
