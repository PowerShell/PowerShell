Describe "Native streams behavior with PowerShell" -Tags 'CI' {
    $powershell = Join-Path -Path $PsHome -ChildPath "powershell"

    Context "Error stream" {
        # we are using powershell itself as an example of a native program.
        # we can create a behavior we want on the fly and test complex scenarios.

        $error.Clear()

        $command = [string]::Join('', @(
            '[Console]::Error.Write(\"foo`n`nbar`n`nbaz\"); ',
            '[Console]::Error.Write(\"middle\"); ',
            '[Console]::Error.Write(\"foo`n`nbar`n`nbaz\")'
        ))

        $out = & $powershell -noprofile -command $command 2>&1

        # this check should be the first one, because $error is a global shared variable
        It 'should not add records to $error variable' {
            # we are keeping existing Windows PS v5.1 behavior for $error variable
            $error.Count | Should Be 9
        }

        It 'uses ErrorRecord object to return stderr output' {
            ($out | measure).Count | Should BeGreaterThan 1

            $out[0] | Should BeOfType 'System.Management.Automation.ErrorRecord'
            $out[0].FullyQualifiedErrorId | Should Be 'NativeCommandError'

            $out | Select-Object -Skip 1 | % {
                $_ | Should BeOfType 'System.Management.Automation.ErrorRecord'
                $_.FullyQualifiedErrorId | Should Be 'NativeCommandErrorMessage'
            }
        }

        It 'uses correct exception messages for error stream' {
            ($out | measure).Count | Should Be 9
            $out[0].Exception.Message | Should Be 'foo'
            $out[1].Exception.Message | Should Be ''
            $out[2].Exception.Message | Should Be 'bar'
            $out[3].Exception.Message | Should Be ''
            $out[4].Exception.Message | Should Be 'bazmiddlefoo'
            $out[5].Exception.Message | Should Be ''
            $out[6].Exception.Message | Should Be 'bar'
            $out[7].Exception.Message | Should Be ''
            $out[8].Exception.Message | Should Be 'baz'
        }

        It 'preserves error stream as is with Out-String' {
            ($out | Out-String).Replace("`r", '') | Should Be "foo`n`nbar`n`nbazmiddlefoo`n`nbar`n`nbaz`n"
        }
    }
}
