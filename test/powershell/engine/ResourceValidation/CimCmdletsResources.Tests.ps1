# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
. "$PSScriptRoot/TestRunner.ps1"

$AssemblyName = "Microsoft.Management.Infrastructure.CimCmdlets"

# load the module since it isn't there by default
if ( $IsWindows )
{
    Import-Module CimCmdlets
}

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName
