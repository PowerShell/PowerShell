/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Management.Automation;
using System.Management.Automation.Internal;

#if CORECLR
using System.Net.Http;
#else
using System.Net;
#endif

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Microsoft.PowerShell.Commands.WebResponse has a public property RawContentStream
    /// which is of type MemoryStream. We shipped like that in PowerShell 3. Creating
    /// this class as a wrapper to MemoryStream to lazily initialize. Otherwise, the 
    /// content will unnecessarily be read even if there are no consumers for it.
    /// </summary>
    internal class WebResponseContentMemoryStream : MemoryStream
    {
        #region Data

        private Stream _originalStreamToProxy;
        private bool _isInitialized = false;
        private Cmdlet _ownerCmdlet;

        #endregion

        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="initialCapacity"></param>
        /// <param name="cmdlet">Owner cmdlet if any</param>
        internal WebResponseContentMemoryStream(Stream stream, int initialCapacity, Cmdlet cmdlet)
            : base(initialCapacity)
        {
            _originalStreamToProxy = stream;
            _ownerCmdlet = cmdlet;
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool CanTimeout
        {
            get
            {
                return base.CanTimeout;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override long Length
        {
            get
            {
                Initialize();
                return base.Length;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="bufferSize"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override System.Threading.Tasks.Task CopyToAsync(Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken)
        {
            Initialize();
            return base.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            Initialize();
            return base.Read(buffer, offset, count);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
        {
            Initialize();
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int ReadByte()
        {
            Initialize();
            return base.ReadByte();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            Initialize();
            base.SetLength(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override byte[] ToArray()
        {
            Initialize();
            return base.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            Initialize();
            base.Write(buffer, offset, count);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
        {
            Initialize();
            return base.WriteAsync(buffer, offset, count, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void WriteByte(byte value)
        {
            Initialize();
            base.WriteByte(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        public override void WriteTo(Stream stream)
        {
            Initialize();
            base.WriteTo(stream);
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

#if !CORECLR
        /// <summary>
        /// 
        /// </summary>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            Initialize();
            return base.BeginRead(buffer, offset, count, callback, state);
        }

        /// <summary>
        /// 
        /// </summary>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            Initialize();
            return base.BeginWrite(buffer, offset, count, callback, state);
        }

        /// <summary>
        /// 
        /// </summary>
        public override byte[] GetBuffer()
        {
            Initialize();
            return base.GetBuffer();
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Close()
        {
            base.Close();
        }
#endif

        /// <summary>
        /// 
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized) { return; }
            _isInitialized = true;
            try
            {
                long totalLength = 0;
                byte[] buffer = new byte[StreamHelper.ChunkSize];
                for (int read = 1; 0 < read; totalLength += read)
                {
                    if (null != _ownerCmdlet)
                    {
                        ProgressRecord record = new ProgressRecord(StreamHelper.ActivityId,
                            WebCmdletStrings.ReadResponseProgressActivity,
                        StringUtil.Format(WebCmdletStrings.ReadResponseProgressStatus, totalLength));
                        _ownerCmdlet.WriteProgress(record);

                        if (_ownerCmdlet.IsStopping)
                        {
                            break;
                        }
                    }

                    read = _originalStreamToProxy.Read(buffer, 0, buffer.Length);

                    if (0 < read)
                    {
                        base.Write(buffer, 0, read);
                    }
                }

                if (_ownerCmdlet != null)
                {
                    ProgressRecord record = new ProgressRecord(StreamHelper.ActivityId,
                        WebCmdletStrings.ReadResponseProgressActivity,
                        StringUtil.Format(WebCmdletStrings.ReadResponseComplete, totalLength));
                    record.RecordType = ProgressRecordType.Completed;
                    _ownerCmdlet.WriteProgress(record);
                }

                // make sure the length is set appropriately
                base.SetLength(totalLength);
                base.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception)
            {
                base.Dispose();
                throw;
            }
        }
    }

    internal static class StreamHelper
    {
        #region Constants

        internal const int DefaultReadBuffer = 100000;

        internal const int ChunkSize = 10000;

        // just picked a random number
        internal const int ActivityId = 174593042;

        #endregion Constants

        #region Static Methods

        internal static void WriteToStream(Stream input, Stream output, PSCmdlet cmdlet)
        {
            byte[] data = new byte[ChunkSize];

            int read = 0;
            long totalWritten = 0;
            do
            {
                if (cmdlet != null)
                {
                    ProgressRecord record = new ProgressRecord(ActivityId,
                        WebCmdletStrings.WriteRequestProgressActivity,
                        StringUtil.Format(WebCmdletStrings.WriteRequestProgressStatus, totalWritten));
                    cmdlet.WriteProgress(record);
                }

                read = input.Read(data, 0, ChunkSize);

                if (0 < read)
                {
                    output.Write(data, 0, read);
                    totalWritten += read;
                }
            } while (read != 0);


            if (cmdlet != null)
            {
                ProgressRecord record = new ProgressRecord(ActivityId,
                    WebCmdletStrings.WriteRequestProgressActivity,
                    StringUtil.Format(WebCmdletStrings.WriteRequestComplete, totalWritten));
                record.RecordType = ProgressRecordType.Completed;
                cmdlet.WriteProgress(record);
            }

            output.Flush();
        }

        internal static void WriteToStream(byte[] input, Stream output)
        {
            output.Write(input, 0, input.Length);
            output.Flush();
        }

        /// <summary>
        /// Saves content from stream into filePath.
        /// Caller need to ensure <paramref name="stream"/> position is properly set.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="filePath"></param>
        /// <param name="cmdlet"></param>
        internal static void SaveStreamToFile(Stream stream, string filePath, PSCmdlet cmdlet)
        {
            using (FileStream output = File.Create(filePath))
            {
                WriteToStream(stream, output, cmdlet);
            }
        }

        internal static string DecodeStream(Stream stream, string characterSet)
        {
            Encoding encoding = ContentHelper.GetEncodingOrDefault(characterSet);
            return DecodeStream(stream, encoding);
        }

        internal static string DecodeStream(Stream stream, Encoding encoding)
        {
            if (null == encoding)
            {
                // just use the default encoding if one wasn't provided
                encoding = ContentHelper.GetDefaultEncoding();
            }

            StringBuilder result = new StringBuilder(capacity: ChunkSize);
            Decoder decoder = encoding.GetDecoder();

            int useBufferSize = 64;
            if (useBufferSize < encoding.GetMaxCharCount(10))
            {
                useBufferSize = encoding.GetMaxCharCount(10);
            }
            char[] chars = new char[useBufferSize];


            byte[] bytes = new byte[useBufferSize * 4];
            int bytesRead = 0;
            do
            {
                // Read at most the number of bytes that will fit in the input buffer. The  
                // return value is the actual number of bytes read, or zero if no bytes remain. 
                bytesRead = stream.Read(bytes, 0, useBufferSize * 4);

                bool completed = false;
                int byteIndex = 0;
                int bytesUsed;
                int charsUsed;

                while (!completed)
                {
                    // If this is the last input data, flush the decoder's internal buffer and state. 
                    bool flush = (bytesRead == 0);
                    decoder.Convert(bytes, byteIndex, bytesRead - byteIndex,
                                    chars, 0, useBufferSize, flush,
                                    out bytesUsed, out charsUsed, out completed);

                    // The conversion produced the number of characters indicated by charsUsed. Write that number 
                    // of characters to our result buffer
                    result.Append(chars, 0, charsUsed);

                    // Increment byteIndex to the next block of bytes in the input buffer, if any, to convert.
                    byteIndex += bytesUsed;
                }
            }
            while (bytesRead != 0);

            return result.ToString();
        }

        internal static Byte[] EncodeToBytes(String str, Encoding encoding)
        {
            if (null == encoding)
            {
                // just use the default encoding if one wasn't provided
                encoding = ContentHelper.GetDefaultEncoding();
            }

            return encoding.GetBytes(str);
        }

        internal static Byte[] EncodeToBytes(String str)
        {
            return EncodeToBytes(str, null);
        }

#if CORECLR
        internal static Stream GetResponseStream(HttpResponseMessage response)
        {
            Stream responseStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            var contentEncoding = response.Content.Headers.ContentEncoding;

            // HttpClient by default will automatically decompress GZip and Deflate content.
            // We keep this decompression logic here just in case.
            if (contentEncoding != null && contentEncoding.Count > 0)
            {
                if (contentEncoding.Contains("gzip"))
                {
                    responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                }
                else if (contentEncoding.Contains("deflate"))
                {
                    responseStream = new DeflateStream(responseStream, CompressionMode.Decompress);
                }
            }

            return responseStream;
        }
#else
        internal static Stream GetResponseStream(WebResponse response)
        {
            Stream responseStream = response.GetResponseStream();

            // See if it had a content-encoding, wrap in a decoding stream if so.
            string contentEncoding = response.Headers["Content-Encoding"];
            if (contentEncoding != null)
            {
                if (contentEncoding.IndexOf("gzip", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                }
                else if (contentEncoding.IndexOf("deflate", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    responseStream = new DeflateStream(responseStream, CompressionMode.Decompress);
                }
            }

            return responseStream;
        }
#endif

        #endregion Static Methods
    }
}
