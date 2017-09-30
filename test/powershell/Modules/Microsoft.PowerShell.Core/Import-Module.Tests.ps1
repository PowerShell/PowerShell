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

    It "should be able to load an already loaded module" {
        Import-Module $moduleName
        { $script:module = Import-Module $moduleName -PassThru } | Should Not Throw
        Get-Module -Name $moduleName | Should Be $script:module
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

    $testCases = @(
            @{ TestNameSuffix = 'for top-level module'; ipmoParms =  @{'Name'='.\module1.psd1'}; Expected = '1' }
            @{ TestNameSuffix = 'for top-level and nested module'; ipmoParms =  @{'Name'='.\module2.psd1'}; Expected = '21' }
            @{ TestNameSuffix = 'for top-level module when -Version is specified'; ipmoParms =  @{'Name'='.\module1.psd1'; 'Version'='0.0.1'}; Expected = '1' }
            @{ TestNameSuffix = 'for top-level and nested module when -Version is specified'; ipmoParms =  @{'Name'='.\module2.psd1'; 'Version'='0.0.1'}; Expected = '21' }
        )

    It "Verify ScriptsToProcess are executed <TestNameSuffix>" -TestCases $testCases {
        param($TestNameSuffix,$ipmoParms,$Expected)
        Import-Module @ipmoParms
        Get-Content out.txt | Should Be $Expected
    }
}

Describe "Import-Module for Binary Modules in GAC" -Tags 'CI' {
    Context "Modules are not loaded from GAC" {
        BeforeAll {
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('DisableGACLoading', $true)
        }

        AfterAll {
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('DisableGACLoading', $false)
        }

        It "Load PSScheduledJob from Windows Powershell Modules folder should fail" -Skip:(-not $IsWindows) {
            $modulePath = Join-Path $env:windir "System32/WindowsPowershell/v1.0/Modules/PSScheduledJob"
            { Import-Module $modulePath -ErrorAction SilentlyContinue } | ShouldBeErrorId 'FormatXmlUpdateException,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }
    }

    Context "Modules are loaded from GAC" {
        It "Load PSScheduledJob from Windows Powershell Modules folder" -Skip:(-not $IsWindows) {
            $modulePath = Join-Path $env:windir "System32/WindowsPowershell/v1.0/Modules/PSScheduledJob"
            Import-Module $modulePath
            (Get-Command New-JobTrigger).Name | Should Be 'New-JobTrigger'
        }
    }
}

Describe "Import-Module for Binary Modules" -Tags 'CI' {

    It "PS should try to load the assembly from file path first" {
 $src = @"
using System.Management.Automation;           // Windows PowerShell namespace.

namespace ModuleCmdlets
{
  [Cmdlet(VerbsDiagnostic.Test,"BinaryModuleCmdlet1")]   
  public class TestBinaryModuleCmdlet1Command : Cmdlet
  {
    protected override void BeginProcessing()
    {
      WriteObject("BinaryModuleCmdlet1 exported by the ModuleCmdlets module.");
    }
  }
}
"@

    Add-Type -TypeDefinition $src -OutputAssembly $TESTDRIVE\System.dll
    $results = powershell -noprofile -c "`$module = Import-Module $TESTDRIVE\System.dll -Passthru; `$module.ImplementingAssembly.Location; Test-BinaryModuleCmdlet1"

    #Ignore slash format difference under windows/Unix
    $path = (Get-ChildItem $TESTDRIVE\System.dll).FullName
    $results[0] | Should Be $path
    $results[1] | Should BeExactly "BinaryModuleCmdlet1 exported by the ModuleCmdlets module."
    }
 }

