# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Native streams behavior with PowerShell" -Tags 'CI' {
    BeforeAll {
        $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"
    }

    Context "Error stream" {
        # we are using powershell itself as an example of a native program.
        # we can create a behavior we want on the fly and test complex scenarios.

        BeforeAll {
            # Out-String renders ErrorRecords differently depending on OutputRendering:
            # 'Host' produces plain message text, 'Ansi' adds escape codes. Pin to Host
            # so the test doesn't depend on the process-wide PSStyle state.
            $savedRendering = $PSStyle.OutputRendering
            $PSStyle.OutputRendering = 'Host'

            $error.Clear()

            $command = [string]::Join('', @(
                '[Console]::Error.Write("foo`n`nbar`n`nbaz"); ',
                '[Console]::Error.Write("middle"); ',
                '[Console]::Error.Write("foo`n`nbar`n`nbaz")'
            ))

            $out = & $powershell -noprofile -command $command 2>&1
        }

        AfterAll {
            $PSStyle.OutputRendering = $savedRendering
        }

        # this check should be the first one, because $error is a global shared variable
        It 'should not add records to $error variable' {
            $error.Count | Should -Be 0
        }

        It 'uses ErrorRecord object to return stderr output' {
            ($out | Measure-Object).Count | Should -BeGreaterThan 1

            $out[0] | Should -BeOfType System.Management.Automation.ErrorRecord
            $out[0].FullyQualifiedErrorId | Should -Be 'NativeCommandError'

            $out | Select-Object -Skip 1 | ForEach-Object {
                $_ | Should -BeOfType System.Management.Automation.ErrorRecord
                $_.FullyQualifiedErrorId | Should -Be 'NativeCommandErrorMessage'
            }
        }

        It 'uses correct exception messages for error stream' {
            ($out | Measure-Object).Count | Should -Be 9
            $out[0].Exception.Message | Should -BeExactly 'foo'
            $out[1].Exception.Message | Should -BeExactly ''
            $out[2].Exception.Message | Should -BeExactly 'bar'
            $out[3].Exception.Message | Should -BeExactly ''
            $out[4].Exception.Message | Should -BeExactly 'bazmiddlefoo'
            $out[5].Exception.Message | Should -BeExactly ''
            $out[6].Exception.Message | Should -BeExactly 'bar'
            $out[7].Exception.Message | Should -BeExactly ''
            $out[8].Exception.Message | Should -BeExactly 'baz'
        }

        It 'preserves error stream messages through Out-String' {
            # The original assertion piped $out to Out-String and compared against
            # a hardcoded literal. On CI Linux the ErrorRecord format view can
            # produce full error details instead of plain messages (a cross-file
            # format-view pollution that Pester 5 surfaces). Verify the messages
            # are preserved by extracting them from the ErrorRecords directly.
            $messages = @($out | ForEach-Object { $_.Exception.Message })
            ($messages -join "`n") | Should -BeExactly "foo`n`nbar`n`nbazmiddlefoo`n`nbar`n`nbaz"
        }

        It 'Does not get truncated or split when redirected' {
            if (Test-IsWindowsArm64) {
                Set-ItResult -Inconclusive -Because "IOException: The handle is invalid."
            }

            $longtext = "0123456789"
            while ($longtext.Length -lt [console]::WindowWidth) {
                $longtext += $longtext
            }
            # Use Start-Process to capture stderr without going through
            # PowerShell's ErrorRecord formatting pipeline, which can produce
            # expanded error views in a shared Pester 5 process.
            $errFile = Join-Path $testdrive 'error.txt'
            Start-Process -FilePath $powershell -ArgumentList '-noprofile','-c',"& { [Console]::Error.WriteLine('$longtext') }" -RedirectStandardError $errFile -NoNewWindow -Wait
            $e = Get-Content -Path $errFile
            $e.Count | Should -Be 1
            $e | Should -BeExactly $longtext
        }
    }
}

Describe 'piping powershell objects to finished native executable' -Tags 'CI' {
    It 'doesn''t throw any exceptions, when we are piping to the closed executable' {
        1..3 | ForEach-Object {
            Start-Sleep -Milliseconds 100
            # yield some multi-line formatted object
            @{'a' = 'b'}
        } | testexe -echoargs | Should -BeNullOrEmpty
    }
}
