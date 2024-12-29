// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management.Automation;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Xml;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Invoke-RestMethod command
    /// This command makes an HTTP or HTTPS request to a web service,
    /// and returns the response in an appropriate way.
    /// Intended to work against the wide spectrum of "RESTful" web services
    /// currently deployed across the web.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "RestMethod", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096706", DefaultParameterSetName = "StandardMethod")]
    public class InvokeRestMethodCommand : WebRequestPSCmdlet
    {
        #region Parameters

        /// <summary>
        /// Enable automatic following of rel links.
        /// </summary>
        [Parameter]
        [Alias("FL")]
        public SwitchParameter FollowRelLink
        {
            get => base._followRelLink;

            set => base._followRelLink = value;
        }

        /// <summary>
        /// Gets or sets the maximum number of rel links to follow.
        /// </summary>
        [Parameter]
        [Alias("ML")]
        [ValidateRange(1, int.MaxValue)]
        public int MaximumFollowRelLink
        {
            get => base._maximumFollowRelLink;

            set => base._maximumFollowRelLink = value;
        }

        /// <summary>
        /// Gets or sets the ResponseHeadersVariable property.
        /// </summary>
        [Parameter]
        [Alias("RHV")]
        public string? ResponseHeadersVariable { get; set; }

        /// <summary>
        /// Gets or sets the variable name to use for storing the status code from the response.
        /// </summary>
        [Parameter]
        public string? StatusCodeVariable { get; set; }

        #endregion Parameters

        #region Virtual Method Overrides

        /// <summary>
        /// Process the web response and output corresponding objects.
        /// </summary>
        /// <param name="response"></param>
        internal override void ProcessResponse(HttpResponseMessage response)
        {
            ArgumentNullException.ThrowIfNull(response);
            ArgumentNullException.ThrowIfNull(_cancelToken);

            TimeSpan perReadTimeout = ConvertTimeoutSecondsToTimeSpan(OperationTimeoutSeconds);
            Stream responseStream = StreamHelper.GetResponseStream(response, _cancelToken.Token);

            if (ShouldWriteToPipeline)
            {
                responseStream = new BufferingStreamReader(responseStream, perReadTimeout, _cancelToken.Token);

                // First see if it is an RSS / ATOM feed, in which case we can
                // stream it - unless the user has overridden it with a return type of "XML"
                if (TryProcessFeedStream(responseStream))
                {
                    // Do nothing, content has been processed.
                }
                else
                {
                    // Try to get the response encoding from the ContentType header.
                    string? characterSet = WebResponseHelper.GetCharacterSet(response);
                    string str = StreamHelper.DecodeStream(responseStream, characterSet, out Encoding encoding, perReadTimeout, _cancelToken.Token);

                    string encodingVerboseName;
                    try
                    {
                        encodingVerboseName = encoding.HeaderName;
                    }
                    catch
                    {
                        encodingVerboseName = string.Empty;
                    }

                    // NOTE: Tests use this verbose output to verify the encoding.
                    WriteVerbose($"Content encoding: {encodingVerboseName}");

                    // Determine the response type
                    RestReturnType returnType = CheckReturnType(response);

                    bool convertSuccess = false;
                    object? obj = null;
                    Exception? ex = null;

                    if (returnType == RestReturnType.Json)
                    {
                        convertSuccess = TryConvertToJson(str, out obj, ref ex) || TryConvertToXml(str, out obj, ref ex);
                    }
                    // Default to try xml first since it's more common
                    else
                    {
                        convertSuccess = TryConvertToXml(str, out obj, ref ex) || TryConvertToJson(str, out obj, ref ex);
                    }

                    if (!convertSuccess)
                    {
                        // Fallback to string
                        obj = str;
                    }

                    WriteObject(obj);
                }

                responseStream.Position = 0;
            }
            
            if (ShouldSaveToOutFile)
            {
                string outFilePath = WebResponseHelper.GetOutFilePath(response, _qualifiedOutFile);

                WriteVerbose($"File Name: {Path.GetFileName(outFilePath)}");

                StreamHelper.SaveStreamToFile(responseStream, outFilePath, this, response.Content.Headers.ContentLength.GetValueOrDefault(), perReadTimeout, _cancelToken.Token);
            }

            if (!string.IsNullOrEmpty(StatusCodeVariable))
            {
                PSVariableIntrinsics vi = SessionState.PSVariable;
                vi.Set(StatusCodeVariable, (int)response.StatusCode);
            }

            if (!string.IsNullOrEmpty(ResponseHeadersVariable))
            {
                PSVariableIntrinsics vi = SessionState.PSVariable;
                vi.Set(ResponseHeadersVariable, WebResponseHelper.GetHeadersDictionary(response));
            }
        }

        #endregion Virtual Method Overrides

        #region Helper Methods

        private static RestReturnType CheckReturnType(HttpResponseMessage response)
        {
            ArgumentNullException.ThrowIfNull(response);

            RestReturnType rt = RestReturnType.Detect;
            string? contentType = ContentHelper.GetContentType(response);

            if (ContentHelper.IsJson(contentType))
            {
                rt = RestReturnType.Json;
            }
            else if (ContentHelper.IsXml(contentType))
            {
                rt = RestReturnType.Xml;
            }

            return rt;
        }

        private bool TryProcessFeedStream(Stream responseStream)
        {
            bool isRssOrFeed = false;

            try
            {
                XmlReaderSettings readerSettings = GetSecureXmlReaderSettings();
                XmlReader reader = XmlReader.Create(responseStream, readerSettings);

                // See if the reader contained an "RSS" or "Feed" in the first 10 elements (RSS and Feed are normally 2 or 3)
                int readCount = 0;
                while ((readCount < 10) && reader.Read())
                {
                    if (string.Equals("rss", reader.Name, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals("feed", reader.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        isRssOrFeed = true;
                        break;
                    }

                    readCount++;
                }

                if (isRssOrFeed)
                {
                    XmlDocument workingDocument = new();

                    // Performing a Read() here to avoid rechecking
                    // "rss" or "feed" items
                    reader.Read();
                    while (!reader.EOF)
                    {
                        // If node is Element and it's the 'Item' or 'Entry' node, emit that node.
                        if ((reader.NodeType == XmlNodeType.Element) &&
                            (string.Equals("Item", reader.Name, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals("Entry", reader.Name, StringComparison.OrdinalIgnoreCase))
                           )
                        {
                            // This one will do reader.Read() internally
                            XmlNode? result = workingDocument.ReadNode(reader);
                            WriteObject(result);
                        }
                        else
                        {
                            reader.Read();
                        }
                    }
                }
            }
            catch (XmlException)
            {
                // Catch XmlException
            }
            finally
            {
                responseStream.Seek(0, SeekOrigin.Begin);
            }

            return isRssOrFeed;
        }

        // Mostly cribbed from Serialization.cs#GetXmlReaderSettingsForCliXml()
        private static XmlReaderSettings GetSecureXmlReaderSettings()
        {
            XmlReaderSettings xrs = new();

            xrs.CheckCharacters = false;
            xrs.CloseInput = false;

            // The XML data needs to be in conformance to the rules for a well-formed XML 1.0 document.
            xrs.IgnoreProcessingInstructions = true;
            xrs.MaxCharactersFromEntities = 1024;
            xrs.DtdProcessing = DtdProcessing.Ignore;
            xrs.XmlResolver = null;

            return xrs;
        }

        private static bool TryConvertToXml(string xml, [NotNullWhen(true)] out object? doc, ref Exception? exRef)
        {
            try
            {
                XmlReaderSettings settings = GetSecureXmlReaderSettings();
                XmlReader xmlReader = XmlReader.Create(new StringReader(xml), settings);

                XmlDocument xmlDoc = new();
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(xmlReader);

                doc = xmlDoc;
            }
            catch (XmlException ex)
            {
                exRef = ex;
                doc = null;
            }

            return doc != null;
        }

        private static bool TryConvertToJson(string json, [NotNullWhen(true)] out object? obj, ref Exception? exRef)
        {
            bool converted = false;
            try
            {
                obj = JsonObject.ConvertFromJson(json, out ErrorRecord error);

                if (obj == null)
                {
                    // This ensures that a null returned by ConvertFromJson() is the actual JSON null literal.
                    // if not, the ArgumentException will be caught.
                    JToken.Parse(json);
                }

                if (error != null)
                {
                    exRef = error.Exception;
                    obj = null;
                }
                else
                {
                    converted = true;
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                exRef = ex;
                obj = null;
            }
            catch (JsonException ex)
            {
                string msg = string.Format(System.Globalization.CultureInfo.CurrentCulture, WebCmdletStrings.JsonDeserializationFailed, ex.Message);
                exRef = new ArgumentException(msg, ex);
                obj = null;
            }

            return converted;
        }

        #endregion Helper Methods

        /// <summary>
        /// Enum for rest return type.
        /// </summary>
        public enum RestReturnType
        {
            /// <summary>
            /// Return type not defined in response,
            /// best effort detect.
            /// </summary>
            Detect,

            /// <summary>
            /// Json return type.
            /// </summary>
            [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
            Json,

            /// <summary>
            /// Xml return type.
            /// </summary>
            Xml,
        }

        internal class BufferingStreamReader : Stream
        {
            internal BufferingStreamReader(Stream baseStream, TimeSpan perReadTimeout, CancellationToken cancellationToken)
            {
                _baseStream = baseStream;
                _streamBuffer = new MemoryStream();
                _length = long.MaxValue;
                _copyBuffer = new byte[4096];
                _perReadTimeout = perReadTimeout;
                _cancellationToken = cancellationToken;
            }

            private readonly Stream _baseStream;
            private readonly MemoryStream _streamBuffer;
            private readonly byte[] _copyBuffer;
            private readonly TimeSpan _perReadTimeout;
            private readonly CancellationToken _cancellationToken;

            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => false;

            public override void Flush()
            {
                _streamBuffer.SetLength(0);
            }

            public override long Length => _length;

            private long _length;

            public override long Position
            {
                get => _streamBuffer.Position;

                set => _streamBuffer.Position = value;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                long previousPosition = Position;
                bool consumedStream = false;
                int totalCount = count;
                while (!consumedStream && (Position + totalCount) > _streamBuffer.Length)
                {
                    // If we don't have enough data to fill this from memory, cache more.
                    // We try to read 4096 bytes from base stream every time, so at most we
                    // may cache 4095 bytes more than what is required by the Read operation.
                    int bytesRead = _baseStream.ReadAsync(_copyBuffer.AsMemory(), _perReadTimeout, _cancellationToken).GetAwaiter().GetResult();

                    if (_streamBuffer.Position < _streamBuffer.Length)
                    {
                        // Win8: 651902 no need to -1 here as Position refers to the place
                        // where we can start writing from.
                        _streamBuffer.Position = _streamBuffer.Length;
                    }

                    _streamBuffer.Write(_copyBuffer, 0, bytesRead);

                    totalCount -= bytesRead;
                    if (bytesRead < _copyBuffer.Length)
                    {
                        consumedStream = true;
                    }
                }

                // Reset our backing store to its official position, as reading
                // for the CopyTo updates the position.
                _streamBuffer.Seek(previousPosition, SeekOrigin.Begin);

                // Read from the backing store into the requested buffer.
                int read = _streamBuffer.Read(buffer, offset, count);

                if (read < count)
                {
                    SetLength(Position);
                }

                return read;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _streamBuffer.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _length = value;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}
