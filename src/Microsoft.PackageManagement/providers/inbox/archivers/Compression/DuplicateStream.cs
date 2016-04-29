//---------------------------------------------------------------------
// <copyright file="DuplicateStream.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Archivers.Internal.Compression
{
    using System;
    using System.IO;

    /// <summary>
    /// Duplicates a source stream by maintaining a separate position.
    /// </summary>
    /// <remarks>
    /// WARNING: duplicate streams are not thread-safe with respect to each other or the original stream.
    /// If multiple threads use duplicate copies of the same stream, they must synchronize for any operations.
    /// </remarks>
    public class DuplicateStream : Stream
    {
        private Stream source;
        private long position;

        /// <summary>
        /// Creates a new duplicate of a stream.
        /// </summary>
        /// <param name="source">source of the duplicate</param>
        public DuplicateStream(Stream source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            this.source = DuplicateStream.OriginalStream(source);
        }

        /// <summary>
        /// Gets the original stream that was used to create the duplicate.
        /// </summary>
        public Stream Source
        {
            get
            {
                return this.source;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the source stream supports reading.
        /// </summary>
        /// <value>true if the stream supports reading; otherwise, false.</value>
        public override bool CanRead
        {
            get
            {
                return this.source.CanRead;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the source stream supports writing.
        /// </summary>
        /// <value>true if the stream supports writing; otherwise, false.</value>
        public override bool CanWrite
        {
            get
            {
                return this.source.CanWrite;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the source stream supports seeking.
        /// </summary>
        /// <value>true if the stream supports seeking; otherwise, false.</value>
        public override bool CanSeek
        {
            get
            {
                return this.source.CanSeek;
            }
        }

        /// <summary>
        /// Gets the length of the source stream.
        /// </summary>
        public override long Length
        {
            get
            {
                return this.source.Length;
            }
        }

        /// <summary>
        /// Gets or sets the position of the current stream,
        /// ignoring the position of the source stream.
        /// </summary>
        public override long Position
        {
            get
            {
                return this.position;
            }

            set
            {
                this.position = value;
            }
        }

        /// <summary>
        /// Retrieves the original stream from a possible duplicate stream.
        /// </summary>
        /// <param name="stream">Possible duplicate stream.</param>
        /// <returns>If the stream is a DuplicateStream, returns
        /// the duplicate's source; otherwise returns the same stream.</returns>
        public static Stream OriginalStream(Stream stream)
        {
            DuplicateStream dupStream = stream as DuplicateStream;
            return dupStream != null ? dupStream.Source : stream;
        }

        /// <summary>
        /// Flushes the source stream.
        /// </summary>
        public override void Flush()
        {
            this.source.Flush();
        }

        /// <summary>
        /// Sets the length of the source stream.
        /// </summary>
        /// <param name="value">The desired length of the stream in bytes.</param>
        public override void SetLength(long value)
        {
            this.source.SetLength(value);
        }

#if !CORECLR
        /// <summary>
        /// Closes the underlying stream, effectively closing ALL duplicates.
        /// </summary>
        public override void Close()
        {
            this.source.Close();
        }
#endif

        /// <summary>
        /// Disposes the stream
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.source.Dispose();
            }
        }

        /// <summary>
        /// Reads from the source stream while maintaining a separate position
        /// and not impacting the source stream's position.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer
        /// contains the specified byte array with the values between offset and
        /// (offset + count - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin
        /// storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less
        /// than the number of bytes requested if that many bytes are not currently available,
        /// or zero (0) if the end of the stream has been reached.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            long saveSourcePosition = this.source.Position;
            this.source.Position = this.position;
            int read = this.source.Read(buffer, offset, count);
            this.position = this.source.Position;
            this.source.Position = saveSourcePosition;
            return read;
        }

        /// <summary>
        /// Writes to the source stream while maintaining a separate position
        /// and not impacting the source stream's position.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count
        /// bytes from buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which
        /// to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the
        /// current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            long saveSourcePosition = this.source.Position;
            this.source.Position = this.position;
            this.source.Write(buffer, offset, count);
            this.position = this.source.Position;
            this.source.Position = saveSourcePosition;
        }

        /// <summary>
        /// Changes the position of this stream without impacting the
        /// source stream's position.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type SeekOrigin indicating the reference
        /// point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            long originPosition = 0;
            if (origin == SeekOrigin.Current)
            {
                originPosition = this.position;
            }
            else if (origin == SeekOrigin.End)
            {
                originPosition = this.Length;
            }

            this.position = originPosition + offset;
            return this.position;
        }
    }
}
