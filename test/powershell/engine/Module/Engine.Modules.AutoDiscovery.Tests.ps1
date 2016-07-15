# This is a Pester test suite to validate the AutoDiscovery feature of Module
#
# Copyright (c) Microsoft Corporation, 2015
#
#
if ( $IsCore ) {
    return
}

$UserModulesPath = Join-path $env:userprofile '\Documents\WindowsPowershell\Modules'

# Current execution path 
$CurrentDir = Split-Path $MyInvocation.MyCommand.Path
$TestModulesFolder= 'TestModulesForAutoLoading'
$TestModulesPath = Join-path $CurrentDir $TestModulesFolder

# Script Modules Names 
$ScriptModuleAllExports = 'ScriptModuleAllExports'
$ScriptModuleSomeFunctionsExports = 'ScriptModuleSomeFunctionsExports'
$ScriptModuleSomeAliasesExports = 'ScriptModuleSomeAliasesExports'

# BinaryModulesNames
$BinaryModuleNoManifest = 'BinaryModuleNoManifest'
$BinaryModuleManifestExportAll = 'BinaryModuleManifestExportAll'
$BinaryModuleManifestExportSome = 'BinaryModuleManifestExportSome'
$BinaryModuleManifestNoExport = 'BinaryModuleManifestNoExport'

# Nested Modules Names 
$NestedModuleAllExports = 'NestedModuleAllExports'
$NestedModuleSomeFunctionsExports = 'NestedModuleSomeFunctionsExports'
$NestedModuleSomeAliasesExports = 'NestedModuleSomeAliasesExports'

# Install the Test Module
function InstallModule($ModuleName)
{
    $ModulePath = Join-Path $TestModulesPath $ModuleName
    Copy-Item $ModulePath $UserModulesPath -Recurse -Force
}

Describe "Lite.Engine.Modules.AutoDiscovery" -Tags "P1", "RI" {


# Purpose:
#   491254: the interaction of command visibility and autoloading in constrained runspaces is not currently tested
#
#    
# Action:
#    Set Import-mOdule Visibility to Private 
#    Verify Anything imported by Internal function should be private and user should not be able to import 
#
# Expected Result:
#   Commands should be private in Constarined runspaces 
#
It "P1_AutoLoadingConstraintRunspaces"{
      	
	# Create Test Module
	$scriptmodule = Join-Path $env:temp "AutoLoadModule1.psm1"	


	"Function Print-Message { ""Hello"" } " | set-content $scriptmodule

	Import-module Microsoft.PowerShell.Utility

	# set import-module to private 
	(get-command Import-module).Visibility = "Private"

	$command = " & { Import-Module  " + $scriptmodule + " }"
        
	Invoke-Expression $command 
	
	# Check Visibility of new imported command. It should be private 
	$result = &{ (get-command print-message).Visibility }
	$result | should be "Private" "Visibility of Command should be private because Import-module is Private"
		
	# try to invoke in script block it should work because it is being called from public function by user 
	$result = &{ print-message }
        
        #Validate result 
	$result | should be "Hello" "Result from function invocation is not as expected"

	# set the visibilty back 
	(get-command import-module).Visibility = "Public"
}

}
