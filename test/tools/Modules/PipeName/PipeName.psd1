@{
    ModuleVersion = '1.0.0'
    GUID = '6b190c5e-88f6-4aca-ba74-42b01c54c3ea'
    Author = 'PowerShell'
    Description = 'A Named Pipe HTTP-like responder for testing purposes'
    RootModule = 'PipeName.psm1'
    FunctionsToExport = @(
        'Get-PipeServer'
        'Get-PipeName'
        'Get-PipeServerUri'
        'Start-PipeServer'
        'Stop-PipeServer'
    )
    AliasesToExport = @()
    CmdletsToExport = @()
}
