/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
#if !CORECLR
using mshtml;
#endif
using Microsoft.Win32;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Base class for Invoke-RestMethod and Invoke-WebRequest commands.
    /// </summary>
    public abstract partial class WebRequestPSCmdlet : PSCmdlet
    {
        #region Virtual Properties

        #region URI

        /// <summary>
        /// gets or sets the parameter UseBasicParsing 
        /// </summary>
        [Parameter]
        public virtual SwitchParameter UseBasicParsing { get; set; }

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
        [Parameter]
        public virtual WebRequestMethod Method
        {
            get { return _method; }
            set { _method = value; }
        }
        private WebRequestMethod _method = WebRequestMethod.Default;

        #endregion

        #region Proxy

        /// <summary>
        /// gets or sets the Proxy property
        /// </summary>
        [Parameter]
        public virtual Uri Proxy { get; set; }

        /// <summary>
        /// gets or sets the ProxyCredential property
        /// </summary>
        [Parameter]
        [Credential]
        public virtual PSCredential ProxyCredential { get; set; }

        /// <summary>
        /// gets or sets the ProxyUseDefaultCredentials property
        /// </summary>
        [Parameter]
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
                                errorRecord = GetValidationError(WebCmdletStrings.DirecotryPathSpecified,
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
            if (null != Credential)
            {
                // get the relevant NetworkCredential
                NetworkCredential netCred = Credential.GetNetworkCredential();
                WebSession.Credentials = netCred;

                // supplying a credential overrides the UseDefaultCredentials setting
                WebSession.UseDefaultCredentials = false;
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

        /// <summary>
        /// Verifies that Internet Explorer is available, and that its first-run
        /// configuration is complete.
        /// </summary>
        /// <param name="checkComObject">True if we should try to access IE's COM object. Not
        /// needed if an HtmlDocument will be created shortly.</param>
        protected bool VerifyInternetExplorerAvailable(bool checkComObject)
        {
            // TODO: Remove this code once the dependency on mshtml has been resolved.
#if CORECLR
            return false;
#else
            bool isInternetExplorerConfigurationComplete = false;
            // Check for IE for both PS Full and PS Core on windows.
            // The registry key DisableFirstRunCustomize can exits at one of the following path.
            // IE uses the same descending order (as mentioned) to check for the presence of this key.
            // If the value of DisableFirstRunCustomize key is set to greater than zero then Run first
            // is disabled.
            string[] disableFirstRunCustomizePaths = new string[] {
                     @"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Internet Explorer\Main",
                     @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Internet Explorer\Main",
                     @"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main",
                     @"HKEY_LOCAL_MACHINE\Software\Microsoft\Internet Explorer\Main"  };

            foreach (string currentRegPath in disableFirstRunCustomizePaths)
            {
                object val = Registry.GetValue(currentRegPath, "DisableFirstRunCustomize", string.Empty);
                if (val != null && !string.Empty.Equals(val) && Convert.ToInt32(val, CultureInfo.InvariantCulture) > 0)
                {
                    isInternetExplorerConfigurationComplete = true;
                    break;
                }
            }

            if (!isInternetExplorerConfigurationComplete)
            {
                // Verify that if IE is installed, it has been through the RunOnce check.
                // Otherwise, the call will hang waiting for users to go through First Run
                // personalization.
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Internet Explorer\\Main"))
                {
                    if (key != null)
                    {
                        foreach (string setting in key.GetValueNames())
                        {
                            if (setting.IndexOf("RunOnce", StringComparison.OrdinalIgnoreCase) > -1)
                            {
                                isInternetExplorerConfigurationComplete = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (isInternetExplorerConfigurationComplete && checkComObject)
            {
                try
                {
                    IHTMLDocument2 ieCheck = (IHTMLDocument2)new HTMLDocument();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(ieCheck);
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    isInternetExplorerConfigurationComplete = false;
                }
            }

            // Throw exception in PS Full only
            if (!isInternetExplorerConfigurationComplete)
                throw new NotSupportedException(WebCmdletStrings.IEDomNotSupported);
            return isInternetExplorerConfigurationComplete;
#endif
        }

        private Uri PrepareUri(Uri uri)
        {
            uri = CheckProtocol(uri);

            // before creating the web request,
            // preprocess Body if content is a dictionary and method is GET (set as query)
            IDictionary bodyAsDictionary;
            LanguagePrimitives.TryConvertTo<IDictionary>(Body, out bodyAsDictionary);
            if ((null != bodyAsDictionary)
                && (Method == WebRequestMethod.Default || Method == WebRequestMethod.Get))
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

        #endregion Helper Methods     
    }
}
