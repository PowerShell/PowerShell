# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Acl cmdlets are available and operate properly" -Tag CI {
    It "Get-Acl returns an ACL object" -pending:(!$IsWindows) {
        $ACL = get-acl $TESTDRIVE
        $ACL | Should -BeOfType "System.Security.AccessControl.DirectorySecurity"
    }
    It "Set-Acl can set the ACL of a directory" -pending {
        Setup -d testdir
        $directory = "$TESTDRIVE/testdir"
        $acl = get-acl $directory
        $accessRule = [System.Security.AccessControl.FileSystemAccessRule]::New("Everyone","FullControl","ContainerInherit,ObjectInherit","None","Allow")
        $acl.AddAccessRule($accessRule)
        { $acl | Set-Acl $directory } | Should -Not -Throw

        $newacl = get-acl $directory
        $newrule = $newacl.Access | Where-Object { $accessrule.FileSystemRights -eq $_.FileSystemRights -and $accessrule.AccessControlType -eq $_.AccessControlType -and $accessrule.IdentityReference -eq $_.IdentityReference }
        $newrule | Should -Not -BeNullOrEmpty
    }
}
