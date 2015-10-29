Describe "ConvertFrom-Csv" {
    $testObject = "a", "1"
    $testDelimiter = "a; b"
    $testColumns = @"
    a,b,c
    1,2,3
"@
    It "Should be able to be called" {
        { ConvertFrom-Csv -InputObject $testObject } | Should Not Throw
    }

    It "Should be able to pipe" {
        { $testObject | ConvertFrom-Csv } | Should Not Throw
    }

    It "Should be able to set a delimiter" {
        { $testDelimiter | ConvertFrom-Csv -Delimiter ";" } | Should Not Throw
    }

    It "Should be able to have multiple columns" {
        $actualData   = $testColumns | ConvertFrom-Csv
        
        $actualLength = $($( $actualData | gm) | Where-Object {$_.MemberType -eq "NoteProperty" }).Length

        $actualLength | Should Be 3
    }
}