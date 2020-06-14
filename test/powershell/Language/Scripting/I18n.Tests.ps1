# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'Testing of script internationalization' -Tags "CI" {
    BeforeAll {
        $dir=$PSScriptRoot
        $defaultParamValues = $PSDefaultParameterValues.Clone()
        #This works only for en-US or fr-FR
        if ($PSUICulture -ne 'en-US' -and $PSUICulture -ne 'fr-FR')
        {
            $PSDefaultParameterValues["It:Skip"] = $true
        }
    }
    AfterAll {
        $global:PSDefaultParameterValues = $defaultParamValues
    }

    It 'convertFromString-Data should work with data statement.' {

        data mydata
        {
            ConvertFrom-StringData @'
        string1=string1
        string2=string2
'@
        }

        $mydata.string1 | Should -BeExactly 'string1'
        $mydata.string2 | Should -BeExactly 'string2'
    }

    It 'Import default culture is done correctly' {

        Import-LocalizedData mydata;

        $mydata.string1 | Should -BeExactly 'string1 for en-US'
        $mydata.string2 | Should -BeExactly 'string2 for en-US'
    }

    It 'Import specific culture(en-US)' {

        Import-LocalizedData mydata -UICulture en-US

        $mydata.string1 | Should -BeExactly 'string1 for en-US'
        $mydata.string2 | Should -BeExactly 'string2 for en-US'

        Import-LocalizedData mydata -UICulture fr-FR

        $mydata.string1 | Should -BeExactly 'string1 for fr-FR'
        $mydata.string2 | Should -BeExactly 'string2 for fr-FR'
    }

    It 'Import non existing culture is done correctly' {

        Import-LocalizedData mydata -UICulture nl-NL -ErrorAction SilentlyContinue -ErrorVariable ev

        $ev | Should -Not -BeNullOrEmpty
        $ev[0].Exception | Should -BeOfType System.Management.Automation.PSInvalidOperationException
    }

    It 'Import different file name is done correctly' {

        Import-LocalizedData mydata -FileName foo

        $mydata.string1 | Should -BeExactly 'string1 from foo in en-US'
        $mydata.string2 | Should -BeExactly 'string2 from foo in en-US'

        Import-LocalizedData mydata -FileName foo -UICulture fr-FR

        $mydata.string1 | Should -BeExactly 'string1 from foo in fr-FR'
        $mydata.string2 | Should -BeExactly 'string2 from foo in fr-FR'
    }

    It 'Import different file base is done correctly' {

        Import-LocalizedData mydata -BaseDirectory "${dir}\newbase"

        $mydata.string1 | Should -BeExactly 'string1 for en-US under newbase'
        $mydata.string2 | Should -BeExactly 'string2 for en-US under newbase'

        Import-LocalizedData mydata -BaseDirectory "${dir}\newbase" -UICulture fr-FR

        $mydata.string1 | Should -BeExactly 'string1 for fr-FR under newbase'
        $mydata.string2 | Should -BeExactly 'string2 for fr-FR under newbase'
    }

    It 'Import different file base and file name' {

        Import-LocalizedData mydata -BaseDirectory "${dir}\newbase" -FileName foo

        $mydata.string1 | Should -BeExactly 'string1 for en-US from foo under newbase'
        $mydata.string2 | Should -BeExactly 'string2 for en-US from foo under newbase'

        Import-LocalizedData mydata -BaseDirectory "${dir}\newbase" -FileName foo -UICulture fr-FR

        $mydata.string1 | Should -BeExactly 'string1 for fr-FR from foo under newbase'
        $mydata.string2 | Should -BeExactly 'string2 for fr-FR from foo under newbase'
        }

    It "Import variable that doesn't exist" {

        Import-LocalizedData mydata2

        $mydata2.string1 | Should -BeExactly 'string1 for en-US'
        $mydata2.string2 | Should -BeExactly 'string2 for en-US'
    }

    It 'Import bad psd1 file - tests the use of disallowed variables' {

        $script:exception = $null
        & {
            trap {$script:exception = $_ ; continue }
            Import-LocalizedData mydata -FileName bad
          }

        $script:exception.exception | Should -Not -BeNullOrEmpty
        $script:exception.exception | Should -BeOfType System.management.automation.psinvalidoperationexception
        }

    It 'Import if psd1 file is done correctly' {

        Import-LocalizedData mydata -FileName if

        if ($PSCulture -eq 'en-US')
        {
            $mydata.string1 | Should -BeExactly 'string1 for en-US in if'
            $mydata.string2 | Should -BeExactly 'string2 for en-US in if'
        }
        else
        {
            $mydata | Should -BeNullOrEmpty
        }
    }

    $testData = @(
        @{cmd = 'data d { @{ x=$(get-command)} }';Expected='get-command'},
        @{cmd = 'data d { if ($(get-command)) {} }';Expected='get-command'},
        @{cmd = 'data d { @(get-command) }';Expected='get-command'}
        )

    It 'Allowed cmdlets checked properly' -TestCase:$testData {
        param ($cmd, $Expected)

        $script:exception = $null
        & {
            trap {$script:exception = $_.Exception ; continue }
            Invoke-Expression $cmd
        }

        $exception | Should -Match $Expected
    }

    It 'Check alternate syntax that also supports complex variable names' {

       & {
            $script:mydata = data { 123 }
         }
        $mydata | Should -Be 123

        $mydata = data { 456 }
        & {
            # This import should not clobber the one at script scope
            Import-LocalizedData mydata -UICulture en-US
        }
        $mydata | Should -Be 456

        & {
            # This import should clobber the one at script scope
            Import-LocalizedData script:mydata -UICulture en-US
        }
        $script:mydata.string1 | Should -BeExactly 'string1 for en-US'
    }

    It 'Check fallback to current directory plus -SupportedCommand parameter is done correctly' {

        New-Alias MyConvertFrom-StringData ConvertFrom-StringData

        Import-LocalizedData local:mydata -UICulture fr-ca -FileName I18n.Tests_fallback.psd1 -SupportedCommand MyConvertFrom-StringData
        $mydata[0].string1 | Should -BeExactly 'fallback string1 for en-US'
        $mydata[1] | Should -Be 42
    }
}
