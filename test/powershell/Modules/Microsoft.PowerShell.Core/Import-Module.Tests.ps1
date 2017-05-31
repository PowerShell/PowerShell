Describe "Import-Module" -Tags "CI" {
    $moduleName = "Microsoft.PowerShell.Security"

    BeforeEach {
        Remove-Module -Name $moduleName -Force
        (Get-Module -Name $moduleName).Name | Should BeNullOrEmpty
    }

    AfterEach {
        Import-Module -Name $moduleName -Force
        (Get-Module -Name $moduleName).Name | Should Be $moduleName
    }

    It "should be able to add a module with using Name switch" {
        { Import-Module -Name $moduleName } | Should Not Throw
        (Get-Module -Name $moduleName).Name | Should Be $moduleName
    }

    It "should be able to add a module with using ModuleInfo switch" {
        $a = Get-Module -ListAvailable $moduleName
        { Import-Module -ModuleInfo $a } | Should Not Throw
        (Get-Module -Name $moduleName).Name | Should Be $moduleName
    }
}

Describe "Import-Module with ScriptsToProcess" -Tags "CI" {
    
    BeforeAll {
        $moduleRootPath = Join-Path $TestDrive 'TestModules'
        New-Item $moduleRootPath -ItemType Directory -Force | Out-Null
        Push-Location $moduleRootPath

        "1 | Out-File out.txt -Append -NoNewline" | Out-File script1.ps1
        "2 | Out-File out.txt -Append -NoNewline" | Out-File script2.ps1
        New-ModuleManifest module1.psd1 -ScriptsToProcess script1.ps1
        New-ModuleManifest module2.psd1 -ScriptsToProcess script2.ps1 -NestedModules module1.psd1
    }

    AfterAll {
        Pop-Location
    }

    BeforeEach {
        New-Item out.txt -ItemType File -Force | Out-Null
    }

    AfterEach {
        $m = @('module1','module2','script1','script2')
        remove-module $m -Force -ErrorAction SilentlyContinue
        Remove-Item out.txt -Force -ErrorAction SilentlyContinue
    }
    
    It "Verify ScriptsToProcess are executed for top-level module" {
        Import-Module .\module1.psd1
        Get-Content out.txt | Should Be '1'
    }

    It "Verify ScriptsToProcess are executed for top-level and nested module" {
        Import-Module .\module2.psd1
        Get-Content out.txt | Should Be '21'
    }
    
    It "Verify ScriptsToProcess are executed for top-level module when -Version is specified" {
        Import-Module .\module1.psd1 -Version 1.0
        Get-Content out.txt | Should Be '1'
    }

    It "Verify ScriptsToProcess are executed for top-level and nested module when -Version is specified" {
        Import-Module .\module2.psd1 -Version 1.0
        Get-Content out.txt | Should Be '21'
    }
}
