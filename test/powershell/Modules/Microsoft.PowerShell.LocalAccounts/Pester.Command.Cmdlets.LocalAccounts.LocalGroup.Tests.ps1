# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Module removed due to #4272
# disabling tests

return

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
        throw "Expected FullyQualifiedErrorId: $expectedFqeid"
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

    Describe "Verify Expected LocalGroup Cmdlets are present" -Tags "CI" {

        It "Test command presence" {
            $result = Get-Command -Module Microsoft.PowerShell.LocalAccounts | ForEach-Object Name

            $result -contains "New-LocalGroup" | Should -BeTrue
            $result -contains "Set-LocalGroup" | Should -BeTrue
            $result -contains "Get-LocalGroup" | Should -BeTrue
            $result -contains "Rename-LocalGroup" | Should -BeTrue
            $result -contains "Remove-LocalGroup" | Should -BeTrue
        }
    }

    Describe "Validate simple New-LocalGroup" -Tags @('CI', 'RequireAdminOnWindows') {

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestGroups -basename TestGroupAddRemove
            }
        }

        It "Creates New-LocalGroup using only name" {
            $result = New-LocalGroup -Name TestGroupAddRemove

            $result.Name | Should -BeExactly TestGroupAddRemove
            $result.ObjectClass | Should -Be Group
        }
    }

    Describe "Validate New-LocalGroup cmdlet" -Tags @('Feature', 'RequireAdminOnWindows') {

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestGroups -basename TestGroupAddRemove
            }
        }

        It "Creates New-LocalGroup with name and description" {
            $result = New-LocalGroup -Name TestGroupAddRemove -Description "Test Group New 1 Description"

            $result.Name | Should -BeExactly TestGroupAddRemove
            $result.Description | Should -BeExactly "Test Group New 1 Description"
            $result.SID | Should -Not -BeNullOrEmpty
            $result.ObjectClass | Should -Be Group
        }

        It "Errors on New-LocalGroup with name collision" {
            $sb = {
                New-LocalGroup TestGroupAddRemove
                return New-LocalGroup TestGroupAddRemove
            }
            VerifyFailingTest $sb "GroupExists,Microsoft.PowerShell.Commands.NewLocalGroupCommand"
        }

        It "Can use SID for group name" {
            $sidName = "S-1-5-21-3949576937-491355012-4054854628-1053"
            try {
                $result = New-LocalGroup -Name $sidName

                $result | Should -Not -BeNullOrEmpty
                $result.Name | Should -BeExactly $sidName
                $result.SID | Should -Not -BeExactly $sidName
                $result.ObjectClass | Should -Be Group
            }
            finally {
                RemoveTestGroups -basename $sidName
            }
        }

        It "Errors on empty group name" {
            $sb = { New-LocalGroup -Name "" }
            VerifyFailingTest $sb "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.NewLocalGroupCommand"
        }

        It "Creates New-LocalGroup with name(256) at max" {
            $nameMax = "A"*256
            $desc = "D"*48

            try {
                $result = New-LocalGroup -Name $nameMax -Description $desc

                $result.Name | Should -BeExactly $nameMax
                $result.Description | Should -BeExactly $desc
                $result.SID | Should -Not -BeNullOrEmpty
                $result.ObjectClass | Should -Be Group
            }
            finally {
                RemoveTestGroups -basename $nameMax
            }
        }

        It "Errors on New-LocalGroup with name(256) over max" {
            $name = "A"*257
            $desc = "D"*129

            try {
                $shouldBeNull = New-LocalGroup -Name $name -Description $desc
                throw "An error was expected"
            }
            catch {
                $_.FullyQualifiedErrorId | Should -Be "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.NewLocalGroupCommand"
            }
            finally {
                #clean up erroneous creation
                if ($shouldBeNull) { Remove-LocalGroup -Name $name }
            }
        }

        It "Creates New-LocalGroup with Description > 48 characters" {
            $descMax = "Test Group Add Description that is longer than 48 characters"
            $result = New-LocalGroup -Name TestGroupAddRemove -Description $descMax

            $result.Description | Should -BeExactly $descMax
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
                    $invalidGroup = New-LocalGroup -Name ("InvalidBecauseOf" + $character) -ErrorAction Stop
                }
                catch {
                }
                finally {
                    if ($invalidGroup) {
                        Remove-LocalGroup -Name $invalidGroup
                        $failedCharacters += $character
                    }
                }
            }

            if ($failedCharacters.Count -gt 0) { Write-Host "characters causing test fail: $failedCharacters" }
            $failedCharacters.Count -eq 0 | Should -BeTrue
        }

        It "Error on names containing only spaces" {
            $sb = { New-LocalGroup -Name "   " }

            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.NewLocalGroupCommand"
        }

        It "Error on names containing only periods" {
            $sb = {
                New-LocalGroup -Name "..."
            }

            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.NewLocalGroupCommand"
        }

        It "Errors on names ending in a period" {
            $sb = {
                New-LocalGroup -Name "TestEndInPeriod."
            }

            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.NewLocalGroupCommand"

            $sb = {
                New-LocalGroup -Name ".TestEndIn.Period.."
            }

            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.NewLocalGroupCommand"
        }

        It "Errors on Name over 256 characters" {
            $sb = { New-LocalGroup -Name ("A"*257) }

            try {
                VerifyFailingTest $sb "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.NewLocalGroupCommand"
            }
            finally {
                RemoveTestGroups -basename ("A"*257)
            }
        }
    }

    Describe "Validate simple Get-LocalGroup" -Tags @('CI', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                New-LocalGroup -Name TestGroupGet1 -Description "Test Group Get 1 Description" | Out-Null
            }
        }

        AfterAll {
            if ($IsNotSkipped) {
                RemoveTestGroups -basename TestGroupGet
            }
        }

        It "Can Get-LocalGroup by specific group name" {
            $result = Get-LocalGroup TestGroupGet1

            $result.Name | Should -Be "TestGroupGet1"
            $result.Description | Should -Be "Test Group Get 1 Description"
            $result.ObjectClass | Should -Be "Group"
        }
    }

    Describe "Validate Get-LocalGroup cmdlet" -Tags @('Feature', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                New-LocalGroup -Name TestGroupGet1 -Description "Test Group Get 1 Description" | Out-Null
                New-LocalGroup -Name TestGroupGet2 -Description "Test Group Get 2 Description" | Out-Null
            }
        }

        AfterAll {
            if ($IsNotSkipped) {
                RemoveTestGroups -basename TestGroupGet
            }
        }

        #Note: this test is no longer in the test plan
        It "Can Get-LocalGroup of all groups"  {
            $result = Get-LocalGroup

            $result.Count -gt 2 | Should -BeTrue
        }

        It "Can Get-LocalGroup of a specific group by SID" {
            $result = Get-LocalGroup TestGroupGet1
            $resultBySID = Get-LocalGroup -SID $result.SID

            $resultBySID.SID | Should -Not -BeNullOrEmpty
            $resultBySID.Name | Should -Be TestGroupGet1
        }

        It "Can Get-LocalGroup of a well-known group by SID string" {
            $sid = New-Object System.Security.Principal.SecurityIdentifier -ArgumentList BG
            $guestGroup = Get-LocalGroup -SID BG

            $guestGroup.SID | Should -Be $sid.Value
        }

        It "Can Get-LocalGroup by wildcard" {
            $result = Get-LocalGroup TestGroupGet*

            $result.Count -eq 2 | Should -BeTrue
            $result.Name -contains "TestGroupGet1" | Should -BeTrue
            $result.Name -contains "TestGroupGet2" | Should -BeTrue
        }

        It "Can Get-LocalGroup gets by array of names" {
            $result = Get-LocalGroup @("TestGroupGet1", "TestGroupGet2")

            $result.Count -eq 2 | Should -BeTrue
            $result.Name -contains "TestGroupGet1" | Should -BeTrue
            $result.Name -contains "TestGroupGet2" | Should -BeTrue
        }

        It "Can Get-LocalGroups by array of SIDs" {
            $sid1 = (Get-LocalGroup TestGroupGet1).SID
            $sid2 = (Get-LocalGroup TestGroupGet2).SID
            $result = Get-LocalGroup -SID @($sid1, $sid2)

            $result.Count -eq 2 | Should -BeTrue
            $result.Name -contains "TestGroupGet1" | Should -BeTrue
            $result.Name -contains "TestGroupGet2" | Should -BeTrue
        }

        It "Can Get-LocalGroups by pipe of an array of Group objects" {
            $testGroups = Get-LocalGroup TestGroupGet*
            $result = @($testGroups, $testGroups) | Get-LocalGroup

            $result.Count -eq 4 | Should -BeTrue
            $result.Name -contains "TestGroupGet1" | Should -BeTrue
            $result.Name -contains "TestGroupGet2" | Should -BeTrue
        }

        It "Can respond to -ErrorAction Stop" {
            $result = $null
            try {
                Get-LocalGroup @("TestGroupGet1", "TestGroupGetNameThatDoesntExist1", "TestGroupGetNameThatDoesntExist2") -ErrorAction Stop -ErrorVariable outErr -OutVariable outOut | Out-Null
            }
            catch {
                $result = @($outErr.Count, $outErr[0].ErrorRecord.CategoryInfo.Reason, $outOut.Name)
            }

            if ($null -eq $result)
            {
                # Force failing the test because an unexpected outcome occurred
                $false | Should -BeTrue
            }
            else
            {
                $result[0] -eq 1 | Should -BeTrue
                $result[1] -match "GroupNotFound" | Should -BeTrue
                $result[2] -match "TestGroupGet1" | Should -BeTrue
            }
        }

        It "Errors on Get-LocalGroup by an invalid group name" {
            $sb = {
                Get-LocalGroup 'TestGroupGetNameThatDoesntExist'
            }
            VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.GetLocalGroupCommand"
        }

        It "Errors on Get-LocalGroup by an invalid group SID" {
            $sb = {
                $result = New-LocalGroup -Name TestGroupGet3 -Description "Test Group Get 3 Description"
                Remove-LocalGroup TestGroupGet3
                Get-LocalGroup -SID $result.SID
            }
            VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.GetLocalGroupCommand"
        }

        It "Can get no local groups if none match wildcard" {
            $localGroupName = 'TestGroupGetNameThatDoesntExist'
            $result = (Get-LocalGroup $localGroupName*).Count

            $result -eq 0 | Should -BeTrue
        }
    }

    Describe "Validate simple Set-LocalGroup" -Tags @('CI', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                $group1SID = ""
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                $group1SID = (New-LocalGroup -Name TestGroupSet1).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestGroups -basename TestGroupSet
                $group1SID = ""
            }
        }

        It "Can Set-LocalGroup by name" {
            Set-LocalGroup -Name TestGroupSet1 -Description "Test Group Set 1 new description"
            $result = Get-LocalGroup -Name TestGroupSet1

            $result.Description | Should -BeExactly "Test Group Set 1 new description"
        }
    }

    Describe "Validate Set-LocalGroup cmdlet" -Tags @('Feature', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                $group1SID = ""
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                $group1SID = (New-LocalGroup -Name TestGroupSet1).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestGroups -basename TestGroupSet
                $group1SID = ""
            }
        }

        It "Can Set-LocalGroup by SID" {
            Set-LocalGroup -SID $group1SID -Description "Test Group Set 1 newer description"
            $result = Get-LocalGroup -Name TestGroupSet1

            $result.Description | Should -BeExactly "Test Group Set 1 newer description"
        }

        It "Can Set-LocalGroup using -InputObject" {
            $group = Get-LocalGroup TestGroupSet1
            Set-LocalGroup -InputObject $group -Description "Test Group Set 1 newer still description"
            $result = Get-LocalGroup TestGroupSet1

            $result.Description | Should -BeExactly "Test Group Set 1 newer still description"
        }

        It "Can Set-LocalGroup using pipeline" {
            Get-LocalGroup TestGroupSet1 | Set-LocalGroup -Description "Test Group Set 1 newer still description"
            $result = Get-LocalGroup TestGroupSet1

            $result.Description | Should -BeExactly "Test Group Set 1 newer still description"
        }

        It "Errors on Set-LocalGroup without specifying a Group" {
            $sb = {
                Set-LocalGroup -Description "Test Group Set 1 newer still description"
            }
            VerifyFailingTest $sb "AmbiguousParameterSet,Microsoft.PowerShell.Commands.SetLocalGroupCommand"
        }

        It "Errors on Set-LocalGroup with an invalid Group name" {
            $sb = {
                Set-LocalGroup -Name "NonexistantGroupName" -Description "Test Group Set 1 newer still description"
            }
            VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.SetLocalGroupCommand"
        }

        It "Errors on Set-LocalGroup with an invalid Group SID" {
            $sb = {
                Set-LocalGroup -SID "S-1-5-21-1220945662-555555555-555555555-5555" -Description "Test Group Set 1 newer still description"
            }

            VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.SetLocalGroupCommand"
        }

        It "Can Set-LocalGroup with description over 48 characters" {
            $desc = "A"*129
            Set-LocalGroup -Name TestGroupSet1 -Description $desc
            $result = Get-LocalGroup -Name TestGroupSet1

            $result.Description | Should -BeExactly $desc
        }
    }

    Describe "Validate simple Rename-LocalGroup" -Tags @('CI', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                $group1SID = ""
                $group2SID = (New-LocalGroup -Name TestGroupRename2 -Description "Test Group Rename 2 Description" ).SID
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                $group1SID = ( New-LocalGroup -Name TestGroupRename1 -Description "Test Group Rename 1 Description" ).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                Remove-LocalGroup -SID $group1SID
                $group1SID = ""
            }
        }

        AfterAll {
            if ($IsNotSkipped) {
                RemoveTestGroups -basename TestGroupRename
            }
        }

        It "Can Rename-LocalGroup using a valid group name" {
            $group1SID | Should -Not -BeNullOrEmpty
            Rename-LocalGroup TestGroupRename1 TestGroupRename1x
            $result = Get-LocalGroup -SID $group1SID

            $result.Name | Should -BeExactly TestGroupRename1x
        }
    }

    Describe "Validate Rename-LocalGroup cmdlet" -Tags @('Feature', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                $group1SID = ""
                $group2SID = (New-LocalGroup -Name TestGroupRename2 -Description "Test Group Rename 2 Description" ).SID
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                $group1SID = (New-LocalGroup -Name TestGroupRename1 -Description "Test Group Rename 1 Description" ).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                Remove-LocalGroup -SID $group1SID
                $group1SID = ""
            }
        }

        AfterAll {
            if ($IsNotSkipped) {
                RemoveTestGroups -basename TestGroupRename
            }
        }

        It "Can Rename-LocalGroup using a valid group SID" {
            $group1SID | Should -Not -BeNullOrEmpty
            Rename-LocalGroup -SID $group1SID TestGroupRename1x
            $result = Get-LocalGroup -SID $group1SID

            $result.Name | Should -BeExactly TestGroupRename1x
        }

        It "Can Rename-LocalGroup using a valid group -InputObject" {
            $group1SID | Should -Not -BeNullOrEmpty
            $group = Get-LocalGroup TestGroupRename1
            Rename-LocalGroup -InputObject $group -NewName TestGroupRename1x
            $result = Get-LocalGroup -SID $group1SID

            $result.Name | Should -BeExactly TestGroupRename1x
        }

        It "Can Rename-LocalGroup using a valid group sent using pipeline" {
            $group1SID | Should -Not -BeNullOrEmpty
            Get-LocalGroup TestGroupRename1 | Rename-LocalGroup -NewName TestGroupRename1x
            $result = Get-LocalGroup -SID $group1SID

            $result.Name | Should -BeExactly TestGroupRename1x
        }

        It "Errors on Rename-LocalGroup without specifying a Group" {
            $sb = {
                Rename-LocalGroup
            }
            VerifyFailingTest $sb "AmbiguousParameterSet,Microsoft.PowerShell.Commands.RenameLocalGroupCommand"
        }

        It "Errors onRename-LocalGroup nonexistant group name" {
            $sb = {
                Rename-LocalGroup nonexistantGroupName -NewName DummyNewName
            }
            VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.RenameLocalGroupCommand"
        }

        It "Errors onRename-LocalGroup nonexistant group SID" {
            $nonexistantSid =
            $sb = {
                Rename-LocalGroup -SID "S-1-5-21-1220945662-555555555-555555555-5555" -NewName DummyNewName
            }
            VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.RenameLocalGroupCommand"
        }

        It "Errors on Rename-LocalGroup Renames a valid group to already existing name" {
            $newName = "TestGroupRename2"

            $sb = {
                Rename-LocalGroup TestGroupRename1 $newName
            }
            VerifyFailingTest $sb "NameInUse,Microsoft.PowerShell.Commands.RenameLocalGroupCommand"

            $group1Name = (Get-LocalGroup -SID $group1SID).Name
            $group2Name = (Get-LocalGroup -SID $group2SID).Name

            $group1Name | Should -BeExactly TestGroupRename1
            $group2Name | Should -BeExactly $newName
        }

        It "Errors on Invalid characters" {
            #Arrange
            #list of characters that should be invalid
            $InvalidCharacters = @"
\/"[]:|<>+=;,?*
"@
            $InvalidCharacters = $InvalidCharacters[0..($InvalidCharacters.Length - 1)]
            $failedCharacters = @()

            #Act
            foreach ($character in $InvalidCharacters) {
                try {
                    Rename-LocalGroup -Name TestGroupRename1 -NewName ("InvalidBecauseOf" + $character) -ErrorAction Stop
                    $invalidGroup = (Get-LocalGroup ("InvalidBecauseOf" + $character))
                }
                catch {
                }
                finally {
                    #handle groups being erroneously renamed
                    if ($invalidGroup) {
                        Rename-LocalGroup -Name ("InvalidBecauseOf" + $character) -NewName TestGroupRename1
                        $failedCharacters += $character
                    }
                }
            }

            #Assert
            if ($failedCharacters.Count -gt 0) { Write-Host "characters causing test fail: $failedCharacters" }
            $failedCharacters.Count -eq 0 | Should -BeTrue
        }

        It "Error on names containing only spaces" {
            $sb = {
                Rename-LocalGroup -Name TestGroupRename1 -NewName "   "
            }
            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.RenameLocalGroupCommand"
        }

        It "Error on names containing only periods" {
            $sb = {
                Rename-LocalGroup -Name TestGroupRename1 -NewName "..."
            }
            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.RenameLocalGroupCommand"
        }

        It "Errors on names ending in a period" {
            $sb = {
                Rename-LocalGroup -Name TestGroupRename1 -NewName "TestEndInPeriod."
            }
            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.RenameLocalGroupCommand"

            $sb = {
                Rename-LocalGroup -Name TestGroupRename1 -NewName ".TestEndIn.Period.."
            }
            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.RenameLocalGroupCommand"
        }

        It "Errors on Rename-LocalGroup using a valid group but invalid -NewName" {
            $sb = {
                Rename-LocalGroup -Name TestGroupRename1 -NewName "TestGroupRename<>1x"
            }
            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.RenameLocalGroupCommand"
        }

        It "Can Rename-LocalGroup using a valid group name at max length 256" {
            $newName = "A"*256
            Rename-LocalGroup TestGroupRename1 $newName
            $result = Get-LocalGroup -SID $group1SID

            $result.Name | Should -BeExactly $newName
        }

        It "Errors on Rename-LocalGroup using a valid group name over max length 256" {
            $newName = "A"*257
            $sb = {
                Rename-LocalGroup TestGroupRename1 $newName
            }
            VerifyFailingTest $sb "InvalidName,Microsoft.PowerShell.Commands.RenameLocalGroupCommand"

            (Get-LocalGroup -SID $group1SID).Name | Should -BeExactly TestGroupRename1
        }
    }

    Describe "Validate simple Remove-LocalGroup" -Tags @('CI', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                $group1SID = ""
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                $group1SID = (New-LocalGroup -Name TestGroupRemove1 -Description "Test Group Remove 1 Description" ).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestGroups -basename TestGroupRemove
                $group1SID = ""
            }
        }

        It "Can Remove-LocalGroup by name" {
            $initialCount = (Get-LocalGroup).Count
            $initialCount -gt 1 | Should -BeTrue

            $removeResult = Remove-LocalGroup TestGroupRemove1 2>&1
            $removeResult | Should -BeNullOrEmpty

            $sb = {
                Get-LocalGroup -SID $group1SID
            }
            VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.GetLocalGroupCommand"

            $finalCount = (Get-LocalGroup).Count
            $initialCount -eq $finalCount + 1 | Should -BeTrue
        }
    }

    Describe "Validate Remove-LocalGroup cmdlet" -Tags @('Feature', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsNotSkipped) {
                $group1SID = ""
                $group2SID = ""

                function VerifyBasicRemoval {
                    param (
                        [scriptblock]$removalAction
                    )
                    $initialCount = (Get-LocalGroup).Count
                    $initialCount -gt 1 | Should -BeTrue

                    & $removalAction

                    $sb = {
                        Get-LocalGroup -SID $group1SID
                    }
                    VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.GetLocalGroupCommand"

                    $finalCount = (Get-LocalGroup).Count
                    $initialCount -eq $finalCount + 1 | Should -BeTrue
                }

                function VerifyArrayRemoval {
                    param (
                        [scriptblock]$removalAction
                    )
                    $initialCount = (Get-LocalGroup).Count
                    $initialCount -gt 1 | Should -BeTrue

                    & $removalAction

                    $sb = {
                        Get-LocalGroup -SID $group1SID
                    }
                    VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.GetLocalGroupCommand"

                    $sb = {
                        Get-LocalGroup -SID $group2SID
                    }
                    VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.GetLocalGroupCommand"

                    $finalCount = (Get-LocalGroup).Count
                    $initialCount -eq $finalCount + 2 | Should -BeTrue
                }
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                $group1SID = [String](New-LocalGroup -Name TestGroupRemove1 -Description "Test Group Remove 1 Description" 2>&1).SID
                $group2SID = [String](New-LocalGroup -Name TestGroupRemove2 -Description "Test Group Remove 2 Description" 2>&1).SID
            }
        }

        AfterEach {
            if ($IsNotSkipped) {
                RemoveTestGroups -basename TestGroupRemove
                $group1SID = ""
                $group2SID = ""
            }
        }

        It "Can Remove-LocalGroup by SID" {
            $sb = {
                $removeResult = Remove-LocalGroup -SID $group1SID 2>&1
                $removeResult | Should -BeNullOrEmpty
            }
            VerifyBasicRemoval $sb
        }

        It "Can Remove-LocalGroup using -InputObject" {
            $sb = {
                $group = Get-LocalGroup TestGroupRemove1
                $removeResult = Remove-LocalGroup -InputObject $group 2>&1
                $removeResult | Should -BeNullOrEmpty
            }
            VerifyBasicRemoval $sb
        }

        It "Can Remove-LocalGroup using pipeline" {
            $sb = {
                $removeResult = Get-LocalGroup TestGroupRemove1 | Remove-LocalGroup 2>&1
                $removeResult | Should -BeNullOrEmpty
            }
            VerifyBasicRemoval $sb
        }

        It "Can Remove-LocalGroup by array of names" {
            $sb = {
                $removeResult = Remove-LocalGroup @("TestGroupRemove1","TestGroupRemove2") 2>&1
                $removeResult | Should -BeNullOrEmpty
            }
            VerifyArrayRemoval $sb
        }

        It "Can Remove-LocalGroup by array of SIDs" {
            $sb = {
                $removeResult = Remove-LocalGroup -SID @($group1SID, $group2SID) 2>&1
                $removeResult | Should -BeNullOrEmpty
            }
            VerifyArrayRemoval $sb
        }

        It "Can Remove-LocalGroup by array using -InputObject" {
            $sb = {
                $groups = Get-LocalGroup -Name @("TestGroupRemove1","TestGroupRemove2")
                $removeResult = Remove-LocalGroup -InputObject $groups 2>&1
                $removeResult | Should -BeNullOrEmpty
            }
            VerifyArrayRemoval $sb
        }

        It "Can Remove-LocalGroup by array using pipeline" {
            $sb = {
                $removeResult = Get-LocalGroup -Name @("TestGroupRemove1","TestGroupRemove2") | Remove-LocalGroup 2>&1
                $removeResult | Should -BeNullOrEmpty
            }
            VerifyArrayRemoval $sb
        }

        It "Errors on Remove-LocalGroup without specifying a Name or SID" {
            $sb = { Remove-LocalGroup }
            VerifyFailingTest $sb "AmbiguousParameterSet,Microsoft.PowerShell.Commands.RemoveLocalGroupCommand"
        }

        It "Can Remove-LocalGroup with members" {
            New-LocalUser TestUserRemove1 -NoPassword | Out-Null
            Add-LocalGroupMember TestGroupRemove1 -Member TestUserRemove1 | Out-Null
            $initialCount = (Get-LocalGroup).Count
            $initialCount -gt 1 | Should -BeTrue

            $removeResult = Remove-LocalGroup TestGroupRemove1 2>&1
            $removeResult | Should -BeNullOrEmpty

            #clean-up
            Remove-LocalUser TestUserRemove1 | Out-Null

            $sb = {
                Get-LocalGroup TestGroupRemove1
            }
            VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.GetLocalGroupCommand"

            $finalCount = (Get-LocalGroup).Count
            $initialCount -eq $finalCount + 1 | Should -BeTrue
        }

        It "Errors on Remove-LocalGroup by invalid name" {
            $initialCount = (Get-LocalGroup).Count
            $initialCount -gt 1 | Should -BeTrue

            $sb = {
                Remove-LocalGroup TestGroupRemove1NameThatDoesntExist
            }
            VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.RemoveLocalGroupCommand"

            $finalCount = (Get-LocalGroup).Count
            $initialCount -eq $finalCount | Should -BeTrue
        }

        It "Errors on Remove-LocalGroup by invalid SID" {
            $initialCount = (Get-LocalGroup).Count
            $initialCount -gt 1 | Should -BeTrue

            $sb = {
                Remove-LocalGroup -SID $group1SID
                Remove-LocalGroup -SID $group1SID
            }
            VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.RemoveLocalGroupCommand"

            $finalCount = (Get-LocalGroup).Count
            $initialCount -eq $finalCount + 1 | Should -BeTrue
        }

        It "Can respond to -ErrorAction Stop" {
            $errCount = 0
            $fqeid = ""
            try {
                Remove-LocalGroup @("TestGroupRemove1", "TestGroupRemoveNameThatDoesntExist1", "TestGroupRemoveNameThatDoesntExist2") -ErrorAction Stop -ErrorVariable outError
            }
            catch {
                $errCount = $outError.Count
                $fqeid = $_.FullyQualifiedErrorId
            }

            # Confirm that the expected errors were caught
            $errCount | Should -Be 2
            $fqeid | Should -Be "GroupNotFound,Microsoft.PowerShell.Commands.RemoveLocalGroupCommand"

            # confirm that the first group was removed
            $sb = {
                Get-LocalGroup "TestGroupRemove1"
            }
            VerifyFailingTest $sb "GroupNotFound,Microsoft.PowerShell.Commands.GetLocalGroupCommand"
        }
    }
}
finally {
    $global:PSDefaultParameterValues = $originalDefaultParameterValues
}
