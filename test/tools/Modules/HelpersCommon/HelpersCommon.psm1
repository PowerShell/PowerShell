# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
function Wait-UntilTrue
{
    [CmdletBinding()]
    param (
        [ScriptBlock]$sb,
        [int]$TimeoutInMilliseconds = 10000,
        [int]$IntervalInMilliseconds = 1000
        )
    # Get the current time
    $startTime = [DateTime]::Now

    # Loop until the script block evaluates to true
    while (-not ($sb.Invoke())) {
        # If the timeout period has passed, return false
        if (([DateTime]::Now - $startTime).TotalMilliseconds -gt $timeoutInMilliseconds) {
            return $false
        }
        # Wait
        Start-Sleep -Milliseconds $intervalInMilliseconds > $null
    }
    return $true
}

function Wait-FileToBePresent
{
    [CmdletBinding()]
    param (
        [string]$File,
        [int]$TimeoutInSeconds = 10,
        [int]$IntervalInMilliseconds = 100
    )

    return Wait-UntilTrue -sb { Test-Path $File } -TimeoutInMilliseconds ($TimeoutInSeconds*1000) -IntervalInMilliseconds $IntervalInMilliseconds
}

function Test-IsElevated
{
    $IsElevated = $false
    if ( $IsWindows ) {
        # on Windows we can determine whether we're executing in an
        # elevated context
        $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
        $windowsPrincipal = New-Object 'Security.Principal.WindowsPrincipal' $identity
        if ($windowsPrincipal.IsInRole("Administrators") -eq 1)
        {
            $IsElevated = $true
        }
    }
    else {
        # on Linux, tests run via sudo will generally report "root" for whoami
        if ( (whoami) -match "root" ) {
            $IsElevated = $true
        }
    }
    return $IsElevated
}
function Get-RandomFileName
{
    [System.IO.Path]::GetFileNameWithoutExtension([IO.Path]::GetRandomFileName())
}

#
# Testhook setting functions
# note these manipulate private data in the PowerShell engine which will
# enable us to not actually alter the system or mock returned data
#
$SCRIPT:TesthookType = [system.management.automation.internal.internaltesthooks]
function Test-TesthookIsSet
{
	[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingEmptyCatchBlock", '')] # , Justification = "an error message is not appropriate for this function")]
    param (
        [ValidateNotNullOrEmpty()]
        [Parameter(Mandatory=$true)]
        $testhookName
    )
    try {
        return ${Script:TesthookType}.GetField($testhookName, "NonPublic,Static").GetValue($null)
    }
    catch {
        # fall through
    }
    return $false
}

function Enable-Testhook
{
    param (
        [ValidateNotNullOrEmpty()]
        [Parameter(Mandatory=$true)]
        $testhookName
    )
    ${Script:TesthookType}::SetTestHook($testhookName, $true)
}

function Disable-Testhook
{
    param (
        [ValidateNotNullOrEmpty()]
        [Parameter(Mandatory=$true)]
        $testhookName
    )
    ${Script:TesthookType}::SetTestHook($testhookName, $false)
}

function Set-TesthookResult
{
    param (
        [ValidateNotNullOrEmpty()]
        [Parameter(Mandatory=$true)]
        $testhookName,
        [ValidateNotNullOrEmpty()]
        [Parameter(Mandatory=$true)]
        $value
    )
    ${Script:TesthookType}::SetTestHook($testhookName, $value)
}

function Add-TestDynamicType
{
    param()

    Add-Type -TypeDefinition @'
using System.Collections.Generic;
using System.Dynamic;

public class TestDynamic : DynamicObject
{
    private static readonly string[] s_dynamicMemberNames = new string[] { "FooProp", "BarProp", "FooMethod", "SerialNumber" };

    private static int s_lastSerialNumber;

    private readonly int _serialNumber;

    public TestDynamic()
    {
        _serialNumber = ++s_lastSerialNumber;
    }

    public override IEnumerable<string> GetDynamicMemberNames()
    {
        return s_dynamicMemberNames;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        result = null;

        if (binder.Name == "FooProp")
        {
            result = 123;
            return true;
        }
        else if (binder.Name == "BarProp")
        {
            result = 456;
            return true;
        }
        else if (binder.Name == "SerialNumber")
        {
            result = _serialNumber;
            return true;
        }
        else if (binder.Name == "HiddenProp")
        {
            // Not presented in GetDynamicMemberNames
            result = 789;
            return true;
        }

        return false;
    }

    public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
    {
        result = null;

        if (binder.Name == "FooMethod")
        {
            result = "yes";
            return true;
        }
        else if (binder.Name == "HiddenMethod")
        {
            // Not presented in GetDynamicMemberNames
            result = _serialNumber;
            return true;
        }

        return false;
    }
}
'@
}

# Upload an artifact in VSTS
# On other systems will just log where the file was placed
function Send-VstsLogFile {
	[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingWriteHost', '', Justification = "needed for VSO")]
    param (
        [parameter(Mandatory,ParameterSetName='contents')]
        [string[]]
        $Contents,
        [parameter(Mandatory,ParameterSetName='contents')]
        [string]
        $LogName,
        [parameter(Mandatory,ParameterSetName='path')]
        [ValidateScript({Test-Path -Path $_})]
        [string]
        $Path
    )

    $logFolder = Join-Path -Path $PWD -ChildPath 'logfile'
    if(!(Test-Path -Path $logFolder))
    {
        $null = New-Item -Path $logFolder -ItemType Directory
        if($IsMacOS -or $IsLinux)
        {
            $null = chmod a+rw $logFolder
        }
    }

    $newName = ([System.Io.Path]::GetRandomFileName() + "-$LogName.txt")
    if($Contents)
    {
        $logFile = Join-Path -Path $logFolder -ChildPath $newName

        $Contents | Out-File -path $logFile -Encoding ascii
    }
    else
    {
        $logFile = Join-Path -Path $logFolder -ChildPath $newName
        Copy-Item -Path $Path -Destination $logFile
    }

    Write-Host "##vso[artifact.upload containerfolder=$newName;artifactname=$newName]$logFile"
    Write-Verbose "Log file captured as $newName" -Verbose
}

# Tests if the Linux or macOS user is root
function Test-IsRoot
{
    if($IsLinux -or $IsMacOS)
    {
        $uid = &id -u
        if($uid -eq 0)
        {
            return $true
        }
    }

    return $false
}

# Tests if we are running is a VSTS Linux Build
function Test-IsVstsLinux
{
    return ($env:TF_BUILD -and $IsLinux)
}

# Tests if we are running is a VSTS Linux Build
function Test-IsVstsWindows
{
    return ($env:TF_BUILD -and $IsWindows)
}

# this function wraps native command Execution
# for more information, read https://mnaoumov.wordpress.com/2015/01/11/execution-of-external-commands-in-powershell-done-right/
function Start-NativeExecution
{
    param(
        [scriptblock]$sb,
        [switch]$IgnoreExitcode,
        [switch]$VerboseOutputOnError
    )
    $backupEAP = $script:ErrorActionPreference
    $script:ErrorActionPreference = "Continue"
    try {
        if($VerboseOutputOnError.IsPresent)
        {
            $output = & $sb 2>&1
        }
        else
        {
            & $sb
        }

        # note, if $sb doesn't have a native invocation, $LASTEXITCODE will
        # point to the obsolete value
        if ($LASTEXITCODE -ne 0 -and -not $IgnoreExitcode) {
            if($VerboseOutputOnError.IsPresent -and $output)
            {
                $output | Out-String | Write-Verbose -Verbose
            }

            # Get caller location for easier debugging
            $caller = Get-PSCallStack -ErrorAction SilentlyContinue
            if($caller)
            {
                $callerLocationParts = $caller[1].Location -split ":\s*line\s*"
                $callerFile = $callerLocationParts[0]
                $callerLine = $callerLocationParts[1]

                $errorMessage = "Execution of {$sb} by ${callerFile}: line $callerLine failed with exit code $LASTEXITCODE"
                throw $errorMessage
            }
            throw "Execution of {$sb} failed with exit code $LASTEXITCODE"
        }
    } finally {
        $script:ErrorActionPreference = $backupEAP
    }
}

# Creates a new random hex string for use with things like test certificate passwords
function New-RandomHexString
{
    param([int]$Length = 10)

    $random = [Random]::new()
    return ((1..$Length).ForEach{ '{0:x}' -f $random.Next(0xf) }) -join ''
}

$script:CanWriteToPsHome = $null
function Test-CanWriteToPsHome
{
	[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingEmptyCatchBlock', '', Justification = "an error message is not appropriate for this function")]
	param ()
    if ($null -ne $script:CanWriteToPsHome) {
        return $script:CanWriteToPsHome
    }

    $script:CanWriteToPsHome = $false

    try {
        $testFileName = Join-Path $PSHOME (New-Guid).Guid
        $null = New-Item -ItemType File -Path $testFileName -ErrorAction Stop
        $script:CanWriteToPsHome = $true
        Remove-Item -Path $testFileName -ErrorAction SilentlyContinue
    }
    catch {
        ; # do nothing
    }

    $script:CanWriteToPsHome
}

# Creates a password meeting Windows complexity rules
function New-ComplexPassword
{
    $numbers = "0123456789"
    $lowercase = "abcdefghijklmnopqrstuvwxyz"
    $uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
    $symbols = "~!@#$%^&*_-+=``|\(){}[]:;`"'<>,.?/"
    $password = [string]::Empty
    # Windows password complexity rule requires minimum 8 characters and using at least 3 of the
    # buckets above, so we just pick one from each bucket twice.
    # https://learn.microsoft.com/windows/security/threat-protection/security-policy-settings/password-must-meet-complexity-requirements
    1..2 | ForEach-Object {
        $Password += $numbers[(Get-Random $numbers.Length)] + $lowercase[(Get-Random $lowercase.Length)] +
            $uppercase[(Get-Random $uppercase.Length)] + $symbols[(Get-Random $symbols.Length)]
    }

    $password
}

# return a specific string with regard to platform information
function Get-PlatformInfo {
    if ( $IsWindows ) {
        return @{Platform = "windows"; Version = '' }
    }
    if ( $IsMacOS ) {
        return @{Platform = "macos"; Version = sw_vers -productversion }
    }
    if ( $IsLinux ) {
        $osrelease = Get-Content /etc/os-release | ConvertFrom-StringData
        if ( -not [string]::IsNullOrEmpty($osrelease.ID) ) {

            $versionId = if (-not $osrelease.Version_ID ) {
                ''
            } else {
                $osrelease.Version_ID.trim('"')
            }

            $platform = $osrelease.ID.trim('"')

            return @{Platform = $platform; Version = $versionId }
        }
        return @{ Platform = "linux"; version = "unknown" }
    }
}

# return true if WsMan is supported on the current platform
function Get-WsManSupport {
    $platformInfo = Get-PlatformInfo
    if (
        ($platformInfo.Platform -eq 'centos' -and $platformInfo.Version -eq '7')
    ) {
        return $true
    }
    return $false
}

function Test-IsWindowsArm64 {
    return $IsWindows -and [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64
}

function Test-IsWinWow64 {
    return $IsWindows -and [System.Environment]::Is64BitOperatingSystem -and -not [System.Environment]::Is64BitProcess
}

function Test-IsPreview
{
    param(
        [parameter(Mandatory)]
        [string]
        $Version,

        [switch]$IsLTS
    )

    if ($IsLTS.IsPresent) {
        ## If we are building a LTS package, then never consider it preview.
        return $false
    }

    return $Version -like '*-*'
}

<#
    .Synopsis
        Tests if a version is a Release Candidate
    .EXAMPLE
        Test-IsReleaseCandidate -version '6.1.0-sometthing' # returns false
        Test-IsReleaseCandidate -version '6.1.0-rc.1' # returns true
        Test-IsReleaseCandidate -version '6.1.0' # returns false
#>
function Test-IsReleaseCandidate
{
    param(
        [parameter(Mandatory)]
        [string]
        $Version
    )

    if ($Version -like '*-rc.*')
    {
        return $true
    }

    return $false
}

function Test-IsWinServer2012R2
{
    if (-not $IsWindows) {
        return $false
    }

    $osInfo = [System.Environment]::OSVersion.Version
    return ($osInfo.Major -eq 6 -and $osInfo.Minor -eq 3)
}

function Test-IsWindows2016 {
    if (-not $IsWindows) {
        return $false
    }

    $osInfo = [System.Environment]::OSVersion.Version
    return ($osInfo.Major -eq 10 -and $osInfo.Minor -eq 0 -and $osInfo.Build -eq 14393)
}


# helpers for managing psdefaultparametervalues
[system.collections.generic.Stack[hashtable]]$script:DefaultParameterValueStack = [system.collections.generic.Stack[hashtable]]::new()

# Ensure that the global:PSDefaultParameterValues variable is a hashtable
function Initialize-PSDefaultParameterValue {
	if ( $global:PSDefaultParameterValues -isnot [hashtable] ) {
		$global:PSDefaultParameterValues = @{}
	}
}

# reset the stack
function Reset-DefaultParameterValueStack {
	$script:DefaultParameterValueStack = [system.collections.generic.Stack[hashtable]]::new()
    Initialize-PSDefaultParameterValue
}

# return the current stack
function Get-DefaultParameterValueStack {
	$script:DefaultParameterValueStack
}

# PSDefaultParameterValue may not have both skip and pending keys
function Test-PSDefaultParameterValue {
    if ( $global:PSDefaultParameterValues -is [hashtable] ) {
        if ( $global:PSDefaultParameterValues.ContainsKey('skip') -and $global:PSDefaultParameterValues.ContainsKey('pending') ) {
            return $false
        }
        return $true
    }
    Initialize-PSDefaultParameterValue
}

# push a new value onto the stack
# if $ht is null, then the current value of $global:PSDefaultParameterValues is pushed
# if $NewValue is used, then $ht is used as the new value of $global:PSDefaultParameterValues
function Push-DefaultParameterValueStack {
	param ([hashtable]$ht, [switch]$NewValue)
    Initialize-PSDefaultParameterValue

	$script:DefaultParameterValueStack.Push($global:PSDefaultParameterValues.Clone())
	if ( $ht ) {
		if ( $NewValue ) {
			$global:PSDefaultParameterValues = $ht
		}
		else {
			foreach ($k in $ht.Keys) {
				$global:PSDefaultParameterValues[$k] = $ht[$k]
			}
		}
        if ( ! (Test-PSDefaultParameterValue)) {
            Write-Warning -Message "PSDefaultParameterValues may not have both skip and pending keys, resetting."
            Pop-DefaultParameterValueStack
        }
	}
}

function Pop-DefaultParameterValueStack {
	try {
		$global:PSDefaultParameterValues = $script:DefaultParameterValueStack.Pop()
		return $true
	}
	catch {
        Initialize-PSDefaultParameterValue
		return $false
	}
}

function Get-HelpNetworkTestCases
{
    param(
        [switch]
        $PositiveCases
    )
    # .NET doesn't consider these path rooted and we won't go to the network:
    # \\?
    # \\.
    # \??

    # Command discovery does not follow symlinks to network locations for module qualified paths
    $networkBlockedError = "CommandNameNotAllowed,Microsoft.PowerShell.Commands.GetHelpCommand"
    $scriptBlockedError = "ScriptsNotAllowed"

    $formats = @(
        '//{0}/share/{1}'
        '\\{0}\share\{1}'
        '//{0}\share/{1}'
        'Microsoft.PowerShell.Core\filesystem:://{0}/share/{1}'
    )

    if (!$PositiveCases) {
        $formats += 'filesystem:://{0}/share/{1}'
    }

    $moduleQualifiedCommand = 'test.dll\fakecommand'
    $lanManFormat = @(
        '//;LanmanRedirector/{0}/share/{1}'
    )

    $hosts = @(
        'fakehost'
        'fakehost.pstest'
    )

    $commands = @(
        'test.ps1'
        'test.dll'
        $moduleQualifiedCommand
    )

    $variants = @()
    $cases = @()
    foreach($command in $commands)  {
        $hostName = $hosts[0]
        $format = $formats[0]
        $cases += @{
            Command = $format -f $hostName, $command
            ExpectedError = $networkBlockedError
        }
    }

    foreach($hostName in $hosts) {
        # chose the format with backslashes(\) to match the host with blackslashes
        $format = $formats[1]
        $command = $commands[0]
        $cases += @{
            Command = $format -f $hostName, $command
            ExpectedError = $networkBlockedError
        }
    }
    foreach($format in $formats) {
        $hostName = $hosts[0]
        $command = $commands[0]
        $cases += @{
            Command = $format -f $hostName, $command
            ExpectedError = $networkBlockedError
        }
    }

    foreach($format in $lanManFormat) {
        $hostName = $hosts[0]
        $command = $moduleQualifiedCommand
        $cases += @{
            Command = $format -f $hostName, $command
            ExpectedError = $scriptBlockedError
        }
    }

    return $cases | Sort-Object -Property ExpectedError, Command -Unique
}

