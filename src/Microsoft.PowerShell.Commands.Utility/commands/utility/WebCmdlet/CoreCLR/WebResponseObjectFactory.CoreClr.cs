// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;
using System.Net.Http;

namespace Microsoft.PowerShell.Commands
{
    internal static class WebResponseObjectFactory
    {
        internal static WebResponseObject GetResponseObject(HttpResponseMessage response, Stream responseStream, ExecutionContext executionContext)
        {
            WebResponseObject output;
            if (WebResponseHelper.IsText(response))
            {
                output = new BasicHtmlWebResponseObject(response, responseStream);
            }
            else
            {
                output = new WebResponseObject(response, responseStream);
            }

            return output;
        }
    }
}
