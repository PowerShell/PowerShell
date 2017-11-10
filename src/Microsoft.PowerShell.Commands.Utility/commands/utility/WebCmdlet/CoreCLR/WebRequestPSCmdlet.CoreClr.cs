/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
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
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Exception class for webcmdlets to enable returning HTTP error response
    /// </summary>
    public sealed class HttpResponseException : HttpRequestException
    {
        /// <summary>
        /// Constructor for HttpResponseException
        /// </summary>
        /// <param name="message">Message for the exception</param>
        /// <param name="response">Response from the HTTP server</param>
        public HttpResponseException (string message, HttpResponseMessage response) : base(message)
        {
            Response = response;
        }

        /// <summary>
        /// HTTP error response
        /// </summary>
        public HttpResponseMessage Response { get; private set; }
    }

    /// <summary>
    /// Base class for Invoke-RestMethod and Invoke-WebRequest commands.
    /// </summary>
    public abstract partial class WebRequestPSCmdlet : PSCmdlet
    {

        /// <summary>
        /// gets or sets the PreserveAuthorizationOnRedirect property
        /// </summary>
        /// <remarks>
        /// This property overrides compatibility with web requests on Windows.
        /// On FullCLR (WebRequest), authorization headers are stripped during redirect.
        /// CoreCLR (HTTPClient) does not have this behavior so web requests that work on
        /// PowerShell/FullCLR can fail with PowerShell/CoreCLR.  To provide compatibility,
        /// we'll detect requests with an Authorization header and automatically strip
        /// the header when the first redirect occurs. This switch turns off this logic for
        /// edge cases where the authorization header needs to be preserved across redirects.
        /// </remarks>
        [Parameter]
        public virtual SwitchParameter PreserveAuthorizationOnRedirect { get; set; }

        /// <summary>
        /// gets or sets the SkipHeaderValidation property
        /// </summary>
        /// <remarks>
        /// This property adds headers to the request's header collection without validation.
        /// </remarks>
        [Parameter]
        public virtual SwitchParameter SkipHeaderValidation { get; set; }

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

        /// <summary>
        /// Parse Rel Links
        /// </summary>
        internal bool _parseRelLink = false;

        /// <summary>
        /// Automatically follow Rel Links
        /// </summary>
        internal bool _followRelLink = false;

        /// <summary>
        /// Automatically follow Rel Links
        /// </summary>
        internal Dictionary<string, string> _relationLink = null;

        /// <summary>
        /// Maximum number of Rel Links to follow
        /// </summary>
        internal int _maximumFollowRelLink = Int32.MaxValue;

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

        // NOTE: Only pass true for handleRedirect if the original request has an authorization header
        // and PreserveAuthorizationOnRedirect is NOT set.
        internal virtual HttpClient GetHttpClient(bool handleRedirect)
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

            if (NoProxy)
            {
                handler.UseProxy = false;
            }
            else if (WebSession.Proxy != null)
            {
                handler.Proxy = WebSession.Proxy;
            }

            if (null != WebSession.Certificates)
            {
                handler.ClientCertificates.AddRange(WebSession.Certificates);
            }

            if (SkipCertificateCheck)
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            }

            // This indicates GetResponse will handle redirects.
            if (handleRedirect)
            {
                handler.AllowAutoRedirect = false;
            }
            else if (WebSession.MaximumRedirection > -1)
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

        internal virtual HttpRequestMessage GetRequest(Uri uri, bool stripAuthorization)
        {
            Uri requestUri = PrepareUri(uri);
            HttpMethod httpMethod = null;

            switch (ParameterSetName)
            {
                case "StandardMethodNoProxy":
                    goto case "StandardMethod";
                case "StandardMethod":
                    // set the method if the parameter was provided
                    httpMethod = GetHttpMethod(Method);
                    break;
                case "CustomMethodNoProxy":
                    goto case "CustomMethod";
                case "CustomMethod":
                    if (!string.IsNullOrEmpty(CustomMethod))
                    {
                        // set the method if the parameter was provided
                        httpMethod = new HttpMethod(CustomMethod.ToString().ToUpperInvariant());
                    }
                    break;
            }

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
                        if (stripAuthorization
                            &&
                            String.Equals(entry.Key, HttpKnownHeaderNames.Authorization.ToString(), StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            continue;
                        }

                        if (SkipHeaderValidation)
                        {
                            request.Headers.TryAddWithoutValidation(entry.Key, entry.Value);
                        }
                        else
                        {
                            request.Headers.Add(entry.Key, entry.Value);
                        }
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
                if (SkipHeaderValidation)
                {
                    request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.UserAgent, WebSession.UserAgent);
                }
                else
                {
                    request.Headers.Add(HttpKnownHeaderNames.UserAgent, WebSession.UserAgent);
                }

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
            // Default behavior is continue to send body content anyway after a short period
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
            else if (Method == WebRequestMethod.Post || (IsCustomMethodSet() && CustomMethod.ToUpperInvariant() == "POST"))
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
                else if (content is MultipartFormDataContent multipartFormDataContent)
                {
                    WebSession.ContentHeaders.Clear();
                    SetRequestContent(request, multipartFormDataContent);
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
                    SetRequestContent(request, new FileStream(InFile, FileMode.Open));
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

        // Returns true if the status code is one of the supported redirection codes.
        static bool IsRedirectCode(HttpStatusCode code)
        {
            int intCode = (int) code;
            return
            (
                (intCode >= 300 && intCode < 304)
                ||
                intCode == 307
            );
        }

        // Returns true if the status code is a redirection code and the action requires switching from POST to GET on redirection.
        // NOTE: Some of these status codes map to the same underlying value but spelling them out for completeness.
        static bool IsRedirectToGet(HttpStatusCode code)
        {
            return
            (
                code == HttpStatusCode.Found
                ||
                code == HttpStatusCode.Moved
                ||
                code == HttpStatusCode.Redirect
                ||
                code == HttpStatusCode.RedirectMethod
                ||
                code == HttpStatusCode.TemporaryRedirect
                ||
                code == HttpStatusCode.RedirectKeepVerb
                ||
                code == HttpStatusCode.SeeOther
            );
        }

        internal virtual HttpResponseMessage GetResponse(HttpClient client, HttpRequestMessage request, bool stripAuthorization)
        {
            if (client == null) { throw new ArgumentNullException("client"); }
            if (request == null) { throw new ArgumentNullException("request"); }

            _cancelToken = new CancellationTokenSource();
            HttpResponseMessage response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cancelToken.Token).GetAwaiter().GetResult();

            if (stripAuthorization && IsRedirectCode(response.StatusCode))
            {
                _cancelToken.Cancel();
                _cancelToken = null;

                // if explicit count was provided, reduce it for this redirection.
                if (WebSession.MaximumRedirection > 0)
                {
                    WebSession.MaximumRedirection--;
                }
                // For selected redirects that used POST, GET must be used with the
                // redirected Location.
                // Since GET is the default; POST only occurs when -Method POST is used.
                if (Method == WebRequestMethod.Post && IsRedirectToGet(response.StatusCode))
                {
                    // See https://msdn.microsoft.com/en-us/library/system.net.httpstatuscode(v=vs.110).aspx
                    Method = WebRequestMethod.Get;
                }

                // recreate the HttpClient with redirection enabled since the first call suppressed redirection
                using (client = GetHttpClient(false))
                using (HttpRequestMessage redirectRequest = GetRequest(response.Headers.Location, stripAuthorization:true))
                {
                    FillRequestStream(redirectRequest);
                    _cancelToken = new CancellationTokenSource();
                    response = client.SendAsync(redirectRequest, HttpCompletionOption.ResponseHeadersRead, _cancelToken.Token).GetAwaiter().GetResult();
                }
            }
            return response;
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

                // if the request contains an authorization header and PreserveAuthorizationOnRedirect is not set,
                // it needs to be stripped on the first redirect.
                bool stripAuthorization = null != WebSession
                                          &&
                                          null != WebSession.Headers
                                          &&
                                          !PreserveAuthorizationOnRedirect.IsPresent
                                          &&
                                          WebSession.Headers.ContainsKey(HttpKnownHeaderNames.Authorization.ToString());

                using (HttpClient client = GetHttpClient(stripAuthorization))
                {
                    int followedRelLink = 0;
                    Uri uri = Uri;
                    do
                    {
                        if (followedRelLink > 0)
                        {
                            string linkVerboseMsg = string.Format(CultureInfo.CurrentCulture,
                                WebCmdletStrings.FollowingRelLinkVerboseMsg,
                                uri.AbsoluteUri);
                            WriteVerbose(linkVerboseMsg);
                        }

                        using (HttpRequestMessage request = GetRequest(uri, stripAuthorization:false))
                        {
                            FillRequestStream(request);
                            try
                            {
                                long requestContentLength = 0;
                                if (request.Content != null)
                                    requestContentLength = request.Content.Headers.ContentLength.Value;

                                string reqVerboseMsg = String.Format(CultureInfo.CurrentCulture,
                                    WebCmdletStrings.WebMethodInvocationVerboseMsg,
                                    request.Method,
                                    request.RequestUri,
                                    requestContentLength);
                                WriteVerbose(reqVerboseMsg);

                                HttpResponseMessage response = GetResponse(client, request, stripAuthorization);

                                string contentType = ContentHelper.GetContentType(response);
                                string respVerboseMsg = string.Format(CultureInfo.CurrentCulture,
                                    WebCmdletStrings.WebResponseVerboseMsg,
                                    response.Content.Headers.ContentLength,
                                    contentType);
                                WriteVerbose(respVerboseMsg);

                                if (!response.IsSuccessStatusCode)
                                {
                                    string message = String.Format(CultureInfo.CurrentCulture, WebCmdletStrings.ResponseStatusCodeFailure,
                                        (int)response.StatusCode, response.ReasonPhrase);
                                    HttpResponseException httpEx = new HttpResponseException(message, response);
                                    ErrorRecord er = new ErrorRecord(httpEx, "WebCmdletWebResponseException", ErrorCategory.InvalidOperation, request);
                                    string detailMsg = "";
                                    StreamReader reader = null;
                                    try
                                    {
                                        reader = new StreamReader(StreamHelper.GetResponseStream(response));
                                        // remove HTML tags making it easier to read
                                        detailMsg = System.Text.RegularExpressions.Regex.Replace(reader.ReadToEnd(), "<[^>]*>","");
                                    }
                                    catch (Exception)
                                    {
                                        // catch all
                                    }
                                    finally
                                    {
                                        if (reader != null)
                                        {
                                            reader.Dispose();
                                        }
                                    }
                                    if (!String.IsNullOrEmpty(detailMsg))
                                    {
                                        er.ErrorDetails = new ErrorDetails(detailMsg);
                                    }
                                    ThrowTerminatingError(er);
                                }

                                if (_parseRelLink || _followRelLink)
                                {
                                    ParseLinkHeader(response, uri);
                                }
                                ProcessResponse(response);
                                UpdateSession(response);

                                // If we hit our maximum redirection count, generate an error.
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
                                if (ex.InnerException != null)
                                {
                                    er.ErrorDetails = new ErrorDetails(ex.InnerException.Message);
                                }
                                ThrowTerminatingError(er);
                            }

                            if (_followRelLink)
                            {
                                if (!_relationLink.ContainsKey("next"))
                                {
                                    return;
                                }
                                uri = new Uri(_relationLink["next"]);
                                followedRelLink++;
                            }
                        }
                    }
                    while (_followRelLink && (followedRelLink < _maximumFollowRelLink));
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

        /// <summary>
        /// Sets the ContentLength property of the request and writes the specified content to the request's RequestStream.
        /// </summary>
        /// <param name="request">The WebRequest who's content is to be set</param>
        /// <param name="multipartContent">A MultipartFormDataContent object containing multipart/form-data content.</param>
        /// <returns>The number of bytes written to the requests RequestStream (and the new value of the request's ContentLength property</returns>
        /// <remarks>
        /// Because this function sets the request's ContentLength property and writes content data into the requests's stream,
        /// it should be called one time maximum on a given request.
        /// </remarks>
        internal long SetRequestContent(HttpRequestMessage request, MultipartFormDataContent multipartContent)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            if (multipartContent == null)
            {
                throw new ArgumentNullException("multipartContent");
            }

            request.Content = multipartContent;

            return multipartContent.Headers.ContentLength.Value;
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

        internal void ParseLinkHeader(HttpResponseMessage response, System.Uri requestUri)
        {
            if (_relationLink == null)
            {
                _relationLink = new Dictionary<string, string>();
            }
            else
            {
                _relationLink.Clear();
            }

            // we only support the URL in angle brackets and `rel`, other attributes are ignored
            // user can still parse it themselves via the Headers property
            string pattern = "<(?<url>.*?)>;\\srel=\"(?<rel>.*?)\"";
            IEnumerable<string> links;
            if (response.Headers.TryGetValues("Link", out links))
            {
                foreach (string linkHeader in links)
                {
                    foreach (string link in linkHeader.Split(","))
                    {
                        Match match = Regex.Match(link, pattern);
                        if (match.Success)
                        {
                            string url = match.Groups["url"].Value;
                            string rel = match.Groups["rel"].Value;
                            if (url != String.Empty && rel != String.Empty && !_relationLink.ContainsKey(rel))
                            {
                                Uri absoluteUri = new Uri(requestUri, url);
                                _relationLink.Add(rel, absoluteUri.AbsoluteUri.ToString());
                            }
                        }
                    }
                }
            }
        }

        #endregion Helper Methods
    }
}
