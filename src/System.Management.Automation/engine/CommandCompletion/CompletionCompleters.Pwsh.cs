// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation.Language;
using System.Runtime.Versioning;

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
                return TryCompletePwshOptionName(context.WordToComplete, results);

            case PwshCompletionContextKind.ScriptPath:
                CompletePwshScriptPath(context, startup.Option, results);
                return true;

            case PwshCompletionContextKind.OptionValue:
                // The scanner emits OptionValue only when a recognized option is awaiting its value.
                CompletePwshOptionValue(context, startup.Option!, results);
                return true;

            case PwshCompletionContextKind.ScriptArguments:
                return false;

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

    private static bool TryCompletePwshOptionName(string wordToComplete, List<CompletionResult> results)
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
            if (option.Advertised && option.IsSupportedOnCurrentPlatform && option.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new CompletionResult(quote + "-" + option.Name + quote, option.Name,
                    CompletionResultType.ParameterName, PwshHelp.GetString(option.ResourceKey)));
            }
        }

        return true;
    }

    private static void CompletePwshScriptPath(CompletionContext context, PwshStartupOption? option, List<CompletionResult> results)
    {
        if (option?.Name == "File")
        {
            CompletePwshStandardInput(context.WordToComplete, results);
        }

        // Windows startup requires .ps1; Unix also accepts extensionless scripts such as shebang files.
        CompletePwshPath(context, results, directoryOnly: false,
            Platform.IsWindows ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".ps1" } : null);
    }

    private static void CompletePwshStandardInput(string wordToComplete, List<CompletionResult> results)
    {
        string prefix = wordToComplete;
        string quote = CompletionHelpers.HandleDoubleAndSingleQuote(ref prefix);
        if (prefix.Length == 0 || prefix == "-")
        {
            results.Add(new CompletionResult(quote + "-" + quote, "-",
                CompletionResultType.ParameterValue, PwshHelp.GetString("ValueStandardInput")));
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
                results.AddRange(CompletePwshConfigurationNames(context.WordToComplete,
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
                CompletePwshPath(context, results, option.ArgumentKind == PwshStartupArgumentKind.DirectoryPath,
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
                results.Add(new CompletionResult(quote + value + quote, value,
                    CompletionResultType.ParameterValue, PwshHelp.GetString("Value" + value)));
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
    internal static IReadOnlyList<CompletionResult> CompletePwshConfigurationNames(string wordToComplete,
        bool isDiscoveryAvailable, Func<bool> isServiceRunning, Func<IEnumerable<string?>?> discoverNames)
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

    private static void AddPwshConfigurationNameResults(string wordToComplete, IEnumerable<string?>? discoveredNames,
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
