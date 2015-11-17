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

    Context "String tests" {
        if ($env:TEMP -eq "/tmp")
        {
            $nl = "`n"
        }
        else {
            $nl = "`r`n"
        }

        $testString = "HAD I the heavens’ embroidered cloths,$nl Enwrought with golden and silver light,$nl The blue and the dim and the dark cloths$nl Of night and light and the half light,$nl I would spread the cloths under your feet:$nl But I, being poor, have only my dreams;$nl I have spread my dreams under your feet;$nl Tread softly because you tread on my dreams."

        It "Should be able to count the number of words in a string" {
            $expectedLength = $testString.Replace($nl,"").Split().length
            $actualLength   = $testString | Measure-Object -Word

            $actualLength.Words | Should Be $expectedLength
        }

        It "Should be able to count the number of characters in a string" {
            $expectedLength = $testString.length
            $actualLength   = $testString | Measure-Object -Character

            $actualLength.Characters | Should Be $expectedLength
        }

        It "Should be able to count the number of lines in a string" {
            $expectedLength = $testString.Split($nl).length
            $actualLength   = $testString | Measure-Object -Line

            $actualLength.Lines | Should Be $expectedLength
        }
    }
}
