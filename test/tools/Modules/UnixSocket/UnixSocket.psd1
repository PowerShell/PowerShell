@{
    ModuleVersion = '1.0.0'
    GUID = '86471f04-5b94-4136-a299-caf98464a06b'
    Author = 'PowerShell'
    Description = 'An UnixSocket Server for testing purposes'
    RootModule = 'UnixSocket.psm1'
    RequiredModules = @()
    FunctionsToExport = @(
        'Get-UnixSocket'
        'Get-UnixSocketName'
        'Get-UnixSocketUri'
        'Start-UnixSocket'
        'Stop-UnixSocket'
    )
    AliasesToExport = @()
    CmdletsToExport = @()
}
