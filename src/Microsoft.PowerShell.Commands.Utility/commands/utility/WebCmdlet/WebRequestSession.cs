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
        public CookieContainer Cookies
        {
            get => _cookies; set
            {
                if (value != _cookies)
                {
                    _changed = true;
                    _cookies = value;
                }

            }
        }

        #region Credentials

        /// <summary>
        /// Gets or sets the UseDefaultCredentials property.
        /// </summary>
        public bool UseDefaultCredentials
        {
            get => _useDefaultCredentials; set
            {
                if (value != _useDefaultCredentials)
                {
                    _changed = true;
                    _useDefaultCredentials = value;
                }

            }
        }

        /// <summary>
        /// Gets or sets the Credentials property.
        /// </summary>
        public ICredentials Credentials
        {
            get => _credentials; set
            {
                if (value != _credentials)
                {
                    _changed = true;
                    _credentials = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the Certificates property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public X509CertificateCollection Certificates
        {
            get => _certificates; set
            {
                if (value != _certificates)
                {
                    _certificates = value;
                    _changed = true;
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets the UserAgent property.
        /// </summary>
        public string UserAgent { get; set; }

        internal bool NoProxy
        {
            get => _noProxy; set
            {
                if (value != _noProxy)
                {
                    _changed = true;
                    _noProxy = value;
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
                if (value != _proxy)
                {
                    _changed = true;
                    _proxy = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the RedirectMax property.
        /// </summary>
        public int MaximumRedirection
        {
            get => _maximumRedirection; set
            {
                if (value != _maximumRedirection)
                {
                    _changed = true;
                    _maximumRedirection = value;
                }
            }
        }

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
        public WebSslProtocol SslProtocol
        {
            get => _sslProtocol; set
            {
                if (value != _sslProtocol)
                {
                    _changed = true;
                    _sslProtocol = value;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRequestSession"/> class.
        /// </summary>
        public WebRequestSession()
        {
            _changed = true;
            // build the headers collection
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ContentHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // build the cookie jar
            _cookies = new CookieContainer();

            // initialize the credential and certificate caches
            _useDefaultCredentials = false;
            _credentials = null;
            _certificates = null;

            // setup the default UserAgent
            UserAgent = PSUserAgent.UserAgent;

            _proxy = null;
            _maximumRedirection = -1;
            _allowAutoRedirect = true;
        }

        /// <summary>
        /// Caches the HttpClientHandler in the session to support persistent HTTP connections
        /// </summary>
        internal HttpClientHandler Handler
        {
            get => _handler; set
            {
                _handler = value;
                // Once a handler has been created, it's assumed that it was created from the session
                // properties
                _changed = false;
            }
        }

        internal bool SkipCertificateCheck
        {
            get => _skipCertificateCheck; set
            {
                if (value != _skipCertificateCheck)
                {
                    _changed = true;
                    _skipCertificateCheck = value;
                }
            }
        }

        internal bool AllowAutoRedirect
        {
            get => _allowAutoRedirect; set
            {
                if (value != _allowAutoRedirect)
                {
                    _changed = true;
                    _allowAutoRedirect = value;
                }
            }
        }

        internal int MaxAutomaticRedirections
        {
            get => _maxAutomaticRedirections; set
            {
                if (value != _maxAutomaticRedirections)
                {
                    _changed = true;
                    _maxAutomaticRedirections = value;
                }
            }
        }

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

        /// <summary>
        /// True if the properties that require a new handler to be created have been updated
        /// </summary>
        internal bool Changed { get => _changed; }
    }
}
