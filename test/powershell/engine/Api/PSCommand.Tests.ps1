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

                # We manually create a CmdletInfo here (with an unresolable command) to verify that CmdletInfo's are cloned.
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

    Context "GetSteppablePipeline with a command added by CommandInfo" {
        BeforeAll {
            function Invoke-SteppablePipeline {
                param([powershell] $Shell)

                $steppablePipeline = $Shell.GetSteppablePipeline()
                try {
                    $steppablePipeline.Begin($false)
                    $steppablePipeline.End()
                }
                finally {
                    $steppablePipeline.Dispose()
                }
            }
        }

        BeforeEach {
            $steppableShell = [powershell]::Create('CurrentRunspace')
        }

        AfterEach {
            $steppableShell.Dispose()
        }

        It "Runs a cmdlet that cannot be resolved by name" {
            $cmdlet = [System.Management.Automation.CmdletInfo]::new('un-resolvable', [Microsoft.PowerShell.Commands.OutStringCommand])
            $null = $steppableShell.AddCommand($cmdlet).AddParameter("InputObject", 'test').AddParameter("Stream")

            $result = Invoke-SteppablePipeline -Shell $steppableShell
            $result | Should -Be "test"
        }

        It "Runs the cmdlet it was given rather than a function of the same name" {
            function Get-Date { 'shadowed' }
            $null = $steppableShell.AddCommand((Get-Command -Name Get-Date -CommandType Cmdlet))

            $result = Invoke-SteppablePipeline -Shell $steppableShell
            $result | Should -BeOfType System.DateTime
        }

        It "Uses the alias's name as the invocation name when given an AliasInfo, as Invoke does" {
            function Get-InvocationName { $MyInvocation.InvocationName }
            Set-Alias -Name Get-InvocationNameAlias -Value Get-InvocationName
            $null = $steppableShell.AddCommand((Get-Alias -Name Get-InvocationNameAlias))

            $result = Invoke-SteppablePipeline -Shell $steppableShell
            $result | Should -BeExactly 'Get-InvocationNameAlias'
        }
    }
}
