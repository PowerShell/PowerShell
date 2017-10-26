/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Net;
using System.IO;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    internal static class WebResponseObjectFactory
    {
        internal static WebResponseObject GetResponseObject(WebResponse response, Stream responseStream, ExecutionContext executionContext, bool useBasicParsing = false)
        {
            WebResponseObject output;
            if (WebResponseHelper.IsText(response))
            {
                if (!useBasicParsing)
                {
                    output = new HtmlWebResponseObject(response, responseStream, executionContext);
                }
                else
                {
                    output = new BasicHtmlWebResponseObject(response, responseStream);
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