# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Tests for $ErrorView' -Tag CI {

    It '$ErrorView is an enum' {
        $ErrorView | Should -BeOfType System.Management.Automation.ErrorView
    }

    It '$ErrorView should have correct default value' {
        $expectedDefault = 'ConciseView'

        $ErrorView | Should -BeExactly $expectedDefault
    }

    It 'Exceptions not thrown do not get formatted as ErrorRecord' {
        $exp = [System.Exception]::new('test') | Out-String
        $exp | Should -BeLike '*Message        : *test*'
    }

    Context 'ConciseView tests' {
        BeforeEach {
            $testScriptPath = Join-Path -Path $TestDrive -ChildPath 'test.ps1'
            $testModulePath = Join-Path -Path $TestDrive -ChildPath 'test.psm1'
        }

        AfterEach {
            Remove-Item -Path $testScriptPath -Force -ErrorAction SilentlyContinue
        }

        It 'Cmdlet error should be one line of text' {
            Get-Item (New-Guid) -ErrorVariable e -ErrorAction SilentlyContinue
            ($e | Out-String).Trim().Count | Should -Be 1
        }

        It 'Script error should contain path to script and line for error' {
            $testScript = @'
                [cmdletbinding()]
                param()
                $a = 1
                123)
                $b = 2
'@

            Set-Content -Path $testScriptPath -Value $testScript
            $e = { & $testScriptPath } | Should -Throw -ErrorId 'UnexpectedToken' -PassThru | Out-String
            $e | Should -BeLike "*${testScriptPath}:4*"
            # validate line number is shown
            $e | Should -BeLike '* 4 *'
        }

        It 'Remote errors show up correctly' {
            Start-Job -ScriptBlock { Get-Item (New-Guid) } | Wait-Job | Receive-Job -ErrorVariable e -ErrorAction SilentlyContinue
            ($e | Out-String).Trim().Count | Should -Be 1
        }

        It 'Activity shows up correctly for scriptblocks' {
            $e = & "$PSHOME/pwsh" -noprofile -command 'Write-Error 'myError' -ErrorAction SilentlyContinue; $error[0] | Out-String'
            [string]::Join('', $e).Trim() | Should -BeLike '*Write-Error:*myError*' # wildcard due to VT100
        }

        It 'Function shows up correctly' {
            function test-myerror { [cmdletbinding()] param() Write-Error 'myError' }

            $e = & "$PSHOME/pwsh" -noprofile -command 'function test-myerror { [cmdletbinding()] param() write-error "myError" }; test-myerror -ErrorAction SilentlyContinue; $error[0] | Out-String'
            [string]::Join('', $e).Trim() | Should -BeLike '*test-myerror:*myError*' # wildcard due to VT100
        }

        It 'Pester Should shows test file and not pester' {
            $testScript = '1 + 1 | Should -Be 3'

            Set-Content -Path $testScriptPath -Value $testScript
            $e = { & $testScriptPath } | Should -Throw -ErrorId 'PesterAssertionFailed' -PassThru | Out-String
            $e | Should -BeLike "*$testScriptPath*"
            $e | Should -Not -BeLike '*pester*'
        }

        It 'Long lines should be rendered correctly with indentation' {
            $testscript = @'
                        $myerrors = [System.Collections.ArrayList]::new()
                        Copy-Item (New-Guid) (New-Guid) -ErrorVariable +myerrors -ErrorAction SilentlyContinue
                $error[0]
'@

            Set-Content -Path $testScriptPath -Value $testScript
            $e = & $testScriptPath | Out-String
            $e | Should -BeLike "*${testScriptPath}:2*"
            # validate line number is shown
            $e | Should -BeLike '* 2 *'
        }

        It 'Long exception message gets rendered' {

            $msg = '1234567890'
            while ($msg.Length -le $Host.UI.RawUI.WindowSize.Width) {
                $msg += $msg
            }

            $e = { throw "$msg" } | Should -Throw $msg -PassThru | Out-String
            $e | Should -BeLike "*$msg*"
        }

        It 'Position message does not contain line information' {

            $e = & "$PSHOME/pwsh" -noprofile -command 'foreach abc' 2>&1 | Out-String
            $e | Should -Not -BeNullOrEmpty
            $e | Should -Not -BeLike '*At line*'
        }

        It "Error shows if `$PSModuleAutoLoadingPreference is set to 'none'" {
            $e = & "$PSHOME/pwsh" -noprofile -command '$PSModuleAutoLoadingPreference = "none"; cmdletThatDoesntExist' 2>&1 | Out-String
            $e | Should -BeLike '*cmdletThatDoesntExist*'
        }

        It 'Error shows for advanced function' {
            # need to have it virtually interactive so that InvocationInfo.MyCommand is empty
            $e = '[cmdletbinding()]param()$pscmdlet.writeerror([System.Management.Automation.ErrorRecord]::new(([System.NotImplementedException]::new("myTest")),"stub","notimplemented","command"))' | pwsh -noprofile -file - 2>&1
            $e = $e | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] } | Out-String
            $e | Should -Not -BeNullOrEmpty

            # need to see if ANSI escape sequences are in the output as ANSI is disabled for CI
            if ($e.Contains("`e")) {
                $e | Should -BeLike "*: `e*myTest*"
            } else {
                $e | Should -BeLike '*: myTest*'
            }
        }

        It "Error containing '<type>' are rendered correctly for scripts" -TestCases @(
            @{ type = 'CRLF'; newline = "`r`n" }
            @{ type = 'LF'  ; newline = "`n" }
        ) {
            param($newline)

            Set-Content -Path $testScriptPath -Value "throw 'hello${newline}there'"
            $e = & "$PSHOME/pwsh" -noprofile -file $testScriptPath 2>&1 | Out-String
            $e.Split("o${newline}t").Count | Should -Be 1 -Because 'Error message should not contain newline'
        }

        It 'Script module error should not show line information' {
            $testModule = @'
                function Invoke-Error() {
                    throw 'oops'
                }
'@

            Set-Content -Path $testModulePath -Value $testModule
            $e = & "$PSHOME/pwsh" -noprofile -command "Import-Module '$testModulePath'; Invoke-Error" 2>&1 | Out-String
            $e | Should -Not -BeNullOrEmpty
            $e | Should -Not -BeLike '*Line*'
        }

        It 'Parser error shows line information' {
            $testScript = '$psstyle.outputrendering = "plaintext"; 1 ++ 1'
            $e = & "$PSHOME/pwsh" -noprofile -command $testScript 2>&1 | Out-String
            $e | Should -Not -BeNullOrEmpty
            $e = $e.Split([Environment]::NewLine)
            $e[0] | Should -BeLike 'ParserError:*'
            $e[1] | Should -BeLike 'Line *' -Because ($e | Out-String)
            $e[2] | Should -BeLike '*|*1 ++ 1*'
        }

        It 'Faux remote parser error shows concise message' {
            Start-Job { [cmdletbinding()]param() $e = [System.Management.Automation.ErrorRecord]::new([System.Exception]::new('hello'), 1, 'ParserError', $null); $pscmdlet.ThrowTerminatingError($e) } | Wait-Job | Receive-Job -ErrorVariable e -ErrorAction SilentlyContinue
            $e | Out-String | Should -BeLike '*ParserError*'
        }

        It 'Parser TargetObject shows Line information' {
            $expected = (@(
                ": "
                "Line |"
                "   1 | This is the line with the error"
                "     | Test Parser Error"
            ) -join ([Environment]::NewLine)).TrimEnd()
            $e = {
                [CmdletBinding()]
                param ()

                $PSCmdlet.ThrowTerminatingError(
                    [System.Management.Automation.ErrorRecord]::new(
                        [System.Exception]::new('Test Parser Error'),
                        'ParserErrorText',
                        [System.Management.Automation.ErrorCategory]::ParserError,
                        @{
                            Line = 1
                            LineText = 'This is the line with the error'
                        }
                    )
                )
            } | Should -Throw -PassThru

            $actual = ($e | Out-String).TrimEnd()
            $actual | Should -BeExactly $expected
        }

        It 'Parser TargetObject shows File information' {
            $expected = (@(
                ": MyFile.ps1"
                "Line |"
                "   1 | This is the line with the error"
                "     | Test Parser Error"
            ) -join ([Environment]::NewLine)).TrimEnd()
            $e = {
                [CmdletBinding()]
                param ()

                $PSCmdlet.ThrowTerminatingError(
                    [System.Management.Automation.ErrorRecord]::new(
                        [System.Exception]::new('Test Parser Error'),
                        'ParserErrorText',
                        [System.Management.Automation.ErrorCategory]::ParserError,
                        @{
                            File = 'MyFile.ps1'
                            Line = 1
                            LineText = 'This is the line with the error'
                        }
                    )
                )
            } | Should -Throw -PassThru

            $actual = ($e | Out-String).TrimEnd()
            $actual | Should -BeExactly $expected
        }

        It 'Parser TargetObject has StartColumn' {
            $expected = (@(
                ": "
                "Line |"
                "   5 | This is the line with the error"
                "     |                  ~~~~~~~~~~~~~~"
                "     | Test Parser Error"
            ) -join ([Environment]::NewLine)).TrimEnd()
            $e = {
                [CmdletBinding()]
                param ()

                $PSCmdlet.ThrowTerminatingError(
                    [System.Management.Automation.ErrorRecord]::new(
                        [System.Exception]::new('Test Parser Error'),
                        'ParserErrorText',
                        [System.Management.Automation.ErrorCategory]::ParserError,
                        @{

                            Line = 5
                            LineText = 'This is the line with the error'
                            StartColumn = 18
                        }
                    )
                )
            } | Should -Throw -PassThru

            $actual = ($e | Out-String).TrimEnd()
            $actual | Should -BeExactly $expected
        }

        It 'Parser TargetObject has StartColumn and EndColumn' {
            $expected = (@(
                ": "
                "Line |"
                "   5 | This is the line with the error"
                "     |                  ~~~~"
                "     | Test Parser Error"
            ) -join ([Environment]::NewLine)).TrimEnd()
            $e = {
                [CmdletBinding()]
                param ()

                $PSCmdlet.ThrowTerminatingError(
                    [System.Management.Automation.ErrorRecord]::new(
                        [System.Exception]::new('Test Parser Error'),
                        'ParserErrorText',
                        [System.Management.Automation.ErrorCategory]::ParserError,
                        @{

                            Line = 5
                            LineText = 'This is the line with the error'
                            StartColumn = 18
                            EndColumn = 22
                        }
                    )
                )
            } | Should -Throw -PassThru

            $actual = ($e | Out-String).TrimEnd()
            $actual | Should -BeExactly $expected
        }

        It 'Parser TargetObject has StartColumn at end of the line' {
            $expected = (@(
                ": "
                "Line |"
                "   5 | This is the line with the error"
                "     |                               ~"
                "     | Test Parser Error"
            ) -join ([Environment]::NewLine)).TrimEnd()
            $e = {
                [CmdletBinding()]
                param ()

                $PSCmdlet.ThrowTerminatingError(
                    [System.Management.Automation.ErrorRecord]::new(
                        [System.Exception]::new('Test Parser Error'),
                        'ParserErrorText',
                        [System.Management.Automation.ErrorCategory]::ParserError,
                        @{

                            Line = 5
                            LineText = 'This is the line with the error'
                            StartColumn = 31
                        }
                    )
                )
            } | Should -Throw -PassThru

            $actual = ($e | Out-String).TrimEnd()
            $actual | Should -BeExactly $expected
        }


        It 'Parser TargetObject has StartColumn at end of the line with EndColumn' {
            $expected = (@(
                ": "
                "Line |"
                "   5 | This is the line with the error"
                "     |                               ~"
                "     | Test Parser Error"
            ) -join ([Environment]::NewLine)).TrimEnd()
            $e = {
                [CmdletBinding()]
                param ()

                $PSCmdlet.ThrowTerminatingError(
                    [System.Management.Automation.ErrorRecord]::new(
                        [System.Exception]::new('Test Parser Error'),
                        'ParserErrorText',
                        [System.Management.Automation.ErrorCategory]::ParserError,
                        @{

                            Line = 5
                            LineText = 'This is the line with the error'
                            StartColumn = 31
                            EndColumn = 32
                        }
                    )
                )
            } | Should -Throw -PassThru

            $actual = ($e | Out-String).TrimEnd()
            $actual | Should -BeExactly $expected
        }

        It 'Parser TargetObject ignores EndColumn if no StartColumn' {
            $expected = (@(
                ": "
                "Line |"
                "   1 | This is the line with the error"
                "     | Test Parser Error"
            ) -join ([Environment]::NewLine)).TrimEnd()
            $e = {
                [CmdletBinding()]
                param ()

                $PSCmdlet.ThrowTerminatingError(
                    [System.Management.Automation.ErrorRecord]::new(
                        [System.Exception]::new('Test Parser Error'),
                        'ParserErrorText',
                        [System.Management.Automation.ErrorCategory]::ParserError,
                        @{
                            Line = 1
                            LineText = 'This is the line with the error'
                            EndColumn = 22
                        }
                    )
                )
            } | Should -Throw -PassThru

            $actual = ($e | Out-String).TrimEnd()
            $actual | Should -BeExactly $expected
        }

        It 'Parser TargetObject converts StartColumn and EndColumn from string' {
            $expected = (@(
                ": "
                "Line |"
                "   5 | This is the line with the error"
                "     |                  ~~~~"
                "     | Test Parser Error"
            ) -join ([Environment]::NewLine)).TrimEnd()
            $e = {
                [CmdletBinding()]
                param ()

                $PSCmdlet.ThrowTerminatingError(
                    [System.Management.Automation.ErrorRecord]::new(
                        [System.Exception]::new('Test Parser Error'),
                        'ParserErrorText',
                        [System.Management.Automation.ErrorCategory]::ParserError,
                        @{

                            Line = 5
                            LineText = 'This is the line with the error'
                            StartColumn = "18"
                            EndColumn = "22"
                        }
                    )
                )
            } | Should -Throw -PassThru

            $actual = ($e | Out-String).TrimEnd()
            $actual | Should -BeExactly $expected
        }

        It 'Parser TargetObject ignores StartColumn if it cannot be converted' {
            $expected = (@(
                ": "
                "Line |"
                "   1 | This is the line with the error"
                "     | Test Parser Error"
            ) -join ([Environment]::NewLine)).TrimEnd()
            $e = {
                [CmdletBinding()]
                param ()

                $PSCmdlet.ThrowTerminatingError(
                    [System.Management.Automation.ErrorRecord]::new(
                        [System.Exception]::new('Test Parser Error'),
                        'ParserErrorText',
                        [System.Management.Automation.ErrorCategory]::ParserError,
                        @{
                            Line = 1
                            LineText = 'This is the line with the error'
                            StartColumn = 'abc'
                            EndColumn = 22
                        }
                    )
                )
            } | Should -Throw -PassThru

            $actual = ($e | Out-String).TrimEnd()
            $actual | Should -BeExactly $expected
        }

        It 'Parser TargetObject ignores EndColumn if it cannot be converted' {
            $expected = (@(
                ": "
                "Line |"
                "   5 | This is the line with the error"
                "     |                  ~~~~~~~~~~~~~~"
                "     | Test Parser Error"
            ) -join ([Environment]::NewLine)).TrimEnd()
            $e = {
                [CmdletBinding()]
                param ()

                $PSCmdlet.ThrowTerminatingError(
                    [System.Management.Automation.ErrorRecord]::new(
                        [System.Exception]::new('Test Parser Error'),
                        'ParserErrorText',
                        [System.Management.Automation.ErrorCategory]::ParserError,
                        @{

                            Line = 5
                            LineText = 'This is the line with the error'
                            StartColumn = 18
                            EndColumn = 'abc'
                        }
                    )
                )
            } | Should -Throw -PassThru

            $actual = ($e | Out-String).TrimEnd()
            $actual | Should -BeExactly $expected
        }

        It 'Parser TargetObject ignores StartColumn with invalid value <Value>' -TestCases @(
            @{ Value = -1 }
            @{ Value = 0 }
            @{ Value = 32 }  # Beyond end of line
        ) {
            param ($Value)

            $expected = (@(
                ": "
                "Line |"
                "   1 | This is the line with the error"
                "     | Test Parser Error"
            ) -join ([Environment]::NewLine)).TrimEnd()
            $e = {
                [CmdletBinding()]
                param ()

                $PSCmdlet.ThrowTerminatingError(
                    [System.Management.Automation.ErrorRecord]::new(
                        [System.Exception]::new('Test Parser Error'),
                        'ParserErrorText',
                        [System.Management.Automation.ErrorCategory]::ParserError,
                        @{
                            Line = 1
                            LineText = 'This is the line with the error'
                            StartColumn = $Value
                        }
                    )
                )
            } | Should -Throw -PassThru

            $actual = ($e | Out-String).TrimEnd()
            $actual | Should -BeExactly $expected
        }

        It 'Parser TargetObject ignores EndColumn with invalid value <Value>' -TestCases @(
            @{ Value = -1 }
            @{ Value = 0 }
            @{ Value = 17 }  # Before StartColumn
            @{ Value = 18 }  # Equal to StartColumn
            @{ Value = 33 }  # Beyond end of line + 1
        ) {
            param ($Value)

            $expected = (@(
                ": "
                "Line |"
                "   1 | This is the line with the error"
                "     |                  ~~~~~~~~~~~~~~"
                "     | Test Parser Error"
            ) -join ([Environment]::NewLine)).TrimEnd()
            $e = {
                [CmdletBinding()]
                param ()

                $PSCmdlet.ThrowTerminatingError(
                    [System.Management.Automation.ErrorRecord]::new(
                        [System.Exception]::new('Test Parser Error'),
                        'ParserErrorText',
                        [System.Management.Automation.ErrorCategory]::ParserError,
                        @{
                            Line = 1
                            LineText = 'This is the line with the error'
                            StartColumn = 18
                            EndColumn = $Value
                        }
                    )
                )
            } | Should -Throw -PassThru

            $actual = ($e | Out-String).TrimEnd()
            $actual | Should -BeExactly $expected
        }

        It 'Exception thrown from Enumerator.MoveNext in a pipeline shows information' {
            $e = {
                $l = [System.Collections.Generic.List[string]] @('one', 'two')
                $l | ForEach-Object { $null = $l.Remove($_) }
            } | Should -Throw -ErrorId 'BadEnumeration' -PassThru | Out-String

            $e | Should -BeLike 'InvalidOperation:*'
        }

        It 'Displays a RecommendedAction if present in the ErrorRecord' {
            $testScript = 'Write-Error -Message ''TestError'' -RecommendedAction ''TestAction'''
            $e = & "$PSHOME/pwsh" -noprofile -command $testScript 2>&1 | Out-String
            $e | Should -BeLike "*$([Environment]::NewLine)  Recommendation: TestAction*"
        }
    }

    Context 'NormalView tests' {

        It 'Error shows up when using strict mode' {
            try {
                $ErrorView = 'NormalView'
                Set-StrictMode -Version 2
                throw 'Oops!'
            } catch {
                $e = $_ | Out-String
            } finally {
                Set-StrictMode -Off
            }

            $e | Should -BeLike '*Oops!*'
        }
    }

    Context 'DetailedView tests' {

        It 'Detailed error is rendered' {
            try {
                $ErrorView = 'DetailedView'
                throw 'Oops!'
            } catch {
                # an extra newline gets added by the formatting system so we remove them
                $e = ($_ | Out-String).Trim([Environment]::NewLine.ToCharArray())
            }

            $e | Should -BeExactly (Get-Error | Out-String).Trim([Environment]::NewLine.ToCharArray())
        }
    }
}
