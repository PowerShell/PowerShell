# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "InvokeOnRunspace method argument error handling" -tags "Feature" {

    BeforeAll {
        $command = [System.Management.Automation.PSCommand]::new()
        $localRunspace = $host.Runspace
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
        $currentRunspace = $host.Runspace

        $results = [System.Management.Automation.HostUtilities]::InvokeOnRunspace($command, $currentRunspace)

        $results[0] | Should -Be "Hello!"
    }
}

Describe "InvokeOnRunspace method on remote runspace" -tags "Feature","RequireAdminOnWindows" {

    BeforeAll {

        if ($IsWindows) {
            $script:remoteRunspace = New-RemoteRunspace
        }
    }

    AfterAll {
        if ($script:remoteRunspace)
        {
            $script:remoteRunspace.Dispose();
        }
    }

    It "Method should successfully invoke command on remote runspace" -Skip:(!$IsWindows) {

        $command = [System.Management.Automation.PSCommand]::new()
        $command.AddScript('"Hello!"')

        $results = [System.Management.Automation.HostUtilities]::InvokeOnRunspace($command, $script:remoteRunspace)

        $results[0] | Should -Be "Hello!"
    }
}
