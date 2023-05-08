# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
. "$PSScriptRoot/TestRunner.ps1"
$AssemblyName = "Microsoft.PowerShell.Commands.Utility"

# excluded resources, taken from the 'EmbeddedResource Remove'
# entries in the csproj for the assembly
$excludeList = "CoreMshSnapinResources.resx",
    "ErrorPackageRemoting.resx",
    "FormatAndOut_out_gridview.resx",
    "UtilityMshSnapinResources.resx",
    "OutPrinterDisplayStrings.resx",
    "UpdateListStrings.resx",
    "ConvertFromStringResources.resx",
    "ConvertStringResources.resx",
    "FlashExtractStrings.resx",
    "ImmutableStrings.resx"
Import-Module Microsoft.Powershell.Utility
# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName -ExcludeList $excludeList
