# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Acl cmdlets are available and operate properly" -Tag CI {
    Context "Windows ACL test" {
        BeforeAll {
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
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

        AfterAll {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
        }
    }
}
