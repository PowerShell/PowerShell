# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Write-Output DRT Unit Tests" -Tags "CI" {
    It "Simple Write Object Test" {
        $objectWritten = 1, 2.2, @("John", "Smith", 10), "abc"
        $results = Write-Output $objectWritten
        $results.Length | Should -Be $objectWritten.Length

        $results[0] | Should -Be $objectWritten[0]
        $results[1] | Should -Be $objectWritten[1]

        $results[2] | Should -Be $objectWritten[2]
        $results[2] -is [array] | Should -BeTrue

        $results[3] | Should -Be $objectWritten[3]
        $results[3] -is [System.String] | Should -BeTrue
    }

    It "Works with NoEnumerate switch" {
        $objectWritten = 1, 2.2, @("John", "Smith", 10), "abc"
        [string]$s = Write-Output $objectWritten -NoEnumerate 6>&1
        $s | Should -Be  '1 2.2 System.Object[] abc'
    }

    It "Preserves the input collection type and contents with -NoEnumerate" {
        $List = [System.Collections.Generic.List[string]]::new( [string[]]@("test", "one", "two") )
        $ObjectWritten = Write-Output $List -NoEnumerate

        $ObjectWritten.GetType() | Should -Be ([System.Collections.Generic.List[string]])
        $ObjectWritten[0] | Should -BeExactly "test"
        $ObjectWritten[1] | Should -BeExactly "one"
        $ObjectWritten[2] | Should -BeExactly "two"
    }
}

Describe "Write-Output" -Tags "CI" {
    $testString = $testString
    Context "Input Tests" {
        It "Should allow piped input" {
            { $testString | Write-Output } | Should -Not -Throw
        }

        It "Should write output to the output stream when using piped input" {
            $testString | Write-Output | Should -Be $testString
        }

        It "Should use inputobject switch" {
            { Write-Output -InputObject $testString } | Should -Not -Throw
        }

        It "Should write output to the output stream when using inputobject switch" {
            Write-Output -InputObject $testString | Should -Be $testString
        }

        It "Should be able to write to a variable" {
            Write-Output -InputObject $testString -OutVariable var
            $var | Should -Be $testString
        }
    }

    Context "Pipeline Command Tests" {
        It "Should send object to the next command in the pipeline" {
            Write-Output -InputObject (1 + 1) | Should -Be 2
        }

        It "Should have the same result between inputobject switch and piped input" {
            Write-Output -InputObject (1 + 1) | Should -Be 2

            1 + 1 | Write-Output | Should -Be 2
        }
    }

    Context "Enumerate Objects" {
        $enumerationObject = @(1, 2, 3)
        It "Should see individual objects when not using the NoEnumerate switch" {
            $singleCollection = $(Write-Output $enumerationObject| Measure-Object).Count

            $singleCollection | Should -Be $enumerationObject.length
        }

        It "Should be able to treat a collection as a single object using the NoEnumerate switch" {
            $singleCollection = $(Write-Output $enumerationObject -NoEnumerate | Measure-Object).Count

            $singleCollection | Should -Be 1
        }
    }
}
