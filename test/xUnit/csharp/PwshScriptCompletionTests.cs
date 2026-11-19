// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using Xunit;

namespace PSTests.Parallel;

public sealed class PwshScriptCompletionTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly Runspace _runspace;
    private readonly PowerShell _shell = PowerShell.Create();
    private readonly string _invocation;

    public PwshScriptCompletionTests()
    {
        Directory.CreateDirectory(_directory);
        string executable = Path.Combine(_directory, Platform.IsWindows ? "pwsh.exe" : "pwsh");
        File.WriteAllText(executable, "This file must not execute.");
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(executable, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        _invocation = "& '" + CodeGeneration.EscapeSingleQuotedStringContent(executable) + "' -File ";

        // These locally generated scripts test completion, not execution policy. Use the base authorization
        // manager in this runspace so Restricted on the CI machine cannot prevent reading their metadata.
        // Setting ExecutionPolicy instead would change process-wide state shared with parallel tests.
        InitialSessionState initialSessionState = InitialSessionState.CreateDefault();
        initialSessionState.AuthorizationManager = new AuthorizationManager("Microsoft.PowerShell");
        _runspace = RunspaceFactory.CreateRunspace(initialSessionState);
        _runspace.Open();
        _shell.Runspace = _runspace;
    }

    [Theory]
    [InlineData("ascii'quote.ps1")]
    [InlineData("left\u2018quote.ps1")]
    [InlineData("right\u2019quote.ps1")]
    [InlineData("low\u201aquote.ps1")]
    [InlineData("x\u2019; Get-Item \u2018x.ps1")]
    public void ScriptTargetRemainsLiteral(string name)
    {
        string target = WriteScript(name, "param([ValidateSet('ExpectedValue')] [string] $Filter) $global:ScriptBodyRan = $true");
        _shell.AddScript("Register-ArgumentCompleter -CommandName Get-Item -ParameterName Filter -ScriptBlock { $global:UnrelatedCompleterRan = $true; 'UnexpectedValue' }").Invoke();
        _shell.Commands.Clear();
        string input = _invocation + target + " -Filter E";
        CommandCompletion completion = CommandCompletion.CompleteInput(input, input.Length, null, _shell);
        Assert.Contains(completion.CompletionMatches, result => result.CompletionText == "ExpectedValue");
        Assert.DoesNotContain(completion.CompletionMatches, result => result.CompletionText == "UnexpectedValue");
        Assert.Null(_runspace.SessionStateProxy.GetVariable("UnrelatedCompleterRan"));
        Assert.Null(_runspace.SessionStateProxy.GetVariable("ScriptBodyRan"));
    }

    [Fact]
    public void DynamicParametersRunWithoutExecutingScriptBody()
    {
        const string source = """
            [CmdletBinding()]
            param()
            dynamicparam {
                $global:DynamicParametersRan = $true
                $attributes = [System.Collections.ObjectModel.Collection[System.Attribute]]::new()
                $attributes.Add([System.Management.Automation.ParameterAttribute]::new())
                $parameter = [System.Management.Automation.RuntimeDefinedParameter]::new('DynamicChoice', [string], $attributes)
                $parameters = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()
                $parameters.Add('DynamicChoice', $parameter)
                $parameters
            }
            end { $global:ScriptBodyRan = $true }
            """;
        string target = WriteScript("dynamic.ps1", source);
        string input = _invocation + target + " -Dynamic";
        CommandCompletion completion = CommandCompletion.CompleteInput(input, input.Length, null, _shell);
        Assert.Contains(completion.CompletionMatches, result => result.CompletionText == "-DynamicChoice");
        Assert.Equal(true, _runspace.SessionStateProxy.GetVariable("DynamicParametersRan"));
        Assert.Null(_runspace.SessionStateProxy.GetVariable("ScriptBodyRan"));
    }

    [Theory]
    [InlineData("$result = $(", "); Write-Output 'after'")]
    [InlineData("Write-Output 'before'; ", " | Out-Null")]
    public void ReplacementRangePreservesSurroundingInput(string prefix, string suffix)
    {
        string target = WriteScript("nested.ps1", "param([string] $DistinctParameter, [string] $Other) $global:ScriptBodyRan = $true");
        string beforeCursor = prefix + _invocation + target + " -Distinct";
        string afterCursor = " -Other untouched" + suffix;
        string input = beforeCursor + afterCursor;
        CommandCompletion completion = CommandCompletion.CompleteInput(input, beforeCursor.Length, null, _shell);
        CompletionResult match = Assert.Single(completion.CompletionMatches, result => result.CompletionText == "-DistinctParameter");
        string edited = input.Remove(completion.ReplacementIndex, completion.ReplacementLength).Insert(completion.ReplacementIndex, match.CompletionText);
        Assert.Equal(beforeCursor + "Parameter" + afterCursor, edited);
        Assert.Null(_runspace.SessionStateProxy.GetVariable("ScriptBodyRan"));
    }

    public void Dispose()
    {
        _shell.Dispose();
        _runspace.Dispose();
        Directory.Delete(_directory, recursive: true);
    }

    private string WriteScript(string name, string source)
    {
        string path = Path.Combine(_directory, name);
        File.WriteAllText(path, source);
        return "'" + CodeGeneration.EscapeSingleQuotedStringContent(path) + "'";
    }
}
