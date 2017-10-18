# note these will manipulate private data in the PowerShell engine which will
# enable us to not actually restart the system, but return right before we do
$TesthooksType = [system.management.automation.internal.internaltesthooks]
$TesthookName = "TestStopComputer"
$TesthookResultName = "TestStopComputerResults"
$DefaultResultValue = 0
function Set-HookResult([int]$result) {
    ${TesthooksType}::SetTestHook($TesthookResultName, $result)
}

# protect the user from from restarting their computer 
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

    Describe "Restart-Computer" -Tag Feature {
        # if we throw in BeforeEach, the test will fail and the restart will not be called
        BeforeEach {
            if ( ! (Test-TesthookIsSet) ) {
                throw "Testhook '${TesthookName}' is not set"
            }
        }

        AfterEach {
            Set-HookResult -result $defaultResultValue
        }

        It "Should restart the local computer" {
            Set-HookResult -result $defaultResultValue
            Restart-Computer -ea Stop| Should BeNullOrEmpty
        }

        It "Should support -computer parameter" {
            Set-HookResult -result $defaultResultValue
            $computerNames = "localhost",(hostname)
            Restart-Computer -Computer $computerNames -ea Stop| Should BeNullOrEmpty
        }

        It "Should support Wsman protocol" {
            Set-HookResult -result $defaultResultValue
            Restart-Computer -Protocol Wsman -ea Stop| Should BeNullOrEmpty
        }

        It "Should support WsmanAuthentication types" {
            $authChoices = "Default","Basic","Negotiate","CredSSP","Digest","Kerberos"
            foreach ( $auth in $authChoices ) {
                Restart-Computer -WsmanAuthentication $auth | Should BeNullOrEmpty
            }
        }

        # this requires setting a test hook, so we wrap the execution with try/finally of the 
        # set operation. Internally, we want to suppress the progress, so 
        # that is also wrapped in try/finally
        It "Should wait for a remote system" {
            try
            {
                ${TesthooksType}::SetTestHook("TestWaitStopComputer",$true)
                $timeout = 3
                try 
                {
                    $pPref = $ProgressPreference
                    $ProgressPreference="SilentlyContinue"
                    $duration = Measure-Command { 
                        Restart-Computer -computer localhost -Wait -Timeout $timeout -ea stop | Should BeNullOrEmpty 
                    }
                }
                finally 
                {
                    $ProgressPreference=$pPref
                }
                $duration.TotalSeconds | Should BeGreaterThan $timeout
            }
            finally
            {
                ${TesthooksType}::SetTestHook("TestWaitStopComputer",$false)
            }

        }

        Context "Restart-Computer Error Conditions" {
            It "Should return the proper error when it occurs" {
                Set-HookResult -result 0x300000
                Restart-Computer -ev RestartError 2>$null
                $RestartError.Exception.Message | Should match 0x300000
            }

            It "Should produce an error when DcomAuth is specified" {
                $expected = "InvalidParameterForCoreClr,Microsoft.PowerShell.Commands.RestartComputerCommand"
                $authChoices = "Default", "None", "Connect", "Call", "Packet", "PacketIntegrity", "PacketPrivacy", "Unchanged"
                foreach ($auth in $authChoices) {
                   { Restart-Computer -DcomAuth $auth } | ShouldBeErrorId $expected 
               }
            }

            It "Should not support impersonations" {
               { Restart-Computer -Impersonation Default } | ShouldBeErrorId "InvalidParameterForCoreClr,Microsoft.PowerShell.Commands.RestartComputerCommand"
            }

            It "Should produce an error when 'DCOM' protocol is specified" {
                { Restart-Computer -Protocol DCOM } | ShouldBeErrorId "InvalidParameterDCOMNotSupported,Microsoft.PowerShell.Commands.RestartComputerCommand"
            }

            It "Should produce an error when WsmanAuth and DComAuth are both specified" {
                { Restart-Computer -Wsmanauthentication Default -DComAuth Default } | ShouldBeErrorId "ParameterConfliction,Microsoft.PowerShell.Commands.RestartComputerCommand"
            }

            It "Should produce an error when 'Delay' is specified" {
                { Restart-Computer -Delay 30 } | ShouldBeErrorId "RestartComputerInvalidParameter,Microsoft.PowerShell.Commands.RestartComputerCommand"
            }

            It "Should produce an error when 'AsJob' is specified" {
                { Restart-Computer -AsJob } | ShouldBeErrorId "InvalidParameterSetAsJob,Microsoft.PowerShell.Commands.RestartComputerCommand"
            }

            It "Should not support timeout on localhost" {
                Set-HookResult -result $defaultResultValue
                { Restart-Computer -timeout 3 -ea Stop } | ShouldBeErrorId "RestartComputerInvalidParameter,Microsoft.PowerShell.Commands.RestartComputerCommand"
            }

            It "Should not support timeout on localhost" {
                Set-HookResult -result $defaultResultValue
                { Restart-Computer -timeout 3 -ea Stop } | ShouldBeErrorId "RestartComputerInvalidParameter,Microsoft.PowerShell.Commands.RestartComputerCommand"
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
