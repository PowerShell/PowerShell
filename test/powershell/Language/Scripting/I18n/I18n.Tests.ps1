# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'Testing of script internationalization' -Tags 'CI' {
    BeforeAll {
        $testCultures = @(
            @{ UICulture = 'en-US' }
            @{ UICulture = 'fr-FR' }
        )

        $currentCulture = $PSUICulture
        [System.Globalization.CultureInfo]::CurrentUICulture = 'en-US'

        $defaultParams = @{
            BindingVariable = 'data'
        }
    }

    BeforeEach {
        Get-Variable -Name data -Scope Local -ErrorAction Ignore | Remove-Variable
    }

    AfterAll {
        [System.Globalization.CultureInfo]::CurrentUICulture = $currentCulture
    }

    Context 'Data section' {
        It 'ConvertFrom-StringData is permitted in a Data section' {
            data dataVariable
            {
                ConvertFrom-StringData @'
string1=string1
string2=string2
'@
            }

            $dataVariable.string1 | Should -BeExactly 'string1'
            $dataVariable.string2 | Should -BeExactly 'string2'
        }

        It 'Throws an error if the data section contains a command which is not allowed' -TestCases @(
            @{ Script = 'data d { @{ x=$(Get-Command)} }'; }
            @{ Script = 'data d { if ($(Get-Command)) {} }' }
            @{ Script = 'data d { @(Get-Command) }' }
        ) {
            param ( $Script )

            { Invoke-Expression $Script } | Should -Throw -ErrorId 'CmdletNotInAllowedListForDataSection,Microsoft.PowerShell.Commands.InvokeExpressionCommand'
        }
    }

    Context 'BindingVariable parameter' {
        It 'BindingVariable binds positionally' {
            Import-LocalizedData data

            $data.string1 | Should -BeExactly 'string1 en-US'
            $data.string2 | Should -BeExactly 'string2 en-US'
        }

        It 'Imports data into the BindingVariable based on the current UICulture' {
            Import-LocalizedData @defaultParams

            $data.string1 | Should -BeExactly 'string1 en-US'
            $data.string2 | Should -BeExactly 'string2 en-US'
        }

        It 'Does not clobber existing variables in a parent scope' {
            $data = data { 456 }
            & {
                 Import-LocalizedData @defaultParams
             }
             $data | Should -Be 456
        }

        It 'Replaces a BindingVariable in a parent scope if a scope modifier is specified' {
            $Script:bindingVariable = data { 456 }

            $Script:bindingVariable | Should -Be 456

            & {
                Import-LocalizedData -BindingVariable Script:bindingVariable
            }

            $Script:bindingVariable.string1 | Should -BeExactly 'string1 en-US'
        }
    }

    Context 'UICulture parameter' {
        It 'Imports specific culture (<UICulture>) defined by the UICulture parameter' -TestCases $testCultures {
            param ( $UICulture )

            [System.Globalization.CultureInfo]::CurrentUICulture = $UICulture

            Import-LocalizedData @defaultParams -UICulture $UICulture

            $data.string1 | Should -BeExactly ('string1 {0}' -f $UICulture)
            $data.string2 | Should -BeExactly ('string2 {0}' -f $UICulture)
        }

        It 'Throws an error if the specified UICulture does not exist' -Skip:(-not $IsWindows) {
            { Import-LocalizedData @defaultParams -UICulture none-none -ErrorAction Stop } |
                Should -Throw -ExceptionType 'System.Management.Automation.PSArgumentException'
        }
    }

    Context 'UICulture fallback' {
        It 'When the UICulture parameter is specified, searches parent culture and current directory' -TestCases @(
            @{ UICulture = 'en-US'; ExpectedString = 'en-US' }
            @{ UICulture = 'en-GB'; ExpectedString = 'en' }
            @{ UICulture = 'no-NL'; ExpectedString = 'fallback' }
        ) {
            param ( $UICulture, $ExpectedString )

            if ($UICulture -eq 'no-NL' -and (Test-IsWinServer2012R2))
            {
                Set-ItResult -Skipped -Because 'no-NL culture is not available on Windows Server 2012 R2'
                return
            }

            [System.Globalization.CultureInfo]::CurrentUICulture = $UICulture

            $data = Import-LocalizedData -UICulture $UICulture

            $data.string1 | Should -Be ('string1 {0}' -f $ExpectedString)
            $data.string2 | Should -Be ('string2 {0}' -f $ExpectedString)
        }

        It 'When the UICulture parameter is not specified and no files exist falls back on en-US and parent cultures' -TestCases @(
            @{ UICulture = 'no-NL'; ExpectedString = 'en-US' }
        ) {
            param ( $UICulture, $ExpectedString )

            if ($UICulture -eq 'no-NL' -and (Test-IsWinServer2012R2))
            {
                Set-ItResult -Skipped -Because 'no-NL culture is not available on Windows Server 2012 R2'
                return
            }

            [System.Globalization.CultureInfo]::CurrentUICulture = $UICulture

            $data = Import-LocalizedData -FileName 'I18n_altfilename'

            $data.string1 | Should -Be ('string1 {0} I18n_altfilename' -f $ExpectedString)
            $data.string2 | Should -Be ('string2 {0} I18n_altfilename' -f $ExpectedString)
        }
    }

    Context 'FileName and BaseDirectory parameters' {
        BeforeAll {
            $fileName = @{ FileName = 'I18n_altfilename' }
            $baseDirectory = @{ BaseDirectory = Join-Path -Path $PSScriptRoot -ChildPath 'I18n_altbase' }
        }

        It 'Imports from the "foo" file name when then FileName parameter is specified (<UICulture>)' -TestCases $testCultures {
            param ( $UICulture )

            [System.Globalization.CultureInfo]::CurrentUICulture = $UICulture

            Import-LocalizedData @defaultParams @fileName -UICulture $UICulture

            $data.string1 | Should -BeExactly ('string1 {0} I18n_altfilename' -f $UICulture)
            $data.string2 | Should -BeExactly ('string2 {0} I18n_altfilename' -f $UICulture)
        }

        It 'Imports from the "I18n_altfilename" directory when the BaseDirectory parameter is specified (<UICulture>)' -TestCases $testCultures {
            param ( $UICulture )

            [System.Globalization.CultureInfo]::CurrentUICulture = $UICulture

            Import-LocalizedData @defaultParams @baseDirectory -UICulture $UICulture

            $data.string1 | Should -BeExactly ('string1 {0} I18n_altbase' -f $UICulture)
            $data.string2 | Should -BeExactly ('string2 {0} I18n_altbase' -f $UICulture)
        }

        It 'Imports from the "I18n_altfilename" file name and "I18n_altbase" directory when both FileName and BaseDirectory are specified (<UICulture>)' -TestCases $testCultures {
            param ( $UICulture )

            [System.Globalization.CultureInfo]::CurrentUICulture = $UICulture

            Import-LocalizedData @defaultParams @fileName @baseDirectory -UICulture $UICulture

            $data.string1 | Should -BeExactly ('string1 {0} I18n_altbase I18n_altfilename' -f $UICulture)
            $data.string2 | Should -BeExactly ('string2 {0} I18n_altbase I18n_altfilename' -f $UICulture)
        }

        It 'Throws an error if the specified FileName does not exist in any path' {
            { Import-LocalizedData @defaultParams -FileName doesNotExist -ErrorAction Stop } |
                Should -Throw -ExceptionType 'System.Management.Automation.PSInvalidOperationException'
        }

        It 'Throws an error if the specified BaseDirectory does not exist' {
            { Import-LocalizedData @defaultParams -BaseDirectory "$PSScriptRoot\doesNotExist" -ErrorAction Stop } |
                Should -Throw -ExceptionType 'System.Management.Automation.PSInvalidOperationException'
        }
    }

    Context 'SupportedCommand parameter' {
        It 'Allows non-standard commands to be used in a data file' {
            $data = Import-LocalizedData -FileName I18n_supportedcommands -SupportedCommand Get-Command

            $data.Name | Should -Be 'Import-LocalizedData'
        }
    }
}
