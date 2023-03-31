// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;

namespace Microsoft.PowerShell.Commands
{
    internal static class WebResponseHelper
    {
        internal static string GetCharacterSet(HttpResponseMessage response) => response.Content.Headers.ContentType?.CharSet ?? string.Empty;

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
            if (response.Content is not null)
            {
                foreach (var entry in response.Content.Headers)
                {
                    headers[entry.Key] = entry.Value;
                }
            }

            return headers;
        }

        internal static string GetOutFilePath(HttpResponseMessage response, string _qualifiedOutFile)
        {
            // Get file name from last segment of Uri
            string? lastUriSegment = System.Net.WebUtility.UrlDecode(response.RequestMessage?.RequestUri?.Segments[^1]);

            return Directory.Exists(_qualifiedOutFile) ? Path.Join(_qualifiedOutFile, lastUriSegment) : _qualifiedOutFile;
        }

        internal static string GetProtocol(HttpResponseMessage response) => string.Create(CultureInfo.InvariantCulture, $"HTTP/{response.Version}");

        internal static int GetStatusCode(HttpResponseMessage response) => (int)response.StatusCode;

        internal static string GetStatusDescription(HttpResponseMessage response) => response.StatusCode.ToString();

        internal static bool IsText(HttpResponseMessage response)
        {
            // ContentType may not exist in response header.
            string? contentType = ContentHelper.GetContentType(response);
            return ContentHelper.IsText(contentType);
        }
    }
}
