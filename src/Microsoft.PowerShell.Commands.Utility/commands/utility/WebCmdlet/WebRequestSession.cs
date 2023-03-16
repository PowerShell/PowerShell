// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// WebRequestSession for holding session infos.
    /// </summary>
    public class WebRequestSession : IDisposable
    {
        private HttpClient _client;
        private CookieContainer _cookies;
        private bool _useDefaultCredentials;
        private ICredentials _credentials;
        private X509CertificateCollection _certificates;
        private IWebProxy _proxy;
        private int _maximumRedirection;
        private WebSslProtocol _sslProtocol;
        private bool _allowAutoRedirect;
        private bool _skipCertificateCheck;
        private bool _noProxy;
        private bool _disposed;
        private int _timeoutSec;
        private UnixDomainSocketEndPoint _unixSocket;

        /// <summary>
        /// Contains true if an existing HttpClient had to be disposed and recreated since the WebSession was last used.
        /// </summary>
        private bool _disposedClient;

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

        /// <summary>
        /// Gets or sets the Proxy property.
        /// </summary>
        public IWebProxy Proxy
        {
            get => _proxy;
            set
            {
                SetClassVar(ref _proxy, value);
                if (_proxy is not null)
                {
                    NoProxy = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets the MaximumRedirection property.
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
        /// Initializes a new instance of the <see cref="WebRequestSession"/> class.
        /// </summary>
        public WebRequestSession()
        {
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

        internal WebSslProtocol SslProtocol { set => SetStructVar(ref _sslProtocol, value); }

        internal bool SkipCertificateCheck { set => SetStructVar(ref _skipCertificateCheck, value); }

        internal int TimeoutSec { set => SetStructVar(ref _timeoutSec, value); }

        internal UnixDomainSocketEndPoint UnixSocket { set => SetClassVar(ref _unixSocket, value); }

        internal bool NoProxy
        {
            set
            {
                SetStructVar(ref _noProxy, value);
                if (_noProxy)
                {
                    Proxy = null;
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
                ResetClient();
                Certificates.Add(certificate);
            }
        }

        /// <summary>
        /// Gets an existing or creates a new HttpClient for this WebRequest session if none currently exists (either because it was never
        /// created, or because changes to the WebSession properties required the existing HttpClient to be disposed).
        /// </summary>
        /// <param name="suppressHttpClientRedirects">True if the caller does not want the HttpClient to ever handle redirections automatically.</param>
        /// <param name="clientWasReset">Contains true if an existing HttpClient had to be disposed and recreated since the WebSession was last used.</param>
        /// <returns>The HttpClient cached in the WebSession, based on all current settings.</returns>
        internal HttpClient GetHttpClient(bool suppressHttpClientRedirects, out bool clientWasReset)
        {
            // Do not auto redirect if the the caller does not want it, or maximum redirections is 0
            SetStructVar(ref _allowAutoRedirect, !(suppressHttpClientRedirects || MaximumRedirection == 0));

            clientWasReset = _disposedClient;

            if (_client is null)
            {
                _client = CreateHttpClient();
                _disposedClient = false;
            }

            return _client;
        }

        private HttpClient CreateHttpClient()
        {
            SocketsHttpHandler handler = new();

            if (_unixSocket is not null)
            {
                handler.ConnectCallback = async (context, token) =>
                {
                    Socket socket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                    UnixDomainSocketEndPoint endpoint = _unixSocket;
                    await socket.ConnectAsync(endpoint).ConfigureAwait(false);

                    return new NetworkStream(socket, ownsSocket: false);
                };
            }

            handler.CookieContainer = Cookies;
            handler.AutomaticDecompression = DecompressionMethods.All;

            if (Credentials is not null)
            {
                handler.Credentials = Credentials;
            }
            else
            {
                handler.Credentials = CredentialCache.DefaultCredentials;
            }

            if (_noProxy)
            {
                handler.UseProxy = false;
            }
            else if (Proxy is not null)
            {
                handler.Proxy = Proxy;
            }

            if (Certificates is not null)
            {
                handler.SslOptions.ClientCertificates = new X509CertificateCollection(Certificates);
            }

            if (_skipCertificateCheck)
            {
                handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
            }

            handler.AllowAutoRedirect = _allowAutoRedirect;
            if (_allowAutoRedirect && MaximumRedirection > 0)
            {
                handler.MaxAutomaticRedirections = MaximumRedirection;
            }

            handler.SslOptions.EnabledSslProtocols = (SslProtocols)_sslProtocol;

            // Check timeout setting (in seconds)
            return new HttpClient(handler)
            {
                Timeout = _timeoutSec is 0 ? TimeSpan.FromMilliseconds(Timeout.Infinite) : TimeSpan.FromSeconds(_timeoutSec)
            };
        }

        private void SetClassVar<T>(ref T oldValue, T newValue) where T : class
        {
            if (oldValue != newValue)
            {
                ResetClient();
                oldValue = newValue;
            }
        }

        private void SetStructVar<T>(ref T oldValue, T newValue) where T : struct
        {
            if (!oldValue.Equals(newValue))
            {
                ResetClient();
                oldValue = newValue;
            }
        }

        private void ResetClient()
        {
            if (_client is not null)
            {
                _disposedClient = true;
                _client.Dispose();
                _client = null;
            }
        }

        /// <summary>
        /// Dispose the WebRequestSession.
        /// </summary>
        /// <param name="disposing">True when called from Dispose() and false when called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _client?.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Dispose the WebRequestSession.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
