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
<<<<<<< HEAD
                output = new BasicHtmlWebResponseObject(response, responseStream);
=======
                if (useBasicParsing)
                {
                    output = new BasicHtmlWebResponseObject(response, responseStream);
                }
                else
                {
                    output = new HtmlWebResponseObject(response, responseStream, executionContext);
                }
>>>>>>> origin/source-depot
            }
            else
            {
                output = new WebResponseObject(response, responseStream);
            }

            return (output);
        }
    }
}
