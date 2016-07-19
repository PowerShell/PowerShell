# This is a Pester test suite to validate the Test Followups for Powershell Module Versioning
#
# Copyright (c) Microsoft Corporation, 2015
#
#

Describe "TestFollowupForBugs" -Tags "Feature" {


BeforeAll {
    if ( $IsCore ) { return }
    $CurrentDir = Split-Path $MyInvocation.MyCommand.Path
    $TestModulesFolder= 'TestModulesForFollowUp'
    $TestModulesFolder1= 'Test.module'
    $TestModulesFolder2= 'TestModuleRelativePath'
    $TestModulesPath = Join-path $CurrentDir $TestModulesFolder
    $TestModulesPath1 = Join-path $CurrentDir $TestModulesFolder1
    $TestModulesPath2 = Join-path $CurrentDir $TestModulesFolder2
    $script:UserModulesPath = Join-path $env:userprofile '\Documents\WindowsPowershell\Modules'
    # Install the Test Module
    Copy-Item $TestModulesPath $UserModulesPath -Recurse -Force
    Copy-Item $TestModulesPath1 $UserModulesPath -Recurse -Force
    Copy-Item $TestModulesPath2 $UserModulesPath -Recurse -Force
#Generate Binary Module
#
function GenerateBinaryModule($ModulePath)
{    
  $BinaryModule = @"
using System;
using System.Management.Automation;
namespace TestBinaryModuleScope
{
    [Cmdlet("Get","TestModule")]
    public class GetTestModuleScopeCommand : PSCmdlet
    {
        protected override void ProcessRecord()
        {
           WriteObject("Testmodule");
        }
    }
}
"@
    if(!(test-Path $modulePath))
    {
        Add-Type -TypeDefinition $BinaryModule -OutputAssembly $ModulePath   
    } 
}

function GenerateBinaryModule1($ModulePath)
{    
  $BinaryModule = @"
using System;
using System.Management.Automation;
namespace TestBinaryModuleScope
{
    [Cmdlet("Get","TestModule1")]
    public class GetTestModuleScopeCommand : PSCmdlet
    {
        protected override void ProcessRecord()
        {
           WriteObject("Testmodule1");
        }
    }
}
"@
    if(!(test-Path $modulePath))
    {
        Add-Type -TypeDefinition $BinaryModule -OutputAssembly $ModulePath   
    }  
}
}

    <#
    Purpose:
        Verify TFS bug: 2173155 Get-Module -ListAvailable report the subfolder 
        as a module when there is psd1 file in the version\subfolder where subfolder 
        name is same as the module name and the root module folder is empty.
                
    Action:
        Create a module under $pshome\Modules with version folder. Place valid psd1 file under both version folder and nested folder.
        In old behavior, get-module -ListAvailable will show both modules.
        The fix should avoid that and ignore the nested module.
               
    Expected Result: 
        Only the psd1 file under version folder will be discovered.
    #>
    It -pending:($IsCore) "Bug 2173155" {
        $moduleName="TestModVer_$(Get-Random)"
        $modulePath = "$pshome\Modules\$modulename"
        new-item -type directory $modulePath

        $version = "1.0.3.1"
        $version2 = "1.0"
        new-item -type directory $modulePath\$version

        $manifestPath = "$modulePath\$version\$moduleName.psd1"

        # create the root psd1 file supposd to be discovered
        New-ModuleManifest $manifestPath -ModuleVersion $version

        $nestedModule = "$modulePath\$version\$moduleName"

        new-item -type directory $nestedModule

        $nestedManifestPath = "$nestedModule\$moduleName.psd1"

        # create the nested psd1 file not supposd to be discovered
        New-ModuleManifest $nestedManifestPath -ModuleVersion $version2

        try
        {
            $module = get-module -ListAvailable $moduleName
            $module.Count | should be 1
            $module.Version.ToString() | should be $version
        }
        catch
        {
            throw $_.FullyQualifiedErrorId
        }
        finally
        {
            Remove-Item $modulePath -Recurse -Force
        }
    }

    <#
    Purpose:
        Verify TFS bug: 1169495 Exceptions raised in a script loaded via a module's ScriptsToProcess 
        manifest entry don't prevent a module from loading
                
    Action:
        Create a module under $pshome\Modules with a script throwing exception. Load the script in ScriptsToProcess in a module Manifest file.
        Import the module manifest
               
    Expected Result: 
        The module should not be loaded.
    #>
    It -pending:($IsCore) "Bug 1169495" {
        $moduleName="ModuleA"
        $modulePath = "$pshome\Modules\$modulename"
        if (test-path $modulePath)
        {
            Remove-Item $modulePath -Recurse -Force
        }
        new-item -type directory $modulePath

        # Create ModuleA manifest
        New-ModuleManifest -path $modulesPath\ModuleA.psd1 -RootModule ModuleA.psm1 -ModuleVersion 1.0.0.1 -ScriptsToProcess ScriptA.ps1 -FileList @('ModuleA.psm1','ModuleA.psd1','ScriptA.ps1')
        
        # Create ModuleA script module
@'
Write-Host 'Look at me, I''m loading, even though an exception was raised!'
'@ | Out-File -LiteralPath $modulesPath\ModuleA.psm1
        # Create ScriptA file
@"
throw 'Something bad happened, so the module shouldn''t load.'
"@ | Out-File -LiteralPath $modulesPath\ScriptA.ps1


        Get-Module ModuleA | Remove-Module -ErrorAction SilentlyContinue

        try
        {
            # Load ModuleA
            Import-Module ModuleA -Force -ErrorAction SilentlyContinue
            throw "Throw exception in scriptToProcess should be caught as it is."
        }
        catch
        {
            $module = Get-Module ModuleA
            $module.Count | should be 0
        }
        finally
        {
            Remove-Item $modulePath -Recurse -Force
        }
    }

    <#
    Purpose:
        Verify TFS bug: 1169509 Exceptions raised in a module manifest cause an 
        incorrect error to be generated after the exception is displayed
                
    Action:
        Create a module under $pshome\Modules with psm1 file throwing exception. 
        Import the module. 
               
    Expected Result: 
        When you import the module, you should only see the one, true exception that the module raised, allowing you to take appropriate action to then load the module again.  
        No error pointing the finger at the module author should be displayed in this use case.

    #>
    It -pending:($IsCore) "Bug 1169509" {
        $moduleName="ModuleB"
        $modulePath = "$pshome\Modules\$modulename"
        if (test-path $modulePath)
        {
            Remove-Item $modulePath -Recurse -Force
        }
        new-item -type directory $modulePath

        # Create ModuleB manifest
        New-ModuleManifest -path $modulesPath\ModuleB.psd1 -RootModule ModuleB.psm1 -ModuleVersion 1.0.0.1 -FileList @('ModuleB.psm1','ModuleB.psd1')
        
        # Create ModuleB script module
@'
function Test-ForPrerequisite {
[CmdletBinding()]
param()
$false
}
if (-not (Test-ForPrerequisite)) {
throw 'Prequisite requirements are not met. Correct them and then try loading the module again.'
}
'@ | Out-File -LiteralPath $modulesPath\ModuleB.psm1

        Get-Module ModuleB | Remove-Module -ErrorAction SilentlyContinue

        try
        {
            # Load ModuleB
            Import-Module ModuleB -Force -ErrorAction SilentlyContinue
            throw "Throw exception in scriptToProcess should be caught as it is."
        }
        catch
        {
        }
        finally
        {
            Remove-Item $modulePath -Recurse -Force
        }
    }

    <#
    Purpose:
        Verify TFS bug: 2320366 "Get-Module -Name $null -ListAvailable", -Name param value is not validated 
                
    Action:
        Get-Module -Name $null -ListAvailable
        Get-Module -Name "" -ListAvailable
               
    Expected Result: 
        Grace exception should be thrown
    #>
        It -pending:($IsCore) "Bug 2320366" {
            try
            {
                Get-Module -Name $null -ListAvailable
                throw "No exception is caught, ParameterArgumentValidationError is expected."
            }
            catch
            {
                $_.FullyQualifiedErrorId | should be "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetModuleCommand"
            }

            try
            {
                Get-Module -Name "" -ListAvailable
                throw "No exception is caught, ParameterArgumentValidationError is expected."
            }
            catch
            {
                $_.FullyQualifiedErrorId | should be "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetModuleCommand"
            }
        }

    <#
    Purpose:
        Verify TFS bug: 1987318 Import-Module incorrectly detects a cycle in the module dependency graph
                
    Action:
        Create a bunch modules which don't contain cycle logically
               
    Expected Result: 
        root module should be imported without exception.

    #>
    It -pending:($IsCore) "Bug 1987318" {
        $ModulePath = Join-Path $env:USERPROFILE "Documents\WindowsPowerShell\Modules"
        "m1","m2","m3","m4","m5","m6" | %{ new-item -type directory (Join-Path $ModulePath $_ ) -ea 0 }
 
        New-ModuleManifest -Path (Join-Path $ModulePath "m1\m1.psd1") -RequiredModules "m5","m2"
        New-ModuleManifest -Path (Join-Path $ModulePath "m2\m2.psd1") -RequiredModules "m3","m4"
        New-ModuleManifest -Path (Join-Path $ModulePath "m3\m3.psd1") -RequiredModules "m4"
        New-ModuleManifest -Path (Join-Path $ModulePath "m4\m4.psd1") -RequiredModules "m5"
        New-ModuleManifest -Path (Join-Path $ModulePath "m5\m5.psd1")
 
        # m1 ------------------------> m5
        # |----> m2 ---------> m4 -----^
        #        |----> m3 ----^
 
        #No cycles, this can be loaded m5, m4, m3, m2, m1


        try
        {
            import-module m1 -force
        }
        catch
        {
            throw $_.FullyQualifiedErrorId
        }
        finally
        {
            "m1","m2","m3","m4","m5" | %{ remove-item -Recurse (Join-Path $ModulePath $_ ) }
        }
    }

    <#
    Purpose:
        Verify TFS bug: 2737519 Import-Module -Assembly does not return object to pipeline when -PassThru is used
                
    Action:
        import a binary module with -passthru
               
    Expected Result: 
        module object should be returned

    #>
    It -pending:($IsCore) "Bug 2737519" {
        $nsName = "MyUnitTest"
        $className = "MyClass_" + (Get-Random)
        $source = @"
using System.Management.Automation;
namespace $nsName {
[Cmdlet("Invoke", "$className")]
public sealed class $className : PSCmdlet {
   protected override void ProcessRecord() {
     this.WriteObject(this.MyInvocation.MyCommand.Name); 
   }
  }
}
"@
        $type = Add-Type -TypeDefinition $source -Language CSharp -PassThru 
        # ** BUG ** Import-Module is not returning an object when -PassThru is used
        $module = Import-Module -Assembly $type[0].Assembly -PassThru -Force
        $module.gettype().name | should be "PSModuleInfo"
    }

    <#
    Purpose:
        Verify TFS bug: 2737859 Import-Module -Assembly with dynamic code incorrectly stores entries in the ModuleTable
                
    Action:
        import several assemblies that doesn't have physical location
               
    Expected Result: 
        the module should be imported and the cmdlets from the module should be valid

    #>
    It -pending:($IsCore) "Bug 2737859" {
    function GenerateCmdlet {
        $nsName = "MyUnitTest"
        $className = "MyClass_" + (Get-Random)
        $source = @"
using System.Management.Automation;
namespace $nsName {
    [Cmdlet("Invoke", "$className")]
    public sealed class $className : PSCmdlet {
        protected override void ProcessRecord() { 
            this.WriteVerbose(string.Format("hi {0}", this.MyInvocation.MyCommand.Name));
        }
    }
}
"@
        $type = Add-Type -TypeDefinition $source -Language CSharp -PassThru
        Import-Module -Assembly $type[0].Assembly -Force
        return "Invoke-$className"
    }
    $Error.Clear()
    Push-Location
    Set-Location cert:
    # Generate memory assemblies several times, make sure the unique module name is added to the module table.
    Set-Location $env:SystemDrive
    $cmdlet = GenerateCmdlet ; . $cmdlet
    $cmdlet = GenerateCmdlet ; . $cmdlet
    Set-Location cert:
    $cmdlet = GenerateCmdlet ; . $cmdlet
    $cmdlet = GenerateCmdlet ; . $cmdlet
    Set-Location $env:SystemDrive
    $cmdlet = GenerateCmdlet ; . $cmdlet
    Pop-Location
}


    It "Bug 4055277" {
        try
        {
            # This would overflow the stack
            $modPath = $env:PSModulePath
            $env:PSModulePath = "$pshome;$psscriptroot\MSFT_4055277"
            Remove-Module Microsoft.PowerShell.Management
            $null = Get-Module -List
        }
        finally
        {
            $env:PSModulePath = $modPath
        }
    }

    <#
    Purpose:
        Verify TFS bug: 3584195 Import-Module fails because ExecutionContext.ModuleBeingProcessed is not always reset after running Test-ModuleManifest
                
    Action:
        Test-ModuleManifest a valid module's manifest, then try to import that module.
               
    Expected Result: 
        Module should be imported

    #>
    It -pending:($IsCore) "Bug 3584195" {

    $m = get-module -list microsoft.* | select -first 1
    $m | Should Not Be $null

    Test-ModuleManifest $m.path

    $errorThrown = $null
    try
    {
    Import-Module -Name $m.Name -ErrorAction Stop 
    }
    catch
    {
    $errorThrown = $_ 
    }

    $errorThrown | Should Be $null

    $importedModule = Get-Module -Name $m.Name
    $importedModule | Should Not Be $null
    Remove-Module $m.Name
}

    <#
    Purpose:
        Verify TFS bug: 4800438 ModuleSpecification.ToString() is not returning a valid module spec string when MaximumVersion is specified
                
    Action:
        New-Object Microsoft.PowerShell.Commands.ModuleSpecification @{ModuleName='Foo';MaximumVersion='2.0'}
               
    Expected Result: 
        the tostring() function should return @{ModuleName='Foo';MaximumVersion='2.0'} 

    #>
    It "Bug 4800438" {
        $moduleObject = New-Object Microsoft.PowerShell.Commands.ModuleSpecification @{ModuleName='Foo';MaximumVersion='2.0'}
        $moduleObject.ToString() | Should be "@{ ModuleName = 'Foo'; MaximumVersion = '2.0' }"
    }

    <#
    Purpose:
        Verify TFS bug: 4980967 qualified <module>\<function> doesn't execute last loaded module version
                
    Action:
        Create 3 versions of this module under %userprofile%\documents\windowspowershell\modules\TestModulesForFollowUp
 
        # TestModulesForFollowUp.psd1
        @{
        ModuleVersion = '1.0.0.0'
        GUID = '2414b58b-b954-4fff-b577-a901265f5690'
        Author = 'cchen'
        CompanyName = 'Unknown'
        Copyright = '(c) 2015 cchen. All rights reserved.'
        NestedModules = @("TestModulesForFollowUp.psm1")
        FunctionsToExport = '*'
        }
 
        # TestModulesForFollowUp.psm1
        function Write-HelloWorld () {
        "Hello World 1.0.0.0"
        }
 
        Update the version numbers to be 1.0.0.0, 1.0.1.0, 1.0.2.0

               
    Expected Result: 
         test\Write-HelloWorld should always call the latest module being imported.

    #>
    It -pending:($IsCore) "Bug 4980967" {
        Import-Module TestModulesForFollowUp -RequiredVersion 1.0.0.0
        Import-Module TestModulesForFollowUp -RequiredVersion 1.0.2.0 
        Import-Module TestModulesForFollowUp -RequiredVersion 1.0.1.0

        TestModulesForFollowUp\Write-HelloWorld | should Be "Hello World 1.0.1.0"
        Write-HelloWorld | should Be "Hello World 1.0.1.0"
    }

    <#
    Purpose:
        Verify TFS bug: 4761030 Get-command should get the command if -FullyQualifiedModule contains guid
                
    Action:
        get-command -FullyQualifiedModule @{ModuleName="TestModulesForFollowup";RequiredVersion="1.0.0.0";Guid='2414b58b-b954-4fff-b577-a901265f5690'}

               
    Expected Result: 
         Function withe specified module should return

    #>
    It -pending:($IsCore) "Bug 4761030" {
        Remove-Module TestModulesForFollowUp -Force -ErrorAction Ignore
        $result = get-command -FullyQualifiedModule @{ModuleName="TestModulesForFollowup";RequiredVersion="1.0.0.0";Guid='2414b58b-b954-4fff-b577-a901265f5690'}
        $result.count | should be 1
        $result.Source | should be "TestModulesForFollowUp"
    }

    <#
    Purpose:
        Verify TFS bug: 5051137 "Get-Module -List -Name <ModuleBasePathEndingWithBackSlash>" is failing with Modules_ModuleNotFoundForGetModule error
                
    Action:
        Get-Module -Name C:\windows\System32\WindowsPowerShell\v1.0\Modules\PSWorkflow\ -ListAvailable

               
    Expected Result: 
         Module should be found just as Get-Module -Name C:\windows\System32\WindowsPowerShell\v1.0\Modules\PSWorkflow -ListAvailable

    #>
    It -pending:($IsCore) "Bug 5051137" {
        $moduleA = Get-Module -Name C:\windows\System32\WindowsPowerShell\v1.0\Modules\PSWorkflow\ -ListAvailable
        $moduleB = Get-Module -Name C:\windows\System32\WindowsPowerShell\v1.0\Modules\PSWorkflow -ListAvailable
        $moduleA.count | should Be $moduleB.count
    }

    <#
    Purpose:
        Verify TFS bug: 4800039 "MaximumVersion value is not validated for multiple *s in ModuleSpecification ctor"
                
    Action:
        New-Object Microsoft.PowerShell.Commands.ModuleSpecification @{ModuleName='Foo';MaximumVersion='1.*.*.0'}

               
    Expected Result: 
         error message that you can't specify multiple wildcards

    #>
    It "Bug 4800039" {
        try
        {
            New-Object Microsoft.PowerShell.Commands.ModuleSpecification @{ModuleName='Foo';MaximumVersion='1.*.*.0'}
            throw "Error message that you can't specify multiple wildcards is expected."
        }
        catch
        {
            if ($_.FullyQualifiedErrorId -ne "ConstructorInvokedThrowException,Microsoft.PowerShell.Commands.NewObjectCommand")
            {
                throw "Incorrect exception is thrown."
            }
        }

    }

    <#
    Purpose:
        Verify TFS bug: 5101884 "Get-Module -List -Name <PathWithMultipleVersion>" is returning only latest version
                
    Action:
        gmo -li powershellget

               
    Expected Result: 
         Module with all the versions should get exposed.

    #>
    It -pending:($IsCore) "Bug 5101884" {
        $result = get-module -ListAvailable Test.module
        $result.count | should be 3
}

    <#
    Purpose:
        Verify TFS bug: 6335613 "Get-Module -List -Name <Path>" is not working if the module name contains "."
                
    Action:
        get-module -ListAvailable C:\Windows\System32\WindowsPowerShell\v1.0\Modules\Microsoft.PowerShell.Archive

               
    Expected Result: 
         Module should be found

    #>
    It -pending:($IsCore) "Bug 6335613" {
        $result = get-module -ListAvailable C:\Windows\System32\WindowsPowerShell\v1.0\Modules\Microsoft.PowerShell.Archive
        $result.count | should be 1
}

    <#
    Purpose:
        Verify TFS bug: 5101923 "Get-Module -List -Name <RelativePath>" is not working
                
    Action:
        cd C:\Windows\System32\WindowsPowerShell\v1.0\Modules\Microsoft.PowerShell.Archive
        get-module -ListAvailable .\

               
    Expected Result: 
         Module should be found

    #>
    It -pending:($IsCore) "Bug 5101923" {
        pushd
        cd $env:SystemRoot\system32\WindowsPowerShell\v1.0\Modules\Microsoft.PowerShell.Archive
        $result = get-module -ListAvailable .\
        popd
        $result.count | should be 1
}

    <#
    Purpose:
        Verify TFS bug: 5101900 "Get-Module -List -Name <PathWithVersionFolder>" is not working
                
    Action:
        get-module -li C:\Windows\System32\WindowsPowerShell\v1.0\Modules\test.module\1.0.0.1

               
    Expected Result: 
         Module should be found

    #>
    It -pending:($IsCore) "Bug 5101900" {
        $enviromentPsmodule = $env:PSModulePath

        $UserModulesPath = Join-path $env:userprofile '\Documents\WindowsPowershell\Modules'
        $result = Get-Module -li $UserModulesPath\Test.module\1.0.0.1
        $result.count | should be 1
}

    <#
    Purpose:
        Verify TFS bug: 5899273 Remove-Module does not work for removing module created with Add-Type if Import-Module was run from a non-FileSystem drive
                
    Action:
        goes to a non file directory
        generate a dynamic assembly and import as module
        try to remove the module

               
    Expected Result: 
         The module should be removed.

    #>
    It -skip:($IsCore) "Bug 5899273" {
        pushd $HOME
        cd hkcu:\


        $name = ("MyCmdlet" + ([System.Random]::new().Next()))
$code = @"
using System;
using System.Management.Automation;
[Cmdlet("Invoke", "$name")]
public class $name : PSCmdlet { protected override void ProcessRecord() { this.WriteObject("hello world"); } }
"@

        $type = Add-Type -TypeDefinition $code -Language CSharp -PassThru
        $mod = Import-Module -Assembly $type.Assembly -PassThru
        . Invoke-$name -ErrorAction Stop | Out-Null
        $mod | Remove-Module -Force
        $result = Get-Module $mod
        popd
        $result.count | should be 0

}

    <#
    Purpose:
        Verify TFS bug: 4761531 Module version support on module-qualified command. Such as “Microsoft.PowerShell.Archive\1.0.0.0\Compress-Archive”
                
    Action:
        test.module\1.0.0.3\foo

               
    Expected Result: 
         Function from that specific version should be executed

    #>
    It -pending:($IsCore) "Bug 4761531" {
        $result = test.module\1.0.0.5\foo 
        $result | should be "1.0.0.5"
        $result = test.module\1.0.0.3\foo 
        $result | should be "1.0.0.3"
        $result = test.module\1.0.0.1\foo 
        $result | should be "1.0.0.1"

}

    <#
    Purpose:
        Verify TFS bug: 6679218 Duplicate path entries in PSModulePath cause modules to be detected multiple times”
                
    Action:
        add same module path with slight different characters(with/without back slash at the end) in $env:psmodulePath
        get-module -listavailable test.module, test.module should exists in the previous module path

               
    Expected Result: 
         No duplicated modules should be found.

    #>
    It -pending:($IsCore) "Bug 6679218" {
        $enviromentPsmodule = $env:PSModulePath

        $UserModulesPath = Join-path $env:userprofile '\Documents\WindowsPowershell\Modules'
        $duplicateUserModulesPath = Join-path $env:userprofile '\Documents\WindowsPowershell\Modules\'
        
        $moduleList1 = Get-Module -ListAvailable test.module
        $env:PSModulePath = $env:PSModulePath + ";" + $UserModulesPath + ";" + $duplicateUserModulesPath
        try
        {
            $moduleList2 = Get-Module -ListAvailable test.module
            $moduleList1.count | should be $moduleList2.count
        }
        finally
        {
            $env:PSModulePath = $enviromentPsmodule
        }

}

    <#
    Purpose:
        Verify TFS bug: 6794059 get-module -listavailable should support wild card in module path
                
    Action:
        get-module -Listavailable c:\$env:userprofile\Documents\WindowsPowershell\Modules\test.modu*

               
    Expected Result: 
         Matched modules should be return

    #>
    It -pending:($IsCore) "Bug 6794059 - 1" {

        $UserModulesPath = Join-path $env:userprofile '\Documents\WindowsPowershell\Modules'
        
        $moduleList = Get-Module -ListAvailable $UserModulesPath\test.modul*
        $moduleList.Count | should be 3
    }

    <#
    Purpose:
        Verify TFS bug: 6794059 get-module -listavailable should support wild card in module path
                
    Action:
        get-module -Listavailable c:\$env:userprofile\Documents\WindowsPower*ell\Modules\test.module

               
    Expected Result: 
         Matched modules should be return

    #>
    It -pending:($IsCore) "Bug 6794059 - 2" {

        $UserModulesPath = Join-path $env:userprofile '\Documents\WindowsPower*ell\Modules'
        
        $moduleList = Get-Module -ListAvailable $UserModulesPath\test.module
        $moduleList.Count | should be 3
    }

    <#
    Purpose:
        Verify TFS bug: 6794059 get-module -listavailable should support wild card in module path
                
    Action:
        get-module -Listavailable .\Documents\WindowsPower*ell\Modules\test.modu*

               
    Expected Result: 
         Matched modules should be return

    #>
    It -pending:($IsCore) "Bug 6794059 - 3" {

        Push-Location
        Set-Location $env:userprofile     
        $moduleList = Get-Module -ListAvailable .\Documents\WindowsPower*ell\Modules\test.modu*
        $moduleList.Count | should be 3
        Pop-Location
    }

    <#
    Purpose:
        Verify TFS bug: 6794059 get-module -listavailable should support wild card in module path
                
    Action:
        get-module -Listavailable c:\$env:userprofile\Documents\WindowsPower*ell\Modules\test.module\*
        multiple paths will be resolved

               
    Expected Result: 
         Matched modules should be return

    #>
    It -pending:($IsCore) "Bug 6794059 - 4" {

        $UserModulesPath = Join-path $env:userprofile '\Documents\WindowsPowershell\Modules'
        
        $moduleList = Get-Module -ListAvailable $UserModulesPath\test.module\*
        $moduleList.Count | should be 3
    }

    <#
    Purpose:
        Verify TFS bug: 6964776 Import-module should handle importing NI and non NI binaries
                
    Action:
        import-module c:\temp\testModule, testModule contains testmodule.ni.dll only

               
    Expected Result: 
         Module should be imported

    #>
    It -pending:($IsCore) "Bug 6964776 - 1" {

        $UserModulesPath = Join-path $env:userprofile '\Documents\WindowsPowershell\Modules\testModule'
        new-item -type directory $UserModulesPath -ErrorAction Ignore
        GenerateBinaryModule -ModulePath $UserModulesPath\testModule.ni.dll

        try 
        {
            import-module $UserModulesPath
            $module = get-module testModule
            $module.Count | should be 1
        }
        catch
        {
            throw $_
        }
        finally
        {
            Remove-Module testModule -force -Erroraction SilentlyContinue
            Remove-Item $UserModulesPath -recurse -force -ErrorAction SilentlyContinue
        }

    }

    <#
    Purpose:
        Verify TFS bug: 6964776 Import-module should handle importing NI and non NI binaries
                
    Action:
        import-module c:\temp\testModule, testModule contains testmodule.ni.dll and the modulemanifest refer the rootmodule as itself. 
        This used to work when the module folder contains testmodule.dll but won't work if the folder contains testmodule.ni.dll only.

               
    Expected Result: 
         Module should be imported

    #>
    It -pending:($IsCore) "Bug 6964776 - 2" {

        $UserModulesPath = Join-path $env:userprofile '\Documents\WindowsPowershell\Modules\testModule'
        new-item -type directory $UserModulesPath -ErrorAction Ignore
        GenerateBinaryModule -ModulePath $UserModulesPath\testModule.ni.dll
        New-ModuleManifest -Path $UserModulesPath\testModule.psd1 -RootModule testModule

        try 
        {
            import-module $UserModulesPath
            $module = get-module testModule
            $module.Count | should be 1
        }
        catch
        {
            throw $_
        }
        finally
        {
            Remove-Module testModule -force -Erroraction SilentlyContinue
            Remove-Item $UserModulesPath -recurse -force -ErrorAction SilentlyContinue
        }

    }

    <#
    Purpose:
        Verify TFS bug: 6964776 Import-module should handle importing NI and non NI binaries
                
    Action:
        get-module -list c:\temp\testModule, testModule contains testmodule.ni.dll only
               
    Expected Result: 
         Module should be returned

    #>
    It -pending:($IsCore) "Bug 6964776 - 3" {

        $UserModulesPath = Join-path $env:userprofile '\Documents\WindowsPowershell\Modules\testModule'
        new-item -type directory $UserModulesPath -ErrorAction Ignore
        GenerateBinaryModule -ModulePath $UserModulesPath\testModule.ni.dll

        try 
        {
            $module = Get-Module -ListAvailable $UserModulesPath
            $module.Count | should be 1
        }
        catch
        {
            throw $_
        }
        finally
        {
            Remove-Module testModule -force -Erroraction SilentlyContinue
            Remove-Item $UserModulesPath -recurse -force -ErrorAction SilentlyContinue
        }

    }

    <#
    Purpose:
        Verify TFS bug: 6964776 Import-module should handle importing NI and non NI binaries
                
    Action:
        get-module -list c:\temp\testModule, testModule contains testmodule.ni.dll and testmodule.dll

               
    Expected Result: 
         testmodule.ni.dll should be imported

    #>
    It -pending:($IsCore) "Bug 6964776 - 4" {

        $UserModulesPath = Join-path $env:userprofile '\Documents\WindowsPowershell\Modules\testModule'
        new-item -type directory $UserModulesPath -ErrorAction Ignore
        remove-item $UserModulesPath\testModule.ni.dll -Force -ErrorAction Ignore
        remove-item $UserModulesPath\testModule.dll -Force -ErrorAction Ignore
        GenerateBinaryModule -ModulePath $UserModulesPath\testModule.ni.dll
        GenerateBinaryModule1 -ModulePath $UserModulesPath\testModule.dll

        try 
        {
            $module = Get-Module -ListAvailable $UserModulesPath
            $module.Count | should be 1
            $command = get-command get-testmodule
            $command.Count | should be 1
        }
        catch
        {
            throw $_
        }
        finally
        {
            Remove-Module testModule -force -Erroraction SilentlyContinue
            Remove-Item $UserModulesPath -recurse -force -ErrorAction SilentlyContinue
        }

    }

        <#
    Purpose:
        Verify TFS bug: 7305422 Relative path in module manifest is checked as relative to $pwd, verify the reqiuredAssembly field
                
    Action:
        go to a drive other than c:
        import-module c:\temp\testModule\testModule.psd1, which contains relative requiredAssembly path 

               
    Expected Result: 
         the assembly locates on c: should be imported.

    #>
    It -pending:($IsCore) "Bug 7305422 - 1" {

        $UserModulesPath = Join-path $env:userprofile '\Documents\WindowsPowershell\Modules\TestModuleRelativePath'
        new-item -type directory $UserModulesPath\assembly -ErrorAction Ignore
        remove-item $UserModulesPath\assembly\TestModuleRelativePath.dll -Force -ErrorAction Ignore
        GenerateBinaryModule -ModulePath $UserModulesPath\assembly\TestModuleRelativePath.dll

        try 
        {
            New-ModuleManifest $UserModulesPath\TestModuleRelativePath.psd1 -RequiredAssemblies "assembly\TestModuleRelativePath.dll"
            pushd
            cd hkcu:\
            Import-Module $UserModulesPath -Force
            $command = get-command get-testmodule
            $command.Count | should be 1
        }
        catch
        {
            throw $_
        }
        finally
        {
            popd
            Remove-Module TestModuleRelativePath -force -Erroraction SilentlyContinue
        }

    }

    <#
    Purpose:
        Verify TFS bug: 7305422 Relative path in module manifest is checked as relative to $pwd, verify the nestedModule
                
    Action:
        go to a drive other than c:
        import-module c:\temp\testModule\testModule.psd1, which contains relative nestedModule path 

               
    Expected Result: 
         the nested module locates on c: should be imported.

    #>
    It -pending:($IsCore) "Bug 7305422 - 2" {

        $UserModulesPath = Join-path $env:userprofile '\Documents\WindowsPowershell\Modules\TestModuleRelativePath'

        try 
        {
            New-ModuleManifest $UserModulesPath\TestModuleRelativePath.psd1 -NestedModules "nestedModule\nestedModule.psm1"
            pushd
            cd hkcu:\
            Import-Module $UserModulesPath -Force
            $command = get-command nestedModuleFunction 
            $command.Count | should be 1
        }
        catch
        {
            throw $_
        }
        finally
        {
            popd
            Remove-Module TestModuleRelativePath -force -Erroraction SilentlyContinue
        }

    }

    <#
    Purpose:
        Verify TFS bug: 7305422 Relative path in module manifest is checked as relative to $pwd, verify the FormatsToProcess
                
    Action:
        go to a drive other than c:
        import-module c:\temp\testModule\testModule.psd1, which contains relative format.ps1xml path

               
    Expected Result: 
         the format.ps1xml locates on c: should be imported.

    #>
    It -pending:($IsCore) "Bug 7305422 - 3" {

        $UserModulesPath = Join-path $env:userprofile '\Documents\WindowsPowershell\Modules\TestModuleRelativePath'

        try 
        {
            New-ModuleManifest $UserModulesPath\TestModuleRelativePath.psd1 -FormatsToProcess "formatfile\formatfile.format.ps1xml"
            pushd
            cd hkcu:\
            Import-Module $UserModulesPath -Force
        }
        catch
        {
            throw $_
        }
        finally
        {
            popd
            Remove-Module TestModuleRelativePath -force -Erroraction SilentlyContinue
        }

    }

}

Describe "Test-ModuleManifest verification tests" -Tags "Feature" {

    BeforeAll {
        if ($IsCore) { return }
        new-item -type directory $UserModulesPath\ModuleManifestVerification -ErrorAction SilentlyContinue
    }

    AfterAll {
        if ( $IsCore ) { return }
        $UserTestModulesPath = Join-path $UserModulesPath $TestModulesFolder
        $UserTestModulesPath1 = Join-path $UserModulesPath $TestModulesFolder1
        $UserTestModulesPath2 = Join-path $UserModulesPath $TestModulesFolder2
        Remove-Item $UserTestModulesPath -Recurse -Force -ErrorAction Ignore
        Remove-Item $UserTestModulesPath1 -Recurse -Force -ErrorAction Ignore
        Remove-Item $UserTestModulesPath2 -Recurse -Force -ErrorAction Ignore
        Remove-Item $UserModulesPath\ModuleManifestVerification -Force -ErrorAction SilentlyContinue
    }

    It -pending:($IsCore) "NestedModuleVerification" {
        $manifestPath = Join-Path $UserModulesPath '\ModuleManifestVerification\ModuleManifestVerification.psd1'
        try
        {
            New-ModuleManifest -Path $manifestPath -NestedModules ".\invalidnestedmodule.psd1"
            Test-ModuleManifest $manifestPath -ErrorAction stop
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be "Modules_InvalidNestedModuleinModuleManifest,Microsoft.PowerShell.Commands.TestModuleManifestCommand"
            $error[0].Exception.tostring().contains("invalidnestedmodule.psd1") | should be true
        }
        finally
        {
            Remove-Item $manifestPath -Force -ErrorAction Ignore
        }
    }

    It -pending:($IsCore) "RequiredAssembliesVerification" {
        $manifestPath = Join-Path $UserModulesPath '\ModuleManifestVerification\ModuleManifestVerification.psd1'
        try
        {
            New-ModuleManifest -Path $manifestPath -RequiredAssemblies ".\invalidassembly.dll"
            Test-ModuleManifest $manifestPath -ErrorAction Stop
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be "Modules_InvalidRequiredAssembliesInModuleManifest,Microsoft.PowerShell.Commands.TestModuleManifestCommand"
            $error[0].Exception.tostring().contains("invalidassembly.dll") | should be true
        }
        finally
        {
            Remove-Item $manifestPath -Force -ErrorAction Ignore
        }
    }

    It -pending:($IsCore) "GacRequiredAssembliesVerification" {
        $manifestPath = Join-Path $UserModulesPath '\ModuleManifestVerification\ModuleManifestVerification.psd1'
        try
        {
            New-ModuleManifest -Path $manifestPath -RequiredAssemblies "system.management.automation"
            Test-ModuleManifest $manifestPath -ErrorAction stop
        }
        catch
        {
            throw "Unexpected Exception is thrown" + $_.FullyQualifiedErrorId
        }
        finally
        {
            Remove-Item $manifestPath -Force -ErrorAction Ignore
        }
    }

    It -pending:($IsCore) "RequiredModuleVerification" {
        $manifestPath = Join-Path $UserModulesPath '\ModuleManifestVerification\ModuleManifestVerification.psd1'
        try
        {
            New-ModuleManifest -Path $manifestPath -RequiredModules ".\invalidrequiredmodule.psd1"
            Test-ModuleManifest $manifestPath -ErrorAction stop
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be "Modules_InvalidRequiredModulesinModuleManifest,Microsoft.PowerShell.Commands.TestModuleManifestCommand"
            $error[0].Exception.tostring().contains("invalidrequiredmodule.psd1") | should be true
        }
        finally
        {
            Remove-Item $manifestPath -Force -ErrorAction Ignore
        }
    }

    It -pending:($IsCore) "ModuleListVerification" {
        $manifestPath = Join-Path $UserModulesPath '\ModuleManifestVerification\ModuleManifestVerification.psd1'
        try
        {
            New-ModuleManifest -Path $manifestPath -ModuleList ".\invalidmodulelist.psd1"
            Test-ModuleManifest $manifestPath -ErrorAction stop
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be "Modules_InvalidModuleListinModuleManifest,Microsoft.PowerShell.Commands.TestModuleManifestCommand"
            $error[0].Exception.tostring().contains("invalidmodulelist.psd1") | should be true
        }
        finally
        {
            Remove-Item $manifestPath -Force -ErrorAction Ignore
        }
    }

    It -pending:($IsCore) "FileListVerification" {
        $manifestPath = Join-Path $UserModulesPath '\ModuleManifestVerification\ModuleManifestVerification.psd1'
        try
        {
            New-ModuleManifest -Path $manifestPath -FileList ".\invalidfilelist.txt"
            Test-ModuleManifest $manifestPath -ErrorAction stop
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be "Modules_InvalidFilePathinModuleManifest,Microsoft.PowerShell.Commands.TestModuleManifestCommand"
            $error[0].Exception.tostring().contains("invalidfilelist.txt") | should be true
        }
        finally
        {
            Remove-Item $manifestPath -Force -ErrorAction Ignore
        }
    }

    It -pending:($IsCore) "FileOUtScopeModuleFolderVerification" {
        $manifestPath = Join-Path $UserModulesPath '\ModuleManifestVerification\ModuleManifestVerification.psd1'
        new-item -type directory $UserModulesPath\outScopeFile.txt -ErrorAction SilentlyContinue
        try
        {
            New-ModuleManifest -Path $manifestPath -FileList "..\outScopeFile.txt"
            Test-ModuleManifest $manifestPath -ErrorAction stop
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be "Modules_InvalidFilePathinModuleManifest,Microsoft.PowerShell.Commands.TestModuleManifestCommand"
            $error[0].Exception.tostring().contains("outScopeFile.txt") | should be true
        }
        finally
        {
            Remove-Item $manifestPath -Force -ErrorAction Ignore
        }
    }
}



Describe "Win8TestFollowupForBugs" -Tags "Feature" {

    It "bug284599-GetModuleFormat" {
        # Do a Get-Module after Get-Module -List
        Get-Module -ListAvailable | Out-Null
        $modules = Get-Module

        foreach ($m in $modules)
        {
            $m.PSTypeNames.Contains("ModuleInfoGrouping") | should be $false
        }
    }
}

Describe "Command analysis parsing tests" -Tags "Feature" {

    BeforeAll {
        $originalPSModulePath = $env:PSModulePath
        $env:PSModulePath = $TestDrive
        $powershell = (Get-Process -id $PID).MainModule.Filename
    }

    BeforeEach {
        if ( $IsWindows ) {
            # If the bug is present, it will be most reliable in a new-process with no analysis cache
            Remove-Item -ea Ignore $env:USERPROFILE\AppData\Local\Microsoft\Windows\PowerShell\ModuleAnalysisCache
        }
        $newModuleName = "mod_$(Get-Random)"
        New-Item -Type Directory TestDrive:\$newModuleName
    }

    AfterEach {
        Remove-Item -ea Ignore -re -force TestDrive:$newModuleName
    }

    AfterAll {
        $env:PSModulePath = $originalPSModulePath
    }

    It "module w/ parse errors doesn't cause problems" {
        'function foo' > TestDrive:\$newModuleName\$newModuleName.psm1
        & $powershell -nop -command "Get-Command *zzzzzzzzzzzzzzzzzzzzzzzzzzzzz*" | Should BeNullOrEmpty
        $LASTEXITCODE | Should Be 0

        if ( $IsWindows ) {
            & $powershell -nop -command "Get-Help *zzzzzzzzzzzzzzzzzzzzzzzzzzzzz*" | Should BeNullOrEmpty
            $LASTEXITCODE | Should Be 0
        }
    }

    It "module w/ invalid manifest doesn't cause problems" {
        '' > TestDrive:\$newModuleName\$newModuleName.psd1
        & $powershell -nop -command "Get-Command *zzzzzzzzzzzzzzzzzzzzzzzzzzzzz*" | Should BeNullOrEmpty
        $LASTEXITCODE | Should Be 0

        if ( $IsWindows ) {
            & $powershell -nop -command "Get-Help *zzzzzzzzzzzzzzzzzzzzzzzzzzzzz*" | Should BeNullOrEmpty
            $LASTEXITCODE | Should Be 0
        }
    }

    It "module w/ funny quotes doesn't cause problems" {
        # Create a file using the default encoding.  This should create a file with
        # no bom, and if it does, it will make sure we read the script with Default encoding correctly.

        $fs = [System.IO.File]::Create("$TestDrive\$newModuleName\$newModuleName.psm1")
        $script = "function Invoke-Hello { $([char]0x201d)hello$([char]0x201c) }"
        if ( $IsWindows ) {
            $bytes = [System.Text.Encoding]::Default.GetBytes($script)
        }
        else {
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($script)
        }
        $fs.Write($bytes, 0, $bytes.Count)
        $fs.Close()
        $fs.Dispose()

        & $powershell -nop -command "Get-Command *zzzzzzzzzzzzzzzzzzzzzzzzzzzzz*" | Should BeNullOrEmpty
        $LASTEXITCODE | Should Be 0


        if ( $IsWindows ) {
            & $powershell -nop -command "Get-Help *zzzzzzzzzzzzzzzzzzzzzzzzzzzzz*" | Should BeNullOrEmpty
            $LASTEXITCODE | Should Be 0
        }
    }

    It "module manifest w/ funny quotes doesn't cause problems" {
        # Create a file using the default encoding.  This should create a file with
        # no bom, and if it does, it will make sure we read the script with Default encoding correctly.

        $fs = [System.IO.File]::Create("$TestDrive\$newModuleName\$newModuleName.psm1")
        $script = "@{ RootModule = $([char]0x201d)Foo$([char]0x201c ) }"
        if ( $IsWindows ) {
            $bytes = [System.Text.Encoding]::Default.GetBytes($script)
        }
        else {
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($script)
        }
        $fs.Write($bytes, 0, $bytes.Count)
        $fs.Close()
        $fs.Dispose()

        & $powershell -nop -command "Get-Command *zzzzzzzzzzzzzzzzzzzzzzzzzzzzz*" | Should BeNullOrEmpty
        $LASTEXITCODE | Should Be 0

        if ( $IsWindows ) {
        & $powershell -nop -command "Get-Help *zzzzzzzzzzzzzzzzzzzzzzzzzzzzz*" | Should BeNullOrEmpty
        $LASTEXITCODE | Should Be 0
        }
    }
}


Describe '$PSEdition is allowed in module manifest' -Tags "CI" {
    
    BeforeAll {
        
        $PSEditionTestModule = Setup -f PSEditionTestModule -Pass
        $TestModuleFile = Setup -f module.psm1 -Content 'function Get-PSEdition { "Get PSEdition" }' -Pass
        $TestManifestFile = Setup -f PSEditionTest.psd1 -Pass -Content @'
@{
RootModule = 'module.psm1'
ModuleVersion = '1.0.0.0'
FunctionsToExport = if ($PSEdition -eq 'Desktop' -or $PSEdition -eq "Linux" ) { @('Get-PSEdition') } else { @('Get-Bar', 'Get-Foo') }
}
'@


        $ManifestWithInvalidVariable = Setup -f "WithInvalidVars.psd1" -Pass -Content @'
@{
RootModule = 'module.psm1'
ModuleVersion = '1.0.0.0'
FunctionsToExport = if ($PID -eq 1234) { @('Get-PSEdition') } else { @('Get-Bar', 'Get-Foo') }
}
'@
    }

    It 'Test $PSEdition can be used in .psd1 file' {
        Import-Module -Name $TestManifestFile
        
        $ExposedFun = Get-Command -Module "PSEditionTest"
        $ExposedFun.Name | Should Be "Get-PSEdition"

        Get-PSEdition | Should Be "Get PSEdition"
    }

    It 'Test error message contains the correct list of allowed variables - from psd1 file' {
        Import-Module -Name $ManifestWithInvalidVariable -ErrorVariable ErrVar -ErrorAction SilentlyContinue
        
        $ExpectedVarList = @('$PSCulture', '$PSUICulture', '$true', '$false', '$null', '$PSScriptRoot', '$PSEdition')
        $ErrMsg = $ErrVar[0].Exception.Message

        foreach ($VarName in $ExpectedVarList)
        {
            $ErrMsg.Contains($VarName) | Should Be $true
        }
    }

    It "Test error message contains the correct list of allowed variables - from restricted runspace" {
        
        try {
            $iis = [initialsessionstate]::CreateDefault2()
            $rs = [runspacefactory]::CreateRunspace($iis)
            $rs.Open()
            $rs.LanguageMode = "RestrictedLanguage"

            $ps = [powershell]::Create()
            $ps.Runspace = $rs

            $ErrVar = $null
            $ExpectedVarList = @('$PSCulture', '$PSUICulture', '$true', '$false', '$null')
            
            ##################################################################
            ##
            ## Reference non-allowed variables in restricted language mode
            ##
            ##################################################################
            $ps.AddScript('$PID') > $null
            try { $ps.Invoke() } catch { $ErrVar = $_ }

            $ErrVar | Should Not BeNullOrEmpty
            $ErrMsg = $ErrVar.Exception.Message

            foreach ($VarName in $ExpectedVarList)
            {
                $ErrMsg.Contains($VarName) | Should Be $true
            }

            ##################################################################
            ##
            ## Use 'OutVariable' parameter in restricted language mode
            ##
            ##################################################################
            $ps.Commands.Clear()
            $ps.Streams.Error.Clear()
            $ps.AddScript("Write-Output def -OutVariable abc") > $null
            $ps.Invoke()

            $ps.Streams.Error[0] | Should Not BeNullOrEmpty
            $ErrMsg = $ps.Streams.Error[0].Exception.Message

            foreach ($VarName in $ExpectedVarList)
            {
                $ErrMsg.Contains($VarName) | Should Be $true
            }
            
        } finally {
            $rs.Close()
            $ps.Dispose()
        }
    }
}
