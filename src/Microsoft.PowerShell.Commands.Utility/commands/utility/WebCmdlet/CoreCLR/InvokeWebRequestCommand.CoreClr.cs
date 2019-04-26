// Copyright (c) Microsoft Corporation. All rights reserved.
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
    [Cmdlet(VerbsLifecycle.Invoke, "WebRequest", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=217035", DefaultParameterSetName = "StandardMethod")]
    public class InvokeWebRequestCommand : WebRequestPSCmdlet
    {
        #region Virtual Method Overrides

        /// <summary>
        /// Default constructor for InvokeWebRequestCommand.
        /// </summary>
        public InvokeWebRequestCommand() : base()
        {
            this._parseRelLink = true;
        }

        /// <summary>
        /// Process the web response and output corresponding objects.
        /// </summary>
        /// <param name="response"></param>
        internal override void ProcessResponse(HttpResponseMessage response)
        {
            if (response == null) { throw new ArgumentNullException("response"); }

            Stream responseStream = StreamHelper.GetResponseStream(response);
            if (ShouldWriteToPipeline)
            {
                // creating a MemoryStream wrapper to response stream here to support IsStopping.
                responseStream = new WebResponseContentMemoryStream(responseStream, StreamHelper.ChunkSize, this);
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
                StreamHelper.SaveStreamToFile(responseStream, QualifiedOutFile, this);
            }
        }

        #endregion Virtual Method Overrides
    }
}
