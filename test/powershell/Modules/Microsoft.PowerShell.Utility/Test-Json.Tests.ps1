# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Test-Json" -Tags "CI" {
    BeforeAll {
        $assetsPath = Join-Path $PSScriptRoot -ChildPath assets

        $validSchemaJsonPath = Join-Path -Path $assetsPath -ChildPath valid_schema_reference.json

        $invalidSchemaJsonPath = Join-Path -Path $assetsPath -ChildPath invalid_schema_reference.json

        $missingSchemaJsonPath = Join-Path -Path $assetsPath -ChildPath no_such_file.json

        $validJsonPath = Join-Path -Path $assetsPath -ChildPath valid.json

        $missingJsonPath = Join-Path -Path $assetsPath -ChildPath no_such_file.json

        $invalidNodeInJsonPath = Join-Path -Path $assetsPath -ChildPath invalid_node.json

        $invalidTypeInJsonPath = Join-Path -Path $assetsPath -ChildPath invalid_type.json

        $invalidTypeInJson2Path = Join-Path -Path $assetsPath -ChildPath invalid_type_2.json

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

    It "Missing JSON file doesn't exist" {
        Test-Path -LiteralPath $missingJsonPath | Should -BeFalse
    }

    It "Json is valid" {
        Test-Json -Json $validJson | Should -BeTrue
        ($validJson | Test-Json) | Should -BeTrue
    }

    It "Json is valid against a valid schema from string" {
        Test-Json -Json $validJson -Schema $validSchemaJson | Should -BeTrue
        ($validJson | Test-Json -Schema $validSchemaJson) | Should -BeTrue
    }

    It "Json is valid against a valid schema from file" {
        Test-Json -Json $validJson -SchemaFile $validSchemaJsonPath | Should -BeTrue
        ($validJson | Test-Json -SchemaFile $validSchemaJsonPath) | Should -BeTrue
    }

    It "Json file specified using -Path is valid" {
        Test-Json -Path $validJsonPath | Should -BeTrue
    }

    It "Json file specified using -JsonFile is valid" {
        Test-Json -JsonFile $validJsonPath | Should -BeTrue
    }

    It "Json file is valid against a valid schema from string" {
        Test-Json -Path $validJsonPath -Schema $validSchemaJson | Should -BeTrue
    }

    It "Json file is valid against a valid schema from file" {
        Test-Json -Path $validJsonPath -SchemaFile $validSchemaJsonPath | Should -BeTrue
    }

    It "Json is invalid" {
        Test-Json -Json $invalidNodeInJson -ErrorAction SilentlyContinue | Should -BeFalse
        ($invalidNodeInJson | Test-Json -ErrorAction SilentlyContinue) | Should -BeFalse
    }

    It "Json is invalid against a valid schema from string" {
        Test-Json -Json $invalidTypeInJson2 -Schema $validSchemaJson -ErrorAction SilentlyContinue | Should -BeFalse
        ($invalidTypeInJson2 | Test-Json -Schema $validSchemaJson -ErrorAction SilentlyContinue) | Should -BeFalse

        Test-Json -Json $invalidNodeInJson -Schema $validSchemaJson -ErrorAction SilentlyContinue | Should -BeFalse
        ($invalidNodeInJson | Test-Json $validSchemaJson -ErrorAction SilentlyContinue) | Should -BeFalse
    }

    It "Json is invalid against a valid schema from file" {
        Test-Json -Json $invalidTypeInJson2 -SchemaFile $validSchemaJsonPath -ErrorAction SilentlyContinue | Should -BeFalse
        ($invalidTypeInJson2 | Test-Json -SchemaFile $validSchemaJsonPath -ErrorAction SilentlyContinue) | Should -BeFalse

        Test-Json -Json $invalidNodeInJson -SchemaFile $validSchemaJsonPath -ErrorAction SilentlyContinue | Should -BeFalse
        ($invalidNodeInJson | Test-Json -SchemaFile $validSchemaJsonPath -ErrorAction SilentlyContinue) | Should -BeFalse
    }

    It "Json file is invalid against a valid schema from file" {
        Test-Json -Path $invalidTypeInJson2Path -SchemaFile $validSchemaJsonPath -ErrorAction SilentlyContinue | Should -BeFalse
        Test-Json -Path $invalidNodeInJsonPath -SchemaFile $validSchemaJsonPath -ErrorAction SilentlyContinue | Should -BeFalse
    }

    It "Json file is invalid" {
        Test-Json -Path $invalidNodeInJsonPath -ErrorAction SilentlyContinue | Should -BeFalse
    }

    It "Json file is invalid against a valid schema from string" {
        Test-Json -Path $invalidTypeInJson2Path -Schema $validSchemaJson -ErrorAction SilentlyContinue | Should -BeFalse
        Test-Json -Path $invalidNodeInJsonPath -Schema $validSchemaJson -ErrorAction SilentlyContinue | Should -BeFalse
    }

    It "Test-Json throw if a schema from string is invalid" {
        { Test-Json -Json $validJson -Schema $invalidSchemaJson -ErrorAction Stop } | Should -Throw -ErrorId "InvalidJsonSchema,Microsoft.PowerShell.Commands.TestJsonCommand"
        { Test-Json -Path $validJsonPath -Schema $invalidSchemaJson -ErrorAction Stop } | Should -Throw -ErrorId "InvalidJsonSchema,Microsoft.PowerShell.Commands.TestJsonCommand"
    }

    It "Test-Json throw if a schema from file is invalid" {
        { Test-Json -Json $validJson -SchemaFile $invalidSchemaJsonPath -ErrorAction Stop } | Should -Throw -ErrorId "InvalidJsonSchema,Microsoft.PowerShell.Commands.TestJsonCommand"
        { Test-Json -Path $validJsonPath -SchemaFile $invalidSchemaJsonPath -ErrorAction Stop } | Should -Throw -ErrorId "InvalidJsonSchema,Microsoft.PowerShell.Commands.TestJsonCommand"
    }

    It "Test-Json throw if a path to a schema from file is invalid" {
        { Test-Json -Json $validJson -SchemaFile $missingSchemaJsonPath -ErrorAction Stop } | Should -Throw -ErrorId "JsonSchemaFileOpenFailure,Microsoft.PowerShell.Commands.TestJsonCommand"
        { Test-Json -Path $validJsonPath -SchemaFile $missingSchemaJsonPath -ErrorAction Stop } | Should -Throw -ErrorId "JsonSchemaFileOpenFailure,Microsoft.PowerShell.Commands.TestJsonCommand"
    }

    It "Test-Json throw if a path from file is invalid" {
        { Test-Json -Path $missingJsonPath -ErrorAction Stop } | Should -Throw -ErrorId "JsonFileOpenFailure,Microsoft.PowerShell.Commands.TestJsonCommand"
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

    It "Test-Json write an error on invalid (<name>) Json file against a valid schema from string" -TestCases @(
        @{ name = "type"; json = $invalidTypeInJsonPath; errorId = "InvalidJsonAgainstSchema,Microsoft.PowerShell.Commands.TestJsonCommand" }
        @{ name = "node"; json = $invalidNodeInJsonPath; errorId = "InvalidJson,Microsoft.PowerShell.Commands.TestJsonCommand" }
    ) {
        param ($json, $errorId)

        $errorVar = $null
        Test-Json -Path $json -Schema $validSchemaJson -ErrorVariable errorVar -ErrorAction SilentlyContinue

        $errorVar.FullyQualifiedErrorId | Should -BeExactly $errorId
    }

    It "Test-Json write an error on invalid (<name>) Json file against a valid schema from file" -TestCases @(
        @{ name = "type"; json = $invalidTypeInJsonPath; errorId = "InvalidJsonAgainstSchema,Microsoft.PowerShell.Commands.TestJsonCommand" }
        @{ name = "node"; json = $invalidNodeInJsonPath; errorId = "InvalidJson,Microsoft.PowerShell.Commands.TestJsonCommand" }
    ) {
        param ($json, $errorId)

        $errorVar = $null
        Test-Json -Path $json -SchemaFile $validSchemaJsonPath -ErrorVariable errorVar -ErrorAction SilentlyContinue

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

    It "Test-Json return all errors when check invalid Json file against a valid schema from string" {
        $errorVar = $null
        Test-Json -Path $invalidTypeInJson2Path -Schema $validSchemaJson -ErrorVariable errorVar -ErrorAction SilentlyContinue

        # '$invalidTypeInJson2Path' contains two errors in property types.
        $errorVar.Count | Should -Be 2
        $errorVar[0].FullyQualifiedErrorId | Should -BeExactly "InvalidJsonAgainstSchema,Microsoft.PowerShell.Commands.TestJsonCommand"
        $errorVar[1].FullyQualifiedErrorId | Should -BeExactly "InvalidJsonAgainstSchema,Microsoft.PowerShell.Commands.TestJsonCommand"
    }

    It "Test-Json return all errors when check invalid Json file against a valid schema from file" {
        $errorVar = $null
        Test-Json -Path $invalidTypeInJson2Path -SchemaFile $validSchemaJsonPath -ErrorVariable errorVar -ErrorAction SilentlyContinue

        # '$invalidTypeInJson2Path' contains two errors in property types.
        $errorVar.Count | Should -Be 2
        $errorVar[0].FullyQualifiedErrorId | Should -BeExactly "InvalidJsonAgainstSchema,Microsoft.PowerShell.Commands.TestJsonCommand"
        $errorVar[1].FullyQualifiedErrorId | Should -BeExactly "InvalidJsonAgainstSchema,Microsoft.PowerShell.Commands.TestJsonCommand"
    }

    It 'Test-Json recognizes non-object types: <name>' -TestCases @(
        @{ name = 'number'; value = 1; expected = 'number' }
        @{ name = '"true"'; value = '"true"'; expected = 'string' }
        @{ name = 'true'; value = 'true'; expected = 'boolean' }
        @{ name = '"false"'; value = '"false"'; expected = 'string' }
        @{ name = 'false'; value = 'false'; expected = 'boolean' }
        @{ name = '"null"'; value = '"null"'; expected = 'string' }
        @{ name = 'null'; value = 'null'; expected = 'null' }
        @{ name = 'string'; value = '"abc"'; expected = 'string' }
        @{ name = 'array'; value = '[ 1, 2 ]'; expected = 'array' }
    ) {
        param ($name, $value, $expected)

        # All JSON valid
        Test-Json -Json $value | Should -BeTrue

        # Exactly one type should match
        $types = 'string', 'number', 'boolean', 'null', 'array', 'object'
        $types | Where-Object {
            $schema = "{ 'type': '$_' }"
            Test-Json -Json $value -Schema $schema -ErrorAction SilentlyContinue
        } | Should -Be $expected
    }
}
