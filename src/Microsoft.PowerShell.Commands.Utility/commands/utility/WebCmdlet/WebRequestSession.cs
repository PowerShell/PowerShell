/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Net;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// WebRequestSession for holding session infos.
    /// </summary>
    public class WebRequestSession
    {
        /// <summary>
        /// gets or sets the Header property
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Dictionary<string, string> Headers { get; set; }

#if CORECLR
        /// <summary>
        /// gets or sets the content Headers when using HttpClient
        /// </summary>
        internal Dictionary<string, string> ContentHeaders { get; set; }
#endif

        /// <summary>
        /// gets or sets the Cookies property
        /// </summary>
        public CookieContainer Cookies { get; set; }

        #region Credentials

        /// <summary>
        /// gets or sets the UseDefaultCredentials property
        /// </summary>
        public bool UseDefaultCredentials { get; set; }

        /// <summary>
        /// gets or sets the Credentials property
        /// </summary>
        public ICredentials Credentials { get; set; }

        /// <summary>
        /// gets or sets the Certificates property
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public X509CertificateCollection Certificates { get; set; }

        #endregion

        /// <summary>
        /// gets or sets the UserAgent property
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// gets or sets the Proxy property
        /// </summary>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// gets or sets the RedirectMax property
        /// </summary>
        public int MaximumRedirection { get; set; }

        /// <summary>
        /// Construct a new instance of a WebRequestSession object.
        /// </summary>
        public WebRequestSession()
        {
            // build the headers collection
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
#if CORECLR
            ContentHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
#endif

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
            if (null == Certificates)
            {
                Certificates = new X509CertificateCollection();
            }
            Certificates.Add(certificate);
        }
    }
}
