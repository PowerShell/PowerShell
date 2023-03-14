// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Net.Http;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Invoke-WebRequest command.
    /// This command makes an HTTP or HTTPS request to a web server and returns the results.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "WebRequest", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097126", DefaultParameterSetName = "StandardMethod")]
    [OutputType(typeof(BasicHtmlWebResponseObject))]
    public class InvokeWebRequestCommand : WebRequestPSCmdlet
    {
        #region Virtual Method Overrides

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeWebRequestCommand"/> class.
        /// </summary>
        public InvokeWebRequestCommand() : base()
        {
            _parseRelLink = true;
        }

        /// <summary>
        /// Process the web response and output corresponding objects.
        /// </summary>
        /// <param name="response"></param>
        internal override void ProcessResponse(HttpResponseMessage response)
        {
            ArgumentNullException.ThrowIfNull(response);

            Stream responseStream = StreamHelper.GetResponseStream(response);
            if (ShouldWriteToPipeline)
            {
                // creating a MemoryStream wrapper to response stream here to support IsStopping.
                responseStream = new WebResponseContentMemoryStream(
                    responseStream,
                    StreamHelper.ChunkSize,
                    this,
                    response.Content.Headers.ContentLength.GetValueOrDefault());
                WebResponseObject ro = WebResponseObjectFactory.GetResponseObject(response, responseStream, this.Context);
                ro.RelationLink = _relationLink;
                WriteObject(ro);

                // use the rawcontent stream from WebResponseObject for further
                // processing of the stream. This is need because WebResponse's
                // stream can be used only once.
                responseStream = ro.RawContentStream;
                responseStream.Seek(0, SeekOrigin.Begin);
            }

            if (ShouldSaveToOutFile)
            {
                if (Directory.Exists(_qualifiedOutFile))
                {
                    // Get file name from last segment of Uri
                    _qualifiedOutFile = Path.Join(_qualifiedOutFile, System.Net.WebUtility.UrlDecode(response.RequestMessage.RequestUri.Segments[^1]));
                }

                WriteVerbose(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"File Name: {Path.GetFileName(_qualifiedOutFile)}"));

                StreamHelper.SaveStreamToFile(responseStream, _qualifiedOutFile, this, response.Content.Headers.ContentLength.GetValueOrDefault(), _cancelToken.Token);
            }
        }

        #endregion Virtual Method Overrides
    }
}
