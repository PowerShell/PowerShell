Describe "Measure-Object" {
    $testObject = 1,3,4

    It "Should be able to be called without error" {
        { Measure-Object | Out-Null } | Should Not Throw
    }

    It "Should be able to call on piped input" {
        { $testObject | Measure-Object } | Should Not Throw
    }

    It "Should be able to count the number of objects input to it" {
        $($testObject | Measure-Object).Count | Should Be $testObject.Length
    }

    It "Should be able to count using the Property switch" {
        $expected = $(Get-ChildItem).Length
        $actual   = $(Get-ChildItem | Measure-Object -Property Length).Count
        
        $actual | Should Be $expected
    }

    It "Should be able to get additional stats" {
        $actual = Get-Process | Measure-Object -Property workingset64 -Minimum -Maximum -Average

        $actual.Average    | Should BeGreaterThan 0
        $actual.Characters | Should BeNullOrEmpty
        $actual.Lines      | Should BeNullOrEmpty
        $actual.Maximum    | Should Not BeNullOrEmpty
        $actual.Minimum    | Should Not BeNullOrEmpty
        $actual.Sum        | Should BeNullOrEmpty
    }
}
