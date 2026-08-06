# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
. "$PSScriptRoot/TestRunner.ps1"

$AssemblyName = "Microsoft.PowerShell.Security"

# load the module since it isn't there by default
Import-Module Microsoft.PowerShell.Security

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName
