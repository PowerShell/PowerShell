// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation.Language;
using System.Runtime.Versioning;
using System.Text;

namespace System.Management.Automation;

public static partial class CompletionCompleters
{
    /// <summary>
    /// Routes native pwsh arguments to startup or script completion in the current runspace.
    /// </summary>
    /// <returns>
    /// True when the input is handled, even with no candidates; false permits ordinary completion fallback.
    /// </returns>
    /// <remarks>
    /// Describe the running engine even when the named executable is a different version.
    /// This lets embedded hosts complete arguments without starting that executable or initializing ConsoleHost.
    /// </remarks>
    private static bool TryCompletePwsh(CommandAst command, CompletionContext context, List<CompletionResult> results)
    {
        if (!IsPwshApplication(command, context))
        {
            return false;
        }

        PwshCompletionContext startup = PwshCompletionContext.Scan(command, context.CursorPosition.Offset);
        switch (startup.Kind)
        {
            case PwshCompletionContextKind.OptionName:
                return TryCompletePwshOptionName(context.WordToComplete, startup, results);

            case PwshCompletionContextKind.Command:
                CompletePwshCommand(command, context, startup, results);
                return true;

            case PwshCompletionContextKind.ScriptPath:
                CompletePwshScriptPath(context, startup.Option, results);
                return true;

            case PwshCompletionContextKind.OptionValue:
                // The scanner emits OptionValue only when a recognized option is awaiting its value.
                CompletePwshOptionValue(context, startup.Option!, results);
                return true;

            case PwshCompletionContextKind.ScriptArguments:
                return TryCompletePwshScript(command, context, startup, results);

            default:
                // Opaque command payloads and unknown startup syntax are handled even
                // when empty, preventing unrelated filename/command-name fallback.
                return true;
        }
    }

    private static bool IsPwshApplication(CommandAst command, CompletionContext context)
    {
        string name = command.GetCommandName();
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        string leaf = Path.GetFileName(name);
        // Unix can resolve a distinct executable named PWSH; do not claim its arguments.
        StringComparison nameComparison = Platform.IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!leaf.Equals("pwsh", nameComparison)
            && !(Platform.IsWindows && leaf.Equals("pwsh.exe", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        try
        {
            // Resolve with ordinary precedence, so a function or script named pwsh
            // keeps its own completion behavior.
            return CommandDiscovery.LookupCommandInfo(name, CommandOrigin.Internal, context.ExecutionContext) is ApplicationInfo;
        }
        catch (CommandNotFoundException)
        {
            return false;
        }
    }

    private static bool TryCompletePwshOptionName(string wordToComplete, PwshCompletionContext startup, List<CompletionResult> results)
    {
        string prefix = wordToComplete;
        string quote = CompletionHelpers.HandleDoubleAndSingleQuote(ref prefix);
        if (prefix.Length != 0 && !PwshStartupOptions.TryGetSwitchKey(prefix, out prefix))
        {
            return false;
        }

        foreach (PwshStartupOption option in PwshStartupOptions.Options)
        {
            // Partial input need not meet the host's minimum abbreviation length yet.
            // Offer canonical spellings so aliases do not produce duplicate menu entries.
            if (option.Advertised && option.IsSupportedOnCurrentPlatform && startup.CanOffer(option)
                && option.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new CompletionResult(
                    quote + "-" + option.Name + quote,
                    option.Name,
                    CompletionResultType.ParameterName,
                    PwshHelp.GetString(option.ResourceKey)));
            }
        }

        return true;
    }

    private static void CompletePwshCommand(
        CommandAst command,
        CompletionContext context,
        PwshCompletionContext startup,
        List<CompletionResult> results)
    {
        string? source = command.Extent.StartScriptPosition.GetFullScript();
        int offset = startup.CommandOffset;
        int end = Math.Max(command.Extent.EndOffset, startup.CursorOffset);
        if (source == null || offset < 0 || end > source.Length)
        {
            return;
        }

        if (TryCompletePwshCommandWithArgs(command, source, context, startup, results, ref end))
        {
            return;
        }

        string input = source.Substring(offset, end - offset);
        string trimmed = input.TrimStart();
        if (trimmed.Length > 0 && (trimmed[0].IsSingleQuote() || trimmed[0].IsDoubleQuote()))
        {
            CompletePwshQuotedCommand(input, offset, context, startup, results);
            return;
        }

        if (trimmed.StartsWith('{') || trimmed.TrimEnd() == "-")
        {
            return;
        }

        CommandCompletion delegated = CommandCompletion.CompleteInput(input, startup.CursorOffset - offset, context.Options);
        ApplyPwshScriptCompletion(delegated, input.Length, 0, offset, context, results);
    }

    /// <summary>
    /// Handles trailing -CommandWithArgs values, or limits code completion to its first argument.
    /// </summary>
    /// <param name="command">The native pwsh invocation being completed.</param>
    /// <param name="source">The original source containing the invocation.</param>
    /// <param name="context">The completion state whose replacement range may be updated.</param>
    /// <param name="startup">The startup option and cursor position identified by the scanner.</param>
    /// <param name="results">The collection to receive argument value completions.</param>
    /// <param name="end">
    /// The end of the source to complete, reduced to the first argument's end when completing command text.
    /// </param>
    /// <returns>True when argument value completion is handled; false to continue completing code.</returns>
    private static bool TryCompletePwshCommandWithArgs(
        CommandAst command,
        string source,
        CompletionContext context,
        PwshCompletionContext startup,
        List<CompletionResult> results,
        ref int end)
    {
        if (startup.Option?.Name != "CommandWithArgs")
        {
            return false;
        }

        foreach (CommandElementAst element in command.CommandElements)
        {
            if (element.Extent.StartOffset < startup.CommandOffset)
            {
                continue;
            }

            // Only the first native argument is code. Later arguments supply values to that code.
            if (startup.CursorOffset > element.Extent.EndOffset)
            {
                CompletePwshCommandArgumentValues(element, source, end, context, startup, results);
                return true;
            }

            end = element.Extent.EndOffset;
            break;
        }

        return false;
    }

    private static void CompletePwshCommandArgumentValues(
        CommandElementAst commandArgument,
        string source,
        int end,
        CompletionContext context,
        PwshCompletionContext startup,
        List<CompletionResult> results)
    {
        string? script = PwshCompletionContext.GetStaticText(commandArgument);
        if (script == null)
        {
            // Read expandable source as code for best-effort $args inference, without evaluating it.
            if (commandArgument is ExpandableStringExpressionAst expandable)
            {
                CompletePwshForwardedArgumentValues(expandable.Value, commandArgument, context, startup, results);
            }

            return;
        }

        ScriptBlockAst ast = Parser.ParseInput(script, out _, out ParseError[] errors);
        if (errors.Length != 0)
        {
            return;
        }

        if (ast.ParamBlock == null)
        {
            CompletePwshForwardedArgumentValues(script, commandArgument, context, startup, results);
            return;
        }

        if (ast.ParamBlock.Parameters.Count == 0)
        {
            return;
        }

        // Completion understands parameters of functions declared in its input. Supply only the root
        // parameter block: the command body and nested declarations must not influence argument binding.
        // This definition is parsed for completion, never installed in the runspace or executed.
        const string name = "__PwshCommandWithArgsCompletion";
        string prefix = "function " + name + " { " + ast.ParamBlock.Extent.Text + " }; " + name;
        int suffixOffset = commandArgument.Extent.EndOffset;
        string input = string.Concat(prefix.AsSpan(), source.AsSpan(suffixOffset, end - suffixOffset));
        int offsetDelta = suffixOffset - prefix.Length;
        CommandCompletion? delegated = CompletePwshChildInput(input, startup.CursorOffset - offsetDelta, context, startup);
        if (delegated == null)
        {
            return;
        }

        var matches = new List<CompletionResult>();
        ApplyPwshScriptCompletion(delegated, input.Length, prefix.Length, offsetDelta, context, matches);
        foreach (CompletionResult match in matches)
        {
            // Trailing arguments get value suggestions only, even when a parameter name is being typed.
            if (match.ResultType is not (CompletionResultType.ParameterName or CompletionResultType.Command))
            {
                results.Add(match);
            }
        }
    }

    private static void CompletePwshQuotedCommand(
        string input,
        int offset,
        CompletionContext context,
        PwshCompletionContext startup,
        List<CompletionResult> results)
    {
        // Use the tokenizer to locate the closing quote, including escaped quotes and unfinished input.
        Parser.ParseInput(input, out Token[] tokens, out _);
        Token token = tokens[0];
        if (token.Kind is not (TokenKind.StringLiteral or TokenKind.StringExpandable))
        {
            return;
        }

        int contentStart = token.Extent.StartOffset + 1;
        bool terminated = (token.TokenFlags & TokenFlags.TokenInError) == 0;
        int contentEnd = token.Extent.EndOffset - (terminated ? 1 : 0);
        int cursor = startup.CursorOffset - offset;
        if (cursor < contentStart || cursor > token.Extent.EndOffset)
        {
            return;
        }

        // Complete the contents as PowerShell source while keeping the original argument a string.
        // Subsequent native arguments are not part of the script being completed.
        string script = input.Substring(contentStart, contentEnd - contentStart);
        CommandCompletion delegated = CommandCompletion.CompleteInput(
            script,
            Math.Min(cursor, contentEnd) - contentStart,
            context.Options);
        var matches = new List<CompletionResult>();
        ApplyPwshScriptCompletion(delegated, script.Length, 0, offset + contentStart, context, matches);
        bool singleQuoted = input[contentStart - 1].IsSingleQuote();
        foreach (CompletionResult match in matches)
        {
            results.Add(new CompletionResult(
                EscapePwshCommandCompletion(match.CompletionText, singleQuoted),
                match.ListItemText,
                match.ResultType,
                match.ToolTip));
        }
    }

    private static string EscapePwshCommandCompletion(string text, bool singleQuoted)
    {
        if (singleQuoted)
        {
            return CodeGeneration.EscapeSingleQuotedStringContent(text);
        }

        var escaped = new StringBuilder();
        foreach (char character in text)
        {
            if (character.IsDoubleQuote() || character is '`' or '$')
            {
                escaped.Append('`');
            }

            escaped.Append(character);
        }

        return escaped.ToString();
    }

    private static void CompletePwshScriptPath(CompletionContext context, PwshStartupOption? option, List<CompletionResult> results)
    {
        if (option?.Name == "File")
        {
            CompletePwshStandardInput(context.WordToComplete, results);
        }

        // Windows startup requires .ps1; Unix also accepts extensionless scripts such as shebang files.
        CompletePwshPath(
            context,
            results,
            directoryOnly: false,
            Platform.IsWindows ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".ps1" } : null);
    }

    private static void CompletePwshStandardInput(string wordToComplete, List<CompletionResult> results)
    {
        string prefix = wordToComplete;
        string quote = CompletionHelpers.HandleDoubleAndSingleQuote(ref prefix);
        if (prefix.Length == 0 || prefix == "-")
        {
            results.Add(new CompletionResult(
                quote + "-" + quote,
                "-",
                CompletionResultType.ParameterValue,
                PwshHelp.GetString("ValueStandardInput")));
        }
    }

    private static void CompletePwshOptionValue(CompletionContext context, PwshStartupOption option, List<CompletionResult> results)
    {
        if (!option.IsSupportedOnCurrentPlatform)
        {
            // Recognition is broader than support so an unsupported option still owns its value.
            // Leave it empty instead of suggesting unrelated files through fallback.
            return;
        }

        if (option.Name == "ConfigurationName")
        {
            if (OperatingSystem.IsWindows())
            {
                results.AddRange(CompletePwshConfigurationNames(
                    context.WordToComplete,
                    Utils.IsAdministrator(),
                    () => IsPwshConfigurationServiceRunning(context),
                    () => GetPwshConfigurationNames(context)));
            }

            return;
        }

        switch (option.ArgumentKind)
        {
            case PwshStartupArgumentKind.FilePath:
            case PwshStartupArgumentKind.DirectoryPath:
                CompletePwshPath(
                    context,
                    results,
                    option.ArgumentKind == PwshStartupArgumentKind.DirectoryPath,
                    option.Name == "ConfigurationFile" ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pssc" } : null);
                break;

            default:
                CompletePwshFiniteValues(context.WordToComplete, option.ArgumentKind, results);
                break;
        }
    }

    private static void CompletePwshFiniteValues(string wordToComplete, PwshStartupArgumentKind kind, List<CompletionResult> results)
    {
        string prefix = wordToComplete;
        string quote = CompletionHelpers.HandleDoubleAndSingleQuote(ref prefix);
        foreach (string value in GetPwshValues(kind))
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new CompletionResult(
                    quote + value + quote,
                    value,
                    CompletionResultType.ParameterValue,
                    PwshHelp.GetString("Value" + value)));
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool IsPwshConfigurationServiceRunning(CompletionContext context)
    {
        context.Helper.AddCommandWithPreferenceSetting("Microsoft.PowerShell.Management\\Get-Service")
            .AddParameter("Name", "WinRM");
        Collection<PSObject> services = context.Helper.ExecuteCurrentPowerShell(out _);
        if (services != null)
        {
            foreach (PSObject service in services)
            {
                if (string.Equals(service.Properties["Status"]?.Value?.ToString(), "Running", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Enumerates registered session configuration names after the caller's privilege and service checks.
    /// </summary>
    /// <remarks>
    /// The WSMan provider can prompt to start WinRM even during enumeration. The caller must check that
    /// it is already running; do not use Force, since tab completion must not start the service.
    /// </remarks>
    [SupportedOSPlatform("windows")]
    private static List<string> GetPwshConfigurationNames(CompletionContext context)
    {
        context.Helper.AddCommandWithPreferenceSetting("Microsoft.PowerShell.Core\\Get-PSSessionConfiguration")
            .AddParameter("Name", "*");
        Collection<PSObject> configurations = context.Helper.ExecuteCurrentPowerShell(out _);
        var names = new List<string>();
        if (configurations != null)
        {
            foreach (PSObject configuration in configurations)
            {
                if (configuration.Properties["Name"]?.Value is string name)
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }

    /// <summary>
    /// Performs best-effort configuration discovery only when platform, privilege, and service checks allow it.
    /// </summary>
    /// <remarks>
    /// The caller supplies platform and privilege availability. Service inspection and discovery remain
    /// deferred so unavailable discovery touches neither, and tests can verify the gates without using WinRM.
    /// </remarks>
    internal static IReadOnlyList<CompletionResult> CompletePwshConfigurationNames(
        string wordToComplete,
        bool isDiscoveryAvailable,
        Func<bool> isServiceRunning,
        Func<IEnumerable<string?>?> discoverNames)
    {
        var results = new List<CompletionResult>();
        if (!isDiscoveryAvailable)
        {
            return results;
        }

        try
        {
            if (!isServiceRunning())
            {
                return results;
            }

            AddPwshConfigurationNameResults(wordToComplete, discoverNames(), results);
        }
        catch (RuntimeException)
        {
            // Missing remoting support and inaccessible configurations produce no candidates.
        }

        return results;
    }

    private static void AddPwshConfigurationNameResults(
        string wordToComplete,
        IEnumerable<string?>? discoveredNames,
        List<CompletionResult> results)
    {
        string prefix = wordToComplete;
        string quote = CompletionHelpers.HandleDoubleAndSingleQuote(ref prefix);
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string? name in discoveredNames ?? [])
        {
            if (!string.IsNullOrWhiteSpace(name) && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                names.Add(name);
            }
        }

        foreach (string name in names)
        {
            // Prefer literal quoting for names that would interpolate inside double quotes.
            string completionText = name.Contains('`') || (quote == "\"" && name.IndexOfAny(['$', '"', '\u201c', '\u201d', '\u201e']) >= 0)
                ? "'" + CodeGeneration.EscapeSingleQuotedStringContent(name) + "'"
                : CompletionHelpers.QuoteCompletionText(name, quote);
            results.Add(new CompletionResult(completionText, name, CompletionResultType.ParameterValue, name));
        }
    }

    private static string[] GetPwshValues(PwshStartupArgumentKind kind) => kind switch
    {
        PwshStartupArgumentKind.Format => ["Text", "XML"],
        PwshStartupArgumentKind.WindowStyle => ["Normal", "Minimized", "Maximized", "Hidden"],
        PwshStartupArgumentKind.ExecutionPolicy => ["AllSigned", "Bypass", "Default", "RemoteSigned", "Restricted", "Undefined", "Unrestricted"],
        _ => [],
    };

    private static void CompletePwshPath(CompletionContext context, List<CompletionResult> results, bool directoryOnly, HashSet<string>? extensions)
    {
        // Startup paths are literal: provider wildcard interpretation would mishandle names containing brackets.
        // The options belong to the caller, so restore only the setting introduced by this completion.
        bool clearLiteralPath = TurnOnLiteralPathOption(context);
        try
        {
            results.AddRange(CompleteFilename(context, directoryOnly, extensions));
        }
        finally
        {
            if (clearLiteralPath)
            {
                context.Options.Remove("LiteralPaths");
            }
        }
    }
}
