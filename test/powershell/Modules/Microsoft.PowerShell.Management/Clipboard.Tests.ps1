# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Clipboard cmdlet tests' -Tag CI {
    BeforeAll {
        $xclip = Get-Command xclip -CommandType Application -ErrorAction Ignore
    }

    Context 'Text' {
        BeforeAll {
            $defaultParamValues = $PSdefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = ($IsWindows -and $env:PROCESSOR_ARCHITECTURE.Contains("arm")) -or ($IsLinux -and $xclip -eq $null)
        }

        AfterAll {
            $PSDefaultParameterValues = $defaultParamValues
        }

        It 'Get-Clipboard returns what is in Set-Clipboard' {
            $guid = New-Guid
            Set-Clipboard -Value $guid
            Get-Clipboard | Should -BeExactly $guid
            Get-Clipboard -Format Text | Should -BeExactly $guid
            Get-Clipboard -TextFormatType UnicodeText | Should -BeExactly $guid
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

        It 'Set-Clipboard -Append will add text' {
            'hello' | Set-Clipboard
            'world' | Set-Clipboard -Append
            Get-Clipboard -Raw | Should -BeExactly "hello$([Environment]::NewLine)world"
        }
    }

    Context 'Windows' {
        BeforeAll {
            $defaultParamValues = $PSdefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = !$IsWindows -or $env:PROCESSOR_ARCHITECTURE.Contains("arm")
        }

        AfterAll {
            $PSDefaultParameterValues = $defaultParamValues
        }

        It 'Html works' {
            $expected = @"
Version:0.9
StartHTML:000000149
EndHTML:000000286
StartFragment:000000249
EndFragment:000000254
StartSelection:000000249
EndSelection:000000254
<!DOCTYPE HTML  PUBLIC "-//W3C//DTD HTML 4.0  Transitional//EN">
<html><body><!--StartFragment-->hello<!--EndFragment--></body></html>`0
"@

            'hello' | Set-Clipboard -AsHtml
            Get-Clipboard | Should -BeNullOrEmpty
            Get-Clipboard -TextFormatType Html -Raw | Should -BeExactly $expected
        }

        It 'FileInfo works' {
            $item = Get-Item ~
            $item | Should -Not -BeNullOrEmpty
            $item | Set-Clipboard
            Get-Clipboard | Should -BeNullOrEmpty
            $out = Get-Clipboard -Format FileDropList
            $out | Should -Not -BeNullOrEmpty
            Compare-Object -ReferenceObject $item -DifferenceObject $out | Should -BeNullOrEmpty
        }

        It '-Path works' {
            Set-Clipboard -Path ~
            Get-Clipboard | Should -BeNullOrEmpty
            $out = Get-Clipboard -Format FileDropList
            $out.FullName | Should -BeExactly (Resolve-Path -Path ~).Path
        }

        It '-LiteralPath works' {
            Set-Clipboard -LiteralPath $TestDrive
            Get-Clipboard | Should -BeNullOrEmpty
            $out = Get-Clipboard -Format FileDropList
            $out.FullName | Should -BeExactly $TestDrive.FullName
        }
    }

    Context 'Unix' {
        BeforeAll {
            $defaultParamValues = $PSdefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = $IsWindows -or ($IsLinux -and $xclip -eq $null)
        }

        AfterAll {
            $PSDefaultParameterValues = $defaultParamValues
        }

        It '-TextFormatType <format>: returns error' -TestCases @(
            @{ format = 'Text' }
            @{ format = 'Rtf' }
            @{ format = 'Html' }
            @{ format = 'CommaSeparatedValue' }
        ){
            param ($format)

            { Get-Clipboard -TextFormatType $format } | Should -Throw -ErrorId 'FailedToGetClipboardUnsupportedTextFormat,Microsoft.PowerShell.Commands.GetClipboardCommand'
        }

        It '-AsHtml returns error' {
            { 'hello' | Set-Clipboard -AsHtml } | Should -Throw -ErrorId 'FailedToSetClipboard,Microsoft.PowerShell.Commands.SetClipboardCommand'
        }

        It '-Format <format>: returns error' -TestCases @(
            @{ format = 'Audio' }
            @{ format = 'FileDropList' }
            @{ format = 'Image' }
        ){
            param ($format)

            { Get-Clipboard -Format $format } | Should -Throw -ErrorId 'FailedToGetClipboardUnsupportedFormat,Microsoft.PowerShell.Commands.GetClipboardCommand'
        }
    }
}
