Describe "Test restricted language check method on scriptblocks" -Tags "CI" {
        BeforeAll {
            set-strictmode -v 2
            function list {

            $l = [System.Collections.Generic.List[String]]::new()
            $args | foreach {$l.Add($_)}
            , $l
            }
        }
        AfterAll {
            Set-StrictMode -Off
        }

        It 'Check basic expressions' {

            {{2+2}.CheckRestrictedLanguage($null, $null, $false) } | Should Not Throw  # Succeed with no variables
        }
        
        It 'Check default variables' {

            {{ $PSCulture, $PSUICulture, $true, $false, $null}.CheckRestrictedLanguage($null, $null, $false) } | Should Not Throw
        }

        It 'Check default variables' {
            
            try
            {
                {2+$a}.CheckRestrictedLanguage($null, $null, $false)
                Throw "Exception expected, execution should not have reached here"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be 'ParseException'
            }
            
        }

        It 'Check union of default + one allowed variables' {

            { { 2 + $a }.CheckRestrictedLanguage($null, (list a), $false) }| Should Not Throw  # succeed
        }

        It 'Check union of default + two allowed variables' {

            { { $a + $b }.CheckRestrictedLanguage($null, (list a b), $false) } | Should Not Throw  # succeed
        }

        It 'Check union of default + allowed variables' {

            { { $PSCulture, $PSUICulture, $true, $false, $null,  $a, $b}.CheckRestrictedLanguage($null, (list a b), $false) }| Should Not Throw
        }

        It 'Check union of default + one disallowed variables' {

            try
            {
                { $a + $b + $c }.CheckRestrictedLanguage($null, (list a b), $false)   # fail
                Throw "Exception expected, execution should not have reached here"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be 'ParseException'
            }
        }

        It 'Check union of default + one allowed variable and but not allow environment variable' {

            try
            {
                { 2 + $a + $env:foo }.CheckRestrictedLanguage($null, (list a), $false)   # fail
                Throw "Exception expected, execution should not have reached here"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be 'ParseException'
            }
        }

        It 'Check union of default + one allowed variable name and allow environment variable ' {
                    
            {{ 2 + $a + $env:foo }.CheckRestrictedLanguage($null, (list a), $true)}   | Should Not Throw # succeed
        }

        It 'Check that wildcard allows env even if the flag is set to false' {

            { { 2 + $a + $b + $c + $env:foo }.CheckRestrictedLanguage($null, (list *), $false)} | Should Not Throw   # succeed
        }

        It 'Check for restricted commands' {
        
            try
            {
                {get-date}.CheckRestrictedLangauge($null, $null, $false)
                Throw "Exception expected, execution should not have reached here"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be 'MethodNotFound'
            }
        }

        It 'Check for allowed commands and variables' {

            { { get-process | where name -match $pattern | foreach $prop }.CheckRestrictedLanguage(
                (list get-process where foreach),
                (list prop pattern)
                , $false) }| Should Not Throw
        }
}
