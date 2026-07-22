# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
. "$PSScriptRoot/TestRunner.ps1"

$AssemblyName = "Microsoft.PowerShell.CoreCLR.Eventing"

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName
