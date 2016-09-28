Describe "InitialSessionState capacity" -Tags CI {
    BeforeAll {
        $iss = [initialsessionstate]::CreateDefault()

        for ($i = 0; $i -lt 5000; $i++)
        {
            $ssfe = [System.Management.Automation.Runspaces.SessionStateFunctionEntry]::new("f$i", "'fn f$i'")
            $iss.Commands.Add($ssfe)

            $ssve = [System.Management.Automation.Runspaces.SessionStateVariableEntry]::new("v$i", "var v$i", $null)
            $iss.Variables.Add($ssve)

            $ssae = [System.Management.Automation.Runspaces.SessionStateAliasEntry]::new("a$i", "f$i")
            $iss.Commands.Add($ssae)
        }

        $ps = [PowerShell]::Create($iss)
    }

    AfterAll {
        $ps.Dispose()
    }

    BeforeEach {
        $ps.Commands.Clear()
    }

    It "function capacity in initial session state should not be limited" {
        $ps.AddCommand('f4999').Invoke() | Should Be "fn f4999"
        $ps.Streams.Error | Should Be $null
    }

    It "alias capacity in initial session state should not be limited" {
        $ps.AddCommand('a4999').Invoke() | Should Be "fn f4999"
        $ps.Streams.Error | Should Be $null
    }

    It "variable capacity in initial session state should not be limited" {
        $ps.AddScript('$v4999').Invoke() | Should Be "var v4999"
        $ps.Streams.Error | Should Be $null
    }

    It "function capacity should not be limited after runspace is opened" {
        $ps.AddScript('function f5000 { "in f5000" } f5000').Invoke() | Should Be "in f5000"
        $ps.Streams.Error | Should Be $null
    }

    It "variable capacity should not be limited after runspace is opened" {
        $ps.AddScript('$v5000 = "var v5000"; $v5000').Invoke() | Should Be "var v5000"
        $ps.Streams.Error | Should Be $null
    }

    It "alias capacity should not be limited after runspace is opened" {
        $ps.AddScript('New-Alias -Name a5000 -Value f1; a5000').Invoke() | Should Be "fn f1"
        $ps.Streams.Error | Should Be $null
    }
}