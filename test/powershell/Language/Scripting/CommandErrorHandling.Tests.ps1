# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Command error handling in general' -Tag 'CI' {

    BeforeAll {
        $pwsh = [PowerShell]::Create()
        $pwsh.AddScript(@'

    function ThrowTerminatingError {
        [CmdletBinding()]
        param()

        $ex = [System.ArgumentException]::new('terminating-exception')
        $er = [System.Management.Automation.ErrorRecord]::new($ex, 'ThrowTerminatingError:error', 'InvalidArgument', $null)
        $PSCmdlet.ThrowTerminatingError($er)

        Write-Verbose -Verbose "verbose-message"
    }

    function ErrorActionStop {
        [CmdletBinding()]
        param()

        Get-Command NonExist -ErrorAction Stop
        Write-Verbose -Verbose "verbose-message"
    }

    function ThrowException {
        [CmdletBinding()]
        param()

        throw 'throw-exception'
        Write-Verbose -Verbose "verbose-message"
    }

    function WriteErrorAPI {
        [CmdletBinding()]
        param()

        $ex = [System.ArgumentException]::new('arg-exception')
        $er = [System.Management.Automation.ErrorRecord]::new($ex, 'WriteErrorAPI:error', 'InvalidArgument', $null)
        $PSCmdlet.WriteError($er)

        Write-Verbose -Verbose "verbose-message"
    }

    function WriteErrorCmdlet {
        [CmdletBinding()]
        param()

        Write-Error 'write-error-cmdlet'
        Write-Verbose -Verbose "verbose-message"
    }

    function MethodInvocationThrowException {
        [CmdletBinding()]
        param()

        ## This method call throws exception.
        $iss = [initialsessionstate]::Create()
        $iss.ImportPSModule($null)

        Write-Verbose -Verbose "verbose-message"
    }

    function ExpressionThrowException {
        [CmdletBinding()]
        param()

        1/0 ## throw exception.
        Write-Verbose -Verbose "verbose-message"
    }

'@).Invoke()

        function RunCommand {
            param(
                [string] $Command,
                [ValidateSet('Continue', 'Ignore', 'SilentlyContinue', 'Stop')]
                [string] $ErrorAction
            )

            $pwsh.Commands.Clear()
            $pwsh.Streams.ClearStreams()
            $pwsh.AddCommand($command).AddParameter('ErrorAction', $ErrorAction).Invoke()
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
    }

    AfterAll {
        $pwsh.Dispose()
    }

    Context 'Terminating error' {

        It "'ThrowTerminatingError' stops execution with 'ActionPreference.<ErrorAction>' for error action" -TestCases @(
            @{ ErrorAction = 'Continue' }
            @{ ErrorAction = 'Ignore' }
            @{ ErrorAction = 'SilentlyContinue' }
        ) {
            param ($ErrorAction)

            $failure = $null

            try {
                RunCommand -Command 'ThrowTerminatingError' -ErrorAction $ErrorAction
            } catch {
                $failure = $_
            }

            $failure | Should -Not -BeNullOrEmpty
            $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
            $failure.Exception.InnerException | Should -BeOfType 'System.Management.Automation.CmdletInvocationException'
            $failure.Exception.InnerException.InnerException | Should -BeOfType 'System.ArgumentException'
            $failure.Exception.InnerException.InnerException.Message | Should -BeExactly 'terminating-exception'

            $pwsh.Streams.Verbose.Count | Should -BeExactly 0
        }

        It "'-ErrorAction Stop' stops execution with 'ActionPreference.<ErrorAction>' for error action" -TestCases @(
            @{ ErrorAction = 'Continue' }
            @{ ErrorAction = 'Ignore' }
            @{ ErrorAction = 'SilentlyContinue' }
        ) {
            param ($ErrorAction)

            $failure = $null

            try {
                RunCommand -Command 'ErrorActionStop' -ErrorAction $ErrorAction
            } catch {
                $failure = $_
            }

            $failure | Should -Not -BeNullOrEmpty
            $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
            $failure.Exception.InnerException | Should -BeOfType 'System.Management.Automation.ActionPreferenceStopException'
            $failure.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'CommandNotFoundException,Microsoft.PowerShell.Commands.GetCommandCommand'

            $pwsh.Streams.Verbose.Count | Should -BeExactly 0
        }

        It "'throw' statement stops execution with 'ActionPreference.Continue' (the default error action)" {
            $failure = $null

            try {
                RunCommand -Command 'ThrowException' -ErrorAction 'Continue'
            } catch {
                $failure = $_
            }

            $failure | Should -Not -BeNullOrEmpty
            $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
            $failure.Exception.InnerException | Should -BeOfType 'System.Management.Automation.RuntimeException'
            $failure.Exception.InnerException.Message | Should -BeExactly 'throw-exception'

            $pwsh.Streams.Verbose.Count | Should -BeExactly 0
        }

        <# The 'throw' statement is special, in that it can be suppressed by '-ErrorAction SilentlyContinue/Ignore' #>
        It "'throw' statement continues execution with 'ActionPreference.<ErrorAction>'" -TestCases @(
            @{ ErrorAction = 'Ignore' }
            @{ ErrorAction = 'SilentlyContinue' }
        ) {
            param ($ErrorAction)

            $failure = $null

            try {
                RunCommand -Command 'ThrowException' -ErrorAction $ErrorAction
            } catch {
                $failure = $_
            }

            $failure | Should -BeNullOrEmpty
            $pwsh.Streams.Verbose.Count | Should -BeExactly 1
            $pwsh.Streams.Verbose | Should -BeExactly 'verbose-message'

            ## The suppressed 'throw' exception is not written to the error stream, not sure why but it's the current behavior.
            $pwsh.Streams.Error.Count | Should -BeExactly 0

            ## The suppressed 'throw' exception is kept in '$Error'
            $err = GetLastError
            $err | Should -Not -BeNullOrEmpty
            $err.FullyQualifiedErrorId | Should -BeExactly 'throw-exception'
        }

        It "'throw' statement stops execution with 'ActionPreference.<ErrorAction>' when it's wrapped in 'try/catch'" -TestCases @(
            @{ ErrorAction = 'Ignore' }
            @{ ErrorAction = 'SilentlyContinue' }
        ) {
            param ($ErrorAction)

            RunScript -Script "try { ThrowException -ErrorAction $ErrorAction } catch { Write-Debug -Debug `$_.FullyQualifiedErrorId }"

            $pwsh.Streams.Verbose.Count | Should -BeExactly 0
            $pwsh.Streams.Debug.Count | Should -BeExactly 1
            $pwsh.Streams.Debug | Should -BeExactly 'throw-exception'
        }

        It "'throw' statement stops execution with 'ActionPreference.<ErrorAction>' when it's accompanied by 'trap' statement" -TestCases @(
            @{ ErrorAction = 'Ignore' }
            @{ ErrorAction = 'SilentlyContinue' }
        ) {
            param ($ErrorAction)

            RunScript -Script "trap { Write-Debug -Debug `$_.FullyQualifiedErrorId; continue } ThrowException -ErrorAction $ErrorAction"

            $pwsh.Streams.Verbose.Count | Should -BeExactly 0
            $pwsh.Streams.Debug.Count | Should -BeExactly 1
            $pwsh.Streams.Debug | Should -BeExactly 'throw-exception'
        }
    }

    Context 'Non-terminating error' {

        It "'WriteErrorAPI' continues execution with with 'ActionPreference.Continue' (the default error action)" {
            RunCommand -Command 'WriteErrorAPI' -ErrorAction Continue

            $pwsh.Streams.Error.Count | Should -BeExactly 1
            $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'arg-exception'
            $pwsh.Streams.Error[0].FullyQualifiedErrorId | Should -BeExactly 'WriteErrorAPI:error,WriteErrorAPI'

            $pwsh.Streams.Verbose.Count | Should -BeExactly 1
            $pwsh.Streams.Verbose | Should -BeExactly 'verbose-message'
        }

        It "'WriteErrorAPI' continues execution with with 'ActionPreference.Ignore' without logging the error in `$Error" {
            ClearDollarError
            RunCommand -Command 'WriteErrorAPI' -ErrorAction Ignore

            $pwsh.Streams.Error.Count | Should -BeExactly 0
            $pwsh.Streams.Verbose.Count | Should -BeExactly 1
            $pwsh.Streams.Verbose | Should -BeExactly 'verbose-message'

            $lastErr = GetLastError
            $lastErr | Should -BeNullOrEmpty
        }

        It "'WriteErrorAPI' continues execution with with 'ActionPreference.SilentlyContinue' and logs the error in `$Error" {
            ClearDollarError
            RunCommand -Command 'WriteErrorAPI' -ErrorAction SilentlyContinue

            $pwsh.Streams.Error.Count | Should -BeExactly 0
            $pwsh.Streams.Verbose.Count | Should -BeExactly 1
            $pwsh.Streams.Verbose | Should -BeExactly 'verbose-message'

            $lastErr = GetLastError
            $lastErr | Should -Not -BeNullOrEmpty
            $lastErr.Exception.Message | Should -BeExactly 'arg-exception'
            $lastErr.FullyQualifiedErrorId | Should -BeExactly 'WriteErrorAPI:error,WriteErrorAPI'
        }

        It "'WriteErrorCmdlet' continues execution with with 'ActionPreference.Continue' (the default error action)" {
            RunCommand -Command 'WriteErrorCmdlet' -ErrorAction Continue

            $pwsh.Streams.Error.Count | Should -BeExactly 1
            $pwsh.Streams.Error[0].Exception.Message | Should -BeExactly 'write-error-cmdlet'
            $pwsh.Streams.Error[0].FullyQualifiedErrorId | Should -BeExactly 'Microsoft.PowerShell.Commands.WriteErrorException,WriteErrorCmdlet'

            $pwsh.Streams.Verbose.Count | Should -BeExactly 1
            $pwsh.Streams.Verbose | Should -BeExactly 'verbose-message'
        }

        It "'WriteErrorCmdlet' continues execution with with 'ActionPreference.Ignore' without logging the error in `$Error" {
            ClearDollarError
            RunCommand -Command 'WriteErrorCmdlet' -ErrorAction Ignore

            $pwsh.Streams.Error.Count | Should -BeExactly 0
            $pwsh.Streams.Verbose.Count | Should -BeExactly 1
            $pwsh.Streams.Verbose | Should -BeExactly 'verbose-message'

            $lastErr = GetLastError
            $lastErr | Should -BeNullOrEmpty
        }

        It "'WriteErrorCmdlet' continues execution with with 'ActionPreference.SilentlyContinue' and logs the error in `$Error" {
            ClearDollarError
            RunCommand -Command 'WriteErrorCmdlet' -ErrorAction SilentlyContinue

            $pwsh.Streams.Error.Count | Should -BeExactly 0
            $pwsh.Streams.Verbose.Count | Should -BeExactly 1
            $pwsh.Streams.Verbose | Should -BeExactly 'verbose-message'

            $lastErr = GetLastError
            $lastErr | Should -Not -BeNullOrEmpty
            $lastErr.Exception.Message | Should -BeExactly 'write-error-cmdlet'
            $lastErr.FullyQualifiedErrorId | Should -BeExactly 'Microsoft.PowerShell.Commands.WriteErrorException,WriteErrorCmdlet'
        }
    }

    Context 'Exception thrown from the method invocation or expression' {

        #region MethodInvocationThrowException

        It "'MethodInvocationThrowException' emits non-terminating error with 'ActionPreference.Continue' (the default error action)" {
            RunCommand -Command 'MethodInvocationThrowException' -ErrorAction 'Continue'

            $pwsh.Streams.Error.Count | Should -BeExactly 1
            $pwsh.Streams.Error[0].FullyQualifiedErrorId | Should -BeExactly 'ArgumentNullException'

            $pwsh.Streams.Verbose.Count | Should -BeLikeExactly 1
            $pwsh.Streams.Verbose | Should -BeExactly 'verbose-message'
        }

        It "'MethodInvocationThrowException' emits no error with 'ActionPreference.<ErrorAction>'" -TestCases @(
            @{ ErrorAction = 'Ignore' }
            @{ ErrorAction = 'SilentlyContinue' }
        ) {
            param ($ErrorAction)

            RunCommand -Command 'MethodInvocationThrowException' -ErrorAction $ErrorAction

            $pwsh.Streams.Error.Count | Should -BeExactly 0
            $pwsh.Streams.Verbose.Count | Should -BeExactly 1
            $pwsh.Streams.Verbose | Should -BeExactly 'verbose-message'

            ## The suppressed exception is kept in '$Error'
            $err = GetLastError
            $err | Should -Not -BeNullOrEmpty
            $err.FullyQualifiedErrorId | Should -BeExactly 'ArgumentNullException'
        }

        It "'MethodInvocationThrowException' emits terminating error with 'ActionPreference.Stop'" {
            $failure = $null

            try {
                RunCommand -Command 'MethodInvocationThrowException' -ErrorAction Stop
            } catch {
                $failure = $_
            }

            $failure | Should -Not -BeNullOrEmpty
            $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
            $failure.Exception.InnerException | Should -BeOfType 'System.Management.Automation.CmdletInvocationException'
            $failure.Exception.InnerException.InnerException.InnerException | Should -BeOfType 'System.ArgumentNullException'

            $pwsh.Streams.Verbose.Count | Should -BeExactly 0
        }

        It "'MethodInvocationThrowException' emits terminating error with 'ActionPreference.<ErrorAction>' when wrapped in 'try/catch'" -TestCases @(
            @{ ErrorAction = 'Continue' }
            @{ ErrorAction = 'Ignore' }
            @{ ErrorAction = 'SilentlyContinue' }
        ) {
            param ($ErrorAction)

            RunScript -Script "try { MethodInvocationThrowException -ErrorAction $ErrorAction } catch { Write-Debug -Debug `$_.Exception.InnerException.GetType().FullName }"

            $pwsh.Streams.Error.Count | Should -BeExactly 0
            $pwsh.Streams.Verbose.Count | Should -BeExactly 0
            $pwsh.Streams.Debug.Count | Should -BeExactly 1
            $pwsh.Streams.Debug | Should -BeExactly 'System.ArgumentNullException'
        }

        It "'MethodInvocationThrowException' emits terminating error with 'ActionPreference.<ErrorAction>' when accompanied by 'trap' statement" -TestCases @(
            @{ ErrorAction = 'Continue' }
            @{ ErrorAction = 'Ignore' }
            @{ ErrorAction = 'SilentlyContinue' }
        ) {
            param ($ErrorAction)

            RunScript -Script "trap { Write-Debug -Debug `$_.Exception.InnerException.GetType().FullName; continue } MethodInvocationThrowException -ErrorAction $ErrorAction"

            $pwsh.Streams.Error.Count | Should -BeExactly 0
            $pwsh.Streams.Verbose.Count | Should -BeExactly 0
            $pwsh.Streams.Debug.Count | Should -BeExactly 1
            $pwsh.Streams.Debug | Should -BeExactly 'System.ArgumentNullException'
        }

        #endregion

        #region ExpressionThrowException

        It "'ExpressionThrowException' emits non-terminating error with 'ActionPreference.Continue' (the default error action)" {
            RunCommand -Command 'ExpressionThrowException' -ErrorAction 'Continue'

            $pwsh.Streams.Error.Count | Should -BeExactly 1
            $pwsh.Streams.Error[0].Exception.InnerException | Should -BeOfType 'System.DivideByZeroException'

            $pwsh.Streams.Verbose.Count | Should -BeLikeExactly 1
            $pwsh.Streams.Verbose | Should -BeExactly 'verbose-message'
        }

        It "'ExpressionThrowException' emits no error with 'ActionPreference.<ErrorAction>'" -TestCases @(
            @{ ErrorAction = 'Ignore' }
            @{ ErrorAction = 'SilentlyContinue' }
        ) {
            param ($ErrorAction)

            RunCommand -Command 'ExpressionThrowException' -ErrorAction $ErrorAction

            $pwsh.Streams.Error.Count | Should -BeExactly 0
            $pwsh.Streams.Verbose.Count | Should -BeExactly 1
            $pwsh.Streams.Verbose | Should -BeExactly 'verbose-message'

            ## The suppressed exception is kept in '$Error'
            $err = GetLastError
            $err | Should -Not -BeNullOrEmpty
            $err.Exception.InnerException | Should -BeOfType 'System.DivideByZeroException'
        }

        It "'ExpressionThrowException' emits terminating error with 'ActionPreference.Stop'" {
            $failure = $null

            try {
                RunCommand -Command 'ExpressionThrowException' -ErrorAction Stop
            } catch {
                $failure = $_
            }

            $failure | Should -Not -BeNullOrEmpty
            $failure.Exception | Should -BeOfType 'System.Management.Automation.MethodInvocationException'
            $failure.Exception.InnerException | Should -BeOfType 'System.Management.Automation.CmdletInvocationException'
            $failure.Exception.InnerException.InnerException.InnerException | Should -BeOfType 'System.DivideByZeroException'

            $pwsh.Streams.Verbose.Count | Should -BeExactly 0
        }

        It "'ExpressionThrowException' emits terminating error with 'ActionPreference.<ErrorAction>' when wrapped in 'try/catch'" -TestCases @(
            @{ ErrorAction = 'Continue' }
            @{ ErrorAction = 'Ignore' }
            @{ ErrorAction = 'SilentlyContinue' }
        ) {
            param ($ErrorAction)

            RunScript -Script "try { ExpressionThrowException -ErrorAction $ErrorAction } catch { Write-Debug -Debug `$_.Exception.InnerException.GetType().FullName }"

            $pwsh.Streams.Error.Count | Should -BeExactly 0
            $pwsh.Streams.Verbose.Count | Should -BeExactly 0
            $pwsh.Streams.Debug.Count | Should -BeExactly 1
            $pwsh.Streams.Debug | Should -BeExactly 'System.DivideByZeroException'
        }

        It "'ExpressionThrowException' emits terminating error with 'ActionPreference.<ErrorAction>' when accompanied by 'trap' statement" -TestCases @(
            @{ ErrorAction = 'Continue' }
            @{ ErrorAction = 'Ignore' }
            @{ ErrorAction = 'SilentlyContinue' }
        ) {
            param ($ErrorAction)

            RunScript -Script "trap { Write-Debug -Debug `$_.Exception.InnerException.GetType().FullName; continue } ExpressionThrowException -ErrorAction $ErrorAction"

            $pwsh.Streams.Error.Count | Should -BeExactly 0
            $pwsh.Streams.Verbose.Count | Should -BeExactly 0
            $pwsh.Streams.Debug.Count | Should -BeExactly 1
            $pwsh.Streams.Debug | Should -BeExactly 'System.DivideByZeroException'
        }

        #endregion
    }
}
