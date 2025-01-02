# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Error handling within a single 'Clean' block" -Tag 'CI' {

    BeforeAll {
        function ErrorInClean {
            [CmdletBinding()]
            param(
                [switch] $ThrowTerminatingError,
                [switch] $ErrorActionStop,
                [switch] $ThrowException,
                [switch] $WriteErrorAPI,
                [switch] $WriteErrorCmdlet,
                [switch] $MethodInvocationThrowException,
                [switch] $ExpressionThrowException
            )

            End { <# use an empty end block to allow the clean block to actually run #> }

            clean {
                if ($ThrowTerminatingError) {
                    $ex = [System.ArgumentException]::new('terminating-exception')
                    $er = [System.Management.Automation.ErrorRecord]::new($ex, 'ThrowTerminatingError:error', 'InvalidArgument', $null)
                    $PSCmdlet.ThrowTerminatingError($er)
                    Write-Verbose -Verbose "verbose-message"
                }
                elseif ($ErrorActionStop) {
                    Get-Command NonExist -ErrorAction Stop
                    Write-Verbose -Verbose "verbose-message"
                }
                elseif ($ThrowException) {
                    throw 'throw-exception'
                    Write-Verbose -Verbose "verbose-message"
                }
                elseif ($WriteErrorAPI) {
                    $ex = [System.ArgumentException]::new('arg-exception')
                    $er = [System.Management.Automation.ErrorRecord]::new($ex, 'WriteErrorAPI:error', 'InvalidArgument', $null)
                    $PSCmdlet.WriteError($er)
                    Write-Verbose -Verbose "verbose-message"
                }
                elseif ($WriteErrorCmdlet) {
                    Write-Error 'write-error-cmdlet'
                    Write-Verbose -Verbose "verbose-message"
                }
                elseif ($MethodInvocationThrowException) {
                    ## This method call throws exception.
                    $iss = [initialsessionstate]::Create()
                    $iss.ImportPSModule($null)
                    Write-Verbose -Verbose "verbose-message"
                }
                elseif ($ExpressionThrowException) {
                    1/0 ## throw exception.
                    Write-Verbose -Verbose "verbose-message"
                }
            }
        }

        function ErrorInEnd {
            [CmdletBinding()]
            param(
                [switch] $ThrowTerminatingError,
                [switch] $ErrorActionStop,
                [switch] $ThrowException,
                [switch] $WriteErrorAPI,
                [switch] $WriteErrorCmdlet,
                [switch] $MethodInvocationThrowException,
                [switch] $ExpressionThrowException
            )

            if ($ThrowTerminatingError) {
                $ex = [System.ArgumentException]::new('terminating-exception')
                $er = [System.Management.Automation.ErrorRecord]::new($ex, 'ThrowTerminatingError:error', 'InvalidArgument', $null)
                $PSCmdlet.ThrowTerminatingError($er)
                Write-Verbose -Verbose "verbose-message"
            }
            elseif ($ErrorActionStop) {
                Get-Command NonExist -ErrorAction Stop
                Write-Verbose -Verbose "verbose-message"
            }
            elseif ($ThrowException) {
                throw 'throw-exception'
                Write-Verbose -Verbose "verbose-message"
            }
            elseif ($WriteErrorAPI) {
                $ex = [System.ArgumentException]::new('arg-exception')
                $er = [System.Management.Automation.ErrorRecord]::new($ex, 'WriteErrorAPI:error', 'InvalidArgument', $null)
                $PSCmdlet.WriteError($er)
                Write-Verbose -Verbose "verbose-message"
            }
            elseif ($WriteErrorCmdlet) {
                Write-Error 'write-error-cmdlet'
                Write-Verbose -Verbose "verbose-message"
            }
            elseif ($MethodInvocationThrowException) {
                ## This method call throws exception.
                $iss = [initialsessionstate]::Create()
                $iss.ImportPSModule($null)
                Write-Verbose -Verbose "verbose-message"
            }
            elseif ($ExpressionThrowException) {
                1/0 ## throw exception.
                Write-Verbose -Verbose "verbose-message"
            }
        }

        function DivideByZeroWrappedInTry {
            [CmdletBinding()]
            param()

            end {}
            clean {
                try {
                    1/0
                    Write-Verbose -Verbose 'clean'
                }
                catch { Write-Verbose -Verbose $_.Exception.InnerException.GetType().FullName }
            }
        }

        function ArgumentNullWrappedInTry {
            [CmdletBinding()]
            param()

            end {}
            clean {
                try {
                    $iss = [initialsessionstate]::Create()
                    $iss.ImportPSModule($null)
                    Write-Verbose -Verbose 'clean'
                }
                catch { Write-Verbose -Verbose $_.Exception.InnerException.GetType().FullName }
            }
        }

        function DivideByZeroWithTrap {
            [CmdletBinding()]
            param()

            end {}
            clean {
                trap {
                    Write-Verbose -Verbose $_.Exception.GetType().FullName
                    continue
                }

                1/0
                Write-Verbose -Verbose 'clean'
            }
        }

        function ArgumentNullWithTrap {
            [CmdletBinding()]
            param()

            end {}
            clean {
                trap {
                    Write-Verbose -Verbose $_.Exception.GetType().FullName
                    continue
                }

                $iss = [initialsessionstate]::Create()
                $iss.ImportPSModule($null)
                Write-Verbose -Verbose 'clean'
            }
        }

        #region Helper

        $pwsh = [PowerShell]::Create()
        $text = (Get-Command ErrorInClean).ScriptBlock.Ast.Extent.Text
        $pwsh.AddScript($text).Invoke()

        $pwsh.Commands.Clear()
        $text = (Get-Command ErrorInEnd).ScriptBlock.Ast.Extent.Text
        $pwsh.AddScript($text).Invoke()

        function RunCommand {
            param(
                [ValidateSet('ErrorInClean', 'ErrorInEnd')]
                [string] $Command,

                [ValidateSet('ThrowTerminatingError', 'ErrorActionStop', 'ThrowException', 'WriteErrorAPI', 
                             'WriteErrorCmdlet', 'MethodInvocationThrowException', 'ExpressionThrowException')]
                [string] $ParamNameToUse,

                [ValidateSet('Continue', 'Ignore', 'SilentlyContinue', 'Stop')]
                [string] $ErrorAction
            )

            $pwsh.Commands.Clear()
            $pwsh.Streams.ClearStreams()
            $pwsh.AddCommand($Command).AddParameter($ParamNameToUse, $true) > $null
            if ($ErrorAction) { $pwsh.AddParameter('ErrorAction', $ErrorAction) > $null }
            $pwsh.Invoke()
        }

        function RunScript {
            param([string] $Script)

            $pwsh.Commands.Clear()
            $pwsh.Streams.ClearStreams()
            $pwsh.AddScript($Script).Invoke()
        }

        function GetLastError {
            $pwsh.Commands.Clear()
            $pwsh.AddCommand('Get-Error').Invoke()
        }

        function ClearDollarError {
            $pwsh.Commands.Clear()
            $pwsh.AddScript('$Error.Clear()').Invoke()
        }

        #endregion
    }

    AfterAll {
        $pwsh.Dispose()
    }

    It "Terminating error should stop the 'Clean' block execution but should not be propagated up" {
        ## 'ThrowTerminatingException' stops the execution within the 'Clean' block, but the error doesn't get
        ## propagated out of the 'Clean' block. Instead, the error is written to the 'ErrorOutput' pipe.
        RunCommand -Command 'ErrorInClean' -ParamNameToUse 'ThrowTerminatingError'
        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'terminating-exception'

        ## 'throw' statement stops the execution within the 'Clean' block by default, but the error doesn't get
        ## propagated out of the 'Clean' block. Instead, the error is written to the 'ErrorOutput' pipe.
        RunCommand -Command 'ErrorInClean' -ParamNameToUse 'ThrowException'
        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'throw-exception'

        ## '-ErrorAction Stop' stops the execution within the 'Clean' block, but the error doesn't get propagated
        ## out of the 'Clean' block. Instead, the error is written to the 'ErrorOutput' pipe.
        RunCommand -Command 'ErrorInClean' -ParamNameToUse 'ErrorActionStop'
        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].FullyQualifiedErrorId | Should -BeExactly 'CommandNotFoundException,Microsoft.PowerShell.Commands.GetCommandCommand'

        ## Turn non-terminating errors into terminating by '-ErrorAction Stop' explicitly.
        ## Execution within the 'Clean' block should be stopped. The resulted terminating error should not get
        ## propagated, but instead should be written to 'ErrorOutput' pipe.
        RunCommand -Command 'ErrorInClean' -ParamNameToUse 'WriteErrorAPI' -ErrorAction Stop
        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'arg-exception'

        RunCommand -Command 'ErrorInClean' -ParamNameToUse 'WriteErrorCmdlet' -ErrorAction Stop
        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'write-error-cmdlet'

        RunCommand -Command 'ErrorInClean' -ParamNameToUse 'MethodInvocationThrowException' -ErrorAction Stop
        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.ArgumentNullException'

        RunCommand -Command 'ErrorInClean' -ParamNameToUse 'ExpressionThrowException' -ErrorAction Stop
        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.DivideByZeroException'
    }

    It "Terminating error should set `$? correctly" {
        RunScript -Script 'ErrorInClean -ThrowTerminatingError; $?' | Should -BeFalse
        RunScript -Script 'ErrorInClean -ThrowException; $?' | Should -BeFalse
        RunScript -Script 'ErrorInClean -ErrorActionStop; $?' | Should -BeFalse
        RunScript -Script 'ErrorInClean -WriteErrorAPI -ErrorAction Stop; $?' | Should -BeFalse
        RunScript -Script 'ErrorInClean -WriteErrorCmdlet -ErrorAction Stop; $?' | Should -BeFalse
        RunScript -Script 'ErrorInClean -MethodInvocationThrowException -ErrorAction Stop; $?' | Should -BeFalse
        RunScript -Script 'ErrorInClean -ExpressionThrowException -ErrorAction Stop; $?' | Should -BeFalse
    }

    It "Track the `$? behavior for non-terminating errors within 'Clean' and 'End' blocks" {
        RunScript -Script 'ErrorInClean -WriteErrorAPI; $?' | Should -BeFalse
        RunScript -Script 'ErrorInEnd -WriteErrorAPI; $?' | Should -BeFalse

        ## The 'Write-Error' is specially weird, in that when a command emits error because of 'Write-Error' within it,
        ## the following '$?' won't reflect '$false', but will be '$true'.
        ## Frankly, this is counter-intuitive, but it's the existing behavior. The tests below just keeps track of this
        ## behavior. Feel free to change this test if someone is fixing this seemingly wrong behavior.
        RunScript -Script 'ErrorInClean -WriteErrorCmdlet; $?' | Should -BeTrue
        RunScript -Script 'ErrorInEnd -WriteErrorCmdlet; $?' | Should -BeTrue

        ## Similarly, when a command emits error because of a method invocation within it throws an exception,
        ## the following '$?' won't reflect '$false', but will be '$true'.
        ## Again, this seems wrong, but it's the existing behavior. The tests below just keeps track of this
        ## behavior. Feel free to change this test if someone is fixing this seemingly wrong behavior.
        RunScript -Script 'ErrorInClean -MethodInvocationThrowException; $?' | Should -BeTrue
        RunScript -Script 'ErrorInEnd -MethodInvocationThrowException; $?' | Should -BeTrue

        ## Again, when a command emits error because of an expression within it throws an exception,
        ## the following '$?' won't reflect '$false', but will be '$true'.
        ## This seems wrong, but it's the existing behavior. The tests below just keeps track of this
        ## behavior. Feel free to change this test if someone is fixing this seemingly wrong behavior.
        RunScript -Script 'ErrorInClean -ExpressionThrowException; $?' | Should -BeTrue
        RunScript -Script 'ErrorInEnd -ExpressionThrowException; $?' | Should -BeTrue
    }

    It "Non-terminating error within 'Clean' block should act based on ErrorActionPreference: <ParamName> - Continue" -TestCases @(
        @{ ParamName = 'WriteErrorAPI';    AssertScript = { param($err) $err.Exception.Message | Should -BeExactly 'arg-exception' } }
        @{ ParamName = 'WriteErrorCmdlet'; AssertScript = { param($err) $err.Exception.Message | Should -BeExactly 'write-error-cmdlet' } }
        @{ ParamName = 'MethodInvocationThrowException'; AssertScript = { param($err) $err.Exception.InnerException | Should -BeOfType 'System.ArgumentNullException' } }
        @{ ParamName = 'ExpressionThrowException';       AssertScript = { param($err) $err.Exception.InnerException | Should -BeOfType 'System.DivideByZeroException' } }
    ) {
        param($ParamName, $AssertScript)

        RunCommand -Command 'ErrorInClean' -ParamNameToUse $ParamName
        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'verbose-message'
        $pwsh.Streams.Error.Count | Should -Be 1
        & $AssertScript $pwsh.Streams.Error[0]
    }

    It "Non-terminating error within 'End' block should act based on ErrorActionPreference: <ParamName> - Continue" -TestCases @(
        @{ ParamName = 'WriteErrorAPI';    AssertScript = { param($err) $err.Exception.Message | Should -BeExactly 'arg-exception' } }
        @{ ParamName = 'WriteErrorCmdlet'; AssertScript = { param($err) $err.Exception.Message | Should -BeExactly 'write-error-cmdlet' } }
        @{ ParamName = 'MethodInvocationThrowException'; AssertScript = { param($err) $err.Exception.InnerException | Should -BeOfType 'System.ArgumentNullException' } }
        @{ ParamName = 'ExpressionThrowException';       AssertScript = { param($err) $err.Exception.InnerException | Should -BeOfType 'System.DivideByZeroException' } }
    ) {
        param($ParamName, $AssertScript)

        RunCommand -Command 'ErrorInEnd' -ParamNameToUse $ParamName
        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'verbose-message'
        $pwsh.Streams.Error.Count | Should -Be 1
        & $AssertScript $pwsh.Streams.Error[0]
    }

    It "Non-terminating error within 'Clean' block should act based on ErrorActionPreference: <ParamName> - <Action>" -TestCases @(
        ### When error action is 'Ignore', non-terminating errors emitted by 'WriteErrorAPI' and 'WriteErrorCmdlet' are not captured in $Error,
        ### but non-terminating errors emitted by method exception or expression exception are captured in $Error.
        ### This inconsistency is surprising, but it's the existing behavior -- same in other named blocks.
        @{ ParamName = 'WriteErrorAPI'; Action = 'Ignore'; AssertScript = $null }
        @{ ParamName = 'WriteErrorCmdlet'; Action = 'Ignore'; AssertScript = $null }
        @{ ParamName = 'MethodInvocationThrowException'; Action = 'Ignore'; AssertScript = { param($err) $err.Exception.InnerException | Should -BeOfType 'System.ArgumentNullException' } }
        @{ ParamName = 'ExpressionThrowException'; Action = 'Ignore'; AssertScript = { param($err) $err.Exception.InnerException | Should -BeOfType 'System.DivideByZeroException' } }

        @{ ParamName = 'WriteErrorAPI'; Action = 'SilentlyContinue'; AssertScript = { param($err) $err.Exception.Message | Should -BeExactly 'arg-exception' } }
        @{ ParamName = 'WriteErrorCmdlet'; Action = 'SilentlyContinue'; AssertScript = { param($err) $err.Exception.Message | Should -BeExactly 'write-error-cmdlet' } }
        @{ ParamName = 'MethodInvocationThrowException'; Action = 'SilentlyContinue'; AssertScript = { param($err) $err.Exception.InnerException | Should -BeOfType 'System.ArgumentNullException' } }
        @{ ParamName = 'ExpressionThrowException'; Action = 'SilentlyContinue'; AssertScript = { param($err) $err.Exception.InnerException | Should -BeOfType 'System.DivideByZeroException' } }
    ) {
        param($ParamName, $Action, $AssertScript)

        ClearDollarError
        RunCommand -Command 'ErrorInClean' -ParamNameToUse $ParamName -ErrorAction $Action

        $pwsh.Streams.Error.Count | Should -Be 0
        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'verbose-message'

        $lastErr = GetLastError
        if ($null -eq $AssertScript) {
            $lastErr | Should -BeNullOrEmpty
        } else {
            $lastErr | Should -Not -BeNullOrEmpty
            & $AssertScript $lastErr
        }
    }

    ### These tests are targeting 'End' block but with the same settings as the ones right above.
    ### They are used as a comparison to prove the consistent behavior in 'End' and 'Clean'.
    It "Non-terminating error within 'End' block should act based on ErrorActionPreference: <ParamName> - <Action>" -TestCases @(
        @{ ParamName = 'WriteErrorAPI'; Action = 'Ignore'; AssertScript = $null }
        @{ ParamName = 'WriteErrorCmdlet'; Action = 'Ignore'; AssertScript = $null }
        @{ ParamName = 'MethodInvocationThrowException'; Action = 'Ignore'; AssertScript = { param($err) $err.Exception.InnerException | Should -BeOfType 'System.ArgumentNullException' } }
        @{ ParamName = 'ExpressionThrowException'; Action = 'Ignore'; AssertScript = { param($err) $err.Exception.InnerException | Should -BeOfType 'System.DivideByZeroException' } }

        @{ ParamName = 'WriteErrorAPI'; Action = 'SilentlyContinue'; AssertScript = { param($err) $err.Exception.Message | Should -BeExactly 'arg-exception' } }
        @{ ParamName = 'WriteErrorCmdlet'; Action = 'SilentlyContinue'; AssertScript = { param($err) $err.Exception.Message | Should -BeExactly 'write-error-cmdlet' } }
        @{ ParamName = 'MethodInvocationThrowException'; Action = 'SilentlyContinue'; AssertScript = { param($err) $err.Exception.InnerException | Should -BeOfType 'System.ArgumentNullException' } }
        @{ ParamName = 'ExpressionThrowException'; Action = 'SilentlyContinue'; AssertScript = { param($err) $err.Exception.InnerException | Should -BeOfType 'System.DivideByZeroException' } }
    ) {
        param($ParamName, $Action, $AssertScript)

        ClearDollarError
        RunCommand -Command 'ErrorInEnd' -ParamNameToUse $ParamName -ErrorAction $Action

        $pwsh.Streams.Error.Count | Should -Be 0
        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'verbose-message'

        $lastErr = GetLastError
        if ($null -eq $AssertScript) {
            $lastErr | Should -BeNullOrEmpty
        } else {
            $lastErr | Should -Not -BeNullOrEmpty
            & $AssertScript $lastErr
        }
    }

    It "'try/catch' and 'trap' should turn general exception thrown from method/expression into terminating error within 'Clean' block. ErrorAction: <ErrorAction>" -TestCases @(
        @{ ErrorAction = 'Continue' }
        @{ ErrorAction = 'Ignore' }
        @{ ErrorAction = 'SilentlyContinue' }
    ) {
        param($ErrorAction)

        $verbose = DivideByZeroWrappedInTry -ErrorAction $ErrorAction 4>&1
        $verbose.Count | Should -Be 1
        $verbose.Message | Should -BeExactly 'System.DivideByZeroException'

        $verbose = ArgumentNullWrappedInTry -ErrorAction $ErrorAction 4>&1
        $verbose.Count | Should -Be 1
        $verbose.Message | Should -BeExactly 'System.ArgumentNullException'

        $verbose = DivideByZeroWithTrap -ErrorAction $ErrorAction 4>&1
        $verbose.Count | Should -Be 2
        $verbose[0].Message | Should -BeExactly 'System.DivideByZeroException'
        $verbose[1].Message | Should -BeExactly 'clean'

        $verbose = ArgumentNullWithTrap -ErrorAction $ErrorAction 4>&1
        $verbose.Count | Should -Be 2
        $verbose[0].Message | Should -BeExactly 'System.ArgumentNullException'
        $verbose[1].Message | Should -BeExactly 'clean'
    }

    It "'try/catch' and 'trap' outside the command should NOT affect general exception thrown from method/expression in the 'Clean' block" {
        ## The catch block should not run
        RunScript -Script "try { ErrorInClean -MethodInvocationThrowException } catch { Write-Debug -Debug 'caught-something' }"

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'verbose-message'
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.ArgumentNullException'
        $pwsh.Streams.Debug.Count | Should -Be 0

        ## The catch block should not run
        RunScript -Script "try { ErrorInClean -ExpressionThrowException } catch { Write-Debug -Debug 'caught-something' }"

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'verbose-message'
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.DivideByZeroException'
        $pwsh.Streams.Debug.Count | Should -Be 0

        ## The trap block should not run
        RunScript -Script "trap { Write-Debug -Debug 'caught-something'; continue } ErrorInClean -MethodInvocationThrowException"

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'verbose-message'
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.ArgumentNullException'
        $pwsh.Streams.Debug.Count | Should -Be 0

        ## The trap block should not run
        RunScript -Script "trap { Write-Debug -Debug 'caught-something'; continue } ErrorInClean -ExpressionThrowException"

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'verbose-message'
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.DivideByZeroException'
        $pwsh.Streams.Debug.Count | Should -Be 0
    }

    It "'try/catch' and 'trap' outside the command should NOT affect 'throw' statement in the 'Clean' block. ErrorAction: <ErrorAction>" -TestCases @(
        @{ ErrorAction = 'Ignore' }
        @{ ErrorAction = 'SilentlyContinue' }
    ) {
        param ($ErrorAction)

        ## 'throw' statement should be suppressed by 'Ignore' or 'SilentlyContinue' within a 'Clean' block,
        ## even if the command is wrapped in try/catch.
        RunScript -Script "try { ErrorInClean -ThrowException -ErrorAction $ErrorAction } catch { Write-Debug -Debug 'caught-something' }"

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'verbose-message'

        ## Nothing written to error stream
        $pwsh.Streams.Error.Count | Should -Be 0
        ## Nothing written to debug stream
        $pwsh.Streams.Debug.Count | Should -Be 0

        ## The suppressed 'throw' exception is kept in '$Error'
        $err = GetLastError
        $err | Should -Not -BeNullOrEmpty
        $err.FullyQualifiedErrorId | Should -BeExactly 'throw-exception'


        ## 'throw' statement should be suppressed by 'Ignore' or 'SilentlyContinue' within a 'Clean' block,
        ## even if the command is accompanied by 'trap'.
        RunScript -Script "trap { Write-Debug -Debug 'caught-something'; continue } ErrorInClean -ThrowException -ErrorAction $ErrorAction"

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'verbose-message'

        ## Nothing written to error stream
        $pwsh.Streams.Error.Count | Should -Be 0
        ## Nothing written to debug stream
        $pwsh.Streams.Debug.Count | Should -Be 0

        ## The suppressed 'throw' exception is kept in '$Error'
        $err = GetLastError
        $err | Should -Not -BeNullOrEmpty
        $err.FullyQualifiedErrorId | Should -BeExactly 'throw-exception'
    }

    It "Error out-variable should work for the 'Clean' block" {
        ## Terminating errors thrown from 'Clean' block are captured and written to the error pipe.
        ## Here we redirect the error pipe to discard the error stream, so the error doesn't pollute
        ## the test output.
        ErrorInClean -ThrowTerminatingError -ErrorVariable err 2>&1 > $null
        $err.Count | Should -Be 1
        $err[0].Message | Should -BeExactly 'terminating-exception'

        ## $err.Count is 3 in this case. It's the same for other named blocks too.
        ## This looks like an existing bug because $err.Count should be 1 since only 1 error happened.
        ## Opened issue https://github.com/PowerShell/PowerShell/issues/15739
        ErrorInClean -ErrorActionStop -ErrorVariable err 2>&1 > $null
        $err[0] | Should -BeOfType 'System.Management.Automation.ActionPreferenceStopException'

        ## $err.Count is 2 in this case. It's the same for other named blocks too.
        ## Similarly, this looks like an existing bug and $err.Count should be 1.
        ## This is tracked by the same issue above.
        ErrorInClean -ThrowException -ErrorVariable err 2>&1 > $null
        $err[0].Exception.Message | Should -BeExactly 'throw-exception'

        ErrorInClean -WriteErrorAPI -ErrorVariable err *>&1 > $null
        $err.Count | Should -Be 1
        $err[0].Exception.Message | Should -BeExactly 'arg-exception'

        ErrorInClean -WriteErrorCmdlet -ErrorVariable err *>&1 > $null
        $err.Count | Should -Be 1
        $err[0].Exception.Message | Should -BeExactly 'write-error-cmdlet'

        ErrorInClean -MethodInvocationThrowException -ErrorVariable err *>&1 > $null
        $err.Count | Should -Be 1
        $err[0].Exception.InnerException | Should -BeOfType 'System.ArgumentNullException'

        ErrorInClean -ExpressionThrowException -ErrorVariable err *>&1 > $null
        $err.Count | Should -Be 1
        $err[0].Exception.InnerException | Should -BeOfType 'System.DivideByZeroException'
    }
}

Describe "Multiple errors from 'Clean' and another named block" -Tag 'CI' {

    BeforeAll {
        function MultipleErrors {
            [CmdletBinding()]
            param(
                [ValidateSet('ThrowTerminatingError', 'ErrorActionStop', 'ThrowException', 'WriteErrorAPI',
                             'WriteErrorCmdlet', 'MethodInvocationThrowException', 'ExpressionThrowException')]
                [Parameter(Mandatory)]
                [string] $ErrorFromEndBlock,

                [ValidateSet('ThrowTerminatingError', 'ErrorActionStop', 'ThrowException', 'WriteErrorAPI',
                             'WriteErrorCmdlet', 'MethodInvocationThrowException', 'ExpressionThrowException')]
                [Parameter(Mandatory)]
                [string] $ErrorFromCleanBlock
            )

            End {
                switch ($ErrorFromEndBlock) {
                    'ThrowTerminatingError' {
                        $ex = [System.ArgumentException]::new('end-terminating-exception')
                        $er = [System.Management.Automation.ErrorRecord]::new($ex, 'ThrowTerminatingError:end-error', 'InvalidArgument', $null)
                        $PSCmdlet.ThrowTerminatingError($er)
                        Write-Verbose -Verbose "end-verbose-message"
                    }

                    'ErrorActionStop' {
                        Get-Command NonExistEnd -ErrorAction Stop
                        Write-Verbose -Verbose "end-verbose-message"
                    }

                    'ThrowException' {
                        throw 'end-throw-exception'
                        Write-Verbose -Verbose "end-verbose-message"
                    }

                    'WriteErrorAPI' {
                        $ex = [System.ArgumentException]::new('end-arg-exception')
                        $er = [System.Management.Automation.ErrorRecord]::new($ex, 'WriteErrorAPI:end-error', 'InvalidArgument', $null)
                        $PSCmdlet.WriteError($er)
                        Write-Verbose -Verbose "end-verbose-message"
                    }

                    'WriteErrorCmdlet' {
                        Write-Error 'end-write-error-cmdlet'
                        Write-Verbose -Verbose "end-verbose-message"
                    }

                    'MethodInvocationThrowException' {
                        ## This method call throws exception.
                        $iss = [initialsessionstate]::Create()
                        $iss.ImportPSModule($null)
                        Write-Verbose -Verbose "end-verbose-message"
                    }

                    'ExpressionThrowException' {
                        1/0 ## throw exception.
                        Write-Verbose -Verbose "end-verbose-message"
                    }
                }
            }

            clean {
                switch ($ErrorFromCleanBlock) {
                    'ThrowTerminatingError' {
                        $ex = [System.ArgumentException]::new('clean-terminating-exception')
                        $er = [System.Management.Automation.ErrorRecord]::new($ex, 'ThrowTerminatingError:clean-error', 'InvalidArgument', $null)
                        $PSCmdlet.ThrowTerminatingError($er)
                        Write-Verbose -Verbose "clean-verbose-message"
                    }

                    'ErrorActionStop' {
                        Get-Command NonExistClean -ErrorAction Stop
                        Write-Verbose -Verbose "clean-verbose-message"
                    }

                    'ThrowException' {
                        throw 'clean-throw-exception'
                        Write-Verbose -Verbose "clean-verbose-message"
                    }

                    'WriteErrorAPI' {
                        $ex = [System.ArgumentException]::new('clean-arg-exception')
                        $er = [System.Management.Automation.ErrorRecord]::new($ex, 'WriteErrorAPI:clean-error', 'InvalidArgument', $null)
                        $PSCmdlet.WriteError($er)
                        Write-Verbose -Verbose "clean-verbose-message"
                    }

                    'WriteErrorCmdlet' {
                        Write-Error 'clean-write-error-cmdlet'
                        Write-Verbose -Verbose "clean-verbose-message"
                    }

                    'MethodInvocationThrowException' {
                        ## This method call throws exception.
                        $iss = [initialsessionstate]::Create()
                        $iss.ImportPSModule($null)
                        Write-Verbose -Verbose "clean-verbose-message"
                    }

                    'ExpressionThrowException' {
                        1/0 ## throw exception.
                        Write-Verbose -Verbose "clean-verbose-message"
                    }
                }
            }
        }

        #region Helper

        $pwsh = [PowerShell]::Create()
        $text = (Get-Command MultipleErrors).ScriptBlock.Ast.Extent.Text
        $pwsh.AddScript($text).Invoke()

        function RunCommand {
            param(
                [ValidateSet('MultipleErrors')]
                [string] $Command,

                [ValidateSet('ThrowTerminatingError', 'ErrorActionStop', 'ThrowException', 'WriteErrorAPI', 
                             'WriteErrorCmdlet', 'MethodInvocationThrowException', 'ExpressionThrowException')]
                [string] $ErrorFromEndBlock,

                [ValidateSet('ThrowTerminatingError', 'ErrorActionStop', 'ThrowException', 'WriteErrorAPI', 
                             'WriteErrorCmdlet', 'MethodInvocationThrowException', 'ExpressionThrowException')]
                [string] $ErrorFromCleanBlock,

                [ValidateSet('Continue', 'Ignore', 'SilentlyContinue', 'Stop')]
                [string] $ErrorAction
            )

            $pwsh.Commands.Clear()
            $pwsh.Streams.ClearStreams()
            $pwsh.AddCommand($Command) > $null
            $pwsh.AddParameter('ErrorFromEndBlock', $ErrorFromEndBlock) > $null
            $pwsh.AddParameter('ErrorFromCleanBlock', $ErrorFromCleanBlock) > $null
            if ($ErrorAction) { $pwsh.AddParameter('ErrorAction', $ErrorAction) > $null }
            $pwsh.Invoke()
        }

        function RunScript {
            param([string] $Script)

            $pwsh.Commands.Clear()
            $pwsh.Streams.ClearStreams()
            $pwsh.AddScript($Script).Invoke()
        }

        function GetLastError {
            $pwsh.Commands.Clear()
            $pwsh.AddCommand('Get-Error').Invoke()
        }

        function GetAllErrors {
            $pwsh.Commands.Clear()
            $pwsh.AddScript('$Error').Invoke()
        }

        function ClearDollarError {
            $pwsh.Commands.Clear()
            $pwsh.AddScript('$Error.Clear()').Invoke()
        }

        #endregion
    }

    AfterAll {
        $pwsh.Dispose()
    }

    It "Terminating errors from both 'End' (ThrowTerminatingError) and 'Clean' (ThrowTerminatingError) should work properly" {
        $failure = $null
        try {
            RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ThrowTerminatingError' -ErrorFromCleanBlock 'ThrowTerminatingError'
        } catch {
            $failure = $_
        }

        $failure | Should -Not -BeNullOrEmpty
        $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
        $failure.Exception.InnerException | Should -BeOfType 'System.Management.Automation.CmdletInvocationException'
        $failure.Exception.InnerException.InnerException | Should -BeOfType 'System.ArgumentException'
        $failure.Exception.InnerException.InnerException.Message | Should -BeExactly 'end-terminating-exception'

        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception | Should -BeOfType 'System.ArgumentException'
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'clean-terminating-exception'
    }

    It "Terminating errors from both 'End' (ErrorActionStop) and 'Clean' (ThrowTerminatingError) should work properly" {
        $failure = $null
        try {
            RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ErrorActionStop' -ErrorFromCleanBlock 'ThrowTerminatingError'
        } catch {
            $failure = $_
        }

        $failure | Should -Not -BeNullOrEmpty
        $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
        $failure.Exception.InnerException | Should -BeOfType 'System.Management.Automation.ActionPreferenceStopException'
        $failure.Exception.InnerException.Message | should -BeLike "*'NonExistEnd'*"

        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception | Should -BeOfType 'System.ArgumentException'
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'clean-terminating-exception'
    }

    It "Terminating errors from both 'End' (ThrowException) and 'Clean' (ThrowTerminatingError) should work properly" {
        $failure = $null
        try {
            RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ThrowException' -ErrorFromCleanBlock 'ThrowTerminatingError'
        } catch {
            $failure = $_
        }

        $failure | Should -Not -BeNullOrEmpty
        $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
        $failure.Exception.InnerException.Message | should -BeExactly 'end-throw-exception'

        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception | Should -BeOfType 'System.ArgumentException'
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'clean-terminating-exception'
    }

    It "Terminating errors from both 'End' (ThrowTerminatingError) and 'Clean' (ErrorActionStop) should work properly" {
        $failure = $null
        try {
            RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ThrowTerminatingError' -ErrorFromCleanBlock 'ErrorActionStop'
        } catch {
            $failure = $_
        }

        $failure | Should -Not -BeNullOrEmpty
        $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
        $failure.Exception.InnerException | Should -BeOfType 'System.Management.Automation.CmdletInvocationException'
        $failure.Exception.InnerException.InnerException | Should -BeOfType 'System.ArgumentException'
        $failure.Exception.InnerException.InnerException.Message | Should -BeExactly 'end-terminating-exception'

        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].FullyQualifiedErrorId | Should -BeExactly 'CommandNotFoundException,Microsoft.PowerShell.Commands.GetCommandCommand'
        $pwsh.Streams.Error[0].Exception.Message | Should -BeLike "*'NonExistClean'*"
    }

    It "Terminating errors from both 'End' (ErrorActionStop) and 'Clean' (ErrorActionStop) should work properly" {
        $failure = $null
        try {
            RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ErrorActionStop' -ErrorFromCleanBlock 'ErrorActionStop'
        } catch {
            $failure = $_
        }

        $failure | Should -Not -BeNullOrEmpty
        $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
        $failure.Exception.InnerException | Should -BeOfType 'System.Management.Automation.ActionPreferenceStopException'
        $failure.Exception.InnerException.Message | should -BeLike "*'NonExistEnd'*"

        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].FullyQualifiedErrorId | Should -BeExactly 'CommandNotFoundException,Microsoft.PowerShell.Commands.GetCommandCommand'
        $pwsh.Streams.Error[0].Exception.Message | Should -BeLike "*'NonExistClean'*"
    }

    It "Terminating errors from both 'End' (ThrowException) and 'Clean' (ErrorActionStop) should work properly" {
        $failure = $null
        try {
            RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ThrowException' -ErrorFromCleanBlock 'ErrorActionStop'
        } catch {
            $failure = $_
        }

        $failure | Should -Not -BeNullOrEmpty
        $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
        $failure.Exception.InnerException.Message | should -BeExactly 'end-throw-exception'

        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].FullyQualifiedErrorId | Should -BeExactly 'CommandNotFoundException,Microsoft.PowerShell.Commands.GetCommandCommand'
        $pwsh.Streams.Error[0].Exception.Message | Should -BeLike "*'NonExistClean'*"
    }

    It "Terminating errors from both 'End' (ThrowTerminatingError) and 'Clean' (ThrowException) should work properly" {
        $failure = $null
        try {
            RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ThrowTerminatingError' -ErrorFromCleanBlock 'ThrowException'
        } catch {
            $failure = $_
        }

        $failure | Should -Not -BeNullOrEmpty
        $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
        $failure.Exception.InnerException | Should -BeOfType 'System.Management.Automation.CmdletInvocationException'
        $failure.Exception.InnerException.InnerException | Should -BeOfType 'System.ArgumentException'
        $failure.Exception.InnerException.InnerException.Message | Should -BeExactly 'end-terminating-exception'

        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'clean-throw-exception'
    }

    It "Terminating errors from both 'End' (ErrorActionStop) and 'Clean' (ThrowException) should work properly" {
        $failure = $null
        try {
            RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ErrorActionStop' -ErrorFromCleanBlock 'ThrowException'
        } catch {
            $failure = $_
        }

        $failure | Should -Not -BeNullOrEmpty
        $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
        $failure.Exception.InnerException | Should -BeOfType 'System.Management.Automation.ActionPreferenceStopException'
        $failure.Exception.InnerException.Message | should -BeLike "*'NonExistEnd'*"

        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'clean-throw-exception'
    }

    It "Terminating errors from both 'End' (ThrowException) and 'Clean' (ThrowException) should work properly" {
        $failure = $null
        try {
            RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ThrowException' -ErrorFromCleanBlock 'ThrowException'
        } catch {
            $failure = $_
        }

        $failure | Should -Not -BeNullOrEmpty
        $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
        $failure.Exception.InnerException.Message | should -BeExactly 'end-throw-exception'

        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'clean-throw-exception'
    }

    It "Terminating errors from both 'End' (ThrowException) and 'Clean' (ThrowException) with ErrorAction '<ErrorAction>' should work properly" -TestCases @(
        @{ ErrorAction = 'Ignore' }
        @{ ErrorAction = 'SilentlyContinue' }
    ) {
        param($ErrorAction)

        ClearDollarError

        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ThrowException' -ErrorFromCleanBlock 'ThrowException' -ErrorAction $ErrorAction

        $pwsh.Streams.Error.Count | Should -Be 0
        $pwsh.Streams.Verbose.Count | Should -Be 2
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'end-verbose-message'
        $pwsh.Streams.Verbose[1] | Should -BeExactly 'clean-verbose-message'

        $ers = GetAllErrors
        $ers.Count | Should -Be 2
        $ers[0].Exception.Message | Should -BeExactly 'clean-throw-exception'
        $ers[1].Exception.Message | Should -BeExactly 'end-throw-exception'
    }

    It "Non-terminating error from 'End' (WriteErrorAPI) and terminating error from 'Clean' (ThrowTerminatingError) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'WriteErrorAPI' -ErrorFromCleanBlock 'ThrowTerminatingError'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'end-arg-exception'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeExactly 'clean-terminating-exception'

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'end-verbose-message'
    }

    It "Non-terminating error from 'End' (WriteErrorAPI) and terminating error from 'Clean' (ErrorActionStop) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'WriteErrorAPI' -ErrorFromCleanBlock 'ErrorActionStop'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'end-arg-exception'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeLike "*'NonExistClean'*"

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'end-verbose-message'
    }

    It "Non-terminating error from 'End' (WriteErrorAPI) and terminating error from 'Clean' (ThrowException) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'WriteErrorAPI' -ErrorFromCleanBlock 'ThrowException'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'end-arg-exception'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeExactly 'clean-throw-exception'

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'end-verbose-message'
    }

    It "Non-terminating error from 'End' (WriteErrorCmdlet) and terminating error from 'Clean' (ThrowTerminatingError) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'WriteErrorCmdlet' -ErrorFromCleanBlock 'ThrowTerminatingError'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'end-write-error-cmdlet'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeExactly 'clean-terminating-exception'

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'end-verbose-message'
    }

    It "Non-terminating error from 'End' (WriteErrorCmdlet) and terminating error from 'Clean' (ErrorActionStop) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'WriteErrorCmdlet' -ErrorFromCleanBlock 'ErrorActionStop'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'end-write-error-cmdlet'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeLike "*'NonExistClean'*"

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'end-verbose-message'
    }

    It "Non-terminating error from 'End' (WriteErrorCmdlet) and terminating error from 'Clean' (ThrowException) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'WriteErrorCmdlet' -ErrorFromCleanBlock 'ThrowException'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'end-write-error-cmdlet'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeExactly 'clean-throw-exception'

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'end-verbose-message'
    }

    It "Non-terminating error from 'End' (MethodInvocationThrowException) and terminating error from 'Clean' (ThrowTerminatingError) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'MethodInvocationThrowException' -ErrorFromCleanBlock 'ThrowTerminatingError'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.ArgumentNullException'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeExactly 'clean-terminating-exception'

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'end-verbose-message'
    }

    It "Non-terminating error from 'End' (MethodInvocationThrowException) and terminating error from 'Clean' (ErrorActionStop) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'MethodInvocationThrowException' -ErrorFromCleanBlock 'ErrorActionStop'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.ArgumentNullException'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeLike "*'NonExistClean'*"

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'end-verbose-message'
    }

    It "Non-terminating error from 'End' (MethodInvocationThrowException) and terminating error from 'Clean' (ThrowException) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'MethodInvocationThrowException' -ErrorFromCleanBlock 'ThrowException'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.ArgumentNullException'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeExactly 'clean-throw-exception'

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'end-verbose-message'
    }

    It "Non-terminating error from 'End' (ExpressionThrowException) and terminating error from 'Clean' (ThrowTerminatingError) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ExpressionThrowException' -ErrorFromCleanBlock 'ThrowTerminatingError'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.DivideByZeroException'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeExactly 'clean-terminating-exception'

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'end-verbose-message'
    }

    It "Non-terminating error from 'End' (ExpressionThrowException) and terminating error from 'Clean' (ErrorActionStop) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ExpressionThrowException' -ErrorFromCleanBlock 'ErrorActionStop'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.DivideByZeroException'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeLike "*'NonExistClean'*"

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'end-verbose-message'
    }

    It "Non-terminating error from 'End' (ExpressionThrowException) and terminating error from 'Clean' (ThrowException) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ExpressionThrowException' -ErrorFromCleanBlock 'ThrowException'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.DivideByZeroException'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeExactly 'clean-throw-exception'

        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'end-verbose-message'
    }

    It "Terminating error from 'End' (ThrowException) and non-terminating error from 'Clean' (WriteErrorAPI) should work properly" {
        $failure = $null
        try {
            RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ThrowException' -ErrorFromCleanBlock 'WriteErrorAPI'
        } catch {
            $failure = $_
        }

        $failure | Should -Not -BeNullOrEmpty
        $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
        $failure.Exception.InnerException.Message | should -BeExactly 'end-throw-exception'

        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'clean-arg-exception'
        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'clean-verbose-message'
    }

    It "Terminating error from 'End' (ThrowException) and non-terminating error from 'Clean' (WriteErrorCmdlet) should work properly" {
        $failure = $null
        try {
            RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ThrowException' -ErrorFromCleanBlock 'WriteErrorCmdlet'
        } catch {
            $failure = $_
        }

        $failure | Should -Not -BeNullOrEmpty
        $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
        $failure.Exception.InnerException.Message | should -BeExactly 'end-throw-exception'

        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'clean-write-error-cmdlet'
        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'clean-verbose-message'
    }

    It "Terminating error from 'End' (ThrowException) and non-terminating error from 'Clean' (MethodInvocationThrowException) should work properly" {
        $failure = $null
        try {
            RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ThrowException' -ErrorFromCleanBlock 'MethodInvocationThrowException'
        } catch {
            $failure = $_
        }

        $failure | Should -Not -BeNullOrEmpty
        $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
        $failure.Exception.InnerException.Message | should -BeExactly 'end-throw-exception'

        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.ArgumentNullException'
        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'clean-verbose-message'
    }

    It "Terminating error from 'End' (ThrowException) and non-terminating error from 'Clean' (ExpressionThrowException) should work properly" {
        $failure = $null
        try {
            RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'ThrowException' -ErrorFromCleanBlock 'ExpressionThrowException'
        } catch {
            $failure = $_
        }

        $failure | Should -Not -BeNullOrEmpty
        $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
        $failure.Exception.InnerException.Message | should -BeExactly 'end-throw-exception'

        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.DivideByZeroException'
        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Verbose[0] | Should -BeExactly 'clean-verbose-message'
    }

    It "Non-terminating error from 'End' (WriteErrorAPI) and non-terminating error from 'Clean' (WriteErrorCmdlet) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'WriteErrorAPI' -ErrorFromCleanBlock 'WriteErrorCmdlet'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'end-arg-exception'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeExactly 'clean-write-error-cmdlet'

        $pwsh.Streams.Verbose.Count | Should -Be 2
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'end-verbose-message'
        $pwsh.Streams.Verbose[1].Message | Should -BeExactly 'clean-verbose-message'
    }

    It "Non-terminating error from 'End' (WriteErrorAPI) and non-terminating error from 'Clean' (MethodInvocationThrowException) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'WriteErrorAPI' -ErrorFromCleanBlock 'MethodInvocationThrowException'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'end-arg-exception'
        $pwsh.Streams.Error[1].Exception.InnerException | Should -BeOfType 'System.ArgumentNullException'

        $pwsh.Streams.Verbose.Count | Should -Be 2
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'end-verbose-message'
        $pwsh.Streams.Verbose[1].Message | Should -BeExactly 'clean-verbose-message'
    }

    It "Non-terminating error from 'End' (WriteErrorCmdlet) and non-terminating error from 'Clean' (WriteErrorAPI) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'WriteErrorCmdlet' -ErrorFromCleanBlock 'WriteErrorAPI'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'end-write-error-cmdlet'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeExactly 'clean-arg-exception'

        $pwsh.Streams.Verbose.Count | Should -Be 2
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'end-verbose-message'
        $pwsh.Streams.Verbose[1].Message | Should -BeExactly 'clean-verbose-message'
    }

    It "Non-terminating error from 'End' (WriteErrorCmdlet) and non-terminating error from 'Clean' (ExpressionThrowException) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'WriteErrorCmdlet' -ErrorFromCleanBlock 'ExpressionThrowException'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'end-write-error-cmdlet'
        $pwsh.Streams.Error[1].Exception.InnerException | Should -BeOfType 'System.DivideByZeroException'

        $pwsh.Streams.Verbose.Count | Should -Be 2
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'end-verbose-message'
        $pwsh.Streams.Verbose[1].Message | Should -BeExactly 'clean-verbose-message'
    }

    It "Non-terminating error from 'End' (MethodInvocationThrowException) and non-terminating error from 'Clean' (WriteErrorCmdlet) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'MethodInvocationThrowException' -ErrorFromCleanBlock 'WriteErrorCmdlet'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.ArgumentNullException'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeExactly 'clean-write-error-cmdlet'

        $pwsh.Streams.Verbose.Count | Should -Be 2
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'end-verbose-message'
        $pwsh.Streams.Verbose[1].Message | Should -BeExactly 'clean-verbose-message'
    }

    It "Non-terminating error from 'End' (MethodInvocationThrowException) and non-terminating error from 'Clean' (ExpressionThrowException) should work properly" {
        ## No exception should be thrown
        RunCommand -Command 'MultipleErrors' -ErrorFromEndBlock 'MethodInvocationThrowException' -ErrorFromCleanBlock 'ExpressionThrowException'

        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.ArgumentNullException'
        $pwsh.Streams.Error[1].Exception.InnerException | Should -BeOfType 'System.DivideByZeroException'

        $pwsh.Streams.Verbose.Count | Should -Be 2
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'end-verbose-message'
        $pwsh.Streams.Verbose[1].Message | Should -BeExactly 'clean-verbose-message'
    }
}

Describe "Error handling within a pipeline (multiple commands with 'Clean' block)" -Tag 'CI' {
    BeforeAll {
        function test1 {
            param([switch] $EmitErrorInProcess)
            process {
                if ($EmitErrorInProcess) {
                    throw 'test1-process'
                }
                Write-Output 'process-obj'
            }
            clean {
                throw 'test1-clean'
                Write-Verbose -Verbose 'test1-clean-verbose'
            }
        }

        function test2 {
            param([switch] $EmitErrorInProcess)
            process {
                if ($EmitErrorInProcess) {
                    throw 'test2-process'
                }
                Write-Verbose -Verbose $_
            }
            clean {
                throw 'test2-clean'
                Write-Verbose -Verbose 'test2-clean-verbose'
            }
        }

        function test-1 {
            param([switch] $EmitException)
            process { 'output' }
            clean {
                if ($EmitException) {
                    throw 'test-1-clean-exception'
                }
                Write-Verbose -Verbose 'test-1-clean'
            }
        }

        function test-2 {
            param([switch] $EmitException)
            process { $_ }
            clean {
                if ($EmitException) {
                    throw 'test-2-clean-exception'
                }
                Write-Verbose -Verbose 'test-2-clean'
            }
        }

        function test-3 {
            param([switch] $EmitException)
            process { Write-Warning $_ }
            clean {
                if ($EmitException) {
                    throw 'test-3-clean-exception'
                }
                Write-Verbose -Verbose 'test-3-clean'
            }
        }

        #region Helper

        $pwsh = [PowerShell]::Create()
        $text = (Get-Command test1, test2, test-1, test-2, test-3).ScriptBlock.Ast.Extent.Text
        $pwsh.AddScript($text -join "`n").Invoke()

        function RunScript {
            param([string] $Script)

            $pwsh.Commands.Clear()
            $pwsh.Streams.ClearStreams()
            $pwsh.AddScript($Script).Invoke()
        }

        #endregion
    }

    It "Errors from multiple 'Clean' blocks should work properly" {
        ## No exception should be thrown
        RunScript -Script "test1 | test2"

        ## Exceptions thrown from 'throw' statement are not propagated up, but instead written to the error stream.
        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'test1-clean'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeExactly 'test2-clean'
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'process-obj'
    }

    It "ErrorAction should be honored by 'Clean' blocks" {
        try {
            RunScript -Script '$ErrorActionPreference = "SilentlyContinue"'
            ## No exception should be thrown.
            RunScript -Script "test1 | test2"

            ## The exception from 'throw' statement should be suppressed by 'SilentlyContinue'.
            $pwsh.Streams.Error.Count | Should -Be 0
            $pwsh.Streams.Verbose.Count | Should -Be 3
            $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'process-obj'
            $pwsh.Streams.Verbose[1].Message | Should -BeExactly 'test1-clean-verbose'
            $pwsh.Streams.Verbose[2].Message | Should -BeExactly 'test2-clean-verbose'
        }
        finally {
            ## Revert error action back to 'Continue'
            RunScript -Script '$ErrorActionPreference = "Continue"'
        }
    }

    It "Errors from 'Clean' blocks should work properly when another named block emits error" {
        $failure = $null
        try {
            RunScript -Script "test1 | test2 -EmitErrorInProcess"
        } catch {
            $failure = $_
        }

        $failure | Should -Not -BeNullOrEmpty
        $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
        $failure.Exception.InnerException.Message | Should -BeExactly 'test2-process'

        $pwsh.Streams.Verbose.Count | Should -Be 0
        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'test1-clean'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeExactly 'test2-clean'
    }

    It "The 'Clean' block should not run when the none of other named blocks from the same command get to run" {
        $failure = $null
        try {
            RunScript -Script "test1 -EmitErrorInProcess | test2"
        } catch {
            $failure = $_
        }

        $failure | Should -Not -BeNullOrEmpty
        $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
        $failure.Exception.InnerException.Message | Should -BeExactly 'test1-process'

        ## Only the 'Clean' block from 'test1' will run.
        ## The 'Clean' block from 'test2' won't run because none of the other blocks from 'test2' gets to run
        ## due to the terminating exception thrown from 'test1.Process'.
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'test1-clean'
    }

    It "Exception from the 'Clean' block at <Position> should not affect other 'Clean' blocks" -TestCases @(
        @{ Position = 'beginning-of-pipeline'; Script = 'test-1 -EmitException | test-2 | test-3'; ExceptionMessage = 'test-1-clean-exception'; VerboseMessages = @('test-2-clean', 'test-3-clean') }
        @{ Position = 'middle-of-pipeline';    Script = 'test-1 | test-2 -EmitException | test-3'; ExceptionMessage = 'test-2-clean-exception'; VerboseMessages = @('test-1-clean', 'test-3-clean') }
        @{ Position = 'end-of-pipeline';       Script = 'test-1 | test-2 | test-3 -EmitException'; ExceptionMessage = 'test-3-clean-exception'; VerboseMessages = @('test-1-clean', 'test-2-clean') }
    ) {
        param($Script, $ExceptionMessage, $VerboseMessages)

        RunScript -Script $Script
        $pwsh.Streams.Error.Count | Should -Be 1
        $pwsh.Streams.Verbose.Count | Should -Be 2
        $pwsh.Streams.Warning.Count | Should -Be 1

        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly $ExceptionMessage
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly $VerboseMessages[0]
        $pwsh.Streams.Verbose[1].Message | Should -BeExactly $VerboseMessages[1]
        $pwsh.Streams.Warning[0].Message | Should -BeExactly 'output'
    }

    It "Multiple exceptions from 'Clean' blocks should not affect other 'Clean' blocks" {
        RunScript -Script 'test-1 -EmitException | test-2 | test-3 -EmitException'
        $pwsh.Streams.Error.Count | Should -Be 2
        $pwsh.Streams.Verbose.Count | Should -Be 1
        $pwsh.Streams.Warning.Count | Should -Be 1

        $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'test-1-clean-exception'
        $pwsh.Streams.Error[1].Exception.Message | Should -BeExactly 'test-3-clean-exception'
        $pwsh.Streams.Verbose[0].Message | Should -BeExactly 'test-2-clean'
        $pwsh.Streams.Warning[0].Message | Should -BeExactly 'output'
    }
}
