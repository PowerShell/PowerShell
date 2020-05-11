# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
. "$PSScriptRoot/TestRunner.ps1"
$AssemblyName = "System.Management.Automation"

# excluded resources, taken from the 'EmbeddedResource Remove'
# entries in the csproj for the assembly
$excludeList = "CoreMshSnapinResources.resx",
    "ErrorPackageRemoting.resx",
    "EventResource.resx"
# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName -ExcludeList $excludeList
