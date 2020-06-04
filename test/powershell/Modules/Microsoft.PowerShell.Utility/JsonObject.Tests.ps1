# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'Unit tests for JsonObject' -tags "CI" {

    BeforeAll {
        $jsonWithEmptyKey = '{"": "Value"}'
        $jsonContainingKeysWithDifferentCasing = '{"key1": "Value1", "Key1": "Value2"}'

        $testCasesJsonDepthWithAndWithoutReturnHashTable = @(
            @{ Depth = 2;    ReturnHashTable = $true  }
            @{ Depth = 2;    ReturnHashTable = $false }
            @{ Depth = 200;  ReturnHashTable = $true  }
            @{ Depth = 200;  ReturnHashTable = $false }
            @{ Depth = 2000; ReturnHashTable = $true  }
            @{ Depth = 2000; ReturnHashTable = $false }
        )

        function New-NestedJson {
            Param(
                [ValidateRange(1, 2048)]
                [int]
                $Depth
            )

            $nestedJson = "true"

            $Depth..1 | ForEach-Object {
                $nestedJson = '{"' + $_ + '":' + $nestedJson + '}'
            }

            return $nestedJson
        }
    }

    It 'No error for valid string ''<name>'' with -ReturnHashTable:<ReturnHashTable>' -TestCases @(
        @{ name = "null";   str = $null;   ReturnHashTable = $true  }
        @{ name = "empty";  str = "";      ReturnHashTable = $true  }
        @{ name = "spaces"; str = "  ";    ReturnHashTable = $true  }
        @{ name = "object"; str = "{a:1}"; ReturnHashTable = $true  }
        @{ name = "null";   str = $null;   ReturnHashTable = $false }
        @{ name = "empty";  str = "";      ReturnHashTable = $false }
        @{ name = "spaces"; str = "  ";    ReturnHashTable = $false }
        @{ name = "object"; str = "{a:1}"; ReturnHashTable = $false }
    ) {
        param ($str, $ReturnHashTable)
        $errRecord = $null
        [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson($str, $ReturnHashTable, [ref]$errRecord)
        $errRecord | Should -BeNullOrEmpty
    }

    It 'Throw ArgumentException for invalid string ''<name>'' with -ReturnHashTable:<ReturnHashTable>' -TestCases @(
        @{ name = "plain text"; str = "plaintext"; ReturnHashTable = $true  }
        @{ name = "part";       str = '{"a" :';    ReturnHashTable = $true  }
        @{ name = "plain text"; str = "plaintext"; ReturnHashTable = $false }
        @{ name = "part";       str = '{"a" :';    ReturnHashTable = $false }
    )  {
        param ($str, $ReturnHashTable)
        $errRecord = $null
        { [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson($str, $ReturnHashTable, [ref]$errRecord) } | Should -Throw -ErrorId "ArgumentException"
    }

    It 'Can Convert json with depth less than -Depth:<Depth> with -ReturnHashTable:<ReturnHashTable>' -TestCases $testCasesJsonDepthWithAndWithoutReturnHashTable {
        param ($Depth, $ReturnHashTable)
        $errRecord = $null
        $json = New-NestedJson -Depth ($Depth - 1)
        [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson($json, $ReturnHashTable, $Depth, [ref]$errRecord)
        $errRecord | Should -BeNullOrEmpty
    }

    It 'Can Convert json with depth equal to -Depth:<Depth> with -ReturnHashTable:<ReturnHashTable>' -TestCases $testCasesJsonDepthWithAndWithoutReturnHashTable {
        param ($Depth, $ReturnHashTable)
        $errRecord = $null
        $json = New-NestedJson -Depth $Depth
        [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson($json, $ReturnHashTable, $Depth, [ref]$errRecord)
        $errRecord | Should -BeNullOrEmpty
    }

    It 'Throws ArgumentException for json with greater depth than -Depth:<Depth> with -ReturnHashTable:<ReturnHashTable>' -TestCases $testCasesJsonDepthWithAndWithoutReturnHashTable {
        param ($Depth, $ReturnHashTable)
        $errRecord = $null
        $json = New-NestedJson -Depth ($Depth + 1)
        { [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson($json, $ReturnHashTable, $Depth, [ref]$errRecord) } |
            Should -Throw -ErrorId "ArgumentException"
    }

    Context 'Empty key name' {
        It 'Throw InvalidOperationException when json contains empty key name' {
            $errorRecord = $null
            [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson($jsonWithEmptyKey, [ref]$errorRecord)
            $errorRecord.FullyQualifiedErrorId | Should -BeExactly 'EmptyKeyInJsonString'
        }

        It 'Not throw when json contains empty key name when ReturnHashTable is true' {
            $errorRecord = $null
            $result = [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson($jsonWithEmptyKey, $true, [ref]$errorRecord)
            $result | Should -Not -BeNullOrEmpty
            $result.Count | Should -Be 1
            $result.'' | Should -Be 'Value'
        }
    }

    Context 'Keys with different casing ' {

        It 'Throw InvalidOperationException when json contains key with different casing' {
            $errorRecord = $null
            [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson($jsonContainingKeysWithDifferentCasing, [ref]$errorRecord)
            $errorRecord.FullyQualifiedErrorId | Should -BeExactly 'KeysWithDifferentCasingInJsonString'
        }

        It 'Not throw when json contains key (same casing) when ReturnHashTable is true' {
            $errorRecord = $null
            $result = [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson($jsonContainingKeysWithDifferentCasing, $true, [ref]$errorRecord)
            $result | Should -Not -BeNullOrEmpty
            $result.Count | Should -Be 2
            $result.key1  | Should -Be 'Value1'
            $result.Key1  | Should -Be 'Value2'
        }
    }
}
