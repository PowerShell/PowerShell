Describe "Remove-Module" -Tags "CI" {
    $moduleName = "Microsoft.PowerShell.Security"

    BeforeEach {
        Import-Module -Name $moduleName -Force
        (Get-Module -Name $moduleName).Name | Should be $moduleName
    }

    It "should be able to remove a module with using Name switch" {
        { Remove-Module -Name $moduleName } | Should Not Throw
        (Get-Module -Name $moduleName).Name | Should BeNullOrEmpty
    }

    It "should be able to remove a module with using ModuleInfo switch" {
        $a = Get-Module -Name $moduleName
        { Remove-Module -ModuleInfo $a } | Should Not Throw
        (Get-Module -Name $moduleName).Name | Should BeNullOrEmpty
    }

	AfterEach {
        Import-Module -Name $moduleName -Force
    }
}
