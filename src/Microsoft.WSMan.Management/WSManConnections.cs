// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Runspaces;
using System.Diagnostics.CodeAnalysis;
using Dbg = System.Management.Automation;

namespace Microsoft.WSMan.Management
{
    #region Base class for cmdlets taking credential, authentication, certificatethumbprint

    /// <summary>
    /// Common base class for all WSMan cmdlets that
    /// take Authentication, CertificateThumbprint and Credential parameters.
    /// </summary>
    public class AuthenticatingWSManCommand : PSCmdlet
    {
        /// <summary>
        /// The following is the definition of the input parameter "Credential".
        /// Specifies a user account that has permission to perform this action. The
        /// default is the current user.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Credential]
        [Alias("cred", "c")]
        public virtual PSCredential Credential
        {
            get { return credential; }

            set
            {
                credential = value;
                ValidateSpecifiedAuthentication();
            }
        }

        private PSCredential credential;

        /// <summary>
        /// The following is the definition of the input parameter "Authentication".
        /// This parameter takes a set of authentication methods the user can select
        /// from. The available method are an enum called Authentication in the
        /// System.Management.Automation.Runspaces namespace. The available options
        /// should be as follows:
        /// - Default : Use the default authentication (ad defined by the underlying
        /// protocol) for establishing a remote connection.
        /// - Negotiate
        /// - Kerberos
        /// - Basic:  Use basic authentication for establishing a remote connection.
        /// -CredSSP: Use CredSSP authentication for establishing a remote connection
        /// which will enable the user to perform credential delegation. (i.e. second
        /// hop)
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("auth", "am")]
        public virtual AuthenticationMechanism Authentication
        {
            get { return authentication; }

            set
            {
                authentication = value;
                ValidateSpecifiedAuthentication();
            }
        }

        private AuthenticationMechanism authentication = AuthenticationMechanism.Default;

        /// <summary>
        /// Specifies the certificate thumbprint to be used to impersonate the user on the
        /// remote machine.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public virtual string CertificateThumbprint
        {
            get { return thumbPrint; }

            set
            {
                thumbPrint = value;
                ValidateSpecifiedAuthentication();
            }
        }

        private string thumbPrint = null;

        internal void ValidateSpecifiedAuthentication()
        {
            WSManHelper.ValidateSpecifiedAuthentication(
                this.Authentication,
                this.Credential,
                this.CertificateThumbprint);
        }
    }

    #endregion

    #region Connect-WsMan
    /// <summary>
    /// Connect wsman cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommunications.Connect, "WSMan", DefaultParameterSetName = "ComputerName", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=141437")]
    public class ConnectWSManCommand : AuthenticatingWSManCommand
    {

        #region Parameters

        /// <summary>
        /// The following is the definition of the input parameter "ApplicationName".
        /// ApplicationName identifies the remote endpoint.
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName")]
        [ValidateNotNullOrEmpty]
        public string ApplicationName
        {
            get { return applicationname; }

            set { applicationname = value; }
        }

        private string applicationname = null;

        /// <summary>
        /// The following is the definition of the input parameter "ComputerName".
        /// Executes the management operation on the specified computer(s). The default
        /// is the local computer. Type the fully qualified domain name, NETBIOS name or
        /// IP address to indicate the remote host(s)
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName", Position = 0)]
        [Alias("cn")]
        public string ComputerName
        {
            get { return computername; }

            set
            {
                computername = value;
                if ((string.IsNullOrEmpty(computername)) || (computername.Equals(".", StringComparison.OrdinalIgnoreCase)))
                {
                    computername = "localhost";
                }
            }
        }

        private string computername = null;

        /// <summary>
        /// The following is the definition of the input parameter "ConnectionURI".
        /// Specifies the transport, server, port, and ApplicationName of the new
        /// runspace. The format of this string is:
        /// transport://server:port/ApplicationName.
        /// </summary>
        [Parameter(ParameterSetName = "URI")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        public Uri ConnectionURI
        {
            get { return connectionuri; }

            set { connectionuri = value; }
        }

        private Uri connectionuri;

        /// <summary>
        /// The following is the definition of the input parameter "OptionSet".
        /// OptionSet is a hash table and is used to pass a set of switches to the
        /// service to modify or refine the nature of the request.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("os")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Hashtable OptionSet
        {
            get { return optionset; }

            set { optionset = value; }
        }

        private Hashtable optionset;

        /// <summary>
        /// The following is the definition of the input parameter "Port".
        /// Specifies the port to be used when connecting to the ws management service.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = "ComputerName")]
        [ValidateRange(1, Int32.MaxValue)]
        public Int32 Port
        {
            get { return port; }

            set { port = value; }
        }

        private Int32 port = 0;

        /// <summary>
        /// The following is the definition of the input parameter "SessionOption".
        /// Defines a set of extended options for the WSMan session.  This hashtable can
        /// be created using New-WSManSessionOption.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("so")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public SessionOption SessionOption
        {
            get { return sessionoption; }

            set { sessionoption = value; }
        }

        private SessionOption sessionoption;

        /// <summary>
        /// The following is the definition of the input parameter "UseSSL".
        /// Uses the Secure Sockets Layer (SSL) protocol to establish a connection to
        /// the remote computer. If SSL is not available on the port specified by the
        /// Port parameter, the command fails.
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        public SwitchParameter UseSSL
        {
            get { return usessl; }

            set { usessl = value; }
        }

        private SwitchParameter usessl;

        #endregion

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {

            WSManHelper helper = new WSManHelper(this);
            if (connectionuri != null)
            {
                try
                {
                    // always in the format http://server:port/applicationname
                    string[] constrsplit = connectionuri.OriginalString.Split(new string[] { ":" + port + "/" + applicationname }, StringSplitOptions.None);
                    string[] constrsplit1 = constrsplit[0].Split(new string[] { "//" }, StringSplitOptions.None);
                    computername = constrsplit1[1].Trim();
                }
                catch (IndexOutOfRangeException)
                {
                    helper.AssertError(helper.GetResourceMsgFromResourcetext("NotProperURI"), false, connectionuri);
                }
            }

            string crtComputerName = computername;
            if (crtComputerName == null)
            {
                crtComputerName = "localhost";
            }

            if (this.SessionState.Path.CurrentProviderLocation(WSManStringLiterals.rootpath).Path.StartsWith(this.SessionState.Drive.Current.Name + ":" + WSManStringLiterals.DefaultPathSeparator + crtComputerName, StringComparison.OrdinalIgnoreCase))
            {
                helper.AssertError(helper.GetResourceMsgFromResourcetext("ConnectFailure"), false, computername);
            }

            helper.CreateWsManConnection(ParameterSetName, connectionuri, port, computername, applicationname, usessl.IsPresent, Authentication, sessionoption, Credential, CertificateThumbprint);
        }

    }
    #endregion

    #region Disconnect-WSMAN
    /// <summary>
    /// The following is the definition of the input parameter "ComputerName".
    /// Executes the management operation on the specified computer(s). The default
    /// is the local computer. Type the fully qualified domain name, NETBIOS name or
    /// IP address to indicate the remote host(s)
    /// </summary>

    [Cmdlet(VerbsCommunications.Disconnect, "WSMan", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=141439")]
    public class DisconnectWSManCommand : PSCmdlet, IDisposable
    {
        /// <summary>
        /// The following is the definition of the input parameter "ComputerName".
        /// Executes the management operation on the specified computer(s). The default
        /// is the local computer. Type the fully qualified domain name, NETBIOS name or
        /// IP address to indicate the remote host(s)
        /// </summary>
        [Parameter(Position = 0)]
        public string ComputerName
        {
            get { return computername; }

            set
            {

                computername = value;
                if ((string.IsNullOrEmpty(computername)) || (computername.Equals(".", StringComparison.OrdinalIgnoreCase)))
                {
                    computername = "localhost";
                }
            }
        }

        private string computername = null;

        #region IDisposable Members

        /// <summary>
        /// Public dispose method.
        /// </summary>
        public
        void
        Dispose()
        {
            // CleanUp();
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Public dispose method.
        /// </summary>
        public
        void
        Dispose(object session)
        {
            session = null;
            this.Dispose();

        }

        #endregion IDisposable Members

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            WSManHelper helper = new WSManHelper(this);
            if (computername == null)
            {
                computername = "localhost";
            }

            if (this.SessionState.Path.CurrentProviderLocation(WSManStringLiterals.rootpath).Path.StartsWith(WSManStringLiterals.rootpath + ":" + WSManStringLiterals.DefaultPathSeparator + computername, StringComparison.OrdinalIgnoreCase))
            {
                helper.AssertError(helper.GetResourceMsgFromResourcetext("DisconnectFailure"), false, computername);
            }

            if (computername.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                helper.AssertError(helper.GetResourceMsgFromResourcetext("LocalHost"), false, computername);
            }

            object _ws = helper.RemoveFromDictionary(computername);
            if (_ws != null)
            {
                Dispose(_ws);
            }
            else
            {
                helper.AssertError(helper.GetResourceMsgFromResourcetext("InvalidComputerName"), false, computername);
            }
        }

    }
    #endregion Disconnect-WSMAN
}
