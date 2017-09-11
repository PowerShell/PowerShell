. "$psscriptroot/TestRunner.ps1"

$assemblyName = "Microsoft.WSMan.Management"
# this list is taken from ${AssemblyName}.csproj
# excluded resources
$excludeList = @()
# load the module since it isn't there by default


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
