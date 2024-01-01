// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerShell.Commands;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// Defines generic utilities and helper methods for Remoting.
    /// </summary>
    internal static class RemotingUtils
    {
        private static readonly string s_LOCALHOST = "localhost";

        /// <summary>
        /// Parse a hostname used with SSH Transport to get embedded
        /// username and/or port.
        /// </summary>
        /// <param name="cmdlet">PSRemoting base cmdlet.</param>
        /// <param name="hostname">Host name to parse.</param>
        /// <param name="host">Resolved target host.</param>
        /// <param name="userName">Resolved target user name.</param>
        /// <param name="port">Resolved target port.</param>
        internal static void ParseSshHostName(PSRemotingBaseCmdlet cmdlet, string hostname, out string host, out string userName, out int port)
        {
            host = hostname;
            userName = cmdlet.UserName;
            port = cmdlet.Port;
            try
            {
                Uri uri = new("ssh://" + hostname);

                if (uri.HostNameType == UriHostNameType.Dns)
                {
                    // Extract original host from URI with preserved case
                    // This is needed since System.Uri canonicalizes URI and makes host lowercase
                    int originalHostIndex = uri.OriginalString.IndexOf(uri.Host, StringComparison.OrdinalIgnoreCase);
                    string originalHost = originalHostIndex != -1 
                        ? uri.OriginalString.Substring(originalHostIndex, uri.Host.Length)
                        : uri.Host;

                    host = ResolveComputerName(originalHost);
                }
                else
                {
                    host = ResolveComputerName(uri.Host);
                }

                ValidateComputerName(cmdlet, new string[] { host });
                if (uri.UserInfo != string.Empty)
                {
                    userName = uri.UserInfo;
                }

                if (uri.Port != -1)
                {
                    port = uri.Port;
                }
            }
            catch (UriFormatException)
            {
                cmdlet.ThrowTerminatingError(new ErrorRecord(
                    new ArgumentException(PSRemotingErrorInvariants.FormatResourceString(
                        RemotingErrorIdStrings.InvalidComputerName)), "PSSessionInvalidComputerName",
                            ErrorCategory.InvalidArgument, hostname));
            }
        }

        /// <summary>
        /// Validates computer names to check if none of them
        /// happen to be a Uri. If so this throws an error.
        /// </summary>
        /// <param name="cmdlet">PSRemoting base cmdlet.</param>
        /// <param name="computerNames">collection of computer
        /// names to validate</param>
        internal static void ValidateComputerName(PSRemotingBaseCmdlet cmdlet, string[] computerNames)
        {
            foreach (string computerName in computerNames)
            {
                UriHostNameType nametype = Uri.CheckHostName(computerName);
                if (!(nametype == UriHostNameType.Dns || nametype == UriHostNameType.IPv4 ||
                    nametype == UriHostNameType.IPv6))
                {
                    cmdlet.ThrowTerminatingError(new ErrorRecord(
                        new ArgumentException(PSRemotingErrorInvariants.FormatResourceString(
                            RemotingErrorIdStrings.InvalidComputerName)), "PSSessionInvalidComputerName",
                                ErrorCategory.InvalidArgument, computerNames));
                }
            }
        }

        /// <summary>
        /// Resolves a computer name. If its null or empty
        /// its assumed to be localhost.
        /// </summary>
        /// <param name="computerName">Computer name to resolve.</param>
        /// <returns>Resolved computer name.</returns>
        internal static string ResolveComputerName(string computerName)
        {
            Diagnostics.Assert(computerName != null, "Null ComputerName");

            if (string.Equals(computerName, ".", StringComparison.OrdinalIgnoreCase))
            {
                // tracer.WriteEvent(ref PSEventDescriptors.PS_EVENT_HOSTNAMERESOLVE);
                // tracer.Dispose();
                // tracer.OperationalChannel.WriteVerbose(PSEventId.HostNameResolve, PSOpcode.Method, PSTask.CreateRunspace);
                return s_LOCALHOST;
            }
            else
            {
                return computerName;
            }
        }
    }
}
