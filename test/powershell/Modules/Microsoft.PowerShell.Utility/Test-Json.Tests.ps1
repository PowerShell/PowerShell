# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Test-Json" -Tags "CI" {
    BeforeAll {
        $validSchemaJsonPath = Join-Path -Path (Join-Path $PSScriptRoot -ChildPath assets) -ChildPath valid_schema_reference.json

        $invalidSchemaJsonPath = Join-Path -Path (Join-Path $PSScriptRoot -ChildPath assets) -ChildPath invalid_schema_reference.json

        $missingSchemaJsonPath = Join-Path -Path (Join-Path $PSScriptRoot -ChildPath assets) -ChildPath no_such_file.json

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

    It "Missing JSON schema file doesn't exist" {
        Test-Path -LiteralPath $missingSchemaJsonPath | Should -BeFalse
    }

    It "Json is valid" {
        Test-Json -Json $validJson | Should -BeTrue
    }

    It "Json is valid against a valid schema from string" {
        Test-Json -Json $validJson -Schema $validSchemaJson | Should -BeTrue
    }

    It "Json is valid against a valid schema from file" {
        Test-Json -Json $validJson -SchemaFile $validSchemaJsonPath | Should -BeTrue
    }

    It "Json is invalid" {
        Test-Json -Json $invalidNodeInJson -ErrorAction SilentlyContinue | Should -BeFalse
    }

    It "Json is invalid against a valid schema from string" {
        Test-Json -Json $invalidTypeInJson2 -Schema $validSchemaJson -ErrorAction SilentlyContinue | Should -BeFalse
        Test-Json -Json $invalidNodeInJson -Schema $validSchemaJson -ErrorAction SilentlyContinue | Should -BeFalse
    }

    It "Json is invalid against a valid schema from file" {
        Test-Json -Json $invalidTypeInJson2 -SchemaFile $validSchemaJsonPath -ErrorAction SilentlyContinue | Should -BeFalse
        Test-Json -Json $invalidNodeInJson -SchemaFile $validSchemaJsonPath -ErrorAction SilentlyContinue | Should -BeFalse
    }

    It "Test-Json throw if a schema from string is invalid" {
        { Test-Json -Json $validJson -Schema $invalidSchemaJson -ErrorAction Stop } | Should -Throw -ErrorId "InvalidJsonSchema,Microsoft.PowerShell.Commands.TestJsonCommand"
    }

    It "Test-Json throw if a schema from file is invalid" {
        { Test-Json -Json $validJson -SchemaFile $invalidSchemaJsonPath -ErrorAction Stop } | Should -Throw -ErrorId "InvalidJsonSchema,Microsoft.PowerShell.Commands.TestJsonCommand"
    }

    It "Test-Json throw if a path to a schema from file is invalid" {
        { Test-Json -Json $validJson -SchemaFile $missingSchemaJsonPath -ErrorAction Stop } | Should -Throw -ErrorId "JsonSchemaFileOpenFailure,Microsoft.PowerShell.Commands.TestJsonCommand"
    }

    It "Test-Json write an error on invalid (<name>) Json against a valid schema from string" -TestCases @(
        @{ name = "type"; json = $invalidTypeInJson; errorId = "InvalidJsonAgainstSchema,Microsoft.PowerShell.Commands.TestJsonCommand" }
        @{ name = "node"; json = $invalidNodeInJson; errorId = "InvalidJson,Microsoft.PowerShell.Commands.TestJsonCommand" }
    ) {
        param ($json, $errorId)

        $errorVar = $null
        Test-Json -Json $json -Schema $validSchemaJson -ErrorVariable errorVar -ErrorAction SilentlyContinue

        $errorVar.FullyQualifiedErrorId | Should -BeExactly $errorId
    }

    It "Test-Json write an error on invalid (<name>) Json against a valid schema from file" -TestCases @(
        @{ name = "type"; json = $invalidTypeInJson; errorId = "InvalidJsonAgainstSchema,Microsoft.PowerShell.Commands.TestJsonCommand" }
        @{ name = "node"; json = $invalidNodeInJson; errorId = "InvalidJson,Microsoft.PowerShell.Commands.TestJsonCommand" }
    ) {
        param ($json, $errorId)

        $errorVar = $null
        Test-Json -Json $json -SchemaFile $validSchemaJsonPath -ErrorVariable errorVar -ErrorAction SilentlyContinue

        $errorVar.FullyQualifiedErrorId | Should -BeExactly $errorId
    }

    It "Test-Json return all errors when check invalid Json against a valid schema from string" {
        $errorVar = $null
        Test-Json -Json $invalidTypeInJson2 -Schema $validSchemaJson -ErrorVariable errorVar -ErrorAction SilentlyContinue

        # '$invalidTypeInJson2' contains two errors in property types.
        $errorVar.Count | Should -Be 2
        $errorVar[0].FullyQualifiedErrorId | Should -BeExactly "InvalidJsonAgainstSchema,Microsoft.PowerShell.Commands.TestJsonCommand"
        $errorVar[1].FullyQualifiedErrorId | Should -BeExactly "InvalidJsonAgainstSchema,Microsoft.PowerShell.Commands.TestJsonCommand"
    }

    It "Test-Json return all errors when check invalid Json against a valid schema from file" {
        $errorVar = $null
        Test-Json -Json $invalidTypeInJson2 -SchemaFile $validSchemaJsonPath -ErrorVariable errorVar -ErrorAction SilentlyContinue

        # '$invalidTypeInJson2' contains two errors in property types.
        $errorVar.Count | Should -Be 2
        $errorVar[0].FullyQualifiedErrorId | Should -BeExactly "InvalidJsonAgainstSchema,Microsoft.PowerShell.Commands.TestJsonCommand"
        $errorVar[1].FullyQualifiedErrorId | Should -BeExactly "InvalidJsonAgainstSchema,Microsoft.PowerShell.Commands.TestJsonCommand"
    }
}
