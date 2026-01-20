// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Runspaces.Internal;
using System.Management.Automation.Tracing;
using System.Reflection;
using System.Threading;

using Microsoft.PowerShell;
using Microsoft.PowerShell.Commands;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
#if NOT_USED

    /// <summary>
    /// This represents that a remote data is incorrectly encoded.
    /// </summary>
    public class RemotingEncodingException : RuntimeException
    {
    #region Constructors

        /// <summary>
        /// </summary>
        public RemotingEncodingException()
            : base()
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        public RemotingEncodingException(string message)
            : base (message)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public RemotingEncodingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <param name="errorRecord"></param>
        public RemotingEncodingException(string message, Exception innerException, ErrorRecord errorRecord)
            : base(message, innerException, errorRecord)
        {
        }

    #endregion Constructors
    }

#endif // NOT_USED

    /// <summary>
    /// Constants used by hosts in remoting.
    /// </summary>
    internal static class RemotingConstants
    {
        internal static readonly Version HostVersion = PSVersionInfo.PSVersion;

        internal static readonly Version ProtocolVersion_2_0 = new(2, 0); // Window 7 RC
        internal static readonly Version ProtocolVersion_2_1 = new(2, 1); // Window 7 RTM
        internal static readonly Version ProtocolVersion_2_2 = new(2, 2); // Window 8 RTM
        internal static readonly Version ProtocolVersion_2_3 = new(2, 3); // Window 10 RTM
        internal static readonly Version ProtocolVersion_2_4 = new(2, 4); // PowerShell 7.6

        // Minor will be incremented for each change in PSRP client/server stack and new versions will be
        // forked on early major release/drop changes history.
        //      2.101 to 2.102 - Disconnect support as of M2
        //      2.102 to 2.103 - Key exchange protocol changes in M3
        //      2.103 to 2.2   - Final ship protocol version value, no change to protocol
        //      2.2 to 2.3     - Enabling informational stream
        //      2.3 to 2.4     - Deprecate the 'Session_Key' exchange. The following messages are obsolete when both server and client are v2.4+:
        //                        - PUBLIC_KEY
        //                        - PUBLIC_KEY_REQUEST
        //                        - ENCRYPTED_SESSION_KEY
        //                       The padding algorithm 'RSAEncryptionPadding.Pkcs1' used in the 'Session_Key' exchange is NOT secure, and therefore,
        //                       PSRP needs to be used on top of a secure transport and the 'Session_Key' doesn't add any extra security.
        //                       So, we decided to deprecate the 'Session_Key' exchange in PSRP and skip encryption and decryption for 'SecureString'
        //                       objects. Instead, we require the transport to be secure for secure data transfer between PSRP clients and servers.
        internal static readonly Version ProtocolVersionCurrent = new(2, 4);
        internal static readonly Version ProtocolVersion = ProtocolVersionCurrent;
        // Used by remoting commands to add remoting specific note properties.
        internal static readonly string ComputerNameNoteProperty = "PSComputerName";
        internal static readonly string RunspaceIdNoteProperty = "RunspaceId";
        internal static readonly string ShowComputerNameNoteProperty = "PSShowComputerName";
        internal static readonly string SourceJobInstanceId = "PSSourceJobInstanceId";
        internal static readonly string EventObject = "PSEventObject";
        // used by Custom Shell related cmdlets.
        internal const string PSSessionConfigurationNoun = "PSSessionConfiguration";
        internal const string PSRemotingNoun = "PSRemoting";
        internal const string PSPluginDLLName = "pwrshplugin.dll";
        internal const string DefaultShellName = "Microsoft.PowerShell";
        internal const string MaxIdleTimeoutMS = "2147483647";
    }

    /// <summary>
    /// String constants used for names of properties that are for storing
    /// remoting message fields in a PSObject property bag.
    /// </summary>
    internal static class RemoteDataNameStrings
    {
        internal const string Destination = "Destination";
        internal const string RemotingTargetInterface = "RemotingTargetInterface";
        internal const string ClientRunspacePoolId = "ClientRunspacePoolId";
        internal const string ClientPowerShellId = "ClientPowerShellId";
        internal const string Action = "Action";
        internal const string DataType = "DataType";
        // used by negotiation algorithm to figure out client's timezone.
        internal const string TimeZone = "TimeZone";
        internal const string SenderInfoPreferenceVariable = "PSSenderInfo";
        // used by negotiation algorithm to figure out if the negotiation
        // request (from client) must comply.
        internal const string MustComply = "MustComply";
        // used by negotiation algorithm. Server sends this information back
        // to client to let client know if the negotiation succeeded.
        internal const string IsNegotiationSucceeded = "IsNegotiationSucceeded";

        #region Host Related Strings

        internal const string CallId = "ci";
        internal const string MethodId = "mi";
        internal const string MethodParameters = "mp";
        internal const string MethodReturnValue = "mr";
        internal const string MethodException = "me";

        internal const string PS_STARTUP_PROTOCOL_VERSION_NAME = "protocolversion";
        internal const string PublicKeyAsXml = "PublicKeyAsXml";
        internal const string PSVersion = "PSVersion";
        internal const string SerializationVersion = "SerializationVersion";

        internal const string MethodArrayElementType = "mat";
        internal const string MethodArrayLengths = "mal";
        internal const string MethodArrayElements = "mae";

        internal const string ObjectType = "T";
        internal const string ObjectValue = "V";

        #endregion

        #region Command discovery pipeline

        internal const string DiscoveryName = "Name";
        internal const string DiscoveryType = "CommandType";
        internal const string DiscoveryModule = "Namespace";
        internal const string DiscoveryFullyQualifiedModule = "FullyQualifiedModule";
        internal const string DiscoveryArgumentList = "ArgumentList";
        internal const string DiscoveryCount = "Count";

        #endregion

        #region PowerShell

        internal const string PSInvocationSettings = "PSInvocationSettings";
        internal const string ApartmentState = "ApartmentState";
        internal const string RemoteStreamOptions = "RemoteStreamOptions";
        internal const string AddToHistory = "AddToHistory";

        internal const string PowerShell = "PowerShell";
        internal const string IsNested = "IsNested";
        internal const string HistoryString = "History";
        internal const string RedirectShellErrorOutputPipe = "RedirectShellErrorOutputPipe";
        internal const string Commands = "Cmds";
        internal const string ExtraCommands = "ExtraCmds";
        internal const string CommandText = "Cmd";
        internal const string IsScript = "IsScript";
        internal const string UseLocalScopeNullable = "UseLocalScope";
        internal const string MergeUnclaimedPreviousCommandResults = "MergePreviousResults";
        internal const string MergeMyResult = "MergeMyResult";
        internal const string MergeToResult = "MergeToResult";
        internal const string MergeError = "MergeError";
        internal const string MergeWarning = "MergeWarning";
        internal const string MergeVerbose = "MergeVerbose";
        internal const string MergeDebug = "MergeDebug";
        internal const string MergeInformation = "MergeInformation";
        internal const string Parameters = "Args";
        internal const string ParameterName = "N";
        internal const string ParameterValue = "V";

        internal const string NoInput = "NoInput";

        #endregion PowerShell

        #region StateInfo

        /// <summary>
        /// Name of property when Exception is serialized as error record.
        /// </summary>
        internal const string ExceptionAsErrorRecord = "ExceptionAsErrorRecord";
        /// <summary>
        /// Property used for encoding state of pipeline when serializing PipelineStateInfo.
        /// </summary>
        internal const string PipelineState = "PipelineState";
        /// <summary>
        /// Property used for encoding state of runspace when serializing RunspaceStateInfo.
        /// </summary>
        internal const string RunspaceState = "RunspaceState";

        #endregion StateInfo

        #region PSEventArgs

        /// <summary>
        /// Properties used for serialization of PSEventArgs.
        /// </summary>
        internal const string PSEventArgsComputerName = "PSEventArgs.ComputerName";
        internal const string PSEventArgsRunspaceId = "PSEventArgs.RunspaceId";
        internal const string PSEventArgsEventIdentifier = "PSEventArgs.EventIdentifier";
        internal const string PSEventArgsSourceIdentifier = "PSEventArgs.SourceIdentifier";
        internal const string PSEventArgsTimeGenerated = "PSEventArgs.TimeGenerated";
        internal const string PSEventArgsSender = "PSEventArgs.Sender";
        internal const string PSEventArgsSourceArgs = "PSEventArgs.SourceArgs";
        internal const string PSEventArgsMessageData = "PSEventArgs.MessageData";

        #endregion PSEventArgs

        #region RunspacePool

        internal const string MinRunspaces = "MinRunspaces";
        internal const string MaxRunspaces = "MaxRunspaces";
        internal const string ThreadOptions = "PSThreadOptions";
        internal const string HostInfo = "HostInfo";
        internal const string RunspacePoolOperationResponse = "SetMinMaxRunspacesResponse";
        internal const string AvailableRunspaces = "AvailableRunspaces";
        internal const string PublicKey = "PublicKey";
        internal const string EncryptedSessionKey = "EncryptedSessionKey";
        internal const string ApplicationArguments = "ApplicationArguments";
        internal const string ApplicationPrivateData = "ApplicationPrivateData";

        #endregion RunspacePool

        #region ProgressRecord

        internal const string ProgressRecord_Activity = "Activity";
        internal const string ProgressRecord_ActivityId = "ActivityId";
        internal const string ProgressRecord_CurrentOperation = "CurrentOperation";
        internal const string ProgressRecord_ParentActivityId = "ParentActivityId";
        internal const string ProgressRecord_PercentComplete = "PercentComplete";
        internal const string ProgressRecord_Type = "Type";
        internal const string ProgressRecord_SecondsRemaining = "SecondsRemaining";
        internal const string ProgressRecord_StatusDescription = "StatusDescription";

        #endregion
    }

    /// <summary>
    /// The destination of the remote message.
    /// </summary>
    [Flags]
    internal enum RemotingDestination : uint
    {
        InvalidDestination = 0x0,
        Client = 0x1,
        Server = 0x2,
        Listener = 0x4,
    }

    /// <summary>
    /// The layer the remoting message is being communicated between.
    /// </summary>
    /// <remarks>
    /// Please keep in sync with RemotingTargetInterface from
    /// C:\e\win7_powershell\admin\monad\nttargets\assemblies\logging\ETW\Manifests\Microsoft-Windows-PowerShell-Instrumentation.man
    /// </remarks>
    internal enum RemotingTargetInterface : int
    {
        InvalidTargetInterface = 0,
        Session = 1,
        RunspacePool = 2,
        PowerShell = 3,
    }

    /// <summary>
    /// The type of the remoting message.
    /// </summary>
    /// <remarks>
    /// Please keep in sync with RemotingDataType from
    /// C:\e\win7_powershell\admin\monad\nttargets\assemblies\logging\ETW\Manifests\Microsoft-Windows-PowerShell-Instrumentation.man
    /// </remarks>
    internal enum RemotingDataType : uint
    {
        InvalidDataType = 0,

        /// <summary>
        /// This data type is used when an Exception derived from IContainsErrorRecord
        /// is caught on server and is sent to client. This exception gets
        /// serialized as an error record. On the client this data type is deserialized in
        /// to an ErrorRecord.
        ///
        /// ErrorRecord on the client has an instance of RemoteException as exception.
        /// </summary>
        ExceptionAsErrorRecord = 1,

        // Session messages
        SessionCapability = 0x00010002,
        CloseSession = 0x00010003,
        CreateRunspacePool = 0x00010004,
        PublicKey = 0x00010005,
        EncryptedSessionKey = 0x00010006,
        PublicKeyRequest = 0x00010007,
        ConnectRunspacePool = 0x00010008,

        // Runspace Pool messages
        SetMaxRunspaces = 0x00021002,
        SetMinRunspaces = 0x00021003,
        RunspacePoolOperationResponse = 0x00021004,
        RunspacePoolStateInfo = 0x00021005,
        CreatePowerShell = 0x00021006,
        AvailableRunspaces = 0x00021007,
        PSEventArgs = 0x00021008,
        ApplicationPrivateData = 0x00021009,
        GetCommandMetadata = 0x0002100A,
        RunspacePoolInitData = 0x0002100B,
        ResetRunspaceState = 0x0002100C,

        // Runspace host messages
        RemoteHostCallUsingRunspaceHost = 0x00021100,
        RemoteRunspaceHostResponseData = 0x00021101,

        // PowerShell messages
        PowerShellInput = 0x00041002,
        PowerShellInputEnd = 0x00041003,
        PowerShellOutput = 0x00041004,
        PowerShellErrorRecord = 0x00041005,
        PowerShellStateInfo = 0x00041006,
        PowerShellDebug = 0x00041007,
        PowerShellVerbose = 0x00041008,
        PowerShellWarning = 0x00041009,
        PowerShellProgress = 0x00041010,
        PowerShellInformationStream = 0x00041011,
        StopPowerShell = 0x00041012,

        // PowerShell host messages
        RemoteHostCallUsingPowerShellHost = 0x00041100,
        RemotePowerShellHostResponseData = 0x00041101,
    }

    /// <summary>
    /// Converts C# types to PSObject properties for embedding in PSObjects transported across the wire.
    /// </summary>
    internal static class RemotingEncoder
    {
        #region NotePropertyHelpers

        internal delegate T ValueGetterDelegate<T>();

        internal static void AddNoteProperty<T>(PSObject pso, string propertyName, ValueGetterDelegate<T> valueGetter)
        {
            T value = default(T);
            try
            {
                value = valueGetter();
            }
            catch (Exception e)
            {
                Dbg.Assert(false, "Internal code shouldn't throw exceptions during serialization");

                PSEtwLog.LogAnalyticWarning(
                    PSEventId.Serializer_PropertyGetterFailed, PSOpcode.Exception, PSTask.Serialization,
                    PSKeyword.Serializer | PSKeyword.UseAlwaysAnalytic,
                    propertyName,
                    valueGetter.Target == null ? string.Empty : valueGetter.Target.GetType().FullName,
                    e.ToString(),
                    e.InnerException == null ? string.Empty : e.InnerException.ToString());
            }

            try
            {
                pso.Properties.Add(new PSNoteProperty(propertyName, value));
            }
            catch (ExtendedTypeSystemException)
            {
                // Member already exists, just make sure the value is the same.
                var existingValue = pso.Properties[propertyName].Value;
                Diagnostics.Assert(object.Equals(existingValue, value),
                                    "Property already exists but new value differs.");
            }
        }

        internal static PSObject CreateEmptyPSObject()
        {
            PSObject pso = new PSObject();
            // we don't care about serializing/deserializing TypeNames in remoting objects/messages
            // so we just omit TypeNames info to lower packet size and improve performance
            pso.InternalTypeNames = ConsolidatedString.Empty;

            return pso;
        }

        private static PSNoteProperty CreateHostInfoProperty(HostInfo hostInfo)
        {
            return new PSNoteProperty(
                RemoteDataNameStrings.HostInfo,
                RemoteHostEncoder.EncodeObject(hostInfo));
        }

        #endregion NotePropertyHelpers

        #region RunspacePool related

        /// <summary>
        /// This method generates a Remoting data structure handler message for
        /// creating a RunspacePool on the server.
        /// </summary>
        /// <param name="clientRunspacePoolId">Id of the clientRunspacePool.</param>
        /// <param name="minRunspaces">minRunspaces for the RunspacePool
        /// to be created at the server</param>
        /// <param name="maxRunspaces">maxRunspaces for the RunspacePool
        /// to be created at the server</param>
        /// <param name="runspacePool">Local runspace pool.</param>
        /// <param name="host">host for the runspacepool at the client end
        /// from this host, information will be extracted and sent to
        /// server</param>
        /// <param name="applicationArguments">
        /// Application arguments the server can see in <see cref="System.Management.Automation.Remoting.PSSenderInfo.ApplicationArguments"/>
        /// </param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data      |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | S |  Session  | CRPID  |    0    | CreateRuns | minRunspaces,  |   InvalidDataType   |
        /// |   |           |        |         | pacePool   | maxRunspaces,  |                     |
        /// |   |           |        |         |            | threadOptions, |                     |
        /// |   |           |        |         |            | apartmentState,|                     |
        /// |   |           |        |         |            | hostInfo       |                     |
        /// |   |           |        |         |            | appParameters  |                     |
        /// --------------------------------------------------------------------------------------
        ///
        ///
        internal static RemoteDataObject GenerateCreateRunspacePool(
            Guid clientRunspacePoolId,
            int minRunspaces,
            int maxRunspaces,
            RemoteRunspacePoolInternal runspacePool,
            PSHost host,
            PSPrimitiveDictionary applicationArguments)
        {
            PSObject dataAsPSObject = CreateEmptyPSObject();
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MinRunspaces, minRunspaces));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MaxRunspaces, maxRunspaces));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ThreadOptions, runspacePool.ThreadOptions));
            ApartmentState poolState = runspacePool.ApartmentState;
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ApartmentState, poolState));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ApplicationArguments, applicationArguments));

            // a runspace's host info always needs to be cached. This is because
            // at a later point in time, a powershell may choose to use the
            // runspace's host and may require that it uses cached Raw UI properties
            dataAsPSObject.Properties.Add(CreateHostInfoProperty(new HostInfo(host)));

            return RemoteDataObject.CreateFrom(RemotingDestination.Server,
                                            RemotingDataType.CreateRunspacePool,
                                            clientRunspacePoolId,
                                            Guid.Empty,
                                            dataAsPSObject);
        }

        /// <summary>
        /// This method generates a Remoting data structure handler message for
        /// creating a RunspacePool on the server.
        /// </summary>
        /// <param name="clientRunspacePoolId">Id of the clientRunspacePool.</param>
        /// <param name="minRunspaces">minRunspaces for the RunspacePool
        /// to be created at the server</param>
        /// <param name="maxRunspaces">maxRunspaces for the RunspacePool
        /// to be created at the server</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data      |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | S |  Runspace | CRPID  |    0    | ConnectRun | minRunspaces,  |   InvalidDataType   |
        /// |   |           |        |         | spacePool  | maxRunspaces,  |                     |
        /// |   |           |        |         |            |                |                     |
        /// --------------------------------------------------------------------------------------
        ///
        ///
        internal static RemoteDataObject GenerateConnectRunspacePool(
            Guid clientRunspacePoolId,
            int minRunspaces,
            int maxRunspaces)
        {
            PSObject dataAsPSObject = CreateEmptyPSObject();
            int propertyCount = 0;
            if (minRunspaces != -1)
            {
                dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MinRunspaces, minRunspaces));
                propertyCount++;
            }

            if (maxRunspaces != -1)
            {
                dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MaxRunspaces, maxRunspaces));
                propertyCount++;
            }

            if (propertyCount > 0)
            {
                return RemoteDataObject.CreateFrom(RemotingDestination.Server,
                                            RemotingDataType.ConnectRunspacePool,
                                            clientRunspacePoolId,
                                            Guid.Empty,
                                            dataAsPSObject);
            }
            else
            {
                return RemoteDataObject.CreateFrom(RemotingDestination.Server,
                                            RemotingDataType.ConnectRunspacePool,
                                            clientRunspacePoolId,
                                            Guid.Empty,
                                            string.Empty);
            }
        }

        /// <summary>
        /// Generates a response message to ConnectRunspace that includes
        /// sufficient information to construction client RunspacePool state.
        /// </summary>
        /// <param name="runspacePoolId">Id of the clientRunspacePool.</param>
        /// <param name="minRunspaces">minRunspaces for the RunspacePool
        /// to be created at the server</param>
        /// <param name="maxRunspaces">maxRunspaces for the RunspacePool
        /// to be created at the server</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data      |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | C |  Runspace | CRPID  |    0    | RunspacePo | minRunspaces,  |   InvalidDataType   |
        /// |   |           |        |         | olInitData | maxRunspaces,  |                     |
        /// |   |           |        |         |            |                |                     |
        /// --------------------------------------------------------------------------------------
        ///
        ///
        internal static RemoteDataObject GenerateRunspacePoolInitData(
            Guid runspacePoolId,
            int minRunspaces,
            int maxRunspaces)
        {
            PSObject dataAsPSObject = CreateEmptyPSObject();
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MinRunspaces, minRunspaces));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MaxRunspaces, maxRunspaces));
            return RemoteDataObject.CreateFrom(RemotingDestination.Client,
                                            RemotingDataType.RunspacePoolInitData,
                                            runspacePoolId,
                                            Guid.Empty,
                                            dataAsPSObject);
        }

        /// <summary>
        /// This method generates a Remoting data structure handler message for
        /// modifying the maxrunspaces of the specified runspace pool on the server.
        /// </summary>
        /// <param name="clientRunspacePoolId">Id of the clientRunspacePool.</param>
        /// <param name="maxRunspaces">new value of maxRunspaces for the
        /// specified RunspacePool  </param>
        /// <param name="callId">Call id of the call at client.</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data     |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | S | Runspace  | CRPID  |    0    |   SetMax   | maxRunspaces  |   InvalidDataType   |
        /// |   |   Pool    |        |         |  Runspaces |               |                     |
        /// |   |           |        |         |            |               |                     |
        /// --------------------------------------------------------------------------------------
        internal static RemoteDataObject GenerateSetMaxRunspaces(Guid clientRunspacePoolId,
                                    int maxRunspaces, long callId)
        {
            PSObject dataAsPSObject = CreateEmptyPSObject();
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MaxRunspaces, maxRunspaces));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.CallId, callId));

            return RemoteDataObject.CreateFrom(RemotingDestination.Server,
                                               RemotingDataType.SetMaxRunspaces,
                                               clientRunspacePoolId,
                                               Guid.Empty,
                                               dataAsPSObject);
        }

        /// <summary>
        /// This method generates a Remoting data structure handler message for
        /// modifying the maxrunspaces of the specified runspace pool on the server.
        /// </summary>
        /// <param name="clientRunspacePoolId">Id of the clientRunspacePool.</param>
        /// <param name="minRunspaces">new value of minRunspaces for the
        /// specified RunspacePool  </param>
        /// <param name="callId">Call id of the call at client.</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data     |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | S | Runspace  | CRPID  |    0    |   SetMin   | minRunspaces  |   InvalidDataType   |
        /// |   |   Pool    |        |         |  Runspaces |               |                     |
        /// |   |           |        |         |            |               |                     |
        /// --------------------------------------------------------------------------------------
        internal static RemoteDataObject GenerateSetMinRunspaces(Guid clientRunspacePoolId,
                                    int minRunspaces, long callId)
        {
            PSObject dataAsPSObject = CreateEmptyPSObject();
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MinRunspaces, minRunspaces));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.CallId, callId));

            return RemoteDataObject.CreateFrom(RemotingDestination.Server,
                                               RemotingDataType.SetMinRunspaces,
                                               clientRunspacePoolId,
                                               Guid.Empty,
                                               dataAsPSObject);
        }

        /// <summary>
        /// This method generates a Remoting data structure handler message for
        /// that contains a response to SetMaxRunspaces or SetMinRunspaces.
        /// </summary>
        /// <param name="clientRunspacePoolId">Id of the clientRunspacePool.</param>
        /// <param name="callId">Call id of the call at client.</param>
        /// <param name="response">Response to the call.</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data     |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | S | Runspace  | CRPID  |    0    |   SetMax   | maxRunspaces  |   InvalidDataType   |
        /// |   |   Pool    |        |         |  Runspaces |               |                     |
        /// |   |           |        |         |            |               |                     |
        /// --------------------------------------------------------------------------------------
        internal static RemoteDataObject GenerateRunspacePoolOperationResponse(Guid clientRunspacePoolId,
                                    object response, long callId)
        {
            PSObject dataAsPSObject = CreateEmptyPSObject();
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.RunspacePoolOperationResponse, response));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.CallId, callId));

            return RemoteDataObject.CreateFrom(RemotingDestination.Client,
                                               RemotingDataType.RunspacePoolOperationResponse,
                                               clientRunspacePoolId,
                                               Guid.Empty,
                                               dataAsPSObject);
        }

        /// <summary>
        /// This method generates a Remoting data structure handler message for
        /// getting the available runspaces on the server.
        /// </summary>
        /// <param name="clientRunspacePoolId">guid of the runspace pool on which
        /// this needs to be queried</param>
        /// <param name="callId">Call id of the call at the client.</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |      Data     |        Type          |
        /// ---------------------------------------------------------------------------
        /// | S | Runspace  | CRPID  |    0    |     null      |GetAvailableRunspaces |
        /// |   |   Pool    |        |         |               |                      |
        /// --------------------------------------------------------------------------
        internal static RemoteDataObject GenerateGetAvailableRunspaces(Guid clientRunspacePoolId,
                                    long callId)
        {
            PSObject dataAsPSObject = CreateEmptyPSObject();
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.CallId, callId));

            return RemoteDataObject.CreateFrom(RemotingDestination.Server,
                                               RemotingDataType.AvailableRunspaces,
                                               clientRunspacePoolId,
                                               Guid.Empty,
                                               dataAsPSObject);
        }

        /// <summary>
        /// This method generates a remoting data structure handler message for
        /// transferring a roles public key to the other side.
        /// </summary>
        /// <param name="runspacePoolId">Runspace pool id.</param>
        /// <param name="publicKey">Public key to send across.</param>
        /// <param name="destination">destination that this message is
        /// targeted to</param>
        /// <returns>Data structure message.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |      Data     |        Type          |
        /// ---------------------------------------------------------------------------
        /// | S | Runspace  | CRPID  |    0    |    public     |      PublicKey       |
        /// |   |   Pool    |        |         |     key       |                      |
        /// --------------------------------------------------------------------------
        internal static RemoteDataObject GenerateMyPublicKey(Guid runspacePoolId,
                                    string publicKey, RemotingDestination destination)
        {
            PSObject dataAsPSObject = CreateEmptyPSObject();
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.PublicKey, publicKey));

            return RemoteDataObject.CreateFrom(destination,
                                               RemotingDataType.PublicKey,
                                               runspacePoolId,
                                               Guid.Empty,
                                               dataAsPSObject);
        }

        /// <summary>
        /// This method generates a remoting data structure handler message for
        /// requesting a public key from the client to the server.
        /// </summary>
        /// <param name="runspacePoolId">Runspace pool id.</param>
        /// <returns>Data structure message.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |      Data     |        Type          |
        /// ---------------------------------------------------------------------------
        /// | S | Runspace  | CRPID  |    0    |               |   PublicKeyRequest   |
        /// |   |   Pool    |        |         |               |                      |
        /// --------------------------------------------------------------------------
        internal static RemoteDataObject GeneratePublicKeyRequest(Guid runspacePoolId)
        {
            return RemoteDataObject.CreateFrom(RemotingDestination.Client,
                                               RemotingDataType.PublicKeyRequest,
                                               runspacePoolId,
                                               Guid.Empty,
                                               string.Empty);
        }

        /// <summary>
        /// This method generates a remoting data structure handler message for
        /// sending an encrypted session key to the client.
        /// </summary>
        /// <param name="runspacePoolId">Runspace pool id.</param>
        /// <param name="encryptedSessionKey">Encrypted session key.</param>
        /// <returns>Data structure message.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |      Data     |        Type          |
        /// ---------------------------------------------------------------------------
        /// | S | Runspace  | CRPID  |    0    |  encrypted    | EncryptedSessionKey  |
        /// |   |   Pool    |        |         | session key   |                      |
        /// --------------------------------------------------------------------------
        internal static RemoteDataObject GenerateEncryptedSessionKeyResponse(Guid runspacePoolId,
            string encryptedSessionKey)
        {
            PSObject dataAsPSObject = CreateEmptyPSObject();
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.EncryptedSessionKey,
                                            encryptedSessionKey));

            return RemoteDataObject.CreateFrom(RemotingDestination.Client,
                                               RemotingDataType.EncryptedSessionKey,
                                               runspacePoolId,
                                               Guid.Empty,
                                               dataAsPSObject);
        }

        /// <summary>
        /// This methods generates a Remoting data structure handler message for
        /// creating a command discovery pipeline on the server.
        /// </summary>
        /// <param name="shell">The client remote powershell from which the
        /// message needs to be generated.
        /// The data is extracted from parameters of the first command named "Get-Command".
        /// </param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// -------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |     Data      |        Type         |
        /// --------------------------------------------------------------------------
        /// | S |  Runspace | CRPID  |  CPID   | name,         | GetCommandMetadata  |
        /// |   |  Pool     |        |         | commandType,  |                     |
        /// |   |           |        |         | module,FQM,   |                     |
        /// |   |           |        |         | argumentList  |                     |
        /// --------------------------------------------------------------------------
        ///
        internal static RemoteDataObject GenerateGetCommandMetadata(ClientRemotePowerShell shell)
        {
            Command getCommand = null;
            foreach (Command c in shell.PowerShell.Commands.Commands)
            {
                if (c.CommandText.Equals("Get-Command", StringComparison.OrdinalIgnoreCase))
                {
                    getCommand = c;
                    break;
                }
            }

            Dbg.Assert(getCommand != null, "Whoever sets PowerShell.IsGetCommandMetadataSpecialPipeline needs to make sure Get-Command is present");

            string[] name = null;
            CommandTypes commandTypes = CommandTypes.Alias | CommandTypes.Cmdlet | CommandTypes.Function | CommandTypes.Filter | CommandTypes.Configuration;
            string[] module = null;
            ModuleSpecification[] fullyQualifiedModule = null;

            object[] argumentList = null;
            foreach (CommandParameter p in getCommand.Parameters)
            {
                if (p.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
                {
                    name = (string[])LanguagePrimitives.ConvertTo(p.Value, typeof(string[]), CultureInfo.InvariantCulture);
                }
                else if (p.Name.Equals("CommandType", StringComparison.OrdinalIgnoreCase))
                {
                    commandTypes = (CommandTypes)LanguagePrimitives.ConvertTo(p.Value, typeof(CommandTypes), CultureInfo.InvariantCulture);
                }
                else if (p.Name.Equals("Module", StringComparison.OrdinalIgnoreCase))
                {
                    module = (string[])LanguagePrimitives.ConvertTo(p.Value, typeof(string[]), CultureInfo.InvariantCulture);
                }
                else if (p.Name.Equals("FullyQualifiedModule", StringComparison.OrdinalIgnoreCase))
                {
                    fullyQualifiedModule = (ModuleSpecification[])LanguagePrimitives.ConvertTo(p.Value, typeof(ModuleSpecification[]), CultureInfo.InvariantCulture);
                }
                else if (p.Name.Equals("ArgumentList", StringComparison.OrdinalIgnoreCase))
                {
                    argumentList = (object[])LanguagePrimitives.ConvertTo(p.Value, typeof(object[]), CultureInfo.InvariantCulture);
                }
            }

            RunspacePool rsPool = shell.PowerShell.GetRunspaceConnection() as RunspacePool;
            Dbg.Assert(rsPool != null, "Runspacepool cannot be null for a CreatePowerShell request");
            Guid clientRunspacePoolId = rsPool.InstanceId;

            PSObject dataAsPSObject = CreateEmptyPSObject();
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.DiscoveryName, name));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.DiscoveryType, commandTypes));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.DiscoveryModule, module));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.DiscoveryFullyQualifiedModule, fullyQualifiedModule));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.DiscoveryArgumentList, argumentList));

            return RemoteDataObject.CreateFrom(RemotingDestination.Server,
                                               RemotingDataType.GetCommandMetadata,
                                               clientRunspacePoolId,
                                               shell.InstanceId,
                                               dataAsPSObject);
        }

        /// <summary>
        /// This methods generates a Remoting data structure handler message for
        /// creating a PowerShell on the server.
        /// </summary>
        /// <param name="shell">The client remote powershell from which the
        /// create powershell message needs to be generated</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// -------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |     Data      |        Type         |
        /// --------------------------------------------------------------------------
        /// | S |  Runspace | CRPID  |  CPID   | serialized    | CreatePowerShell    |
        /// |   |  Pool     |        |         | powershell,   |                     |
        /// |   |           |        |         | noInput,      |                     |
        /// |   |           |        |         | hostInfo,     |                     |
        /// |   |           |        |         | invocationset |                     |
        /// |   |           |        |         | tings, stream |                     |
        /// |   |           |        |         | options       |                     |
        /// --------------------------------------------------------------------------
        ///
        internal static RemoteDataObject GenerateCreatePowerShell(ClientRemotePowerShell shell)
        {
            PowerShell powerShell = shell.PowerShell;
            PSInvocationSettings settings = shell.Settings;

            PSObject dataAsPSObject = CreateEmptyPSObject();
            Guid clientRunspacePoolId = Guid.Empty;
            HostInfo hostInfo;
            PSNoteProperty hostInfoProperty;

            RunspacePool rsPool = powerShell.GetRunspaceConnection() as RunspacePool;

            Dbg.Assert(rsPool != null, "Runspacepool cannot be null for a CreatePowerShell request");

            clientRunspacePoolId = rsPool.InstanceId;

            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.PowerShell, powerShell.ToPSObjectForRemoting()));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.NoInput, shell.NoInput));

            if (settings == null)
            {
                hostInfo = new HostInfo(null);
                hostInfo.UseRunspaceHost = true;

                ApartmentState passedApartmentState = rsPool.ApartmentState;
                dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ApartmentState, passedApartmentState));
                dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.RemoteStreamOptions, RemoteStreamOptions.AddInvocationInfo));
                dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.AddToHistory, false));
            }
            else
            {
                hostInfo = new HostInfo(settings.Host);
                if (settings.Host == null)
                {
                    hostInfo.UseRunspaceHost = true;
                }

                ApartmentState passedApartmentState = settings.ApartmentState;
                dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ApartmentState, passedApartmentState));
                dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.RemoteStreamOptions, settings.RemoteStreamOptions));
                dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.AddToHistory, settings.AddToHistory));
            }

            hostInfoProperty = CreateHostInfoProperty(hostInfo);
            dataAsPSObject.Properties.Add(hostInfoProperty);
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.IsNested, shell.PowerShell.IsNested));
            return RemoteDataObject.CreateFrom(RemotingDestination.Server,
                                               RemotingDataType.CreatePowerShell,
                                               clientRunspacePoolId,
                                               shell.InstanceId,
                                               dataAsPSObject);
        }

        /// <summary>
        /// This method creates a remoting data structure handler message for transporting
        /// application private data from server to client.
        /// </summary>
        /// <param name="clientRunspacePoolId">Id of the client RunspacePool.</param>
        /// <param name="applicationPrivateData">Application private data.</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data     |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | C |  Runspace | CRPID  |   -1    |    Data    | appl. private | PSPrimitive         |
        /// |   |    Pool   |        |         |            | data          |           Dictionary|
        /// --------------------------------------------------------------------------------------
        internal static RemoteDataObject GenerateApplicationPrivateData(
                                    Guid clientRunspacePoolId,
                                    PSPrimitiveDictionary applicationPrivateData)
        {
            PSObject dataAsPSObject = CreateEmptyPSObject();

            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ApplicationPrivateData, applicationPrivateData));

            return RemoteDataObject.CreateFrom(RemotingDestination.Client,
                                               RemotingDataType.ApplicationPrivateData,
                                               clientRunspacePoolId,
                                               Guid.Empty,
                                               dataAsPSObject);
        }

        /// <summary>
        /// This method creates a remoting data structure handler message for transporting a state
        /// information from server to client.
        /// </summary>
        /// <param name="clientRunspacePoolId">Id of the client RunspacePool.</param>
        /// <param name="stateInfo">State information object.</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data     |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | C |  Runspace | CRPID  |   -1    |    Data    | RunspacePool  | RunspacePoolState   |
        /// |   |    Pool   |        |         |            | StateInfo     | Info                |
        /// --------------------------------------------------------------------------------------
        internal static RemoteDataObject GenerateRunspacePoolStateInfo(
                                    Guid clientRunspacePoolId,
                                    RunspacePoolStateInfo stateInfo)
        {
            // BUGBUG: This object creation needs to be relooked
            PSObject dataAsPSObject = CreateEmptyPSObject();

            // Add State Property
            PSNoteProperty stateProperty =
                        new PSNoteProperty(RemoteDataNameStrings.RunspaceState,
                            (int)(stateInfo.State));
            dataAsPSObject.Properties.Add(stateProperty);

            // Add Reason property
            if (stateInfo.Reason != null)
            {
                PSNoteProperty exceptionProperty = GetExceptionProperty(
                    exception: stateInfo.Reason,
                    errorId: "RemoteRunspaceStateInfoReason",
                    category: ErrorCategory.NotSpecified);
                dataAsPSObject.Properties.Add(exceptionProperty);
            }

            return RemoteDataObject.CreateFrom(RemotingDestination.Client,
                                               RemotingDataType.RunspacePoolStateInfo,
                                               clientRunspacePoolId,
                                               Guid.Empty,
                                               dataAsPSObject);
        }

        /// <summary>
        /// This method creates a remoting data structure handler message for transporting a PowerShell
        /// event from server to client.
        /// </summary>
        /// <param name="clientRunspacePoolId">Id of the client RunspacePool.</param>
        /// <param name="e">PowerShell event.</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data     |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | C |  Runspace | CRPID  |   -1    |    Data    | RunspacePool  | PSEventArgs         |
        /// |   |    Pool   |        |         |            | StateInfo     |                     |
        /// --------------------------------------------------------------------------------------
        internal static RemoteDataObject GeneratePSEventArgs(Guid clientRunspacePoolId, PSEventArgs e)
        {
            PSObject dataAsPSObject = CreateEmptyPSObject();

            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.PSEventArgsEventIdentifier, e.EventIdentifier));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.PSEventArgsSourceIdentifier, e.SourceIdentifier));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.PSEventArgsTimeGenerated, e.TimeGenerated));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.PSEventArgsSender, e.Sender));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.PSEventArgsSourceArgs, e.SourceArgs));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.PSEventArgsMessageData, e.MessageData));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.PSEventArgsComputerName, e.ComputerName));
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.PSEventArgsRunspaceId, e.RunspaceId));

            return RemoteDataObject.CreateFrom(RemotingDestination.Client,
                                               RemotingDataType.PSEventArgs,
                                               clientRunspacePoolId,
                                               Guid.Empty,
                                               dataAsPSObject);
        }

        /// <summary>
        /// This method creates a remoting data structure handler message to instruct the server to reset
        /// the single runspace on the server.
        /// </summary>
        /// <param name="clientRunspacePoolId"></param>
        /// <param name="callId">Caller Id.</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action          |      Data     |        Type        |
        /// --------------------------------------------------------------------------------------------
        /// | S |  Runspace | CRPID  |   -1    |  Reset server     |     None      | ResetRunspaceState |
        /// |   |    Pool   |        |         |  runspace state   |               |                    |
        /// ---------------------------------------------------------------------------------------------
        internal static RemoteDataObject GenerateResetRunspaceState(Guid clientRunspacePoolId, long callId)
        {
            PSObject dataAsPSObject = CreateEmptyPSObject();
            dataAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.CallId, callId));

            return RemoteDataObject.CreateFrom(RemotingDestination.Server,
                                               RemotingDataType.ResetRunspaceState,
                                               clientRunspacePoolId,
                                               Guid.Empty,
                                               dataAsPSObject);
        }

        /// <summary>
        /// Returns the PS remoting protocol version associated with the provided.
        /// </summary>
        /// <param name="rsPool">RunspacePool.</param>
        /// <returns>PS remoting protocol version.</returns>
        internal static Version GetPSRemotingProtocolVersion(RunspacePool rsPool)
        {
            return (rsPool != null && rsPool.RemoteRunspacePoolInternal != null) ?
                rsPool.RemoteRunspacePoolInternal.PSRemotingProtocolVersion : null;
        }

        #endregion RunspacePool related

        #region PowerShell related

        /// <summary>
        /// This method creates a remoting data structure handler message for sending a powershell
        /// input data from the client to the server.
        /// </summary>
        /// <param name="data">Input data to send.</param>
        /// <param name="clientRemoteRunspacePoolId">Client runspace pool id.</param>
        /// <param name="clientPowerShellId">Client powershell id.</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data     |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | S |PowerShell | CRPID  |   CPID  |    Data    |  input data   |   PowerShellInput   |
        /// |   |           |        |         |            |               |                     |
        /// --------------------------------------------------------------------------------------
        internal static RemoteDataObject GeneratePowerShellInput(object data, Guid clientRemoteRunspacePoolId,
            Guid clientPowerShellId)
        {
            return RemoteDataObject.CreateFrom(RemotingDestination.Server,
                                               RemotingDataType.PowerShellInput,
                                               clientRemoteRunspacePoolId,
                                               clientPowerShellId,
                                               data);
        }

        /// <summary>
        /// This method creates a remoting data structure handler message for signalling
        /// end of input data for powershell.
        /// </summary>
        /// <param name="clientRemoteRunspacePoolId">Client runspace pool id.</param>
        /// <param name="clientPowerShellId">Client powershell id.</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data     |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | S |PowerShell | CRPID  |   CPID  |    Data    | bool.         | PowerShellInputEnd  |
        /// |   |           |        |         |            | TrueString    |                     |
        /// --------------------------------------------------------------------------------------
        internal static RemoteDataObject GeneratePowerShellInputEnd(Guid clientRemoteRunspacePoolId,
            Guid clientPowerShellId)
        {
            return RemoteDataObject.CreateFrom(RemotingDestination.Server,
                                               RemotingDataType.PowerShellInputEnd,
                                               clientRemoteRunspacePoolId,
                                               clientPowerShellId,
                                               null);
        }

        /// <summary>
        /// This method creates a remoting data structure handler message for transporting a
        /// powershell output data from server to client.
        /// </summary>
        /// <param name="data">Data to be sent.</param>
        /// <param name="clientPowerShellId">id of client powershell
        /// to which this information need to be delivered</param>
        /// <param name="clientRunspacePoolId">id of client runspacepool
        /// associated with this powershell</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data     |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | C |PowerShell |  CRPID |  CPID   |    Data    | data to send  |  PowerShellOutput   |
        /// |   |           |        |         |            |               |                     |
        /// --------------------------------------------------------------------------------------
        internal static RemoteDataObject GeneratePowerShellOutput(PSObject data, Guid clientPowerShellId,
            Guid clientRunspacePoolId)
        {
            return RemoteDataObject.CreateFrom(RemotingDestination.Client,
                                               RemotingDataType.PowerShellOutput,
                                               clientRunspacePoolId,
                                               clientPowerShellId,
                                               data);
        }

        /// <summary>
        /// This method creates a remoting data structure handler message for transporting a
        /// powershell informational message (debug/verbose/warning/progress)from
        /// server to client.
        /// </summary>
        /// <param name="data">Data to be sent.</param>
        /// <param name="clientPowerShellId">id of client powershell
        /// to which this information need to be delivered</param>
        /// <param name="clientRunspacePoolId">id of client runspacepool
        /// associated with this powershell</param>
        /// <param name="dataType">data type of this informational
        /// message</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data     |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | C |PowerShell |  CRPID |  CPID   |    Data    | data to send  | DataType - debug,   |
        /// |   |           |        |         |            |               | verbose, warning    |
        /// --------------------------------------------------------------------------------------
        internal static RemoteDataObject GeneratePowerShellInformational(object data,
            Guid clientRunspacePoolId, Guid clientPowerShellId, RemotingDataType dataType)
        {
            return RemoteDataObject.CreateFrom(RemotingDestination.Client,
                                               dataType,
                                               clientRunspacePoolId,
                                               clientPowerShellId,
                                               PSObject.AsPSObject(data));
        }

        /// <summary>
        /// This method creates a remoting data structure handler message for transporting a
        /// powershell progress message from
        /// server to client.
        /// </summary>
        /// <param name="progressRecord">Progress record to send.</param>
        /// <param name="clientPowerShellId">id of client powershell
        /// to which this information need to be delivered</param>
        /// <param name="clientRunspacePoolId">id of client runspacepool
        /// associated with this powershell</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data     |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | C |PowerShell |  CRPID |  CPID   |    Data    | progress      | PowerShellProgress  |
        /// |   |           |        |         |            |   message     |                     |
        /// --------------------------------------------------------------------------------------
        internal static RemoteDataObject GeneratePowerShellInformational(ProgressRecord progressRecord,
            Guid clientRunspacePoolId, Guid clientPowerShellId)
        {
            if (progressRecord == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(progressRecord));
            }

            return RemoteDataObject.CreateFrom(RemotingDestination.Client,
                                               RemotingDataType.PowerShellProgress,
                                               clientRunspacePoolId,
                                               clientPowerShellId,
                                               progressRecord.ToPSObjectForRemoting());
        }

        /// <summary>
        /// This method creates a remoting data structure handler message for transporting a
        /// powershell information stream message from
        /// server to client.
        /// </summary>
        /// <param name="informationRecord">Information record to send.</param>
        /// <param name="clientPowerShellId">id of client powershell
        /// to which this information need to be delivered</param>
        /// <param name="clientRunspacePoolId">id of client runspacepool
        /// associated with this powershell</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// -----------------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data     |        Type                 |
        /// -----------------------------------------------------------------------------------------------
        /// | C |PowerShell |  CRPID |  CPID   |    Data    | information   | PowerShellInformationStream |
        /// |   |           |        |         |            |   message     |                             |
        /// -----------------------------------------------------------------------------------------------
        internal static RemoteDataObject GeneratePowerShellInformational(InformationRecord informationRecord,
            Guid clientRunspacePoolId, Guid clientPowerShellId)
        {
            if (informationRecord == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(informationRecord));
            }

            return RemoteDataObject.CreateFrom(RemotingDestination.Client,
                                               RemotingDataType.PowerShellInformationStream,
                                               clientRunspacePoolId,
                                               clientPowerShellId,
                                               informationRecord.ToPSObjectForRemoting());
        }

        /// <summary>
        /// This method creates a remoting data structure handler message for transporting a
        /// powershell error record from server to client.
        /// </summary>
        /// <param name="errorRecord">Error record to be sent.</param>
        /// <param name="clientPowerShellId">id of client powershell
        /// to which this information need to be delivered</param>
        /// <param name="clientRunspacePoolId">id of client runspacepool
        /// associated with this powershell</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data     |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | C |PowerShell |  CRPID |  CPID   |    Data    | error record  |   PowerShellError   |
        /// |   |           |        |         |            |    to send    |                     |
        /// --------------------------------------------------------------------------------------
        internal static RemoteDataObject GeneratePowerShellError(object errorRecord,
            Guid clientRunspacePoolId, Guid clientPowerShellId)
        {
            return RemoteDataObject.CreateFrom(RemotingDestination.Client,
                                               RemotingDataType.PowerShellErrorRecord,
                                               clientRunspacePoolId,
                                               clientPowerShellId,
                                               PSObject.AsPSObject(errorRecord));
        }

        /// <summary>
        /// This method creates a remoting data structure handler message for transporting a
        /// powershell state information from server to client.
        /// </summary>
        /// <param name="stateInfo">State information object.</param>
        /// <param name="clientPowerShellId">id of client powershell
        /// to which this information need to be delivered</param>
        /// <param name="clientRunspacePoolId">id of client runspacepool
        /// associated with this powershell</param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data     |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | C |PowerShell |  CRPID |  CPID   |    Data    | PSInvocation  | PowerShellStateInfo |
        /// |   |           |        |         |            | StateInfo     |                     |
        /// --------------------------------------------------------------------------------------
        internal static RemoteDataObject GeneratePowerShellStateInfo(PSInvocationStateInfo stateInfo,
            Guid clientPowerShellId, Guid clientRunspacePoolId)
        {
            // Encode Pipeline StateInfo as PSObject
            PSObject dataAsPSObject = CreateEmptyPSObject();

            // Convert the state to int and add as property
            PSNoteProperty stateProperty = new PSNoteProperty(
                RemoteDataNameStrings.PipelineState, (int)(stateInfo.State));
            dataAsPSObject.Properties.Add(stateProperty);

            // Add exception property
            if (stateInfo.Reason != null)
            {
                PSNoteProperty exceptionProperty = GetExceptionProperty(
                    exception: stateInfo.Reason,
                    errorId: "RemotePSInvocationStateInfoReason",
                    category: ErrorCategory.NotSpecified);
                dataAsPSObject.Properties.Add(exceptionProperty);
            }

            return RemoteDataObject.CreateFrom(RemotingDestination.Client,
                                               RemotingDataType.PowerShellStateInfo,
                                               clientRunspacePoolId,
                                               clientPowerShellId,
                                               dataAsPSObject);
        }

        #endregion PowerShell related

        #region Exception

        /// <summary>
        /// Gets the error record from exception of type IContainsErrorRecord.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns>
        /// ErrorRecord if exception is of type IContainsErrorRecord
        /// Null if exception is not of type IContainsErrorRecord
        /// </returns>
        internal static ErrorRecord GetErrorRecordFromException(Exception exception)
        {
            Dbg.Assert(exception != null, "Caller should validate the data");

            ErrorRecord er = null;
            IContainsErrorRecord cer = exception as IContainsErrorRecord;
            if (cer != null)
            {
                er = cer.ErrorRecord;
                // Exception inside the error record is ParentContainsErrorRecordException which
                // doesn't have stack trace. Replace it with top level exception.
                er = new ErrorRecord(er, exception);
            }

            return er;
        }

        /// <summary>
        /// Gets a Note Property for the exception.
        /// </summary>
        /// <remarks>
        /// If <paramref name="exception"/> is of not type IContainsErrorRecord, a new ErrorRecord is created.
        /// </remarks>
        /// <param name="exception"></param>
        /// <param name="errorId">ErrorId to use if exception is not of type IContainsErrorRecord.</param>
        /// <param name="category">ErrorCategory to use if exception is not of type IContainsErrorRecord.</param>
        /// <returns></returns>
        private static PSNoteProperty GetExceptionProperty(Exception exception, string errorId, ErrorCategory category)
        {
            Dbg.Assert(exception != null, "Caller should validate the data");

            ErrorRecord er = GetErrorRecordFromException(exception) ??
                             new ErrorRecord(exception, errorId, category, null);
            return new PSNoteProperty(RemoteDataNameStrings.ExceptionAsErrorRecord, er);
        }

        #endregion Exception

        #region Session related

        /// <summary>
        /// This method creates a remoting data structure handler message for transporting a session
        /// capability message. Should be used by client.
        /// </summary>
        /// <param name="capability">RemoteSession capability object to encode.</param>
        /// <param name="runspacePoolId"></param>
        /// <returns>Data structure handler message encoded as RemoteDataObject.</returns>
        /// The message format is as under for this message
        /// --------------------------------------------------------------------------------------
        /// | D |    TI     |  RPID  |   PID   |   Action   |      Data     |        Type         |
        /// --------------------------------------------------------------------------------------
        /// | C |  Session  |  RPID  |  Empty  |    Data    |    session    | SessionCapability   |
        /// | / |           |        |         |            |   capability  |                     |
        /// | S |           |        |         |            |               |                     |
        /// --------------------------------------------------------------------------------------
        internal static RemoteDataObject GenerateClientSessionCapability(RemoteSessionCapability capability,
                Guid runspacePoolId)
        {
            PSObject temp = GenerateSessionCapability(capability);
            return RemoteDataObject.CreateFrom(capability.RemotingDestination,
                RemotingDataType.SessionCapability, runspacePoolId, Guid.Empty, temp);
        }

        internal static RemoteDataObject GenerateServerSessionCapability(RemoteSessionCapability capability,
            Guid runspacePoolId)
        {
            PSObject temp = GenerateSessionCapability(capability);
            return RemoteDataObject.CreateFrom(capability.RemotingDestination,
                RemotingDataType.SessionCapability, runspacePoolId, Guid.Empty, temp);
        }

        private static PSObject GenerateSessionCapability(RemoteSessionCapability capability)
        {
            PSObject temp = CreateEmptyPSObject();
            temp.Properties.Add(
                new PSNoteProperty(RemoteDataNameStrings.PS_STARTUP_PROTOCOL_VERSION_NAME, capability.ProtocolVersion));
            temp.Properties.Add(
                new PSNoteProperty(RemoteDataNameStrings.PSVersion, capability.PSVersion));
            temp.Properties.Add(
                new PSNoteProperty(RemoteDataNameStrings.SerializationVersion, capability.SerializationVersion));
            return temp;
        }

        #endregion Session related
    }

    /// <summary>
    /// Converts fields of PSObjects containing remoting messages to C# types.
    /// </summary>
    internal static class RemotingDecoder
    {
        private static T ConvertPropertyValueTo<T>(string propertyName, object propertyValue)
        {
            if (propertyName == null) // comes from internal caller
            {
                throw PSTraceSource.NewArgumentNullException(nameof(propertyName));
            }

            if (typeof(T).IsEnum)
            {
                if (propertyValue is string)
                {
                    try
                    {
                        string stringValue = (string)propertyValue;
                        T value = (T)Enum.Parse(typeof(T), stringValue, true);
                        return value;
                    }
                    catch (ArgumentException)
                    {
                        throw new PSRemotingDataStructureException(
                            RemotingErrorIdStrings.CantCastPropertyToExpectedType,
                            propertyName,
                            typeof(T).FullName,
                            propertyValue.GetType().FullName);
                    }
                }

                try
                {
                    Type underlyingType = Enum.GetUnderlyingType(typeof(T));
                    object underlyingValue = LanguagePrimitives.ConvertTo(propertyValue, underlyingType, CultureInfo.InvariantCulture);
                    T value = (T)underlyingValue;
                    return value;
                }
                catch (InvalidCastException)
                {
                    throw new PSRemotingDataStructureException(
                        RemotingErrorIdStrings.CantCastPropertyToExpectedType,
                        propertyName,
                        typeof(T).FullName,
                        propertyValue.GetType().FullName);
                }
            }
            else if (typeof(T).Equals(typeof(PSObject)))
            {
                if (propertyValue == null)
                {
                    return default(T); // => "return null" for PSObject
                }
                else
                {
                    return (T)(object)PSObject.AsPSObject(propertyValue);
                }
            }
            else if (propertyValue == null)
            {
                if (!typeof(T).IsValueType)
                {
                    return default(T);
                }

                if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                {
                    return default(T);
                }

                throw new PSRemotingDataStructureException(
                    RemotingErrorIdStrings.CantCastPropertyToExpectedType,
                    propertyName,
                    typeof(T).FullName,
                    propertyValue != null ? propertyValue.GetType().FullName : "null");
            }
            else if (propertyValue is T)
            {
                return (T)(propertyValue);
            }
            else if (propertyValue is PSObject)
            {
                PSObject psObject = (PSObject)propertyValue;
                return ConvertPropertyValueTo<T>(propertyName, psObject.BaseObject);
            }
            else if ((propertyValue is Hashtable) && (typeof(T).Equals(typeof(PSPrimitiveDictionary))))
            {
                // rehydration of PSPrimitiveDictionary might not work when CreateRunspacePool message is received
                // (there is no runspace and so no type table at this point) so try converting manually
                try
                {
                    return (T)(object)(new PSPrimitiveDictionary((Hashtable)propertyValue));
                }
                catch (ArgumentException)
                {
                    throw new PSRemotingDataStructureException(
                        RemotingErrorIdStrings.CantCastPropertyToExpectedType,
                        propertyName,
                        typeof(T).FullName,
                        propertyValue != null ? propertyValue.GetType().FullName : "null");
                }
            }
            else
            {
                throw new PSRemotingDataStructureException(
                    RemotingErrorIdStrings.CantCastPropertyToExpectedType,
                    propertyName,
                    typeof(T).FullName,
                    propertyValue.GetType().FullName);
            }
        }

        private static PSPropertyInfo GetProperty(PSObject psObject, string propertyName)
        {
            if (psObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(psObject));
            }

            if (propertyName == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(propertyName));
            }

            PSPropertyInfo property = psObject.Properties[propertyName];

            if (property == null)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.MissingProperty, propertyName);
            }

            return property;
        }

        internal static T GetPropertyValue<T>(PSObject psObject, string propertyName)
        {
            if (psObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(psObject));
            }

            if (propertyName == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(propertyName));
            }

            PSPropertyInfo property = GetProperty(psObject, propertyName);
            object propertyValue = property.Value;
            return ConvertPropertyValueTo<T>(propertyName, propertyValue);
        }

        internal static IEnumerable<T> EnumerateListProperty<T>(PSObject psObject, string propertyName)
        {
            if (psObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(psObject));
            }

            if (propertyName == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(propertyName));
            }

            IEnumerable e = GetPropertyValue<IEnumerable>(psObject, propertyName);
            if (e != null)
            {
                foreach (object o in e)
                {
                    yield return ConvertPropertyValueTo<T>(propertyName, o);
                }
            }
        }

        internal static IEnumerable<KeyValuePair<TKey, TValue>> EnumerateHashtableProperty<TKey, TValue>(PSObject psObject, string propertyName)
        {
            if (psObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(psObject));
            }

            if (propertyName == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(propertyName));
            }

            Hashtable h = GetPropertyValue<Hashtable>(psObject, propertyName);
            if (h != null)
            {
                foreach (DictionaryEntry e in h)
                {
                    TKey key = ConvertPropertyValueTo<TKey>(propertyName, e.Key);
                    TValue value = ConvertPropertyValueTo<TValue>(propertyName, e.Value);
                    yield return new KeyValuePair<TKey, TValue>(key, value);
                }
            }
        }

        /// <summary>
        /// Decode and obtain the RunspacePool state info from the
        /// data object specified.
        /// </summary>
        /// <param name="dataAsPSObject">Data object to decode.</param>
        /// <returns>RunspacePoolStateInfo.</returns>
        internal static RunspacePoolStateInfo GetRunspacePoolStateInfo(PSObject dataAsPSObject)
        {
            if (dataAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(dataAsPSObject));
            }

            RunspacePoolState state = GetPropertyValue<RunspacePoolState>(dataAsPSObject, RemoteDataNameStrings.RunspaceState);
            Exception reason = GetExceptionFromStateInfoObject(dataAsPSObject);

            return new RunspacePoolStateInfo(state, reason);
        }

        /// <summary>
        /// Decode and obtain the application private data from the
        /// data object specified.
        /// </summary>
        /// <param name="dataAsPSObject">Data object to decode.</param>
        /// <returns>Application private data.</returns>
        internal static PSPrimitiveDictionary GetApplicationPrivateData(PSObject dataAsPSObject)
        {
            if (dataAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(dataAsPSObject));
            }

            return GetPropertyValue<PSPrimitiveDictionary>(dataAsPSObject, RemoteDataNameStrings.ApplicationPrivateData);
        }

        /// <summary>
        /// Gets the public key from the encoded message.
        /// </summary>
        /// <param name="dataAsPSObject">Data object to decode.</param>
        /// <returns>Public key as string.</returns>
        internal static string GetPublicKey(PSObject dataAsPSObject)
        {
            if (dataAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(dataAsPSObject));
            }

            return GetPropertyValue<string>(dataAsPSObject, RemoteDataNameStrings.PublicKey);
        }

        /// <summary>
        /// Gets the encrypted session key from the encoded message.
        /// </summary>
        /// <param name="dataAsPSObject">Data object to decode.</param>
        /// <returns>Encrypted session key as string.</returns>
        internal static string GetEncryptedSessionKey(PSObject dataAsPSObject)
        {
            if (dataAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(dataAsPSObject));
            }

            return GetPropertyValue<string>(dataAsPSObject, RemoteDataNameStrings.EncryptedSessionKey);
        }

        /// <summary>
        /// Decode and obtain the RunspacePool state info from the
        /// data object specified.
        /// </summary>
        /// <param name="dataAsPSObject">Data object to decode.</param>
        /// <returns>RunspacePoolStateInfo.</returns>
        internal static PSEventArgs GetPSEventArgs(PSObject dataAsPSObject)
        {
            if (dataAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(dataAsPSObject));
            }

            int eventIdentifier = GetPropertyValue<int>(dataAsPSObject, RemoteDataNameStrings.PSEventArgsEventIdentifier);
            string sourceIdentifier = GetPropertyValue<string>(dataAsPSObject, RemoteDataNameStrings.PSEventArgsSourceIdentifier);
            object sender = GetPropertyValue<object>(dataAsPSObject, RemoteDataNameStrings.PSEventArgsSender);
            object messageData = GetPropertyValue<object>(dataAsPSObject, RemoteDataNameStrings.PSEventArgsMessageData);
            string computerName = GetPropertyValue<string>(dataAsPSObject, RemoteDataNameStrings.PSEventArgsComputerName);
            Guid runspaceId = GetPropertyValue<Guid>(dataAsPSObject, RemoteDataNameStrings.PSEventArgsRunspaceId);

            var sourceArgs = new List<object>();
            foreach (object argument in RemotingDecoder.EnumerateListProperty<object>(dataAsPSObject, RemoteDataNameStrings.PSEventArgsSourceArgs))
            {
                sourceArgs.Add(argument);
            }

            PSEventArgs eventArgs = new PSEventArgs(
                computerName,
                runspaceId,
                eventIdentifier,
                sourceIdentifier,
                sender,
                sourceArgs.ToArray(),
                messageData == null ? null : PSObject.AsPSObject(messageData));

            eventArgs.TimeGenerated = GetPropertyValue<DateTime>(dataAsPSObject, RemoteDataNameStrings.PSEventArgsTimeGenerated);

            return eventArgs;
        }

        /// <summary>
        /// Decode and obtain the minimum runspaces to create in the
        /// runspace pool from the data object specified.
        /// </summary>
        /// <param name="dataAsPSObject">Data object to decode.</param>
        /// <returns>Minimum runspaces.</returns>
        internal static int GetMinRunspaces(PSObject dataAsPSObject)
        {
            if (dataAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(dataAsPSObject));
            }

            return GetPropertyValue<int>(dataAsPSObject, RemoteDataNameStrings.MinRunspaces);
        }

        /// <summary>
        /// Decode and obtain the maximum runspaces to create in the
        /// runspace pool from the data object specified.
        /// </summary>
        /// <param name="dataAsPSObject">Data object to decode.</param>
        /// <returns>Maximum runspaces.</returns>
        internal static int GetMaxRunspaces(PSObject dataAsPSObject)
        {
            if (dataAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(dataAsPSObject));
            }

            return GetPropertyValue<int>(dataAsPSObject, RemoteDataNameStrings.MaxRunspaces);
        }

        /// <summary>
        /// Decode and obtain the thread options for the runspaces in the
        /// runspace pool from the data object specified.
        /// </summary>
        /// <param name="dataAsPSObject">Data object to decode.</param>
        /// <returns>Thread options.</returns>
        internal static PSPrimitiveDictionary GetApplicationArguments(PSObject dataAsPSObject)
        {
            if (dataAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(dataAsPSObject));
            }

            // rehydration might not work yet (there is no type table before a runspace is created)
            // so try to cast ApplicationArguments to PSPrimitiveDictionary manually
            return GetPropertyValue<PSPrimitiveDictionary>(dataAsPSObject, RemoteDataNameStrings.ApplicationArguments);
        }

        /// <summary>
        /// Generates RunspacePoolInitInfo object from a received PSObject.
        /// </summary>
        /// <param name="dataAsPSObject">Data object to decode.</param>
        /// <returns>RunspacePoolInitInfo generated.</returns>
        internal static RunspacePoolInitInfo GetRunspacePoolInitInfo(PSObject dataAsPSObject)
        {
            if (dataAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(dataAsPSObject));
            }

            int maxRS = GetPropertyValue<int>(dataAsPSObject, RemoteDataNameStrings.MaxRunspaces);
            int minRS = GetPropertyValue<int>(dataAsPSObject, RemoteDataNameStrings.MinRunspaces);

            return new RunspacePoolInitInfo(minRS, maxRS);
        }

        /// <summary>
        /// Decode and obtain the thread options for the runspaces in the
        /// runspace pool from the data object specified.
        /// </summary>
        /// <param name="dataAsPSObject">Data object to decode.</param>
        /// <returns>Thread options.</returns>
        internal static PSThreadOptions GetThreadOptions(PSObject dataAsPSObject)
        {
            if (dataAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(dataAsPSObject));
            }

            return GetPropertyValue<PSThreadOptions>(dataAsPSObject, RemoteDataNameStrings.ThreadOptions);
        }

        /// <summary>
        /// Decode and obtain the host info for the host
        /// associated with the runspace pool.
        /// </summary>
        /// <param name="dataAsPSObject">DataAsPSObject object to decode.</param>
        /// <returns>Host information.</returns>
        internal static HostInfo GetHostInfo(PSObject dataAsPSObject)
        {
            if (dataAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(dataAsPSObject));
            }

            PSObject propertyValue = GetPropertyValue<PSObject>(dataAsPSObject, RemoteDataNameStrings.HostInfo);
            return RemoteHostEncoder.DecodeObject(propertyValue, typeof(HostInfo)) as HostInfo;
        }

        /// <summary>
        /// Gets the exception if any from the serialized state info object.
        /// </summary>
        /// <param name="stateInfo"></param>
        /// <returns></returns>
        private static Exception GetExceptionFromStateInfoObject(PSObject stateInfo)
        {
            // Check if exception is encoded as errorrecord
            PSPropertyInfo property = stateInfo.Properties[RemoteDataNameStrings.ExceptionAsErrorRecord];
            if (property != null && property.Value != null)
            {
                return GetExceptionFromSerializedErrorRecord(property.Value);
            }
            // Exception is not present and return null.
            return null;
        }

        /// <summary>
        /// Get the exception from serialized error record.
        /// </summary>
        /// <param name="serializedErrorRecord"></param>
        /// <returns></returns>
        internal static Exception GetExceptionFromSerializedErrorRecord(object serializedErrorRecord)
        {
            ErrorRecord er = ErrorRecord.FromPSObjectForRemoting(PSObject.AsPSObject(serializedErrorRecord));

            if (er == null)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.DecodingErrorForErrorRecord);
            }
            else
            {
                return er.Exception;
            }
        }

        /// <summary>
        /// Gets the output from the message.
        /// </summary>
        /// <param name="data">Object to decode.</param>
        /// <returns>Output object.</returns>
        /// <remarks>the current implementation does nothing,
        /// however this method is there in place as the
        /// packaging of output data may change in the future</remarks>
        internal static object GetPowerShellOutput(object data)
        {
            return data;
        }

        /// <summary>
        /// Gets the PSInvocationStateInfo from the data.
        /// </summary>
        /// <param name="data">Object to decode.</param>
        /// <returns>PSInvocationInfo.</returns>
        internal static PSInvocationStateInfo GetPowerShellStateInfo(object data)
        {
            if (data is not PSObject dataAsPSObject)
            {
                throw new PSRemotingDataStructureException(
                    RemotingErrorIdStrings.DecodingErrorForPowerShellStateInfo);
            }

            PSInvocationState state = GetPropertyValue<PSInvocationState>(dataAsPSObject, RemoteDataNameStrings.PipelineState);
            Exception reason = GetExceptionFromStateInfoObject(dataAsPSObject);
            return new PSInvocationStateInfo(state, reason);
        }

        /// <summary>
        /// Gets the ErrorRecord from the message.
        /// </summary>
        /// <param name="data">Data to decode.</param>
        /// <returns>Error record.</returns>
        internal static ErrorRecord GetPowerShellError(object data)
        {
            if (data == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(data));
            }

            PSObject dataAsPSObject = data as PSObject;

            ErrorRecord errorRecord = ErrorRecord.FromPSObjectForRemoting(dataAsPSObject);

            return errorRecord;
        }

        /// <summary>
        /// Gets the WarningRecord from the message.
        /// </summary>
        internal static WarningRecord GetPowerShellWarning(object data)
        {
            if (data == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(data));
            }

            return new WarningRecord((PSObject)data);
        }

        /// <summary>
        /// Gets the VerboseRecord from the message.
        /// </summary>
        internal static VerboseRecord GetPowerShellVerbose(object data)
        {
            if (data == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(data));
            }

            return new VerboseRecord((PSObject)data);
        }

        /// <summary>
        /// Gets the DebugRecord from the message.
        /// </summary>
        internal static DebugRecord GetPowerShellDebug(object data)
        {
            if (data == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(data));
            }

            return new DebugRecord((PSObject)data);
        }

        /// <summary>
        /// Gets the ProgressRecord from the message.
        /// </summary>
        internal static ProgressRecord GetPowerShellProgress(object data)
        {
            PSObject dataAsPSObject = PSObject.AsPSObject(data);
            if (dataAsPSObject == null)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.CantCastRemotingDataToPSObject, data.GetType().FullName);
            }

            return ProgressRecord.FromPSObjectForRemoting(dataAsPSObject);
        }

        /// <summary>
        /// Gets the InformationRecord from the message.
        /// </summary>
        internal static InformationRecord GetPowerShellInformation(object data)
        {
            PSObject dataAsPSObject = PSObject.AsPSObject(data);
            if (dataAsPSObject == null)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.CantCastRemotingDataToPSObject, data.GetType().FullName);
            }

            return InformationRecord.FromPSObjectForRemoting(dataAsPSObject);
        }

        /// <summary>
        /// Gets the PowerShell object from the specified data.
        /// </summary>
        /// <param name="data">Data to decode.</param>
        /// <returns>Deserialized PowerShell object.</returns>
        internal static PowerShell GetPowerShell(object data)
        {
            PSObject dataAsPSObject = PSObject.AsPSObject(data);
            if (dataAsPSObject == null)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.CantCastRemotingDataToPSObject, data.GetType().FullName);
            }

            PSObject powerShellAsPSObject = GetPropertyValue<PSObject>(dataAsPSObject, RemoteDataNameStrings.PowerShell);
            return PowerShell.FromPSObjectForRemoting(powerShellAsPSObject);
        }

        /// <summary>
        /// Gets the PowerShell object from the specified data.
        /// </summary>
        /// <param name="data">Data to decode.</param>
        /// <returns>Deserialized PowerShell object.</returns>
        internal static PowerShell GetCommandDiscoveryPipeline(object data)
        {
            PSObject dataAsPSObject = PSObject.AsPSObject(data);
            if (dataAsPSObject == null)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.CantCastRemotingDataToPSObject, data.GetType().FullName);
            }

            CommandTypes commandType = GetPropertyValue<CommandTypes>(dataAsPSObject, RemoteDataNameStrings.DiscoveryType);

            string[] name;
            if (GetPropertyValue<PSObject>(dataAsPSObject, RemoteDataNameStrings.DiscoveryName) != null)
            {
                IEnumerable<string> tmp = EnumerateListProperty<string>(dataAsPSObject, RemoteDataNameStrings.DiscoveryName);
                name = new List<string>(tmp).ToArray();
            }
            else
            {
                name = new string[] { "*" };
            }

            string[] module;
            if (GetPropertyValue<PSObject>(dataAsPSObject, RemoteDataNameStrings.DiscoveryModule) != null)
            {
                IEnumerable<string> tmp = EnumerateListProperty<string>(dataAsPSObject, RemoteDataNameStrings.DiscoveryModule);
                module = new List<string>(tmp).ToArray();
            }
            else
            {
                module = new string[] { string.Empty };
            }

            ModuleSpecification[] fullyQualifiedName = null;
            if (DeserializingTypeConverter.GetPropertyValue<PSObject>(dataAsPSObject,
                                                                      RemoteDataNameStrings.DiscoveryFullyQualifiedModule,
                                                                      DeserializingTypeConverter.RehydrationFlags.NullValueOk | DeserializingTypeConverter.RehydrationFlags.MissingPropertyOk) != null)
            {
                IEnumerable<ModuleSpecification> tmp = EnumerateListProperty<ModuleSpecification>(dataAsPSObject, RemoteDataNameStrings.DiscoveryFullyQualifiedModule);
                fullyQualifiedName = new List<ModuleSpecification>(tmp).ToArray();
            }

            object[] argumentList;
            if (GetPropertyValue<PSObject>(dataAsPSObject, RemoteDataNameStrings.DiscoveryArgumentList) != null)
            {
                IEnumerable<object> tmp = EnumerateListProperty<object>(dataAsPSObject, RemoteDataNameStrings.DiscoveryArgumentList);
                argumentList = new List<object>(tmp).ToArray();
            }
            else
            {
                argumentList = null;
            }

            PowerShell powerShell = PowerShell.Create();
            powerShell.AddCommand("Get-Command");
            powerShell.AddParameter("Name", name);
            powerShell.AddParameter("CommandType", commandType);
            if (fullyQualifiedName != null)
            {
                powerShell.AddParameter("FullyQualifiedModule", fullyQualifiedName);
            }
            else
            {
                powerShell.AddParameter("Module", module);
            }

            powerShell.AddParameter("ArgumentList", argumentList);
            return powerShell;
        }

        /// <summary>
        /// Gets the NoInput setting from the specified data.
        /// </summary>
        /// <param name="data">Data to decode.</param>
        /// <returns><see langword="true"/> if there is no pipeline input; <see langword="false"/> otherwise.</returns>
        internal static bool GetNoInput(object data)
        {
            PSObject dataAsPSObject = PSObject.AsPSObject(data);

            if (dataAsPSObject == null)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.CantCastRemotingDataToPSObject, data.GetType().FullName);
            }

            return GetPropertyValue<bool>(dataAsPSObject, RemoteDataNameStrings.NoInput);
        }

        /// <summary>
        /// Gets the AddToHistory setting from the specified data.
        /// </summary>
        /// <param name="data">Data to decode.</param>
        /// <returns><see langword="true"/> if there is addToHistory data; <see langword="false"/> otherwise.</returns>
        internal static bool GetAddToHistory(object data)
        {
            PSObject dataAsPSObject = PSObject.AsPSObject(data);

            if (dataAsPSObject == null)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.CantCastRemotingDataToPSObject, data.GetType().FullName);
            }

            return GetPropertyValue<bool>(dataAsPSObject, RemoteDataNameStrings.AddToHistory);
        }

        /// <summary>
        /// Gets the IsNested setting from the specified data.
        /// </summary>
        /// <param name="data">Data to decode.</param>
        /// <returns><see langword="true"/> if there is IsNested data; <see langword="false"/> otherwise.</returns>
        internal static bool GetIsNested(object data)
        {
            PSObject dataAsPSObject = PSObject.AsPSObject(data);

            if (dataAsPSObject == null)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.CantCastRemotingDataToPSObject, data.GetType().FullName);
            }

            return GetPropertyValue<bool>(dataAsPSObject, RemoteDataNameStrings.IsNested);
        }

        /// <summary>
        /// Gets the invocation settings information from the message.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static ApartmentState GetApartmentState(object data)
        {
            PSObject dataAsPSObject = PSObject.AsPSObject(data);
            return GetPropertyValue<ApartmentState>(dataAsPSObject, RemoteDataNameStrings.ApartmentState);
        }

        /// <summary>
        /// Gets the stream options from the message.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static RemoteStreamOptions GetRemoteStreamOptions(object data)
        {
            PSObject dataAsPSObject = PSObject.AsPSObject(data);
            return GetPropertyValue<RemoteStreamOptions>(dataAsPSObject, RemoteDataNameStrings.RemoteStreamOptions);
        }

        /// <summary>
        /// Decodes a RemoteSessionCapability object.
        /// </summary>
        /// <param name="data">Data to decode.</param>
        /// <returns>RemoteSessionCapability object.</returns>
        internal static RemoteSessionCapability GetSessionCapability(object data)
        {
            if (data is not PSObject dataAsPSObject)
            {
                throw new PSRemotingDataStructureException(
                    RemotingErrorIdStrings.CantCastRemotingDataToPSObject, data.GetType().FullName);
            }

            Version protocolVersion = GetPropertyValue<Version>(dataAsPSObject, RemoteDataNameStrings.PS_STARTUP_PROTOCOL_VERSION_NAME);
            Version psVersion = GetPropertyValue<Version>(dataAsPSObject, RemoteDataNameStrings.PSVersion);
            Version serializationVersion = GetPropertyValue<Version>(dataAsPSObject,
                RemoteDataNameStrings.SerializationVersion);

            RemoteSessionCapability result = new RemoteSessionCapability(
                RemotingDestination.InvalidDestination,
                protocolVersion, psVersion, serializationVersion);

            return result;
        }

        /// <summary>
        /// Checks if the server supports batch invocation.
        /// </summary>
        /// <param name="runspace">Runspace instance.</param>
        /// <returns>True if batch invocation is supported, false if not.</returns>
        internal static bool ServerSupportsBatchInvocation(Runspace runspace)
        {
            if (runspace == null || runspace.RunspaceStateInfo.State == RunspaceState.BeforeOpen)
            {
                return false;
            }

            return (runspace.GetRemoteProtocolVersion() >= RemotingConstants.ProtocolVersion_2_2);
        }
    }
}
