# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'OutputRendering tests' {
    BeforeAll {
        $PSDefaultParameterValues.Add('It:Skip', (-not $EnabledExperimentalFeatures.Contains('PSAnsiRendering')))
        $th = New-TestHost
        $rs = [runspacefactory]::Createrunspace($th)
        $rs.open()
        $ps = [powershell]::Create()
        $ps.Runspace = $rs
    }

    AfterAll {
        $PSDefaultParameterValues.Remove('It:Skip')
    }

    BeforeEach {
        if ($null -ne $PSStyle) {
            $oldOutputRendering = $PSStyle.OutputRendering
        }
    }

    AfterEach {
        if ($null -ne $PSStyle) {
            $PSStyle.OutputRendering = $oldOutputRendering
        }

        $ps.Commands.Clear()
    }

    It 'OutputRendering works for "<outputRendering>" to the host' -TestCases @(
        @{ outputRendering = 'automatic'; ansi = $true }
        @{ outputRendering = 'host'     ; ansi = $true }
        @{ outputRendering = 'ansi'     ; ansi = $true }
        @{ outputRendering = 'plaintext'; ansi = $false }
    ) {
        param($outputRendering, $ansi)

        $out = pwsh -noprofile -command "[System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('BypassOutputRedirectionCheck', `$true); `$PSStyle.OutputRendering = '$outputRendering'; write-host '$($PSStyle.Foreground.Green)hello'"

        if ($ansi) {
            $out | Should -BeLike "*`e*" -Because ($out | Format-Hex | Out-String)
        }
        else {
            $out | Should -Not -BeLike "*`e*" -Because ($out | Format-Hex | Out-String)
        }
    }

    It 'OutputRendering works for "<outputRendering>" to the pipeline' -TestCases @(
        @{ outputRendering = 'automatic'; ansi = $true }
        @{ outputRendering = 'host'     ; ansi = $false }
        @{ outputRendering = 'ansi'     ; ansi = $true }
        @{ outputRendering = 'plaintext'; ansi = $false }
    ) {
        param($outputRendering, $ansi)

        $PSStyle.OutputRendering = $outputRendering
        $out = "$($PSStyle.Foreground.Green)hello" | Out-String

        if ($ansi) {
            $out | Should -BeLike "*`e*" -Because ($out | Format-Hex | Out-String)
        }
        else {
            $out | Should -Not -BeLike "*`e*" -Because ($out | Format-Hex | Out-String)
        }
    }
}
