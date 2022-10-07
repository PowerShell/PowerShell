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
                return string.Format(CultureInfo.InvariantCulture, "[{0}@{1}]: {2}", sshConnectionInfo.UserName, sshConnectionInfo.ComputerName, basePrompt);
            }

            return string.Format(CultureInfo.InvariantCulture, "[{0}]: {1}", runspace.ConnectionInfo.ComputerName, basePrompt);
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
