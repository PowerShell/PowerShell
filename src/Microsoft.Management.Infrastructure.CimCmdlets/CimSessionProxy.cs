// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Management.Infrastructure.Generic;
using Microsoft.Management.Infrastructure.Options;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    #region Context base class

    /// <summary>
    /// Context base class for cross operations
    /// for example, some cmdlets need to query instance first and then
    /// remove instance, those scenarios need context object transferred
    /// from one operation to another.
    /// </summary>
    internal abstract class XOperationContextBase
    {
        /// <summary>
        /// <para>namespace</para>
        /// </summary>
        internal string Namespace
        {
            get
            {
                return this.nameSpace;
            }
        }

        protected string nameSpace;

        /// <summary>
        /// <para>
        /// Session proxy.
        /// </para>
        /// </summary>
        internal CimSessionProxy Proxy
        {
            get
            {
                return this.proxy;
            }
        }

        protected CimSessionProxy proxy;
    }

    /// <summary>
    /// Class provides all information regarding the
    /// current invocation to the .NET API.
    /// </summary>
    internal class InvocationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvocationContext"/> class.
        /// </summary>
        /// <param name="proxy"></param>
        internal InvocationContext(CimSessionProxy proxy)
        {
            if (proxy != null)
            {
                this.ComputerName = proxy.CimSession.ComputerName;
                this.TargetCimInstance = proxy.TargetCimInstance;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvocationContext"/> class.
        /// </summary>
        /// <param name="proxy"></param>
        internal InvocationContext(string computerName, CimInstance targetCimInstance)
        {
            this.ComputerName = computerName;
            this.TargetCimInstance = targetCimInstance;
        }

        /// <summary>
        /// <para>
        /// ComputerName of the session
        /// </para>
        /// </summary>
        /// <remarks>
        /// return value could be null
        /// </remarks>
        internal virtual string ComputerName { get; }

        /// <summary>
        /// <para>
        /// CimInstance on which the current operation against.
        /// </para>
        /// </summary>
        /// <remarks>
        /// return value could be null
        /// </remarks>
        internal virtual CimInstance TargetCimInstance { get; }
    }
    #endregion

    #region Preprocessing of result object interface
    /// <summary>
    /// Defines a method to preprocessing an result object before sending to
    /// output pipeline.
    /// </summary>
    [ComVisible(false)]
    internal interface IObjectPreProcess
    {
        /// <summary>
        /// Performs pre processing of given result object.
        /// </summary>
        /// <param name="resultObject"></param>
        /// <returns>Pre-processed object.</returns>
        object Process(object resultObject);
    }
    #endregion

    #region Eventargs class
    /// <summary>
    /// <para>
    /// CmdletActionEventArgs holds a CimBaseAction object
    /// </para>
    /// </summary>
    internal sealed class CmdletActionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CmdletActionEventArgs"/> class.
        /// </summary>
        /// <param name="action">CimBaseAction object bound to the event.</param>
        public CmdletActionEventArgs(CimBaseAction action)
        {
            this.Action = action;
        }

        public readonly CimBaseAction Action;
    }

    /// <summary>
    /// OperationEventArgs holds a cancellation object, and an operation.
    /// </summary>
    internal sealed class OperationEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationEventArgs"/> class.
        /// </summary>
        /// <param name="operationCancellation">Object used to cancel the operation.</param>
        /// <param name="operation">Async observable operation.</param>
        public OperationEventArgs(IDisposable operationCancellation,
            IObservable<object> operation,
            bool theSuccess)
        {
            this.operationCancellation = operationCancellation;
            this.operation = operation;
            this.success = theSuccess;
        }

        public readonly IDisposable operationCancellation;
        public readonly IObservable<object> operation;
        public readonly bool success;
    }

    #endregion

    /// <summary>
    /// <para>
    /// Wrapper of <see cref="CimSession"/> object.
    /// A CimSessionProxy object can only execute one operation at specific moment.
    /// </para>
    /// </summary>
    internal class CimSessionProxy : IDisposable
    {
        #region static members

        /// <summary>
        /// <para>
        /// global operation counter
        /// </para>
        /// </summary>
        private static long gOperationCounter = 0;

        /// <summary>
        /// Temporary CimSession cache lock.
        /// </summary>
        private static readonly object temporarySessionCacheLock = new();

        /// <summary>
        /// <para>temporary CimSession cache</para>
        /// <para>Temporary CimSession means the session is created by cimcmdlets,
        /// which is not created by <see cref="New-CimSession"/> cmdlet.
        /// Due to some cmdlet, such as <see cref="Remove-CimInstance"/>
        /// might need to split the operation into multiple stages, i.e., query
        /// CimInstance firstly, then remove the CimInstance resulted from query,
        /// such that the temporary CimSession need to be shared between
        /// multiple <see cref="CimSessionProxy"/> objects, introducing a
        /// temporary session cache is necessary to control the lifetime of the
        /// temporary CimSession objects.</para>
        /// <para>
        /// Once the reference count of the CimSession is decreased to 0,
        /// then call Dispose on it.
        /// </para>
        /// </summary>
        private static readonly Dictionary<CimSession, uint> temporarySessionCache = new();

        /// <summary>
        /// <para>
        /// Add <see cref="CimSession"/> to temporary cache.
        /// If CimSession already present in cache, then increase the refcount by 1,
        /// otherwise insert it into the cache.
        /// </para>
        /// </summary>
        /// <param name="session">CimSession to be added.</param>
        internal static void AddCimSessionToTemporaryCache(CimSession session)
        {
            if (session != null)
            {
                lock (temporarySessionCacheLock)
                {
                    if (temporarySessionCache.ContainsKey(session))
                    {
                        temporarySessionCache[session]++;
                        DebugHelper.WriteLogEx(@"Increase cimsession ref count {0}", 1, temporarySessionCache[session]);
                    }
                    else
                    {
                        temporarySessionCache.Add(session, 1);
                        DebugHelper.WriteLogEx(@"Add cimsession to cache. Ref count {0}", 1, temporarySessionCache[session]);
                    }
                }
            }
        }

        /// <summary>
        /// <para>Wrapper function to remove CimSession from cache</para>
        /// </summary>
        /// <param name="session"></param>
        /// <param name="dispose">Whether need to dispose the <see cref="CimSession"/> object.</param>
        private static void RemoveCimSessionFromTemporaryCache(CimSession session,
            bool dispose)
        {
            if (session != null)
            {
                bool removed = false;
                lock (temporarySessionCacheLock)
                {
                    if (temporarySessionCache.ContainsKey(session))
                    {
                        temporarySessionCache[session]--;
                        DebugHelper.WriteLogEx(@"Decrease cimsession ref count {0}", 1, temporarySessionCache[session]);
                        if (temporarySessionCache[session] == 0)
                        {
                            removed = true;
                            temporarySessionCache.Remove(session);
                        }
                    }
                }
                // there is a race condition that if
                // one thread is waiting to add CimSession to cache,
                // while current thread is removing the CimSession,
                // then invalid CimSession may be added to cache.
                // Ignored this scenario in CimCmdlet implementation,
                // since the code inside cimcmdlet will not hit this
                // scenario anyway.
                if (removed && dispose)
                {
                    DebugHelper.WriteLogEx(@"Dispose cimsession ", 1);
                    session.Dispose();
                }
            }
        }

        /// <summary>
        /// <para>
        /// Remove <see cref="CimSession"/> from temporary cache.
        /// If CimSession already present in cache, then decrease the refcount by 1,
        /// otherwise ignore.
        /// If refcount became 0, call dispose on the <see cref="CimSession"/> object.
        /// </para>
        /// </summary>
        /// <param name="session">CimSession to be added.</param>
        internal static void RemoveCimSessionFromTemporaryCache(CimSession session)
        {
            RemoveCimSessionFromTemporaryCache(session, true);
        }
        #endregion

        #region Event definitions

        /// <summary>
        /// Define an Event based on the NewActionHandler.
        /// </summary>
        public event EventHandler<CmdletActionEventArgs> OnNewCmdletAction;

        /// <summary>
        /// Event triggered when a new operation is started.
        /// </summary>
        public event EventHandler<OperationEventArgs> OnOperationCreated;

        /// <summary>
        /// Event triggered when a new operation is completed,
        /// either success or failed.
        /// </summary>
        public event EventHandler<OperationEventArgs> OnOperationDeleted;

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionProxy"/> class.
        /// </summary>
        /// <remarks>
        /// Then create wrapper object by given CimSessionProxy object.
        /// </remarks>
        /// <param name="computerName"></param>
        public CimSessionProxy(CimSessionProxy proxy)
        {
            DebugHelper.WriteLogEx("protocol = {0}", 1, proxy.Protocol);

            CreateSetSession(null, proxy.CimSession, null, proxy.OperationOptions, proxy.IsTemporaryCimSession);
            this.Protocol = proxy.Protocol;
            this.OperationTimeout = proxy.OperationTimeout;
            this.isDefaultSession = proxy.isDefaultSession;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionProxy"/> class.
        /// </summary>
        /// <remarks>
        /// Create <see cref="CimSession"/> by given computer name.
        /// Then create wrapper object.
        /// </remarks>
        /// <param name="computerName"></param>
        public CimSessionProxy(string computerName)
        {
            CreateSetSession(computerName, null, null, null, false);
            this.isDefaultSession = computerName == ConstValue.NullComputerName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionProxy"/> class.
        /// </summary>
        /// <remarks>
        /// Create <see cref="CimSession"/> by given computer name
        /// and session options.
        /// Then create wrapper object.
        /// </remarks>
        /// <param name="computerName"></param>
        /// <param name="sessionOptions"></param>
        public CimSessionProxy(string computerName, CimSessionOptions sessionOptions)
        {
            CreateSetSession(computerName, null, sessionOptions, null, false);
            this.isDefaultSession = computerName == ConstValue.NullComputerName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionProxy"/> class.
        /// </summary>
        /// <remarks>
        /// Create <see cref="CimSession"/> by given computer name
        /// and cimInstance. Then create wrapper object.
        /// </remarks>
        /// <param name="computerName"></param>
        /// <param name="cimInstance"></param>
        public CimSessionProxy(string computerName, CimInstance cimInstance)
        {
            DebugHelper.WriteLogEx("ComputerName {0}; cimInstance.CimSessionInstanceID = {1}; cimInstance.CimSessionComputerName = {2}.",
                0,
                computerName,
                cimInstance.GetCimSessionInstanceId(),
                cimInstance.GetCimSessionComputerName());

            if (computerName != ConstValue.NullComputerName)
            {
                CreateSetSession(computerName, null, null, null, false);
                return;
            }

            Debug.Assert(cimInstance != null, "Caller should verify cimInstance != null");

            // computerName is null, fallback to create session from cimInstance
            CimSessionState state = CimSessionBase.GetCimSessionState();
            if (state != null)
            {
                CimSession session = state.QuerySession(cimInstance.GetCimSessionInstanceId());
                if (session != null)
                {
                    DebugHelper.WriteLogEx("Found the session from cache with InstanceID={0}.", 0, cimInstance.GetCimSessionInstanceId());
                    CreateSetSession(null, session, null, null, false);
                    return;
                }
            }

            string cimsessionComputerName = cimInstance.GetCimSessionComputerName();
            CreateSetSession(cimsessionComputerName, null, null, null, false);
            this.isDefaultSession = cimsessionComputerName == ConstValue.NullComputerName;

            DebugHelper.WriteLogEx("Create a temp session with computerName = {0}.", 0, cimsessionComputerName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionProxy"/> class.
        /// </summary>
        /// <remarks>
        /// Create <see cref="CimSession"/> by given computer name,
        /// session options.
        /// </remarks>
        /// <param name="computerName"></param>
        /// <param name="sessionOptions"></param>
        /// <param name="operOptions">Used when create async operation.</param>
        public CimSessionProxy(string computerName, CimSessionOptions sessionOptions, CimOperationOptions operOptions)
        {
            CreateSetSession(computerName, null, sessionOptions, operOptions, false);
            this.isDefaultSession = computerName == ConstValue.NullComputerName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionProxy"/> class.
        /// </summary>
        /// <remarks>
        /// Create <see cref="CimSession"/> by given computer name.
        /// Then create wrapper object.
        /// </remarks>
        /// <param name="computerName"></param>
        /// <param name="operOptions">Used when create async operation.</param>
        public CimSessionProxy(string computerName, CimOperationOptions operOptions)
        {
            CreateSetSession(computerName, null, null, operOptions, false);
            this.isDefaultSession = computerName == ConstValue.NullComputerName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionProxy"/> class.
        /// </summary>
        /// <remarks>
        /// Create wrapper object by given session object.
        /// </remarks>
        /// <param name="session"></param>
        public CimSessionProxy(CimSession session)
        {
            CreateSetSession(null, session, null, null, false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionProxy"/> class.
        /// </summary>
        /// <remarks>
        /// Create wrapper object by given session object.
        /// </remarks>
        /// <param name="session"></param>
        /// <param name="operOptions">Used when create async operation.</param>
        public CimSessionProxy(CimSession session, CimOperationOptions operOptions)
        {
            CreateSetSession(null, session, null, operOptions, false);
        }

        /// <summary>
        /// Initialize CimSessionProxy object.
        /// </summary>
        /// <param name="computerName"></param>
        /// <param name="session"></param>
        /// <param name="sessionOptions"></param>
        /// <param name="options"></param>
        private void CreateSetSession(
            string computerName,
            CimSession cimSession,
            CimSessionOptions sessionOptions,
            CimOperationOptions operOptions,
            bool temporaryCimSession)
        {
            DebugHelper.WriteLogEx("computername {0}; cimsession {1}; sessionOptions {2}; operationOptions {3}.", 0, computerName, cimSession, sessionOptions, operOptions);

            lock (this.stateLock)
            {
                this.CancelOperation = null;
                this.operation = null;
            }

            InitOption(operOptions);
            this.Protocol = ProtocolType.Wsman;
            this.IsTemporaryCimSession = temporaryCimSession;

            if (cimSession != null)
            {
                this.CimSession = cimSession;
                CimSessionState state = CimSessionBase.GetCimSessionState();
                if (state != null)
                {
                    CimSessionWrapper wrapper = state.QuerySession(cimSession);
                    if (wrapper != null)
                    {
                        this.Protocol = wrapper.GetProtocolType();
                    }
                }
            }
            else
            {
                if (sessionOptions != null)
                {
                    if (sessionOptions is DComSessionOptions)
                    {
                        string defaultComputerName = ConstValue.IsDefaultComputerName(computerName) ? ConstValue.NullComputerName : computerName;
                        this.CimSession = CimSession.Create(defaultComputerName, sessionOptions);
                        this.Protocol = ProtocolType.Dcom;
                    }
                    else
                    {
                        this.CimSession = CimSession.Create(computerName, sessionOptions);
                    }
                }
                else
                {
                    this.CimSession = CreateCimSessionByComputerName(computerName);
                }

                this.IsTemporaryCimSession = true;
            }

            if (this.IsTemporaryCimSession)
            {
                AddCimSessionToTemporaryCache(this.CimSession);
            }

            this.invocationContextObject = new InvocationContext(this);
            DebugHelper.WriteLog("Protocol {0}, Is temporary session ? {1}", 1, this.Protocol, this.IsTemporaryCimSession);
        }

        #endregion

        #region set operation options

        /// <summary>
        /// Gets or sets a value indicating whether to retrieve localized information for the CIM class.
        /// </summary>
        public bool Amended
        {
            get => OperationOptions.Flags.HasFlag(CimOperationFlags.LocalizedQualifiers);

            set
            {
                if (value)
                {
                    OperationOptions.Flags |= CimOperationFlags.LocalizedQualifiers;
                }
                else
                {
                    OperationOptions.Flags &= ~CimOperationFlags.LocalizedQualifiers;
                }
            }
        }

        /// <summary>
        /// Set timeout value (seconds) of the operation.
        /// </summary>
        public uint OperationTimeout
        {
            get
            {
                return (uint)this.OperationOptions.Timeout.TotalSeconds;
            }

            set
            {
                DebugHelper.WriteLogEx("OperationTimeout {0},", 0, value);

                this.OperationOptions.Timeout = TimeSpan.FromSeconds((double)value);
            }
        }

        /// <summary>
        /// Set resource URI of the operation.
        /// </summary>
        public Uri ResourceUri
        {
            get
            {
                return this.OperationOptions.ResourceUri;
            }

            set
            {
                DebugHelper.WriteLogEx("ResourceUri {0},", 0, value);

                this.OperationOptions.ResourceUri = value;
            }
        }

        /// <summary>
        /// Enable/Disable the method result streaming,
        /// it is enabled by default.
        /// </summary>
        public bool EnableMethodResultStreaming
        {
            get
            {
                return this.OperationOptions.EnableMethodResultStreaming;
            }

            set
            {
                DebugHelper.WriteLogEx("EnableMethodResultStreaming {0}", 0, value);
                this.OperationOptions.EnableMethodResultStreaming = value;
            }
        }

        /// <summary>
        /// Enable/Disable prompt user streaming,
        /// it is enabled by default.
        /// </summary>
        public bool EnablePromptUser
        {
            set
            {
                DebugHelper.WriteLogEx("EnablePromptUser {0}", 0, value);
                if (value)
                {
                    this.OperationOptions.PromptUser = this.PromptUser;
                }
            }
        }

        /// <summary>
        /// Enable the pssemantics.
        /// </summary>
        private void EnablePSSemantics()
        {
            DebugHelper.WriteLogEx();

            // this.options.PromptUserForceFlag...
            // this.options.WriteErrorMode
            this.OperationOptions.WriteErrorMode = CimCallbackMode.Inquire;

            // !!!NOTES: Does not subscribe to PromptUser for CimCmdlets now
            // since cmdlet does not provider an approach
            // to let user select how to handle prompt message
            // this can be enabled later if needed.
            this.OperationOptions.WriteError = this.WriteError;
            this.OperationOptions.WriteMessage = this.WriteMessage;
            this.OperationOptions.WriteProgress = this.WriteProgress;
        }

        /// <summary>
        /// Set keyonly property.
        /// </summary>
        public SwitchParameter KeyOnly
        {
            set { this.OperationOptions.KeysOnly = value.IsPresent; }
        }

        /// <summary>
        /// Set Shallow flag.
        /// </summary>
        public SwitchParameter Shallow
        {
            set
            {
                if (value.IsPresent)
                {
                    this.OperationOptions.Flags = CimOperationFlags.PolymorphismShallow;
                }
                else
                {
                    this.OperationOptions.Flags = CimOperationFlags.None;
                }
            }
        }

        /// <summary>
        /// Initialize the operation option.
        /// </summary>
        private void InitOption(CimOperationOptions operOptions)
        {
            DebugHelper.WriteLogEx();

            if (operOptions != null)
            {
                this.OperationOptions = new CimOperationOptions(operOptions);
            }
            else
            {
                this.OperationOptions ??= new CimOperationOptions();
            }

            this.EnableMethodResultStreaming = true;
            this.EnablePSSemantics();
        }

        #endregion

        #region misc operations

        /// <summary>
        /// Caller call Detach to retrieve the session
        /// object and control the lifecycle of the CimSession object.
        /// </summary>
        /// <returns></returns>
        public CimSession Detach()
        {
            DebugHelper.WriteLogEx();

            // Remove the CimSession from cache but don't dispose it
            RemoveCimSessionFromTemporaryCache(this.CimSession, false);
            CimSession sessionToReturn = this.CimSession;
            this.CimSession = null;
            this.IsTemporaryCimSession = false;
            return sessionToReturn;
        }

        /// <summary>
        /// Add a new operation to cache.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="cancelObject"></param>
        private void AddOperation(IObservable<object> operation)
        {
            DebugHelper.WriteLogEx();

            lock (this.stateLock)
            {
                Debug.Assert(this.Completed, "Caller should verify that there is no operation in progress");
                this.operation = operation;
            }
        }

        /// <summary>
        /// Remove object from cache.
        /// </summary>
        /// <param name="operation"></param>
        private void RemoveOperation(IObservable<object> operation)
        {
            DebugHelper.WriteLogEx();

            lock (this.stateLock)
            {
                Debug.Assert(this.operation == operation, "Caller should verify that the operation to remove is the operation in progress");

                this.DisposeCancelOperation();

                if (this.operation != null)
                {
                    this.operation = null;
                }

                if (this.CimSession != null && this.ContextObject == null)
                {
                    DebugHelper.WriteLog("Dispose this proxy object @ RemoveOperation");
                    this.Dispose();
                }
            }
        }

        /// <summary>
        /// <para>
        /// Trigger an event that new action available
        /// </para>
        /// </summary>
        /// <param name="action"></param>
        protected void FireNewActionEvent(CimBaseAction action)
        {
            DebugHelper.WriteLogEx();

            CmdletActionEventArgs actionArgs = new(action);
            if (!PreNewActionEvent(actionArgs))
            {
                return;
            }

            EventHandler<CmdletActionEventArgs> temp = this.OnNewCmdletAction;
            if (temp != null)
            {
                temp(this.CimSession, actionArgs);
            }
            else
            {
                DebugHelper.WriteLog("Ignore action since OnNewCmdletAction is null.", 5);
            }

            this.PostNewActionEvent(actionArgs);
        }

        /// <summary>
        /// <para>
        /// Trigger an event that new operation is created
        /// </para>
        /// </summary>
        /// <param name="cancelOperation"></param>
        /// <param name="operation"></param>
        private void FireOperationCreatedEvent(
            IDisposable cancelOperation,
            IObservable<object> operation)
        {
            DebugHelper.WriteLogEx();

            OperationEventArgs args = new(
                cancelOperation, operation, false);
            this.OnOperationCreated?.Invoke(this.CimSession, args);

            this.PostOperationCreateEvent(args);
        }

        /// <summary>
        /// <para>
        /// Trigger an event that an operation is deleted
        /// </para>
        /// </summary>
        /// <param name="operation"></param>
        private void FireOperationDeletedEvent(
            IObservable<object> operation,
            bool success)
        {
            DebugHelper.WriteLogEx();
            this.WriteOperationCompleteMessage(this.operationName);
            OperationEventArgs args = new(
                null, operation, success);
            PreOperationDeleteEvent(args);
            this.OnOperationDeleted?.Invoke(this.CimSession, args);

            this.PostOperationDeleteEvent(args);
            this.RemoveOperation(operation);
            this.operationName = null;
        }

        #endregion

        #region PSExtension callback functions

        /// <summary>
        /// <para>
        /// WriteMessage callback
        /// </para>
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        internal void WriteMessage(uint channel, string message)
        {
            DebugHelper.WriteLogEx("Channel = {0} message = {1}", 0, channel, message);
            try
            {
                CimWriteMessage action = new(channel, message);
                this.FireNewActionEvent(action);
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLogEx("{0}", 0, ex);
            }
        }

        /// <summary>
        /// <para>
        /// Write operation start verbose message
        /// </para>
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="parameters"></param>
        internal void WriteOperationStartMessage(string operation, Hashtable parameterList)
        {
            DebugHelper.WriteLogEx();
            StringBuilder parameters = new();
            if (parameterList != null)
            {
                foreach (string key in parameterList.Keys)
                {
                    if (parameters.Length > 0)
                    {
                        parameters.Append(',');
                    }

                    parameters.Append(CultureInfo.CurrentUICulture, $@"'{key}' = {parameterList[key]}");
                }
            }

            string operationStartMessage = string.Format(CultureInfo.CurrentUICulture,
                CimCmdletStrings.CimOperationStart,
                operation,
                (parameters.Length == 0) ? "null" : parameters.ToString());
            WriteMessage((uint)CimWriteMessageChannel.Verbose, operationStartMessage);
        }

        /// <summary>
        /// <para>
        /// Write operation complete verbose message
        /// </para>
        /// </summary>
        /// <param name="operation"></param>
        internal void WriteOperationCompleteMessage(string operation)
        {
            DebugHelper.WriteLogEx();
            string operationCompleteMessage = string.Format(CultureInfo.CurrentUICulture,
                CimCmdletStrings.CimOperationCompleted,
                operation);
            WriteMessage((uint)CimWriteMessageChannel.Verbose, operationCompleteMessage);
        }

        /// <summary>
        /// <para>
        /// WriteProgress callback
        /// </para>
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="currentOperation"></param>
        /// <param name="statusDescription"></param>
        /// <param name="percentageCompleted"></param>
        /// <param name="secondsRemaining"></param>
        public void WriteProgress(string activity,
            string currentOperation,
            string statusDescription,
            uint percentageCompleted,
            uint secondsRemaining)
        {
            DebugHelper.WriteLogEx("activity:{0}; currentOperation:{1}; percentageCompleted:{2}; secondsRemaining:{3}",
                0, activity, currentOperation, percentageCompleted, secondsRemaining);

            try
            {
                CimWriteProgress action = new(
                    activity,
                    (int)this.operationID,
                    currentOperation,
                    statusDescription,
                    percentageCompleted,
                    secondsRemaining);
                this.FireNewActionEvent(action);
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLogEx("{0}", 0, ex);
            }
        }

        /// <summary>
        /// <para>
        /// WriteError callback
        /// </para>
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public CimResponseType WriteError(CimInstance instance)
        {
            DebugHelper.WriteLogEx("Error:{0}", 0, instance);
            try
            {
                CimWriteError action = new(instance, this.invocationContextObject);
                this.FireNewActionEvent(action);
                return action.GetResponse();
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLogEx("{0}", 0, ex);
                return CimResponseType.NoToAll;
            }
        }

        /// <summary>
        /// PromptUser callback.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prompt"></param>
        /// <returns></returns>
        public CimResponseType PromptUser(string message, CimPromptType prompt)
        {
            DebugHelper.WriteLogEx("message:{0} prompt:{1}", 0, message, prompt);
            try
            {
                CimPromptUser action = new(message, prompt);
                this.FireNewActionEvent(action);
                return action.GetResponse();
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLogEx("{0}", 0, ex);
                return CimResponseType.NoToAll;
            }
        }
        #endregion

        #region Async result handler

        /// <summary>
        /// <para>
        /// Handle async event triggered by <see cref="CimResultObserver{T}"/>
        /// </para>
        /// </summary>
        /// <param name="observer">Object triggered the event.</param>
        /// <param name="resultArgs">Async result event argument.</param>
        internal void ResultEventHandler(
            object observer,
            AsyncResultEventArgsBase resultArgs)
        {
            DebugHelper.WriteLogEx();
            switch (resultArgs.resultType)
            {
                case AsyncResultType.Completion:
                    {
                        DebugHelper.WriteLog("ResultEventHandler::Completion", 4);

                        AsyncResultCompleteEventArgs args = resultArgs as AsyncResultCompleteEventArgs;
                        this.FireOperationDeletedEvent(args.observable, true);
                    }

                    break;
                case AsyncResultType.Exception:
                    {
                        AsyncResultErrorEventArgs args = resultArgs as AsyncResultErrorEventArgs;
                        DebugHelper.WriteLog("ResultEventHandler::Exception {0}", 4, args.error);

                        using (CimWriteError action = new(args.error, this.invocationContextObject, args.context))
                        {
                            this.FireNewActionEvent(action);
                        }

                        this.FireOperationDeletedEvent(args.observable, false);
                    }

                    break;
                case AsyncResultType.Result:
                    {
                        AsyncResultObjectEventArgs args = resultArgs as AsyncResultObjectEventArgs;
                        DebugHelper.WriteLog("ResultEventHandler::Result {0}", 4, args.resultObject);
                        object resultObject = args.resultObject;
                        if (!this.isDefaultSession)
                        {
                            AddShowComputerNameMarker(resultObject);
                        }

                        if (this.ObjectPreProcess != null)
                        {
                            resultObject = this.ObjectPreProcess.Process(resultObject);
                        }
#if DEBUG
                        resultObject = PostProcessCimInstance(resultObject);
#endif
                        CimWriteResultObject action = new(resultObject, this.ContextObject);
                        this.FireNewActionEvent(action);
                    }

                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// This method adds a note property to <paramref name="o"/>,
        /// which will cause the default PowerShell formatting and output
        /// to include PSComputerName column/property in the display.
        /// </summary>
        /// <param name="o"></param>
        private static void AddShowComputerNameMarker(object o)
        {
            if (o == null)
            {
                return;
            }

            PSObject pso = PSObject.AsPSObject(o);
            if (pso.BaseObject is not CimInstance)
            {
                return;
            }

            PSNoteProperty psShowComputerNameProperty = new(ConstValue.ShowComputerNameNoteProperty, true);
            pso.Members.Add(psShowComputerNameProperty);
        }

#if DEBUG
        private static readonly bool isCliXmlTestabilityHookActive = GetIsCliXmlTestabilityHookActive();

        private static bool GetIsCliXmlTestabilityHookActive()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CDXML_CLIXML_TEST"));
        }

        private static object PostProcessCimInstance(object resultObject)
        {
            DebugHelper.WriteLogEx();
            if (isCliXmlTestabilityHookActive && (resultObject is CimInstance))
            {
                string serializedForm = PSSerializer.Serialize(resultObject as CimInstance, depth: 1);
                object deserializedObject = PSSerializer.Deserialize(serializedForm);
                object returnObject = (deserializedObject is PSObject) ? (deserializedObject as PSObject).BaseObject : deserializedObject;
                DebugHelper.WriteLogEx("Deserialized object is {0}, type {1}", 1, returnObject, returnObject.GetType());
                return returnObject;
            }

            return resultObject;
        }
#endif
        #endregion

        #region Async operations

        /// <summary>
        /// <para>
        /// create a cim instance asynchronously
        /// </para>
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="instance"></param>
        public void CreateInstanceAsync(string namespaceName, CimInstance instance)
        {
            Debug.Assert(instance != null, "Caller should verify that instance != NULL.");
            DebugHelper.WriteLogEx("EnableMethodResultStreaming = {0}", 0, this.OperationOptions.EnableMethodResultStreaming);
            this.CheckAvailability();
            this.TargetCimInstance = instance;
            this.operationName = CimCmdletStrings.CimOperationNameCreateInstance;
            this.operationParameters.Clear();
            this.operationParameters.Add(@"namespaceName", namespaceName);
            this.operationParameters.Add(@"instance", instance);
            this.WriteOperationStartMessage(this.operationName, this.operationParameters);
            CimAsyncResult<CimInstance> asyncResult = this.CimSession.CreateInstanceAsync(namespaceName, instance, this.OperationOptions);
            ConsumeCimInstanceAsync(asyncResult, new CimResultContext(instance));
        }

        /// <summary>
        /// Delete a cim instance asynchronously.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="instance"></param>
        public void DeleteInstanceAsync(string namespaceName, CimInstance instance)
        {
            Debug.Assert(instance != null, "Caller should verify that instance != NULL.");
            DebugHelper.WriteLogEx("namespace = {0}; classname = {1};", 0, namespaceName, instance.CimSystemProperties.ClassName);
            this.CheckAvailability();
            this.TargetCimInstance = instance;
            this.operationName = CimCmdletStrings.CimOperationNameDeleteInstance;
            this.operationParameters.Clear();
            this.operationParameters.Add(@"namespaceName", namespaceName);
            this.operationParameters.Add(@"instance", instance);
            this.WriteOperationStartMessage(this.operationName, this.operationParameters);
            CimAsyncStatus asyncResult = this.CimSession.DeleteInstanceAsync(namespaceName, instance, this.OperationOptions);
            ConsumeObjectAsync(asyncResult, new CimResultContext(instance));
        }

        /// <summary>
        /// Get cim instance asynchronously.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="instanceId"></param>
        public void GetInstanceAsync(string namespaceName, CimInstance instance)
        {
            Debug.Assert(instance != null, "Caller should verify that instance != NULL.");
            DebugHelper.WriteLogEx("namespace = {0}; classname = {1}; keyonly = {2}", 0, namespaceName, instance.CimSystemProperties.ClassName, this.OperationOptions.KeysOnly);
            this.CheckAvailability();
            this.TargetCimInstance = instance;
            this.operationName = CimCmdletStrings.CimOperationNameGetInstance;
            this.operationParameters.Clear();
            this.operationParameters.Add(@"namespaceName", namespaceName);
            this.operationParameters.Add(@"instance", instance);
            this.WriteOperationStartMessage(this.operationName, this.operationParameters);
            CimAsyncResult<CimInstance> asyncResult = this.CimSession.GetInstanceAsync(namespaceName, instance, this.OperationOptions);
            ConsumeCimInstanceAsync(asyncResult, new CimResultContext(instance));
        }

        /// <summary>
        /// Modify cim instance asynchronously.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="instance"></param>
        public void ModifyInstanceAsync(string namespaceName, CimInstance instance)
        {
            Debug.Assert(instance != null, "Caller should verify that instance != NULL.");
            DebugHelper.WriteLogEx("namespace = {0}; classname = {1}", 0, namespaceName, instance.CimSystemProperties.ClassName);
            this.CheckAvailability();
            this.TargetCimInstance = instance;
            this.operationName = CimCmdletStrings.CimOperationNameModifyInstance;
            this.operationParameters.Clear();
            this.operationParameters.Add(@"namespaceName", namespaceName);
            this.operationParameters.Add(@"instance", instance);
            this.WriteOperationStartMessage(this.operationName, this.operationParameters);
            CimAsyncResult<CimInstance> asyncResult = this.CimSession.ModifyInstanceAsync(namespaceName, instance, this.OperationOptions);
            ConsumeObjectAsync(asyncResult, new CimResultContext(instance));
        }

        /// <summary>
        /// Enumerate cim instance associated with the
        /// given instance asynchronously.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="sourceInstance"></param>
        /// <param name="associationClassName"></param>
        /// <param name="resultClassName"></param>
        /// <param name="sourceRole"></param>
        /// <param name="resultRole"></param>
        public void EnumerateAssociatedInstancesAsync(
            string namespaceName,
            CimInstance sourceInstance,
            string associationClassName,
            string resultClassName,
            string sourceRole,
            string resultRole)
        {
            Debug.Assert(sourceInstance != null, "Caller should verify that sourceInstance != NULL.");
            DebugHelper.WriteLogEx("Instance class {0}, association class {1}", 0, sourceInstance.CimSystemProperties.ClassName, associationClassName);
            this.CheckAvailability();
            this.TargetCimInstance = sourceInstance;
            this.operationName = CimCmdletStrings.CimOperationNameEnumerateAssociatedInstances;
            this.operationParameters.Clear();
            this.operationParameters.Add(@"namespaceName", namespaceName);
            this.operationParameters.Add(@"sourceInstance", sourceInstance);
            this.operationParameters.Add(@"associationClassName", associationClassName);
            this.operationParameters.Add(@"resultClassName", resultClassName);
            this.operationParameters.Add(@"sourceRole", sourceRole);
            this.operationParameters.Add(@"resultRole", resultRole);
            this.WriteOperationStartMessage(this.operationName, this.operationParameters);
            CimAsyncMultipleResults<CimInstance> asyncResult = this.CimSession.EnumerateAssociatedInstancesAsync(namespaceName, sourceInstance, associationClassName, resultClassName, sourceRole, resultRole, this.OperationOptions);
            ConsumeCimInstanceAsync(asyncResult, new CimResultContext(sourceInstance));
        }

        /// <summary>
        /// Enumerate cim instance asynchronously.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="className"></param>
        public void EnumerateInstancesAsync(string namespaceName, string className)
        {
            DebugHelper.WriteLogEx("KeyOnly {0}", 0, this.OperationOptions.KeysOnly);
            this.CheckAvailability();
            this.TargetCimInstance = null;
            this.operationName = CimCmdletStrings.CimOperationNameEnumerateInstances;
            this.operationParameters.Clear();
            this.operationParameters.Add(@"namespaceName", namespaceName);
            this.operationParameters.Add(@"className", className);
            this.WriteOperationStartMessage(this.operationName, this.operationParameters);
            CimAsyncMultipleResults<CimInstance> asyncResult = this.CimSession.EnumerateInstancesAsync(namespaceName, className, this.OperationOptions);
            string errorSource = string.Create(CultureInfo.CurrentUICulture, $"{namespaceName}:{className}");
            ConsumeCimInstanceAsync(asyncResult, new CimResultContext(errorSource));
        }

        /// <summary>
        /// <para>
        /// Enumerate referencing instance associated with
        /// the given instance asynchronously
        /// </para>
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="sourceInstance"></param>
        /// <param name="associationClassName"></param>
        /// <param name="sourceRole"></param>
        public void EnumerateReferencingInstancesAsync(
            string namespaceName,
            CimInstance sourceInstance,
            string associationClassName,
            string sourceRole)
        {
            this.CheckAvailability();
        }

        /// <summary>
        /// <para>
        /// Query cim instance asynchronously
        /// </para>
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="queryDialect"></param>
        /// <param name="queryExpression"></param>
        public void QueryInstancesAsync(
            string namespaceName,
            string queryDialect,
            string queryExpression)
        {
            DebugHelper.WriteLogEx("KeyOnly = {0}", 0, this.OperationOptions.KeysOnly);
            this.CheckAvailability();
            this.TargetCimInstance = null;
            this.operationName = CimCmdletStrings.CimOperationNameQueryInstances;
            this.operationParameters.Clear();
            this.operationParameters.Add(@"namespaceName", namespaceName);
            this.operationParameters.Add(@"queryDialect", queryDialect);
            this.operationParameters.Add(@"queryExpression", queryExpression);
            this.WriteOperationStartMessage(this.operationName, this.operationParameters);
            CimAsyncMultipleResults<CimInstance> asyncResult = this.CimSession.QueryInstancesAsync(namespaceName, queryDialect, queryExpression, this.OperationOptions);
            ConsumeCimInstanceAsync(asyncResult, null);
        }

        /// <summary>
        /// Enumerate cim class asynchronously.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="className"></param>
        public void EnumerateClassesAsync(string namespaceName)
        {
            DebugHelper.WriteLogEx("namespace {0}", 0, namespaceName);
            this.CheckAvailability();
            this.TargetCimInstance = null;
            this.operationName = CimCmdletStrings.CimOperationNameEnumerateClasses;
            this.operationParameters.Clear();
            this.operationParameters.Add(@"namespaceName", namespaceName);
            this.WriteOperationStartMessage(this.operationName, this.operationParameters);
            CimAsyncMultipleResults<CimClass> asyncResult = this.CimSession.EnumerateClassesAsync(namespaceName, null, this.OperationOptions);
            ConsumeCimClassAsync(asyncResult, null);
        }

        /// <summary>
        /// Enumerate cim class asynchronously.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="className"></param>
        public void EnumerateClassesAsync(string namespaceName, string className)
        {
            this.CheckAvailability();
            this.TargetCimInstance = null;
            this.operationName = CimCmdletStrings.CimOperationNameEnumerateClasses;
            this.operationParameters.Clear();
            this.operationParameters.Add(@"namespaceName", namespaceName);
            this.operationParameters.Add(@"className", className);
            this.WriteOperationStartMessage(this.operationName, this.operationParameters);
            CimAsyncMultipleResults<CimClass> asyncResult = this.CimSession.EnumerateClassesAsync(namespaceName, className, this.OperationOptions);
            string errorSource = string.Create(CultureInfo.CurrentUICulture, $"{namespaceName}:{className}");
            ConsumeCimClassAsync(asyncResult, new CimResultContext(errorSource));
        }

        /// <summary>
        /// Get cim class asynchronously.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="className"></param>
        public void GetClassAsync(string namespaceName, string className)
        {
            DebugHelper.WriteLogEx("namespace = {0}, className = {1}", 0, namespaceName, className);
            this.CheckAvailability();
            this.TargetCimInstance = null;
            this.operationName = CimCmdletStrings.CimOperationNameGetClass;
            this.operationParameters.Clear();
            this.operationParameters.Add(@"namespaceName", namespaceName);
            this.operationParameters.Add(@"className", className);
            this.WriteOperationStartMessage(this.operationName, this.operationParameters);
            CimAsyncResult<CimClass> asyncResult = this.CimSession.GetClassAsync(namespaceName, className, this.OperationOptions);
            string errorSource = string.Create(CultureInfo.CurrentUICulture, $"{namespaceName}:{className}");
            ConsumeCimClassAsync(asyncResult, new CimResultContext(errorSource));
        }

        /// <summary>
        /// Invoke method of a given cim instance asynchronously.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="instance"></param>
        /// <param name="methodName"></param>
        /// <param name="methodParameters"></param>
        public void InvokeMethodAsync(
            string namespaceName,
            CimInstance instance,
            string methodName,
            CimMethodParametersCollection methodParameters)
        {
            Debug.Assert(instance != null, "Caller should verify that instance != NULL.");
            DebugHelper.WriteLogEx("EnableMethodResultStreaming = {0}", 0, this.OperationOptions.EnableMethodResultStreaming);
            this.CheckAvailability();
            this.TargetCimInstance = instance;
            this.operationName = CimCmdletStrings.CimOperationNameInvokeMethod;
            this.operationParameters.Clear();
            this.operationParameters.Add(@"namespaceName", namespaceName);
            this.operationParameters.Add(@"instance", instance);
            this.operationParameters.Add(@"methodName", methodName);
            this.WriteOperationStartMessage(this.operationName, this.operationParameters);
            CimAsyncMultipleResults<CimMethodResultBase> asyncResult = this.CimSession.InvokeMethodAsync(namespaceName, instance, methodName, methodParameters, this.OperationOptions);
            ConsumeCimInvokeMethodResultAsync(asyncResult, instance.CimSystemProperties.ClassName, methodName, new CimResultContext(instance));
        }

        /// <summary>
        /// Invoke static method of a given class asynchronously.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="methodParameters"></param>
        public void InvokeMethodAsync(
            string namespaceName,
            string className,
            string methodName,
            CimMethodParametersCollection methodParameters)
        {
            DebugHelper.WriteLogEx("EnableMethodResultStreaming = {0}", 0, this.OperationOptions.EnableMethodResultStreaming);
            this.CheckAvailability();
            this.TargetCimInstance = null;
            this.operationName = CimCmdletStrings.CimOperationNameInvokeMethod;
            this.operationParameters.Clear();
            this.operationParameters.Add(@"namespaceName", namespaceName);
            this.operationParameters.Add(@"className", className);
            this.operationParameters.Add(@"methodName", methodName);
            this.WriteOperationStartMessage(this.operationName, this.operationParameters);
            CimAsyncMultipleResults<CimMethodResultBase> asyncResult = this.CimSession.InvokeMethodAsync(namespaceName, className, methodName, methodParameters, this.OperationOptions);
            string errorSource = string.Create(CultureInfo.CurrentUICulture, $"{namespaceName}:{className}");
            ConsumeCimInvokeMethodResultAsync(asyncResult, className, methodName, new CimResultContext(errorSource));
        }

        /// <summary>
        /// <para>
        /// Subscribe to cim indication asynchronously
        /// </para>
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="queryDialect"></param>
        /// <param name="queryExpression"></param>
        public void SubscribeAsync(
            string namespaceName,
            string queryDialect,
            string queryExpression)
        {
            DebugHelper.WriteLogEx("QueryDialect = '{0}'; queryExpression = '{1}'", 0, queryDialect, queryExpression);
            this.CheckAvailability();
            this.TargetCimInstance = null;
            this.operationName = CimCmdletStrings.CimOperationNameSubscribeIndication;
            this.operationParameters.Clear();
            this.operationParameters.Add(@"namespaceName", namespaceName);
            this.operationParameters.Add(@"queryDialect", queryDialect);
            this.operationParameters.Add(@"queryExpression", queryExpression);
            this.WriteOperationStartMessage(this.operationName, this.operationParameters);

            this.OperationOptions.Flags |= CimOperationFlags.ReportOperationStarted;
            CimAsyncMultipleResults<CimSubscriptionResult> asyncResult = this.CimSession.SubscribeAsync(namespaceName, queryDialect, queryExpression, this.OperationOptions);
            ConsumeCimSubscriptionResultAsync(asyncResult, null);
        }

        /// <summary>
        /// <para>
        /// Test connection asynchronously
        /// </para>
        /// </summary>
        public void TestConnectionAsync()
        {
            DebugHelper.WriteLogEx("Start test connection", 0);
            this.CheckAvailability();
            this.TargetCimInstance = null;
            CimAsyncResult<CimInstance> asyncResult = this.CimSession.TestConnectionAsync();
            // ignore the test connection result objects
            ConsumeCimInstanceAsync(asyncResult, true, null);
        }

        #endregion

        #region pre action APIs
        /// <summary>
        /// Called before new action event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual bool PreNewActionEvent(CmdletActionEventArgs args)
        {
            return true;
        }
        /// <summary>
        /// Called before operation delete event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void PreOperationDeleteEvent(OperationEventArgs args)
        {
        }
        #endregion

        #region post action APIs

        /// <summary>
        /// Called after new action event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void PostNewActionEvent(CmdletActionEventArgs args)
        {
        }
        /// <summary>
        /// Called after operation create event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void PostOperationCreateEvent(OperationEventArgs args)
        {
        }
        /// <summary>
        /// Called after operation delete event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void PostOperationDeleteEvent(OperationEventArgs args)
        {
        }
        #endregion

        #region members

        /// <summary>
        /// <para>
        /// Unique operation ID
        /// </para>
        /// </summary>
        private long operationID;

        /// <summary>
        /// The CimSession object managed by this proxy object,
        /// which is either created by constructor OR passed in by caller.
        /// The session will be closed while disposing this proxy object
        /// if it is created by constructor.
        /// </summary>
        internal CimSession CimSession { get; private set; }

        /// <summary>
        /// The current CimInstance object, against which issued
        /// current operation, it could be null.
        /// </summary>
        internal CimInstance TargetCimInstance { get; private set; }

        internal bool IsTemporaryCimSession { get; private set; }

        /// <summary>
        /// The CimOperationOptions object, which specifies the options
        /// of the operation against the session object.
        /// Caller can control the timeout, method streaming support, and
        /// extended ps semantics support, etc.
        /// The setting MUST be set before start new operation on the
        /// this proxy object.
        /// </summary>
        internal CimOperationOptions OperationOptions { get; private set; }

        /// <summary>
        /// All operations completed.
        /// </summary>
        private bool Completed
        {
            get { return this.operation == null; }
        }

        /// <summary>
        /// Lock object used to lock
        /// operation & cancelOperation members.
        /// </summary>
        private readonly object stateLock = new();

        /// <summary>
        /// The operation issued by cimSession.
        /// </summary>
        private IObservable<object> operation;

        /// <summary>
        /// The current operation name.
        /// </summary>
        private string operationName;

        /// <summary>
        /// The current operation parameters.
        /// </summary>
        private readonly Hashtable operationParameters = new();

        /// <summary>
        /// Handler used to cancel operation.
        /// </summary>
        private IDisposable _cancelOperation;

        /// <summary>
        /// CancelOperation disposed flag.
        /// </summary>
        private int _cancelOperationDisposed = 0;

        /// <summary>
        /// Dispose the cancel operation.
        /// </summary>
        private void DisposeCancelOperation()
        {
            DebugHelper.WriteLogEx("CancelOperation Disposed = {0}", 0, this._cancelOperationDisposed);
            if (Interlocked.CompareExchange(ref this._cancelOperationDisposed, 1, 0) == 0)
            {
                if (this._cancelOperation != null)
                {
                    DebugHelper.WriteLog("CimSessionProxy::Dispose async operation.", 4);
                    this._cancelOperation.Dispose();
                    this._cancelOperation = null;
                }
            }
        }

        /// <summary>
        /// Set the cancel operation.
        /// </summary>
        private IDisposable CancelOperation
        {
            get
            {
                return this._cancelOperation;
            }

            set
            {
                DebugHelper.WriteLogEx();
                this._cancelOperation = value;
                Interlocked.Exchange(ref this._cancelOperationDisposed, 0);
            }
        }

        /// <summary>
        /// Current protocol name
        /// DCOM or WSMAN.
        /// </summary>
        internal ProtocolType Protocol { get; private set; }

        /// <summary>
        /// Cross operation context object.
        /// </summary>
        internal XOperationContextBase ContextObject { get; set; }

        /// <summary>
        /// Invocation context object.
        /// </summary>
        private InvocationContext invocationContextObject;

        /// <summary>
        /// A preprocess object to pre-processing the result object,
        /// for example, adding PSTypeName, etc.
        /// </summary>
        internal IObjectPreProcess ObjectPreProcess { get; set; }

        /// <summary>
        /// <see cref="isDefaultSession"/> is <see langword="true"/> if this <see cref="CimSessionProxy"/> was
        /// created to handle the "default" session, in cases where cmdlets are invoked without
        /// ComputerName and/or CimSession parameters.
        /// </summary>
        private readonly bool isDefaultSession;

        #endregion

        #region IDisposable

        /// <summary>
        /// IDisposable interface.
        /// </summary>
        private int _disposed;

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected virtual void Dispose(bool disposing)
        {
            DebugHelper.WriteLogEx("Disposed = {0}", 0, this.IsDisposed);

            if (Interlocked.CompareExchange(ref this._disposed, 1, 0) == 0)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                    this.DisposeCancelOperation();

                    if (this.OperationOptions != null)
                    {
                        this.OperationOptions.Dispose();
                        this.OperationOptions = null;
                    }

                    DisposeTemporaryCimSession();
                }
            }
        }

        public bool IsDisposed
        {
            get
            {
                return this._disposed == 1;
            }
        }

        /// <summary>
        /// <para>
        /// Dispose temporary <see cref="CimSession"/>.
        /// </para>
        /// </summary>
        private void DisposeTemporaryCimSession()
        {
            if (this.IsTemporaryCimSession && this.CimSession != null)
            {
                // remove the cimsession from temporary cache
                RemoveCimSessionFromTemporaryCache(this.CimSession);
                this.IsTemporaryCimSession = false;
                this.CimSession = null;
            }
        }
        #endregion

        #region helper methods

        /// <summary>
        /// <para>
        /// Consume the results of async operations
        /// </para>
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <param name="cimResultContext"></param>
        protected void ConsumeCimInstanceAsync(IObservable<CimInstance> asyncResult,
            CimResultContext cimResultContext)
        {
            ConsumeCimInstanceAsync(asyncResult, false, cimResultContext);
        }

        /// <summary>
        /// <para>
        /// Consume the CimInstance results of async operations.
        /// </para>
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <param name="ignoreResultObjects"></param>
        /// <param name="cimResultContext"></param>
        protected void ConsumeCimInstanceAsync(
            IObservable<CimInstance> asyncResult,
            bool ignoreResultObjects,
            CimResultContext cimResultContext)
        {
            CimResultObserver<CimInstance> observer;
            if (ignoreResultObjects)
            {
                observer = new IgnoreResultObserver(this.CimSession, asyncResult);
            }
            else
            {
                observer = new CimResultObserver<CimInstance>(this.CimSession, asyncResult, cimResultContext);
            }

            observer.OnNewResult += this.ResultEventHandler;
            this.operationID = Interlocked.Increment(ref gOperationCounter);
            this.AddOperation(asyncResult);
            this.CancelOperation = asyncResult.Subscribe(observer);
            this.FireOperationCreatedEvent(this.CancelOperation, asyncResult);
        }

        /// <summary>
        /// <para>
        /// Consume the results of async operations
        /// </para>
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <param name="cimResultContext"></param>
        protected void ConsumeObjectAsync(IObservable<object> asyncResult,
            CimResultContext cimResultContext)
        {
            CimResultObserver<object> observer = new(
                this.CimSession, asyncResult, cimResultContext);

            observer.OnNewResult += this.ResultEventHandler;
            this.operationID = Interlocked.Increment(ref gOperationCounter);
            this.AddOperation(asyncResult);
            this.CancelOperation = asyncResult.Subscribe(observer);
            DebugHelper.WriteLog("FireOperationCreatedEvent");
            this.FireOperationCreatedEvent(this.CancelOperation, asyncResult);
        }

        /// <summary>
        /// <para>
        /// Consume the <see cref="CimClass"/> of async operations
        /// </para>
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <param name="cimResultContext"></param>
        protected void ConsumeCimClassAsync(IObservable<CimClass> asyncResult,
            CimResultContext cimResultContext)
        {
            CimResultObserver<CimClass> observer = new(
                this.CimSession, asyncResult, cimResultContext);

            observer.OnNewResult += this.ResultEventHandler;
            this.operationID = Interlocked.Increment(ref gOperationCounter);
            this.AddOperation(asyncResult);
            this.CancelOperation = asyncResult.Subscribe(observer);
            this.FireOperationCreatedEvent(this.CancelOperation, asyncResult);
        }

        /// <summary>
        /// <para>
        /// Consume the <see cref="CimSubscriptionResult"/> of async operations
        /// </para>
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <param name="cimResultContext"></param>
        protected void ConsumeCimSubscriptionResultAsync(
            IObservable<CimSubscriptionResult> asyncResult,
            CimResultContext cimResultContext)
        {
            CimSubscriptionResultObserver observer = new(
                this.CimSession, asyncResult, cimResultContext);
            observer.OnNewResult += this.ResultEventHandler;
            this.operationID = Interlocked.Increment(ref gOperationCounter);
            this.AddOperation(asyncResult);
            this.CancelOperation = asyncResult.Subscribe(observer);
            this.FireOperationCreatedEvent(this.CancelOperation, asyncResult);
        }

        /// <summary>
        /// <para>
        /// Consume the <see cref="CimMethodResultBase"/> of async operations
        /// </para>
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="cimResultContext"></param>
        protected void ConsumeCimInvokeMethodResultAsync(
            IObservable<CimMethodResultBase> asyncResult,
            string className,
            string methodName,
            CimResultContext cimResultContext)
        {
            CimMethodResultObserver observer = new(this.CimSession, asyncResult, cimResultContext)
            {
                ClassName = className,
                MethodName = methodName
            };

            observer.OnNewResult += this.ResultEventHandler;
            this.operationID = Interlocked.Increment(ref gOperationCounter);
            this.AddOperation(asyncResult);
            this.CancelOperation = asyncResult.Subscribe(observer);
            this.FireOperationCreatedEvent(this.CancelOperation, asyncResult);
        }

        /// <summary>
        /// <para>
        /// Check whether current proxy object is available
        /// </para>
        /// </summary>
        private void CheckAvailability()
        {
            DebugHelper.WriteLogEx();

            AssertSession();
            lock (this.stateLock)
            {
                if (!this.Completed)
                {
                    throw new InvalidOperationException(CimCmdletStrings.OperationInProgress);
                }
            }

            DebugHelper.WriteLog("KeyOnly {0},", 1, this.OperationOptions.KeysOnly);
        }

        /// <summary>
        /// <para>
        /// Check the wrapped <see cref="CimSession"/> object
        /// </para>
        /// </summary>
        private void AssertSession()
        {
            if (this.IsDisposed || (this.CimSession == null))
            {
                DebugHelper.WriteLogEx("Invalid CimSessionProxy object, disposed? {0}; session object {1}", 1, this.IsDisposed, this.CimSession);
                throw new ObjectDisposedException(this.ToString());
            }
        }

        /// <summary>
        /// <para>
        /// Create <see cref="CimSessionOptions"/> based on the given computerName
        /// </para>
        /// </summary>
        /// <returns></returns>
        private CimSession CreateCimSessionByComputerName(string computerName)
        {
            DebugHelper.WriteLogEx("ComputerName {0}", 0, computerName);

            CimSessionOptions option = CreateCimSessionOption(computerName, 0, null);
            if (option is DComSessionOptions)
            {
                DebugHelper.WriteLog("Create dcom cimSession");
                this.Protocol = ProtocolType.Dcom;
                return CimSession.Create(ConstValue.NullComputerName, option);
            }
            else
            {
                DebugHelper.WriteLog("Create wsman cimSession");
                return CimSession.Create(computerName, option);
            }
        }

        /// <summary>
        /// <para>
        /// Create <see cref="CimSessionOptions"/> based on the given computerName,
        /// timeout and credential
        /// </para>
        /// </summary>
        /// <param name="computerName"></param>
        /// <param name="timeout"></param>
        /// <param name="credential"></param>
        /// <returns></returns>
        internal static CimSessionOptions CreateCimSessionOption(string computerName,
            uint timeout, CimCredential credential)
        {
            DebugHelper.WriteLogEx();

            CimSessionOptions option;
            if (ConstValue.IsDefaultComputerName(computerName))
            {
                DebugHelper.WriteLog("<<<<<<<<<< Use protocol DCOM  {0}", 1, computerName);
                option = new DComSessionOptions();
            }
            else
            {
                DebugHelper.WriteLog("<<<<<<<<<< Use protocol WSMAN {0}", 1, computerName);
                option = new WSManSessionOptions();
            }

            if (timeout != 0)
            {
                option.Timeout = TimeSpan.FromSeconds((double)timeout);
            }

            if (credential != null)
            {
                option.AddDestinationCredentials(credential);
            }

            DebugHelper.WriteLogEx("returned option :{0}.", 1, option);
            return option;
        }

        #endregion

    }

    #region class CimSessionProxyTestConnection
    /// <summary>
    /// <para>
    /// Write session to pipeline after test connection success
    /// </para>
    /// </summary>
    internal class CimSessionProxyTestConnection : CimSessionProxy
    {
        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionProxyTestConnection"/> class.
        /// </summary>
        /// <remarks>
        /// Create <see cref="CimSession"/> by given computer name
        /// and session options.
        /// Then create wrapper object.
        /// </remarks>
        /// <param name="computerName"></param>
        /// <param name="sessionOptions"></param>
        public CimSessionProxyTestConnection(string computerName, CimSessionOptions sessionOptions)
            : base(computerName, sessionOptions)
        {
        }

        #endregion

        #region pre action APIs

        /// <summary>
        /// Called after operation delete event.
        /// </summary>
        /// <param name="args"></param>
        protected override void PreOperationDeleteEvent(OperationEventArgs args)
        {
            DebugHelper.WriteLogEx("test connection result {0}", 0, args.success);

            if (args.success)
            {
                // test connection success, write session object to pipeline
                CimWriteResultObject result = new(this.CimSession, this.ContextObject);
                this.FireNewActionEvent(result);
            }
        }

        #endregion
    }

    #endregion

    #region class CimSessionProxyGetCimClass

    /// <summary>
    /// <para>
    /// Write CimClass to pipeline if the CimClass satisfied
    /// the given conditions
    /// </para>
    /// </summary>
    internal class CimSessionProxyGetCimClass : CimSessionProxy
    {
        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionProxyGetCimClass"/> class.
        /// </summary>
        /// <remarks>
        /// Create <see cref="CimSession"/> by given computer name.
        /// Then create wrapper object.
        /// </remarks>
        /// <param name="computerName"></param>
        public CimSessionProxyGetCimClass(string computerName)
            : base(computerName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionProxyGetCimClass"/> class.
        /// </summary>
        /// <remarks>
        /// Create <see cref="CimSession"/> by given computer name
        /// and session options.
        /// Then create wrapper object.
        /// </remarks>
        /// <param name="computerName"></param>
        /// <param name="sessionOptions"></param>
        public CimSessionProxyGetCimClass(CimSession session)
            : base(session)
        {
        }

        #endregion

        #region pre action APIs
        /// <summary>
        /// Called before new action event.
        /// </summary>
        /// <param name="args"></param>
        protected override bool PreNewActionEvent(CmdletActionEventArgs args)
        {
            DebugHelper.WriteLogEx();

            if (args.Action is not CimWriteResultObject)
            {
                // allow all other actions
                return true;
            }

            CimWriteResultObject writeResultObject = args.Action as CimWriteResultObject;
            if (!(writeResultObject.Result is CimClass cimClass))
            {
                return true;
            }

            DebugHelper.WriteLog("class name = {0}", 1, cimClass.CimSystemProperties.ClassName);

            CimGetCimClassContext context = this.ContextObject as CimGetCimClassContext;
            Debug.Assert(context != null, "Caller should verify that CimGetCimClassContext != NULL.");

            WildcardPattern pattern;
            if (WildcardPattern.ContainsWildcardCharacters(context.ClassName))
            {
                pattern = new WildcardPattern(context.ClassName, WildcardOptions.IgnoreCase);
                if (!pattern.IsMatch(cimClass.CimSystemProperties.ClassName))
                {
                    return false;
                }
            }

            if (context.PropertyName != null)
            {
                bool match = false;
                if (cimClass.CimClassProperties != null)
                {
                    pattern = new WildcardPattern(context.PropertyName, WildcardOptions.IgnoreCase);
                    foreach (CimPropertyDeclaration decl in cimClass.CimClassProperties)
                    {
                        DebugHelper.WriteLog("--- property name : {0}", 1, decl.Name);
                        if (pattern.IsMatch(decl.Name))
                        {
                            match = true;
                            break;
                        }
                    }
                }

                if (!match)
                {
                    DebugHelper.WriteLog("Property name does not match: {0}", 1, context.PropertyName);
                    return match;
                }
            }

            if (context.MethodName != null)
            {
                bool match = false;
                if (cimClass.CimClassMethods != null)
                {
                    pattern = new WildcardPattern(context.MethodName, WildcardOptions.IgnoreCase);
                    foreach (CimMethodDeclaration decl in cimClass.CimClassMethods)
                    {
                        DebugHelper.WriteLog("--- method name : {0}", 1, decl.Name);
                        if (pattern.IsMatch(decl.Name))
                        {
                            match = true;
                            break;
                        }
                    }
                }

                if (!match)
                {
                    DebugHelper.WriteLog("Method name does not match: {0}", 1, context.MethodName);
                    return match;
                }
            }

            if (context.QualifierName != null)
            {
                bool match = false;
                if (cimClass.CimClassQualifiers != null)
                {
                    pattern = new WildcardPattern(context.QualifierName, WildcardOptions.IgnoreCase);
                    foreach (CimQualifier qualifier in cimClass.CimClassQualifiers)
                    {
                        DebugHelper.WriteLog("--- qualifier name : {0}", 1, qualifier.Name);
                        if (pattern.IsMatch(qualifier.Name))
                        {
                            match = true;
                            break;
                        }
                    }
                }

                if (!match)
                {
                    DebugHelper.WriteLog("Qualifier name does not match: {0}", 1, context.QualifierName);
                    return match;
                }
            }

            DebugHelper.WriteLog("CimClass '{0}' is qualified.", 1, cimClass.CimSystemProperties.ClassName);
            return true;
        }
        #endregion
    }

    #endregion

    #region class CimSessionProxyNewCimInstance

    /// <summary>
    /// <para>
    /// Get full <see cref="CimInstance"/> if create successfully.
    /// </para>
    /// </summary>
    internal class CimSessionProxyNewCimInstance : CimSessionProxy
    {
        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionProxyNewCimInstance"/> class.
        /// </summary>
        /// <remarks>
        /// Create <see cref="CimSession"/> by given computer name.
        /// Then create wrapper object.
        /// </remarks>
        public CimSessionProxyNewCimInstance(string computerName, CimNewCimInstance operation)
            : base(computerName)
        {
            this.NewCimInstanceOperation = operation;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionProxyNewCimInstance"/> class.
        /// </summary>
        /// <param name="computerName"></param>
        /// <remarks>
        /// Create <see cref="CimSession"/> by given computer name
        /// and session options.
        /// Then create wrapper object.
        /// </remarks>
        /// <param name="computerName"></param>
        /// <param name="sessionOptions"></param>
        public CimSessionProxyNewCimInstance(CimSession session, CimNewCimInstance operation)
            : base(session)
        {
            this.NewCimInstanceOperation = operation;
        }

        #endregion

        #region pre action APIs
        /// <summary>
        /// Called before new action event.
        /// </summary>
        /// <param name="args"></param>
        protected override bool PreNewActionEvent(CmdletActionEventArgs args)
        {
            DebugHelper.WriteLogEx();

            if (args.Action is not CimWriteResultObject)
            {
                // allow all other actions
                return true;
            }

            CimWriteResultObject writeResultObject = args.Action as CimWriteResultObject;
            if (!(writeResultObject.Result is CimInstance cimInstance))
            {
                return true;
            }

            DebugHelper.WriteLog("Going to read CimInstance classname = {0}; namespace = {1}", 1, cimInstance.CimSystemProperties.ClassName, cimInstance.CimSystemProperties.Namespace);
            this.NewCimInstanceOperation.GetCimInstance(cimInstance, this.ContextObject);
            return false;
        }
        #endregion

        #region private members

        internal CimNewCimInstance NewCimInstanceOperation { get; }

        #endregion
    }

    #endregion

    #region class CimSessionProxyNewCimInstance

    /// <summary>
    /// <para>
    /// Support PassThru for set-ciminstance.
    /// </para>
    /// </summary>
    internal class CimSessionProxySetCimInstance : CimSessionProxy
    {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionProxySetCimInstance"/> class.
        /// </summary>
        /// <remarks>
        /// Create <see cref="CimSession"/> by given <see cref="CimSessionProxy"/> object.
        /// Then create wrapper object.
        /// </remarks>
        /// <param name="originalProxy"><see cref="CimSessionProxy"/> object to clone.</param>
        /// <param name="passThru">PassThru, true means output the modified instance; otherwise does not output.</param>
        public CimSessionProxySetCimInstance(CimSessionProxy originalProxy, bool passThru)
            : base(originalProxy)
        {
            this.passThru = passThru;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionProxySetCimInstance"/> class.
        /// </summary>
        /// <remarks>
        /// Create <see cref="CimSession"/> by given computer name.
        /// Then create wrapper object.
        /// </remarks>
        /// <param name="computerName"></param>
        /// <param name="cimInstance"></param>
        /// <param name="passThru"></param>
        public CimSessionProxySetCimInstance(string computerName,
            CimInstance cimInstance,
            bool passThru)
            : base(computerName, cimInstance)
        {
            this.passThru = passThru;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionProxySetCimInstance"/> class.
        /// </summary>
        /// <remarks>
        /// Create <see cref="CimSession"/> by given computer name
        /// and session options.
        /// Then create wrapper object.
        /// </remarks>
        /// <param name="computerName"></param>
        /// <param name="sessionOptions"></param>
        public CimSessionProxySetCimInstance(CimSession session, bool passThru)
            : base(session)
        {
            this.passThru = passThru;
        }
        #endregion

        #region pre action APIs
        /// <summary>
        /// Called before new action event.
        /// </summary>
        /// <param name="args"></param>
        protected override bool PreNewActionEvent(CmdletActionEventArgs args)
        {
            DebugHelper.WriteLogEx();

            if ((!this.passThru) && (args.Action is CimWriteResultObject))
            {
                // filter out any output object
                return false;
            }

            return true;
        }
        #endregion

        #region private members

        /// <summary>
        /// Ture indicates need to output the modified result.
        /// </summary>
        private readonly bool passThru = false;

        #endregion
    }

    #endregion
}
