# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
. "$PSScriptRoot/TestRunner.ps1"

$AssemblyName = "Microsoft.WSMan.Management"

# load the module since it isn't there by default
if ( $IsWindows ) {
    Import-Module Microsoft.WSMan.Management
}

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName
