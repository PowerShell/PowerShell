# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
using namespace System.Diagnostics

# Minishell (Singleshell) is a powershell concept.
# Its primary use-case is when somebody executes a scriptblock in the new powershell process.
# The objects are automatically marshelled to the child process and
# back to the parent session, so users can avoid custom
# serialization to pass objects between two processes.

Describe 'minishell for native executables' -Tag 'CI' {

    BeforeAll {
        $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"
    }

    Context 'Streams from minishell' {

        It 'gets a hashtable object from minishell' {
            $output = & $powershell -noprofile { @{'a' = 'b'} }
            ($output | Measure-Object).Count | Should -Be 1
            $output | Should -BeOfType Hashtable
            $output['a'] | Should -Be 'b'
        }

        It 'gets the error stream from minishell' {
            $PSNativeCommandUseErrorActionPreference = $false
            $output = & $powershell -noprofile { Write-Error 'foo' } 2>&1
            ($output | Measure-Object).Count | Should -Be 1
            $output | Should -BeOfType System.Management.Automation.ErrorRecord
            $output.FullyQualifiedErrorId | Should -Be 'Microsoft.PowerShell.Commands.WriteErrorException'
        }

        It 'gets the information stream from minishell' {
            $output = & $powershell -noprofile { Write-Information 'foo' } 6>&1
            ($output | Measure-Object).Count | Should -Be 1
            $output | Should -BeOfType System.Management.Automation.InformationRecord
            $output | Should -Be 'foo'
        }
    }

    Context 'Streams to minishell' {
        It "passes input into minishell" {
            $a = 1,2,3
            $val  = $a | & $powershell -noprofile -command { $input }
            $val.Count | Should -Be 3
            $val[0] | Should -Be 1
            $val[1] | Should -Be 2
            $val[2] | Should -Be 3
        }
    }
}

Describe "ConsoleHost unit tests" -tags "Feature" {

    BeforeAll {
        $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"
        $ExitCodeBadCommandLineParameter = 64

        function NewProcessStartInfo([string]$CommandLine, [switch]$RedirectStdIn)
        {
            return [ProcessStartInfo]@{
                FileName               = $powershell
                Arguments              = $CommandLine
                RedirectStandardInput  = $RedirectStdIn
                RedirectStandardOutput = $true
                RedirectStandardError  = $true
                UseShellExecute        = $false
            }
        }

        function RunPowerShell([ProcessStartInfo]$si)
        {
            $process = [Process]::Start($si)

            return $process
        }

        function EnsureChildHasExited([Process]$process, [int]$WaitTimeInMS = 15000)
        {
            $process.WaitForExit($WaitTimeInMS)

            if (!$process.HasExited)
            {
                $process.HasExited | Should -BeTrue
                $process.Kill()
            }
        }
    }

    AfterEach {
        $error.Clear()
    }

    It "Clear-Host does not injects data into PowerShell output stream" {
        if (Test-IsWindowsArm64) {
            Set-ItResult -Pending -Because "ARM64 runs in non-interactively mode and Clear-Host does not work."
        }

        & { Clear-Host; 'hi' } | Should -BeExactly 'hi'
    }

    Context "ShellInterop" {
        It "Verify Parsing Error Output Format Single Shell should throw exception" {
            { & $powershell -outp blah -comm { $input } } | Should -Throw -ErrorId "IncorrectValueForFormatParameter"
        }

        It "Verify Validate Output Format As Text Explicitly Child Single Shell does not throw" {
            {
                "blahblah" | & $powershell -noprofile -out text -com { $input }
            } | Should -Not -Throw
        }

        It "Verify Parsing Error Input Format Single Shell should throw exception" {
            { & $powershell -input blah -comm { $input } } | Should -Throw -ErrorId "IncorrectValueForFormatParameter"
        }
    }

    Context "CommandLine" {
        It "simple -args" {
            & $powershell -noprofile { $args[0] } -args "hello world" | Should -Be "hello world"
        }

        It "array -args" {
            & $powershell -noprofile { $args[0] } -args 1,(2,3) | Should -Be 1
            (& $powershell -noprofile { $args[1] } -args 1,(2,3))[1]  | Should -Be 3
        }
        foreach ($x in "--help", "-help", "-h", "-?", "--he", "-hel", "--HELP", "-hEl") {
            It "Accepts '$x' as a parameter for help" {
                & $powershell -noprofile $x | Where-Object { $_ -match "pwsh[.exe] -Help | -? | /?" } | Should -Not -BeNullOrEmpty
            }
        }

        It "Should accept a Base64 encoded command" {
            $commandString = "Get-Location"
            $encodedCommand = [System.Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($commandString))
            # We don't compare to `Get-Location` directly because object and formatted output comparisons are difficult
            $expected = & $powershell -noprofile -command $commandString
            $actual = & $powershell -noprofile -EncodedCommand $encodedCommand
            $actual | Should -Be $expected
        }

        It "-Version should return the engine version using: -version <value>" -TestCases @(
            @{value = ""},
            @{value = "2"},
            @{value = "-command 1-1"}
        ) {
            $currentVersion = "PowerShell " + $PSVersionTable.GitCommitId.ToString()
            $observed = & $powershell -version $value 2>&1
            $observed | Should -Be $currentVersion
            $LASTEXITCODE | Should -Be 0
        }

        It "-File should be default parameter" {
            Set-Content -Path $testdrive/test.ps1 -Value "'hello'"
            $observed = & $powershell -NoProfile $testdrive/test.ps1
            $observed | Should -Be "hello"
        }

        It "-File accepts scripts with .ps1 extension" {
            $Filename = 'test.ps1'
            Set-Content -Path $testdrive/$Filename -Value "'hello'"
            $observed = & $powershell -NoProfile -File $testdrive/$Filename
            $observed | Should -Be "hello"
        }

        It "-File accepts scripts without .ps1 extension to support shebang" -Skip:($IsWindows) {
            $Filename = 'test.xxx'
            Set-Content -Path $testdrive/$Filename -Value "'hello'"
            $observed = & $powershell -NoProfile -File $testdrive/$Filename
            $observed | Should -Be "hello"
        }

        It "-File should fail for script without .ps1 extension" -Skip:(!$IsWindows) {
            $Filename = 'test.xxx'
            Set-Content -Path $testdrive/$Filename -Value "'hello'"
            & $powershell -NoProfile -File $testdrive/$Filename 2>&1 $null
            $LASTEXITCODE | Should -Be 64
        }

        It "-File should pass additional arguments to script" {
            Set-Content -Path $testdrive/script.ps1 -Value 'foreach($arg in $args){$arg}'
            $observed = & $powershell -NoProfile $testdrive/script.ps1 foo bar
            $observed.Count | Should -Be 2
            $observed[0] | Should -Be "foo"
            $observed[1] | Should -Be "bar"
        }

        It "-File should be able to pass bool string values as string to parameters: <BoolString>" -TestCases @(
            # validates case is preserved
            @{BoolString = '$truE'},
            @{BoolString = '$falSe'},
            @{BoolString = 'trUe'},
            @{BoolString = 'faLse'}
        ) {
            param([string]$BoolString)
            Set-Content -Path $testdrive/test.ps1 -Value 'param([string]$bool) $bool'
            $observed = & $powershell -NoProfile -Nologo -File $testdrive/test.ps1 -Bool $BoolString
            $observed | Should -Be $BoolString
        }

        It "-File should be able to pass bool string values as string to positional parameters: <BoolString>" -TestCases @(
            # validates case is preserved
            @{BoolString = '$tRue'},
            @{BoolString = '$falSe'},
            @{BoolString = 'tRUe'},
            @{BoolString = 'fALse'}
        ) {
            param([string]$BoolString)
            Set-Content -Path $testdrive/test.ps1 -Value 'param([string]$bool) $bool'
            $observed = & $powershell -NoProfile -Nologo -File $testdrive/test.ps1 $BoolString
            $observed | Should -BeExactly $BoolString
        }

        It "-File should be able to pass bool string values as bool to switches: <BoolString>" -TestCases @(
            @{BoolString = '$tRue'; BoolValue = 'True'},
            @{BoolString = '$faLse'; BoolValue = 'False'},
            @{BoolString = 'tRue'; BoolValue = 'True'},
            @{BoolString = 'fAlse'; BoolValue = 'False'}
        ) {
            param([string]$BoolString, [string]$BoolValue)
            Set-Content -Path $testdrive/test.ps1 -Value 'param([switch]$switch) $switch.IsPresent'
            $observed = & $powershell -NoProfile -Nologo -File $testdrive/test.ps1 -switch:$BoolString
            $observed | Should -Be $BoolValue
        }

        It "-File should return exit code from script" {
            $Filename = 'test.ps1'
            Set-Content -Path $testdrive/$Filename -Value 'exit 123'
            & $powershell $testdrive/$Filename
            $LASTEXITCODE | Should -Be 123
        }

        It "A single dash should be passed as an arg" {
            $testScript = @'
    [CmdletBinding()]param(
        [string]$p1,
        [string]$p2,
        [Parameter(ValueFromPipeline)][string]$InputObject
    )
    process{
        $input.replace($p1, $p2)
    }
'@
            $testFilePath = Join-Path $TestDrive "test.ps1"
            Set-Content -Path $testFilePath -Value $testScript
            $observed = echo hello | & $powershell -noprofile $testFilePath e -
            $observed | Should -BeExactly "h-llo"
        }

        It "Missing command should fail" {
            & $powershell -noprofile -c
            $LASTEXITCODE | Should -Be 64
        }

        It "Empty space command should succeed on non-Windows" -skip:$IsWindows {
            & $powershell -noprofile -c '' | Should -BeNullOrEmpty
            $LASTEXITCODE | Should -Be 0
        }

        It "Whitespace command should succeed" {
            & $powershell -noprofile -c ' ' | Should -BeNullOrEmpty
            $LASTEXITCODE | Should -Be 0
        }
    }

    Context "-Login pwsh switch" {
        BeforeAll {
            $profilePath = "~/.profile"
            $backupProfilePath = "profile.bak"
            if (Test-Path $profilePath) {
                Move-Item -Path $profilePath -Destination $backupProfilePath -Force
            }

            $envVarName = 'PSTEST_PROFILE_LOAD'

            $guid = New-Guid

            Set-Content -Force -Path $profilePath -Value @"
export $envVarName='$guid'
"@
        }

        AfterAll {
            if (Test-Path $backupProfilePath) {
                Move-Item -Path $backupProfilePath -Destination $profilePath -Force
            }
        }

        It "Doesn't run the login profile when -Login not used" {
            $result = & $powershell -noprofile -Command "`$env:$envVarName"
            $result | Should -BeNullOrEmpty
            $LASTEXITCODE | Should -Be 0
        }

        It "Doesn't falsely recognise -Login when elsewhere in the invocation" {
            $result = & $powershell -nop -c 'Write-Output "-login"'
            $result | Should -BeExactly '-login'
            $LASTEXITCODE | Should -Be 0
        }

        It "Doesn't falsely recognise -Login when used after -Command" {
            $result = & $powershell -nop -c 'Write-Output' -Login
            $result | Should -BeExactly '-Login'
            $LASTEXITCODE | Should -Be 0
        }

        It "Accepts the <LoginSwitch> switch for -Login and behaves correctly" -TestCases @(
            @{ LoginSwitch = '-l' }
            @{ LoginSwitch = '-L' }
            @{ LoginSwitch = '-login' }
            @{ LoginSwitch = '-Login' }
            @{ LoginSwitch = '-LOGIN' }
            @{ LoginSwitch = '-log' }
        ) {
            param($LoginSwitch)

            $result = & $powershell $LoginSwitch -NoProfile -Command "`$env:$envVarName"

            if ($IsWindows) {
                $result | Should -BeNullOrEmpty
                $LASTEXITCODE | Should -Be 0
                return
            }

            $result | Should -BeExactly $guid
            $LASTEXITCODE | Should -Be 0
        }

        It "Starts as a login shell with '-' prepended to name" -Skip:(-not (Get-Command -Name /bin/bash -ErrorAction Ignore)) {
            $quoteEscapedPwsh = $powershell.Replace("'", "\'")
            $pwshCommand = "`$env:$envVarName"
            $bashCommand = "exec -a '-pwsh' '$quoteEscapedPwsh' -NoProfile -Command '`$env:$envVarName' ''"
            $result = /bin/bash -c $bashCommand
            $result | Should -BeExactly $guid
            $LASTEXITCODE | Should -Be 0 # Exit code will be PowerShell's since it was exec'd
        }
    }

    Context "-SettingsFile Commandline switch" {

        BeforeAll {
            if ($IsWindows) {
                $CustomSettingsFile = Join-Path -Path $TestDrive -ChildPath 'Powershell.test.json'
                $DefaultExecutionPolicy = 'RemoteSigned'
            }
        }
        BeforeEach {
            if ($IsWindows) {
                # reset the content of the settings file to a known state.
                Set-Content -Path $CustomSettingsfile -Value "{`"Microsoft.PowerShell:ExecutionPolicy`":`"$DefaultExecutionPolicy`"}" -ErrorAction Stop
            }
        }

        # NOTE: The -settingsFile command-line option only reads settings for the local machine. As a result, the tests that use Set/Get-ExecutionPolicy
        # must use an explicit scope of LocalMachine to ensure the setting is written to the expected file.
        # Skip the tests on Unix platforms because *-ExecutionPolicy cmdlets don't work by design.

        It "Verifies PowerShell reads from the custom -settingsFile" -Skip:(!$IsWindows) {
            $actualValue = & $powershell -NoProfile -SettingsFile $CustomSettingsFile -Command {(Get-ExecutionPolicy -Scope LocalMachine).ToString()}
            $actualValue  | Should -Be $DefaultExecutionPolicy
        }

        It "Verifies PowerShell writes to the custom -settingsFile" -Skip:(!$IsWindows) {
            $expectedValue = 'AllSigned'

            # Update the execution policy; this should update the settings file.
            & $powershell -NoProfile -SettingsFile $CustomSettingsFile -Command {Set-ExecutionPolicy -ExecutionPolicy AllSigned -Scope LocalMachine }

            # ensure the setting was written to the settings file.
            $content = (Get-Content -Path $CustomSettingsFile | ConvertFrom-Json)
            $content.'Microsoft.PowerShell:ExecutionPolicy' | Should -Be $expectedValue

            # ensure the setting is applied on next run
            $actualValue = & $powershell -NoProfile -SettingsFile $CustomSettingsFile -Command {(Get-ExecutionPolicy -Scope LocalMachine).ToString()}
            $actualValue  | Should -Be $expectedValue
        }

        It "Verify PowerShell removes a setting from the custom -settingsFile" -Skip:(!$IsWindows) {
            # Remove the LocalMachine execution policy; this should update the settings file.
            & $powershell -NoProfile -SettingsFile $CustomSettingsFile -Command {Set-ExecutionPolicy -ExecutionPolicy Undefined -Scope LocalMachine }

            # ensure the setting was removed from the settings file.
            $content = (Get-Content -Path $CustomSettingsFile | ConvertFrom-Json)
            $content.'Microsoft.PowerShell:ExecutionPolicy' | Should -Be $null
        }
    }

    Context "Pipe to/from powershell" {
        BeforeAll {
            if ($null -ne $PSStyle) {
                $outputRendering = $PSStyle.OutputRendering
                $PSStyle.OutputRendering = 'plaintext'
            }

            $p = [PSCustomObject]@{X=10;Y=20}
        }

        AfterAll {
            if ($null -ne $PSStyle) {
                $PSStyle.OutputRendering = $outputRendering
            }
        }

        It "xml input" {
            $p | & $powershell -noprofile { $input | ForEach-Object {$a = 0} { $a += $_.X + $_.Y } { $a } } | Should -Be 30
            $p | & $powershell -noprofile -inputFormat xml { $input | ForEach-Object {$a = 0} { $a += $_.X + $_.Y } { $a } } | Should -Be 30
        }

        It "text input" {
            # Join (multiple lines) and remove whitespace (we don't care about spacing) to verify we converted to string (by generating a table)
            $p | & $powershell -noprofile -inputFormat text { -join ($input -replace "\s","") } | Should -Be "XY--1020"
        }

        It "xml output" {
            & $powershell -noprofile { [PSCustomObject]@{X=10;Y=20} } | ForEach-Object {$a = 0} { $a += $_.X + $_.Y } { $a } | Should -Be 30
            & $powershell -noprofile -outputFormat xml { [PSCustomObject]@{X=10;Y=20} } | ForEach-Object {$a = 0} { $a += $_.X + $_.Y } { $a } | Should -Be 30
        }

        It "text output" {
            # Join (multiple lines) and remove whitespace (we don't care about spacing) to verify we converted to string (by generating a table)
            -join (& $powershell -noprofile -outputFormat text { $PSStyle.OutputRendering = 'PlainText'; [PSCustomObject]@{X=10;Y=20} }) -replace "\s","" | Should -Be "XY--1020"
        }

        It "errors are in text if error is redirected, encoded command, non-interactive, and outputformat specified" {
            $p = [Diagnostics.Process]::new()
            $p.StartInfo.FileName = "pwsh"
            $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes('throw "boom"'))
            $p.StartInfo.Arguments = "-EncodedCommand $encoded -ExecutionPolicy Bypass -NoLogo -NonInteractive -NoProfile -OutputFormat text"
            $p.StartInfo.UseShellExecute = $false
            $p.StartInfo.RedirectStandardError = $true
            $p.Start() | Out-Null
            $out = $p.StandardError.ReadToEnd()
            $out | Should -Not -BeNullOrEmpty
            $out = $out.Split([Environment]::NewLine)[0]
            [System.Management.Automation.Internal.StringDecorated]::new($out).ToString("PlainText") | Should -BeExactly "Exception: boom"
        }
    }

    Context "Redirected standard output" {
        It "Simple redirected output" {
            $si = NewProcessStartInfo "-noprofile -c 1+1"
            $process = RunPowerShell $si
            $process.StandardOutput.ReadToEnd() | Should -Be 2
            EnsureChildHasExited $process
        }
    }

    Context "Input redirected but not reading from stdin (not really interactive)" {
        # Tests under this context are testing that we do not read from StandardInput
        # even though it is redirected - we want to make sure we don't stop responding.
        # So none of these tests should close StandardInput

        It "Redirected input w/ implicit -Command w/ -NonInteractive" {
            $si = NewProcessStartInfo "-NonInteractive -noprofile -c 1+1" -RedirectStdIn
            $process = RunPowerShell $si
            $process.StandardOutput.ReadToEnd() | Should -Be 2
            EnsureChildHasExited $process
        }

        It "Redirected input w/ implicit -Command w/o -NonInteractive" {
            $si = NewProcessStartInfo "-noprofile -c 1+1" -RedirectStdIn
            $process = RunPowerShell $si
            $process.StandardOutput.ReadToEnd() | Should -Be 2
            EnsureChildHasExited $process
        }

        It "Redirected input w/ explicit -Command w/ -NonInteractive" {
            $si = NewProcessStartInfo "-NonInteractive -noprofile -Command 1+1" -RedirectStdIn
            $process = RunPowerShell $si
            $process.StandardOutput.ReadToEnd() | Should -Be 2
            EnsureChildHasExited $process
        }

        It "Redirected input w/ explicit -Command w/o -NonInteractive" {
            $si = NewProcessStartInfo "-noprofile -Command 1+1" -RedirectStdIn
            $process = RunPowerShell $si
            $process.StandardOutput.ReadToEnd() | Should -Be 2
            EnsureChildHasExited $process
        }

        It "Redirected input w/ -File w/ -NonInteractive" {
            '1+1' | Out-File -Encoding Ascii -FilePath TestDrive:test.ps1 -Force
            $si = NewProcessStartInfo "-noprofile -NonInteractive -File $testDrive\test.ps1" -RedirectStdIn
            $process = RunPowerShell $si
            $process.StandardOutput.ReadToEnd() | Should -Be 2
            EnsureChildHasExited $process
        }

        It "Redirected input w/ -File w/o -NonInteractive" {
            '1+1' | Out-File -Encoding Ascii -FilePath TestDrive:test.ps1 -Force
            $si = NewProcessStartInfo "-noprofile -File $testDrive\test.ps1" -RedirectStdIn
            $process = RunPowerShell $si
            $process.StandardOutput.ReadToEnd() | Should -Be 2
            EnsureChildHasExited $process
        }
    }

    Context "Redirected standard input for 'interactive' use" {
        BeforeAll {
            $nl = [Environment]::Newline
            $oldColor = $env:NO_COLOR
            $env:NO_COLOR = 1
        }

        AfterAll {
            $env:NO_COLOR = $oldColor
        }

        # All of the following tests replace the prompt (either via an initial command or interactively)
        # so that we can read StandardOutput and reliably know exactly what the prompt is.

        It "Interactive redirected input: <InteractiveSwitch>" -Pending:($IsWindows) -TestCases @(
            @{InteractiveSwitch = ""}
            @{InteractiveSwitch = " -IntERactive"}
            @{InteractiveSwitch = " -i"}
        ) {
            param($interactiveSwitch)
            $si = NewProcessStartInfo "-noprofile -nologo$interactiveSwitch" -RedirectStdIn
            $process = RunPowerShell $si
            $process.StandardInput.Write("`$function:prompt = { 'PS> ' }`n")
            $null = $process.StandardOutput.ReadLine()
            $process.StandardInput.Write("1+1`n")
            $process.StandardOutput.ReadLine() | Should -Be "PS> 1+1"
            $process.StandardOutput.ReadLine() | Should -Be "2"
            $process.StandardInput.Write("1+2`n")
            $process.StandardOutput.ReadLine() | Should -Be "PS> 1+2"
            $process.StandardOutput.ReadLine() | Should -Be "3"

            # Backspace should work as expected
            $process.StandardInput.Write("1+2`b3`n")
            # A real console should render 2`b3 as just 3, but we're just capturing exactly what is written
            $process.StandardOutput.ReadLine() | Should -Be "PS> 1+2`b3"
            $process.StandardOutput.ReadLine() | Should -Be "4"
            $process.StandardInput.Close()
            $process.StandardOutput.ReadToEnd() | Should -Be "PS> "
            EnsureChildHasExited $process
        }

        It "Interactive redirected input w/ initial command" -Pending:($IsWindows) {
            $si = NewProcessStartInfo "-noprofile -noexit -c ""`$function:prompt = { 'PS> ' }""" -RedirectStdIn
            $process = RunPowerShell $si
            $process.StandardInput.Write("1+1`n")
            $process.StandardOutput.ReadLine() | Should -Be "PS> 1+1"
            $process.StandardOutput.ReadLine() | Should -Be "2"
            $process.StandardInput.Write("1+2`n")
            $process.StandardOutput.ReadLine() | Should -Be "PS> 1+2"
            $process.StandardOutput.ReadLine() | Should -Be "3"
            $process.StandardInput.Close()
            $process.StandardOutput.ReadToEnd() | Should -Be "PS> "
            EnsureChildHasExited $process
        }

        It "Redirected input explicit prompting (-File -)" -Pending:($IsWindows) {
            $si = NewProcessStartInfo "-noprofile -" -RedirectStdIn
            $process = RunPowerShell $si
            $process.StandardInput.Write("`$function:prompt = { 'PS> ' }`n")
            $null = $process.StandardOutput.ReadLine()
            $process.StandardInput.Write("1+1`n")
            $process.StandardOutput.ReadLine() | Should -Be "PS> 1+1"
            $process.StandardOutput.ReadLine() | Should -Be "2"
            $process.StandardInput.Close()
            $process.StandardOutput.ReadToEnd() | Should -Be "PS> "
            EnsureChildHasExited $process
        }

        It "Redirected input no prompting (-Command -)" -Pending:($IsWindows) {
            $si = NewProcessStartInfo "-noprofile -Command -" -RedirectStdIn
            $process = RunPowerShell $si
            $process.StandardInput.Write("1+1`n")
            $process.StandardOutput.ReadLine() | Should -Be "2"

            # Multi-line input
            $process.StandardInput.Write("if (1)`n{`n    42`n}`n`n")
            $process.StandardOutput.ReadLine() | Should -Be "42"
            $process.StandardInput.Write(@"
function foo
{
    'in foo'
}

foo

"@)
            $process.StandardOutput.ReadLine() | Should -Be "in foo"

            # Backspace sent through stdin should be in the final string
            $process.StandardInput.Write("`"a`bc`".Length`n")
            $process.StandardOutput.ReadLine() | Should -Be "3"

            # Last command with no newline - should be accepted and
            # produce output after closing stdin.
            $process.StandardInput.Write('22 + 22')
            $process.StandardInput.Close()
            $process.StandardOutput.ReadLine() | Should -Be "44"

            EnsureChildHasExited $process
        }

        It "Redirected input w/ nested prompt" -Pending:($IsWindows) {
            $si = NewProcessStartInfo "-noprofile -noexit -c ""`$function:prompt = { 'PS' + ('>'*(`$NestedPromptLevel+1)) + ' ' }""" -RedirectStdIn
            $process = RunPowerShell $si
            $process.StandardInput.Write("`$PSStyle.OutputRendering='plaintext'`n")
            $null = $process.StandardOutput.ReadLine()
            $process.StandardInput.Write("`$Host.EnterNestedPrompt()`n")
            $process.StandardOutput.ReadLine() | Should -Be "PS> `$Host.EnterNestedPrompt()"
            $process.StandardInput.Write("exit`n")
            $process.StandardOutput.ReadLine() | Should -Be "PS>> exit"
            $process.StandardInput.Close()
            $process.StandardOutput.ReadToEnd() | Should -Be "PS> "
            EnsureChildHasExited $process
        }
    }

    Context "Exception handling" {
        BeforeAll {
            # the default stack size in PowerShell is 10000000, set the stack
            # to something much smaller which will produce the error much faster
            # I saw a reduction from 65 seconds to 79 milliseconds.
            $classDefinition = @'
using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
namespace StackTest {
    public class StackDepthTest {
        public static PowerShell ps;
        public static int size = 512 * 1024;
        public static void CauseError() {
            Thread t = new Thread(RunPS, size);
            t.Start();
            t.Join();
        }
        public static void RunPS() {
            InitialSessionState iss = InitialSessionState.CreateDefault2();
            iss.ThreadOptions = PSThreadOptions.UseCurrentThread;
            ps = PowerShell.Create(iss);
            ps.AddScript("function recurse { recurse }; recurse").Invoke();
        }
        public static void GetPSError() {
            if ( ps.Streams.Error.Count > 0) {
                throw ps.Streams.Error[0].Exception.InnerException;
            }
        }
    }
}
'@
            $TestType = Add-Type -PassThru -TypeDefinition $classDefinition
        }

        It "Should handle a CallDepthOverflow" {
            $TestType::CauseError()
            { $TestType::GetPSError() } | Should -Throw -ErrorId "CallDepthOverflow"
        }
    }

    Context "Data, Config, and Cache locations" {
        BeforeEach {
            $XDG_CACHE_HOME = $env:XDG_CACHE_HOME
            $XDG_DATA_HOME = $env:XDG_DATA_HOME
            $XDG_CONFIG_HOME = $env:XDG_CONFIG_HOME
        }

        AfterEach {
            $env:XDG_CACHE_HOME = $XDG_CACHE_HOME
            $env:XDG_DATA_HOME = $XDG_DATA_HOME
            $env:XDG_CONFIG_HOME = $XDG_CONFIG_HOME
        }

        It "Should start if Data, Config, and Cache location is not accessible" -Skip:($IsWindows) {
            $env:XDG_CACHE_HOME = "/dev/cpu"
            $env:XDG_DATA_HOME = "/dev/cpu"
            $env:XDG_CONFIG_HOME = "/dev/cpu"
            $output = & $powershell -noprofile -Command { (Get-Command).count }
            [int]$output | Should -BeGreaterThan 0
        }
    }

    Context "HOME environment variable" {
        It "Should start if HOME is not defined" -Skip:($IsWindows) {
            bash -c "unset HOME;$powershell -c '1+1'" | Should -BeExactly 2
        }

        It "Same user should use the same temporary HOME directory for different sessions" -Skip:($IsWindows) {
            $results = bash -c @"
unset HOME;
$powershell -c '[System.Management.Automation.Platform]::SelectProductNameForDirectory([System.Management.Automation.Platform+XDG_Type]::DEFAULT)';
$powershell -c '[System.Management.Automation.Platform]::SelectProductNameForDirectory([System.Management.Automation.Platform+XDG_Type]::DEFAULT)';
"@
            $results | Should -HaveCount 2
            $results[0] | Should -BeExactly $results[1]

            $tempHomeName = "pwsh-{0}-98288ff9-5712-4a14-9a11-23693b9cd91a" -f [System.Environment]::UserName
            $defaultPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "$tempHomeName/.config/powershell"
            $results[0] | Should -BeExactly $defaultPath
        }
    }

    Context "PATH environment variable" {
        It "`$PSHOME should be in front so that pwsh.exe starts current running PowerShell" {
            & $powershell -v | Should -Match $PSVersionTable.GitCommitId
        }

        It "powershell starts if PATH is not set" -Skip:($IsWindows) {
            bash -c "unset PATH;$powershell -nop -c '1+1'" | Should -BeExactly 2
        }
    }

    Context "Ambiguous arguments" {
        It "Ambiguous argument '<testArg>' should return possible matches" -TestCases @(
            @{testArg="-no";expectedMatches=@("-nologo","-noexit","-noprofile","-noninteractive")},
            @{testArg="-format";expectedMatches=@("-inputformat","-outputformat")}
        ) {
            param($testArg, $expectedMatches)
            $output = & $powershell $testArg -File foo 2>&1
            $LASTEXITCODE | Should -Be $ExitCodeBadCommandLineParameter
            $outString = [String]::Join(",", $output)
            foreach ($expectedMatch in $expectedMatches)
            {
                $outString | Should -Match $expectedMatch
            }
        }
    }

    Context "-WorkingDirectory parameter" {
        BeforeAll {
            $folderName = (New-Guid).ToString() + " test";
            New-Item -Path ~/$folderName -ItemType Directory
            $ExitCodeBadCommandLineParameter = 64
        }

        AfterAll {
            Remove-Item ~/$folderName -Force -ErrorAction SilentlyContinue
        }

        It "Can set working directory to '<value>'" -TestCases @(
            @{ value = "~"            ; expectedPath = $((Get-Item ~).FullName) },
            @{ value = "~/$folderName"; expectedPath = $((Get-Item ~/$folderName).FullName) },
            @{ value = "~\$folderName"; expectedPath = $((Get-Item ~\$folderName).FullName) }
        ) {
            param($value, $expectedPath)
            $output = & $powershell -NoProfile -WorkingDirectory "$value" -Command '(Get-Location).Path'
            $output | Should -BeExactly $expectedPath
        }

        It "Can use '<parameter>' to set working directory" -TestCases @(
            @{ parameter = '-workingdirectory' },
            @{ parameter = '-wd' },
            @{ parameter = '-wo' }
        ) {
            param($parameter)
            $output = & $powershell -NoProfile $parameter ~ -Command "`$PWD.Path"
            $output | Should -BeExactly $((Get-Item ~).FullName)
        }

        It "Error case if -WorkingDirectory isn't given argument as last on command line" {
            $output = & $powershell -WorkingDirectory 2>&1
            $LASTEXITCODE | Should -Be $ExitCodeBadCommandLineParameter
            $output | Should -Not -BeNullOrEmpty
        }

        It "-WorkingDirectory should be processed before profiles" {

            if (Test-Path $PROFILE) {
                $currentProfile = Get-Content $PROFILE
            }
            else {
                New-Item -ItemType File -Path $PROFILE -Force
            }

            @"
                (Get-Location).Path
                Set-Location $testdrive
"@ > $PROFILE

            try {
                $out = & $powershell -workingdirectory ~ -c '(Get-Location).Path'
                $out | Should -HaveCount 2
                $out[0] | Should -BeExactly (Get-Item ~).FullName
                $out[1] | Should -BeExactly "$testdrive"
            }
            finally {
                if ($currentProfile) {
                    Set-Content $PROFILE -Value $currentProfile
                }
                else {
                    Remove-Item $PROFILE
                }
            }
        }
    }

    Context "CustomPipeName startup tests" {

        It "Should create pipe file if CustomPipeName is specified" {
            $pipeName = [System.IO.Path]::GetRandomFileName()
            $pipePath = Get-PipePath $pipeName

            # The pipePath should be created by the time the -Command is executed.
            & $powershell -CustomPipeName $pipeName -Command "Test-Path '$pipePath'" | Should -BeTrue
        }

        It "Should throw if CustomPipeName is too long on Linux or macOS" -Skip:($IsWindows) {
            # Generate a string that is larger than the max pipe name length.
            $longPipeName = [string]::new("A", 200)

            "`$PID" | & $powershell -CustomPipeName $longPipeName -c -
            $LASTEXITCODE | Should -Be $ExitCodeBadCommandLineParameter
        }
    }

    Context "ApartmentState WPF tests" -Tag Slow {

        It "WPF requires STA and will work" -Skip:(!$IsWindows -or [System.Management.Automation.Platform]::IsNanoServer) {
            Add-Type -AssemblyName presentationframework

            $xaml = [xml]@"
            <Window
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                x:Name="Window" Title="Initial Window" WindowStartupLocation = "CenterScreen"
                Width = "400" Height = "300" ShowInTaskbar = "True">
            </Window>
"@

            $reader = [System.Xml.XmlNodeReader]::new($xaml)
            $Window = [System.Windows.Markup.XamlReader]::Load($reader)
            # This will throw an exception if MTA
            { $Window.Show() } | Should -Not -Throw
            $Window.Close()
        }

    }

    Context "ApartmentState tests" {

        It "Default apartment state for main thread is STA" -Skip:(!$IsWindows -or [System.Management.Automation.Platform]::IsNanoServer) {
            [System.Threading.Thread]::CurrentThread.GetApartmentState() | Should -BeExactly "STA"
        }

        It "Default apartment state for new runspace is MTA" -Skip:(!$IsWindows) {
            $ps = [powershell]::Create()
            $ps.AddScript({[System.Threading.Thread]::CurrentThread.GetApartmentState()})
            $ps.Invoke() | Should -BeExactly "MTA"
        }

        It "Should be able to set apartment state to: <apartment>" -Skip:(!$IsWindows -or [System.Management.Automation.Platform]::IsNanoServer) -TestCases @(
            @{ apartment = "STA"; switch = "-sta" }
            @{ apartment = "MTA"; switch = "-mta" }
        ) {
            param ($apartment, $switch)

            & $powershell $switch -noprofile -command "[System.Threading.Thread]::CurrentThread.GetApartmentState()" | Should -BeExactly $apartment
        }

        It "Should fail to set apartment state to: <switch>" -Skip:($IsWindows -and ![System.Management.Automation.Platform]::IsNanoServer) -TestCases @(
            @{ switch = "-sta" }
            @{ switch = "-mta" }
        ) {
            param ($switch)

            & $powershell $switch -noprofile -command exit
            $LASTEXITCODE | Should -Be $ExitCodeBadCommandLineParameter
        }
    }

    Context "Startup banner text tests" -Tag Slow {
        BeforeAll {
            $outputPath = "Temp:\StartupBannerTest-Output-${Pid}.txt"
            $inputPath  = "Temp:\StartupBannerTest-Input.txt"
            "exit" > $inputPath

            # Not testing update notification banner text here
            $oldPowerShellUpdateCheck = $env:POWERSHELL_UPDATECHECK
            $env:POWERSHELL_UPDATECHECK = "Off"

            # Set TERM to "dumb" to avoid DECCKM codes in the output
            $oldTERM = $env:TERM
            $env:TERM = "dumb"

            $escPwd = [regex]::Escape($pwd)
            $expectedPromptPattern = "^PS ${escPwd}> exit`$"

            $spArgs = @{
                FilePath = $powershell
                ArgumentList = @("-NoProfile")
                RedirectStandardInput = $inputPath
                RedirectStandardOutput = $outputPath
                WorkingDirectory = $pwd
                PassThru = $true
                NoNewWindow = $true
                UseNewEnvironment = $false
            }
        }
        AfterAll {
            $env:TERM = $oldTERM
            $env:POWERSHELL_UPDATECHECK = $oldPowerShellUpdateCheck

            Remove-Item $inputPath -Force -ErrorAction Ignore
            Remove-Item $outputPath -Force -ErrorAction Ignore
        }
        BeforeEach {
            Remove-Item $outputPath -Force -ErrorAction Ignore
        }
        It "Displays expected startup banner text by default" {
            $process = Start-Process @spArgs
            Wait-UntilTrue -sb { $process.HasExited } -TimeoutInMilliseconds 5000 -IntervalInMilliseconds 250 | Should -BeTrue

            $out = @(Get-Content $outputPath)
            $out.Count | Should -Be 2
            $out[0] | Should -BeExactly "PowerShell $($PSVersionTable.GitCommitId)"
            $out[1] | Should -MatchExactly $expectedPromptPattern
        }
        It "Displays only the prompt with -NoLogo" {
            $spArgs["ArgumentList"] += "-NoLogo"
            $process = Start-Process @spArgs
            Wait-UntilTrue -sb { $process.HasExited } -TimeoutInMilliseconds 5000 -IntervalInMilliseconds 250 | Should -BeTrue

            $out = @(Get-Content $outputPath)
            $out.Count | Should -Be 1
            $out[0] | Should -MatchExactly $expectedPromptPattern
        }
    }

    Context 'CommandWithArgs tests' {
        It 'Should be able to run a pipeline with arguments using <param>' -TestCases @(
            @{ param = '-commandwithargs' }
            @{ param = '-cwa' }
        ){
            param($param)
            $out = pwsh -nologo -noprofile $param '$args | % { "[$_]" }'  '$fun' '@times'
            $out.Count | Should -Be 2 -Because ($out | Out-String)
            $out[0] | Should -BeExactly '[$fun]'
            $out[1] | Should -BeExactly '[@times]'
        }

        It 'Should be able to handle boolean switch: <param>' -TestCases @(
            @{ param = '-switch:$true'; expected = 'True'}
            @{ param = '-switch:$false'; expected = 'False'}
        ){
            param($param, $expected)
            $out = pwsh -nologo -noprofile -cwa 'param([switch]$switch) $switch.IsPresent' $param
            $out | Should -Be $expected
        }
    }
}

Describe "WindowStyle argument" -Tag Feature {
    BeforeAll {
        $defaultParamValues = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues["it:skip"] = !$IsWindows

        if ($IsWindows)
        {
            $ExitCodeBadCommandLineParameter = 64
            Add-Type -Name User32 -Namespace Test -MemberDefinition @"
public static WINDOWPLACEMENT GetPlacement(IntPtr hwnd)
{
    WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
    placement.length = Marshal.SizeOf(placement);
    GetWindowPlacement(hwnd, ref placement);
    return placement;
}

[DllImport("user32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
public static extern bool GetWindowPlacement(
    IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct WINDOWPLACEMENT
{
    public int length;
    public int flags;
    public ShowWindowCommands showCmd;
    public System.Drawing.Point ptMinPosition;
    public System.Drawing.Point ptMaxPosition;
    public System.Drawing.Rectangle rcNormalPosition;
}

public enum ShowWindowCommands : int
{
    Hidden = 0,
    Normal = 1,
    Minimized = 2,
    Maximized = 3,
}
"@
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $defaultParamValues
    }

    It "-WindowStyle <WindowStyle> should work on Windows" -Pending -TestCases @(
            @{WindowStyle="Normal"},
            @{WindowStyle="Minimized"},
            @{WindowStyle="Maximized"}  # hidden doesn't work in CI/Server Core
        ) {
        param ($WindowStyle)

        if (Test-IsWindowsArm64) {
            Set-ItResult -Pending -Because "All windows are showing up as hidden or ARM64"
        }

        try {
            $ps = Start-Process $powershell -ArgumentList "-WindowStyle $WindowStyle -noexit -interactive" -PassThru
            $startTime = Get-Date
            $showCmd = "Unknown"
            while (((Get-Date) - $startTime).TotalSeconds -lt 10 -and $showCmd -ne $WindowStyle) {
                Start-Sleep -Milliseconds 100
                $showCmd = ([Test.User32]::GetPlacement($ps.MainWindowHandle)).showCmd
            }

            $showCmd | Should -BeExactly $WindowStyle
        } finally {
            $ps | Stop-Process -Force
        }
    }

    It "Invalid -WindowStyle returns error" {
        & $powershell -WindowStyle invalid
        $LASTEXITCODE | Should -Be $ExitCodeBadCommandLineParameter
    }
}

Describe "Console host api tests" -Tag CI {
    Context "String escape and control sequences" {
        $esc = [char]0x1b
        $csi = [char]0x9b
        $testCases =
            @{InputObject = "abc"; Length = 3; Name = "No escapes"},
            @{InputObject = "${esc} [31mabc"; Length = 9; Name = "Malformed escape - extra space"},
            @{InputObject = "${esc}abc"; Length = 4; Name = "Malformed escape - no csi"},
            @{InputObject = "[31mabc"; Length = 7; Name = "Malformed escape - no escape"}

        $testCases += if ($Host.UI.SupportsVirtualTerminal)
        {
            @{InputObject = "$esc[31mabc"; Length = 3; Name = "Escape at start"}
            @{InputObject = "$esc[31mabc$esc[0m"; Length = 3; Name = "Escape at start and end"}
            @{InputObject = "${csi}31mabc"; Length = 3; Name = "C1 CSI at start"}
            @{InputObject = "${csi}31mabc${csi}0m"; Length = 3; Name = "C1 CSI at start and end"}
            @{InputObject = "abc${csi}m"; Length = 3; Name = "C1 CSI, no params"}
            @{InputObject = "abc${csi}#{"; Length = 3; Name = "C1 CSI, XTPUSHSGR"}
            @{InputObject = "abc${csi}#}"; Length = 3; Name = "C1 CSI, XTPOPSGR"}
            @{InputObject = "abc${csi}#p"; Length = 3; Name = "C1 CSI, XTPUSHSGR (alias)"}
            @{InputObject = "abc${csi}#q"; Length = 3; Name = "C1 CSI, XTPOPSGR (alias)"}
            @{InputObject = "abc${esc}[0#p"; Length = 3; Name = "XTPUSHSGR, with param"}
            @{InputObject = "${esc}[0;1#qabc"; Length = 3; Name = "XTPOPSGR, with multiple params"}
        }
        else
        {
            @{InputObject = "$esc[31mabc"; Length = 8; Name = "Escape at start - no virtual term support"}
            @{InputObject = "$esc[31mabc$esc[0m"; Length = 12; Name = "Escape at start and end - no virtual term support"}
            @{InputObject = "${csi}31mabc"; Length = 7; Name = "C1 CSI at start - no virtual term support"}
            @{InputObject = "${csi}31mabc${csi}0m"; Length = 10; Name = "C1 CSI at start and end - no virtual term support"}
            @{InputObject = "abc${csi}m"; Length = 5; Name = "C1 CSI, no params - no virtual term support"}
            @{InputObject = "abc${csi}#{"; Length = 6; Name = "C1 CSI, XTPUSHSGR - no virtual term support"}
            @{InputObject = "abc${csi}#}"; Length = 6; Name = "C1 CSI, XTPOPSGR - no virtual term support"}
            @{InputObject = "abc${csi}#p"; Length = 6; Name = "C1 CSI, XTPUSHSGR (alias) - no virtual term support"}
            @{InputObject = "abc${csi}#q"; Length = 6; Name = "C1 CSI, XTPOPSGR (alias) - no virtual term support"}
            @{InputObject = "abc${esc}[0#p"; Length = 8; Name = "XTPUSHSGR, with param - no virtual term support"}
            @{InputObject = "${esc}[0;1#qabc"; Length = 10; Name = "XTPOPSGR, with multiple params - no virtual term support"}
        }

        It "Should properly calculate buffer cell width of '<Name>'" -TestCases $testCases {
            param($InputObject, $Length)
            $Host.UI.RawUI.LengthInBufferCells($InputObject) | Should -Be $Length
        }
    }
}

Describe "Pwsh exe resources tests" -Tag CI {
    It "Resource strings are embedded in the executable" -Skip:(!$IsWindows) {
        $pwsh = Get-Item -Path "$PSHOME\pwsh.exe"
        $pwsh.VersionInfo.FileVersion | Should -Match $PSVersionTable.PSVersion.ToString().Split("-")[0]
        $productVersion = $pwsh.VersionInfo.ProductVersion.Replace("-dirty","").Replace(" Commits: ","-").Replace(" SHA: ","-g")
        if ($PSVersionTable.GitCommitId.Contains("-g")) {
            $productVersion | Should -BeExactly $PSVersionTable.GitCommitId
        } else {
            $productVersion | Should -Match $PSVersionTable.GitCommitId
        }
        $pwsh.VersionInfo.ProductName | Should -BeExactly "PowerShell"
    }

    It "Manifest contains compatibility section" -Skip:(!$IsWindows) {
        $osversion = [System.Environment]::OSVersion.Version
        $PSVersionTable.os | Should -MatchExactly "$($osversion.Major).$($osversion.Minor)"
    }
}

Describe 'Pwsh startup in directories that contain wild cards' -Tag CI {
    BeforeAll {
        $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"
        $dirnames = "[T]est","[Test","T][est","Test"
        $testcases = @()
        foreach ( $d in $dirnames ) {
            $null = New-Item -type Directory -Path "${TESTDRIVE}/$d"
            $testcases += @{ Dirname = $d }
        }
    }

    It "pwsh can startup in a directory named <dirname>" -TestCases $testcases {
        param ( $dirname )
        try {
            Push-Location -LiteralPath "${TESTDRIVE}/${dirname}"
            $result = & $powershell -noprofile -c '(Get-Item .).Name'
            $result | Should -BeExactly $dirname
        }
        finally {
            Pop-Location
        }
    }
}

Describe 'Pwsh startup and PATH' -Tag CI {
    BeforeEach {
        $oldPath = $env:PATH
    }

    AfterEach {
        $env:PATH = $oldPath
    }

    It 'Calling pwsh starts the same version of PowerShell as currently running' {
        $version = pwsh -v
        $version | Should -BeExactly "PowerShell $($PSVersionTable.GitCommitId)"
    }

    It 'pwsh starts even if PATH is not defined' {
        $pwsh = Join-Path -Path $PSHOME -ChildPath "pwsh"
        Remove-Item Env:\PATH
        $path = & $pwsh -noprofile -command '$env:PATH'
        $path | Should -BeExactly ($PSHOME + [System.IO.Path]::PathSeparator)
    }
}

Describe 'Console host name' -Tag CI {
    It 'Name is pwsh' {
        (Get-Process -Id $PID).Name | Should -BeExactly 'pwsh'
    }
}

Describe 'TERM env var' -Tag CI {
    BeforeAll {
        $oldTERM = $env:TERM
    }

    AfterAll {
        $env:TERM = $oldTERM
    }

    It 'TERM = "dumb"' {
        $env:TERM = 'dumb'
        pwsh -noprofile -command '$Host.UI.SupportsVirtualTerminal' | Should -BeExactly 'False'
    }

    It 'TERM = "<term>"' -TestCases @(
        @{ term = "xterm-mono" }
        @{ term = "xtermm" }
    ) {
        param ($term)

        $env:TERM = $term
        pwsh -noprofile -command '$PSStyle.OutputRendering' | Should -BeExactly 'PlainText'
    }

    It 'NO_COLOR' {
        try {
            $env:NO_COLOR = 1
            pwsh -noprofile -command '$PSStyle.OutputRendering' | Should -BeExactly 'PlainText'
        }
        finally {
            $env:NO_COLOR = $null
        }
    }
}
