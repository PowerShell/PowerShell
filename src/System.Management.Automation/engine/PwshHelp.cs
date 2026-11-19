// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Globalization;
using System.Resources;
using System.Text;

namespace System.Management.Automation;

/// <summary>
/// Startup help shared by ConsoleHost and completion, without initializing a console host.
/// Option and value spellings are defined separately from these localized descriptions.
/// </summary>
internal static class PwshHelp
{
    private static readonly ResourceManager s_resources = new ResourceManager(
        "System.Management.Automation.resources.PwshHelpStrings", typeof(PwshHelp).Assembly);

    private static readonly string[] s_extendedHelpKeys =
    [
        "ExtendedHelpPrefix",
        "OptionFile",
        "OptionCommand",
        "OptionCommandWithArgs",
        "OptionConfigurationName",
        "OptionConfigurationFile",
        "OptionCustomPipeName",
        "OptionEncodedCommand",
        "OptionExecutionPolicy",
        "OptionInputFormat",
        "OptionInteractive",
        "OptionLogin",
        "OptionMTA",
        "OptionNoExit",
        "OptionNoLogo",
        "OptionNonInteractive",
        "OptionNoProfile",
        "OptionNoProfileLoadTime",
        "OptionOutputFormat",
        "OptionSettingsFile",
        "OptionSSHServerMode",
        "OptionSTA",
        "OptionVersion",
        "OptionWindowStyle",
        "OptionWorkingDirectory",
        "OptionHelp",
    ];

    internal static string UsageHelp => GetString("UsageHelp");

    internal static string ExtendedHelp => GetExtendedHelp(CultureInfo.CurrentUICulture);

    // ResourceManager applies ordinary parent-culture and invariant fallback for each key.
    internal static string GetString(string key, CultureInfo? culture = null)
        // Every requested key must exist in the invariant resources, even when a translation is missing.
        => s_resources.GetString(key, culture ?? CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException($"Missing pwsh help resource '{key}'.");

    /// <summary>
    /// Assembles full startup help from the option summaries shared with completion and their remaining details.
    /// </summary>
    /// <remarks>
    /// Resource fragments own their whitespace and ordering is explicit, preserving the existing localized
    /// help layout. Resolve every fragment through ResourceManager so missing translations use normal fallback.
    /// </remarks>
    internal static string GetExtendedHelp(CultureInfo culture)
    {
        var builder = new StringBuilder();
        foreach (string key in s_extendedHelpKeys)
        {
            builder.Append(GetString(key, culture));
            // Completion menus can suppress oversized tooltips. Each option key holds
            // a concise summary; full startup help also includes its remaining details.
            if (key != "ExtendedHelpPrefix")
            {
                builder.Append(GetString(key + "Details", culture));
            }
        }

        return builder.ToString();
    }
}
