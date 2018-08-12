# Copyright (c) Microsoft Corporation. All rights reserved.
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
        # Sleep for the specified interval
        Start-Sleep -Milliseconds $intervalInMilliseconds
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

    Wait-UntilTrue -sb { Test-Path $File } -TimeoutInMilliseconds ($TimeoutInSeconds*1000) -IntervalInMilliseconds $IntervalInMilliseconds > $null
}

function Test-IsElevated
{
    $IsElevated = $False
    if ( $IsWindows ) {
        # on Windows we can determine whether we're executing in an
        # elevated context
        $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
        $windowsPrincipal = new-object 'Security.Principal.WindowsPrincipal' $identity
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

    $logFolder = Join-Path -path $pwd -ChildPath 'logfile'
    if(!(Test-Path -Path $logFolder))
    {
        $null = New-Item -Path $logFolder -ItemType Directory
        if($IsMacOS -or $IsLinux)
        {
            $null = chmod a+rw $logFolder
        }
    }

    if($Contents)
    {
        $logFile = Join-Path -Path $logFolder -ChildPath ([System.Io.Path]::GetRandomFileName() + "-$LogName.txt")
        $name = Split-Path -leaf -Path $logFile

        $Contents | out-file -path $logFile -Encoding ascii
    }
    else
    {
        $name = Split-Path -leaf -Path $path
        $logFile = Join-Path -Path $logFolder -ChildPath ([System.Io.Path]::GetRandomFileName() + '-' + $name)
        Copy-Item -Path $Path -Destination $logFile
    }

    Write-Host "##vso[artifact.upload containerfolder=$name;artifactname=$name]$logFile"
    Write-Verbose "Log file captured as $name" -Verbose
}
