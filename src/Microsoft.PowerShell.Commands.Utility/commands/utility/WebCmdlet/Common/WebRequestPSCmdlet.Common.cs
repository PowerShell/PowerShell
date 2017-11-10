/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The valid values for the -Authentication parameter for Invoke-RestMethod and Invoke-WebRequest
    /// </summary>
    public enum WebAuthenticationType
    {
        /// <summary>
        /// No authentication. Default.
        /// </summary>
        None,

        /// <summary>
        /// RFC-7617 Basic Authentication. Requires -Credential
        /// </summary>
        Basic,

        /// <summary>
        /// RFC-6750 OAuth 2.0 Bearer Authentication. Requires -Token
        /// </summary>
        Bearer,

        /// <summary>
        /// RFC-6750 OAuth 2.0 Bearer Authentication. Requires -Token
        /// </summary>
        OAuth,
    }

    /// <summary>
    /// Base class for Invoke-RestMethod and Invoke-WebRequest commands.
    /// </summary>
    public abstract partial class WebRequestPSCmdlet : PSCmdlet
    {
        #region Virtual Properties

        #region URI

        /// <summary>
        /// Deprecated. Gets or sets UseBasicParsing. This has no affect on the operation of the Cmdlet.
        /// </summary>
        [Parameter(DontShow = true)]
        public virtual SwitchParameter UseBasicParsing { get; set; } = true;

        /// <summary>
        /// gets or sets the Uri property
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public virtual Uri Uri { get; set; }

        #endregion

        #region Session
        /// <summary>
        /// gets or sets the Session property
        /// </summary>
        [Parameter]
        public virtual WebRequestSession WebSession { get; set; }

        /// <summary>
        /// gets or sets the SessionVariable property
        /// </summary>
        [Parameter]
        [Alias("SV")]
        public virtual string SessionVariable { get; set; }

        #endregion

        #region Authorization and Credentials

        /// <summary>
        /// Gets or sets the AllowUnencryptedAuthentication property
        /// </summary>
        [Parameter]
        public virtual SwitchParameter AllowUnencryptedAuthentication { get; set; }

        /// <summary>
        /// Gets or sets the Authentication property used to determin the Authentication method for the web session.
        /// Authentication does not work with UseDefaultCredentials.
        /// Authentication over unencrypted sessions requires AllowUnencryptedAuthentication.
        /// Basic: Requires Credential
        /// OAuth/Bearer: Requires Token
        /// </summary>
        [Parameter]
        public virtual WebAuthenticationType Authentication { get; set; } = WebAuthenticationType.None;

        /// <summary>
        /// gets or sets the Credential property
        /// </summary>
        [Parameter]
        [Credential]
        public virtual PSCredential Credential { get; set; }

        /// <summary>
        /// gets or sets the UseDefaultCredentials property
        /// </summary>
        [Parameter]
        public virtual SwitchParameter UseDefaultCredentials { get; set; }

        /// <summary>
        /// gets or sets the CertificateThumbprint property
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public virtual string CertificateThumbprint { get; set; }

        /// <summary>
        /// gets or sets the Certificate property
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public virtual X509Certificate Certificate { get; set; }

        /// <summary>
        /// gets or sets the SkipCertificateCheck property
        /// </summary>
        [Parameter]
        public virtual SwitchParameter SkipCertificateCheck { get; set; }

        /// <summary>
        /// Gets or sets the Token property. Token is required by Authentication OAuth and Bearer.
        /// </summary>
        [Parameter]
        public virtual SecureString Token { get; set; }

        #endregion

        #region Headers

        /// <summary>
        /// gets or sets the UserAgent property
        /// </summary>
        [Parameter]
        public virtual string UserAgent { get; set; }

        /// <summary>
        /// gets or sets the DisableKeepAlive property
        /// </summary>
        [Parameter]
        public virtual SwitchParameter DisableKeepAlive { get; set; }

        /// <summary>
        /// gets or sets the TimeOut property
        /// </summary>
        [Parameter]
        [ValidateRange(0, Int32.MaxValue)]
        public virtual int TimeoutSec { get; set; }

        /// <summary>
        /// gets or sets the Headers property
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Parameter]
        public virtual IDictionary Headers { get; set; }

        #endregion

        #region Redirect

        /// <summary>
        /// gets or sets the RedirectMax property
        /// </summary>
        [Parameter]
        [ValidateRange(0, Int32.MaxValue)]
        public virtual int MaximumRedirection
        {
            get { return _maximumRedirection; }
            set { _maximumRedirection = value; }
        }
        private int _maximumRedirection = -1;

        #endregion

        #region Method

        /// <summary>
        /// gets or sets the Method property
        /// </summary>
        [Parameter(ParameterSetName = "StandardMethod")]
        [Parameter(ParameterSetName = "StandardMethodNoProxy")]
        public virtual WebRequestMethod Method
        {
            get { return _method; }
            set { _method = value; }
        }
        private WebRequestMethod _method = WebRequestMethod.Default;

        /// <summary>
        /// gets or sets the CustomMethod property
        /// </summary>
        [Parameter(Mandatory=true,ParameterSetName = "CustomMethod")]
        [Parameter(Mandatory=true,ParameterSetName = "CustomMethodNoProxy")]
        [Alias("CM")]
        [ValidateNotNullOrEmpty]
        public virtual string CustomMethod
        {
            get { return _customMethod; }
            set { _customMethod = value; }
        }
        private string _customMethod;

        #endregion

        #region NoProxy

        /// <summary>
        /// gets or sets the NoProxy property
        /// </summary>
        [Parameter(Mandatory=true,ParameterSetName = "CustomMethodNoProxy")]
        [Parameter(Mandatory=true,ParameterSetName = "StandardMethodNoProxy")]
        public virtual SwitchParameter NoProxy { get; set; }

        #endregion

        #region Proxy

        /// <summary>
        /// gets or sets the Proxy property
        /// </summary>
        [Parameter(ParameterSetName = "StandardMethod")]
        [Parameter(ParameterSetName = "CustomMethod")]
        public virtual Uri Proxy { get; set; }

        /// <summary>
        /// gets or sets the ProxyCredential property
        /// </summary>
        [Parameter(ParameterSetName = "StandardMethod")]
        [Parameter(ParameterSetName = "CustomMethod")]
        [Credential]
        public virtual PSCredential ProxyCredential { get; set; }

        /// <summary>
        /// gets or sets the ProxyUseDefaultCredentials property
        /// </summary>
        [Parameter(ParameterSetName = "StandardMethod")]
        [Parameter(ParameterSetName = "CustomMethod")]
        public virtual SwitchParameter ProxyUseDefaultCredentials { get; set; }

        #endregion

        #region Input

        /// <summary>
        /// gets or sets the Body property
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public virtual object Body { get; set; }

        /// <summary>
        /// gets or sets the ContentType property
        /// </summary>
        [Parameter]
        public virtual string ContentType { get; set; }

        /// <summary>
        /// gets or sets the TransferEncoding property
        /// </summary>
        [Parameter]
        [ValidateSet("chunked", "compress", "deflate", "gzip", "identity", IgnoreCase = true)]
        public virtual string TransferEncoding { get; set; }

        /// <summary>
        /// gets or sets the InFile property
        /// </summary>
        [Parameter]
        public virtual string InFile { get; set; }

        /// <summary>
        /// keep the original file path after the resolved provider path is
        /// assigned to InFile
        /// </summary>
        private string _originalFilePath;

        #endregion

        #region Output

        /// <summary>
        /// gets or sets the OutFile property
        /// </summary>
        [Parameter]
        public virtual string OutFile { get; set; }

        /// <summary>
        /// gets or sets the PassThrough property
        /// </summary>
        [Parameter]
        public virtual SwitchParameter PassThru { get; set; }

        #endregion

        #endregion Virtual Properties

        #region Virtual Methods

        internal virtual void ValidateParameters()
        {
            // sessions
            if ((null != WebSession) && (null != SessionVariable))
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.SessionConflict,
                                                       "WebCmdletSessionConflictException");
                ThrowTerminatingError(error);
            }

            // Authentication
            if (UseDefaultCredentials && (Authentication != WebAuthenticationType.None))
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.AuthenticationConflict,
                                                       "WebCmdletAuthenticationConflictException");
                ThrowTerminatingError(error);
            }
            if ((Authentication != WebAuthenticationType.None) && (null != Token) && (null != Credential))
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.AuthenticationTokenConflict,
                                                       "WebCmdletAuthenticationTokenConflictException");
                ThrowTerminatingError(error);
            }
            if ((Authentication == WebAuthenticationType.Basic) && (null == Credential))
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.AuthenticationCredentialNotSupplied,
                                                       "WebCmdletAuthenticationCredentialNotSuppliedException");
                ThrowTerminatingError(error);
            }
            if ((Authentication == WebAuthenticationType.OAuth || Authentication == WebAuthenticationType.Bearer) && (null == Token))
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.AuthenticationTokenNotSupplied,
                                                       "WebCmdletAuthenticationTokenNotSuppliedException");
                ThrowTerminatingError(error);
            }
            if (!AllowUnencryptedAuthentication && (Authentication != WebAuthenticationType.None) && (Uri.Scheme != "https"))
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.AllowUnencryptedAuthenticationRequired,
                                                       "WebCmdletAllowUnencryptedAuthenticationRequiredException");
                ThrowTerminatingError(error);
            }
            if (!AllowUnencryptedAuthentication && (null != Credential || UseDefaultCredentials) && (Uri.Scheme != "https"))
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.AllowUnencryptedAuthenticationRequired,
                                                       "WebCmdletAllowUnencryptedAuthenticationRequiredException");
                ThrowTerminatingError(error);
            }

            // credentials
            if (UseDefaultCredentials && (null != Credential))
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.CredentialConflict,
                                                       "WebCmdletCredentialConflictException");
                ThrowTerminatingError(error);
            }

            // Proxy server
            if (ProxyUseDefaultCredentials && (null != ProxyCredential))
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.ProxyCredentialConflict,
                                                       "WebCmdletProxyCredentialConflictException");
                ThrowTerminatingError(error);
            }
            else if ((null == Proxy) && ((null != ProxyCredential) || ProxyUseDefaultCredentials))
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.ProxyUriNotSupplied,
                                                       "WebCmdletProxyUriNotSuppliedException");
                ThrowTerminatingError(error);
            }

            // request body content
            if ((null != Body) && (null != InFile))
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.BodyConflict,
                                                       "WebCmdletBodyConflictException");
                ThrowTerminatingError(error);
            }

            // validate InFile path
            if (InFile != null)
            {
                ProviderInfo provider = null;
                ErrorRecord errorRecord = null;

                try
                {
                    Collection<string> providerPaths = GetResolvedProviderPathFromPSPath(InFile, out provider);

                    if (!provider.Name.Equals(FileSystemProvider.ProviderName, StringComparison.OrdinalIgnoreCase))
                    {
                        errorRecord = GetValidationError(WebCmdletStrings.NotFilesystemPath,
                                                         "WebCmdletInFileNotFilesystemPathException", InFile);
                    }
                    else
                    {
                        if (providerPaths.Count > 1)
                        {
                            errorRecord = GetValidationError(WebCmdletStrings.MultiplePathsResolved,
                                                             "WebCmdletInFileMultiplePathsResolvedException", InFile);
                        }
                        else if (providerPaths.Count == 0)
                        {
                            errorRecord = GetValidationError(WebCmdletStrings.NoPathResolved,
                                                             "WebCmdletInFileNoPathResolvedException", InFile);
                        }
                        else
                        {
                            if (Directory.Exists(providerPaths[0]))
                            {
                                errorRecord = GetValidationError(WebCmdletStrings.DirectoryPathSpecified,
                                                                 "WebCmdletInFileNotFilePathException", InFile);
                            }
                            _originalFilePath = InFile;
                            InFile = providerPaths[0];
                        }
                    }
                }
                catch (ItemNotFoundException pathNotFound)
                {
                    errorRecord = new ErrorRecord(pathNotFound.ErrorRecord, pathNotFound);
                }
                catch (ProviderNotFoundException providerNotFound)
                {
                    errorRecord = new ErrorRecord(providerNotFound.ErrorRecord, providerNotFound);
                }
                catch (System.Management.Automation.DriveNotFoundException driveNotFound)
                {
                    errorRecord = new ErrorRecord(driveNotFound.ErrorRecord, driveNotFound);
                }

                if (errorRecord != null)
                {
                    ThrowTerminatingError(errorRecord);
                }
            }

            // output ??
            if (PassThru && (OutFile == null))
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.OutFileMissing,
                                                       "WebCmdletOutFileMissingException");
                ThrowTerminatingError(error);
            }
        }

        internal virtual void PrepareSession()
        {
            // make sure we have a valid WebRequestSession object to work with
            if (null == WebSession)
            {
                WebSession = new WebRequestSession();
            }

            if (null != SessionVariable)
            {
                // save the session back to the PS environment if requested
                PSVariableIntrinsics vi = SessionState.PSVariable;
                vi.Set(SessionVariable, WebSession);
            }

            //
            // handle credentials
            //
            if (null != Credential && Authentication == WebAuthenticationType.None)
            {
                // get the relevant NetworkCredential
                NetworkCredential netCred = Credential.GetNetworkCredential();
                WebSession.Credentials = netCred;

                // supplying a credential overrides the UseDefaultCredentials setting
                WebSession.UseDefaultCredentials = false;
            }
            else if ((null != Credential || null!= Token) && Authentication != WebAuthenticationType.None)
            {
                ProcessAuthentication();
            }
            else if (UseDefaultCredentials)
            {
                WebSession.UseDefaultCredentials = true;
            }


            if (null != CertificateThumbprint)
            {
                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
                X509Certificate2Collection tbCollection = (X509Certificate2Collection)collection.Find(X509FindType.FindByThumbprint, CertificateThumbprint, false);
                if (tbCollection.Count == 0)
                {
                    CryptographicException ex = new CryptographicException(WebCmdletStrings.ThumbprintNotFound);
                    throw ex;
                }
                foreach (X509Certificate2 tbCert in tbCollection)
                {
                    X509Certificate certificate = (X509Certificate)tbCert;
                    WebSession.AddCertificate(certificate);
                }
            }

            if (null != Certificate)
            {
                WebSession.AddCertificate(Certificate);
            }

            //
            // handle the user agent
            //
            if (null != UserAgent)
            {
                // store the UserAgent string
                WebSession.UserAgent = UserAgent;
            }

            if (null != Proxy)
            {
                WebProxy webProxy = new WebProxy(Proxy);
                webProxy.BypassProxyOnLocal = false;
                if (null != ProxyCredential)
                {
                    webProxy.Credentials = ProxyCredential.GetNetworkCredential();
                }
                else if (ProxyUseDefaultCredentials)
                {
                    // If both ProxyCredential and ProxyUseDefaultCredentials are passed,
                    // UseDefaultCredentials will overwrite the supplied credentials.
                    webProxy.UseDefaultCredentials = true;
                }
                WebSession.Proxy = webProxy;
            }

            if (-1 < MaximumRedirection)
            {
                WebSession.MaximumRedirection = MaximumRedirection;
            }

            // store the other supplied headers
            if (null != Headers)
            {
                foreach (string key in Headers.Keys)
                {
                    // add the header value (or overwrite it if already present)
                    WebSession.Headers[key] = Headers[key].ToString();
                }
            }
        }

        #endregion Virtual Methods

        #region Helper Properties

        internal string QualifiedOutFile
        {
            get { return (QualifyFilePath(OutFile)); }
        }

        internal bool ShouldSaveToOutFile
        {
            get { return (!string.IsNullOrEmpty(OutFile)); }
        }

        internal bool ShouldWriteToPipeline
        {
            get { return (!ShouldSaveToOutFile || PassThru); }
        }

        #endregion Helper Properties

        #region Helper Methods

        private Uri PrepareUri(Uri uri)
        {
            uri = CheckProtocol(uri);

            // before creating the web request,
            // preprocess Body if content is a dictionary and method is GET (set as query)
            IDictionary bodyAsDictionary;
            LanguagePrimitives.TryConvertTo<IDictionary>(Body, out bodyAsDictionary);
            if ((null != bodyAsDictionary)
                && ((IsStandardMethodSet() && (Method == WebRequestMethod.Default || Method == WebRequestMethod.Get))
                     || (IsCustomMethodSet() && CustomMethod.ToUpperInvariant() == "GET")))
            {
                UriBuilder uriBuilder = new UriBuilder(uri);
                if (uriBuilder.Query != null && uriBuilder.Query.Length > 1)
                {
                    uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + FormatDictionary(bodyAsDictionary);
                }
                else
                {
                    uriBuilder.Query = FormatDictionary(bodyAsDictionary);
                }
                uri = uriBuilder.Uri;
                // set body to null to prevent later FillRequestStream
                Body = null;
            }

            return uri;
        }

        private Uri CheckProtocol(Uri uri)
        {
            if (null == uri) { throw new ArgumentNullException("uri"); }

            if (!uri.IsAbsoluteUri)
            {
                uri = new Uri("http://" + uri.OriginalString);
            }
            return (uri);
        }

        private string QualifyFilePath(string path)
        {
            string resolvedFilePath = PathUtils.ResolveFilePath(path, this, false);
            return resolvedFilePath;
        }

        private string FormatDictionary(IDictionary content)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            StringBuilder bodyBuilder = new StringBuilder();
            foreach (string key in content.Keys)
            {
                if (0 < bodyBuilder.Length)
                {
                    bodyBuilder.Append("&");
                }

                object value = content[key];

                // URLEncode the key and value
                string encodedKey = WebUtility.UrlEncode(key);
                string encodedValue = String.Empty;
                if (null != value)
                {
                    encodedValue = WebUtility.UrlEncode(value.ToString());
                }

                bodyBuilder.AppendFormat("{0}={1}", encodedKey, encodedValue);
            }
            return bodyBuilder.ToString();
        }

        private ErrorRecord GetValidationError(string msg, string errorId)
        {
            var ex = new ValidationMetadataException(msg);
            var error = new ErrorRecord(ex, errorId, ErrorCategory.InvalidArgument, this);
            return (error);
        }

        private ErrorRecord GetValidationError(string msg, string errorId, params object[] args)
        {
            msg = string.Format(CultureInfo.InvariantCulture, msg, args);
            var ex = new ValidationMetadataException(msg);
            var error = new ErrorRecord(ex, errorId, ErrorCategory.InvalidArgument, this);
            return (error);
        }

        private bool IsStandardMethodSet()
        {
            return (ParameterSetName == "StandardMethod");
        }

        private bool IsCustomMethodSet()
        {
            return (ParameterSetName == "CustomMethod");
        }

        private string GetBasicAuthorizationHeader()
        {
            string unencoded = String.Format("{0}:{1}", Credential.UserName, Credential.GetNetworkCredential().Password);
            Byte[] bytes = Encoding.UTF8.GetBytes(unencoded);
            return String.Format("Basic {0}", Convert.ToBase64String(bytes));
        }

        private string GetBearerAuthorizationHeader()
        {
            return String.Format("Bearer {0}", new NetworkCredential(String.Empty, Token).Password);
        }

        private void ProcessAuthentication()
        {
            if(Authentication == WebAuthenticationType.Basic)
            {
                WebSession.Headers["Authorization"] = GetBasicAuthorizationHeader();
            }
            else if (Authentication == WebAuthenticationType.Bearer || Authentication == WebAuthenticationType.OAuth)
            {
                WebSession.Headers["Authorization"] = GetBearerAuthorizationHeader();
            }
            else
            {
                Diagnostics.Assert(false, String.Format("Unrecognized Authentication value: {0}", Authentication));
            }
        }

        #endregion Helper Methods
    }
}
