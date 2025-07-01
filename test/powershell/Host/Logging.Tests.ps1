# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
using namespace System.Text

using module ..\..\tools\Modules\HelpersCommon
using module ..\..\tools\Modules\PSSysLog

Set-StrictMode -Version 3
$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

<#
    Define enums that mirror the internal enums used
    in product code. These are used to configure
    syslog logging.
#>
enum LogLevel
{
    LogAlways = 0x0
    Critical = 0x1
    Error = 0x2
    Warning = 0x3
    Informational = 0x4
    Verbose = 0x5
    Debug = 0x14
}

enum LogChannel
{
    Operational = 0x10
    Analytic = 0x11
}

enum LogKeyword
{
    Runspace = 0x1
    Pipeline = 0x2
    Protocol = 0x4
    Transport = 0x8
    Host = 0x10
    Cmdlets = 0x20
    Serializer = 0x40
    Session = 0x80
    ManagedPlugin = 0x100
}

# mac log command can emit json, so just use that
# we need to deconstruct the eventmessage to get the event id
# we also need to filter out the non-default messages
function Get-MacOsSyslogItems {
    param ([int]$processId, [string]$logId)
    $logArgs = "show", "--process", "$processId", "--style", "json"
    log $logArgs |
        ConvertFrom-Json |
        Where-Object { $_.category -eq "$logId" -and $_.messageType -eq "Default" } |
        ForEach-Object {
            $s = $_.eventMessage.IndexOf('[') + 1
            $e = $_.EventMessage.IndexOf(']')
            $l = $e - $s
            if ($l -gt 0) {
                $eventId = $_.eventMessage.SubString($s, $l)
            }
            else {
                $eventId = "unknown"
            }
            $_ | Add-Member -MemberType NoteProperty -Name EventId -Value $eventId -PassThru
        }
}

<#
.SYNOPSIS
   Creates a powershell.config.json file with syslog settings

.PARAMETER logId
    The identifier to use for logging

.PARAMETER logLevel
    The optional logging level, see the LogLevel enum

.PARAMETER logChannels
    The optional logging channels to enable; see the LogChannel enum

.PARAMETER logKeywords
    The optional keywords to enable ; see the LogKeyword enum
#>
function WriteLogSettings
{
    param
    (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $LogId,

        [System.Nullable[LogLevel]] $LogLevel = $null,

        [LogChannel[]] $LogChannels = $null,

        [LogKeyword[]] $LogKeywords = $null,

        [switch] $ScriptBlockLogging
    )

    $filename = [Guid]::NewGuid().ToString('N')
    $fullPath = Join-Path -Path $TestDrive -ChildPath "$filename.config.json"

    $values = @{}
    $values['LogIdentity'] = $LogId

    if ($LogChannels -ne $null)
    {
        $values['LogChannels'] = $LogChannels -join ', '
    }

    if ($LogKeywords -ne $null)
    {
        $values['LogKeywords'] = $LogKeywords -join ', '
    }

    if ($LogLevel)
    {
        $values['LogLevel'] = $LogLevel.ToString()
    }

    if($IsWindows)
    {
        $values["Microsoft.PowerShell:ExecutionPolicy"] = "RemoteSigned"
    }

    if($ScriptBlockLogging.IsPresent)
    {
        $powerShellPolicies = @{
            ScriptBlockLogging = @{
                EnableScriptBlockLogging = $ScriptBlockLogging.IsPresent
                EnableScriptBlockInvocationLogging = $true
            }
        }

        $values['PowerShellPolicies'] = $powerShellPolicies
    }

    ConvertTo-Json -InputObject $values | Set-Content -Path $fullPath -ErrorAction Stop
    return $fullPath
}

function Get-RegEx
{
    param($SimpleMatch)

    $regex = $SimpleMatch -replace '\\', '\\'
    $regex = $regex -replace '\(', '\('
    $regex = $regex -replace '\)', '\)'
    $regex = $regex -replace '\[', '\['
    $regex = $regex -replace '\]', '\]'
    $regex = $regex -replace '\-', '\-'
    $regex = $regex -replace '\$', '\$'
    $regex = $regex -replace '\^', '\^'
    return $regex
}

Describe 'Basic SysLog tests on Linux' -Tag @('CI','RequireSudoOnUnix') {
    BeforeAll {
        [bool] $IsSupportedEnvironment = $IsLinux
        [string] $SysLogFile = [string]::Empty

        if ($IsSupportedEnvironment)
        {
            # TODO: Update to use a PowerShell specific syslog file
            if (Test-Path -Path '/var/log/syslog')
            {
                $SysLogFile = '/var/log/syslog'
            }
            elseif (Test-Path -Path '/var/log/messages')
            {
                $SysLogFile = '/var/log/messages'
            }
            else
            {
                # TODO: Look into journalctl and other variations.
                Write-Warning -Message 'Unsupported Linux syslog configuration.'
                $IsSupportedEnvironment = $false
            }
            [string] $powershell = Join-Path -Path $PSHOME -ChildPath 'pwsh'
            $scriptBlockCreatedRegExTemplate = @"
Creating Scriptblock text \(1 of 1\):#012{0}(⏎|#012)*ScriptBlock ID: [0-9a-z\-]*#012Path:.*
"@

        }
    }

    BeforeEach {
        # generate a unique log application id
        [string] $logId = [Guid]::NewGuid().ToString('N')
    }

    It 'Verifies basic logging with no customizations' -Skip:(!$IsSupportedEnvironment) {
        $configFile = WriteLogSettings -LogId $logId
        & $powershell -NoProfile -SettingsFile $configFile -Command '$env:PSModulePath | out-null'

        # Get log entries from the last 100 that match our id and are after the time we launched Powershell
        $items = Get-PSSysLog -Path $SyslogFile -Id $logId -Tail 100 -Verbose -TotalCount 3

        $items | Should -Not -Be $null
        $items.Length | Should -BeGreaterThan 1
        $items[0].EventId | Should -BeExactly 'Perftrack_ConsoleStartupStart:PowershellConsoleStartup.WinStart.Informational'
        $items[1].EventId | Should -BeExactly 'NamedPipeIPC_ServerListenerStarted:NamedPipe.Open.Informational'
        $items[2].EventId | Should -BeExactly 'Perftrack_ConsoleStartupStop:PowershellConsoleStartup.WinStop.Informational'
        # if there are more items than expected...
        if ($items.Length -gt 3)
        {
            # Force reporting of the first unexpected item to help diagnosis
            $items[3] | Should -Be $null
        }
    }

    # Skip test as it is failing in PowerShell CI on Linux platform.
    # Tracking Issue: https://github.com/PowerShell/PowerShell/issues/17092
    It 'Verifies scriptblock logging' -Skip <#-Skip:(!$IsSupportedEnvironment)#> {
        $configFile = WriteLogSettings -LogId $logId -ScriptBlockLogging -LogLevel Verbose
        $script = @'
$PID
& ([scriptblock]::create("Write-Verbose 'testheader123' ;Write-verbose 'after'"))
'@
        $testFileName = 'test01.ps1'
        $testScriptPath = Join-Path -Path $TestDrive -ChildPath $testFileName
        $script | Out-File -FilePath $testScriptPath -Force
        $null = & $powershell -NoProfile -SettingsFile $configFile -Command $testScriptPath

        # Get log entries from the last 100 that match our id and are after the time we launched Powershell
        $items = Get-PSSysLog -Path $SyslogFile -Id $logId -Tail 100 -Verbose -TotalCount 18

        $items | Should -Not -Be $null
        $items.Count | Should -BeGreaterThan 2
        $createdEvents = $items | Where-Object {$_.EventId -eq 'ScriptBlock_Compile_Detail:ExecuteCommand.Create.Verbose'}
        $createdEvents.Count | Should -BeGreaterOrEqual 3

        # Verify we log that we are executing a file
        $createdEvents[0].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f ".*/$testFileName")

        # Verify we log that we are the script to create the scriptblock
        $createdEvents[1].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f (Get-RegEx -SimpleMatch $Script.Replace([System.Environment]::NewLine,"⏎")))

        # Verify we log that we are executing the created scriptblock
        $createdEvents[2].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f "Write\-Verbose 'testheader123' ;Write\-verbose 'after'")
    }

    # Skip test as it is failing in PowerShell CI on Linux platform.
    # Tracking Issue: https://github.com/PowerShell/PowerShell/issues/17092
    It 'Verifies scriptblock logging with null character' -Skip <#-Skip:(!$IsSupportedEnvironment)#> {
        $configFile = WriteLogSettings -LogId $logId -ScriptBlockLogging -LogLevel Verbose
        $script = @'
$PID
& ([scriptblock]::create("Write-Verbose 'testheader123$([char]0x0000)' ;Write-verbose 'after'"))
'@
        $testFileName = 'test01.ps1'
        $testScriptPath = Join-Path -Path $TestDrive -ChildPath $testFileName
        $script | Out-File -FilePath $testScriptPath -Force
        $null = & $powershell -NoProfile -SettingsFile $configFile -Command $testScriptPath

        # Get log entries from the last 100 that match our id and are after the time we launched Powershell
        $items = Get-PSSysLog -Path $SyslogFile -Id $logId -Tail 100 -Verbose -TotalCount 18

        $items | Should -Not -Be $null
        $items.Count | Should -BeGreaterThan 2
        $createdEvents = $items | Where-Object {$_.EventId -eq 'ScriptBlock_Compile_Detail:ExecuteCommand.Create.Verbose'}
        $createdEvents.Count | Should -BeGreaterOrEqual 3

        # Verify we log that we are executing a file
        $createdEvents[0].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f ".*/$testFileName")

        # Verify we log that we are the script to create the scriptblock
        $createdEvents[1].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f (Get-RegEx -SimpleMatch $Script.Replace([System.Environment]::NewLine,"⏎")))

        # Verify we log that we are executing the created scriptblock
        $createdEvents[2].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f "Write\-Verbose 'testheader123␀' ;Write\-verbose 'after'")
    }

    It 'Verifies logging level filtering works' -Skip:(!$IsSupportedEnvironment) {
        $configFile = WriteLogSettings -LogId $logId -LogLevel Warning
        $result = & $powershell -NoProfile -SettingsFile $configFile -Command '$PID'
        $result | Should -Not -BeNullOrEmpty

        # by default, PowerShell only logs informational events on startup. With Level = Warning, nothing should
        # have been logged. We'll collect all the syslog entries and look for $PID (there should be none).
        $items = Get-PSSysLog -Path $SyslogFile
        @($items).Count | Should -BeGreaterThan 0
        $logs = $items | Where-Object { $_.ProcessId -eq $result }
        $logs | Should -BeNullOrEmpty
    }
}

Describe 'Basic os_log tests on MacOS' -Tag @('CI','RequireSudoOnUnix') {
    BeforeAll {
        [bool] $IsSupportedEnvironment = $IsMacOS
        [bool] $persistenceEnabled = $false

        $currentWarningPreference = $WarningPreference
        $WarningPreference = "SilentlyContinue"

        if ($IsSupportedEnvironment)
        {
            # Check the current state.
            $persistenceEnabled  = (Get-OSLogPersistence).Enabled
            if (!$persistenceEnabled)
            {
                # enable powershell log persistence to support exporting log entries
                # for each test
                Set-OsLogPersistence -Enable
            }
        }
        [string] $powershell = Join-Path -Path $PSHOME -ChildPath 'pwsh'
        $scriptBlockCreatedRegExTemplate = @'
Creating Scriptblock text \(1 of 1\):
{0}
ScriptBlock ID: [0-9a-z\-]*
Path:.*
'@
    }

    BeforeEach {
        if ($IsSupportedEnvironment)
        {
            # generate a unique log application id
            [string] $logId = [Guid]::NewGuid().ToString('N')

            # Generate a working directory and content file for Export-OSLog
            [string] $workingDirectory = Join-Path -Path $TestDrive -ChildPath $logId
            $null = New-Item -Path $workingDirectory -ItemType Directory -ErrorAction Stop

            [string] $contentFile = Join-Path -Path $workingDirectory -ChildPath ('pwsh.log.txt')
            # get log items after current time.
            [DateTime] $after = [DateTime]::Now
        }
    }

    AfterAll {
        $WarningPreference = $currentWarningPreference
        if ($IsSupportedEnvironment -and !$persistenceEnabled)
        {
            # disable persistence if it wasn't enabled
            Set-OsLogPersistence -Disable
        }
    }

    It 'Verifies basic logging with no customizations' -Skip:(!$IsMacOS) {
        try {
            $timeString = [DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss')
            $configFile = WriteLogSettings -LogId $logId
            copy-item $configFile /tmp/pwshtest.config.json
            $testPid = & $powershell -NoProfile -SettingsFile $configFile -Command '$PID'
            $items = Get-MacOsSyslogItems -processId $testPid -logId $logId

            $items | Should -Not -Be $null
            $items.Count | Should -BeGreaterThan 2
            $items.EventId | Should -Contain 'Perftrack_ConsoleStartupStart:PowershellConsoleStartup.WinStart.Informational'
            $items.EventId | Should -Contain 'NamedPipeIPC_ServerListenerStarted:NamedPipe.Open.Informational'
            $items.EventId | Should -Contain 'Perftrack_ConsoleStartupStop:PowershellConsoleStartup.WinStop.Informational'
        }
        catch {
            if (Test-Path $contentFile) {
                Send-VstsLogFile -Path $contentFile
            }
            throw
        }
    }

    It 'Verifies scriptblock logging' -Skip:(!$IsMacOS) {
        try {
            $script = @'
$PID
& ([scriptblock]::create("Write-Verbose 'testheader123' ;Write-verbose 'after'"))
'@
            $configFile = WriteLogSettings -ScriptBlockLogging -LogId $logId -LogLevel Verbose
            $testFileName = 'test01.ps1'
            $testScriptPath = Join-Path -Path $TestDrive -ChildPath $testFileName
            $script | Out-File -FilePath $testScriptPath -Force
            $testPid = & $powershell -NoProfile -SettingsFile $configFile -Command $testScriptPath
            $items = Get-MacOsSyslogItems -processId $testPid -logId $logId

            $items | Should -Not -Be $null
            $items.Count | Should -BeGreaterThan 2
            $createdEvents = $items | Where-Object {$_.EventId -eq 'ScriptBlock_Compile_Detail:ExecuteCommand.Create.Verbose'}
            $createdEvents.Count | Should -BeGreaterOrEqual 3

            $createdEvents | ConvertTo-Json | set-content /tmp/createdEvents.json

            # Verify we log that we are executing a file
            $createdEvents[0].EventMessage | Should -Match $testFileName

            # Verify we log that we are the script to create the scriptblock
            $createdEvents[1].EventMessage | Should -Match (Get-RegEx -SimpleMatch $Script)

            # Verify we log that we are executing the created scriptblock
            $createdEvents[2].EventMessage | Should -Match "Write-Verbose 'testheader123' ;Write-verbose 'after'"
        }
        catch {
            if (Test-Path $contentFile) {
                Send-VstsLogFile -Path $contentFile
            }
            throw
        }
    }

    It 'Verifies scriptblock logging with null character' -Skip:(!$IsMacOS) {
        try {
            $script = @'
$PID
& ([scriptblock]::create("Write-Verbose 'testheader123$([char]0x0000)' ;Write-verbose 'after'"))
'@
            $configFile = WriteLogSettings -ScriptBlockLogging -LogId $logId -LogLevel Verbose
            $testFileName = 'test02.ps1'
            $testScriptPath = Join-Path -Path $TestDrive -ChildPath $testFileName
            $script | Out-File -FilePath $testScriptPath -Force
            $testPid = & $powershell -NoProfile -SettingsFile $configFile -Command $testScriptPath | Select-Object -First 1

            $items = Get-MacOsSyslogItems -processId $testPid -logId $logId
            $items | convertto-json | set-content /tmp/items.json

            $createdEvents = $items | Where-Object {$_.EventId -eq 'ScriptBlock_Compile_Detail:ExecuteCommand.Create.Verbose'}

            # Verify we log that we are executing a file
            $createdEvents[0].EventMessage | Should -Match $testFileName

            # Verify we log the null in the message
            $createdEvents[1].EventMessage | Should -Match "Write-Verbose 'testheader123\`$\(\[char\]0x0000\)' ;Write-verbose 'after'"
        }
        catch {
            if (Test-Path $contentFile) {
                Send-VstsLogFile -Path $contentFile
            }
            throw
        }
    }

    # this is now specific to MacOS
    It 'Verifies logging level filtering works' -skip:(!$IsMacOs) {
        $configFile = WriteLogSettings -LogId $logId -LogLevel Warning
        $testPid = & $powershell -NoLogo -NoProfile -SettingsFile $configFile -Command '$PID'

        $items = Get-MacOsSyslogItems -processId $testPid -logId $logId
        $items | Should -Be $null -Because ("{0} Warning event logs were found" -f @($items).Count)
    }
}

Describe 'Basic EventLog tests on Windows' -Tag @('CI','RequireAdminOnWindows') {
    BeforeAll {
        [bool] $IsSupportedEnvironment = $IsWindows
        [string] $powershell = Join-Path -Path $PSHOME -ChildPath 'pwsh'

        $currentWarningPreference = $WarningPreference
        $WarningPreference = "SilentlyContinue"

        $scriptBlockLoggingCases = @(
            @{
                name = 'normal script block'
                script = "Write-Verbose 'testheader123' ;Write-verbose 'after'"
                expectedText="Write-Verbose 'testheader123' ;Write-verbose 'after'`r`n"
            }
            @{
                name = 'script block with Null'
                script = "Write-Verbose 'testheader123$([char]0x0000)' ;Write-verbose 'after'"
                expectedText="Write-Verbose 'testheader123␀' ;Write-verbose 'after'`r`n"
            }
        )

        if ($IsSupportedEnvironment)
        {
            & "$PSHOME\RegisterManifest.ps1"
        }
    }

    AfterAll {
        $WarningPreference = $currentWarningPreference
    }

    BeforeEach {
        if ($IsSupportedEnvironment)
        {
            # generate a unique log application id
            [string] $logId = [Guid]::NewGuid().ToString('N')

            $logName = 'PowerShellCore'

            # get log items after current time.
            [DateTime] $after = [DateTime]::Now
            Clear-PSEventLog -Name "$logName/Operational"
        }
    }

    It 'Verifies scriptblock logging: <name>' -Skip:(!$IsSupportedEnvironment) -TestCases $scriptBlockLoggingCases {
        param(
            [string] $script,
            [string] $expectedText,
            [string] $name
        )
        $configFile = WriteLogSettings -ScriptBlockLogging -LogId $logId
        $testFileName = 'test01.ps1'
        $testScriptPath = Join-Path -Path $TestDrive -ChildPath $testFileName
        $script | Out-File -FilePath $testScriptPath -Force
        $null = & $powershell -NoProfile -SettingsFile $configFile -Command $testScriptPath

        $created = Wait-PSWinEvent -FilterHashtable @{ ProviderName=$logName; Id = 4104 } `
            -PropertyName Message -PropertyValue $expectedText

        $created | Should -Not -BeNullOrEmpty
        $created.Properties[0].Value | Should -Be 1
        $created.Properties[1].Value | Should -Be 1
        $created.Properties[2].Value | Should -Be $expectedText
    }
}
