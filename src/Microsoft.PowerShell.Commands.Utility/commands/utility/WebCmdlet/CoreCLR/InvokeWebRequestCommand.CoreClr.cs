// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.IO;
using System.Management.Automation;
using System.Net.Http;
using System.Threading;
using System.IO.Pipes;
using System.Text;
using System.Linq;

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

                responseStream = ro.RawContentStream;
                responseStream.Seek(0, SeekOrigin.Begin);
            }

            if (ShouldSaveToOutFile)
            {
                WriteVerbose($"File Name: {Path.GetFileName(outFilePath)}");
                StreamHelper.SaveStreamToFile(responseStream, outFilePath, this, response.Content.Headers.ContentRange?.Length.GetValueOrDefault() ?? response.Content.Headers.ContentLength.GetValueOrDefault(), perReadTimeout, _cancelToken.Token);
            }
        }

        internal override HttpResponseMessage GetResponse(HttpClient client, HttpRequestMessage request, bool handleRedirect)
        {
            Uri uri = request.RequestUri ?? throw new ArgumentNullException(nameof(request.RequestUri), "Request URI cannot be null.");

            if (uri.Scheme.Equals("npipe", StringComparison.OrdinalIgnoreCase))
            {
                TimeSpan timeout = ConvertTimeoutSecondsToTimeSpan(OperationTimeoutSeconds);
                return ProcessNamedPipeRequest(uri, request, timeout);
            }

            return base.GetResponse(client, request, handleRedirect);
        }

        private static HttpResponseMessage ProcessNamedPipeRequest(Uri uri, HttpRequestMessage request, TimeSpan timeout)
        {   
            string requestUrl = uri.ToString();
            string pipeName = requestUrl.Replace("npipe://", string.Empty);
            
            int index = pipeName.IndexOf("/");
            string leftSide = index != -1 ? pipeName.Substring(0, index) : pipeName; // Everything before '/'
            string rightSide = index != -1 ? pipeName.Substring(index) : string.Empty; // Everything after '/', including '/'
            pipeName = leftSide;
            string extractedPart = rightSide;

            StringBuilder responseBuilder = new StringBuilder();

            Console.WriteLine($"Detected Named Pipe Request: {pipeName}");

            try
            {
                using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut))
                {
                    client.Connect((int)timeout.TotalMilliseconds);
                    Console.WriteLine("Connected to Named Pipe.");

                    byte[] requestBytes = Encoding.UTF8.GetBytes($"GET {extractedPart} HTTP/1.1\r\nHost: pipe\r\n\r\n");
                    client.Write(requestBytes, 0, requestBytes.Length);
                    client.Flush();

                    using (StreamReader reader = new StreamReader(client))
                    {
                        char[] buffer = new char[1024];
                        int bytesRead;
                        DateTime startTime = DateTime.Now;

                        Console.WriteLine("Response from Named Pipe:");
                        while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            string chunk = new string(buffer, 0, bytesRead);
                            responseBuilder.Append(chunk);
                            Console.Write(chunk);

                            if (chunk.Contains("0\r\n\r\n"))
                            {
                                Console.WriteLine("\nPossible end of response detected.");
                                break;
                            }

                            // if ((DateTime.Now - startTime) > timeout)
                            // {
                            //     Console.WriteLine("\nResponse timeout reached. Stopping read.");
                            //     break;
                            // }

                            Thread.Sleep(100);
                        }
                        Console.WriteLine("\nDone reading response.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error communicating with named pipe `{pipeName}`: {ex.Message}");
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"Pipe communication failed: {ex.Message}")
                };
            }

            // Return a properly formatted HTTP response
            string responseText = responseBuilder.ToString();

            Console.WriteLine($"responseBuilder: {(responseBuilder == null ? "null" : "not null")}");
            Console.WriteLine($"responseBuilder.ToString(): {responseBuilder?.ToString().Length ?? -1} chars");

            if (string.IsNullOrEmpty(responseText))
            {
                Console.WriteLine("Warning: responseBuilder returned null or empty string.");
                responseText = "(empty response)";
            }

            var stringContent = new StringContent(responseText, Encoding.UTF8);

            var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = stringContent
            };

            if (responseMessage.Content?.Headers != null)
            {
                responseMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
                Console.WriteLine("Set content type");
            }
            else
            {
                Console.WriteLine("Warning: Content or Content.Headers was null.");
            }

            Console.WriteLine("Returning response");
            return responseMessage;
        }
        #endregion Virtual Method Overrides
    }
}
