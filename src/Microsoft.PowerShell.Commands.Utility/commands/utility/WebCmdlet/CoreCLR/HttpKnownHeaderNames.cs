// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.PowerShell.Commands
{
    internal static class HttpKnownHeaderNames
    {
        #region Known_HTTP_Header_Names

        // Known HTTP Header Names.
        // List comes from corefx/System/Net/HttpKnownHeaderNames.cs
        internal const string Accept = "Accept";
        internal const string AcceptCharset = "Accept-Charset";
        internal const string AcceptEncoding = "Accept-Encoding";
        internal const string AcceptLanguage = "Accept-Language";
        internal const string AcceptRanges = "Accept-Ranges";
        internal const string Age = "Age";
        internal const string Allow = "Allow";
        internal const string Authorization = "Authorization";
        internal const string CacheControl = "Cache-Control";
        internal const string Connection = "Connection";
        internal const string ContentDisposition = "Content-Disposition";
        internal const string ContentEncoding = "Content-Encoding";
        internal const string ContentLanguage = "Content-Language";
        internal const string ContentLength = "Content-Length";
        internal const string ContentLocation = "Content-Location";
        internal const string ContentMD5 = "Content-MD5";
        internal const string ContentRange = "Content-Range";
        internal const string ContentType = "Content-Type";
        internal const string Cookie = "Cookie";
        internal const string Cookie2 = "Cookie2";
        internal const string Date = "Date";
        internal const string ETag = "ETag";
        internal const string Expect = "Expect";
        internal const string Expires = "Expires";
        internal const string From = "From";
        internal const string Host = "Host";
        internal const string IfMatch = "If-Match";
        internal const string IfModifiedSince = "If-Modified-Since";
        internal const string IfNoneMatch = "If-None-Match";
        internal const string IfRange = "If-Range";
        internal const string IfUnmodifiedSince = "If-Unmodified-Since";
        internal const string KeepAlive = "Keep-Alive";
        internal const string LastModified = "Last-Modified";
        internal const string Location = "Location";
        internal const string MaxForwards = "Max-Forwards";
        internal const string Origin = "Origin";
        internal const string P3P = "P3P";
        internal const string Pragma = "Pragma";
        internal const string ProxyAuthenticate = "Proxy-Authenticate";
        internal const string ProxyAuthorization = "Proxy-Authorization";
        internal const string ProxyConnection = "Proxy-Connection";
        internal const string Range = "Range";
        internal const string Referer = "Referer"; // NB: The spelling-mistake "Referer" for "Referrer" must be matched.
        internal const string RetryAfter = "Retry-After";
        internal const string SecWebSocketAccept = "Sec-WebSocket-Accept";
        internal const string SecWebSocketExtensions = "Sec-WebSocket-Extensions";
        internal const string SecWebSocketKey = "Sec-WebSocket-Key";
        internal const string SecWebSocketProtocol = "Sec-WebSocket-Protocol";
        internal const string SecWebSocketVersion = "Sec-WebSocket-Version";
        internal const string Server = "Server";
        internal const string SetCookie = "Set-Cookie";
        internal const string SetCookie2 = "Set-Cookie2";
        internal const string TE = "TE";
        internal const string Trailer = "Trailer";
        internal const string TransferEncoding = "Transfer-Encoding";
        internal const string Upgrade = "Upgrade";
        internal const string UserAgent = "User-Agent";
        internal const string Vary = "Vary";
        internal const string Via = "Via";
        internal const string WWWAuthenticate = "WWW-Authenticate";
        internal const string Warning = "Warning";
        internal const string XAspNetVersion = "X-AspNet-Version";
        internal const string XPoweredBy = "X-Powered-By";

        #endregion Known_HTTP_Header_Names

        private static HashSet<string> s_contentHeaderSet = null;

        internal static HashSet<string> ContentHeaders
        {
            get
            {
                if (s_contentHeaderSet == null)
                {
                    s_contentHeaderSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    s_contentHeaderSet.Add(HttpKnownHeaderNames.Allow);
                    s_contentHeaderSet.Add(HttpKnownHeaderNames.ContentDisposition);
                    s_contentHeaderSet.Add(HttpKnownHeaderNames.ContentEncoding);
                    s_contentHeaderSet.Add(HttpKnownHeaderNames.ContentLanguage);
                    s_contentHeaderSet.Add(HttpKnownHeaderNames.ContentLength);
                    s_contentHeaderSet.Add(HttpKnownHeaderNames.ContentLocation);
                    s_contentHeaderSet.Add(HttpKnownHeaderNames.ContentMD5);
                    s_contentHeaderSet.Add(HttpKnownHeaderNames.ContentRange);
                    s_contentHeaderSet.Add(HttpKnownHeaderNames.ContentType);
                    s_contentHeaderSet.Add(HttpKnownHeaderNames.Expires);
                    s_contentHeaderSet.Add(HttpKnownHeaderNames.LastModified);
                }

                return s_contentHeaderSet;
            }
        }
    }
}
