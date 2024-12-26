// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.IO;
using System.Management.Automation;
using System.Net.Http;
using System.Threading;

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
            TimeSpan perReadTimeout = ConvertTimeoutSecondsToTimeSpan(OperationTimeoutSeconds);
            Stream responseStream = StreamHelper.GetResponseStream(response, _cancelToken.Token);
            string outFilePath = WebResponseHelper.GetOutFilePath(response, _qualifiedOutFile);

            if (ShouldWriteToPipeline)
            {
                // Creating a MemoryStream wrapper to response stream here to support IsStopping.
                responseStream = new WebResponseContentMemoryStream(
                    responseStream,
                    StreamHelper.ChunkSize,
                    this,
                    response.Content.Headers.ContentLength.GetValueOrDefault(),
                    perReadTimeout,
                    _cancelToken.Token);
                WebResponseObject ro = WebResponseHelper.IsText(response) ? new BasicHtmlWebResponseObject(response, responseStream, perReadTimeout, _cancelToken.Token) : new WebResponseObject(response, responseStream, perReadTimeout, _cancelToken.Token);
                ro.RelationLink = _relationLink;
                ro.OutFile = outFilePath;
                WriteObject(ro);

                // Use the rawcontent stream from WebResponseObject for further
                // processing of the stream. This is need because WebResponse's
                // stream can be used only once.
                responseStream = ro.RawContentStream;
                responseStream.Seek(0, SeekOrigin.Begin);
            }

            if (ShouldSaveToOutFile)
            {
                WriteVerbose($"File Name: {Path.GetFileName(outFilePath)}");

                // ContentLength is always the partial length, while ContentRange is the full length
                // Without Request.Range set, ContentRange is null and partial length (ContentLength) equals to full length
                StreamHelper.SaveStreamToFile(responseStream, outFilePath, this, response.Content.Headers.ContentRange?.Length.GetValueOrDefault() ?? response.Content.Headers.ContentLength.GetValueOrDefault(), perReadTimeout, _cancelToken.Token);
            }
        }

        #endregion Virtual Method Overrides
    }
}
