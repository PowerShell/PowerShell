//---------------------------------------------------------------------
// <copyright file="ConcatStream.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Archivers.Internal.Compression.Zip
{
    using System;
    using System.IO;

    /// <summary>
    /// Used to trick a DeflateStream into reading from or writing to
    /// a series of (chunked) streams instead of a single stream.
    /// </summary>
    internal class ConcatStream : Stream
    {
        private Stream source;
        private long position;
        private long length;
        private Action<ConcatStream> nextStreamHandler;

        public ConcatStream(Action<ConcatStream> nextStreamHandler)
        {
            if (nextStreamHandler == null)
            {
                throw new ArgumentNullException("nextStreamHandler");
            }

            this.nextStreamHandler = nextStreamHandler;
            this.length = Int64.MaxValue;
        }

        public Stream Source
        {
            get { return this.source; }
            set { this.source = value; }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override long Length
        {
            get
            {
                return this.length;
            }
        }

        public override long Position
        {
            get { return this.position; }
            set { throw new NotSupportedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.source == null)
            {
                this.nextStreamHandler(this);
            }

            count = (int) Math.Min(count, this.length - this.position);

            int bytesRemaining = count;
            while (bytesRemaining > 0)
            {
                if (this.source == null)
                {
                    throw new InvalidOperationException();
                }

                int partialCount = (int) Math.Min(bytesRemaining,
                    this.source.Length - this.source.Position);

                if (partialCount == 0)
                {
                    this.nextStreamHandler(this);
                    continue;
                }

                partialCount = this.source.Read(
                    buffer, offset + count - bytesRemaining, partialCount);
                bytesRemaining -= partialCount;
                this.position += partialCount;
            }

            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.source == null)
            {
                this.nextStreamHandler(this);
            }

            int bytesRemaining = count;
            while (bytesRemaining > 0)
            {
                if (this.source == null)
                {
                    throw new InvalidOperationException();
                }

                int partialCount = (int) Math.Min(bytesRemaining,
                    Math.Max(0, this.length - this.source.Position));

                if (partialCount == 0)
                {
                    this.nextStreamHandler(this);
                    continue;
                }

                this.source.Write(
                    buffer, offset + count - bytesRemaining, partialCount);
                bytesRemaining -= partialCount;
                this.position += partialCount;
            }
        }

        public override void Flush()
        {
            if (this.source != null)
            {
                this.source.Flush();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            this.length = value;
        }

#if !CORECLR
        /// <summary>
        /// Closes underlying stream
        /// </summary>
        public override void Close()
        {
            if (this.source != null)
            {
                this.source.Close();
            }
        }
#endif

        /// <summary>
        /// Disposes underlying stream
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.source.Dispose();
            }
        }
    }
}
