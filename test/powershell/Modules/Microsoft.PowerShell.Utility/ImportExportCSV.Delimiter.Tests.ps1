Describe "Using delimiters with Export-CSV and Import-CSV behave correctly" -tags P1 {
    BeforeAll {
        # note, we will not use "," as that's the default for CSV
        $delimiters = "/", " ", "@", "#", "$", "\", "&", "(", ")", 
              "{", "}", "|", "<", ">", ";", "'", 
              '"', "~", "!", "%", "^", "*", "_", "+", ":",
              "?", "-", "=", "[", "]", "."
        $defaultDelimiter = [System.Globalization.CultureInfo]::CurrentCulture.TextInfo.ListSeparator
        $d = get-date
        $testCases = @(
            foreach($del in $delimiters)
            {
                @{ Delimiter = $del; Data = $d; ExpectedResult = $d.Ticks }
            }
            )
    }
    AfterEach {
        [System.Globalization.CultureInfo]::CurrentCulture.TextInfo.ListSeparator = $defaultDelimiter 
        remove-item -force -ea silentlycontinue TESTDRIVE:/file.csv
    }

    It "Disallow use of null delimiter" {
        $d | export-csv TESTDRIVE:/file.csv
        { import-csv -path TESTDRIVE:/file.csv -delimiter $null } | Should Throw "Delimiter"
    }

    It "Disallow use of delimiter with useCulture parameter" {
        $d | export-csv TESTDRIVE:/file.csv
        { import-csv -path TESTDRIVE:/file.csv -useCulture "," } | Should Throw "','"
    }

    It "Imports the same properties as exported" {
        $a = [pscustomobject]@{ a = 1; b = 2; c = 3 }
        $a | export-Csv TESTDRIVE:/file.csv
        $b = import-csv TESTDRIVE:/file.csv
        @($b.psobject.properties).count | should be 3
        $b.a | Should be $a.a
        $b.b | Should be $a.b
        $b.c | Should be $a.c
    }

    # parameter generated tests
    It 'Delimiter <Delimiter> with CSV import will fail correctly when culture does not match' -testCases $testCases {
        param ($delimiter, $Data, $ExpectedResult)
        [System.Globalization.CultureInfo]::CurrentCulture.TextInfo.ListSeparator = $delimiter
        $Data | export-CSV TESTDRIVE:\File.csv -useCulture
        $i = Import-CSV TESTDRIVE:\File.csv
        $i.Ticks | Should Not Be $ExpectedResult
    }

    It 'Delimiter <Delimiter> with CSV import will succeed when culture matches export' -testCases $testCases {
        param ($delimiter, $Data, $ExpectedResult)
        [System.Globalization.CultureInfo]::CurrentCulture.TextInfo.ListSeparator = $delimiter
        $Data | export-CSV TESTDRIVE:\File.csv -useCulture
        $i = Import-CSV TESTDRIVE:\File.csv -useCulture
        $i.Ticks | Should Be $ExpectedResult
    }

    It 'Delimiter <Delimiter> with CSV import will succeed when delimiter is used explicitly' -testCases $testCases {
        param ($delimiter, $Data, $ExpectedResult)
        $Data | export-CSV TESTDRIVE:\File.csv -delimiter $delimiter
        $i = Import-CSV TESTDRIVE:\File.csv -delimiter $delimiter
        $i.Ticks | Should Be $ExpectedResult
    }
}
