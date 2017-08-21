@{
    ModuleVersion = '1.0.0'
    GUID = '90572e25-3f15-49b0-8f25-fb717d3ef46a'
    Author = 'Mark Kraus'
    CompanyName = ''
    Copyright = ''
    Description = 'Creates a new HTTPS Listener for testing purposes'
    RootModule = 'HttpsListener.psm1'
    FunctionsToExport = @('Start-HttpsListener','Stop-HttpsListener', 'Get-HttpsListener')
}
