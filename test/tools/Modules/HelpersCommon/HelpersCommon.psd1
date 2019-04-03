#
# Module manifest for module 'HelpersCommon'
#

@{

RootModule = 'HelpersCommon.psm1'

ModuleVersion = '1.0'

GUID = 'cc1c8e94-51d1-4bc1-b508-62bc09f02f54'

CompanyName = 'Microsoft Corporation'

Copyright = 'Copyright (c) Microsoft Corporation. All rights reserved.'

Description = 'Temporary module contains functions for using in tests'

FunctionsToExport = @(
        'Add-TestDynamicType'
        'Test-CanWriteToPsHome'
        'Disable-Testhook'
        'Enable-Testhook'
        'Get-RandomFileName'
        'New-RandomHexString'
        'Send-VstsLogFile'
        'Set-TesthookResult'
        'Start-NativeExecution'
        'Test-IsElevated'
        'Test-IsRoot'
        'Test-IsVstsLinux'
        'Test-IsVstsWindows'
        'Test-TesthookIsSet'
        'Wait-FileToBePresent'
        'Wait-UntilTrue'
    )

CmdletsToExport= @()

AliasesToExport= @()

}
