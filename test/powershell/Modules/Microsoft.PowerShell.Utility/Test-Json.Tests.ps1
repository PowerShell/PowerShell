Describe "Test-Json" -Tags "CI" {
    BeforeAll {
        $validSchemaJson = @"
            {
            'description': 'A person',
            'type': 'object',
            'properties': {
                'name': {'type': 'string'},
                'hobbies': {
                'type': 'array',
                'items': {'type': 'string'}
                }
                }
            }
"@

        $invalidSchemaJson = @"
            {
            'description',
            'type': 'object',
            'properties': {
                'name': {'type': 'string'},
                'hobbies': {
                'type': 'array',
                'items': {'type': 'string'}
                }
                }
            }
"@

        $validJson = @"
            {
                'name': 'James',
                'hobbies': ['.NET', 'Blogging', 'Reading', 'Xbox', 'LOLCATS']
            }
"@

        $invalidTypeInJson = @"
            {
                'name': 123,
                'hobbies': ['.NET', 'Blogging', 'Reading', 'Xbox', 'LOLCATS']
            }
"@

        $invalidTypeInJson2 = @"
            {
                'name': 123,
                'hobbies': [456, 'Blogging', 'Reading', 'Xbox', 'LOLCATS']
            }
"@

        $invalidNodeInJson = @"
            {
                'name': 'James',
                'hobbies': ['.NET', 'Blogging', 'Reading', 'Xbox', 'LOLCATS']
                errorNode
            }
"@
}

    It "Json is valid" {
        Test-Json -Json $validJson | Should Be $true
    }

    It "Json is valid aganist a valid schema" {
        Test-Json -Json $validJson -Schema $validSchemaJson | Should Be $true
    }

    It "Json is invalid" {
        Test-Json -Json $invalidNodeInJson -ErrorAction SilentlyContinue | Should Be $false
    }

    It "Json is invalid aganist a valid schema" {
        Test-Json -Json $invalidTypeInJson2 -Schema $validSchemaJson -ErrorAction SilentlyContinue | Should Be $false
        Test-Json -Json $invalidNodeInJson -Schema $validSchemaJson -ErrorAction SilentlyContinue | Should Be $false
    }

    It "Test-Json throw if a schema is invalid" {
        { Test-Json -Json $validJson -Schema $invalidSchemaJson -ErrorAction Stop } | ShouldBeErrorId "InvalidJsonSchema,Microsoft.PowerShell.Commands.TestJsonCommand"
    }

    It "Test-Json write an error on invalid (<name>) Json aganist a valid schema" -TestCases @(
        @{ name = "type"; json = $invalidTypeInJson; error = "InvalidJsonAgainistSchema,Microsoft.PowerShell.Commands.TestJsonCommand" }
        @{ name = "node"; json = $invalidNodeInJson; error = "InvalidJson,Microsoft.PowerShell.Commands.TestJsonCommand" }
        ) {
            param($json, $error)

            $errorVar = $null
            Test-Json -Json $json -Schema $validSchemaJson -ErrorVariable errorVar -ErrorAction SilentlyContinue

            $errorVar.FullyQualifiedErrorId | Should BeExactly $error
    }

    It "Test-Json return all errors when check invalid Json aganist a valid schema" {
        $errorVar = $null
        Test-Json -Json $invalidTypeInJson2 -Schema $validSchemaJson -ErrorVariable errorVar -ErrorAction SilentlyContinue

        # '$invalidTypeInJson2' contains two errors in property types.
        $errorVar.Count | Should Be 2
        $errorVar.FullyQualifiedErrorId | Should BeExactly "InvalidJsonAgainistSchema,Microsoft.PowerShell.Commands.TestJsonCommand"
    }
}
