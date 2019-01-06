// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Xml;
using System.Net;
using System.Resources;
using System.Reflection;
using System.ComponentModel;

using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly: CLSCompliant(true)]
namespace Microsoft.WSMan.Management
{

    /// <summary>
    /// Session option class.
    /// </summary>

    public sealed class SessionOption
    {

        /// <summary>
        /// Property.
        /// </summary>
        public bool SkipCACheck
        {
            get { return _SkipCACheck; }

            set
            {
                _SkipCACheck = value;
            }
        }

        private bool _SkipCACheck;

        /// <summary>
        /// Property.
        /// </summary>
        public bool SkipCNCheck
        {
            get { return _SkipCNCheck; }

            set
            {
                _SkipCNCheck = value;
            }
        }

        private bool _SkipCNCheck;

        /// <summary>
        /// Property.
        /// </summary>
        public bool SkipRevocationCheck
        {
            get { return _SkipRevocationCheck; }

            set
            {
                _SkipRevocationCheck = value;
            }

        }

        private bool _SkipRevocationCheck;

        /// <summary>
        /// Property.
        /// </summary>
        public bool UseEncryption
        {
            get { return _useencryption; }

            set
            {
                _useencryption = value;
            }
        }

        private bool _useencryption = true;

        /// <summary>
        /// Property.
        /// </summary>
        public bool UseUtf16
        {
            get { return _UTF16; }

            set
            {
                _UTF16 = value;
            }
        }

        private bool _UTF16;

        /// <summary>
        /// Property.
        /// </summary>
        public ProxyAuthentication ProxyAuthentication
        {
            get { return _ProxyAuthentication; }

            set
            {
                _ProxyAuthentication = value;
            }
        }

        private ProxyAuthentication _ProxyAuthentication;

        /// <summary>
        /// Property.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SPN")]
        public int SPNPort
        {
            get { return _SPNPort; }

            set
            {
                _SPNPort = value;
            }
        }

        private int _SPNPort;

        /// <summary>
        /// Property.
        /// </summary>
        public int OperationTimeout
        {
            get { return _OperationTimeout; }

            set
            {
                _OperationTimeout = value;
            }
        }

        private int _OperationTimeout;

        /// <summary>
        /// Property.
        /// </summary>
        public NetworkCredential ProxyCredential
        {
            get { return _ProxyCredential; }

            set
            {
                _ProxyCredential = value;
            }
        }

        private NetworkCredential _ProxyCredential;

        /// <summary>
        /// Property.
        /// </summary>
        public ProxyAccessType ProxyAccessType
        {
            get { return _proxyaccesstype; }

            set
            {
                _proxyaccesstype = value;
            }
        }

        private ProxyAccessType _proxyaccesstype;
    }

    /// <summary>
    /// Property.
    /// </summary>
    public enum ProxyAccessType
    {
        /// <summary>
        /// Property.
        /// </summary>
        ProxyIEConfig = 0,
        /// <summary>
        /// Property.
        /// </summary>
        ProxyWinHttpConfig = 1,
        /// <summary>
        /// Property.
        /// </summary>
        ProxyAutoDetect = 2,
        /// <summary>
        /// Property.
        /// </summary>
        ProxyNoProxyServer = 3
    }

    /// <summary>
    /// Property.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum ProxyAuthentication
    {
        /// <summary>
        /// Property.
        /// </summary>
        Negotiate = 1,
        /// <summary>
        /// Property.
        /// </summary>
        Basic = 2,
        /// <summary>
        /// Property.
        /// </summary>
        Digest = 4
    }
}
