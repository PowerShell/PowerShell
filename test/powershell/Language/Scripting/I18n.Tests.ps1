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

        $mydata.string1 | Should Be 'string1'
        $mydata.string2 | Should Be 'string2'
    }

    It 'Import default culture is done correctly' {

        import-localizedData mydata;

        $mydata.string1 | Should Be 'string1 for en-US'
        $mydata.string2 | Should be 'string2 for en-US'
    }

    It 'Import specific culture(en-US)' {

        import-localizedData mydata -uiculture en-US

        $mydata.string1 | Should Be 'string1 for en-US'
        $mydata.string2 | Should Be 'string2 for en-US'

        import-localizedData mydata -uiculture fr-FR

        $mydata.string1 | Should Be 'string1 for fr-FR'
        $mydata.string2 | Should Be 'string2 for fr-FR'
    }

    It 'Import non existing culture is done correctly' {
    
        import-localizedData mydata -uiculture nl-NL -ea SilentlyContinue -ev ev

        $ev[0].Exception.GetType() | Should Be System.Management.Automation.PSInvalidOperationException
    }

    It 'Import different file name is done correctly' {

        import-localizedData mydata -filename foo

        $mydata.string1 | Should Be 'string1 from foo in en-US'
        $mydata.string2 | Should Be 'string2 from foo in en-US'

        import-localizedData mydata -filename foo -uiculture fr-FR

        $mydata.string1 | Should Be 'string1 from foo in fr-FR'
        $mydata.string2 | Should Be 'string2 from foo in fr-FR'
    }

    It 'Import different file base is done correctly' {
    
        import-localizedData mydata -basedirectory "${dir}\newbase"

        $mydata.string1 | Should Be 'string1 for en-US under newbase'
        $mydata.string2 | Should Be 'string2 for en-US under newbase'

        import-localizedData mydata -basedirectory "${dir}\newbase" -uiculture fr-FR

        $mydata.string1 | Should Be 'string1 for fr-FR under newbase'
        $mydata.string2 | Should Be 'string2 for fr-FR under newbase'
    }

    It 'Import different file base and file name' {
    
        import-localizedData mydata -basedirectory "${dir}\newbase" -filename foo

        $mydata.string1 | Should Be 'string1 for en-US from foo under newbase'
        $mydata.string2 | Should Be 'string2 for en-US from foo under newbase'

        import-localizedData mydata -basedirectory "${dir}\newbase" -filename foo -uiculture fr-FR

        $mydata.string1 | Should Be 'string1 for fr-FR from foo under newbase'
        $mydata.string2 | Should Be 'string2 for fr-FR from foo under newbase'
        }

    It "Import variable that doesn't exist" {    

        import-localizedData mydata2 

        $mydata2.string1 | Should Be 'string1 for en-US'
        $mydata2.string2 | Should Be 'string2 for en-US'
    }

    It 'Import bad psd1 file - tests the use of disallowed variables' {
    
        $script:exception = $null
        & { 
            trap {$script:exception = $_ ; continue }
            import-localizedData mydata -filename bad
          } 

        $script:exception.exception.gettype() | Should Be System.management.automation.psinvalidoperationexception
        }

    It 'Import if psd1 file is done correctly' {
    
        import-localizedData mydata -filename if

        if ($psculture -eq 'en-US')
        {    
            $mydata.string1 | Should Be 'string1 for en-US in if'
            $mydata.string2 | Should Be 'string2 for en-US in if'
        }
        else
        {
            $mydata | Should Be $null
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
            invoke-expression $cmd 
        }       
        
        $exception | Should Match $Expected
    }


    it 'Check alternate syntax that also supports complex variable names' {
    
       & {
            $script:mydata = data { 123 }
         }
        $mydata | Should Be 123

        $mydata = data { 456 }
        & {
            # This import should not clobber the one at script scope
            import-localizedData mydata -uiculture en-US
        }
        $mydata | Should Be 456

        & {
            # This import should clobber the one at script scope
            import-localizedData script:mydata -uiculture en-US
        }        
        $script:mydata.string1 | Should Be 'string1 for en-US'
    }

    It 'Check fallback to current directory plus -SupportedCommand parameter is done correctly' {
    
        new-alias MyConvertFrom-StringData ConvertFrom-StringData

        import-localizeddata local:mydata -uiculture fr-ca -filename I18n.Tests_fallback.psd1 -SupportedCommand MyConvertFrom-StringData
        $mydata[0].string1 | Should Be 'fallback string1 for en-US'
        $mydata[1] | Should Be 42
    }
}
