/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections.Generic;
using System.IO;
using System.Management.Automation.Host;
using System.Management.Automation.Internal.Host;

#if !CORECLR
using BinaryFormatter = System.Runtime.Serialization.Formatters.Binary.BinaryFormatter;
#endif

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
        internal Version ProtocolVersion { get; set; }

        internal Version PSVersion { get; }
        internal Version SerializationVersion { get; }
        internal RemotingDestination RemotingDestination { get; }

#if !CORECLR // TimeZone Not In CoreCLR
#endif

        private static byte[] s_timeZoneInByteFormat;

        /// <summary>
        /// Constructor for RemoteSessionCapability.
        /// </summary>
        /// <remarks>should not be called from outside, use create methods instead
        /// </remarks>
        internal RemoteSessionCapability(RemotingDestination remotingDestination)
        {
            ProtocolVersion = RemotingConstants.ProtocolVersion;
            // PS Version 3 is fully backward compatible with Version 2
            // In the remoting protocol sense, nothing is changing between PS3 and PS2
            // For negotiation to succeed with old client/servers we have to use 2.
            PSVersion = new Version(2, 0); //PSVersionInfo.PSVersion;
            SerializationVersion = PSVersionInfo.SerializationVersion;
            RemotingDestination = remotingDestination;
        }

        internal RemoteSessionCapability(RemotingDestination remotingDestination,
            Version protocolVersion,
            Version psVersion,
            Version serVersion)
        {
            ProtocolVersion = protocolVersion;
            PSVersion = psVersion;
            SerializationVersion = serVersion;
            RemotingDestination = remotingDestination;
        }

        /// <summary>
        /// Added to enable ClrFacade.GetUninitializedObject to instantiate an uninitialized version of this class.
        /// </summary>
        internal RemoteSessionCapability()
        { }

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

        /// <summary>
        /// This is static property which gets Current TimeZone in byte format
        /// by using ByteFormatter. 
        /// This is static to make client generate this only once.
        /// </summary>
        internal static byte[] GetCurrentTimeZoneInByteFormat()
        {
            if (null == s_timeZoneInByteFormat)
            {
#if CORECLR //TODO:CORECLR 'BinaryFormatter' is not in CORE CLR. Since this is TimeZone related and TimeZone is not available on CORE CLR so wait until we solve TimeZone issue.
                s_timeZoneInByteFormat = Utils.EmptyArray<byte>();
#else
                Exception e = null;
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    using (MemoryStream stream = new MemoryStream())
                    {
                        formatter.Serialize(stream, TimeZone.CurrentTimeZone);
                        stream.Seek(0, SeekOrigin.Begin);
                        byte[] result = new byte[stream.Length];
                        stream.Read(result, 0, (int)stream.Length);
                        s_timeZoneInByteFormat = result;
                    }
                }
                catch (ArgumentNullException ane)
                {
                    e = ane;
                }
                catch (System.Runtime.Serialization.SerializationException sre)
                {
                    e = sre;
                }
                catch (System.Security.SecurityException se)
                {
                    e = se;
                }

                // if there is any exception serializing the timezone information
                // ignore it and dont try to serialize again.
                if (null != e)
                {
                    s_timeZoneInByteFormat = Utils.EmptyArray<byte>();
                }
#endif
            }

            return s_timeZoneInByteFormat;
        }

#if !CORECLR // TimeZone Not In CoreCLR
        /// <summary>
        /// Gets the TimeZone of the destination machine. This may be null
        /// </summary>
        internal TimeZone TimeZone { get; set; }
#endif

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
    internal class HostDefaultData
    {
        /// <summary>
        /// Data.
        /// </summary>
        // DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting
        private Dictionary<HostDefaultDataId, object> data;

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
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
            }

            // Set BackgroundColor.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.BackgroundColor, hostRawUI.BackgroundColor);
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
            }

            // Set CursorPosition.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.CursorPosition, hostRawUI.CursorPosition);
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
            }

            // Set WindowPosition.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.WindowPosition, hostRawUI.WindowPosition);
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
            }

            // Set CursorSize.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.CursorSize, hostRawUI.CursorSize);
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
            }

            // Set BufferSize.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.BufferSize, hostRawUI.BufferSize);
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
            }

            // Set WindowSize.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.WindowSize, hostRawUI.WindowSize);
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
            }

            // Set MaxWindowSize.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.MaxWindowSize, hostRawUI.MaxWindowSize);
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
            }

            // Set MaxPhysicalWindowSize.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.MaxPhysicalWindowSize, hostRawUI.MaxPhysicalWindowSize);
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
            }

            // Set WindowTitle.
            try
            {
                hostDefaultData.SetValue(HostDefaultDataId.WindowTitle, hostRawUI.WindowTitle);
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
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
        private bool _isHostUINull;

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
        private bool _isHostRawUINull;

        private readonly bool _isHostNull;

        // DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting
        private readonly HostDefaultData _hostDefaultData;
        private bool _useRunspaceHost;

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
        /// Added to enable ClrFacade.GetUninitializedObject to instantiate an uninitialized version of this class.
        /// </summary>
        internal HostInfo()
        { }

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
            if (host.UI == null) { return; }
            isHostUINull = false;

            // Verify that the raw UI is not null.
            if (host.UI.RawUI == null) { return; }
            isHostRawUINull = false;
        }
    }
}
