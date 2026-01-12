// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Subsystem.Feedback;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace System.Management.Automation
{
    #region Public HostUtilities Class

    /// <summary>
    /// Implements utility methods that might be used by Hosts.
    /// </summary>
    public static class HostUtilities
    {
        #region Internal Access

        private static readonly char s_actionIndicator = HostSupportUnicode() ? '\u27a4' : '>';

        private static bool HostSupportUnicode()
        {
            // Reference: https://github.com/zkat/supports-unicode/blob/main/src/lib.rs
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.GetEnvironmentVariable("WT_SESSION") is not null ||
                    Environment.GetEnvironmentVariable("TERM_PROGRAM") is "vscode" ||
                    Environment.GetEnvironmentVariable("ConEmuTask") is "{cmd:Cmder}" ||
                    Environment.GetEnvironmentVariable("TERM") is "xterm-256color" or "alacritty";
            }

            string ctype = Environment.GetEnvironmentVariable("LC_ALL") ??
                Environment.GetEnvironmentVariable("LC_CTYPE") ??
                Environment.GetEnvironmentVariable("LANG") ??
                string.Empty;

            return ctype.EndsWith("UTF8") || ctype.EndsWith("UTF-8");
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
            }

            if (string.IsNullOrEmpty(basePath))
            {
                return string.Empty;
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
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "[{0}@{1}]: {2}",
                    sshConnectionInfo.UserName,
                    sshConnectionInfo.ComputerName,
                    basePrompt);
            }
            if(runspace.ConnectionInfo.HideRemoteComputerName != null)
            {
                return string.Format(
                CultureInfo.InvariantCulture,
                "{0}",
                basePrompt);
            }
            else
            {
                return string.Format(
                CultureInfo.InvariantCulture,
                "[{0}]: {1}",
                runspace.ConnectionInfo.ComputerName,
                basePrompt);
            }
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

        #region Feedback Rendering

        /// <summary>
        /// Render the feedbacks to the specified host.
        /// </summary>
        /// <param name="feedbacks">The feedback results.</param>
        /// <param name="ui">The host to render to.</param>
        public static void RenderFeedback(List<FeedbackResult> feedbacks, PSHostUserInterface ui)
        {
            // Caption style is dimmed bright white with italic effect, used for fixed captions, such as '[' and ']'.
            string captionStyle = "\x1b[97;2;3m";
            string italics = "\x1b[3m";
            string nameStyle = PSStyle.Instance.Formatting.FeedbackName;
            string textStyle = PSStyle.Instance.Formatting.FeedbackText;
            string actionStyle = PSStyle.Instance.Formatting.FeedbackAction;
            string ansiReset = PSStyle.Instance.Reset;

            if (!ui.SupportsVirtualTerminal)
            {
                captionStyle = string.Empty;
                italics = string.Empty;
                nameStyle = string.Empty;
                textStyle = string.Empty;
                actionStyle = string.Empty;
                ansiReset = string.Empty;
            }

            var output = new StringBuilder();
            var chkset = new HashSet<FeedbackItem>();

            foreach (FeedbackResult entry in feedbacks)
            {
                output.AppendLine();
                output.Append($"{captionStyle}[{ansiReset}")
                    .Append($"{nameStyle}{italics}{entry.Name}{ansiReset}")
                    .Append($"{captionStyle}]{ansiReset}");

                FeedbackItem item = entry.Item;
                chkset.Add(item);

                do
                {
                    RenderText(output, item.Header, textStyle, ansiReset, indent: 2, startOnNewLine: true);
                    RenderActions(output, item, textStyle, actionStyle, ansiReset);
                    RenderText(output, item.Footer, textStyle, ansiReset, indent: 2, startOnNewLine: true);

                    // A feedback provider may return multiple feedback items, though that may be rare.
                    item = item.Next;
                }
                while (item is not null && chkset.Add(item));

                ui.Write(output.ToString());
                output.Clear();
                chkset.Clear();
            }

            // Feedback section ends with a new line.
            ui.WriteLine();
        }

        /// <summary>
        /// Helper function to render feedback message.
        /// </summary>
        /// <param name="output">The output string builder to write to.</param>
        /// <param name="text">The text to be rendered.</param>
        /// <param name="style">The style to be used.</param>
        /// <param name="ansiReset">The ANSI code to reset.</param>
        /// <param name="indent">The number of spaces for indentation.</param>
        /// <param name="startOnNewLine">Indicates whether to start writing from a new line.</param>
        internal static void RenderText(StringBuilder output, string text, string style, string ansiReset, int indent, bool startOnNewLine)
        {
            if (text is null)
            {
                return;
            }

            if (startOnNewLine)
            {
                // Start writing the text on the next line.
                output.AppendLine();
            }

            // Apply the style.
            output.Append(style);

            int count = 0;
            var trimChars = "\r\n".AsSpan();
            var span = text.AsSpan().Trim(trimChars);

            // This loop renders the text with minimal allocation.
            while (true)
            {
                int index = span.IndexOf('\n');
                var line = index is -1 ? span : span.Slice(0, index);

                if (startOnNewLine || count > 0)
                {
                    output.Append(' ', indent);
                }

                output.Append(line.TrimEnd('\r')).AppendLine();

                // Break out the loop if we are done with the last line.
                if (index is -1)
                {
                    break;
                }

                // Point to the rest of feedback text.
                span = span.Slice(index + 1);
                count++;
            }

            output.Append(ansiReset);
        }

        /// <summary>
        /// Helper function to render feedback actions.
        /// </summary>
        /// <param name="output">The output string builder to write to.</param>
        /// <param name="item">The feedback item to be rendered.</param>
        /// <param name="textStyle">The style used for feedback messages.</param>
        /// <param name="actionStyle">The style used for feedback actions.</param>
        /// <param name="ansiReset">The ANSI code to reset.</param>
        internal static void RenderActions(StringBuilder output, FeedbackItem item, string textStyle, string actionStyle, string ansiReset)
        {
            if (item.RecommendedActions is null || item.RecommendedActions.Count is 0)
            {
                return;
            }

            List<string> actions = item.RecommendedActions;
            if (item.Layout is FeedbackDisplayLayout.Landscape)
            {
                // Add 4-space indentation and write the indicator.
                output.Append($"    {textStyle}{s_actionIndicator}{ansiReset} ");

                // Then concatenate the action texts.
                for (int i = 0; i < actions.Count; i++)
                {
                    string action = actions[i];
                    if (i > 0)
                    {
                        output.Append(", ");
                    }

                    output.Append(actionStyle).Append(action).Append(ansiReset);
                }

                output.AppendLine();
            }
            else
            {
                int lastIndex = actions.Count - 1;
                for (int i = 0; i < actions.Count; i++)
                {
                    string action = actions[i];

                    // Add 4-space indentation and write the indicator, then write the action.
                    output.Append($"    {textStyle}{s_actionIndicator}{ansiReset} ");

                    if (action.Contains('\n'))
                    {
                        // If the action is a code snippet, properly render it with the right indentation.
                        RenderText(output, action, actionStyle, ansiReset, indent: 6, startOnNewLine: false);

                        // Append an extra line unless it's the last action.
                        if (i != lastIndex)
                        {
                            output.AppendLine();
                        }
                    }
                    else
                    {
                        output.Append(actionStyle).Append(action).Append(ansiReset)
                            .AppendLine();
                    }
                }
            }
        }

        #endregion

        #endregion
    }

    #endregion
}
