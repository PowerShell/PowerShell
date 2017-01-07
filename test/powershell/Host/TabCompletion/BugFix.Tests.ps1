Describe "Tab completion bug fix" -Tags "CI" {
    
    It "Issue#682 - '[system.manage<tab>' should work" {
        $result = TabExpansion2 -inputScript "[system.manage" -cursorColumn "[system.manage".Length
        $result | Should Not BeNullOrEmpty
        $result.CompletionMatches.Count | Should Be 1
        $result.CompletionMatches[0].CompletionText | Should Be "System.Management"
    }

    It "Issue#1350 - '1 -sp<tab>' should work" {
        $result = TabExpansion2 -inputScript "1 -sp" -cursorColumn "1 -sp".Length
        $result | Should Not BeNullOrEmpty
        $result.CompletionMatches.Count | Should Be 1
        $result.CompletionMatches[0].CompletionText | Should Be "-split"
    }

    It "Issue#1350 - '1 -a<tab>' should work" {
        $result = TabExpansion2 -inputScript "1 -a" -cursorColumn "1 -a".Length
        $result | Should Not BeNullOrEmpty
        $result.CompletionMatches.Count | Should Be 2
        $result.CompletionMatches[0].CompletionText | Should Be "-and"
        $result.CompletionMatches[1].CompletionText | Should Be "-as"
    }
    It "Issue#2295 - '[pscu<tab>' should expand to [pscustomobject]" {
        $result = TabExpansion2 -inputScript "[pscu" -cursorColumn "[pscu".Length
        $result | Should Not BeNullOrEmpty
        $result.CompletionMatches.Count | Should Be 1
        $result.CompletionMatches[0].CompletionText | Should Be "pscustomobject"
    }
}
