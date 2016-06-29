# Pester Module for testing Proxy Command APIs

Describe "Validation tests for Proxy Command APIs" {

    It "Validates that ProxyCommands work for cmdlets with dynamic parameters" {
    
        $command = Get-Command Get-ChildItem
        $md = [System.Management.Automation.CommandMetadata] $command
        $source = [System.Management.Automation.ProxyCommand]::Create($md)
        Set-Content function:\MyGetChildItem -Value $source
        $results = MyGetChildItem "Cert:\currentuser\my","-Name" -CodeSign 2>&1

        ## Validates that we can actually use the CodeSign dynamic parameter
        $certs = $results | Where-Object Thumbprint
        ($certs.Count -gt 0) | Should be $true

        ## Validates that the -Name parameter was passed as an argument to the Path
        ## parameter, rather than it being treated as a unique parameter
        $errorResult = $results | Where-object FullyQualifiedErrorId
        $errorResult.FullyQualifiedErrorId | Should be "PathNotFound,Microsoft.PowerShell.Commands.GetChildItemCommand"
    }
    
}
