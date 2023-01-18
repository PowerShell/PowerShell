// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.Management.Infrastructure.Options;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    #region CimSessionWrapper

    internal class CimSessionWrapper
    {
        #region members

        /// <summary>
        /// Id of the cimsession.
        /// </summary>
        public uint SessionId { get; }

        /// <summary>
        /// InstanceId of the cimsession.
        /// </summary>
        public Guid InstanceId { get; }

        /// <summary>
        /// Name of the cimsession.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Computer name of the cimsession.
        /// </summary>
        public string ComputerName { get; }

        /// <summary>
        /// Wrapped cimsession object.
        /// </summary>
        public CimSession CimSession { get; }

        /// <summary>
        /// Computer name of the cimsession.
        /// </summary>
        public string Protocol
        {
            get
            {
                switch (protocol)
                {
                    case ProtocolType.Dcom:
                        return "DCOM";
                    case ProtocolType.Default:
                    case ProtocolType.Wsman:
                    default:
                        return "WSMAN";
                }
            }
        }

        internal ProtocolType GetProtocolType()
        {
            return protocol;
        }

        private readonly ProtocolType protocol;

        /// <summary>
        /// PSObject that wrapped the cimSession.
        /// </summary>
        private PSObject psObject;

        #endregion

        internal CimSessionWrapper(
            uint theSessionId,
            Guid theInstanceId,
            string theName,
            string theComputerName,
            CimSession theCimSession,
            ProtocolType theProtocol)
        {
            this.SessionId = theSessionId;
            this.InstanceId = theInstanceId;
            this.Name = theName;
            this.ComputerName = theComputerName;
            this.CimSession = theCimSession;
            this.psObject = null;
            this.protocol = theProtocol;
        }

        internal PSObject GetPSObject()
        {
            if (psObject == null)
            {
                psObject = new PSObject(this.CimSession);
                psObject.Properties.Add(new PSNoteProperty(CimSessionState.idPropName, this.SessionId));
                psObject.Properties.Add(new PSNoteProperty(CimSessionState.namePropName, this.Name));
                psObject.Properties.Add(new PSNoteProperty(CimSessionState.instanceidPropName, this.InstanceId));
                psObject.Properties.Add(new PSNoteProperty(CimSessionState.computernamePropName, this.ComputerName));
                psObject.Properties.Add(new PSNoteProperty(CimSessionState.protocolPropName, this.Protocol));
            }
            else
            {
                psObject.Properties[CimSessionState.idPropName].Value = this.SessionId;
                psObject.Properties[CimSessionState.namePropName].Value = this.Name;
                psObject.Properties[CimSessionState.instanceidPropName].Value = this.InstanceId;
                psObject.Properties[CimSessionState.computernamePropName].Value = this.ComputerName;
                psObject.Properties[CimSessionState.protocolPropName].Value = this.Protocol;
            }

            return psObject;
        }
    }

    #endregion

    #region CimSessionState

    /// <summary>
    /// <para>
    /// Class used to hold all cimsession related status data related to a runspace.
    /// Including the CimSession cache, session counters for generating session name.
    /// </para>
    /// </summary>
    internal class CimSessionState : IDisposable
    {
        #region private members

        /// <summary>
        /// Default session name.
        /// If a name is not passed, then the session is given the name CimSession<int>,
        /// where <int> is the next available session number.
        /// For example, CimSession1, CimSession2, etc...
        /// </summary>
        internal static readonly string CimSessionClassName = "CimSession";

        /// <summary>
        /// CimSession object name.
        /// </summary>
        internal static readonly string CimSessionObject = "{CimSession Object}";

        /// <summary>
        /// <para>
        /// CimSession object path, which is identifying a cimsession object
        /// </para>
        /// </summary>
        internal static readonly string SessionObjectPath = @"CimSession id = {0}, name = {2}, ComputerName = {3}, instance id = {1}";

        /// <summary>
        /// Id property name of cimsession wrapper object.
        /// </summary>
        internal static readonly string idPropName = "Id";

        /// <summary>
        /// Instanceid property name of cimsession wrapper object.
        /// </summary>
        internal static readonly string instanceidPropName = "InstanceId";

        /// <summary>
        /// Name property name of cimsession wrapper object.
        /// </summary>
        internal static readonly string namePropName = "Name";

        /// <summary>
        /// Computer name property name of cimsession object.
        /// </summary>
        internal static readonly string computernamePropName = "ComputerName";

        /// <summary>
        /// Protocol name property name of cimsession object.
        /// </summary>
        internal static readonly string protocolPropName = "Protocol";

        /// <summary>
        /// <para>
        /// Session counter bound to current runspace.
        /// </para>
        /// </summary>
        private uint sessionNameCounter;

        /// <summary>
        /// <para>
        /// Dictionary used to holds all CimSessions in current runspace by session name.
        /// </para>
        /// </summary>
        private readonly Dictionary<string, HashSet<CimSessionWrapper>> curCimSessionsByName;

        /// <summary>
        /// <para>
        /// Dictionary used to holds all CimSessions in current runspace by computer name.
        /// </para>
        /// </summary>
        private readonly Dictionary<string, HashSet<CimSessionWrapper>> curCimSessionsByComputerName;

        /// <summary>
        /// <para>
        /// Dictionary used to holds all CimSessions in current runspace by instance ID.
        /// </para>
        /// </summary>
        private readonly Dictionary<Guid, CimSessionWrapper> curCimSessionsByInstanceId;

        /// <summary>
        /// <para>
        /// Dictionary used to holds all CimSessions in current runspace by session id.
        /// </para>
        /// </summary>
        private readonly Dictionary<uint, CimSessionWrapper> curCimSessionsById;

        /// <summary>
        /// <para>
        /// Dictionary used to link CimSession object with PSObject.
        /// </para>
        /// </summary>
        private readonly Dictionary<CimSession, CimSessionWrapper> curCimSessionWrapper;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionState"/> class.
        /// </summary>
        internal CimSessionState()
        {
            sessionNameCounter = 1;
            curCimSessionsByName = new Dictionary<string, HashSet<CimSessionWrapper>>(
                StringComparer.OrdinalIgnoreCase);
            curCimSessionsByComputerName = new Dictionary<string, HashSet<CimSessionWrapper>>(
                StringComparer.OrdinalIgnoreCase);
            curCimSessionsByInstanceId = new Dictionary<Guid, CimSessionWrapper>();
            curCimSessionsById = new Dictionary<uint, CimSessionWrapper>();
            curCimSessionWrapper = new Dictionary<CimSession, CimSessionWrapper>();
        }

        /// <summary>
        /// <para>
        /// Get sessions count.
        /// </para>
        /// </summary>
        /// <returns>The count of session objects in current runspace.</returns>
        internal int GetSessionsCount()
        {
            return this.curCimSessionsById.Count;
        }

        /// <summary>
        /// <para>
        /// Generates an unique session id.
        /// </para>
        /// </summary>
        /// <returns>Unique session id under current runspace.</returns>
        internal uint GenerateSessionId()
        {
            return this.sessionNameCounter++;
        }
        #region IDisposable

        /// <summary>
        /// <para>
        /// Indicates whether this object was disposed or not.
        /// </para>
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// <para>
        /// Dispose() calls Dispose(true).
        /// Implement IDisposable. Do not make this method virtual.
        /// A derived class should not be able to override this method.
        /// </para>
        /// </summary>
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

        /// <summary>
        /// <para>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </para>
        /// </summary>
        /// <param name="disposing">Whether it is directly called.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    // free managed resources
                    Cleanup();
                    this._disposed = true;
                }
                // free native resources if there are any
            }
        }

        /// <summary>
        /// <para>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources.
        /// </para>
        /// </summary>
        public void Cleanup()
        {
            foreach (CimSession session in curCimSessionWrapper.Keys)
            {
                session.Dispose();
            }

            curCimSessionWrapper.Clear();
            curCimSessionsByName.Clear();
            curCimSessionsByComputerName.Clear();
            curCimSessionsByInstanceId.Clear();
            curCimSessionsById.Clear();
            sessionNameCounter = 1;
        }

        #endregion

        #region Add CimSession to/remove CimSession from cache

        /// <summary>
        /// <para>
        /// Add new CimSession object to cache.
        /// </para>
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sessionId"></param>
        /// <param name="instanceId"></param>
        /// <param name="name"></param>
        /// <param name="computerName"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        internal PSObject AddObjectToCache(
            CimSession session,
            uint sessionId,
            Guid instanceId,
            string name,
            string computerName,
            ProtocolType protocol)
        {
            CimSessionWrapper wrapper = new(
                sessionId, instanceId, name, computerName, session, protocol);

            HashSet<CimSessionWrapper> objects;
            if (!this.curCimSessionsByComputerName.TryGetValue(computerName, out objects))
            {
                objects = new HashSet<CimSessionWrapper>();
                this.curCimSessionsByComputerName.Add(computerName, objects);
            }

            objects.Add(wrapper);

            if (!this.curCimSessionsByName.TryGetValue(name, out objects))
            {
                objects = new HashSet<CimSessionWrapper>();
                this.curCimSessionsByName.Add(name, objects);
            }

            objects.Add(wrapper);

            this.curCimSessionsByInstanceId.Add(instanceId, wrapper);
            this.curCimSessionsById.Add(sessionId, wrapper);
            this.curCimSessionWrapper.Add(session, wrapper);
            return wrapper.GetPSObject();
        }

        /// <summary>
        /// <para>
        /// Generates remove session message by given wrapper object.
        /// </para>
        /// </summary>
        /// <param name="psObject"></param>
        internal string GetRemoveSessionObjectTarget(PSObject psObject)
        {
            string message = string.Empty;
            if (psObject.BaseObject is CimSession)
            {
                uint id = 0x0;
                Guid instanceId = Guid.Empty;
                string name = string.Empty;
                string computerName = string.Empty;
                if (psObject.Properties[idPropName].Value is uint)
                {
                    id = Convert.ToUInt32(psObject.Properties[idPropName].Value, null);
                }

                if (psObject.Properties[instanceidPropName].Value is Guid)
                {
                    instanceId = (Guid)psObject.Properties[instanceidPropName].Value;
                }

                if (psObject.Properties[namePropName].Value is string)
                {
                    name = (string)psObject.Properties[namePropName].Value;
                }

                if (psObject.Properties[computernamePropName].Value is string)
                {
                    computerName = (string)psObject.Properties[computernamePropName].Value;
                }

                message = string.Format(CultureInfo.CurrentUICulture, SessionObjectPath, id, instanceId, name, computerName);
            }

            return message;
        }

        /// <summary>
        /// <para>
        /// Remove given <see cref="PSObject"/> object from cache.
        /// </para>
        /// </summary>
        /// <param name="psObject"></param>
        internal void RemoveOneSessionObjectFromCache(PSObject psObject)
        {
            DebugHelper.WriteLogEx();

            if (psObject.BaseObject is CimSession)
            {
                RemoveOneSessionObjectFromCache(psObject.BaseObject as CimSession);
            }
        }

        /// <summary>
        /// <para>
        /// Remove given <see cref="CimSession"/> object from cache.
        /// </para>
        /// </summary>
        /// <param name="session"></param>
        internal void RemoveOneSessionObjectFromCache(CimSession session)
        {
            DebugHelper.WriteLogEx();

            if (!this.curCimSessionWrapper.ContainsKey(session))
            {
                return;
            }

            CimSessionWrapper wrapper = this.curCimSessionWrapper[session];
            string name = wrapper.Name;
            string computerName = wrapper.ComputerName;

            DebugHelper.WriteLog("name {0}, computername {1}, id {2}, instanceId {3}", 1, name, computerName, wrapper.SessionId, wrapper.InstanceId);

            HashSet<CimSessionWrapper> objects;
            if (this.curCimSessionsByComputerName.TryGetValue(computerName, out objects))
            {
                objects.Remove(wrapper);
            }

            if (this.curCimSessionsByName.TryGetValue(name, out objects))
            {
                objects.Remove(wrapper);
            }

            RemoveSessionInternal(session, wrapper);
        }

        /// <summary>
        /// <para>
        /// Remove given <see cref="CimSession"/> object from partial of the cache only.
        /// </para>
        /// </summary>
        /// <param name="session"></param>
        /// <param name="psObject"></param>
        private void RemoveSessionInternal(CimSession session, CimSessionWrapper wrapper)
        {
            DebugHelper.WriteLogEx();

            this.curCimSessionsByInstanceId.Remove(wrapper.InstanceId);
            this.curCimSessionsById.Remove(wrapper.SessionId);
            this.curCimSessionWrapper.Remove(session);
            session.Dispose();
        }

        #endregion

        #region Query CimSession from cache

        /// <summary>
        /// <para>
        /// Add ErrorRecord to list.
        /// </para>
        /// </summary>
        /// <param name="errRecords"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        private static void AddErrorRecord(
            ref List<ErrorRecord> errRecords,
            string propertyName,
            object propertyValue)
        {
            errRecords.Add(
                new ErrorRecord(
                    new CimException(string.Format(CultureInfo.CurrentUICulture, CimCmdletStrings.CouldNotFindCimsessionObject, propertyName, propertyValue)),
                    string.Empty,
                    ErrorCategory.ObjectNotFound,
                    null));
        }

        /// <summary>
        /// Query session list by given id array.
        /// </summary>
        /// <param name="ids"></param>
        /// <returns>List of session wrapper objects.</returns>
        internal IEnumerable<PSObject> QuerySession(
            IEnumerable<uint> ids,
            out IEnumerable<ErrorRecord> errorRecords)
        {
            HashSet<PSObject> sessions = new();
            HashSet<uint> sessionIds = new();
            List<ErrorRecord> errRecords = new();
            errorRecords = errRecords;
            // NOTES: use template function to implement this will save duplicate code
            foreach (uint id in ids)
            {
                if (this.curCimSessionsById.ContainsKey(id))
                {
                    if (!sessionIds.Contains(id))
                    {
                        sessionIds.Add(id);
                        sessions.Add(this.curCimSessionsById[id].GetPSObject());
                    }
                }
                else
                {
                    AddErrorRecord(ref errRecords, idPropName, id);
                }
            }

            return sessions;
        }

        /// <summary>
        /// Query session list by given instance id array.
        /// </summary>
        /// <param name="instanceIds"></param>
        /// <returns>List of session wrapper objects.</returns>
        internal IEnumerable<PSObject> QuerySession(
            IEnumerable<Guid> instanceIds,
            out IEnumerable<ErrorRecord> errorRecords)
        {
            HashSet<PSObject> sessions = new();
            HashSet<uint> sessionIds = new();
            List<ErrorRecord> errRecords = new();
            errorRecords = errRecords;
            foreach (Guid instanceid in instanceIds)
            {
                if (this.curCimSessionsByInstanceId.ContainsKey(instanceid))
                {
                    CimSessionWrapper wrapper = this.curCimSessionsByInstanceId[instanceid];
                    if (!sessionIds.Contains(wrapper.SessionId))
                    {
                        sessionIds.Add(wrapper.SessionId);
                        sessions.Add(wrapper.GetPSObject());
                    }
                }
                else
                {
                    AddErrorRecord(ref errRecords, instanceidPropName, instanceid);
                }
            }

            return sessions;
        }

        /// <summary>
        /// Query session list by given name array.
        /// </summary>
        /// <param name="nameArray"></param>
        /// <returns>List of session wrapper objects.</returns>
        internal IEnumerable<PSObject> QuerySession(IEnumerable<string> nameArray,
            out IEnumerable<ErrorRecord> errorRecords)
        {
            HashSet<PSObject> sessions = new();
            HashSet<uint> sessionIds = new();
            List<ErrorRecord> errRecords = new();
            errorRecords = errRecords;
            foreach (string name in nameArray)
            {
                bool foundSession = false;
                WildcardPattern pattern = new(name, WildcardOptions.IgnoreCase);
                foreach (KeyValuePair<string, HashSet<CimSessionWrapper>> kvp in this.curCimSessionsByName)
                {
                    if (pattern.IsMatch(kvp.Key))
                    {
                        HashSet<CimSessionWrapper> wrappers = kvp.Value;
                        foundSession = wrappers.Count > 0;
                        foreach (CimSessionWrapper wrapper in wrappers)
                        {
                            if (!sessionIds.Contains(wrapper.SessionId))
                            {
                                sessionIds.Add(wrapper.SessionId);
                                sessions.Add(wrapper.GetPSObject());
                            }
                        }
                    }
                }

                if (!foundSession && !WildcardPattern.ContainsWildcardCharacters(name))
                {
                    AddErrorRecord(ref errRecords, namePropName, name);
                }
            }

            return sessions;
        }

        /// <summary>
        /// Query session list by given computer name array.
        /// </summary>
        /// <param name="computernameArray"></param>
        /// <returns>List of session wrapper objects.</returns>
        internal IEnumerable<PSObject> QuerySessionByComputerName(
            IEnumerable<string> computernameArray,
            out IEnumerable<ErrorRecord> errorRecords)
        {
            HashSet<PSObject> sessions = new();
            HashSet<uint> sessionIds = new();
            List<ErrorRecord> errRecords = new();
            errorRecords = errRecords;
            foreach (string computername in computernameArray)
            {
                bool foundSession = false;
                if (this.curCimSessionsByComputerName.ContainsKey(computername))
                {
                    HashSet<CimSessionWrapper> wrappers = this.curCimSessionsByComputerName[computername];
                    foundSession = wrappers.Count > 0;
                    foreach (CimSessionWrapper wrapper in wrappers)
                    {
                        if (!sessionIds.Contains(wrapper.SessionId))
                        {
                            sessionIds.Add(wrapper.SessionId);
                            sessions.Add(wrapper.GetPSObject());
                        }
                    }
                }

                if (!foundSession)
                {
                    AddErrorRecord(ref errRecords, computernamePropName, computername);
                }
            }

            return sessions;
        }

        /// <summary>
        /// Query session list by given session objects array.
        /// </summary>
        /// <param name="cimsessions"></param>
        /// <returns>List of session wrapper objects.</returns>
        internal IEnumerable<PSObject> QuerySession(IEnumerable<CimSession> cimsessions,
            out IEnumerable<ErrorRecord> errorRecords)
        {
            HashSet<PSObject> sessions = new();
            HashSet<uint> sessionIds = new();
            List<ErrorRecord> errRecords = new();
            errorRecords = errRecords;
            foreach (CimSession cimsession in cimsessions)
            {
                if (this.curCimSessionWrapper.ContainsKey(cimsession))
                {
                    CimSessionWrapper wrapper = this.curCimSessionWrapper[cimsession];
                    if (!sessionIds.Contains(wrapper.SessionId))
                    {
                        sessionIds.Add(wrapper.SessionId);
                        sessions.Add(wrapper.GetPSObject());
                    }
                }
                else
                {
                    AddErrorRecord(ref errRecords, CimSessionClassName, CimSessionObject);
                }
            }

            return sessions;
        }

        /// <summary>
        /// Query session wrapper object.
        /// </summary>
        /// <param name="cimsessions"></param>
        /// <returns>Session wrapper.</returns>
        internal CimSessionWrapper QuerySession(CimSession cimsession)
        {
            CimSessionWrapper wrapper;
            this.curCimSessionWrapper.TryGetValue(cimsession, out wrapper);
            return wrapper;
        }

        /// <summary>
        /// Query session object with given CimSessionInstanceID.
        /// </summary>
        /// <param name="cimSessionInstanceId"></param>
        /// <returns>CimSession object.</returns>
        internal CimSession QuerySession(Guid cimSessionInstanceId)
        {
            if (this.curCimSessionsByInstanceId.ContainsKey(cimSessionInstanceId))
            {
                CimSessionWrapper wrapper = this.curCimSessionsByInstanceId[cimSessionInstanceId];
                return wrapper.CimSession;
            }

            return null;
        }
        #endregion
    }

    #endregion

    #region CimSessionBase

    /// <summary>
    /// <para>
    /// Base class of all session operation classes.
    /// All sessions created will be held in a ConcurrentDictionary:cimSessions.
    /// It manages the lifecycle of the sessions being created for each
    /// runspace according to the state of the runspace.
    /// </para>
    /// </summary>
    internal class CimSessionBase
    {
        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSessionBase"/> class.
        /// </summary>
        public CimSessionBase()
        {
            this.sessionState = cimSessions.GetOrAdd(
                CurrentRunspaceId,
                (Guid instanceId) =>
                {
                    if (Runspace.DefaultRunspace != null)
                    {
                        Runspace.DefaultRunspace.StateChanged += DefaultRunspace_StateChanged;
                    }

                    return new CimSessionState();
                });
        }

        #endregion

        #region members

        /// <summary>
        /// <para>
        /// Thread safe static dictionary to store session objects associated
        /// with each runspace, which is identified by a GUID. NOTE: cmdlet
        /// can running parallelly under more than one runspace(s).
        /// </para>
        /// </summary>
        internal static readonly ConcurrentDictionary<Guid, CimSessionState> cimSessions
            = new();

        /// <summary>
        /// <para>
        /// Default runspace Id.
        /// </para>
        /// </summary>
        internal static readonly Guid defaultRunspaceId = Guid.Empty;

        /// <summary>
        /// <para>
        /// Object used to hold all CimSessions and status data bound
        /// to current runspace.
        /// </para>
        /// </summary>
        internal CimSessionState sessionState;

        /// <summary>
        /// Get current runspace id.
        /// </summary>
        private static Guid CurrentRunspaceId
        {
            get
            {
                if (Runspace.DefaultRunspace != null)
                {
                    return Runspace.DefaultRunspace.InstanceId;
                }
                else
                {
                    return CimSessionBase.defaultRunspaceId;
                }
            }
        }
        #endregion

        public static CimSessionState GetCimSessionState()
        {
            CimSessionState state = null;
            cimSessions.TryGetValue(CurrentRunspaceId, out state);
            return state;
        }

        /// <summary>
        /// <para>
        /// Clean up the dictionaries if the runspace is closed or broken.
        /// </para>
        /// </summary>
        /// <param name="sender">Runspace.</param>
        /// <param name="e">Event args.</param>
        private static void DefaultRunspace_StateChanged(object sender, RunspaceStateEventArgs e)
        {
            Runspace runspace = (Runspace)sender;
            switch (e.RunspaceStateInfo.State)
            {
                case RunspaceState.Broken:
                case RunspaceState.Closed:
                    CimSessionState state;
                    if (cimSessions.TryRemove(runspace.InstanceId, out state))
                    {
                        DebugHelper.WriteLog(string.Format(CultureInfo.CurrentUICulture, DebugHelper.runspaceStateChanged, runspace.InstanceId, e.RunspaceStateInfo.State));
                        state.Dispose();
                    }

                    runspace.StateChanged -= DefaultRunspace_StateChanged;
                    break;
                default:
                    break;
            }
        }
    }

    #endregion

    #region CimTestConnection

    #endregion

    #region CimNewSession

    /// <summary>
    /// <para>
    /// <c>CimNewSession</c> is the class to create cimSession
    /// based on given <c>NewCimSessionCommand</c>.
    /// </para>
    /// </summary>
    internal class CimNewSession : CimSessionBase, IDisposable
    {
        /// <summary>
        /// CimTestCimSessionContext.
        /// </summary>
        internal class CimTestCimSessionContext : XOperationContextBase
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CimTestCimSessionContext"/> class.
            /// </summary>
            /// <param name="theProxy"></param>
            /// <param name="wrapper"></param>
            internal CimTestCimSessionContext(
                CimSessionProxy theProxy,
                CimSessionWrapper wrapper)
            {
                this.proxy = theProxy;
                this.CimSessionWrapper = wrapper;
                this.nameSpace = null;
            }

            /// <summary>
            /// <para>Namespace</para>
            /// </summary>
            internal CimSessionWrapper CimSessionWrapper { get; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimNewSession"/> class.
        /// </summary>
        internal CimNewSession() : base()
        {
            this.cimTestSession = new CimTestSession();
            this.Disposed = false;
        }

        /// <summary>
        /// Create a new <see cref="CimSession"/> base on given cmdlet
        /// and its parameter.
        /// </summary>
        /// <param name="cmdlet"></param>
        /// <param name="sessionOptions"></param>
        /// <param name="credential"></param>
        internal void NewCimSession(NewCimSessionCommand cmdlet,
            CimSessionOptions sessionOptions,
            CimCredential credential)
        {
            DebugHelper.WriteLogEx();

            IEnumerable<string> computerNames = ConstValue.GetComputerNames(cmdlet.ComputerName);
            foreach (string computerName in computerNames)
            {
                CimSessionProxy proxy;
                if (sessionOptions == null)
                {
                    DebugHelper.WriteLog("Create CimSessionOption due to NewCimSessionCommand has null sessionoption", 1);
                    sessionOptions = CimSessionProxy.CreateCimSessionOption(computerName,
                        cmdlet.OperationTimeoutSec, credential);
                }

                proxy = new CimSessionProxyTestConnection(computerName, sessionOptions);
                string computerNameValue = (computerName == ConstValue.NullComputerName) ? ConstValue.LocalhostComputerName : computerName;
                CimSessionWrapper wrapper = new(0, Guid.Empty, cmdlet.Name, computerNameValue, proxy.CimSession, proxy.Protocol);
                CimTestCimSessionContext context = new(proxy, wrapper);
                proxy.ContextObject = context;
                // Skip test the connection if user intend to
                if (cmdlet.SkipTestConnection.IsPresent)
                {
                    AddSessionToCache(proxy.CimSession, context, new CmdletOperationBase(cmdlet));
                }
                else
                {
                    // CimSession will be returned as part of TestConnection
                    this.cimTestSession.TestCimSession(computerName, proxy);
                }
            }
        }

        /// <summary>
        /// <para>
        /// Add session to global cache,
        /// </para>
        /// </summary>
        /// <param name="cimSession"></param>
        /// <param name="context"></param>
        /// <param name="cmdlet"></param>
        internal void AddSessionToCache(CimSession cimSession, XOperationContextBase context, CmdletOperationBase cmdlet)
        {
            DebugHelper.WriteLogEx();

            CimTestCimSessionContext testCimSessionContext = context as CimTestCimSessionContext;
            uint sessionId = this.sessionState.GenerateSessionId();
            string originalSessionName = testCimSessionContext.CimSessionWrapper.Name;
            string sessionName = originalSessionName ?? string.Format(CultureInfo.CurrentUICulture, $@"{CimSessionState.CimSessionClassName}{sessionId}");

            // detach CimSession from the proxy object
            CimSession createdCimSession = testCimSessionContext.Proxy.Detach();
            PSObject psObject = this.sessionState.AddObjectToCache(
                createdCimSession,
                sessionId,
                createdCimSession.InstanceId,
                sessionName,
                testCimSessionContext.CimSessionWrapper.ComputerName,
                testCimSessionContext.Proxy.Protocol);
            cmdlet.WriteObject(psObject, null);
        }

        /// <summary>
        /// <para>
        /// Process all actions in the action queue.
        /// </para>
        /// </summary>
        /// <param name="cmdletOperation">
        /// Wrapper of cmdlet, <seealso cref="CmdletOperationBase"/> for details.
        /// </param>
        public void ProcessActions(CmdletOperationBase cmdletOperation)
        {
            this.cimTestSession.ProcessActions(cmdletOperation);
        }

        /// <summary>
        /// <para>
        /// Process remaining actions until all operations are completed or
        /// current cmdlet is terminated by user.
        /// </para>
        /// </summary>
        /// <param name="cmdletOperation">
        /// Wrapper of cmdlet, <seealso cref="CmdletOperationBase"/> for details.
        /// </param>
        public void ProcessRemainActions(CmdletOperationBase cmdletOperation)
        {
            this.cimTestSession.ProcessRemainActions(cmdletOperation);
        }

        #region private members
        /// <summary>
        /// <para>
        /// <see cref="CimTestSession"/> object.
        /// </para>
        /// </summary>
        private readonly CimTestSession cimTestSession;
        #endregion // private members

        #region IDisposable

        /// <summary>
        /// <para>
        /// Indicates whether this object was disposed or not.
        /// </para>
        /// </summary>
        protected bool Disposed { get; private set; }

        /// <summary>
        /// <para>
        /// Dispose() calls Dispose(true).
        /// Implement IDisposable. Do not make this method virtual.
        /// A derived class should not be able to override this method.
        /// </para>
        /// </summary>
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

        /// <summary>
        /// <para>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </para>
        /// </summary>
        /// <param name="disposing">Whether it is directly called.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.Disposed)
            {
                if (disposing)
                {
                    // free managed resources
                    this.cimTestSession.Dispose();
                    this.Disposed = true;
                }
                // free native resources if there are any
            }
        }
        #endregion
    }

    #endregion

    #region CimGetSession

    /// <summary>
    /// <para>
    /// Get CimSession based on given id/instanceid/computername/name.
    /// </para>
    /// </summary>
    internal class CimGetSession : CimSessionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CimGetSession"/> class.
        /// </summary>
        public CimGetSession() : base()
        {
        }

        /// <summary>
        /// Get <see cref="CimSession"/> objects based on the given cmdlet
        /// and its parameter.
        /// </summary>
        /// <param name="cmdlet"></param>
        public void GetCimSession(GetCimSessionCommand cmdlet)
        {
            DebugHelper.WriteLogEx();

            IEnumerable<PSObject> sessionToGet = null;
            IEnumerable<ErrorRecord> errorRecords = null;
            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.ComputerNameSet:
                    if (cmdlet.ComputerName == null)
                    {
                        sessionToGet = this.sessionState.QuerySession(ConstValue.DefaultSessionName, out errorRecords);
                    }
                    else
                    {
                        sessionToGet = this.sessionState.QuerySessionByComputerName(cmdlet.ComputerName, out errorRecords);
                    }

                    break;
                case CimBaseCommand.SessionIdSet:
                    sessionToGet = this.sessionState.QuerySession(cmdlet.Id, out errorRecords);
                    break;
                case CimBaseCommand.InstanceIdSet:
                    sessionToGet = this.sessionState.QuerySession(cmdlet.InstanceId, out errorRecords);
                    break;
                case CimBaseCommand.NameSet:
                    sessionToGet = this.sessionState.QuerySession(cmdlet.Name, out errorRecords);
                    break;
                default:
                    break;
            }

            if (sessionToGet != null)
            {
                foreach (PSObject psobject in sessionToGet)
                {
                    cmdlet.WriteObject(psobject);
                }
            }

            if (errorRecords != null)
            {
                foreach (ErrorRecord errRecord in errorRecords)
                {
                    cmdlet.WriteError(errRecord);
                }
            }
        }

        #region helper methods

        #endregion
    }

    #endregion

    #region CimRemoveSession

    /// <summary>
    /// <para>
    /// Get CimSession based on given id/instanceid/computername/name.
    /// </para>
    /// </summary>
    internal class CimRemoveSession : CimSessionBase
    {
        /// <summary>
        /// Remove session action string.
        /// </summary>
        internal static readonly string RemoveCimSessionActionName = "Remove CimSession";

        /// <summary>
        /// Initializes a new instance of the <see cref="CimRemoveSession"/> class.
        /// </summary>
        public CimRemoveSession() : base()
        {
        }

        /// <summary>
        /// Remove the <see cref="CimSession"/> objects based on given cmdlet
        /// and its parameter.
        /// </summary>
        /// <param name="cmdlet"></param>
        public void RemoveCimSession(RemoveCimSessionCommand cmdlet)
        {
            DebugHelper.WriteLogEx();

            IEnumerable<PSObject> sessionToRemove = null;
            IEnumerable<ErrorRecord> errorRecords = null;
            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.CimSessionSet:
                    sessionToRemove = this.sessionState.QuerySession(cmdlet.CimSession, out errorRecords);
                    break;
                case CimBaseCommand.ComputerNameSet:
                    sessionToRemove = this.sessionState.QuerySessionByComputerName(cmdlet.ComputerName, out errorRecords);
                    break;
                case CimBaseCommand.SessionIdSet:
                    sessionToRemove = this.sessionState.QuerySession(cmdlet.Id, out errorRecords);
                    break;
                case CimBaseCommand.InstanceIdSet:
                    sessionToRemove = this.sessionState.QuerySession(cmdlet.InstanceId, out errorRecords);
                    break;
                case CimBaseCommand.NameSet:
                    sessionToRemove = this.sessionState.QuerySession(cmdlet.Name, out errorRecords);
                    break;
                default:
                    break;
            }

            if (sessionToRemove != null)
            {
                foreach (PSObject psobject in sessionToRemove)
                {
                    if (cmdlet.ShouldProcess(this.sessionState.GetRemoveSessionObjectTarget(psobject), RemoveCimSessionActionName))
                    {
                        this.sessionState.RemoveOneSessionObjectFromCache(psobject);
                    }
                }
            }

            if (errorRecords != null)
            {
                foreach (ErrorRecord errRecord in errorRecords)
                {
                    cmdlet.WriteError(errRecord);
                }
            }
        }
    }

    #endregion

    #region CimTestSession

    /// <summary>
    /// Class <see cref="CimTestSession"/>, which is used to
    /// test cimsession and execute async operations.
    /// </summary>
    internal class CimTestSession : CimAsyncOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CimTestSession"/> class.
        /// </summary>
        internal CimTestSession()
            : base()
        {
        }

        /// <summary>
        /// Test the session connection with
        /// given <see cref="CimSessionProxy"/> object.
        /// </summary>
        /// <param name="computerName"></param>
        /// <param name="proxy"></param>
        internal void TestCimSession(
            string computerName,
            CimSessionProxy proxy)
        {
            DebugHelper.WriteLogEx();
            this.SubscribeEventAndAddProxytoCache(proxy);
            proxy.TestConnectionAsync();
        }
    }

    #endregion

}
