# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
. "$PSScriptRoot/TestRunner.ps1"

$AssemblyName = "Microsoft.PowerShell.ConsoleHost"

# excluded resources, taken from the 'EmbeddedResource Remove'
# entries in the csproj for the assembly
$excludeList = @("HostMshSnapinResources.resx")

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName -ExcludeList $excludeList
