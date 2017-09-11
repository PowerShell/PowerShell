. "$psscriptroot/TestRunner.ps1"

$AssemblyName = "Microsoft.PowerShell.ConsoleHost"

# excluded resources
$excludeList = @("HostMshSnapinResources.resx")

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName -ExcludeList $excludeList
