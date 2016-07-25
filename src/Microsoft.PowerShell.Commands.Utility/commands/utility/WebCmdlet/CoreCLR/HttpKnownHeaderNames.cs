#if CORECLR

/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;

namespace Microsoft.PowerShell.Commands
{
    internal static partial class HttpKnownHeaderNames
    {
        #region Known_HTTP_Header_Names

        // Known HTTP Header Names. 
        // List comes from corefx/System/Net/HttpKnownHeaderNames.cs
        public const string Accept = "Accept"; 
        public const string AcceptCharset = "Accept-Charset"; 
        public const string AcceptEncoding = "Accept-Encoding"; 
        public const string AcceptLanguage = "Accept-Language"; 
        public const string AcceptRanges = "Accept-Ranges"; 
        public const string Age = "Age"; 
        public const string Allow = "Allow"; 
        public const string Authorization = "Authorization"; 
        public const string CacheControl = "Cache-Control"; 
        public const string Connection = "Connection"; 
        public const string ContentDisposition = "Content-Disposition"; 
        public const string ContentEncoding = "Content-Encoding"; 
        public const string ContentLanguage = "Content-Language"; 
        public const string ContentLength = "Content-Length"; 
        public const string ContentLocation = "Content-Location"; 
        public const string ContentMD5 = "Content-MD5"; 
        public const string ContentRange = "Content-Range"; 
        public const string ContentType = "Content-Type"; 
        public const string Cookie = "Cookie"; 
        public const string Cookie2 = "Cookie2"; 
        public const string Date = "Date"; 
        public const string ETag = "ETag"; 
        public const string Expect = "Expect"; 
        public const string Expires = "Expires"; 
        public const string From = "From"; 
        public const string Host = "Host"; 
        public const string IfMatch = "If-Match"; 
        public const string IfModifiedSince = "If-Modified-Since"; 
        public const string IfNoneMatch = "If-None-Match"; 
        public const string IfRange = "If-Range"; 
        public const string IfUnmodifiedSince = "If-Unmodified-Since"; 
        public const string KeepAlive = "Keep-Alive"; 
        public const string LastModified = "Last-Modified"; 
        public const string Location = "Location"; 
        public const string MaxForwards = "Max-Forwards"; 
        public const string Origin = "Origin"; 
        public const string P3P = "P3P"; 
        public const string Pragma = "Pragma"; 
        public const string ProxyAuthenticate = "Proxy-Authenticate"; 
        public const string ProxyAuthorization = "Proxy-Authorization"; 
        public const string ProxyConnection = "Proxy-Connection"; 
        public const string Range = "Range"; 
        public const string Referer = "Referer"; // NB: The spelling-mistake "Referer" for "Referrer" must be matched. 
        public const string RetryAfter = "Retry-After"; 
        public const string SecWebSocketAccept = "Sec-WebSocket-Accept"; 
        public const string SecWebSocketExtensions = "Sec-WebSocket-Extensions"; 
        public const string SecWebSocketKey = "Sec-WebSocket-Key"; 
        public const string SecWebSocketProtocol = "Sec-WebSocket-Protocol"; 
        public const string SecWebSocketVersion = "Sec-WebSocket-Version"; 
        public const string Server = "Server"; 
        public const string SetCookie = "Set-Cookie"; 
        public const string SetCookie2 = "Set-Cookie2"; 
        public const string TE = "TE"; 
        public const string Trailer = "Trailer"; 
        public const string TransferEncoding = "Transfer-Encoding"; 
        public const string Upgrade = "Upgrade"; 
        public const string UserAgent = "User-Agent"; 
        public const string Vary = "Vary"; 
        public const string Via = "Via"; 
        public const string WWWAuthenticate = "WWW-Authenticate"; 
        public const string Warning = "Warning"; 
        public const string XAspNetVersion = "X-AspNet-Version"; 
        public const string XPoweredBy = "X-Powered-By";

        #endregion Known_HTTP_Header_Names

        private static HashSet<string> _contentHeaderSet = null;
        internal static HashSet<string> ContentHeaders
        {
            get {
                if (_contentHeaderSet == null)
                {
                    _contentHeaderSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    _contentHeaderSet.Add(HttpKnownHeaderNames.Allow);
                    _contentHeaderSet.Add(HttpKnownHeaderNames.ContentDisposition);
                    _contentHeaderSet.Add(HttpKnownHeaderNames.ContentEncoding);
                    _contentHeaderSet.Add(HttpKnownHeaderNames.ContentLanguage);
                    _contentHeaderSet.Add(HttpKnownHeaderNames.ContentLength);
                    _contentHeaderSet.Add(HttpKnownHeaderNames.ContentLocation);
                    _contentHeaderSet.Add(HttpKnownHeaderNames.ContentMD5);
                    _contentHeaderSet.Add(HttpKnownHeaderNames.ContentRange);
                    _contentHeaderSet.Add(HttpKnownHeaderNames.ContentType);
                    _contentHeaderSet.Add(HttpKnownHeaderNames.Expires);
                    _contentHeaderSet.Add(HttpKnownHeaderNames.LastModified);
                }

                return _contentHeaderSet;
            }
        }
    }
}
#endif