# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# note these will manipulate private data in the PowerShell engine which will
# enable us to not actually stop the system, but return right before we do
$stopTesthook = "TestStopComputer"
$stopTesthookResultName = "TestStopComputerResults"
$DefaultResultValue = 0

try
{
    # set up for testing
    Enable-Testhook -testhookName $stopTesthook

    Describe "Stop-Computer" -Tag Feature {
        # if we throw in BeforeEach, the test will fail and the stop will not be called
        BeforeEach {
            if ( ! (Test-TesthookIsSet -testhookName $stopTesthook) ) {
                throw "Testhook '${stopTesthook}' is not set"
            }
        }

        AfterEach {
            Set-TesthookResult -testhookName $stopTesthookResultName -Value $defaultResultValue
        }

        It "Should stop the local computer" {
            Set-TesthookResult -testhookName $stopTesthookResultName -Value $defaultResultValue
            Stop-Computer -ErrorAction Stop | Should -BeNullOrEmpty
        }

        It "Should support -Computer parameter" -Skip:(!$IsWindows) {
            Set-TesthookResult -testhookName $stopTesthookResultName -Value $defaultResultValue
            $computerNames = "localhost","${env:COMPUTERNAME}"
            Stop-Computer -Computer $computerNames -ErrorAction Stop | Should -BeNullOrEmpty
        }

        It "Should support WsmanAuthentication types" -Skip:(!$IsWindows) {
            $authChoices = "Default","Basic","Negotiate","CredSSP","Digest","Kerberos"
            foreach ( $auth in $authChoices ) {
                Stop-Computer -WsmanAuthentication $auth | Should -BeNullOrEmpty
            }
        }

        Context "Stop-Computer Error Conditions" {
            It "Should return the proper error when it occurs" {
                Set-TesthookResult -testhookName $stopTesthookResultName -Value 0x300000
                Stop-Computer -ErrorVariable StopError 2> $null
                $StopError.Exception.Message | Should -Match 0x300000
            }
        }
    }

}
finally
{
    $PSDefaultParameterValues.Remove("it:skip")
    Disable-Testhook -testhookName $stopTesthook
    Set-TesthookResult -testhookName $stopTesthookResultName -Value $DefaultResultValue
}

Describe 'Non-admin on Unix' {
    BeforeAll {
        if ([environment]::IsPrivilegedProcess) {
            $IsWindowsOrSudo = $true
        }
        else {
            $IsWindowsOrSudo = $false
        }
    }

    It 'Reports error if not run under sudo' -Skip:($IsWindowsOrSudo) {
        { Stop-Computer -ErrorAction Stop } | Should -Throw -ErrorId "CommandFailed,Microsoft.PowerShell.Commands.StopComputerCommand"
    }
}
