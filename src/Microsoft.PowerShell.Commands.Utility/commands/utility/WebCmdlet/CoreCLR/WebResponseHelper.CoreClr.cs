// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;

namespace Microsoft.PowerShell.Commands
{
    internal static partial class WebResponseHelper
    {
        internal static string GetCharacterSet(HttpResponseMessage response)
        {
            string characterSet = response.Content.Headers.ContentType.CharSet;
            return characterSet;
        }

        internal static Dictionary<string, IEnumerable<string>> GetHeadersDictionary(HttpResponseMessage response)
        {
            var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in response.Headers)
            {
                headers[entry.Key] = entry.Value;
            }
            // In CoreFX, HttpResponseMessage separates content related headers, such as Content-Type to
            // HttpResponseMessage.Content.Headers. The remaining headers are in HttpResponseMessage.Headers.
            // The keys in both should be unique with no duplicates between them.
            // Added for backwards compatibility with PowerShell 5.1 and earlier.
            if (response.Content != null)
            {
                foreach (var entry in response.Content.Headers)
                {
                    headers[entry.Key] = entry.Value;
                }
            }

            return headers;
        }

        internal static string GetProtocol(HttpResponseMessage response)
        {
            string protocol = string.Format(CultureInfo.InvariantCulture,
                                            "HTTP/{0}", response.Version);
            return protocol;
        }

        internal static int GetStatusCode(HttpResponseMessage response)
        {
            int statusCode = (int)response.StatusCode;
            return statusCode;
        }

        internal static string GetStatusDescription(HttpResponseMessage response)
        {
            string statusDescription = response.StatusCode.ToString();
            return statusDescription;
        }

        internal static bool IsText(HttpResponseMessage response)
        {
            // ContentType may not exist in response header.
            string contentType = response.Content.Headers.ContentType?.MediaType;
            return ContentHelper.IsText(contentType);
        }
    }
}
