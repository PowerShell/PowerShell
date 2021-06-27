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
            "outputformat",
            "removeworkingdirectorytrailingcharacter",
            "settingsfile",
            "version",
            "windowstyle",
            "workingdirectory"
        };

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
                    hostUI.WriteLine();
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
                    break;
                }

                if (MatchSwitch(switchKey, "help", "h") || MatchSwitch(switchKey, "?", "?"))
                {
                    _showHelp = true;
                    _showExtendedHelp = true;
                    _abortStartup = true;
                }
                else if (MatchSwitch(switchKey, "login", "l"))
                {
                    // On Windows, '-Login' does nothing.
                    // On *nix, '-Login' is already handled much earlier to improve startup performance, so we do nothing here.
                }
                else if (MatchSwitch(switchKey, "noexit", "noe"))
                {
                    _noExit = true;
                    noexitSeen = true;
                }
                else if (MatchSwitch(switchKey, "noprofile", "nop"))
                {
                    _skipUserInit = true;
                }
                else if (MatchSwitch(switchKey, "nologo", "nol"))
                {
                    _showBanner = false;
                }
                else if (MatchSwitch(switchKey, "noninteractive", "noni"))
                {
                    _noInteractive = true;
                }
                else if (MatchSwitch(switchKey, "socketservermode", "so"))
                {
                    _socketServerMode = true;
                }
                else if (MatchSwitch(switchKey, "servermode", "s"))
                {
                    _serverMode = true;
                }
                else if (MatchSwitch(switchKey, "namedpipeservermode", "nam"))
                {
                    _namedPipeServerMode = true;
                }
                else if (MatchSwitch(switchKey, "sshservermode", "sshs"))
                {
                    _sshServerMode = true;
                }
                else if (MatchSwitch(switchKey, "interactive", "i"))
                {
                    _noInteractive = false;
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
                }
                else if (MatchSwitch(switchKey, "command", "c"))
                {
                    if (!ParseCommand(args, ref i, noexitSeen, false))
                    {
                        break;
                    }
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
#endif
                }
                else if (MatchSwitch(switchKey, "file", "f"))
                {
                    if (!ParseFile(args, ref i, noexitSeen))
                    {
                        break;
                    }
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
                }
                else if (MatchSwitch(switchKey, "inputformat", "inp") || MatchSwitch(switchKey, "if", "if"))
                {
                    ParseFormat(args, ref i, ref _inFormat, CommandLineParameterParserStrings.MissingInputFormatParameter);
                }
                else if (MatchSwitch(switchKey, "executionpolicy", "ex") || MatchSwitch(switchKey, "ep", "ep"))
                {
                    ParseExecutionPolicy(args, ref i, ref _executionPolicy, CommandLineParameterParserStrings.MissingExecutionPolicyParameter);
                }
                else if (MatchSwitch(switchKey, "encodedcommand", "e") || MatchSwitch(switchKey, "ec", "e"))
                {
                    _wasCommandEncoded = true;
                    if (!ParseCommand(args, ref i, noexitSeen, true))
                    {
                        break;
                    }
                }
                else if (MatchSwitch(switchKey, "encodedarguments", "encodeda") || MatchSwitch(switchKey, "ea", "ea"))
                {
                    if (!CollectArgs(args, ref i))
                    {
                        break;
                    }
                }
                else if (MatchSwitch(switchKey, "settingsfile", "settings"))
                {
                    // Parse setting file arg and write error
                    if (!TryParseSettingFileHelper(args, ++i))
                    {
                        break;
                    }
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
            foreach (string s in Enum.GetNames(typeof(Serialization.DataFormat)))
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
        private bool _showVersion;
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
