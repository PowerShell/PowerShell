Describe "Tests for Get-Acl and Set-Acl" -Tags "CI" {

    It "Verifies that you can change the ACL of a file that you own, but don't have explicit access to" -pending:($IsCore) {

        $caughtError = $false
        
        try
        {
            $tempDirectory = Join-Path TestDrive: GetSetAclPrivilegeTest
            $null = New-Item -ItemType Directory $tempDirectory
            $acl = Get-Acl $tempDirectory
            $acl.SetSecurityDescriptorSddlForm("O:SYG:SYD:PAI")
            Set-Acl -AclObject $acl $tempDirectory -ErrorAction Stop

            $acl.SetSecurityDescriptorSddlForm("O:DUG:DUD:PAI(A;OICI;FA;;;DU)")
            Set-Acl -AclObject $acl $tempDirectory -ErrorAction Stop
        }
        catch
        {
            $caughtError = $true
        }
                
        $caughtError | Should be $false
    }

	It "invalid access policy throws an error" -pending:($IsCore) {
        $fileName = New-Item TestDrive:\newFile.txt -Force
    	Get-Acl $fileName | Set-Acl $fileName -CentralAccessPolicy "SomeInvalidAccessPolicy" -ErrorAction SilentlyContinue -ErrorVariable setAclError
        $setAclError.FullyQualifiedErrorId | Should Be "SetAcl_CentralAccessPolicy,Microsoft.PowerShell.Commands.SetAclCommand"   	    
    }
}
