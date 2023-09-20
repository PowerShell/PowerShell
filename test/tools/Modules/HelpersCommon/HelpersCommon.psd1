#
# Module manifest for module 'HelpersCommon'
#

@{

RootModule = 'HelpersCommon.psm1'

ModuleVersion = '1.0'

GUID = 'cc1c8e94-51d1-4bc1-b508-62bc09f02f54'

CompanyName = 'Microsoft Corporation'

Copyright = 'Copyright (c) Microsoft Corporation.'

Description = 'Temporary module contains functions for using in tests'

FunctionsToExport = @(
        'Add-TestDynamicType'
        'Test-CanWriteToPsHome'
        'Disable-Testhook'
        'Enable-Testhook'
        'Get-RandomFileName'
        'New-RandomHexString'
        'New-ComplexPassword'
        'Send-VstsLogFile'
        'Set-TesthookResult'
        'Start-NativeExecution'
        'Test-IsElevated'
        'Test-IsPreview',
        'Test-IsReleaseCandidate'
        'Test-IsRoot'
        'Test-IsVstsLinux'
        'Test-IsVstsWindows'
        'Test-IsWindowsArm64'
        'Test-IsWinWow64'
        'Test-TesthookIsSet'
        'Wait-FileToBePresent'
        'Wait-UntilTrue'
        'Get-HelpNetworkTestCases'
        'Get-PlatformInfo'
        'Get-WSManSupport'
    )

CmdletsToExport= @()

AliasesToExport= @()

}
