// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Management.Automation.Host;
using System.Management.Automation.Internal.Host;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// This class contains information about the capability of one side of the connection. The client
    /// side and the server side will have their own capabilities. These two sets of capabilities will
    /// be used in a capability negotiation algorithm to determine if it is possible to establish a
    /// connection between the client and the server.
    /// </summary>
    internal class RemoteSessionCapability
    {
        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell

        private readonly Version _psversion;
        private readonly Version _serversion;
        private Version _protocolVersion;
        private readonly RemotingDestination _remotingDestination;

        #endregion

        internal Version ProtocolVersion
        {
            get
            {
                return _protocolVersion;
            }

            set
            {
                _protocolVersion = value;
            }
        }

        internal Version PSVersion { get { return _psversion; } }

        internal Version SerializationVersion { get { return _serversion; } }

        internal RemotingDestination RemotingDestination { get { return _remotingDestination; } }

        /// <summary>
        /// Constructor for RemoteSessionCapability.
        /// </summary>
        /// <remarks>should not be called from outside, use create methods instead
        /// </remarks>
        internal RemoteSessionCapability(RemotingDestination remotingDestination)
        {
            _protocolVersion = RemotingConstants.ProtocolVersion;
            // PS Version 3 is fully backward compatible with Version 2
            // In the remoting protocol sense, nothing is changing between PS3 and PS2
            // For negotiation to succeed with old client/servers we have to use 2.
            _psversion = new Version(2, 0); // PSVersionInfo.PSVersion;
            _serversion = PSVersionInfo.SerializationVersion;
            _remotingDestination = remotingDestination;
        }

        internal RemoteSessionCapability(RemotingDestination remotingDestination,
            Version protocolVersion,
            Version psVersion,
            Version serVersion)
        {
            _protocolVersion = protocolVersion;
            _psversion = psVersion;
            _serversion = serVersion;
            _remotingDestination = remotingDestination;
        }

        /// <summary>
        /// Create client capability.
        /// </summary>
        internal static RemoteSessionCapability CreateClientCapability()
        {
            return new RemoteSessionCapability(RemotingDestination.Server);
        }

        /// <summary>
        /// Create server capability.
        /// </summary>
        internal static RemoteSessionCapability CreateServerCapability()
        {
            return new RemoteSessionCapability(RemotingDestination.Client);
        }
    }

    /// <summary>
    /// The HostDefaultDataId enum.
    /// </summary>
    internal enum HostDefaultDataId
    {
        ForegroundColor,
        BackgroundColor,
        CursorPosition,
        WindowPosition,
        CursorSize,
        BufferSize,
        WindowSize,
        MaxWindowSize,
        MaxPhysicalWindowSize,
        WindowTitle,
    }

    /// <summary>
    /// The HostDefaultData class.
    /// </summary>
    internal sealed class HostDefaultData
    {
        /// <summary>
        /// Data.
        /// </summary>
        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell

        private readonly Dictionary<HostDefaultDataId, object> data;

        #endregion

        /// <summary>
        /// Private constructor to force use of Create.
        /// </summary>
        private HostDefaultData()
        {
            data = new Dictionary<HostDefaultDataId, object>();
        }

        /// <summary>
        /// Indexer to provide clean access to data.
        /// </summary>
        internal object this[HostDefaultDataId id]
        {
            get
            {
                return this.GetValue(id);
            }
        }

        /// <summary>
        /// Has value.
        /// </summary>
        internal bool HasValue(HostDefaultDataId id)
        {
            return data.ContainsKey(id);
        }

        /// <summary>
        /// Set value.
        /// </summary>
        internal void SetValue(HostDefaultDataId id, object dataValue)
        {
            data[id] = dataValue;
        }

        /// <summary>
        /// Get value.
        /// </summary>
        internal object GetValue(HostDefaultDataId id)
        {
            object result;
            data.TryGetValue(id, out result);
            return result;
        }

        /// <summary>
        /// Returns null if host is null or if reading RawUI fields fails; otherwise returns a valid object.
        /// </summary>
        internal static HostDefaultData Create(PSHostRawUserInterface hostRawUI)
        {
            if (hostRawUI == null)
            {
                return null;
            }

            HostDefaultData hostDefaultData = new HostDefaultData();

            // Try to get values from the host. Catch-all okay because of 3rd party call-out.

            // Set ForegroundColor.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.ForegroundColor, hostRawUI.ForegroundColor);
            }
            catch (Exception)
            {
            }

            // Set BackgroundColor.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.BackgroundColor, hostRawUI.BackgroundColor);
            }
            catch (Exception)
            {
            }

            // Set CursorPosition.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.CursorPosition, hostRawUI.CursorPosition);
            }
            catch (Exception)
            {
            }

            // Set WindowPosition.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.WindowPosition, hostRawUI.WindowPosition);
            }
            catch (Exception)
            {
            }

            // Set CursorSize.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.CursorSize, hostRawUI.CursorSize);
            }
            catch (Exception)
            {
            }

            // Set BufferSize.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.BufferSize, hostRawUI.BufferSize);
            }
            catch (Exception)
            {
            }

            // Set WindowSize.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.WindowSize, hostRawUI.WindowSize);
            }
            catch (Exception)
            {
            }

            // Set MaxWindowSize.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.MaxWindowSize, hostRawUI.MaxWindowSize);
            }
            catch (Exception)
            {
            }

            // Set MaxPhysicalWindowSize.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.MaxPhysicalWindowSize, hostRawUI.MaxPhysicalWindowSize);
            }
            catch (Exception)
            {
            }

            // Set WindowTitle.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.WindowTitle, hostRawUI.WindowTitle);
            }
            catch (Exception)
            {
            }

            return hostDefaultData;
        }
    }

    /// <summary>
    /// The HostInfo class.
    /// </summary>
    internal class HostInfo
    {
        /// <summary>
        /// Host default data.
        /// </summary>
        internal HostDefaultData HostDefaultData
        {
            get { return _hostDefaultData; }
        }

        /// <summary>
        /// Is host null.
        /// </summary>
        internal bool IsHostNull
        {
            get { return _isHostNull; }
        }

        /// <summary>
        /// Is host ui null.
        /// </summary>
        private readonly bool _isHostUINull;

        /// <summary>
        /// Is host ui null.
        /// </summary>
        internal bool IsHostUINull
        {
            get
            {
                return _isHostUINull;
            }
        }

        /// <summary>
        /// Is host raw ui null.
        /// </summary>
        private readonly bool _isHostRawUINull;

        private readonly bool _isHostNull;

        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell

        private readonly HostDefaultData _hostDefaultData;
        private bool _useRunspaceHost;

        #endregion

        /// <summary>
        /// Is host raw ui null.
        /// </summary>
        internal bool IsHostRawUINull
        {
            get
            {
                return _isHostRawUINull;
            }
        }

        /// <summary>
        /// Use runspace host.
        /// </summary>
        internal bool UseRunspaceHost
        {
            get { return _useRunspaceHost; }

            set { _useRunspaceHost = value; }
        }

        /// <summary>
        /// Constructor for HostInfo.
        /// </summary>
        internal HostInfo(PSHost host)
        {
            // Set these flags based on investigating the host.
            CheckHostChain(host, ref _isHostNull, ref _isHostUINull, ref _isHostRawUINull);

            // If raw UI is non-null then get the host-info object.
            if (!_isHostUINull && !_isHostRawUINull)
            {
                _hostDefaultData = HostDefaultData.Create(host.UI.RawUI);
            }
        }

        /// <summary>
        /// Check host chain.
        /// </summary>
        private static void CheckHostChain(PSHost host, ref bool isHostNull, ref bool isHostUINull, ref bool isHostRawUINull)
        {
            // Set the defaults.
            isHostNull = true;
            isHostUINull = true;
            isHostRawUINull = true;

            // Unwrap the host: remove outer InternalHost object.
            if (host == null)
            {
                // If host is null then the bools are correct. Nothing further to do here.
                return;
            }
            else if (host is InternalHost)
            {
                // This nesting can only be one level deep.
                host = ((InternalHost)host).ExternalHost;
            }

            // At this point we know for sure that the host is not null.
            isHostNull = false;

            // Verify that the UI is not null.
            if (host.UI == null)
            {
                return;
            }

            isHostUINull = false;

            // Verify that the raw UI is not null.
            if (host.UI.RawUI == null)
            {
                return;
            }

            isHostRawUINull = false;
        }
    }
}
