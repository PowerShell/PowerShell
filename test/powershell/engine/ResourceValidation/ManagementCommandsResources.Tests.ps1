. "$psscriptroot/TestRunner.ps1"
$AssemblyName = "Microsoft.PowerShell.Commands.Management"
# this list is taken from ${AssemblyName}.csproj
# excluded resources
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
