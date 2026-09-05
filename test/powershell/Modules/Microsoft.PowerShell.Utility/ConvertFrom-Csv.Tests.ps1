# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "ConvertFrom-Csv" -Tags "CI" {

    BeforeAll {
        $testObject = "a", "1"
        $testcsv = Join-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath assets) -ChildPath TestCsv2.csv
        $testName = "Zaphod BeebleBrox"
        $testColumns = @"
a,b,c
1,2,3
"@
        $testTypeData = @"
#TYPE My.Custom.Object
a,b,c
1,2,3
"@
    }

    It "Should be able to be called" {
        { ConvertFrom-Csv -InputObject $testObject } | Should -Not -Throw
    }

    It "Should be able to pipe" {
        { $testObject | ConvertFrom-Csv } | Should -Not -Throw
    }

    It "Should have expected results when using piped inputs" {
        $csvContent   = Get-Content $testcsv
        $actualresult = $csvContent | ConvertFrom-Csv

        ,$actualresult | Should -BeOfType System.Array
        $actualresult[0] | Should -BeOfType PSCustomObject

        #Should have a name property in the result
        $actualresult[0].Name | Should -Be $testName
    }

    It "Should be able to set a delimiter" {
        { $testcsv | ConvertFrom-Csv -Delimiter ";" } | Should -Not -Throw
    }

    It "Should actually delimit the output" {
        $csvContent   = Get-Content $testcsv
        $actualresult = $csvContent | ConvertFrom-Csv -Delimiter ";"

        ,$actualresult | Should -BeOfType System.Array
        $actualresult[0] | Should -BeOfType PSCustomObject

        # ConvertFrom-Csv takes the first line of the input as a header by default
        $actualresult.Length | Should -Be $($csvContent.Length - 1)
    }

    It "Should be able to have multiple columns" {
        $actualData   = $testColumns | ConvertFrom-Csv

        $actualLength = $($( $actualData | Get-Member) | Where-Object {$_.MemberType -eq "NoteProperty" }).Length

        $actualLength | Should -Be 3
    }

    It "Uses the default delimiter when -UseCulture is explicitly false" {
        $originalCulture = [System.Threading.Thread]::CurrentThread.CurrentCulture
        $originalUICulture = [System.Threading.Thread]::CurrentThread.CurrentUICulture

        try {
            [System.Threading.Thread]::CurrentThread.CurrentCulture = [CultureInfo]'de-DE'
            [System.Threading.Thread]::CurrentThread.CurrentUICulture = [CultureInfo]'de-DE'
            [CultureInfo]::CurrentCulture.TextInfo.ListSeparator | Should -Not -BeExactly ','

            $actualData = $testColumns | ConvertFrom-Csv -UseCulture:$false

            $actualData.a | Should -Be 1
            $actualData.b | Should -Be 2
            $actualData.c | Should -Be 3
        }
        finally {
            [System.Threading.Thread]::CurrentThread.CurrentCulture = $originalCulture
            [System.Threading.Thread]::CurrentThread.CurrentUICulture = $originalUICulture
        }
    }

    It "Should Contain the Imported Type data" {
        $actualData = $testTypeData | ConvertFrom-Csv
        $actualData.PSObject.TypeNames.Count | Should -Be 2
        $actualData.PSObject.TypeNames[0] | Should -BeExactly "My.Custom.Object"
        $actualData.PSObject.TypeNames[1] | Should -BeExactly "CSV:My.Custom.Object"
    }
}

Describe "ConvertFrom-Csv DRT Unit Tests" -Tags "CI" {
    It "Test ConvertFrom-Csv with pipelined InputObject and Header" {
        $inputObject = [pscustomobject]@{ First = 1; Second = 2 }
        $res = $inputObject | ConvertTo-Csv
        $result = $res | ConvertFrom-Csv -Header "Header1","Header2"

        $result[0].Header1 | Should -BeExactly "First"
        $result[0].Header2 | Should -BeExactly "Second"
        $result[1].Header1 | Should -BeExactly "1"
        $result[1].Header2 | Should -BeExactly "2"
    }
}
