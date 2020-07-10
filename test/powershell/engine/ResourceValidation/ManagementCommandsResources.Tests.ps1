# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
. "$PSScriptRoot/TestRunner.ps1"
$AssemblyName = "Microsoft.PowerShell.Commands.Management"

# excluded resources, taken from the 'EmbeddedResource Remove'
# entries in the csproj for the assembly
$excludeList = "EventlogResources.resx",
    "TransactionResources.resx",
    "WebServiceResources.resx",
    "HotFixResources.resx",
    "ControlPanelResources.resx",
    "WmiResources.resx",
    "ManagementMshSnapInResources.resx",
    "ClearRecycleBinResources.resx",
    "ClipboardResources.resx"

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName -ExcludeList $excludeList
