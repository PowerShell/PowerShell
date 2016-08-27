#This works only for en-US or fr-FR
if ($PSUICulture -ne 'en-US' -and $PSUICulture -ne 'fr-FR')
{
    return
}


Describe 'Testing of script internationalization' -Tags "CI" {
    BeforeAll {
        $dir=$PSScriptRoot
        }
    

    Context 'converFromString-Data.' {

        data mydata
        {
            ConvertFrom-StringData @'
        string1=string1
        string2=string2
'@
        }

        It '$mydata.string1' { $mydata.string1 | Should Be 'string1' }
        It '$mydata.string2' { $mydata.string2 | Should Be 'string2' }
    }

    Context 'Import default culture' {

        import-localizedData mydata;

        It '$mydata.string1' { $mydata.string1 | Should Be 'string1 for en-US' }
        It '$mydata.string2' { $mydata.string2 | Should be 'string2 for en-US' }
    }

    Context 'Import specific culture' {

        import-localizedData mydata -uiculture en-US

        It '$mydata.string1' { $mydata.string1 | Should Be 'string1 for en-US' }
        It '$mydata.string2' { $mydata.string2 | Should Be 'string2 for en-US' }

        import-localizedData mydata -uiculture fr-FR

        It '$mydata.string1' { $mydata.string1 | Should Be 'string1 for fr-FR' }
        It '$mydata.string2' { $mydata.string2 | Should Be 'string2 for fr-FR' }
    }

    It 'Import non existing culture' {
    
        import-localizedData mydata -uiculture nl-NL -ea SilentlyContinue -ev ev

        $ev[0].Exception.GetType() | Should Be System.Management.Automation.PSInvalidOperationException
    }

    Context 'Import different file name' {

        import-localizedData mydata -filename foo

        It '$mydata.string1' { $mydata.string1 | Should Be 'string1 from foo in en-US' }
        It '$mydata.string2' { $mydata.string2 | Should Be 'string2 from foo in en-US' }

        import-localizedData mydata -filename foo -uiculture fr-FR

        It '$mydata.string1' { $mydata.string1 | Should Be 'string1 from foo in fr-FR' }
        It '$mydata.string2' { $mydata.string2 | Should Be 'string2 from foo in fr-FR' }
    }

    Context 'Import different file base' {
    
        import-localizedData mydata -basedirectory "${dir}\newbase"

        It '$mydata.string1' { $mydata.string1 | Should Be 'string1 for en-US under newbase' }
        It '$mydata.string2' { $mydata.string2 | Should Be 'string2 for en-US under newbase' }

        import-localizedData mydata -basedirectory "${dir}\newbase" -uiculture fr-FR

        It '$mydata.string1' { $mydata.string1 | Should Be 'string1 for fr-FR under newbase' }
        It '$mydata.string2' { $mydata.string2 | Should Be 'string2 for fr-FR under newbase' }
    }

    Context 'Import different file base and file name' {
    
        import-localizedData mydata -basedirectory "${dir}\newbase" -filename foo

        It '$mydata.string1' { $mydata.string1 | Should Be 'string1 for en-US from foo under newbase' }
        It '$mydata.string2' { $mydata.string2 | Should Be 'string2 for en-US from foo under newbase' }

        import-localizedData mydata -basedirectory "${dir}\newbase" -filename foo -uiculture fr-FR

        It '$mydata.string1' { $mydata.string1 | Should Be 'string1 for fr-FR from foo under newbase' }
        It '$mydata.string2' { $mydata.string2 | Should Be 'string2 for fr-FR from foo under newbase' }
        }

    Context "Import variable that doesn't exist" {    

        import-localizedData mydata2 

        It '$mydata.string1' { $mydata2.string1 | Should Be 'string1 for en-US' }
        It '$mydata.string2' { $mydata2.string2 | Should Be 'string2 for en-US' }
    }

    It 'Import bad psd1 file - tests the use of disallowed variables' {
    
        $script:exception = $null
        & { 
            trap {$script:exception = $_ ; continue }
            import-localizedData mydata -filename bad
          } 

        $script:exception.exception.gettype() | Should Be System.management.automation.psinvalidoperationexception
        }

    Context 'Import if psd1 file' {
    
        import-localizedData mydata -filename if

        if ($psculture -eq 'en-US')
        {    
            It '$mydata.string1' { $mydata.string1 | Should Be 'string1 for en-US in if' }
            It '$mydata.string2' { $mydata.string2 | Should Be 'string2 for en-US in if' }
        }
        else
        {
            It '$mydata should not be null' { $mydata | Should Be $null }
        }
    }

    $testData = @(
        @{cmd = 'data d { @{ x=$(get-command)} }';Expected='*get-command*'},
        @{cmd = 'data d { if ($(get-command)) {} }';Expected='*get-command*'},
        @{cmd = 'data d { @(get-command) }';Expected='*get-command*'}
        )

    It 'Allowed cmdlets checked properly' -TestCase:$testData {
        param ($cmd, $Expected)

        $script:exception = $null
        & {
            trap {$script:exception = $_.Exception ; continue }            
            iex $cmd
        }
        #$exception -like '*get-command*' | Should Be $true
        $exception -like $Expected | Should Be $true
    }


    Context 'Check alternate syntax that also supports complex variable names' {
    
       & {
        $script:mydata = data { 123 }
        }
        It '$mydata' { $mydata | Should Be 123 }

        $mydata = data { 456 }
        & {
            # This import should not clobber the one at script scope
            import-localizedData mydata -uiculture en-US
        }
        It '$mydata' { $mydata | Should Be 456 }

        & {
            # This import should clobber the one at script scope
            import-localizedData script:mydata -uiculture en-US
        }        
        It '$script:mydata.string1' { $script:mydata.string1 | Should Be 'string1 for en-US'}
    }


    Context 'Check fallback to current directory plus -SupportedCommand parameter' {
    
        new-alias MyConvertFrom-StringData ConvertFrom-StringData

        import-localizeddata local:mydata -uiculture fr-ca -filename I18n.Tests_fallback.psd1 -SupportedCommand MyConvertFrom-StringData
        It '$mydata[0].string1' { $mydata[0].string1 | Should Be 'fallback string1 for en-US' }
        It '$mydata[1]' { $mydata[1] | Should Be 42 }
    }
}
