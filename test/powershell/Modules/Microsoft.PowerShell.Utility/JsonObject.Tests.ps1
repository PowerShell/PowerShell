Describe 'Unit tests for JsonObject' -tags "CI" {

    function ShouldThrow
    {
        param (
            [Parameter(ValueFromPipeline = $true)]
            $InputObject,
            [Parameter(Position = 0)]
            $expectedException
        )

        try
        {
            & $InputObject
            throw "Should throw exception"
        } catch
        {
            $_.FullyQualifiedErrorId | should be $expectedException
        }
    }

    $validStrings = @(
        @{ str = "" }
        @{ str = "  " }
        @{ str = "{a:1}" }
    )

    It 'no error for valid string' -TestCase $validStrings {
        param ($str)
        $errRecord = $null
        [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson($str, [ref]$errRecord)
        $errRecord | Should BeNullOrEmpty
    }

    $invalidStrings = @(
        @{ str = "plaintext" }
        @{ str = "{a:1" }
    )

    It 'throw ArgumentException for invalid string' -TestCase $invalidStrings  {
        param ($str)
        $errRecord = $null
        { [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson($str, [ref]$errRecord) } | ShouldThrow 'ArgumentException'
    }
}
