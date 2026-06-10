# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

$script:RenameTesthook = "TestRenameComputer"
$script:RenameResultName = "TestRenameComputerResults"
$script:DefaultResultValue = 0
$script:skipRenameComputer = ! $IsWindows

Describe "Rename-Computer" -Tag Feature,RequireAdminOnWindows -Skip:$script:skipRenameComputer {
    BeforeAll {
        $RenameTesthook = $script:RenameTesthook
        $RenameResultName = $script:RenameResultName
        $DefaultResultValue = $script:DefaultResultValue
        if (Get-Command Enable-Testhook -ErrorAction SilentlyContinue) {
            Enable-Testhook -testhookName $RenameTesthook
            # we also set TestStopComputer
            Enable-Testhook -testhookName TestStopComputer
        }
    }

    AfterAll {
        if (Get-Command Disable-Testhook -ErrorAction SilentlyContinue) {
            Disable-Testhook -testhookName $script:RenameTesthook
            Disable-Testhook -testhookName TestStopComputer
        }
        if (Get-Command Set-TesthookResult -ErrorAction SilentlyContinue) {
            Set-TesthookResult -testhookName $script:RenameResultName -value 0
        }
    }

    # if we throw in BeforeEach, the test will fail and the stop will not be called
    BeforeEach {
        if ( ! (Test-TesthookIsSet -testhookName $RenameTesthook) ) {
            throw "Testhook '$RenameTesthook' is not set"
        }
    }

    AfterEach {
        Set-TesthookResult -testhookName $RenameResultName -value $DefaultResultValue
    }

    It "Should rename the local computer" {
        Set-TesthookResult -testhookName $RenameResultName -value $DefaultResultValue
        $newname = "mynewname"
        $result = Rename-Computer -ErrorAction Stop -ComputerName . -NewName "$newname" -Pass -WarningAction SilentlyContinue
        $result.HasSucceeded | Should -BeTrue
        $result.NewComputerName | Should -BeExactly $newname
    }

    # we can't really look for the string "reboot" as it will change
    # when translated. We are guaranteed that the old computer name will
    # be present, so we'll look for that
    It "Should produce a reboot warning when renaming computer" {
        Set-TesthookResult -testhookName $RenameResultName -value $DefaultResultValue
        $newname = "mynewname"
        $result = Rename-Computer -ErrorAction Stop -ComputerName . -NewName "$newname" -Pass -WarningAction SilentlyContinue -WarningVariable WarnVar
        $WarnVar.Message | Should -Match $result.OldComputerName
    }

    It "Should not produce a reboot warning when renaming a computer with the reboot flag" {
        Set-TesthookResult -testhookName $RenameResultName -value $DefaultResultValue
        $newname = "mynewname"
        $result = Rename-Computer -ErrorAction Stop -ComputerName . -NewName "$newname" -Pass -WarningAction SilentlyContinue -WarningVariable WarnVar -Restart
        $result.HasSucceeded | Should -BeTrue
        $result.NewComputerName | Should -BeExactly $newname
        $WarnVar | Should -BeNullOrEmpty
    }

    Context "Rename-Computer Error Conditions" {
        BeforeDiscovery {
            $testcases =
                @{ OldName = "." ; NewName = "localhost" ; ExpectedError = "FailToRenameComputer,Microsoft.PowerShell.Commands.RenameComputerCommand" },
                @{ OldName = "." ; NewName = "." ; ExpectedError = "InvalidNewName,Microsoft.PowerShell.Commands.RenameComputerCommand" },
                @{ OldName = "." ; NewName = "::1" ; ExpectedError = "InvalidNewName,Microsoft.PowerShell.Commands.RenameComputerCommand" },
                @{ OldName = "." ; NewName = "127.0.0.1" ; ExpectedError = "InvalidNewName,Microsoft.PowerShell.Commands.RenameComputerCommand" },
                @{ OldName = "." ; NewName = ${env:ComputerName} ; ExpectedError = "NewNameIsOldName,Microsoft.PowerShell.Commands.RenameComputerCommand" },
                @{ OldName = "." ; NewName = ${env:ComputerName} + "." + ${env:USERDNSDOMAIN} ; ExpectedError = "InvalidNewName,Microsoft.PowerShell.Commands.RenameComputerCommand" },
                @{ OldName = ".\$#" ; NewName  = "NewName"; ExpectedError = "AddressResolutionException,Microsoft.PowerShell.Commands.RenameComputerCommand" }
        }

        It "Renaming '<OldName>' to '<NewName>' creates the right error" -testcase $testcases {
            param ( $OldName, $NewName, $ExpectedError )
            Set-TesthookResult -testhookName $RenameResultName -value 0x1
            { Rename-Computer -ComputerName $OldName -NewName $NewName -ErrorAction Stop } | Should -Throw -ErrorId $ExpectedError
        }
    }
}
