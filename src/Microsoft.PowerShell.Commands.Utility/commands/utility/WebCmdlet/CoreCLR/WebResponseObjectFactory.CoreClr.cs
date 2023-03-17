// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.IO;
using System.Management.Automation;
using System.Net.Http;

namespace Microsoft.PowerShell.Commands
{
    internal static class WebResponseObjectFactory
    {
        internal static WebResponseObject GetResponseObject(HttpResponseMessage response, Stream responseStream, ExecutionContext executionContext)
        {
            WebResponseObject output = WebResponseHelper.IsText(response) ? new BasicHtmlWebResponseObject(response, responseStream) : new WebResponseObject(response, responseStream);

            return output;
        }
    }
}
