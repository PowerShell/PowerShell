. "$psscriptroot/TestRunner.ps1"

$assemblyName = "Microsoft.WSMan.Management"

# excluded resources, taken from the 'EmbeddedResource Remove'
# entries in the csproj for the assembly
$excludeList = @()
# load the module since it isn't there by default
if ( $IsWindows ) {
    import-module Microsoft.WSMan.Management
}

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName -ExcludeList $excludeList
