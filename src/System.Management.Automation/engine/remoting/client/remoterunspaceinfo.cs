// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation.Internal;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Computer target type.
    /// </summary>
    public enum TargetMachineType
    {
        /// <summary>
        /// Target is a machine with which the session is based on networking.
        /// </summary>
        RemoteMachine,

        /// <summary>
        /// Target is a virtual machine with which the session is based on Hyper-V socket.
        /// </summary>
        VirtualMachine,

        /// <summary>
        /// Target is a container with which the session is based on Hyper-V socket (Hyper-V
        /// container) or named pipe (windows container)
        /// </summary>
        Container
    }

    /// <summary>
    /// Class that exposes read only properties and which conveys information
    /// about a remote runspace object to the user. The class serves the
    /// following purpose:
    ///     1. Exposes useful information to the user as properties
    ///     2. Shields the remote runspace object from directly being exposed
    ///        to the user. This way, the user will not be able to directly
    ///        act upon the object, but instead will have to use the remoting
    ///        cmdlets. This will prevent any unpredictable behavior.
    /// </summary>
    public sealed class PSSession
    {
        #region Private Members

        private RemoteRunspace _remoteRunspace;

        /// <summary>
        /// Static variable which is incremented to generate id.
        /// </summary>
        private static int s_seed = 0;

        #endregion Private Members

        #region Public Properties

        /// <summary>
        /// Type of the computer target.
        /// </summary>
        public TargetMachineType ComputerType { get; set; }

        /// <summary>
        /// Name of the computer target.
        /// </summary>
        public string ComputerName
        {
            get
            {
                return _remoteRunspace.ConnectionInfo.ComputerName;
            }
        }

        /// <summary>
        /// Id of the container target.
        /// </summary>
        public string ContainerId
        {
            get
            {
                if (ComputerType == TargetMachineType.Container)
                {
                    ContainerConnectionInfo connectionInfo = _remoteRunspace.ConnectionInfo as ContainerConnectionInfo;
                    return connectionInfo.ContainerProc.ContainerId;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Name of the virtual machine target.
        /// </summary>
        public string VMName
        {
            get
            {
                if (ComputerType == TargetMachineType.VirtualMachine)
                {
                    return _remoteRunspace.ConnectionInfo.ComputerName;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Guid of the virtual machine target.
        /// </summary>
        public Guid? VMId
        {
            get
            {
                if (ComputerType == TargetMachineType.VirtualMachine)
                {
                    VMConnectionInfo connectionInfo = _remoteRunspace.ConnectionInfo as VMConnectionInfo;
                    return connectionInfo.VMGuid;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Shell which is executed in the remote machine.
        /// </summary>
        public string ConfigurationName { get; }

        /// <summary>
        /// InstanceID that identifies this runspace.
        /// </summary>
        public Guid InstanceId
        {
            get
            {
                return _remoteRunspace.InstanceId;
            }
        }

        /// <summary>
        /// SessionId of this runspace. This is unique only across
        /// a session.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Friendly name for identifying this runspace.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Indicates whether the specified runspace is available
        /// for executing commands.
        /// </summary>
        public RunspaceAvailability Availability
        {
            get
            {
                return Runspace.RunspaceAvailability;
            }
        }

        /// <summary>
        /// Private data to be used by applications built on top of PowerShell.
        /// Optionally sent by the remote server when creating a new session / runspace.
        /// </summary>
        public PSPrimitiveDictionary ApplicationPrivateData
        {
            get
            {
                return this.Runspace.GetApplicationPrivateData();
            }
        }

        /// <summary>
        /// The remote runspace object based on which this information object
        /// is derived.
        /// </summary>
        /// <remarks>This property is marked internal to allow other cmdlets
        /// to get access to the RemoteRunspace object and operate on it like
        /// for instance test-runspace, close-runspace etc</remarks>
        public Runspace Runspace
        {
            get
            {
                return _remoteRunspace;
            }
        }

        /// <summary>
        /// Name of the transport used.
        /// </summary>
        public string Transport => GetTransportName();

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// ToString method override.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString()
        {
            // PSSession is a PowerShell type name and so should not be localized.
            string formatString = "[PSSession]{0}";
            return StringUtil.Format(formatString, Name);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Internal method to insert a runspace into a PSSession object.
        /// This is used only for Disconnect/Reconnect scenarios where the
        /// new runspace is a reconstructed runspace having the same Guid
        /// as the existing runspace.
        /// </summary>
        /// <param name="remoteRunspace">Runspace to insert.</param>
        /// <returns>Boolean indicating if runspace was inserted.</returns>
        internal bool InsertRunspace(RemoteRunspace remoteRunspace)
        {
            if (remoteRunspace == null ||
                remoteRunspace.InstanceId != _remoteRunspace.InstanceId)
            {
                return false;
            }

            _remoteRunspace = remoteRunspace;
            return true;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// This constructor will be used to created a remote runspace info
        /// object with a auto generated name.
        /// </summary>
        /// <param name="remoteRunspace">Remote runspace object for which
        /// the info object need to be created</param>
        internal PSSession(RemoteRunspace remoteRunspace)
        {
            _remoteRunspace = remoteRunspace;

            // Use passed in session Id, if available.
            if (remoteRunspace.PSSessionId != -1)
            {
                Id = remoteRunspace.PSSessionId;
            }
            else
            {
                Id = System.Threading.Interlocked.Increment(ref s_seed);
                remoteRunspace.PSSessionId = Id;
            }

            // Use passed in friendly name, if available.
            if (!string.IsNullOrEmpty(remoteRunspace.PSSessionName))
            {
                Name = remoteRunspace.PSSessionName;
            }
            else
            {
                Name = "Runspace" + Id;
                remoteRunspace.PSSessionName = Name;
            }

            // WSMan session
            if (remoteRunspace.ConnectionInfo is WSManConnectionInfo)
            {
                ComputerType = TargetMachineType.RemoteMachine;

                string fullShellName = WSManConnectionInfo.ExtractPropertyAsWsManConnectionInfo<string>(
                    remoteRunspace.ConnectionInfo,
                    "ShellUri", string.Empty);

                ConfigurationName = GetDisplayShellName(fullShellName);
                return;
            }

            // VM session
            VMConnectionInfo vmConnectionInfo = remoteRunspace.ConnectionInfo as VMConnectionInfo;
            if (vmConnectionInfo != null)
            {
                ComputerType = TargetMachineType.VirtualMachine;
                ConfigurationName = vmConnectionInfo.ConfigurationName;
                return;
            }

            // Container session
            ContainerConnectionInfo containerConnectionInfo = remoteRunspace.ConnectionInfo as ContainerConnectionInfo;
            if (containerConnectionInfo != null)
            {
                ComputerType = TargetMachineType.Container;
                ConfigurationName = containerConnectionInfo.ContainerProc.ConfigurationName;
                return;
            }

            // SSH session
            SSHConnectionInfo sshConnectionInfo = remoteRunspace.ConnectionInfo as SSHConnectionInfo;
            if (sshConnectionInfo != null)
            {
                ComputerType = TargetMachineType.RemoteMachine;
                ConfigurationName = "DefaultShell";
                return;
            }

            // We only support WSMan/VM/Container sessions now.
            Dbg.Assert(false, "Invalid Runspace");
        }

        #endregion Constructor

        #region Private Methods

        /// <summary>
        /// Generates and returns the runspace name.
        /// </summary>
        /// <returns>Auto generated name.</returns>
        private string GetTransportName()
        {
            if (_remoteRunspace.ConnectionInfo is WSManConnectionInfo)
            {
                return "WSMan";
            }
            else if (_remoteRunspace.ConnectionInfo is SSHConnectionInfo)
            {
                return "SSH";
            }
            else if (_remoteRunspace.ConnectionInfo is NamedPipeConnectionInfo)
            {
                return "NamedPipe";
            }
            else if (_remoteRunspace.ConnectionInfo is ContainerConnectionInfo)
            {
                return "Container";
            }
            else if (_remoteRunspace.ConnectionInfo is NewProcessConnectionInfo)
            {
                return "Process";
            }
            else if (_remoteRunspace.ConnectionInfo is VMConnectionInfo)
            {
                return "VMBus";
            }
            else
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Returns shell configuration name with shell prefix removed.
        /// </summary>
        /// <param name="shell">Shell configuration name.</param>
        /// <returns>Display shell name.</returns>
        private string GetDisplayShellName(string shell)
        {
            string shellPrefix = System.Management.Automation.Remoting.Client.WSManNativeApi.ResourceURIPrefix;
            int index = shell.IndexOf(shellPrefix, StringComparison.OrdinalIgnoreCase);

            return (index == 0) ? shell.Substring(shellPrefix.Length) : shell;
        }

        #endregion Private Methods

        #region Static Methods

        /// <summary>
        /// Generates a unique runspace id.
        /// </summary>
        /// <param name="rtnId">Returned Id.</param>
        /// <returns>Returned name.</returns>
        internal static string GenerateRunspaceName(out int rtnId)
        {
            int id = GenerateRunspaceId();
            rtnId = id;
            return "Runspace" + id.ToString();
        }

        /// <summary>
        /// Increments and returns a session unique runspace Id.
        /// </summary>
        /// <returns>Id.</returns>
        internal static int GenerateRunspaceId()
        {
            return System.Threading.Interlocked.Increment(ref s_seed);
        }

        #endregion
    }
}
