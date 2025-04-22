# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Acl cmdlets are available and operate properly" -Tag CI {
    Context "Windows ACL test" {
        BeforeAll {
            $PSDefaultParameterValues["It:Skip"] = -not $IsWindows
        }

        It "Get-Acl returns an ACL DirectorySecurity object"  {
            $ACL = Get-Acl $TESTDRIVE
            $ACL | Should -BeOfType System.Security.AccessControl.DirectorySecurity
        }

        It "Get-Acl -LiteralPath HKLM:Software\Classes\*"  {
            $ACL = Get-Acl -LiteralPath HKLM:Software\Classes\*
            $ACL | Should -BeOfType System.Security.AccessControl.RegistrySecurity
        }

        It "Get-Acl -LiteralPath .\Software\Classes\*"  {
            $currentPath = Get-Location
            Set-Location -LiteralPath HKLM:\
            $ACL = Get-Acl -LiteralPath .\Software\Classes\*
            $ACL | Should -BeOfType System.Security.AccessControl.RegistrySecurity
            $currentPath | Set-Location
        }

        It "Get-Acl -LiteralPath ."  {
            $currentPath = Get-Location
            Set-Location -LiteralPath $TESTDRIVE
            $ACL = Get-Acl -LiteralPath .
            $ACL | Should -BeOfType System.Security.AccessControl.DirectorySecurity
            $currentPath | Set-Location
        }

        It "Get-Acl -LiteralPath .."  {
            $currentPath = Get-Location
            Set-Location -LiteralPath $TESTDRIVE
            $ACL = Get-Acl -LiteralPath ..
            $ACL | Should -BeOfType System.Security.AccessControl.DirectorySecurity
            $currentPath | Set-Location
        }

        It "Get-Acl -Path .\Software\Classes\"  {
            $currentPath = Get-Location
            Set-Location -LiteralPath HKLM:\
            $ACL = Get-Acl -Path .\Software\Classes\
            $ACL | Should -BeOfType System.Security.AccessControl.RegistrySecurity
            $currentPath | Set-Location
        }

        It "Get-Acl -Path ."  {
            $currentPath = Get-Location
            Set-Location -LiteralPath $TESTDRIVE
            $ACL = Get-Acl -Path .
            $ACL | Should -BeOfType System.Security.AccessControl.DirectorySecurity
            $currentPath | Set-Location
        }

        It "Get-Acl -Path .."  {
            $currentPath = Get-Location
            Set-Location -LiteralPath $TESTDRIVE
            $ACL = Get-Acl -Path ..
            $ACL | Should -BeOfType System.Security.AccessControl.DirectorySecurity
            $currentPath | Set-Location
        }

        It "Set-Acl can set the ACL of a directory" {
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

        It "Can edit SD that contains an orphaned SID" {
            $badSid = [System.Security.Principal.SecurityIdentifier]::new("S-1-5-1234-5678")
            $currentUserSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User

            $testFilePath = "TestDrive:\pwsh-acl-test.txt"
            $testFile = New-Item -Path $testFilePath -ItemType File -Value 'foo' -Force

            # We should be able to set an SD entry to an untranslatable SID
            $fileSecurity = $testFilePath | Get-Acl
            $fileSecurity.SetGroup($badSid)
            Set-Acl -Path $testFile -AclObject $fileSecurity

            # We should be able to get the SD with an untranslatable SID
            $setSD = Get-Acl -Path $testFile
            $setSD.GetGroup([System.Security.Principal.SecurityIdentifier]) | Should -Be $badSid

            # We should be able to set it back to a known SID
            $setSD.SetGroup($currentUserSid)
            Set-Acl -Path $testFile -AclObject $setSD

            $actual = Get-Acl -Path $testFile
            $actualGroup = $actual.GetGroup([System.Security.Principal.SecurityIdentifier])
            $actualGroup | Should -Be $currentUserSid
        }

        AfterAll {
            $PSDefaultParameterValues.Remove("It:Skip")
        }
    }
}
