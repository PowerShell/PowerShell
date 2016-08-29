$script1 = @'
'aaa'.ToString() > $null
'aa' > $null
"a" 2> $null | % { $_ }
'bb' > $null
'bb'.ToSTring() > $null
'bbb'
'@
$script2 = @'
"line 1"
"line 2"
"line 3"
'@

$testroot = resolve-path (join-path $psscriptroot ../../..)
$common = join-path $testroot Common
$helperModule = join-path $common Test.Helpers.psm1

Describe "Breakpoints when set should be hit" -tag "CI" {
    BeforeAll {
        $path = setup -pass -f TestScript_1.ps1 -content $script1
        $bps = 1..6 | %{ set-psbreakpoint -script $path -line $_ -Action { continue } }
    }
    AfterAll {
        $bps | Remove-PSBreakPoint
    }
    It "A redirected breakpoint is hit" {
        & $path
        foreach ( $bp in $bps ) {
            $bp.HitCount | should be 1
        }
    }
}

Describe "It should be possible to reset runspace debugging" -tag "Feature" {
    BeforeAll {
        import-module $helperModule -force
        $path = setup -pass -f TestScript_2.ps1 -content $script2
        $scriptPath = "$testdrive/TestScript_2.ps1"
        $iss = [initialsessionstate]::CreateDefault2();
        $rs = [runspacefactory]::CreateRunspace($iss)
        $rs.Name = "TestRunspaceDebuggerReset"
        $rs.Open()
        $rs | Enable-RunspaceDebug

        $debuggerBeforeReset = $rs.Debugger

        # Create PowerShell to run script.
        $ps = [powershell]::Create()
        $ps.Runspace = $rs

        # Set breakpoints in runspace.
        $result = $ps.AddScript("Set-PSBreakpoint -Script '$scriptPath' -line 1").Invoke()
        $ps.Commands.Clear()
        $result = $ps.AddScript("Set-PSBreakpoint -Script '$scriptPath' -line 3").Invoke()
        $ps.Commands.Clear()
        $breakpoints = $ps.AddScript("Get-PSBreakpoint").Invoke()

        # Run script file until breakpoint hit.
        $ar = $ps.AddScript("$scriptPath").BeginInvoke()
        $completed = Wait-CompleteExecution { $rs.Debugger.InBreakPoint -eq $true } -timeout 10000 -interval 200 
        $ps.Stop()
        $rs.ResetRunspaceState()
    }
    AfterAll {
        if ( $ps -ne $null ) { $ps.Dispose() }
        if ( $ss -ne $null ) { $rs.Dispose() }
    }
    It "2 breakpoints should have been set" {
        $breakpoints.Count | Should be 2
    }
    It "The breakpoint Should have been hit" {
        $completed | should be $true
    }
    It "The reset debugger should not be in a breakpoint" {
        $rs.Debugger.InBreakPoint | should be $false
    }
    It "The reset debugger should not be active" {
        $rs.Debugger.IsActive | should be $false
    }
    It "The reset debugger mode should be set to 'Default'" {
        $rs.Debugger.DebugMode | should be "Default"
    }
    It "The debugger should be the same before and after the reset" {
        $rs.Debugger | Should be $debuggerBeforeReset
    }
    It "The breakpoints should be gone after reset" {
        $ps.Commands.clear()
        $ps.AddCommand("Get-PSBreakpoint").Invoke() | Should BeNullOrEmpty
    }
    It "The script should run without a break" {
        $ps.Commands.Clear()
        $ps.addscript("$testdrive/TestScript_2.ps1").Invoke().Count | should be 3
    }
}
