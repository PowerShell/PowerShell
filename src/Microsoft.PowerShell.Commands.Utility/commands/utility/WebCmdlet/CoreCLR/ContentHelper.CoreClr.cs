/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System.Text;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.PowerShell.Commands
{
    internal static partial class ContentHelper
    {
        internal static Encoding GetEncoding(HttpResponseMessage response)
        {
            // ContentType may not exist in response header.
            string charSet = response.Content.Headers.ContentType?.CharSet;
            return GetEncodingOrDefault(charSet);
        }

        internal static string GetContentType(HttpResponseMessage response)
        {
            // ContentType may not exist in response header.  Return null if not.
            return response.Content.Headers.ContentType?.MediaType;
        }

        internal static StringBuilder GetRawContentHeader(HttpResponseMessage response)
        {
            StringBuilder raw = new StringBuilder();

            string protocol = WebResponseHelper.GetProtocol(response);
            if (!string.IsNullOrEmpty(protocol))
            {
                int statusCode = WebResponseHelper.GetStatusCode(response);
                string statusDescription = WebResponseHelper.GetStatusDescription(response);
                raw.AppendFormat("{0} {1} {2}", protocol, statusCode, statusDescription);
                raw.AppendLine();
            }

            HttpHeaders[] headerCollections =
            {
                response.Headers,
                response.Content == null ? null : response.Content.Headers
            };

            foreach (var headerCollection in headerCollections)
            {
                if (headerCollection == null)
                {
                    continue;
                }
                foreach (var header in headerCollection)
                {
                    // Headers may have multiple entries with different values
                    foreach (var headerValue in header.Value)
                    {
                        raw.Append(header.Key);
                        raw.Append(": ");
                        raw.Append(headerValue);
                        raw.AppendLine();
                    }
                }
            }

            raw.AppendLine();
            return raw;
        }
    }
}
