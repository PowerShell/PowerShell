# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'OutputRendering tests' -Tag 'CI' {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        # Console host does not support VT100 escape sequences on Windows 2012R2 or earlier

        if (-not $host.ui.SupportsVirtualTerminal) {
            $PSDefaultParameterValues["it:skip"] = $true
        }
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
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It 'OutputRendering works for "<outputRendering>" to the host' -TestCases @(
        @{ outputRendering = 'host'     ; ansi = $true }
        @{ outputRendering = 'ansi'     ; ansi = $true }
        @{ outputRendering = 'plaintext'; ansi = $false }
    ) {
        param($outputRendering, $ansi)

        $out = pwsh -noprofile -command "`$PSStyle.OutputRendering = '$outputRendering'; write-host '$($PSStyle.Foreground.Green)hello'"

        if ($ansi) {
            $out | Should -BeLike "*`e*" -Because ($out | Format-Hex | Out-String)
        }
        else {
            $out | Should -Not -BeLike "*`e*" -Because ($out | Format-Hex | Out-String)
        }
    }

    It 'OutputRendering works for "<outputRendering>" to the pipeline' -TestCases @(
        @{ outputRendering = 'host'     ; ansi = $false }
        @{ outputRendering = 'ansi'     ; ansi = $true }
        @{ outputRendering = 'plaintext'; ansi = $false }
    ) {
        param($outputRendering, $ansi)

        $PSStyle.OutputRendering = $outputRendering
        $out = [pscustomobject] @{ key = "$($PSStyle.Foreground.Green)hello" } | Out-String

        if ($ansi) {
            $out | Should -BeLike "*`e*" -Because ($out | Format-Hex | Out-String)
        }
        else {
            $out | Should -Not -BeLike "*`e*" -Because ($out | Format-Hex | Out-String)
        }
    }

    # Error isn't covered here because it has custom formatting
    It 'OutputRendering is correct for <stream>' -TestCases @(
        @{ stream = 'Verbose' }
        @{ stream = 'Debug' }
        @{ stream = 'Warning' }
    ) {
        param($stream)

        if ($stream -ne 'Warning')
        {
            $switch = "-$stream"
        }

        $out = pwsh -noprofile -command "write-$stream $switch 'hello';'bye'"
        $out.Count | Should -Be 2
        $out[0] | Should -BeExactly "$($PSStyle.Formatting.$stream)$($stream.ToUpper()): hello$($PSStyle.Reset)" -Because ($out[0] | Out-String | Format-hex)
        $out[1] | Should -BeExactly "bye"
    }

    It 'ToString(OutputRendering) works correctly' {
        $s = [System.Management.Automation.Internal.StringDecorated]::new($PSStyle.Foreground.Red + "Hello`e[m.")
        $s.IsDecorated | Should -BeTrue
        $s.ToString() | Should -BeExactly "$($PSStyle.Foreground.Red)Hello`e[m."
        $s.ToString([System.Management.Automation.OutputRendering]::ANSI) | Should -BeExactly "$($PSStyle.Foreground.Red)Hello`e[m."
        $s.ToString([System.Management.Automation.OutputRendering]::PlainText) | Should -BeExactly 'Hello.'
        { $s.ToString([System.Management.Automation.OutputRendering]::Host) } | Should -Throw -ErrorId 'ArgumentException'
    }
}
