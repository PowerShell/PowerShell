# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe '$env:__SuppressAnsiEscapeSequences tests' -Tag CI {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()

        if (-not $host.ui.SupportsVirtualTerminal) {
            $PSDefaultParameterValues["it:skip"] = $true
        }

        $originalSuppressPref = $env:__SuppressAnsiEscapeSequences
        $originalRendering = $PSStyle.OutputRendering
        $PSStyle.OutputRendering = 'Ansi'
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
        $env:__SuppressAnsiEscapeSequences = $originalSuppressPref
        $PSStyle.OutputRendering = $originalRendering
    }


    Context 'Allow Escape Sequences' {
        BeforeAll {
            Remove-Item env:__SuppressAnsiEscapeSequences -ErrorAction Ignore
        }

        It 'Select-String emits VT' {
            "select this string" | Select-String 'this' | Out-String | Should -BeLikeExactly "*`e*"
        }

        It 'ConciseView emits VT' {
            $oldErrorView = $ErrorView

            try {
                $ErrorView = 'ConciseView'
                Invoke-Expression '1/d'
            }
            catch {
                $e = $_
            }
            finally {
                $ErrorView = $oldErrorView
            }

            $e | Out-String | Should -BeLikeExactly "*`e*"
        }

        It 'Get-Error emits VT' {
            try {
                1/0
            }
            catch {
                # ignore
            }

            Get-Error | Out-String | Should -BeLikeExactly "*`e*"
        }
    }

    Context 'No Escape Sequences' {
        BeforeAll {
            $env:__SuppressAnsiEscapeSequences = 1
        }

        It 'Select-String does not emit VT' {
            "select this string" | Select-String 'this' | Out-String | Should -Not -BeLikeExactly "*`e*"
        }

        It 'ConciseView does not emit VT' {
            $oldErrorView = $ErrorView

            try {
                $ErrorView = 'ConciseView'
                Invoke-Expression '1/d'
            }
            catch {
                $e = $_
            }
            finally {
                $ErrorView = $oldErrorView
            }

            $e | Out-String | Should -Not -BeLikeExactly "*`e*"
        }

        It 'Get-Error does not emit VT' {
            try {
                1/0
            }
            catch {
                # ignore
            }

            Get-Error | Out-String | Should -Not -BeLikeExactly "*`e*"
        }
    }
}
