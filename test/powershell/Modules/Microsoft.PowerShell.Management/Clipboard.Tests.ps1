# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Clipboard cmdlet tests' -Tag CI {
    BeforeAll {
        $xclip = Get-Command xclip -CommandType Application -ErrorAction Ignore
        $wlcopy = Get-Command wl-copy -CommandType Application -ErrorAction Ignore
        $wlpaste = Get-Command wl-paste -CommandType Application -ErrorAction Ignore
    }

    Context 'Text' {
        BeforeAll {
            $defaultParamValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = ($IsWindows -and $env:PROCESSOR_ARCHITECTURE.Contains("arm")) -or ($IsLinux -and ($xclip -eq $null) -and ($wlcopy -eq $null) -and ($wlpaste -eq $null) )
        }

        AfterAll {
            $global:PSDefaultParameterValues = $defaultParamValues
        }

        It 'Get-Clipboard returns what is in Set-Clipboard' {
            $guid = New-Guid
            Set-Clipboard -Value $guid
            Get-Clipboard | Should -BeExactly $guid
        }

        It 'Get-Clipboard returns an array' {
            1,2 | Set-Clipboard
            $out = Get-Clipboard
            $out.Count | Should -Be 2
            $out[0] | Should -Be 1
            $out[1] | Should -Be 2
        }

        It 'Get-Clipboard -Raw returns one item' {
            1,2 | Set-Clipboard
            (Get-Clipboard -Raw).Count | Should -Be 1
            Get-Clipboard -Raw | Should -BeExactly "1$([Environment]::NewLine)2"
        }

        It 'Get-Clipboard -Delimiter should return items based on the delimiter' {
            Set-Clipboard -Value "Line1`r`nLine2`nLine3"
            $result = Get-Clipboard -Delimiter "`r`n", "`n"
            $result.Count | Should -Be 3
            $result | ForEach-Object -Process {$_.Length | Should -Be "LineX".Length}
        }

        It 'Set-Clipboard -Append will add text' {
            'hello' | Set-Clipboard
            'world' | Set-Clipboard -Append
            Get-Clipboard -Raw | Should -BeExactly "hello$([Environment]::NewLine)world"
        }

        It 'Set-Clipboard accepts <value> string' -TestCases @(
            @{ value = 'empty'; text = "" }
            @{ value = 'null' ; text = $null }
        ){
            param ($text)

            $text | Set-Clipboard
            Get-Clipboard -Raw | Should -BeNullOrEmpty
        }

        It 'Set-Clipboard should not return object' {
            $result = 'hello' | Set-Clipboard
            $result | Should -BeNullOrEmpty
        }

        It 'Set-Clipboard -PassThru returns single object with -Append = <Append>' -TestCases @(
            @{ Append = $false }
            @{ Append = $true }
        ){
            param ($append)

            $params = @{ PassThru = $true; Append = $append }

            Set-Clipboard -Value 'world'
            $result = 'hello' | Set-Clipboard @params
            $result | Should -BeExactly 'hello'
        }

        It 'Set-Clipboard -PassThru returns multiple objects with -Append = <Append>' -TestCases @(
            @{ Append = $false }
            @{ Append = $true }
        ){
            param ($append)

            $params = @{ PassThru = $true; Append = $append }

            Set-Clipboard -Value 'world'
            $result = 'hello', 'world' | Set-Clipboard @params
            $result | Should -BeExactly @('hello', 'world')
        }
    }
}
