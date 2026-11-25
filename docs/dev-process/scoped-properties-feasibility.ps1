# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Diagnostic only: compare the legacy $? expression with a helper getter that
# reads the same ExecutionContext flag in isolated runspaces. No product files
# are changed. Run with `pwsh -NoProfile -File ./docs/dev-process/scoped-properties-feasibility.ps1`.

$source = @'
using System;
using System.Reflection;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

public sealed class ScopedPropertiesQuestionMarkProbe
{
    private static readonly Assembly s_assembly = typeof(PSObject).Assembly;
    private static readonly Type s_pipeline = s_assembly.GetType("System.Management.Automation.Runspaces.LocalPipeline", true);
    private static readonly MethodInfo s_getContext = s_pipeline.GetMethod("GetExecutionContextFromTLS", BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly PropertyInfo s_value = s_assembly.GetType("System.Management.Automation.ExecutionContext", true)
        .GetProperty("QuestionMarkVariableValue", BindingFlags.NonPublic | BindingFlags.Instance);

    public bool Value => (bool)s_value.GetValue(s_getContext.Invoke(null, null));
}

[Cmdlet("Invoke", "ScopedProbeWriteError")]
public sealed class ScopedProbeWriteErrorCommand : PSCmdlet
{
    protected override void ProcessRecord()
    {
        WriteError(new ErrorRecord(new InvalidOperationException("expected probe error"),
            "ScopedProbeExpectedError", ErrorCategory.NotSpecified, null));
    }
}
'@

if (-not ('ScopedPropertiesQuestionMarkProbe' -as [type])) {
    Add-Type -TypeDefinition $source
}

$reader = [ScopedPropertiesQuestionMarkProbe]::new()

function Invoke-IsolatedExpression {
    param(
        [Parameter(Mandatory)] [string] $Trigger,
        [Parameter(Mandatory)] [string] $Expression,
        [switch] $ImportProbeCmdlet
    )

    $runspace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
    $runspace.Open()
    $powershell = [System.Management.Automation.PowerShell]::Create()
    $powershell.Runspace = $runspace

    $setup = '$reader = $args[0]; $ErrorActionPreference = ''Continue'';'
    if ($ImportProbeCmdlet) {
        $setup += ' Import-Module -Assembly ([ScopedPropertiesQuestionMarkProbe].Assembly);'
    }

    try {
        $powershell.AddScript("$setup $Trigger; $Expression").AddArgument($reader) | Out-Null
        $output = @($powershell.Invoke())
        return ,$output
    }
    finally {
        $powershell.Dispose()
        $runspace.Dispose()
    }
}

$forms = @(
    @{ Name = 'direct'; Legacy = '$?'; Getter = '$reader.Value' }
    @{ Name = 'parentheses'; Legacy = '($?)'; Getter = '($reader.Value)' }
    @{ Name = 'subexpression'; Legacy = '$($?)'; Getter = '$($reader.Value)' }
    @{ Name = 'array'; Legacy = '@($?)'; Getter = '@($reader.Value)' }
    @{ Name = 'interpolation'; Legacy = '"$?"'; Getter = '"$($reader.Value)"' }
)

$triggers = @(
    @{ Name = 'success'; Script = '$null = 1'; Import = $false; Expected = $true }
    @{ Name = 'Write-Error'; Script = "Write-Error 'expected probe error' -ErrorAction Continue"; Import = $false; Expected = $false }
    @{ Name = 'PSCmdlet.WriteError'; Script = 'Invoke-ScopedProbeWriteError -ErrorAction Continue'; Import = $true; Expected = $false }
)

if ($IsWindows) {
    $triggers += @{ Name = 'native exit 7'; Script = 'cmd.exe /d /c exit 7'; Import = $false; Expected = $false }
}

$results = [System.Collections.Generic.List[object]]::new()
foreach ($trigger in $triggers) {
    foreach ($form in $forms) {
        $legacy = Invoke-IsolatedExpression -Trigger $trigger.Script -Expression $form.Legacy -ImportProbeCmdlet:$trigger.Import
        $facade = Invoke-IsolatedExpression -Trigger $trigger.Script -Expression $form.Getter -ImportProbeCmdlet:$trigger.Import
        $legacyValue = if ($legacy.Count -eq 1) { $legacy[0] } else { ,$legacy }
        $facadeValue = if ($facade.Count -eq 1) { $facade[0] } else { ,$facade }
        $equal = [System.Management.Automation.LanguagePrimitives]::Equals($legacyValue, $facadeValue)
        $expected = if ($form.Name -eq 'interpolation') {
            [string]$legacyValue -eq $(if ($trigger.Expected) { 'True' } else { 'False' })
        }
        else {
            $legacy.Count -eq 1 -and [bool]$legacyValue -eq $trigger.Expected
        }
        $results.Add([pscustomobject]@{
            Trigger = $trigger.Name
            Form = $form.Name
            Legacy = $legacyValue
            Getter = $facadeValue
            Equal = $equal
            ExpectedLegacyResult = $expected
        })
    }
}

# Check native-command state independently: reading any value after the exit
# would itself change $? and invalidate that comparison.
$nativeExitCode = $null
if ($IsWindows) {
    $runspace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
    $runspace.Open()
    $powershell = [System.Management.Automation.PowerShell]::Create()
    $powershell.Runspace = $runspace
    try {
        $nativeOutput = @($powershell.AddScript('cmd.exe /d /c exit 7; $LASTEXITCODE').Invoke())
        $nativeExitCode = $nativeOutput[-1]
    }
    finally {
        $powershell.Dispose()
        $runspace.Dispose()
    }
}

# Record legacy tuple behavior for a nested function and a dotted scriptblock.
# The values are read while their producer frames are active; this does not
# emulate the proposed facade resolver.
function Get-ScopedProbeInner {
    [pscustomobject]@{
        Arguments = @($args)
        InvocationName = $MyInvocation.InvocationName
        CommandPath = $MyInvocation.PSCommandPath
        ScriptRoot = $PSScriptRoot
    }
}

function Get-ScopedProbeOuter {
    $outerArguments = @($args)
    $inner = Get-ScopedProbeInner inner-argument
    $dotted = . {
        [pscustomobject]@{
            Arguments = @($args)
            InvocationName = $MyInvocation.InvocationName
            CommandPath = $MyInvocation.PSCommandPath
            ScriptRoot = $PSScriptRoot
        }
    } dotted-argument

    $restoredOuterArguments = @($args)
    [pscustomobject]@{ OuterArguments = $outerArguments; Inner = $inner; Dotted = $dotted; RestoredOuterArguments = $restoredOuterArguments }
}

$invocationFrames = Get-ScopedProbeOuter outer-argument
$strictModePresence = & {
    Set-StrictMode -Version 2
    $unsetThrows = $false
    try { & { $this } | Out-Null } catch { $unsetThrows = $true }
    $nullValue = $null
    $assignedNullThrows = $false
    try { & { $this = $null; $nullValue = $this } } catch { $assignedNullThrows = $true }
    [pscustomobject]@{ UnsetAutomaticVariableThrows = $unsetThrows; AssignedNullThrows = $assignedNullThrows; AssignedNull = $nullValue }
}

[pscustomobject]@{
    HostVersion = $PSVersionTable.PSVersion.ToString()
    GetterAssembly = [System.Management.Automation.PSObject].Assembly.Location
    NativeExitCode = $nativeExitCode
    Results = $results.ToArray()
    LegacyInvocationFrames = $invocationFrames
    StrictModePresence = $strictModePresence
} | ConvertTo-Json -Depth 6

if (@($results | Where-Object { -not $_.Equal -or -not $_.ExpectedLegacyResult }).Count -gt 0) {
    throw 'Question-mark getter and legacy expression results differed; inspect the JSON report.'
}

if ($IsWindows -and $nativeExitCode -ne 7) {
    throw "Expected native LASTEXITCODE 7, received '$nativeExitCode'."
}

$expectedScriptPath = $PSCommandPath
if (($invocationFrames.OuterArguments -join ',') -ne 'outer-argument' -or
    ($invocationFrames.RestoredOuterArguments -join ',') -ne 'outer-argument' -or
    ($invocationFrames.Inner.Arguments -join ',') -ne 'inner-argument' -or
    $invocationFrames.Inner.InvocationName -ne 'Get-ScopedProbeInner' -or
    ($invocationFrames.Dotted.Arguments -join ',') -ne 'dotted-argument' -or
    $invocationFrames.Dotted.InvocationName -ne '.' -or
    $invocationFrames.Inner.CommandPath -ne $expectedScriptPath -or
    $invocationFrames.Inner.ScriptRoot -ne (Split-Path -Parent $expectedScriptPath) -or
    $invocationFrames.Dotted.CommandPath -ne $expectedScriptPath -or
    $invocationFrames.Dotted.ScriptRoot -ne (Split-Path -Parent $expectedScriptPath)) {
    throw 'Legacy nested or dotted invocation tuple behavior did not match the expected frame values.'
}

if (-not $strictModePresence.UnsetAutomaticVariableThrows -or
    $strictModePresence.AssignedNullThrows -or
    $null -ne $strictModePresence.AssignedNull) {
    throw 'Strict-mode missing versus assigned-null behavior changed.'
}
