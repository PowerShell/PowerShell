# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "InvokeOnRunspace method argument error handling" -tags "Feature" {

    BeforeAll {
        $command = [System.Management.Automation.PSCommand]::new()
        $localRunspace = $Host.Runspace
    }

    It "Null argument exception should be thrown for null PSCommand argument" {
        { [System.Management.Automation.HostUtilities]::InvokeOnRunspace($null, $localRunspace) } |
            Should -Throw -ErrorId "PSArgumentNullException"
    }

    It "Null argument exception should be thrown for null Runspace argument" {
        { [System.Management.Automation.HostUtilities]::InvokeOnRunspace($command, $null) } |
            Should -Throw -ErrorId "PSArgumentNullException"
    }
}

Describe "InvokeOnRunspace method as nested command" -tags "Feature" {

    It "Method should successfully invoke command as nested on busy runspace" {

        $command = [System.Management.Automation.PSCommand]::new()
        $command.AddScript('"Hello!"')
        $currentRunspace = $Host.Runspace

        $results = [System.Management.Automation.HostUtilities]::InvokeOnRunspace($command, $currentRunspace)

        $results[0] | Should -Be "Hello!"
    }
}

Describe "InvokeOnRunspace method on remote runspace" -tags "Feature","RequireAdminOnWindows" {

    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()

        $skipTest = (Test-IsWinWow64) -or !$IsWindows

        if ($skipTest) {
            $global:PSDefaultParameterValues["it:skip"] = $true
            return
        }

        if ($IsWindows) {
            $script:remoteRunspace = New-RemoteRunspace
        }
    }

    AfterAll {
        if ($script:remoteRunspace -and -not $pendingTest)
        {
            $script:remoteRunspace.Dispose();
        }

        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It "Method should successfully invoke command on remote runspace" -Skip:$skipTest {

        $command = [System.Management.Automation.PSCommand]::new()
        $command.AddScript('"Hello!"')

        $results = [System.Management.Automation.HostUtilities]::InvokeOnRunspace($command, $script:remoteRunspace)

        $results[0] | Should -Be "Hello!"
    }
}

Describe 'PromptForCredential' -Tags "CI" {
    BeforeAll {
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('NoPromptForPassword', $true)
    }

    AfterAll {
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('NoPromptForPassword', $false)
    }

    It 'Should accept no targetname' {
        $out = $Host.UI.PromptForCredential('caption','message','myUser',$null)
        $out.UserName | Should -BeExactly 'myUser'
    }

    It 'Should accept targetname as domain' {
        $out = $Host.UI.PromptForCredential('caption','message','myUser','myDomain')
        $out.UserName | Should -BeExactly 'myDomain\myUser'
    }
}
