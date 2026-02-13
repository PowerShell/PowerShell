# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Test-Json" -Tags "CI" {
    BeforeAll {
        $assetsPath = Join-Path $PSScriptRoot -ChildPath assets
        $validSchemaJsonPath = Join-Path -Path $assetsPath -ChildPath valid_schema_reference.json
        $invalidSchemaJsonPath = Join-Path -Path $assetsPath -ChildPath invalid_schema_reference.json
        $missingSchemaJsonPath = Join-Path -Path $assetsPath -ChildPath no_such_file.json
        $missingJsonPath = Join-Path -Path $assetsPath -ChildPath no_such_file.json

        $validSchemaJson = @'
            {
            "description": "A person",
            "type": "object",
            "properties": {
                "name": {"type": "string"},
                "hobbies": {
                "type": "array",
                "items": {"type": "string"}
                }
                }
            }
'@

        $invalidSchemaJson = @'
            {
            "description",
            "type": "object",
            "properties": {
                "name": {"type": "string"},
                "hobbies": {
                "type": "array",
                "items": {"type": "string"}
                }
                }
            }
'@

        $validJson = @'
            {
                "name": "James",
                "hobbies": [".NET", "Blogging", "Reading", "Xbox", "LOLCATS"]
            }
'@

        $invalidTypeInJson = @'
            {
                "name": 123,
                "hobbies": [".NET", "Blogging", "Reading", "Xbox", "LOLCATS"]
            }
'@

        $invalidTypeInJson2 = @'
            {
                "name": 123,
                "hobbies": [456, "Blogging", "Reading", "Xbox", "LOLCATS"]
            }
'@

        $invalidNodeInJson = @'
            {
                "name": "James",
                "hobbies": [".NET", "Blogging", "Reading", "Xbox", "LOLCATS"]
                errorNode
            }
'@

        $jsonWithComments = @'
            {
                // A Json comment
                "string": "test"
            }
'@

        $jsonWithTrailingComma = @'
            {
                "string": "test",
            }
'@

        $jsonWithCommentsAndTrailingComma = @'
            {
                // A Json comment
                "string": "test",
            }
'@

        # Schema using oneOf to allow either integer or string pattern for port items
        $oneOfSchema = @'
            {
                "type": "object",
                "properties": {
                    "ports": {
                        "type": "array",
                        "items": {
                            "oneOf": [
                                { "type": "integer", "minimum": 0, "maximum": 65535 },
                                { "type": "string", "pattern": "^\\d+-\\d+$" }
                            ]
                        }
                    }
                }
            }
'@

        # Valid JSON where ports are integers (first oneOf choice matches)
        $validOneOfJson = '{ "ports": [80, 443, 8080] }'

        # Invalid JSON where a port value matches neither oneOf choice
        $invalidOneOfJson = '{ "ports": [80, "invalid-port", 8080] }'

        # Schema using oneOf to allow either smartphone or laptop device types
        $oneOfDeviceSchema = @'
            {
                "type": "object",
                "properties": {
                    "Devices": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "oneOf": [
                                {
                                    "properties": {
                                        "id": { "type": "string" },
                                        "deviceType": { "const": "smartphone" },
                                        "os": { "type": "string", "enum": ["iOS", "Android"] }
                                    },
                                    "required": ["deviceType", "os"]
                                },
                                {
                                    "properties": {
                                        "id": { "type": "string" },
                                        "deviceType": { "const": "laptop" },
                                        "arch": { "type": "string", "enum": ["x86", "x64", "arm64"] }
                                    },
                                    "required": ["deviceType", "arch"]
                                }
                            ]
                        }
                    }
                },
                "required": ["Devices"]
            }
'@

        # Valid JSON with mixed device types (all matching their respective oneOf choice)
        $validOneOfDeviceJson = @'
            {
                "Devices": [
                    { "id": "0", "deviceType": "laptop", "arch": "x64" },
                    { "id": "1", "deviceType": "smartphone", "os": "iOS" },
                    { "id": "2", "deviceType": "laptop", "arch": "arm64" },
                    { "id": "3", "deviceType": "smartphone", "os": "Android" }
                ]
            }
'@

        # Invalid JSON where only Devices/3 has an invalid os value
        $invalidOneOfDeviceJson = @'
            {
                "Devices": [
                    { "id": "0", "deviceType": "laptop", "arch": "x64" },
                    { "id": "1", "deviceType": "smartphone", "os": "iOS" },
                    { "id": "2", "deviceType": "laptop", "arch": "arm64" },
                    { "id": "3", "deviceType": "smartphone", "os": "WindowsPhone" }
                ]
            }
'@

        # Schema using anyOf to allow either smartphone or laptop device types
        $anyOfDeviceSchema = @'
            {
                "type": "object",
                "properties": {
                    "Devices": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "anyOf": [
                                {
                                    "properties": {
                                        "deviceType": { "const": "smartphone" },
                                        "os": { "type": "string", "enum": ["iOS", "Android"] }
                                    },
                                    "required": ["deviceType", "os"]
                                },
                                {
                                    "properties": {
                                        "deviceType": { "const": "laptop" },
                                        "arch": { "type": "string", "enum": ["x86", "x64", "arm64"] }
                                    },
                                    "required": ["deviceType", "arch"]
                                }
                            ]
                        }
                    }
                },
                "required": ["Devices"]
            }
'@

        # Valid JSON with mixed device types (all matching their respective anyOf choice)
        $validAnyOfDeviceJson = @'
            {
                "Devices": [
                    { "deviceType": "laptop", "arch": "x64" },
                    { "deviceType": "smartphone", "os": "iOS" }
                ]
            }
'@

        # Invalid JSON where only Devices/2 has an invalid os value
        $invalidAnyOfDeviceJson = @'
            {
                "Devices": [
                    { "deviceType": "laptop", "arch": "x64" },
                    { "deviceType": "smartphone", "os": "iOS" },
                    { "deviceType": "smartphone", "os": "WindowsPhone" }
                ]
            }
'@

        $validJsonPath = Join-Path -Path $TestDrive -ChildPath 'validJson.json'
        $validLiteralJsonPath = Join-Path -Path $TestDrive -ChildPath "[valid]Json.json"
        $invalidNodeInJsonPath = Join-Path -Path $TestDrive -ChildPath 'invalidNodeInJson.json'
        $invalidTypeInJsonPath = Join-Path -Path $TestDrive -ChildPath 'invalidTypeInJson.json'
        $invalidTypeInJson2Path = Join-Path -Path $TestDrive -ChildPath 'invalidTypeInJson2.json'
        $invalidEmptyJsonPath = Join-Path -Path $TestDrive -ChildPath 'emptyJson.json'

        Set-Content -Path $validJsonPath -Value $validJson
        Set-Content -LiteralPath $validLiteralJsonPath -Value $validJson
        Set-Content -Path $invalidNodeInJsonPath -Value $invalidNodeInJson
        Set-Content -Path $invalidTypeInJsonPath -Value $invalidTypeInJson
        Set-Content -Path $invalidTypeInJson2Path -Value $invalidTypeInJson2
        New-Item -Path $invalidEmptyJsonPath -ItemType File
    }

    It "Missing JSON schema file doesn't exist" {
        Test-Path -LiteralPath $missingSchemaJsonPath | Should -BeFalse
    }

    It "Missing JSON file doesn't exist" {
        Test-Path -LiteralPath $missingJsonPath | Should -BeFalse
    }

    It "Json is valid" {
        Test-Json -Json $validJson | Should -BeTrue
    }

    It "Json is valid against a valid schema from string" {
        Test-Json -Json $validJson -Schema $validSchemaJson | Should -BeTrue
    }

    It "Json is valid against a valid schema from file" {
        Test-Json -Json $validJson -SchemaFile $validSchemaJsonPath | Should -BeTrue
        ($validJson | Test-Json -SchemaFile $validSchemaJsonPath) | Should -BeTrue
    }

    It "Json file specified using -Path is valid" {
        Test-Json -Path $validJsonPath | Should -BeTrue
    }

    It "Json file specified using -LiteralPath is valid" {
        Test-Json -LiteralPath $validLiteralJsonPath | Should -BeTrue
    }

    It "Json file specified using LiteralPath aliases -PSPath and -LP is valid" {
        Test-Json -PSPath $validLiteralJsonPath | Should -BeTrue
        Test-Json -LP $validLiteralJsonPath | Should -BeTrue
    }

    It "Json file specified using -Path from pipeline is valid" {
        (Get-ChildItem -Path $validJsonPath -File | Test-Json) | Should -BeTrue
    }

    It "Json file specified using -LiteralPath from pipeline is valid" {
        (Get-ChildItem -LiteralPath $validLiteralJsonPath -File | Test-Json) | Should -BeTrue
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
        ($invalidNodeInJson | Test-Json -Schema $validSchemaJson -ErrorAction SilentlyContinue) | Should -BeFalse
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

    It "Json file is invalid against an empty file" {
        Test-Json -Path $invalidEmptyJsonPath -ErrorAction SilentlyContinue | Should -BeFalse
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
        { Test-Json -Path $missingJsonPath -ErrorAction Stop } | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.TestJsonCommand"
    }

    It "Test-Json throw if a path from file using -Path is a literal path" {
        { Test-Json -Path $validLiteralJsonPath -ErrorAction Stop } | Should -Throw -ErrorId "FileOpenFailure,Microsoft.PowerShell.Commands.TestJsonCommand"
    }

    It "Json file throw if a path from file using -LiteralPath is a wildcard or regular expression" {
        { Test-Json -LiteralPath (Join-Path -Path $TestDrive -ChildPath "*Json.json") -ErrorAction Stop } | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.TestJsonCommand"
        { Test-Json -LiteralPath (Join-Path -Path $TestDrive -ChildPath "[a-z]Json.json") -ErrorAction Stop } | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.TestJsonCommand"
    }

    It "Test-Json write an error on invalid (<name>) Json against a valid schema from string" -TestCases @(
        @{ name = "type"; json = $invalidTypeInJson; errorId = "InvalidJsonAgainstSchemaDetailed,Microsoft.PowerShell.Commands.TestJsonCommand" }
        @{ name = "node"; json = $invalidNodeInJson; errorId = "InvalidJson,Microsoft.PowerShell.Commands.TestJsonCommand" }
    ) {
        param ($json, $errorId)

        $errorVar = $null
        Test-Json -Json $json -Schema $validSchemaJson -ErrorVariable errorVar -ErrorAction SilentlyContinue

        $errorVar.FullyQualifiedErrorId | Should -BeExactly $errorId
    }

    It "Test-Json write an error on invalid (<name>) Json against a valid schema from file" -TestCases @(
        @{ name = "type"; json = $invalidTypeInJson; errorId = "InvalidJsonAgainstSchemaDetailed,Microsoft.PowerShell.Commands.TestJsonCommand" }
        @{ name = "node"; json = $invalidNodeInJson; errorId = "InvalidJson,Microsoft.PowerShell.Commands.TestJsonCommand" }
    ) {
        param ($json, $errorId)

        $errorVar = $null
        Test-Json -Json $json -SchemaFile $validSchemaJsonPath -ErrorVariable errorVar -ErrorAction SilentlyContinue

        $errorVar.FullyQualifiedErrorId | Should -BeExactly $errorId
    }

    It "Test-Json write an error on invalid (<name>) Json file against a valid schema from string" -TestCases @(
        @{ name = "type"; json = $invalidTypeInJsonPath; errorId = "InvalidJsonAgainstSchemaDetailed,Microsoft.PowerShell.Commands.TestJsonCommand" }
        @{ name = "node"; json = $invalidNodeInJsonPath; errorId = "InvalidJson,Microsoft.PowerShell.Commands.TestJsonCommand" }
    ) {
        param ($json, $errorId)

        $errorVar = $null
        Test-Json -Path $json -Schema $validSchemaJson -ErrorVariable errorVar -ErrorAction SilentlyContinue

        $errorVar.FullyQualifiedErrorId | Should -BeExactly $errorId
    }

    It "Test-Json write an error on invalid (<name>) Json file against a valid schema from file" -TestCases @(
        @{ name = "type"; json = $invalidTypeInJsonPath; errorId = "InvalidJsonAgainstSchemaDetailed,Microsoft.PowerShell.Commands.TestJsonCommand" }
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
        $errorVar[0].FullyQualifiedErrorId | Should -BeExactly "InvalidJsonAgainstSchemaDetailed,Microsoft.PowerShell.Commands.TestJsonCommand"
        $errorVar[1].FullyQualifiedErrorId | Should -BeExactly "InvalidJsonAgainstSchemaDetailed,Microsoft.PowerShell.Commands.TestJsonCommand"
    }

    It "Test-Json return all errors when check invalid Json against a valid schema from file" {
        $errorVar = $null
        Test-Json -Json $invalidTypeInJson2 -SchemaFile $validSchemaJsonPath -ErrorVariable errorVar -ErrorAction SilentlyContinue

        # '$invalidTypeInJson2' contains two errors in property types.
        $errorVar.Count | Should -Be 2
        $errorVar[0].FullyQualifiedErrorId | Should -BeExactly "InvalidJsonAgainstSchemaDetailed,Microsoft.PowerShell.Commands.TestJsonCommand"
        $errorVar[1].FullyQualifiedErrorId | Should -BeExactly "InvalidJsonAgainstSchemaDetailed,Microsoft.PowerShell.Commands.TestJsonCommand"
    }

    It "Test-Json return all errors when check invalid Json file against a valid schema from string" {
        $errorVar = $null
        Test-Json -Path $invalidTypeInJson2Path -Schema $validSchemaJson -ErrorVariable errorVar -ErrorAction SilentlyContinue

        # '$invalidTypeInJson2Path' contains two errors in property types.
        $errorVar.Count | Should -Be 2
        $errorVar[0].FullyQualifiedErrorId | Should -BeExactly "InvalidJsonAgainstSchemaDetailed,Microsoft.PowerShell.Commands.TestJsonCommand"
        $errorVar[1].FullyQualifiedErrorId | Should -BeExactly "InvalidJsonAgainstSchemaDetailed,Microsoft.PowerShell.Commands.TestJsonCommand"
    }

    It "Test-Json return all errors when check invalid Json file against a valid schema from file" {
        $errorVar = $null
        Test-Json -Path $invalidTypeInJson2Path -SchemaFile $validSchemaJsonPath -ErrorVariable errorVar -ErrorAction SilentlyContinue

        # '$invalidTypeInJson2Path' contains two errors in property types.
        $errorVar.Count | Should -Be 2
        $errorVar[0].FullyQualifiedErrorId | Should -BeExactly "InvalidJsonAgainstSchemaDetailed,Microsoft.PowerShell.Commands.TestJsonCommand"
        $errorVar[1].FullyQualifiedErrorId | Should -BeExactly "InvalidJsonAgainstSchemaDetailed,Microsoft.PowerShell.Commands.TestJsonCommand"
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
            $schema = "{ `"type`": `"$_`" }"
            Test-Json -Json $value -Schema $schema -ErrorAction SilentlyContinue
        } | Should -Be $expected
    }

    It "Test-Json returns True with document options '<options>'" -TestCases @(
        @{ Json = $jsonWithComments; Options = 'IgnoreComments' }
        @{ Json = $jsonWithTrailingComma; Options = 'AllowTrailingCommas'}
        @{ Json = $jsonWithCommentsAndTrailingComma; Options = 'IgnoreComments', 'AllowTrailingCommas'}
    ) {
        param($Json, $Options)

        # Without options should fail
        ($Json | Test-Json -ErrorAction SilentlyContinue) | Should -BeFalse

        # With options should pass
        ($Json | Test-Json -Option $Options -ErrorAction SilentlyContinue) | Should -BeTrue
    }

    It "Test-Json does not report false positives for valid oneOf matches" {
        $errorVar = $null
        $result = Test-Json -Json $validOneOfJson -Schema $oneOfSchema -ErrorVariable errorVar -ErrorAction SilentlyContinue

        $result | Should -BeTrue
        $errorVar.Count | Should -Be 0
    }

    It "Test-Json reports only relevant errors for invalid oneOf values" {
        $errorVar = $null
        $result = Test-Json -Json $invalidOneOfJson -Schema $oneOfSchema -ErrorVariable errorVar -ErrorAction SilentlyContinue

        $result | Should -BeFalse
        # Should report error only for the invalid item, not for valid items
        $errorVar.Count | Should -BeGreaterThan 0
        $errorVar[0].Exception.Message | Should -Match "/ports/1"
    }

    It "Test-Json does not report false positives for valid oneOf device matches" {
        $errorVar = $null
        $result = Test-Json -Json $validOneOfDeviceJson -Schema $oneOfDeviceSchema -ErrorVariable errorVar -ErrorAction SilentlyContinue

        $result | Should -BeTrue
        $errorVar.Count | Should -Be 0
    }

    It "Test-Json reports errors only for the invalid device in oneOf schema" {
        $errorVar = $null
        $result = Test-Json -Json $invalidOneOfDeviceJson -Schema $oneOfDeviceSchema -ErrorVariable errorVar -ErrorAction SilentlyContinue

        $result | Should -BeFalse
        # Should not report errors for valid devices (Devices/0, /1, /2)
        $falsePositives = $errorVar | Where-Object { $_.Exception.Message -match '/Devices/(0|1|2)' }
        $falsePositives.Count | Should -Be 0
        # Should report errors only for the invalid device (Devices/3)
        $relevantErrors = $errorVar | Where-Object { $_.Exception.Message -match '/Devices/3' }
        $relevantErrors.Count | Should -BeGreaterThan 0
    }

    It "Test-Json does not report false positives for valid anyOf device matches" {
        $errorVar = $null
        $result = Test-Json -Json $validAnyOfDeviceJson -Schema $anyOfDeviceSchema -ErrorVariable errorVar -ErrorAction SilentlyContinue

        $result | Should -BeTrue
        $errorVar.Count | Should -Be 0
    }

    It "Test-Json reports errors only for the invalid device in anyOf schema" {
        $errorVar = $null
        $result = Test-Json -Json $invalidAnyOfDeviceJson -Schema $anyOfDeviceSchema -ErrorVariable errorVar -ErrorAction SilentlyContinue

        $result | Should -BeFalse
        # Should not report errors for valid devices (Devices/0, /1)
        $falsePositives = $errorVar | Where-Object { $_.Exception.Message -match '/Devices/(0|1)' }
        $falsePositives.Count | Should -Be 0
        # Should report errors only for the invalid device (Devices/2)
        $relevantErrors = $errorVar | Where-Object { $_.Exception.Message -match '/Devices/2' }
        $relevantErrors.Count | Should -BeGreaterThan 0
    }
}
