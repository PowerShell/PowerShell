# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Module removed due to #4272
# disabling tests

return

Set-Variable dateInFuture -Option Constant -Value "12/12/2036 09:00"
Set-Variable dateInPast -Option Constant -Value "12/12/2010 09:00"
Set-Variable dateInvalid -Option Constant -Value "12/12/2016 25:00"

function RemoveTestUsers
{
    param([string] $basename)

    $results = Get-LocalUser $basename*
    foreach ($element in $results) {
        Remove-LocalUser -SID $element.SID
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

    Describe "Verify Expected LocalUser Cmdlets are present" -Tags 'CI' {

        It "Test command presence" {
            $result = Get-Command -Module Microsoft.PowerShell.LocalAccounts | ForEach-Object Name

            $result -contains "New-LocalUser" | Should -BeTrue
            $result -contains "Set-LocalUser" | Should -BeTrue
            $result -contains "Get-LocalUser" | Should -BeTrue
            $result -contains "Rename-LocalUser" | Should -BeTrue
            $result -contains "Remove-LocalUser" | Should -BeTrue
            $result -contains "Enable-LocalUser" | Should -BeTrue
            $result -contains "Disable-LocalUser" | Should -BeTrue
        }
    }

    Describe "Verify Expected LocalUser Aliases are present" -Tags @('CI', 'RequireAdminOnWindows') {

        It "Test command presence" {
            $result = Get-Alias | ForEach-Object { if ($_.Source -eq "Microsoft.PowerShell.LocalAccounts") {$_}}

            $result.Name -contains "algm" | Should -BeTrue
            $result.Name -contains "dlu" | Should -BeTrue
            $result.Name -contains "elu" | Should -BeTrue
            $result.Name -contains "glg" | Should -BeTrue
            $result.Name -contains "glgm" | Should -BeTrue
            $result.Name -contains "glu" | Should -BeTrue
            $result.Name -contains "nlg" | Should -BeTrue
            $result.Name -contains "nlu" | Should -BeTrue
            $result.Name -contains "rlg" | Should -BeTrue
            $result.Name -contains "rlgm" | Should -BeTrue
            $result.Name -contains "rlu" | Should -BeTrue
            $result.Name -contains "rnlg" | Should -BeTrue
            $result.Name -contains "rnlu" | Should -BeTrue
            $result.Name -contains "slg" | Should -BeTrue
            $result.Name -contains "slu" | Should -BeTrue
        }
    }

    Describe "Validate simple New-LocalUser" -Tags @('CI', 'RequireAdminOnWindows') {

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestUsers -basename TestUserNew
            }
        }

        It "Can create New-LocalUser with only name" {
            $result = New-LocalUser TestUserNew1 -NoPassword

            $result.Name | Should -BeExactly TestUserNew1
            $result.Description | Should -BeNullOrEmpty
            $result.Enabled | Should -BeTrue
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
        }
    }

    Describe "Validate New-LocalUser cmdlet" -Tags @('Feature', 'RequireAdminOnWindows') {

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestUsers -basename TestUserNew
                RemoveTestUsers -basename "S-1-5-32-545"
            }
        }

        It "Can set a SID like name" {
            $userName = "S-1-5-32-545"
            $result = New-LocalUser $userName -NoPassword

            $result.Name | Should -BeExactly $userName
            $result.Description | Should -BeNullOrEmpty
            $result.Enabled | Should -BeTrue
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
        }

        It "Errors on Name argument of empty string or null" {
            $sb = {
                New-LocalUser -Name "" -NoPassword
            }
            VerifyFailingTest $sb "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.NewLocalUserCommand"

            $sb = {
                New-LocalUser -Name $null -NoPassword
            }
            VerifyFailingTest $sb "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.NewLocalUserCommand"
        }

        It "Errors on Invalid characters" {
            #Arrange
            #list of characters that should be invalid
            $InvalidCharacters = @"
\/"[]:|<>+=;,?*
"@
            $InvalidCharacters = $InvalidCharacters[0..($InvalidCharacters.Length - 1)]
            #Act
            foreach ($character in $InvalidCharacters) {
                try {
                    $invalidUser = New-LocalUser -Name ("InvalidBecauseOf" + $character) -NoPassword -ErrorAction Stop
                }
                catch {
                }
                finally {
                    if ($invalidUser) {
                        Remove-LocalUser -Name $invalidUser
                        $failedCharacters += $character
                    }
                }
            }

            #Assert
            if ($failedCharacters.Count -gt 0) { Write-Host "characters causing test fail: $failedCharacters" }
            $failedCharacters.Count -eq 0 | Should -BeTrue
        }

        It "Errors on names containing only spaces or periods" {
            $sb = {
                New-LocalUser -Name "   " -NoPassword
            }
            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.NewLocalUserCommand"

            $sb = {
                New-LocalUser -Name "..." -NoPassword
            }
            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.NewLocalUserCommand"
        }

        It "Errors on names ending in a period" {
            $sb = {
                New-LocalUser -Name "TestEndInPeriod." -NoPassword
            }
            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.NewLocalUserCommand"

            $sb = {
                New-LocalUser -Name ".TestEndIn.Period.." -NoPassword
            }
            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.NewLocalUserCommand"
        }

        It "Errors on name collision" {
            $sb = {
                New-LocalUser TestUserNew1 -NoPassword
                New-LocalUser TestUserNew1 -NoPassword
            }
            VerifyFailingTest $sb "UserExists,Microsoft.PowerShell.Commands.NewLocalUserCommand"
        }

        It "Errors on Name over 20 characters" {
            $sb = {
                New-LocalUser -Name ("A"*21) -NoPassword
            }
            try {
                VerifyFailingTest $sb "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.NewLocalUserCommand"
            }
            finally {
                RemoveTestUsers -basename ("A"*21)
            }
        }

        It "Can set AccountExpires to the future" {
            $expiration = $dateInFuture
            $result = New-LocalUser TestUserNew1 -NoPassword -AccountExpires $expiration

            $result.Name | Should -BeExactly TestUserNew1
            $result.Description | Should -BeNullOrEmpty
            $result.Enabled | Should -BeTrue
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
            $result.AccountExpires | Should -Be ([DateTime]$expiration)
        }

        It "Can set AccountExpires to the past" {
            $expiration = $dateInPast
            $result = New-LocalUser TestUserNew1 -NoPassword -AccountExpires $expiration

            $result.Name | Should -BeExactly TestUserNew1
            $result.Description | Should -BeNullOrEmpty
            $result.Enabled | Should -BeTrue
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
            $result.AccountExpires | Should -Be ([DateTime]$expiration)
        }

        It "Errors on AccountExpires being set to invalid date" {
            $expiration = $dateInvalid
            $sb = {
                New-LocalUser TestUserNew1 -NoPassword -AccountExpires $expiration
            }
            VerifyFailingTest $sb "CannotConvertArgumentNoMessage,Microsoft.PowerShell.Commands.NewLocalUserCommand"
        }

        It "Can set AccountNeverExpires to create a user with null for AccountExpires date" {
            $result = New-LocalUser TestUserNew1 -NoPassword -AccountNeverExpires

            $result.Name | Should -BeExactly TestUserNew1
            $result.Description | Should -BeNullOrEmpty
            $result.Enabled | Should -BeTrue
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
            $result.AccountExpires | Should -BeNullOrEmpty
        }

         It "Errors on both AccountExpires and AccountNeverExpires being set" {
            $sb = {
                New-LocalUser TestUserNew1 -NoPassword -AccountExpires $dateInFuture -AccountNeverExpires
            }
            VerifyFailingTest $sb "InvalidParameters,Microsoft.PowerShell.Commands.NewLocalUserCommand"
        }

        It "Can set empty string for Description" {
            $result = New-LocalUser TestUserNew1 -NoPassword -Description ""

            $result.Name | Should -BeExactly TestUserNew1
            $result.Description | Should -BeExactly ""
            $result.Enabled | Should -BeTrue
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
        }

        It "Can set with description at max 48" {
            $result = New-LocalUser TestUserNew1 -NoPassword -Description ("A"*48)

            $result.Name | Should -BeExactly TestUserNew1
            $result.Description | Should -BeExactly ("A"*48)
            $result.Enabled | Should -BeTrue
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
        }

        It "Can set with description over max 48" {
            $result = New-LocalUser TestUserNew1 -NoPassword -Description ("A"*257)

            $result.Name | Should -BeExactly TestUserNew1
        }

        It "Enabled is true by default" {
            $result = New-LocalUser TestUserNew1 -NoPassword

            $result.Name | Should -BeExactly TestUserNew1
            $result.Description | Should -BeNullOrEmpty
            $result.Enabled | Should -BeTrue
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
        }

        It "Can set enabled to false" {
            $result = New-LocalUser TestUserNew1 -NoPassword -Disabled

            $result.Name | Should -BeExactly TestUserNew1
            $result.Description | Should -BeNullOrEmpty
            $result.Enabled | Should -BeFalse
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
        }

        It "Can set empty string for FullName" {
            $result = New-LocalUser TestUserNew1 -NoPassword -FullName ""

            $result.Name | Should -BeExactly TestUserNew1
            $result.Description | Should -BeNullOrEmpty
            $result.Enabled | Should -BeTrue
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
            $result.FullName | Should -BeNullOrEmpty
        }

        It "Can set string for FullName at 256 characters" {
            $result = New-LocalUser TestUserNew1 -NoPassword -FullName ("A"*256)

            $result.Name | Should -BeExactly TestUserNew1
            $result.Description | Should -BeNullOrEmpty
            $result.Enabled | Should -BeTrue
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
            $result.FullName | Should -BeExactly ("A"*256)
        }

        It "Errors when Password is an empty string" {
            $sb = {
                New-LocalUser TestUserNew1 -Password (ConvertTo-SecureString "" -AsPlainText -Force)
            }
            VerifyFailingTest $sb "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.ConvertToSecureStringCommand"
        }

        It "Can set Password value at max 256" {
            #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc/test secret.")]
            $result = New-LocalUser TestUserNew1 -Password (ConvertTo-SecureString ("135@"+"A"*252) -AsPlainText -Force)

            $result.Name | Should -BeExactly TestUserNew1
            $result.Description | Should -BeNullOrEmpty
            $result.Enabled | Should -BeTrue
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
        }

        It "Errors when Password over max 257" {
            $sb = {
                New-LocalUser TestUserNew1 -Password (ConvertTo-SecureString ("A"*257) -AsPlainText -Force)
            }
            VerifyFailingTest $sb "InvalidPassword,Microsoft.PowerShell.Commands.NewLocalUserCommand"
        }

        It "User should not be created when invalid password is provided" {
            $sb = {
                New-LocalUser TestUserNew1 -Password (ConvertTo-SecureString ("A"*257) -AsPlainText -Force)
            }
            VerifyFailingTest $sb "InvalidPassword,Microsoft.PowerShell.Commands.NewLocalUserCommand"
            $sb1 = {
                Get-LocalUser TestUserNew1
            }
            VerifyFailingTest $sb1 "UserNotFound,Microsoft.PowerShell.Commands.GetLocalUserCommand"
        }

        It "Can set UserMayNotChangePassword" {
            $result = New-LocalUser TestUserNew1 -NoPassword -UserMayNotChangePassword

            $result.Name | Should -BeExactly TestUserNew1
            $result.Description | Should -BeNullOrEmpty
            $result.Enabled | Should -BeTrue
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
            $result.UserMayChangePassword | Should -BeFalse
        }

        It "Can set PasswordNeverExpires to create a user with null for PasswordExpires date" {
            #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc/test secret.")]
            $result = New-LocalUser TestUserNew1 -Password (ConvertTo-SecureString "p@ssw0rd" -AsPlainText -Force) -PasswordNeverExpires

            $result.Name | Should -BeExactly TestUserNew1
            $result.PasswordExpires | Should -BeNullOrEmpty
        }

        It "Errors on both NoPassword and PasswordNeverExpires being set" {
            $sb = {
                New-LocalUser TestUserNew1 -NoPassword -PasswordNeverExpires
            }
            VerifyFailingTest $sb "AmbiguousParameterSet,Microsoft.PowerShell.Commands.NewLocalUserCommand"
        }

        It "UserMayChangePassword is true by default" {
            $result = New-LocalUser TestUserNew1 -NoPassword

            $result.Name | Should -BeExactly TestUserNew1
            $result.Description | Should -BeNullOrEmpty
            $result.Enabled | Should -BeTrue
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
            $result.UserMayChangePassword | Should -BeTrue
        }
    }

    Describe "Validate simple Get-LocalUser" -Tags @('CI', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                New-LocalUser -Name TestUserGet1 -NoPassword -Description "Test User Get 1 Description" | Out-Null
                New-LocalUser -Name TestUserGet2 -NoPassword -Description "Test User Get 2 Description" | Out-Null
            }
        }

        AfterAll {
            if ($IsNotSkipped) {
                RemoveTestUsers -basename TestUserGet
            }
        }

        It "Can Get-LocalUser by only name" {
            $result = Get-LocalUser TestUserGet1

            $result.Name | Should -Be "TestUserGet1"
            $result.Description | Should -Be "Test User Get 1 Description"
            $result.ObjectClass | Should -Be "User"
        }
    }

    Describe "Validate Get-LocalUser cmdlet" -Tags @('Feature', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                New-LocalUser -Name TestUserGet1 -NoPassword -Description "Test User Get 1 Description" | Out-Null
                New-LocalUser -Name TestUserGet2 -NoPassword -Description "Test User Get 2 Description" | Out-Null
            }
        }

        AfterAll {
            if ($IsNotSkipped) {
                RemoveTestUsers -basename TestUserGet
            }
        }

        It "Get-LocalUser gets all users"  {
            $result = Get-LocalUser

            $result.Count -gt 2 | Should -BeTrue
        }

        It "Can get a specific user by SID" {
            $result = Get-LocalUser TestUserGet1
            $resultBySID = Get-LocalUser -SID $result.SID

            $resultBySID.SID | Should -Not -BeNullOrEmpty
            $resultBySID.Name | Should -Be TestUserGet1
        }

        It "Can get a well-known user by SID string" {
            $sid = New-Object System.Security.Principal.SecurityIdentifier -ArgumentList LG
            $guestUser = Get-LocalUser -SID LG

            $guestUser.SID | Should -Be $sid.Value
        }

        It "Can get users by wildcard" {
            $result = Get-LocalUser TestUserGet*

            $result.Count -eq 2 | Should -BeTrue
            $result.Name -contains "TestUserGet1" | Should -BeTrue
            $result.Name -contains "TestUserGet2" | Should -BeTrue
        }

        It "Can get a user by array of names" {
            $result = Get-LocalUser @("TestUserGet1", "TestUserGet2")

            $result.Count -eq 2 | Should -BeTrue
            $result.Name -contains "TestUserGet1" | Should -BeTrue
            $result.Name -contains "TestUserGet2" | Should -BeTrue
        }

        It "Can get a user by array of SIDs" {
            $sid1 = (Get-LocalUser TestUserGet1).SID
            $sid2 = (Get-LocalUser TestUserGet2).SID
            $result = Get-LocalUser -SID @($sid1, $sid2)

            $result.Count -eq 2 | Should -BeTrue
            $result.Name -contains "TestUserGet1" | Should -BeTrue
            $result.Name -contains "TestUserGet2" | Should -BeTrue
        }

        It "Can respond to -ErrorAction Stop" {
            Try {
                Get-LocalUser @("TestUserGet1", "TestUserGetNameThatDoesntExist1", "TestUserGetNameThatDoesntExist2") -ErrorAction Stop -ErrorVariable outErr -OutVariable outOut | Out-Null
            }
            Catch {
                # Ignore the exception
            }
            $outErr.Count -eq 1 | Should -BeTrue
            $outErr[0].ErrorRecord.CategoryInfo.Reason -match "UserNotFound" | Should -BeTrue
            $outOut.Name -match "TestUserGet1" | Should -BeTrue
        }

        It "Error on Name not being supplied an argument" {
            $sb = {
                Get-LocalUser -Name
            }
            VerifyFailingTest $sb "MissingArgument,Microsoft.PowerShell.Commands.GetLocalUserCommand"
        }

        It "Error on SID not being supplied an argument" {
            $sb = {
                Get-LocalUser -SID
            }
            VerifyFailingTest $sb "MissingArgument,Microsoft.PowerShell.Commands.GetLocalUserCommand"
        }

        It "Error on both -Name and -SID being supplied at the same time" {
            $sb = {
                Get-LocalUser -Name TestUserGet1 -SID (Get-LocalUser TestUserGet1).SID
            }
            VerifyFailingTest $sb "AmbiguousParameterSet,Microsoft.PowerShell.Commands.GetLocalUserCommand"
        }

        It "Errors on a non-existant user by name" {
            $sb = {
                Get-LocalUser 'TestUserGetNameThatDoesntExist'
            }
            VerifyFailingTest $sb "UserNotFound,Microsoft.PowerShell.Commands.GetLocalUserCommand"
        }

        It "Errors on a non-existant user by SID" {
            $sb = {
                New-LocalUser -Name TestUserGet3 -NoPassword -Description "Test User Get 3 Description"
                $sid = (Get-LocalUser -Name TestUserGet3).SID
                Remove-LocalUser TestUserGet3
                Get-LocalUser -SID $sid
            }
            VerifyFailingTest $sb "UserNotFound,Microsoft.PowerShell.Commands.GetLocalUserCommand"
        }

        It "Gets no results using a wildcard" {
            $localUserName = 'TestUserGetNameThatDoesntExist'
            $result = Get-LocalGroup $localUserName*

            $result | Should -Be $null
        }

        It "Returns the correct property values of a user" {
            $Name = "TestUserGet3"
            $AccountExpires = $dateInFuture
            $Description = "Describe"
            $FullName = $Name
            $ObjectClass = "User"
            # TODO $LastLogon
            # TODO $PasswordExpires
            # TODO $PasswordLastSet
            # TODO $PasswordRequired
            # TODO $PrincipalSource

            $result = New-LocalUser TestUserGet3 -NoPassword -AccountExpires $AccountExpires -Description $Description -Disabled -FullName $FullName -UserMayNotChangePassword

            $result.Name | Should -BeExactly $Name
            $result.AccountExpires | Should -Be ([DateTime]$AccountExpires)
            $result.Description | Should -BeExactly $Description
            $result.Enabled | Should -BeFalse
            $result.FullName | Should -BeExactly $FullName
            $result.ObjectClass -eq "User" | Should -BeTrue
            $result.UserMayChangePassword | Should -BeFalse
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
        }
    }

    Describe "Validate simple Set-LocalUser" -Tags @('CI', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                $user1SID = ""
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                New-LocalUser -Name TestUserSet1 -NoPassword -Description "Test User Set 1 Description" | Out-Null
                $user1SID = [String](Get-LocalUser -Name TestUserSet1).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestUsers -basename TestUserSet
                $user1SID = ""
            }
        }

        It "Can Set-LocalUser description by only name" {
            Set-LocalUser -Name TestUserSet1 -Description "Test User Set 1 new description"
            $result = Get-LocalUser -Name TestUserSet1

            $result.Description | Should -BeExactly "Test User Set 1 new description"
        }
    }

    Describe "Validate Set-LocalUser cmdlet" -Tags @('Feature', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                $user1SID = ""
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                New-LocalUser -Name TestUserSet1 -NoPassword -Description "Test User Set 1 Description" | Out-Null
                $user1SID = [String](Get-LocalUser -Name TestUserSet1).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestUsers -basename TestUserSet
                $user1SID = ""
            }
        }

        It "Can set user description by SID" {
            Set-LocalUser -SID $user1SID -Description "Test User Set 1 new description"
            $result = Get-LocalUser -Name TestUserSet1

            $result.Description | Should -BeExactly "Test User Set 1 new description"
        }

        It "Can set user description by -InputObject" {
            $user = Get-LocalUser -Name TestUserSet1
            Set-LocalUser -InputObject $user -Description "Test User Set 1 new description"
            $result = Get-LocalUser -Name TestUserSet1

            $result.Description | Should -BeExactly "Test User Set 1 new description"
        }

        It "Can set user description by pipeline" {
            Get-LocalUser -Name TestUserSet1 | Set-LocalUser -Description "Test User Set 1 new description"
            $result = Get-LocalUser -Name TestUserSet1

            $result.Description | Should -BeExactly "Test User Set 1 new description"
        }

        It "Errors on nonexistent user name" {
            $sb = {
                Set-LocalUser -Name TestUserSetNonexistent1 -Description "Test User Set 1 new description" -ErrorAction Stop
            }
            VerifyFailingTest $sb "UserNotFound,Microsoft.PowerShell.Commands.SetLocalUserCommand"
        }

        It "Errors on nonexistent SID" {
            $sb = {
                Set-LocalUser -SID "S-1-5-32-545" -Description "Test User Set 1 new description" -ErrorAction Stop
            }
            VerifyFailingTest $sb "UserNotFound,Microsoft.PowerShell.Commands.SetLocalUserCommand"
        }

        It "Can set AccountExpires to the future" {
            $expiration = $dateInFuture
            Set-LocalUser -Name TestUserSet1 -AccountExpires $expiration
            $result = Get-LocalUser -Name TestUserSet1

            $result.Name | Should -BeExactly TestUserSet1
            $result.Description | Should -BeExactly "Test User Set 1 Description"
            $result.Enabled | Should -BeTrue
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
            $result.AccountExpires | Should -Be ([DateTime]$expiration)
        }

        It "Can set AccountExpires to the past" {
            $expiration = $dateInPast
            Set-LocalUser -Name TestUserSet1 -AccountExpires $expiration
            $result = Get-LocalUser -Name TestUserSet1

            $result.Name | Should -BeExactly TestUserSet1
            $result.Description | Should -BeExactly "Test User Set 1 Description"
            $result.Enabled | Should -BeTrue
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
            $result.AccountExpires | Should -Be ([DateTime]$expiration)
        }

        It "Errors on AccountExpires being set to invalid date" {
            $expiration = $dateInvalid
            $sb = {
                Set-LocalUser TestUserSet1 -AccountExpires $expiration
            }
            VerifyFailingTest $sb "CannotConvertArgumentNoMessage,Microsoft.PowerShell.Commands.SetLocalUserCommand"
        }

        It "Can set AccountNeverExpires to create a user with null for AccountExpires date" {
            Set-LocalUser -Name TestUserSet1 -AccountExpires $dateInFuture
            Set-LocalUser -Name TestUserSet1 -AccountNeverExpires
            $result = Get-LocalUser -Name TestUserSet1

            $result.Name | Should -BeExactly TestUserSet1
            $result.AccountExpires | Should -BeNullOrEmpty
        }

        It "Errors on both AccountExpires and AccountNeverExpires being set" {
            $sb = {
                Set-LocalUser TestUserSet1 -AccountExpires $dateInFuture -AccountNeverExpires
            }
            VerifyFailingTest $sb "InvalidParameters,Microsoft.PowerShell.Commands.SetLocalUserCommand"
        }

        It "Can set user description to empty string" {
            Set-LocalUser -Name TestUserSet1 -Description ""
            $result = Get-LocalUser -Name TestUserSet1

            $result.Description | Should -BeExactly ""
        }

        It "Can set empty string for Description" {
            Set-LocalUser -Name TestUserSet1 -Description ""
            $result = Get-LocalUser -Name TestUserSet1

            $result.Description | Should -BeExactly ""
        }

        It "Can set string for Description at max 48" {
            Set-LocalUser TestUserSet1 -Description ("A"*48)
            $result = Get-LocalUser TestUserSet1

            $result.Name | Should -BeExactly TestUserSet1
            $result.Description | Should -BeExactly ("A"*48)
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
        }

        It "Can set empty string for FullName" {
            Set-LocalUser -Name TestUserSet1 -FullName ""
            $result = Get-LocalUser -Name TestUserSet1

            $result.FullName | Should -BeExactly ""
        }

        It "Can set string for FullName at 256" {
            Set-LocalUser TestUserSet1 -FullName ("A"*256)
            $result = Get-LocalUser TestUserSet1

            $result.Name | Should -BeExactly TestUserSet1
            $result.FullName | Should -BeExactly ("A"*256)
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
        }

        It "Errors when Password is an empty string" {
            $sb = {
                Set-LocalUser -Name TestUserSet1 -Password (ConvertTo-SecureString "" -AsPlainText -Force)
            }
            VerifyFailingTest $sb "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.ConvertToSecureStringCommand"
        }

        It "Errors when Password is null" {
            $sb = {
                Set-LocalUser -Name TestUserSet1 -Password (ConvertTo-SecureString $null -AsPlainText -Force)
            }
            VerifyFailingTest $sb "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.ConvertToSecureStringCommand"
        }

        It "Can set Password value at max 256" {
            #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc/test secret.")]
            Set-LocalUser -Name TestUserSet1 -Password (ConvertTo-SecureString ("123@"+"A"*252) -AsPlainText -Force)
            $result = Get-LocalUser -Name TestUserSet1

            $result.Name | Should -BeExactly TestUserSet1
            $result.Enabled | Should -BeTrue
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
        }

        It "Errors when Password over max 257" {
            $sb = {
                Set-LocalUser -Name TestUserSet1 -Password (ConvertTo-SecureString ("A"*257) -AsPlainText -Force) -ErrorAction Stop
            }
            VerifyFailingTest $sb "InvalidPassword,Microsoft.PowerShell.Commands.SetLocalUserCommand"
        }

        It 'Can use PasswordNeverExpires:$true to null a PasswordExpires date' {
            #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc/test secret.")]
            $user = New-LocalUser TestUserSet2 -Password (ConvertTo-SecureString "p@ssw0rd" -AsPlainText -Force)
            $user | Set-LocalUser -PasswordNeverExpires:$true
            $result = Get-LocalUser TestUserSet2

            $result.Name | Should -BeExactly TestUserSet2
            $result.PasswordExpires | Should -BeNullOrEmpty
        }

        It 'Can use PasswordNeverExpires:$false to activate a PasswordExpires date' {
            #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc/test secret.")]
            $user = New-LocalUser TestUserSet2 -Password (ConvertTo-SecureString "p@ssw0rd" -AsPlainText -Force) -PasswordNeverExpires
            $user | Set-LocalUser -PasswordNeverExpires:$false
            $result = Get-LocalUser TestUserSet2

            $result.Name | Should -BeExactly TestUserSet2
            $result.PasswordExpires | Should -Not -BeNullOrEmpty
        }

        It "Can set UserMayChangePassword to true" {
            Set-LocalUser TestUserSet1 -UserMayChangePassword $true
            $result = Get-LocalUser -Name TestUserSet1

            $result.Name | Should -BeExactly TestUserSet1
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
            $result.UserMayChangePassword | Should -BeTrue
        }

        It "Can set UserMayChangePassword to false" {
            Set-LocalUser TestUserSet1 -UserMayChangePassword $false
            $result = Get-LocalUser -Name TestUserSet1

            $result.Name | Should -BeExactly TestUserSet1
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be User
            $result.UserMayChangePassword | Should -BeFalse
        }
    }

    Describe "Validate simple Rename-LocalUser" -Tags @('CI', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                $user1SID = ""
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                New-LocalUser -Name TestUserRename1 -NoPassword -Description "Test User Rename 1 Description" | Out-Null
                $user1SID = [String](Get-LocalUser -Name TestUserRename1).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestUsers -basename TestUserRename
                $user1SID = ""
            }
        }

        It "Can Rename-LocalUser by only name" {
            Rename-LocalUser -Name TestUserRename1 -NewName TestUserRename2
            $result = Get-LocalUser -SID $user1SID

            $result.Name | Should -BeExactly TestUserRename2
        }
    }

    Describe "Validate Rename-LocalUser cmdlet" -Tags @('Feature', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                $user1SID = ""
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                New-LocalUser -Name TestUserRename1 -NoPassword -Description "Test User Rename 1 Description" | Out-Null
                $user1SID = [String](Get-LocalUser -Name TestUserRename1).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestUsers -basename TestUserRename
                $user1SID = ""
            }
        }

        It "Can rename by SID" {
            Rename-LocalUser -SID $user1SID -NewName TestUserRename2
            $result = Get-LocalUser -SID $user1SID

            $result.Name | Should -BeExactly TestUserRename2
        }

        It "Can rename using -InputObject" {
            $user = Get-LocalUser -SID $user1SID
            Rename-LocalUser -InputObject $user -NewName TestUserRename2
            $result = Get-LocalUser -SID $user1SID

            $result.Name | Should -BeExactly TestUserRename2
            $result.SID | Should -BeExactly $user1SID
        }

        It "Can rename using pipeline" {
            Get-LocalUser -SID $user1SID | Rename-LocalUser -NewName TestUserRename2
            $result = Get-LocalUser -SID $user1SID

            $result.Name | Should -BeExactly TestUserRename2
            $result.SID | Should -BeExactly $user1SID
        }

        It "Errors on no name or SID specified" {
            $sb = {
                Rename-LocalUser
            }
            VerifyFailingTest $sb "AmbiguousParameterSet,Microsoft.PowerShell.Commands.RenameLocalUserCommand"
        }

        It "Errors on nonexistent user name" {
            $sb = {
                Rename-LocalUser -Name TestUserRenameThatDoesntExist -NewName TestUserRenameThatDoesntExist2
            }
            VerifyFailingTest $sb "UserNotFound,Microsoft.PowerShell.Commands.RenameLocalUserCommand"
        }

        It "Errors on nonexistent user SID" {
            $sb = {
                Remove-LocalUser -SID $user1SID
                Rename-LocalUser -SID $user1SID -NewName TestUserRename2
            }
            VerifyFailingTest $sb "UserNotFound,Microsoft.PowerShell.Commands.RenameLocalUserCommand"
        }

        It "Errors on rename of user to existing user, name collision" {
            $sb = {
                New-LocalUser TestUserRename4 -NoPassword
                Rename-LocalUser -Name TestUserRename1 -NewName TestUserRename4
            }
            try {
                VerifyFailingTest $sb "NameInUse,Microsoft.PowerShell.Commands.RenameLocalUserCommand"
            }
            finally {
                RemoveTestUsers -basename TestUserRename4
            }
        }

        It "Errors on Invalid characters" {
            #Arrange
            #list of characters that should be invalid
            $InvalidCharacters = @"
\/"[]:|<>+=;,?*
"@
            $failedCharacters = @()
            $InvalidCharacters = $InvalidCharacters[0..($InvalidCharacters.Length - 1)]

            #Act
            foreach ($character in $InvalidCharacters) {
                try {
                    Rename-LocalUser -Name TestUserRename1 -NewName ("InvalidBecauseOf" + $character) -ErrorAction Stop
                    $invalidUser = (Get-LocalUser ("InvalidBecauseOf" + $character))
                }
                catch {
                }
                finally {
                    #handle users being erroneously renamed
                    if ($invalidUser) {
                        Rename-LocalUser -Name ("InvalidBecauseOf" + $character) -NewName TestUserRename1
                        $failedCharacters += $character
                    }
                }
            }

            #Assert
            if ($failedCharacters.Count -gt 0) { Write-Host "characters causing test fail: $failedCharacters" }
            $failedCharacters.Count -eq 0 | Should -BeTrue
        }

        It "Errors on names containing only spaces or periods" {
            $sb = {
                Rename-LocalUser -Name TestUserRename1 -NewName "..."
            }
            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.RenameLocalUserCommand"

            $sb = {
                Rename-LocalUser -Name TestUserRename1 -NewName "   "
            }
            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.RenameLocalUserCommand"
        }

        It "Errors on names ending in a period" {
            $sb = {
                Rename-LocalUser -Name TestUserRename1 -NewName "TestEndInPeriod."
            }
            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.RenameLocalUserCommand"

            $sb = {
                Rename-LocalUser -Name TestUserRename1 -NewName ".TestEndIn.Period.."
            }
            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.RenameLocalUserCommand"
        }

        It "Can rename by only name" {
            Rename-LocalUser -Name TestUserRename1 -NewName TestUserRename2
            $result = Get-LocalUser -SID $user1SID

            $result.Name | Should -BeExactly TestUserRename2
        }

        It "Errors when NewName over max 20" {
            $sb = {
                Rename-LocalUser -Name TestUserRename1 -NewName ("A"*21)
            }
            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.RenameLocalUserCommand"
        }
    }

    Describe "Validate simple Remove-LocalUser" -Tags @('CI', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                $user1SID = ""
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                New-LocalUser -Name TestUserRemove1 -NoPassword -Description "Test User Remove 1 Description" | Out-Null
                $user1SID = [String](Get-LocalUser -Name TestUserRemove1).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestUsers -basename TestUserRemove
                $user1SID = ""
            }
        }

        It "Can Remove-LocalUser with only name" {
            $initialCount = (Get-LocalUser).Count
            $initialCount -gt 1 | Should -BeTrue

            $removeResult = Remove-LocalUser TestUserRemove1 2>&1
            $removeResult | Should -BeNullOrEmpty

            $sb = {
                Get-LocalUser -SID $user1SID
            }
            VerifyFailingTest $sb "UserNotFound,Microsoft.PowerShell.Commands.GetLocalUserCommand"

            $finalCount = (Get-LocalUser).Count
            $initialCount -eq $finalCount + 1 | Should -BeTrue
        }
    }

    Describe "Validate Remove-LocalUser cmdlet" -Tags @('Feature', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                $user1SID = ""
                $user2SID = ""

                function VerifyBasicRemoval {
                    param (
                        [scriptblock]$removalAction
                    )
                    $initialCount = (Get-LocalUser).Count
                    $initialCount -gt 1 | Should -BeTrue

                    & $removalAction

                    $sb = {
                        Get-LocalUser -SID $user1SID
                    }
                    VerifyFailingTest $sb "UserNotFound,Microsoft.PowerShell.Commands.GetLocalUserCommand"

                    $finalCount = (Get-LocalUser).Count
                    $initialCount -eq $finalCount + 1 | Should -BeTrue
                }

                function VerifyArrayRemoval {
                    param (
                        [scriptblock]$removalAction
                    )
                    $initialCount = (Get-LocalUser).Count
                    $initialCount -gt 1 | Should -BeTrue

                    & $removalAction

                    $sb = {
                        Get-LocalUser -SID $user1SID
                    }
                    VerifyFailingTest $sb "UserNotFound,Microsoft.PowerShell.Commands.GetLocalUserCommand"

                    $sb = {
                        Get-LocalUser -SID $user2SID
                    }
                    VerifyFailingTest $sb "UserNotFound,Microsoft.PowerShell.Commands.GetLocalUserCommand"

                    $finalCount = (Get-LocalUser).Count
                    $initialCount -eq $finalCount + 2 | Should -BeTrue
                }
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                $user1SID = [String](New-LocalUser -Name TestUserRemove1 -NoPassword -Description "Test User Remove 1 Description").SID
                $user2SID = [String](New-LocalUser -Name TestUserRemove2 -NoPassword -Description "Test User Remove 2 Description").SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestUsers -basename TestUserRemove
                $user1SID = ""
                $user2SID = ""
            }
        }

        It "Can remove by SID" {
            $user1SID | Should -Not -BeNullOrEmpty
            $sb = {
                $result = Remove-LocalUser -SID $user1SID 2>&1
                $result | Should -BeNullOrEmpty
            }
            VerifyBasicRemoval $sb
        }

        It "Can remove using -InputObject" {
            $sb = {
                $user = Get-LocalUser -SID $user1SID
                $result = Remove-LocalUser -InputObject $user 2>&1
                $result | Should -BeNullOrEmpty
            }
            VerifyBasicRemoval $sb
        }

        It "Can remove using pipeline" {
            $sb = {
                $result = Get-LocalUser -SID $user1SID | Remove-LocalUser 2>&1
                $result | Should -BeNullOrEmpty
            }
            VerifyBasicRemoval $sb
        }

        It "Errors on no name or SID specified" {
            $sb = {
                Remove-LocalUser
            }
            VerifyFailingTest $sb "AmbiguousParameterSet,Microsoft.PowerShell.Commands.RemoveLocalUserCommand"
        }

        It "Can remove by array of names" {
            $sb = {
                $result = Remove-LocalUser @("TestUserRemove1", "TestUserRemove2") 2>&1
                $result | Should -BeNullOrEmpty
            }
            VerifyArrayRemoval $sb
        }

        It "Can remove by array of SIDs" {
            $sb = {
                $result = Remove-LocalUser -SID @($user1SID, $user2SID) 2>&1
                $result | Should -BeNullOrEmpty
            }
            VerifyArrayRemoval $sb
        }

        It "Can remove by array using -InputObject" {
            $sb = {
                $users = Get-LocalUser @("TestUserRemove1", "TestUserRemove2")
                $results = Remove-LocalUser -InputObject $users 2>&1
                $result | Should -BeNullOrEmpty
            }
            VerifyArrayRemoval $sb
        }

        It "Can remove by array using pipeline" {
            $sb = {
                $result = Get-LocalUser @("TestUserRemove1", "TestUserRemove2") | Remove-LocalUser 2>&1
                $result | Should -BeNullOrEmpty
            }
            VerifyArrayRemoval $sb
        }

        It "Errors on remove by invalid name" {
            $initialCount =  (Get-LocalUser).Count
            $initialCount -gt 1 | Should -BeTrue

            $sb = {
                Remove-LocalUser TestUserRemove1NameThatDoesntExist
            }
            VerifyFailingTest $sb "UserNotFound,Microsoft.PowerShell.Commands.RemoveLocalUserCommand"

            $finalCount = (Get-LocalUser).Count
            $initialCount -eq $finalCount | Should -BeTrue
        }

        It "Errors on remove by invalid SID" {
            Remove-LocalUser -SID $user1SID
            # This test verifies that it cannot be removed a second time
            $initialCount =  (Get-LocalUser).Count
            $initialCount -gt 1 | Should -BeTrue

            $sb = {
                Remove-LocalUser -SID $user1SID
            }
            VerifyFailingTest $sb "UserNotFound,Microsoft.PowerShell.Commands.RemoveLocalUserCommand"

            $finalCount = (Get-LocalUser).Count
            $initialCount -eq $finalCount | Should -BeTrue
        }

        It "Can respond to -ErrorAction Stop" {
            try {
                Remove-LocalUser @("TestUserGet1", "TestUserGetNameThatDoesntExist1", "TestUserGetNameThatDoesntExist2") -ErrorAction Stop -ErrorVariable outError | Out-Null
            }
            catch {
                # Nothing to do here
            }
            $outError.Count | Should -Be 2
            $outError[0].ErrorRecord.FullyQualifiedErrorId | Should -Be "UserNotFound,Microsoft.PowerShell.Commands.RemoveLocalUserCommand"

            $getResult = Get-LocalUser TestUserGet1 2>&1
            $getResult.FullyQualifiedErrorId -match "UserNotFound" | Should -BeTrue
        }
    }

    Describe "Validate simple Enable-LocalUser" -Tags @('CI', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                $disabledUser1SID = ""
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                New-LocalUser -Name TestUserDisabled1 -NoPassword -Disabled -Description "Test User Disabled 1 Description" | Out-Null
                $disabledUser1SID = [String](Get-LocalUser -Name TestUserDisabled1).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                Remove-LocalUser -SID $disabledUser1SID
                $disabledUser1SID = ""
            }
        }

        It "Can Enable-LocalUser that is disabled by name" {
            Enable-LocalUser TestUserDisabled1
            $result = Get-LocalUser TestUserDisabled1

            $result.Enabled | Should -BeTrue
        }
    }

    Describe "Validate Enable-LocalUser cmdlet" -Tags @('Feature', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                $enabledUser1SID = ""
                $enabledUser2SID = ""
                $disabledUser1SID = ""
                $disabledUser2SID = ""
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                $enabledUser1SID = [String](New-LocalUser -Name TestUserEnabled1 -NoPassword -Description "Test User Enabled 1 Description").SID
                $enabledUser2SID = [String](New-LocalUser -Name TestUserEnabled2 -NoPassword -Description "Test User Enabled 2 Description").SID
                $disabledUser1SID = [String](New-LocalUser -Name TestUserDisabled1 -NoPassword -Description "Test User Disabled 1 Description" -Disabled).SID
                $disabledUser2SID = [String](New-LocalUser -Name TestUserDisabled2 -NoPassword -Description "Test User Disabled 2 Description" -Disabled).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestUsers -basename TestUserEnabled
                RemoveTestUsers -basename TestUserDisabled

                $enabledUser1SID = ""
                $enabledUser2SID = ""
                $disabledUser1SID = ""
                $disabledUser2SID = ""
            }
        }

        It "Can enable a disabled user by SID" {
            Enable-LocalUser -SID $disabledUser1SID
            $result = Get-LocalUser -SID $disabledUser1SID

            $result.Enabled | Should -BeTrue
        }

        It "Can enable a disabled user using -InputObject" {
            $user = Get-LocalUser TestUserDisabled1
            Enable-LocalUser -InputObject $user
            $result = Get-LocalUser TestUserDisabled1

            $result.Enabled | Should -BeTrue
        }

        It "Can enable a disabled user using pipeline" {
            Get-LocalUser TestUserDisabled1 | Enable-LocalUser
            $result = Get-LocalUser TestUserDisabled1

            $result.Enabled | Should -BeTrue
        }

        It "Can enable a disabled user by array of names" {
            Enable-LocalUser @("TestUserDisabled1", "TestUserDisabled2")

            (Get-LocalUser "TestUserDisabled1").Enabled | Should -BeTrue
            (Get-LocalUser "TestUserDisabled2").Enabled | Should -BeTrue
        }

        It "Can enable a disabled user by array of SIDs" {
            Enable-LocalUser -SID @($disabledUser1SID, $disabledUser2SID)

            (Get-LocalUser -SID $disabledUser1SID).Enabled | Should -BeTrue
            (Get-LocalUser -SID $disabledUser2SID).Enabled | Should -BeTrue
        }

        It "Can enable a disabled user by array sent using -InputObject" {
            $users = @((Get-LocalUser "TestUserDisabled1"), (Get-LocalUser "TestUserDisabled2"))
            Enable-LocalUser -InputObject $users

            (Get-LocalUser "TestUserDisabled1").Enabled | Should -BeTrue
            (Get-LocalUser "TestUserDisabled2").Enabled | Should -BeTrue
        }

        It "Can enable a disabled user by array sent using pipeline" {
            @((Get-LocalUser "TestUserDisabled1"), (Get-LocalUser "TestUserDisabled2")) | Enable-LocalUser

            (Get-LocalUser "TestUserDisabled1").Enabled | Should -BeTrue
            (Get-LocalUser "TestUserDisabled2").Enabled | Should -BeTrue
        }

        It "Errors on no name or SID specified" {
            $sb = {
                Enable-LocalUser
            }
            VerifyFailingTest $sb "AmbiguousParameterSet,Microsoft.PowerShell.Commands.EnableLocalUserCommand"
        }

        It "Can enable an already enabled user by name" {
            Enable-LocalUser TestUserEnabled1
            $result = Get-LocalUser TestUserEnabled1

            $result.Enabled | Should -BeTrue
        }

        It "Can enable an already enabled user by SID" {
            Enable-LocalUser -SID $enabledUser1SID
            $result = Get-LocalUser -SID $enabledUser1SID

            $result.Enabled | Should -BeTrue
        }

        It "Can enable an already enabled user using the pipeline" {
            Get-LocalUser TestUserEnabled1 | Enable-LocalUser
            $result = Get-LocalUser TestUserEnabled1

            $result.Enabled | Should -BeTrue
        }

        It "Errors on enabling an invalid user by name" {
            $sb = {
                Enable-LocalUser -Name TestUserEnableNameThatDoesntExist
            }
            VerifyFailingTest $sb "UserNotFound,Microsoft.PowerShell.Commands.EnableLocalUserCommand"
        }

        It "Errors on enabling an invalid user by SID" {
            $sb = {
                Remove-LocalUser -SID $enabledUser1SID
                Enable-LocalUser -SID $enabledUser1SID
            }
            VerifyFailingTest $sb "UserNotFound,Microsoft.PowerShell.Commands.EnableLocalUserCommand"
        }

        It "Can respond to -ErrorAction Stop" {
            Try {
                Enable-LocalUser @("TestUserDisabled1", "TestUserNameThatDoesntExist1", "TestUserNameThatDoesntExist2") -ErrorAction Stop -ErrorVariable outError | Out-Null
            }
            Catch {
                # do nothing
            }
            $outError.Count | Should -Be 2
            $outError[0].ErrorRecord.FullyQualifiedErrorId | Should -Be "UserNotFound,Microsoft.PowerShell.Commands.EnableLocalUserCommand"

            $getResult = Get-LocalUser TestUserDisabled1 2>&1
            $getResult.Enabled | Should -BeTrue
        }
    }

    Describe "Validate simple Disable-LocalUser" -Tags @('CI', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                $enabledUser1SID = ""
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                New-LocalUser -Name TestUserEnabled1 -NoPassword -Disabled -Description "Test User Enabled 1 Description" | Out-Null
                $enabledUser1SID = [String](Get-LocalUser -Name TestUserEnabled1).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                Remove-LocalUser -SID $enabledUser1SID
                $enabledUser1SID = ""
            }
        }

        It "Can Disable-LocalUser that is enabled by name" {
            Disable-LocalUser TestUserEnabled1
            $result = Get-LocalUser TestUserEnabled1

            $result.Enabled | Should -BeFalse
        }
    }

    Describe "Validate Disable-LocalUser cmdlet" -Tags @('Feature', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                $enabledUser1SID = ""
                $enabledUser2SID = ""
                $disabledUser1SID = ""
                $disabledUser2SID = ""
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                $enabledUser1SID = [String](New-LocalUser -Name TestUserEnabled1 -NoPassword -Description "Test User Enabled 1 Description").SID
                $enabledUser2SID = [String](New-LocalUser -Name TestUserEnabled2 -NoPassword -Description "Test User Enabled 2 Description").SID
                $disabledUser1SID = [String](New-LocalUser -Name TestUserDisabled1 -NoPassword -Description "Test User Disabled 1 Description" -Disabled).SID
                $disabledUser2SID = [String](New-LocalUser -Name TestUserDisabled2 -NoPassword -Description "Test User Disabled 2 Description" -Disabled).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestUsers -basename TestUserEnabled
                RemoveTestUsers -basename TestUserDisabled

                $enabledUser1SID = ""
                $enabledUser2SID = ""
                $disabledUser1SID = ""
                $disabledUser2SID = ""
            }
        }

        It "Can disable an enabled user by SID" {
            Disable-LocalUser -SID $enabledUser1SID
            $result = Get-LocalUser -SID $enabledUser1SID

            $result.Enabled | Should -BeFalse
        }

        It "Can disable an enabled user using -InputObject" {
            $user = Get-LocalUser TestUserEnabled1
            Disable-LocalUser -InputObject $user
            $result = Get-LocalUser TestUserEnabled1

            $result.Enabled | Should -BeFalse
        }

        It "Can disable an enabled user using pipeline" {
            Get-LocalUser TestUserEnabled1 | Disable-LocalUser
            $result = Get-LocalUser TestUserEnabled1

            $result.Enabled | Should -BeFalse
        }

        It "Can disable an enabled user by array of names" {
            Disable-LocalUser @("TestUserEnabled1", "TestUserEnabled2")

            (Get-LocalUser "TestUserEnabled1").Enabled | Should -BeFalse
            (Get-LocalUser "TestUserEnabled2").Enabled | Should -BeFalse
        }

        It "Can disable an enabled user by array of SIDs" {
            Disable-LocalUser -SID @($enabledUser1SID, $enabledUser2SID)

            (Get-LocalUser -SID $enabledUser1SID).Enabled | Should -BeFalse
            (Get-LocalUser -SID $enabledUser2SID).Enabled | Should -BeFalse
        }

        It "Can disable an enabled user by array sent using pipeline" {
            $users = @((Get-LocalUser "TestUserEnabled1"), (Get-LocalUser "TestUserEnabled2"))
            Disable-LocalUser -InputObject $users

            (Get-LocalUser "TestUserEnabled1").Enabled | Should -BeFalse
            (Get-LocalUser "TestUserEnabled2").Enabled | Should -BeFalse
        }

        It "Can disable an enabled user by array sent using pipeline" {
            @((Get-LocalUser "TestUserEnabled1"), (Get-LocalUser "TestUserEnabled2")) | Disable-LocalUser

            (Get-LocalUser "TestUserEnabled1").Enabled | Should -BeFalse
            (Get-LocalUser "TestUserEnabled2").Enabled | Should -BeFalse
        }

        It "Errors on no name or SID specified" {
            $sb = {
                Disable-LocalUser
            }
            VerifyFailingTest $sb "AmbiguousParameterSet,Microsoft.PowerShell.Commands.DisableLocalUserCommand"
        }

        It "Can disable an already disabled user by name" {
            Disable-LocalUser TestUserDisabled1

            (Get-LocalUser TestUserDisabled1).Enabled | Should -BeFalse
        }

        It "Can disable an already disabled user by SID" {
            Disable-LocalUser -SID $disabledUser1SID

            (Get-LocalUser -SID $disabledUser1SID).Enabled | Should -BeFalse
        }

        It "Can disable an already disabled user using the pipeline" {
            Get-LocalUser TestUserDisabled1 | Disable-LocalUser

            (Get-LocalUser TestUserDisabled1).Enabled | Should -BeFalse
        }

        It "Errors on disabling an invalid user by name" {
            $sb = {
                Disable-LocalUser -Name TestUserNameThatDoesntExist
            }
            VerifyFailingTest $sb "UserNotFound,Microsoft.PowerShell.Commands.DisableLocalUserCommand"
        }

        It "Errors on disabling an invalid user by SID" {
            $sb = {
                Remove-LocalUser -SID $enabledUser1SID
                return Disable-LocalUser -SID $enabledUser1SID
            }
            VerifyFailingTest $sb "UserNotFound,Microsoft.PowerShell.Commands.DisableLocalUserCommand"
        }

        It "Can respond to -ErrorAction Stop" {
            Try {
                Disable-LocalUser @("TestUserEnabled1", "TestUserNameThatDoesntExist1", "TestUserNameThatDoesntExist2") -ErrorAction Stop -ErrorVariable outError | Out-Null
            }
            Catch {
                # Do nothing here
            }
            $outError.Count | Should -Be 2
            $outError[0].ErrorRecord.FullyQualifiedErrorId | Should -Be "UserNotFound,Microsoft.PowerShell.Commands.DisableLocalUserCommand"

            $getResult = Get-LocalUser TestUserEnabled1 2>&1
            $getResult.Enabled | Should -BeFalse
        }
    }
}
finally {
    $global:PSDefaultParameterValues = $originalDefaultParameterValues
}

