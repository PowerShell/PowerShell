# note these will manipulate private data in the PowerShell engine which will
# enable us to not actually stop the system, but return right before we do
$TesthooksType = [system.management.automation.internal.internaltesthooks]
$TesthookName = "TestStopComputer"
$TesthookResultName = "TestStopComputerResults"
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

    Describe "Stop-Computer" -Tag Feature {
        # if we throw in BeforeEach, the test will fail and the stop will not be called
        BeforeEach {
            if ( ! (Test-TesthookIsSet) ) {
                throw "Testhook '${TesthookName}' is not set"
            }
        }

        AfterEach {
            Set-HookResult -result $defaultResultValue
        }

        It "Should stop the local computer" {
            Set-HookResult -result $defaultResultValue
            Stop-Computer -ea Stop| Should BeNullOrEmpty
        }

        It "Should support -computer parameter" {
            Set-HookResult -result $defaultResultValue
            Stop-Computer -computer (hostname) -ea Stop| Should BeNullOrEmpty
        }
    
        It "Should support Wsman protocol" {
            Set-HookResult -result $defaultResultValue
            Stop-Computer -Protocol Wsman -ea Stop| Should BeNullOrEmpty
        }

        It "Should support WsmanAuthentication types" {
            $authChoices = "Default","Basic","Negotiate","CredSSP","Digest","Kerberos"
            foreach ( $auth in $authChoices ) {
                Stop-Computer -WsmanAuthentication $auth | Should BeNullOrEmpty
            }
        }

        Context "Stop-Computer Error Conditions" {
            It "Should return the proper error when it occurs" {
                Set-HookResult -result 0x300000
                Stop-Computer -ev StopError 2>$null
                $StopError.Exception.Message | Should match 0x300000
            }

            It "Should produce an error when DcomAuth is specified" {
                $expected = "InvalidParameter,Microsoft.PowerShell.Commands.StopComputerCommand"
                $authChoices = "Default", "None", "Connect", "Call", "Packet", "PacketIntegrity", "PacketPrivacy", "Unchanged"
                foreach ($auth in $authChoices) {
                   { Stop-Computer -DcomAuth $auth } | ShouldBeErrorId $expected 
               }
            }

            It "Should not support impersonations" {
               { Stop-Computer -Impersonation Default } | ShouldBeErrorId "InvalidParameter,Microsoft.PowerShell.Commands.StopComputerCommand"
            }

            It "Should produce an error when 'DCOM' protocol is specified" {
                { Stop-Computer -Protocol DCOM } | ShouldBeErrorId "InvalidParameterDCOMNotSupported,Microsoft.PowerShell.Commands.StopComputerCommand"
            }

            It "Should produce an error when WsmanAuth and DComAuth are both specified" {
                { Stop-Computer -Wsmanauthentication Default -DComAuth Default } | ShouldBeErrorId "InvalidParameter,Microsoft.PowerShell.Commands.StopComputerCommand"
            }

            It "Should produce an error when 'AsJob' is specified" {
                { Stop-Computer -AsJob } | ShouldBeErrorId "NotSupported,Microsoft.PowerShell.Commands.StopComputerCommand"
            }

        }
    }

}
finally
{
    $PSDefaultParameterValues.Remove("it:skip")
    ${TesthooksType}::SetTestHook($TesthookName, $false)
    Set-HookResult -result 0
    ${TesthooksType}::SetTestHook($TesthookResultName, $DefaultResultValue)
}
