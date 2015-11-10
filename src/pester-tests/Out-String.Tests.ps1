Describe "Out-String" {
    if (Test-Path /tmp)
    {
        $nl = "`n"
    }
    else
    {
        $nl = "`r`n"
    }

    It "Should accumulate the strings and returns them as a single string" {
        $testArray = "a", " b"
        
        $testArray.GetType().BaseType | Should Be array
        
        $testArray | Out-String | Should Be "a$nl b$nl"

        $($testArray | Out-String).GetType() | Should Be string
    }

    It "Should be able to return an array of strings using the stream switch" {
        $testInput = "a", "b"

        $($testInput | Out-String).GetType() | Should Be string

        $($testInput | Out-String -Stream).GetType().BaseType.Name | Should Be array
    }

    It "Should send all objects through a pipeline when not using the stream switch" {
        $testInput = "a", "b"
        $streamoutputlength = $($testInput | Out-String -Stream).Length
        $nonstreamoutputlength = $($testInput | Out-String).Length

        $nonstreamoutputlength| Should BeGreaterThan $streamoutputlength
    }

    It "Should send a single object through a pipeline when the stream switch is used" {
        $testInput = "a", "b"
        $streamoutputlength = $($testInput | Out-String -Stream).Length
        $nonstreamoutputlength = $($testInput | Out-String).Length

        $streamoutputlength | Should BeLessThan $nonstreamoutputlength
    }
}