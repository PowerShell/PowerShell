# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
. "$psscriptroot/TestRunner.ps1"

$assemblyName = "Microsoft.Management.Infrastructure.CimCmdlets"

# excluded resources, taken from the 'EmbeddedResource Remove'
# entries in the csproj for the assembly
$excludeList = @()
# load the module since it isn't there by default
if ( $IsWindows )
{
    import-module CimCmdlets
}

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName -ExcludeList $excludeList
