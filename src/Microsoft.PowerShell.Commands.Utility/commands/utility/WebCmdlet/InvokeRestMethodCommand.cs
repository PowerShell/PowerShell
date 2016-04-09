/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Net;
using System.Collections;
using System.IO;
using System.Xml;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Invoke-RestMethod command
    /// This command makes an HTTP or HTTPS request to a web service,
    /// and returns the response in an appropriate way. 
    /// Intended to work against the wide spectrum of “RESTful” web services 
    /// currently deployed across the web.  
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "RestMethod", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=217034")]
    public class InvokeRestMethodCommand : WebRequestPSCmdlet
    {
        #region Parameters

        /// <summary>
        /// gets or sets the parameter Method 
        /// </summary>
        [Parameter]
        public override WebRequestMethod Method
        {
            get { return base.Method; }
            set { base.Method = value; }
        }

        #endregion Parameters

        #region Virtual Method Overrides

        /// <summary>
        /// Process the web reponse and output corresponding objects. 
        /// </summary>
        /// <param name="response"></param>
        internal override void ProcessResponse(WebResponse response)
        {
            if (null == response) { throw new ArgumentNullException("response"); }

            using (BufferingStreamReader responseStream = new BufferingStreamReader(StreamHelper.GetResponseStream(response)))
            {
                if (ShouldWriteToPipeline)
                {
                    // First see if it is an RSS / ATOM feed, in which case we can 
                    // stream it - unless the user has overriden it with a return type of "XML"
                    if (TryProcessFeedStream(responseStream))
                    {
                        // Do nothing, content has been processed.
                    }
                    else
                    {
                        // determine the response type
                        RestReturnType returnType = CheckReturnType(response);
                        // get the response encoding
                        Encoding encoding = ContentHelper.GetEncoding(response);

                        object obj = null;
                        Exception ex = null;

                        string str = StreamHelper.DecodeStream(responseStream, encoding);
                        bool convertSuccess = false;
                        if (returnType == RestReturnType.Json)
                        {
                            convertSuccess = TryConvertToJson(str, out obj, ref ex) || TryConvertToXml(str, out obj, ref ex);
                        }
                        // default to try xml first since it's more common
                        else
                        {
                            convertSuccess = TryConvertToXml(str, out obj, ref ex) || TryConvertToJson(str, out obj, ref ex);
                        }

                        if (!convertSuccess)
                        {
                            // fallback to string
                            obj = str;
                        }

                        WriteObject(obj);
                    }
                }

                if (ShouldSaveToOutFile)
                {
                    StreamHelper.SaveStreamToFile(responseStream, QualifiedOutFile, this);
                }
            }
        }

        private bool TryProcessFeedStream(BufferingStreamReader responseStream)
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
                    if (String.Equals("rss", reader.Name, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals("feed", reader.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        isRssOrFeed = true;
                        break;
                    }

                    readCount++;
                }

                if (isRssOrFeed)
                {
                    XmlDocument workingDocument = new XmlDocument();
                    // performing a Read() here to avoid rrechecking
                    // "rss" or "feed" itmes
                    reader.Read();
                    while (!reader.EOF)
                    {
                        // If node is Elemen and 
                        // it's the 'Item' node or Entry, emit that node.
                        if ((reader.NodeType == XmlNodeType.Element) &&
                            (String.Equals("Item", reader.Name, StringComparison.OrdinalIgnoreCase) ||
                            String.Equals("Entry", reader.Name, StringComparison.OrdinalIgnoreCase))
                            )
                        {
                            // this one will do reader.Read() internally
                            XmlNode result = workingDocument.ReadNode(reader);
                            WriteObject(result);
                        }
                        else
                        {
                            reader.Read();
                        }
                    }
                }
            }
            catch (XmlException) { }
            finally
            {
                responseStream.Seek(0, SeekOrigin.Begin);
            }

            return isRssOrFeed;
        }

        // Mostly cribbed from Serialization.cs#GetXmlReaderSettingsForCliXml()
        private XmlReaderSettings GetSecureXmlReaderSettings()
        {
            XmlReaderSettings xrs = new XmlReaderSettings();

            xrs.CheckCharacters = false;
            xrs.CloseInput = false;

            //The XML data needs to be in conformance to the rules for a well-formed XML 1.0 document.
            xrs.IgnoreProcessingInstructions = true;
            xrs.MaxCharactersFromEntities = 1024;
            xrs.DtdProcessing = DtdProcessing.Ignore;

            return xrs;
        }


        #endregion Virtual Method Overrides

        #region Helper Methods

        private bool TryConvertToXml(string xml, out object doc, ref Exception exRef)
        {
            try
            {
                XmlReaderSettings settings = GetSecureXmlReaderSettings();
                XmlReader xmlReader = XmlReader.Create(new StringReader(xml), settings);
                doc = new XmlDocument();
                ((XmlDocument) doc).PreserveWhitespace = true;
                ((XmlDocument) doc).Load(xmlReader);
            }
            catch (XmlException ex)
            {
                exRef = ex;
                doc = null;
            }
            return (null != doc);
        }

        private bool TryConvertToJson(string json, out object obj, ref Exception exRef)
        {
            try
            {
                ErrorRecord error;
                obj = JsonObject.ConvertFromJson(json, out error);

                if (error != null)
                {
                    exRef = error.Exception;
                    obj = null;
                }
            }
            catch (ArgumentException ex)
            {
                exRef = ex;
                obj = null;
            }
            catch (InvalidOperationException ex)
            {
                exRef = ex;
                obj = null;
            }
            return (null != obj);
        }

        private RestReturnType CheckReturnType(WebResponse response)
        {
            if (null == response) { throw new ArgumentNullException("response"); }

            RestReturnType rt = RestReturnType.Detect;
            string contentType = ContentHelper.GetContentType(response);
            if (string.IsNullOrEmpty(contentType))
            {
                rt = RestReturnType.Detect;
            }
            else if (ContentHelper.IsJson(contentType))
            {
                rt = RestReturnType.Json;
            }
            else if (ContentHelper.IsXml(contentType))
            {
                rt = RestReturnType.Xml;
            }

            return (rt);
        }

        #endregion Helper Methods
    }

    /// <summary>
    /// enum for rest return type.
    /// </summary>
    public enum RestReturnType
    {
        /// <summary>
        /// Return type not defined in reponse, 
        /// best effort detect
        /// </summary>
        Detect,

        /// <summary>
        /// Json return type
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")] 
        Json,

        /// <summary>
        /// Xml return type
        /// </summary>
        Xml,
    }

    internal class BufferingStreamReader : Stream
    {
        internal BufferingStreamReader(Stream baseStream)
        {
            this.baseStream = baseStream;
            streamBuffer = new MemoryStream();
            this.length = long.MaxValue;
            copyBuffer = new byte[4096];
        }
        
        private Stream baseStream;
        private MemoryStream streamBuffer;
        private byte[] copyBuffer;

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            streamBuffer.SetLength(0);
        }

        public override long Length
        {
            get { return this.length; }
        }
        private long length;

        public override long Position
        {
            get { return streamBuffer.Position; }
            set { streamBuffer.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // If we don't have enough data to fill this from memory, cache more.
            long previousPosition = Position;
            bool consumedStream = false;
            int totalCount = count;
            while((! consumedStream) &&
                ((Position + totalCount) > streamBuffer.Length))
            {
                int bytesToRead = Math.Min(totalCount, copyBuffer.Length);
                int bytesRead = baseStream.Read(copyBuffer, 0, bytesToRead);

                if (streamBuffer.Position < streamBuffer.Length)
                {
                    // Win8: 651902 no need to -1 here as Position refers to the place
                    // where we can start writing from.
                    streamBuffer.Position = streamBuffer.Length;
                }

                streamBuffer.Write(copyBuffer, 0, bytesRead);

                totalCount -= bytesRead;
                if (bytesRead < bytesToRead)
                {
                    consumedStream = true;
                }
            }

            // Reset our backing store to its official position, as reading
            // for the CopyTo updates the position.
            streamBuffer.Seek(previousPosition, SeekOrigin.Begin);

            // Read from the backing store into the requested buffer.
            int read = streamBuffer.Read(buffer, offset, count);

            if (read < count)
            {
                SetLength(Position);
            }

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return streamBuffer.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }

}
