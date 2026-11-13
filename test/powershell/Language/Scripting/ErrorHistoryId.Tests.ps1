# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Error HistoryId Tests" -Tags "CI" {

    BeforeAll {
        $setting = [System.Management.Automation.PSInvocationSettings]::New()
        $setting.AddToHistory = $true

        function RunCommand($ps, $command) {
            $ps.Commands.Clear()
            $ps.AddScript($command).Invoke($null, $setting)
        }

        $funcBarUseWriteErrorApi = @'
function bar {
    [CmdletBinding()]
    param()

    $er = [System.Management.Automation.ErrorRecord]::new(
        [System.ArgumentException]::new(),
        'PSCmdlet.WriteError',
        [System.Management.Automation.ErrorCategory]::InvalidOperation,
        $null)
    $PSCmdlet.WriteError($er)
}
'@
        $funcBarThrowTermError = @'
function bar {
    [CmdletBinding()]
    param()

    $er = [System.Management.Automation.ErrorRecord]::new(
        [System.ArgumentException]::new(),
        'PSCmdlet.ThrowTerminatingError',
        [System.Management.Automation.ErrorCategory]::InvalidOperation,
        $null)
    $PSCmdlet.ThrowTerminatingError($er)
}
'@
    }

    BeforeEach {
        $ps = [PowerShell]::Create("NewRunspace")
    }

    AfterEach {
        $ps.Dispose()
    }

    It "Error from <caption> has the right history Id" -TestCases @(
        @{ caption = "'throw' in global scope"; cmd1 = "1+1";                          cmd2 = "throw 'abc'" }
        @{ caption = "'throw' in function";     cmd1 = "function bar { throw 'abc' }"; cmd2 = "bar" }
    ) {
        param($cmd1, $cmd2)

        try {
            ## 1st in history
            $null = RunCommand $ps $cmd1
            ## 2nd in history
            $null = RunCommand $ps $cmd2
        } catch {
            ## ignore the exception
        }

        $ps.HadErrors | Should -BeTrue
        $err = RunCommand $ps '$Error[0]'
        $err.FullyQualifiedErrorId | Should -BeExactly 'abc'
        $err.InvocationInfo.HistoryId | Should -Be 2
    }

    It "Error from <caption> has the right history Id" -TestCases @(
        @{ caption = "parameter binding";     cmd1 = "1+1";                  cmd2 = "Get-Verb -abc"; errId = "NamedParameterNotFound,Microsoft.PowerShell.Commands.GetVerbCommand" }
        @{ caption = "'1/0' in global scope"; cmd1 = "1+1";                  cmd2 = "1/0";           errId = "RuntimeException" }
        @{ caption = "'1/0' in function";     cmd1 = "function bar { 1/0 }"; cmd2 = "bar";           errId = "RuntimeException" }
        @{ caption = "method exception in global scope"; cmd1 = "1+1";       cmd2 = "[System.IO.Path]::GetExtension()"; errId = "MethodCountCouldNotFindBest" }
        @{ caption = "method exception in function"; cmd1 = "function bar { [System.IO.Path]::GetExtension() }"; cmd2 = "bar"; errId = "MethodCountCouldNotFindBest" }
        @{ caption = "'Write-Error'";         cmd1 = "function bar { Write-Error 'abc' }"; cmd2 = "bar"; errId = "Microsoft.PowerShell.Commands.WriteErrorException,bar" }
        @{ caption = "'PSCmdlet.WriteError'"; cmd1 = $funcBarUseWriteErrorApi;             cmd2 = "bar"; errId = "PSCmdlet.WriteError,bar" }
        @{ caption = "'PSCmdlet.ThrowTerminatingError'"; cmd1 = $funcBarThrowTermError;    cmd2 = "bar"; errId = "PSCmdlet.ThrowTerminatingError,bar" }
    ) {
        param($cmd1, $cmd2, $errId)

        ## 1st in history
        $null = RunCommand $ps $cmd1
        ## 2nd in history
        $null = RunCommand $ps $cmd2

        $ps.HadErrors | Should -BeTrue
        $err = RunCommand $ps '$Error[0]'
        $err.FullyQualifiedErrorId | Should -BeExactly $errId
        $err.InvocationInfo.HistoryId | Should -Be 2
    }

    It "ParseError has the right history Id" {
        try {
            ## 1st in history
            $null = RunCommand $ps '1+1'
            ## 2nd in history
            $null = RunCommand $ps 'for (int i = 2; i < 3; i++) { foreach {} }'
        } catch {
            ## ignore the exception
        }

        $ps.HadErrors | Should -BeTrue
        $err = RunCommand $ps '$Error[0]'
        $err | Should -BeOfType 'System.Management.Automation.ParseException'
        $err.ErrorRecord.InvocationInfo.HistoryId | Should -Be 2
    }
}
