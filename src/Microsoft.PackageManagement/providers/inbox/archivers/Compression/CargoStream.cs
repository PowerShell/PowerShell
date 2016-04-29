//---------------------------------------------------------------------
// <copyright file="CargoStream.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Archivers.Internal.Compression
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Wraps a source stream and carries additional items that are disposed when the stream is closed.
    /// </summary>
    public class CargoStream : Stream
    {
        private Stream source;
        private List<IDisposable> cargo;

        /// <summary>
        /// Creates a new a cargo stream.
        /// </summary>
        /// <param name="source">source of the stream</param>
        /// <param name="cargo">List of additional items that are disposed when the stream is closed.
        /// The order of the list is the order in which the items are disposed.</param>
        public CargoStream(Stream source, params IDisposable[] cargo)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            this.source = source;
            this.cargo = new List<IDisposable>(cargo);
        }

        /// <summary>
        /// Gets the source stream of the cargo stream.
        /// </summary>
        public Stream Source
        {
            get
            {
                return this.source;
            }
        }

        /// <summary>
        /// Gets the list of additional items that are disposed when the stream is closed.
        /// The order of the list is the order in which the items are disposed. The contents can be modified any time.
        /// </summary>
        public IList<IDisposable> Cargo
        {
            get
            {
                return this.cargo;
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
        /// Gets or sets the position of the source stream.
        /// </summary>
        public override long Position
        {
            get
            {
                return this.source.Position;
            }

            set
            {
                this.source.Position = value;
            }
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
        /// Closes the source stream and also closes the additional objects that are carried.
        /// </summary>
        public override void Close()
        {
            this.source.Close();

            foreach (IDisposable cargoObject in this.cargo)
            {
                cargoObject.Dispose();
            }
        }
#endif

        /// <summary>
        /// Disposes source stream and additional objects carried
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.source.Dispose();

                foreach (IDisposable cargoObject in this.cargo)
                {
                    cargoObject.Dispose();
                }

            }
        }

        /// <summary>
        /// Reads from the source stream.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer
        /// contains the specified byte array with the values between offset and
        /// (offset + count - 1) replaced by the bytes read from the source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin
        /// storing the data read from the stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less
        /// than the number of bytes requested if that many bytes are not currently available,
        /// or zero (0) if the end of the stream has been reached.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.source.Read(buffer, offset, count);
        }

        /// <summary>
        /// Writes to the source stream.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count
        /// bytes from buffer to the stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which
        /// to begin copying bytes to the stream.</param>
        /// <param name="count">The number of bytes to be written to the stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            this.source.Write(buffer, offset, count);
        }

        /// <summary>
        /// Changes the position of the source stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type SeekOrigin indicating the reference
        /// point used to obtain the new position.</param>
        /// <returns>The new position within the stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.source.Seek(offset, origin);
        }
    }
}
