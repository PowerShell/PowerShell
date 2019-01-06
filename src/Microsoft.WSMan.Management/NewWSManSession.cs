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
using System.Net;

namespace Microsoft.WSMan.Management
{
    /// <summary>
    /// Creates a WSMan Session option hashtable which can be passed into WSMan
    /// cmdlets:
    /// Get-WSManInstance
    /// Set-WSManInstance
    /// Invoke-WSManAction
    /// Connect-WSMan.
    /// </summary>

    [Cmdlet(VerbsCommon.New, "WSManSessionOption", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=141449")]
    public class NewWSManSessionOptionCommand : PSCmdlet
    {
        /// <summary>
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public ProxyAccessType ProxyAccessType
        {
            get
            {
                return _proxyaccesstype;
            }

            set
            {
                _proxyaccesstype = value;
            }
        }

        private ProxyAccessType _proxyaccesstype;

        /// <summary>
        /// The following is the definition of the input parameter "ProxyAuthentication".
        /// This parameter takes a set of authentication methods the user can select
        /// from.  The available options should be as follows:
        /// - Negotiate: Use the default authentication (ad defined by the underlying
        /// protocol) for establishing a remote connection.
        /// - Basic:  Use basic authentication for establishing a remote connection
        /// - Digest: Use Digest authentication for establishing a remote connection.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public ProxyAuthentication ProxyAuthentication
        {
            get { return proxyauthentication; }

            set
            {
                proxyauthentication = value;
            }
        }

        private ProxyAuthentication proxyauthentication;

        /// <summary>
        /// The following is the definition of the input parameter "ProxyCredential".
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Credential]
        public PSCredential ProxyCredential
        {
            get { return _proxycredential; }

            set
            {
                _proxycredential = value;
            }
        }

        private PSCredential _proxycredential;

        /// <summary>
        /// The following is the definition of the input parameter "SkipCACheck".
        /// When connecting over HTTPS, the client does not validate that the server
        /// certificate is signed by a trusted certificate authority (CA). Use only when
        /// the remote computer is trusted by other means, for example, if the remote
        /// computer is part of a network that is physically secure and isolated or the
        /// remote computer is listed as a trusted host in WinRM configuration.
        /// </summary>
        [Parameter]
        public SwitchParameter SkipCACheck
        {
            get { return skipcacheck; }

            set
            {
                skipcacheck = value;
            }
        }

        private bool skipcacheck;

        /// <summary>
        /// The following is the definition of the input parameter "SkipCNCheck".
        /// Indicates that certificate common name (CN) of the server need not match the
        /// hostname of the server. Used only in remote operations using https. This
        /// option should only be used for trusted machines.
        /// </summary>
        [Parameter]
        public SwitchParameter SkipCNCheck
        {
            get { return skipcncheck; }

            set
            {
                skipcncheck = value;
            }
        }

        private bool skipcncheck;

        /// <summary>
        /// The following is the definition of the input parameter "SkipRevocation".
        /// Indicates that certificate common name (CN) of the server need not match the
        /// hostname of the server. Used only in remote operations using https. This
        /// option should only be used for trusted machines.
        /// </summary>
        [Parameter]
        public SwitchParameter SkipRevocationCheck
        {
            get { return skiprevocationcheck; }

            set
            {
                skiprevocationcheck = value;
            }
        }

        private bool skiprevocationcheck;

        /// <summary>
        /// The following is the definition of the input parameter "SPNPort".
        /// Appends port number to the connection Service Principal Name SPN of the
        /// remote server.
        /// SPN is used when authentication mechanism is Kerberos or Negotiate.
        /// </summary>
        [Parameter]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SPN")]
        [ValidateRange(0, Int32.MaxValue)]
        public Int32 SPNPort
        {
            get { return spnport; }

            set
            {
                spnport = value;
            }
        }

        private Int32 spnport;

        /// <summary>
        /// The following is the definition of the input parameter "Timeout".
        /// Defines the timeout in ms for the wsman operation.
        /// </summary>
        [Parameter]
        [Alias("OperationTimeoutMSec")]
        [ValidateRange(0, Int32.MaxValue)]
        public Int32 OperationTimeout
        {
            get { return operationtimeout; }

            set
            {
                operationtimeout = value;
            }
        }

        private Int32 operationtimeout;

        /// <summary>
        /// The following is the definition of the input parameter "UnEncrypted".
        /// Specifies that no encryption will be used when doing remote operations over
        /// http. Unencrypted traffic is not allowed by default and must be enabled in
        /// the local configuration.
        /// </summary>
        [Parameter]
        public SwitchParameter NoEncryption
        {
            get { return noencryption; }

            set
            {
                noencryption = value;
            }
        }

        private bool noencryption;

        /// <summary>
        /// The following is the definition of the input parameter "UTF16".
        /// Indicates the request is encoded in UTF16 format rather than UTF8 format;
        /// UTF8 is the default.
        /// </summary>
        [Parameter]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "UTF")]
        public SwitchParameter UseUTF16
        {
            get { return useutf16; }

            set
            {
                useutf16 = value;
            }
        }

        private bool useutf16;

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            WSManHelper helper = new WSManHelper(this);

            if (proxyauthentication.Equals(ProxyAuthentication.Basic) || proxyauthentication.Equals(ProxyAuthentication.Digest))
            {
                if (_proxycredential == null)
                {
                    InvalidOperationException ex = new InvalidOperationException(helper.GetResourceMsgFromResourcetext("NewWSManSessionOptionCred"));
                    ErrorRecord er = new ErrorRecord(ex, "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                    WriteError(er);
                    return;
                }
            }

            if ((_proxycredential != null) && (proxyauthentication == 0))
            {
                InvalidOperationException ex = new InvalidOperationException(helper.GetResourceMsgFromResourcetext("NewWSManSessionOptionAuth"));
                ErrorRecord er = new ErrorRecord(ex, "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                WriteError(er);
                return;
            }

            // Creating the Session Object
            SessionOption objSessionOption = new SessionOption();

            objSessionOption.SPNPort = spnport;
            objSessionOption.UseUtf16 = useutf16;
            objSessionOption.SkipCNCheck = skipcncheck;
            objSessionOption.SkipCACheck = skipcacheck;
            objSessionOption.OperationTimeout = operationtimeout;
            objSessionOption.SkipRevocationCheck = skiprevocationcheck;
            // Proxy Settings
            objSessionOption.ProxyAccessType = _proxyaccesstype;
            objSessionOption.ProxyAuthentication = proxyauthentication;

            if (noencryption)
            {
                objSessionOption.UseEncryption = false;
            }

            if (_proxycredential != null)
            {
                NetworkCredential nwCredentials = _proxycredential.GetNetworkCredential();
                objSessionOption.ProxyCredential = nwCredentials;
            }

            WriteObject(objSessionOption);

        }
    }
}
