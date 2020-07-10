// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Remoting;

using System.Management.Automation.Runspaces;
using System.Diagnostics.CodeAnalysis;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This cmdlet stops the runspace and frees the resources associated with
    /// that runspace. If any execution is in process in that runspace, it is
    /// stopped. Also, the runspace is removed from the global cache.
    ///
    /// This cmdlet can be used in the following ways:
    ///
    /// Remove the runspace specified
    ///     $runspace = New-PSSession
    ///     Remove-PSSession -remoterunspaceinfo $runspace
    ///
    /// Remove the runspace specified (no need for a parameter name)
    ///     $runspace = New-PSSession
    ///     Remove-PSSession $runspace.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PSSession", SupportsShouldProcess = true,
            DefaultParameterSetName = RemovePSSessionCommand.IdParameterSet,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096963", RemotingCapability = RemotingCapability.OwnedByCommand)]
    public class RemovePSSessionCommand : PSRunspaceCmdlet
    {
        #region Parameters

        /// <summary>
        /// Specifies the PSSession objects which need to be
        /// removed.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = RemovePSSessionCommand.SessionParameterSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public PSSession[] Session { get; set; }

        /// <summary>
        /// ID of target container.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "This is by spec.")]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRunspaceCmdlet.ContainerIdParameterSet)]
        [ValidateNotNullOrEmpty]
        public override string[] ContainerId { get; set; }

        /// <summary>
        /// Guid of target virtual machine.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "This is by spec.")]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRunspaceCmdlet.VMIdParameterSet)]
        [ValidateNotNullOrEmpty]
        [Alias("VMGuid")]
        public override Guid[] VMId { get; set; }

        /// <summary>
        /// Name of target virtual machine.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "This is by spec.")]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRunspaceCmdlet.VMNameParameterSet)]
        [ValidateNotNullOrEmpty]
        public override string[] VMName { get; set; }

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// Do the following actions:
        ///     1. If runspace is in opened state,
        ///             a. stop any execution in process in the runspace
        ///             b. close the runspace
        ///     2. Remove the runspace from the global cache.
        /// </summary>
        protected override void ProcessRecord()
        {
            ICollection<PSSession> toRemove = null;

            switch (ParameterSetName)
            {
                case RemovePSSessionCommand.ComputerNameParameterSet:
                case RemovePSSessionCommand.NameParameterSet:
                case RemovePSSessionCommand.InstanceIdParameterSet:
                case RemovePSSessionCommand.IdParameterSet:
                case RemovePSSessionCommand.ContainerIdParameterSet:
                case RemovePSSessionCommand.VMIdParameterSet:
                case RemovePSSessionCommand.VMNameParameterSet:
                    {
                        Dictionary<Guid, PSSession> matches = GetMatchingRunspaces(false, true);

                        toRemove = matches.Values;
                    }

                    break;
                case RemovePSSessionCommand.SessionParameterSet:
                    {
                        toRemove = Session;
                    }

                    break;
                default:
                    Diagnostics.Assert(false, "Invalid Parameter Set");
                    toRemove = new Collection<PSSession>(); // initialize toRemove to turn off PREfast warning about it being null
                    break;
            }

            foreach (PSSession remoteRunspaceInfo in toRemove)
            {
                RemoteRunspace remoteRunspace = (RemoteRunspace)remoteRunspaceInfo.Runspace;

                if (ShouldProcess(remoteRunspace.ConnectionInfo.ComputerName, "Remove"))
                {
                    // If the remote runspace is in a disconnected state, first try to connect it so that
                    // it can be removed from both the client and server.
                    if (remoteRunspaceInfo.Runspace.RunspaceStateInfo.State == RunspaceState.Disconnected)
                    {
                        bool ConnectSucceeded;

                        try
                        {
                            remoteRunspaceInfo.Runspace.Connect();
                            ConnectSucceeded = true;
                        }
                        catch (InvalidRunspaceStateException)
                        {
                            ConnectSucceeded = false;
                        }
                        catch (PSRemotingTransportException)
                        {
                            ConnectSucceeded = false;
                        }

                        if (!ConnectSucceeded)
                        {
                            // Write error notification letting user know that session cannot be removed
                            // from server due to lack of connection.
                            string msg = System.Management.Automation.Internal.StringUtil.Format(
                                RemotingErrorIdStrings.RemoveRunspaceNotConnected, remoteRunspace.PSSessionName);
                            Exception reason = new RuntimeException(msg);
                            ErrorRecord errorRecord = new ErrorRecord(reason, "RemoveSessionCannotConnectToServer",
                                ErrorCategory.InvalidOperation, remoteRunspace);
                            WriteError(errorRecord);

                            // Continue removing the runspace from the client.
                        }
                    }

                    try
                    {
                        // Dispose internally calls Close() and Close()
                        // is a no-op if the state is not Opened, so just
                        // dispose the runspace
                        remoteRunspace.Dispose();
                    }
                    catch (PSRemotingTransportException)
                    {
                        // just ignore, there is some transport error
                        // on Close()
                    }

                    try
                    {
                        // Remove the runspace from the repository
                        this.RunspaceRepository.Remove(remoteRunspaceInfo);
                    }
                    catch (ArgumentException)
                    {
                        // just ignore, the runspace may already have
                        // been removed
                    }
                }
            }
        }

        #endregion Overrides
    }
}
