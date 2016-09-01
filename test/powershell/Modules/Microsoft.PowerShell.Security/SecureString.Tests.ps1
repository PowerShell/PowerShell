Describe "SecureString conversion tests" -Tags "CI" {
    BeforeAll {
        $string = "ABCD"
        $secureString = [System.Security.SecureString]::New()
        $string.ToCharArray() | foreach-object { $securestring.AppendChar($_) }
        $defaultParamValues = $PSdefaultParameterValues.Clone()
        $PSdefaultParameterValues = @{}
        if ( ! $IsWindows ) { $PSdefaultParameterValues["it:pending"] = $true }
    }
    AfterAll {
        $PSdefaultParameterValues = $defaultParamValues 
    }

    It "using null arguments to ConvertFrom-SecureString produces an exception" {
        try {
            ConvertFrom-SecureString -secureString $null -key $null
        }
        catch {
            $_.FullyQualifiedErrorId | should be "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.ConvertFromSecureStringCommand"
        }
        
    }
    
    It "using a bad key produces an exception" {
        try {
            $badkey = [byte[]]@(1,2)
            ConvertFrom-SecureString -securestring $secureString -key $badkey
            throw "Command did not throw exception"
        }
        catch {
            $_.FullyQualifiedErrorId | should be "Argument,Microsoft.PowerShell.Commands.ConvertFromSecureStringCommand"
        }
    }

    It "Can convert to a secure string" {
        $ss = ConvertTo-SecureString -AsPlainText -Force abcd
        $ss.GetType().Name | should be "SecureString"
    }
    It "can convert back from a secure string" {
        $secret = "abcd"
        $ss1 = ConvertTo-SecureString -AsPlainText -Force $secret
        $ss2 = convertfrom-securestring $ss1 | convertto-securestring
        [pscredential]::New("user",$ss2).GetNetworkCredential().Password | should be $secret
    }
}
