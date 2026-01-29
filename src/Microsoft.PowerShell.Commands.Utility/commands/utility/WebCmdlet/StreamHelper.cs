// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Buffers;
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
    internal sealed class WebResponseContentMemoryStream : MemoryStream
    {
        #region Data

        private readonly long? _contentLength;
        private readonly Stream _originalStreamToProxy;
        private readonly Cmdlet? _ownerCmdlet;
        private readonly CancellationToken _cancellationToken;
        private readonly TimeSpan _perReadTimeout;
        private bool _isInitialized = false;

        #endregion Data

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="WebResponseContentMemoryStream"/> class.
        /// </summary>
        /// <param name="stream">Response stream.</param>
        /// <param name="initialCapacity">Presize the memory stream.</param>
        /// <param name="cmdlet">Owner cmdlet if any.</param>
        /// <param name="contentLength">Expected download size in Bytes.</param>
        /// <param name="perReadTimeout">Time permitted between reads or Timeout.InfiniteTimeSpan for no timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        internal WebResponseContentMemoryStream(Stream stream, int initialCapacity, Cmdlet? cmdlet, long? contentLength, TimeSpan perReadTimeout, CancellationToken cancellationToken) : base(initialCapacity)
        {
            this._contentLength = contentLength;
            _originalStreamToProxy = stream;
            _ownerCmdlet = cmdlet;
            _cancellationToken = cancellationToken;
            _perReadTimeout = perReadTimeout;
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
            Initialize(cancellationToken);
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
            Initialize(cancellationToken);
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
            Initialize(cancellationToken);
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

        private void Initialize(CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
            {
                return;
            }

            if (cancellationToken == default)
            {
                cancellationToken = _cancellationToken;
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

                    read = _originalStreamToProxy.ReadAsync(buffer.AsMemory(), _perReadTimeout, cancellationToken).GetAwaiter().GetResult();

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
                Seek(0, SeekOrigin.Begin);
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }
    }

    internal static class StreamTimeoutExtensions
    {
        internal static async Task<int> ReadAsync(this Stream stream, Memory<byte> buffer, TimeSpan readTimeout, CancellationToken cancellationToken)
        {
            if (readTimeout == Timeout.InfiniteTimeSpan)
            {
                return await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                cts.CancelAfter(readTimeout);
                return await stream.ReadAsync(buffer, cts.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex)
            {
                if (cts.IsCancellationRequested)
                {
                    throw new TimeoutException($"The request was canceled due to the configured OperationTimeout of {readTimeout.TotalSeconds} seconds elapsing", ex);
                }
                else
                {
                    throw;
                }
            }
        }

        internal static async Task CopyToAsync(this Stream source, Stream destination, TimeSpan perReadTimeout, CancellationToken cancellationToken)
        {
            if (perReadTimeout == Timeout.InfiniteTimeSpan)
            {
                // No timeout - use fast path
                await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
                return;
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(StreamHelper.ChunkSize);
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                while (true)
                {
                    if (!cts.TryReset())
                    {
                        cts.Dispose();
                        cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    }

                    cts.CancelAfter(perReadTimeout);
                    int bytesRead = await source.ReadAsync(buffer, cts.Token).ConfigureAwait(false);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException ex)
            {
                if (cts.IsCancellationRequested)
                {
                    throw new TimeoutException($"The request was canceled due to the configured OperationTimeout of {perReadTimeout.TotalSeconds} seconds elapsing", ex);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                cts.Dispose();
                ArrayPool<byte>.Shared.Return(buffer);
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

        internal static void WriteToStream(Stream input, Stream output, PSCmdlet cmdlet, long? contentLength, TimeSpan perReadTimeout, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(cmdlet);

            Task copyTask = input.CopyToAsync(output, perReadTimeout, cancellationToken);

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
        /// <param name="perReadTimeout">Time permitted between reads or Timeout.InfiniteTimeSpan for no timeout.</param>
        /// <param name="cancellationToken">CancellationToken to track the cmdlet cancellation.</param>
        internal static void SaveStreamToFile(Stream stream, string filePath, PSCmdlet cmdlet, long? contentLength, TimeSpan perReadTimeout, CancellationToken cancellationToken)
        {
            // If the web cmdlet should resume, append the file instead of overwriting.
            FileMode fileMode = cmdlet is WebRequestPSCmdlet webCmdlet && webCmdlet.ShouldResume ? FileMode.Append : FileMode.Create;
            using FileStream output = new(filePath, fileMode, FileAccess.Write, FileShare.Read);
            WriteToStream(stream, output, cmdlet, contentLength, perReadTimeout, cancellationToken);
        }

        private static string StreamToString(Stream stream, Encoding encoding, TimeSpan perReadTimeout, CancellationToken cancellationToken)
        {
            StringBuilder result = new(capacity: ChunkSize);
            Decoder decoder = encoding.GetDecoder();

            int useBufferSize = 64;
            if (useBufferSize < encoding.GetMaxCharCount(10))
            {
                useBufferSize = encoding.GetMaxCharCount(10);
            }

            char[] chars = ArrayPool<char>.Shared.Rent(useBufferSize);
            byte[] bytes = ArrayPool<byte>.Shared.Rent(useBufferSize * 4);
            try
            {
                int bytesRead = 0;
                do
                {
                    // Read at most the number of bytes that will fit in the input buffer. The
                    // return value is the actual number of bytes read, or zero if no bytes remain.
                    bytesRead = stream.ReadAsync(bytes.AsMemory(), perReadTimeout, cancellationToken).GetAwaiter().GetResult();

                    bool completed = false;
                    int byteIndex = 0;

                    while (!completed)
                    {
                        // If this is the last input data, flush the decoder's internal buffer and state.
                        bool flush = bytesRead is 0;
                        decoder.Convert(bytes, byteIndex, bytesRead - byteIndex, chars, 0, useBufferSize, flush, out int bytesUsed, out int charsUsed, out completed);

                        // The conversion produced the number of characters indicated by charsUsed. Write that number
                        // of characters to our result buffer
                        result.Append(chars, 0, charsUsed);

                        // Increment byteIndex to the next block of bytes in the input buffer, if any, to convert.
                        byteIndex += bytesUsed;

                        // The behavior of decoder.Convert changed start .NET 3.1-preview2.
                        // The change was made in https://github.com/dotnet/coreclr/pull/27229
                        // The recommendation from .NET team is to not check for 'completed' if 'flush' is false.
                        // Break out of the loop if all bytes have been read.
                        if (!flush && bytesRead == byteIndex)
                        {
                            break;
                        }
                    }
                }
                while (bytesRead != 0);

                return result.ToString();
            }
            finally
            {
                ArrayPool<char>.Shared.Return(chars);
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        internal static string DecodeStream(Stream stream, string? characterSet, out Encoding encoding, TimeSpan perReadTimeout, CancellationToken cancellationToken)
        {
            bool isDefaultEncoding = !TryGetEncoding(characterSet, out encoding);

            string content = StreamToString(stream, encoding, perReadTimeout, cancellationToken);
            if (isDefaultEncoding)
            {
                // We only look within the first 1k characters as the meta element and
                // the xml declaration are at the start of the document
                string substring = content.Substring(0, Math.Min(content.Length, 1024));

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

                    if (TryGetEncoding(characterSet, out Encoding localEncoding))
                    {
                        // We try again to look for the meta element to test if it makes sense to change encoding
                        // For example to change from the default encoding (UTF8) to UTF16 will make the meta element unreadable
                        substring = localEncoding.GetString(encoding.GetBytes(match.Value));
                        match = s_metaRegex.Match(substring);
                        if (!match.Success)
                        {
                            match = s_xmlRegex.Match(substring);
                        }

                        if (match.Success && match.Groups["charset"].Value == characterSet)
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            content = StreamToString(stream, localEncoding, perReadTimeout, cancellationToken);
                            encoding = localEncoding;
                        }
                    }
                }
            }

            return content;
        }

        internal static bool TryGetEncoding(string? characterSet, out Encoding encoding)
        {
            bool result = false;
            try
            {
                encoding = Encoding.GetEncoding(characterSet!);
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

        internal static Stream GetResponseStream(HttpResponseMessage response, CancellationToken cancellationToken) => response.Content.ReadAsStreamAsync(cancellationToken).GetAwaiter().GetResult();

        #endregion Static Methods
    }
}
