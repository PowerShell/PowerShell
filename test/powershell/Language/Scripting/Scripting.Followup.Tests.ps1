Describe "Scripting.Followup.Tests" -Tags "CI" {
    It "'[void](New-Item) | <Cmdlet>' should work and behave like passing AutomationNull to the pipe" {
        try {
            $testFile = "$TestDrive\test.txt"
            [void](New-Item $testFile -ItemType File) | ForEach-Object { "YES" } | Should Be $null
            ## file should be created
            $testFile | Should Exist
        } finally {
            Remove-Item $testFile -Force -ErrorAction SilentlyContinue
        }
    }
}
