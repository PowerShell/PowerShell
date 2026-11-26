// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Management.Automation.Language;

namespace System.Management.Automation;

[Flags]
internal enum PwshStartupPlatform
{
    Windows = 1,
    Unix = 2,
    All = Windows | Unix,
}

internal enum PwshStartupArgumentKind
{
    None,
    String,
    FilePath,
    DirectoryPath,
    ScriptPath,
    Command,
    EncodedCommand,
    EncodedArguments,
    Format,
    WindowStyle,
    ExecutionPolicy,
}

internal enum PwshStartupArgumentConsumption
{
    None,
    One,
    Remaining,
}

/// <summary>Startup syntax shared by the host and completion; validation belongs to the host.</summary>
internal sealed class PwshStartupOption
{
    internal PwshStartupOption(string name, int minimumAbbreviationLength, PwshStartupPlatform platforms,
        PwshStartupArgumentKind argumentKind, PwshStartupArgumentConsumption consumption, bool advertised,
        string? alias = null, int aliasMinimumAbbreviationLength = 0)
    {
        Name = name;
        MinimumAbbreviationLength = minimumAbbreviationLength;
        Platforms = platforms;
        ArgumentKind = argumentKind;
        Consumption = consumption;
        Advertised = advertised;
        Aliases = Array.AsReadOnly<string>(alias is null ? [] : [alias]);
        AliasMinimumAbbreviationLength = aliasMinimumAbbreviationLength;
    }

    internal string Name { get; }

    internal IReadOnlyList<string> Aliases { get; }

    internal int MinimumAbbreviationLength { get; }

    internal int AliasMinimumAbbreviationLength { get; }

    internal PwshStartupPlatform Platforms { get; }

    internal PwshStartupArgumentKind ArgumentKind { get; }

    internal PwshStartupArgumentConsumption Consumption { get; }

    internal bool Advertised { get; }

    internal bool StopsParsing => Consumption == PwshStartupArgumentConsumption.Remaining || Name == "Version";

    internal string ResourceKey => "Option" + Name;

    internal bool IsSupportedOnCurrentPlatform
    {
        get
        {
            bool windowsDesktop = Platform.IsWindows && (Name is "STA" or "MTA") && Platform.IsWindowsDesktop;
            bool staSupported = Name == "STA" && windowsDesktop && Platform.IsStaSupported;
            return IsSupportedOnPlatform(Platform.IsWindows, windowsDesktop, staSupported);
        }
    }

    /// <summary>
    /// Checks whether an already recognized option can be offered on the given platform.
    /// </summary>
    /// <remarks>
    /// Recognition must remain independent: the host recognizes apartment options even on Windows variants
    /// that cannot support them, then reports its existing validation error. Explicit capability inputs also
    /// let tests cover those variants without requiring the corresponding operating system.
    /// </remarks>
    internal bool IsSupportedOnPlatform(bool isWindows, bool isWindowsDesktop, bool isStaSupported)
    {
        if ((Platforms & (isWindows ? PwshStartupPlatform.Windows : PwshStartupPlatform.Unix)) == 0)
        {
            return false;
        }

        return Name switch
        {
            "STA" => isWindowsDesktop && isStaSupported,
            "MTA" => isWindowsDesktop,
            _ => true,
        };
    }

    internal bool Matches(string key)
    {
        if (key.Length >= MinimumAbbreviationLength && Name.StartsWith(key, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Aliases.Count != 0 && key.Length >= AliasMinimumAbbreviationLength
            && Aliases[0].StartsWith(key, StringComparison.OrdinalIgnoreCase);
    }
}

internal static class PwshStartupOptions
{
    // Order is significant: startup historically resolves abbreviations in this order.
    // Unsupported options remain recognizable so the host can report its existing errors.
    internal static IReadOnlyList<PwshStartupOption> Options { get; } = Array.AsReadOnly<PwshStartupOption>(
    [
        new("Version", 1, PwshStartupPlatform.All, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, true),
        new("Help", 1, PwshStartupPlatform.All, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, true, "?", 1),
        new("Login", 1, PwshStartupPlatform.Unix, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, true),
        new("NoExit", 3, PwshStartupPlatform.All, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, true),
        new("NoProfile", 3, PwshStartupPlatform.All, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, true),
        new("NoLogo", 3, PwshStartupPlatform.All, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, true),
        new("NonInteractive", 4, PwshStartupPlatform.All, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, true),
        new("SocketServerMode", 2, PwshStartupPlatform.All, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, false),
#if !UNIX
        new("V2SocketServerMode", 4, PwshStartupPlatform.Windows, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, false),
#endif
        new("ServerMode", 1, PwshStartupPlatform.All, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, false),
        new("NamedPipeServerMode", 3, PwshStartupPlatform.All, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, false),
        new("SSHServerMode", 4, PwshStartupPlatform.All, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, false),
        new("NoProfileLoadTime", 17, PwshStartupPlatform.All, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, true),
        new("Interactive", 1, PwshStartupPlatform.All, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, true),
        new("ConfigurationFile", 17, PwshStartupPlatform.All, PwshStartupArgumentKind.FilePath, PwshStartupArgumentConsumption.One, true),
        new("ConfigurationName", 6, PwshStartupPlatform.All, PwshStartupArgumentKind.String, PwshStartupArgumentConsumption.One, true),
        new("CustomPipeName", 3, PwshStartupPlatform.All, PwshStartupArgumentKind.String, PwshStartupArgumentConsumption.One, true),
        new("CommandWithArgs", 15, PwshStartupPlatform.All, PwshStartupArgumentKind.Command, PwshStartupArgumentConsumption.Remaining, true, "cwa", 3),
        new("Command", 1, PwshStartupPlatform.All, PwshStartupArgumentKind.Command, PwshStartupArgumentConsumption.Remaining, true),
        new("WindowStyle", 1, PwshStartupPlatform.Windows, PwshStartupArgumentKind.WindowStyle, PwshStartupArgumentConsumption.One, true),
        new("File", 1, PwshStartupPlatform.All, PwshStartupArgumentKind.ScriptPath, PwshStartupArgumentConsumption.Remaining, true),
#if DEBUG
        new("ISSWait", 7, PwshStartupPlatform.All, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, false),
#endif
        new("OutputFormat", 1, PwshStartupPlatform.All, PwshStartupArgumentKind.Format, PwshStartupArgumentConsumption.One, true, "of", 1),
        new("InputFormat", 3, PwshStartupPlatform.All, PwshStartupArgumentKind.Format, PwshStartupArgumentConsumption.One, true, "if", 2),
        new("ExecutionPolicy", 2, PwshStartupPlatform.Windows, PwshStartupArgumentKind.ExecutionPolicy, PwshStartupArgumentConsumption.One, true, "ep", 2),
        new("EncodedCommand", 1, PwshStartupPlatform.All, PwshStartupArgumentKind.EncodedCommand, PwshStartupArgumentConsumption.One, true, "ec", 1),
        new("EncodedArguments", 8, PwshStartupPlatform.All, PwshStartupArgumentKind.EncodedArguments, PwshStartupArgumentConsumption.One, false, "ea", 2),
        new("SettingsFile", 8, PwshStartupPlatform.All, PwshStartupArgumentKind.FilePath, PwshStartupArgumentConsumption.One, true),
        new("STA", 3, PwshStartupPlatform.Windows, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, true),
        new("MTA", 3, PwshStartupPlatform.Windows, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, true),
        new("WorkingDirectory", 2, PwshStartupPlatform.All, PwshStartupArgumentKind.DirectoryPath, PwshStartupArgumentConsumption.One, true, "wd", 2),
#if !UNIX
        new("RemoveWorkingDirectoryTrailingCharacter", 39, PwshStartupPlatform.Windows, PwshStartupArgumentKind.None, PwshStartupArgumentConsumption.None, false),
#endif
#if !UNIX
        new("Token", 2, PwshStartupPlatform.Windows, PwshStartupArgumentKind.String, PwshStartupArgumentConsumption.One, false),
#endif
#if !UNIX
        new("UTCTimestamp", 3, PwshStartupPlatform.Windows, PwshStartupArgumentKind.String, PwshStartupArgumentConsumption.One, false),
#endif
    ]);

    /// <summary>
    /// Resolves a normalized switch key using the host's historical abbreviation lengths and precedence.
    /// </summary>
    /// <remarks>
    /// Do not filter by advertised or supported options here: hidden options still parse, and unsupported
    /// options must reach the host's validation rather than being mistaken for implicit script paths.
    /// </remarks>
    internal static PwshStartupOption? Find(string switchKey)
    {
        foreach (PwshStartupOption option in Options)
        {
            if (option.Matches(switchKey))
            {
                return option;
            }
        }

        return null;
    }

    /// <summary>
    /// Removes a startup switch prefix without deciding whether the resulting key names a known option.
    /// </summary>
    /// <remarks>
    /// Preserve the host's spelling rules: slash or Unicode dash prefixes are accepted, and a second dash
    /// is removed only when it matches the first. Unknown keys may instead be implicit script filenames.
    /// </remarks>
    internal static bool TryGetSwitchKey(string argument, out string switchKey)
    {
        switchKey = argument.Trim();
        if (switchKey.Length == 0 || (!CharExtensions.IsDash(switchKey[0]) && switchKey[0] != '/'))
        {
            return false;
        }

        char prefix = switchKey[0];
        switchKey = switchKey.Substring(1);
        if (switchKey.Length != 0 && CharExtensions.IsDash(prefix) && switchKey[0] == prefix)
        {
            switchKey = switchKey.Substring(1);
        }

        return true;
    }
}
