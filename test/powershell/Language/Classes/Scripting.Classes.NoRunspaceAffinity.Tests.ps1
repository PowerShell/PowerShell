# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Class can be defined without Runspace affinity" -Tags "CI" {

    It "Applying the 'NoRunspaceAffinity' attribute make the class not affiliate with a particular Runspace/SessionState" {
        [NoRunspaceAffinity()]
        class NoAffinity {
            [string] $Name;
            [int] $RunspaceId;

            NoAffinity() {
               $this.RunspaceId = [runspace]::DefaultRunspace.Id
            }

            static [int] Echo() {
                return [runspace]::DefaultRunspace.Id
            }

            [int] SetAndEcho([string] $value) {
                $this.Name = $value
                return [runspace]::DefaultRunspace.Id
            }
        }

        $t = [NoAffinity]
        $o = [NoAffinity]::new()

        ## Running directly should use the current Runspace/SessionState.
        $t::Echo() | Should -Be $Host.Runspace.Id
        $o.RunspaceId | Should -Be $Host.Runspace.Id
        $o.SetAndEcho('Blue') | Should -Be $Host.Runspace.Id
        $o.Name | Should -Be 'Blue'

        ## Running in a new Runspace should use that Runspace and its current SessionState.
        try {
            $ps = [powershell]::Create()
            $ps.AddScript('function CallEcho($type) { $type::Echo() }').Invoke() > $null; $ps.Commands.Clear()
            $ps.AddScript('function CallSetAndEcho($obj) { $obj.SetAndEcho(''Hello world'') }').Invoke() > $null; $ps.Commands.Clear()
            $ps.AddScript('function GetName($obj) { $obj.Name }').Invoke() > $null; $ps.Commands.Clear()
            $ps.AddScript('function NewObj($type) { $type::new().RunspaceId }').Invoke() > $null; $ps.Commands.Clear()

            $ps.AddCommand('CallEcho').AddArgument($t).Invoke() | Should -Be $ps.Runspace.Id; $ps.Commands.Clear()
            $ps.AddCommand('CallSetAndEcho').AddArgument($o).Invoke() | Should -Be $ps.Runspace.Id; $ps.Commands.Clear()
            $ps.AddCommand('GetName').AddArgument($o).Invoke() | Should -Be 'Hello world'; $ps.Commands.Clear()
            $ps.AddCommand('NewObj').AddArgument($t).Invoke() | Should -Be $ps.Runspace.Id; $ps.Commands.Clear()
        }
        finally {
            $ps.Dispose()
        }
    }
}
