try {
    # Get-CimClass works only on windows right now
    if ( ! $IsWindows ) {
        $PSDefaultParameterValues['it:pending'] = $true
    }

    Describe 'Get-CimClass' -tags "CI" {
        It 'can get CIM_Error CIM class' {
            Get-CimClass -ClassName CIM_Error | Should Not BeNullOrEmpty
        }
        It 'can get class when namespace is specified' {
            Get-CimClass -ClassName CIM_OperatingSystem -Namespace root/cimv2 | Should Not BeNullOrEmpty
        }

        It 'produces an error when a non-existent class is used' {
            try {
                Get-CimClass -ClassName thisclasstypedoesnotexist -ea stop
                throw "Expected error did not occur"
            }
            catch {
                $_.FullyQualifiedErrorId | should be "HRESULT 0x80041002,Microsoft.Management.Infrastructure.CimCmdlets.GetCimClassCommand"
            }
        }
        It 'produces an error when an improper namespace is used' {
            try {
                Get-CimClass -ClassName CIM_OperatingSystem -Namespace badnamespace -ea stop
                throw "Expected error did not occur"
            }
            catch {
                $_.FullyQualifiedErrorId | should be "HRESULT 0x8004100e,Microsoft.Management.Infrastructure.CimCmdlets.GetCimClassCommand"
            }
        }
    }

    # feature tests
    Describe 'Get-CimClass' -tags @("Feature") {
        It 'can retrieve a class when a method is provided' {
            Get-CimClass -MethodName Reboot | Should Not BeNullOrEmpty
        }
    }
}
finally {
    $PSDefaultParameterValues.Remove('it:pending')
}
