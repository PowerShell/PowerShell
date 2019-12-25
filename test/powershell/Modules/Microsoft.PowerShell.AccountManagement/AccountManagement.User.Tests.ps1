# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

try {
    #skip all tests on non-windows platform
    $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
    $PSDefaultParameterValues["it:skip"] = !$IsWindows

    Describe "Validate AccountManagement user cmdlets" -Tags @('CI', 'RequireAdminOnWindows') {

        BeforeAll {
            $Password = ([char[]]([char]33..[char]95) + ([char[]]([char]97..[char]126)) + 0..9 | Sort-Object {Get-Random})[0..12] -join ''
            $pwd = (New-Object -TypeName Net.NetworkCredential("", $Password)).SecurePassword
        }

        AfterEach {
            if ($IsWindows) {
                net user TestUserNew /delete
            }
        }

        Context "Validate New-User" {

            It "Can create New-User with only name" {
                $result = New-User -Name TestUserNew -PasswordNotRequired -AccountStore Machine

                $result.Name | Should -BeExactly TestUserNew
                $result.Description | Should -BeNullOrEmpty

                # New account is disabled by default
                $result.Enabled | Should -BeFalse
                $result.SID | Should -Not -BeNullOrEmpty
            }

            It "Can create New-User with password" {
                $result = New-User -Name TestUserNew -AccountStore Machine -Password $pwd

                $result.Name | Should -BeExactly TestUserNew
            }

            It "Can create New-User with explicit properties in local machine" {
                $result = New-User -AccountStore Machine `
                    -Name TestUserNew `
                    -PasswordNotRequired `
                    -AccountNeverExpires `
                    -Description "desc" `
                    -DisplayName "disp" `
                    -SamAccountName "samq2" `
                    -UserCannotChangePassword `
                    -PasswordNeverExpires `
                    -Enabled

                # For AccountStore == Machine if we set SamAccountName we get Name the same.
                $result.Name | Should -BeExactly "samq2"
                $result.PasswordNotRequired | Should -BeTrue
                $result.AccountExpirationDate | Should -Be $null
                $result.Description | Should -BeExactly "desc"
                $result.DisplayName | Should -BeExactly "disp"
                $result.SamAccountName | Should -BeExactly "samq2"
                $result.UserCannotChangePassword | Should -BeTrue
                $result.PasswordNeverExpires | Should -BeTrue
                # Account is disable because PasswordNotRequired is set.
                $result.Enabled | Should -BeFalse
            }

            It "Can create New-User with explicit properties in a domain" -Pending:$true {
                # The test works only with ActiveDirectory so skip it.
                $result = New-User -AccountStore Machine `
                    -Name TestUserNew `
                    -PasswordNotRequired `
                    -AccountNeverExpires `
                    -AccountStore Domain `
                    -DelegationPermitted `
                    -Description "desc" `
                    -DisplayName "disp" `
                    -EmailAddress "email@domain.com" `
                    -EmployeeId "empl" `
                    -GivenName "gn" `
                    -HomeDirectory "hd" `
                    -HomeDrive "hdrv" `
                    -MiddleName "mn" `
                    -SamAccountName "samq2" `
                    -SurName "sn" `
                    -UserCannotChangePassword `
                    -PasswordNeverExpires `
                    -Enabled

                $result.Name | Should -BeExactly TestUserNew
                $result.PasswordNotRequired | Should -BeTrue
                $result.AccountExpirationDate | Should -Be $null
                $result.DelegationPermitted | Should -BeTrue
                $result.Description | Should -BeExactly "desc"
                $result.DisplayName | Should -BeExactly "disp"
                $result.EmailAddress | Should -BeExactly "email@domain.com"
                $result.EmployeeId | Should -BeExactly "empl"
                $result.GivenName | Should -BeExactly "gn"
                $result.HomeDirectory | Should -BeExactly "hd"
                $result.HomeDrive | Should -BeExactly "hdrv"
                $result.MiddleName | Should -BeExactly "mn"
                $result.SamAccountName | Should -BeExactly "samq2"
                $result.SurName | Should -BeExactly "sn"
                $result.UserCannotChangePassword | Should -BeTrue
                $result.PasswordNeverExpires | Should -BeTrue
                $result.Enabled | Should -BeTrue
            }

            It "Errors on Name argument of empty string, null, spaces" {
                { New-User -Name "" -PasswordNotRequired -ErrorAction Stop } | Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.NewUserCommand"
                { New-User -Name $null -PasswordNotRequired -ErrorAction Stop } | Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.NewUserCommand"
                { New-User -Name "   " -PasswordNotRequired -ErrorAction Stop } | Should -Throw -ErrorId "InvalidValue,Microsoft.PowerShell.Commands.NewUserCommand"
            }

            It "Error on user exists" {
                $result = New-User -Name TestUserNew -PasswordNotRequired -AccountStore Machine
                $exc = { New-User -Name TestUserNew -PasswordNotRequired -AccountStore Machine -ErrorAction Stop } | Should -PassThru -Throw -ErrorId "InvalidValue,Microsoft.PowerShell.Commands.NewUserCommand"
                $exc.Exception | Should -BeOfType "System.DirectoryServices.AccountManagement.PrincipalExistsException"
            }
        }

        Context "Validate Remove-User" {
            AfterAll {
                if ($IsWindows) {
                    net user TestUserNew /delete
                }
            }

            BeforeEach {
                if ($IsWindows) {
                    $userIdentity = New-User -Name TestUserNew -AccountStore Machine -Password $pwd
                }
            }

            It "Can remove user account with pipeline" {
                { $userIdentity | Remove-User -ErrorAction Stop } | Should -Not -Throw
                $err = net user TestUserNew *>&1 | Out-String
                $err | Should -BeLike "The user name could not be found*"
                { $userIdentity | Remove-User -ErrorAction Stop } | Should -Throw -ErrorId "UserAlreadyRemoved,Microsoft.PowerShell.Commands.RemoveUserCommand"
            }

            It "Can remove user account with parameter" {
                { Remove-User -Identity $userIdentity -ErrorAction Stop } | Should -Not -Throw
                $err = net user TestUserNew *>&1 | Out-String
                $err | Should -BeLike "The user name could not be found*"
                { Remove-User -Identity $userIdentity -ErrorAction Stop } | Should -Throw -ErrorId "UserAlreadyRemoved,Microsoft.PowerShell.Commands.RemoveUserCommand"
            }
        }
    }
}
finally {
    $global:PSDefaultParameterValues = $originalDefaultParameterValues
}
