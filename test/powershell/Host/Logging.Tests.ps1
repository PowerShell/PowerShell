# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
using namespace System.Text

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module HelpersCommon
Import-Module PSSysLog

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
            [string] $powershell = Join-Path -Path $PSHome -ChildPath 'pwsh'
            $scriptBlockCreatedRegExTemplate = @'
Creating Scriptblock text \(1 of 1\):#012{0}(#012)*ScriptBlock ID: [0-9a-z\-]*#012Path:.*
'@

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

    It 'Verifies scriptblock logging' -Skip:(!$IsSupportedEnvironment) {
        $configFile = WriteLogSettings -LogId $logId -ScriptBlockLogging -LogLevel Verbose
        $script = @'
$pid
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
        $createdEvents = $items | where-object {$_.EventId -eq 'ScriptBlock_Compile_Detail:ExecuteCommand.Create.Verbose'}
        $createdEvents.Count | should -BeGreaterOrEqual 3

        # Verify we log that we are executing a file
        $createdEvents[0].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f ".*/$testFileName")

        # Verify we log that we are the script to create the scriptblock
        $createdEvents[1].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f (Get-RegEx -SimpleMatch $Script.Replace([System.Environment]::NewLine,'#012')))

        # Verify we log that we are excuting the created scriptblock
        $createdEvents[2].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f "Write\-Verbose 'testheader123' ;Write\-verbose 'after'")
    }

    It 'Verifies scriptblock logging with null character' -Skip:(!$IsSupportedEnvironment) {
        $configFile = WriteLogSettings -LogId $logId -ScriptBlockLogging -LogLevel Verbose
        $script = @'
$pid
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
        $createdEvents = $items | where-object {$_.EventId -eq 'ScriptBlock_Compile_Detail:ExecuteCommand.Create.Verbose'}
        $createdEvents.Count | should -BeGreaterOrEqual 3

        # Verify we log that we are executing a file
        $createdEvents[0].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f ".*/$testFileName")

        # Verify we log that we are the script to create the scriptblock
        $createdEvents[1].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f (Get-RegEx -SimpleMatch $Script.Replace([System.Environment]::NewLine,'#012')))

        # Verify we log that we are excuting the created scriptblock
        $createdEvents[2].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f "Write\-Verbose 'testheader123␀' ;Write\-verbose 'after'")
    }

    It 'Verifies logging level filtering works' -Skip:(!$IsSupportedEnvironment) {
        $configFile = WriteLogSettings -LogId $logId -LogLevel Warning
        & $powershell -NoProfile -SettingsFile $configFile -Command '$env:PSModulePath | out-null'

        # by default, PowerShell only logs informational events on startup. With Level = Warning, nothing should
        # have been logged.
        $items = Get-PSSysLog -Path $SyslogFile -Id $logId -Tail 100 -TotalCount 1
        $items | Should -Be $null
    }
}

Describe 'Basic os_log tests on MacOS' -Tag @('CI','RequireSudoOnUnix') {
    BeforeAll {
        [bool] $IsSupportedEnvironment = $IsMacOS
        [bool] $persistenceEnabled = $false

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
        [string] $powershell = Join-Path -Path $PSHome -ChildPath 'pwsh'
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
        if ($IsSupportedEnvironment -and !$persistenceEnabled)
        {
            # disable persistence if it wasn't enabled
            Set-OsLogPersistence -Disable
        }
    }

    It 'Verifies basic logging with no customizations' -Skip:(!$IsSupportedEnvironment) {
        try {
            $configFile = WriteLogSettings -LogId $logId
            $testPid = & $powershell -NoProfile -SettingsFile $configFile -Command '$PID'

            Export-PSOsLog -After $after -LogPid $testPid -TimeoutInMilliseconds 30000 -IntervalInMilliseconds 3000 -MinimumCount 3 |
                Set-Content -Path $contentFile
            $items = @(Get-PSOsLog -Path $contentFile -Id $logId -After $after -TotalCount 3 -Verbose)

            $items | Should -Not -Be $null
            $items.Count | Should -BeGreaterThan 2
            $items[0].EventId | Should -BeExactly 'Perftrack_ConsoleStartupStart:PowershellConsoleStartup.WinStart.Informational'
            $items[1].EventId | Should -BeExactly 'NamedPipeIPC_ServerListenerStarted:NamedPipe.Open.Informational'
            $items[2].EventId | Should -BeExactly 'Perftrack_ConsoleStartupStop:PowershellConsoleStartup.WinStop.Informational'
            # if there are more items than expected...
            if ($items.Count -gt 3)
            {
                # Force reporting of the first unexpected item to help diagnosis
                $items[3] | Should -Be $null
            }
        }
        catch {
            if (Test-Path $contentFile) {
                Send-VstsLogFile -Path $contentFile
            }
            throw
        }
    }

    It 'Verifies scriptblock logging' -Skip:(!$IsSupportedEnvironment) {
        try {
            $script = @'
$pid
& ([scriptblock]::create("Write-Verbose 'testheader123' ;Write-verbose 'after'"))
'@
            $configFile = WriteLogSettings -ScriptBlockLogging -LogId $logId -LogLevel Verbose
            $testFileName = 'test01.ps1'
            $testScriptPath = Join-Path -Path $TestDrive -ChildPath $testFileName
            $script | Out-File -FilePath $testScriptPath -Force
            $testPid = & $powershell -NoProfile -SettingsFile $configFile -Command $testScriptPath

            Export-PSOsLog -After $after -LogPid $testPid -TimeoutInMilliseconds 30000 -IntervalInMilliseconds 3000 -MinimumCount 17 |
                Set-Content -Path $contentFile
            $items = @(Get-PSOsLog -Path $contentFile -Id $logId -After $after -Verbose)

            $items | Should -Not -Be $null
            $items.Count | Should -BeGreaterThan 2
            $createdEvents = $items | where-object {$_.EventId -eq 'ScriptBlock_Compile_Detail:ExecuteCommand.Create.Verbose'}
            $createdEvents.Count | should -BeGreaterOrEqual 3

            # Verify we log that we are executing a file
            $createdEvents[0].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f ".*/$testFileName")

            # Verify we log that we are the script to create the scriptblock
            $createdEvents[1].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f (Get-RegEx -SimpleMatch $Script))

            # Verify we log that we are excuting the created scriptblock
            $createdEvents[2].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f "Write\-Verbose 'testheader123' ;Write\-verbose 'after'")
        }
        catch {
            if (Test-Path $contentFile) {
                Send-VstsLogFile -Path $contentFile
            }
            throw
        }
    }

    It 'Verifies scriptblock logging with null character' -Skip:(!$IsSupportedEnvironment) {
        try {
            $script = @'
$pid
& ([scriptblock]::create("Write-Verbose 'testheader123$([char]0x0000)' ;Write-verbose 'after'"))
'@
            $configFile = WriteLogSettings -ScriptBlockLogging -LogId $logId -LogLevel Verbose
            $testFileName = 'test01.ps1'
            $testScriptPath = Join-Path -Path $TestDrive -ChildPath $testFileName
            $script | Out-File -FilePath $testScriptPath -Force
            $testPid = & $powershell -NoProfile -SettingsFile $configFile -Command $testScriptPath

            Export-PSOsLog -After $after -LogPid $testPid -TimeoutInMilliseconds 30000 -IntervalInMilliseconds 3000 -MinimumCount 17 |
                Set-Content -Path $contentFile
            $items = @(Get-PSOsLog -Path $contentFile -Id $logId -After $after -Verbose)

            $items | Should -Not -Be $null
            $items.Count | Should -BeGreaterThan 2
            $createdEvents = $items | where-object {$_.EventId -eq 'ScriptBlock_Compile_Detail:ExecuteCommand.Create.Verbose'}
            $createdEvents.Count | should -BeGreaterOrEqual 3

            # Verify we log that we are executing a file
            $createdEvents[0].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f ".*/$testFileName")

            # Verify we log that we are the script to create the scriptblock
            $createdEvents[1].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f (Get-RegEx -SimpleMatch $Script))

            # Verify we log that we are excuting the created scriptblock
            $createdEvents[2].Message | Should -Match ($scriptBlockCreatedRegExTemplate -f "Write\-Verbose 'testheader123␀' ;Write\-verbose 'after'")
        }
        catch {
            if (Test-Path $contentFile) {
                Send-VstsLogFile -Path $contentFile
            }
            throw
        }
    }

    # This is pending because it results in false postitives (-Skip:(!$IsSupportedEnvironment) )
    It 'Verifies logging level filtering works' -Pending {
        try {
            $configFile = WriteLogSettings -LogId $logId -LogLevel Warning
            $testPid = & $powershell -NoLogo -NoProfile -SettingsFile $configFile -Command '$PID'

            Export-PSOsLog -After $after -LogPid $testPid |
                Set-Content -Path $contentFile
            # by default, powershell startup should only logs informational events.
            # With Level = Warning, nothing should be logged.
            $items = Get-PSOsLog -Path $contentFile -Id $logId -After $after -TotalCount 3
            $items | Should -Be $null
        }
        catch {
            if (Test-Path $contentFile) {
                Send-VstsLogFile -Path $contentFile
            }
            throw
        }
    }
}

Describe 'Basic EventLog tests on Windows' -Tag @('CI','RequireAdminOnWindows') {
    BeforeAll {
        [bool] $IsSupportedEnvironment = $IsWindows
        [string] $powershell = Join-Path -Path $PSHome -ChildPath 'pwsh'
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
            & "$PSHome\RegisterManifest.ps1"
        }
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
