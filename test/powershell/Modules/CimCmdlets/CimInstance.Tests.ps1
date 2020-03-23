# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "CimInstance cmdlet tests" -Tag @("CI") {
    BeforeAll {
        if ( ! $IsWindows ) { return }
        $instance = Get-CimInstance CIM_ComputerSystem
    }

    It "CimClass property should not be null" -Pending:(-not $IsWindows) {
        # we can't use equals here as on windows cimclassname
        # is win32_computersystem, but that's not likely to be the
        # case on non-Windows systems
        $instance.CimClass.CimClassName | Should -Match _computersystem
    }

    It "Property access should be case insensitive" -Pending:(-not $IsWindows) {
        foreach($property in $instance.psobject.properties.name) {
            $pUpper = $property.ToUpper()
            $pLower = $property.ToLower()
            [string]$pLowerValue = $instance.$pLower -join ","
            [string]$pUpperValue = $instance.$pUpper -join ","
            $pLowerValue | Should -BeExactly $pUpperValue
        }
    }

    It "GetCimSessionInstanceId method invocation should return data" -Pending:(-not $IsWindows) {
        $instance.GetCimSessionInstanceId() | Should -BeOfType Guid
    }

    It "should produce an error for a non-existing classname" -Pending:(-not $IsWindows) {
        { Get-CimInstance -ClassName thisnameshouldnotexist -ErrorAction Stop } |
            Should -Throw -ErrorId "HRESULT 0x80041010,Microsoft.Management.Infrastructure.CimCmdlets.GetCimInstanceCommand"
    }
}
