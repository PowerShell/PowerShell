// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using Xunit;

namespace PSTests.Parallel;

public sealed class PwshCompletionEmbeddedTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly string _executable;

    public PwshCompletionEmbeddedTests()
    {
        Directory.CreateDirectory(_directory);
        _executable = Path.Combine(_directory, Platform.IsWindows ? "pwsh.exe" : "pwsh");

        // Discovery only needs an application. Completion must never execute it.
        File.WriteAllText(_executable, "This file must not execute.");
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(_executable, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private string Invocation => "& '" + _executable.Replace("'", "''") + "' ";

    [Theory]
    [InlineData(false, "-review", "-review-script.ps1")]
    [InlineData(true, "-review", "-review-script.ps1")]
    [InlineData(false, "'-review'", "-review-script.ps1")]
    [InlineData(false, "--review", "--review-script.ps1")]
    public void CompletesImplicitScriptPathsBeginningWithDash(bool useAst, string prefix, string fileName)
    {
        string script = Path.Combine(_directory, fileName);
        File.WriteAllText(script, "param() $global:ScriptBodyRan = $true");
        using Runspace runspace = RunspaceFactory.CreateRunspace();
        runspace.Open();
        using PowerShell shell = PowerShell.Create();
        shell.Runspace = runspace;
        shell.AddCommand("Set-Location").AddParameter("LiteralPath", _directory).Invoke();
        Assert.False(shell.HadErrors);
        shell.Commands.Clear();

        string input = Invocation + prefix;
        CommandCompletion completion = Complete(input, useAst, shell);
        CompletionResult match = Assert.Single(completion.CompletionMatches);
        Assert.Equal(CompletionResultType.ProviderItem, match.ResultType);
        Assert.Equal(Invocation.Length, completion.ReplacementIndex);
        Assert.Equal(prefix.Length, completion.ReplacementLength);

        // Check the resulting argument rather than imposing platform-specific path separators or quoting.
        string edited = input.Remove(completion.ReplacementIndex, completion.ReplacementLength)
            .Insert(completion.ReplacementIndex, match.CompletionText);
        Ast ast = Parser.ParseInput(edited, out _, out ParseError[] errors);
        Assert.Empty(errors);
        CommandAst command = Assert.IsType<CommandAst>(ast.Find(node => node is CommandAst, searchNestedScriptBlocks: false));
        StringConstantExpressionAst argument = Assert.IsType<StringConstantExpressionAst>(command.CommandElements[1]);
        Assert.Equal(script, Path.GetFullPath(argument.Value, _directory));
        Assert.Null(runspace.SessionStateProxy.GetVariable("ScriptBodyRan"));
    }

    [Fact]
    public void ExecutableNameCasingFollowsPlatformRules()
    {
        string alternateDirectory = Path.Combine(_directory, "alternate");
        Directory.CreateDirectory(alternateDirectory);
        string executable = Path.Combine(alternateDirectory, Platform.IsWindows ? "PWSH.EXE" : "PWSH");
        File.WriteAllText(executable, "This file must not execute.");
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(executable, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        using Runspace runspace = RunspaceFactory.CreateRunspace();
        runspace.Open();
        using PowerShell shell = PowerShell.Create();
        shell.Runspace = runspace;
        CommandCompletion completion = Complete("& '" + executable.Replace("'", "''") + "' -NoP", false, shell);
        Assert.Equal(
            Platform.IsWindows,
            System.Linq.Enumerable.Any(completion.CompletionMatches, result => result.CompletionText == "-NoProfile"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StartupCompletionNeedsNoConsoleHost(bool useAst)
    {
        using Runspace runspace = RunspaceFactory.CreateRunspace();
        runspace.Open();
        using PowerShell shell = PowerShell.Create();
        shell.Runspace = runspace;
        string input = Invocation + "-NoP";
        CommandCompletion completion = Complete(input, useAst, shell);
        Assert.Contains(
            completion.CompletionMatches,
            result => result.CompletionText == "-NoProfile"
            && result.ResultType == CompletionResultType.ParameterName && !string.IsNullOrEmpty(result.ToolTip));
        Assert.Equal(input.Length - 4, completion.ReplacementIndex);
        Assert.Equal(4, completion.ReplacementLength);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScriptCompletionUsesEmbeddedRunspace(bool useAst)
    {
        string script = Path.Combine(_directory, "test script.ps1");
        File.WriteAllText(script, "param([ValidateSet('EmbeddedValue')] [string] $Mode) $global:ScriptBodyRan = $true");
        using Runspace runspace = CreateScriptCompletionRunspace();
        runspace.Open();
        using PowerShell shell = PowerShell.Create();
        shell.Runspace = runspace;
        string input = Invocation + "-NoProfile -File '" + script.Replace("'", "''") + "' -Mode Embed";
        CommandCompletion completion = Complete(input, useAst, shell);
        CompletionResult match = Assert.Single(completion.CompletionMatches, result => result.CompletionText == "EmbeddedValue");
        string edited = input.Remove(completion.ReplacementIndex, completion.ReplacementLength).Insert(completion.ReplacementIndex, match.CompletionText);
        Assert.EndsWith("-Mode EmbeddedValue", edited);
        Assert.Null(runspace.SessionStateProxy.GetVariable("ScriptBodyRan"));
    }

    [Fact]
    public void FunctionNamedPwshKeepsItsParameters()
    {
        using Runspace runspace = RunspaceFactory.CreateRunspace();
        runspace.Open();
        using PowerShell shell = PowerShell.Create();
        shell.Runspace = runspace;
        shell.AddScript("function global:pwsh { param($DistinctParameter) }").Invoke();
        shell.Commands.Clear();
        CommandCompletion completion = Complete("pwsh -Distinct", false, shell);
        Assert.Contains(completion.CompletionMatches, result => result.CompletionText == "-DistinctParameter");
        Assert.DoesNotContain(completion.CompletionMatches, result => result.CompletionText == "-NoProfile");
    }

    [Fact]
    public void ScriptNamedPwshKeepsItsParameters()
    {
        string script = Path.Combine(_directory, "pwsh.ps1");
        File.WriteAllText(script, "param($ScriptParameter) $global:ScriptBodyRan = $true");
        using Runspace runspace = CreateScriptCompletionRunspace();
        runspace.Open();
        using PowerShell shell = PowerShell.Create();
        shell.Runspace = runspace;
        shell.AddCommand("Set-Alias").AddParameter("Name", "pwsh").AddParameter("Value", script).Invoke();
        shell.Commands.Clear();
        CommandCompletion completion = Complete("pwsh -Script", false, shell);
        Assert.Contains(completion.CompletionMatches, result => result.CompletionText == "-ScriptParameter");
        Assert.Null(runspace.SessionStateProxy.GetVariable("ScriptBodyRan"));
        Assert.DoesNotContain(completion.CompletionMatches, result => result.CompletionText == "-NoProfile");
    }

    [Fact]
    public void CompletionRespectsPlatformAndLocalizedHelp()
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            using Runspace runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            using PowerShell shell = PowerShell.Create();
            shell.Runspace = runspace;
            CommandCompletion completion = Complete(Invocation + "-", false, shell);
            Assert.Equal(Platform.IsWindows, System.Linq.Enumerable.Any(completion.CompletionMatches, result => result.CompletionText == "-WindowStyle"));
            CompletionResult profile = Assert.Single(completion.CompletionMatches, result => result.CompletionText == "-NoProfile");
            Assert.Equal(PwshHelp.GetString("OptionNoProfile"), profile.ToolTip);
            Assert.False(string.IsNullOrWhiteSpace(PwshHelp.GetString("OptionNoProfile", CultureInfo.GetCultureInfo("en-NZ"))));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void RegisteredNativeCompleterTakesPrecedence()
    {
        using Runspace runspace = RunspaceFactory.CreateRunspace();
        runspace.Open();
        using PowerShell shell = PowerShell.Create();
        shell.Runspace = runspace;
        shell.AddScript("Register-ArgumentCompleter -Native -CommandName pwsh,pwsh.exe -ScriptBlock { 'CustomNativeValue' }").Invoke();
        shell.Commands.Clear();
        CommandCompletion completion = Complete(Invocation + "-NoP", false, shell);
        Assert.Contains(completion.CompletionMatches, result => result.CompletionText == "CustomNativeValue");
        Assert.DoesNotContain(completion.CompletionMatches, result => result.CompletionText == "-NoProfile");
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private static Runspace CreateScriptCompletionRunspace()
    {
        // Allow metadata access for these locally generated test scripts regardless of the machine's policy.
        // A runspace-local authorization manager avoids changing process-wide policy in parallel tests.
        InitialSessionState initialSessionState = InitialSessionState.CreateDefault();
        initialSessionState.AuthorizationManager = new AuthorizationManager("Microsoft.PowerShell");
        return RunspaceFactory.CreateRunspace(initialSessionState);
    }

    private static CommandCompletion Complete(string input, bool useAst, PowerShell shell)
    {
        if (!useAst)
        {
            return CommandCompletion.CompleteInput(input, input.Length, null, shell);
        }

        Ast ast = Parser.ParseInput(input, out Token[] tokens, out _);
        return CommandCompletion.CompleteInput(ast, tokens, ast.Extent.EndScriptPosition, null, shell);
    }
}
