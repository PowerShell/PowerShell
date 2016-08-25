using System;
using System.Activities;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.ComponentModel;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using Microsoft.PowerShell.Workflow;

namespace Microsoft.PowerShell.Activities
{
    /// <summary>
    /// Base class for activities that can use WsMan directly to contact ta managed node.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CIM")]
    public abstract class PSGeneratedCIMActivity : PSActivity, IImplementsConnectionRetry
    {
        /// <summary>
        /// The computer name to invoke this activity on.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public InArgument<string[]> PSComputerName
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the credential to use in the remote connection.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public InArgument<PSCredential> PSCredential
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the authentication type to be used in the remote connection.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<AuthenticationMechanism?> PSAuthentication { get; set; }


        /// <summary>
        /// Defines the certificate thumbprint to be used in the remote connection.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public InArgument<string> PSCertificateThumbprint { get; set; }

        /// <summary>
        /// Defines the number of retries that the activity will make to connect to a remote
        /// machine when it encounters an error. The default is to not retry.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<uint?> PSConnectionRetryCount { get; set; }

        /// <summary>
        /// Defines the delay, in seconds, between connection retry attempts.
        /// The default is one second.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<uint?> PSConnectionRetryIntervalSec { get; set; }

        /// <summary>
        /// Defines the resource URI used by the CIM cmdlet.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<Uri> ResourceUri { get; set; }

        /// <summary>
        /// The port to use in a remote connection attempt. The default is:
        /// HTTP: 5985, HTTPS: 5986.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<uint?> PSPort { get; set; }

        /// <summary>
        /// Determines whether to use SSL in the connection attempt. The default is false.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<bool?> PSUseSsl { get; set; }

        /// <summary>
        /// Defines any session options to be used in the remote connection.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public InArgument<PSSessionOption> PSSessionOption { get; set; }

        /// <summary>
        /// CIM Sessions to use for this activity.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public InArgument<CimSession[]> CimSession { get; set; }


        /// <summary>
        /// Contains the powershell text defining the mode for this command. 
        /// </summary>
        protected abstract string ModuleDefinition { get; }

        /// <summary>
        /// Returns TRUE if the PSComputerName argument has been specified, and
        /// contains at least one target.
        /// </summary>
        /// <param name="context">The workflow NativeActivityContext</param>
        /// <returns></returns>
        protected bool GetIsComputerNameSpecified(ActivityContext context)
        {
            return ((PSComputerName.Get(context) != null) &&
                (PSComputerName.Get(context).Length > 0));
        }

        /// <summary>
        /// Prepare commands that use CIM for remoting...
        /// </summary>
        /// <param name="context">The activity context to use</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected override List<ActivityImplementationContext> GetImplementation(NativeActivityContext context)
        {
            bool needRunspace = !typeof(GenericCimCmdletActivity).IsAssignableFrom(this.GetType());
            string[] computernames = PSComputerName.Get(context);
            CimSession[] sessions = this.CimSession.Get(context);
            Uri resourceUri = null;

            if (ResourceUri != null)
            {
                resourceUri = ResourceUri.Get(context);
            }

            List<ActivityImplementationContext> commands = new List<ActivityImplementationContext>();

            // Configure the remote connectivity options...
            if (computernames != null && computernames.Length > 0)
            {
                WSManSessionOptions sessionOptions = new WSManSessionOptions();

                // Set a timeout on the connection...
                uint? timeout = PSActionRunningTimeoutSec.Get(context);
                if (timeout.HasValue)
                {
                    sessionOptions.Timeout = TimeSpan.FromSeconds((double)(timeout.Value));
                }

                // See if we should use SSL or not...
                bool? useSsl = PSUseSsl.Get(context);
                bool sessionOptionUseSsl = false;

                if (useSsl.HasValue)
                {
                    sessionOptions.UseSsl = useSsl.Value;
                    sessionOptionUseSsl = useSsl.Value;
                }

                // Set the port to use
                uint? port = PSPort.Get(context);
                uint sessionOptionPort = 0;

                if (port.HasValue)
                {
                    sessionOptions.DestinationPort = port.Value;
                    sessionOptionPort = port.Value;
                }

                // Map over options from PSSessionConfig to WSManSessionOptions
                PSSessionOption pso = PSSessionOption.Get(context);
                if (pso != null)
                {
                    sessionOptions.NoEncryption = pso.NoEncryption;
                    sessionOptions.CertCACheck = pso.SkipCACheck;
                    sessionOptions.CertCNCheck = pso.SkipCNCheck;
                    sessionOptions.CertRevocationCheck = pso.SkipRevocationCheck;

                    if (pso.UseUTF16)
                        sessionOptions.PacketEncoding = PacketEncoding.Utf16;

                    if (pso.Culture != null)
                        sessionOptions.Culture = pso.Culture;

                    if (pso.UICulture != null)
                        sessionOptions.UICulture = pso.UICulture;

                    if (pso.ProxyCredential != null)
                    {   
                        string[] parts = pso.ProxyCredential.UserName.Split('\\');
                        string domain, userid;
                        if (parts.Length < 2)
                        {
                            domain = string.Empty;
                            userid = parts[0];
                        }
                        else
                        {
                            domain = parts[0];
                            userid = parts[1];
                        }

                        sessionOptions.AddProxyCredentials(
                            new CimCredential(ConvertPSAuthenticationMechanismToCimPasswordAuthenticationMechanism(pso.ProxyAuthentication), 
                                domain, userid, pso.ProxyCredential.Password));
                    }

                    switch (pso.ProxyAccessType)
                    {
                        case ProxyAccessType.WinHttpConfig:
                            sessionOptions.ProxyType = ProxyType.WinHttp;
                            break;
                        case ProxyAccessType.AutoDetect:
                            sessionOptions.ProxyType = ProxyType.Auto;
                            break;

                        case ProxyAccessType.IEConfig:
                            sessionOptions.ProxyType = ProxyType.InternetExplorer;
                            break;
                    }
                }

                PSCredential pscreds = PSCredential.Get(context);
                string certificateThumbprint = PSCertificateThumbprint.Get(context);

                if (pscreds != null && certificateThumbprint != null)
                {
                    throw new ArgumentException(Resources.CredentialParameterCannotBeSpecifiedWithPSCertificateThumbPrint);
                }

                PasswordAuthenticationMechanism passwordAuthenticationMechanism = PasswordAuthenticationMechanism.Default;
                AuthenticationMechanism? authenticationMechanism = PSAuthentication.Get(context);
                
                if (authenticationMechanism.HasValue)
                    passwordAuthenticationMechanism = ConvertPSAuthenticationMechanismToCimPasswordAuthenticationMechanism(authenticationMechanism.Value);

                
                if (certificateThumbprint != null)
                {
                    sessionOptions.AddDestinationCredentials(new CimCredential(CertificateAuthenticationMechanism.Default, certificateThumbprint));
                }

                if (pscreds != null)
                {
                    string[] parts = pscreds.UserName.Split('\\');
                    string domain, userid;
                    if (parts.Length < 2)
                    {
                        domain = string.Empty;
                        userid = parts[0];
                    }
                    else
                    {
                        domain = parts[0];
                        userid = parts[1];
                    }

                    sessionOptions.AddDestinationCredentials(new CimCredential(passwordAuthenticationMechanism, domain, userid, pscreds.Password));
                }

                // Create the PowerShell instance, and add the script to it.
                if (sessions != null && sessions.Length > 0)
                {
                    foreach (CimSession session in sessions)
                    {
                        ActivityImplementationContext configuredCommand = GetPowerShell(context);

                        CimActivityImplementationContext activityImplementationContext = 
                            new CimActivityImplementationContext(
                                configuredCommand, 
                                session.ComputerName, 
                                pscreds, 
                                certificateThumbprint, 
                                authenticationMechanism,
                                sessionOptionUseSsl, 
                                sessionOptionPort, 
                                pso, 
                                session, 
                                sessionOptions, 
                                ModuleDefinition, 
                                resourceUri);

                        commands.Add(activityImplementationContext);
                        //if (needRunspace)
                        //    GetRunspaceForCimCmdlet(context, activityImplementationContext);
                    }
                }
                else if (this.PSCommandName.Equals("CimCmdlets\\New-CimSession", StringComparison.OrdinalIgnoreCase))
                {
                    // NewCimSession activity is a special one as it creates the required sessions based on number of computers specified in one go.

                    ActivityImplementationContext baseContext = GetPowerShell(context);

                    CimActivityImplementationContext activityImplementationContext =
                        new CimActivityImplementationContext(baseContext,
                                                                null,  // ComputerName
                                                                pscreds,
                                                                certificateThumbprint,
                                                                authenticationMechanism,
                                                                sessionOptionUseSsl,
                                                                sessionOptionPort,
                                                                pso,
                                                                null, // session
                                                                sessionOptions,
                                                                ModuleDefinition,
                                                                resourceUri);

                    commands.Add(activityImplementationContext);
                }
                else
                {
                    foreach (string computer in computernames)
                    {
                        ActivityImplementationContext baseContext = GetPowerShell(context);

                        CimActivityImplementationContext activityImplementationContext =
                            new CimActivityImplementationContext(baseContext, 
                                                                 computer, 
                                                                 pscreds,
                                                                 certificateThumbprint,
                                                                 authenticationMechanism,
                                                                 sessionOptionUseSsl, 
                                                                 sessionOptionPort,
                                                                 pso, 
                                                                 null, // session
                                                                 sessionOptions, 
                                                                 ModuleDefinition, 
                                                                 resourceUri);

                        commands.Add(activityImplementationContext);
                    }
                }
            }
            // Configure the local invocation options
            else
            {
                // Create the PowerShell instance, and add the script to it.
                ActivityImplementationContext baseContext = GetPowerShell(context);
                CimActivityImplementationContext activityImplementationContext =
                    new CimActivityImplementationContext(baseContext, 
                                                         null,  // ComputerName
                                                         null,  // Credential
                                                         null,  // CertificateThumbprint
                                                         AuthenticationMechanism.Default,
                                                         false, // UseSsl
                                                         0,     // Port
                                                         null,  // PSSessionOption
                                                         null,  // Session
                                                         null,  // CimSessionOptions
                                                         ModuleDefinition,
                                                         resourceUri);

                commands.Add(activityImplementationContext);
            }

            return commands;
        }

        internal static PasswordAuthenticationMechanism ConvertPSAuthenticationMechanismToCimPasswordAuthenticationMechanism(AuthenticationMechanism psAuthenticationMechanism)
        {
            switch (psAuthenticationMechanism)
            {
                case  AuthenticationMechanism.Basic:
                    return PasswordAuthenticationMechanism.Basic;

                case AuthenticationMechanism.Negotiate:
                case AuthenticationMechanism.NegotiateWithImplicitCredential:
                    return PasswordAuthenticationMechanism.Negotiate;
                            
                case AuthenticationMechanism.Credssp:
                    return PasswordAuthenticationMechanism.CredSsp;

                case  AuthenticationMechanism.Digest:
                    return PasswordAuthenticationMechanism.Digest;

                case AuthenticationMechanism.Kerberos:
                    return PasswordAuthenticationMechanism.Kerberos;

                case  AuthenticationMechanism.Default:
                default:
                    return PasswordAuthenticationMechanism.Default;                   
            }
        }
    }

    /// <summary>
    /// Base class for the built-in generic CIM cmdlets.
    /// </summary>
    public abstract class GenericCimCmdletActivity : PSGeneratedCIMActivity
    {
        /// <summary>
        /// For these cmdlets, there is no defining text.
        /// </summary>
        protected override string ModuleDefinition { get { return string.Empty; } }

        /// <summary>
        /// The .NET type that implements the associated cmdlet
        /// </summary>
        public abstract Type TypeImplementingCmdlet { get; }
    }


    /// <summary>
    /// Provides additional functionality for CIM activity implementations.
    /// </summary>
    public class CimActivityImplementationContext : ActivityImplementationContext
    {
        /// <summary>
        /// Create an instance of the CIM activity implementation class
        /// </summary>
        /// <param name="activityImplementationContext"></param>
        /// <param name="computerName"></param>
        /// <param name="credential"></param>
        /// <param name="certificateThumbprint"></param>
        /// <param name="authenticationMechanism"></param>
        /// <param name="useSsl"></param>
        /// <param name="port"></param>
        /// <param name="sessionOption"></param>
        /// <param name="session"></param>
        /// <param name="cimSessionOptions"></param>
        /// <param name="moduleDefinition"></param>
        /// <param name="resourceUri"></param>
        public CimActivityImplementationContext(
            ActivityImplementationContext activityImplementationContext,
            string computerName, 
            PSCredential credential, 
            string certificateThumbprint, 
            AuthenticationMechanism? authenticationMechanism, 
            bool useSsl, 
            uint port, 
            PSSessionOption sessionOption, 
            CimSession session, 
            CimSessionOptions cimSessionOptions, 
            string moduleDefinition,
            Uri resourceUri)
        {
            if (activityImplementationContext == null)
            {
                throw new ArgumentNullException("activityImplementationContext");
            }
            this.PowerShellInstance = activityImplementationContext.PowerShellInstance;
            ResourceUri = resourceUri;
            ComputerName = computerName;
            PSCredential = credential;
            PSCertificateThumbprint = certificateThumbprint;
            PSAuthentication = authenticationMechanism;
            PSUseSsl = useSsl;
            PSPort = port;
            PSSessionOption = sessionOption;
            Session = session;
            SessionOptions = cimSessionOptions;
            if (moduleDefinition != null)
            {
                // Creating a script block forces the string into the compiled script cache so we
                // don't need to reparse it at execution time. Locking the static _moduleDefinition is not
                // required since the operation is idempotent.
                _moduleScriptBlock = ScriptBlock.Create(moduleDefinition);
                _moduleDefinition = moduleDefinition;
            }
        }

        /// <summary>
        /// Gets the scriptblock that implements this activity's command
        /// </summary>
        public ScriptBlock ModuleScriptBlock
        {
            get { return _moduleScriptBlock; }
        }
        private static  ScriptBlock _moduleScriptBlock;

        /// <summary>
        /// Defines the resource URI used by the CIM cmdlet.
        /// </summary>
        public Uri ResourceUri { get; set; }

        /// <summary>
        /// The session specified for this activity
        /// </summary>
        public CimSession Session { get; set; }

        /// <summary>
        /// The session options used to create the session for this activity...
        /// </summary>
        public CimSessionOptions SessionOptions { get; set; }

        /// <summary>
        /// The name of the computer this activity targets 
        /// </summary>
        public string ComputerName { get; set; }
        
        /// <summary>
        /// Base64 encoded string defining this module...
        /// </summary>
        public string ModuleDefinition { get { return _moduleDefinition; } }
        string _moduleDefinition;

        /// <summary>
        /// Return the session to the session manager
        /// </summary>
        public override void CleanUp()
        {
            if (Session != null && !string.IsNullOrEmpty(ComputerName))
            {
                CimConnectionManager.GetGlobalCimConnectionManager().ReleaseSession(ComputerName, Session);
                Session = null;
                ComputerName = null;
            }
        }
    }


    /// <summary>
    /// Class to manage reuse of CIM connections.
    /// </summary>
    internal class CimConnectionManager
    {
        private readonly System.Timers.Timer _cleanupTimer = new System.Timers.Timer();
        private bool firstTime = true;
        internal CimConnectionManager()
        {
            _cleanupTimer.Elapsed += HandleCleanupTimerElapsed;
            _cleanupTimer.AutoReset = true;
            _cleanupTimer.Interval = 20*1000;
            _cleanupTimer.Start();
        }

        private void HandleCleanupTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (firstTime)
            {
                // this is to enable a start delay of 20 seconds
                firstTime = false;
                return;
            }

            lock (SyncRoot)
            {
                List<string> computersToRemove = new List<string>();
                foreach (var pair in availableSessions)
                {
                    List<SessionEntry> sel = pair.Value;
                    if (sel.Count == 0)
                    {
                        computersToRemove.Add(pair.Key);
                    }
                    else
                    {
                        for (int i = sel.Count - 1; i >= 0; i--)
                        {
                            // If this connection in use, then skip it
                            if (sel[i].GetReferenceCount > 0)
                                continue;

                            if (--sel[i].IterationsRemaining <= 0)
                            {
                                sel[i].Session.Close();
                                sel.RemoveAt(i);
                            }
                        }
                    }
                }
            }
        }

        // Number of scan cycles left until this session entry will be removed.
        internal const int MaxIterations = 6;

        internal const int MaxCimSessionsUpperLimit = 500;
        internal const int MaxCimSessionsLowerLimit = 1;

        Dictionary<string, List<SessionEntry>> availableSessions = new Dictionary<string, List<SessionEntry>>();

        private class SessionEntry
        {
            public int IterationsRemaining = MaxIterations;
            public CimSessionOptions SessionOptions;
            public CimSession Session;
            
            public void AddReference()
            {
                Interlocked.Add(ref _numberOfUses, 1);
            }

            public void RemoveReference()
            {
                Interlocked.Decrement(ref _numberOfUses);
            }
            int _numberOfUses;

            public int GetReferenceCount
            {
                get { return _numberOfUses; }
            }

            public PSCredential Credential
            {
                get { return _credential; }
            }
            private PSCredential _credential;

            public bool UseSsl 
            {
                get
                {
                    return _useSsl;
                }
            }
            private bool _useSsl;

            public uint Port
            {
                get
                {
                    return _port;
                }
            }
            private uint _port;


            public PSSessionOption PSSessionOption
            {
                get
                {
                    return _psSessionOption;
                }
            }
            private PSSessionOption _psSessionOption;

            public string CertificateThumbprint
            {
                get 
                {
                    return _certificateThumbprint;
                }
            }
            private string _certificateThumbprint;

            public AuthenticationMechanism AuthenticationMechanism
            {
                get
                {
                    return _authenticationMechanism;
                }
            }
            private AuthenticationMechanism _authenticationMechanism;

            public SessionEntry(string computerName, PSCredential credential, string certificateThumbprint, AuthenticationMechanism authenticationMechanism, CimSessionOptions sessionOptions, bool useSsl, uint port, PSSessionOption pssessionOption)
            {
                SessionOptions = sessionOptions;
                _credential = credential;
                _certificateThumbprint = certificateThumbprint;
                _authenticationMechanism = authenticationMechanism;
                _useSsl = useSsl;
                _port = port;
                _psSessionOption = pssessionOption;
                Session = CimSession.Create(computerName, sessionOptions);
            }
        }

        object SyncRoot = new object();        

        /// <summary>
        /// Get a CIM session for the target computer
        /// </summary>
        /// <param name="computerName"></param>
        /// <param name="credential"></param>
        /// <param name="certificateThumbprint"></param>
        /// <param name="authenticationMechanism"></param>
        /// <param name="sessionOptions"></param>
        /// <param name="useSsl"></param>
        /// <param name="port"></param>
        /// <param name="pssessionOption"></param>
        /// <returns></returns>
        internal CimSession GetSession(string computerName, PSCredential credential, string certificateThumbprint, AuthenticationMechanism authenticationMechanism, CimSessionOptions sessionOptions, bool useSsl, uint port, PSSessionOption pssessionOption)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(computerName), "ComputerName is null in GetSession. GetSession should not be called in this case.");
            lock (SyncRoot)
            {
                SessionEntry newSessionEntry;

                if (availableSessions.ContainsKey(computerName))
                {

                    List<SessionEntry> sel = availableSessions[computerName];
                    if (sel.Count > 0)
                    {
                        for (int i = 0; i < sel.Count; i++)
                        {
                            SessionEntry se = sel[i];

                            // No session options specified or the object matches exactly...
                            if ((se.SessionOptions == null && sessionOptions == null) || CompareSessionOptions(se, sessionOptions, credential, certificateThumbprint, authenticationMechanism, useSsl, port, pssessionOption))
                            {
                                // Up the number of references to this session object...
                                se.AddReference();
                                return se.Session;
                            }
                        }
                    }
                }

                
                // Allocate a new session entry for this computer

                newSessionEntry = new SessionEntry(computerName, credential, certificateThumbprint, authenticationMechanism, sessionOptions, useSsl, port, pssessionOption);
                newSessionEntry.IterationsRemaining = MaxIterations;
                newSessionEntry.AddReference();
                if (! availableSessions.ContainsKey(computerName))
                {
                    availableSessions.Add(computerName, new List<SessionEntry>());
                }

                availableSessions[computerName].Add(newSessionEntry);

                // Return the session object
                return newSessionEntry.Session;
            }
        }

        

        /// <summary>
        /// Return a session to the session table.
        /// </summary>
        /// <param name="computerName"></param>
        /// <param name="session"></param>
        internal void ReleaseSession(string computerName, CimSession session)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(computerName), "ComputerName is null in ReleaseSession. ReleaseSession should not be called in this case.");
            lock (SyncRoot)
            {
                if (availableSessions.ContainsKey(computerName))
                {
                    foreach (var se in availableSessions[computerName])
                    {
                        if (se.Session == session)
                        {
                            se.RemoveReference();
                        }
                    }
                }
            }
        }

        private static bool CompareSessionOptions(SessionEntry sessionEntry, CimSessionOptions options2, PSCredential credential2, string certificateThumbprint, AuthenticationMechanism authenticationMechanism, bool useSsl, uint port, PSSessionOption pssessionOption)
        {
            if (!sessionEntry.SessionOptions.Timeout.Equals(options2.Timeout))
                return false;

            if (!string.Equals(sessionEntry.SessionOptions.Culture.ToString(), options2.Culture.ToString(), StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.Equals(sessionEntry.SessionOptions.UICulture.ToString(), options2.UICulture.ToString(), StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.Equals(sessionEntry.CertificateThumbprint, certificateThumbprint, StringComparison.OrdinalIgnoreCase))
                return false;

            if (sessionEntry.AuthenticationMechanism != authenticationMechanism)
                return false;

            if (!Workflow.WorkflowUtils.CompareCredential(sessionEntry.Credential, credential2))
                return false;

            if (sessionEntry.UseSsl != useSsl)
                return false;

            if (sessionEntry.Port != port)
                return false;


            // check PSSessionOption if present
            if (pssessionOption == null ^ sessionEntry.PSSessionOption == null)
            {
                return false;
            }

            if (pssessionOption != null && sessionEntry.PSSessionOption != null)
            {
                if (sessionEntry.PSSessionOption.ProxyAccessType != pssessionOption.ProxyAccessType)
                    return false;

                if (sessionEntry.PSSessionOption.ProxyAuthentication != pssessionOption.ProxyAuthentication)
                    return false;

                if (!Workflow.WorkflowUtils.CompareCredential(sessionEntry.PSSessionOption.ProxyCredential, pssessionOption.ProxyCredential))
                    return false;

                if (sessionEntry.PSSessionOption.SkipCACheck != pssessionOption.SkipCACheck)
                    return false;

                if (sessionEntry.PSSessionOption.SkipCNCheck != pssessionOption.SkipCNCheck)
                    return false;

                if (sessionEntry.PSSessionOption.SkipRevocationCheck != pssessionOption.SkipRevocationCheck)
                    return false;

                if (sessionEntry.PSSessionOption.NoEncryption != pssessionOption.NoEncryption)
                    return false;

                if (sessionEntry.PSSessionOption.UseUTF16 != pssessionOption.UseUTF16)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the global instance of the CIM session manager
        /// </summary>
        /// <returns></returns>
        public static CimConnectionManager GetGlobalCimConnectionManager()
        {
            lock (gcmLock)
            {
                if (_globalConnectionManagerInstance == null)
                {
                    _globalConnectionManagerInstance = new CimConnectionManager();
                }
                return _globalConnectionManagerInstance;
            }
        }
        static CimConnectionManager _globalConnectionManagerInstance;
        static object gcmLock = new object();
    }
}

