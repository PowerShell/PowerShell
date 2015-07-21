
Describe "Test-Get-PSDrive" {
    It "Should not throw" {
        Get-PSDrive | Should Not BeNullOrEmpty
        (Get-PSDrive).Name | Should Not BeNullOrEmpty 
        (Get-PSDrive).Root.Length | Should Not BeLessThan 1

        gdr | Should Not BeNullOrEmpty
        (gdr).Name | Should Not BeNullOrEmpty 
        (gdr).Root.Length | Should Not BeLessThan 1
    }

    It "Should return drive info"{
        (Get-PSDrive D).Name | Should Be D
        (Get-PSDrive D).Root | Should Be D:\
        (Get-PSDrive D).Provider.Name | Should Be FileSystem

        (gdr D).Name | Should Be D
        (gdr D).Root | Should Be D:\
        (gdr D).Provider.Name | Should Be FileSystem
    }

    It "Should be able to access switches"{
        (Get-PSDrive -PSProvider FileSystem).Name.Length | Should BeGreaterThan 0

        (gdr -PSProvider FileSystem).Name.Length | Should BeGreaterThan 0
    }

    It "Should return true for a drive that does not exist"{
        !(Get-PSDrive fake -ErrorAction SilentlyContinue) | Should Be $True
    }
}
