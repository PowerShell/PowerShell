// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

using Microsoft.Win32;

namespace Microsoft.PowerShell.Commands
{
    internal static class ContentHelper
    {
        #region Internal Methods

        internal static string GetContentType(HttpResponseMessage response)
        {
            // ContentType may not exist in response header.  Return null if not.
            return response.Content.Headers.ContentType?.MediaType;
        }

        internal static Encoding GetDefaultEncoding() => Encoding.UTF8;

        internal static StringBuilder GetRawContentHeader(HttpResponseMessage response)
        {
            StringBuilder raw = new();

            string protocol = WebResponseHelper.GetProtocol(response);
            if (!string.IsNullOrEmpty(protocol))
            {
                int statusCode = WebResponseHelper.GetStatusCode(response);
                string statusDescription = WebResponseHelper.GetStatusDescription(response);
                raw.AppendFormat($"{protocol} {statusCode} {statusDescription}");
                raw.AppendLine();
            }

            HttpHeaders[] headerCollections =
            {
                response.Headers,
                response.Content?.Headers
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

        internal static bool IsJson(string contentType)
        {
            contentType = GetContentTypeSignature(contentType);
            return CheckIsJson(contentType);
        }

        internal static bool IsText(string contentType)
        {
            contentType = GetContentTypeSignature(contentType);
            return CheckIsText(contentType);
        }

        internal static bool IsXml(string contentType)
        {
            contentType = GetContentTypeSignature(contentType);
            return CheckIsXml(contentType);
        }

        #endregion Internal Methods

        #region Private Helper Methods

        private static bool CheckIsJson(string contentType)
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

        private static bool CheckIsText(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }

            // Any text, xml or json types are text
            bool isText = contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || CheckIsXml(contentType)
            || CheckIsJson(contentType);

            // Further content type analysis is available on Windows
            if (Platform.IsWindows && !isText)
            {
                // Media types registered with Windows as having a perceived type of text, are text
                using (RegistryKey contentTypeKey = Registry.ClassesRoot.OpenSubKey(@"MIME\Database\Content Type\" + contentType))
                {
                    if (contentTypeKey != null)
                    {
                        string extension = contentTypeKey.GetValue("Extension") as string;
                        if (extension != null)
                        {
                            using (RegistryKey extensionKey = Registry.ClassesRoot.OpenSubKey(extension))
                            {
                                if (extensionKey != null)
                                {
                                    string perceivedType = extensionKey.GetValue("PerceivedType") as string;
                                    isText = (perceivedType == "text");
                                }
                            }
                        }
                    }
                }
            }

            return isText;
        }

        private static bool CheckIsXml(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }

            // RFC 3023: Media types with the suffix "+xml" are XML
            bool isXml = (contentType.Equals("application/xml", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/xml-external-parsed-entity", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/xml-dtd", StringComparison.OrdinalIgnoreCase));

            isXml |= contentType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase);
            return isXml;
        }

        private static string GetContentTypeSignature(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return null;
            }

            string sig = contentType.Split(';', 2)[0].ToUpperInvariant();
            return sig;
        }

        #endregion Private Helper Methods
    }
}
