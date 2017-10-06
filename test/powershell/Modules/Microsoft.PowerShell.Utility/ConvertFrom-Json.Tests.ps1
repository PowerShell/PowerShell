Describe 'ConvertFrom-Json' -tags "CI" {
    
    $testCasesWithAndWithoutAsHashtableSwitch = @(
        @{ AsHashtable = $true  }
        @{ AsHashtable = $false }
    )

    It 'can convert a single-line object with AsHashtable switch set to <AsHashtable>' -TestCase $testCasesWithAndWithoutAsHashtableSwitch {
        Param($AsHashtable)
        ('{"a" : "1"}' | ConvertFrom-Json -AsHashtable:$AsHashtable).a | Should Be 1
    }

    It 'can convert one string-per-object with AsHashtable switch set to <AsHashtable>' -TestCase $testCasesWithAndWithoutAsHashtableSwitch {
        Param($AsHashtable)
        $json = @('{"a" : "1"}', '{"a" : "x"}') | ConvertFrom-Json -AsHashtable:$AsHashtable
        $json.Count | Should Be 2
        $json[1].a | Should Be 'x'
        if ($AsHashtable)
        {
            $json | Should BeOfType Hashtable
        }
    }

    It 'can convert multi-line object with AsHashtable switch set to <AsHashtable>' -TestCase $testCasesWithAndWithoutAsHashtableSwitch {
        Param($AsHashtable)
        $json = @('{"a" :', '"x"}') | ConvertFrom-Json -AsHashtable:$AsHashtable
        $json.a | Should Be 'x'
        if ($AsHashtable)
        {
            $json | Should BeOfType Hashtable
        }
    }
}
