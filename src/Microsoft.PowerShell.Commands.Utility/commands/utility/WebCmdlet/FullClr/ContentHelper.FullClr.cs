/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Text;
using System.Net;

namespace Microsoft.PowerShell.Commands
{
    internal static partial class ContentHelper
    {
        internal static Encoding GetEncoding(WebResponse response)
        {
            string characterSet = null;
            HttpWebResponse httpResponse = response as HttpWebResponse;
            if (null != httpResponse)
            {
                characterSet = httpResponse.CharacterSet;
            }

            return GetEncodingOrDefault(characterSet);
        }

        // Gets the content type with safe fallback - in the situation
        // of FTPWebResponse that returns NotImplemented.
        internal static string GetContentType(WebResponse response)
        {
            string contentType = null;
            try
            {
                contentType = response.ContentType;
            }
            catch (NotImplementedException) { }

            return contentType;
        }

        internal static StringBuilder GetRawContentHeader(WebResponse baseResponse)
        {
            StringBuilder raw = new StringBuilder();

            // add protocol and status line
            string protocol = WebResponseHelper.GetProtocol(baseResponse);
            if (!String.IsNullOrEmpty(protocol))
            {
                int statusCode = WebResponseHelper.GetStatusCode(baseResponse);
                string statusDescription = WebResponseHelper.GetStatusDescription(baseResponse);
                raw.AppendFormat("{0} {1} {2}", protocol, statusCode, statusDescription);
                raw.AppendLine();
            }

            // add headers
            foreach (string key in baseResponse.Headers.AllKeys)
            {
                string value = baseResponse.Headers[key];
                raw.AppendFormat("{0}: {1}", key, value);
                raw.AppendLine();
            }

            raw.AppendLine();
            return raw;
        }
    }
}