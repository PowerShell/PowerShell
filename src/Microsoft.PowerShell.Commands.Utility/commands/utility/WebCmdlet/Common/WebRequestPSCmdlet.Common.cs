// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Xml;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

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

    // WebSslProtocol is used because not all SslProtocols are supported by HttpClientHandler.
    // Also SslProtocols.Default is not the "default" for HttpClientHandler as SslProtocols.Ssl3 is not supported.
    /// <summary>
    /// The valid values for the -SslProtocol parameter for Invoke-RestMethod and Invoke-WebRequest
    /// </summary>
    [Flags]
    public enum WebSslProtocol
    {
        /// <summary>
        ///  No SSL protocol will be set and the system defaults will be used.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Specifies the TLS 1.0 security protocol. The TLS protocol is defined in IETF RFC 2246.
        /// </summary>
        Tls = SslProtocols.Tls,

        /// <summary>
        ///  Specifies the TLS 1.1 security protocol. The TLS protocol is defined in IETF RFC 4346.
        /// </summary>
        Tls11 = SslProtocols.Tls11,

        /// <summary>
        /// Specifies the TLS 1.2 security protocol. The TLS protocol is defined in IETF RFC 5246
        /// </summary>
        Tls12 = SslProtocols.Tls12
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
        /// Gets or sets the TLS/SSL protocol used by the Web Cmdlet
        /// </summary>
        [Parameter]
        public virtual WebSslProtocol SslProtocol { get; set; } = WebSslProtocol.Default;

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
        /// Dictionary for use with RFC-7578 multipart/form-data submissions. 
        /// Keys are form fields and their respective values are form values.
        /// A value may be a collection of form values or single form value.
        /// </summary>
        [Parameter]
        public virtual IDictionary Form {get; set;}

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
            if ((null != Body) && (null != Form))
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.BodyFormConflict,
                                                       "WebCmdletBodyFormConflictException");
                ThrowTerminatingError(error);
            }
            if ((null != InFile) && (null != Form))
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.FormInFileConflict,
                                                       "WebCmdletFormInFileConflictException");
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

    // TODO: Merge Partials

    /// <summary>
    /// Exception class for webcmdlets to enable returning HTTP error response
    /// </summary>
    public sealed class HttpResponseException : HttpRequestException
    {
        /// <summary>
        /// Constructor for HttpResponseException
        /// </summary>
        /// <param name="message">Message for the exception</param>
        /// <param name="response">Response from the HTTP server</param>
        public HttpResponseException (string message, HttpResponseMessage response) : base(message)
        {
            Response = response;
        }

        /// <summary>
        /// HTTP error response
        /// </summary>
        public HttpResponseMessage Response { get; private set; }
    }

    /// <summary>
    /// Base class for Invoke-RestMethod and Invoke-WebRequest commands.
    /// </summary>
    public abstract partial class WebRequestPSCmdlet : PSCmdlet
    {

        /// <summary>
        /// gets or sets the PreserveAuthorizationOnRedirect property
        /// </summary>
        /// <remarks>
        /// This property overrides compatibility with web requests on Windows.
        /// On FullCLR (WebRequest), authorization headers are stripped during redirect.
        /// CoreCLR (HTTPClient) does not have this behavior so web requests that work on
        /// PowerShell/FullCLR can fail with PowerShell/CoreCLR.  To provide compatibility,
        /// we'll detect requests with an Authorization header and automatically strip
        /// the header when the first redirect occurs. This switch turns off this logic for
        /// edge cases where the authorization header needs to be preserved across redirects.
        /// </remarks>
        [Parameter]
        public virtual SwitchParameter PreserveAuthorizationOnRedirect { get; set; }

        /// <summary>
        /// gets or sets the SkipHeaderValidation property
        /// </summary>
        /// <remarks>
        /// This property adds headers to the request's header collection without validation.
        /// </remarks>
        [Parameter]
        public virtual SwitchParameter SkipHeaderValidation { get; set; }

        #region Abstract Methods

        /// <summary>
        /// Read the supplied WebResponse object and push the
        /// resulting output into the pipeline.
        /// </summary>
        /// <param name="response">Instance of a WebResponse object to be processed</param>
        internal abstract void ProcessResponse(HttpResponseMessage response);

        #endregion Abstract Methods

        /// <summary>
        /// Cancellation token source
        /// </summary>
        private CancellationTokenSource _cancelToken = null;

        /// <summary>
        /// Parse Rel Links
        /// </summary>
        internal bool _parseRelLink = false;

        /// <summary>
        /// Automatically follow Rel Links
        /// </summary>
        internal bool _followRelLink = false;

        /// <summary>
        /// Automatically follow Rel Links
        /// </summary>
        internal Dictionary<string, string> _relationLink = null;

        /// <summary>
        /// Maximum number of Rel Links to follow
        /// </summary>
        internal int _maximumFollowRelLink = Int32.MaxValue;

        private HttpMethod GetHttpMethod(WebRequestMethod method)
        {
            switch (Method)
            {
                case WebRequestMethod.Default:
                case WebRequestMethod.Get:
                    return HttpMethod.Get;
                case WebRequestMethod.Head:
                    return HttpMethod.Head;
                case WebRequestMethod.Post:
                    return HttpMethod.Post;
                case WebRequestMethod.Put:
                    return HttpMethod.Put;
                case WebRequestMethod.Delete:
                    return HttpMethod.Delete;
                case WebRequestMethod.Trace:
                    return HttpMethod.Trace;
                case WebRequestMethod.Options:
                    return HttpMethod.Options;
                default:
                    // Merge and Patch
                    return new HttpMethod(Method.ToString().ToUpperInvariant());
            }
        }

        #region Virtual Methods

        // NOTE: Only pass true for handleRedirect if the original request has an authorization header
        // and PreserveAuthorizationOnRedirect is NOT set.
        internal virtual HttpClient GetHttpClient(bool handleRedirect)
        {
            // By default the HttpClientHandler will automatically decompress GZip and Deflate content
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = WebSession.Cookies;

            // set the credentials used by this request
            if (WebSession.UseDefaultCredentials)
            {
                // the UseDefaultCredentials flag overrides other supplied credentials
                handler.UseDefaultCredentials = true;
            }
            else if (WebSession.Credentials != null)
            {
                handler.Credentials = WebSession.Credentials;
            }

            if (NoProxy)
            {
                handler.UseProxy = false;
            }
            else if (WebSession.Proxy != null)
            {
                handler.Proxy = WebSession.Proxy;
            }

            if (null != WebSession.Certificates)
            {
                handler.ClientCertificates.AddRange(WebSession.Certificates);
            }

            if (SkipCertificateCheck)
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            }

            // This indicates GetResponse will handle redirects.
            if (handleRedirect)
            {
                handler.AllowAutoRedirect = false;
            }
            else if (WebSession.MaximumRedirection > -1)
            {
                if (WebSession.MaximumRedirection == 0)
                {
                    handler.AllowAutoRedirect = false;
                }
                else
                {
                    handler.MaxAutomaticRedirections = WebSession.MaximumRedirection;
                }
            }

            handler.SslProtocols = (SslProtocols)SslProtocol;

            HttpClient httpClient = new HttpClient(handler);

            // check timeout setting (in seconds instead of milliseconds as in HttpWebRequest)
            if (TimeoutSec == 0)
            {
                // A zero timeout means infinite
                httpClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
            }
            else if (TimeoutSec > 0)
            {
                httpClient.Timeout = new TimeSpan(0, 0, TimeoutSec);
            }

            return httpClient;
        }

        internal virtual HttpRequestMessage GetRequest(Uri uri, bool stripAuthorization)
        {
            Uri requestUri = PrepareUri(uri);
            HttpMethod httpMethod = null;

            switch (ParameterSetName)
            {
                case "StandardMethodNoProxy":
                    goto case "StandardMethod";
                case "StandardMethod":
                    // set the method if the parameter was provided
                    httpMethod = GetHttpMethod(Method);
                    break;
                case "CustomMethodNoProxy":
                    goto case "CustomMethod";
                case "CustomMethod":
                    if (!string.IsNullOrEmpty(CustomMethod))
                    {
                        // set the method if the parameter was provided
                        httpMethod = new HttpMethod(CustomMethod.ToString().ToUpperInvariant());
                    }
                    break;
            }

            // create the base WebRequest object
            var request = new HttpRequestMessage(httpMethod, requestUri);

            // pull in session data
            if (WebSession.Headers.Count > 0)
            {
                WebSession.ContentHeaders.Clear();
                foreach (var entry in WebSession.Headers)
                {
                    if (HttpKnownHeaderNames.ContentHeaders.Contains(entry.Key))
                    {
                        WebSession.ContentHeaders.Add(entry.Key, entry.Value);
                    }
                    else
                    {
                        if (stripAuthorization
                            &&
                            String.Equals(entry.Key, HttpKnownHeaderNames.Authorization.ToString(), StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            continue;
                        }

                        if (SkipHeaderValidation)
                        {
                            request.Headers.TryAddWithoutValidation(entry.Key, entry.Value);
                        }
                        else
                        {
                            request.Headers.Add(entry.Key, entry.Value);
                        }
                    }
                }
            }

            // Set 'Transfer-Encoding: chunked' if 'Transfer-Encoding' is specified
            if (WebSession.Headers.ContainsKey(HttpKnownHeaderNames.TransferEncoding))
            {
                request.Headers.TransferEncodingChunked = true;
            }

            // Set 'User-Agent' if WebSession.Headers doesn't already contain it
            string userAgent = null;
            if (WebSession.Headers.TryGetValue(HttpKnownHeaderNames.UserAgent, out userAgent))
            {
                WebSession.UserAgent = userAgent;
            }
            else
            {
                if (SkipHeaderValidation)
                {
                    request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.UserAgent, WebSession.UserAgent);
                }
                else
                {
                    request.Headers.Add(HttpKnownHeaderNames.UserAgent, WebSession.UserAgent);
                }

            }

            // Set 'Keep-Alive' to false. This means set the Connection to 'Close'.
            if (DisableKeepAlive)
            {
                request.Headers.Add(HttpKnownHeaderNames.Connection, "Close");
            }

            // Set 'Transfer-Encoding'
            if (TransferEncoding != null)
            {
                request.Headers.TransferEncodingChunked = true;
                var headerValue = new TransferCodingHeaderValue(TransferEncoding);
                if (!request.Headers.TransferEncoding.Contains(headerValue))
                {
                    request.Headers.TransferEncoding.Add(headerValue);
                }
            }

            // Some web sites (e.g. Twitter) will return exception on POST when Expect100 is sent
            // Default behavior is continue to send body content anyway after a short period
            // Here it send the two part as a whole.
            request.Headers.ExpectContinue = false;

            return (request);
        }

        internal virtual void FillRequestStream(HttpRequestMessage request)
        {
            if (null == request) { throw new ArgumentNullException("request"); }

            // set the content type
            if (ContentType != null)
            {
                WebSession.ContentHeaders[HttpKnownHeaderNames.ContentType] = ContentType;
                //request
            }
            // ContentType == null
            else if (Method == WebRequestMethod.Post || (IsCustomMethodSet() && CustomMethod.ToUpperInvariant() == "POST"))
            {
                // Win8:545310 Invoke-WebRequest does not properly set MIME type for POST
                string contentType = null;
                WebSession.ContentHeaders.TryGetValue(HttpKnownHeaderNames.ContentType, out contentType);
                if (string.IsNullOrEmpty(contentType))
                {
                    WebSession.ContentHeaders[HttpKnownHeaderNames.ContentType] = "application/x-www-form-urlencoded";
                }
            }

            if (null != Form)
            {
                // Content headers will be set by MultipartFormDataContent which will throw unless we clear them first
                WebSession.ContentHeaders.Clear();

                var formData = new MultipartFormDataContent();
                foreach (DictionaryEntry formEntry in Form)
                {
                    // AddMultipartContent will handle PSObject unwrapping, Object type determination and enumerateing top level IEnumerables.
                    AddMultipartContent(fieldName: formEntry.Key, fieldValue: formEntry.Value, formData: formData, enumerate: true);
                }
                SetRequestContent(request, formData);
            }
            // coerce body into a usable form
            else if (Body != null)
            {
                object content = Body;

                // make sure we're using the base object of the body, not the PSObject wrapper
                PSObject psBody = Body as PSObject;
                if (psBody != null)
                {
                    content = psBody.BaseObject;
                }

                if (content is FormObject)
                {
                    FormObject form = content as FormObject;
                    SetRequestContent(request, form.Fields);
                }
                else if (content is IDictionary && request.Method != HttpMethod.Get)
                {
                    IDictionary dictionary = content as IDictionary;
                    SetRequestContent(request, dictionary);
                }
                else if (content is XmlNode)
                {
                    XmlNode xmlNode = content as XmlNode;
                    SetRequestContent(request, xmlNode);
                }
                else if (content is Stream)
                {
                    Stream stream = content as Stream;
                    SetRequestContent(request, stream);
                }
                else if (content is byte[])
                {
                    byte[] bytes = content as byte[];
                    SetRequestContent(request, bytes);
                }
                else if (content is MultipartFormDataContent multipartFormDataContent)
                {
                    WebSession.ContentHeaders.Clear();
                    SetRequestContent(request, multipartFormDataContent);
                }
                else
                {
                    SetRequestContent(request,
                        (string)LanguagePrimitives.ConvertTo(content, typeof(string), CultureInfo.InvariantCulture));
                }
            }
            else if (InFile != null) // copy InFile data
            {
                try
                {
                    // open the input file
                    SetRequestContent(request, new FileStream(InFile, FileMode.Open));
                }
                catch (UnauthorizedAccessException)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture, WebCmdletStrings.AccessDenied,
                                               _originalFilePath);
                    throw new UnauthorizedAccessException(msg);
                }
            }

            // Add the content headers
            if (request.Content != null)
            {
                foreach (var entry in WebSession.ContentHeaders)
                {
                    if (SkipHeaderValidation)
                    {
                        request.Content.Headers.TryAddWithoutValidation(entry.Key, entry.Value);
                    }
                    else
                    {
                        try
                        {
                            request.Content.Headers.Add(entry.Key, entry.Value);
                        }
                        catch (FormatException ex)
                        {
                            var outerEx = new ValidationMetadataException(WebCmdletStrings.ContentTypeException, ex);
                            ErrorRecord er = new ErrorRecord(outerEx, "WebCmdletContentTypeException", ErrorCategory.InvalidArgument, ContentType);
                            ThrowTerminatingError(er);
                        }
                    }
                }
            }
        }

        // Returns true if the status code is one of the supported redirection codes.
        static bool IsRedirectCode(HttpStatusCode code)
        {
            int intCode = (int) code;
            return
            (
                (intCode >= 300 && intCode < 304)
                ||
                intCode == 307
            );
        }

        // Returns true if the status code is a redirection code and the action requires switching from POST to GET on redirection.
        // NOTE: Some of these status codes map to the same underlying value but spelling them out for completeness.
        static bool IsRedirectToGet(HttpStatusCode code)
        {
            return
            (
                code == HttpStatusCode.Found
                ||
                code == HttpStatusCode.Moved
                ||
                code == HttpStatusCode.Redirect
                ||
                code == HttpStatusCode.RedirectMethod
                ||
                code == HttpStatusCode.TemporaryRedirect
                ||
                code == HttpStatusCode.RedirectKeepVerb
                ||
                code == HttpStatusCode.SeeOther
            );
        }

        internal virtual HttpResponseMessage GetResponse(HttpClient client, HttpRequestMessage request, bool stripAuthorization)
        {
            if (client == null) { throw new ArgumentNullException("client"); }
            if (request == null) { throw new ArgumentNullException("request"); }

            _cancelToken = new CancellationTokenSource();
            HttpResponseMessage response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cancelToken.Token).GetAwaiter().GetResult();

            if (stripAuthorization && IsRedirectCode(response.StatusCode))
            {
                _cancelToken.Cancel();
                _cancelToken = null;

                // if explicit count was provided, reduce it for this redirection.
                if (WebSession.MaximumRedirection > 0)
                {
                    WebSession.MaximumRedirection--;
                }
                // For selected redirects that used POST, GET must be used with the
                // redirected Location.
                // Since GET is the default; POST only occurs when -Method POST is used.
                if (Method == WebRequestMethod.Post && IsRedirectToGet(response.StatusCode))
                {
                    // See https://msdn.microsoft.com/en-us/library/system.net.httpstatuscode(v=vs.110).aspx
                    Method = WebRequestMethod.Get;
                }

                // recreate the HttpClient with redirection enabled since the first call suppressed redirection
                using (client = GetHttpClient(false))
                using (HttpRequestMessage redirectRequest = GetRequest(response.Headers.Location, stripAuthorization:true))
                {
                    FillRequestStream(redirectRequest);
                    _cancelToken = new CancellationTokenSource();
                    response = client.SendAsync(redirectRequest, HttpCompletionOption.ResponseHeadersRead, _cancelToken.Token).GetAwaiter().GetResult();
                }
            }
            return response;
        }

        internal virtual void UpdateSession(HttpResponseMessage response)
        {
            if (response == null) { throw new ArgumentNullException("response"); }
        }

        #endregion Virtual Methods

        #region Overrides

        /// <summary>
        /// the main execution method for cmdlets derived from WebRequestPSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                // Set cmdlet context for write progress
                ValidateParameters();
                PrepareSession();

                // if the request contains an authorization header and PreserveAuthorizationOnRedirect is not set,
                // it needs to be stripped on the first redirect.
                bool stripAuthorization = null != WebSession
                                          &&
                                          null != WebSession.Headers
                                          &&
                                          !PreserveAuthorizationOnRedirect.IsPresent
                                          &&
                                          WebSession.Headers.ContainsKey(HttpKnownHeaderNames.Authorization.ToString());

                using (HttpClient client = GetHttpClient(stripAuthorization))
                {
                    int followedRelLink = 0;
                    Uri uri = Uri;
                    do
                    {
                        if (followedRelLink > 0)
                        {
                            string linkVerboseMsg = string.Format(CultureInfo.CurrentCulture,
                                WebCmdletStrings.FollowingRelLinkVerboseMsg,
                                uri.AbsoluteUri);
                            WriteVerbose(linkVerboseMsg);
                        }

                        using (HttpRequestMessage request = GetRequest(uri, stripAuthorization:false))
                        {
                            FillRequestStream(request);
                            try
                            {
                                long requestContentLength = 0;
                                if (request.Content != null)
                                    requestContentLength = request.Content.Headers.ContentLength.Value;

                                string reqVerboseMsg = String.Format(CultureInfo.CurrentCulture,
                                    WebCmdletStrings.WebMethodInvocationVerboseMsg,
                                    request.Method,
                                    request.RequestUri,
                                    requestContentLength);
                                WriteVerbose(reqVerboseMsg);

                                HttpResponseMessage response = GetResponse(client, request, stripAuthorization);

                                string contentType = ContentHelper.GetContentType(response);
                                string respVerboseMsg = string.Format(CultureInfo.CurrentCulture,
                                    WebCmdletStrings.WebResponseVerboseMsg,
                                    response.Content.Headers.ContentLength,
                                    contentType);
                                WriteVerbose(respVerboseMsg);

                                if (!response.IsSuccessStatusCode)
                                {
                                    string message = String.Format(CultureInfo.CurrentCulture, WebCmdletStrings.ResponseStatusCodeFailure,
                                        (int)response.StatusCode, response.ReasonPhrase);
                                    HttpResponseException httpEx = new HttpResponseException(message, response);
                                    ErrorRecord er = new ErrorRecord(httpEx, "WebCmdletWebResponseException", ErrorCategory.InvalidOperation, request);
                                    string detailMsg = "";
                                    StreamReader reader = null;
                                    try
                                    {
                                        reader = new StreamReader(StreamHelper.GetResponseStream(response));
                                        // remove HTML tags making it easier to read
                                        detailMsg = System.Text.RegularExpressions.Regex.Replace(reader.ReadToEnd(), "<[^>]*>","");
                                    }
                                    catch (Exception)
                                    {
                                        // catch all
                                    }
                                    finally
                                    {
                                        if (reader != null)
                                        {
                                            reader.Dispose();
                                        }
                                    }
                                    if (!String.IsNullOrEmpty(detailMsg))
                                    {
                                        er.ErrorDetails = new ErrorDetails(detailMsg);
                                    }
                                    ThrowTerminatingError(er);
                                }

                                if (_parseRelLink || _followRelLink)
                                {
                                    ParseLinkHeader(response, uri);
                                }
                                ProcessResponse(response);
                                UpdateSession(response);

                                // If we hit our maximum redirection count, generate an error.
                                // Errors with redirection counts of greater than 0 are handled automatically by .NET, but are
                                // impossible to detect programmatically when we hit this limit. By handling this ourselves
                                // (and still writing out the result), users can debug actual HTTP redirect problems.
                                if (WebSession.MaximumRedirection == 0) // Indicate "HttpClientHandler.AllowAutoRedirect == false"
                                {
                                    if (response.StatusCode == HttpStatusCode.Found ||
                                        response.StatusCode == HttpStatusCode.Moved ||
                                        response.StatusCode == HttpStatusCode.MovedPermanently)
                                    {
                                        ErrorRecord er = new ErrorRecord(new InvalidOperationException(), "MaximumRedirectExceeded", ErrorCategory.InvalidOperation, request);
                                        er.ErrorDetails = new ErrorDetails(WebCmdletStrings.MaximumRedirectionCountExceeded);
                                        WriteError(er);
                                    }
                                }
                            }
                            catch (HttpRequestException ex)
                            {
                                ErrorRecord er = new ErrorRecord(ex, "WebCmdletWebResponseException", ErrorCategory.InvalidOperation, request);
                                if (ex.InnerException != null)
                                {
                                    er.ErrorDetails = new ErrorDetails(ex.InnerException.Message);
                                }
                                ThrowTerminatingError(er);
                            }

                            if (_followRelLink)
                            {
                                if (!_relationLink.ContainsKey("next"))
                                {
                                    return;
                                }
                                uri = new Uri(_relationLink["next"]);
                                followedRelLink++;
                            }
                        }
                    }
                    while (_followRelLink && (followedRelLink < _maximumFollowRelLink));
                }
            }
            catch (CryptographicException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "WebCmdletCertificateException", ErrorCategory.SecurityError, null);
                ThrowTerminatingError(er);
            }
            catch (NotSupportedException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "WebCmdletIEDomNotSupportedException", ErrorCategory.NotImplemented, null);
                ThrowTerminatingError(er);
            }
        }

        /// <summary>
        /// Implementing ^C, after start the BeginGetResponse
        /// </summary>
        protected override void StopProcessing()
        {
            if (_cancelToken != null)
            {
                _cancelToken.Cancel();
            }
        }

        #endregion Overrides

        #region Helper Methods

        /// <summary>
        /// Sets the ContentLength property of the request and writes the specified content to the request's RequestStream.
        /// </summary>
        /// <param name="request">The WebRequest who's content is to be set</param>
        /// <param name="content">A byte array containing the content data.</param>
        /// <returns>The number of bytes written to the requests RequestStream (and the new value of the request's ContentLength property</returns>
        /// <remarks>
        /// Because this function sets the request's ContentLength property and writes content data into the requests's stream,
        /// it should be called one time maximum on a given request.
        /// </remarks>
        internal long SetRequestContent(HttpRequestMessage request, Byte[] content)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (content == null)
                return 0;

            var byteArrayContent = new ByteArrayContent(content);
            request.Content = byteArrayContent;

            return byteArrayContent.Headers.ContentLength.Value;
        }

        /// <summary>
        /// Sets the ContentLength property of the request and writes the specified content to the request's RequestStream.
        /// </summary>
        /// <param name="request">The WebRequest who's content is to be set</param>
        /// <param name="content">A String object containing the content data.</param>
        /// <returns>The number of bytes written to the requests RequestStream (and the new value of the request's ContentLength property</returns>
        /// <remarks>
        /// Because this function sets the request's ContentLength property and writes content data into the requests's stream,
        /// it should be called one time maximum on a given request.
        /// </remarks>
        internal long SetRequestContent(HttpRequestMessage request, string content)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (content == null)
                return 0;

            Encoding encoding = null;
            if (ContentType != null)
            {
                // If Content-Type contains the encoding format (as CharSet), use this encoding format
                // to encode the Body of the WebRequest sent to the server. Default Encoding format
                // would be used if Charset is not supplied in the Content-Type property.
                try
                {
                    var mediaTypeHeaderValue = new MediaTypeHeaderValue(ContentType);
                    if (!string.IsNullOrEmpty(mediaTypeHeaderValue.CharSet))
                    {
                        encoding = Encoding.GetEncoding(mediaTypeHeaderValue.CharSet);
                    }
                }
                catch (FormatException ex)
                {
                    if (!SkipHeaderValidation)
                    {
                        var outerEx = new ValidationMetadataException(WebCmdletStrings.ContentTypeException, ex);
                        ErrorRecord er = new ErrorRecord(outerEx, "WebCmdletContentTypeException", ErrorCategory.InvalidArgument, ContentType);
                        ThrowTerminatingError(er);
                    }
                }
                catch (ArgumentException ex)
                {
                    if (!SkipHeaderValidation)
                    {
                        var outerEx = new ValidationMetadataException(WebCmdletStrings.ContentTypeException, ex);
                        ErrorRecord er = new ErrorRecord(outerEx, "WebCmdletContentTypeException", ErrorCategory.InvalidArgument, ContentType);
                        ThrowTerminatingError(er);
                    }
                }
            }

            Byte[] bytes = StreamHelper.EncodeToBytes(content, encoding);
            var byteArrayContent = new ByteArrayContent(bytes);
            request.Content = byteArrayContent;

            return byteArrayContent.Headers.ContentLength.Value;
        }

        internal long SetRequestContent(HttpRequestMessage request, XmlNode xmlNode)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (xmlNode == null)
                return 0;

            Byte[] bytes = null;
            XmlDocument doc = xmlNode as XmlDocument;
            if (doc != null && (doc.FirstChild as XmlDeclaration) != null)
            {
                XmlDeclaration decl = doc.FirstChild as XmlDeclaration;
                Encoding encoding = Encoding.GetEncoding(decl.Encoding);
                bytes = StreamHelper.EncodeToBytes(doc.OuterXml, encoding);
            }
            else
            {
                bytes = StreamHelper.EncodeToBytes(xmlNode.OuterXml);
            }

            var byteArrayContent = new ByteArrayContent(bytes);
            request.Content = byteArrayContent;

            return byteArrayContent.Headers.ContentLength.Value;
        }

        /// <summary>
        /// Sets the ContentLength property of the request and writes the specified content to the request's RequestStream.
        /// </summary>
        /// <param name="request">The WebRequest who's content is to be set</param>
        /// <param name="contentStream">A Stream object containing the content data.</param>
        /// <returns>The number of bytes written to the requests RequestStream (and the new value of the request's ContentLength property</returns>
        /// <remarks>
        /// Because this function sets the request's ContentLength property and writes content data into the requests's stream,
        /// it should be called one time maximum on a given request.
        /// </remarks>
        internal long SetRequestContent(HttpRequestMessage request, Stream contentStream)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (contentStream == null)
                throw new ArgumentNullException("contentStream");

            var streamContent = new StreamContent(contentStream);
            request.Content = streamContent;

            return streamContent.Headers.ContentLength.Value;
        }

        /// <summary>
        /// Sets the ContentLength property of the request and writes the specified content to the request's RequestStream.
        /// </summary>
        /// <param name="request">The WebRequest who's content is to be set</param>
        /// <param name="multipartContent">A MultipartFormDataContent object containing multipart/form-data content.</param>
        /// <returns>The number of bytes written to the requests RequestStream (and the new value of the request's ContentLength property</returns>
        /// <remarks>
        /// Because this function sets the request's ContentLength property and writes content data into the requests's stream,
        /// it should be called one time maximum on a given request.
        /// </remarks>
        internal long SetRequestContent(HttpRequestMessage request, MultipartFormDataContent multipartContent)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            if (multipartContent == null)
            {
                throw new ArgumentNullException("multipartContent");
            }

            request.Content = multipartContent;

            return multipartContent.Headers.ContentLength.Value;
        }

        internal long SetRequestContent(HttpRequestMessage request, IDictionary content)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (content == null)
                throw new ArgumentNullException("content");

            string body = FormatDictionary(content);
            return (SetRequestContent(request, body));

        }

        internal void ParseLinkHeader(HttpResponseMessage response, System.Uri requestUri)
        {
            if (_relationLink == null)
            {
                // Must ignore the case of relation links. See RFC 8288 (https://tools.ietf.org/html/rfc8288)
                _relationLink = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                _relationLink.Clear();
            }

            // we only support the URL in angle brackets and `rel`, other attributes are ignored
            // user can still parse it themselves via the Headers property
            string pattern = "<(?<url>.*?)>;\\srel=\"(?<rel>.*?)\"";
            IEnumerable<string> links;
            if (response.Headers.TryGetValues("Link", out links))
            {
                foreach (string linkHeader in links)
                {
                    foreach (string link in linkHeader.Split(","))
                    {
                        Match match = Regex.Match(link, pattern);
                        if (match.Success)
                        {
                            string url = match.Groups["url"].Value;
                            string rel = match.Groups["rel"].Value;
                            if (url != String.Empty && rel != String.Empty && !_relationLink.ContainsKey(rel))
                            {
                                Uri absoluteUri = new Uri(requestUri, url);
                                _relationLink.Add(rel, absoluteUri.AbsoluteUri.ToString());
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds content to a <see cref="MultipartFormDataContent" />. Object type detection is used to determine if the value is String, File, or Collection.
        /// </summary>
        /// <param name="fieldName">The Field Name to use.</param>
        /// <param name="fieldValue">The Field Value to use.</param>
        /// <param name="formData">The <see cref="MultipartFormDataContent" />> to update.</param>
        /// <param name="enumerate">If true, collection types in <paramref name="fieldValue" /> will be enumerated. If false, collections will be treated as single value.</param>
        private void AddMultipartContent(object fieldName, object fieldValue, MultipartFormDataContent formData, bool enumerate)
        {
            if (null == formData)
            {
                throw new ArgumentNullException("formDate");
            }

            // It is possible that the dictionary keys or values are PSObject wrapped depending on how the dictionary is defined and assigned.
            // Before processing the field name and value we need to ensure we are working with the base objects and not the PSObject wrappers.

            // Unwrap fieldName PSObjects
            if (fieldName is PSObject namePSObject)
            {
                fieldName = namePSObject.BaseObject;
            }

            // Unwrap fieldValue PSObjects
            if (fieldValue is PSObject valuePSObject)
            {
                fieldValue = valuePSObject.BaseObject;
            }

            // Treat a single FileInfo as a FileContent
            if (fieldValue is FileInfo file)
            {
                formData.Add(GetMultipartFileContent(fieldName: fieldName, file: file));
                return;
            }

            // Treat Strings and other single values as a StringContent. 
            // If enumeration is false, also treat IEnumerables as StringContents.
            // String implements IEnumerable so the explicit check is required.
            if (enumerate == false || fieldValue is String || !(fieldValue is IEnumerable))
            {
                formData.Add(GetMultipartStringContent(fieldName: fieldName, fieldValue: fieldValue));
                return;
            }

            // Treat the value as a collection and enumerate it if enumeration is true
            if (enumerate == true && fieldValue is IEnumerable items)
            {
                foreach (var item in items)
                {
                    // Recruse, but do not enumerate the next level. IEnumerables will be treated as single values.
                    AddMultipartContent(fieldName: fieldName, fieldValue: item, formData: formData, enumerate: false);
                }
            }
        }

        /// <summary>
        /// Gets a <see cref="StringContent" /> from the supplied field name and field value. Uses <see cref="ConvertTo<T>(Object)" /> to convert the objects to strings.
        /// </summary>
        /// <param name="fieldName">The Field Name to use for the <see cref="StringContent" />.</param>
        /// <param name="fieldValue">The Field Value to use for the <see cref="StringContent" />.</param>
        private StringContent GetMultipartStringContent(Object fieldName, Object fieldValue)
        {
            var contentDisposition = new ContentDispositionHeaderValue("form-data");
            contentDisposition.Name = LanguagePrimitives.ConvertTo<String>(fieldName);

            var result = new StringContent(LanguagePrimitives.ConvertTo<String>(fieldValue));
            result.Headers.ContentDisposition = contentDisposition;

            return result;
        }

        /// <summary>
        /// Gets a <see cref="StreamContent" /> from the supplied field name and <see cref="Stream" />. Uses <see cref="ConvertTo<T>(Object)" /> to convert the fieldname to a string.
        /// </summary>
        /// <param name="fieldName">The Field Name to use for the <see cref="StreamContent" />.</param>
        /// <param name="stream">The <see cref="Stream" /> to use for the <see cref="StreamContent" />.</param>
        private StreamContent GetMultipartStreamContent(Object fieldName, Stream stream)
        {
            var contentDisposition = new ContentDispositionHeaderValue("form-data");
            contentDisposition.Name = LanguagePrimitives.ConvertTo<String>(fieldName);

            var result = new StreamContent(stream);
            result.Headers.ContentDisposition = contentDisposition;
            result.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            return result;
        }

        /// <summary>
        /// Gets a <see cref="StreamContent" /> from the supplied field name and file. Calls <see cref="GetMultipartStreamContent(Object, Stream)" /> to create the <see cref="StreamContent" /> and then sets the file name.
        /// </summary>
        /// <param name="fieldName">The Field Name to use for the <see cref="StreamContent" />.</param>
        /// <param name="file">The file to use for the <see cref="StreamContent" />.</param>
        private StreamContent GetMultipartFileContent(Object fieldName, FileInfo file)
        {
            var result = GetMultipartStreamContent(fieldName: fieldName, stream: new FileStream(file.FullName, FileMode.Open));
            result.Headers.ContentDisposition.FileName = file.Name;

            return result;
        }

        #endregion Helper Methods
    }
}
