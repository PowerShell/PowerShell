# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
try {
    if ( ! $IsWindows ) {
        $PSDefaultParameterValues['it:pending'] = $true
    }

    Describe "CimInstance cmdlet tests" -Tag @("CI") {
        BeforeAll {
            if ( ! $IsWindows ) { return }
            $instance = Get-CimInstance CIM_ComputerSystem
        }

        It "CimClass property should not be null" {
            # we can't use equals here as on windows cimclassname
            # is win32_computersystem, but that's not likely to be the
            # case on non-Windows systems
            $instance.CimClass.CimClassName | Should -Match _computersystem
        }

        It "Property access should be case insensitive" {
            foreach($property in $instance.psobject.properties.name) {
                $pUpper = $property.ToUpper()
                $pLower = $property.ToLower()
                [string]$pLowerValue = $instance.$pLower -join ","
                [string]$pUpperValue = $instance.$pUpper -join ","
                $pLowerValue | Should -BeExactly $pUpperValue
            }
        }

        It "GetCimSessionInstanceId method invocation should return data" {
            $instance.GetCimSessionInstanceId() | Should -BeOfType "Guid"
        }

        It "should produce an error for a non-existing classname" {
<<<<<<< HEAD
            { get-ciminstance -classname thisnameshouldnotexist -ErrorAction stop } |
=======
            { Get-CimInstance -ClassName thisnameshouldnotexist -ErrorAction Stop } |
>>>>>>> 500c8535507a723951462ec2d465b78aac36b3d8
                Should -Throw -ErrorId "HRESULT 0x80041010,Microsoft.Management.Infrastructure.CimCmdlets.GetCimInstanceCommand"
        }
    }
}

finally {
    $PSDefaultParameterValues.Remove('it:pending')
}
