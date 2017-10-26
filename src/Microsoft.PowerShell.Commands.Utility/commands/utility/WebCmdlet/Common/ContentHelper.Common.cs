/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Text;
using Microsoft.Win32;

namespace Microsoft.PowerShell.Commands
{
    internal static partial class ContentHelper
    {
        #region Constants

        // used to split contentType arguments
        private static readonly char[] s_contentTypeParamSeparator = { ';' };

        // default codepage encoding for web content.  See RFC 2616.
        private const string _defaultCodePage = "ISO-8859-1";

        #endregion Constants

        #region Internal Methods

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

        internal static bool IsJson(string contentType)
        {
            contentType = GetContentTypeSignature(contentType);
            return CheckIsJson(contentType);
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

        internal static Encoding GetDefaultEncoding()
        {
            return GetEncodingOrDefault((string)null);
        }

        #endregion Internal Methods

        #region Private Helper Methods

        private static string GetContentTypeSignature(string contentType)
        {
            if (String.IsNullOrEmpty(contentType))
                return null;

            string sig = contentType.Split(s_contentTypeParamSeparator, 2)[0].ToUpperInvariant();
            return (sig);
        }

        private static bool CheckIsText(string contentType)
        {
            if (String.IsNullOrEmpty(contentType))
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
            if (String.IsNullOrEmpty(contentType))
                return false;

            // RFC 3023: Media types with the suffix "+xml" are XML
            bool isXml = (contentType.Equals("application/xml", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/xml-external-parsed-entity", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/xml-dtd", StringComparison.OrdinalIgnoreCase));

            isXml |= contentType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase);
            return (isXml);
        }

        private static bool CheckIsJson(string contentType)
        {
            if (String.IsNullOrEmpty(contentType))
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

        #endregion Internal Helper Methods
    }
}
