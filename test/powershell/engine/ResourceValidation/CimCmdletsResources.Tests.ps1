# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
. "$PSScriptRoot/TestRunner.ps1"

$assemblyName = "Microsoft.Management.Infrastructure.CimCmdlets"

# excluded resources, taken from the 'EmbeddedResource Remove'
# entries in the csproj for the assembly
$excludeList = @()
# load the module since it isn't there by default
if ( $IsWindows )
{
    Import-Module CimCmdlets
}

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName -ExcludeList $excludeList
