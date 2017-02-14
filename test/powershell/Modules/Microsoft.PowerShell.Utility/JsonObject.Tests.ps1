Import-Module $PSScriptRoot\..\..\Common\Test.Helpers.psm1

Describe 'Unit tests for JsonObject' -tags "CI" {

    $validStrings = @(
        @{ name = "empty"; str = "" }
        @{ name = "spaces"; str = "  " }
        @{ name = "object"; str = "{a:1}" }
    )

    It 'no error for valid string - <name>' -TestCase $validStrings {
        param ($str)
        $errRecord = $null
        [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson($str, [ref]$errRecord)
        $errRecord | Should BeNullOrEmpty
    }

    $invalidStrings = @(
        @{ name = "plain text"; str = "plaintext" }
        @{ name = "part"; str = '{"a" :' }
    )

    It 'throw ArgumentException for invalid string - <name>' -TestCase $invalidStrings  {
        param ($str)
        $errRecord = $null
        { [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson($str, [ref]$errRecord) } | ShouldBeErrorId 'ArgumentException'
    }
}
