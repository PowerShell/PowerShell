. "$psscriptroot/TestRunner.ps1"

$assemblyName = "Microsoft.PowerShell.CoreCLR.Eventing"
# this list is taken from ${AssemblyName}.csproj
# excluded resources
$excludeList = @()
# load the module since it isn't there by default

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName -ExcludeList $excludeList
