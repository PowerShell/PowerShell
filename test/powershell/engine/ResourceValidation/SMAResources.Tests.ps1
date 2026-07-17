# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
. "$PSScriptRoot/TestRunner.ps1"
$AssemblyName = "System.Management.Automation"

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName
