// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;

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
        public bool SkipCACheck { get; set; }

        /// <summary>
        /// Property.
        /// </summary>
        public bool SkipCNCheck { get; set; }

        /// <summary>
        /// Property.
        /// </summary>
        public bool SkipRevocationCheck { get; set; }

        /// <summary>
        /// Property.
        /// </summary>
        public bool UseEncryption { get; set; } = true;

        /// <summary>
        /// Property.
        /// </summary>
        public bool UseUtf16 { get; set; }

        /// <summary>
        /// Property.
        /// </summary>
        public ProxyAuthentication ProxyAuthentication { get; set; }

        /// <summary>
        /// Property.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SPN")]
        public int SPNPort { get; set; }

        /// <summary>
        /// Property.
        /// </summary>
        public int OperationTimeout { get; set; }

        /// <summary>
        /// Property.
        /// </summary>
        public NetworkCredential ProxyCredential { get; set; }

        /// <summary>
        /// Property.
        /// </summary>
        public ProxyAccessType ProxyAccessType { get; set; }
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
