// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// WebRequestSession for holding session infos.
    /// </summary>
    public class WebRequestSession
    {
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
        public CookieContainer Cookies { get; set; }

        #region Credentials

        /// <summary>
        /// Gets or sets the UseDefaultCredentials property.
        /// </summary>
        public bool UseDefaultCredentials { get; set; }

        /// <summary>
        /// Gets or sets the Credentials property.
        /// </summary>
        public ICredentials Credentials { get; set; }

        /// <summary>
        /// Gets or sets the Certificates property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public X509CertificateCollection Certificates { get; set; }

        #endregion

        /// <summary>
        /// Gets or sets the UserAgent property.
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the Proxy property.
        /// </summary>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// Gets or sets the RedirectMax property.
        /// </summary>
        public int MaximumRedirection { get; set; }

        /// <summary>
        /// Gets or sets the count of retries for request failures.
        /// </summary>
        public int MaximumRetryCount { get; set; }

        /// <summary>
        /// Gets or sets the interval in seconds between retries.
        /// </summary>
        public int RetryIntervalInSeconds { get; set; }

        /// <summary>
        /// Construct a new instance of a WebRequestSession object.
        /// </summary>
        public WebRequestSession()
        {
            // build the headers collection
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ContentHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // build the cookie jar
            Cookies = new CookieContainer();

            // initialize the credential and certificate caches
            UseDefaultCredentials = false;
            Credentials = null;
            Certificates = null;

            // setup the default UserAgent
            UserAgent = PSUserAgent.UserAgent;

            Proxy = null;
            MaximumRedirection = -1;
        }

        /// <summary>
        /// Add a X509Certificate to the Certificates collection.
        /// </summary>
        /// <param name="certificate">The certificate to be added.</param>
        internal void AddCertificate(X509Certificate certificate)
        {
            if (Certificates == null)
            {
                Certificates = new X509CertificateCollection();
            }

            Certificates.Add(certificate);
        }
    }
}
