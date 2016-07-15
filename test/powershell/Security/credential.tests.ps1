Describe "Get-Credential" -Tags "CI" {
    It "throws error on a long message" -pending:($IsCore) {
        try {
            Get-Credential -Message ('a'*2MB) -ExecutionAction Stop
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | Should be "CouldNotPromptForCredential,Microsoft.PowerShell.Commands.GetCredentialCommand"
            $_
        }
    }
}
