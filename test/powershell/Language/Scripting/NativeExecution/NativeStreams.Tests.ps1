# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Native streams behavior with PowerShell" -Tags 'CI' {
    $powershell = Join-Path -Path $PsHome -ChildPath "pwsh"

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
            $error.Count | Should -Be 9
        }

        It 'uses ErrorRecord object to return stderr output' {
            ($out | Measure-Object).Count | Should -BeGreaterThan 1

            $out[0] | Should -BeOfType 'System.Management.Automation.ErrorRecord'
            $out[0].FullyQualifiedErrorId | Should -Be 'NativeCommandError'

            $out | Select-Object -Skip 1 | ForEach-Object {
                $_ | Should -BeOfType 'System.Management.Automation.ErrorRecord'
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

        It 'preserves error stream as is with Out-String' {
            ($out | Out-String).Replace("`r", '') | Should -BeExactly "foo`n`nbar`n`nbazmiddlefoo`n`nbar`n`nbaz`n"
        }

        It 'does not get truncated or split when redirected' {
            $longtext = "0123456789"
            while ($longtext.Length -lt [console]::WindowWidth) {
                $longtext += $longtext
            }
            pwsh -c "& { [Console]::Error.WriteLine('$longtext') }" 2>&1 > $testdrive\error.txt
            $e = Get-Content -Path $testdrive\error.txt
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
