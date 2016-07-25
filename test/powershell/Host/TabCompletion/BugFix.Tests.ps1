Describe "Issue#682 - [system.manage<tab>] should work" -Tags "CI" {
    
    It "[system.manage<tab>" {
        $result = TabExpansion2 -inputScript "[system.manage" -cursorColumn "[system.manage".Length
        $result | Should Not BeNullOrEmpty
        $result.CompletionMatches.Count | Should Be 1
        $result.CompletionMatches[0].CompletionText | Should Be "System.Management"
    }
}
