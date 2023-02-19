# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "PSCommand API tests" -Tag "CI" {
    BeforeAll {
        $shell = [PowerShell]::Create()
    }

    AfterAll {
        $shell.Dispose()
    }

    BeforeEach {
        $psCommand = [System.Management.Automation.PSCommand]::new()
    }

    AfterEach {
        $shell.Commands.Clear()
    }

    Context "AddCommand method on PSCommand" {
        It "Can add a command by name and parameter to a PSCommand" {
            $null = $psCommand.AddCommand("Write-Output").AddParameter("InputObject", 5)
            $shell.Commands = $psCommand
            $result = $shell.Invoke()
            $result | Should -Be 5
        }

        It "Can add a command by Command object and parameter to a PSCommand" {
            $command = [System.Management.Automation.Runspaces.Command]::new("Get-Date")
            $null = $psCommand.AddCommand($command)
            $shell.Commands = $psCommand
            $result = $shell.Invoke()
            $result | Should -BeOfType System.DateTime
        }
    }

    Context "Cloning PSCommands" {
        It "The clone method successfully copies commands" {
            $null = $psCommand.AddCommand("Write-Output").AddParameter("InputObject", 5)

            $newCommand = $psCommand.Clone()

            $newCommand.Commands | Should -Not -BeNullOrEmpty
            $newCommand.Commands[0].CommandText | Should -Be "Write-Output"
            $newCommand.Commands[0].Parameters | Should -Not -BeNullOrEmpty
            $newCommand.Commands[0].Parameters[0].Name | Should -Be "InputObject"
            $newCommand.Commands[0].Parameters[0].Value | Should -Be 5
        }

        It "clones properly when using the setter on the PowerShell type" {
            try {
                $otherShell = [powershell]::Create('CurrentRunspace')

                # We manually create a CmdletInfo here (with an unresolvable command) to verify that CmdletInfo's are cloned.
                $cmdlet = [System.Management.Automation.CmdletInfo]::new('un-resolvable', [Microsoft.PowerShell.Commands.OutStringCommand])
                $null = $otherShell.AddCommand($cmdlet).AddParameter("InputObject", 'test').AddParameter("Stream")

                # Setter for "Commands" calls PSCommand.Clone()
                $shell.Commands = $otherShell.Commands

                $result = $shell.Invoke()
                $result | Should -Be "test"
            }
            finally {
                $otherShell.Dispose()
            }
        }
    }
}
