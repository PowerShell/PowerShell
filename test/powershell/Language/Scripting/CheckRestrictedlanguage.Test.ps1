#  <Test>
#    <summary>Test restricted language check method on scriptblocks</summary>
#  </Test>
Describe "Test restricted language check method on scriptblocks" -Tags "CI" {
        BeforeAll {
            set-strictmode -v 2
        }

        function list {

            $l = new-object system.collections.generic.list[string]
            $args | foreach {$l.Add($_)}
            , $l
        }

        It 'Check basic expressions' {

            {2+2}.CheckRestrictedLanguage($null, $null, $false)  # Succeed with no variables
        }
        
        It 'Check default variables' {

            {$PSCulture, $PSUICulture, $true, $false, $null}.CheckRestrictedLanguage($null, $null, $false)
        }

        It 'Check default variables' {

            $failed = $true
            try
            {
                {2+$a}.CheckRestrictedLanguage($null, $null, $false)
                $failed = $false
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be 'ParseException'
            }

            $failed | Should be $true
        }

        It 'Check union of default + one allowed variables' {

            { 2 + $a }.CheckRestrictedLanguage($null, (list a), $false)   # succeed
        }

        It 'Check union of default + two allowed variables' {

            { $a + $b }.CheckRestrictedLanguage($null, (list a b), $false)   # succeed
        }

        It 'Check union of default + allowed variables' {

            { $PSCulture, $PSUICulture, $true, $false, $null,  $a, $b}.CheckRestrictedLanguage($null, (list a b), $false)
        }

        It 'Check union of default + one disallowed variables' {

            $failed = $true
            try
            {
                { $a + $b + $c }.CheckRestrictedLanguage($null, (list a b), $false)   # fail
                $failed = $false
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be 'ParseException'
            }
            $failed | Should be $true
        }

        It 'Check union of default + one allowed variable and but not allow evironment variable' {

            $failed = $true
            try
            {
                { 2 + $a + $env:foo }.CheckRestrictedLanguage($null, (list a), $false)   # fail
                $failed = $false
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be 'ParseException'
            }
            $failed | Should be $true
        }

        It 'Check union of default + one allowed varialbe name and allow environment variable ' {
                    
            { 2 + $a + $env:foo }.CheckRestrictedLanguage($null, (list a), $true)   # succeed
        }

        It 'Check that wildcard allows env even if the flag is set to false' {

            { 2 + $a + $b + $c + $env:foo }.CheckRestrictedLanguage($null, (list *), $false)   # succeed
        }

        It 'Check for restricted commands' {
        
            $failed = $true
            try
            {
                {get-date}.CheckRestrictedLangauge($null, $null, $false)
                $failed = $false
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be 'MethodNotFound'
            }
            $failed | Should be $true
        }

        It 'Check for allowed commands and variables' {

            { get-process | where name -match $pattern | foreach $prop }.CheckRestrictedLanguage(
                (list get-process where foreach),
                (list prop pattern)
                , $false)
        }

}
