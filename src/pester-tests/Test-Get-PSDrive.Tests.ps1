Describe "Test-Get-PSDrive" {
    It "Should not throw" {
        Get-PSDrive | Should Not BeNullOrEmpty
    }

    It "Should have a name and a length property" {
        (Get-PSDrive).Name        | Should Not BeNullOrEmpty 
        (Get-PSDrive).Root.Length | Should Not BeLessThan 1
    }

    It "Should be able to be called with the gdr alias" {
        { gdr } | Should Not Throw

        gdr | Should Not BeNullOrEmpty
    }

    It "Should return drive info"{
        (Get-PSDrive Env).Name        | Should Be Env
        (Get-PSDrive /).Root          | Should Be /
        (Get-PSDrive /).Provider.Name | Should Be FileSystem
    }

    It "Should be able to access a drive using the PSProvider switch" {
        (Get-PSDrive -PSProvider FileSystem).Name.Length | Should BeGreaterThan 0
    }

    It "Should return true that a drive that does not exist"{
        !(Get-PSDrive fake -ErrorAction SilentlyContinue) | Should Be $True
        Get-PSDrive fake -ErrorAction SilentlyContinue    | Should BeNullOrEmpty
    }
}
