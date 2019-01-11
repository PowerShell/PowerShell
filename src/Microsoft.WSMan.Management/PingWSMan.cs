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
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.WSMan.Management
{

    #region Test-WSMAN

    /// <summary>
    /// Issues an operation against the remote machine to ensure that the wsman
    /// service is running.
    /// </summary>

    [Cmdlet(VerbsDiagnostic.Test, "WSMan", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=141464")]
    public class TestWSManCommand : AuthenticatingWSManCommand, IDisposable
    {
        /// <summary>
        /// The following is the definition of the input parameter "ComputerName".
        /// Executes the management operation on the specified computer. The default is
        /// the local computer. Type the fully qualified domain name, NETBIOS name or IP
        /// address to indicate the remote host.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
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
        /// The following is the definition of the input parameter "Authentication".
        /// This parameter takes a set of authentication methods the user can select
        /// from. The available method are an enum called AuthenticationMechanism in the
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
        /// <remarks>
        /// Overriding to use a different default than the one in AuthenticatingWSManCommand base class
        /// </remarks>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("auth", "am")]
        public override AuthenticationMechanism Authentication
        {
            get { return authentication; }

            set
            {
                authentication = value;
                ValidateSpecifiedAuthentication();
            }
        }

        private AuthenticationMechanism authentication = AuthenticationMechanism.None;

        /// <summary>
        /// The following is the definition of the input parameter "Port".
        /// Specifies the port to be used when connecting to the ws management service.
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName")]
        [ValidateNotNullOrEmpty]
        [ValidateRange(1, Int32.MaxValue)]
        public Int32 Port
        {
            get { return port; }

            set { port = value; }
        }

        private Int32 port = 0;

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
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {

            WSManHelper helper = new WSManHelper(this);
            IWSManEx wsmanObject = (IWSManEx)new WSManClass();
            string connectionStr = string.Empty;
            connectionStr = helper.CreateConnectionString(null, port, computername, applicationname);
            IWSManSession m_SessionObj = null;
            try
            {
                m_SessionObj = helper.CreateSessionObject(wsmanObject, Authentication, null, Credential, connectionStr, CertificateThumbprint, usessl.IsPresent);
                m_SessionObj.Timeout = 1000; // 1 sec. we are putting this low so that Test-WSMan can return promptly if the server goes unresponsive.
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.LoadXml(m_SessionObj.Identify(0));
                WriteObject(xmldoc.DocumentElement);
            }
            catch(Exception)
            {
                try
                {
                    if (!string.IsNullOrEmpty(m_SessionObj.Error))
                    {
                        XmlDocument ErrorDoc = new XmlDocument();
                        ErrorDoc.LoadXml(m_SessionObj.Error);
                        InvalidOperationException ex = new InvalidOperationException(ErrorDoc.OuterXml);
                        ErrorRecord er = new ErrorRecord(ex, "WsManError", ErrorCategory.InvalidOperation, computername);
                        this.WriteError(er);
                    }
                }
                catch(Exception)
                {}
            }
            finally
            {
                if (m_SessionObj != null)
                    Dispose(m_SessionObj);
            }
        }

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
        Dispose(IWSManSession sessionObject)
        {
            sessionObject = null;
            this.Dispose();
        }

        #endregion IDisposable Members

    }
    #endregion
}
