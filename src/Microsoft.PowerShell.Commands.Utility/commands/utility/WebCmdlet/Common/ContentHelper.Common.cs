// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Management.Automation;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

using Microsoft.Win32;

namespace Microsoft.PowerShell.Commands
{
    internal static class ContentHelper
    {
        #region Constants

        // default codepage encoding for web content.  See RFC 2616.
        private const string _defaultCodePage = "ISO-8859-1";

        #endregion Constants

        #region Fields

        // used to split contentType arguments
        private static readonly char[] s_contentTypeParamSeparator = { ';' };

        #endregion Fields

        #region Internal Methods

        internal static string GetContentType(HttpResponseMessage response)
        {
            // ContentType may not exist in response header.  Return null if not.
            return response.Content.Headers.ContentType?.MediaType;
        }

        internal static Encoding GetDefaultEncoding()
        {
            return GetEncodingOrDefault((string)null);
        }

        internal static Encoding GetEncoding(HttpResponseMessage response)
        {
            // ContentType may not exist in response header.
            string charSet = response.Content.Headers.ContentType?.CharSet;
            return GetEncodingOrDefault(charSet);
        }

        internal static Encoding GetEncodingOrDefault(string characterSet)
        {
            // get the name of the codepage to use for response content
            string codepage = (string.IsNullOrEmpty(characterSet) ? _defaultCodePage : characterSet);
            Encoding encoding = null;

            try
            {
                encoding = Encoding.GetEncoding(codepage);
            }
            catch (ArgumentException)
            {
                // 0, default code page
                encoding = Encoding.GetEncoding(0);
            }

            return encoding;
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
                return false;

            // the correct type for JSON content, as specified in RFC 4627
            bool isJson = contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase);

            // add in these other "javascript" related types that
            // sometimes get sent down as the mime type for JSON content
            isJson |= contentType.Equals("text/json", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/x-javascript", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("text/x-javascript", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/javascript", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("text/javascript", StringComparison.OrdinalIgnoreCase);

            return (isJson);
        }

        private static bool CheckIsText(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            // any text, xml or json types are text
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

            return (isText);
        }

        private static bool CheckIsXml(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            // RFC 3023: Media types with the suffix "+xml" are XML
            bool isXml = (contentType.Equals("application/xml", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/xml-external-parsed-entity", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/xml-dtd", StringComparison.OrdinalIgnoreCase));

            isXml |= contentType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase);
            return (isXml);
        }

        private static string GetContentTypeSignature(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return null;

            string sig = contentType.Split(s_contentTypeParamSeparator, 2)[0].ToUpperInvariant();
            return (sig);
        }

        #endregion Private Helper Methods
    }
}
