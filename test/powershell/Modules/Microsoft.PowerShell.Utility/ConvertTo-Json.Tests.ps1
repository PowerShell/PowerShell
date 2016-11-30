Describe "ConvertTo-Json" -tags "CI" {
    It "Should indent by two spaces" {
        (@{ a = 1 } | ConvertTo-Json)[6] | Should Be 'a'
        (1, 2 | ConvertTo-Json)[5] | Should Be '1'
    }

    It "Should have one space after colons" {
        (@{ a = 1 } | ConvertTo-Json)[10] | Should Be '1'
    }
    
    It "Should minify Json with Compress switch" {
        (@{ a = 1 } | ConvertTo-Json -Compress).Length | Should Be 7
    }
}
