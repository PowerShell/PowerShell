Describe 'Unit tests for JsonObject' -tags "CI" {

    $validStrings = @(
        @{ name = "empty";  str = "";      ReturnHashTable = $true  }
        @{ name = "spaces"; str = "  ";    ReturnHashTable = $true  }
        @{ name = "object"; str = "{a:1}"; ReturnHashTable = $true  }
        @{ name = "empty";  str = "";      ReturnHashTable = $false }
        @{ name = "spaces"; str = "  ";    ReturnHashTable = $false }
        @{ name = "object"; str = "{a:1}"; ReturnHashTable = $false }
    )

    It 'no error for valid string ''<name>'' with -ReturnHashTable:$<ReturnHashTable>' -TestCase $validStrings {
        param ($str, $ReturnHashTable)
        $errRecord = $null
        [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson($str, $ReturnHashTable, [ref]$errRecord)
        $errRecord | Should BeNullOrEmpty
    }

    $invalidStrings = @(
        @{ name = "plain text"; str = "plaintext"; ReturnHashTable = $true  }
        @{ name = "part";       str = '{"a" :';    ReturnHashTable = $true  }
        @{ name = "plain text"; str = "plaintext"; ReturnHashTable = $false }
        @{ name = "part";       str = '{"a" :';    ReturnHashTable = $false }
    )

    It 'throw ArgumentException for invalid string ''<name>'' with -ReturnHashTable:$<ReturnHashTable>' -TestCase $invalidStrings  {
        param ($str, $ReturnHashTable)
        $errRecord = $null
        { [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson($str, $ReturnHashTable, [ref]$errRecord) } | ShouldBeErrorId "ArgumentException"
    }
}
