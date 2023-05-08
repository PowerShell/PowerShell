# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Get-CimClass' -Tags "CI" {

    It 'can get CIM_Error CIM class' -Pending:(-not $IsWindows) {
        Get-CimClass -ClassName CIM_Error | Should -Not -BeNullOrEmpty
    }

    It 'can get class when namespace is specified' -Pending:(-not $IsWindows) {
        Get-CimClass -ClassName CIM_OperatingSystem -Namespace root/cimv2 | Should -Not -BeNullOrEmpty
    }

    It 'produces an error when a non-existent class is used' -Pending:(-not $IsWindows) {
        { Get-CimClass -ClassName thisclasstypedoesnotexist -ErrorAction stop } |
            Should -Throw -ErrorId "HRESULT 0x80041002,Microsoft.Management.Infrastructure.CimCmdlets.GetCimClassCommand"
    }

    It 'produces an error when an improper namespace is used' -Pending:(-not $IsWindows) {
        { Get-CimClass -ClassName CIM_OperatingSystem -Namespace badnamespace -ErrorAction stop } |
            Should -Throw -ErrorId "HRESULT 0x8004100e,Microsoft.Management.Infrastructure.CimCmdlets.GetCimClassCommand"
    }
}

# feature tests
Describe 'Get-CimClass' -Tags @("Feature") {
    It 'can retrieve a class when a method is provided' -Pending:(-not $IsWindows) {
        Get-CimClass -MethodName Reboot | Should -Not -BeNullOrEmpty
    }

    It 'can retrieve class amended qualifiers' -Pending:(-not $IsWindows) {
        $a = Get-CimClass -Class 'Win32_LogicalDisk' -Amended
        $a.CimClassProperties['DriveType'].Qualifiers.Item('Values').Value.Count | Should -Be 7
    }
}
