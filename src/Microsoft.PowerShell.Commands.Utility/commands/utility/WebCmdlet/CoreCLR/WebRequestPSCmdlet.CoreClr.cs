#if CORECLR

/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading;
using System.Xml;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Base class for Invoke-RestMethod and Invoke-WebRequest commands.
    /// </summary>
    public abstract partial class WebRequestPSCmdlet : PSCmdlet
    {
        #region Abstract Methods

        /// <summary>
        /// Read the supplied WebResponse object and push the 
        /// resulting output into the pipeline.
        /// </summary>
        /// <param name="response">Instance of a WebResponse object to be processed</param>
        internal abstract void ProcessResponse(HttpResponseMessage response);

        #endregion Abstract Methods

        /// <summary>
        /// Cancellation token source
        /// </summary>
        private CancellationTokenSource _cancelToken = null;

        private HttpMethod GetHttpMethod(WebRequestMethod method)
        {
            switch (Method)
            {
                case WebRequestMethod.Default:
                case WebRequestMethod.Get:
                    return HttpMethod.Get;
                case WebRequestMethod.Head:
                    return HttpMethod.Head;
                case WebRequestMethod.Post:
                    return HttpMethod.Post;
                case WebRequestMethod.Put:
                    return HttpMethod.Put;
                case WebRequestMethod.Delete:
                    return HttpMethod.Delete;
                case WebRequestMethod.Trace:
                    return HttpMethod.Trace;
                case WebRequestMethod.Options:
                    return HttpMethod.Options;
                default:
                    // Merge and Patch
                    return new HttpMethod(Method.ToString().ToUpperInvariant());
            }
        }

        #region Virtual Methods

        internal virtual HttpClient GetHttpClient()
        {
            // By default the HttpClientHandler will automatically decompress GZip and Deflate content
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = WebSession.Cookies;

            // set the credentials used by this request
            if (WebSession.UseDefaultCredentials)
            {
                // the UseDefaultCredentials flag overrides other supplied credentials
                handler.UseDefaultCredentials = true;
            }
            else if (WebSession.Credentials != null)
            {
                handler.Credentials = WebSession.Credentials;
            }

            if (WebSession.Proxy != null)
            {
                handler.Proxy = WebSession.Proxy;
            }

            /*
            TODO: HttpClientHandler will support client certificate in RTM
            See https://github.com/dotnet/corefx/issues/7623 for more details.
            if (null != WebSession.Certificates)
            {
                handler.ClientCertificates = WebSession.Certificates;
            }*/

            if (WebSession.MaximumRedirection > -1)
            {
                if (WebSession.MaximumRedirection == 0)
                {
                    handler.AllowAutoRedirect = false;
                }
                else
                {
                    handler.MaxAutomaticRedirections = WebSession.MaximumRedirection;
                }
            }

            HttpClient httpClient = new HttpClient(handler);

            // check timeout setting (in seconds instead of milliseconds as in HttpWebRequest)
            if (TimeoutSec == 0)
            {
                // A zero timeout means infinite
                httpClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
            }
            else if (TimeoutSec > 0)
            {
                httpClient.Timeout = new TimeSpan(0, 0, TimeoutSec);
            }

            return httpClient;
        }

        internal virtual HttpRequestMessage GetRequest(Uri uri)
        {
            Uri requestUri = PrepareUri(uri);
            HttpMethod httpMethod = GetHttpMethod(Method);

            // create the base WebRequest object
            var request = new HttpRequestMessage(httpMethod, requestUri);

            // pull in session data
            if (WebSession.Headers.Count > 0)
            {
                WebSession.ContentHeaders.Clear();
                foreach (var entry in WebSession.Headers)
                {
                    if (HttpKnownHeaderNames.ContentHeaders.Contains(entry.Key))
                    {
                        WebSession.ContentHeaders.Add(entry.Key, entry.Value);
                    }
                    else
                    {
                        request.Headers.Add(entry.Key, entry.Value);
                    }
                }
            }

            // Set 'Transfer-Encoding: chunked' if 'Transfer-Encoding' is specified
            if (WebSession.Headers.ContainsKey(HttpKnownHeaderNames.TransferEncoding))
            {
                request.Headers.TransferEncodingChunked = true;
            }

            // Set 'User-Agent' if WebSession.Headers doesn't already contain it
            string userAgent = null;
            if (WebSession.Headers.TryGetValue(HttpKnownHeaderNames.UserAgent, out userAgent))
            {
                WebSession.UserAgent = userAgent;
            }
            else
            {
                request.Headers.Add(HttpKnownHeaderNames.UserAgent, WebSession.UserAgent);
            }

            // Set 'Keep-Alive' to false. This means set the Connection to 'Close'.
            if (DisableKeepAlive)
            {
                request.Headers.Add(HttpKnownHeaderNames.Connection, "Close");
            }

            // Set 'Transfer-Encoding'
            if (TransferEncoding != null)
            {
                request.Headers.TransferEncodingChunked = true;
                var headerValue = new TransferCodingHeaderValue(TransferEncoding);
                if (!request.Headers.TransferEncoding.Contains(headerValue))
                {
                    request.Headers.TransferEncoding.Add(headerValue);
                }
            }

            // Some web sites (e.g. Twitter) will return exception on POST when Expect100 is sent
            // Default behaviour is continue to send body content anyway after a short period
            // Here it send the two part as a whole. 
            request.Headers.ExpectContinue = false;

            return (request);
        }

        internal virtual void FillRequestStream(HttpRequestMessage request)
        {
            if (null == request) { throw new ArgumentNullException("request"); }

            // set the content type
            if (ContentType != null)
            {
                WebSession.ContentHeaders[HttpKnownHeaderNames.ContentType] = ContentType;
                //request
            }
            // ContentType == null
            else if (Method == WebRequestMethod.Post)
            {
                // Win8:545310 Invoke-WebRequest does not properly set MIME type for POST
                string contentType = null;
                WebSession.ContentHeaders.TryGetValue(HttpKnownHeaderNames.ContentType, out contentType);
                if (string.IsNullOrEmpty(contentType))
                {
                    WebSession.ContentHeaders[HttpKnownHeaderNames.ContentType] = "application/x-www-form-urlencoded";
                }
            }

            // coerce body into a usable form
            if (Body != null)
            {
                object content = Body;

                // make sure we're using the base object of the body, not the PSObject wrapper
                PSObject psBody = Body as PSObject;
                if (psBody != null)
                {
                    content = psBody.BaseObject;
                }

                /* TODO: This needs to be enable after the dependency on mshtml is resolved.
                var html = content as HtmlWebResponseObject;
                if (html != null)
                {
                    // use the form if it's the only one present
                    if (html.Forms.Count == 1)
                    {
                        SetRequestContent(request, html.Forms[0].Fields);
                    }
                }
                else if (content is FormObject)
                */

                if (content is FormObject)
                {
                    FormObject form = content as FormObject;
                    SetRequestContent(request, form.Fields);
                }
                else if (content is IDictionary && request.Method != HttpMethod.Get)
                {
                    IDictionary dictionary = content as IDictionary;
                    SetRequestContent(request, dictionary);
                }
                else if (content is XmlNode)
                {
                    XmlNode xmlNode = content as XmlNode;
                    SetRequestContent(request, xmlNode);
                }
                else if (content is Stream)
                {
                    Stream stream = content as Stream;
                    SetRequestContent(request, stream);
                }
                else if (content is byte[])
                {
                    byte[] bytes = content as byte[];
                    SetRequestContent(request, bytes);
                }
                else
                {
                    SetRequestContent(request,
                        (string)LanguagePrimitives.ConvertTo(content, typeof(string), CultureInfo.InvariantCulture));
                }
            }
            else if (InFile != null) // copy InFile data
            {
                try
                {
                    // open the input file
                    using (FileStream fs = new FileStream(InFile, FileMode.Open))
                    {
                        SetRequestContent(request, fs);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture, WebCmdletStrings.AccessDenied,
                                               _originalFilePath);
                    throw new UnauthorizedAccessException(msg);
                }
            }

            // Add the content headers
            if (request.Content != null)
            {
                foreach (var entry in WebSession.ContentHeaders)
                {
                    request.Content.Headers.Add(entry.Key, entry.Value);
                }
            }
        }

        internal virtual HttpResponseMessage GetResponse(HttpClient client, HttpRequestMessage request)
        {
            if (client == null) { throw new ArgumentNullException("client"); }
            if (request == null) { throw new ArgumentNullException("request"); }

            _cancelToken = new CancellationTokenSource();
            return client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cancelToken.Token).GetAwaiter().GetResult();
        }

        internal virtual void UpdateSession(HttpResponseMessage response)
        {
            if (response == null) { throw new ArgumentNullException("response"); }
        }

        #endregion Virtual Methods

        #region Overrides

        /// <summary>
        /// the main execution method for cmdlets derived from WebRequestPSCmdlet. 
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                // Set cmdlet context for write progress
                ValidateParameters();
                PrepareSession();
                HttpClient client = GetHttpClient();
                HttpRequestMessage request = GetRequest(Uri);
                FillRequestStream(request);

                try
                {
                    long requestContentLength = 0;
                    if (request.Content != null)
                        requestContentLength = request.Content.Headers.ContentLength.Value;

                    string reqVerboseMsg = String.Format(CultureInfo.CurrentCulture,
                        "{0} {1} with {2}-byte payload",
                        request.Method,
                        request.RequestUri,
                        requestContentLength);
                    WriteVerbose(reqVerboseMsg);

                    HttpResponseMessage response = GetResponse(client, request);
                    response.EnsureSuccessStatusCode();

                    string contentType = ContentHelper.GetContentType(response);
                    string respVerboseMsg = string.Format(CultureInfo.CurrentCulture,
                        "received {0}-byte response of content type {1}",
                        response.Content.Headers.ContentLength,
                        contentType);
                    WriteVerbose(respVerboseMsg);
                    ProcessResponse(response);
                    UpdateSession(response);

                    // If we hit our maxium redirection count, generate an error.
                    // Errors with redirection counts of greater than 0 are handled automatically by .NET, but are
                    // impossible to detect programmatically when we hit this limit. By handling this ourselves
                    // (and still writing out the result), users can debug actual HTTP redirect problems.
                    if (WebSession.MaximumRedirection == 0) // Indicate "HttpClientHandler.AllowAutoRedirect == false"
                    {
                        if (response.StatusCode == HttpStatusCode.Found ||
                            response.StatusCode == HttpStatusCode.Moved ||
                            response.StatusCode == HttpStatusCode.MovedPermanently)
                        {
                            ErrorRecord er = new ErrorRecord(new InvalidOperationException(), "MaximumRedirectExceeded", ErrorCategory.InvalidOperation, request);
                            er.ErrorDetails = new ErrorDetails(WebCmdletStrings.MaximumRedirectionCountExceeded);
                            WriteError(er);
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    ErrorRecord er = new ErrorRecord(ex, "WebCmdletWebResponseException", ErrorCategory.InvalidOperation, request);
                    ThrowTerminatingError(er);
                }
            }
            catch (CryptographicException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "WebCmdletCertificateException", ErrorCategory.SecurityError, null);
                ThrowTerminatingError(er);
            }
            catch (NotSupportedException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "WebCmdletIEDomNotSupportedException", ErrorCategory.NotImplemented, null);
                ThrowTerminatingError(er);
            }
        }

        /// <summary>
        /// Implementing ^C, after start the BeginGetResponse
        /// </summary>
        protected override void StopProcessing()
        {
            if (_cancelToken != null)
            {
                _cancelToken.Cancel();
            }
        }

        #endregion Overrides

        #region Helper Methods

        /// <summary>
        /// Sets the ContentLength property of the request and writes the specified content to the request's RequestStream.
        /// </summary>
        /// <param name="request">The WebRequest who's content is to be set</param>
        /// <param name="content">A byte array containing the content data.</param>
        /// <returns>The number of bytes written to the requests RequestStream (and the new value of the request's ContentLength property</returns>
        /// <remarks>
        /// Because this function sets the request's ContentLength property and writes content data into the requests's stream,
        /// it should be called one time maximum on a given request.
        /// </remarks>
        internal long SetRequestContent(HttpRequestMessage request, Byte[] content)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (content == null)
                return 0;

            var byteArrayContent = new ByteArrayContent(content);
            request.Content = byteArrayContent;

            return byteArrayContent.Headers.ContentLength.Value;
        }

        /// <summary>
        /// Sets the ContentLength property of the request and writes the specified content to the request's RequestStream.
        /// </summary>
        /// <param name="request">The WebRequest who's content is to be set</param>
        /// <param name="content">A String object containing the content data.</param>
        /// <returns>The number of bytes written to the requests RequestStream (and the new value of the request's ContentLength property</returns>
        /// <remarks>
        /// Because this function sets the request's ContentLength property and writes content data into the requests's stream,
        /// it should be called one time maximum on a given request.
        /// </remarks>
        internal long SetRequestContent(HttpRequestMessage request, string content)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (content == null)
                return 0;

            Encoding encoding = null;
            if (ContentType != null)
            {
                // If Content-Type contains the encoding format (as CharSet), use this encoding format 
                // to encode the Body of the WebRequest sent to the server. Default Encoding format 
                // would be used if Charset is not supplied in the Content-Type property.
                var mediaTypeHeaderValue = new MediaTypeHeaderValue(ContentType);
                if (!string.IsNullOrEmpty(mediaTypeHeaderValue.CharSet))
                {
                    try
                    {
                        encoding = Encoding.GetEncoding(mediaTypeHeaderValue.CharSet);
                    }
                    catch (ArgumentException ex)
                    {
                        ErrorRecord er = new ErrorRecord(ex, "WebCmdletEncodingException", ErrorCategory.InvalidArgument, ContentType);
                        ThrowTerminatingError(er);
                    }
                }
            }

            Byte[] bytes = StreamHelper.EncodeToBytes(content, encoding);
            var byteArrayContent = new ByteArrayContent(bytes);
            request.Content = byteArrayContent;

            return byteArrayContent.Headers.ContentLength.Value;
        }

        internal long SetRequestContent(HttpRequestMessage request, XmlNode xmlNode)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (xmlNode == null)
                return 0;

            Byte[] bytes = null;
            XmlDocument doc = xmlNode as XmlDocument;
            if (doc != null && (doc.FirstChild as XmlDeclaration) != null)
            {
                XmlDeclaration decl = doc.FirstChild as XmlDeclaration;
                Encoding encoding = Encoding.GetEncoding(decl.Encoding);
                bytes = StreamHelper.EncodeToBytes(doc.OuterXml, encoding);
            }
            else
            {
                bytes = StreamHelper.EncodeToBytes(xmlNode.OuterXml);
            }

            var byteArrayContent = new ByteArrayContent(bytes);
            request.Content = byteArrayContent;

            return byteArrayContent.Headers.ContentLength.Value;
        }

        /// <summary>
        /// Sets the ContentLength property of the request and writes the specified content to the request's RequestStream.
        /// </summary>
        /// <param name="request">The WebRequest who's content is to be set</param>
        /// <param name="contentStream">A Stream object containing the content data.</param>
        /// <returns>The number of bytes written to the requests RequestStream (and the new value of the request's ContentLength property</returns>
        /// <remarks>
        /// Because this function sets the request's ContentLength property and writes content data into the requests's stream,
        /// it should be called one time maximum on a given request.
        /// </remarks>
        internal long SetRequestContent(HttpRequestMessage request, Stream contentStream)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (contentStream == null)
                throw new ArgumentNullException("contentStream");

            var streamContent = new StreamContent(contentStream);
            request.Content = streamContent;

            return streamContent.Headers.ContentLength.Value;
        }

        internal long SetRequestContent(HttpRequestMessage request, IDictionary content)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (content == null)
                throw new ArgumentNullException("content");

            string body = FormatDictionary(content);
            return (SetRequestContent(request, body));

        }

        #endregion Helper Methods
    }
}
#endif