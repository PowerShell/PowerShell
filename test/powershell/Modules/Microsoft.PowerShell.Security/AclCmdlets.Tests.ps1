# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Acl cmdlets are available and operate properly" -Tag CI {
    It "Get-Acl returns an ACL object" -Pending:(!$IsWindows) {
        $ACL = Get-Acl $TESTDRIVE
        $ACL | Should -BeOfType System.Security.AccessControl.DirectorySecurity
    }
    It "Set-Acl can set the ACL of a directory" -Pending {
        Setup -d testdir
        $directory = "$TESTDRIVE/testdir"
        $acl = Get-Acl $directory
        $accessRule = [System.Security.AccessControl.FileSystemAccessRule]::New("Everyone","FullControl","ContainerInherit,ObjectInherit","None","Allow")
        $acl.AddAccessRule($accessRule)
        { $acl | Set-Acl $directory } | Should -Not -Throw

        $newacl = Get-Acl $directory
        $newrule = $newacl.Access | Where-Object { $accessrule.FileSystemRights -eq $_.FileSystemRights -and $accessrule.AccessControlType -eq $_.AccessControlType -and $accessrule.IdentityReference -eq $_.IdentityReference }
        $newrule | Should -Not -BeNullOrEmpty
    }
}
