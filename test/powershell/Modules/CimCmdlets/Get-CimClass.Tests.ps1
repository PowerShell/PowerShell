# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
try {
    # Get-CimClass works only on windows right now
    if ( ! $IsWindows ) {
        $PSDefaultParameterValues['it:pending'] = $true
    }

    Describe 'Get-CimClass' -Tags "CI" {

        It 'can get CIM_Error CIM class' {
            Get-CimClass -ClassName CIM_Error | Should -Not -BeNullOrEmpty
        }

        It 'can get class when namespace is specified' {
            Get-CimClass -ClassName CIM_OperatingSystem -Namespace root/cimv2 | Should -Not -BeNullOrEmpty
        }

        It 'produces an error when a non-existent class is used' {
            { Get-CimClass -ClassName thisclasstypedoesnotexist -ErrorAction Stop } | Should -Throw -ErrorId "HRESULT 0x80041002,Microsoft.Management.Infrastructure.CimCmdlets.GetCimClassCommand"
        }

        It 'produces an error when an improper namespace is used' {
            { Get-CimClass -ClassName CIM_OperatingSystem -Namespace badnamespace -ErrorAction Stop } | Should -Throw -ErrorId "HRESULT 0x8004100e,Microsoft.Management.Infrastructure.CimCmdlets.GetCimClassCommand"
        }
    }

    # feature tests
    Describe 'Get-CimClass' -Tags @("Feature") {
        It 'can retrieve a class when a method is provided' {
            Get-CimClass -MethodName Reboot | Should -Not -BeNullOrEmpty
        }
    }
}

finally {
    $PSDefaultParameterValues.Remove('it:pending')
}
