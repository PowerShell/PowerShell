# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Module manifest for module 'ExpTest'

@{

# Version number of this module.
ModuleVersion = '0.0.1'

# Supported PSEditions
CompatiblePSEditions = @('Core')

# ID used to uniquely identify this module
GUID = '109f75d1-38c1-46b3-8995-e80661ce822d'

# Functions to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no functions to export.
FunctionsToExport = if ($EnabledExperimentalFeatures -contains "ExpTest.FeatureOne") {
    'Invoke-AzureFunctionV2', 'Get-GreetingMessage', 'Invoke-MyCommand', 'Test-MyRemoting', 'Save-MyFile', 'Test-MyDynamicParamOne', 'Test-MyDynamicParamTwo'
} else {
    'Invoke-AzureFunction', 'Get-GreetingMessage', 'Invoke-MyCommand', 'Test-MyRemoting', 'Save-MyFile', 'Test-MyDynamicParamOne', 'Test-MyDynamicParamTwo'
}

# Cmdlets to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no cmdlets to export.
CmdletsToExport = 'Invoke-AzureFunctionCSharp', 'Get-GreetingMessageCSharp', 'Invoke-MyCommandCSharp', 'Test-MyRemotingCSharp', 'Save-MyFileCSharp', 'Test-MyDynamicParamOneCSharp', 'Test-MyDynamicParamTwoCSharp'

# Aliases to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no aliases to export.
AliasesToExport = @(if ($EnabledExperimentalFeatures -contains "ExpTest.FeatureOne") { 'Invoke-AzureFunction' })

# Modules to import as nested modules of the module specified in RootModule/ModuleToProcess
NestedModules = @('ExpTest.psm1', 'ExpTest.dll')

# Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
PrivateData = @{
    PSData = @{
        ExperimentalFeatures = @(
            @{ Name = 'ExpTest.FeatureOne'; Description = "Test feature number one." }
            @{ Name = 'ExpTest.FeatureTwo'; Description = "Test feature number two." }
        )
    }
}

}
