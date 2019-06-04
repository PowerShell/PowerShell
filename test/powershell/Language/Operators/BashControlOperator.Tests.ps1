# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "&& and || operators" -Tag CI {
    BeforeAll {
        function Test-SuccessfulCommand
        {
            Write-Output "SUCCESS"
        }

        function Test-NonTerminatingBadCommand
        {
            [CmdletBinding()]
            param()
            Write-Output "NTRESULT"
            $exception = [System.Exception]::new("NTERROR")
            $errorId = "NTERROR"
            $errorCategory = [System.Management.Automation.ErrorCategory]::NotSpecified

            $errorRecord = [System.Management.Automation.ErrorRecord]::new($exception, $errorId, $errorCategory, $null)
            $PSCmdlet.WriteError($errorRecord)
            Write-Output "NTRESULT2"
        }

        function Test-TerminatingBadCommand
        {
            [CmdletBinding()]
            param()

            $exception = [System.Exception]::new("BAD")
            $errorId = "BAD"
            $errorCategory = [System.Management.Automation.ErrorCategory]::NotSpecified

            $errorRecord = [System.Management.Automation.ErrorRecord]::new($exception, $errorId, $errorCategory, $null)

            $PSCmdlet.ThrowTerminatingError($errorRecord)
        }

        $simpleTestCases = @(
            @{ Statement = "testexe -returncode -1 && testexe -echoargs 'A'"; Output = @('-1') }
            @{ Statement = "testexe -returncode -1 || testexe -echoargs 'A'"; Output = '-1','Arg 0 is <A>' }
            @{ Statement = "testexe -returncode 0 && testexe -echoargs 'A'"; Output = '0','Arg 0 is <A>' }
            @{ Statement = "testexe -returncode 0 || testexe -echoargs 'A'"; Output = @('0') }

            @{ Statement = "testexe -returncode -1 && testexe -returncode -2 && testexe -echoargs 'A'"; Output = @('-1') }
            @{ Statement = "testexe -echoargs 'A' && testexe -returncode -2 && testexe -echoargs 'A'"; Output = @('Arg 0 is <A>', '-2') }
            @{ Statement = "testexe -echoargs 'A' && testexe -returncode -2 || testexe -echoargs 'B'"; Output = @('Arg 0 is <A>', '-2', 'Arg 0 is <B>') }
            @{ Statement = "testexe -returncode -1 || testexe -returncode -2 && testexe -echoargs 'A'"; Output = @('-1', '-2') }
            @{ Statement = "testexe -returncode -1 || testexe -returncode -2 || testexe -echoargs 'B'"; Output = @('-1', '-2', 'Arg 0 is <B>') }

            @{ Statement = 'Test-SuccessfulCommand && testexe -returncode 0'; Output = @('SUCCESS', '0') }
            @{ Statement = 'testexe -returncode 0 && Test-SuccessfulCommand'; Output = @('0', 'SUCCESS') }
            @{ Statement = 'Test-SuccessfulCommand && testexe -returncode 1'; Output = @('SUCCESS', '1') }
            @{ Statement = 'testexe -returncode 1 && Test-SuccessfulCommand'; Output = @('1') }

            @{ Statement = 'Test-NonTerminatingBadCommand && testexe -returncode 0'; Output = @('NTRESULT', 'NTRESULT2') }
            @{ Statement = 'testexe -returncode 0 && Test-NonTerminatingBadCommand'; Output = @('0', 'NTRESULT', 'NTRESULT2') }
            @{ Statement = 'Test-NonTerminatingBadCommand || testexe -returncode 0'; Output = @('NTRESULT', 'NTRESULT2', '0') }
            @{ Statement = 'testexe -returncode 0 || Test-NonTerminatingBadCommand'; Output = @('0') }

            @{ Statement = '"hi" && testexe -returncode 0'; Output = @('hi', '0') }
            @{ Statement = 'testexe -returncode 0 && "Hi"'; Output = @('0', 'Hi') }
            @{ Statement = '"hi" || testexe -returncode 0'; Output = @('hi') }
            @{ Statement = 'testexe -returncode 0 || "hi"'; Output = @('0') }
        )

        $variableTestCases = @(
        )

        $redirectionTestCases = @(

        )
    }

    It "Gets the correct output with statement '<Statement>'" -TestCases $simpleTestCases {
        param($Statement, $Output)

        Invoke-Expression -Command $Statement 2>$null | Should -Be $Output
    }

    It "Sets the variable correctly with statement '<Statement>'" -TestCases $variableTestCases {

    }

    It "Handles redirection correctly with statement '<Statement>'" -TestCases $redirectionTestCases {

    }
}
