#if CORECLR

/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Net.Http;
using System.Globalization;

namespace Microsoft.PowerShell.Commands
{
    internal static partial class WebResponseHelper
    {
        internal static string GetCharacterSet(HttpResponseMessage response)
        {
            string characterSet = response.Content.Headers.ContentType.CharSet;
            return characterSet;
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
            string contentType = response.Content.Headers.ContentType.MediaType;
            return ContentHelper.IsText(contentType);
        }
    }
}
#endif