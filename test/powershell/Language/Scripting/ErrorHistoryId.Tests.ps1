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
    }

    BeforeEach {
        $ps = [PowerShell]::Create("NewRunspace")
    }

    AfterEach {
        $ps.Dispose()
    }

    It "Error from 'Write-Error' has the right history Id" {
        $null = RunCommand $ps "function bar { Write-Error 'abc' }"   ## 1st in history
        $null = RunCommand $ps "bar"                                  ## 2nd in history

        $ps.HadErrors | Should -BeTrue
        RunCommand $ps '$Error[0].InvocationInfo.HistoryId' | Should -Be 2
    }

    It "Error from 'PSCmdlet.WriteError' has the right history Id" {
        ## 1st in history
        $null = RunCommand $ps @'
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
        ## 2nd in history
        $null = RunCommand $ps 'bar'

        $ps.HadErrors | Should -BeTrue
        $err = RunCommand $ps '$Error[0]'
        $err.FullyQualifiedErrorId | Should -BeExactly "PSCmdlet.WriteError,bar"
        $err.InvocationInfo.HistoryId | Should -Be 2
    }

    It "Error from 'PSCmdlet.ThrowTerminatingError' has the right history Id" {
        ## 1st in history
        $null = RunCommand $ps @'
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
        ## 2nd in history
        $null = RunCommand $ps 'bar'
        $ps.HadErrors | Should -BeTrue
        $err = RunCommand $ps '$Error[0]'
        $err.FullyQualifiedErrorId | Should -BeExactly "PSCmdlet.ThrowTerminatingError,bar"
        $err.InvocationInfo.HistoryId | Should -Be 2
    }

    It "Error from 'Throw' has the right history Id - 1" {
        try {
            ## 1st in history
            $null = RunCommand $ps '1+1'
            ## 2nd in history
            $null = RunCommand $ps "throw 'abc'"
        } catch {
            ## ignore the exception
        }

        $ps.HadErrors | Should -BeTrue
        $err = RunCommand $ps '$Error[0]'
        $err.FullyQualifiedErrorId | Should -BeExactly 'abc'
        $err.InvocationInfo.HistoryId | Should -Be 2
    }

    It "Error from 'Throw' has the right history Id - 2" {
        try {
            ## 1st in history
            $null = RunCommand $ps "function bar { throw 'abc' }"
            ## 2nd in history
            $null = RunCommand $ps 'bar'
        } catch {
            ## ignore the exception
        }

        $ps.HadErrors | Should -BeTrue
        $err = RunCommand $ps '$Error[0]'
        $err.FullyQualifiedErrorId | Should -BeExactly 'abc'
        $err.InvocationInfo.HistoryId | Should -Be 2
    }

    It "Error from parameter binding has the right history Id" {
        ## 1st in history
        $null = RunCommand $ps '1+1'
        ## 2nd in history
        $null = RunCommand $ps 'Get-Verb -abc'

        $ps.HadErrors | Should -BeTrue
        $err = RunCommand $ps '$Error[0]'
        $err.FullyQualifiedErrorId | Should -BeExactly 'NamedParameterNotFound,Microsoft.PowerShell.Commands.GetVerbCommand'
        $err.InvocationInfo.HistoryId | Should -Be 2
    }

    It "Error from language exception has the right history Id - 1" {
        ## 1st in history
        $null = RunCommand $ps '1+1'
        ## 2nd in history
        $null = RunCommand $ps "1/0"

        $ps.HadErrors | Should -BeTrue
        $err = RunCommand $ps '$Error[0]'
        $err.FullyQualifiedErrorId | Should -BeExactly 'RuntimeException'
        $err.InvocationInfo.HistoryId | Should -Be 2
    }

    It "Error from language exception has the right history Id - 2" {
        ## 1st in history
        $null = RunCommand $ps 'function bar { 1/0 }'
        ## 2nd in history
        $null = RunCommand $ps 'bar'

        $ps.HadErrors | Should -BeTrue
        $err = RunCommand $ps '$Error[0]'
        $err.FullyQualifiedErrorId | Should -BeExactly 'RuntimeException'
        $err.InvocationInfo.HistoryId | Should -Be 2
    }

    It "Error from language exception has the right history Id - 3" {
        ## 1st in history
        $null = RunCommand $ps '1+1'
        ## 2nd in history
        $null = RunCommand $ps '[System.IO.Path]::GetExtension()'

        $ps.HadErrors | Should -BeTrue
        $err = RunCommand $ps '$Error[0]'
        $err.FullyQualifiedErrorId | Should -BeExactly 'MethodCountCouldNotFindBest'
        $err.InvocationInfo.HistoryId | Should -Be 2
    }

    It "Error from language exception has the right history Id - 4" {
        ## 1st in history
        $null = RunCommand $ps 'function bar { [System.IO.Path]::GetExtension() }'
        ## 2nd in history
        $null = RunCommand $ps 'bar'

        $ps.HadErrors | Should -BeTrue
        $err = RunCommand $ps '$Error[0]'
        $err.FullyQualifiedErrorId | Should -BeExactly 'MethodCountCouldNotFindBest'
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
