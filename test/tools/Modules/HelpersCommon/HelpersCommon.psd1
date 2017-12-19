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

FunctionsToExport = 'Wait-UntilTrue', 'Test-IsElevated', 'ShouldBeErrorId', 'Wait-FileToBePresent', 'Get-RandomFileName', 'Enable-Testhook', 'Disable-Testhook', 'Set-TesthookResult', 'Test-TesthookIsSet'
}
