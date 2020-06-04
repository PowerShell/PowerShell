// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// Holds the #defines for any special strings used in session state.
    /// </summary>
    internal static class StringLiterals
    {
        // constants

        /// <summary>
        /// The separator used in provider base paths. The format is
        /// providerId::providerPath.
        /// </summary>
        internal const string ProviderPathSeparator = "::";

        /// <summary>
        /// The default path separator used by the base implementation of the providers.
        ///
        /// Porting note: IO.Path.DirectorySeparatorChar is correct for all platforms. On Windows,
        /// it is '\', and on Linux, it is '/', as expected.
        /// </summary>
        internal static readonly char DefaultPathSeparator = System.IO.Path.DirectorySeparatorChar;
        internal static readonly string DefaultPathSeparatorString = DefaultPathSeparator.ToString();

        /// <summary>
        /// The alternate path separator used by the base implementation of the providers.
        ///
        /// Porting note: we do not use .NET's AlternatePathSeparatorChar here because it correctly
        /// states that both the default and alternate are '/' on Linux. However, for PowerShell to
        /// be "slash agnostic", we need to use the assumption that a '\' is the alternate path
        /// separator on Linux.
        /// </summary>
        internal static readonly char AlternatePathSeparator = Platform.IsWindows ? '/' : '\\';
        internal static readonly string AlternatePathSeparatorString = AlternatePathSeparator.ToString();

        /// <summary>
        /// The default path prefix for remote paths. This is to mimic
        /// UNC paths in the file system.
        /// </summary>
        internal const string DefaultRemotePathPrefix = "\\\\";

        /// <summary>
        /// The alternate path prefix for remote paths. This is to mimic
        /// UNC paths in the file system.
        /// </summary>
        internal const string AlternateRemotePathPrefix = "//";

        /// <summary>
        /// The character used in a path to indicate the home location.
        /// </summary>
        internal const string HomePath = "~";

        /// <summary>
        /// Name of the global variable table in Variable scopes of session state.
        /// </summary>
        internal const string Global = "GLOBAL";

        /// <summary>
        /// Name of the current scope variable table of session state.
        /// </summary>
        internal const string Local = "LOCAL";

        /// <summary>
        /// When prefixing a variable "private" makes the variable
        /// only visible in the current scope.
        /// </summary>
        internal const string Private = "PRIVATE";

        /// <summary>
        /// When prefixing a variable "script" makes the variable
        /// global to the script but not global to the entire session.
        /// </summary>
        internal const string Script = "SCRIPT";

        /// <summary>
        /// Session state string used as resource name in exceptions.
        /// </summary>
        internal const string SessionState = "SessionState";

        /// <summary>
        /// The file extension (including the dot) of an PowerShell script file.
        /// </summary>
        internal const string PowerShellScriptFileExtension = ".ps1";

        /// <summary>
        /// The file extension (including the dot) of an PowerShell module file.
        /// </summary>
        internal const string PowerShellModuleFileExtension = ".psm1";

        /// <summary>
        /// The file extension (including the dot) of an Mof file.
        /// </summary>
        internal const string PowerShellMofFileExtension = ".mof";

        /// <summary>
        /// The file extension (including the dot) of a PowerShell cmdletization file.
        /// </summary>
        internal const string PowerShellCmdletizationFileExtension = ".cdxml";

        /// <summary>
        /// The file extension (including the dot) of a PowerShell declarative session configuration file.
        /// </summary>
        internal const string PowerShellDISCFileExtension = ".pssc";

        /// <summary>
        /// The file extension (including the dot) of a PowerShell role capability file.
        /// </summary>
        internal const string PowerShellRoleCapabilityFileExtension = ".psrc";

        /// <summary>
        /// The file extension (including the dot) of an PowerShell data file.
        /// </summary>
        internal const string PowerShellDataFileExtension = ".psd1";

        /// <summary>
        /// The file extension (including the dot) of an workflow dependent assembly.
        /// </summary>
        internal const string PowerShellILAssemblyExtension = ".dll";

        /// <summary>
        /// The file extension (including the dot) of an workflow dependent Ngen assembly.
        /// </summary>
        internal const string PowerShellNgenAssemblyExtension = ".ni.dll";

        /// <summary>
        /// The file extension (including the dot) of an executable file.
        /// </summary>
        internal const string PowerShellILExecutableExtension = ".exe";

        internal const string PowerShellConsoleFileExtension = ".psc1";

        /// <summary>
        /// The default verb/noun separator for a command. verb-noun or verb/noun.
        /// </summary>
        internal const char CommandVerbNounSeparator = '-';

        /// <summary>
        /// The default verb to try if the command was not resolved.
        /// </summary>
        internal const string DefaultCommandVerb = "get";

        /// <summary>
        /// The default extension for a help file relative to its code assembly name.
        /// </summary>
        internal const string HelpFileExtension = "-Help.xml";

        /// <summary>
        /// The language representation of null.
        /// </summary>
        internal const string DollarNull = "$null";

        /// <summary>
        /// The language representation of null.
        /// </summary>
        internal const string Null = "null";

        /// <summary>
        /// The language representation of false.
        /// </summary>
        internal const string False = "false";

        /// <summary>
        /// The language representation of true.
        /// </summary>
        internal const string True = "true";

        /// <summary>
        /// The escape character used in the language.
        /// </summary>
        internal const char EscapeCharacter = '`';

        /// <summary>
        /// The default cmdlet adapter for cmdletization / cdxml modules.
        /// </summary>
        internal const string DefaultCmdletAdapter = "Microsoft.PowerShell.Cmdletization.Cim.CimCmdletAdapter, Microsoft.PowerShell.Commands.Management, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
    }
}
