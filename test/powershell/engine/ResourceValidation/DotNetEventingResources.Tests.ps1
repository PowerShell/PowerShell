# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
. "$psscriptroot/TestRunner.ps1"

$assemblyName = "Microsoft.PowerShell.CoreCLR.Eventing"

# excluded resources, taken from the 'EmbeddedResource Remove'
# entries in the csproj for the assembly
$excludeList = @()
# load the module since it isn't there by default

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName -ExcludeList $excludeList
