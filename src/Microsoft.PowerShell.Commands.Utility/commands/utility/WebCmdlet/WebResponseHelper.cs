/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Net;
using System.Globalization;
using System.Reflection;

namespace Microsoft.PowerShell.Commands
{
    internal static class WebResponseHelper
    {
        internal static string GetCharacterSet(WebResponse response)
        {
            string characterSet = null;

            HttpWebResponse httpResponse = response as HttpWebResponse;
            if (null != httpResponse)
            {
                characterSet = httpResponse.CharacterSet;
            }

            return (characterSet);
        }

        internal static string GetProtocol(WebResponse response)
        {
            string protocol = string.Empty;

            HttpWebResponse httpResponse = response as HttpWebResponse;
            if (null != httpResponse)
            {
                protocol = string.Format(CultureInfo.InvariantCulture, 
                    "HTTP/{0}", httpResponse.ProtocolVersion);
            }

            return (protocol);
        }

        internal static int GetStatusCode(WebResponse response)
        {
            int statusCode = 0;

            HttpWebResponse httpResponse = response as HttpWebResponse;
            if (null != httpResponse)
            {
                statusCode = (int)httpResponse.StatusCode;
            }

            return (statusCode);
        }

        internal static string GetStatusDescription(WebResponse response)
        {
            string statusDescription = string.Empty;

            HttpWebResponse httpResponse = response as HttpWebResponse;
            if (null != httpResponse)
            {
                statusDescription = httpResponse.StatusDescription;
            }

            return (statusDescription);
        }

        internal static bool IsText(WebResponse response)
        {
            string contentType = ContentHelper.GetContentType(response);
            return (ContentHelper.IsText(contentType));
        }
    }
}
