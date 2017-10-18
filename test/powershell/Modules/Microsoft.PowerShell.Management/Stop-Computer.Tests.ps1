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
            $computerNames = "localhost",(hostname)
            Stop-Computer -computer $computerNames -ea Stop| Should BeNullOrEmpty
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
