# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
try {
    if ( ! $IsWindows ) {
        $PSDefaultParameterValues['it:skip'] = $true
    }
    Describe " WSMan SessionOption object" -Tag @("CI") {
        It "The SessionOption type exists" {
            "Microsoft.WSMan.Management.SessionOption" -as "Type" | Should -Not -BeNullOrEmpty
        }
        It "The SessionOption type can be created" {
            $result = [Microsoft.WSMan.Management.SessionOption]::new()
            $result | should -BeOfType "Microsoft.WSMan.Management.SessionOption"
        }
        It "The SessionOption type has the proper properties when created with the default constructor" {
            $result = [Microsoft.WSMan.Management.SessionOption]::new()
            $result.SkipCACheck         | should -BeFalse
            $result.SkipCNCheck         | should -BeFalse
            $result.SkipRevocationCheck | should -BeFalse
            $result.UseEncryption       | should -BeTrue
            $result.UseUtf16            | should -BeFalse
            $result.ProxyAuthentication | should -Be 0
            $result.SPNPort             | should -Be 0
            $result.OperationTimeout    | should -Be 0
            $result.ProxyCredential     | should -BeNullOrEmpty
            $result.ProxyAccessType     | should -Be ProxyIEConfig
        }
        It "The values of SessionOption may be set" {
            $result = [Microsoft.WSMan.Management.SessionOption]::new()
            $result.SkipCACheck = $true
            $result.SkipCNCheck = $true
            $result.SkipRevocationCheck = $true
            $result.UseUtf16 = $True
            $result.UseEncryption = $false
            $result.ProxyAuthentication = "Negotiate"
            $result.SPNPort = 10
            $result.OperationTimeout = 10
            $result.ProxyAccessType = "ProxyAutoDetect"
            $result.ProxyCredential = [System.Net.NetworkCredential]::new("user","pass")

            $result.SkipCACheck         | should -BeTrue
            $result.SkipCNCheck         | should -BeTrue
            $result.SkipRevocationCheck | should -BeTrue
            $result.UseEncryption       | should -BeFalse
            $result.UseUtf16            | should -BeTrue
            $result.ProxyAuthentication | should -Be "Negotiate"
            $result.SPNPort             | should -Be 10
            $result.OperationTimeout    | should -Be 10
            $result.ProxyCredential     | should -Not -BeNullOrEmpty
            $result.ProxyAccessType     | should -Be "ProxyAutoDetect"
        }
    }
}
finally {
    $PSDefaultParameterValues.remove("it:skip")
}
