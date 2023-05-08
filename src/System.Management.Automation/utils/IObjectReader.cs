// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// PipelineReader provides asynchronous access to the stream of objects emitted by
    /// a <see cref="System.Management.Automation.Runspaces.Pipeline"/>.
    /// </summary>
    /// <seealso cref="System.Management.Automation.Runspaces.Pipeline.Output"/>
    /// <seealso cref="System.Management.Automation.Runspaces.Pipeline.Error"/>
    public abstract class PipelineReader<T>
    {
        /// <summary>
        /// Event fired when data is added to the buffer.
        /// </summary>
        public abstract event EventHandler DataReady;

        /// <summary>
        /// Signaled when data is available.
        /// </summary>
        public abstract WaitHandle WaitHandle
        {
            get;
        }

        /// <summary>
        /// Check if the stream is closed and contains no data.
        /// </summary>
        /// <value>True if the stream is closed and contains no data, otherwise false</value>
        /// <remarks>
        /// Attempting to read from the underlying stream if EndOfPipeline is true returns
        /// zero objects.
        /// </remarks>
        public abstract bool EndOfPipeline
        {
            get;
        }

        /// <summary>
        /// Check if the stream is open for further writes.
        /// </summary>
        /// <value>true if the underlying stream is open, otherwise false</value>
        /// <remarks>
        /// The underlying stream may be readable after it is closed if data remains in the
        /// internal buffer. Check <see cref="EndOfPipeline"/> to determine if
        /// the underlying stream is closed and contains no data.
        /// </remarks>
        public abstract bool IsOpen
        {
            get;
        }

        /// <summary>
        /// Returns the number of objects currently available in the underlying stream.
        /// </summary>
        public abstract int Count
        {
            get;
        }

        /// <summary>
        /// Get the capacity of the stream.
        /// </summary>
        /// <value>
        /// The capacity of the stream.
        /// </value>
        /// <remarks>
        /// The capacity is the number of objects that stream may contain at one time.  Once this
        /// limit is reached, attempts to write into the stream block until buffer space
        /// becomes available.
        /// </remarks>
        public abstract int MaxCapacity
        {
            get;
        }

        /// <summary>
        /// Close the stream.
        /// </summary>
        /// <remarks>
        /// Causes subsequent calls to IsOpen to return false and calls to
        /// a write operation to throw an PipelineClosedException.
        /// All calls to Close() after the first call are silently ignored.
        /// </remarks>
        /// <exception cref="PipelineClosedException">
        /// The stream is already disposed
        /// </exception>
        public abstract void Close();

        /// <summary>
        /// Read at most <paramref name="count"/> objects.
        /// </summary>
        /// <param name="count">The maximum number of objects to read.</param>
        /// <returns>The objects read.</returns>
        /// <remarks>
        /// This method blocks if the number of objects in the stream is less than <paramref name="count"/>
        /// and the stream is not closed.
        /// </remarks>
        public abstract Collection<T> Read(int count);

        /// <summary>
        /// Read a single object from the stream.
        /// </summary>
        /// <returns>The next object in the stream.</returns>
        /// <remarks>This method blocks if the stream is empty</remarks>
        public abstract T Read();

        /// <summary>
        /// Blocks until the pipeline closes and reads all objects.
        /// </summary>
        /// <returns>A collection of zero or more objects.</returns>
        /// <remarks>
        /// If the stream is empty, an empty collection is returned.
        /// </remarks>
        public abstract Collection<T> ReadToEnd();

        /// <summary>
        /// Reads all objects currently in the stream, but does not block.
        /// </summary>
        /// <returns>A collection of zero or more objects.</returns>
        /// <remarks>
        /// This method performs a read of all objects currently in the
        /// stream.  If there are no objects in the stream,
        /// an empty collection is returned.
        /// </remarks>
        public abstract Collection<T> NonBlockingRead();

        // 892370-2003/10/29-JonN added this method
        /// <summary>
        /// Reads objects currently in the stream, but does not block.
        /// </summary>
        /// <returns>A collection of zero or more objects.</returns>
        /// <remarks>
        /// This method performs a read of objects currently in the
        /// stream.  If there are no objects in the stream,
        /// an empty collection is returned.
        /// </remarks>
        /// <param name="maxRequested">
        /// Return no more than maxRequested objects.
        /// </param>
        public abstract Collection<T> NonBlockingRead(int maxRequested);

        /// <summary>
        /// Peek the next object, but do not remove it from the stream.  Non-blocking.
        /// </summary>
        /// <returns>
        /// The next object in the stream or AutomationNull.Value if the stream is empty
        /// </returns>
        /// <exception cref="PipelineClosedException">The stream is closed.</exception>
        public abstract T Peek();

        #region IEnumerable<T> Members

        /// <summary>
        /// Returns an enumerator that reads the items in the pipeline.
        /// </summary>
        internal IEnumerator<T> GetReadEnumerator()
        {
            while (!this.EndOfPipeline)
            {
                T t = this.Read();
                if (object.Equals(t, System.Management.Automation.Internal.AutomationNull.Value))
                {
                    yield break;
                }
                else
                {
                    yield return t;
                }
            }
        }

        #endregion
    }
}
