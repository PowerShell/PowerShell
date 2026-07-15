# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
try {
    $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
    if ( ! $IsWindows ) {
        $PSDefaultParameterValues['it:skip'] = $true
    }
    Describe " WSMan SessionOption object" -Tag @("CI") {
        It "The SessionOption type exists" {
            "Microsoft.WSMan.Management.SessionOption" -as "Type" | Should -Not -BeNullOrEmpty
        }
        It "The SessionOption type can be created" {
            $result = [Microsoft.WSMan.Management.SessionOption]::new()
            $result | Should -BeOfType Microsoft.WSMan.Management.SessionOption
        }
        It "The SessionOption type has the proper properties when created with the default constructor" {
            $result = [Microsoft.WSMan.Management.SessionOption]::new()
            $result.SkipCACheck         | Should -BeFalse
            $result.SkipCNCheck         | Should -BeFalse
            $result.SkipRevocationCheck | Should -BeFalse
            $result.UseEncryption       | Should -BeTrue
            $result.UseUtf16            | Should -BeFalse
            $result.ProxyAuthentication | Should -Be 0
            $result.SPNPort             | Should -Be 0
            $result.OperationTimeout    | Should -Be 0
            $result.ProxyCredential     | Should -BeNullOrEmpty
            $result.ProxyAccessType     | Should -Be ProxyIEConfig
        }
        It "The values of SessionOption may be set" {
            $result = [Microsoft.WSMan.Management.SessionOption]::new()
            $result.SkipCACheck = $true
            $result.SkipCNCheck = $true
            $result.SkipRevocationCheck = $true
            $result.UseUtf16 = $true
            $result.UseEncryption = $false
            $result.ProxyAuthentication = "Negotiate"
            $result.SPNPort = 10
            $result.OperationTimeout = 10
            $result.ProxyAccessType = "ProxyAutoDetect"
            $result.ProxyCredential = [System.Net.NetworkCredential]::new("user","pass")

            $result.SkipCACheck         | Should -BeTrue
            $result.SkipCNCheck         | Should -BeTrue
            $result.SkipRevocationCheck | Should -BeTrue
            $result.UseEncryption       | Should -BeFalse
            $result.UseUtf16            | Should -BeTrue
            $result.ProxyAuthentication | Should -Be "Negotiate"
            $result.SPNPort             | Should -Be 10
            $result.OperationTimeout    | Should -Be 10
            $result.ProxyCredential     | Should -Not -BeNullOrEmpty
            $result.ProxyAccessType     | Should -Be "ProxyAutoDetect"
        }
    }
}
finally {
    $global:PSDefaultParameterValues = $originalDefaultParameterValues
}
