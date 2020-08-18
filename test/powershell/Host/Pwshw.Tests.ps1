# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'PowerShell WinExe tests' -Tags CI {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if ( ! $IsWindows ) {
            $PSDefaultParameterValues["it:skip"] = $true
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It 'pwshw.exe works' {
        pwshw -noprofile -command "'hello' > $TestDrive\out.txt"
        # since pwshw runs async, we wait til it's gone
        Wait-UntilTrue -sb { $null -eq (Get-Process pwshw) }

        "$TestDrive\out.txt" | Should -FileContentMatch 'hello'
    }

    It 'pwshw.exe is non-interactive' {
        pwshw -outputlog $TestDrive\test.log -interactive -noprofile -command "read-host"
        # since pwshw runs async, we wait til it's gone
        Wait-UntilTrue -sb { $null -eq (Get-Process pwshw) }

        "$TestDrive\test.log" | Should -FileContentMatch 'NonInteractive'
    }
}
