// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace System.Management.Automation
{
    internal enum SuggestionMatchType
    {
        /// <summary>Match on a command.</summary>
        Command = 0,
        /// <summary>Match based on exception message.</summary>
        Error = 1,
        /// <summary>Match by running a script block.</summary>
        Dynamic = 2,

        /// <summary>Match by fully qualified ErrorId.</summary>
        ErrorId = 3
    }

    #region Public HostUtilities Class

    /// <summary>
    /// Implements utility methods that might be used by Hosts.
    /// </summary>
    public static class HostUtilities
    {
        #region Internal Access

        private static readonly string s_checkForCommandInCurrentDirectoryScript = @"
            [System.Diagnostics.DebuggerHidden()]
            param()

            $foundSuggestion = $false

            if($lastError -and
                ($lastError.Exception -is ""System.Management.Automation.CommandNotFoundException""))
            {
                $escapedCommand = [System.Management.Automation.WildcardPattern]::Escape($lastError.TargetObject)
                $foundSuggestion = @(Get-Command ($ExecutionContext.SessionState.Path.Combine(""."", $escapedCommand)) -ErrorAction Ignore).Count -gt 0
            }

            $foundSuggestion
        ";

        private static readonly string s_createCommandExistsInCurrentDirectoryScript = @"
            [System.Diagnostics.DebuggerHidden()]
            param([string] $formatString)

            $formatString -f $lastError.TargetObject,"".\$($lastError.TargetObject)""
        ";

        private static readonly string s_getFuzzyMatchedCommands = @"
            [System.Diagnostics.DebuggerHidden()]
            param([string] $formatString)

            $formatString -f [string]::Join(', ', (Get-Command $lastError.TargetObject -UseFuzzyMatching -FuzzyMinimumDistance 1 | Select-Object -First 5 -Unique -ExpandProperty Name))
        ";

        private static readonly List<Hashtable> s_suggestions = InitializeSuggestions();

        private static List<Hashtable> InitializeSuggestions()
        {
            var suggestions = new List<Hashtable>(
                new Hashtable[]
                {
                    NewSuggestion(
                        id: 3,
                        category: "General",
                        matchType: SuggestionMatchType.Dynamic,
                        rule: ScriptBlock.CreateDelayParsedScriptBlock(s_checkForCommandInCurrentDirectoryScript, isProductCode: true),
                        suggestion: ScriptBlock.CreateDelayParsedScriptBlock(s_createCommandExistsInCurrentDirectoryScript, isProductCode: true),
                        suggestionArgs: new object[] { CodeGeneration.EscapeSingleQuotedStringContent(SuggestionStrings.Suggestion_CommandExistsInCurrentDirectory) },
                        enabled: true)
                });

            if (ExperimentalFeature.IsEnabled("PSCommandNotFoundSuggestion"))
            {
                suggestions.Add(
                    NewSuggestion(
                        id: 4,
                        category: "General",
                        matchType: SuggestionMatchType.ErrorId,
                        rule: "CommandNotFoundException",
                        suggestion: ScriptBlock.CreateDelayParsedScriptBlock(s_getFuzzyMatchedCommands, isProductCode: true),
                        suggestionArgs: new object[] { CodeGeneration.EscapeSingleQuotedStringContent(SuggestionStrings.Suggestion_CommandNotFound) },
                        enabled: true));
            }

            return suggestions;
        }

        #region GetProfileCommands
        /// <summary>
        /// Gets a PSObject whose base object is currentUserCurrentHost and with notes for the other 4 parameters.
        /// </summary>
        /// <param name="allUsersAllHosts">The profile file name for all users and all hosts.</param>
        /// <param name="allUsersCurrentHost">The profile file name for all users and current host.</param>
        /// <param name="currentUserAllHosts">The profile file name for current user and all hosts.</param>
        /// <param name="currentUserCurrentHost">The profile name for current user and current host.</param>
        /// <returns>A PSObject whose base object is currentUserCurrentHost and with notes for the other 4 parameters.</returns>
        internal static PSObject GetDollarProfile(string allUsersAllHosts, string allUsersCurrentHost, string currentUserAllHosts, string currentUserCurrentHost)
        {
            PSObject returnValue = new PSObject(currentUserCurrentHost);
            returnValue.Properties.Add(new PSNoteProperty("AllUsersAllHosts", allUsersAllHosts));
            returnValue.Properties.Add(new PSNoteProperty("AllUsersCurrentHost", allUsersCurrentHost));
            returnValue.Properties.Add(new PSNoteProperty("CurrentUserAllHosts", currentUserAllHosts));
            returnValue.Properties.Add(new PSNoteProperty("CurrentUserCurrentHost", currentUserCurrentHost));
            return returnValue;
        }

        /// <summary>
        /// Gets the object that serves as a value to $profile and the paths on it.
        /// </summary>
        /// <param name="shellId">The id identifying the host or shell used in profile file names.</param>
        /// <param name="useTestProfile">Used from test not to overwrite the profile file names from development boxes.</param>
        /// <param name="allUsersAllHosts">Path for all users and all hosts.</param>
        /// <param name="currentUserAllHosts">Path for current user and all hosts.</param>
        /// <param name="allUsersCurrentHost">Path for all users current host.</param>
        /// <param name="currentUserCurrentHost">Path for current user and current host.</param>
        /// <param name="dollarProfile">The object that serves as a value to $profile.</param>
        /// <returns></returns>
        internal static void GetProfileObjectData(string shellId, bool useTestProfile, out string allUsersAllHosts, out string allUsersCurrentHost, out string currentUserAllHosts, out string currentUserCurrentHost, out PSObject dollarProfile)
        {
            allUsersAllHosts = HostUtilities.GetFullProfileFileName(null, false, useTestProfile);
            allUsersCurrentHost = HostUtilities.GetFullProfileFileName(shellId, false, useTestProfile);
            currentUserAllHosts = HostUtilities.GetFullProfileFileName(null, true, useTestProfile);
            currentUserCurrentHost = HostUtilities.GetFullProfileFileName(shellId, true, useTestProfile);
            dollarProfile = HostUtilities.GetDollarProfile(allUsersAllHosts, allUsersCurrentHost, currentUserAllHosts, currentUserCurrentHost);
        }

        /// <summary>
        /// Gets an array of commands that can be run sequentially to set $profile and run the profile commands.
        /// </summary>
        /// <param name="shellId">The id identifying the host or shell used in profile file names.</param>
        /// <param name="useTestProfile">Used from test not to overwrite the profile file names from development boxes.</param>
        /// <returns></returns>
        internal static PSCommand[] GetProfileCommands(string shellId, bool useTestProfile)
        {
            List<PSCommand> commands = new List<PSCommand>();
            string allUsersAllHosts, allUsersCurrentHost, currentUserAllHosts, currentUserCurrentHost;
            PSObject dollarProfile;
            HostUtilities.GetProfileObjectData(shellId, useTestProfile, out allUsersAllHosts, out allUsersCurrentHost, out currentUserAllHosts, out currentUserCurrentHost, out dollarProfile);

            PSCommand command = new PSCommand();
            command.AddCommand("set-variable");
            command.AddParameter("Name", "profile");
            command.AddParameter("Value", dollarProfile);
            command.AddParameter("Option", ScopedItemOptions.None);
            commands.Add(command);

            string[] profilePaths = new string[] { allUsersAllHosts, allUsersCurrentHost, currentUserAllHosts, currentUserCurrentHost };
            foreach (string profilePath in profilePaths)
            {
                if (!System.IO.File.Exists(profilePath))
                {
                    continue;
                }

                command = new PSCommand();
                command.AddCommand(profilePath, false);
                commands.Add(command);
            }

            return commands.ToArray();
        }

        /// <summary>
        /// Used to get all profile file names for the current or all hosts and for the current or all users.
        /// </summary>
        /// <param name="shellId">Null for all hosts, not null for the specified host.</param>
        /// <param name="forCurrentUser">False for all users, true for the current user.</param>
        /// <returns>The profile file name matching the parameters.</returns>
        internal static string GetFullProfileFileName(string shellId, bool forCurrentUser)
        {
            return HostUtilities.GetFullProfileFileName(shellId, forCurrentUser, false);
        }

        /// <summary>
        /// Used to get all profile file names for the current or all hosts and for the current or all users.
        /// </summary>
        /// <param name="shellId">Null for all hosts, not null for the specified host.</param>
        /// <param name="forCurrentUser">False for all users, true for the current user.</param>
        /// <param name="useTestProfile">Used from test not to overwrite the profile file names from development boxes.</param>
        /// <returns>The profile file name matching the parameters.</returns>
        internal static string GetFullProfileFileName(string shellId, bool forCurrentUser, bool useTestProfile)
        {
            string basePath = null;

            if (forCurrentUser)
            {
                basePath = Platform.ConfigDirectory;
            }
            else
            {
                basePath = GetAllUsersFolderPath(shellId);
                if (string.IsNullOrEmpty(basePath))
                {
                    return string.Empty;
                }
            }

            string profileName = useTestProfile ? "profile_test.ps1" : "profile.ps1";

            if (!string.IsNullOrEmpty(shellId))
            {
                profileName = shellId + "_" + profileName;
            }

            string fullPath = basePath = IO.Path.Combine(basePath, profileName);

            return fullPath;
        }

        /// <summary>
        /// Used internally in GetFullProfileFileName to get the base path for all users profiles.
        /// </summary>
        /// <param name="shellId">The shellId to use.</param>
        /// <returns>The base path for all users profiles.</returns>
        private static string GetAllUsersFolderPath(string shellId)
        {
            string folderPath = string.Empty;
            try
            {
                folderPath = Utils.GetApplicationBase(shellId);
            }
            catch (System.Security.SecurityException)
            {
            }

            return folderPath;
        }
        #endregion GetProfileCommands

        /// <summary>
        /// Gets the first <paramref name="maxLines"/> lines of <paramref name="source"/>.
        /// </summary>
        /// <param name="source">String we want to limit the number of lines.</param>
        /// <param name="maxLines">Maximum number of lines to be returned.</param>
        /// <returns>The first lines of <paramref name="source"/>.</returns>
        internal static string GetMaxLines(string source, int maxLines)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            StringBuilder returnValue = new StringBuilder();

            for (int i = 0, lineCount = 1; i < source.Length; i++)
            {
                char c = source[i];

                if (c == '\n')
                {
                    lineCount++;
                }

                returnValue.Append(c);

                if (lineCount == maxLines)
                {
                    returnValue.Append(PSObjectHelper.Ellipsis);
                    break;
                }
            }

            return returnValue.ToString();
        }

        internal static List<string> GetSuggestion(Runspace runspace)
        {
            if (!(runspace is LocalRunspace localRunspace)) { return new List<string>(); }

            // Get the last value of $?
            bool questionMarkVariableValue = localRunspace.ExecutionContext.QuestionMarkVariableValue;

            // Get the last history item
            History history = localRunspace.History;
            HistoryInfo[] entries = history.GetEntries(-1, 1, true);

            if (entries.Length == 0)
                return new List<string>();

            HistoryInfo lastHistory = entries[0];

            // Get the last error
            ArrayList errorList = (ArrayList)localRunspace.GetExecutionContext.DollarErrorVariable;
            object lastError = null;

            if (errorList.Count > 0)
            {
                lastError = errorList[0] as Exception;
                ErrorRecord lastErrorRecord = null;

                // The error was an actual ErrorRecord
                if (lastError == null)
                {
                    lastErrorRecord = errorList[0] as ErrorRecord;
                }
                else if (lastError is RuntimeException)
                {
                    lastErrorRecord = ((RuntimeException)lastError).ErrorRecord;
                }

                // If we got information about the error invocation,
                // we can be more careful with the errors we pass along
                if ((lastErrorRecord != null) && (lastErrorRecord.InvocationInfo != null))
                {
                    if (lastErrorRecord.InvocationInfo.HistoryId == lastHistory.Id)
                        lastError = lastErrorRecord;
                    else
                        lastError = null;
                }
            }

            Runspace oldDefault = null;
            bool changedDefault = false;
            if (Runspace.DefaultRunspace != runspace)
            {
                oldDefault = Runspace.DefaultRunspace;
                changedDefault = true;
                Runspace.DefaultRunspace = runspace;
            }

            List<string> suggestions = null;

            try
            {
                suggestions = GetSuggestion(lastHistory, lastError, errorList);
            }
            finally
            {
                if (changedDefault)
                {
                    Runspace.DefaultRunspace = oldDefault;
                }
            }

            // Restore $?
            localRunspace.ExecutionContext.QuestionMarkVariableValue = questionMarkVariableValue;
            return suggestions;
        }

        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        internal static List<string> GetSuggestion(HistoryInfo lastHistory, object lastError, ArrayList errorList)
        {
            var returnSuggestions = new List<string>();

            PSModuleInfo invocationModule = new PSModuleInfo(true);
            invocationModule.SessionState.PSVariable.Set("lastHistory", lastHistory);
            invocationModule.SessionState.PSVariable.Set("lastError", lastError);

            int initialErrorCount = 0;

            // Go through all of the suggestions
            foreach (Hashtable suggestion in s_suggestions)
            {
                initialErrorCount = errorList.Count;

                // Make sure the rule is enabled
                if (!LanguagePrimitives.IsTrue(suggestion["Enabled"]))
                    continue;

                SuggestionMatchType matchType = (SuggestionMatchType)LanguagePrimitives.ConvertTo(
                    suggestion["MatchType"],
                    typeof(SuggestionMatchType),
                    CultureInfo.InvariantCulture);

                // If this is a dynamic match, evaluate the ScriptBlock
                if (matchType == SuggestionMatchType.Dynamic)
                {
                    object result = null;

                    ScriptBlock evaluator = suggestion["Rule"] as ScriptBlock;
                    if (evaluator == null)
                    {
                        suggestion["Enabled"] = false;

                        throw new ArgumentException(
                            SuggestionStrings.RuleMustBeScriptBlock, "Rule");
                    }

                    try
                    {
                        result = invocationModule.Invoke(evaluator, null);
                    }
                    catch (Exception)
                    {
                        // Catch-all OK. This is a third-party call-out.
                        suggestion["Enabled"] = false;
                        continue;
                    }

                    // If it returned results, evaluate its suggestion
                    if (LanguagePrimitives.IsTrue(result))
                    {
                        string suggestionText = GetSuggestionText(suggestion["Suggestion"], (object[])suggestion["SuggestionArgs"], invocationModule);

                        if (!string.IsNullOrEmpty(suggestionText))
                        {
                            string returnString = string.Format(
                                CultureInfo.CurrentCulture,
                                "Suggestion [{0},{1}]: {2}",
                                (int)suggestion["Id"],
                                (string)suggestion["Category"],
                                suggestionText);

                            returnSuggestions.Add(returnString);
                        }
                    }
                }
                else
                {
                    string matchText = string.Empty;

                    // Otherwise, this is a Regex match against the
                    // command or error
                    if (matchType == SuggestionMatchType.Command)
                    {
                        matchText = lastHistory.CommandLine;
                    }
                    else if (matchType == SuggestionMatchType.Error)
                    {
                        if (lastError != null)
                        {
                            Exception lastException = lastError as Exception;
                            if (lastException != null)
                            {
                                matchText = lastException.Message;
                            }
                            else
                            {
                                matchText = lastError.ToString();
                            }
                        }
                    }
                    else if (matchType == SuggestionMatchType.ErrorId)
                    {
                        if (lastError != null && lastError is ErrorRecord errorRecord)
                        {
                            matchText = errorRecord.FullyQualifiedErrorId;
                        }
                    }
                    else
                    {
                        suggestion["Enabled"] = false;

                        throw new ArgumentException(
                            SuggestionStrings.InvalidMatchType,
                            "MatchType");
                    }

                    // If the text matches, evaluate the suggestion
                    if (Regex.IsMatch(matchText, (string)suggestion["Rule"], RegexOptions.IgnoreCase))
                    {
                        string suggestionText = GetSuggestionText(suggestion["Suggestion"], (object[])suggestion["SuggestionArgs"], invocationModule);

                        if (!string.IsNullOrEmpty(suggestionText))
                        {
                            string returnString = string.Format(
                                CultureInfo.CurrentCulture,
                                "Suggestion [{0},{1}]: {2}",
                                (int)suggestion["Id"],
                                (string)suggestion["Category"],
                                suggestionText);

                            returnSuggestions.Add(returnString);
                        }
                    }
                }

                // If the rule generated an error, disable it
                if (errorList.Count != initialErrorCount)
                {
                    suggestion["Enabled"] = false;
                }
            }

            return returnSuggestions;
        }

        /// <summary>
        /// Remove the GUID from the message if the message is in the pre-defined format.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="matchPattern"></param>
        /// <returns></returns>
        internal static string RemoveGuidFromMessage(string message, out bool matchPattern)
        {
            matchPattern = false;
            if (string.IsNullOrEmpty(message))
                return message;

            const string pattern = @"^([\d\w]{8}\-[\d\w]{4}\-[\d\w]{4}\-[\d\w]{4}\-[\d\w]{12}:).*";
            Match matchResult = Regex.Match(message, pattern);
            if (matchResult.Success)
            {
                string partToRemove = matchResult.Groups[1].Captures[0].Value;
                message = message.Remove(0, partToRemove.Length);
                matchPattern = true;
            }

            return message;
        }

        internal static string RemoveIdentifierInfoFromMessage(string message, out bool matchPattern)
        {
            matchPattern = false;
            if (string.IsNullOrEmpty(message))
                return message;

            const string pattern = @"^([\d\w]{8}\-[\d\w]{4}\-[\d\w]{4}\-[\d\w]{4}\-[\d\w]{12}:\[.*\]:).*";
            Match matchResult = Regex.Match(message, pattern);
            if (matchResult.Success)
            {
                string partToRemove = matchResult.Groups[1].Captures[0].Value;
                message = message.Remove(0, partToRemove.Length);
                matchPattern = true;
            }

            return message;
        }

        /// <summary>
        /// Create suggestion with string rule and scriptblock suggestion.
        /// </summary>
        /// <param name="id">Identifier for the suggestion.</param>
        /// <param name="category">Category for the suggestion.</param>
        /// <param name="matchType">Suggestion match type.</param>
        /// <param name="rule">Rule to match.</param>
        /// <param name="suggestion">Scriptblock to run that returns the suggestion.</param>
        /// <param name="suggestionArgs">Arguments to pass to suggestion scriptblock.</param>
        /// <param name="enabled">True if the suggestion is enabled.</param>
        /// <returns>Hashtable representing the suggestion.</returns>
        private static Hashtable NewSuggestion(int id, string category, SuggestionMatchType matchType, string rule, ScriptBlock suggestion, object[] suggestionArgs, bool enabled)
        {
            Hashtable result = new Hashtable(StringComparer.CurrentCultureIgnoreCase);

            result["Id"] = id;
            result["Category"] = category;
            result["MatchType"] = matchType;
            result["Rule"] = rule;
            result["Suggestion"] = suggestion;
            result["SuggestionArgs"] = suggestionArgs;
            result["Enabled"] = enabled;

            return result;
        }

        /// <summary>
        /// Create suggestion with scriptblock rule and suggestion.
        /// </summary>
        private static Hashtable NewSuggestion(int id, string category, SuggestionMatchType matchType, ScriptBlock rule, ScriptBlock suggestion, bool enabled)
        {
            Hashtable result = new Hashtable(StringComparer.CurrentCultureIgnoreCase);

            result["Id"] = id;
            result["Category"] = category;
            result["MatchType"] = matchType;
            result["Rule"] = rule;
            result["Suggestion"] = suggestion;
            result["Enabled"] = enabled;

            return result;
        }

        /// <summary>
        /// Create suggestion with scriptblock rule and scriptblock suggestion with arguments.
        /// </summary>
        private static Hashtable NewSuggestion(int id, string category, SuggestionMatchType matchType, ScriptBlock rule, ScriptBlock suggestion, object[] suggestionArgs, bool enabled)
        {
            Hashtable result = NewSuggestion(id, category, matchType, rule, suggestion, enabled);
            result.Add("SuggestionArgs", suggestionArgs);

            return result;
        }

        /// <summary>
        /// Get suggestion text from suggestion scriptblock with arguments.
        /// </summary>
        private static string GetSuggestionText(object suggestion, object[] suggestionArgs, PSModuleInfo invocationModule)
        {
            if (suggestion is ScriptBlock)
            {
                ScriptBlock suggestionScript = (ScriptBlock)suggestion;

                object result = null;
                try
                {
                    result = invocationModule.Invoke(suggestionScript, suggestionArgs);
                }
                catch (Exception)
                {
                    // Catch-all OK. This is a third-party call-out.
                    return string.Empty;
                }

                return (string)LanguagePrimitives.ConvertTo(result, typeof(string), CultureInfo.CurrentCulture);
            }
            else
            {
                return (string)LanguagePrimitives.ConvertTo(suggestion, typeof(string), CultureInfo.CurrentCulture);
            }
        }

        /// <summary>
        /// Returns the prompt used in remote sessions: "[machine]: basePrompt"
        /// </summary>
        internal static string GetRemotePrompt(RemoteRunspace runspace, string basePrompt, bool configuredSession = false)
        {
            if (configuredSession ||
                runspace.ConnectionInfo is NamedPipeConnectionInfo ||
                runspace.ConnectionInfo is VMConnectionInfo ||
                runspace.ConnectionInfo is ContainerConnectionInfo)
            {
                return basePrompt;
            }

            SSHConnectionInfo sshConnectionInfo = runspace.ConnectionInfo as SSHConnectionInfo;

            // Usernames are case-sensitive on Unix systems
            if (sshConnectionInfo != null &&
                !string.IsNullOrEmpty(sshConnectionInfo.UserName) &&
                !System.Environment.UserName.Equals(sshConnectionInfo.UserName, StringComparison.Ordinal))
            {
                return string.Create(CultureInfo.InvariantCulture, $"[{sshConnectionInfo.UserName}@{sshConnectionInfo.ComputerName}]: {basePrompt}");
            }

            return string.Create(CultureInfo.InvariantCulture, $"[{runspace.ConnectionInfo.ComputerName}]: {basePrompt}");
        }

        /// <summary>
        /// Create a configured remote runspace from provided name.
        /// </summary>
        /// <param name="configurationName"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        internal static RemoteRunspace CreateConfiguredRunspace(
            string configurationName,
            PSHost host)
        {
            // Create a loop-back remote runspace with network access enabled, and
            // with the provided endpoint configurationname.
            TypeTable typeTable = TypeTable.LoadDefaultTypeFiles();
            var connectInfo = new WSManConnectionInfo();
            connectInfo.ShellUri = configurationName.Trim();
            connectInfo.EnableNetworkAccess = true;

            RemoteRunspace remoteRunspace = null;
            try
            {
                remoteRunspace = (RemoteRunspace)RunspaceFactory.CreateRunspace(connectInfo, host, typeTable);
                remoteRunspace.Open();
            }
            catch (Exception e)
            {
                throw new PSInvalidOperationException(
                    StringUtil.Format(RemotingErrorIdStrings.CannotCreateConfiguredRunspace, configurationName),
                    e);
            }

            remoteRunspace.IsConfiguredLoopBack = true;
            return remoteRunspace;
        }

        #endregion

        #region Public Access

        #region Runspace Invoke

        /// <summary>
        /// Helper method to invoke a PSCommand on a given runspace.  This method correctly invokes the command for
        /// these runspace cases:
        ///   1. Local runspace.  If the local runspace is busy it will invoke as a nested command.
        ///   2. Remote runspace.
        ///   3. Runspace that is stopped in the debugger at a breakpoint.
        ///
        /// Error and information streams are ignored and only the command result output is returned.
        ///
        /// This method is NOT thread safe.  It does not support running commands from different threads on the
        /// provided runspace.  It assumes the thread invoking this method is the same that runs all other
        /// commands on the provided runspace.
        /// </summary>
        /// <param name="runspace">Runspace to invoke the command on.</param>
        /// <param name="command">Command to invoke.</param>
        /// <returns>Collection of command output result objects.</returns>
        public static Collection<PSObject> InvokeOnRunspace(PSCommand command, Runspace runspace)
        {
            if (command == null)
            {
                throw new PSArgumentNullException(nameof(command));
            }

            if (runspace == null)
            {
                throw new PSArgumentNullException(nameof(runspace));
            }

            if ((runspace.Debugger != null) && runspace.Debugger.InBreakpoint)
            {
                // Use the Debugger API to run the command when a runspace is stopped in the debugger.
                PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();
                runspace.Debugger.ProcessCommand(
                    command,
                    output);

                return new Collection<PSObject>(output);
            }

            // Otherwise run command directly in runspace.
            PowerShell ps = PowerShell.Create();
            ps.Runspace = runspace;
            ps.IsRunspaceOwner = false;
            if (runspace.ConnectionInfo == null)
            {
                // Local runspace.  Make a nested PowerShell object as needed.
                ps.SetIsNested(runspace.GetCurrentlyRunningPipeline() != null);
            }

            using (ps)
            {
                ps.Commands = command;
                return ps.Invoke<PSObject>();
            }
        }

        #endregion

        #region PSEdit Support

        /// <summary>
        /// PSEditFunction script string.
        /// </summary>
        public const string PSEditFunction = @"
            param (
                [Parameter(Mandatory=$true)] [string[]] $FileName
            )

            foreach ($file in $FileName)
            {
                Get-ChildItem $file -File | ForEach-Object {
                    $filePathName = $_.FullName

                    # Get file contents
                    $contentBytes = Get-Content -Path $filePathName -Raw -Encoding Byte

                    # Notify client for file open.
                    New-Event -SourceIdentifier PSISERemoteSessionOpenFile -EventArguments @($filePathName, $contentBytes) > $null
                }
            }
        ";

        /// <summary>
        /// CreatePSEditFunction script string.
        /// </summary>
        public const string CreatePSEditFunction = @"
            param (
                [string] $PSEditFunction
            )

            Register-EngineEvent -SourceIdentifier PSISERemoteSessionOpenFile -Forward -SupportEvent

            if ((Test-Path -Path 'function:\global:PSEdit') -eq $false)
            {
                Set-Item -Path 'function:\global:PSEdit' -Value $PSEditFunction
            }
        ";

        /// <summary>
        /// RemovePSEditFunction script string.
        /// </summary>
        public const string RemovePSEditFunction = @"
            if ((Test-Path -Path 'function:\global:PSEdit') -eq $true)
            {
                Remove-Item -Path 'function:\global:PSEdit' -Force
            }

            Unregister-Event -SourceIdentifier PSISERemoteSessionOpenFile -Force -ErrorAction Ignore
        ";

        /// <summary>
        /// Open file event.
        /// </summary>
        public const string RemoteSessionOpenFileEvent = "PSISERemoteSessionOpenFile";

        #endregion

        #endregion
    }

    #endregion
}
