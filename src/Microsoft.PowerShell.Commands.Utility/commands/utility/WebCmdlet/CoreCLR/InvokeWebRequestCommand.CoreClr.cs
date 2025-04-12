// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Management.Automation;
using System.Net.Http;
using System.Net;
using System.Linq;
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

        #endregion Virtual Method Overrides

        #region Named Pipe Support

        /// <summary>
        /// Determines if the request should be handled as a named pipe request.
        /// </summary>
        /// <param name="client">The HttpClient to use.</param>
        /// <param name="request">The HttpRequestMessage to send.</param>
        /// <param name="handleRedirect">Whether to handle redirects.</param>
        /// <returns>The HttpResponseMessage.</returns>
        internal override HttpResponseMessage GetResponse(HttpClient client, HttpRequestMessage request, bool handleRedirect)
        {
            Uri uri = request.RequestUri ?? throw new ArgumentNullException(nameof(request.RequestUri), "Request URI cannot be null.");

            if (uri.Scheme.Equals("npipe", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessNamedPipeRequest(uri, request);
            }

            return base.GetResponse(client, request, handleRedirect);
        }

        /// <summary>
        /// Processes an HTTP-like request using Named Pipes.
        /// </summary>
        /// <param name="uri">The URI containing the named pipe path.</param>
        /// <param name="request">The HttpRequestMessage to send.</param>
        /// <returns>An HttpResponseMessage containing the response.</returns>
        private HttpResponseMessage ProcessNamedPipeRequest(Uri uri, HttpRequestMessage request)
        {
            // Retrieve named pipe name from the URI path.
            string pipeName = uri.AbsolutePath.TrimStart('/');
            Console.WriteLine($"[DEBUG] Processing Named Pipe request for pipe: {pipeName}");

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut))
            {
                try
                {
                    Console.WriteLine("[DEBUG] Attempting to connect to the named pipe...");
                    pipeClient.Connect();
                    Console.WriteLine("[DEBUG] Connected to the named pipe server.");

                    // Build and send the HTTP-like request string.
                    string requestString = BuildHttpRequestString(request);
                    Console.WriteLine($"[DEBUG] Sending request string:\n{requestString}");
                    byte[] requestBytes = Encoding.UTF8.GetBytes(requestString);
                    pipeClient.Write(requestBytes, 0, requestBytes.Length);
                    Console.WriteLine("[DEBUG] Request sent to the named pipe server.");

                    // Read response from named pipe.
                    byte[] buffer = new byte[4096]; // Buffer for bigger responses.
                    int bytesRead = pipeClient.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("[ERROR] No data received from the named pipe server.");
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            Content = new StringContent("[ERROR] No response received.")
                        };
                    }

                    string responseString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"[DEBUG] Received response string:\n{responseString}");

                    // Build HttpResponseMessage from raw response string.
                    HttpResponseMessage responseMessage = BuildHttpResponse(responseString);
                    Console.WriteLine("[DEBUG] Passing response to ProcessResponse...");
                    if (responseMessage == null)
                    {
                        throw new ArgumentException("responeseMessage was processed incorrectly before");
                    }
                    ProcessResponse(responseMessage);

                    if (responseMessage == null)
                    {
                        throw new ArgumentException("responeseMessage was processed incorrectly after");
                    }

                    Console.WriteLine("Returning responseMessage");
                    return responseMessage;
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"[ERROR] IOException while interacting with the named pipe: {ex.Message}");
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent($"[ERROR] Connection error: {ex.Message}")
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Unexpected error: {ex.Message}");
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent($"[ERROR] Unexpected error: {ex.Message}")
                    };
                }
            }
        }

        // Helper method to build a basic HTTP-like request string.
        private static string BuildHttpRequestString(HttpRequestMessage request)
        {
            // Build a simple HTTP request string based on the HttpRequestMessage.
            StringBuilder requestString = new StringBuilder();
            if (request.Method == null)
            {
                throw new ArgumentException("Request method must not be null");
            }
            
            if (request.RequestUri?.AbsolutePath == null)
            {
                throw new ArgumentException("RequestUri Absolute Path must not be null");
            }

            requestString.AppendLine($"{request.Method} {request.RequestUri.AbsolutePath} HTTP/1.1");
            requestString.AppendLine($"Host: {request.RequestUri.Host}");
            
            // Add headers
            foreach (var header in request.Headers)
            {
                foreach (var value in header.Value)
                {
                    requestString.AppendLine($"{header.Key}: {value}");
                }
            }

            // Add empty line to separate headers from the body (HTTP convention)
            requestString.AppendLine();

            // Add body if present
            if (request.Content != null)
            {
                var body = request.Content.ReadAsStringAsync().Result;
                requestString.AppendLine(body);
            }

            return requestString.ToString();
        }

        // Helper method to build an HttpResponseMessage from the response string.
        private static HttpResponseMessage BuildHttpResponse(string responseString)
        {
            Console.WriteLine($"[DEBUG] Building HttpResponseMessage from response string:\n{responseString}");

            var responseMessage = new HttpResponseMessage();

            // Split response into lines.
            string[] responseLines = responseString.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // Parse status line (e.g., HTTP/1.1 200 OK).
            if (responseLines.Length > 0)
            {
                string statusLine = responseLines[0];
                string[] statusParts = statusLine.Split(' ');

                if (statusParts.Length >= 2)
                {
                    responseMessage.StatusCode = Enum.Parse<HttpStatusCode>(statusParts[1]);
                    responseMessage.ReasonPhrase = statusParts.Length > 2 ? statusParts[2] : string.Empty;
                }
                else
                {
                    throw new FormatException("Invalid status line format.");
                }
            }

            // Parse headers.
            int emptyLineIndex = Array.IndexOf(responseLines, string.Empty);
            for (int i = 1; i < emptyLineIndex; i++) // Headers end before the empty line.
            {
                string headerLine = responseLines[i];
                var headerParts = headerLine.Split(':', 2);
                if (headerParts.Length == 2)
                {
                    responseMessage.Headers.TryAddWithoutValidation(headerParts[0].Trim(), headerParts[1].Trim());
                }
            }

            // Parse body.
            if (emptyLineIndex != -1 && emptyLineIndex + 1 < responseLines.Length)
            {
                string body = string.Join("\n", responseLines[(emptyLineIndex + 1)..]);
                responseMessage.Content = new StringContent(body);
            }
            else
            {
                responseMessage.Content = new StringContent(string.Empty);
            }

            return responseMessage;
        }

        #endregion Named Pipe Support
    }
}
