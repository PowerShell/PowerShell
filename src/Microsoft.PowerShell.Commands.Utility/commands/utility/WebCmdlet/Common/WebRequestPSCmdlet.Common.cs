// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The valid values for the -Authentication parameter for Invoke-RestMethod and Invoke-WebRequest.
    /// </summary>
    public enum WebAuthenticationType
    {
        /// <summary>
        /// No authentication. Default.
        /// </summary>
        None,

        /// <summary>
        /// RFC-7617 Basic Authentication. Requires -Credential.
        /// </summary>
        Basic,

        /// <summary>
        /// RFC-6750 OAuth 2.0 Bearer Authentication. Requires -Token.
        /// </summary>
        Bearer,

        /// <summary>
        /// RFC-6750 OAuth 2.0 Bearer Authentication. Requires -Token.
        /// </summary>
        OAuth,
    }

    // WebSslProtocol is used because not all SslProtocols are supported by HttpClientHandler.
    // Also SslProtocols.Default is not the "default" for HttpClientHandler as SslProtocols.Ssl3 is not supported.
    /// <summary>
    /// The valid values for the -SslProtocol parameter for Invoke-RestMethod and Invoke-WebRequest.
    /// </summary>
    [Flags]
    public enum WebSslProtocol
    {
        /// <summary>
        /// No SSL protocol will be set and the system defaults will be used.
        /// </summary>
        Default = SslProtocols.None,

        /// <summary>
        /// Specifies the TLS 1.0 is obsolete. Using this value now defaults to TLS 1.2.
        /// </summary>
        Tls = SslProtocols.Tls12,

        /// <summary>
        /// Specifies the TLS 1.1 is obsolete. Using this value now defaults to TLS 1.2.
        /// </summary>
        Tls11 = SslProtocols.Tls12,

        /// <summary>
        /// Specifies the TLS 1.2 security protocol. The TLS protocol is defined in IETF RFC 5246.
        /// </summary>
        Tls12 = SslProtocols.Tls12,

        /// <summary>
        /// Specifies the TLS 1.3 security protocol. The TLS protocol is defined in IETF RFC 8446.
        /// </summary>
        Tls13 = SslProtocols.Tls13
    }

    /// <summary>
    /// Base class for Invoke-RestMethod and Invoke-WebRequest commands.
    /// </summary>
    public abstract class WebRequestPSCmdlet : PSCmdlet
    {
        #region Fields

        /// <summary>
        /// Cancellation token source.
        /// </summary>
        internal CancellationTokenSource? _cancelToken = null;

        /// <summary>
        /// Automatically follow Rel Links.
        /// </summary>
        internal bool _followRelLink = false;

        /// <summary>
        /// Maximum number of Rel Links to follow.
        /// </summary>
        internal int _maximumFollowRelLink = int.MaxValue;

        /// <summary>
        /// Parse Rel Links.
        /// </summary>
        internal bool _parseRelLink = false;

        /// <summary>
        /// Automatically follow Rel Links.
        /// </summary>
        internal Dictionary<string, string>? _relationLink = null;

        /// <summary>
        /// The current size of the local file being resumed.
        /// </summary>
        private long _resumeFileSize = 0;

        /// <summary>
        /// The remote endpoint returned a 206 status code indicating successful resume.
        /// </summary>
        private bool _resumeSuccess = false;

        #endregion Fields

        #region Virtual Properties

        #region URI

        /// <summary>
        /// Deprecated. Gets or sets UseBasicParsing. This has no affect on the operation of the Cmdlet.
        /// </summary>
        [Parameter(DontShow = true)]
        public virtual SwitchParameter UseBasicParsing { get; set; } = true;

        /// <summary>
        /// Gets or sets the Uri property.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public virtual Uri? Uri { get; set; }

        #endregion URI

        #region HTTP Version

        /// <summary>
        /// Gets or sets the HTTP Version property.
        /// </summary>
        [Parameter]
        [ArgumentToVersionTransformation]
        [HttpVersionCompletions]
        public virtual Version? HttpVersion { get; set; }

        #endregion HTTP Version

        #region Session
        /// <summary>
        /// Gets or sets the Session property.
        /// </summary>
        [Parameter]
        public virtual WebRequestSession? WebSession { get; set; }

        /// <summary>
        /// Gets or sets the SessionVariable property.
        /// </summary>
        [Parameter]
        [Alias("SV")]
        public virtual string? SessionVariable { get; set; }

        #endregion Session

        #region Authorization and Credentials

        /// <summary>
        /// Gets or sets the AllowUnencryptedAuthentication property.
        /// </summary>
        [Parameter]
        public virtual SwitchParameter AllowUnencryptedAuthentication { get; set; }

        /// <summary>
        /// Gets or sets the Authentication property used to determine the Authentication method for the web session.
        /// Authentication does not work with UseDefaultCredentials.
        /// Authentication over unencrypted sessions requires AllowUnencryptedAuthentication.
        /// Basic: Requires Credential.
        /// OAuth/Bearer: Requires Token.
        /// </summary>
        [Parameter]
        public virtual WebAuthenticationType Authentication { get; set; } = WebAuthenticationType.None;

        /// <summary>
        /// Gets or sets the Credential property.
        /// </summary>
        [Parameter]
        [Credential]
        public virtual PSCredential? Credential { get; set; }

        /// <summary>
        /// Gets or sets the UseDefaultCredentials property.
        /// </summary>
        [Parameter]
        public virtual SwitchParameter UseDefaultCredentials { get; set; }

        /// <summary>
        /// Gets or sets the CertificateThumbprint property.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public virtual string? CertificateThumbprint { get; set; }

        /// <summary>
        /// Gets or sets the Certificate property.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public virtual X509Certificate? Certificate { get; set; }

        /// <summary>
        /// Gets or sets the SkipCertificateCheck property.
        /// </summary>
        [Parameter]
        public virtual SwitchParameter SkipCertificateCheck { get; set; }

        /// <summary>
        /// Gets or sets the TLS/SSL protocol used by the Web Cmdlet.
        /// </summary>
        [Parameter]
        public virtual WebSslProtocol SslProtocol { get; set; } = WebSslProtocol.Default;

        /// <summary>
        /// Gets or sets the Token property. Token is required by Authentication OAuth and Bearer.
        /// </summary>
        [Parameter]
        public virtual SecureString? Token { get; set; }

        #endregion Authorization and Credentials

        #region Headers

        /// <summary>
        /// Gets or sets the UserAgent property.
        /// </summary>
        [Parameter]
        public virtual string? UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the DisableKeepAlive property.
        /// </summary>
        [Parameter]
        public virtual SwitchParameter DisableKeepAlive { get; set; }

        /// <summary>
        /// Gets or sets the TimeOut property.
        /// </summary>
        [Parameter]
        [ValidateRange(0, int.MaxValue)]
        public virtual int TimeoutSec { get; set; }

        /// <summary>
        /// Gets or sets the Headers property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Parameter]
        public virtual IDictionary? Headers { get; set; }

        /// <summary>
        /// Gets or sets the SkipHeaderValidation property.
        /// </summary>
        /// <remarks>
        /// This property adds headers to the request's header collection without validation.
        /// </remarks>
        [Parameter]
        public virtual SwitchParameter SkipHeaderValidation { get; set; }

        #endregion Headers

        #region Redirect

        /// <summary>
        /// Gets or sets the AllowInsecureRedirect property used to follow HTTP redirects from HTTPS.
        /// </summary>
        [Parameter]
        public virtual SwitchParameter AllowInsecureRedirect { get; set; }

        /// <summary>
        /// Gets or sets the RedirectMax property.
        /// </summary>
        [Parameter]
        [ValidateRange(0, int.MaxValue)]
        public virtual int MaximumRedirection { get; set; } = -1;

        /// <summary>
        /// Gets or sets the MaximumRetryCount property, which determines the number of retries of a failed web request.
        /// </summary>
        [Parameter]
        [ValidateRange(0, int.MaxValue)]
        public virtual int MaximumRetryCount { get; set; }

        /// <summary>
        /// Gets or sets the PreserveAuthorizationOnRedirect property.
        /// </summary>
        /// <remarks>
        /// This property overrides compatibility with web requests on Windows.
        /// On FullCLR (WebRequest), authorization headers are stripped during redirect.
        /// CoreCLR (HTTPClient) does not have this behavior so web requests that work on
        /// PowerShell/FullCLR can fail with PowerShell/CoreCLR. To provide compatibility,
        /// we'll detect requests with an Authorization header and automatically strip
        /// the header when the first redirect occurs. This switch turns off this logic for
        /// edge cases where the authorization header needs to be preserved across redirects.
        /// </remarks>
        [Parameter]
        public virtual SwitchParameter PreserveAuthorizationOnRedirect { get; set; }

        /// <summary>
        /// Gets or sets the RetryIntervalSec property, which determines the number seconds between retries.
        /// </summary>
        [Parameter]
        [ValidateRange(1, int.MaxValue)]
        public virtual int RetryIntervalSec { get; set; } = 5;

        #endregion Redirect

        #region Method

        /// <summary>
        /// Gets or sets the Method property.
        /// </summary>
        [Parameter(ParameterSetName = "StandardMethod")]
        [Parameter(ParameterSetName = "StandardMethodNoProxy")]
        public virtual WebRequestMethod Method { get; set; } = WebRequestMethod.Default;

        /// <summary>
        /// Gets or sets the CustomMethod property.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CustomMethod")]
        [Parameter(Mandatory = true, ParameterSetName = "CustomMethodNoProxy")]
        [Alias("CM")]
        [ValidateNotNullOrEmpty]
        public virtual string? CustomMethod
        {
            get => _custommethod;

            set => _custommethod = value!.ToUpperInvariant();
        }

        private string? _custommethod;

        /// <summary>
        /// Gets or sets the PreserveHttpMethodOnRedirect property.
        /// </summary>
        [Parameter]
        public virtual SwitchParameter PreserveHttpMethodOnRedirect { get; set; }

        #endregion Method

        #region NoProxy

        /// <summary>
        /// Gets or sets the NoProxy property.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CustomMethodNoProxy")]
        [Parameter(Mandatory = true, ParameterSetName = "StandardMethodNoProxy")]
        public virtual SwitchParameter NoProxy { get; set; }

        #endregion NoProxy

        #region Proxy

        /// <summary>
        /// Gets or sets the Proxy property.
        /// </summary>
        [Parameter(ParameterSetName = "StandardMethod")]
        [Parameter(ParameterSetName = "CustomMethod")]
        public virtual Uri? Proxy { get; set; }

        /// <summary>
        /// Gets or sets the ProxyCredential property.
        /// </summary>
        [Parameter(ParameterSetName = "StandardMethod")]
        [Parameter(ParameterSetName = "CustomMethod")]
        [Credential]
        public virtual PSCredential? ProxyCredential { get; set; }

        /// <summary>
        /// Gets or sets the ProxyUseDefaultCredentials property.
        /// </summary>
        [Parameter(ParameterSetName = "StandardMethod")]
        [Parameter(ParameterSetName = "CustomMethod")]
        public virtual SwitchParameter ProxyUseDefaultCredentials { get; set; }

        #endregion Proxy

        #region Input

        /// <summary>
        /// Gets or sets the Body property.
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public virtual object? Body { get; set; }

        /// <summary>
        /// Dictionary for use with RFC-7578 multipart/form-data submissions.
        /// Keys are form fields and their respective values are form values.
        /// A value may be a collection of form values or single form value.
        /// </summary>
        [Parameter]
        public virtual IDictionary? Form { get; set; }

        /// <summary>
        /// Gets or sets the ContentType property.
        /// </summary>
        [Parameter]
        public virtual string? ContentType { get; set; }

        /// <summary>
        /// Gets or sets the TransferEncoding property.
        /// </summary>
        [Parameter]
        [ValidateSet("chunked", "compress", "deflate", "gzip", "identity", IgnoreCase = true)]
        public virtual string? TransferEncoding { get; set; }

        /// <summary>
        /// Gets or sets the InFile property.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public virtual string? InFile { get; set; }

        /// <summary>
        /// Keep the original file path after the resolved provider path is assigned to InFile.
        /// </summary>
        private string? _originalFilePath;

        #endregion Input

        #region Output

        /// <summary>
        /// Gets or sets the OutFile property.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public virtual string? OutFile { get; set; }

        /// <summary>
        /// Gets or sets the PassThrough property.
        /// </summary>
        [Parameter]
        public virtual SwitchParameter PassThru { get; set; }

        /// <summary>
        /// Resumes downloading a partial or incomplete file. OutFile is required.
        /// </summary>
        [Parameter]
        public virtual SwitchParameter Resume { get; set; }

        /// <summary>
        /// Gets or sets whether to skip checking HTTP status for error codes.
        /// </summary>
        [Parameter]
        public virtual SwitchParameter SkipHttpErrorCheck { get; set; }

        #endregion Output

        #endregion Virtual Properties

        #region Helper Properties

        internal string QualifiedOutFile => QualifyFilePath(OutFile);

        internal bool ShouldCheckHttpStatus => !SkipHttpErrorCheck;

        /// <summary>
        /// Determines whether writing to a file should Resume and append rather than overwrite.
        /// </summary>
        internal bool ShouldResume => Resume.IsPresent && _resumeSuccess;

        internal bool ShouldSaveToOutFile => !string.IsNullOrEmpty(OutFile);

        internal bool ShouldWriteToPipeline => !ShouldSaveToOutFile || PassThru;

        #endregion Helper Properties

        #region Abstract Methods

        /// <summary>
        /// Read the supplied WebResponse object and push the resulting output into the pipeline.
        /// </summary>
        /// <param name="response">Instance of a WebResponse object to be processed.</param>
        internal abstract void ProcessResponse(HttpResponseMessage response);

        #endregion Abstract Methods

        #region Overrides

        /// <summary>
        /// The main execution method for cmdlets derived from WebRequestPSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                // Set cmdlet context for write progress
                ValidateParameters();
                PrepareSession();

                // If the request contains an authorization header and PreserveAuthorizationOnRedirect is not set,
                // it needs to be stripped on the first redirect.
                bool keepAuthorizationOnRedirect = PreserveAuthorizationOnRedirect.IsPresent
                                                   && WebSession!.Headers.ContainsKey(HttpKnownHeaderNames.Authorization);

                bool handleRedirect = keepAuthorizationOnRedirect || AllowInsecureRedirect || PreserveHttpMethodOnRedirect;

                using (HttpClient client = GetHttpClient(handleRedirect))
                {
                    int followedRelLink = 0;
                    Uri? uri = Uri;
                    do
                    {
                        if (followedRelLink > 0)
                        {
                            string linkVerboseMsg = string.Format(
                                CultureInfo.CurrentCulture,
                                WebCmdletStrings.FollowingRelLinkVerboseMsg,
                                uri?.AbsoluteUri);

                            WriteVerbose(linkVerboseMsg);
                        }

                        using (HttpRequestMessage request = GetRequest(uri))
                        {
                            FillRequestStream(request);
                            try
                            {
                                long? requestContentLength = request.Content is null ? 0 : request.Content.Headers.ContentLength;

                                string reqVerboseMsg = string.Format(
                                    CultureInfo.CurrentCulture,
                                    WebCmdletStrings.WebMethodInvocationVerboseMsg,
                                    request.Version,
                                    request.Method,
                                    requestContentLength);

                                WriteVerbose(reqVerboseMsg);

                                using HttpResponseMessage response = GetResponse(client, request, handleRedirect);

                                string contentType = ContentHelper.GetContentType(response);
                                string respVerboseMsg = string.Format(
                                    CultureInfo.CurrentCulture,
                                    WebCmdletStrings.WebResponseVerboseMsg,
                                    response.Content.Headers.ContentLength,
                                    contentType);

                                WriteVerbose(respVerboseMsg);

                                bool _isSuccess = response.IsSuccessStatusCode;

                                // Check if the Resume range was not satisfiable because the file already completed downloading.
                                // This happens when the local file is the same size as the remote file.
                                if (Resume.IsPresent
                                    && response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable
                                    && response.Content.Headers.ContentRange?.Length == _resumeFileSize)
                                {
                                    _isSuccess = true;
                                    WriteVerbose(string.Format(
                                        CultureInfo.CurrentCulture,
                                        WebCmdletStrings.OutFileWritingSkipped,
                                        OutFile));

                                    // Disable writing to the OutFile.
                                    OutFile = null;
                                }
                                
                                // Detect insecure redirection
                                if (!AllowInsecureRedirect && response.RequestMessage?.RequestUri?.Scheme == "https" && response.Headers.Location?.Scheme == "http")
                                {
                                        ErrorRecord er = new(new InvalidOperationException(), "InsecureRedirection", ErrorCategory.InvalidOperation, request);
                                        er.ErrorDetails = new ErrorDetails(WebCmdletStrings.InsecureRedirection);
                                        ThrowTerminatingError(er);
                                }
                                
                                if (ShouldCheckHttpStatus && !_isSuccess)
                                {
                                    string message = string.Format(
                                        CultureInfo.CurrentCulture,
                                        WebCmdletStrings.ResponseStatusCodeFailure,
                                        (int)response.StatusCode,
                                        response.ReasonPhrase);

                                    HttpResponseException httpEx = new(message, response);
                                    ErrorRecord er = new(httpEx, "WebCmdletWebResponseException", ErrorCategory.InvalidOperation, request);
                                    string detailMsg = string.Empty;
                                    StreamReader? reader = null;
                                    try
                                    {
                                        reader = new StreamReader(StreamHelper.GetResponseStream(response));
                                        detailMsg = FormatErrorMessage(reader.ReadToEnd(), contentType);
                                    }
                                    catch
                                    {
                                        // Catch all
                                    }
                                    finally
                                    {
                                        reader?.Dispose();
                                    }

                                    if (!string.IsNullOrEmpty(detailMsg))
                                    {
                                        er.ErrorDetails = new ErrorDetails(detailMsg);
                                    }

                                    ThrowTerminatingError(er);
                                }

                                if (_parseRelLink || _followRelLink)
                                {
                                    ParseLinkHeader(response);
                                }

                                ProcessResponse(response);
                                UpdateSession(response);

                                // If we hit our maximum redirection count, generate an error.
                                // Errors with redirection counts of greater than 0 are handled automatically by .NET, but are
                                // impossible to detect programmatically when we hit this limit. By handling this ourselves
                                // (and still writing out the result), users can debug actual HTTP redirect problems.
                                if (WebSession!.MaximumRedirection == 0 && IsRedirectCode(response.StatusCode))
                                {
                                    ErrorRecord er = new(new InvalidOperationException(), "MaximumRedirectExceeded", ErrorCategory.InvalidOperation, request);
                                    er.ErrorDetails = new ErrorDetails(WebCmdletStrings.MaximumRedirectionCountExceeded);
                                    WriteError(er);
                                }
                            }
                            catch (HttpRequestException ex)
                            {
                                ErrorRecord er = new(ex, "WebCmdletWebResponseException", ErrorCategory.InvalidOperation, request);
                                if (ex.InnerException is not null)
                                {
                                    er.ErrorDetails = new ErrorDetails(ex.InnerException.Message);
                                }

                                ThrowTerminatingError(er);
                            }

                            if (_followRelLink)
                            {
                                if (!_relationLink!.ContainsKey("next"))
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
                ErrorRecord er = new(ex, "WebCmdletCertificateException", ErrorCategory.SecurityError, null);
                ThrowTerminatingError(er);
            }
            catch (NotSupportedException ex)
            {
                ErrorRecord er = new(ex, "WebCmdletIEDomNotSupportedException", ErrorCategory.NotImplemented, null);
                ThrowTerminatingError(er);
            }
        }

        /// <summary>
        /// To implement ^C.
        /// </summary>
        protected override void StopProcessing() => _cancelToken?.Cancel();

        #endregion Overrides

        #region Virtual Methods

        internal virtual void ValidateParameters()
        {
            // Sessions
            if (WebSession is not null && SessionVariable is not null)
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.SessionConflict, "WebCmdletSessionConflictException");
                ThrowTerminatingError(error);
            }

            // Authentication
            if (UseDefaultCredentials && Authentication != WebAuthenticationType.None)
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.AuthenticationConflict, "WebCmdletAuthenticationConflictException");
                ThrowTerminatingError(error);
            }

            if (Authentication != WebAuthenticationType.None && Token is not null && Credential is not null)
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.AuthenticationTokenConflict, "WebCmdletAuthenticationTokenConflictException");
                ThrowTerminatingError(error);
            }

            if (Authentication == WebAuthenticationType.Basic && Credential is null)
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.AuthenticationCredentialNotSupplied, "WebCmdletAuthenticationCredentialNotSuppliedException");
                ThrowTerminatingError(error);
            }

            if ((Authentication == WebAuthenticationType.OAuth || Authentication == WebAuthenticationType.Bearer) && Token is null)
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.AuthenticationTokenNotSupplied, "WebCmdletAuthenticationTokenNotSuppliedException");
                ThrowTerminatingError(error);
            }

            if (!AllowUnencryptedAuthentication && (Authentication != WebAuthenticationType.None || Credential is not null || UseDefaultCredentials) && Uri?.Scheme != "https")
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.AllowUnencryptedAuthenticationRequired, "WebCmdletAllowUnencryptedAuthenticationRequiredException");
                ThrowTerminatingError(error);
            }

            // Credentials
            if (UseDefaultCredentials && Credential is not null)
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.CredentialConflict, "WebCmdletCredentialConflictException");
                ThrowTerminatingError(error);
            }

            // Proxy server
            if (ProxyUseDefaultCredentials && ProxyCredential is not null)
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.ProxyCredentialConflict, "WebCmdletProxyCredentialConflictException");
                ThrowTerminatingError(error);
            }
            else if (Proxy is null && (ProxyCredential is not null || ProxyUseDefaultCredentials))
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.ProxyUriNotSupplied, "WebCmdletProxyUriNotSuppliedException");
                ThrowTerminatingError(error);
            }

            // Request body content
            if (Body is not null && InFile is not null)
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.BodyConflict, "WebCmdletBodyConflictException");
                ThrowTerminatingError(error);
            }

            if (Body is not null && Form is not null)
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.BodyFormConflict, "WebCmdletBodyFormConflictException");
                ThrowTerminatingError(error);
            }

            if (InFile is not null && Form is not null)
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.FormInFileConflict, "WebCmdletFormInFileConflictException");
                ThrowTerminatingError(error);
            }

            // Validate InFile path
            if (InFile is not null)
            {
                ErrorRecord? errorRecord = null;

                try
                {
                    Collection<string> providerPaths = GetResolvedProviderPathFromPSPath(InFile, out ProviderInfo provider);

                    if (!provider.Name.Equals(FileSystemProvider.ProviderName, StringComparison.OrdinalIgnoreCase))
                    {
                        errorRecord = GetValidationError(WebCmdletStrings.NotFilesystemPath, "WebCmdletInFileNotFilesystemPathException", InFile);
                    }
                    else
                    {
                        if (providerPaths.Count > 1)
                        {
                            errorRecord = GetValidationError(WebCmdletStrings.MultiplePathsResolved, "WebCmdletInFileMultiplePathsResolvedException", InFile);
                        }
                        else if (providerPaths.Count == 0)
                        {
                            errorRecord = GetValidationError(WebCmdletStrings.NoPathResolved, "WebCmdletInFileNoPathResolvedException", InFile);
                        }
                        else
                        {
                            if (Directory.Exists(providerPaths[0]))
                            {
                                errorRecord = GetValidationError(WebCmdletStrings.DirectoryPathSpecified, "WebCmdletInFileNotFilePathException", InFile);
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

                if (errorRecord is not null)
                {
                    ThrowTerminatingError(errorRecord);
                }
            }

            // Output ??
            if (PassThru && OutFile is null)
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.OutFileMissing, "WebCmdletOutFileMissingException", nameof(PassThru));
                ThrowTerminatingError(error);
            }

            // Resume requires OutFile.
            if (Resume.IsPresent && OutFile is null)
            {
                ErrorRecord error = GetValidationError(WebCmdletStrings.OutFileMissing, "WebCmdletOutFileMissingException", nameof(Resume));
                ThrowTerminatingError(error);
            }
        }

        internal virtual void PrepareSession()
        {
            // Make sure we have a valid WebRequestSession object to work with
            WebSession ??= new WebRequestSession();

            if (SessionVariable is not null)
            {
                // Save the session back to the PS environment if requested
                PSVariableIntrinsics vi = SessionState.PSVariable;
                vi.Set(SessionVariable, WebSession);
            }

            // Handle credentials
            if (Credential is not null && Authentication == WebAuthenticationType.None)
            {
                // Get the relevant NetworkCredential
                NetworkCredential netCred = Credential.GetNetworkCredential();
                WebSession.Credentials = netCred;

                // Supplying a credential overrides the UseDefaultCredentials setting
                WebSession.UseDefaultCredentials = false;
            }
            else if ((Credential is not null || Token is not null) && Authentication != WebAuthenticationType.None)
            {
                ProcessAuthentication();
            }
            else if (UseDefaultCredentials)
            {
                WebSession.UseDefaultCredentials = true;
            }

            if (CertificateThumbprint is not null)
            {
                using X509Store store = new(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
                X509Certificate2Collection tbCollection = (X509Certificate2Collection)collection.Find(X509FindType.FindByThumbprint, CertificateThumbprint, false);
                if (tbCollection.Count == 0)
                {
                    throw new CryptographicException(WebCmdletStrings.ThumbprintNotFound);
                }

                foreach (X509Certificate2 tbCert in tbCollection)
                {
                    X509Certificate certificate = (X509Certificate)tbCert;
                    WebSession.AddCertificate(certificate);
                }
            }

            if (Certificate is not null)
            {
                WebSession.AddCertificate(Certificate);
            }

            // Handle the user agent
            if (UserAgent is not null)
            {
                // Store the UserAgent string
                WebSession.UserAgent = UserAgent;
            }

            if (Proxy is not null)
            {
                WebProxy webProxy = new(Proxy);
                webProxy.BypassProxyOnLocal = false;
                if (ProxyCredential is not null)
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

            if (MaximumRedirection > -1)
            {
                WebSession.MaximumRedirection = MaximumRedirection;
            }

            // Store the other supplied headers
            if (Headers is not null)
            {
                foreach (string key in Headers.Keys)
                {
                    object? value = Headers[key];

                    // null is not valid value for header.
                    // We silently ignore header if value is null.
                    if (value is not null)
                    {
                        // Add the header value (or overwrite it if already present)
                        WebSession.Headers[key] = value.ToString();
                    }
                }
            }

            if (MaximumRetryCount > 0)
            {
                WebSession.MaximumRetryCount = MaximumRetryCount;

                // Only set retry interval if retry count is set.
                WebSession.RetryIntervalInSeconds = RetryIntervalSec;
            }
        }
        
        internal virtual HttpClient GetHttpClient(bool handleRedirect)
        {
            HttpClientHandler handler = new();
            handler.CookieContainer = WebSession!.Cookies;
            handler.AutomaticDecompression = DecompressionMethods.All;

            // Set the credentials used by this request
            if (WebSession.UseDefaultCredentials)
            {
                // The UseDefaultCredentials flag overrides other supplied credentials
                handler.UseDefaultCredentials = true;
            }
            else if (WebSession.Credentials is not null)
            {
                handler.Credentials = WebSession.Credentials;
            }

            if (NoProxy)
            {
                handler.UseProxy = false;
            }
            else if (WebSession.Proxy is not null)
            {
                handler.Proxy = WebSession.Proxy;
            }

            if (WebSession.Certificates is not null)
            {
                handler.ClientCertificates.AddRange(WebSession.Certificates);
            }

            if (SkipCertificateCheck)
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            }

            // This indicates GetResponse will handle redirects.
            if (handleRedirect || WebSession.MaximumRedirection == 0)
            {
                handler.AllowAutoRedirect = false;
            }
            else if (WebSession.MaximumRedirection > 0)
            {
                handler.MaxAutomaticRedirections = WebSession.MaximumRedirection;
            }

            handler.SslProtocols = (SslProtocols)SslProtocol;

            HttpClient httpClient = new(handler);

            // Check timeout setting (in seconds instead of milliseconds as in HttpWebRequest)
            httpClient.Timeout = TimeoutSec is 0 ? TimeSpan.FromMilliseconds(Timeout.Infinite) : new TimeSpan(0, 0, TimeoutSec);

            return httpClient;
        }

        internal virtual HttpRequestMessage GetRequest(Uri? uri)
        {
            Uri? requestUri = PrepareUri(uri);
            HttpMethod httpMethod = string.IsNullOrEmpty(CustomMethod) ? GetHttpMethod(Method) : new HttpMethod(CustomMethod);

            // Create the base WebRequest object
            HttpRequestMessage request = new(httpMethod, requestUri);

            if (HttpVersion is not null)
            {
                request.Version = HttpVersion;
            }

            // Pull in session data
            if (WebSession!.Headers.Count > 0)
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
            if (WebSession.Headers.TryGetValue(HttpKnownHeaderNames.UserAgent, out string? userAgent))
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
            if (TransferEncoding is not null)
            {
                request.Headers.TransferEncodingChunked = true;
                TransferCodingHeaderValue headerValue = new(TransferEncoding);
                if (!request.Headers.TransferEncoding.Contains(headerValue))
                {
                    request.Headers.TransferEncoding.Add(headerValue);
                }
            }

            // If the file to resume downloading exists, create the Range request header using the file size.
            // If not, create a Range to request the entire file.
            if (Resume.IsPresent)
            {
                FileInfo fileInfo = new(QualifiedOutFile);
                if (fileInfo.Exists)
                {
                    request.Headers.Range = new RangeHeaderValue(fileInfo.Length, null);
                    _resumeFileSize = fileInfo.Length;
                }
                else
                {
                    request.Headers.Range = new RangeHeaderValue(0, null);
                }
            }

            return request;
        }

        internal virtual void FillRequestStream(HttpRequestMessage request)
        {
            ArgumentNullException.ThrowIfNull(request);

            // Set the request content type
            if (ContentType is not null)
            {
                WebSession!.ContentHeaders[HttpKnownHeaderNames.ContentType] = ContentType;
            }
            else if (request.Method == HttpMethod.Post)
            {
                // Win8:545310 Invoke-WebRequest does not properly set MIME type for POST
                WebSession!.ContentHeaders.TryGetValue(HttpKnownHeaderNames.ContentType, out string? contentType);
                if (string.IsNullOrEmpty(contentType))
                {
                    WebSession.ContentHeaders[HttpKnownHeaderNames.ContentType] = "application/x-www-form-urlencoded";
                }
            }

            if (Form is not null)
            {
                MultipartFormDataContent formData = new();
                foreach (DictionaryEntry formEntry in Form)
                {
                    // AddMultipartContent will handle PSObject unwrapping, Object type determination and enumerateing top level IEnumerables.
                    AddMultipartContent(fieldName: formEntry.Key, fieldValue: formEntry.Value, formData: formData, enumerate: true);
                }

                SetRequestContent(request, formData);
            }
            else if (Body is not null)
            {
                // Coerce body into a usable form
                // Make sure we're using the base object of the body, not the PSObject wrapper
                object content = Body is PSObject psBody ? psBody.BaseObject : Body;

                switch (content)
                {
                    case FormObject form:
                        SetRequestContent(request, form.Fields);
                        break;
                    case IDictionary dictionary when request.Method != HttpMethod.Get:
                        SetRequestContent(request, dictionary);
                        break;
                    case XmlNode xmlNode:
                        SetRequestContent(request, xmlNode);
                        break;
                    case Stream stream:
                        SetRequestContent(request, stream);
                        break;
                    case byte[] bytes:
                        SetRequestContent(request, bytes);
                        break;
                    case MultipartFormDataContent multipartFormDataContent:
                        SetRequestContent(request, multipartFormDataContent);
                        break;
                    default:
                        SetRequestContent(request, (string)LanguagePrimitives.ConvertTo(content, typeof(string), CultureInfo.InvariantCulture));
                        break;
                }
            }
            else if (InFile is not null)
            {
                // Copy InFile data
                try
                {
                    // Open the input file
                    SetRequestContent(request, new FileStream(InFile, FileMode.Open, FileAccess.Read, FileShare.Read));
                }
                catch (UnauthorizedAccessException)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture, WebCmdletStrings.AccessDenied, _originalFilePath);

                    throw new UnauthorizedAccessException(msg);
                }
            }

            // For other methods like Put where empty content has meaning, we need to fill in the content
            if (request.Content is null)
            {
                // If this is a Get request and there is no content, then don't fill in the content as empty content gets rejected by some web services per RFC7230
                if (request.Method == HttpMethod.Get && ContentType is null)
                {
                    return;
                }

                request.Content = new StringContent(string.Empty);
                request.Content.Headers.Clear();
            }

            foreach (KeyValuePair<string, string> entry in WebSession!.ContentHeaders)
            {
                if (!string.IsNullOrWhiteSpace(entry.Value))
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
                            ValidationMetadataException outerEx = new(WebCmdletStrings.ContentTypeException, ex);
                            ErrorRecord er = new(outerEx, "WebCmdletContentTypeException", ErrorCategory.InvalidArgument, ContentType);
                            ThrowTerminatingError(er);
                        }
                    }
                }
            }
        }

        internal virtual HttpResponseMessage GetResponse(HttpClient client, HttpRequestMessage request, bool handleRedirect)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(request);

            // Add 1 to account for the first request.
            int totalRequests = WebSession!.MaximumRetryCount + 1;
            HttpRequestMessage currentRequest = request;
            HttpResponseMessage? response = null;

            do
            {
                // Track the current URI being used by various requests and re-requests.
                Uri? currentUri = currentRequest.RequestUri;

                _cancelToken = new CancellationTokenSource();
                response = client.SendAsync(currentRequest, HttpCompletionOption.ResponseHeadersRead, _cancelToken.Token).GetAwaiter().GetResult();

                if (handleRedirect
                    && WebSession.MaximumRedirection is not 0
                    && IsRedirectCode(response.StatusCode)
                    && response.Headers.Location is not null)
                {
                    _cancelToken.Cancel();
                    _cancelToken = null;

                    // If explicit count was provided, reduce it for this redirection.
                    if (WebSession.MaximumRedirection > 0)
                    {
                        WebSession.MaximumRedirection--;
                    }

                    // For selected redirects, GET must be used with the redirected Location.
                    if (RequestRequiresForceGet(response.StatusCode, currentRequest.Method) && !PreserveHttpMethodOnRedirect)
                    {
                        Method = WebRequestMethod.Get;
                        CustomMethod = string.Empty;
                    }

                    currentUri = new Uri(request.RequestUri!, response.Headers.Location);

                    // Continue to handle redirection
                    using HttpRequestMessage redirectRequest = GetRequest(currentUri);
                    response.Dispose();
                    response = GetResponse(client, redirectRequest, handleRedirect);
                }

                // Request again without the Range header because the server indicated the range was not satisfiable.
                // This happens when the local file is larger than the remote file.
                // If the size of the remote file is the same as the local file, there is nothing to resume.
                if (Resume.IsPresent
                    && response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable
                    && response.Content.Headers.ContentRange?.Length != _resumeFileSize)
                {
                    _cancelToken?.Cancel();

                    WriteVerbose(WebCmdletStrings.WebMethodResumeFailedVerboseMsg);

                    // Disable the Resume switch so the subsequent calls to GetResponse() and FillRequestStream()
                    // are treated as a standard -OutFile request. This also disables appending local file.
                    Resume = new SwitchParameter(false);

                    using (HttpRequestMessage requestWithoutRange = GetRequest(currentUri))
                    {
                        FillRequestStream(requestWithoutRange);

                        long? requestContentLength = requestWithoutRange.Content is null ? 0 : requestWithoutRange.Content.Headers.ContentLength;

                        string reqVerboseMsg = string.Format(
                            CultureInfo.CurrentCulture,
                            WebCmdletStrings.WebMethodInvocationVerboseMsg,
                            requestWithoutRange.Version,
                            requestWithoutRange.Method,
                            requestContentLength);
                        
                        WriteVerbose(reqVerboseMsg);

                        response.Dispose();
                        response = GetResponse(client, requestWithoutRange, handleRedirect);
                    }
                }

                _resumeSuccess = response.StatusCode == HttpStatusCode.PartialContent;

                // When MaximumRetryCount is not specified, the totalRequests is 1.
                if (totalRequests > 1 && ShouldRetry(response.StatusCode))
                {
                    int retryIntervalInSeconds = WebSession.RetryIntervalInSeconds;

                    // If the status code is 429 get the retry interval from the Headers.
                    // Ignore broken header and its value.
                    if (response.StatusCode is HttpStatusCode.Conflict && response.Headers.TryGetValues(HttpKnownHeaderNames.RetryAfter, out IEnumerable<string>? retryAfter)) 
                    {
                        try 
                        {
                            IEnumerator<string> enumerator = retryAfter.GetEnumerator();
                            if (enumerator.MoveNext())
                            {
                                retryIntervalInSeconds = Convert.ToInt32(enumerator.Current);
                            }
                        }
                        catch
                        {
                            // Ignore broken header.
                        }
                    }
                    
                    string retryMessage = string.Format(
                        CultureInfo.CurrentCulture,
                        WebCmdletStrings.RetryVerboseMsg,
                        retryIntervalInSeconds,
                        response.StatusCode);

                    WriteVerbose(retryMessage);

                    _cancelToken = new CancellationTokenSource();
                    Task.Delay(retryIntervalInSeconds * 1000, _cancelToken.Token).GetAwaiter().GetResult();
                    _cancelToken.Cancel();
                    _cancelToken = null;

                    currentRequest.Dispose();
                    currentRequest = GetRequest(currentUri);
                    FillRequestStream(currentRequest);
                }

                totalRequests--;
            }
            while (totalRequests > 0 && !response.IsSuccessStatusCode);

            return response;
        }

        internal virtual void UpdateSession(HttpResponseMessage response)
        {
            ArgumentNullException.ThrowIfNull(response);
        }

        #endregion Virtual Methods

        #region Helper Methods
        private Uri PrepareUri(Uri? uri)
        {
            uri = CheckProtocol(uri);

            // Before creating the web request,
            // preprocess Body if content is a dictionary and method is GET (set as query)
            LanguagePrimitives.TryConvertTo<IDictionary>(Body, out IDictionary bodyAsDictionary);
            if (bodyAsDictionary is not null && (Method == WebRequestMethod.Default || Method == WebRequestMethod.Get || CustomMethod == "GET"))
            {
                UriBuilder uriBuilder = new(uri);
                if (uriBuilder.Query is not null && uriBuilder.Query.Length > 1)
                {
                    uriBuilder.Query = string.Concat(uriBuilder.Query.AsSpan(1), "&", FormatDictionary(bodyAsDictionary));
                }
                else
                {
                    uriBuilder.Query = FormatDictionary(bodyAsDictionary);
                }

                uri = uriBuilder.Uri;

                // Set body to null to prevent later FillRequestStream
                Body = null;
            }

            return uri;
        }

        private static Uri CheckProtocol(Uri? uri)
        {
            ArgumentNullException.ThrowIfNull(uri);

            return uri.IsAbsoluteUri ? uri : new Uri("http://" + uri.OriginalString);
        }

        private string QualifyFilePath(string? path) => PathUtils.ResolveFilePath(filePath: path, command: this, isLiteralPath: true);
        
        private static string FormatDictionary(IDictionary content)
        {
            ArgumentNullException.ThrowIfNull(content);

            StringBuilder bodyBuilder = new();
            foreach (string key in content.Keys)
            {
                if (bodyBuilder.Length > 0)
                {
                    bodyBuilder.Append('&');
                }

                object? value = content[key];

                // URLEncode the key and value
                string encodedKey = WebUtility.UrlEncode(key);
                string encodedValue = value is null ? string.Empty : WebUtility.UrlEncode(value.ToString()!);

                bodyBuilder.Append($"{encodedKey}={encodedValue}");
            }

            return bodyBuilder.ToString();
        }

        private ErrorRecord GetValidationError(string msg, string errorId)
        {
            ValidationMetadataException ex = new(msg);
            return new ErrorRecord(ex, errorId, ErrorCategory.InvalidArgument, this);
        }

        private ErrorRecord GetValidationError(string msg, string errorId, params object[] args)
        {
            msg = string.Format(CultureInfo.InvariantCulture, msg, args);
            ValidationMetadataException ex = new(msg);
            return new ErrorRecord(ex, errorId, ErrorCategory.InvalidArgument, this);
        }

        private string GetBasicAuthorizationHeader()
        {
            string password = new NetworkCredential(string.Empty, Credential!.Password).Password;
            string unencoded = string.Create(CultureInfo.InvariantCulture, $"{Credential.UserName}:{password}");
            byte[] bytes = Encoding.UTF8.GetBytes(unencoded);
            return string.Create(CultureInfo.InvariantCulture, $"Basic {Convert.ToBase64String(bytes)}");
        }

        private string GetBearerAuthorizationHeader()
        {
            return string.Create(CultureInfo.InvariantCulture, $"Bearer {new NetworkCredential(string.Empty, Token).Password}");
        }

        private void ProcessAuthentication()
        {
            if (Authentication == WebAuthenticationType.Basic)
            {
                WebSession!.Headers["Authorization"] = GetBasicAuthorizationHeader();
            }
            else if (Authentication == WebAuthenticationType.Bearer || Authentication == WebAuthenticationType.OAuth)
            {
                WebSession!.Headers["Authorization"] = GetBearerAuthorizationHeader();
            }
            else
            {
                Diagnostics.Assert(false, string.Create(CultureInfo.InvariantCulture, $"Unrecognized Authentication value: {Authentication}"));
            }
        }

        /// <summary>
        /// Sets the ContentLength property of the request and writes the specified content to the request's RequestStream.
        /// </summary>
        /// <param name="request">The WebRequest who's content is to be set.</param>
        /// <param name="content">A byte array containing the content data.</param>
        /// <remarks>
        /// Because this function sets the request's ContentLength property and writes content data into the request's stream,
        /// it should be called one time maximum on a given request.
        /// </remarks>
        internal void SetRequestContent(HttpRequestMessage request, byte[] content)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(content);

            request.Content = new ByteArrayContent(content);
        }

        /// <summary>
        /// Sets the ContentLength property of the request and writes the specified content to the request's RequestStream.
        /// </summary>
        /// <param name="request">The WebRequest who's content is to be set.</param>
        /// <param name="content">A String object containing the content data.</param>
        /// <remarks>
        /// Because this function sets the request's ContentLength property and writes content data into the request's stream,
        /// it should be called one time maximum on a given request.
        /// </remarks>
        internal void SetRequestContent(HttpRequestMessage request, string content)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(content);
            
            Encoding? encoding = null;
            if (ContentType is not null)
            {
                // If Content-Type contains the encoding format (as CharSet), use this encoding format
                // to encode the Body of the WebRequest sent to the server. Default Encoding format
                // would be used if Charset is not supplied in the Content-Type property.
                try
                {
                    MediaTypeHeaderValue mediaTypeHeaderValue = MediaTypeHeaderValue.Parse(ContentType);
                    if (!string.IsNullOrEmpty(mediaTypeHeaderValue.CharSet))
                    {
                        encoding = Encoding.GetEncoding(mediaTypeHeaderValue.CharSet);
                    }
                }
                catch (Exception ex) when (ex is FormatException || ex is ArgumentException)
                {
                    if (!SkipHeaderValidation)
                    {
                        ValidationMetadataException outerEx = new(WebCmdletStrings.ContentTypeException, ex);
                        ErrorRecord er = new(outerEx, "WebCmdletContentTypeException", ErrorCategory.InvalidArgument, ContentType);
                        ThrowTerminatingError(er);
                    }
                }
            }

            byte[] bytes = StreamHelper.EncodeToBytes(content, encoding);
            request.Content = new ByteArrayContent(bytes);
        }

        internal void SetRequestContent(HttpRequestMessage request, XmlNode xmlNode)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(xmlNode);

            byte[]? bytes = null;
            XmlDocument? doc = xmlNode as XmlDocument;
            if (doc?.FirstChild is XmlDeclaration decl)
            {
                Encoding encoding = Encoding.GetEncoding(decl.Encoding);
                bytes = StreamHelper.EncodeToBytes(doc.OuterXml, encoding);
            }
            else
            {
                bytes = StreamHelper.EncodeToBytes(xmlNode.OuterXml, encoding: null);
            }

            request.Content = new ByteArrayContent(bytes);
        }

        /// <summary>
        /// Sets the ContentLength property of the request and writes the specified content to the request's RequestStream.
        /// </summary>
        /// <param name="request">The WebRequest who's content is to be set.</param>
        /// <param name="contentStream">A Stream object containing the content data.</param>
        /// <remarks>
        /// Because this function sets the request's ContentLength property and writes content data into the request's stream,
        /// it should be called one time maximum on a given request.
        /// </remarks>
        internal void SetRequestContent(HttpRequestMessage request, Stream contentStream)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(contentStream);

            request.Content = new StreamContent(contentStream);
        }

        /// <summary>
        /// Sets the ContentLength property of the request and writes the ContentLength property of the request and writes the specified content to the request's RequestStream.
        /// </summary>
        /// <param name="request">The WebRequest who's content is to be set.</param>
        /// <param name="multipartContent">A MultipartFormDataContent object containing multipart/form-data content.</param>
        /// <remarks>
        /// Because this function sets the request's ContentLength property and writes content data into the request's stream,
        /// it should be called one time maximum on a given request.
        /// </remarks>
        internal void SetRequestContent(HttpRequestMessage request, MultipartFormDataContent multipartContent)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(multipartContent);
            
            // Content headers will be set by MultipartFormDataContent which will throw unless we clear them first
            WebSession!.ContentHeaders.Clear();

            request.Content = multipartContent;
        }

        internal void SetRequestContent(HttpRequestMessage request, IDictionary content)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(content);

            string body = FormatDictionary(content);
            SetRequestContent(request, body);
        }

        internal void ParseLinkHeader(HttpResponseMessage response)
        {
            Uri? requestUri = response.RequestMessage?.RequestUri;

            ArgumentNullException.ThrowIfNull(requestUri);

            if (_relationLink is null)
            {
                // Must ignore the case of relation links. See RFC 8288 (https://tools.ietf.org/html/rfc8288)
                _relationLink = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                _relationLink.Clear();
            }

            // We only support the URL in angle brackets and `rel`, other attributes are ignored
            // user can still parse it themselves via the Headers property
            const string Pattern = "<(?<url>.*?)>;\\s*rel=(?<quoted>\")?(?<rel>(?(quoted).*?|[^,;]*))(?(quoted)\")";
            if (response.Headers.TryGetValues("Link", out IEnumerable<string>? links))
            {
                foreach (string linkHeader in links)
                {
                    MatchCollection matchCollection = Regex.Matches(linkHeader, Pattern);
                    foreach (Match match in matchCollection)
                    {
                        if (match.Success)
                        {
                            string url = match.Groups["url"].Value;
                            string rel = match.Groups["rel"].Value;
                            if (url != string.Empty && rel != string.Empty && !_relationLink.ContainsKey(rel))
                            {
                                Uri absoluteUri = new(requestUri, url);
                                _relationLink.Add(rel, absoluteUri.AbsoluteUri);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds content to a <see cref="MultipartFormDataContent"/>. Object type detection is used to determine if the value is string, File, or Collection.
        /// </summary>
        /// <param name="fieldName">The Field Name to use.</param>
        /// <param name="fieldValue">The Field Value to use.</param>
        /// <param name="formData">The <see cref="MultipartFormDataContent"/> to update.</param>
        /// <param name="enumerate">If true, collection types in <paramref name="fieldValue"/> will be enumerated. If false, collections will be treated as single value.</param>
        private void AddMultipartContent(object fieldName, object? fieldValue, MultipartFormDataContent formData, bool enumerate)
        {
            ArgumentNullException.ThrowIfNull(formData);

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
            if (!enumerate || fieldValue is string || fieldValue is not IEnumerable)
            {
                formData.Add(GetMultipartStringContent(fieldName: fieldName, fieldValue: fieldValue));
                return;
            }

            // Treat the value as a collection and enumerate it if enumeration is true
            if (enumerate && fieldValue is IEnumerable items)
            {
                foreach (object item in items)
                {
                    // Recurse, but do not enumerate the next level. IEnumerables will be treated as single values.
                    AddMultipartContent(fieldName: fieldName, fieldValue: item, formData: formData, enumerate: false);
                }
            }
        }

        /// <summary>
        /// Gets a <see cref="StringContent"/> from the supplied field name and field value. Uses <see cref="LanguagePrimitives.ConvertTo{T}(object)"/> to convert the objects to strings.
        /// </summary>
        /// <param name="fieldName">The Field Name to use for the <see cref="StringContent"/></param>
        /// <param name="fieldValue">The Field Value to use for the <see cref="StringContent"/></param>
        private static StringContent GetMultipartStringContent(object fieldName, object? fieldValue)
        {
            ContentDispositionHeaderValue contentDisposition = new("form-data");
            
            // .NET does not enclose field names in quotes, however, modern browsers and curl do.
            contentDisposition.Name = "\"" + LanguagePrimitives.ConvertTo<string>(fieldName) + "\"";

            StringContent result = new(LanguagePrimitives.ConvertTo<string>(fieldValue));
            result.Headers.ContentDisposition = contentDisposition;

            return result;
        }

        /// <summary>
        /// Gets a <see cref="StreamContent"/> from the supplied field name and <see cref="Stream"/>. Uses <see cref="LanguagePrimitives.ConvertTo{T}(object)"/> to convert the fieldname to a string.
        /// </summary>
        /// <param name="fieldName">The Field Name to use for the <see cref="StreamContent"/></param>
        /// <param name="stream">The <see cref="Stream"/> to use for the <see cref="StreamContent"/></param>
        private static StreamContent GetMultipartStreamContent(object fieldName, Stream stream)
        {
            ContentDispositionHeaderValue contentDisposition = new("form-data");

            // .NET does not enclose field names in quotes, however, modern browsers and curl do.
            contentDisposition.Name = "\"" + LanguagePrimitives.ConvertTo<string>(fieldName) + "\"";

            StreamContent result = new(stream);
            result.Headers.ContentDisposition = contentDisposition;
            result.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            return result;
        }

        /// <summary>
        /// Gets a <see cref="StreamContent"/> from the supplied field name and file. Calls <see cref="GetMultipartStreamContent(object, Stream)"/> to create the <see cref="StreamContent"/> and then sets the file name.
        /// </summary>
        /// <param name="fieldName">The Field Name to use for the <see cref="StreamContent"/></param>
        /// <param name="file">The file to use for the <see cref="StreamContent"/></param>
        private static StreamContent GetMultipartFileContent(object fieldName, FileInfo file)
        {
            StreamContent result = GetMultipartStreamContent(fieldName: fieldName, stream: new FileStream(file.FullName, FileMode.Open));

            // .NET does not enclose field names in quotes, however, modern browsers and curl do.
            result.Headers.ContentDisposition!.FileName = "\"" + file.Name + "\"";

            return result;
        }

        private static string FormatErrorMessage(string error, string contentType)
        {
            string? formattedError = null;

            try
            {
                if (ContentHelper.IsXml(contentType))
                {
                    XmlDocument doc = new();
                    doc.LoadXml(error);

                    XmlWriterSettings settings = new XmlWriterSettings {
                        Indent = true,
                        NewLineOnAttributes = true,
                        OmitXmlDeclaration = true
                    };

                    if (doc.FirstChild is XmlDeclaration decl)
                    {
                        settings.Encoding = Encoding.GetEncoding(decl.Encoding);
                    }

                    StringBuilder stringBuilder = new();
                    using XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, settings);
                    doc.Save(xmlWriter);
                    string xmlString = stringBuilder.ToString();

                    formattedError = Environment.NewLine + xmlString;
                }
                else if (ContentHelper.IsJson(contentType))
                {
                    JsonNode? jsonNode = JsonNode.Parse(error);
                    JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
                    string jsonString = jsonNode!.ToJsonString(options);

                    formattedError = Environment.NewLine + jsonString;
                }
            }
            catch
            {
                // Ignore errors
            }
            
            if (string.IsNullOrEmpty(formattedError))
            {
                // Remove HTML tags making it easier to read
                formattedError = System.Text.RegularExpressions.Regex.Replace(error, "<[^>]*>", string.Empty);
            }

            return formattedError;
        }

        // Returns true if the status code is one of the supported redirection codes.
        private static bool IsRedirectCode(HttpStatusCode statusCode) => statusCode switch
        {
            HttpStatusCode.Found
            or HttpStatusCode.Moved
            or HttpStatusCode.MultipleChoices
            or HttpStatusCode.PermanentRedirect
            or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect => true,
            _ => false
        };

        // Returns true if the status code is a redirection code and the action requires switching to GET on redirection.
        // See https://learn.microsoft.com/en-us/dotnet/api/system.net.httpstatuscode
        private static bool RequestRequiresForceGet(HttpStatusCode statusCode, HttpMethod requestMethod) => statusCode switch
        {
            HttpStatusCode.Found
            or HttpStatusCode.Moved
            or HttpStatusCode.MultipleChoices => requestMethod == HttpMethod.Post,
            HttpStatusCode.SeeOther => requestMethod != HttpMethod.Get && requestMethod != HttpMethod.Head,
            _ => false
        };

        // Returns true if the status code shows a server or client error and MaximumRetryCount > 0
        private static bool ShouldRetry(HttpStatusCode statusCode) => (int)statusCode switch
        {
            304 or (>= 400 and <= 599) => true,
            _ => false
        };

        private static HttpMethod GetHttpMethod(WebRequestMethod method) => method switch
        {
            WebRequestMethod.Default or WebRequestMethod.Get => HttpMethod.Get,
            WebRequestMethod.Delete => HttpMethod.Delete,
            WebRequestMethod.Head => HttpMethod.Head,
            WebRequestMethod.Patch => HttpMethod.Patch,
            WebRequestMethod.Post => HttpMethod.Post,
            WebRequestMethod.Put => HttpMethod.Put,
            WebRequestMethod.Options => HttpMethod.Options,
            WebRequestMethod.Trace => HttpMethod.Trace,
            _ => new HttpMethod(method.ToString().ToUpperInvariant())
        };

        #endregion Helper Methods
    }

    /// <summary>
    /// Exception class for webcmdlets to enable returning HTTP error response.
    /// </summary>
    public sealed class HttpResponseException : HttpRequestException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseException"/> class.
        /// </summary>
        /// <param name="message">Message for the exception.</param>
        /// <param name="response">Response from the HTTP server.</param>
        public HttpResponseException(string message, HttpResponseMessage response) : base(message, inner: null, response.StatusCode)
        {
            Response = response;
        }

        /// <summary>
        /// HTTP error response.
        /// </summary>
        public HttpResponseMessage Response { get; }
    }
}
