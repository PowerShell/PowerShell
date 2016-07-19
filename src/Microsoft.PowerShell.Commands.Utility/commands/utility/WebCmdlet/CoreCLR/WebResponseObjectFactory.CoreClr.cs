#if CORECLR

/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Net.Http;
using System.IO;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    internal static class WebResponseObjectFactory
    {
        internal static WebResponseObject GetResponseObject(HttpResponseMessage response, Stream responseStream, ExecutionContext executionContext, bool useBasicParsing = false)
        {
            WebResponseObject output;
            if (WebResponseHelper.IsText(response))
            {
                if (useBasicParsing)
                {
                    output = new BasicHtmlWebResponseObject(response, responseStream);
                }
                else
                {
                    output = new HtmlWebResponseObject(response, responseStream, executionContext);
                }
            }
            else
            {
                output = new WebResponseObject(response, responseStream);
            }
            return (output);
        }
    }
}
#endif