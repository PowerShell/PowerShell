// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.IO;
using System.Management.Automation;
using System.Net;
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
            WriteVerbose($"Response Content-Type: {response.Content.Headers.ContentType}");
            WriteVerbose($"Response Content-Length: {response.Content.Headers.ContentLength}");

            TimeSpan perReadTimeout = ConvertTimeoutSecondsToTimeSpan(OperationTimeoutSeconds);
            Console.WriteLine("Got perReadTimeout");
            if (response.Content == null)
            {
                throw new InvalidOperationException("Response content is null.");
            }

            if (response.Content == null)
            {
                throw new InvalidOperationException("Response content is null.");
            }

            if (response.Content.Headers.ContentType == null)
            {
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            }

            if (!response.Content.Headers.ContentLength.HasValue)
            {
                response.Content.Headers.ContentLength = response.Content.ReadAsByteArrayAsync().Result.Length;
            }

            try
            {
                using var testStream = response.Content.ReadAsStream();
                Console.WriteLine("Stream test passed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReadAsStream failed: {ex.Message}");
            }

            Stream responseStream = StreamHelper.GetResponseStream(response, _cancelToken.Token);
            Console.WriteLine("got response stream");
            string outFilePath = WebResponseHelper.GetOutFilePath(response, _qualifiedOutFile);
            Console.WriteLine("outfilepath set");

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
            Console.WriteLine("ProcessResponse succeed with no errors");
        }

        internal override HttpResponseMessage GetResponse(HttpClient client, HttpRequestMessage request, bool handleRedirect)
        {
            Uri uri = request.RequestUri ?? throw new ArgumentNullException(nameof(request.RequestUri), "Request URI cannot be null.");

            if (uri.Scheme.Equals("npipe", StringComparison.OrdinalIgnoreCase))
            {
                TimeSpan timeout = ConvertTimeoutSecondsToTimeSpan(OperationTimeoutSeconds);
                var response = ProcessNamedPipeRequest(uri, request, timeout);

                // Force PowerShell pipeline to process the response
                ProcessResponse(response);

                // Do *not* return this response — PowerShell has already handled it
                return null!;
            }

            return base.GetResponse(client, request, handleRedirect);
        }

        private static HttpResponseMessage ProcessNamedPipeRequest(Uri uri, HttpRequestMessage request, TimeSpan timeout)
        {
            string requestUrl = uri.ToString();
            string pipeName = requestUrl.Replace("npipe://", string.Empty);

            int index = pipeName.IndexOf("/");
            string leftSide = index != -1 ? pipeName.AsSpan(0, index).ToString() : pipeName;
            string rightSide = index != -1 ? pipeName.AsSpan(index).ToString() : string.Empty;
            pipeName = leftSide;
            string extractedPart = rightSide;

            StringBuilder responseBuilder = new StringBuilder();

            Console.WriteLine($"Connected to Named Pipe: {pipeName}");

            try
            {
                using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut))
                {
                    client.Connect((int)timeout.TotalMilliseconds);
                    Console.WriteLine("Connected to Named Pipe.");
                    Console.WriteLine("Reading response...");

                    byte[] requestBytes = Encoding.UTF8.GetBytes($"GET {extractedPart} HTTP/1.1\r\nHost: pipe\r\n\r\n");
                    client.Write(requestBytes, 0, requestBytes.Length);
                    client.Flush();

                    using StreamReader reader = new StreamReader(client, Encoding.UTF8);
                    char[] buffer = new char[1024];
                    int bytesRead;
                    DateTime startTime = DateTime.Now;

                    while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        string chunk = new string(buffer, 0, bytesRead);
                        responseBuilder.Append(chunk);
                        Console.Write(chunk);

                        if (chunk.Contains("0\r\n\r\n"))
                        {
                            Console.WriteLine("\nEnd of chunked response detected.");
                            break;
                        }

                        Thread.Sleep(100);
                    }

                    Console.WriteLine("Done reading response.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error communicating with named pipe `{pipeName}`: {ex.Message}");
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"Pipe communication failed: {ex.Message}", Encoding.UTF8, "text/plain")
                };
            }

            // Now parse the full raw HTTP response
            string raw = responseBuilder.ToString();
            int bodyIndex = raw.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (bodyIndex == -1)
            {
                return new HttpResponseMessage(HttpStatusCode.BadGateway)
                {
                    Content = new StringContent("Malformed response: missing headers", Encoding.UTF8, "text/plain")
                };
            }

            string headers = raw.Substring(0, bodyIndex);
            string body = raw.Substring(bodyIndex + 4);

            // If chunked, dechunk it
            if (headers.Contains("Transfer-Encoding: chunked", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Parsing response...");
                body = DechunkHttpBody(body);
            }

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            var content = new StreamContent(stream);

            // Parse Content-Type header from raw headers if available
            string? contentType = headers.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(h => h.StartsWith("Content-Type:", StringComparison.OrdinalIgnoreCase))?
                .Split(":", 2)[1].Trim();

            if (!string.IsNullOrEmpty(contentType))
            {
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            }
            else
            {
                // Default if not found
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            }

            content.Headers.ContentLength = stream.Length;

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            };

            Console.WriteLine("Returning clean HttpResponseMessage.");
            return response;
        }

        private static string DechunkHttpBody(string chunkedBody)
        {
            ReadOnlySpan<char> span = chunkedBody.AsSpan();
            StringBuilder result = new StringBuilder();
            int i = 0;

            while (i < span.Length)
            {
                int crlf = span.Slice(i).IndexOf("\r\n");
                if (crlf == -1) break;

                ReadOnlySpan<char> chunkSizeSpan = span.Slice(i, crlf);
                if (!int.TryParse(chunkSizeSpan, System.Globalization.NumberStyles.HexNumber, null, out int chunkSize))
                    break;

                i += crlf + 2; // skip past chunk size line and CRLF

                if (chunkSize == 0 || (i + chunkSize > span.Length)) break;

                result.Append(span.Slice(i, chunkSize));
                i += chunkSize + 2; // move past chunk + CRLF
            }

            return result.ToString();
        }
        #endregion Virtual Method Overrides
    }
}
