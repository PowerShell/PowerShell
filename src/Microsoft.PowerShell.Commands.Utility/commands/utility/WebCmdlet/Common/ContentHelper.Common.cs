// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Humanizer;
using Microsoft.Win32;

namespace Microsoft.PowerShell.Commands
{
    internal static class ContentHelper
    {
        #region Internal Methods

        // ContentType may not exist in response header.  Return null if not.
        internal static string? GetContentType(HttpResponseMessage response) => response.Content.Headers.ContentType?.MediaType;

        internal static string? GetContentType(HttpRequestMessage request) => request.Content?.Headers.ContentType?.MediaType;

        internal static Encoding GetDefaultEncoding() => Encoding.UTF8;

        internal static string GetFriendlyContentLength(long? length) =>
            length.HasValue
            ? $"{length.Value.Bytes().Humanize()} ({length.Value:#,0} bytes)"
            : "unknown size";

        internal static StringBuilder GetRawContentHeader(HttpResponseMessage response)
        {
            StringBuilder raw = new();

            string protocol = WebResponseHelper.GetProtocol(response);
            if (!string.IsNullOrEmpty(protocol))
            {
                int statusCode = WebResponseHelper.GetStatusCode(response);
                string statusDescription = WebResponseHelper.GetStatusDescription(response);
                raw.AppendLine($"{protocol} {statusCode} {statusDescription}");
            }

            HttpHeaders[] headerCollections =
            {
                response.Headers,
                response.Content.Headers
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
                    foreach (string headerValue in header.Value)
                    {
                        raw.AppendLine($"{header.Key}: {headerValue}");
                    }
                }
            }

            raw.AppendLine();
            return raw;
        }

        internal static bool IsJson([NotNullWhen(true)] string? contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }

            // The correct type for JSON content, as specified in RFC 4627
            bool isJson = contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase);

            // Add in these other "javascript" related types that
            // sometimes get sent down as the mime type for JSON content
            isJson |= contentType.Equals("text/json", StringComparison.OrdinalIgnoreCase)
                   || contentType.Equals("application/x-javascript", StringComparison.OrdinalIgnoreCase)
                   || contentType.Equals("text/x-javascript", StringComparison.OrdinalIgnoreCase)
                   || contentType.Equals("application/javascript", StringComparison.OrdinalIgnoreCase)
                   || contentType.Equals("text/javascript", StringComparison.OrdinalIgnoreCase);

            return isJson;
        }

        internal static bool IsText([NotNullWhen(true)] string? contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }

            // Any text, xml or json types are text
            bool isText = contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                        || IsXml(contentType)
                        || IsJson(contentType);

            // Further content type analysis is available on Windows
            if (Platform.IsWindows && !isText)
            {
                // Media types registered with Windows as having a perceived type of text, are text
                using (RegistryKey? contentTypeKey = Registry.ClassesRoot.OpenSubKey(@"MIME\Database\Content Type\" + contentType))
                {
                    if (contentTypeKey != null)
                    {
                        if (contentTypeKey.GetValue("Extension") is string extension)
                        {
                            using (RegistryKey? extensionKey = Registry.ClassesRoot.OpenSubKey(extension))
                            {
                                if (extensionKey != null)
                                {
                                    string? perceivedType = extensionKey.GetValue("PerceivedType") as string;
                                    isText = perceivedType == "text";
                                }
                            }
                        }
                    }
                }
            }

            return isText;
        }

        internal static bool IsXml([NotNullWhen(true)] string? contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }

            // RFC 3023: Media types with the suffix "+xml" are XML
            bool isXml = contentType.Equals("application/xml", StringComparison.OrdinalIgnoreCase)
                       || contentType.Equals("application/xml-external-parsed-entity", StringComparison.OrdinalIgnoreCase)
                       || contentType.Equals("application/xml-dtd", StringComparison.OrdinalIgnoreCase)
                       || contentType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase);

            return isXml;
        }

        internal static bool IsTextBasedContentType([NotNullWhen(true)] string? contentType)
            => IsText(contentType) || IsJson(contentType) || IsXml(contentType);

        #endregion Internal Methods
    }
}
