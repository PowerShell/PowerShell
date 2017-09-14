. "$psscriptroot/TestRunner.ps1"

$assemblyName = "Microsoft.WSMan.Management"

# excluded resources, taken from the 'EmbeddedResource Remove'
# entries in the csproj for the assembly
$excludeList = @()
# load the module since it isn't there by default
import-module Microsoft.WSMan.Management


try {
    if ( ! $IsWindows ) 
    {
        $PSDefaultParameterValues["it:skip"] = $true
    }
    # run the tests
    Test-ResourceStrings -AssemblyName $AssemblyName -ExcludeList $excludeList
}
finally {
    $PSDefaultParameterValues.Remove("it:skip")
}
