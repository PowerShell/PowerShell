#if CORECLR

/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Net.Http;
using System.IO;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Invoke-RestMethod command
    /// This command makes an HTTP or HTTPS request to a web server and returns the results.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "WebRequest", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=217035", DefaultParameterSetName = "StandardMethod")]
    public class InvokeWebRequestCommand : WebRequestPSCmdlet
    {
        #region Virtual Method Overrides

        /// <summary>
        /// Default constructor for InvokeWebRequestCommand
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
            if (null == response) { throw new ArgumentNullException("response"); }

            // check for Server Core, throws exception if -UseBasicParsing is not used
            if (ShouldWriteToPipeline && !UseBasicParsing)
            {
                // IE is not available in PS Linux, and may not available in PS Core depending on
                // where it's running (desktop/nano/iot).
                // For PS Linux and PS Core, if IE is not available, we always use basic parsing.
                if (!VerifyInternetExplorerAvailable(true))
                {
                    UseBasicParsing = true;
                }
            }

            Stream responseStream = StreamHelper.GetResponseStream(response);
            if (ShouldWriteToPipeline)
            {
                // creating a MemoryStream wrapper to response stream here to support IsStopping.
                responseStream = new WebResponseContentMemoryStream(responseStream, StreamHelper.ChunkSize, this);
                WebResponseObject ro = WebResponseObjectFactory.GetResponseObject(response, responseStream, this.Context, UseBasicParsing);
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
#endif