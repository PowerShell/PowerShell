# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
. "$psscriptroot/TestRunner.ps1"

$assemblyName = "Microsoft.PowerShell.Security"

# excluded resources, taken from the 'EmbeddedResource Remove'
# entries in the csproj for the assembly
$excludeList = @("SecurityMshSnapinResources.resx")
# load the module since it isn't there by default
import-module Microsoft.PowerShell.Security

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName -ExcludeList $excludeList
