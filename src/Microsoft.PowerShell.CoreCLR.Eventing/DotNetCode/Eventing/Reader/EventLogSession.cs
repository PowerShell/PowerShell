// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*============================================================
**
**
** Purpose:
** Defines a session for Event Log operations.  The session can
** be configured for a remote machine and can use specific
** user credentials.
============================================================*/

using System.Security;
using System.Collections.Generic;
using System.Globalization;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Session Login Type.
    /// </summary>
    public enum SessionAuthentication
    {
        Default = 0,
        Negotiate = 1,
        Kerberos = 2,
        Ntlm = 3
    }

    /// <summary>
    /// The type: log / external log file to query.
    /// </summary>
    public enum PathType
    {
        LogName = 1,
        FilePath = 2
    }

    public class EventLogSession : IDisposable
    {
        //
        // the two context handles for rendering (for EventLogRecord).
        // the system and user context handles. They are both common for all the event instances and can be created only once.
        // access to the data member references is safe, while
        // invoking methods on it is marked SecurityCritical as appropriate.
        //
        internal EventLogHandle renderContextHandleSystem = EventLogHandle.Zero;
        internal EventLogHandle renderContextHandleUser = EventLogHandle.Zero;

        // the dummy sync object for the two contexts.
        private object _syncObject = null;

        private string _server;
        private string _user;
        private string _domain;
        private SessionAuthentication _logOnType;
        // we do not maintain the password here.

        //
        // access to the data member references is safe, while
        // invoking methods on it is marked SecurityCritical as appropriate.
        //
        private EventLogHandle _handle = EventLogHandle.Zero;

        // setup the System Context, once for all the EventRecords.
        [System.Security.SecuritySafeCritical]
        internal void SetupSystemContext()
        {
            if (!this.renderContextHandleSystem.IsInvalid)
                return;
            lock (_syncObject)
            {
                if (this.renderContextHandleSystem.IsInvalid)
                {
                    // create the SYSTEM render context
                    // call the EvtCreateRenderContext to get the renderContextHandleSystem, so that we can get the system/values/user properties.
                    this.renderContextHandleSystem = NativeWrapper.EvtCreateRenderContext(0, null, UnsafeNativeMethods.EvtRenderContextFlags.EvtRenderContextSystem);
                }
            }
        }

        [System.Security.SecuritySafeCritical]
        internal void SetupUserContext()
        {
            lock (_syncObject)
            {
                if (this.renderContextHandleUser.IsInvalid)
                {
                    // create the USER render context
                    this.renderContextHandleUser = NativeWrapper.EvtCreateRenderContext(0, null, UnsafeNativeMethods.EvtRenderContextFlags.EvtRenderContextUser);
                }
            }
        }

        // marked as SecurityCritical because allocates SafeHandle.
        // marked as TreatAsSafe because performs Demand().
        [System.Security.SecurityCritical]
        public EventLogSession()
        {
            // handle = EventLogHandle.Zero;
            _syncObject = new object();
        }

        public EventLogSession(string server)
            :
            this(server, null, null, (SecureString)null, SessionAuthentication.Default)
        {
        }

        // marked as TreatAsSafe because performs Demand().
        [System.Security.SecurityCritical]
        public EventLogSession(string server, string domain, string user, SecureString password, SessionAuthentication logOnType)
        {
            if (server == null)
                server = "localhost";

            _syncObject = new object();

            _server = server;
            _domain = domain;
            _user = user;
            _logOnType = logOnType;

            UnsafeNativeMethods.EvtRpcLogin erLogin = new UnsafeNativeMethods.EvtRpcLogin();
            erLogin.Server = _server;
            erLogin.User = _user;
            erLogin.Domain = _domain;
            erLogin.Flags = (int)_logOnType;
            erLogin.Password = CoTaskMemUnicodeSafeHandle.Zero;

            try
            {
                if (password != null)
                    erLogin.Password.SetMemory(SecureStringMarshal.SecureStringToCoTaskMemUnicode(password));
                // open a session using the erLogin structure.
                _handle = NativeWrapper.EvtOpenSession(UnsafeNativeMethods.EvtLoginClass.EvtRpcLogin, ref erLogin, 0, 0);
            }
            finally
            {
                erLogin.Password.Dispose();
            }
        }

        internal EventLogHandle Handle
        {
            get
            {
                return _handle;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [System.Security.SecuritySafeCritical]
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this == s_globalSession)
                    throw new InvalidOperationException();
            }

            if (this.renderContextHandleSystem != null &&
                !this.renderContextHandleSystem.IsInvalid)
                this.renderContextHandleSystem.Dispose();

            if (this.renderContextHandleUser != null &&
                !this.renderContextHandleUser.IsInvalid)
                this.renderContextHandleUser.Dispose();

            if (_handle != null && !_handle.IsInvalid)
                _handle.Dispose();
        }

        public void CancelCurrentOperations()
        {
            NativeWrapper.EvtCancel(_handle);
        }

        private static EventLogSession s_globalSession = new EventLogSession();
        public static EventLogSession GlobalSession
        {
            get { return s_globalSession; }
        }

        [System.Security.SecurityCritical]
        public IEnumerable<string> GetProviderNames()
        {
            List<string> namesList = new List<string>(100);

            using (EventLogHandle ProviderEnum = NativeWrapper.EvtOpenProviderEnum(this.Handle, 0))
            {
                bool finish = false;

                do
                {
                    string s = NativeWrapper.EvtNextPublisherId(ProviderEnum, ref finish);
                    if (finish == false) namesList.Add(s);
                }
                while (finish == false);

                return namesList;
            }
        }

        [System.Security.SecurityCritical]
        public IEnumerable<string> GetLogNames()
        {
            List<string> namesList = new List<string>(100);

            using (EventLogHandle channelEnum = NativeWrapper.EvtOpenChannelEnum(this.Handle, 0))
            {
                bool finish = false;

                do
                {
                    string s = NativeWrapper.EvtNextChannelPath(channelEnum, ref finish);
                    if (finish == false) namesList.Add(s);
                }
                while (finish == false);

                return namesList;
            }
        }

        public EventLogInformation GetLogInformation(string logName, PathType pathType)
        {
            if (logName == null)
                throw new ArgumentNullException("logName");

            return new EventLogInformation(this, logName, pathType);
        }

        public void ExportLog(string path, PathType pathType, string query, string targetFilePath)
        {
            this.ExportLog(path, pathType, query, targetFilePath, false);
        }

        public void ExportLog(string path, PathType pathType, string query, string targetFilePath, bool tolerateQueryErrors)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (targetFilePath == null)
                throw new ArgumentNullException("targetFilePath");

            UnsafeNativeMethods.EvtExportLogFlags flag;
            switch (pathType)
            {
                case PathType.LogName:
                    flag = UnsafeNativeMethods.EvtExportLogFlags.EvtExportLogChannelPath;
                    break;
                case PathType.FilePath:
                    flag = UnsafeNativeMethods.EvtExportLogFlags.EvtExportLogFilePath;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("pathType");
            }

            if (tolerateQueryErrors == false)
                NativeWrapper.EvtExportLog(this.Handle, path, query, targetFilePath, (int)flag);
            else
                NativeWrapper.EvtExportLog(this.Handle, path, query, targetFilePath, (int)flag | (int)UnsafeNativeMethods.EvtExportLogFlags.EvtExportLogTolerateQueryErrors);
        }

        public void ExportLogAndMessages(string path, PathType pathType, string query, string targetFilePath)
        {
            this.ExportLogAndMessages(path, pathType, query, targetFilePath, false, CultureInfo.CurrentCulture);
        }

        public void ExportLogAndMessages(string path, PathType pathType, string query, string targetFilePath, bool tolerateQueryErrors, CultureInfo targetCultureInfo)
        {
            if (targetCultureInfo == null)
                targetCultureInfo = CultureInfo.CurrentCulture;
            ExportLog(path, pathType, query, targetFilePath, tolerateQueryErrors);
            // Ignore the CultureInfo, pass 0 to use the calling thread's locale
            NativeWrapper.EvtArchiveExportedLog(this.Handle, targetFilePath, 0, 0);
        }

        public void ClearLog(string logName)
        {
            this.ClearLog(logName, null);
        }

        public void ClearLog(string logName, string backupPath)
        {
            if (logName == null)
                throw new ArgumentNullException("logName");

            NativeWrapper.EvtClearLog(this.Handle, logName, backupPath, 0);
        }
    }
}
