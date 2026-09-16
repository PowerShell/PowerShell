# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
. "$PSScriptRoot/TestRunner.ps1"
$AssemblyName = "Microsoft.PowerShell.Commands.Management"

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName
