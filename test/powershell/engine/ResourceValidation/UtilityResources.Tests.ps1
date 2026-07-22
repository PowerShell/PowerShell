# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
. "$PSScriptRoot/TestRunner.ps1"
$AssemblyName = "Microsoft.PowerShell.Commands.Utility"

Import-Module Microsoft.PowerShell.Utility
# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName
