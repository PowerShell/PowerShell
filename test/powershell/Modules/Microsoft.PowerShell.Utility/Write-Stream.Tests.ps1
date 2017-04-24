Describe "Stream writer tests" -Tags "CI" {
    $targetfile = Join-Path -Path $TestDrive -ChildPath "writeoutput.txt"

    # A custom function is defined here do handle the debug stream dealing with the confirm prompt
    # that would normally
    function Write-Messages
    {
        [CmdletBinding()]

        param()
        If ($PSBoundParameters['Debug']) { $DebugPreference = 'Continue' }
        Write-Verbose "Verbose message"

        Write-Debug "Debug message"

    }

    function Get-OutputResults
    {
        # Get the contents of the targetfile.
        # Make the array a string for less brittle testing
        $output = $(Get-Content $args[0])
        [String]::Join([Environment]::NewLine, $output )

        return $output
    }
    Context "Redirect Stream Tests" {
        # These tests validate that a stream is actually being written to by redirecting the output of that stream

        AfterEach { Remove-Item $targetfile }
        It "Should write warnings to the warning stream" {
            Write-Warning "Test Warning" 3>&1 > $targetfile

            Get-Content $targetfile | Should Be "Test Warning"
        }

        It "Should write error messages to the error stream" {
            Write-Error "Testing Error" 2>&1 > $targetfile

            $result = Get-OutputResults $targetfile
            # The contents of the error stream should contain the expected text
            $result -match ": Testing Error" | Should Be $true
        }

        It "Should write debug messages to the debug stream" {
            Write-Messages -Debug -EA SilentlyContinue 5>&1 > $targetfile

            $result = Get-OutputResults $targetfile

            # The contents of the debug stream should contain the expected text
            $result -match "Debug Message" | Should Be $true
        }

        It "Should write messages to the verbose stream" {
            Write-Messages -Verbose 4>&1 > $targetfile

            $result = Get-OutputResults $targetfile

            # The contents of the debug stream should contain the expected text
            $result -match "Verbose Message" | Should Be $true
        }
    }

    Context "Error automatic variable" {
        It "Should write error messages to the `$Error automatic variable" {
            Write-Error "Test Error Message" -ErrorAction SilentlyContinue

            $Error[0] | Should Match "Test Error Message"
        }
    }

    Context "Write-Information cmdlet" {
        BeforeAll {
            $ps = [powershell]::Create()
        }

        BeforeEach {
            $ps.Commands.Clear()
            $ps.Streams.ClearStreams()
        }

        AfterAll {
            $ps.Dispose()
        }

       It "Write-Information outputs an information object" -Pending:($IsOSX) {
            # redirect the streams is sufficient
            $result = Write-Information "Test Message" *>&1
            $result.NativeThreadId | Should Not Be 0
            $result.ProcessId | Should Be $pid
            $result | Should BeOfType System.Management.Automation.InformationRecord

            # Use Match instead of Be so we can avoid dealing with a potential domain name
            $result.Computer | Should Match "^($([environment]::MachineName)){1}(\.[a-zA-Z0-9]+)*$"
            if ($IsWindows)
            {
                $result.User | Should Match ".*${env:USERNAME}"
            }
            else
            {
                $result.User | Should Be $(whoami)
            }

            "$result" | Should be "Test Message"
       }

       It "Write-Information accept objects from pipe" {
            $ps.AddScript("'teststring',12345 | Write-Information -InformationAction Continue").Invoke()
            $result = $ps.Streams.Information
            $result.Count | Should be 2
            $result[0].MessageData | Should be "teststring"
            $result[1].MessageData | Should be "12345"
       }
    }
}
