// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using Microsoft.Management.Infrastructure.Options;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// This Cmdlet enables the IT Pro to create a CIM Session. CIM Session object
    /// is a client-side representation of the connection between the client and the
    /// server.
    /// The CimSession object returned by the Cmdlet is used by all other CIM
    /// cmdlets.
    /// </summary>
    [Alias("ncms")]
    [Cmdlet(VerbsCommon.New, "CimSession", DefaultParameterSetName = CredentialParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227967")]
    [OutputType(typeof(CimSession))]
    public sealed class NewCimSessionCommand : CimBaseCommand
    {
        #region cmdlet parameters

        /// <summary>
        /// The following is the definition of the input parameter "Authentication".
        /// The following is the validation set for allowed authentication types.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = CredentialParameterSet)]
        public PasswordAuthenticationMechanism Authentication
        {
            get
            {
                return authentication;
            }

            set
            {
                authentication = value;
                authenticationSet = true;
            }
        }

        private PasswordAuthenticationMechanism authentication;
        private bool authenticationSet = false;

        /// <summary>
        /// The following is the definition of the input parameter "Credential".
        /// Specifies a user account that has permission to perform this action.
        /// The default is the current user.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = CredentialParameterSet)]
        [Credential]
        public PSCredential Credential { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "CertificateThumbprint".
        /// This is specificly for wsman protocol.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CertificateParameterSet)]
        public string CertificateThumbprint { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "ComputerName".
        /// Specifies the computer on which the commands associated with this session
        /// will run. The default value is LocalHost.
        /// </summary>
        [Alias(AliasCN, AliasServerName)]
        [Parameter(
            Position = 0,
            ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName { get; set; }

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "Name".
        /// Specifies a friendly name for the CIM Session connection.
        /// </para>
        /// <para>
        /// If a name is not passed, then the session is given the name CimSession<int>,
        /// where <int> is the next available session number. Example, CimSession1,
        /// CimSession2, etc...
        /// </para>
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "OperationTimeoutSec".
        /// Specifies the operation timeout for all operations in session. Individual
        /// operations can override this timeout.
        /// </para>
        /// <para>
        /// The unit is Second.
        /// </para>
        /// </summary>
        [Alias(AliasOT)]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public uint OperationTimeoutSec
        {
            get
            {
                return operationTimeout;
            }

            set
            {
                operationTimeout = value;
                operationTimeoutSet = true;
            }
        }

        private uint operationTimeout;
        internal bool operationTimeoutSet = false;

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "SkipTestConnection".
        /// Specifies where test connection should be skipped
        /// </para>
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter SkipTestConnection { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Port".
        /// Specifies the port number to use, if different than the default port number.
        /// This is specificly for wsman protocol.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public uint Port
        {
            get
            {
                return port;
            }

            set
            {
                port = value;
                portSet = true;
            }
        }

        private uint port;
        private bool portSet = false;

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "SessionOption".
        /// Specifies the SessionOption object that is passed to the Cmdlet as argument.
        /// </para>
        /// <para>
        /// If the argument is not given, a default SessionOption will be created for
        /// the session in .NET API layer.
        /// </para>
        /// <para>
        /// If a <see cref="DCOMSessionOption"/> object is passed, then
        /// connection is made using DCOM. If a <see cref="WsManSessionOption"/>
        /// object is passed, then connection is made using WsMan.
        /// </para>
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public Microsoft.Management.Infrastructure.Options.CimSessionOptions SessionOption { get; set; }

        #endregion

        #region cmdlet processing methods

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            cimNewSession = new CimNewSession();
            this.CmdletOperation = new CmdletOperationTestCimSession(this, this.cimNewSession);
            this.AtBeginProcess = false;
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            CimSessionOptions outputOptions;
            CimCredential outputCredential;
            BuildSessionOptions(out outputOptions, out outputCredential);
            cimNewSession.NewCimSession(this, outputOptions, outputCredential);
            cimNewSession.ProcessActions(this.CmdletOperation);
        }

        /// <summary>
        /// EndProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            cimNewSession.ProcessRemainActions(this.CmdletOperation);
        }
        #endregion

        #region helper methods

        /// <summary>
        /// Build a CimSessionOptions, used to create CimSession.
        /// </summary>
        /// <returns>Null means no prefer CimSessionOptions.</returns>
        internal void BuildSessionOptions(out CimSessionOptions outputOptions, out CimCredential outputCredential)
        {
            DebugHelper.WriteLogEx();

            CimSessionOptions options = null;
            if (this.SessionOption != null)
            {
                // clone the sessionOption object
                if (this.SessionOption is WSManSessionOptions)
                {
                    options = new WSManSessionOptions(this.SessionOption as WSManSessionOptions);
                }
                else
                {
                    options = new DComSessionOptions(this.SessionOption as DComSessionOptions);
                }
            }

            outputOptions = null;
            outputCredential = null;
            if (options != null)
            {
                if (options is DComSessionOptions dcomOptions)
                {
                    bool conflict = false;
                    string parameterName = string.Empty;
                    if (this.CertificateThumbprint != null)
                    {
                        conflict = true;
                        parameterName = @"CertificateThumbprint";
                    }

                    if (portSet)
                    {
                        conflict = true;
                        parameterName = @"Port";
                    }

                    if (conflict)
                    {
                        ThrowConflictParameterWasSet(@"New-CimSession", parameterName, @"DComSessionOptions");
                        return;
                    }
                }
            }

            if (portSet || (this.CertificateThumbprint != null))
            {
                WSManSessionOptions wsmanOptions = (options == null) ? new WSManSessionOptions() : options as WSManSessionOptions;
                if (portSet)
                {
                    wsmanOptions.DestinationPort = this.Port;
                    portSet = false;
                }

                if (this.CertificateThumbprint != null)
                {
                    CimCredential credentials = new(CertificateAuthenticationMechanism.Default, this.CertificateThumbprint);
                    wsmanOptions.AddDestinationCredentials(credentials);
                }

                options = wsmanOptions;
            }

            if (this.operationTimeoutSet)
            {
                if (options != null)
                {
                    options.Timeout = TimeSpan.FromSeconds((double)this.OperationTimeoutSec);
                }
            }

            if (this.authenticationSet || (this.Credential != null))
            {
                PasswordAuthenticationMechanism authentication = this.authenticationSet ? this.Authentication : PasswordAuthenticationMechanism.Default;
                if (this.authenticationSet)
                {
                    this.authenticationSet = false;
                }

                CimCredential credentials = CreateCimCredentials(this.Credential, authentication, @"New-CimSession", @"Authentication");
                if (credentials == null)
                {
                    return;
                }

                DebugHelper.WriteLog("Credentials: {0}", 1, credentials);
                outputCredential = credentials;
                if (options != null)
                {
                    DebugHelper.WriteLog("Add credentials to option: {0}", 1, options);
                    options.AddDestinationCredentials(credentials);
                }
            }

            DebugHelper.WriteLogEx("Set outputOptions: {0}", 1, outputOptions);
            outputOptions = options;
        }

        #endregion

        #region private members

        /// <summary>
        /// <para>
        /// CimNewSession object
        /// </para>
        /// </summary>
        private CimNewSession cimNewSession;

        #endregion

        #region IDisposable
        /// <summary>
        /// Clean up resources.
        /// </summary>
        protected override void DisposeInternal()
        {
            base.DisposeInternal();

            // Dispose managed resources.
            this.cimNewSession?.Dispose();
        }
        #endregion
    }
}
