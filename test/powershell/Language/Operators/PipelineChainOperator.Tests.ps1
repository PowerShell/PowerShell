# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Experimental Feature: && and || operators - Feature-Enabled" -Tag CI {
    BeforeAll {
        function Test-SuccessfulCommand
        {
            Write-Output "SUCCESS"
        }

        filter Test-NonTerminatingError
        {
            [CmdletBinding()]
            param(
                [Parameter(ValueFromPipeline)]
                [object[]]
                $InputObject
            )

            if ($InputObject -ne 2)
            {
                return $InputObject
            }

            $exception = [System.Exception]::new("NTERROR")
            $errorId = 'NTERROR'
            $errorCategory = [System.Management.Automation.ErrorCategory]::NotSpecified

            $errorRecord = [System.Management.Automation.ErrorRecord]::new($exception, $errorId, $errorCategory, $null)
            $PSCmdlet.WriteError($errorRecord)
        }

        $simpleTestCases = @(
            # Two native commands
            @{ Statement = 'testexe -returncode -1 && testexe -echoargs "A"'; Output = @('-1') }
            @{ Statement = '& "testexe" -returncode -1 && & "testexe" -echoargs "A"'; Output = @('-1') }
            @{ Statement = 'testexe -returncode -1 || testexe -echoargs "A"'; Output = '-1','Arg 0 is <A>' }
            @{ Statement = 'testexe -returncode 0 && testexe -echoargs "A"'; Output = '0','Arg 0 is <A>' }
            @{ Statement = 'testexe -returncode 0 || testexe -echoargs "A"'; Output = @('0') }
            @{ Statement = 'testexe -returncode 0 > $null && testexe -returncode 1'; Output = @('1') }
            @{ Statement = 'testexe -returncode 0 && testexe -echoargs "A" > $null'; Output = @('0') }

            # Three native commands
            @{ Statement = 'testexe -returncode -1 && testexe -returncode -2 && testexe -echoargs "A"'; Output = @('-1') }
            @{ Statement = 'testexe -echoargs "A" && testexe -returncode -2 && testexe -echoargs "A"'; Output = @('Arg 0 is <A>', '-2') }
            @{ Statement = 'testexe -echoargs "A" && testexe -returncode -2 || testexe -echoargs "B"'; Output = @('Arg 0 is <A>', '-2', 'Arg 0 is <B>') }
            @{ Statement = 'testexe -returncode -1 || testexe -returncode -2 && testexe -echoargs "A"'; Output = @('-1', '-2') }
            @{ Statement = 'testexe -returncode -1 || testexe -returncode -2 || testexe -echoargs "B"'; Output = @('-1', '-2', 'Arg 0 is <B>') }

            # Native command and successful cmdlet
            @{ Statement = 'Test-SuccessfulCommand && testexe -returncode 0'; Output = @('SUCCESS', '0') }
            @{ Statement = 'testexe -returncode 0 && Test-SuccessfulCommand'; Output = @('0', 'SUCCESS') }
            @{ Statement = 'Test-SuccessfulCommand && testexe -returncode 1'; Output = @('SUCCESS', '1') }
            @{ Statement = 'testexe -returncode 1 && Test-SuccessfulCommand'; Output = @('1') }

            # Native command and non-terminating unsuccessful cmdlet
            @{ Statement = '1,2 | Test-NonTerminatingError && testexe -returncode 0'; Output = @(1) }
            @{ Statement = 'testexe -returncode 0 && 1,2 | Test-NonTerminatingError'; Output = @('0', 1) }
            @{ Statement = '1,2 | Test-NonTerminatingError || testexe -returncode 0'; Output = @(1, '0') }
            @{ Statement = 'testexe -returncode 0 || 1, 2 | Test-NonTerminatingError'; Output = @('0') }

            # Expression and native command
            @{ Statement = '"hi" && testexe -returncode 0'; Output = @('hi', '0') }
            @{ Statement = 'testexe -returncode 0 && "Hi"'; Output = @('0', 'Hi') }
            @{ Statement = '"hi" || testexe -returncode 0'; Output = @('hi') }
            @{ Statement = 'testexe -returncode 0 || "hi"'; Output = @('0') }
            @{ Statement = '"hi" && testexe -returncode 1'; Output = @('hi', '1') }
            @{ Statement = 'testexe -returncode 1 && "Hi"'; Output = @('1') }
            @{ Statement = 'testexe -returncode 1 || "hi"'; Output = @('1', 'hi') }
            @{ Statement = '"hello" && get-item macarena_doesnotexist || "there"; "hello" || "there"'; Output = @('hello', 'there', 'hello') }
            @{ Statement = '1 + 1 && "Hi"'; Output = @(2, 'Hi') }

            # Pipeline and native command
            @{ Statement = '1,2,3 | % { $_ + 1 } && testexe -returncode 0'; Output = @('2','3','4','0') }
            @{ Statement = 'testexe -returncode 0 && 1,2,3 | % { $_ + 1 }'; Output = @('0','2','3','4') }
            @{ Statement = 'testexe -returncode 1 && 1,2,3 | % { $_ + 1 }'; Output = @('1') }
            @{ Statement = 'testexe -returncode 1 || 1,2,3 | % { $_ + 1 }'; Output = @('1','2','3','4') }
            @{ Statement = 'testexe -returncode 1 | % { [int]$_ + 1 } && testexe -returncode 0'; Output = @('2') }
            @{ Statement = 'testexe -returncode 1 | % { [int]$_ + 1 } || testexe -returncode 0'; Output = @('2', '0') }
            @{ Statement = '0,1 | % { testexe -returncode $_ } && testexe -returncode 0'; Output = @('0','1','0') }
            @{ Statement = '1,2 | % { testexe -returncode $_ } && testexe -returncode 0'; Output = @('1','2','0') }

            # Subpipeline and subexpression cases
            @{ Statement = '(testexe -returncode 0 && testexe -returncode 1) && "Hi"'; Output = @('0','1') }
            @{ Statement = '$(testexe -returncode 0 && testexe -returncode 1) && "Hi"'; Output = @('0','1') }
            @{ Statement = '(testexe -returncode 0 && testexe -returncode 1) || "Bad"'; Output = @('0','1','Bad') }
            @{ Statement = '$(testexe -returncode 0 && testexe -returncode 1) && "Bad"'; Output = @('0','1') }
            @{ Statement = '(testexe -returncode 1 || testexe -returncode 1) && "Hi"'; Output = @('1','1') }

            # Control flow statements
            @{ Statement = 'foreach ($v in 0,1,2) { testexe -returncode $v || $(break) }'; Output = @('0', '1') }
            @{ Statement = 'foreach ($v in 0,1,2) { testexe -returncode $v || $(continue); $v + 1 }'; Output = @('0', 1, '1', '2') }

            # Use in conditionals
            @{ Statement = 'if ($false && $true) { "Hi" }'; Output = 'Hi' }
            @{ Statement = 'if ("Hello" && testexe -return 1) { $?; 1 } else { throw "else" }'; Output = $true,1 }
            @{ Statement = 'if ("Hello" && Write-Error "Bad" -ErrorAction Ignore) { $?; 1 } else { throw "else" }'; Output = $true,1 }
            @{ Statement = 'if (Write-Error "Bad" -ErrorAction Ignore && "Hello") { throw "if" } else { $?; 1 }'; Output = $true,1 }
            @{ Statement = 'if ($y = $x = "Hello" && testexe -returncode 1) { $?; $x; $y } else { throw "else" }'; Output = $true,'Hello',1,'Hello',1 }
        )

        $variableTestCases = @(
            @{ Statement = '$x = testexe -returncode 0 && testexe -returncode 1'; Variables = @{ x = '0','1' } }
            @{ Statement = '$x = testexe -returncode 0 || testexe -returncode 1'; Variables = @{ x = '0' } }
            @{ Statement = '$x = testexe -returncode 1 || testexe -returncode 0'; Variables = @{ x = '1','0' } }
            @{ Statement = '$x = testexe -returncode 1 && testexe -returncode 0'; Variables = @{ x = '1' } }
            @{ Statement = '$x = @(1); $x += testexe -returncode 0 && testexe -returncode 1'; Variables = @{ x = 1,'0','1' } }
            @{ Statement = '$x = $y = testexe -returncode 0 && testexe -returncode 1'; Variables = @{ x = '0','1'; y = '0','1' } }
            @{ Statement = '$x, $y = $z = testexe -returncode 0 && testexe -returncode 1'; Variables = @{ x = '0'; y = '1'; z = '0','1' } }
            @{ Statement = '$x = @(1); $v = $w, $y = $x += $z = testexe -returncode 0 && testexe -returncode 1'; Variables = @{ v = '1',@('0','1'); w = '1'; y = '0','1'; x = '1','0','1'; z = '0','1' } }
            @{ Statement = '$x = 1 && @(2, 3)'; Variables = @{ x = 1,2,3 } }
            @{ Statement = '$x = 1 && ,@(2, 3)'; Variables = @{ x = 1,@(2,3) } }
            @{ Statement = '$x = 1 && 2,@(3, 4)'; Variables = @{ x = 1,2,@(3,4) } }
        )

        $jobTestCases = @(
            @{ Statement = 'testexe -returncode 0 && testexe -returncode 1 &'; Output = @('0', '1') }
            @{ Statement = 'testexe -returncode 1 && testexe -returncode 0 &'; Output = @('1') }
            @{ Statement = '$x = testexe -returncode 0 && Write-Output "mice" &'; Output = '0','mice'; Variable = 'x' }
        )

        $invalidSyntaxCases = @(
            @{ Statement = 'testexe -returncode 0 & && testexe -returncode 1'; ErrorID = 'BackgroundOperatorInPipelineChain' }
            @{ Statement = 'testexe -returncode 0 && '; ErrorID = 'EmptyPipelineChainElement'; IncompleteInput = $true }
            @{ Statement = 'testexe -returncode 0 && testexe -returncode 1 && &'; ErrorID = 'MissingExpression' }
        )
    }

    It "Gets the correct output with statement '<Statement>'" -TestCases $simpleTestCases {
        param($Statement, $Output)

        $result = Invoke-Expression -Command $Statement 2>$null
        $result | Should -Be $Output
    }

    It "Sets the variable correctly with statement '<Statement>'" -TestCases $variableTestCases {
        param($Statement, $Variables)

        Invoke-Expression -Command $Statement
        foreach ($variableName in $Variables.get_Keys())
        {
            (Get-Variable -Name $variableName -ErrorAction Ignore).Value | Should -Be $Variables[$variableName] -Because "variable is '`$$variableName'"
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
        param([string]$Statement, [string]$ErrorID, [bool]$IncompleteInput)

        $tokens = $errors = $null
        [System.Management.Automation.Language.Parser]::ParseInput($Statement, [ref]$tokens, [ref]$errors)

        $errors.Count | Should -BeExactly 1
        $errors[0].ErrorId | Should -BeExactly $ErrorID
        $errors[0].IncompleteInput | Should -Be $IncompleteInput
    }

    Context "File redirection with && and ||" {
        BeforeAll {
            $redirectionTestCases = @(
                @{ Statement = "testexe -returncode 0 > '$TestDrive/1.txt' && testexe -returncode 1 > '$TestDrive/2.txt'"; Files = @{ "$TestDrive/1.txt" = '0'; "$TestDrive/2.txt" = '1' } }
                @{ Statement = "testexe -returncode 1 > '$TestDrive/1.txt' && testexe -returncode 1 > '$TestDrive/2.txt'"; Files = @{ "$TestDrive/1.txt" = '1'; "$TestDrive/2.txt" = $null } }
                @{ Statement = "testexe -returncode 1 > '$TestDrive/1.txt' || testexe -returncode 1 > '$TestDrive/2.txt'"; Files = @{ "$TestDrive/1.txt" = '1'; "$TestDrive/2.txt" = '1' } }
                @{ Statement = "testexe -returncode 0 > '$TestDrive/1.txt' || testexe -returncode 1 > '$TestDrive/2.txt'"; Files = @{ "$TestDrive/1.txt" = '0'; "$TestDrive/2.txt" = $null } }
                @{ Statement = "(testexe -returncode 0 && testexe -returncode 1) > '$TestDrive/3.txt'"; Files = @{ "$TestDrive/3.txt" = "0$([System.Environment]::NewLine)1$([System.Environment]::NewLine)" } }
                @{ Statement = "(testexe -returncode 0 && testexe -returncode 1 > '$TestDrive/2.txt') > '$TestDrive/3.txt'"; Files = @{ "$TestDrive/2.txt" = '1'; "$TestDrive/3.txt" = '0' } }
                @{ Statement = "(testexe -returncode 0 > '$TestDrive/1.txt' && testexe -returncode 1 > '$TestDrive/2.txt') > '$TestDrive/3.txt'"; Files = @{ "$TestDrive/1.txt" = '0'; "$TestDrive/2.txt" = '1'; "$TestDrive/3.txt" = '' } }
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

    Context "Pipeline chain error semantics" {
        BeforeAll {
            $pwsh = [powershell]::Create()

            $pwsh.AddScript(@'
filter Test-NonTerminatingError
{
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline)]
        [object[]]
        $input
    )

    if ($input -ne 2)
    {
        return $input
    }

    $exception = [System.Exception]::new("NTERROR")
    $errorId = "NTERROR"
    $errorCategory = [System.Management.Automation.ErrorCategory]::NotSpecified

    $errorRecord = [System.Management.Automation.ErrorRecord]::new($exception, $errorId, $errorCategory, $null)
    $PSCmdlet.WriteError($errorRecord)
}

filter Test-PipelineTerminatingError
{
    [CmdletBinding()]
    param([Parameter(ValueFromPipeline)][int[]]$input)

    if ($input -ne 4)
    {
        return $input
    }

    $exception = [System.Exception]::new("PIPELINE")
    $errorId = "PIPELINE"
    $errorCategory = [System.Management.Automation.ErrorCategory]::NotSpecified

    $errorRecord = [System.Management.Automation.ErrorRecord]::new($exception, $errorId, $errorCategory, $null)

    $PSCmdlet.ThrowTerminatingError($errorRecord)
}

function Test-FullyTerminatingError
{
    throw 'TERMINATE'
}
'@).Invoke()

            $errorSemanticsCases = @(
                # Simple error semantics
                @{ Statement = '1,2,3 | Test-NonTerminatingError || Write-Output 4'; Output = @(1, 3, 4); NTErrors = @('NTError') }
                @{ Statement = '1,3,4,5 | Test-PipelineTerminatingError || Write-Output 2'; Output = @(1, 3, 2); NTErrors = @('PIPELINE') }
                @{ Statement = 'Test-FullyTerminatingError || Write-Output 2'; ThrownError = 'TERMINATE' }
                @{ Statement = '1,2,3 | Test-NonTerminatingError -ErrorAction Stop || Write-Output 4'; ThrownError = 'NTERROR,Test-NonTerminatingError' }

                # Assignment error semantics
                @{ Statement = '$x = 1,2,3 | Test-NonTerminatingError || Write-Output 4; $x'; Output = @(1, 3, 4); NTErrors = @('NTError') }
                @{ Statement = '$x = 1,3,4,5 | Test-PipelineTerminatingError || Write-Output 2; $x'; Output = @(1, 3, 2); NTErrors = @('PIPELINE') }

                # Try/catch semantics
                @{ Statement = 'try { Write-Output 2 && Test-FullyTerminatingError } catch {}'; Output = @(2) }
                @{ Statement = 'try { 1,3,4,5 | Test-PipelineTerminatingError || Write-Output 2 } catch {}'; Output = @(1, 3) }
                @{ Statement = 'try { 1,2,3 | Test-NonTerminatingError -ErrorAction Stop || Write-Output 4 } catch {}'; Output = @(1) }
                @{ Statement = 'try { 1,2,3 | Test-NonTerminatingError || Write-Output 4 } catch {}'; Output = @(1, 3, 4); NTErrors = @('NTError') }
                @{ Statement = 'try { $result = Write-Output 2 && Test-FullyTerminatingError } catch {}; $result'; Output = @() }
                @{ Statement = 'try { $result = 1,3,4,5 | Test-PipelineTerminatingError || Write-Output 2 } catch {}; $result'; Output = @() }
                @{ Statement = 'try { $result = 1,2,3 | Test-NonTerminatingError -ErrorAction Stop || Write-Output 4 } catch {}; $result'; Output = @() }
                @{ Statement = 'try { $result = 1,2,3 | Test-NonTerminatingError || Write-Output 4 } catch {}; $result'; Output = @(1, 3, 4); NTErrors = @('NTError') }
                @{ Statement = 'try { "Hi" && "Bye" } catch { "Nothing" }'; Output = @("Hi", "Bye") }
                @{ Statement = 'try { "Hi" && "Bye" } catch { "Nothing" } finally { "Final" }'; Output = @("Hi", "Bye", "Final") }
                @{ Statement = 'try { "Hi" && Test-FullyTerminatingError || "Bye" } catch { "Nothing" } finally { "Final" }'; Output = @("Hi", "Nothing", "Final") }

                # Trap continue semantics
                @{ Statement = 'trap { continue }; Write-Output 2 && Test-FullyTerminatingError'; Output = @(2) }
                @{ Statement = 'trap { continue }; Test-FullyTerminatingError && Write-Output 2'; Output = @() }
                @{ Statement = 'trap { continue }; Test-FullyTerminatingError || Write-Output 2'; Output = @(2) }
                @{ Statement = 'trap { continue }; 1,3,4,5 | Test-PipelineTerminatingError || Write-Output 2'; Output = @(1,3,2) }
                @{ Statement = 'trap { continue }; 1,2,3 | Test-NonTerminatingError -ErrorAction Stop || Write-Output 4'; Output = @(1,4) }
                @{ Statement = 'trap { continue }; 1,2,3 | Test-NonTerminatingError || Write-Output 4'; Output = @(1,3,4); NTErrors = @('NTError') }
                @{ Statement = 'trap { continue }; $result = Write-Output 2 && Test-FullyTerminatingError'; Output = @() }
                @{ Statement = 'trap { continue }; $result = 1,3,4,5 | Test-PipelineTerminatingError || Write-Output 2; $result'; Output = @(1,3,2) }
                @{ Statement = 'trap { continue }; $result = 1,2,3 | Test-NonTerminatingError -ErrorAction Stop || Write-Output 4; $result'; Output = @(1,4) }
                @{ Statement = 'trap { continue }; $result = 1,2,3 | Test-NonTerminatingError || Write-Output 4; $result'; Output = @(1,3,4); NTErrors = @('NTError') }

                # Trap break semantics
                @{ Statement = 'trap { break }; 1,3,4,5 | Test-PipelineTerminatingError || Write-Output 2'; ThrownError = 'PIPELINE,Test-PipelineTerminatingError' }
                @{ Statement = 'trap { break }; 1,2,3 | Test-NonTerminatingError -ErrorAction Stop || Write-Output 4'; ThrownError = 'NTERROR,Test-NonTerminatingError' }
                @{ Statement = 'trap { break }; 1,2,3 | Test-NonTerminatingError || Write-Output 4'; Output = @(1,3,4); NTErrors = @('NTError') }
                @{ Statement = 'trap { break }; $result = Write-Output 2 && Test-FullyTerminatingError'; ThrownError = 'TERMINATE' }
                @{ Statement = 'trap { break }; $result = 1,3,4,5 | Test-PipelineTerminatingError || Write-Output 2; $result'; ThrownError = 'PIPELINE,Test-PipelineTerminatingError' }
                @{ Statement = 'trap { break }; $result = 1,2,3 | Test-NonTerminatingError -ErrorAction Stop || Write-Output 4; $result'; ThrownError = 'NTERROR,Test-NonTerminatingError' }
                @{ Statement = 'trap { break }; $result = 1,2,3 | Test-NonTerminatingError || Write-Output 4; $result'; Output = @(1,3,4); NTErrors = @('NTError') }
            )
        }

        AfterEach {
            $pwsh.Commands.Clear()
            $pwsh.AddScript('Remove-Variable -Name result').Invoke()
            $pwsh.Commands.Clear()
            $pwsh.Streams.ClearStreams()
        }

        AfterAll {
            $pwsh.Dispose()
        }

        It "Uses the correct error semantics with statement '<Statement>'" -TestCases $errorSemanticsCases {
            param([string]$Statement, $Output, [array]$NTErrors, [string]$ThrownError)

            try
            {
                $result = $pwsh.AddScript($Statement).Invoke()
            }
            catch
            {
                $_.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly $ThrownError
            }

            $result | Should -Be $Output
            $pwsh.Streams.Error | Should -Be $NTErrors
        }
    }

    It "Recognises invalid assignment" {
        {
            Invoke-Expression -Command '$x = $x, $y += $z = testexe -returncode 0 && testexe -returncode 1'
        } | Should -Throw -ErrorId 'InvalidLeftHandSide,Microsoft.PowerShell.Commands.InvokeExpressionCommand'
    }
}
