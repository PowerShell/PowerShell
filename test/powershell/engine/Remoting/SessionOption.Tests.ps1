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
            $result.SkipCACheck         | should -Be $False
            $result.SkipCNCheck         | should -Be $False
            $result.SkipRevocationCheck | should -Be $False
            $result.UseEncryption       | should -Be $True
            $result.UseUtf16            | should -Be $False
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

            $result.SkipCACheck         | should -Be $true
            $result.SkipCNCheck         | should -Be $true
            $result.SkipRevocationCheck | should -Be $true
            $result.UseEncryption       | should -Be $False
            $result.UseUtf16            | should -Be $True
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
