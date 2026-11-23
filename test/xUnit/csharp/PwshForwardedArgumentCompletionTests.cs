// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;
using Xunit;

namespace PSTests.Parallel;

public sealed class PwshForwardedArgumentCompletionTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly string _childDirectory;
    private readonly string _invocation;

    public PwshForwardedArgumentCompletionTests()
    {
        Directory.CreateDirectory(_directory);
        _childDirectory = Path.Combine(_directory, "child [space]");
        Directory.CreateDirectory(_childDirectory);
        File.WriteAllText(Path.Combine(_childDirectory, "ChildValue.txt"), string.Empty);
        File.WriteAllText(Path.Combine(_directory, "CallerValue.txt"), string.Empty);
        string executable = Path.Combine(_directory, Platform.IsWindows ? "pwsh.exe" : "pwsh");
        File.WriteAllText(executable, "This file must not execute.");
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(executable, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        _invocation = "& '" + CodeGeneration.EscapeSingleQuotedStringContent(executable) + "' ";
    }

    [Theory]
    [InlineData(false, "-wd", "'gci $args[0]'", "Chi<cursor>")]
    [InlineData(true, "-WorkingDirectory", "\"gci $args[0]\"", "Chi<cursor>")]
    [InlineData(false, "--workingdirectory", "'Get-Item -LiteralPath $args[1]'", "ignored Chi<cursor>")]
    [InlineData(true, "-wd", "'Get-ChildItem -Path $args[0]'", "ChildV<cursor>alue.txt untouched")]
    [InlineData(false, "-wd", "'gci $args[0]'", "<cursor>")]
    public void CompletesForwardedPathsInChildWorkingDirectory(
        bool useAst,
        string option,
        string command,
        string markedArguments)
    {
        using PowerShell shell = CreateCaller();
        string prefix = "$unused = 1; " + _invocation + option + " 'child [space]' -cwa " + command + " ";
        string input = prefix + markedArguments.Replace("<cursor>", string.Empty);
        int cursor = prefix.Length + markedArguments.IndexOf("<cursor>", StringComparison.Ordinal);
        CommandCompletion completion = Complete(input, cursor, useAst, shell);
        CompletionResult match = Assert.Single(completion.CompletionMatches, result => result.ListItemText == "ChildValue.txt");
        Assert.Equal(Path.Combine(_childDirectory, "ChildValue.txt"), match.ToolTip);
        Assert.DoesNotContain(completion.CompletionMatches, result => result.ResultType == CompletionResultType.ParameterName);
        string edited = input.Remove(completion.ReplacementIndex, completion.ReplacementLength)
            .Insert(completion.ReplacementIndex, match.CompletionText);
        Assert.StartsWith(prefix, edited);
        if (markedArguments.EndsWith("untouched", StringComparison.Ordinal))
        {
            Assert.EndsWith(" untouched", edited);
        }

        Assert.Equal(_directory, shell.Runspace.SessionStateProxy.Path.CurrentLocation.ProviderPath);
        Assert.Null(shell.Runspace.SessionStateProxy.GetVariable("LocationChanged"));
    }

    [Fact]
    public void RootRelativePathUsesChildDriveRatherThanCallerDrive()
    {
        using PowerShell shell = CreateCaller();
        shell.AddCommand("New-PSDrive")
            .AddParameter("Name", "PwshCaller")
            .AddParameter("PSProvider", "FileSystem")
            .AddParameter("Root", _directory)
            .AddParameter("Scope", "Global")
            .Invoke();
        shell.Commands.Clear();
        shell.AddCommand("Set-Location").AddParameter("LiteralPath", "PwshCaller:").Invoke();
        shell.Commands.Clear();
        shell.Runspace.SessionStateProxy.SetVariable("LocationChanged", null);

        string root = Path.GetPathRoot(_directory)!;
        string input = _invocation + "-wd '" + root + "' -cwa \"gci $args[0]\" " + Path.DirectorySeparatorChar;
        CommandCompletion completion = Complete(input, input.Length, false, shell);
        var expected = new DirectoryInfo(_directory);
        while (expected.Parent?.Parent != null)
        {
            expected = expected.Parent;
        }

        Assert.Contains(completion.CompletionMatches, result => Path.TrimEndingDirectorySeparator(result.ToolTip) == expected.FullName);
        Assert.Equal("PwshCaller", shell.Runspace.SessionStateProxy.Path.CurrentLocation.Drive.Name);
        Assert.Null(shell.Runspace.SessionStateProxy.GetVariable("LocationChanged"));
    }

    [Fact]
    public void MissingWorkingDirectoryUsesCallersFileSystemLocation()
    {
        using PowerShell shell = CreateCaller();
        string input = _invocation + "-cwa 'gci $args[0]' CallerV";
        CommandCompletion completion = Complete(input, input.Length, false, shell);
        Assert.Contains(completion.CompletionMatches, result => result.ToolTip == Path.Combine(_directory, "CallerValue.txt"));
    }

    [Fact]
    public void MapsOtherArgumentsAndCompletesEnumValuesWithoutExecutingBody()
    {
        using PowerShell shell = CreateCaller();
        string sentinel = Path.Combine(_directory, "body-ran.txt");
        string script = "Write-Host -ForegroundColor $args[1] $args[0]; [IO.File]::WriteAllText('"
            + CodeGeneration.EscapeSingleQuotedStringContent(sentinel) + "', 'ran')";
        string prefix = _invocation + "-cwa '" + CodeGeneration.EscapeSingleQuotedStringContent(script) + "' hello ";
        string input = prefix + "Re";
        CommandCompletion completion = Complete(input, input.Length, true, shell);
        CompletionResult match = Assert.Single(completion.CompletionMatches, result => result.CompletionText == "Red");
        Assert.Equal(prefix.Length, completion.ReplacementIndex);
        Assert.Equal(2, completion.ReplacementLength);
        Assert.Equal(CompletionResultType.ParameterValue, match.ResultType);
        Assert.False(File.Exists(sentinel));
    }

    [Theory]
    [InlineData("gci $args[0]; Get-Item $args[0]", "Chi")]
    [InlineData("function Nested { gci $args[0] }", "Chi")]
    [InlineData("$args[0] = 'changed'; gci $args[0]", "Chi")]
    [InlineData("gci $args[1 + 0]", "ignored Chi")]
    [InlineData("gci $args[1]", "$(throw 'must not execute') Chi")]
    [InlineData("param($First) gci $args[0]", "ignored -NoP")]
    [InlineData("gci ($args[0] + '/suffix')", "Chi")]
    [InlineData("gci $args[0]", "-NoP")]
    public void DoesNotGuessAmbiguousMappingsOrOfferParameterNames(string script, string arguments)
    {
        using PowerShell shell = CreateCaller();
        string input = _invocation + "-cwa '" + CodeGeneration.EscapeSingleQuotedStringContent(script) + "' " + arguments;
        Assert.Empty(Complete(input, input.Length, false, shell).CompletionMatches);
    }

    [Fact]
    public void InvalidWorkingDirectoryProducesNoCandidates()
    {
        using PowerShell shell = CreateCaller();
        string input = _invocation + "-wd 'missing-directory' -cwa 'gci $args[0]' CallerV";
        Assert.Empty(Complete(input, input.Length, false, shell).CompletionMatches);
        Assert.Equal(_directory, shell.Runspace.SessionStateProxy.Path.CurrentLocation.ProviderPath);
        Assert.Null(shell.Runspace.SessionStateProxy.GetVariable("LocationChanged"));
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private static CommandCompletion Complete(string input, int cursor, bool useAst, PowerShell shell)
    {
        if (!useAst)
        {
            return CommandCompletion.CompleteInput(input, cursor, null, shell);
        }

        Ast ast = Parser.ParseInput(input, out Token[] tokens, out _);
        return CommandCompletion.CompleteInput(
            ast,
            tokens,
            ((InternalScriptPosition)ast.Extent.EndScriptPosition).CloneWithNewOffset(cursor),
            null,
            shell);
    }

    private PowerShell CreateCaller()
    {
        PowerShell shell = PowerShell.Create(RunspaceMode.NewRunspace);
        shell.AddCommand("Set-Location").AddParameter("LiteralPath", _directory).Invoke();
        Assert.False(shell.HadErrors);
        shell.Commands.Clear();
        shell.AddScript("$ExecutionContext.InvokeCommand.LocationChangedAction = { $global:LocationChanged = $true }").Invoke();
        shell.Commands.Clear();
        return shell;
    }
}
