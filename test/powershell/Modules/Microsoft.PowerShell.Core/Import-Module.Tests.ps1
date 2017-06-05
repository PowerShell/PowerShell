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

Describe "Import-Module for Binary Modules in GAC" -Tags 'CI' {

    BeforeAll {
        ##To enable loading assemblies from GAC, we need to add an entry to PowershellProperties.json
        $PropertiesFilePath = Join-Path $pshome 'PowershellProperties.json'
        $savePSProperties = Get-Content $PropertiesFilePath -Raw
        $PSProperties = ConvertFrom-Json $savePSProperties
        $PSProperties | Add-Member -MemberType NoteProperty -Name 'AllowGacLoading' -Value 'true'
        $PSProperties | ConvertTo-Json | Out-File $PropertiesFilePath -Force
    }

    AfterAll {
        #Reset the PowershellProperties.json file.
        $savePSProperties | Out-File $PropertiesFilePath -Force -ErrorAction SilentlyContinue
        Remove-Module PSScheduledJob -Force -ErrorAction SilentlyContinue
    }

    It "Load PSScheduledJob from Windows Powershell Modules folder" -Skip:(-not $IsWindows) {
        $modulePath = Join-Path $env:windir "System32/WindowsPowershell/v1.0/Modules/PSScheduledJob"
        Import-Module $modulePath
        (Get-Command New-JobTrigger).Name | Should Be 'New-JobTrigger'
    }
}
