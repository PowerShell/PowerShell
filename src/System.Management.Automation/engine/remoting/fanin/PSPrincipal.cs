// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/*
 * Contains definition for PSSenderInfo, PSPrincipal, PSIdentity which are
 * used to provide remote user information to different plugin snapins
 * like Exchange.
 */

using System;
using System.Security.Principal;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.PowerShell;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// This class is used in the server side remoting scenarios. This class
    /// holds information about the incoming connection like:
    /// (a) Connecting User information
    /// (b) Connection String used by the user to connect to the server.
    /// </summary>
    [Serializable]
    public sealed class PSSenderInfo : ISerializable
    {
        #region Private Data

        private PSPrimitiveDictionary _applicationArguments;

        #endregion

        #region Serialization

        /// <summary>
        /// Serialization.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            PSObject psObject = PSObject.AsPSObject(this);
            psObject.GetObjectData(info, context);
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        private PSSenderInfo(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                return;
            }

            string serializedData = null;

            try
            {
                serializedData = info.GetValue("CliXml", typeof(string)) as string;
            }
            catch (Exception)
            {
                // When a workflow is run locally, there won't be PSSenderInfo
                return;
            }

            if (serializedData == null)
            {
                return;
            }

            try
            {
                PSObject result = PSObject.AsPSObject(PSSerializer.Deserialize(serializedData));
                PSSenderInfo senderInfo = DeserializingTypeConverter.RehydratePSSenderInfo(result);

                UserInfo = senderInfo.UserInfo;
                ConnectionString = senderInfo.ConnectionString;
                _applicationArguments = senderInfo._applicationArguments;
            }
            catch (Exception)
            {
                // Ignore conversion errors
                return;
            }
        }

        #endregion

        #region Public Constructors

        /// <summary>
        /// Constructs PSPrincipal using PSIdentity and a token (used to construct WindowsIdentity)
        /// </summary>
        /// <param name="userPrincipal">
        /// Connecting User Information
        /// </param>
        /// <param name="httpUrl">
        /// httpUrl element (from WSMAN_SENDER_DETAILS struct).
        /// </param>
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "1#")]
        public PSSenderInfo(PSPrincipal userPrincipal, string httpUrl)
        {
            UserInfo = userPrincipal;
            ConnectionString = httpUrl;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Contains information related to the user connecting to the server.
        /// </summary>
        public PSPrincipal UserInfo
        {
            get;
            // No public set because PSSenderInfo/PSPrincipal is used by PSSessionConfiguration's
            // and usually they dont cache this data internally..so did not want to give
            // cmdlets/scripts a chance to modify these.
        }

        /// <summary>
        /// Contains the TimeZone information from the client machine.
        /// </summary>
        public TimeZoneInfo ClientTimeZone => null;

        /// <summary>
        /// Connection string used by the client to connect to the server. This is
        /// directly taken from WSMAN_SENDER_DETAILS struct (from wsman.h)
        /// </summary>
        public string ConnectionString
        {
            get;
            // No public set because PSSenderInfo/PSPrincipal is used by PSSessionConfiguration's
            // and usually they dont cache this data internally..so did not want to give
            // cmdlets/scripts a chance to modify these.
        }

        /// <summary>
        /// Application arguments (i.e. specified in New-PSSessionOptions -ApplicationArguments)
        /// </summary>
        public PSPrimitiveDictionary ApplicationArguments
        {
            get { return _applicationArguments; }

            internal set { _applicationArguments = value; }
        }

        /// <summary>
        /// "ConfigurationName" from the sever remote session.
        /// </summary>
        public string ConfigurationName { get; internal set; }

        #endregion
    }

    /// <summary>
    /// Defines the basic functionality of a PSPrincipal object.
    /// </summary>
    public sealed class PSPrincipal : IPrincipal
    {
        #region Private Data

        #endregion

        /// <summary>
        /// Gets the identity of the current user principal.
        /// </summary>
        public PSIdentity Identity
        {
            get;
            // No public set because PSSenderInfo/PSPrincipal is used by PSSessionConfiguration's
            // and usually they dont cache this data internally..so did not want to give
            // cmdlets/scripts a chance to modify these.
        }

        /// <summary>
        /// Gets the WindowsIdentity (if possible) representation of the current Identity.
        /// PSPrincipal can represent any user for example a LiveID user, network user within
        /// a domain etc. This property tries to convert the Identity to WindowsIdentity
        /// using the user token supplied.
        /// </summary>
        public WindowsIdentity WindowsIdentity
        {
            get;
            // No public set because PSSenderInfo/PSPrincipal is used by PSSessionConfiguration's
            // and usually they dont cache this data internally..so did not want to give
            // cmdlets/scripts a chance to modify these.
        }

        /// <summary>
        /// Gets the identity of the current principal.
        /// </summary>
        IIdentity IPrincipal.Identity
        {
            get { return this.Identity; }
        }

        /// <summary>
        /// Determines if the current principal belongs to a specified rule.
        /// If we were able to get a WindowsIdentity then this will perform the
        /// check using the WindowsIdentity otherwise this will return false.
        /// </summary>
        /// <param name="role"></param>
        /// <returns>
        /// If we were able to get a WindowsIdentity then this will perform the
        /// check using the WindowsIdentity otherwise this will return false.
        /// </returns>
        public bool IsInRole(string role)
        {
            if (WindowsIdentity != null)
            {
                // Get Windows Principal for this identity
                WindowsPrincipal windowsPrincipal = new WindowsPrincipal(WindowsIdentity);
                return windowsPrincipal.IsInRole(role);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Internal overload of IsInRole() taking a WindowsBuiltInRole enum value.
        /// </summary>
        internal bool IsInRole(WindowsBuiltInRole role)
        {
            if (WindowsIdentity != null)
            {
                // Get Windows Principal for this identity
                WindowsPrincipal windowsPrincipal = new WindowsPrincipal(WindowsIdentity);
                return windowsPrincipal.IsInRole(role);
            }
            else
            {
                return false;
            }
        }

        #region Constructor

        /// <summary>
        /// Constructs PSPrincipal using PSIdentity and a WindowsIdentity.
        /// </summary>
        /// <param name="identity">
        /// An instance of PSIdentity
        /// </param>
        /// <param name="windowsIdentity">
        /// An instance of WindowsIdentity, if psIdentity represents a windows user. This can be
        /// null.
        /// </param>
        public PSPrincipal(PSIdentity identity, WindowsIdentity windowsIdentity)
        {
            Identity = identity;
            WindowsIdentity = windowsIdentity;
        }

        #endregion
    }

    /// <summary>
    /// Defines the basic functionality of a PSIdentity object.
    /// </summary>
    public sealed class PSIdentity : IIdentity
    {
        #region Private Data

        #endregion

        /// <summary>
        /// Gets the type of authentication used.
        /// For a WSMan service authenticated user this will be one of the following:
        ///  WSMAN_DEFAULT_AUTHENTICATION
        ///  WSMAN_NO_AUTHENTICATION
        ///  WSMAN_AUTH_DIGEST
        ///  WSMAN_AUTH_NEGOTIATE
        ///  WSMAN_AUTH_BASIC
        ///  WSMAN_AUTH_KERBEROS
        ///  WSMAN_AUTH_CLIENT_CERTIFICATE
        ///  WSMAN_AUTH_LIVEID.
        /// </summary>
        public string AuthenticationType { get; }

        /// <summary>
        /// Gets a value that indicates whether the user has been authenticated.
        /// </summary>
        public bool IsAuthenticated { get; }

        /// <summary>
        /// Gets the name of the user.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the certificate details of the user if supported, null otherwise.
        /// </summary>
        public PSCertificateDetails CertificateDetails { get; }

        #region Public Constructor

        /// <summary>
        /// Constructor used to construct a PSIdentity object.
        /// </summary>
        /// <param name="authType">
        /// Type of authentication used to authenticate this user.
        /// For a WSMan service authenticated user this will be one of the following:
        ///  WSMAN_DEFAULT_AUTHENTICATION
        ///  WSMAN_NO_AUTHENTICATION
        ///  WSMAN_AUTH_DIGEST
        ///  WSMAN_AUTH_NEGOTIATE
        ///  WSMAN_AUTH_BASIC
        ///  WSMAN_AUTH_KERBEROS
        ///  WSMAN_AUTH_CLIENT_CERTIFICATE
        ///  WSMAN_AUTH_LIVEID
        /// </param>
        /// <param name="isAuthenticated">
        /// true if this user is authenticated.
        /// </param>
        /// <param name="userName">
        /// Name of the user
        /// </param>
        /// <param name="cert">
        /// Certificate details if Certificate authentication is used.
        /// </param>
        public PSIdentity(string authType, bool isAuthenticated, string userName, PSCertificateDetails cert)
        {
            AuthenticationType = authType;
            IsAuthenticated = isAuthenticated;
            Name = userName;
            CertificateDetails = cert;
        }

        #endregion
    }

    /// <summary>
    /// Represents the certificate of a user.
    /// </summary>
    public sealed class PSCertificateDetails
    {
        #region Private Data

        #endregion

        /// <summary>
        /// Gets Subject of the certificate.
        /// </summary>
        public string Subject { get; }

        /// <summary>
        /// Gets the issuer name of the certificate.
        /// </summary>
        public string IssuerName { get; }

        /// <summary>
        /// Gets the issuer thumb print.
        /// </summary>
        public string IssuerThumbprint { get; }

        #region Constructor

        /// <summary>
        /// Constructor used to construct a PSCertificateDetails object.
        /// </summary>
        /// <param name="subject">
        /// Subject of the certificate.
        /// </param>
        /// <param name="issuerName">
        /// Issuer name of the certificate.
        /// </param>
        /// <param name="issuerThumbprint">
        /// Issuer thumb print of the certificate.
        /// </param>
        public PSCertificateDetails(string subject, string issuerName, string issuerThumbprint)
        {
            Subject = subject;
            IssuerName = issuerName;
            IssuerThumbprint = issuerThumbprint;
        }

        #endregion
    }
}
