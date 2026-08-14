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
        $skipTest = (Test-IsWinWow64) -or !$IsWindows

        if ($skipTest) {
            return
        }

        $script:remoteRunspace = New-RemoteRunspace
    }

    AfterAll {
        if ($script:remoteRunspace)
        {
            $script:remoteRunspace.Dispose();
        }
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

Describe 'PushRunspaceLocalFailure' -Tags 'CI' {
    It 'Should throw an exception when pushing a local runspace' {
        $runspace = [RunspaceFactory]::CreateRunspace()
        try {
            $runspace.Open()
            $exc = { $Host.PushRunspace($runspace) } | Should -Throw -PassThru
            $exc.Exception.InnerException | Should -BeOfType ([System.ArgumentException])
            [string]$exc | Should -BeLike "*PushRunspace can only push a remote runspace. (Parameter 'runspace')*"
        }
        finally {
            $runspace.Dispose()
        }
    }
}

Describe 'Host.Rectangle.GetHashCode' -Tags 'CI' {
    It 'Folds negative (Left XOR Right) using lower, not upper, bits' {
        # Regression: the branch for lower < 0 (and not Int32.MinValue) incorrectly did
        # i64 += (UInt64)(-upper) instead of (UInt64)(-lower).
        # left=-1, right=0 -> lower = -1; top=bottom=0 -> upper = 0. Correct i64 low part is 1; buggy was 0.
        $rect = [System.Management.Automation.Host.Rectangle]::new(-1, 0, 0, 0)
        $rect.GetHashCode() | Should -Be 1
    }
}
