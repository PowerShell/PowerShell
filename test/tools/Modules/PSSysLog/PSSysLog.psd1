@{
    ModuleVersion = '1.0.0'
    GUID = '56b63338-045c-4697-a24b-5a756268c8b2'
    Author = 'PowerShell'
    CompanyName = 'Microsoft Corporation'
    Copyright = 'Copyright (c) Microsoft Corporation. All rights reserved.'
    Description = 'Provides a reader for powershell syslog and os_log entries'
    RootModule = 'PSSysLog.psm1'
    FunctionsToExport = @(
        'Get-PSSysLog'
        'Get-PSOsLog'
        'Export-PSOsLog'
        'Get-OsLogPersistence'
        'Set-OsLogPersistence'
        'Clear-PSEventLog'
        'Wait-PSWinEvent'
    )
    AliasesToExport = @()
    CmdletsToExport = @()
}
