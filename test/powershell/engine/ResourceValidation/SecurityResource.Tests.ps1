. "$psscriptroot/TestRunner.ps1"

$assemblyName = "Microsoft.PowerShell.Security"
# this list is taken from ${AssemblyName}.csproj
# excluded resources
$excludeList = @("SecurityMshSnapinResources.resx")
# load the module since it isn't there by default
import-module Microsoft.PowerShell.Security

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName -ExcludeList $excludeList
