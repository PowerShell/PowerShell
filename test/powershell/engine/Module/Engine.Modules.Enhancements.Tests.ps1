# This is a Pester test suite to validate the Enhancements feature of Module
#
# Copyright (c) Microsoft Corporation, 2015
#
#


function TestCaseSetup()
{
    # Clear error array
    $error.Clear()
 
    #If $TestPath is not existed, will create a new one
    if(!(Test-Path $TestPath))
    {    
        New-Item -Force -Path $TestPath -Type Directory
    
        # Generate BinaryModules
        GenerateTestBinaryModule1
        GenerateTestBinaryModule2
    }    
}

function TestCaseCleanup()
{
    # Clear error array
    $error.Clear()
    
    if(Test-Path $TestPath)
    {
        if(Get-Module $testModule1)
        {                      
            Remove-Module -Force $testModule1         
        } 
        if(Get-Module $testModule2)
        {
            Remove-Module -Force $testModule2         
        }
    }
}

# Create PS Files
#
function CreatePSFile($PSFileContent, $FilePath, $PFFileName)
{
    $ModuleFilePath = Join-Path $FilePath $PFFileName
    $PSFileContent | Out-File -Force -FilePath $ModuleFilePath
}

# Import Binary Module and run cmdlets
#
function GenerateBinaryModule($ModuleContent, $ModulePath)
{
    $AddTypeCommand = Add-Type -TypeDefinition $ModuleContent -OutputAssembly $ModulePath

    $runSpace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunSpace($host)
    $powerShell = [System.Management.Automation.PowerShell]::Create()
    $powerShell.RunSpace = $runSpace
    $powerShell.Runspace.Open()

    $pipeLine = $powerShell.Runspace.CreatePipeline($AddTypeCommand)    
    $pipeLine.Invoke()
}


# Generate Test Binary Module
#
function GenerateTestBinaryModule1()
{    
    $BinaryModule = @'
namespace TestBinaryModule1
{
    using System;
    using System.Management.Automation;

    [Cmdlet(VerbsCommon.Get, "TestModule")]
    public class TestSameCmdlets : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            WriteObject("module1");
        }
    }

    [Cmdlet(VerbsCommon.Get, "MachineName")]
    public class TestDiffCmdlets : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            WriteObject("Machine name is " + System.Environment.MachineName);
        }
    }
}
'@
    Add-Type -TypeDefinition $BinaryModule -OutputAssembly $TestBinaryModulePath1    
}

function GenerateTestBinaryModule2()
{    
    $BinaryModule = @'
namespace TestBinaryModule2
{
    using System;
    using System.Management.Automation;
    [Cmdlet(VerbsCommon.Get, "TestModule")]
    public class TestSameCmdlets : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            WriteObject("module2");
        }
    }

    [Cmdlet(VerbsCommon.Get, "OSVersion")]
    public class TestDiffCmdlets : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            WriteObject("Machine name is " + System.Environment.OSVersion);
        }
    }
}

'@
    Add-Type -TypeDefinition $BinaryModule -OutputAssembly $TestBinaryModulePath2    
}
#>

# Create Script Module
#
function createTempScriptModule($ModuleStorePath)
{
    # Create template Script Module 1
    $ScriptModuleContent1 = @'
    function Get-TestModule
    {
        Process
        {
            Write-output 'module1'
        }
    }
'@      
    
    # Create template Script Module 2
    $ScriptModuleContent2 = @'
    function Get-TestModule
    {
        Process
        {
            Write-output 'module2'
        }
    }
'@   

    CreatePSFile -PSFileContent $ScriptModuleContent1 -FilePath $TestPath -PFFileName $TempScriptModuleFile1   
    CreatePSFile -PSFileContent $ScriptModuleContent2 -FilePath $TestPath -PFFileName $TempScriptModuleFile2    
}

# Create Module Manifests
#
function CreateModuleManifests($moduleName)
{
    $ModuleManifestPath = Join-Path $TestPath ($moduleName + ".psd1")
    $BinaryModulePath = Join-Path $TestPath ($moduleName + ".dll")    

    # Create module manifest
    New-ModuleManifest -Path $ModuleManifestPath -TypesToProcess @() -NestedModules @() -FormatsToPRocess @() -RequiredAssemblies @() `
                       -FileList @() -Author 'v-chongd' -CompanyName 'Microsoft' -Copyright "0" -Description "Test Module" -ModuleToProcess $BinaryModulePath
}

# Load two module file without any parameter
#
function LoadModuleWithoutParameter($testModuleFile1, $testModuleFile2)
{
    Import-Module (Join-Path $TestPath $testModuleFile1)

    (Get-TestModule) | should be 'module1'
       
    Import-Module (Join-Path $TestPath $testModuleFile2)    
}

# Load two module files with NoClobber parameter
#
function LoadModuleNoClobber($testModuleFile1, $testModuleFile2)
{       
    Import-Module (Join-Path $TestPath $testModuleFile1)
        
    (Get-TestModule) | should be 'module1'
        
    Import-Module (Join-Path $TestPath $testModuleFile2) -NoClobber 
}
#Compare powershell eventlog given expect log message
#
function CompareEventLog($expectLog, $actualLog)
{
   $isSuccess = $false
   foreach($r in $actualLog)
   {
     if ($r.Message.Contains($expectLog))
     {
       $isSuccess = $True
       break
     }
   } 
   $isSuccess | should be $true
}

#Create module manifest using New-ModuleManifest with RootModule parameter
#
function GenerateModuleManifest($moduleManifestPath, $rootModulePath)
{
   #Create module manifest file with RootModule
   New-ModuleManifest -Path $moduleManifestPath -TypesToProcess @() -NestedModules @() -FormatsToPRocess @() -RequiredAssemblies @() `
                      -FileList @() -Author 'Tester' -CompanyName 'Microsoft' -Copyright '1.0' -Description "Test" `
                      -RootModule $rootModulePath
}

#Create module manifest file with ModuleToProcess key
#
function CreateModuleManifestWithModuleProcess($moduleManifestPath, $rootModulePath)
{
   GenerateModuleManifest -moduleManifestPath $moduleManifestPath -rootModulePath $rootModulePath
   #Replace RootModule with ModuleToProcess 
   $originContent = Get-Content $moduleManifestPath
   $replacedContent = $origincontent -replace "RootModule =", "ModuleToProcess ="
   Set-Content -Path $moduleManifestPath -Value $replacedContent
}


#Create module manifest file with both RootModule and ModuleToProcess keys
#
function CreateModuleManifestWithModuleProcessRootModule($moduleManifestPath)
{
  $moduleManifestContent = "
     @{
        ModuleToProcess = 'ImportModuleWithModuleProcessRootModule.psm1'
        ModuleVersion = '1.0'
        Copyright = '1.0'
        Description = 'Test'
        PowerShellVersion = ''
        PowerShellHostName = ''
        PowerShellHostVersion = ''
        DotNetFrameworkVersion = ''
        CLRVersion = ''
        ProcessorArchitecture = ''
        RequiredModules = @()
        RequiredAssemblies = @()
        ScriptsToProcess = @()
        TypesToProcess = @()
        FormatsToProcess = @()
        NestedModules = @()
        FunctionsToExport = '*'
        CmdletsToExport = '*'
        VariablesToExport = '*'
        AliasesToExport = '*'
        ModuleList = @()
        FileList = @()
        HelpInfoURI = ''
        RootModule = 'ImportModuleWithModuleProcessRootModule.psm1'
       }"
   Out-File -FilePath $moduleManifestPath -InputObject $moduleManifestContent
}


# Create Script Module with parameter
#
function GenerateScriptModule($ScriptModulePath)
{
    # Create Script Module
    $ScriptModuleContent = @'
    function Get-ScriptModule
    {
        param($a = "Nothing")
        
        Process
        {
            Write-output $a
        }
    }
    export-modulemember -function Get-ScriptModule
'@
  #Create script module file
  $ScriptModuleContent | Out-File -Force -FilePath $ScriptModulePath
}

#Generate Binary Module with parameter
#
function GenerateBinaryModule($ModulePath)
{    
  $BinaryModule = @"
using System;
using System.Management.Automation;
namespace TestBinaryModule
{
    [Cmdlet("Set","BinaryModule")]
    public class SetModuleCommand : PSCmdlet
    {
        [Parameter]
        public int a { 
            get;
            set;
        }
        protected override void ProcessRecord()
        {
            String s = "Value is :" + a;
            WriteObject(s);
        }
    }
}
"@
    Add-Type -TypeDefinition $BinaryModule -OutputAssembly $ModulePath    
}


# Generate a customized PSSnapin
#
function GenerateCustomPSSnapin($SnapinPath)
{   
    $assembly = "System.Configuration.Install, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
    $snapin = @'
namespace MySnapin
{
    using System;
    using System.Management.Automation;
    using System.ComponentModel;

    [RunInstaller(true)]
    public class MySnapin1 : PSSnapIn
    {
      public override string Name
      {
        get { return "My.Powershell.Test1"; }
      }

      public override string Vendor
      {
        get { return "Tester"; }
      }

      public override string Description
      {
        get { return " This is a sample PowerShell custom snap-in"; }
      }
    }

    [RunInstaller(true)]
    public class MySnapin3 : PSSnapIn
    {
      public override string Name
      {
        get { return "My.Powershell.Test3"; }
      }

      public override string Vendor
      {
        get { return "Tester"; }
      }

      public override string Description
      {
        get { return " This is a sample PowerShell custom snap-in"; }
      }
    }


    [Cmdlet("set", "hello")]
    public class SetHelloCmdlet : Cmdlet
    {
        protected override void ProcessRecord()
        {
            WriteObject("Hello!");
        }
    }
}
'@
    Add-Type -ReferencedAssemblies $assembly -TypeDefinition $snapin -OutputAssembly $SnapinPath 
}

# get path for InstallUtil.exe
#
function GetInstallUtilPath
{
    if($env:PROCESSOR_ARCHITECTURE -eq "amd64") {
        return "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\installUtil.exe"
    } else {
        return "$env:SystemRoot\Microsoft.NET\Framework\v4.0.30319\installUtil.exe"
    }
}

# InstallUtil PSSnapin
#
function InstallUtilPSSnapin($SnapinPath)
{
  $installUtilPath = GetInstallUtilPath
  & $installUtilPath $SnapinPath
}

Describe "Engine Modules Enhancements Test" -Tags "CI" {


BeforeAll{

    if ( $IsWindows ) {
        # Define Variables which used in test cases
        #
        $TestPath = Join-Path $env:Temp ([System.Guid]::NewGuid().ToString())
        $testModule1 = 'testModule1'
        $testModule2 = 'testModule2'
        $TempScriptModuleFile1 = ($testModule1 + ".psm1")
        $TempScriptModuleFile2 = ($testModule2 + ".psm1")
        $TestBinaryModulePath1 = Join-Path $TestPath ($testModule1 + ".dll")
        $TestBinaryModulePath2 = Join-Path $TestPath ($testModule2 + ".dll")

        TestCaseSetup
    }
}

AfterAll{
    if ( $IsWindows ) {
        TestCaseCleanup
    }
}

# Verify Get-Command <command name> -all will return all module list if different modules have same commands. 
#
It -pending:(!$IsWindows) VerifyGetCommandWithAllParameter {
   
    $ExpectedModuleArray = ($testModule1, $testModule2)
    $isSuccess = $true
    $unExistItem = $null
    
    $TestCommand = "Get-TestModule"    
      
    $job = Start-Job {
    $binaryModule1 = $args[0]
    $binaryModule2 = $args[1]
    $testCommand = $args[2]
    
    import-Module $binaryModule1
    import-Module $binaryModule2
    Get-Command -all $args[2]

} -Argumentlist $TestBinaryModulePath1, $TestBinaryModulePath2, $TestCommand
        
    Wait-Job $job

    $testObjects = Receive-Job $job    
    
	$testObjects.Count | should be $ExpectedModuleArray.Count
    foreach ($testObject in $testObjects)
    {
        if(!$ExpectedModuleArray.Contains($testObject.Module))
        {
            $unExistItem = $testObject.Module
            $isSuccess = $false
            break
        }
    }
    $isSuccess | should be $true
}

# Verify Import Script Module with -NoClobber parameter
#
It -pending:(!$IsWindows) VerfiyNoClobberParameterWithScriptModule {

    createTempScriptModule($TestPath)

    LoadModuleNoClobber -testModuleFile1 $TempScriptModuleFile1 -testModuleFile2 $TempScriptModuleFile2
    
    $testObject = Get-Command get-TestModule -all
    
    $expect = $testModule1

    $testObject.Module.Name | should be $expect
}

#Verify when the logger sees that the value passed to a parameter is an array, 
#it needs to unfold the array and provide the actual values for each item in the array
#
It -pending:(!$IsWindows) LoggerUnfoldArray {

  $testScript = @"
   `$CSVfilePath = Join-Path `$env:temp "LoggerUnfoldArray.csv"
   `$notePad = "NotePad"
   `$wusa = "Wusa"
   
   #Create CSV file with the couple of processes
   Set-Content -path `$CSVfilePath -value `$notePad, `$wusa

   #Stop all of the two processes
   Start-Process `$notepad
   Start-Process `$wusa
   Import-CSV `$CSVfilePath -Header name | Stop-process

   #Enable logging for Stop-Process 
   (Get-Module Microsoft.PowerShell.Management).LogPipelineExecutionDetails = `$true

   #Start the two processes
   Start-Process `$notepad
   Start-Process `$wusa

   #Clear powershell event log 
   Clear-EventLog "Windows Powershell"

   #Stop processes
   Import-CSV `$CSVfilePath -Header name | Stop-process
   `$actual = Get-EventLog -LogName "Windows Powershell"
   `$actual
"@

   #Get powershell event logs
   $rm = New-Object resources.resourcemanager PipelineStrings, ([psobject].Assembly)
   $expectStr = $rm.GetString("PipelineExecutionParameterBinding", (Get-UICulture))
   $expect1 = $expectStr -f "Stop-Process", "Name", "NotePad"
   $expect2 = $expectStr -f "Stop-Process", "Name", "Wusa"
   $expect = $expect1 + "`r`n" + $expect2

  $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
  $ps = [System.Management.Automation.PowerShell]::Create($iss)
  $ps.Streams.Error.Clear()
  $result = $ps.AddScript($testScript).Invoke()
   CompareEventLog -expectLog $expect -actualLog $result
}


#Verify error is displayed with two parameters ?RootModule? and ?ModuleToProcess?
#
It -pending:(!$IsWindows) NewModuleManifestWithModuleProcessRootModule {

  $error.Clear()

  $moduleManifestPath = Join-Path $env:Temp "NewModuleManifestWithModuleProcessRootModule.psd1"
  $rootModulePath = Join-Path $env:Temp "testRootModule.dll"

  #New-ModuleManifest with parameter RootModule and ModuleToProcess
  try 
  { New-ModuleManifest -Path $moduleManifestPath -TypesToProcess @() -NestedModules @() -FormatsToPRocess @() -RequiredAssemblies @() `
                      -FileList @() -Author 'Tester' -CompanyName 'Microsoft' -Copyright '1.0' -Description 'Test'`
                      -RootModule $rootModulePath -ModuleToProcess $rootModulePath
  }
  catch
  {
    $_.FullyQualifiedErrorId |
    should be "ParameterAlreadyBound,Microsoft.PowerShell.Commands.NewModuleManifestCommand"
  }
}



#Verify error message throws if importing the module manifest with both ModuleToProcess and RootModule fields.
#
It -pending:(!$IsWindows) ImportModuleWithModuleProcessRootModule {

  $error.Clear()
  
  $ModuleFileName = "ImportModuleWithModuleProcessRootModule"
  $moduleManifestPath = Join-Path $env:Temp($ModuleFileName + ".psd1")

  CreateModuleManifestWithModuleProcessRootModule($moduleManifestPath)

  try
  {
    Import-Module $moduleManifestPath -ErrorAction stop
  }
  catch
  {
      $expectErrorId = 'Modules_ModuleManifestCannotContainBothModuleToProcessAndRootModule,Microsoft.PowerShell.Commands.ImportModuleCommand'
      $_.FullyQualifiedErrorId | should be $expectErrorId
  }
  
}

#Verify the module manifest with ModuleToProcess field can be imported normally.
#
It -pending:(!$IsWindows) ImportModuleWithModuleToProcess {
  $error.Clear()
  try
  {
   $ModuleFileName = "ImportModuleWithModuleToProcess"
   $rootModulePath = Join-Path $env:Temp($ModuleFileName + ".psm1")
   $moduleManifestPath = Join-Path $env:Temp($ModuleFileName + ".psd1")

   #Create a script module as RootModule
   GenerateScriptModule -ScriptModulePath $rootModulePath

   #Create module manifest file with ModuleToProcess
   CreateModuleManifestWithModuleProcess -moduleManifestPath $moduleManifestPath -rootModulePath $rootModulePath

   Import-Module $moduleManifestPath -ErrorAction stop
  }
  catch
  {
    Throw "Module enhancement expect no error displayed. See failure information: $error"
  }
}




#Verify Test-ModuleManifest cmdlet throws error if the module manifest has both ModuleToProcess and RootModule fields defined.
#
It -pending:(!$IsWindows) TestModuleManifestWithModuleProcessRootModule {
  $error.Clear()

  $ModuleFileName = "ModuleManifestWithModuleProcessRootModule"
  $moduleManifestPath = Join-Path $env:Temp($ModuleFileName + ".psd1")

  #Create module manifest file with RootModule and ModuleToProcess
  CreateModuleManifestWithModuleProcessRootModule($moduleManifestPath)
     
  try
  {
    Test-ModuleManifest $moduleManifestPath
  }
  catch
  {
    $expectErrorId = 'Modules_ModuleManifestCannotContainBothModuleToProcessAndRootModule,Microsoft.PowerShell.Commands.TestModuleManifestCommand'
    $_.FullyQualifiedErrorId | should be $expectErrorId
  }
 }



#Create module manifest using New-ModuleManifest with RootModule, DefaultCommandPrefix parameter
#
function GenerateModuleManifestWithPrefix($moduleManifestPath, $rootModulePath, $defaultPrefix)
{
   #Create module manifest file with RootModule
   New-ModuleManifest -Path $moduleManifestPath -TypesToProcess @() -NestedModules @() -FormatsToPRocess @() -RequiredAssemblies @() `
                      -FileList @() -Author 'Tester' -CompanyName 'Microsoft' -Copyright '1.0' -Description "Test" `
                      -RootModule $rootModulePath -DefaultCommandPrefix $defaultPrefix
}

#Win8 122268
#Verify the DefaultCommandPrefix value is added as the default Prefix value for commands
#
It -pending:(!$IsWindows) ImportModuleWithDefaultCommandPrefixKey {
   $moduleName = 'ImportModuleWithDefaultCommandPrefixKey'
   $scriptModulePath = Join-Path $env:Temp($moduleName + '.psm1')
   $moduleManifestPath = Join-Path $env:Temp($moduleName + '.psd1')

   #Create script module as root module
   GenerateScriptModule -ScriptModulePath $scriptModulePath

   #Create module manifest with DefaultCommandPrefix
   GenerateModuleManifestWithPrefix -moduleManifestPath $moduleManifestPath -rootModulePath $scriptModulePath -defaultPrefix 'Test'
   
   #Import module using default prefix
   Import-Module $moduleManifestPath

   $actual = Get-Command -Module $moduleName
   $expect = 'Get-TestScriptModule'
   $actual.Name | should be $expect
}

#Execute Remove-PSSnapin for the core snapin 
#
function OperateCoreSnapin($CoreSnapin)
{
  $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
  $ps = [System.Management.Automation.PowerShell]::Create($iss)
  $ps.Streams.Error.Clear() 
  $ps.AddCommand("Remove-PSSnapin").AddArgument($CoreSnapin).Invoke()
  $result = $ps.Streams.Error[0]
  $rm = New-Object resources.resourcemanager ConsoleInfoErrorStrings, ([psobject].Assembly)
  $expectStr = $rm.GetString("CannotRemoveDefault", (Get-UICulture))
  $expect = $expectStr -f $CoreSnapin
  $expectId = "RemovePSSnapIn,Microsoft.PowerShell.Commands.RemovePSSnapinCommand"
  $result.FullyQualifiedErrorId | should be $expectId
  $result.exception.message | should be $expect
}

#Verify the core module Microsoft.Powershell.Core is constant module and cannot be removed.
#
It -pending:(!$IsWindows) ModuleEnhancementForOperateCoreModule {

   $SnapinName = 'Microsoft.PowerShell.Core'
   OperateCoreSnapin($SnapinName) 
}

  
#Execute Remove-Module, Get-Module, Import-Module and compare ExportedCommands for core modules.
#
function OperateCoreModule($CoreModule)
{
    $testScript =
    @"
    param(`$CoreModuleName = `$null)
    Import-Module `$CoreModuleName
    `$expectExportedCommands = (Get-Module `$CoreModuleName).ExportedCommands.Keys | Sort-Object
    [bool]`$isCoreModule = ((`$expectExportedCommands -ne `$null) -and (`$expectExportedCommands.Count -ne 0))

    Remove-Module `$CoreModuleName -Force
    [bool]`$isRemoved = ((Get-Module `$CoreModuleName) -eq `$null)

    Import-Module `$CoreModuleName

    `$actualExportedCommands = (Get-Module `$CoreModuleName).ExportedCommands.Keys | Sort-Object

    [bool]`$isSame = `$True
     
    for([int] `$i=0; `$i -lt `$expectExportedCommands.Count-1 ; `$i=`$i+1  )
    {
      if(`$expectExportedCommands[`$i] -ne `$actualExportedCommands[`$i])
      {
        `$isSame = `$False
        break
      }
    }

(`$isCoreModule -and `$isSame -and `$isRemoved)

"@

  $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
  $ps = [System.Management.Automation.PowerShell]::Create($iss)

  $result = $ps.AddScript($testScript).AddParameter("CoreModuleName",$CoreModule).Invoke()
  $result | should be $true
}



#Verify the core snapin Microsoft.WSMan.Management has been converted to modules and can be operated as module normally.
#
It -pending:(!$IsWindows) ModuleEnhancementForOperateWSManModule {

   $ModuleName = 'Microsoft.WSMan.Management'
   OperateCoreModule($ModuleName)
}



#Create module in specified path
function CreateScriptModuleInSpecifiedPath($modulePath, $moduleName)
{
  new-item -type directory $modulePath -Force | Out-Null
  $moduleFilePath = Join-Path $modulePath $moduleName
  GenerateScriptModule -ScriptModulePath $moduleFilePath
}




#Verify that if it finds a cdxml file in a given directory, 
#The nested modules are not visible using Get-Module ?ListAvailable unless people do Get-Module ?ListAvailable -All
#
It -pending:(!$IsWindows) GetModuleListRecurseForCdxmlFile {

   $moduleName = 'GetModuleListRecurseForCdxmlModule'
   $oldPSModulePath = $env:PSModulePath
   $tempdir = Join-Path $TestPath $moduleName
   $env:PSModulePath = $tempdir
   try
   {
     new-item -type directory $tempdir\TopModule\NestedModule1 -Force | Out-Null
     new-item -type directory $tempdir\TopModule\NestedModule2 -Force | Out-Null

     # Create top module
     CreateScriptModuleInSpecifiedPath -modulePath $tempdir\TopModule -moduleName TopModule.cdxml

     #Create nested modules
     CreateScriptModuleInSpecifiedPath -modulePath $tempdir\TopModule\NestedModule1 -moduleName NestedModule1.psm1
     CreateScriptModuleInSpecifiedPath -modulePath $tempdir\TopModule\NestedModule2 -moduleName NestedModule2.psm1

    $TopModule = 'TopModule'
    $Nested1 = 'NestedModule1'
    $Nested2 = 'NestedModule2'
    $actual = Get-Module -ListAvailable
    $actualAll = Get-Module -ListAvailable -All | Sort-Object
    $actual.Name | should be $TopModule
    $actualAll[0].Name | should be $Nested1
    $actualAll[1].Name | should be $Nested2 
    $actualAll[2].Name | should be $TopModule
   }
   finally
   {
       $env:PSModulePath = $oldPSModulePath
   }
}




#Win8 259048
#Verify the script with #Requires -Module can be executed
# Updating the module name since Mcirosoft.PowerShell.Core module is not available
It -pending:(!$IsWindows) InvokeScriptWithRequiresModule{

  $error.Clear()
  $ScriptFilePath = Join-Path $env:Temp 'TestScript.ps1'
  try
  {
    @"
#Requires -Module psworkflow
New-Variable -Name isExecuted -Value 100 -Scope Global
"@ | Set-Content $ScriptFilePath
   & $ScriptFilePath
   $isExecuted | should be 100 "Module enhancement expect the script file is executed, actually it is not executed."
  }
  catch
  {
    Throw "Module enhancement expect no error displayed. See failure information: $error"
  }
  finally
  {
    if (Test-Path $ScriptFilePath)
    {
      Remove-Item $ScriptFilePath -Force
    }
  }
}

#Verify the script with #Requires ?Module will attempt to load the specified modules before executing the script
#
It -pending:(!$IsWindows) InvokeScriptAttemptLoadRequiresModule {
  $error.Clear()
  try
  {
  $testScript=@" 
  `$ScriptFilePath = Join-Path `$env:Temp 'TestScript.ps1'
  try
  {
    "
#Requires -Module PSDiagnostics
New-Variable -Name isExecuted03 -Value 100 -Scope Global
" | Set-Content `$ScriptFilePath

     `& `$ScriptFilePath 
     [Bool]`$isLoaded = ((Get-Module PSDiagnostics).Name -eq 'PSDiagnostics')
     `$isLoaded
     `$isExecuted03
  }
  finally
  {
    if (Test-Path `$ScriptFilePath)
    {
      Remove-Item `$ScriptFilePath -Force
    }
  }
"@

  $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
  $ps = [System.Management.Automation.PowerShell]::Create($iss)

  $result = $ps.AddScript($testScript).Invoke()
  $result[0] | should be $true
  $result[1] | should be 100 

  }
  catch
  {
    Throw "Module enhancement expect no error displayed. See failure information: $error"
  }
}

#Verify if the required module cannot be loaded, an error is returned and the script will not be executed.
#
It -pending:(!$IsWindows) InvokeScriptNoRequiresModule {

  $error.Clear()
  try
  {
  $testScript=@" 

    `$ScriptFilePath = Join-Path `$env:Temp 'TestScript.ps1'
    "
#Requires -Module UnexistTestModule
New-Variable -Name isExecuted06 -Value 100 -Scope Global

" | Set-Content `$ScriptFilePath

     `& `$ScriptFilePath
     (`$isExecuted06 -eq `$null)
     `$error
"@

  $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
  $ps = [System.Management.Automation.PowerShell]::Create($iss)

  $result = $ps.AddScript($testScript).Invoke()
  $expectErrorId = 'ScriptRequiresMissingModules'
  $result[0] | should be $true
  $result[1].FullyQualifiedErrorId | should be $expectErrorId

  }
  catch
  {
    Throw "Module enhancement expect no error displayed. See failure information: $error"
  }
}




#Win8 122372
#Verify that the ModuleType of the imported module reflects the type of the module in ?RootModule? filed of the manifest
#
It -pending:(!$IsWindows) GetModuleListEnhancementForModuleType {
  $error.Clear()
  $ModuleName = 'PSDiagnostics'
  try
  {
    Import-Module $ModuleName
    $result1 = Get-Module $ModuleName
    Get-Module -ListAvailable -All $ModuleName
    $result2 = Get-Module $ModuleName
    $expectType = 'Script'
    $result1.ModuleType | should be $expectType 
    $result2.ModuleType | should be $expectType
  }
  catch
  {
    Throw "Module enhancement expect no error displayed. See failure information: $error"
  }
  finally
  {
    Remove-Module $ModuleName 
  }
}

#Generate Binary Module1
#
function GenerateBinaryModule11($ModulePath)
{    
  $BinaryModule = @"
using System;
using System.Management.Automation;
namespace TestBinaryModuleScope
{
    [Cmdlet("Get","TestModuleScope11")]
    public class GetTestModuleScopeCommand : PSCmdlet
    {
        protected override void ProcessRecord()
        {
           WriteObject("module1");
        }
    }
}
"@
    Add-Type -TypeDefinition $BinaryModule -OutputAssembly $ModulePath    
}

#Generate Binary Module2
#
function GenerateBinaryModule22($ModulePath)
{    
  $BinaryModule = @"
using System;
using System.Management.Automation;
namespace TestBinaryModuleScope
{
    [Cmdlet("Get","TestModuleScope22")]
    public class GetTestModuleScopeCommand : PSCmdlet
    {
        protected override void ProcessRecord()
        {
           WriteObject("module2");
        }
    }
}
"@
    Add-Type -TypeDefinition $BinaryModule -OutputAssembly $ModulePath    
}

#Win8 122329
#Verify the Get-Module -ListAvailable can retrieve all the information for PSModuleInfo object
#
It -pending:(!$IsWindows) GetPSModuleInfoPropertiesForNonImportedModule {

  $oldPSModulePath = $env:PSModulePath
  $tempdir = $TestPath
  $env:PSModulePath = $tempdir
  try
  {
   $message = "{0} should be retrieved for PSModuleInfo object"

   #Generate manifest module to verify the properties
   #
   $MyModulePath1 = Join-Path $tempdir 'GetPSModuleInfoPropertiesForNonImportedModule'
   $MyModulePath2 = Join-Path $tempdir 'GetPSModuleInfoPropertiesForNonImportedModule2'
   $MyModulePath3 = Join-Path $tempdir 'GetPSModuleInfoPropertiesForNonImportedModule3'
   new-item -type directory $MyModulePath1 -Force | Out-Null
   new-item -type directory $MyModulePath2 -Force | Out-Null
   new-item -type directory $MyModulePath3 -Force | Out-Null

 @"
@{
GUID="{3b6cc51d-c096-4b38-b78d-0fed6277096a}"
Author="Microsoft Corporation"
CompanyName="Microsoft Corporation"
Copyright="? Microsoft Corporation. All rights reserved."
Description = 'Test'
ModuleVersion="1.0.0.0"
PowerShellVersion="3.0"
CLRVersion="4.0"
NestedModules="GetPSModuleInfoPropertiesForNonImportedModule3.dll"
RequiredAssemblies= "Microsoft.PowerShell.Core.Activities.dll"
TypesToProcess="My.Types.ps1xml"
FormatsToProcess="My.Format.ps1xml"
HelpInfoURI = 'http://go.microsoft.com/fwlink/?LinkID=210603'
}

"@ | Set-Content $MyModulePath3\GetPSModuleInfoPropertiesForNonImportedModule3.psd1

  GenerateBinaryModule11 -ModulePath $MyModulePath3\GetPSModuleInfoPropertiesForNonImportedModule3.dll

@"
<?xml version="1.0" encoding="utf-8" ?>

<Configuration>  
  <ViewDefinitions>
    <View>
      <Name>ServiceWideView</Name>
      <ViewSelectedBy>
        <TypeName>System.ServiceProcess.ServiceController</TypeName>
      </ViewSelectedBy>
      <WideControl>
        <WideEntries>
          <WideEntry>
            <WideItem>
              <PropertyName>ServiceName</PropertyName>
            </WideItem>
          </WideEntry>
        </WideEntries>
      </WideControl>
    </View>
  </ViewDefinitions>
</Configuration>
"@ | Set-Content $MyModulePath3\My.Format.ps1xml

@"
<Type>
    <Name>System.ServiceProcess.ServiceController</Name>
       <Members>
         <MemberSet>
          <Name>PSStandardMembers</Name>
       <Members>
          <PropertySet>
             <Name>DefaultDisplayPropertySet</Name>
                <ReferencedProperties>
                        <Name>Status</Name>
                        <Name>Name</Name>
                        <Name>DisplayName</Name>
                 </ReferencedProperties>
           </PropertySet>
       </Members>
          </MemberSet>
       </Members>
</Type>
"@ | Set-Content $MyModulePath3\My.Types.ps1xml


  $result1 = Get-Module GetPSModuleInfoPropertiesForNonImportedModule3 -ListAvailable
  $result1.GUID | should not be $null 
  $result1.Author.Contains('Microsoft') | should be $true
  $result1.CompanyName  | should be 'Microsoft Corporation'
  $result1.Copyright.Contains('Microsoft') | should be $true
  $result1.Version.ToString() | should be '1.0.0.0'
  $result1.PowerShellVersion.ToString() | should be '3.0'
  $result1.CLRVersion.ToString() | should be '4.0'
  $result1.Name | should be 'GetPSModuleInfoPropertiesForNonImportedModule3'

  ($result1.ExportedCmdlets.Count -ne 0) | should be $true
  ($result1.NestedModules.Count -ne 0) | should be $true
  ($result1.ExportedTypeFiles.Count -ne 0) | should be $true
  ($result1.ExportedFormatFiles.Count -ne 0) | should be $true
  ($result1.RequiredAssemblies.Count -ne 0) | should be $true
  ($result1.HelpInfoUri.Contains('http')) | should be $true
  ($result1.Description.Contains('Test')) | should be $true
  $result1.AccessMode.ToString() | should be 'ReadWrite'

 
  @"
`$msg = 'hello world'
`$AnswerToTheUniverse = 42
 
function Send-Greeting() { Write-Host $msg }
function Add-Numbers([int] `$a,[int] `$b) { `$a + `$b }
 
Set-Alias Add Add-Numbers
Set-Alias Hi Send-Greeting 

Export-ModuleMember -Function Send-Greeting, Add-Numbers
Export-ModuleMember -Variable msg, AnswerToTheUniverse
Export-ModuleMember -Alias Hi, Add

"@ | Set-Content $MyModulePath1\GetPSModuleInfoPropertiesForNonImportedModule.psm1

 New-ModuleManifest -RootModule 'GetPSModuleInfoPropertiesForNonImportedModule.psm1'`
  -FunctionsToExport @('Send-Greeting','Add-Numbers') -VariablesToExport 'msg' -AliasesToExport 'Add'`
  -Path $MyModulePath1\GetPSModuleInfoPropertiesForNonImportedModule.psd1 -PrivateData 'MyData' 
  
 New-ModuleManifest -ScriptsToProcess 'TestScript.ps1' -ModuleList 'Microsoft.PowerShell.Core' `
  -RequiredModules 'PSDiagnostics' `
  -Path $MyModulePath2\GetPSModuleInfoPropertiesForNonImportedModule2.psd1
@"
Write-Host 'TestScript'
"@ | Set-Content $MyModulePath2\TestScript.ps1

  $result3 = Get-Module GetPSModuleInfoPropertiesForNonImportedModule -ListAvailable
  $result3.RootModule | should be 'GetPSModuleInfoPropertiesForNonImportedModule.psm1' 
  $result3.ExportedFunctions.Count | should be 2 
  $result3.ExportedVariables.Count | should be 1
  $result3.ExportedAliases.Count | should be 1
  $result3.PrivateData | should be 'MyData' 
  $result3.Path | should be (Join-Path $MyModulePath1 'GetPSModuleInfoPropertiesForNonImportedModule.psd1')
  ($result3.ExportedCommands.Count -ne 0) | should be $true

  $result4 = Get-Module GetPSModuleInfoPropertiesForNonImportedModule2 -ListAvailable
  $result4.Scripts | should be (Join-Path $MyModulePath2 'TestScript.ps1')
  $result4.ModuleList[0].Name | should be 'Microsoft.PowerShell.Core'

}
catch
{
   Throw "Module enhancement expect no error displayed. See failure information: $_"
}
finally
{
   $env:PSModulePath = $oldModulePath
}

}



#Win8 122365
#Compare property names for the PSModuleInfo
#
function ComparePSModuleInfoProperty($ModuleName, $ExpectPropertyName)
{
   $result = Get-Module $ModuleName | Get-Member $ExpectPropertyName
   $result.Name | should be $ExpectPropertyName
}



#Win8 146926
#Import two modules. One is in Global scope, another one is in Local scope
#
function ImportModuleWithScopeParameter($Module1, $Module2)
{
     #Import module with Global scope parameter
     Import-Module $Module1 -Scope Global
     
     #Verify the module has been imported
     (Get-TestModuleScope11)  | should be 'module1'
     
     #Import module with Local scope parameter
     Import-Module $Module2 -Scope Local
     
     #Verify the module has been imported
     (Get-TestModuleScope22) | should be 'module2'
}

#Import two modules. One is in Global scope, another one is in default scope
#
function ImportModuleWithDefaultScope($Module1, $Module2)
{
     #Import module with Global scope parameter
     Import-Module $Module1 -Scope Global
     
     #Verify the module has been imported
     (Get-TestModuleScope11) | should be 'module1'
     
     #Import module with default scope
     Import-Module $Module2
     
     #Verify the module has been imported
     (Get-TestModuleScope22) | should be 'module2'
}


#Verify Import-Module with Scope parameter can work properly for script module
#
It -pending:(!$IsWindows) ImportScriptModuleWithScopeParameter {
  $error.Clear()
  try
  {
   $scriptModule1 = Join-Path $env:Temp 'TestScriptModule1.psm1'
   $scriptModule2 = Join-Path $env:Temp 'TestScriptModule2.psm1'
   
   #Create two script modules
   "function Get-TestModuleScope11{ 'module1' }" > $scriptModule1
   "function Get-TestModuleScope22{ 'module2' }" > $scriptModule2
   
   ImportModuleWithScopeParameter -Module1 $scriptModule1 -Module2 $scriptModule2
   
   #Verify the script module imported in Local scope is unavailable in the global scope
   (Get-Command -Module TestScriptModule2) | should be $null
  }
  catch
  {
    Throw "Module enhancement expect no error displayed. See failure information: $error"
  }
  Finally
  {
    Get-Module -Name TestScriptModule* | Remove-Module
  }
}


#Verify an error is displayed if given both Scope and Global parameter 
#
It -pending:(!$IsWindows) ImportModuleWithBothScopeAndGlobalParameter {
  $error.Clear()
  try
  {
    Import-Module PSDiagnostics -Global -Scope Global -ErrorAction stop
  }
  catch
  {
    $_.FullyQualifiedErrorId |
    should be "Modules_GlobalAndScopeParameterCannotBeSpecifiedTogether,Microsoft.PowerShell.Commands.ImportModuleCommand"
  }
}


# Verify that Alias attribute is working properly for Binary cmdlets, Adv. functions and Workflows
#
It -pending:(!$IsWindows) AliasAttributeTestForCmdletAdvFnAndWorkflow {
    if ($env:PROCESSOR_ARCHITECTURE -eq "arm")
    {
        write-host "skipping this test on arm machine"
        return
    }

    $Error.Clear()
    # Define Variables which used in this test case
    #
    $TestPath = Join-Path $env:Temp ([System.Guid]::NewGuid().ToString())

    $testModule = 'TestModuleWithAliases'
    $TestScriptModuleFile = Join-Path $TestPath ($testModule + "Script.psm1")
    $TestModuleManifestFile = Join-Path $TestPath ($testModule + ".psd1")
    $TestBinaryModulePath = Join-Path $TestPath ($testModule + "Binary.dll")

    $aliasesToExport = @("cbm1", "ctm1","ctm2", "fbm1", "ftm2", "wbm1", "wbm2","wtm2")

    try
    {
        $BinaryModule = @'
            namespace TestBinaryModule
            {
                using System;
                using System.Management.Automation;
 
                [Cmdlet(VerbsCommon.Get, "BinaryTestCmdlet")]
                [Alias("ctm1", "cbm1","ctm2", "cbm2")]
                public class TestSameCmdlets : PSCmdlet
                {
                    protected override void ProcessRecord()
                    {
                        WriteObject("TestBinaryModule");
                    }
                }
            }
'@


        $null = New-Item -Force -Path $TestPath -Type Directory


        Add-Type -TypeDefinition $BinaryModule -OutputAssembly $TestBinaryModulePath
        

        # Create Script Module file
        @'
            function Get-TestModule
            {
                [CmdletBinding(DefaultParameterSetName = "Name")]
                [Alias("ftm1", "fbm1", "ftm2", "fbm2")]
                param(
                    [Parameter(ParameterSetName = "Name")]
                    [string]
                    $Name
                )

                Process
                {
                    Write-output 'Adv. Fn'
                }
            }

            workflow Test-WFAdvFnCmdlet
            {
                [Alias("wtm1", "wbm1", "wtm2", "wbm2")]
                param(
                    [Parameter(ParameterSetName = "Name")]
                    [string]
                    $Name
                )

                "Workflow"
            } 
'@ > $TestScriptModuleFile

        New-ModuleManifest -Path $TestModuleManifestFile -NestedModules $TestScriptModuleFile,$TestBinaryModulePath -AliasesToExport $aliasesToExport
        $null = Import-Module $TestModuleManifestFile -Force
        $gmoResult = Get-Module -name $testModule
        $gmoResult | should not be $null
        $aliasesNotFound = $aliasesToExport | ? {$_ -notin $gmoResult.ExportedAliases.Keys}

        $aliasesNotFound | should be $null

        (cbm1) | should be "TestBinaryModule" 
        (ctm1) | should be "TestBinaryModule" 
        (ctm2) | should be "TestBinaryModule"
        (fbm1) | should be "Adv. Fn"
        (ftm2) | should be "Adv. Fn" 
        (wbm1) | should be "Workflow"
        (wbm2) | should be "Workflow"
        (wtm2) | should be "Workflow"
        (gcm cbm2 -ErrorAction SilentlyContinue) | should be $null
        (gcm fbm2 -ErrorAction SilentlyContinue) | should be $null
        (gcm ftm1 -ErrorAction SilentlyContinue) | should be $null
        (gcm wtm1 -ErrorAction SilentlyContinue) | should be $null
    }
    finally
    {
        Remove-Module $testModule -Force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
        Remove-Item -Path $TestPath -Recurse -Force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
    }
}

}
