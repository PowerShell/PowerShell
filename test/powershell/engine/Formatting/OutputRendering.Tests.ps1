# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'OutputRendering tests' {
    BeforeAll {
        $PSDefaultParameterValues.Add('It:Skip', (-not $EnabledExperimentalFeatures.Contains('PSAnsiRendering')))
    }

    AfterAll {
        $PSDefaultParameterValues.Remove('It:Skip')
    }

    BeforeEach {
        $oldOutputRendering = $PSStyle.OutputRendering
    }

    AfterEach {
        $PSStyle.OutputRendering = $oldOutputRendering
    }

    It 'OutputRendering works for "<outputRendering>" to the host' -TestCases @(
        @{ outputRendering = 'automatic'; ansi = $true }
        @{ outputRendering = 'host'     ; ansi = $true }
        @{ outputRendering = 'ansi'     ; ansi = $true }
        @{ outputRendering = 'plaintext'; ansi = $false }
    ) {
        param($outputRendering, $ansi)

        $out = pwsh -noprofile -command "`$PSStyle.OutputRendering = '$outputRendering'; write-host '$($PSStyle.Foreground.Green)hello'"

        if ($ansi) {
            $out | Should -BeLike "*`e*"
        }
        else {
            $out | Should -Not -BeLike "*`e*"
        }
    }

    It 'OutputRendering works for "<outputRendering>" to the pipeline' -TestCases @(
        @{ outputRendering = 'automatic'; ansi = $true }
        @{ outputRendering = 'host'     ; ansi = $true }
        @{ outputRendering = 'ansi'     ; ansi = $true }
        @{ outputRendering = 'plaintext'; ansi = $false }
    ) {
        param($outputRendering, $ansi)

        $out = pwsh -noprofile -command "`$PSStyle.OutputRendering = '$outputRendering'; '$($PSStyle.Foreground.Green)hello'"

        if ($ansi) {
            $out | Should -BeLike "*`e*"
        }
        else {
            $out | Should -Not -BeLike "*`e*"
        }
    }
}
