Describe 'Unit tests for JsonObject' -tags "CI" {

    function ShouldThrow
    {
        param (
            [Parameter(ValueFromPipeline = $true)]
            $InputObject,
            [Parameter(Position = 0)]
            $ExpectedException
        )

        try
        {
            & $InputObject
            throw "Should throw exception"
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be $ExpectedException
        }
    }

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
        { [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson($str, [ref]$errRecord) } | ShouldThrow 'ArgumentException'
    }
}
