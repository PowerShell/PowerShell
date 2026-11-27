// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Searching one large UTF-8 file on several threads with exactly the results of reading it in one pass.
//
// The file is cut into chunks that each start just after a '\n' byte. In UTF-8 that byte is never part of
// another character, so decoding each chunk alone gives the same characters as decoding the whole file, and no
// line, and no "\r\n" pair, is split between chunks. Each chunk counts its own lines; line numbers are made
// global afterwards by adding the line counts of the chunks before it.
//
// Each chunk reads through its own file handle, because Windows completes reads on one synchronous handle one at
// a time even when they name different offsets (one shared handle: 0.57 s for a 3 GB file; one per chunk: 0.30 s).
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation.Internal;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.PowerShell.Commands
{
    internal static class ChunkedFile
    {
        // Files below this are read in one pass; chunks are about this size. Tests lower both through InternalTestHooks.
        internal static long MinLength => InternalTestHooks.SelectStringSplitMinLength > 0 ? InternalTestHooks.SelectStringSplitMinLength : 64L << 20;

        internal static int ChunkLength => InternalTestHooks.SelectStringChunkLength > 0 ? InternalTestHooks.SelectStringChunkLength : 16 << 20;

        /// <summary>
        /// Chunk start offsets: 0, then just after the first '\n' at or past each multiple of the chunk length.
        /// </summary>
        internal static List<long> Cut(SafeFileHandle handle, long length)
        {
            var starts = new List<long> { 0 };
            // 4 KB per look: lines are usually far shorter, and a longer line just takes more looks.
            byte[] probe = new byte[4 * 1024];
            long target = ChunkLength;
            while (target < length)
            {
                long pos = target;
                long found = -1;
                while (pos < length)
                {
                    int n = RandomAccess.Read(handle, probe, pos);
                    if (n == 0)
                    {
                        break;
                    }

                    int i = Array.IndexOf(probe, (byte)'\n', 0, n);
                    if (i >= 0)
                    {
                        found = pos + i + 1;
                        break;
                    }

                    pos += n;
                }

                if (found < 0 || found >= length)
                {
                    break;
                }

                starts.Add(found);
                target = found + ChunkLength;
            }

            return starts;
        }

        /// <summary>
        /// Whether this file may be split: the text encoding is UTF-8 and the file does not start with a UTF-16 or
        /// UTF-32 byte-order mark (which would make StreamReader decode it as that encoding instead).
        /// </summary>
        internal static bool CanSplit(SafeFileHandle handle, long length, Encoding encoding)
        {
            if (length < MinLength || encoding == null || encoding.CodePage != 65001)
            {
                return false;
            }

            Span<byte> head = stackalloc byte[4];
            int n = RandomAccess.Read(handle, head, 0);
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
    }

    /// <summary>
    /// Reads one byte range of a file as text, as StreamReader would decode those bytes in the middle of the file:
    /// 1 MB reads into a reused buffer, decoded with the encoding's own decoder. For the first chunk, a UTF-8
    /// byte-order mark is skipped, as StreamReader skips it.
    /// </summary>
    internal sealed class ChunkTextReader : TextReader
    {
        private const int ReadLength = 1 << 20;

        private readonly SafeFileHandle _handle;
        private readonly long _end;
        private readonly Decoder _decoder;
        private readonly byte[] _bytes = System.Buffers.ArrayPool<byte>.Shared.Rent(ReadLength);
        private long _position;
        private bool _flushed;

        public ChunkTextReader(SafeFileHandle handle, long start, long end, Encoding encoding)
        {
            _handle = handle;
            _position = start;
            _end = end;
            _decoder = encoding.GetDecoder();
            if (start == 0)
            {
                Span<byte> bom = stackalloc byte[3];
                int n = RandomAccess.Read(handle, bom, 0);
                if (n == 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF && end >= 3)
                {
                    _position = 3;
                }
            }
        }

        // Decoded characters not yet handed out (the caller may ask for fewer than one read decodes to).
        private readonly char[] _chars = System.Buffers.ArrayPool<char>.Shared.Rent(ReadLength);
        private int _charStart;
        private int _charEnd;

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
                System.Buffers.ArrayPool<byte>.Shared.Return(_bytes);
                System.Buffers.ArrayPool<char>.Shared.Return(_chars);
            }

            base.Dispose(disposing);
        }
    }
}
