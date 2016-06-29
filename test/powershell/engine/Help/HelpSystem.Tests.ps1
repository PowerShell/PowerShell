Describe "Help System tests" -tags BVT {
	It "non existent item does not return any result and finishes within 5 minutes" {
		$startTime = [DateTime]::Now
		$helpOutput = Get-Help fakecommandforgettinghelp -ErrorAction SilentlyContinue
		$endTime = [DateTime]::Now

		$helpOutput | Should BeNullOrEmpty
		($endTime - $startTime).TotalMinutes | Should BeLessThan 5
	}
}

Describe "Help system tests" -Tags 'P1','RI' {
    BeforeAll {
        $moduleName = "TestModuleWithVersion"
        $moduleVersionFolder = New-Item -ItemType Directory -Path "TestDrive:\$moduleName\1.0" -Force
        $helpContent = "TestModuleWithVersion help content"
        $helpContent | Out-File -FilePath "$($moduleVersionFolder.FullName)\about_TestModuleWithVersion.help.txt"
        $orginalPsModulePath = $env:PSModulePath 
        $env:PSModulePath += ";TestDrive:\"
    }

    It "can get help content of module with version" {
        $content = Get-Help about_TestModuleWithVersion

        ##We need to add `r`n here as HelpSystem adds it to the end.
        $content | Should Be "$helpContent`r`n"
    }

    AfterAll {
        $env:PSModulePath = $originalPsModulePath
    }
}

Describe "Get-Help works with Ngen binaries" -tags BVT {

    $moduleName = "TestModule"

    # This function creates a PSModule with an assembly which contains '.ni' in the name 
    # (.ni files refer to Ngen binaries)
    #
    function New-TestModule
    {
        param ([switch]$ngenBinary)
        
        # Note: I am using $testDirectory instead of TestDrive:\ because  Pester throws an error when trying
        #       to delete the contents of TestDrive:\.
        $testDirectory = Join-Path $env:TEMP (New-Guid).Guid

        $modulePath = Join-Path $testDirectory $moduleName
        $helpFolder = Join-Path $modulePath (Get-UICulture).Name

        # Create the folder structure.
        if (-not (Test-Path $helpFolder))
        {
            New-Item -Path $helpFolder -Force -ItemType Directory | Out-Null
        }

        $assemblyPath = Join-Path $modulePath ($moduleName + ".ni.dll")
        $moduleManifestPath = Join-Path $modulePath ($moduleName + ".psd1")

        $source = @'
namespace TestModule
{
    using System;
    using System.Management.Automation;

    [Cmdlet(VerbsCommon.Get, "TestNIModule")]
    public class TestSameCmdlets : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            WriteObject("TestNIModule");
        }
    }
}
'@
        # Create the assembly
        Add-Type -TypeDefinition $source -OutputAssembly $assemblyPath

        # Module manifest
        New-ModuleManifest -RootModule ($moduleName + ".ni.dll") -Path $moduleManifestPath -CmdletsToExport "Get-TestNIModule"


        # Create the help file
    $maml = @'
<?xml version="1.0" encoding="utf-8" ?>
<helpItems schema="maml">
  <!-- v 1.1.0.9 English -->
  <command:command xmlns:maml="http://schemas.microsoft.com/maml/2004/10" xmlns:command="http://schemas.microsoft.com/maml/dev/command/2004/10" xmlns:dev="http://schemas.microsoft.com/maml/dev/2004/10">
    <command:details>
      <command:name>
        Get-TestNIModule
      </command:name>
      <maml:description>
        <maml:para>SYNOPSIS for TestNIModule</maml:para>
      </maml:description>
      <maml:copyright>
        <maml:para></maml:para>
      </maml:copyright>
      <command:verb>Get</command:verb>
      <command:noun>TestNIModule</command:noun>
      <dev:version></dev:version>
    </command:details>
    <maml:description>
      <maml:para>Description for TestNIModule</maml:para>
    </maml:description>
    <!-- Cmdlet syntax section-->
    <command:syntax>
      <command:syntaxItem>
        <maml:name>Get-TestNIModule</maml:name>
        <command:parameter required="true" variableLength="true" globbing="false" pipelineInput="true (ByValue)" position="0">
          <maml:name>Name</maml:name>
          <maml:description>
            <maml:para></maml:para>
          </maml:description>
          <command:parameterValue required="true" variableLength="true">string</command:parameterValue>
        </command:parameter>
      </command:syntaxItem>
    </command:syntax>
    <!-- Example section  -->
    <command:examples>
      <command:example>
        <maml:title>Examples for Get-TestNIModule</maml:title>
        <maml:introduction>
          <maml:para></maml:para>
        </maml:introduction>
        <dev:code>
        </dev:code>
        <dev:remarks>
        </dev:remarks>
        <command:commandLines>
          <command:commandLine>
            <command:commandText></command:commandText>
          </command:commandLine>
        </command:commandLines>
      </command:example>
    </command:examples>
  </command:command>
</helpItems>
'@
        if ($ngenBinary)
        {
            $helpFilePath = Join-Path $helpFolder ($moduleName + ".dll-Help.xml")
        }
        else
        {
            $helpFilePath = Join-Path $helpFolder ($moduleName + ".ni.dll-Help.xml")
        }
        Set-Content -Value $maml -Path $helpFilePath -Force

        return $modulePath
    }

    <#
        Get-Help works for a PSModule with an Ngen assembly. Ngen assemblies contain .ni in their name, 
        e.g., MyModuleName.ni.dll. This means that the help file name will not have '.ni' in the name,
        so it will look something like this: MyModuleName.dll-Help.xml
    #>
    It "Get-Help works for a PSModule with an Ngen assembly" {
        
        try
        {
            $testModule = New-TestModule -ngenBinary
            Import-Module $testModule

            # Make sure the test cmdlet is available
            Get-TestNIModule | Should Be TestNIModule

            # Call get-help
            $helpContent = Get-Help Get-TestNIModule
            $helpContent.examples | Should Match Get-TestNIModule
        }
        finally
        {
            Get-Module $moduleName -ea SilentlyContinue | Remove-Module -ea SilentlyContinue 
        }
    }

    <#
        Get-Help works for a PSModule with .ni' in the assembly name, 
        e.g., MyModuleName.ni.dll. This means that the help file name will contain '.ni' 
        in the name, so it will look something like this: MyModuleName.ni.dll-Help.xml
    #>
    It "Get-Help works for a PSModule with an assembly which contains '.NI' in the name" {
        
        try
        {
            $testModule = New-TestModule
            Import-Module $testModule

            # Make sure the test cmdlet is available
            Get-TestNIModule | Should Be TestNIModule

            # Call get-help
            $helpContent = Get-Help Get-TestNIModule
            $helpContent.examples | Should Match Get-TestNIModule
        }
        finally
        {
            Get-Module $moduleName -ea SilentlyContinue | Remove-Module -ea SilentlyContinue 
        }
    }
}

Describe "Comment based help preserves the types in Syntax" -Tags 'P1','RI' {

    BeforeAll {
        <#   
        .DESCRIPTION  
           Test function for GetHelpRegression422466.ps1 
           and GetHelpRegression422467.ps1    

        .SYNOPSIS   
           A test function whose help content contains Common parameters and  
           a mandatory parameter in syntax section.   
        #>   
        function TestFunction1 {  
           Param(  
               [Parameter(Mandatory=$true)]  
               [string[]] $Names,     

               [System.Management.Automation.SwitchParameter]  
               $Force  
           )  
        }
    }

    It "Comment based help displays syntax properly and has only one 'CommonParameters'" {
        $help = Get-Help TestFunction1
        $stringSyntax = $help.Syntax | Out-String -Stream -Width 1000           
        $stringSyntax = ($stringSyntax -join ' ').Trim()
        $stringSyntax | Should Be 'TestFunction1 [-Names] <String[]> [-Force] [<CommonParameters>]'
        ($stringSyntax -split 'CommonParameters').Count | Should Be 2        
    }   
}

Describe "About help for modules with multiple versions shows help for latest version" -Tags 'P1','RI' {
    
    BeforeAll {
        $version1Help = 'This is help version 1'
        $version2Help = 'This is help version 2'
        $version3Help = 'This is help version 3'
        $oldHelp = 'This help is old'
        $oldPSModPath = $env:PSModulePath
        
        $moduleName = "HelpProviderTest" + (Get-Random)
        $moduleDirectoryBase = "$TestDrive\Modules" 
        $moduleDirectoryPath = "$moduleDirectoryBase\$moduleName" 
        $null = New-Item -ItemType Directory -Path $moduleDirectoryPath -Force
        $null = New-Item -ItemType Directory -Path "$moduleDirectoryPath\1.0.0.0\en-US"
        $null = New-Item -ItemType Directory -Path "$moduleDirectoryPath\2.0.0.0\en-US"
        $null = New-ModuleManifest -Path "$moduleDirectoryPath\1.0.0.0\$moduleName.psd1" -ModuleVersion "1.0.0.0"   
        $null = New-ModuleManifest -Path "$moduleDirectoryPath\2.0.0.0\$moduleName.psd1" -ModuleVersion "2.0.0.0"        
        $null = New-Item "$moduleDirectoryPath\1.0.0.0\en-US\about_$moduleName.help.txt" -Value $version1Help
        $null = New-Item "$moduleDirectoryPath\2.0.0.0\en-US\about_$moduleName.help.txt" -Value $version2Help
        $null = New-Item "$moduleDirectoryPath\1.0.0.0\en-US\about_My$moduleName.help.txt" -Value $oldHelp
        
        $userModuleDirectoryBase = "$TestDrive\Modules2"
        $userModuleDirectory = "$userModuleDirectoryBase\$moduleName"
        $env:PSModulePath += ";$moduleDirectoryBase;$userModuleDirectoryBase"
    }

    AfterAll {
        if(Test-Path $userModuleDirectory)
        {
            Remove-Item -Recurse -Force -Path $userModuleDirectory -ErrorAction SilentlyContinue
        }

        $env:PSModulePath = $oldPSModPath
    }
    
    It "shows help content" {
        Get-Help "about_$moduleName" | Should Be $version2Help
    }

    It "shows help content with wildcard" {
        Get-Help "about_$moduleName*" | Should Be $version2Help
    }

    It "shows help content when filename is not repeated and is in older module" {
        Get-Help "about_My$moduleName" | Should Be $oldHelp
    }

    It "shows help when modules are rooted at different locations" {
        $null = New-Item "$userModuleDirectory\3.0.0.0\en-US" -Force -ItemType Directory
        $null = New-ModuleManifest -Path "$userModuleDirectory\3.0.0.0\$moduleName.psd1" -ModuleVersion "3.0.0.0"
        $null = New-Item "$userModuleDirectory\3.0.0.0\en-US\about_$moduleName.help.txt" -Value $version3Help

        Get-Help "about_$moduleName" | Should Be $version3Help
    }

    It "shows help when modules are rooted at different locations with newer version first is modulepath" {
        $env:PSModulePath = $oldPSModPath
        $env:PSModulePath += ";$userModuleDirectoryBase;$moduleDirectoryBase"

        $null = New-Item "$userModuleDirectory\3.0.0.0\en-US" -Force -ItemType Directory
        $null = New-ModuleManifest -Path "$userModuleDirectory\3.0.0.0\$moduleName.psd1" -ModuleVersion "3.0.0.0"
        $null = New-Item "$userModuleDirectory\3.0.0.0\en-US\about_$moduleName.help.txt" -Value $version3Help -Force

        Get-Help "about_$moduleName" | Should Be $version3Help
    }
}