# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Basic debugger tests' -Tag 'CI' {

    BeforeAll {
        Register-DebuggerHandler
    }

    AfterAll {
        Unregister-DebuggerHandler
    }

    Context 'The value of $? should be preserved when exiting the debugger' {
        BeforeAll {
            $testScript = {
                function Test-DollarQuestionMark {
                    [CmdletBinding()]
                    param()
                    Get-Process -Id ([int]::MaxValue)
                    if (-not $?) {
                        'The value of $? was preserved during debugging.'
                    } else {
                        'The value of $? was changed to $true during debugging.'
                    }
                }
                $global:DollarQuestionMarkResults = Test-DollarQuestionMark -ErrorAction Break
            }

            $global:results = @(Test-Debugger -Scriptblock $testScript -CommandQueue '$?')
        }

        AfterAll {
            Remove-Variable -Name DollarQuestionMarkResults -Scope Global -ErrorAction Ignore
        }

        It 'Should show 2 debugger commands were invoked' {
            # One extra for the implicit 'c' command that keeps the debugger automation moving
            $results.Count | Should -Be 2
        }

        It 'Should have $false output from the first $? command' {
            $results[0].Output | Should -BeOfType bool
            $results[0].Output | Should -Not -BeTrue
        }

        It 'Should have string output showing that $? was preserved as $false by the debugger' {
            $global:DollarQuestionMarkResults | Should -BeOfType string
            $global:DollarQuestionMarkResults | Should -BeExactly 'The value of $? was preserved during debugging.'
        }
    }
}

Describe "Breakpoints when set should be hit" -Tag "CI" {
    Context "Basic tests" {
        BeforeAll {
            $script = @'
'aaa'.ToString() > $null
'aa' > $null
"a" 2> $null | ForEach-Object { $_ }
'bb' > $null
'bb'.ToSTring() > $null
'bbb'
'@
            $path = Setup -PassThru -File BasicTest.ps1 -Content $script
            $bps = 1..6 | ForEach-Object { Set-PSBreakpoint -Script $path -Line $_ -Action { continue } }
        }

        AfterAll {
            $bps | Remove-PSBreakpoint
        }

        It "A redirected breakpoint is hit" {
            & $path
            foreach ( $bp in $bps ) {
                $bp.HitCount | Should -Be 1
            }
        }
    }

    Context "Break point on switch condition should be hit only when enumerating it" {
        BeforeAll {
            $script = @'
$test = 1..2
switch ($test)
{
    default {}
}
'@
            $path = Setup -PassThru -File SwitchScript.ps1 -Content $script
            $breakpoint = Set-PSBreakpoint -Script $path -Line 2 -Action { continue }
        }

        AfterAll {
            Remove-PSBreakpoint -Breakpoint $breakpoint
        }

        It "switch condition should be hit 3 times" {
            ## MoveNext() will be called on the condition for 3 times
            $null = & $path
            $breakpoint.HitCount | Should -Be 3
        }
    }

    Context "Break point on for-statement initializer should be hit" {
        BeforeAll {
            $for_script_1 = @'
$test = 2
for ("string".Length;
     $test -gt 0; $test--) { }
'@

            $for_script_2 = @'
$test = $PSCommandPath
for (Test-Path $test;
     $test -eq "blah";) { }
'@

            $for_script_3 = @'
for ($test = 2;
     $test -gt 0; $test--) { }
'@

            $for_script_4 = @'
$test = 2
for (;$test -gt 0;
     $test--) { }
'@

            $for_script_5 = @'
$test = $PSCommandPath
for (;Test-Path $test;)
{
    $test = "blah"
}
'@
            $ForScript_1 = Setup -PassThru -File ForScript_1.ps1 -Content $for_script_1
            $bp_1 = Set-PSBreakpoint -Script $ForScript_1 -Line 2 -Action { continue }

            $ForScript_2 = Setup -PassThru -File ForScript_2.ps1 -Content $for_script_2
            $bp_2 = Set-PSBreakpoint -Script $ForScript_2 -Line 2 -Action { continue }

            $ForScript_3 = Setup -PassThru -File ForScript_3.ps1 -Content $for_script_3
            $bp_3 = Set-PSBreakpoint -Script $ForScript_3 -Line 1 -Action { continue }

            $ForScript_4 = Setup -PassThru -File ForScript_4.ps1 -Content $for_script_4
            $bp_4 = Set-PSBreakpoint -Script $ForScript_4 -Line 2 -Action { continue }

            $ForScript_5 = Setup -PassThru -File ForScript_5.ps1 -Content $for_script_5
            $bp_5 = Set-PSBreakpoint -Script $ForScript_5 -Line 2 -Action { continue }

            $testCases = @(
                @{ Name = "expression initializer should be hit once";    Path = $ForScript_1; Breakpoint = $bp_1; HitCount = 1 }
                @{ Name = "pipeline initializer should be hit once";      Path = $ForScript_2; Breakpoint = $bp_2; HitCount = 1 }
                @{ Name = "assignment initializer should be hit 3 times"; Path = $ForScript_3; Breakpoint = $bp_3; HitCount = 1 }
                @{ Name = "pipeline condition should be hit 3 times";     Path = $ForScript_4; Breakpoint = $bp_4; HitCount = 3 }
                @{ Name = "pipeline condition should be hit 2 times";     Path = $ForScript_5; Breakpoint = $bp_5; HitCount = 2 }
            )
        }

        AfterAll {
            Get-PSBreakpoint -Script $ForScript_1, $ForScript_2, $ForScript_3, $ForScript_4, $ForScript_5 | Remove-PSBreakpoint
        }

        It "for-statement <Name>" -TestCases $testCases {
            param($Path, $Breakpoint, $HitCount)
            $null = & $Path
            $Breakpoint.HitCount | Should -Be $HitCount
        }
    }

    Context "Break point on while loop condition should be hit" {
        BeforeAll {
            $while_script_1 = @'
$test = "string"
while ($test.Contains("str"))
{
    $test = "blah"
}
'@

            $while_script_2 = @'
$test = $PSCommandPath
while (Test-Path $test)
{
    $test = "blah"
}
'@
            $WhileScript_1 = Setup -PassThru -File WhileScript_1.ps1 -Content $while_script_1
            $bp_1 = Set-PSBreakpoint -Script $WhileScript_1 -Line 2 -Action { continue }

            $WhileScript_2 = Setup -PassThru -File WhileScript_2.ps1 -Content $while_script_2
            $bp_2 = Set-PSBreakpoint -Script $WhileScript_2 -Line 2 -Action { continue }

            $testCases = @(
                @{ Name = "expression condition should be hit 2 times"; Path = $WhileScript_1; Breakpoint = $bp_1; HitCount = 2 }
                @{ Name = "pipeline condition should be hit 2 times";   Path = $WhileScript_2; Breakpoint = $bp_2; HitCount = 2 }
            )
        }

        AfterAll {
            Get-PSBreakpoint -Script $WhileScript_1, $WhileScript_2 | Remove-PSBreakpoint
        }

        It "while loop <Name>" -TestCases $testCases {
            param($Path, $Breakpoint, $HitCount)
            $null = & $Path
            $Breakpoint.HitCount | Should -Be $HitCount
        }
    }

    Context "Break point on do-while loop condition should be hit" {
        BeforeAll {
            $do_while_script_1 = @'
$test = "blah"
do { echo $test }
while ($test.Contains("str"))
'@

            $do_while_script_2 = @'
$test = "blah"
do { echo $test }
while (Test-Path $test)
'@
            $DoWhileScript_1 = Setup -PassThru -File DoWhileScript_1.ps1 -Content $do_while_script_1
            $bp_1 = Set-PSBreakpoint -Script $DoWhileScript_1 -Line 2 -Action { continue }

            $DoWhileScript_2 = Setup -PassThru -File DoWhileScript_2.ps1 -Content $do_while_script_2
            $bp_2 = Set-PSBreakpoint -Script $DoWhileScript_2 -Line 2 -Action { continue }

            $testCases = @(
                @{ Name = "expression condition should be hit 2 times"; Path = $DoWhileScript_1; Breakpoint = $bp_1; HitCount = 1 }
                @{ Name = "pipeline condition should be hit 2 times";   Path = $DoWhileScript_2; Breakpoint = $bp_2; HitCount = 1 }
            )
        }

        AfterAll {
            Get-PSBreakpoint -Script $DoWhileScript_1, $DoWhileScript_2 | Remove-PSBreakpoint
        }

        It "Do-While loop <Name>" -TestCases $testCases {
            param($Path, $Breakpoint, $HitCount)
            $null = & $Path
            $Breakpoint.HitCount | Should -Be $HitCount
        }
    }

    Context "Break point on do-until loop condition should be hit" {
        BeforeAll {
            $do_until_script_1 = @'
$test = "blah"
do { echo $test }
until ($test.Contains("bl"))
'@

            $do_until_script_2 = @'
$test = $PSCommandPath
do { echo $test }
until (Test-Path $test)
'@
            $DoUntilScript_1 = Setup -PassThru -File DoUntilScript_1.ps1 -Content $do_until_script_1
            $bp_1 = Set-PSBreakpoint -Script $DoUntilScript_1 -Line 2 -Action { continue }

            $DoUntilScript_2 = Setup -PassThru -File DoUntilScript_2.ps1 -Content $do_until_script_2
            $bp_2 = Set-PSBreakpoint -Script $DoUntilScript_2 -Line 2 -Action { continue }

            $testCases = @(
                @{ Name = "expression condition should be hit 2 times"; Path = $DoUntilScript_1; Breakpoint = $bp_1; HitCount = 1 }
                @{ Name = "pipeline condition should be hit 2 times";   Path = $DoUntilScript_2; Breakpoint = $bp_2; HitCount = 1 }
            )
        }

        AfterAll {
            Get-PSBreakpoint -Script $DoUntilScript_1, $DoUntilScript_2 | Remove-PSBreakpoint
        }

        It "Do-Until loop <Name>" -TestCases $testCases {
            param($Path, $Breakpoint, $HitCount)
            $null = & $Path
            $Breakpoint.HitCount | Should -Be $HitCount
        }
    }

    Context "Break point on if condition should be hit" {
        BeforeAll {
            $if_script_1 = @'
if ("string".Contains('str'))
{ }
'@
            $if_script_2 = @'
if (Test-Path $PSCommandPath)
{ }
'@
            $if_script_3 = @'
if ($false) {}
elseif ("string".Contains('str'))
{ }
'@
            $if_script_4 = @'
if ($false) {}
elseif (Test-Path $PSCommandPath)
{ }
'@
            $IfScript_1 = Setup -PassThru -File IfScript_1.ps1 -Content $if_script_1
            $bp_1 = Set-PSBreakpoint -Script $IfScript_1 -Line 1 -Action { continue }

            $IfScript_2 = Setup -PassThru -File IfScript_2.ps1 -Content $if_script_2
            $bp_2 = Set-PSBreakpoint -Script $IfScript_2 -Line 1 -Action { continue }

            $IfScript_3 = Setup -PassThru -File IfScript_3.ps1 -Content $if_script_3
            $bp_3 = Set-PSBreakpoint -Script $IfScript_3 -Line 2 -Action { continue }

            $IfScript_4 = Setup -PassThru -File IfScript_4.ps1 -Content $if_script_4
            $bp_4 = Set-PSBreakpoint -Script $IfScript_4 -Line 2 -Action { continue }

            $testCases = @(
                @{ Name = "expression if-condition should be hit once";     Path = $IfScript_1; Breakpoint = $bp_1; HitCount = 1 }
                @{ Name = "pipeline if-condition should be hit once";       Path = $IfScript_2; Breakpoint = $bp_2; HitCount = 1 }
                @{ Name = "expression elseif-condition should be hit once"; Path = $IfScript_3; Breakpoint = $bp_3; HitCount = 1 }
                @{ Name = "pipeline elseif-condition should be hit once";   Path = $IfScript_4; Breakpoint = $bp_4; HitCount = 1 }
            )
        }

        AfterAll {
            Get-PSBreakpoint -Script $IfScript_1, $IfScript_2, $IfScript_3, $IfScript_4 | Remove-PSBreakpoint
        }

        It "If statement <Name>" -TestCases $testCases {
            param($Path, $Breakpoint, $HitCount)
            $null = & $Path
            $Breakpoint.HitCount | Should -Be $HitCount
        }
    }

    Context "Break point on return should hit" {
        BeforeAll {
            $return_script_1 = @'
return
'@
            $return_script_2 = @'
return 10
'@
            $return_script_3 = @'
trap {
    'statement to ignore trap registration sequence point'
    return
}

throw
'@

            $ReturnScript_1 = Setup -PassThru -File ReturnScript_1.ps1 -Content $return_script_1
            $bp_1 = Set-PSBreakpoint -Script $ReturnScript_1 -Line 1 -Action { continue }

            $ReturnScript_2 = Setup -PassThru -File ReturnScript_2.ps1 -Content $return_script_2
            $bp_2 = Set-PSBreakpoint -Script $ReturnScript_2 -Line 1 -Action { continue }

            $ReturnScript_3 = Setup -PassThru -File ReturnScript_3.ps1 -Content $return_script_3
            $bp_3 = Set-PSBreakpoint -Script $ReturnScript_3 -Line 3 -Action { continue }

            $testCases = @(
                @{ Name = "return without pipeline should be hit once"; Path = $ReturnScript_1; Breakpoint = $bp_1; HitCount = 1 }
                @{ Name = "return with pipeline should be hit once";    Path = $ReturnScript_2; Breakpoint = $bp_2; HitCount = 1 }
                @{ Name = "return from trap should be hit once";        Path = $ReturnScript_3; Breakpoint = $bp_3; HitCount = 1 }
            )
        }

        AfterAll {
            Get-PSBreakpoint -Script $ReturnScript_1, $ReturnScript_2, $ReturnScript_3 | Remove-PSBreakpoint
        }

        It "Return statement <Name>" -TestCases $testCases {
            param($Path, $Breakpoint, $HitCount)
            $null = & $Path
            $Breakpoint.HitCount | Should -Be $HitCount
        }
    }
}

Describe "It should be possible to reset runspace debugging" -Tag "Feature" {
    BeforeAll {
        $script = @'
"line 1"
"line 2"
"line 3"
'@
        $scriptPath = Setup -PassThru -File TestScript.ps1 -Content $script
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
        $completed = Wait-UntilTrue { $rs.Debugger.InBreakPoint -eq $true } -timeout 10000 -interval 200
        $ps.Stop()
        $rs.ResetRunspaceState()
    }
    AfterAll {
        if ( $null -ne $ps ) { $ps.Dispose() }
        if ( $null -ne $ss ) { $rs.Dispose() }
    }
    It "2 breakpoints should have been set" {
        $breakpoints.Count | Should -Be 2
    }
    It "The breakpoint Should have been hit" {
        $completed | Should -BeTrue
    }
    It "The reset debugger should not be in a breakpoint" {
        $rs.Debugger.InBreakPoint | Should -BeFalse
    }
    It "The reset debugger should not be active" {
        $rs.Debugger.IsActive | Should -BeFalse
    }
    It "The reset debugger mode should be set to 'Default'" {
        $rs.Debugger.DebugMode | Should -Be "Default"
    }
    It "The debugger should be the same before and after the reset" {
        $rs.Debugger | Should -Be $debuggerBeforeReset
    }
    It "The breakpoints should be gone after reset" {
        $ps.Commands.clear()
        $ps.AddCommand("Get-PSBreakpoint").Invoke() | Should -BeNullOrEmpty
    }
    It "The script should run without a break" {
        $ps.Commands.Clear()
        $ps.addscript($scriptPath).Invoke().Count | Should -Be 3
    }
}
