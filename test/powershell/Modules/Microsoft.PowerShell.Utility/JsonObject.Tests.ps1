# Unit tests for JsonObject
Describe 'JsonObject' -tags "CI" {

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
        }
        catch
        {
            $ex = $_.Exception.InnerException
        }

        $ex | Should Not BeNullOrEmpty
        $ex.GetType() | Should Be $expectedException
    }

    It 'empty string' {
        $errRecord = $null
        [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson("", [ref]$errRecord)
        $errRecord | Should BeNullOrEmpty
    }

    It 'whitespace' {
        $errRecord = $null
        [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson("   ", [ref]$errRecord)
        $errRecord | Should BeNullOrEmpty
    }

    It 'plain text' {
        $errRecord = $null
        { [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson("plaintext", [ref]$errRecord) } | ShouldThrow 'System.ArgumentException'
    }

    It 'object' {
        $errRecord = $null
        [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson("{a:1}", [ref]$errRecord)
        $errRecord | Should BeNullOrEmpty
    }

    It 'part' {
        $errRecord = $null
        { [Microsoft.PowerShell.Commands.JsonObject]::ConvertFromJson("{a:1", [ref]$errRecord) } | ShouldThrow 'System.ArgumentException'
    }
}