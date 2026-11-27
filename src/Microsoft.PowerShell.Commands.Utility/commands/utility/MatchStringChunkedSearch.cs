// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Searching one large UTF-8 file on several threads with exactly the results of reading it in one pass.
//
// The file is cut into chunks that each start just after a '\n' byte. In UTF-8 that byte is never part of
// another character, so decoding each chunk alone gives the same characters as decoding the whole file, and no
// line, and no "\r\n" pair, is split between chunks. Each chunk counts its own lines; line numbers are made
// global afterwards by adding the line counts of the chunks before it. The last chunk reads to the end of the file
// as it is when that chunk reaches it, as a one-pass read would.
using System;
using System.IO;
using System.Management.Automation.Internal;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.PowerShell.Commands
{
    internal static class ChunkedFile
    {
        // Files below this are read in one pass; chunks are about this size. Tests lower both through InternalTestHooks.
        internal static long MinLength => InternalTestHooks.SelectStringSplitMinLength > 0 ? InternalTestHooks.SelectStringSplitMinLength : 64L << 20;

        internal static int ChunkLength => InternalTestHooks.SelectStringChunkLength > 0 ? InternalTestHooks.SelectStringChunkLength : 16 << 20;

        // A chunk that runs to the end of the file.
        internal const long ToEnd = long.MaxValue;

        // How far past a chunk's nominal end a line end is looked for.
        internal const long MaxProbeLength = 4L << 20;

        private static readonly Type s_sealedUtf8Type = Encoding.UTF8.GetType();

        // How many chunk searches have been started in this process; read by tests to see that a file was split.
        private static int s_chunksStarted;

        internal static int ChunksStarted => Volatile.Read(ref s_chunksStarted);

        internal static void CountChunk() => Interlocked.Increment(ref s_chunksStarted);

        /// <summary>
        /// The start of the chunk after the one starting at <paramref name="start"/>: just after the first '\n' at or
        /// past start plus the chunk length. <see cref="ToEnd"/> when the chunk at <paramref name="start"/> is the
        /// last: no '\n' before the end of the file, or within <see cref="MaxProbeLength"/> bytes of where the look
        /// starts (a file with CR-only line ends or very long lines would otherwise be read to the end before its first
        /// chunk is searched; it is then searched as one chunk). Looks 4 KB at a time, doubling up to 1 MB, and checks
        /// for cancellation before each look.
        /// </summary>
        internal static long NextStart(SafeFileHandle handle, long start, CancellationToken token)
        {
            long length = RandomAccess.GetLength(handle);
            long pos = start + ChunkLength;
            long stop = Math.Min(length, pos + MaxProbeLength);
            int probeLength = 4 * 1024;
            byte[] probe = System.Buffers.ArrayPool<byte>.Shared.Rent(1 << 20);
            try
            {
                while (pos < stop)
                {
                    token.ThrowIfCancellationRequested();
                    int n = RandomAccess.Read(handle, probe.AsSpan(0, probeLength), pos);
                    if (n == 0)
                    {
                        break;
                    }

                    int i = Array.IndexOf(probe, (byte)'\n', 0, n);
                    if (i >= 0)
                    {
                        long next = pos + i + 1;
                        return next < length ? next : ToEnd;
                    }

                    pos += n;
                    probeLength = Math.Min(probeLength * 2, 1 << 20);
                }

                return ToEnd;
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(probe);
            }
        }

        /// <summary>
        /// Whether this file may be split: the text encoding is UTF-8, replaces each invalid byte with U+FFFD, and the file does not start with a UTF-16 or UTF-32 byte-order mark (which would
        /// make StreamReader decode it as that encoding instead).
        /// </summary>
        internal static bool CanSplit(SafeFileHandle handle, long length, Encoding encoding)
        {
            // Only the standard replacement character: with a byte-order mark StreamReader decodes with Encoding.UTF8,
            // so a custom replacement would give different text in one pass than in chunks.
            // Only the runtime's own UTF-8 classes (UTF8Encoding and the sealed one behind Encoding.UTF8 and
            // Encoding.Default): another class derived from UTF8Encoding may define its own byte-order mark, which
            // StreamReader would recognise and the chunks would not.
            if (length < MinLength || encoding == null
                || (encoding.GetType() != typeof(UTF8Encoding) && encoding.GetType() != s_sealedUtf8Type)
                || encoding.DecoderFallback is not DecoderReplacementFallback { DefaultString: "�" })
            {
                return false;
            }

            Span<byte> head = stackalloc byte[4];
            int n = ReadFully(handle, head, 0);
            if (n >= 2 && ((head[0] == 0xFF && head[1] == 0xFE) || (head[0] == 0xFE && head[1] == 0xFF)))
            {
                return false;
            }

            if (n >= 4 && head[0] == 0 && head[1] == 0 && head[2] == 0xFE && head[3] == 0xFF)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Whether the file starts with a UTF-8 byte-order mark. StreamReader then skips it and decodes the whole file
        /// with Encoding.UTF8, whatever replacement character the given encoding uses.
        /// </summary>
        internal static bool HasUtf8Bom(SafeFileHandle handle)
        {
            Span<byte> bom = stackalloc byte[3];
            return ReadFully(handle, bom, 0) == 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF;
        }

        /// <summary>Reads until the span is full or the file ends; returns the bytes read.</summary>
        internal static int ReadFully(SafeFileHandle handle, Span<byte> buffer, long offset)
        {
            int got = 0;
            while (got < buffer.Length)
            {
                int n = RandomAccess.Read(handle, buffer.Slice(got), offset + got);
                if (n == 0)
                {
                    break;
                }

                got += n;
            }

            return got;
        }
    }

    /// <summary>
    /// Reads one byte range of a file as text, as StreamReader would decode those bytes in the middle of the file:
    /// 1 MB reads into a reused buffer, decoded with the encoding's own decoder. The range may run to the end of the
    /// file (<see cref="ChunkedFile.ToEnd"/>).
    /// </summary>
    internal sealed class ChunkTextReader : TextReader
    {
        private const int ReadLength = 1 << 20;

        private readonly SafeFileHandle _handle;
        private readonly long _end;
        private readonly Decoder _decoder;
        private readonly CancellationToken _token;
        private byte[] _bytes;
        private char[] _chars;
        private long _position;
        private bool _flushed;

        // Decoded characters not yet handed out (the caller may ask for fewer than one read decodes to).
        private int _charStart;
        private int _charEnd;

        /// <param name="handle">The file.</param>
        /// <param name="start">The first byte to read (after any byte-order mark).</param>
        /// <param name="end">The byte after the last to read, or <see cref="ChunkedFile.ToEnd"/>.</param>
        /// <param name="encoding">An encoding that decodes each invalid byte to at most one character.</param>
        /// <param name="token">Checked before each read.</param>
        public ChunkTextReader(SafeFileHandle handle, long start, long end, Encoding encoding, CancellationToken token)
        {
            _handle = handle;
            _position = start;
            _end = end;
            _token = token;
            _decoder = encoding.GetDecoder();
            _bytes = System.Buffers.ArrayPool<byte>.Shared.Rent(ReadLength);
            _chars = System.Buffers.ArrayPool<char>.Shared.Rent(ReadLength);
        }

        public override int Read(char[] buffer, int index, int count)
        {
            while (_charStart == _charEnd)
            {
                if (!Decode())
                {
                    return 0;
                }
            }

            int n = Math.Min(count, _charEnd - _charStart);
            Array.Copy(_chars, _charStart, buffer, index, n);
            _charStart += n;
            return n;
        }

        // Reads and decodes the next piece into _chars. False at the end of the range, once the decoder is flushed.
        private bool Decode()
        {
            _token.ThrowIfCancellationRequested();
            _charStart = 0;
            _charEnd = 0;
            if (_position >= _end)
            {
                if (_flushed)
                {
                    return false;
                }

                _flushed = true;
                _charEnd = _decoder.GetChars(Array.Empty<byte>(), 0, 0, _chars, 0, flush: true);
                return true;
            }

            // A few bytes held over by the decoder can add characters; reading 16 fewer bytes keeps room for them.
            int want = (int)Math.Min(ReadLength - 16, _end - _position);
            int got = RandomAccess.Read(_handle, _bytes.AsSpan(0, want), _position);
            if (got == 0)
            {
                // The end of the file.
                _position = _end;
                return true;
            }

            _position += got;
            _charEnd = _decoder.GetChars(_bytes, 0, got, _chars, 0, flush: false);
            return true;
        }

        public override int Read() => throw new NotSupportedException();

        public override int Peek() => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                byte[] bytes = Interlocked.Exchange(ref _bytes, null);
                if (bytes != null)
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(bytes);
                }

                char[] chars = Interlocked.Exchange(ref _chars, null);
                if (chars != null)
                {
                    System.Buffers.ArrayPool<char>.Shared.Return(chars);
                }
            }

            base.Dispose(disposing);
        }
    }
}
