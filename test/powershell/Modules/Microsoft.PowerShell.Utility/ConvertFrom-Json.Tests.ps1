Describe 'ConvertFrom-Json' -tags "CI" {
    It 'can convert a single-line object' {
        ('{"a" : "1"}' | ConvertFrom-Json).a | Should Be 1
    }

    It 'can convert one string-per-object' {
        $json = @('{"a" : "1"}', '{"a" : "x"}') | ConvertFrom-Json
        $json.Count | Should Be 2
        $json[1].a | Should Be 'x'
    }

    It 'can convert multi-line object' {
        $json = @('{"a" :', '"x"}') | ConvertFrom-Json
        $json.a | Should Be 'x'
    }
}
