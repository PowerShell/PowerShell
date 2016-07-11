Describe "Get-Credential" -Tags "innerloop", "P1", "RI" {
    It "throws error on a long message" {
        try {
            Get-Credential -Message ('a'*2MB) -ExecutionAction Stop
            throw "Execution OK"
        catch {
            $_.FullyQualifiedErrorId | Should be "CouldNotPromptForCredential,Microsoft.PowerShell.Commands.GetCredentialCommand"
            $_
        }
    }
}
