#if CORECLR

/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Net.Http;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Invoke-RestMethod command
    /// This command makes an HTTP or HTTPS request to a web service,
    /// and returns the response in an appropriate way.
    /// Intended to work against the wide spectrum of "RESTful" web services
    /// currently deployed across the web.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "RestMethod", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=217034", DefaultParameterSetName = "StandardMethod")]
    public partial class InvokeRestMethodCommand : WebRequestPSCmdlet
    {
        #region Virtual Method Overrides

        /// <summary>
        /// Process the web response and output corresponding objects.
        /// </summary>
        /// <param name="response"></param>
        internal override void ProcessResponse(HttpResponseMessage response)
        {
            if (null == response) { throw new ArgumentNullException("response"); }

            using (BufferingStreamReader responseStream = new BufferingStreamReader(StreamHelper.GetResponseStream(response)))
            {
                if (ShouldWriteToPipeline)
                {
                    // First see if it is an RSS / ATOM feed, in which case we can
                    // stream it - unless the user has overridden it with a return type of "XML"
                    if (TryProcessFeedStream(responseStream))
                    {
                        // Do nothing, content has been processed.
                    }
                    else
                    {
                        // determine the response type
                        RestReturnType returnType = CheckReturnType(response);

                        // Try to get the response encoding from the ContentType header.
                        Encoding encoding = null;
                        string charSet = response.Content.Headers.ContentType?.CharSet;
                        if (!string.IsNullOrEmpty(charSet))
                        {
                            // NOTE: Don't use ContentHelper.GetEncoding; it returns a
                            // default which bypasses checking for a meta charset value.
                            StreamHelper.TryGetEncoding(charSet, out encoding);
                        }

                        object obj = null;
                        Exception ex = null;

                        string str = StreamHelper.DecodeStream(responseStream, ref encoding);
                        // NOTE: Tests use this verbose output to verify the encoding.
                        WriteVerbose(string.Format
                        (
                            System.Globalization.CultureInfo.InvariantCulture,
                            "Content encoding: {0}",
                            string.IsNullOrEmpty(encoding.HeaderName) ? encoding.EncodingName : encoding.HeaderName)
                        );
                        bool convertSuccess = false;

                        if (returnType == RestReturnType.Json)
                        {
                            convertSuccess = TryConvertToJson(str, out obj, ref ex) || TryConvertToXml(str, out obj, ref ex);
                        }
                        // default to try xml first since it's more common
                        else
                        {
                            convertSuccess = TryConvertToXml(str, out obj, ref ex) || TryConvertToJson(str, out obj, ref ex);
                        }

                        if (!convertSuccess)
                        {
                            // fallback to string
                            obj = str;
                        }

                        WriteObject(obj);
                    }
                }

                if (ShouldSaveToOutFile)
                {
                    StreamHelper.SaveStreamToFile(responseStream, QualifiedOutFile, this);
                }

                if (!String.IsNullOrEmpty(ResponseHeadersVariable))
                {
                    PSVariableIntrinsics vi = SessionState.PSVariable;
                    vi.Set(ResponseHeadersVariable, WebResponseHelper.GetHeadersDictionary(response));
                }
            }
        }

        #endregion Virtual Method Overrides

        #region Helper Methods

        private RestReturnType CheckReturnType(HttpResponseMessage response)
        {
            if (null == response) { throw new ArgumentNullException("response"); }

            RestReturnType rt = RestReturnType.Detect;
            string contentType = ContentHelper.GetContentType(response);
            if (string.IsNullOrEmpty(contentType))
            {
                rt = RestReturnType.Detect;
            }
            else if (ContentHelper.IsJson(contentType))
            {
                rt = RestReturnType.Json;
            }
            else if (ContentHelper.IsXml(contentType))
            {
                rt = RestReturnType.Xml;
            }

            return (rt);
        }

        #endregion Helper Methods
    }
}
#endif