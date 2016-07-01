/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/


using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Diagnostics;
using Microsoft.Win32;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell
{
    internal class CommandLineParameterParser
    {
        internal CommandLineParameterParser(ConsoleHost p, Version ver, string bannerText, string helpText)
        {
            Dbg.Assert(p != null, "parent ConsoleHost must be supplied");

            this.bannerText = bannerText;
            this.helpText = helpText;
            this.parent = p;
            this.ui = (ConsoleHostUserInterface)p.UI;
            this.ver = ver;
        }

        internal bool AbortStartup
        {
            get
            {
                Dbg.Assert(dirty, "Parse has not been called yet");

                return abortStartup;
            }
        }

        internal string InitialCommand
        {
            get
            {
                Dbg.Assert(dirty, "Parse has not been called yet");

                return commandLineCommand;
            }
        }

        internal bool WasInitialCommandEncoded
        {
            get
            {
                Dbg.Assert(dirty, "Parse has not been called yet");

                return wasCommandEncoded;
            }
        }

        internal bool ShowBanner
        {
            get
            {
                Dbg.Assert(dirty, "Parse has not been called yet");
                return showBanner;
            }
        }

        internal bool NoExit
        {
            get
            {
                Dbg.Assert(dirty, "Parse has not been called yet");

                return noExit;
            }
        }

        internal bool ImportSystemModules
        {
            get
            {
                return importSystemModules;
            }
        }

        internal bool SkipProfiles
        {
            get
            {
                Dbg.Assert(dirty, "Parse has not been called yet");

                return skipUserInit;
            }
        }

        internal uint ExitCode
        {
            get
            {
                Dbg.Assert(dirty, "Parse has not been called yet");

                return exitCode;
            }
        }

        internal bool ExplicitReadCommandsFromStdin
        {
            get
            {
                Dbg.Assert(dirty, "Parse has not been called yet");

                return explicitReadCommandsFromStdin;
            }
        }

        internal bool NoPrompt
        {
            get
            {
                Dbg.Assert(dirty, "Parse has not been called yet");

                return noPrompt;
            }
        }

        internal Collection<CommandParameter> Args
        {
            get
            {
                Dbg.Assert(dirty, "Parse has not been called yet");

                return collectedArgs;
            }
        }

        internal string ConfigurationName
        {
            get { return configurationName; }
        }

        internal bool SocketServerMode
        {
            get
            {
                return socketServerMode;
            }
        }

        internal bool NamedPipeServerMode
        {
            get { return namedPipeServerMode; }
        }

        internal bool ServerMode
        {
            get
            {
                return serverMode;
            }
        }

        internal Serialization.DataFormat OutputFormat
        {
            get
            {
                Dbg.Assert(dirty, "Parse has not been called yet");

                return outFormat;
            }
        }

        internal Serialization.DataFormat InputFormat
        {
            get
            {
                Dbg.Assert(dirty, "Parse has not been called yet");
                return inFormat;
            }
        }

        internal string File
        {
            get
            {
                Dbg.Assert(dirty, "Parse has not been called yet");
                return file;
            }
        }

        internal string ExecutionPolicy
        {
            get
            {
                Dbg.Assert(dirty, "Parse has not been called yet");
                return executionPolicy;
            }
        }

        internal bool ThrowOnReadAndPrompt
        {
            get
            {
                return noInteractive;
            }
        }

        internal bool NonInteractive
        {
            get { return noInteractive; }
        }

        private void ShowHelp()
        {
            ui.WriteLine("");
            if (this.helpText == null)
            {
                ui.WriteLine(CommandLineParameterParserStrings.DefaultHelp);
            }
            else
            {
                ui.Write(helpText);
            }
            ui.WriteLine("");
        }

        private void DisplayBanner()
        {
            // If banner text is not supplied do nothing.
            if (!String.IsNullOrEmpty(bannerText))
            {
                ui.WriteLine(bannerText);
                ui.WriteLine();
            }
        }

        internal bool StaMode
        {
            get
            {
                if (staMode.HasValue)
                {
                    return staMode.Value;
                }
                else
                {
#if CORECLR
                    // Nano doesn't support STA COM apartment, so on Nano powershell has to use MTA as the default.
                    return false;
#else
                    // Win8: 182409 PowerShell 3.0 should run in STA mode by default 
                    return true;
#endif
                }
            }
        }


        /// <summary>
        /// 
        /// Processes all the command line parameters to ConsoleHost.  Returns the exit code to be used to terminate the process, or
        /// Success to indicate that the program should continue running.
        /// 
        /// </summary>
        /// <param name="args">
        /// 
        /// The command line parameters to be processed.
        /// 
        /// </param>

        internal void Parse(string[] args)
        {
            Dbg.Assert(!dirty, "This instance has already been used. Create a new instance.");

            // indicates that we've called this method on this instance, and that when it's done, the state variables 
            // will reflect the parse.

            dirty = true;

            ParseHelper(args);

            // Check registry setting for a Group Policy ConfigurationName entry and 
            // use it to override anything set by the user.
            var configurationName = GetConfigurationNameFromGroupPolicy();
            if (!string.IsNullOrEmpty(configurationName))
            {
                this.configurationName = configurationName;
            }
        }

        private static string groupPolicyBase = @"Software\Policies\Microsoft\Windows\PowerShell";
        private static string ConsoleSessionConfigurationKey = "ConsoleSessionConfiguration";
        private static string EnableConsoleSessionConfiguration = "EnableConsoleSessionConfiguration";
        private static string ConsoleSessionConfigurationName = "ConsoleSessionConfigurationName";
        private static string GetConfigurationNameFromGroupPolicy()
        {
            // Current user policy takes precedence.
            var groupPolicySettings = Utils.GetGroupPolicySetting(groupPolicyBase, ConsoleSessionConfigurationKey, Utils.RegCurrentUserThenLocalMachine);
            if (groupPolicySettings != null)
            {
                object keyValue;
                if (groupPolicySettings.TryGetValue(EnableConsoleSessionConfiguration, out keyValue))
                {
                    if (String.Equals(keyValue.ToString(), "1", StringComparison.OrdinalIgnoreCase))
                    {
                        if (groupPolicySettings.TryGetValue(ConsoleSessionConfigurationName, out keyValue))
                        {
                            string consoleSessionConfigurationName = keyValue.ToString();
                            if (!string.IsNullOrEmpty(consoleSessionConfigurationName))
                            {
                                return consoleSessionConfigurationName;
                            }
                        }
                    }
                }
            }

            return string.Empty;
        }

        private void ParseHelper(string[] args)
        {
            Dbg.Assert(args != null, "Argument 'args' to ParseHelper should never be null");
            bool noexitSeen = false;

            for (int i = 0; i < args.Length; ++i)
            {
                // Invariant culture used because command-line parameters are not localized.

                string switchKey = args[i].Trim().ToLowerInvariant();
                if (String.IsNullOrEmpty(switchKey))
                {
                    continue;
                }

                if (!SpecialCharacters.IsDash(switchKey[0]) && switchKey[0] != '/')
                {
                    // then its a command

                    --i;
                    ParseCommand(args, ref i, noexitSeen, false);
                    break;
                }

                // chop off the first character so that we're agnostic wrt specifying / or -
                // in front of the switch name.

                switchKey = switchKey.Substring(1);

                // chop off the second dash so we're agnostic wrt specifying - or --
                if (SpecialCharacters.IsDash(switchKey[0]))
                {
                    switchKey = switchKey.Substring(1);
                }

                if (MatchSwitch(switchKey, "help", "h") || MatchSwitch(switchKey, "?", "?"))
                {
                    showHelp = true;
                    abortStartup = true;
                }
                else if (MatchSwitch(switchKey, "noexit", "noe"))
                {
                    noExit = true;
                    noexitSeen = true;
                }
                else if (MatchSwitch(switchKey, "importsystemmodules", "imp"))
                {
                    importSystemModules = true;
                }
                else if (MatchSwitch(switchKey, "noprofile", "nop"))
                {
                    skipUserInit = true;
                }
                else if (MatchSwitch(switchKey, "nologo", "nol"))
                {
                    showBanner = false;
                }
                else if (MatchSwitch(switchKey, "noninteractive", "noni"))
                {
                    noInteractive = true;
                }
                else if (MatchSwitch(switchKey, "socketservermode", "so"))
                {
                    socketServerMode = true;
                }
                else if (MatchSwitch(switchKey, "servermode", "s"))
                {
                    serverMode = true;
                }
                else if (MatchSwitch(switchKey, "namedpipeservermode", "nam"))
                {
                    namedPipeServerMode = true;
                }
                else if (MatchSwitch(switchKey, "configurationname", "config"))
                {
                    ++i;
                    if (i >= args.Length)
                    {
                        WriteCommandLineError(
                            CommandLineParameterParserStrings.MissingConfigurationNameArgument);
                        break;
                    }

                    configurationName = args[i];
                }
                else if (MatchSwitch(switchKey, "command", "c"))
                {
                    if (!ParseCommand(args, ref i, noexitSeen, false))
                    {
                        break;
                    }
                }
#if !CORECLR  // windowstyle parameter not supported on NanoServer because ProcessWindowStyle does Not exist on CoreCLR
                else if (MatchSwitch(switchKey, "windowstyle", "w"))
                {
                    ++i;
                    if (i >= args.Length)
                    {
                        WriteCommandLineError(
                            CommandLineParameterParserStrings.MissingWindowStyleArgument);
                        break;
                    }

                    try
                    {
                        ProcessWindowStyle style = (ProcessWindowStyle)LanguagePrimitives.ConvertTo(
                            args[i], typeof(ProcessWindowStyle), CultureInfo.InvariantCulture);
                        ConsoleControl.SetConsoleMode(style);

                    }
                    catch (PSInvalidCastException e)
                    {
                        WriteCommandLineError(
                            string.Format(CultureInfo.CurrentCulture, CommandLineParameterParserStrings.InvalidWindowStyleArgument, args[i], e.Message));
                        break;
                    }
                }
#endif
                else if (MatchSwitch(switchKey, "file", "f"))
                {
                    // Process file execution. We don't need to worry about checking -command
                    // since if -command comes before -file, -file will be treated as part
                    // of the script to evaluate. If -file comes before -command, it will
                    // treat -command as an argument to the script...

                    ++i;
                    if (i >= args.Length)
                    {
                        WriteCommandLineError(
                            CommandLineParameterParserStrings.MissingFileArgument,
                            showHelp: true,
                            showBanner: true);
                        break;
                    }

                    // Don't show the startup banner unless -noexit has been specified.
                    if (!noexitSeen)
                        showBanner = false;

                    // Process interactive input...
                    if (args[i] == "-")
                    {
                        // the arg to -file is -, which is secret code for "read the commands from stdin with prompts"

                        explicitReadCommandsFromStdin = true;
                        noPrompt = false;
                    }
                    else
                    {
                        // Exit on script completion unless -noexit was specified...
                        if (!noexitSeen)
                            noExit = false;

                        // We need to get the full path to the script because it will be
                        // executed after the profiles are run and they may change the current
                        // directory.
                        string exceptionMessage = null;
                        try
                        {
                            // Normalize slashes
                            file = args[i].Replace(StringLiterals.AlternatePathSeparator,
                                                   StringLiterals.DefaultPathSeparator);
                            file = Path.GetFullPath(file);
                        }
                        catch (Exception e)
                        {
                            // Catch all exceptions - we're just going to exit anyway so there's
                            // no issue of the system begin destablized. We'll still
                            // Watson on "severe" exceptions to get the reports.
                            ConsoleHost.CheckForSevereException(e);
                            exceptionMessage = e.Message;
                        }

                        if (exceptionMessage != null)
                        {
                            WriteCommandLineError(
                                string.Format(CultureInfo.CurrentCulture, CommandLineParameterParserStrings.InvalidFileArgument, args[i], exceptionMessage),
                                showBanner: true);
                            break;
                        }

                        if (!Path.GetExtension(file).Equals(".ps1", StringComparison.OrdinalIgnoreCase))
                        {
                            WriteCommandLineError(
                                string.Format(CultureInfo.CurrentCulture, CommandLineParameterParserStrings.InvalidFileArgumentExtension, args[i]),
                                showBanner: true);
                            break;
                        }

                        if (!System.IO.File.Exists(file))
                        {
                            WriteCommandLineError(
                                string.Format(CultureInfo.CurrentCulture, CommandLineParameterParserStrings.ArgumentFileDoesNotExist, args[i]),
                                showBanner: true);
                            break;
                        }

                        i++;

                        Regex argPattern = new Regex(@"^.\w+\:", RegexOptions.CultureInvariant);
                        string pendingParameter = null;

                        // Accumulate the arguments to this script...
                        while (i < args.Length)
                        {
                            string arg = args[i];

                            // If there was a pending parameter, add a named parameter
                            // using the pending parameter and current argument
                            if (pendingParameter != null)
                            {
                                collectedArgs.Add(new CommandParameter(pendingParameter, arg));
                                pendingParameter = null;
                            }
                            else if (!string.IsNullOrEmpty(arg) && SpecialCharacters.IsDash(arg[0]))
                            {
                                Match m = argPattern.Match(arg);
                                if (m.Success)
                                {
                                    int offset = arg.IndexOf(':');
                                    if (offset == arg.Length - 1)
                                    {
                                        pendingParameter = arg.TrimEnd(':');
                                    }
                                    else
                                    {
                                        collectedArgs.Add(new CommandParameter(arg.Substring(0, offset), arg.Substring(offset + 1)));
                                    }
                                }
                                else
                                {
                                    collectedArgs.Add(new CommandParameter(arg));
                                }
                            }
                            else
                            {
                                collectedArgs.Add(new CommandParameter(null, arg));
                            }
                            ++i;
                        }
                    }
                    break;
                }
#if DEBUG
                // this option is useful when debugging ConsoleHost remotely using VS remote debugging, as you can only 
                // attach to an already running process with that debugger.
                else if (MatchSwitch(switchKey, "wait", "w"))
                {
                    // This does not need to be localized: its chk only 

                    ui.WriteToConsole("Waiting - type enter to continue:", false);
                    ui.ReadLine();
                }

                // this option is useful for testing the initial InitialSessionState experience
                else if (MatchSwitch(switchKey, "iss", "iss"))
                {
                    // Just toss this option, it was processed earlier...
                }

                // this option is useful for testing the initial InitialSessionState experience
                // this is independent of the normal wait switch because configuration processing
                // happens so early in the cycle...
                else if (MatchSwitch(switchKey, "isswait", "isswait"))
                {
                    // Just toss this option, it was processed earlier...
                }

                else if (MatchSwitch(switchKey, "modules", "mod"))
                {
                    if (ConsoleHost.DefaultInitialSessionState == null)
                    {
                        WriteCommandLineError(
                            "The -module option can only be specified with the -iss option.",
                            showHelp: true,
                            showBanner: true);
                        break;
                    }

                    ++i;
                    int moduleCount = 0;
                    // Accumulate the arguments to this script...
                    while (i < args.Length)
                    {
                        string arg = args[i];

                        if (!string.IsNullOrEmpty(arg) && SpecialCharacters.IsDash(arg[0]))
                        {
                            break;
                        }
                        else
                        {
                            ConsoleHost.DefaultInitialSessionState.ImportPSModule(new string[] { arg });
                            moduleCount++;
                        }
                        ++i;
                    }
                    if (moduleCount < 1)
                    {
                        ui.WriteErrorLine("No modules specified for -module option");
                    }
                }
#endif
                else if (MatchSwitch(switchKey, "outputformat", "o") || MatchSwitch(switchKey, "of", "o"))
                {
                    ParseFormat(args, ref i, ref outFormat, CommandLineParameterParserStrings.MissingOutputFormatParameter);
                }
                else if (MatchSwitch(switchKey, "inputformat", "i") || MatchSwitch(switchKey, "if", "i"))
                {
                    ParseFormat(args, ref i, ref inFormat, CommandLineParameterParserStrings.MissingInputFormatParameter);
                }
                else if (MatchSwitch(switchKey, "executionpolicy", "ex") || MatchSwitch(switchKey, "ep", "ep"))
                {
                    ParseExecutionPolicy(args, ref i, ref executionPolicy, CommandLineParameterParserStrings.MissingExecutionPolicyParameter);
                }
                else if (MatchSwitch(switchKey, "encodedcommand", "e") || MatchSwitch(switchKey, "ec", "e"))
                {
                    wasCommandEncoded = true;
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
#if !CORECLR  // explicit setting of the ApartmentState Not supported on NanoServer
                else if (MatchSwitch(switchKey, "sta", "s"))
                {
                    if (staMode.HasValue)
                    {
                        // -sta and -mta are mutually exclusive.
                        WriteCommandLineError(
                            CommandLineParameterParserStrings.MtaStaMutuallyExclusive);
                        break;
                    }

                    staMode = true;
                }
                // Win8: 182409 PowerShell 3.0 should run in STA mode by default..so, consequently adding the switch -mta.
                // Not deleting -sta for backward compatability reasons
                else if (MatchSwitch(switchKey, "mta", "mta"))
                {
                    if (staMode.HasValue)
                    {
                        // -sta and -mta are mutually exclusive.
                        WriteCommandLineError(
                            CommandLineParameterParserStrings.MtaStaMutuallyExclusive);
                        break;
                    }

                    staMode = false;
                }
#endif
                else
                {
                    // The first parameter we fail to recognize marks the beginning of the command string.

                    --i;
                    if (!ParseCommand(args, ref i, noexitSeen, false))
                    {
                        break;
                    }
                }
            }

            if (showHelp)
            {
                ShowHelp();
            }
            
            if (showBanner && !showHelp)
            {
                DisplayBanner();
            }

            Dbg.Assert(
                    ((exitCode == ConsoleHost.ExitCodeBadCommandLineParameter) && abortStartup)
                || (exitCode == ConsoleHost.ExitCodeSuccess),
                "if exit code is failure, then abortstartup should be true");
        }

        private void WriteCommandLineError(string msg, bool showHelp = false, bool showBanner = false)
        {
            this.ui.WriteErrorLine(msg);
            this.showHelp = showHelp;
            this.showBanner = showBanner;
            this.abortStartup = true;
            this.exitCode = ConsoleHost.ExitCodeBadCommandLineParameter;
        }

        private bool MatchSwitch(string switchKey, string match, string smallestUnambiguousMatch)
        {
            Dbg.Assert(switchKey != null, "need a value");
            Dbg.Assert(!String.IsNullOrEmpty(match), "need a value");
            Dbg.Assert(match.Trim().ToLowerInvariant() == match, "match should be normalized to lowercase w/ no outside whitespace");
            Dbg.Assert(smallestUnambiguousMatch.Trim().ToLowerInvariant() == smallestUnambiguousMatch, "match should be normalized to lowercase w/ no outside whitespace");
            Dbg.Assert(match.Contains(smallestUnambiguousMatch), "sUM should be a substring of match");

            if (match.Trim().ToLowerInvariant().IndexOf(switchKey, StringComparison.Ordinal) == 0)
            {
                if (switchKey.Length >= smallestUnambiguousMatch.Length)
                {
                    return true;
                }
            }

            return false;
        }

        private void ParseFormat(string[] args, ref int i, ref Serialization.DataFormat format, string resourceStr)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in Enum.GetNames(typeof(Serialization.DataFormat)))
            {
                sb.Append(s);
                sb.Append(ConsoleHostUserInterface.Crlf);
            }

            ++i;
            if (i >= args.Length)
            {
                ui.WriteErrorLine(
                    StringUtil.Format(
                        resourceStr,
                        sb.ToString()));
                showHelp = true;
                abortStartup = true;
                exitCode = ConsoleHost.ExitCodeBadCommandLineParameter;
                return;
            }

            try
            {
                format = (Serialization.DataFormat)Enum.Parse(typeof(Serialization.DataFormat), args[i], true);
            }
            catch (ArgumentException)
            {
                ui.WriteErrorLine(
                    StringUtil.Format(
                        CommandLineParameterParserStrings.BadFormatParameterValue,
                        args[i],
                        sb.ToString()));
                showHelp = true;
                abortStartup = true;
                exitCode = ConsoleHost.ExitCodeBadCommandLineParameter;
            }
        }

        private void ParseExecutionPolicy(string[] args, ref int i, ref string executionPolicy, string resourceStr)
        {
            ++i;
            if (i >= args.Length)
            {
                ui.WriteErrorLine(resourceStr);

                showHelp = true;
                abortStartup = true;
                exitCode = ConsoleHost.ExitCodeBadCommandLineParameter;
                return;
            }

            executionPolicy = args[i];
        }

        private bool ParseCommand(string[] args, ref int i, bool noexitSeen, bool isEncoded)
        {
            if (commandLineCommand != null)
            {
                // we've already set the command, so squawk

                ui.WriteErrorLine(CommandLineParameterParserStrings.CommandAlreadySpecified);
                showHelp = true;
                abortStartup = true;
                exitCode = ConsoleHost.ExitCodeBadCommandLineParameter;
                return false;
            }

            ++i;
            if (i >= args.Length)
            {
                ui.WriteErrorLine(CommandLineParameterParserStrings.MissingCommandParameter);
                showHelp = true;
                abortStartup = true;
                exitCode = ConsoleHost.ExitCodeBadCommandLineParameter;
                return false;
            }

            if (isEncoded)
            {
                try
                {
                    commandLineCommand = StringToBase64Converter.Base64ToString(args[i]);
                }
                // decoding failed
                catch
                {
                    ui.WriteErrorLine(CommandLineParameterParserStrings.BadCommandValue);
                    showHelp = true;
                    abortStartup = true;
                    exitCode = ConsoleHost.ExitCodeBadCommandLineParameter;
                    return false;
                }

            }
            else if (args[i] == "-")
            {
                // the arg to -command is -, which is secret code for "read the commands from stdin with no prompts"

                explicitReadCommandsFromStdin = true;
                noPrompt = true;

                ++i;
                if (i != args.Length)
                {
                    // there are more parameters to -command than -, which is an error.

                    ui.WriteErrorLine(CommandLineParameterParserStrings.TooManyParametersToCommand);
                    showHelp = true;
                    abortStartup = true;
                    exitCode = ConsoleHost.ExitCodeBadCommandLineParameter;
                    return false;
                }

                if (!Console.IsInputRedirected)
                {
                    ui.WriteErrorLine(CommandLineParameterParserStrings.StdinNotRedirected);
                    showHelp = true;
                    abortStartup = true;
                    exitCode = ConsoleHost.ExitCodeBadCommandLineParameter;
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
                commandLineCommand = cmdLineCmdSB.ToString();
            }

            if (!noexitSeen && !explicitReadCommandsFromStdin)
            {
                // don't reset this if they've already specified -noexit
                noExit = false;
            }

            showBanner = false;

            return true;
        }

        private bool CollectArgs(string[] args, ref int i)
        {

            if (collectedArgs.Count != 0)
            {
                ui.WriteErrorLine(CommandLineParameterParserStrings.ArgsAlreadySpecified);
                showHelp = true;
                abortStartup = true;
                exitCode = ConsoleHost.ExitCodeBadCommandLineParameter;
                return false;
            }

            ++i;
            if (i >= args.Length)
            {
                ui.WriteErrorLine(CommandLineParameterParserStrings.MissingArgsValue);
                showHelp = true;
                abortStartup = true;
                exitCode = ConsoleHost.ExitCodeBadCommandLineParameter;
                return false;
            }

            try
            {
                object[] a = StringToBase64Converter.Base64ToArgsConverter(args[i]);
                if (a != null)
                {
                    foreach (object obj in a)
                    {
                        collectedArgs.Add(new CommandParameter(null, obj));
                    }
                }
            }
            catch
            {
                // decoding failed

                ui.WriteErrorLine(CommandLineParameterParserStrings.BadArgsValue);
                showHelp = true;
                abortStartup = true;
                exitCode = ConsoleHost.ExitCodeBadCommandLineParameter;
                return false;
            }

            return true;
        }

        private bool socketServerMode;
        private bool serverMode;
        private bool namedPipeServerMode;
        private string configurationName;
        private ConsoleHost parent;
        private ConsoleHostUserInterface ui;
        private bool showHelp;
        private bool showBanner = true;
        private bool noInteractive;
        private string bannerText;
        private string helpText;
        private bool abortStartup;
        private bool skipUserInit;
        // Win8: 182409 PowerShell 3.0 should run in STA mode by default
        // -sta and -mta are mutually exclusive..so tracking them using nullable boolean
        // if true, then sta is specified on the command line.
        // if false, then mta is specified on the command line.
        // if null, then none is specified on the command line..use default in this case
        // default is sta.
        private bool? staMode = null;
        private bool noExit = true;
        private bool explicitReadCommandsFromStdin;
        private bool noPrompt;
        private string commandLineCommand;
        private bool wasCommandEncoded;
        private uint exitCode = ConsoleHost.ExitCodeSuccess;
        private bool dirty;
        private Version ver;
        private Serialization.DataFormat outFormat = Serialization.DataFormat.Text;
        private Serialization.DataFormat inFormat = Serialization.DataFormat.Text;
        private Collection<CommandParameter> collectedArgs = new Collection<CommandParameter>();
        private string file;
        private string executionPolicy;
        private bool importSystemModules = false;
    }

}   // namespace

