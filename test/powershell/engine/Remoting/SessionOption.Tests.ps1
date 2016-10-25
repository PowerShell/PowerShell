try {
    if ( ! $IsWindows ) {
        $PSDefaultParameterValues['it:skip'] = $true
    }
    Describe " WSMan SessionOption object" -Tag @("CI") {
        It "The SessionOption type exists" {
            "Microsoft.WSMan.Management.SessionOption" -as "Type" | Should Not BeNullOrEmpty
        }
        It "The SessionOption type can be created" {
            $result = [Microsoft.WSMan.Management.SessionOption]::new()
            $result | should BeOfType "Microsoft.WSMan.Management.SessionOption"
        }
        It "The SessionOption type has the proper properties when created with the default constructor" {
            $result = [Microsoft.WSMan.Management.SessionOption]::new()
            $result.SkipCACheck         | should be $False
            $result.SkipCNCheck         | should be $False
            $result.SkipRevocationCheck | should be $False
            $result.UseEncryption       | should be $True
            $result.UseUtf16            | should be $False
            $result.ProxyAuthentication | should be 0
            $result.SPNPort             | should be 0
            $result.OperationTimeout    | should be 0
            $result.ProxyCredential     | should BeNullOrEmpty
            $result.ProxyAccessType     | should be ProxyIEConfig
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

            $result.SkipCACheck         | should be $true
            $result.SkipCNCheck         | should be $true
            $result.SkipRevocationCheck | should be $true
            $result.UseEncryption       | should be $False
            $result.UseUtf16            | should be $True
            $result.ProxyAuthentication | should be "Negotiate"
            $result.SPNPort             | should be 10
            $result.OperationTimeout    | should be 10
            $result.ProxyCredential     | should Not BeNullOrEmpty
            $result.ProxyAccessType     | should be "ProxyAutoDetect"
        }
    }
}
finally {
    $PSDefaultParameterValues.remove("it:skip")
}
