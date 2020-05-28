# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "TestWSMan tests" -Tags 'Feature','RequireAdminOnWindows' {

    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if ( !$IsWindows ) {
            $PSDefaultParameterValues["it:skip"] = $true
        }
        else {
            $testWsman = [Microsoft.WSMan.Management.TestWSManCommand]::new()
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It "TestWSmanCommand can be used as API for '<parameter>' with '<value>'" -TestCases @(
        @{ parameter = "computername"    ; value = "foo" },
        @{ parameter = "computername"    ; value = $null ; expected = "localhost" },
        @{ parameter = "computername"    ; value = "."   ; expected = "localhost" },
        @{ parameter = "authentication"  ; value = "Basic" },
        @{ parameter = "authentication"  ; value = "CredSSP" },
        @{ parameter = "authentication"  ; value = "Digest" },
        @{ parameter = "authentication"  ; value = "Negotiate" },
        @{ parameter = "authentication"  ; value = "ClientCertificate" },
        @{ parameter = "authentication"  ; value = "Default" },
        @{ parameter = "authentication"  ; value = "Kerberos" },
        @{ parameter = "authentication"  ; value = "None" },
        @{ parameter = "port"            ; value = 5985 },
        @{ parameter = "port"            ; value = 8888 },
        @{ parameter = "usessl"          ; value = $true },
        @{ parameter = "usessl"          ; value = $false },
        @{ parameter = "applicationname" ; value = "foo" }
    ) {
        param($parameter, $value, $expected)
        $testWsman.$parameter = $value
        if ($expected -eq $null) {
            $expected = $value
        }
        $testWsman.$parameter | Should -Be $expected
    }

    It "-Authentication for unsupported type should return error" {
        { Test-WSMan -Authentication foo -ErrorAction Stop } | Should -Throw -ErrorId "CannotConvertArgumentNoMessage,Microsoft.WSMan.Management.TestWSManCommand"
    }

    It "Test-WSMan works for <testName>" -TestCases @(
        @{ testName = "Null"; computername = $null },
        @{ testName = "localhost"; computername = "localhost" },
        @{ testName = "computername"; computername = $env:COMPUTERNAME }
    ) {
        param($computername)
        $response = Test-WSMan -ComputerName $computername
        $response | Should -Not -BeNullOrEmpty
        $response.PSObject.TypeNames[0] | Should -Be "System.Xml.XmlElement#http://schemas.dmtf.org/wbem/wsman/identity/1/wsmanidentity.xsd#IdentifyResponse"
        $response.wsmid | Should -Be "http://schemas.dmtf.org/wbem/wsman/identity/1/wsmanidentity.xsd"
        $response.ProtocolVersion | Should -Be "http://schemas.dmtf.org/wbem/wsman/1/wsman.xsd"
        $response.ProductVendor | Should -Be "Microsoft Corporation"
        $response.ProductVersion | Should -Be "OS: 0.0.0 SP: 0.0 Stack: 3.0"
    }
}
