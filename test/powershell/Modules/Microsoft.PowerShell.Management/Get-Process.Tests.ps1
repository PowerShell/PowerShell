Describe "Get-Process" {
    It "Should support -IncludeUserName" {
        (Get-Process powershell -IncludeUserName).UserName | Should Be $env:USER
    }

    # These tests are no good, please replace!
    It "Should return a type of Object[] for Get-Process cmdlet" -Pending:$IsOSX {
        (Get-Process).GetType().BaseType | Should Be 'array'
        (Get-Process).GetType().Name | Should Be Object[]
    }

    It "Should have not empty Name flags set for Get-Process object" -Pending:$IsOSX {
        Get-Process | foreach-object { $_.Name | Should Not BeNullOrEmpty }
    }
}
