@{
    ModuleVersion = '1.0.0'
    GUID = '90572e25-3f15-49b0-8f25-fb717d3ef46a'
    Author = 'Mark Kraus'
    Description = 'An HTTP and HTTPS Listener for testing purposes'
    RootModule = 'WebListener.psm1'
    RequiredModules = @(
        'SelfSignedCertificate'
    )
    FunctionsToExport = @(
        'Get-WebListener'
        'Get-WebListenerClientCertificate'
        'Get-WebListenerUrl'
        'Start-WebListener'
        'Stop-WebListener'
    )
    AliasesToExport = @()
    CmdletsToExport = @()
}
