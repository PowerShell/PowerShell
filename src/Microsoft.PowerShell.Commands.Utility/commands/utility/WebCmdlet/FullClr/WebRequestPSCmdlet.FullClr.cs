/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading;
using System.Web;
using System.Xml;
using mshtml;

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
        internal abstract void ProcessResponse(WebResponse response);

        #endregion Abstract Methods

        /// <summary>
        /// the WebRequest we will GetResponse for.
        /// </summary>
        private WebRequest _webRequest;

        #region Virtual Methods

        internal virtual WebRequest GetRequest(Uri uri)
        {
            uri = PrepareUri(uri);
            // create the base WebRequest object
            WebRequest request = WebRequest.Create(uri);

            // pull in session data
            if (0 < WebSession.Headers.Count)
            {
                try
                {
                    HttpWebRequest webRequest = request as HttpWebRequest;
                    request.Headers.Clear();
                    foreach (string key in WebSession.Headers.Keys)
                    {
                        bool setHeaderViaProperty = TryMapHeaderToProperty(webRequest, key);

                        if (!setHeaderViaProperty)
                        {
                            request.Headers[key] = WebSession.Headers[key];
                        }
                    }
                }
                catch (NotImplementedException)
                {
                }
            }

            // set the credentials used by this request
            if (WebSession.UseDefaultCredentials)
            {
                // the UseDefaultCredentials flag overrides other supplied credentials
                request.UseDefaultCredentials = true;
            }
            else if (null != WebSession.Credentials)
            {
                request.Credentials = WebSession.Credentials;
            }

            if (null != WebSession.Proxy)
            {
                request.Proxy = WebSession.Proxy;
            }

            // set the method if the parameter was provided
            if (WebRequestMethod.Default != Method)
            {
                request.Method = Method.ToString().ToUpperInvariant();
            }

            // pull in http specific properties
            HttpWebRequest httpRequest = request as HttpWebRequest;
            if (null != httpRequest)
            {
                httpRequest.CookieContainer = WebSession.Cookies;
                httpRequest.UserAgent = WebSession.UserAgent;

                if (null != WebSession.Certificates)
                {
                    httpRequest.ClientCertificates = WebSession.Certificates;
                }

                if (-1 < WebSession.MaximumRedirection)
                {
                    if (WebSession.MaximumRedirection == 0)
                    {
                        httpRequest.AllowAutoRedirect = false;
                    }
                    else
                    {
                        httpRequest.MaximumAutomaticRedirections = WebSession.MaximumRedirection;
                    }
                }

                // check timeout setting (in seconds instead of milliseconds as in HttpWebRequest)
                if (TimeoutSec == 0)
                {
                    // A zero timeout means infinite
                    httpRequest.Timeout = Timeout.Infinite;
                }
                else if (TimeoutSec > 0)
                {
                    // just to make sure
                    if (TimeoutSec > Int32.MaxValue / 1000)
                    {
                        httpRequest.Timeout = Int32.MaxValue;
                    }
                    else
                    {
                        httpRequest.Timeout = TimeoutSec * 1000;
                    }
                }

                // check keep-alive setting
                if (DisableKeepAlive)
                {
                    // default value is true, so only need to set if false.
                    httpRequest.KeepAlive = false;
                }

                if (null != TransferEncoding)
                {
                    httpRequest.SendChunked = true;
                    httpRequest.TransferEncoding = TransferEncoding;
                }
            }

            return (request);
        }

        private bool TryMapHeaderToProperty(HttpWebRequest webRequest, string key)
        {
            bool setHeaderViaProperty = false;

            // Perform header-to-property overrides
            if (webRequest != null)
            {
                if (String.Equals("Accept", key, StringComparison.OrdinalIgnoreCase))
                {
                    webRequest.Accept = WebSession.Headers[key];
                    setHeaderViaProperty = true;
                }

                if (String.Equals("Connection", key, StringComparison.OrdinalIgnoreCase))
                {
                    webRequest.Connection = WebSession.Headers[key];
                    setHeaderViaProperty = true;
                }

                if (String.Equals("Content-Length", key, StringComparison.OrdinalIgnoreCase))
                {
                    webRequest.ContentLength = Convert.ToInt64(WebSession.Headers[key], CultureInfo.InvariantCulture);
                    setHeaderViaProperty = true;
                }

                if (String.Equals("Content-Type", key, StringComparison.OrdinalIgnoreCase))
                {
                    webRequest.ContentType = WebSession.Headers[key];
                    setHeaderViaProperty = true;
                }

                if (String.Equals("Date", key, StringComparison.OrdinalIgnoreCase))
                {
                    webRequest.Date = DateTime.Parse(WebSession.Headers[key], CultureInfo.InvariantCulture);
                    setHeaderViaProperty = true;
                }

                if (String.Equals("Expect", key, StringComparison.OrdinalIgnoreCase))
                {
                    webRequest.Expect = WebSession.Headers[key];
                    setHeaderViaProperty = true;
                }

                if (String.Equals("Host", key, StringComparison.OrdinalIgnoreCase))
                {
                    webRequest.Host = WebSession.Headers[key];
                    setHeaderViaProperty = true;
                }

                if (String.Equals("If-Modified-Since", key, StringComparison.OrdinalIgnoreCase))
                {
                    webRequest.IfModifiedSince = DateTime.Parse(WebSession.Headers[key], CultureInfo.InvariantCulture);
                    setHeaderViaProperty = true;
                }

                if (String.Equals("Referer", key, StringComparison.OrdinalIgnoreCase))
                {
                    webRequest.Referer = WebSession.Headers[key];
                    setHeaderViaProperty = true;
                }

                if (String.Equals("Transfer-Encoding", key, StringComparison.OrdinalIgnoreCase))
                {
                    webRequest.SendChunked = true;

                    if (String.Equals("Chunked", WebSession.Headers[key], StringComparison.OrdinalIgnoreCase))
                    {
                        // .NET Doesn't support setting both the header and the property
                        webRequest.SendChunked = true;
                    }
                    else
                    {
                        webRequest.TransferEncoding = WebSession.Headers[key];
                    }

                    setHeaderViaProperty = true;
                }

                if (String.Equals("User-Agent", key, StringComparison.OrdinalIgnoreCase))
                {
                    WebSession.UserAgent = WebSession.Headers[key];
                    webRequest.UserAgent = WebSession.Headers[key];
                    setHeaderViaProperty = true;
                }
            }
            return setHeaderViaProperty;
        }

        internal virtual void FillRequestStream(WebRequest request)
        {
            if (null == request) { throw new ArgumentNullException("request"); }

            // set the content type
            if (null != ContentType)
            {
                request.ContentType = ContentType;
            }
            // ContentType == null
            else if (Method == WebRequestMethod.Post)
            {
                // Win8:545310 Invoke-WebRequest does not properly set MIME type for POST
                if (String.IsNullOrEmpty(request.ContentType))
                {
                    request.ContentType = "application/x-www-form-urlencoded";
                }
            }

            // coerce body into a usable form
            if (null != Body)
            {
                object content = Body;

                // make sure we're using the base object of the body, not the PSObject wrapper
                PSObject psBody = Body as PSObject;
                if (null != psBody)
                {
                    content = psBody.BaseObject;
                }

                if (null != content as HtmlWebResponseObject)
                {
                    HtmlWebResponseObject html = content as HtmlWebResponseObject;
                    // use the form it's the only one present
                    if (html.Forms.Count == 1)
                    {
                        SetRequestContent(request, html.Forms[0].Fields);
                    }
                }
                else if (null != content as FormObject)
                {
                    FormObject form = content as FormObject;
                    SetRequestContent(request, form.Fields);
                }
                else if (null != content as IDictionary && request.Method != WebRequestMethods.Http.Get)
                {
                    IDictionary dictionary = content as IDictionary;
                    SetRequestContent(request, dictionary);
                }
                else if (null != content as XmlNode)
                {
                    XmlNode xmlNode = content as XmlNode;
                    SetRequestContent(request, xmlNode);
                }
                else if (null != content as Stream)
                {
                    Stream stream = content as Stream;
                    SetRequestContent(request, stream);
                }
                else if (null != content as byte[])
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
            else if (null != InFile) // copy InFile data
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
            else
            {
                request.ContentLength = 0;
            }
        }

        internal virtual WebResponse GetResponse(WebRequest request)
        {
            if (null == request) { throw new ArgumentNullException("request"); }

            // Construct TimeoutState
            HttpWebRequest httpRequest = request as HttpWebRequest;
            TimeoutState timeoutState = null;
            if (httpRequest != null && httpRequest.Timeout > 0)
            {
                timeoutState = new TimeoutState(httpRequest);
            }

            // Construct WebRequestState
            _webRequest = request;
            WebRequestState requestState = new WebRequestState(request);

            // Call asynchronous GetResponse
            IAsyncResult asyncResult = (IAsyncResult)request.BeginGetResponse(new AsyncCallback(ResponseCallback), requestState);
            // Set timeout if necessary
            if (timeoutState != null)
            {
                ThreadPool.RegisterWaitForSingleObject(asyncResult.AsyncWaitHandle, new WaitOrTimerCallback(TimeoutCallback), timeoutState, timeoutState.httpRequest.Timeout, true);
            }

            // Wait on signal
            requestState.waithandle.WaitOne(-1, false);
            requestState.waithandle.Close();
            _webRequest = null;

            // The current thread will be waked up in three cases:
            //    1. the EngGetResponse is done. In this case, we EITHER get the response (requestState.response != null),
            //       OR a WebException is raised (requestState.webException != null).
            //    2. the ^C is typed, a PipelineStoppedException is raised. StopProcessing will abort the request. In this
            //       case, there will be a WebException with status 'RequestCancelled'.
            //    3. the time is up. The TimeoutCallback method will abort the request. In this case, there will also be a
            //       WebException with status 'RequestCancelled' and timeoutState.abort will be true.
            if (requestState.webException != null)
            {
                // Case 3. We wrap the exception to be 'Timeout' WebException
                if (timeoutState != null && timeoutState.abort && requestState.webException.Status.Equals(WebExceptionStatus.RequestCanceled))
                {
                    throw new WebException(WebCmdletStrings.RequestTimeout, WebExceptionStatus.Timeout);
                }

                // Case 1 or 2
                throw requestState.webException;
            }

            return (requestState.response);
        }

        internal virtual void UpdateSession(WebResponse response)
        {
            if (null == response) { throw new ArgumentNullException("response"); }

            // process HTTP specific session data
            HttpWebResponse httpResponse = response as HttpWebResponse;
            if ((null != WebSession) && (null != httpResponse))
            {
                // save response cookies into the session
                WebSession.Cookies.Add(httpResponse.Cookies);
            }
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
                WebRequest request = GetRequest(Uri);
                FillRequestStream(request);

                // Some web sites (e.g. Twitter) will return exception on POST when Expect100 is sent
                // Default behaviour is continue to send body content anyway after a short period
                // Here it send the two part as a whole. 
                ServicePointManager.Expect100Continue = false;

                try
                {
                    string reqVerboseMsg = String.Format(CultureInfo.CurrentCulture,
                        "{0} {1} with {2}-byte payload",
                        request.Method,
                        request.RequestUri,
                        request.ContentLength);
                    WriteVerbose(reqVerboseMsg);
                    WebResponse response = GetResponse(request);
                    try
                    {
                        string contentType = ContentHelper.GetContentType(response);
                        string respVerboseMsg = String.Format(CultureInfo.CurrentCulture,
                            "received {0}-byte response of content type {1}",
                            response.ContentLength,
                            contentType);
                        WriteVerbose(respVerboseMsg);
                        ProcessResponse(response);
                        UpdateSession(response);

                        // If we hit our maximum redirection count, generate an error.
                        // Errors with redirection counts of greater than 0 are handled automatically by .NET, but are
                        // impossible to detect programmatically when we hit this limit. By handling this ourselves
                        // (and still writing out the result), users can debug actual HTTP redirect problems.
                        HttpWebRequest httpRequest = request as HttpWebRequest;
                        if ((httpRequest != null) && (httpRequest.AllowAutoRedirect == false))
                        {
                            HttpWebResponse webResponse = response as HttpWebResponse;
                            if ((webResponse.StatusCode == HttpStatusCode.Found) ||
                                (webResponse.StatusCode == HttpStatusCode.Moved) ||
                                webResponse.StatusCode == HttpStatusCode.MovedPermanently)
                            {
                                ErrorRecord er = new ErrorRecord(new InvalidOperationException(), "MaximumRedirectExceeded", ErrorCategory.InvalidOperation, httpRequest);
                                er.ErrorDetails = new ErrorDetails(WebCmdletStrings.MaximumRedirectionCountExceeded);
                                WriteError(er);
                            }
                        }
                    }
                    finally
                    {
                        // Choosing to close the stream instead of Dispose as the
                        // response object is being written to the output pipe.
                        if (response != null)
                        {
                            response.Close();
                        }
                    }
                }
                catch (WebException ex)
                {
                    WebException exThrown = ex;
                    string detailMsg = String.Empty;
                    try
                    {
                        if (ex.Response != null && ex.Response.ContentLength > 0)
                        {
                            Stream input = StreamHelper.GetResponseStream(ex.Response);
                            StreamReader reader = new StreamReader(input);
                            detailMsg = reader.ReadToEnd();

                            // If we were asked to not use the IE engine (or this is Invoke-RestMethod), use a simple
                            // regex replace to remove tags.
                            if (UseBasicParsing || (this is InvokeRestMethodCommand))
                            {
                                detailMsg = System.Text.RegularExpressions.Regex.Replace(detailMsg, "<[^>]*>", "");
                            }
                            else
                            {
                                // Otherwise, use IE to clean it up, as errors often come back as HTML
                                VerifyInternetExplorerAvailable(false);

                                try
                                {
                                    IHTMLDocument2 _parsedHtml = (IHTMLDocument2)new HTMLDocument();
                                    _parsedHtml.write(detailMsg);
                                    detailMsg = _parsedHtml.body.outerText;
                                }
                                catch (System.Runtime.InteropServices.COMException) { }
                            }
                        }
                    }
                    // catch all
                    catch (Exception)
                    {
                    }
                    ErrorRecord er = new ErrorRecord(exThrown, "WebCmdletWebResponseException", ErrorCategory.InvalidOperation, request);
                    if (!String.IsNullOrEmpty(detailMsg))
                    {
                        er.ErrorDetails = new ErrorDetails(detailMsg);
                    }
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
            if (_webRequest != null)
            {
                _webRequest.Abort();
            }
        }

        #endregion Overrides

        #region Helper Methods

        /// <summary>
        /// Call back method for BeginGetResponse
        /// </summary>
        /// <param name="asyncResult"></param>
        private static void ResponseCallback(IAsyncResult asyncResult)
        {
            WebRequestState myRequestState = (WebRequestState)asyncResult.AsyncState;

            try
            {
                myRequestState.response = myRequestState.request.EndGetResponse(asyncResult);
            }
            catch (WebException ex)
            {
                myRequestState.response = null;
                myRequestState.webException = ex;
            }
            finally
            {
                myRequestState.waithandle.Set();
            }
        }

        /// <summary>
        /// Call back method for timeout
        /// </summary>
        /// <param name="state"></param>
        /// <param name="timeout"></param>
        private static void TimeoutCallback(object state, bool timeout)
        {
            if (timeout)
            {
                TimeoutState timeoutState = state as TimeoutState;
                if (timeoutState != null)
                {
                    timeoutState.abort = true;
                    timeoutState.httpRequest.Abort();
                }
            }
        }

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
        internal long SetRequestContent(WebRequest request, Byte[] content)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (content != null)
            {
                if (request.ContentLength == 0)
                {
                    request.ContentLength = content.Length;
                }
                StreamHelper.WriteToStream(content, request.GetRequestStream());
            }
            else
            {
                request.ContentLength = 0;
            }

            return request.ContentLength;
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
        internal long SetRequestContent(WebRequest request, String content)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (content != null)
            {
                Encoding encoding = null;
                if (null != ContentType)
                {
                    // If Content-Type contains the encoding format (as CharSet), use this encoding format 
                    // to encode the Body of the WebRequest sent to the server. Default Encoding format 
                    // would be used if Charset is not supplied in the Content-Type property.
                    System.Net.Mime.ContentType mimeContentType = new System.Net.Mime.ContentType(ContentType);
                    if (!String.IsNullOrEmpty(mimeContentType.CharSet))
                    {
                        try
                        {
                            encoding = Encoding.GetEncoding(mimeContentType.CharSet);
                        }
                        catch (ArgumentException ex)
                        {
                            ErrorRecord er = new ErrorRecord(ex, "WebCmdletEncodingException", ErrorCategory.InvalidArgument, ContentType);
                            ThrowTerminatingError(er);
                        }
                    }
                }

                Byte[] bytes = StreamHelper.EncodeToBytes(content, encoding);

                if (request.ContentLength == 0)
                {
                    request.ContentLength = bytes.Length;
                }
                StreamHelper.WriteToStream(bytes, request.GetRequestStream());
            }
            else
            {
                request.ContentLength = 0;
            }

            return request.ContentLength;
        }

        internal long SetRequestContent(WebRequest request, XmlNode xmlNode)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (xmlNode != null)
            {
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

                if (request.ContentLength == 0)
                {
                    request.ContentLength = bytes.Length;
                }
                StreamHelper.WriteToStream(bytes, request.GetRequestStream());
            }
            else
            {
                request.ContentLength = 0;
            }

            return request.ContentLength;
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
        internal long SetRequestContent(WebRequest request, Stream contentStream)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (contentStream == null)
                throw new ArgumentNullException("contentStream");

            if (request.ContentLength == 0)
            {
                request.ContentLength = contentStream.Length;
            }

            StreamHelper.WriteToStream(contentStream, request.GetRequestStream(), this);

            return request.ContentLength;
        }

        internal long SetRequestContent(WebRequest request, IDictionary content)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (content == null)
                throw new ArgumentNullException("content");

            string body = FormatDictionary(content);
            return (SetRequestContent(request, body));
        }

        #endregion Helper Methods

        #region private State class

        /// <summary>
        /// The web request state is used when place asynchronous call to get response of a web request.
        /// </summary>
        private class WebRequestState
        {
            public WebRequest request;
            public WebResponse response;
            public ManualResetEvent waithandle;
            public WebException webException;

            public WebRequestState(WebRequest webRequest)
            {
                request = webRequest;
                response = null;
                webException = null;
                waithandle = new ManualResetEvent(false);
            }
        }

        /// <summary>
        /// The timeout state is used when the request is a http request and need to timeout
        /// </summary>
        private class TimeoutState
        {
            public HttpWebRequest httpRequest;
            public bool abort;

            public TimeoutState(HttpWebRequest request)
            {
                httpRequest = request;
                abort = false;
            }
        }

        #endregion private State class
    }
}