# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Exec function tests' {
    It 'Exec by itself does nothing' {
        exec | Should -BeNullOrEmpty
    }

    It 'Exec should run a command line: <command>' -TestCases @(
        @{ command = 'get-command -verb invoke' }
        @{ command = 'get-module' }
        @{ command = 'write-output (1+1)' }
    ) {
        param($command)

        $a = Invoke-Expression "exec $command"
        $b = Invoke-Expression $command
        Compare-Object -ReferenceObject $a -DifferenceObject $b | Should -BeNullOrEmpty
    }
}
