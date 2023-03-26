// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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

        private readonly long? _contentLength;
        private readonly Stream _originalStreamToProxy;
        private bool _isInitialized = false;
        private readonly Cmdlet _ownerCmdlet;

        #endregion Data

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="WebResponseContentMemoryStream"/> class.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="initialCapacity"></param>
        /// <param name="cmdlet">Owner cmdlet if any.</param>
        /// <param name="contentLength">Expected download size in Bytes.</param>
        internal WebResponseContentMemoryStream(Stream stream, int initialCapacity, Cmdlet cmdlet, long? contentLength) : base(initialCapacity)
        {
            this._contentLength = contentLength;
            _originalStreamToProxy = stream;
            _ownerCmdlet = cmdlet;
        }
        #endregion Constructors

        /// <summary>
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// </summary>
        public override bool CanSeek => true;

        /// <summary>
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
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
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="bufferSize"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            Initialize();
            return base.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        /// <summary>
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
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Initialize();
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override int ReadByte()
        {
            Initialize();
            return base.ReadByte();
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            Initialize();
            base.SetLength(value);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override byte[] ToArray()
        {
            Initialize();
            return base.ToArray();
        }

        /// <summary>
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
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Initialize();
            return base.WriteAsync(buffer, offset, count, cancellationToken);
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        public override void WriteByte(byte value)
        {
            Initialize();
            base.WriteByte(value);
        }

        /// <summary>
        /// </summary>
        /// <param name="stream"></param>
        public override void WriteTo(Stream stream)
        {
            Initialize();
            base.WriteTo(stream);
        }

        /// <summary>
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        /// <summary>
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized) 
            {
                return;
            }

            _isInitialized = true;
            try
            {
                long totalRead = 0;
                byte[] buffer = new byte[StreamHelper.ChunkSize];
                ProgressRecord record = new(StreamHelper.ActivityId, WebCmdletStrings.ReadResponseProgressActivity, "statusDescriptionPlaceholder");
                string totalDownloadSize = _contentLength is null ? "???" : Utils.DisplayHumanReadableFileSize((long)_contentLength);
                for (int read = 1; read > 0; totalRead += read)
                {
                    if (_ownerCmdlet is not null)
                    {
                        record.StatusDescription = StringUtil.Format(
                            WebCmdletStrings.ReadResponseProgressStatus,
                            Utils.DisplayHumanReadableFileSize(totalRead),
                            totalDownloadSize);

                        if (_contentLength > 0)
                        {
                            record.PercentComplete = Math.Min((int)(totalRead * 100 / (long)_contentLength), 100);
                        }

                        _ownerCmdlet.WriteProgress(record);

                        if (_ownerCmdlet.IsStopping)
                        {
                            break;
                        }
                    }

                    read = _originalStreamToProxy.Read(buffer, 0, buffer.Length);

                    if (read > 0)
                    {
                        base.Write(buffer, 0, read);
                    }
                }

                if (_ownerCmdlet is not null)
                {
                    record.StatusDescription = StringUtil.Format(WebCmdletStrings.ReadResponseComplete, totalRead);
                    record.RecordType = ProgressRecordType.Completed;
                    _ownerCmdlet.WriteProgress(record);
                }

                // Make sure the length is set appropriately
                base.SetLength(totalRead);
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

        // Just picked a random number
        internal const int ActivityId = 174593042;

        #endregion Constants

        #region Static Methods

        internal static void WriteToStream(Stream input, Stream output, PSCmdlet cmdlet, long? contentLength, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(cmdlet);

            Task copyTask = input.CopyToAsync(output, cancellationToken);

            bool wroteProgress = false;
            ProgressRecord record = new(
                ActivityId,
                WebCmdletStrings.WriteRequestProgressActivity,
                WebCmdletStrings.WriteRequestProgressStatus);
            string totalDownloadSize = contentLength is null ? "???" : Utils.DisplayHumanReadableFileSize((long)contentLength);

            try
            {
                while (!copyTask.Wait(1000, cancellationToken))
                {
                    record.StatusDescription = StringUtil.Format(
                        WebCmdletStrings.WriteRequestProgressStatus,
                        Utils.DisplayHumanReadableFileSize(output.Position),
                        totalDownloadSize);

                    if (contentLength > 0)
                    {
                        record.PercentComplete = Math.Min((int)(output.Position * 100 / (long)contentLength), 100);
                    }

                    cmdlet.WriteProgress(record);
                    wroteProgress = true;
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (wroteProgress)
                {
                    // Write out the completion progress record only if we did render the progress.
                    record.StatusDescription = StringUtil.Format(
                        copyTask.IsCompleted
                            ? WebCmdletStrings.WriteRequestComplete
                            : WebCmdletStrings.WriteRequestCancelled,
                        output.Position);
                    record.RecordType = ProgressRecordType.Completed;
                    cmdlet.WriteProgress(record);
                }
            }
        }

        /// <summary>
        /// Saves content from stream into filePath.
        /// Caller need to ensure <paramref name="stream"/> position is properly set.
        /// </summary>
        /// <param name="stream">Input stream.</param>
        /// <param name="filePath">Output file name.</param>
        /// <param name="cmdlet">Current cmdlet (Invoke-WebRequest or Invoke-RestMethod).</param>
        /// <param name="contentLength">Expected download size in Bytes.</param>
        /// <param name="cancellationToken">CancellationToken to track the cmdlet cancellation.</param>
        internal static void SaveStreamToFile(Stream stream, string filePath, PSCmdlet cmdlet, long? contentLength, CancellationToken cancellationToken)
        {
            // If the web cmdlet should resume, append the file instead of overwriting.
            FileMode fileMode = cmdlet is WebRequestPSCmdlet webCmdlet && webCmdlet.ShouldResume ? FileMode.Append : FileMode.Create;
            using FileStream output = new(filePath, fileMode, FileAccess.Write, FileShare.Read);
            WriteToStream(stream, output, cmdlet, contentLength, cancellationToken);
        }

        internal static string DecodeStream(Stream stream, string characterSet, out Encoding encoding)
        {
            bool isDefaultEncoding = !TryGetEncodingFromCharset(characterSet, out encoding);

            using StreamReader reader = new(stream, encoding, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

            // reader.Peek();
            encoding = reader.CurrentEncoding;

            if (isDefaultEncoding)
            {
                // We only look within the first 1k characters as the meta element and
                // the xml declaration are at the start of the document
                int bufferLength = (int)Math.Min(reader.BaseStream.Length, 1024);

                char[] buffer = new char[bufferLength];
                reader.ReadBlock(buffer, 0, bufferLength);
                stream.Seek(0, SeekOrigin.Begin);

                string substring = new(buffer);

                // Check for a charset attribute on the meta element to override the default
                Match match = s_metaRegex.Match(substring);
                
                // Check for a encoding attribute on the xml declaration to override the default
                if (!match.Success)
                {
                    match = s_xmlRegex.Match(substring);
                }
                
                if (match.Success)
                {
                    characterSet = match.Groups["charset"].Value;

                    if (TryGetEncodingFromCharset(characterSet, out Encoding localEncoding))
                    {
                        encoding = localEncoding;
                    }
                }
            }

            return new StreamReader(stream, encoding, leaveOpen: true).ReadToEnd();
        }

        internal static bool TryGetEncodingFromCharset(string characterSet, out Encoding encoding)
        {
            bool result = false;
            try
            {
                encoding = Encoding.GetEncoding(characterSet);
                result = true;
            }
            catch (ArgumentException)
            {
                // Use the default encoding if one wasn't provided
                encoding = ContentHelper.GetDefaultEncoding();
            }

            return result;
        }

        private static readonly Regex s_metaRegex = new(
                @"<meta\s.*[^.><]*charset\s*=\s*[""'\n]?(?<charset>[A-Za-z].[^\s""'\n<>]*)[\s""'\n>]",
                RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking
            );
        
        private static readonly Regex s_xmlRegex = new(
                @"<\?xml\s.*[^.><]*encoding\s*=\s*[""'\n]?(?<charset>[A-Za-z].[^\s""'\n<>]*)[\s""'\n>]",
                RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking
            ); 

        internal static byte[] EncodeToBytes(string str, Encoding encoding)
        {
            // Just use the default encoding if one wasn't provided
            encoding ??= ContentHelper.GetDefaultEncoding();

            return encoding.GetBytes(str);
        }

        internal static string GetResponseString(HttpResponseMessage response) => response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        internal static Stream GetResponseStream(HttpResponseMessage response) => response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();

        #endregion Static Methods
    }
}
