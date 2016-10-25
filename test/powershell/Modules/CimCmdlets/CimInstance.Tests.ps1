Try {
    if ( ! $IsWindows ) {
        $PSDefaultParameterValues['it:pending'] = $true
    }
    Describe "CimInstance cmdlet tests" -Tag @("CI") {
        BeforeAll {
            if ( ! $IsWindows ) { return }
            $instance = get-ciminstance cim_computersystem
        }
        It "CimClass property should not be null" {
            # we can't use equals here as on windows cimclassname
            # is win32_computersystem, but that's not likely to be the
            # case on non-Windows systems
            $instance.cimClass.CimClassName | should match _computersystem
        }
        It "Property access should be case insensitive" {
            foreach($property in $instance.psobject.properties.name) {
                $pUpper = $property.ToUpper()
                $pLower = $property.ToLower()
                [string]$pLowerValue = $pinstance.$pLower -join ","
                [string]$pUpperValue = $pinstance.$pUpper -join ","
                $pLowerValue | should be $pUpperValue
            }
        }
        It "GetCimSessionInstanceId method invocation should return data" {
           $instance.GetCimSessionInstanceId() | Should BeOfType "Guid"
        }
        It "should produce an error for a non-existing classname" {
            try {
                get-ciminstance -classname thisnameshouldnotexist -ea stop
                throw "expected error did not occur"
            }
            catch {
                $_.FullyQualifiedErrorId | should be "HRESULT 0x80041010,Microsoft.Management.Infrastructure.CimCmdlets.GetCimInstanceCommand"
            }
        }
    }
}
finally {
    $PSDefaultParameterValues.Remove('it:pending')
}
