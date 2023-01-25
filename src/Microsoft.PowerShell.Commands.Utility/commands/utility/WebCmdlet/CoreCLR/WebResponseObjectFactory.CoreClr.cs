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
            WebResponseObject output = WebResponseHelper.IsText(response) switch
            {
                true => new BasicHtmlWebResponseObject(response, responseStream),
                _ => new WebResponseObject(response, responseStream)
            };

            return output;
        }
    }
}
