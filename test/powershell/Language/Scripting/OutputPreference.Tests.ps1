# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe '$OutputPreference tests' -Tag CI {
    BeforeAll {
        $originalOutputPreference = $OutputPreference
    }

    AfterAll {
        $OutputPreference = $originalOutputPreference
    }


    Context 'AllowVtEscapeSequences' {
        BeforeAll {
            $OutputPreference = 'AllowVtEscapeSequences'
        }

        It 'Select-String emits VT' {
            "select this string" | Select-String 'this' | Out-String |Should -BeLikeExactly "*`e*"
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
            }

            Get-Error | Out-String | Should -BeLikeExactly "*`e*"
        }
    }

    Context 'NoVtEscapeSequences' {
        BeforeAll {
            $OutputPreference = 'NoVtEscapeSequences'
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
            }

            Get-Error | Out-String | Should -Not -BeLikeExactly "*`e*"
        }
    }
}
