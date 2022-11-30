// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Configuration;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Text;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// Null class implementation of PSHostUserInterface used when no console is available and when PowerShell
    /// is run in servmode where no console is needed.
    /// </summary>
    internal class NullHostUserInterface : PSHostUserInterface
    {
        /// <summary>
        /// RawUI.
        /// </summary>
        public override PSHostRawUserInterface? RawUI
            => null;

        /// <summary>
        /// Prompt.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="message"></param>
        /// <param name="descriptions"></param>
        /// <returns></returns>
        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
            => throw new PSNotImplementedException();

        /// <summary>
        /// PromptForChoice.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="message"></param>
        /// <param name="choices"></param>
        /// <param name="defaultChoice"></param>
        /// <returns></returns>
        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
            => throw new PSNotImplementedException();

        /// <summary>
        /// PromptForCredential.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="message"></param>
        /// <param name="userName"></param>
        /// <param name="targetName"></param>
        /// <returns></returns>
        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
            => throw new PSNotImplementedException();

        /// <summary>
        /// PromptForCredential.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="message"></param>
        /// <param name="userName"></param>
        /// <param name="targetName"></param>
        /// <param name="allowedCredentialTypes"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
            => throw new PSNotImplementedException();

        /// <summary>
        /// ReadLine.
        /// </summary>
        /// <returns></returns>
        public override string ReadLine()
            => throw new PSNotImplementedException();

        /// <summary>
        /// ReadLineAsSecureString.
        /// </summary>
        /// <returns></returns>
        public override SecureString ReadLineAsSecureString()
            => throw new PSNotImplementedException();

        /// <summary>
        /// Write.
        /// </summary>
        /// <param name="value"></param>
        public override void Write(string value)
        { }

        /// <summary>
        /// Write.
        /// </summary>
        /// <param name="foregroundColor"></param>
        /// <param name="backgroundColor"></param>
        /// <param name="value"></param>
        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        { }

        /// <summary>
        /// WriteDebugLine.
        /// </summary>
        /// <param name="message"></param>
        public override void WriteDebugLine(string message)
        { }

        /// <summary>
        /// WriteErrorLine.
        /// </summary>
        /// <param name="value"></param>
        public override void WriteErrorLine(string value)
            => Console.Out.WriteLine(value);

        /// <summary>
        /// WriteLine.
        /// </summary>
        /// <param name="value"></param>
        public override void WriteLine(string value)
        { }

        /// <summary>
        /// WriteProgress.
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="record"></param>
        public override void WriteProgress(long sourceId, ProgressRecord record)
        { }

        /// <summary>
        /// WriteVerboseLine.
        /// </summary>
        /// <param name="message"></param>
        public override void WriteVerboseLine(string message)
        { }

        /// <summary>
        /// WriteWarningLine.
        /// </summary>
        /// <param name="message"></param>
        public override void WriteWarningLine(string message)
        { }
    }

    internal class CommandLineParameterParser
    {
        private const int MaxPipePathLengthLinux = 108;
        private const int MaxPipePathLengthMacOS = 104;

        internal static int MaxNameLength()
        {
            if (Platform.IsWindows)
            {
                return ushort.MaxValue;
            }

            int maxLength = Platform.IsLinux ? MaxPipePathLengthLinux : MaxPipePathLengthMacOS;
            return maxLength - Path.GetTempPath().Length;
        }

        internal bool? InputRedirectedTestHook;

        private static readonly string[] s_validParameters = {
            "sta",
            "mta",
            "command",
            "configurationname",
            "custompipename",
            "encodedcommand",
            "executionpolicy",
            "file",
            "help",
            "inputformat",
            "login",
            "noexit",
            "nologo",
            "noninteractive",
            "noprofile",
            "noprofileloadtime",
            "outputformat",
            "removeworkingdirectorytrailingcharacter",
            "settingsfile",
            "version",
            "windowstyle",
            "workingdirectory"
        };

        /// <summary>
        /// These represent the parameters that are used when starting pwsh.
        /// We can query in our telemetry to determine how pwsh was invoked.
        /// </summary>
        [Flags]
        internal enum ParameterBitmap : long
        {
            Command             = 0x00000001, // -Command | -c
            ConfigurationName   = 0x00000002, // -ConfigurationName | -config
            CustomPipeName      = 0x00000004, // -CustomPipeName
            EncodedCommand      = 0x00000008, // -EncodedCommand | -e | -ec
            EncodedArgument     = 0x00000010, // -EncodedArgument
            ExecutionPolicy     = 0x00000020, // -ExecutionPolicy | -ex | -ep
            File                = 0x00000040, // -File | -f
            Help                = 0x00000080, // -Help, -?, /?
            InputFormat         = 0x00000100, // -InputFormat | -inp | -if
            Interactive         = 0x00000200, // -Interactive | -i
            Login               = 0x00000400, // -Login | -l
            MTA                 = 0x00000800, // -MTA
            NoExit              = 0x00001000, // -NoExit | -noe
            NoLogo              = 0x00002000, // -NoLogo | -nol
            NonInteractive      = 0x00004000, // -NonInteractive | -noni
            NoProfile           = 0x00008000, // -NoProfile | -nop
            OutputFormat        = 0x00010000, // -OutputFormat | -o | -of
            SettingsFile        = 0x00020000, // -SettingsFile | -settings
            SSHServerMode       = 0x00040000, // -SSHServerMode | -sshs
            SocketServerMode    = 0x00080000, // -SocketServerMode | -sockets
            ServerMode          = 0x00100000, // -ServerMode | -server
            NamedPipeServerMode = 0x00200000, // -NamedPipeServerMode | -namedpipes
            STA                 = 0x00400000, // -STA
            Version             = 0x00800000, // -Version | -v
            WindowStyle         = 0x01000000, // -WindowStyle | -w
            WorkingDirectory    = 0x02000000, // -WorkingDirectory | -wd
            ConfigurationFile   = 0x04000000, // -ConfigurationFile
            NoProfileLoadTime   = 0x08000000, // -NoProfileLoadTime
            // Enum values for specified ExecutionPolicy
            EPUnrestricted      = 0x0000000100000000, // ExecutionPolicy unrestricted
            EPRemoteSigned      = 0x0000000200000000, // ExecutionPolicy remote signed
            EPAllSigned         = 0x0000000400000000, // ExecutionPolicy all signed
            EPRestricted        = 0x0000000800000000, // ExecutionPolicy restricted
            EPDefault           = 0x0000001000000000, // ExecutionPolicy default
            EPBypass            = 0x0000002000000000, // ExecutionPolicy bypass
            EPUndefined         = 0x0000004000000000, // ExecutionPolicy undefined
            EPIncorrect         = 0x0000008000000000, // ExecutionPolicy incorrect
        }

        internal ParameterBitmap ParametersUsed = 0;

        internal double ParametersUsedAsDouble
        {
            get { return (double)ParametersUsed; }
        }

        [Conditional("DEBUG")]
        private void AssertArgumentsParsed()
        {
            if (!_dirty)
            {
                throw new InvalidOperationException("Parse has not been called yet");
            }
        }

        internal CommandLineParameterParser()
        {
        }

        #region Internal properties

        internal bool AbortStartup
        {
            get
            {
                AssertArgumentsParsed();
                return _abortStartup;
            }
        }

        internal string? SettingsFile
        {
            get
            {
                AssertArgumentsParsed();
                return _settingsFile;
            }
        }

        internal string? InitialCommand
        {
            get
            {
                AssertArgumentsParsed();
                return _commandLineCommand;
            }
        }

        internal bool WasInitialCommandEncoded
        {
            get
            {
                AssertArgumentsParsed();
                return _wasCommandEncoded;
            }
        }

#if !UNIX
        internal ProcessWindowStyle? WindowStyle
        {
            get
            {
                AssertArgumentsParsed();
                return _windowStyle;
            }
        }
#endif

        internal bool ShowBanner
        {
            get
            {
                AssertArgumentsParsed();
                return _showBanner;
            }
        }

        internal bool NoExit
        {
            get
            {
                AssertArgumentsParsed();
                return _noExit;
            }
        }

        internal bool SkipProfiles
        {
            get
            {
                AssertArgumentsParsed();
                return _skipUserInit;
            }
        }

        internal uint ExitCode
        {
            get
            {
                AssertArgumentsParsed();
                return _exitCode;
            }
        }

        internal bool ExplicitReadCommandsFromStdin
        {
            get
            {
                AssertArgumentsParsed();
                return _explicitReadCommandsFromStdin;
            }
        }

        internal bool NoPrompt
        {
            get
            {
                AssertArgumentsParsed();
                return _noPrompt;
            }
        }

        internal Collection<CommandParameter> Args
        {
            get
            {
                AssertArgumentsParsed();
                return _collectedArgs;
            }
        }

        internal string? ConfigurationFile
        {
            get
            {
                AssertArgumentsParsed();
                return _configurationFile;
            }
        }

        internal string? ConfigurationName
        {
            get
            {
                AssertArgumentsParsed();
                return _configurationName;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _configurationName = value;
                }
            }
        }

        internal bool SocketServerMode
        {
            get
            {
                AssertArgumentsParsed();
                return _socketServerMode;
            }
        }

        internal bool NamedPipeServerMode
        {
            get
            {
                AssertArgumentsParsed();
                return _namedPipeServerMode;
            }
        }

        internal bool SSHServerMode
        {
            get
            {
                AssertArgumentsParsed();
                return _sshServerMode;
            }
        }

        internal bool ServerMode
        {
            get
            {
                AssertArgumentsParsed();
                return _serverMode;
            }
        }

        // Added for using in xUnit tests
        internal string? ErrorMessage
        {
            get
            {
                AssertArgumentsParsed();
                return _error;
            }
        }

        // Added for using in xUnit tests
        internal bool ShowShortHelp
        {
            get
            {
                AssertArgumentsParsed();
                return _showHelp;
            }
        }

        // Added for using in xUnit tests
        internal bool ShowExtendedHelp
        {
            get
            {
                AssertArgumentsParsed();
                return _showExtendedHelp;
            }
        }

        internal bool NoProfileLoadTime
        {
            get
            {
                AssertArgumentsParsed();
                return _noProfileLoadTime;
            }
        }

        internal bool ShowVersion
        {
            get
            {
                AssertArgumentsParsed();
                return _showVersion;
            }
        }

        internal string? CustomPipeName
        {
            get
            {
                AssertArgumentsParsed();
                return _customPipeName;
            }
        }

        internal Serialization.DataFormat OutputFormat
        {
            get
            {
                AssertArgumentsParsed();
                return _outFormat;
            }
        }

        internal bool OutputFormatSpecified
        {
            get
            {
                AssertArgumentsParsed();
                return _outputFormatSpecified;
            }
        }

        internal Serialization.DataFormat InputFormat
        {
            get
            {
                AssertArgumentsParsed();
                return _inFormat;
            }
        }

        internal string? File
        {
            get
            {
                AssertArgumentsParsed();
                return _file;
            }
        }

        internal string? ExecutionPolicy
        {
            get
            {
                AssertArgumentsParsed();
                return _executionPolicy;
            }
        }

        internal bool StaMode
        {
            get
            {
                AssertArgumentsParsed();
                if (_staMode.HasValue)
                {
                    return _staMode.Value;
                }
                else
                {
                    return Platform.IsStaSupported;
                }
            }
        }

        internal bool ThrowOnReadAndPrompt
        {
            get
            {
                AssertArgumentsParsed();
                return _noInteractive;
            }
        }

        internal bool NonInteractive
        {
            get
            {
                AssertArgumentsParsed();
                return _noInteractive;
            }
        }

        internal string? WorkingDirectory
        {
            get
            {
                AssertArgumentsParsed();
#if !UNIX
                if (_removeWorkingDirectoryTrailingCharacter && _workingDirectory?.Length > 0)
                {
                    return _workingDirectory.Remove(_workingDirectory.Length - 1);
                }
#endif
                return _workingDirectory;
            }
        }

#if !UNIX
        internal bool RemoveWorkingDirectoryTrailingCharacter
        {
            get
            {
                AssertArgumentsParsed();
                return _removeWorkingDirectoryTrailingCharacter;
            }
        }
#endif

        #endregion Internal properties

        #region static methods
        /// <summary>
        /// Processes the -SettingFile Argument.
        /// </summary>
        /// <param name="args">
        /// The command line parameters to be processed.
        /// </param>
        /// <param name="settingFileArgIndex">
        /// The index in args to the argument following '-SettingFile'.
        /// </param>
        /// <returns>
        /// Returns true if the argument was parsed successfully and false if not.
        /// </returns>
        private bool TryParseSettingFileHelper(string[] args, int settingFileArgIndex)
        {
            if (settingFileArgIndex >= args.Length)
            {
                SetCommandLineError(CommandLineParameterParserStrings.MissingSettingsFileArgument);
                return false;
            }

            string configFile;
            try
            {
                configFile = NormalizeFilePath(args[settingFileArgIndex]);
            }
            catch (Exception ex)
            {
                string error = string.Format(CultureInfo.CurrentCulture, CommandLineParameterParserStrings.InvalidSettingsFileArgument, args[settingFileArgIndex], ex.Message);
                SetCommandLineError(error);
                return false;
            }

            if (!System.IO.File.Exists(configFile))
            {
                string error = string.Format(CultureInfo.CurrentCulture, CommandLineParameterParserStrings.SettingsFileNotExists, configFile);
                SetCommandLineError(error);
                return false;
            }

            _settingsFile = configFile;

            return true;
        }

        internal static string GetConfigurationNameFromGroupPolicy()
        {
            // Current user policy takes precedence.
            var consoleSessionSetting = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.CurrentUserThenSystemWideConfig);

            return (consoleSessionSetting?.EnableConsoleSessionConfiguration == true && !string.IsNullOrEmpty(consoleSessionSetting?.ConsoleSessionConfigurationName)) ?
                    consoleSessionSetting.ConsoleSessionConfigurationName : string.Empty;
        }

        /// <summary>
        /// Gets the word in a switch from the current argument or parses a file.
        /// For example -foo, /foo, or --foo would return 'foo'.
        /// </summary>
        /// <param name="args">
        /// The command line parameters to be processed.
        /// </param>
        /// <param name="argIndex">
        /// The index in args to the argument to process.
        /// </param>
        /// <param name="noexitSeen">
        /// Used during parsing files.
        /// </param>
        /// <returns>
        /// Returns a Tuple:
        /// The first value is a String called 'switchKey' with the word in a switch from the current argument or null.
        /// The second value is a bool called 'shouldBreak', indicating if the parsing look should break.
        /// </returns>
        private (string switchKey, bool shouldBreak) GetSwitchKey(string[] args, ref int argIndex, ref bool noexitSeen)
        {
            string switchKey = args[argIndex].Trim();
            if (string.IsNullOrEmpty(switchKey))
            {
                return (switchKey: string.Empty, shouldBreak: false);
            }

            char firstChar = switchKey[0];
            if (!CharExtensions.IsDash(firstChar) && firstChar != '/')
            {
                // then it's a file
                --argIndex;
                ParseFile(args, ref argIndex, noexitSeen);

                return (switchKey: string.Empty, shouldBreak: true);
            }

            // chop off the first character so that we're agnostic wrt specifying / or -
            // in front of the switch name.
            switchKey = switchKey.Substring(1);

            // chop off the second dash so we're agnostic wrt specifying - or --
            if (!string.IsNullOrEmpty(switchKey) && CharExtensions.IsDash(firstChar) && switchKey[0] == firstChar)
            {
                switchKey = switchKey.Substring(1);
            }

            return (switchKey: switchKey, shouldBreak: false);
        }

        internal static string NormalizeFilePath(string path)
        {
            // Normalize slashes
            path = path.Replace(
                StringLiterals.AlternatePathSeparator,
                StringLiterals.DefaultPathSeparator);

            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Determine the execution policy based on the supplied string.
        /// If the string doesn't match to any known execution policy, set it to incorrect.
        /// </summary>
        /// <param name="_executionPolicy">The value provided on the command line.</param>
        /// <returns>The execution policy.</returns>
        private static ParameterBitmap GetExecutionPolicy(string? _executionPolicy)
        {
            if (_executionPolicy is null)
            {
                return ParameterBitmap.EPUndefined;
            }

            ParameterBitmap executionPolicySetting = ParameterBitmap.EPIncorrect;

            if (string.Equals(_executionPolicy, "default", StringComparison.OrdinalIgnoreCase))
            {
                executionPolicySetting = ParameterBitmap.EPDefault;
            }
            else if (string.Equals(_executionPolicy, "remotesigned", StringComparison.OrdinalIgnoreCase))
            {
                executionPolicySetting = ParameterBitmap.EPRemoteSigned;
            }
            else if (string.Equals(_executionPolicy, "bypass", StringComparison.OrdinalIgnoreCase))
            {
                executionPolicySetting = ParameterBitmap.EPBypass;
            }
            else if (string.Equals(_executionPolicy, "allsigned", StringComparison.OrdinalIgnoreCase))
            {
                executionPolicySetting = ParameterBitmap.EPAllSigned;
            }
            else if (string.Equals(_executionPolicy, "restricted", StringComparison.OrdinalIgnoreCase))
            {
                executionPolicySetting = ParameterBitmap.EPRestricted;
            }
            else if (string.Equals(_executionPolicy, "unrestricted", StringComparison.OrdinalIgnoreCase))
            {
                executionPolicySetting = ParameterBitmap.EPUnrestricted;
            }
            else if (string.Equals(_executionPolicy, "undefined", StringComparison.OrdinalIgnoreCase))
            {
                executionPolicySetting = ParameterBitmap.EPUndefined;
            }

            return executionPolicySetting;
        }

        private static bool MatchSwitch(string switchKey, string match, string smallestUnambiguousMatch)
        {
            Dbg.Assert(!string.IsNullOrEmpty(match), "need a value");
            Dbg.Assert(match.Trim().ToLowerInvariant() == match, "match should be normalized to lowercase w/ no outside whitespace");
            Dbg.Assert(smallestUnambiguousMatch.Trim().ToLowerInvariant() == smallestUnambiguousMatch, "match should be normalized to lowercase w/ no outside whitespace");
            Dbg.Assert(match.Contains(smallestUnambiguousMatch), "sUM should be a substring of match");

            return (switchKey.Length >= smallestUnambiguousMatch.Length
                    && match.StartsWith(switchKey, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        private void ShowError(PSHostUserInterface hostUI)
        {
            if (_error != null)
            {
                hostUI.WriteErrorLine(_error);
            }
        }

        private void ShowHelp(PSHostUserInterface hostUI, string? helpText)
        {
            if (helpText is null)
            {
                return;
            }

            if (_showHelp)
            {
                hostUI.WriteLine();
                hostUI.Write(helpText);
                if (_showExtendedHelp)
                {
                    hostUI.Write(ManagedEntranceStrings.ExtendedHelp);
                }

                hostUI.WriteLine();
            }
        }

        private void DisplayBanner(PSHostUserInterface hostUI, string? bannerText)
        {
            if (_showBanner && !_showHelp)
            {
                // If banner text is not supplied do nothing.
                if (!string.IsNullOrEmpty(bannerText))
                {
                    hostUI.WriteLine(bannerText);
                }

                if (UpdatesNotification.CanNotifyUpdates)
                {
                    UpdatesNotification.ShowUpdateNotification(hostUI);
                }
            }
        }

        /// <summary>
        /// Processes all the command line parameters to ConsoleHost.  Returns the exit code to be used to terminate the process, or
        /// Success to indicate that the program should continue running.
        /// </summary>
        /// <param name="args">
        /// The command line parameters to be processed.
        /// </param>
        internal void Parse(string[] args)
        {
            if (_dirty)
            {
                throw new InvalidOperationException("This instance has already been used. Create a new instance.");
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is null)
                {
                    throw new ArgumentNullException(nameof(args), CommandLineParameterParserStrings.NullElementInArgs);
                }
            }

            // Indicates that we've called this method on this instance, and that when it's done, the state variables
            // will reflect the parse.
            _dirty = true;

            ParseHelper(args);
        }

        private void ParseHelper(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }

            bool noexitSeen = false;

            for (int i = 0; i < args.Length; ++i)
            {
                (string switchKey, bool shouldBreak) switchKeyResults = GetSwitchKey(args, ref i, ref noexitSeen);
                if (switchKeyResults.shouldBreak)
                {
                    break;
                }

                string switchKey = switchKeyResults.switchKey;

                // If version is in the commandline, don't continue to look at any other parameters
                if (MatchSwitch(switchKey, "version", "v"))
                {
                    _showVersion = true;
                    _showBanner = false;
                    _noInteractive = true;
                    _skipUserInit = true;
                    _noExit = false;
                    ParametersUsed |= ParameterBitmap.Version;
                    break;
                }

                if (MatchSwitch(switchKey, "help", "h") || MatchSwitch(switchKey, "?", "?"))
                {
                    _showHelp = true;
                    _showExtendedHelp = true;
                    _abortStartup = true;
                    ParametersUsed |= ParameterBitmap.Help;
                }
                else if (MatchSwitch(switchKey, "login", "l"))
                {
                    // On Windows, '-Login' does nothing.
                    // On *nix, '-Login' is already handled much earlier to improve startup performance, so we do nothing here.
                    ParametersUsed |= ParameterBitmap.Login;
                }
                else if (MatchSwitch(switchKey, "noexit", "noe"))
                {
                    _noExit = true;
                    noexitSeen = true;
                    ParametersUsed |= ParameterBitmap.NoExit;
                }
                else if (MatchSwitch(switchKey, "noprofile", "nop"))
                {
                    _skipUserInit = true;
                    ParametersUsed |= ParameterBitmap.NoProfile;
                }
                else if (MatchSwitch(switchKey, "nologo", "nol"))
                {
                    _showBanner = false;
                    ParametersUsed |= ParameterBitmap.NoLogo;
                }
                else if (MatchSwitch(switchKey, "noninteractive", "noni"))
                {
                    _noInteractive = true;
                    ParametersUsed |= ParameterBitmap.NonInteractive;
                }
                else if (MatchSwitch(switchKey, "socketservermode", "so"))
                {
                    _socketServerMode = true;
                    _showBanner = false;
                    ParametersUsed |= ParameterBitmap.SocketServerMode;
                }
                else if (MatchSwitch(switchKey, "servermode", "s"))
                {
                    _serverMode = true;
                    _showBanner = false;
                    ParametersUsed |= ParameterBitmap.ServerMode;
                }
                else if (MatchSwitch(switchKey, "namedpipeservermode", "nam"))
                {
                    _namedPipeServerMode = true;
                    _showBanner = false;
                    ParametersUsed |= ParameterBitmap.NamedPipeServerMode;
                }
                else if (MatchSwitch(switchKey, "sshservermode", "sshs"))
                {
                    _sshServerMode = true;
                    _showBanner = false;
                    ParametersUsed |= ParameterBitmap.SSHServerMode;
                }
                else if (MatchSwitch(switchKey, "noprofileloadtime", "noprofileloadtime"))
                {
                    _noProfileLoadTime = true;
                    ParametersUsed |= ParameterBitmap.NoProfileLoadTime;
                }
                else if (MatchSwitch(switchKey, "interactive", "i"))
                {
                    _noInteractive = false;
                    ParametersUsed |= ParameterBitmap.Interactive;
                }
                else if (MatchSwitch(switchKey, "configurationfile", "configurationfile"))
                {
                    ++i;
                    if (i >= args.Length)
                    {
                        SetCommandLineError(
                            CommandLineParameterParserStrings.MissingConfigurationFileArgument);
                        break;
                    }

                    _configurationFile = args[i];
                    ParametersUsed |= ParameterBitmap.ConfigurationFile;
                }
                else if (MatchSwitch(switchKey, "configurationname", "config"))
                {
                    ++i;
                    if (i >= args.Length)
                    {
                        SetCommandLineError(
                            CommandLineParameterParserStrings.MissingConfigurationNameArgument);
                        break;
                    }

                    _configurationName = args[i];
                    ParametersUsed |= ParameterBitmap.ConfigurationName;
                }
                else if (MatchSwitch(switchKey, "custompipename", "cus"))
                {
                    ++i;
                    if (i >= args.Length)
                    {
                        SetCommandLineError(
                            CommandLineParameterParserStrings.MissingCustomPipeNameArgument);
                        break;
                    }

#if UNIX
                    int maxNameLength = MaxNameLength();
                    if (args[i].Length > maxNameLength)
                    {
                        SetCommandLineError(
                            string.Format(
                                CommandLineParameterParserStrings.CustomPipeNameTooLong,
                                maxNameLength,
                                args[i],
                                args[i].Length));
                        break;
                    }
#endif

                    _customPipeName = args[i];
                    ParametersUsed |= ParameterBitmap.CustomPipeName;
                }
                else if (MatchSwitch(switchKey, "command", "c"))
                {
                    if (!ParseCommand(args, ref i, noexitSeen, false))
                    {
                        break;
                    }

                    ParametersUsed |= ParameterBitmap.Command;
                }
                else if (MatchSwitch(switchKey, "windowstyle", "w"))
                {
#if UNIX
                    SetCommandLineError(
                        CommandLineParameterParserStrings.WindowStyleArgumentNotImplemented);
                    break;
#else
                    ++i;
                    if (i >= args.Length)
                    {
                        SetCommandLineError(
                            CommandLineParameterParserStrings.MissingWindowStyleArgument);
                        break;
                    }

                    try
                    {
                        _windowStyle = LanguagePrimitives.ConvertTo<ProcessWindowStyle>(args[i]);
                    }
                    catch (PSInvalidCastException e)
                    {
                        SetCommandLineError(
                            string.Format(CultureInfo.CurrentCulture, CommandLineParameterParserStrings.InvalidWindowStyleArgument, args[i], e.Message));
                        break;
                    }

                    ParametersUsed |= ParameterBitmap.WindowStyle;
#endif
                }
                else if (MatchSwitch(switchKey, "file", "f"))
                {
                    if (!ParseFile(args, ref i, noexitSeen))
                    {
                        break;
                    }

                    ParametersUsed |= ParameterBitmap.File;
                }
#if DEBUG
                else if (MatchSwitch(switchKey, "isswait", "isswait"))
                {
                    // Just toss this option, it was processed earlier in 'ManagedEntrance.Start()'.
                }
#endif
                else if (MatchSwitch(switchKey, "outputformat", "o") || MatchSwitch(switchKey, "of", "o"))
                {
                    ParseFormat(args, ref i, ref _outFormat, CommandLineParameterParserStrings.MissingOutputFormatParameter);
                    _outputFormatSpecified = true;
                    ParametersUsed |= ParameterBitmap.OutputFormat;
                }
                else if (MatchSwitch(switchKey, "inputformat", "inp") || MatchSwitch(switchKey, "if", "if"))
                {
                    ParseFormat(args, ref i, ref _inFormat, CommandLineParameterParserStrings.MissingInputFormatParameter);
                    ParametersUsed |= ParameterBitmap.InputFormat;
                }
                else if (MatchSwitch(switchKey, "executionpolicy", "ex") || MatchSwitch(switchKey, "ep", "ep"))
                {
                    ParseExecutionPolicy(args, ref i, ref _executionPolicy, CommandLineParameterParserStrings.MissingExecutionPolicyParameter);
                    ParametersUsed |= ParameterBitmap.ExecutionPolicy;
                    ParametersUsed |= GetExecutionPolicy(_executionPolicy);
                }
                else if (MatchSwitch(switchKey, "encodedcommand", "e") || MatchSwitch(switchKey, "ec", "e"))
                {
                    _wasCommandEncoded = true;
                    if (!ParseCommand(args, ref i, noexitSeen, true))
                    {
                        break;
                    }

                    ParametersUsed |= ParameterBitmap.EncodedCommand;
                }
                else if (MatchSwitch(switchKey, "encodedarguments", "encodeda") || MatchSwitch(switchKey, "ea", "ea"))
                {
                    if (!CollectArgs(args, ref i))
                    {
                        break;
                    }

                    ParametersUsed |= ParameterBitmap.EncodedArgument;
                }
                else if (MatchSwitch(switchKey, "settingsfile", "settings"))
                {
                    // Parse setting file arg and write error
                    if (!TryParseSettingFileHelper(args, ++i))
                    {
                        break;
                    }

                    ParametersUsed |= ParameterBitmap.SettingsFile;
                }
                else if (MatchSwitch(switchKey, "sta", "sta"))
                {
                    if (!Platform.IsWindowsDesktop || !Platform.IsStaSupported)
                    {
                        SetCommandLineError(
                            CommandLineParameterParserStrings.STANotImplemented);
                        break;
                    }

                    if (_staMode.HasValue)
                    {
                        // -sta and -mta are mutually exclusive.
                        SetCommandLineError(
                            CommandLineParameterParserStrings.MtaStaMutuallyExclusive);
                        break;
                    }

                    _staMode = true;
                    ParametersUsed |= ParameterBitmap.STA;
                }
                else if (MatchSwitch(switchKey, "mta", "mta"))
                {
                    if (!Platform.IsWindowsDesktop)
                    {
                        SetCommandLineError(
                            CommandLineParameterParserStrings.MTANotImplemented);
                        break;
                    }

                    if (_staMode.HasValue)
                    {
                        // -sta and -mta are mutually exclusive.
                        SetCommandLineError(
                            CommandLineParameterParserStrings.MtaStaMutuallyExclusive);
                        break;
                    }

                    _staMode = false;
                    ParametersUsed |= ParameterBitmap.MTA;
                }
                else if (MatchSwitch(switchKey, "workingdirectory", "wo") || MatchSwitch(switchKey, "wd", "wd"))
                {
                    ++i;
                    if (i >= args.Length)
                    {
                        SetCommandLineError(
                            CommandLineParameterParserStrings.MissingWorkingDirectoryArgument);
                        break;
                    }

                    _workingDirectory = args[i];
                    ParametersUsed |= ParameterBitmap.WorkingDirectory;
                }
#if !UNIX
                else if (MatchSwitch(switchKey, "removeworkingdirectorytrailingcharacter", "removeworkingdirectorytrailingcharacter"))
                {
                    _removeWorkingDirectoryTrailingCharacter = true;
                }
#endif
                else
                {
                    // The first parameter we fail to recognize marks the beginning of the file string.
                    --i;
                    if (!ParseFile(args, ref i, noexitSeen))
                    {
                        break;
                    }

                    // default to filename being the next argument.
                    ParametersUsed |= ParameterBitmap.File;
                }
            }

            Dbg.Assert(
                    ((_exitCode == ConsoleHost.ExitCodeBadCommandLineParameter) && _abortStartup)
                || (_exitCode == ConsoleHost.ExitCodeSuccess),
                "if exit code is failure, then abortstartup should be true");
        }

        internal void ShowErrorHelpBanner(PSHostUserInterface hostUI, string? bannerText, string? helpText)
        {
            ShowError(hostUI);
            ShowHelp(hostUI, helpText);
            DisplayBanner(hostUI, bannerText);
        }

        private void SetCommandLineError(string msg, bool showHelp = false, bool showBanner = false)
        {
            if (_error != null)
            {
                throw new ArgumentException(nameof(SetCommandLineError), nameof(_error));
            }

            _error = msg;
            _showHelp = showHelp;
            _showBanner = showBanner;
            _abortStartup = true;
            _exitCode = ConsoleHost.ExitCodeBadCommandLineParameter;
        }

        private void ParseFormat(string[] args, ref int i, ref Serialization.DataFormat format, string resourceStr)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in Enum.GetNames<Serialization.DataFormat>())
            {
                sb.Append(s);
                sb.Append(Environment.NewLine);
            }

            ++i;
            if (i >= args.Length)
            {
                SetCommandLineError(
                    StringUtil.Format(
                        resourceStr,
                        sb.ToString()),
                    showHelp: true);
                return;
            }

            try
            {
                format = (Serialization.DataFormat)Enum.Parse(typeof(Serialization.DataFormat), args[i], true);
            }
            catch (ArgumentException)
            {
                SetCommandLineError(
                    StringUtil.Format(
                        CommandLineParameterParserStrings.BadFormatParameterValue,
                        args[i],
                        sb.ToString()),
                    showHelp: true);
            }
        }

        private void ParseExecutionPolicy(string[] args, ref int i, ref string? executionPolicy, string resourceStr)
        {
            ++i;
            if (i >= args.Length)
            {
                SetCommandLineError(resourceStr, showHelp: true);
                return;
            }

            executionPolicy = args[i];
        }

        // Process file execution. We don't need to worry about checking -command
        // since if -command comes before -file, -file will be treated as part
        // of the script to evaluate. If -file comes before -command, it will
        // treat -command as an argument to the script...
        private bool ParseFile(string[] args, ref int i, bool noexitSeen)
        {
            // Try parse '$true', 'true', '$false' and 'false' values.
            static object ConvertToBoolIfPossible(string arg)
            {
                // Before parsing we skip '$' if present.
                return arg.Length > 0 && bool.TryParse(arg.AsSpan(arg[0] == '$' ? 1 : 0), out bool boolValue)
                    ? (object)boolValue
                    : (object)arg;
            }

            ++i;
            if (i >= args.Length)
            {
                SetCommandLineError(
                    CommandLineParameterParserStrings.MissingFileArgument,
                    showHelp: true,
                    showBanner: false);
                return false;
            }

            // Don't show the startup banner unless -noexit has been specified.
            if (!noexitSeen)
                _showBanner = false;

            // Process interactive input...
            if (args[i] == "-")
            {
                // the arg to -file is -, which is secret code for "read the commands from stdin with prompts"

                _explicitReadCommandsFromStdin = true;
                _noPrompt = false;
            }
            else
            {
                // Exit on script completion unless -noexit was specified...
                if (!noexitSeen)
                    _noExit = false;

                // We need to get the full path to the script because it will be
                // executed after the profiles are run and they may change the current
                // directory.
                try
                {
                    _file = NormalizeFilePath(args[i]);
                }
                catch (Exception e)
                {
                    // Catch all exceptions - we're just going to exit anyway so there's
                    // no issue of the system being destabilized.
                    SetCommandLineError(
                        string.Format(CultureInfo.CurrentCulture, CommandLineParameterParserStrings.InvalidFileArgument, args[i], e.Message),
                        showBanner: false);
                    return false;
                }

                if (!System.IO.File.Exists(_file))
                {
                    if (args[i].StartsWith('-') && args[i].Length > 1)
                    {
                        string param = args[i].Substring(1, args[i].Length - 1);
                        StringBuilder possibleParameters = new StringBuilder();
                        foreach (string validParameter in s_validParameters)
                        {
                            if (validParameter.Contains(param, StringComparison.OrdinalIgnoreCase))
                            {
                                possibleParameters.Append("\n  -");
                                possibleParameters.Append(validParameter);
                            }
                        }

                        if (possibleParameters.Length > 0)
                        {
                            SetCommandLineError(
                                string.Format(CultureInfo.CurrentCulture, CommandLineParameterParserStrings.InvalidArgument, args[i])
                                    + Environment.NewLine
                                    + possibleParameters.ToString(),
                                showBanner: false);
                            return false;
                        }
                    }

                    SetCommandLineError(
                        string.Format(CultureInfo.CurrentCulture, CommandLineParameterParserStrings.ArgumentFileDoesNotExist, args[i]),
                        showHelp: true);
                    return false;
                }
#if !UNIX
                // Only do the .ps1 extension check on Windows since shebang is not supported
                if (!_file.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                {
                    SetCommandLineError(string.Format(CultureInfo.CurrentCulture, CommandLineParameterParserStrings.InvalidFileArgumentExtension, args[i]));
                    return false;
                }
#endif

                i++;

                string? pendingParameter = null;

                // Accumulate the arguments to this script...
                while (i < args.Length)
                {
                    string arg = args[i];

                    // If there was a pending parameter, add a named parameter
                    // using the pending parameter and current argument
                    if (pendingParameter != null)
                    {
                        _collectedArgs.Add(new CommandParameter(pendingParameter, arg));
                        pendingParameter = null;
                    }
                    else if (!string.IsNullOrEmpty(arg) && CharExtensions.IsDash(arg[0]) && arg.Length > 1)
                    {
                        int offset = arg.IndexOf(':');
                        if (offset >= 0)
                        {
                            if (offset == arg.Length - 1)
                            {
                                pendingParameter = arg.TrimEnd(':');
                            }
                            else
                            {
                                string argValue = arg.Substring(offset + 1);
                                string argName = arg.Substring(0, offset);
                                _collectedArgs.Add(new CommandParameter(argName, ConvertToBoolIfPossible(argValue)));
                            }
                        }
                        else
                        {
                            _collectedArgs.Add(new CommandParameter(arg));
                        }
                    }
                    else
                    {
                        _collectedArgs.Add(new CommandParameter(null, arg));
                    }

                    ++i;
                }
            }

            return true;
        }

        private bool ParseCommand(string[] args, ref int i, bool noexitSeen, bool isEncoded)
        {
            if (_commandLineCommand != null)
            {
                // we've already set the command, so squawk
                SetCommandLineError(CommandLineParameterParserStrings.CommandAlreadySpecified, showHelp: true);
                return false;
            }

            ++i;
            if (i >= args.Length)
            {
                SetCommandLineError(CommandLineParameterParserStrings.MissingCommandParameter, showHelp: true);
                return false;
            }

            if (isEncoded)
            {
                try
                {
                    _commandLineCommand = StringToBase64Converter.Base64ToString(args[i]);
                }
                // decoding failed
                catch
                {
                    SetCommandLineError(CommandLineParameterParserStrings.BadCommandValue, showHelp: true);
                    return false;
                }
            }
            else if (args[i] == "-")
            {
                // the arg to -command is -, which is secret code for "read the commands from stdin with no prompts"

                _explicitReadCommandsFromStdin = true;
                _noPrompt = true;

                ++i;
                if (i != args.Length)
                {
                    // there are more parameters to -command than -, which is an error.

                    SetCommandLineError(CommandLineParameterParserStrings.TooManyParametersToCommand, showHelp: true);
                    return false;
                }

                if (InputRedirectedTestHook.HasValue ? !InputRedirectedTestHook.Value : !Console.IsInputRedirected)
                {
                    SetCommandLineError(CommandLineParameterParserStrings.StdinNotRedirected, showHelp: true);
                    return false;
                }
            }
            else
            {
                // Collect the remaining parameters and combine them into a single command to be run.

                StringBuilder cmdLineCmdSB = new StringBuilder();

                while (i < args.Length)
                {
                    cmdLineCmdSB.Append(args[i] + " ");
                    ++i;
                }

                if (cmdLineCmdSB.Length > 0)
                {
                    // remove the last blank
                    cmdLineCmdSB.Remove(cmdLineCmdSB.Length - 1, 1);
                }

                _commandLineCommand = cmdLineCmdSB.ToString();
            }

            if (!noexitSeen && !_explicitReadCommandsFromStdin)
            {
                // don't reset this if they've already specified -noexit
                _noExit = false;
            }

            _showBanner = false;

            return true;
        }

        private bool CollectArgs(string[] args, ref int i)
        {
            if (_collectedArgs.Count != 0)
            {
                SetCommandLineError(CommandLineParameterParserStrings.ArgsAlreadySpecified, showHelp: true);
                return false;
            }

            ++i;
            if (i >= args.Length)
            {
                SetCommandLineError(CommandLineParameterParserStrings.MissingArgsValue, showHelp: true);
                return false;
            }

            try
            {
                object[] a = StringToBase64Converter.Base64ToArgsConverter(args[i]);
                if (a != null)
                {
                    foreach (object obj in a)
                    {
                        _collectedArgs.Add(new CommandParameter(null, obj));
                    }
                }
            }
            catch
            {
                // decoding failed
                SetCommandLineError(CommandLineParameterParserStrings.BadArgsValue, showHelp: true);
                return false;
            }

            return true;
        }

        private bool _socketServerMode;
        private bool _serverMode;
        private bool _namedPipeServerMode;
        private bool _sshServerMode;
        private bool _noProfileLoadTime;
        private bool _showVersion;
        private string? _configurationFile;
        private string? _configurationName;
        private string? _error;
        private bool _showHelp;
        private bool _showExtendedHelp;
        private bool _showBanner = true;
        private bool _noInteractive;
        private bool _abortStartup;
        private bool _skipUserInit;
        private string? _customPipeName;
        private bool? _staMode = null;
        private bool _noExit = true;
        private bool _explicitReadCommandsFromStdin;
        private bool _noPrompt;
        private string? _commandLineCommand;
        private bool _wasCommandEncoded;
        private uint _exitCode = ConsoleHost.ExitCodeSuccess;
        private bool _dirty;
        private Serialization.DataFormat _outFormat = Serialization.DataFormat.Text;
        private bool _outputFormatSpecified = false;
        private Serialization.DataFormat _inFormat = Serialization.DataFormat.Text;
        private readonly Collection<CommandParameter> _collectedArgs = new Collection<CommandParameter>();
        private string? _file;
        private string? _executionPolicy;
        private string? _settingsFile;
        private string? _workingDirectory;

#if !UNIX
        private ProcessWindowStyle? _windowStyle;
        private bool _removeWorkingDirectoryTrailingCharacter = false;
#endif
    }
}   // namespace
