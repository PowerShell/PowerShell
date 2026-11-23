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
        using PowerShell shell = PowerShell.Create(RunspaceMode.NewRunspace);
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
        Assert.Null(shell.Runspace.SessionStateProxy.GetVariable("ScriptBodyRan"));
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

        using PowerShell shell = PowerShell.Create(RunspaceMode.NewRunspace);
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
        using PowerShell shell = PowerShell.Create(RunspaceMode.NewRunspace);
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
        Assert.Null(shell.Runspace.SessionStateProxy.GetVariable("ScriptBodyRan"));
    }

    [Theory]
    [InlineData("-NoP -No", "-NoProfile")]
    [InlineData("--NoProfile -No", "-NoProfile")]
    [InlineData("/noprofile -No", "-NoProfile")]
    [InlineData("-of XML -", "-OutputFormat")]
    [InlineData("-STA -", "-MTA")]
    [InlineData("-MTA -", "-STA")]
    public void OmitsProvidedAndConflictingOptions(string arguments, string excluded)
    {
        using PowerShell shell = PowerShell.Create(RunspaceMode.NewRunspace);
        CommandCompletion completion = Complete(Invocation + arguments, false, shell);
        Assert.DoesNotContain(completion.CompletionMatches, result => result.CompletionText == excluded);
        Assert.Contains(completion.CompletionMatches, result => result.CompletionText == "-NoLogo");
    }

    [Theory]
    [InlineData("-Help ")]
    [InlineData("-h -No")]
    [InlineData("/? ")]
    [InlineData("-Version ")]
    [InlineData("-c - ")]
    [InlineData("-c 'Get-' ")]
    public void TerminalOptionsAndOpaqueCommandsHaveNoCompletions(string arguments)
    {
        using PowerShell shell = PowerShell.Create(RunspaceMode.NewRunspace);
        Assert.Empty(Complete(Invocation + arguments, false, shell).CompletionMatches);
    }

    [Theory]
    [InlineData(false, "-Command", "Get-CompletionPro", "Get-CompletionProbe", false)]
    [InlineData(true, "-c", "Get-CompletionProbe -Mo", "-Mode", false)]
    [InlineData(false, "--command", "Get-CompletionProbe -Mode Emb", "EmbeddedValue", false)]
    [InlineData(true, "/c", "Get-CompletionProbe -Mode Emb", "EmbeddedValue", true)]
    [InlineData(false, "-c", "pwsh -c Get-CompletionPro", "Get-CompletionProbe", false)]
    [InlineData(false, "-CommandWithArgs", "Get-CompletionPro", "Get-CompletionProbe", false)]
    [InlineData(true, "-cwa", "Get-CompletionPro", "Get-CompletionProbe", false)]
    public void CommandCompletionMapsOriginalOffsets(bool useAst, string option, string payload, string expected, bool middle)
    {
        using PowerShell shell = PowerShell.Create(RunspaceMode.NewRunspace);
        shell.AddScript("function global:Get-CompletionProbe { param([ValidateSet('EmbeddedValue')] $Mode) $global:CommandBodyRan = $true }").Invoke();
        shell.Commands.Clear();

        string prefix = "$unused = 1; " + Invocation + "-NoProfile " + option + " ";
        string input = prefix + payload;
        int cursor = input.Length;
        if (middle)
        {
            input += "eddedValue -Other";
        }

        CommandCompletion completion = Complete(input, useAst, shell, cursor);
        CompletionResult match = Assert.Single(completion.CompletionMatches, result => result.CompletionText == expected);
        int wordStart = prefix.Length + payload.LastIndexOf(' ') + 1;
        Assert.Equal(wordStart, completion.ReplacementIndex);
        Assert.Equal(middle ? "EmbeddedValue".Length : input.Length - wordStart, completion.ReplacementLength);
        string edited = input.Remove(completion.ReplacementIndex, completion.ReplacementLength)
            .Insert(completion.ReplacementIndex, match.CompletionText);
        Assert.StartsWith(prefix, edited);
        Assert.Null(shell.Runspace.SessionStateProxy.GetVariable("CommandBodyRan"));
    }

    [Theory]
    [InlineData(false, "-Command", "'Get-CompletionPro<cursor>'", "'Get-CompletionProbe'", "Get-CompletionProbe")]
    [InlineData(true, "-c", "\"Get-CompletionPro<cursor>\"", "\"Get-CompletionProbe\"", "Get-CompletionProbe")]
    [InlineData(false, "-CommandWithArgs", "'Get-CompletionPro<cursor>' argument", "'Get-CompletionProbe' argument", "Get-CompletionProbe")]
    [InlineData(true, "-cwa", "\"Get-CompletionProbe -Mo<cursor>\" argument", "\"Get-CompletionProbe -Mode\" argument", "Mode")]
    [InlineData(false, "-c", "'Get-CompletionPro<cursor>", "'Get-CompletionProbe", "Get-CompletionProbe")]
    [InlineData(true, "-cwa", "\"Get-CompletionProbe -Mo<cursor>", "\"Get-CompletionProbe -Mode", "Mode")]
    [InlineData(false, "-c", "'Get-CompletionPro'<cursor>", "'Get-CompletionProbe'", "Get-CompletionProbe")]
    [InlineData(true, "-cwa", "'Get-CompletionProbe -Mode Emb<cursor>eddedValue' tail", "'Get-CompletionProbe -Mode EmbeddedValue' tail", "EmbeddedValue")]
    [InlineData(false, "-c", "'1 | Get-CompletionPro<cursor>'", "'1 | Get-CompletionProbe'", "Get-CompletionProbe")]
    [InlineData(true, "-c", "'Get-CompletionProbe -Mode Spa<cursor>'", "'Get-CompletionProbe -Mode ''Spaced Value'''", "Spaced Value")]
    [InlineData(false, "-cwa", "\"Get-CompletionProbe -Mode Spa<cursor>\"", "\"Get-CompletionProbe -Mode 'Spaced Value'\"", "Spaced Value")]
    [InlineData(true, "-c", "\"Get-CompletionProbe -Mode Dol<cursor>\"", "\"Get-CompletionProbe -Mode 'Dollar`$Value'\"", "Dollar$Value")]
    public void QuotedCommandCompletionPreservesStringArgument(
        bool useAst,
        string option,
        string markedPayload,
        string expectedPayload,
        string expectedItem)
    {
        using PowerShell shell = PowerShell.Create(RunspaceMode.NewRunspace);
        shell.AddScript("function global:Get-CompletionProbe { param([ValidateSet('EmbeddedValue', 'Spaced Value', 'Dollar$Value')] $Mode) $global:CommandBodyRan = $true }").Invoke();
        shell.Commands.Clear();

        string prefix = "$unused = 1; " + Invocation + "-NoProfile " + option + " ";
        int cursor = prefix.Length + markedPayload.IndexOf("<cursor>", StringComparison.Ordinal);
        string input = prefix + markedPayload.Replace("<cursor>", string.Empty);
        CommandCompletion completion = Complete(input, useAst, shell, cursor);
        CompletionResult match = Assert.Single(completion.CompletionMatches, result => result.ListItemText == expectedItem);
        string edited = input.Remove(completion.ReplacementIndex, completion.ReplacementLength)
            .Insert(completion.ReplacementIndex, match.CompletionText);
        Assert.Equal(prefix + expectedPayload, edited);
        Assert.Null(shell.Runspace.SessionStateProxy.GetVariable("CommandBodyRan"));
    }

    [Fact]
    public void FunctionNamedPwshKeepsItsParameters()
    {
        using PowerShell shell = PowerShell.Create(RunspaceMode.NewRunspace);
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
        Assert.Null(shell.Runspace.SessionStateProxy.GetVariable("ScriptBodyRan"));
        Assert.DoesNotContain(completion.CompletionMatches, result => result.CompletionText == "-NoProfile");
    }

    [Fact]
    public void CompletionRespectsPlatformAndLocalizedHelp()
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            using PowerShell shell = PowerShell.Create(RunspaceMode.NewRunspace);
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
        using PowerShell shell = PowerShell.Create(RunspaceMode.NewRunspace);
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

    private static CommandCompletion Complete(string input, bool useAst, PowerShell shell, int? cursor = null)
    {
        if (!useAst)
        {
            return CommandCompletion.CompleteInput(input, cursor ?? input.Length, null, shell);
        }

        Ast ast = Parser.ParseInput(input, out Token[] tokens, out _);
        return CommandCompletion.CompleteInput(
            ast,
            tokens,
            ((InternalScriptPosition)ast.Extent.EndScriptPosition).CloneWithNewOffset(cursor ?? input.Length),
            null,
            shell);
    }
}
