. "$psscriptroot/TestRunner.ps1"
$AssemblyName = "Microsoft.PowerShell.Commands.Utility"
# this list is taken from ${AssemblyName}.csproj
# excluded resources
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
import-module Microsoft.Powershell.Utility
# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName -ExcludeList $excludeList
