# note these will manipulate private data in the PowerShell engine which will
# enable us to not actually rename the system, but return right before we do
$TesthooksType = [system.management.automation.internal.internaltesthooks]
$TesthookName = "TestRenameComputer"
$TesthookResultName = "TestRenameComputerResults"
$DefaultResultValue = 0
function Set-HookResult([int]$result) {
    ${TesthooksType}::SetTestHook($TesthookResultName, $result)
}

# protect the user from from stop their computer 
function Test-TesthookIsSet() {
    try {
        return ${TesthooksType}.GetField($TesthookName, "NonPublic,Static").GetValue($null)
    }
    catch {
        # fall through
    }
    return $false
}

try 
{
    # set up for testing
    $PSDefaultParameterValues["it:skip"] = ! $IsWindows
    ${TesthooksType}::SetTestHook($TesthookName,$true)
    # we also set TestStopComputer
    ${TesthooksType}::SetTestHook("TestStopComputer", $true)

    # TEST START HERE
    Describe "Rename-Computer" -Tag Feature {
        # if we throw in BeforeEach, the test will fail and the stop will not be called
        BeforeEach {
            if ( ! (Test-TesthookIsSet) ) {
                throw "Testhook '${TesthookName}' is not set"
            }
        }

        AfterEach {
            Set-HookResult -result $defaultResultValue
        }

        It "Should rename the local computer" {
            Set-HookResult -result $defaultResultValue
            $newname = "mynewname"
            $result = Rename-Computer -ea Stop -ComputerName . -NewName "$newname" -Pass -WarningAction SilentlyContinue
            $result.HasSucceeded | should be $true
            $result.NewComputerName | should be $newname
        }

        # we can't really look for the string "reboot" as it will change 
        # when translated. We are guaranteed that the old computer name will
        # be present, so we'll look for that
        It "Should produce a reboot warning when renaming computer" {
            Set-HookResult -result $defaultResultValue
            $newname = "mynewname"
            $result = Rename-Computer -ea Stop -ComputerName . -NewName "$newname" -Pass -WarningAction SilentlyContinue -WarningVariable WarnVar
            $WarnVar.Message | should match $result.OldComputerName
        }

        It "Should not produce a reboot warning when renaming a computer with the reboot flag" {
            Set-HookResult -result $defaultResultValue
            $newname = "mynewname"
            $result = Rename-Computer -ea Stop -ComputerName . -NewName "$newname" -Pass -WarningAction SilentlyContinue -WarningVariable WarnVar -Restart
            $result.HasSucceeded | should be $true
            $result.NewComputerName | should be $newname
            $WarnVar | should BeNullOrEmpty
            
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
                Set-HookResult -result 0x1
                { Rename-Computer -ComputerName $OldName -NewName $NewName -ea Stop } | ShouldBeErrorId $ExpectedError
            }
        }
    }

}
finally
{
    $PSDefaultParameterValues.Remove("it:skip")
    ${TesthooksType}::SetTestHook($TesthookName, $false)
    ${TesthooksType}::SetTestHook("TestStopComputer", $false)
    Set-HookResult -result 0
}
