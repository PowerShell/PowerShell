<##################################################################################### 
 # File: Engine.Modules.PSEdition.Tests.ps1
 # Tests for PowerShell Edition support
 #
 # Copyright (c) Microsoft Corporation, 2016
 #####################################################################################>
 
$CurrentDir = $PSScriptRoot
$ModuleVersioningUtilsPath = Join-Path -Path $CurrentDir -ChildPath ModuleVersioningUtils.psm1
Import-Module $ModuleVersioningUtilsPath

# Run the new parser, return either errors or the ast
#
function Get-ParseFileResults
{
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline=$True,Mandatory=$True)]
        [string]$FilePath, 
        [switch]$Ast
    )

    $errors = $null
    $result = [System.Management.Automation.Language.Parser]::ParseFile($FilePath, [ref]$null, [ref]$errors)
    if ($Ast) { $result } else { ,$errors }
}

Describe 'ModuleManifest and #Requires tests for PSEdition support' -Tags 'BVT', 'InnerLoop' {
     
    # Purpose: Test the CompatiblePSEditions on New-ModuleManifest and PSModuleInfo object
    #
    # Action: New-ModuleManifest -CompatiblePSEditions @('Desktop','Core') -PowerShellVersion '5.1'
    #   
    # Expected Result: PSModuleInfo should have the specified CompatiblePSEditions value
    #   
    It 'Validate New-ModuleManifest with CompatiblePSEditions parameter' {
        $CompatiblePSEditions = @('Desktop','Core')
        $ManifestFileName = "TestPSEdition.psd1"
        $ManifestFilePath = Join-Path -Path $TestDrive -ChildPath $ManifestFileName
        New-ModuleManifest -Path $ManifestFilePath -CompatiblePSEditions $CompatiblePSEditions -PowerShellVersion '5.1'
        $ModuleInfo = Test-ModuleManifest -Path $ManifestFilePath

        $CompatiblePSEditions | % {
            $ModuleInfo.CompatiblePSEditions -contains $_ | Should be true
        }
    }

    It 'Validate New-ModuleManifest with duplicate values for CompatiblePSEditions parameter' {
        $ManifestFilePath = Join-Path -Path $TestDrive -ChildPath 'TestPSEdition.psd1'
        { New-ModuleManifest -Path $ManifestFilePath -CompatiblePSEditions Desktop,deskTop } | should throw Desktop
        { New-ModuleManifest -Path $ManifestFilePath -CompatiblePSEditions coRE,Core } | should throw Core
    }

    # Purpose: Test New-ModuleManifest and PSModuleInfo object without specified CompatiblePSEditions key
    #
    # Action: New-ModuleManifest, Test-ModuleManifest
    #   
    # Expected Result: PSModuleInfo should have zero elements in CompatiblePSEditions
    #   
    It 'Validate New-ModuleManifest without CompatiblePSEditions parameter' {
        $ManifestFileName = "TestPSEdition.psd1"
        $ManifestFilePath = Join-Path -Path $TestDrive -ChildPath $ManifestFileName
        New-ModuleManifest -Path $ManifestFilePath
        $ModuleInfo = Test-ModuleManifest -Path $ManifestFilePath
        $ModuleInfo.CompatibelPSEditions.Count | should be 0
    }

    
    # Purpose: Test the Test-ModuleManifest with only CompatiblePSEditions key and without PowerShellVersion
    #
    # Action: Test-ModuleManifest -Path $ManifestFilePath
    #   
    # Expected Result: Should fail with an error.
    #   
    It 'Validate Test-ModuleManifest with CompatiblePSEditions and without PowerShellVersion value' {
        $CompatiblePSEditions = @('Core')
        $ManifestFileName = "TestPSEdition.psd1"
        $ManifestFilePath = Join-Path -Path $TestDrive -ChildPath $ManifestFileName
        New-ModuleManifest -Path $ManifestFilePath -CompatiblePSEditions @('Desktop','Core')

        { Test-ModuleManifest -Path $ManifestFilePath -ErrorAction Stop } | should throw
    }

    # Purpose: Test the Test-ModuleManifest with only CompatiblePSEditions key and with PowerShellVersion = '3.0'
    #
    # Action: Test-ModuleManifest -Path $ManifestFilePath
    #   
    # Expected Result: Should fail with an error.
    # 
    It 'Validate Test-ModuleManifest with both CompatiblePSEditions and PowerShellVersion = 3.0' {
        $ManifestFileName = "TestPSEdition.psd1"
        $ManifestFilePath = Join-Path -Path $TestDrive -ChildPath $ManifestFileName
        New-ModuleManifest -Path $ManifestFilePath -CompatiblePSEditions @('Desktop','Core') -PowerShellVersion 3.0
        
        { Test-ModuleManifest -Path $ManifestFilePath -ErrorAction Stop } | should throw
    }

    # Purpose: Validate #Requires -PSEdition <CurrentPSEdition>
    #
    # Action: #Requires -PSEdition Desktop
    #   
    # Expected Result: Script should validate the required PSEdition and execute it without any issues.
    #   
    It -skip:($IsCore) 'Validate #Requires -PSEdition in script file' {
        $TestScriptFilePath = Join-Path -Path $TestDrive -ChildPath "ScriptWithRequiredEdition.ps1"

        Set-Content -Path $TestScriptFilePath -Value @"
#requires -PSEdition Desktop, core
Get-Process -Id $pid
"@
        $Result = & $TestScriptFilePath
        $Result.Id | should be $pid
    }

    # Purpose: Validate #Requires -PSEdition InvalidEdition
    #
    # Action: #Requires -PSEdition InvalidEdition
    #   
    # Expected Result: should fail
    #   
    It 'Validate #Requires -PSEdition in a script file with InvalidEdition' {
        $CurrentPSEdition = $PSVersionTable['PSEdition']
        $TestScriptFilePath = Join-Path -Path $TestDrive -ChildPath "ScriptWithRequiredEdition.ps1"

        Set-Content -Path $TestScriptFilePath -Value @"
#requires -PSEdition desktop,InvalidEdition
Get-Process -Id $pid
"@
        { & $TestScriptFilePath } | should throw
    }

    # Purpose: Validate #Requires -PSEdition InvalidEdition
    #
    # Action: #Requires -PSEdition InvalidEdition
    #   
    # Expected Result: should fail
    #   
    It 'Validate multiple #Requires -PSEdition statements in a script file' {
        $TestScriptFilePath = Join-Path -Path $TestDrive -ChildPath "ScriptWithRequiredEdition.ps1"

        Set-Content -Path $TestScriptFilePath -Value @"
#requires -PSEdition Desktop,Core
#requires -PSEdition Core
Get-Process -Id $pid
"@

        $errors = Get-ParseFileResults -FilePath $TestScriptFilePath
        $errors.ErrorId | should be 'ParameterAlreadyBound'
    }

    It 'Validate #Requires -PSEdition statement without any value in a script file' {
        $TestScriptFilePath = Join-Path -Path $TestDrive -ChildPath "ScriptWithRequiredEdition.ps1"

        Set-Content -Path $TestScriptFilePath -Value @"
#requires -PSEdition 
Get-Process -Id $pid
"@

        $errors = Get-ParseFileResults -FilePath $TestScriptFilePath
        $errors.ErrorId | should be 'ParameterRequiresArgument'
    }

    It 'Validate #Requires -PSEdition statement with duplicate values in a script file' {
        $TestScriptFilePath = Join-Path -Path $TestDrive -ChildPath "ScriptWithRequiredEdition.ps1"

        Set-Content -Path $TestScriptFilePath -Value @"
#requires -PSEdition desktop,core,desktop
Get-Process -Id $pid
"@

        $errors = Get-ParseFileResults -FilePath $TestScriptFilePath
        $errors.ErrorId | should be 'RequiresPSEditionValueIsAlreadySpecified'
    }
}

Describe 'Get-Module tests for PSEdition support' -Tags 'PriorityOne','RI' {

    BeforeAll {
        if ( $IsCore ) { return }
        $ModuleNamePrefix="TestModWithEdition_"
        $TestModule_Desktop = "$ModuleNamePrefix" + 'Desktop'
        $TestModule_Core = "$ModuleNamePrefix" + 'Core'
        $TestModule_AllEditions = "$ModuleNamePrefix" + 'AllEditions'

        $ProgramFilesModulesPath = Join-Path -Path $env:ProgramFiles -ChildPath "WindowsPowerShell\Modules"
        $MyDocumentsModulesPath = Join-Path -Path ([Environment]::GetFolderPath("MyDocuments")) -ChildPath "WindowsPowerShell\Modules"

        # Create and Install test modules
        Install-MultiVersionedModule -ModuleName $script:TestModule_Desktop `
                                     -Versions '1.0' `
                                     -NewModuleManifestParams @{CompatiblePSEditions='Desktop'; PowerShellVersion='5.1'}

        Install-MultiVersionedModule -ModuleName $script:TestModule_Core `
                                     -Versions '5.0' `
                                     -NewModuleManifestParams @{CompatiblePSEditions=@('Core'); PowerShellVersion='5.1'}

        Install-MultiVersionedModule -ModuleName $script:TestModule_AllEditions `
                                     -Versions '11.0' `
                                     -ModulePath $MyDocumentsModulesPath `
                                     -NewModuleManifestParams @{CompatiblePSEditions=@('Desktop','Core'); PowerShellVersion='5.1'}

        $NewEditionName = 'NewPSEdition'
        $TestModule_WithNewEdition = "$ModuleNamePrefix" + "$NewEditionName"
        $NewEditionModulePath = Join-Path -Path $ProgramFilesModulesPath -ChildPath ("$TestModule_WithNewEdition\$TestModule_WithNewEdition" + ".psd1")
        $null = New-Item -Path $NewEditionModulePath -ItemType File -Force
        Set-Content -LiteralPath $NewEditionModulePath -Force -Value @"
@{
ModuleVersion = '1.0'
GUID = 'fbf497ce-618c-452a-8ab7-967bb29f67d7'
CompatiblePSEditions = 'Desktop','Core','NewPSEdition'
FunctionsToExport = '*'
CmdletsToExport = '*'
VariablesToExport = '*'
AliasesToExport = '*'
}
"@        
    }

    AfterAll {
        if ( $IsCore ) { return }
        RemoveItem "$ProgramFilesModulesPath\$script:ModuleNamePrefix*"
        RemoveItem "$MyDocumentsModulesPath\$script:ModuleNamePrefix*"
    }

    # Purpose: Test the Get-Module functionality with PSEdition parameter
    #
    # Action: Get-Module -ListAvailable -PSEdition Desktop
    #   
    # Expected Result: should list the expected module
    #   
    It -skip:($IsCore) 'Validate Get-Module cmdlet with -ListAvaiable and -PSEdition' {
       
        $res = Get-Module -ListAvailable -PSEdition 'Desktop'
        $res.Count -ge 2 | should be true
        
        $res = Get-Module -ListAvailable -Name $TestModule_Desktop -PSEdition 'Desktop'
        $res.Name | should be $TestModule_Desktop
        
        $res.CompatiblePSEditions -contains 'Desktop' | should be true

        $res = Get-Module -ListAvailable -PSEdition 'Core'
        $res.Count -ge 2 | should be true
    }

    It -skip:($IsCore) 'Validate Get-Module -ListAvaiable with -PSEdition NewPSEdition' {        
        $res = Get-Module -ListAvailable -PSEdition $NewEditionName -Name $TestModule_WithNewEdition
        $res.Name | should be $TestModule_WithNewEdition
        $res.CompatiblePSEditions -contains $NewEditionName | should be $true
    }

    # Purpose: Validate Get-Module with PSSession and PSEdition
    #
    # Action: Invoke-Command -Session $session {Get-Module -ListAvailable -PSEdition Desktop}
    #   
    # Expected Result: Should get the list of available modules with specified PSEdition value
    #   
    It -skip:($IsCore) 'Validate Get-Module with ListAvailable, PSSession and PSEditon parameters' {
        $session = New-PSSession
        try
        {
            $res = Invoke-Command -Session $session {Get-Module -ListAvailable -PSEdition Desktop}
            $res.Count -ge 2 | should be true
            $res[0].CompatiblePSEditions -contains 'Desktop' | should be true

            $res = Get-Module -ListAvailable -PSEdition Core -PSSession $session
            $res.Count -ge 2 | should be true
            $res[0].CompatiblePSEditions -contains 'Core' | should be true
        }
        finally
        {
            $session | Remove-PSSession
        }
    }
}
