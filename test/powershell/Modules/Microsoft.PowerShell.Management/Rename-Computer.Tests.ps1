# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
$RenameTesthook = "TestRenameComputer"
$RenameResultName = "TestRenameComputerResults"
$DefaultResultValue = 0

try
{
    # set up for testing
    $PSDefaultParameterValues["it:skip"] = ! $IsWindows
    Enable-Testhook -testhookName $RenameTesthook
    # we also set TestStopComputer
    Enable-Testhook -testhookName TestStopComputer

    # TEST START HERE
    Describe "Rename-Computer" -Tag Feature,RequireAdminOnWindows {
        # if we throw in BeforeEach, the test will fail and the stop will not be called
        BeforeEach {
            if ( ! (Test-TesthookIsSet -testhookName $RenameTesthook) ) {
                throw "Testhook '${TesthookName}' is not set"
            }
        }

        AfterEach {
            Set-TesthookResult -testhookName $RenameResultName -value $defaultResultValue
        }

        It "Should rename the local computer" {
            Set-TesthookResult -testhookName $RenameResultName -value $defaultResultValue
            $newname = "mynewname"
            $result = Rename-Computer -ErrorAction Stop -ComputerName . -NewName "$newname" -Pass -WarningAction SilentlyContinue
            $result.HasSucceeded | Should -BeTrue
            $result.NewComputerName | Should -BeExactly $newname
        }

        # we can't really look for the string "reboot" as it will change
        # when translated. We are guaranteed that the old computer name will
        # be present, so we'll look for that
        It "Should produce a reboot warning when renaming computer" {
            Set-TesthookResult -testhookName $RenameResultName -value $defaultResultValue
            $newname = "mynewname"
            $result = Rename-Computer -ErrorAction Stop -ComputerName . -NewName "$newname" -Pass -WarningAction SilentlyContinue -WarningVariable WarnVar
            $WarnVar.Message | Should -Match $result.OldComputerName
        }

        It "Should not produce a reboot warning when renaming a computer with the reboot flag" {
            Set-TesthookResult -testhookName $RenameResultName -value $defaultResultValue
            $newname = "mynewname"
            $result = Rename-Computer -ErrorAction Stop -ComputerName . -NewName "$newname" -Pass -WarningAction SilentlyContinue -WarningVariable WarnVar -Restart
            $result.HasSucceeded | Should -BeTrue
            $result.NewComputerName | Should -BeExactly $newname
            $WarnVar | Should -BeNullOrEmpty
        }

        Context "Rename-Computer Error Conditions" {
            $testcases =
                @{ OldName = "." ; NewName = "localhost" ; ExpectedError = "FailToRenameComputer,Microsoft.PowerShell.Commands.RenameComputerCommand" },
                @{ OldName = "." ; NewName = "." ; ExpectedError = "InvalidNewName,Microsoft.PowerShell.Commands.RenameComputerCommand" },
                @{ OldName = "." ; NewName = "::1" ; ExpectedError = "InvalidNewName,Microsoft.PowerShell.Commands.RenameComputerCommand" },
                @{ OldName = "." ; NewName = "127.0.0.1" ; ExpectedError = "InvalidNewName,Microsoft.PowerShell.Commands.RenameComputerCommand" },
                @{ OldName = "." ; NewName = ${env:ComputerName} ; ExpectedError = "NewNameIsOldName,Microsoft.PowerShell.Commands.RenameComputerCommand" },
                @{ OldName = "." ; NewName = ${env:ComputerName} + "." + ${env:USERDNSDOMAIN} ; ExpectedError = "InvalidNewName,Microsoft.PowerShell.Commands.RenameComputerCommand" },
                @{ OldName = ".\$#" ; NewName  = "NewName"; ExpectedError = "AddressResolutionException,Microsoft.PowerShell.Commands.RenameComputerCommand" }

            It "Renaming '<OldName>' to '<NewName>' creates the right error" -testcase $testcases {
                param ( $OldName, $NewName, $ExpectedError )
                Set-TesthookResult -testhookName $RenameResultName -value 0x1
                { Rename-Computer -ComputerName $OldName -NewName $NewName -ErrorAction Stop } | Should -Throw -ErrorId $ExpectedError
            }
        }
    }

}
finally
{
    $PSDefaultParameterValues.Remove("it:skip")
    Disable-Testhook -testhookName $RenameTestHook
    Disable-Testhook -testhookName TestStopComputer
    Set-TesthookResult -testhookName $RenameResultName -value 0
}
