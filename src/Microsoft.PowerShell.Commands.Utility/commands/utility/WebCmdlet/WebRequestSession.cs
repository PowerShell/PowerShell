// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// WebRequestSession for holding session infos.
    /// </summary>
    public class WebRequestSession
    {
        private bool _changed;
        private CookieContainer _cookies;
        private bool _useDefaultCredentials;
        private ICredentials _credentials;
        private X509CertificateCollection _certificates;
        private IWebProxy _proxy;
        private int _maximumRedirection;
        private HttpClientHandler _handler;
        private WebSslProtocol _sslProtocol;
        private int _maxAutomaticRedirections;
        private bool _allowAutoRedirect;
        private bool _skipCertificateCheck;
        private bool _noProxy;

        /// <summary>
        /// Gets or sets the Header property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Gets or sets the content Headers when using HttpClient.
        /// </summary>
        internal Dictionary<string, string> ContentHeaders { get; set; }

        /// <summary>
        /// Gets or sets the Cookies property.
        /// </summary>
        public CookieContainer Cookies { get => _cookies; set => SetClassVar(ref _cookies, value); }

        #region Credentials

        /// <summary>
        /// Gets or sets the UseDefaultCredentials property.
        /// </summary>
        public bool UseDefaultCredentials { get => _useDefaultCredentials; set => SetStructVar(ref _useDefaultCredentials, value); }

        /// <summary>
        /// Gets or sets the Credentials property.
        /// </summary>
        public ICredentials Credentials { get => _credentials; set => SetClassVar(ref _credentials, value); }

        /// <summary>
        /// Gets or sets the Certificates property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public X509CertificateCollection Certificates { get => _certificates; set => SetClassVar(ref _certificates, value); }

        #endregion

        /// <summary>
        /// Gets or sets the UserAgent property.
        /// </summary>
        public string UserAgent { get; set; }

        internal bool NoProxy
        {
            get => _noProxy; set
            {
                SetStructVar(ref _noProxy, value);
                if (_noProxy)
                {
                    Proxy = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the Proxy property.
        /// </summary>
        public IWebProxy Proxy
        {
            get => _proxy; set
            {
                SetClassVar(ref _proxy, value);
                if (_proxy is not null)
                {
                    NoProxy = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets the RedirectMax property.
        /// </summary>
        public int MaximumRedirection { get => _maximumRedirection; set => SetStructVar(ref _maximumRedirection, value); }

        /// <summary>
        /// Gets or sets the count of retries for request failures.
        /// </summary>
        public int MaximumRetryCount { get; set; }

        /// <summary>
        /// Gets or sets the interval in seconds between retries.
        /// </summary>
        public int RetryIntervalInSeconds { get; set; }

        /// <summary>
        /// Gets or sets the SslProtocol for the session.
        /// </summary>
        public WebSslProtocol SslProtocol { get => _sslProtocol; set => SetStructVar(ref _sslProtocol, value); }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRequestSession"/> class.
        /// </summary>
        public WebRequestSession()
        {
            _changed = true;

            // Build the headers collection
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ContentHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Build the cookie jar
            _cookies = new CookieContainer();

            // Initialize the credential and certificate caches
            _useDefaultCredentials = false;
            _credentials = null;
            _certificates = null;

            // Setup the default UserAgent
            UserAgent = PSUserAgent.UserAgent;

            _proxy = null;
            _maximumRedirection = -1;
            _allowAutoRedirect = true;
        }

        /// <summary>
        /// Gets a value indicating whether a new handler must be created.
        /// </summary>
        internal bool Changed => _changed;

        /// <summary>
        /// Gets or sets the cached HttpClientHandler in the session to support persistent HTTP connections.
        /// </summary>
        internal HttpClientHandler Handler
        {
            get => _handler; set
            {
                // Once a handler has been created, it's assumed that it was created from the session properties.
                _handler = value;
                _changed = false;
            }
        }

        internal bool SkipCertificateCheck { get => _skipCertificateCheck; set => SetStructVar(ref _skipCertificateCheck, value); }

        internal bool AllowAutoRedirect { get => _allowAutoRedirect; set => SetStructVar(ref _allowAutoRedirect, value); }

        internal int MaxAutomaticRedirections { get => _maxAutomaticRedirections; set => SetStructVar(ref _maxAutomaticRedirections, value); }

        /// <summary>
        /// Add a X509Certificate to the Certificates collection.
        /// </summary>
        /// <param name="certificate">The certificate to be added.</param>
        internal void AddCertificate(X509Certificate certificate)
        {
            Certificates ??= new X509CertificateCollection();
            if (!Certificates.Contains(certificate))
            {
                _changed = true;
                Certificates.Add(certificate);
            }
        }

        private void SetClassVar<T>(ref T oldValue, T newValue) where T : class
        {
            if (oldValue != newValue)
            {
                _changed = true;
                oldValue = newValue;
            }
        }

        private void SetStructVar<T>(ref T oldValue, T newValue) where T : struct
        {
            if (!oldValue.Equals(newValue))
            {
                _changed = true;
                oldValue = newValue;
            }
        }
    }
}
