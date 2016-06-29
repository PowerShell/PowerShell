using namespace System.Diagnostics

Describe "ConsoleHost unit tests" -Tag 'Slow' {

    $powershell = Join-Path -Path $PsHome -ChildPath "powershell"

    Context "CommandLine" {
        It "simple -args" {
            & $powershell -noprofile { $args[0] } -args "hello world" | Should Be "hello world"
        }

        It "array -args" {
            & $powershell -noprofile { $args[0] } -args 1,(2,3) | Should Be 1
            (& $powershell -noprofile { $args[1] } -args 1,(2,3))[1]  | Should Be 3
        }
        foreach ($x in "--help", "-help", "-h", "-?", "--he", "-hel", "--HELP", "-hEl") {
            It "Accepts '$x' as a parameter for help" {
                & $powershell -noprofile $x | ?{ $_ -match "PowerShell[.exe] -Help | -? | /?" } | Should Not BeNullOrEmpty
            }
        }

        It "Should accept a Base64 encoded command" {
            $commandString = "Get-Location"
            $encodedCommand = [System.Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($commandString))
            # We don't compare to `Get-Location` directly because object and formatted output comparisons are difficult
            $expected = & $powershell -noprofile -command $commandString
            $actual = & $powershell -noprofile -EncodedCommand $encodedCommand
            $actual | Should Be $expected
        }

    }

    Context "Pipe to/from powershell" {
        $p = [PSCustomObject]@{X=10;Y=20}

        It "xml input" {
            $p | & $powershell -noprofile { $input | Foreach-Object {$a = 0} { $a += $_.X + $_.Y } { $a } } | Should Be 30
            $p | & $powershell -noprofile -inputFormat xml { $input | Foreach-Object {$a = 0} { $a += $_.X + $_.Y } { $a } } | Should Be 30
        }

        It "text input" {
            # Join (multiple lines) and remove whitespace (we don't care about spacing) to verify we converted to string (by generating a table)
            $p | & $powershell -noprofile -inputFormat text { -join ($input -replace "\s","") } | Should Be "XY--1020"
        }

        It "xml output" {
            & $powershell -noprofile { [PSCustomObject]@{X=10;Y=20} } | Foreach-Object {$a = 0} { $a += $_.X + $_.Y } { $a } | Should Be 30
            & $powershell -noprofile -outputFormat xml { [PSCustomObject]@{X=10;Y=20} } | Foreach-Object {$a = 0} { $a += $_.X + $_.Y } { $a } | Should Be 30
        }

        It "text output" {
            # Join (multiple lines) and remove whitespace (we don't care about spacing) to verify we converted to string (by generating a table)
            -join (& $powershell -noprofile -outputFormat text { [PSCustomObject]@{X=10;Y=20} }) -replace "\s","" | Should Be "XY--1020"
        }
    }

    Context "Redirected standard handles" {
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
                $process.HasExited | Should Be $true
                $process.Kill()
            }
        }

        It "Simple redirected output" {
            $si = NewProcessStartInfo "-noprofile 1+1"
            $process = RunPowerShell $si
            $process.StandardOutput.ReadToEnd() | Should Be 2
            EnsureChildHasExited $process
        }

        $nl = [Environment]::Newline

        # Redirected input is broken on Windows in .NET Core
        It "Redirected input" -Pending:$IsWindows {
            $si = NewProcessStartInfo "-noprofile ""`$function:prompt = { 'PS> ' }""" -RedirectStdIn
            $process = RunPowerShell $si
            $process.StandardInput.Write("1+1`n")
            $process.StandardOutput.ReadLine() | Should Be "PS> 1+1"
            $process.StandardOutput.ReadLine() | Should Be "2"
            $process.StandardInput.Write("1+2`n")
            $process.StandardOutput.ReadLine() | Should Be "PS> 1+2"
            $process.StandardOutput.ReadLine() | Should Be "3"
            $process.StandardInput.Close()
            $process.StandardOutput.ReadToEnd() | Should Be "PS> "
            EnsureChildHasExited $process
        }

        It "Redirected input explicit prompting" -Pending:$IsCore {
            $si = NewProcessStartInfo "-noprofile -File -" -RedirectStdIn
            $process = RunPowerShell $si
            $process.StandardInput.Write("`$function:prompt = { 'PS> ' }`n")
            $null = $process.StandardOutput.ReadLine()
            $process.StandardInput.Write("1+1`n")
            $process.StandardOutput.ReadLine() | Should Be "PS> 1+1"
            $process.StandardOutput.ReadLine() | Should Be "2"
            $process.StandardInput.Close()
            $process.StandardOutput.ReadToEnd() | Should Be "PS> "
            EnsureChildHasExited $process
        }

        It "Redirected input no prompting" -Pending:$IsCore {
            $si = NewProcessStartInfo "-noprofile -" -RedirectStdIn
            $process = RunPowerShell $si
            $process.StandardInput.Write("1+1`n")
            $process.StandardInput.Close()
            $process.StandardOutput.ReadToEnd() | Should Be "2${nl}"
            EnsureChildHasExited $process
        }

        It "Redirected input w/ nested prompt" -Pending:$IsCore {
            $si = NewProcessStartInfo "-noprofile ""`$function:prompt = { 'PS' + ('>'*(`$nestedPromptLevel+1)) + ' ' }""" -RedirectStdIn
            $process = RunPowerShell $si
            $process.StandardInput.Write("`$host.EnterNestedPrompt()`n")
            $process.StandardOutput.ReadLine() | Should Be "PS> `$host.EnterNestedPrompt()"
            $process.StandardInput.Write("exit`n")
            $process.StandardOutput.ReadLine() | Should Be "PS>> exit"
            $process.StandardInput.Close()
            $process.StandardOutput.ReadToEnd() | Should Be "PS> "
            EnsureChildHasExited $process
        }
    }

    Context "Exception handling" {
        It "Should handle a CallDepthOverflow" {
            # Infinite recursion
            function recurse
            {
                recurse $args
            }

            try
            {
                recurse "args"
                Throw "Incorrect exception"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be "CallDepthOverflow"
            }
        }
    }
}
