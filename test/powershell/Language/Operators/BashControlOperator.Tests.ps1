# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Experimental Feature: && and || operators - Feature-Enabled" -Tag CI {
    BeforeAll {
        $configFilePath = Join-Path $testdrive "experimentalfeature.json"

        @"
        {
            "ExperimentalFeatures": [
              "PSBashCommandOperators"
            ]
        }
"@ > $configFilePath

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
            # Two native commands
            @{ Statement = "testexe -returncode -1 && testexe -echoargs 'A'"; Output = @('-1') }
            @{ Statement = "testexe -returncode -1 || testexe -echoargs 'A'"; Output = '-1','Arg 0 is <A>' }
            @{ Statement = "testexe -returncode 0 && testexe -echoargs 'A'"; Output = '0','Arg 0 is <A>' }
            @{ Statement = "testexe -returncode 0 || testexe -echoargs 'A'"; Output = @('0') }

            # Three native commands
            @{ Statement = "testexe -returncode -1 && testexe -returncode -2 && testexe -echoargs 'A'"; Output = @('-1') }
            @{ Statement = "testexe -echoargs 'A' && testexe -returncode -2 && testexe -echoargs 'A'"; Output = @('Arg 0 is <A>', '-2') }
            @{ Statement = "testexe -echoargs 'A' && testexe -returncode -2 || testexe -echoargs 'B'"; Output = @('Arg 0 is <A>', '-2', 'Arg 0 is <B>') }
            @{ Statement = "testexe -returncode -1 || testexe -returncode -2 && testexe -echoargs 'A'"; Output = @('-1', '-2') }
            @{ Statement = "testexe -returncode -1 || testexe -returncode -2 || testexe -echoargs 'B'"; Output = @('-1', '-2', 'Arg 0 is <B>') }

            # Native command and succesful cmdlet
            @{ Statement = 'Test-SuccessfulCommand && testexe -returncode 0'; Output = @('SUCCESS', '0') }
            @{ Statement = 'testexe -returncode 0 && Test-SuccessfulCommand'; Output = @('0', 'SUCCESS') }
            @{ Statement = 'Test-SuccessfulCommand && testexe -returncode 1'; Output = @('SUCCESS', '1') }
            @{ Statement = 'testexe -returncode 1 && Test-SuccessfulCommand'; Output = @('1') }

            # Native command and non-terminating unsuccessful cmdlet
            @{ Statement = 'Test-NonTerminatingBadCommand && testexe -returncode 0'; Output = @('NTRESULT', 'NTRESULT2') }
            @{ Statement = 'testexe -returncode 0 && Test-NonTerminatingBadCommand'; Output = @('0', 'NTRESULT', 'NTRESULT2') }
            @{ Statement = 'Test-NonTerminatingBadCommand || testexe -returncode 0'; Output = @('NTRESULT', 'NTRESULT2', '0') }
            @{ Statement = 'testexe -returncode 0 || Test-NonTerminatingBadCommand'; Output = @('0') }

            # Expression and native command
            @{ Statement = '"hi" && testexe -returncode 0'; Output = @('hi', '0') }
            @{ Statement = 'testexe -returncode 0 && "Hi"'; Output = @('0', 'Hi') }
            @{ Statement = '"hi" || testexe -returncode 0'; Output = @('hi') }
            @{ Statement = 'testexe -returncode 0 || "hi"'; Output = @('0') }
            @{ Statement = '"hi" && testexe -returncode 1'; Output = @('hi', '1') }
            @{ Statement = 'testexe -returncode 1 && "Hi"'; Output = @('1') }
            @{ Statement = 'testexe -returncode 1 || "hi"'; Output = @('1', 'hi') }

            # Pipeline and native command
            @{ Statement = '1,2,3 | % { $_ + 1 } && testexe -returncode 0'; Output = @('2','3','4','0') }
            @{ Statement = 'testexe -returncode 0 && 1,2,3 | % { $_ + 1 }'; Output = @('0','2','3','4') }
            @{ Statement = 'testexe -returncode 1 && 1,2,3 | % { $_ + 1 }'; Output = @('1') }
            @{ Statement = 'testexe -returncode 1 || 1,2,3 | % { $_ + 1 }'; Output = @('1','2','3','4') }
            @{ Statement = 'testexe -returncode 1 | % { [int]$_ + 1 } && testexe -returncode 0'; Output = @('2') }
            @{ Statement = 'testexe -returncode 1 | % { [int]$_ + 1 } || testexe -returncode 0'; Output = @('2', '0') }
            @{ Statement = '0,1 | % { testexe -returncode $_ } && testexe -returncode 0'; Output = @('0','1','0') }
            @{ Statement = '1,2 | % { testexe -returncode $_ } && testexe -returncode 0'; Output = @('1','2','0') }

            # Control flow statements
            @{ Statement = 'foreach ($v in 0,1,2) { testexe -returncode $v || break }'; Output = @('0', '1') }
            @{ Statement = 'foreach ($v in 0,1,2) { testexe -returncode $v || continue; $v + 1 }'; Output = @('0', 1, '1', '2') }
            @{ Statement = 'function Test-ControlFlow { foreach ($v in 0,1,2) { testexe -returncode $v || return 10 } }; Test-ControlFlow'; Output = @('0', '1', 10) }
        )

        $throwTestCases = @(
            # Command that throws
            @{ Statement = "testexe -returncode 0 && Test-TerminatingBadCommand"; Output = @('0'); ErrorID = 'BAD,Test-TerminatingBadCommand' }
            @{ Statement = "Test-TerminatingBadCommand && testexe -returncode 0"; Output = @(); ErrorID = 'BAD,Test-TerminatingBadCommand' }
            @{ Statement = "Test-TerminatingBadCommand || testexe -returncode 0"; Output = @(); ErrorID = 'BAD,Test-TerminatingBadCommand' }
            @{ Statement = "testexe -returncode 0 || Test-TerminatingBadCommand"; Output = @('0') }

            # Throw directly
            @{ Statement = "testexe -returncode 0 && throw 'Bad'"; Output = @('0'); ErrorID = 'Bad' }
            @{ Statement = "testexe -returncode 0 || throw 'Bad'"; Output = @('0') }

            # Confusing edge case in PS syntax
            @{ Statement = 'testexe -returncode 0 && throw "Bad" &'; ErrorID = 'System.Management.Automation.PSRemotingJob' }

            # Where pipeline is subordinate to `throw`
            @{ Statement = 'throw "Bad" || testexe -returncode 0'; ErrorId = 'Bad' }
            @{ Statement = 'throw "Bad" && testexe -returncode 0'; ErrorId = 'Bad 0' }
        )

        $variableTestCases = @(
            @{ Statement = '$x = testexe -returncode 0 && testexe -returncode 1'; Variables = @{ x = '0','1' } }
            @{ Statement = '$x = testexe -returncode 0 || testexe -returncode 1'; Variables = @{ x = '0' } }
            @{ Statement = '$x = testexe -returncode 1 || testexe -returncode 0'; Variables = @{ x = '1','0' } }
            @{ Statement = '$x = testexe -returncode 1 && testexe -returncode 0'; Variables = @{ x = '1' } }
        )

        $jobTestCases = @(
            @{ Statement = 'testexe -returncode 0 && testexe -returncode 1 &'; Output = @('0', '1') }
            @{ Statement = 'testexe -returncode 1 && testexe -returncode 0 &'; Output = @('1') }
            @{ Statement = '$x = testexe -returncode 0 && Write-Output "mice" &'; Output = '0','mice'; Variable = 'x' }
        )

        $invalidSyntaxCases = @(
            @{ Statement = 'testexe -returncode 0 & && testexe -returncode 1'; ErrorID = 'X' }
            @{ Statement = 'testexe -returncode 0 && testexe -returncode 1 && &'; ErrorID = 'X' }
        )
    }

    It "Gets the correct output with statement '<Statement>'" -TestCases $simpleTestCases {
        param($Statement, $Output)

        Invoke-Expression -Command $Statement 2>$null | Should -Be $Output
    }

    It "Gets the correct output and errorID with terminating statement '<Statement>'" -TestCases $throwTestCases {
        param($Statement, $Output, $ErrorID)

        try
        {
            $result = Invoke-Expression -Command $Statement
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should -Be $ErrorID
        }

        $result | Should -Be $Output
    }

    It "Sets the variable correctly with statement '<Statement>'" -TestCases $variableTestCases {
        param($Statement, $Variables)

        Invoke-Expression -Command $Statement
        foreach ($variableName in $Variables.get_Keys())
        {
            (Get-Variable -Name $variableName -ErrorAction Ignore).Value | Should -Be $Variables[$variableName]
        }
    }

    It "Runs the statement chain '<Statement>' as a job" -TestCases $jobTestCases {
        param($Statement, $Output, $Variable)

        $resultJob = Invoke-Expression -Command $Statement

        if ($Variable)
        {
            $resultJob = (Get-Variable $Variable).Value
        }

        $resultJob | Wait-Job | Receive-Job | Should -Be $Output
    }

    It "Rejects invalid syntax usage in '<Statement>'" -TestCases $invalidSyntaxCases {
        param($Statement, $ErrorID)

        { Invoke-Expression -Command $Statement } | Should -Throw -ErrorId $ErrorID
    }

    Context "File redirection with && and ||" {
        BeforeAll {
            $redirectionTestCases = @(
                @{ Statement = "testexe -returncode 0 > $TestDrive/1.txt && testexe -returncode 1 > $TestDrive/2.txt"; Files = @{ "$TestDrive/1.txt" = '0'; "$TestDrive/2.txt" = '1' } }
                @{ Statement = "testexe -returncode 1 > $TestDrive/1.txt && testexe -returncode 1 > $TestDrive/2.txt"; Files = @{ "$TestDrive/1.txt" = '1'; "$TestDrive/2.txt" = $null } }
                @{ Statement = "testexe -returncode 1 > $TestDrive/1.txt || testexe -returncode 1 > $TestDrive/2.txt"; Files = @{ "$TestDrive/1.txt" = '1'; "$TestDrive/2.txt" = '1' } }
                @{ Statement = "testexe -returncode 0 > $TestDrive/1.txt || testexe -returncode 1 > $TestDrive/2.txt"; Files = @{ "$TestDrive/1.txt" = '0'; "$TestDrive/2.txt" = $null } }
                @{ Statement = "(testexe -returncode 0 && testexe -returncode 1) > $TestDrive/3.txt"; Files = @{ "$TestDrive/3.txt" = "0$([System.Environment]::NewLine)1$([System.Environment]::NewLine)" } }
                @{ Statement = "(testexe -returncode 0 > $TestDrive/1.txt && testexe -returncode 1 > $TestDrive/2.txt) > $TestDrive/3.txt"; Files = @{ "$TestDrive/1.txt" = '0'; "$TestDrive/2.txt" = '1'; "$TestDrive/3.txt" = '' } }
            )
        }

        BeforeEach {
            Remove-Item -Path $TestDrive/*
        }

        It "Handles redirection correctly with statement '<Statement>'" -TestCases $redirectionTestCases {
            param($Statement, $Files)

            Invoke-Expression -Command $Statement

            foreach ($file in $Files.get_Keys())
            {
                $expectedValue = $Files[$file]

                if ($null -eq $expectedValue)
                {
                    $file | Should -Not -Exist
                    continue
                }

                # Special case for empty file
                if ($expectedValue -eq '')
                {
                    (Get-Item $file).Length | Should -Be 0
                    continue
                }


                $file | Should -FileContentMatchMultiline $expectedValue
            }
        }
    }
}
