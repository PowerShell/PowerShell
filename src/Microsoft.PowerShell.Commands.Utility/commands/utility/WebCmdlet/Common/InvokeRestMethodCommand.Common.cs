/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.IO;
using System.Xml;

namespace Microsoft.PowerShell.Commands
{
    public partial class InvokeRestMethodCommand
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

        #region Helper Methods

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

        private bool TryConvertToXml(string xml, out object doc, ref Exception exRef)
        {
            try
            {
                XmlReaderSettings settings = GetSecureXmlReaderSettings();
                XmlReader xmlReader = XmlReader.Create(new StringReader(xml), settings);

                var xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(xmlReader);

                doc = xmlDoc;
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

        #endregion

        /// <summary>
        /// enum for rest return type.
        /// </summary>
        public enum RestReturnType
        {
            /// <summary>
            /// Return type not defined in response, 
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
                _baseStream = baseStream;
                _streamBuffer = new MemoryStream();
                _length = long.MaxValue;
                _copyBuffer = new byte[4096];
            }

            private Stream _baseStream;
            private MemoryStream _streamBuffer;
            private byte[] _copyBuffer;

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
                _streamBuffer.SetLength(0);
            }

            public override long Length
            {
                get { return _length; }
            }
            private long _length;

            public override long Position
            {
                get { return _streamBuffer.Position; }
                set { _streamBuffer.Position = value; }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                long previousPosition = Position;
                bool consumedStream = false;
                int totalCount = count;
                while ((!consumedStream) &&
                    ((Position + totalCount) > _streamBuffer.Length))
                {
                    // If we don't have enough data to fill this from memory, cache more.
                    // We try to read 4096 bytes from base stream every time, so at most we 
                    // may cache 4095 bytes more than what is required by the Read operation.
                    int bytesRead = _baseStream.Read(_copyBuffer, 0, _copyBuffer.Length);

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