# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Module removed due to #4272
# disabling tests

return

function IsWin10OrHigher
{
    $version = [system.environment]::osversion.version

    return ($version.Major -ge 10)
}

function RemoveTestUsers
{
    param([string] $basename)

    $results = Get-LocalUser $basename*
    foreach ($element in $results) {
        Remove-LocalUser -SID $element.SID
    }
}

function RemoveTestGroups
{
    param([string] $basename)

    $results = Get-LocalGroup $basename*
    foreach ($element in $results) {
        Remove-LocalGroup -SID $element.SID
    }
}

function VerifyFailingTest
{
    param(
        [scriptblock] $sb,
        [string] $expectedFqeid
    )

    $backupEAP = $script:ErrorActionPreference
    $script:ErrorActionPreference = "Stop"

    try {
        & $sb
        throw "Expected error: $expectedFqeid"
    }
    catch {
        $_.FullyQualifiedErrorId | Should -Be $expectedFqeid
    }
    finally {
        $script:ErrorActionPreference = $backupEAP
    }
}

try {
    #skip all tests on non-windows platform
    $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
    $IsNotSkipped = ($IsWindows -eq $true);
    $PSDefaultParameterValues["it:skip"] = !$IsNotSkipped

    Describe "Verify Expected LocalGroupMember Cmdlets are present" -Tags "CI" {

        It "Test command presence" {
            $result = Get-Command -Module Microsoft.PowerShell.LocalAccounts | ForEach-Object Name

            $result -contains "Add-LocalGroupMember" | Should -BeTrue
            $result -contains "Get-LocalGroupMember" | Should -BeTrue
            $result -contains "Remove-LocalGroupMember" | Should -BeTrue
        }
    }

    Describe "Validate simple Add-LocalGroupMember" -Tags @('CI', 'RequireAdminOnWindows') {

        BeforeEach {
            if ($IsNotSkipped) {
                $user1sid = [string](New-LocalUser TestUser1 -NoPassword).SID
                $group1sid = [string](New-LocalGroup TestGroup1).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestGroups -basename TestGroup
                RemoveTestUsers -basename TestUser
            }
        }

        It "Can add local user to group by name" {
            Add-LocalGroupMember TestGroup1 -Member TestUser1
            $result = Get-LocalGroupMember TestGroup1

            $result.Name.EndsWith("TestUser1") | Should -BeTrue
            $result.SID | Should -Be $user1sid
        }
    }

    Describe "Validate Add-LocalGroupMember cmdlet" -Tags @('Feature', 'RequireAdminOnWindows') {

        BeforeAll {
            $OptDomainPrefix="(.+\\)?"
        }

        BeforeEach {
            if ($IsNotSkipped) {
                $user1sid = [string](New-LocalUser TestUser1 -NoPassword).SID
                $user2sid = [string](New-LocalUser TestUser2 -NoPassword).SID
                $group1sid = [string](New-LocalGroup TestGroup1).SID
                $group2sid = [string](New-LocalGroup TestGroup2).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestGroups -basename TestGroup
                RemoveTestUsers -basename TestUser
            }
        }

        It "Can add user to group using SID" {
            Add-LocalGroupMember -SID $group1sid -Member TestUser1
            $result = Get-LocalGroupMember TestGroup1

            $result.Name.EndsWith("TestUser1") | Should -BeTrue
            $result.SID | Should -Be $user1sid
        }

        It "Can add user to group using group object" {
            $groupObject = Get-LocalGroup TestGroup1
            Add-LocalGroupMember -Group $groupObject -Member TestUser1
            $result = Get-LocalGroupMember TestGroup1

            $result.Name.EndsWith("TestUser1") | Should -BeTrue
            $result.SID | Should -Be $user1sid
        }

        It "Can add user to group using pipeline" {
            Get-LocalUser TestUser1 | Add-LocalGroupMember -Name TestGroup1
            $result = Get-LocalGroupMember TestGroup1

            $result.Name.EndsWith("TestUser1") | Should -BeTrue
            $result.SID | Should -Be $user1sid
        }

        It "Errors on missing group parameter value missing" {
            $sb = {
                Add-LocalGroupMember -Member TestUser1
            }
            VerifyFailingTest $sb "AmbiguousParameterSet,Microsoft.PowerShell.Commands.AddLocalGroupMemberCommand"
        }

        It "Errors on missing user parameter value missing" {
            $sb = {
                Add-LocalGroupMember TestGroup1 -Member
            }
            VerifyFailingTest $sb "MissingArgument,Microsoft.PowerShell.Commands.AddLocalGroupMemberCommand"
        }

        It "Errors on adding group to group" {
            $sb = {
                Add-LocalGroupMember TestGroup1 TestGroup2
            }
            VerifyFailingTest $sb "Internal,Microsoft.PowerShell.Commands.AddLocalGroupMemberCommand"
        }

        It "Can add array of members to group" {
            Add-LocalGroupMember TestGroup1 -Member @("TestUser1", "TestUser2")
            $result = Get-LocalGroupMember TestGroup1

            $result[0].Name -match ($OptDomainPrefix + "TestUser1") | Should -BeTrue
            $result[1].Name -match ($OptDomainPrefix + "TestUser2") | Should -BeTrue
        }

        It "Can add array of user SIDs to group" {
            Add-LocalGroupMember TestGroup1 -Member @($user1sid, $user2sid)
            $result = Get-LocalGroupMember TestGroup1

            $result[0].Name -match ($OptDomainPrefix + "TestUser1") | Should -BeTrue
            $result[1].Name -match ($OptDomainPrefix + "TestUser2") | Should -BeTrue
        }

        It "Can add array of users names or SIDs to group" {
            Add-LocalGroupMember TestGroup1 -Member @($user1sid, "TestUser2")
            $result = Get-LocalGroupMember TestGroup1

            $result[0].Name -match ($OptDomainPrefix + "TestUser1") | Should -BeTrue
            $result[1].Name -match ($OptDomainPrefix + "TestUser2") | Should -BeTrue
        }

        It "Can add array of user names using pipeline" {
            @("TestUser1", "TestUser2") | Add-LocalGroupMember TestGroup1
            $result = Get-LocalGroupMember TestGroup1

            $result[0].Name -match ($OptDomainPrefix + "TestUser1") | Should -BeTrue
            $result[1].Name -match ($OptDomainPrefix + "TestUser2") | Should -BeTrue
        }

        It "Can add array of existent and nonexistent users names to group" {
            $sb = {
                Add-LocalGroupMember TestGroup1 -Member @("TestUser1", "TestNonexistentUser", "TestUser2")
            }
            VerifyFailingTest $sb "PrincipalNotFound,Microsoft.PowerShell.Commands.AddLocalGroupMemberCommand"

            $result = Get-LocalGroupMember TestGroup1
            $result.Name -match ($OptDomainPrefix + "TestUser1") | Should -BeTrue
            $result.Name -match ($OptDomainPrefix + "TestUser2") | Should -BeFalse
        }

        It "Errors on adding user to group by name twice" {
            $sb = {
                Add-LocalGroupMember TestGroup1 -Member TestUser1
                Add-LocalGroupMember TestGroup1 -Member TestUser1
            }
            VerifyFailingTest $sb "MemberExists,Microsoft.PowerShell.Commands.AddLocalGroupMemberCommand"

            $result = Get-LocalGroupMember TestGroup1
            $result.Name.EndsWith("TestUser1") | Should -BeTrue
            $result.SID | Should -Be $user1sid
        }

        It "Errors on adding nonexistent user to group" {
            $sb = {
                Add-LocalGroupMember -Name TestGroup1 -Member TestNonexistentUser1
            }
            VerifyFailingTest $sb "PrincipalNotFound,Microsoft.PowerShell.Commands.AddLocalGroupMemberCommand"
        }

        It "Errors on adding user to nonexistent group" {
            $sb = {
                Add-LocalGroupMember TestNonexistentGroup1 -Member TestUser1 -ErrorAction Stop
            }
            VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.AddLocalGroupMemberCommand"
        }

        It "Can respond to -ErrorAction Stop" {
            $sb = {
                Add-LocalGroupMember TestGroup1 -Member @("TestUser1", "TestNonexistentUser1", "TestNonexistentUser2") -ErrorAction Stop -ErrorVariable OutputError | Out-Null
            }
            VerifyFailingTest $sb "PrincipalNotFound,Microsoft.PowerShell.Commands.AddLocalGroupMemberCommand"

            $result = Get-LocalGroupMember TestGroup1
            $result.Name -match ($OptDomainPrefix + "TestUser1") | Should -BeTrue
        }
    }

    Describe "Validate simple Get-LocalGroupMember" -Tags @('CI', 'RequireAdminOnWindows') {

        BeforeEach {
            if ($IsNotSkipped) {
                $user1 = New-LocalUser TestUserGet1 -NoPassword
                $group1 = New-LocalGroup TestGroupGet1
                Add-LocalGroupMember TestGroupGet1 -Member TestUserGet1
                $user1sid = [string]($user1.SID)
                $group1sid = [string]($group1.SID)
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestGroups -basename TestGroupGet
                RemoveTestUsers -basename TestUserGet
            }
        }

        It "Can get a local group member by name" {
            $result = Get-LocalGroupMember TestGroupGet1

            $result.Name.EndsWith("TestUserGet1") | Should -BeTrue
            $result.SID | Should -Be $user1sid
            if (IsWin10OrHigher)
            {
                $result.PrincipalSource | Should -Be Local
            }
            $result.ObjectClass | Should -Be User
        }
    }

    Describe "Validate Get-LocalGroupMember cmdlet" -Tags @('Feature', 'RequireAdminOnWindows') {

        BeforeAll {
            $OptDomainPrefix="(.+\\)?"
        }

        BeforeEach {
            if ($IsNotSkipped) {
                $user1 = New-LocalUser TestUserGet1 -NoPassword
                $user2 = New-LocalUser TestUserGet2 -NoPassword
                $group1 = New-LocalGroup TestGroupGet1
                $group2 = New-LocalGroup TestGroupGet2
                Add-LocalGroupMember TestGroupGet1 -Member TestUserGet1
                Add-LocalGroupMember TestGroupGet1 -Member TestUserGet2
                $user1sid = [string]($user1.SID)
                $user2sid = [string]($user2.SID)
                $group1sid = [string]($group1.SID)
                $group2sid = [string]($group2.SID)
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestGroups -basename TestGroupGet
                RemoveTestUsers -basename TestUserGet
            }
        }

        It "Can get all group members by name" {
            $result = Get-LocalGroupMember TestGroupGet1

            $result[0].Name.EndsWith("TestUserGet1") | Should -BeTrue
            $result[0].SID | Should -Be $user1sid
            if (IsWin10OrHigher)
            {
                $result[0].PrincipalSource | Should -Be Local
            }
            $result[0].ObjectClass | Should -Be User
            $result[1].Name.EndsWith("TestUserGet2") | Should -BeTrue
            $result[1].SID | Should -Be $user2sid
            if (IsWin10OrHigher)
            {
                $result[1].PrincipalSource | Should -Be Local
            }
            $result[1].ObjectClass | Should -Be User
        }

        It "Can get all group members by SID" {
            $result = Get-LocalGroupMember -SID $group1sid

            $result[0].Name.EndsWith("TestUserGet1") | Should -BeTrue
            $result[0].SID | Should -Be $user1sid
            if (IsWin10OrHigher)
            {
                $result[0].PrincipalSource | Should -Be Local
            }
            $result[0].ObjectClass | Should -Be User
            $result[1].Name.EndsWith("TestUserGet2") | Should -BeTrue
            $result[1].SID | Should -Be $user2sid
            if (IsWin10OrHigher)
            {
                $result[1].PrincipalSource | Should -Be Local
            }
            $result[1].ObjectClass | Should -Be User
        }

        It "Can get all group members by Group object" {
            $group = Get-LocalGroup TestGroupGet1
            $result = Get-LocalGroupMember -Group $group

            $result[0].Name.EndsWith("TestUserGet1") | Should -BeTrue
            $result[0].SID | Should -Be $user1sid
            if (IsWin10OrHigher)
            {
                $result[0].PrincipalSource | Should -Be Local
            }
            $result[0].ObjectClass | Should -Be User
            $result[1].Name.EndsWith("TestUserGet2") | Should -BeTrue
            $result[1].SID | Should -Be $user2sid
            if (IsWin10OrHigher)
            {
                $result[1].PrincipalSource | Should -Be Local
            }
            $result[1].ObjectClass | Should -Be User
        }

        It "Can get all group members by pipeline" {
            $result = Get-LocalGroup TestGroupGet1 | Get-LocalGroupMember

            $result[0].Name.EndsWith("TestUserGet1") | Should -BeTrue
            $result[0].SID | Should -Be $user1sid
            if (IsWin10OrHigher)
            {
                $result[0].PrincipalSource | Should -Be Local
            }
            $result[0].ObjectClass | Should -Be User
            $result[1].Name.EndsWith("TestUserGet2") | Should -BeTrue
            $result[1].SID | Should -Be $user2sid
            if (IsWin10OrHigher)
            {
                $result[1].PrincipalSource | Should -Be Local
            }
            $result[1].ObjectClass | Should -Be User
        }

        It "Can get group members by wildcard" {
            $result = Get-LocalGroupMember TestGroupGet1 -Member TestUserGet*
            $result.Count -eq 2 | Should -BeTrue
            $result[0].Name -match ($OptDomainPrefix+"TestUserGet1") | Should -BeTrue
            $result[1].Name -match ($OptDomainPrefix + "TestUserGet2") | Should -BeTrue
        }

        It "Errors on group name being nonexistent" {
            $sb = {
                Get-LocalGroupMember NonexistentGroup
            }
            VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.GetLocalGroupMemberCommand"
        }

        It "Can get specific group member by name" {
            $result = Get-LocalGroupMember TestGroupGet1 -Member TestUserGet1

            $result.Name.EndsWith("TestUserGet1") | Should -BeTrue
            $result.SID | Should -Be $user1sid
            if (IsWin10OrHigher)
            {
                $result.PrincipalSource | Should -Be Local
            }
            $result.ObjectClass | Should -Be User
        }

        #TODO: 10.A valid user attempts to get membership from a group to which they don't have access
    }

    Describe "Validate simple Remove-LocalGroupMember" -Tags @('CI', 'RequireAdminOnWindows') {

        BeforeEach {
            if ($IsNotSkipped) {
                $user1 = New-LocalUser TestUserRemove1 -NoPassword
                $group1 = New-LocalGroup TestGroupRemove1
                Add-LocalGroupMember TestGroupRemove1 -Member TestUserRemove1
                $user1sid = [string]($user1.SID)
                $group1sid = [string]($group1.SID)
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestGroups -basename TestGroupRemove
                RemoveTestUsers -basename TestUserRemove
            }
        }

        It "Can remove a local group member by name" {
            Remove-LocalGroupMember TestGroupRemove1 -Member TestUserRemove1
            $result = Get-LocalGroupMember TestGroupRemove1

            $result | Should -Be $null
        }
    }

    Describe "Validate Remove-LocalGroupMember cmdlet" -Tags @('Feature', 'RequireAdminOnWindows') {

        BeforeEach {
            if ($IsNotSkipped) {
                $user1 = New-LocalUser TestUserRemove1 -NoPassword
                $user2 = New-LocalUser TestUserRemove2 -NoPassword
                $group1 = New-LocalGroup TestGroupRemove1
                $group2 = New-LocalGroup TestGroupRemove2
                Add-LocalGroupMember TestGroupRemove1 -Member TestUserRemove1
                Add-LocalGroupMember TestGroupRemove1 -Member TestUserRemove2
                $user1sid = [string]($user1.SID)
                $user2sid = [string]($user2.SID)
                $group1sid = [string]($group1.SID)
                $group2sid = [string]($group2.SID)
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestGroups -basename TestGroupRemove
                RemoveTestUsers -basename TestUserRemove
            }
        }

        It "Can remove a group member by name" {
            Remove-LocalGroupMember TestGroupRemove1 -Member TestUserRemove2
            $result = Get-LocalGroupMember TestGroupRemove1

            $result.Name.EndsWith("TestUserRemove1") | Should -BeTrue
        }

        It "Can remove a group member by SID" {
            Remove-LocalGroupMember -SID $group1sid -Member TestUserRemove2
            $result = Get-LocalGroupMember TestGroupRemove1

            $result.Name.EndsWith("TestUserRemove1") | Should -BeTrue
        }

        It "Can remove a group member by Group object" {
            $group = Get-LocalGroup TestGroupRemove1
            Remove-LocalGroupMember -Group $group -Member TestUserRemove2
            $result = Get-LocalGroupMember TestGroupRemove1

            $result.Name.EndsWith("TestUserRemove1") | Should -BeTrue
        }

        It "Can remove a group member by pipeline" {
            Get-LocalUser TestUserRemove2 | Remove-LocalGroupMember -Name TestGroupRemove1
            $result = Get-LocalGroupMember TestGroupRemove1

            $result.Name.EndsWith("TestUserRemove1") | Should -BeTrue
        }

        It "Errors on group argument missing" {
            $sb = {
                Remove-LocalGroupMember -Member TestUserRemove2
            }
            VerifyFailingTest $sb "AmbiguousParameterSet,Microsoft.PowerShell.Commands.RemoveLocalGroupMemberCommand"
        }

        It "Errors on member argument missing" {
            $sb = {
                Remove-LocalGroupMember TestGroupRemove1 -Member
            }
            VerifyFailingTest $sb "MissingArgument,Microsoft.PowerShell.Commands.RemoveLocalGroupMemberCommand"
        }

        It "Errors on remove a group member not in the group" {
            $sb = {
                Remove-LocalGroupMember TestGroupRemove2 -Member TestUserRemove2
            }
            VerifyFailingTest $sb "MemberNotFound,Microsoft.PowerShell.Commands.RemoveLocalGroupMemberCommand"
        }

        It "Errors on remove group members by array of name" {
            $sb = {
                Remove-LocalGroupMember TestGroupRemove2 -Member TestUserRemove2
                Get-LocalGroupMember TestGroupRemove1
            }
            VerifyFailingTest $sb "MemberNotFound,Microsoft.PowerShell.Commands.RemoveLocalGroupMemberCommand"
        }

        It "Can remove array of user names from group" {
            Remove-LocalGroupMember TestGroupRemove1 -Member @("TestUserRemove1", "TestUserRemove2")
            $result = Get-LocalGroupMember TestGroupRemove1

            $result | Should -Be $null
        }

        It "Can remove array of user SIDs from group" {
            Remove-LocalGroupMember TestGroupRemove1 -Member @($user1sid, $user2sid)
            $result = Get-LocalGroupMember TestGroupRemove1

            $result | Should -Be $null
        }

        It "Can remove array of users names or SIDs from group" {
            Remove-LocalGroupMember TestGroupRemove1 -Member @($user1sid, "TestUserRemove2")
            $result = Get-LocalGroupMember TestGroupRemove1

            $result | Should -Be $null
        }

        It "Can remove array of user names using pipeline" {
            $name1 = (Get-LocalUser "TestUserRemove1").Name
            $name2 = (Get-LocalUser "TestUserRemove2").Name
            @($name1, $name2) | Remove-LocalGroupMember TestGroupRemove1
            $result = Get-LocalGroupMember TestGroupRemove1

            $result | Should -Be $null
        }

        It "Errors on remove nonexistent user from group" {
            $sb = {
                Remove-LocalGroupMember TestGroupRemove1 -Member TestNonexistentUser1
            }
            VerifyFailingTest $sb "PrincipalNotFound,Microsoft.PowerShell.Commands.RemoveLocalGroupMemberCommand"
        }

        It "Errors on remove user from nonexistent group" {
            $sb = {
                Remove-LocalGroupMember TestNonexistentGroup1 -Member TestUserRemove1 -ErrorAction Stop
            }
            VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.RemoveLocalGroupMemberCommand"
        }

        #TODO: 16.A valid user attempts to remove a user/group from a group to which they don’t have access

        It "Can remove array of existent and nonexistent users names from group" {
            $sb = {
                Remove-LocalGroupMember TestGroupRemove1 -Member @("TestUserRemove1", "TestNonexistentUser", "TestUserRemove2")
            }
            VerifyFailingTest $sb "PrincipalNotFound,Microsoft.PowerShell.Commands.RemoveLocalGroupMemberCommand"

            $result = Get-LocalGroupMember TestGroupRemove2
            $result | Should -Be $null
        }

        It "Errors on remove user from nonexistent group" {
            $sb = {
                Remove-LocalGroupMember TestGroupRemove1 -Member TestGroupRemove2 -ErrorAction Stop
            }
            VerifyFailingTest $sb "MemberNotFound,Microsoft.PowerShell.Commands.RemoveLocalGroupMemberCommand"
        }

        It "Can respond to -ErrorAction Stop" {
            $sb = {
                Remove-LocalGroupMember TestGroupRemove1 -Member @("TestUserRemove1", "TestNonexistentUser1", "TestUserRemove2") -ErrorAction Stop -ErrorVariable outError | Out-Null
            }
            VerifyFailingTest $sb "PrincipalNotFound,Microsoft.PowerShell.Commands.RemoveLocalGroupMemberCommand"

            $result = Get-LocalGroupMember TestGroupRemove1 2>&1
            $result.Name -match ($OptDomainPrefix + "TestUserRemove2") | Should -BeTrue
        }
    }
}
finally {
    $global:PSDefaultParameterValues = $originalDefaultParameterValues
}
